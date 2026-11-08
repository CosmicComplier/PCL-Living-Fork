using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using MeloongCore;
using PCLCS;
using PCLLF.MMCC.Core;

namespace PCL;

// Original LF host integration; governed by the repository LICENCE.
public static class MmcLaunch {
    private const string WrapperSha256 = "88eb3ee58854e916437831697ef6a2fd493da9bc4ffb018f620cd88f090952aa";
    private static readonly ConcurrentDictionary<Guid, MmcRunningSession> Active = new();
    private static int closeAfterSessions;
    public static bool HasActiveSessions => !Active.IsEmpty;
    // Observes a genuinely registered child; tests may request cancellation here,
    // but cannot replace process creation, the session receipt, or Java pinning.
    internal static Action<Guid, Process> ProcessCreated { get; set; }
    internal static Func<MmcJavaPinStore> JavaPins { get; set; } = () => ModJava.MmcPins;
    internal static Func<MmcMutableRuntimeAuthority> MutableRuntimeAuthorityFactory { get; set; } = () => MmcMutableRuntimeAuthority.Default;
    internal static Func<ModMinecraft.McInstance, MmcLaunchJavaSelection, bool> ConfirmVariantAdoption { get; set; } = (_, selection) =>
        ModBase.RunInUiWait(() => ModMain.MyMsgBox("此实例尚未使用 Java " + selection.RuntimeJava.Major +
            "。是否按整合包声明配置它并启动？", "配置 Java 环境", "配置并启动", "取消") == 1);
    public static bool KeepAliveWhileSessionsFinish() {
        if (Active.IsEmpty) return false;
        Interlocked.Exchange(ref closeAfterSessions, 1);
        return true;
    }

    public static IList<LegacyLaunchStep> CreateSteps(ModMinecraft.McInstance instance, ModLaunch.McLaunchOptions options,
        Action<Java> selectedJava) {
        var context = new Context(instance, options, selectedJava);
        return new List<LegacyLaunchStep> {
            new LegacyLaunchIntStep { Name = "准备 MMC 实例环境", Work = context.Prepare, ProgressWeight = 15, InterruptOnCancel = false },
            new LegacyLaunchIntStep { Name = "执行自定义命令", Work = context.Custom, InterruptOnCancel = false },
            new LegacyLaunchProcessStep { Name = "启动 MMC 游戏", Work = context.Start, ProgressWeight = 2, InterruptOnCancel = false },
            new LegacyLaunchWaitStep { Name = "等待游戏窗口出现", Work = context.Wait, InterruptOnCancel = false }
        };
    }

