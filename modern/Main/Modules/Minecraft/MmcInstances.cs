using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MeloongCore;
using MeloongCore.Extensions;
using Newtonsoft.Json.Linq;
using PCLCS;
using PCLLF.MMCC.Core;

namespace PCL;

// Original LF host integration; governed by the repository LICENCE.
/// <summary>Small host seams for owned facade fixtures; production still uses the genuine Loader/NetFile backend.</summary>
public sealed class MmcImportServices {
    public MmcMetadataStore Metadata { get; init; }
    public MmcArtifactCacheRoots CacheRoots { get; init; }
    public Func<string, string> SelectName { get; init; }
    public Action<ModLoader.LoaderCombo<string>> RegisterTask { get; init; }
    public Action<MmcInstanceBackendResult> Published { get; init; }
    public Action<ModNet.LoaderDownload> DownloadCreated { get; init; }
    public Action<MmcTransactionCheckpoint, int> TransactionObserved { get; init; }
    public Action<MmcInstanceBackendProgress> ProgressObserved { get; init; }
}

public static class MmcInstances {
    // Only transport/UI boundaries are replaceable. Source parsing, Java probing/filtering, file
    // publication, cancellation and the Loader workload are always the production implementations.
    internal static Func<ModpackMmcInstallRequest, MmcImportServices> ServicesFactory { get; set; }

    public static ModLoader.LoaderCombo<string> StartImport(ModpackMmcInstallRequest request) {
        ArgumentNullException.ThrowIfNull(request);
        var services = ServicesFactory?.Invoke(request) ?? CreateServices(request);
        if (services.Metadata is null || services.CacheRoots is null) throw new InvalidOperationException("MMC 导入缺少下载和元数据服务。");
        var cancellation = new ImportCancellationFence();
        ModLoader.LoaderCombo<string> combo = null;
        var worker = new ImportWorker<string, MmcInstanceBackendResult>(cancellation, task => {
            try { task.Output = ImportAsync(task, request, services, name => combo.Name = "MMC 整合包安装：" + name, cancellation).GetAwaiter().GetResult(); }
            finally { cancellation.Complete(); }
        }) { InterruptOnCancel = false };
        combo = new ImportCombo<string>(cancellation, "MMC 整合包安装：" + Path.GetFileNameWithoutExtension(request.ArchivePath), [worker]);
        // Deliberately does not attach McInstallState: its legacy one-second failure cleanup does
        // not own MMC import reservations or durable recovery journals.
        if (services.RegisterTask is not null) services.RegisterTask(combo);
        else {
            ModLoader.LoaderTaskbarAdd(combo);
            ModBase.RunInUi(() => ModMain.FrmMain.BtnExtraDownload.ShowRefresh());
        }
        combo.Start(request.ArchivePath);
        return combo;
    }

