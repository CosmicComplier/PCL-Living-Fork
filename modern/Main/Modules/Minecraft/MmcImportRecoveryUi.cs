using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using MeloongCore;
using PCLCS;

namespace PCL;

// Original LF host integration; repository LICENCE applies.
public sealed class MmcImportRecoveryUiServices {
    public MmcMetadataStore Metadata { get; init; }
    public MmcArtifactCacheRoots CacheRoots { get; init; }
    public Func<MmcImportRecoveryCatalog, int?> SelectEntry { get; init; }
    // Uses the existing confirmation button values: 1 recovers, 3 starts a fresh retry.
    public Func<MmcImportRecoveryEntry, int> SelectAction { get; init; }
    public Func<MmcImportRecoveryEntry, string> SelectArchive { get; init; }
    public Func<MmcImportRecoveryEntry, string> SelectName { get; init; }
    public Func<MmcImportRecoveryEntry, string, string, bool> ConfirmRetry { get; init; }
    public Action<ModLoader.LoaderCombo<string>> RegisterTask { get; init; }
    public Action<MmcImportRecoveryResult> Recovered { get; init; }
    public Action<MmcInstanceBackendResult> Retried { get; init; }
    public Action<ModNet.LoaderDownload> DownloadCreated { get; init; }
    public Action<MmcTransactionCheckpoint, int> TransactionObserved { get; init; }
    public Action<MmcInstanceBackendProgress> ProgressObserved { get; init; }
    public Action<Exception, string> ErrorObserved { get; init; }
}

internal static class MmcImportRecoveryUi {
    private static int busy;
    internal static bool HasActiveOperation => Volatile.Read(ref busy) != 0;
    internal static Func<string, MmcImportRecoveryUiServices> ServicesFactory { get; set; }