    private sealed class Context(ModMinecraft.McInstance instance, ModLaunch.McLaunchOptions options, Action<Java> selectedJava) {
        private readonly Guid sessionId = Guid.NewGuid();
        private MmcSavedInstanceSources saved;
        private MmcPreparedInstanceLaunch prepared;
        private MmcLaunchPreferenceSnapshot preferences;
        private MmcLaunchJavaSelection java;
        private MmcRuntimeLaunchPlan runtime;
        private MmcInstanceLaunchSupport[] support;
        private ModWatcher.Watcher watcher;
        private MmcRunningSession session;
        private MmcJavaPinStore pins;
        private MmcMutableRuntimeAuthority runtimeAuthority;
        private int canceled;
        private readonly TaskCompletionSource monitoringFinished = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Prepare(LegacyLaunchTask<int, int> task) => PrepareAsync(task.CreateCancellationToken(), task.CreateSyncProgressProvider()).GetAwaiter().GetResult();
        private async Task PrepareAsync(CancellationToken token, ProgressProvider progress) {
            var operation = ModLaunch.McLaunchLoaderReal;
            if (operation is not null) lock (operation.LockState) {
                operation.HasOnStateChangedThread = true;
                operation.OnStateChangedThread += (_, next, _) => { if (next == ModBase.LoadState.Canceled) CancelGame(); };
                if (operation.State == ModBase.LoadState.Canceled) CancelGame();
            }
            if (options.SaveBatch is not null) throw new InvalidOperationException("MMC 启动参数包含会话绑定信息，当前不能导出普通启动脚本。");
            var paths = MmcInstancePaths.Resolve(instance.PathVersion);
            pins = JavaPins() ?? throw new IOException("MMC Java 保护服务不可用。");
            runtimeAuthority=MutableRuntimeAuthorityFactory()??throw new IOException("MMC 会话临时目录授权不可用。");
            var primary = MmcSavedSourceStore.ReadPrimary(paths);
            paths = MmcInstancePaths.Resolve(instance.PathVersion, primary.Definition.Layout.GameRootLeaf);
            ReconcilePrevious(paths, primary.Definition.InstanceId, pins, token,runtimeAuthority);
            saved = await MmcSavedSourceStore.LoadAsync(paths, primary.Definition, token).ConfigureAwait(false);
            preferences = MmcLaunchPreferences.Capture(instance, saved.PackageConfiguration, sessionId);
            java = await MmcLaunchJava.SelectAsync(instance, primary.Definition, saved.Resolved.Resolution.Profile,
                preferences.JavaPreference, token).ConfigureAwait(false);
            selectedJava(java.Java);
            string gameRoot = Path.GetDirectoryName(Path.GetDirectoryName(paths.InstanceRoot))!;
            string cache = Path.Combine(gameRoot, "PCL", "MmcCache");
            var backend = new MmcInstanceBackend<ModNet.NetFile>(MmcDownloads.CreateMetadataStore(Path.Combine(cache, "metadata")),
                new(Path.Combine(gameRoot, "libraries"), Path.Combine(gameRoot, "assets"), Path.Combine(cache, "sources"), Path.Combine(gameRoot, "versions")),
                MmcDownloads.BuildFile, (files, cancellation) => MmcDownloads.RunBatchAsync(files, cancellation, progress));
            if (java.RequiresVariantAdoption) {
                if (!ConfirmVariantAdoption(instance, java)) throw new OperationCanceledException(token);
                token.ThrowIfCancellationRequested(); MmcLaunchPreferences.Revalidate(instance, preferences);
                var adopted = await backend.AdoptVariantAsync(new(paths.InstanceRoot, java.Environment, primary.Definition.InstanceId,
                    primary.Definition.Generation, primary.Sha256), token).ConfigureAwait(false);
                if (adopted.Status == MmcInstanceBackendStatus.Cancelled) throw new OperationCanceledException(token);
                if (adopted.Status != MmcInstanceBackendStatus.Succeeded || adopted.Definition is null)
                    throw new IOException("Java 环境未完成配置：" + adopted.Failure);
                primary = MmcSavedSourceStore.ReadPrimary(paths);
            }
            var result = await backend.PrepareLaunchAsync(new(paths.InstanceRoot, java.Environment, primary.Definition.InstanceId,
                primary.Definition.Generation), token).ConfigureAwait(false);
            if (result.Status == MmcInstanceBackendStatus.Cancelled) throw new OperationCanceledException(token);
            prepared = result.Snapshot ?? throw new IOException("MMC 环境准备未完成：" + result.Failure);
            instance.Load();
            support = preferences.RuntimeSettings.WrapperMode == MmcRuntimeWrapperMode.JavaWrapper
                ? (await MmcInstanceBackend<ModNet.NetFile>.PrepareLaunchSupportAsync(prepared,
                    new[] { new MmcInstanceLaunchSupportInput("JavaWrapper.jar", WrapperBytes(), WrapperSha256),
                        new MmcInstanceLaunchSupportInput("MmcExactBridge.jar", MmcRuntimeBridgeTransport.CreateJarBytes(), MmcRuntimeBridgeTransport.JarSha256()) }, token).ConfigureAwait(false)).ToArray()
                : [];
            WorldBackups.RecoverBeforeLaunch(prepared.Paths.GameRoot, token);
            try { options.PersonalOptions = PersonalGameSettings.Prepare(instance); }
            catch (Exception error) { Logger.Warn(error, "MMC 个人游戏设置同步未完成，保留现有配置"); }
            options.QuickPlay = QuickPlayWorlds.Prepare(instance, options.QuickPlayWorld, options.ExtraGameArgs, false);
            token.ThrowIfCancellationRequested();
        }