    private static async Task<MmcInstanceBackendResult> ImportAsync(ModLoader.LoaderTask<string, MmcInstanceBackendResult> task,
        ModpackMmcInstallRequest request, MmcImportServices services, Action<string> named, ImportCancellationFence cancellation) {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(task.CreateCancellationToken(), cancellation.Token);
        CancellationToken token = linked.Token;
        using var source = await MmcArchiveSourceReader.ReadAsync(request.ArchivePath, cancellationToken: token,
            progress: new SyncProgress<MmcArchiveProgress>(value => { if (value.TotalBytes > 0) task.Progress = Math.Min(0.15, 0.15 * value.CompletedBytes / value.TotalBytes); })).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        string candidate = request.InstanceName ?? source.Package.Configuration.Name;
        string name = services.SelectName is not null ? services.SelectName(candidate) : SelectName(request.MinecraftFolder, candidate);
        if (string.IsNullOrWhiteSpace(name)) throw new OperationCanceledException(token);
        var validator = new ValidateFolderName(Path.Combine(request.MinecraftFolder, "versions"));
        string validation = validator.Validate(name);
        if (!string.IsNullOrEmpty(validation)) throw new InvalidDataException("整合包实例名称无效：" + validation);
        named(name); task.Name = "解析整合包组件";
        var resolved = await MmcMetadataResolution.ResolveAsync(source.Package, services.Metadata, cancellationToken: token).ConfigureAwait(false);
        if (!resolved.Resolution.IsComplete || resolved.Resolution.Profile is null)
            throw new InvalidDataException("无法解析 MMC 组件：" + string.Join("；", resolved.Resolution.Diagnostics.Where(value => value.Severity == MmcDiagnosticSeverity.Error).Select(value => value.Message)));
        task.Name = "选择整合包 Java";
        var environment = await SelectJavaAsync(source.Package, resolved.Resolution.Profile, token).ConfigureAwait(false);
        string target = Path.Combine(request.MinecraftFolder, "versions", name);
        var backend = new MmcInstanceBackend<ModNet.NetFile>(services.Metadata, services.CacheRoots, MmcDownloads.BuildFile,
            (files, cancellation) => MmcDownloads.RunBatchAsync(files, cancellation, task.CreateSyncProgressProvider(ToPercentage: 0.85), services.DownloadCreated));
        task.Name = "安装 MMC 实例环境";
        var result = await backend.ImportPreparedAsync(source, new(request.ArchivePath, target, name, environment) {
            TransactionCheckpoint = (phase, index) => {
                if (phase == MmcTransactionCheckpoint.BeforeDecision) cancellation.EnterDecision(token);
                services.TransactionObserved?.Invoke(phase, index);
            }
        }, token,
            new SyncProgress<MmcInstanceBackendProgress>(value => {
                if (value.Phase == "publishing-instance") task.Name = "保存 MMC 实例环境";
                services.ProgressObserved?.Invoke(value);
            }), resolved).ConfigureAwait(false);
        if (result.Status == MmcInstanceBackendStatus.Cancelled) throw new OperationCanceledException(result.Failure, token);
        if (result.Status != MmcInstanceBackendStatus.Succeeded || result.Definition is null || result.Paths is null)
            throw new IOException("MMC 整合包未完成安装，现场已保留：" + result.Failure);
        task.Progress = 1;
        ModMinecraft.McInstanceListForceRefresh = true;
        ModMinecraft.McInstanceListLoader.Start(request.MinecraftFolder, IsForceRestart: true);
        if (services.Published is not null) services.Published(result);
        else {
            ModBase.RunInUi(() => ModMain.Hint("MMC 整合包安装完成：" + name));
        }
        return result;
    }

    private static string SelectName(string minecraftFolder, string candidate) => ModBase.RunInUiWait(() => {
        var rule = new ValidateFolderName(Path.Combine(minecraftFolder, "versions"));
        if (!string.IsNullOrWhiteSpace(candidate) && rule.Validate(candidate) == "") return candidate;
        return ModMain.MyMsgBoxInput("输入版本名称", "", "", new Collection<Validate> { rule });
    });