    internal static void PromptCatalog(string minecraftRoot) {
        string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(minecraftRoot));
        var services = ServicesFactory?.Invoke(root) ?? Services(root);
        ModBase.RunInNewThread(() => {
            try {
                var catalog = MmcImportRecovery.Read(root);
                ModBase.RunInUi(() => {
                    try {
                        if (!PathUtils.IsSame(root, ModMinecraft.McFolderSelected)) { ModMain.Hint("游戏文件夹已切换，请重新打开导入恢复列表。", ModMain.HintType.Blue); return; }
                        if (catalog.Entries.IsEmpty) { ModMain.MyMsgBox("当前游戏文件夹没有 MMC 初始导入恢复记录。", "MMC 导入恢复"); return; }
                        if (catalog.Truncated) ModMain.Hint("恢复记录较多，本次仅显示有界范围内的记录；其余现场仍被保留。", ModMain.HintType.Blue);
                        int? selected = services.SelectEntry is not null ? services.SelectEntry(catalog) : ModMain.MyMsgBoxSelect(catalog.Entries.Select(entry => (IMyRadio)new MyRadioBox {
                            Text = entry.Name + " · " + StateLabel(entry.State), ToolTip = Details(entry)
                        }), "MMC 导入恢复", "下一步", "取消");
                        if (selected is null || selected < 0 || selected >= catalog.Entries.Length) return;
                        var entry = catalog.Entries[selected.Value];
                        if (DiagnosticOnly(entry) || !entry.CanRecover && !entry.CanRetry) { ModMain.MyMsgBox(Details(entry), "导入记录"); return; }
                        int action = services.SelectAction is not null ? services.SelectAction(entry) : ChooseAction(entry);
                        if (action == 1 && entry.CanRecover) StartRecovery(entry, services);
                        else if (action == 3 && entry.CanRetry) PromptRetry(entry, services);
                    } catch (Exception error) { Report(services, error, "无法开始 MMC 导入恢复"); }
                });
            } catch (Exception error) { Report(services, error, "读取 MMC 导入恢复列表失败"); }
        }, "Read MMC initial import recovery");
    }

    private static int ChooseAction(MmcImportRecoveryEntry entry) {
        if (entry.CanRecover) return ModMain.MyMsgBox(Details(entry) + "\n\n恢复会核对原记录并完成已有导入决定，不覆盖同名内容。恢复开始后将等待实际文件操作结束，不能中途取消。",
            "确认恢复 MMC 导入", "恢复导入", "取消", entry.CanRetry ? "用同一原包重新导入" : "", IsWarn: true);
        return ModMain.MyMsgBox(Details(entry) + "\n\n旧现场保持原样。只能选择相同 SHA256 的原 MMC ZIP，使用不同名称重新导入。",
            "重新导入 MMC 原包", "选择原包", "取消", IsWarn: true) == 1 ? 3 : 0;
    }

    private static void PromptRetry(MmcImportRecoveryEntry entry, MmcImportRecoveryUiServices services) {
        RequireAction(entry, recover: false);
        string archive = services.SelectArchive is not null ? services.SelectArchive(entry) : MeloongCore.Wpf.Dialogs.SelectFile("选择相同 SHA256 的 MMC 原始 ZIP", false,
            filter: new[] { (new[] { "zip" }, "MMC 原始整合包") }).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(archive)) return;
        string name = services.SelectName is not null ? services.SelectName(entry) : ModMain.MyMsgBoxInput("重新导入到不同名称", "原导入现场和原目标名称会保留，请为新实例选择不同名称。", entry.Name + "-重试",
            new Collection<Validate> { new ValidateFolderName(Path.Combine(entry.MinecraftRoot, "versions")) });
        if (string.IsNullOrWhiteSpace(name)) return;
        ValidateRetryName(entry, name);
        bool confirmed = services.ConfirmRetry is not null ? services.ConfirmRetry(entry, archive, name) : ModMain.MyMsgBox(
            "使用所选原包创建新实例“" + name + "”？\n原包 SHA256 必须为：\n" + entry.PackageSha256 + "\n\n旧导入现场及其数据保留，不会采用或覆盖旧半成品。",
            "确认重新导入 MMC", "重新导入", "取消", IsWarn: true) == 1;
        if (confirmed) StartRetry(entry, archive, name, services);
    }

    internal static ModLoader.LoaderCombo<string> StartRecovery(MmcImportRecoveryEntry entry, MmcImportRecoveryUiServices services) {
        RequireAction(entry, recover: true);
        var fence = EnterBusy();
        var worker = new MmcOperationWorker<string, MmcImportRecoveryResult>(fence, "核对并恢复 MMC 初始导入", task => {
            // Recovery replays durable decisions. The fence rejects an earlier cancellation,
            // then keeps the genuine producer attached until noncancelable recovery IO returns.
            fence.EnterDecision(fence.Token);
            var result = MmcImportRecovery.RecoverAsync(entry).GetAwaiter().GetResult(); task.Output = result;
            if (result.Status != MmcInstanceBackendStatus.Succeeded || result.Paths is null)
                throw new IOException("MMC 导入恢复未完成，所有现场已保留：" + result.Diagnostic + (result.FreshImportRequired ? "\n请重新打开列表，选择同 SHA 原包并使用不同名称重新导入。" : ""));
            if (services.Recovered is not null) services.Recovered(result);
            else Publish(entry.MinecraftRoot, "MMC 导入已恢复。", services);
        }) { InterruptOnCancel = false };
        return Register(new MmcOperationCombo<string>(fence, "恢复 MMC 导入：" + entry.Name, [worker]), entry.OperationDirectory, services, fence);
    }

    internal static ModLoader.LoaderCombo<string> StartRetry(MmcImportRecoveryEntry entry, string archive, string name, MmcImportRecoveryUiServices services) {
        RequireAction(entry, recover: false); ValidateRetryName(entry, name);
        if (services.Metadata is null || services.CacheRoots is null) throw new IOException("MMC 重新导入缺少下载和元数据服务。");
        var fence = EnterBusy();
        var progress = new MmcImportProgress();
        var worker = new MmcOperationWorker<string, MmcInstanceBackendResult>(fence, "读取 MMC 原包", task => {
            progress.Attach(task); progress.Begin(MmcImportStage.ReadArchive);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(task.CreateCancellationToken(), fence.Token);
            var token = linked.Token;
            using var source = MmcArchiveSourceReader.ReadAsync(archive, cancellationToken: token,
                progress: new RetryArchiveProgress(progress, services.ProgressObserved)).GetAwaiter().GetResult();
            progress.SourceRead(source);
            if (!string.Equals(source.PackageSha256, entry.PackageSha256, StringComparison.Ordinal)) throw new IOException("所选 ZIP 的 SHA256 与原导入记录不同，请选择同一个原包。");
            progress.Begin(MmcImportStage.ResolveComponents);
            var resolved = MmcMetadataResolution.ResolveAsync(source.Package, services.Metadata, cancellationToken: token).GetAwaiter().GetResult();
            if (!resolved.Resolution.IsComplete || resolved.Resolution.Profile is null) throw new IOException("无法完整解析所选 MMC 原包组件。");
            progress.Begin(MmcImportStage.SelectJava);
            var environment = MmcInstances.SelectJavaAsync(source.Package, resolved.Resolution.Profile, token).GetAwaiter().GetResult();
            var backend = new MmcInstanceBackend<ModNet.NetFile>(services.Metadata, services.CacheRoots, MmcDownloads.BuildFile,
                (files, cancellation) => progress.DownloadAsync(files, cancellation, services.DownloadCreated));
            progress.RecheckSource();
            // Pass the selected backend entry unchanged. The backend rechecks the preview,
            // original package SHA and different destination before doing a fresh import.
            var result = backend.RetryInitialImportAsync(entry, new(archive, Path.Combine(entry.MinecraftRoot, "versions", name), name, environment) {
                TransactionCheckpoint = (phase, index) => {
                    if (phase == MmcTransactionCheckpoint.BeforeDecision) { fence.EnterDecision(token); progress.EnteredDecision(); }
                    services.TransactionObserved?.Invoke(phase, index);
                }
            }, token, new RetryProgress(progress, services.ProgressObserved)).GetAwaiter().GetResult(); task.Output = result;
            if (result.Status == MmcInstanceBackendStatus.Cancelled) throw new OperationCanceledException(result.Failure, token);
            if (result.Status != MmcInstanceBackendStatus.Succeeded || result.Paths is null) throw new IOException("MMC 重新导入未完成，旧现场保留：" + result.Failure +
                (result.WorkingRoot is null ? "" : "\n本次导入现场：" + result.WorkingRoot + "\n可在版本选择页的“MMC 导入恢复”中查看记录。"));
            if (services.Retried is not null) services.Retried(result);
            else Publish(entry.MinecraftRoot, "MMC 原包已重新导入，旧导入现场保留。", services);
        }) { InterruptOnCancel = false };
        return Register(new MmcImportOperationCombo<string>(fence, progress, "重新导入 MMC 原包：" + name, [worker]), archive, services, fence);
    }

    private static bool DiagnosticOnly(MmcImportRecoveryEntry entry) => entry.State is MmcImportRecoveryState.ProducerActive or MmcImportRecoveryState.Unknown or MmcImportRecoveryState.Conflict or MmcImportRecoveryState.CloneOnly or MmcImportRecoveryState.Completed;
    private static void RequireAction(MmcImportRecoveryEntry entry, bool recover) {
        ArgumentNullException.ThrowIfNull(entry);
        if (DiagnosticOnly(entry) || !(recover ? entry.CanRecover : entry.CanRetry)) throw new IOException("当前导入记录仅供诊断，不能执行此操作：" + entry.Description);
    }
    private static void ValidateRetryName(MmcImportRecoveryEntry entry, string name) {
        string error = new ValidateFolderName(Path.Combine(entry.MinecraftRoot, "versions")).Validate(name);
        if (!string.IsNullOrEmpty(error)) throw new IOException(error);
        string target = Path.Combine(entry.MinecraftRoot, "versions", name);
        if (string.Equals(name, entry.Name, StringComparison.OrdinalIgnoreCase) || new[] { entry.TemporaryRoot, entry.FinalRoot }.OfType<string>().Any(path => PathUtils.IsSame(path, target)))
            throw new IOException("重新导入必须使用不同的新名称，原现场及其原目标名称会保留。");
    }
    private static MmcOperationCancellationFence EnterBusy() {
        if (Interlocked.CompareExchange(ref busy, 1, 0) != 0) throw new IOException("另一项 MMC 导入恢复或重新导入尚未结束。");
        return new(() => Interlocked.Exchange(ref busy, 0));
    }
    private static ModLoader.LoaderCombo<string> Register(ModLoader.LoaderCombo<string> combo, string input, MmcImportRecoveryUiServices services, MmcOperationCancellationFence fence) {
        try {
            if (services.RegisterTask is not null) services.RegisterTask(combo);
            else { ModLoader.LoaderTaskbarAdd(combo); ModBase.RunInUi(() => ModMain.FrmMain?.BtnExtraDownload.ShowRefresh()); }
            combo.Start(input); return combo;
        } catch { fence.AbandonUnstarted(); throw; }
    }
    private static void Publish(string root, string message, MmcImportRecoveryUiServices services) => ModBase.RunInUi(() => {
        try {
            ModMinecraft.McInstanceListForceRefresh = true;
            if (PathUtils.IsSame(root, ModMinecraft.McFolderSelected)) ModMinecraft.McInstanceListLoader.Start(root, IsForceRestart: true);
            ModMain.Hint(message, ModMain.HintType.Green);
        } catch (Exception error) { Report(services, error, "刷新 MMC 导入结果失败"); }
    });
    private static void Report(MmcImportRecoveryUiServices services, Exception error, string message) {
        if (services.ErrorObserved is not null) services.ErrorObserved(error, message);
        else Logger.Error(error, message, LogBehavior.Alert);
    }
    private static string Details(MmcImportRecoveryEntry entry) => entry.Description + (entry.PackageSha256 is null ? "" : "\n原包 SHA256：" + entry.PackageSha256);
    private static string StateLabel(MmcImportRecoveryState state) => state switch {
        MmcImportRecoveryState.ProducerActive => "生产者仍在运行", MmcImportRecoveryState.Recoverable => "可恢复", MmcImportRecoveryState.OriginalRequired => "需同一原包",
        MmcImportRecoveryState.Completed => "已完成", MmcImportRecoveryState.LegacyUnproven => "旧版残留", MmcImportRecoveryState.CloneOnly => "克隆记录",
        MmcImportRecoveryState.Conflict => "存在冲突", _ => "无法验证"
    };
    private static MmcImportRecoveryUiServices Services(string root) {
        string cache = Path.Combine(root, "PCL", "MmcCache");
        return new() { Metadata = MmcDownloads.CreateMetadataStore(Path.Combine(cache, "metadata")),
            CacheRoots = new(Path.Combine(root, "libraries"), Path.Combine(root, "assets"), Path.Combine(cache, "sources"), Path.Combine(root, "versions")) };
    }
    private sealed class RetryArchiveProgress(MmcImportProgress progress, Action<MmcInstanceBackendProgress> observed) : IProgress<MmcArchiveProgress> {
        public void Report(MmcArchiveProgress value) {
            progress.Archive(value); observed?.Invoke(new(value.Phase, value.CompletedBytes, value.TotalBytes));
        }
    }
    private sealed class RetryProgress(MmcImportProgress progress, Action<MmcInstanceBackendProgress> observed) : IProgress<MmcInstanceBackendProgress> {
        public void Report(MmcInstanceBackendProgress value) {
            progress.Backend(value); observed?.Invoke(value);
        }
    }
}