        public void Custom(LegacyLaunchTask<int, int> task) {
            foreach (var command in preferences.CustomCommands) {
                CancellationToken token = task.CreateCancellationToken(); token.ThrowIfCancellationRequested();
                // Commands remain explicit player preferences. Package commands unsupported by the
                // format profile were rejected at import; never infer or execute an unknown command.
                string text = ModMain.ArgumentReplace(command.Command);
                var start = new ProcessStartInfo("cmd.exe") { UseShellExecute = false, CreateNoWindow = true,
                    WorkingDirectory = prepared.Paths.GameRoot };
                start.ArgumentList.Add("/d"); start.ArgumentList.Add("/s"); start.ArgumentList.Add("/c"); start.ArgumentList.Add(text);
                using var process = Process.Start(start) ?? throw new IOException("无法启动实例自定义命令。");
                try {
                    if (command.WaitForExit) while (!process.HasExited) { token.ThrowIfCancellationRequested(); Thread.Sleep(20); }
                } finally { if (token.IsCancellationRequested && !process.HasExited) process.Kill(entireProcessTree: true); }
            }
        }

        public void Start(LegacyLaunchTask<int, Process> task) {
            CancellationToken token = task.CreateCancellationToken(); token.ThrowIfCancellationRequested();
            var login = ModLaunch.McLoginLoader.Output;
            if (login.Type is "Auth" or "Nide") throw new InvalidOperationException("此 MMC 启动方式尚未绑定认证代理，请使用已支持的登录方式。");
            var account = new MmcRuntimeAccountSnapshot(login.Name, login.Uuid, login.AccessToken,
                "token:" + login.AccessToken + ":" + login.Uuid, login.Type == "Microsoft" ? "msa" : "legacy", "{}", login.ClientToken);
            var settings = preferences.RuntimeSettings with {
                ExtraGameTokens = preferences.RuntimeSettings.ExtraGameTokens.Concat(options.ExtraGameArgs).ToImmutableArray()
            };
            var request = new MmcRuntimeLaunchRequest(prepared.Definition, prepared.BoundPlan, prepared.Paths,
                prepared.Targets.Select(value => new MmcRuntimeArtifactTarget(value.ArtifactKey, value.TargetRelativePath, value.ActualPath)).ToImmutableArray(),
                java.RuntimeJava, settings, account,
                new(prepared.Directories.ViewKey, prepared.Directories.NativeDirectory, prepared.Directories.AssetsRoot, prepared.Directories.GameAssetsRoot),
                Support("JavaWrapper.jar"), Support("MmcExactBridge.jar")){RuntimeAuthority=runtimeAuthority};
            try {
                // The named admission mutex is thread-affine. Block only this worker while the
                // pure runtime reader finishes, retaining the same thread through Process.Start.
                using var admission = MmcAdmissionGate.Acquire(prepared.Paths.InstanceRoot, prepared.Definition.InstanceId, TimeSpan.Zero, token);
                MmcInstanceBackend<ModNet.NetFile>.ValidatePreparedLaunch(admission, prepared);
                MmcLaunchPreferences.Revalidate(instance, preferences);
                runtime = MmcRuntimeLaunchBuilder.BuildAsync(request, token).GetAwaiter().GetResult();
                var classes = MmcClasspathInspector.ObserveAsync(runtime, token).GetAwaiter().GetResult();
                string wrapperHash = Support("JavaWrapper.jar")?.Sha256 ?? Convert.ToHexStringLower(SHA256.HashData("PCLLF:direct-main:v1"u8));
                var authority = MmcLaunchAuthorityEvaluator.Evaluate(prepared.Definition, prepared.BoundPlan, saved.Resolved.Resolution.Profile,
                    new(java.RuntimeJava.IdentitySha256, wrapperHash, runtime.NonSensitiveArgumentFingerprint) {
                        AdditionalJvmArguments = runtime.NonSensitiveAdditionalJvmArguments,
                        AdditionalRequiredClasses = runtime.RequiredClassNames
                    }, classes.CoreObservations);
                if (authority.Diagnostics.Any(value => value.Severity == MmcDiagnosticSeverity.Error))
                    throw new IOException("MMC 启动链校验失败：" + string.Join("；", authority.Diagnostics.Select(value => value.Message)));
                var retainedJava = ModJava.GetManagedJavaRequirement(instance, authority, login.Type == "Nide");
                if (!retainedJava.Range.Contains(java.Java.Version))
                    throw new IOException("选定的 Java 不满足尚未被完整启动链证明替代的约束：" + retainedJava.Range +
                        string.Concat(authority.Diagnostics.Where(value => value.Severity == MmcDiagnosticSeverity.Warning).Select(value => "；" + value.Message)));
                if (authority.HasAuthority) ModLaunch.McLaunchLog("MMC 自定义启动链已核对：" + string.Join("、", authority.Proof.Targets.Select(value => value.ComponentUid).Distinct()));
                token.ThrowIfCancellationRequested();
                session = MmcSessions.Launch(admission, new(prepared.Definition.InstanceId, prepared.Definition.Generation, runtime.RuntimeInputFingerprint,
                    MmcSessions.ObserveExecutable(java.Java.JavaExePath), MmcSessions.ObserveExecutable(Environment.ProcessPath!), runtime.CreateStartInfo(), prepared.Paths.GameRootLeaf) {
                        SessionId = sessionId, RuntimeBinding = runtime.RuntimeBinding,RuntimeAuthority=runtimeAuthority
                    }, () => {
                        token.ThrowIfCancellationRequested();
                        MmcInstanceBackend<ModNet.NetFile>.ValidatePreparedLaunch(admission, prepared);
                        MmcLaunchPreferences.Revalidate(instance, preferences);
                        MmcRuntimeLaunchBuilder.RevalidateFilesAsync(runtime, token).GetAwaiter().GetResult();
                        MmcClasspathInspector.RevalidateManifestInputs(runtime, classes, token);
                    }, (id, identity) => pins.Acquire(id, identity, prepared.Definition.InstanceId, prepared.Paths.InstanceRoot, token));
            } catch {
                // A durable Launching intent may already represent a live process. Abandon only
                // when the session service confirms no active or uncertain record exists.
                if (runtime is not null && session is null) {
                    try {
                        using var admission = MmcAdmissionGate.Acquire(prepared.Paths.InstanceRoot, prepared.Definition.InstanceId, TimeSpan.Zero);
                        MmcSessionRuntimeDirectories.AbandonUnlaunched(admission, sessionId, runtime.RuntimeBinding,runtimeAuthority);
                    } catch (Exception error) { Logger.Warn(error, "MMC 启动临时目录保留，等待会话恢复确认"); }
                }
                throw;
            }
            Active.TryAdd(sessionId, session);
            _ = MonitorExitAsync(session, prepared.Paths.InstanceRoot, prepared.Definition.InstanceId, monitoringFinished.Task);
            ModLaunch.McLaunchProcess = session.Process;
            try {
                ProcessCreated?.Invoke(sessionId, session.Process);
                var watchTask = new ModLoader.LoaderTask<Process, int>("MMC 窗口监控", _ => { }) { Input = session.Process };
                watcher = new ModWatcher.Watcher(watchTask, instance, "", game => {
                    TryOptional(() => WorldBackups.Track(instance, game), "记录 MMC 存档备份会话失败");
                    TryOptional(() => PersonalGameSettings.Track((PersonalOptionsSession)options.PersonalOptions, game), "记录 MMC 个人设置会话失败");
                    TryOptional(() => QuickPlayWorlds.Track((QuickPlaySession)options.QuickPlay, game), "记录 MMC Quick Play 会话失败");
                }, () => {
                    TryOptional(() => PersonalGameSettings.Finish((PersonalOptionsSession)options.PersonalOptions), "保存 MMC 个人游戏设置失败");
                    TryOptional(() => QuickPlayWorlds.Finish((QuickPlaySession)options.QuickPlay), "保存 MMC 最近存档记录失败");
                }, LogRedactor: runtime.Redact, WindowTypeOverride: preferences.MaximizeWindow ? 4 : 0,
                    OnMonitoringFinished: () => monitoringFinished.TrySetResult(), RequireVerifiedExit: true);
                ModLaunch.McLaunchWatcher = watcher;
                if ((task.IsCanceled || Volatile.Read(ref canceled) != 0) && !session.Process.HasExited) session.Process.Kill(entireProcessTree: true);
                task.Output = session.Process;
                ModLaunch.McLaunchLog("已启动 MMC 受管理游戏会话：" + sessionId.ToString("N"));
            } catch {
                if (!session.Process.HasExited) session.Process.Kill(entireProcessTree: true);
                monitoringFinished.TrySetResult();
                throw;
            }
        }
        private void CancelGame() {
            Interlocked.Exchange(ref canceled, 1);
            _ = Task.Run(() => {
                try { if (session is not null && !session.Process.HasExited) session.Process.Kill(entireProcessTree: true); }
                catch (Exception error) { Logger.Warn(error, "取消 MMC 游戏启动时保留会话监视器等待退出"); }
            });
        }