    private static async Task<MmcRuntimeEnvironment> SelectJavaAsync(MmcPackage package, MmcResolvedProfile resolved, CancellationToken token) {
        var provisional = MmcProfileCompiler.Compile(resolved, new("windows", "x86_64", 0));
        if (provisional.HasErrors || provisional.Profile is null) throw new InvalidDataException("整合包启动配置无法解析。" + string.Join("；", provisional.Diagnostics.Select(value => value.Message)));
        var policy = provisional.Profile.JavaPolicy;
        if (policy.Kind == MmcJavaPolicyKind.Unsatisfiable) throw new InvalidDataException("整合包声明的 Java 要求无法同时满足。");
        string forced = null;
        string legacy = Settings.Get<string>("LaunchArgumentJavaSelect");
        if (legacy?.StartsWith("{", StringComparison.Ordinal) == true) {
            try { forced = JObject.Parse(legacy).Value<string>("Path"); } catch (Newtonsoft.Json.JsonException) { throw new InvalidDataException("全局指定 Java 设置无效，请重新选择。"); }
        }
        string recommended = package.Configuration.Values.TryGetValue("OverrideJavaLocation", out string flag) && bool.TryParse(flag, out bool enabled) && enabled
            ? package.Configuration.Values.GetValueOrDefault("JavaPath") : null;
        for (int attempt = 0; attempt < 2; attempt++) {
            var candidates = Configs.JavaList.Get()!.ToArray();
            if (!string.IsNullOrEmpty(forced)) candidates = candidates.Where(java => SameJavaPath(java, forced)).ToArray();
            else if (!string.IsNullOrWhiteSpace(recommended)) candidates = candidates.OrderByDescending(java => SameJavaPath(java, recommended)).ToArray();
            foreach (Java candidate in candidates) {
                token.ThrowIfCancellationRequested();
                if (!await candidate.CheckAsync(token).ConfigureAwait(false) || candidate.Version is null || !policy.Allows(candidate.Version.Major)) continue;
                string architecture = JavaArchitecture(candidate.JavaExePath);
                var actual = MmcProfileCompiler.Compile(resolved, new("windows", architecture, candidate.Version.Major, 64));
                if (!actual.HasErrors && actual.Profile is not null && actual.Profile.JavaPolicy.Allows(candidate.Version.Major)) return actual.Profile.Environment;
            }
            if (!string.IsNullOrEmpty(forced)) break;
            if (attempt == 0) {
                ModJava.JavaListRefreshWorker.Start(cancellationToken: token);
                ModJava.JavaListRefreshWorker.WaitIfRunning(cancellationToken: token);
            }
        }
        string versions = policy.Kind == MmcJavaPolicyKind.DeclaredCandidates ? string.Join("、", policy.AllowedMajors) : "符合整合包要求的版本";
        throw new InvalidOperationException("未找到兼容的 Java（" + versions + "）。请在设置 → 启动 → Java 列表中下载或导入兼容版本后重试。指定的 Java 也必须满足整合包声明。");
    }
    private static bool SameJavaPath(Java candidate, string path) {
        try { return PathUtils.IsSame(candidate.Folder, path) || PathUtils.IsSame(candidate.JavaExePath, path); } catch (ArgumentException) { return false; }
    }
    private static string JavaArchitecture(string executable) {
        using FileStream input = File.OpenRead(executable); using BinaryReader reader = new(input);
        if (input.Length < 64 || reader.ReadUInt16() != 0x5a4d) throw new InvalidDataException("Java 入口不是有效的 Windows 程序。");
        input.Position = 0x3c; int offset = reader.ReadInt32();
        if (offset < 64 || offset > input.Length - 6) throw new InvalidDataException("Java 程序头损坏。");
        input.Position = offset; if (reader.ReadUInt32() != 0x00004550) throw new InvalidDataException("Java 程序头无效。");
        return reader.ReadUInt16() switch { 0x8664 => "x86_64", 0xaa64 => "arm64", _ => throw new InvalidDataException("MMC 当前需要受支持的 64 位 Java。") };
    }
    private static MmcImportServices CreateServices(ModpackMmcInstallRequest request) {
        string cache = Path.Combine(request.MinecraftFolder, "PCL", "MmcCache");
        return new() {
            Metadata = MmcDownloads.CreateMetadataStore(Path.Combine(cache, "metadata")),
            CacheRoots = new(Path.Combine(request.MinecraftFolder, "libraries"), Path.Combine(request.MinecraftFolder, "assets"), Path.Combine(cache, "sources"), Path.Combine(request.MinecraftFolder, "versions"))
        };
    }
    private sealed class SyncProgress<T>(Action<T> report) : IProgress<T> { public void Report(T value) => report(value); }
    private sealed class ImportCancellationFence {
        private readonly object gate = new();
        private readonly CancellationTokenSource source = new();
        private bool finishing, completed;
        public CancellationToken Token => source.Token;
        public void Cancel(Action standardCancel) {
            lock (gate) {
                if (finishing || completed) return;
                source.Cancel(); standardCancel();
            }
        }
        public void EnterDecision(CancellationToken token) {
            lock (gate) { token.ThrowIfCancellationRequested(); finishing = true; }
        }
        public void Complete() { lock (gate) { completed = true; source.Dispose(); } }
    }
    // The existing scheduler reads the concrete type's first generic argument as its input
    // contract. Keep the MMC cancellation adapters generic so that contract remains available.
    private sealed class ImportWorker<TIn, TOut>(ImportCancellationFence fence, Action<ModLoader.LoaderTask<TIn, TOut>> work)
        : ModLoader.LoaderTask<TIn, TOut>("读取 MMC 整合包", work) {
        public override void Cancel() => fence.Cancel(() => base.Cancel());
    }
    private sealed class ImportCombo<TIn>(ImportCancellationFence fence, string name, IEnumerable<ModLoader.LoaderBase> children)
        : ModLoader.LoaderCombo<TIn>(name, children) {
        public override void Cancel() => fence.Cancel(() => base.Cancel());
    }
}