        public void Wait(LegacyLaunchTask<Process, int> task) {
            while (watcher.State == ModWatcher.Watcher.MinecraftState.Loading) {
                if (task.IsCanceled && !session.Process.HasExited) session.Process.Kill(entireProcessTree: true);
                Thread.Sleep(100);
            }
            if (watcher.State == ModWatcher.Watcher.MinecraftState.Canceled) throw new OperationCanceledException();
            if (watcher.State == ModWatcher.Watcher.MinecraftState.Crashed) throw new IOException("MMC 游戏进程未完成启动，请查看游戏日志。");
        }
        private MmcRuntimeSupportArtifact Support(string name) {
            var value = support.SingleOrDefault(value => value.FileName == name);
            return value is null ? null : new(value.ActualPath, value.Sha256, value.SizeBytes);
        }
    }
    private static void TryOptional(Action action, string message) { try { action(); } catch (Exception error) { Logger.Warn(error, message); } }

    private static byte[] WrapperBytes() {
        using var input = typeof(MmcRuntimeLaunchBuilder).Assembly.GetManifestResourceStream("Launch\\JavaWrapper.jar")
            ?? throw new IOException("MMC JavaWrapper 资源缺失。");
        if (input.Length > 4 * 1024 * 1024) throw new IOException("MMC JavaWrapper 资源超过预算。");
        using MemoryStream output = new(); input.CopyTo(output); byte[] bytes = output.ToArray();
        if (Convert.ToHexStringLower(SHA256.HashData(bytes)) != WrapperSha256) throw new IOException("MMC JavaWrapper 资源摘要不符。");
        return bytes;
    }
    private static void ReconcilePrevious(MmcInstancePaths paths, Guid instanceId, MmcJavaPinStore pins, CancellationToken token,MmcMutableRuntimeAuthority runtimeAuthority) {
        using var admission = MmcAdmissionGate.Acquire(paths.InstanceRoot, instanceId, TimeSpan.Zero, token);
        MmcFileTransactions.AssertNoPending(admission);
        string directory = paths.ResolveRelative(MmcPathRole.Instance, "PCL/MmcSessions");
        if (!Directory.Exists(directory)) return;
        var entries = Directory.EnumerateFileSystemEntries(directory).Take(1025).ToArray();
        if (entries.Length > 1024) throw new IOException("MMC 会话记录超过恢复预算。");
        foreach (string entry in entries) {
            if (!Guid.TryParseExact(Path.GetFileName(entry), "N", out Guid id)) throw new IOException("MMC 存在无法识别的会话记录，需要恢复确认。");
            var result = MmcSessions.Reconcile(admission, id, new MmcSessionProcessInspector(), pins.ReleaseVerified,runtimeAuthority);
            if (result.Observation != MmcSessionObservation.ProvenExited)
                throw new IOException("此 MMC 实例仍在运行或会话尚待确认：" + result.Diagnostic);
        }
    }
    private static async Task MonitorExitAsync(MmcRunningSession session, string root, Guid instanceId, Task monitoringFinished) {
        bool reported = false;
        while (true) {
            try {
                await session.Process.WaitForExitAsync().ConfigureAwait(false);
                using (var admission = MmcAdmissionGate.Acquire(root, instanceId, TimeSpan.FromSeconds(10))) session.CompleteVerifiedExit(admission);
                await monitoringFinished.ConfigureAwait(false);
                Active.TryRemove(session.Receipt.SessionId, out _);
                if (Active.IsEmpty && Interlocked.Exchange(ref closeAfterSessions, 0) != 0)
                    ModBase.RunInUi(() => ModMain.FrmMain?.EndProgram(false));
                // Watcher retains the shared Process/log readers for crash UI and stop callbacks.
                return;
            } catch (Exception error) {
                if (!reported) { Logger.Warn(error, "MMC 游戏会话退出尚未完成确认，保留 Java pin 和恢复记录并等待重试"); reported = true; }
                await Task.Delay(3000).ConfigureAwait(false);
            }
        }
    }
}
