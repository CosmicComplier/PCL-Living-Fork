using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using PCLCS;

namespace PCL;

// Original LF host integration; repository LICENCE applies.
internal static class MmcPurgeUi {
    internal static Func<MmcInstancePurgePreview, int> Confirm { get; set; }
    internal static Action<ModLoader.LoaderCombo<string>> RegisterTask { get; set; }
    internal static Action<MmcInstancePurgeResult> Published { get; set; }
    internal static Action<MmcInstancePurgeCheckpoint, string> CheckpointObserved { get; set; }
    internal static Action DecisionReady { get; set; }
    internal static Action<Exception, string> ErrorObserved { get; set; }
    internal static void Prompt(MmcLifecycleCatalogEntry selected) {
        try { Start(selected); } catch (Exception error) { Report(error, "无法开始永久清理 MMC 隔离实例"); }
    }
    internal static ModLoader.LoaderCombo<string> Start(MmcLifecycleCatalogEntry selected) {
        ArgumentNullException.ThrowIfNull(selected);
        var fence = MmcDeleteUi.AcquireOperationFence();
        var worker = new MmcOperationWorker<string, MmcInstancePurgeResult>(fence, "核对 MMC 隔离内容", task => {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(task.CreateCancellationToken(), fence.Token);
            var token = linked.Token;
            var preview = MmcInstanceLifecycleCatalog.PreviewPurgeAsync(selected, token).GetAwaiter().GetResult();
            token.ThrowIfCancellationRequested(); task.Name = "等待确认永久清理 MMC 隔离实例";
            int confirmed = ModBase.RunInUiWait(() => {
                token.ThrowIfCancellationRequested();
                return Confirm is not null ? Confirm(preview) : ModMain.MyMsgBox(
                    "永久清理“" + selected.Name + "”？\n\n隔离位置：" + preview.QuarantineRoot +
                    "\n文件：" + preview.Entries.Count(value => !value.Directory).ToString(CultureInfo.CurrentCulture) +
                    "，总计 " + (preview.TotalBytes / (1024d * 1024)).ToString("0.##", CultureInfo.CurrentCulture) + " MiB。\n\n" +
                    "包括存档、模组、配置及实例私有文件。永久清理后无法还原；共享资源和其他实例不在清理范围内。",
                    "永久清理 MMC 隔离实例", "永久清理", "取消", IsWarn: true);
            });
            token.ThrowIfCancellationRequested();
            if (confirmed != 1) { task.Cancel(); throw new OperationCanceledException(token); }
            task.Name = "永久清理 MMC 隔离实例";
            var result = new MmcInstanceLifecycleBackend().PurgeAsync(new(preview, preview.SnapshotFingerprint,
                MmcInstancePurgeIntent.PermanentlyDeleteQuarantinedInstance) {
                Checkpoint = (phase, path) => CheckpointObserved?.Invoke(phase, path),
                BeforeCommitDecision = () => { DecisionReady?.Invoke(); fence.EnterDecision(token); }
            }, token).GetAwaiter().GetResult();
            task.Output = result;
            if (result.Status == MmcInstanceBackendStatus.Cancelled) throw new OperationCanceledException(token);
            if (result.Status != MmcInstanceBackendStatus.Succeeded || !result.Completed)
                throw new IOException((result.DecisionRecorded ? "永久清理尚未完成，请从 MMC 隔离区与恢复继续清理。" : "无法永久清理，隔离内容已保留。") + "\n" + result.Failure);
            if (Published is not null) Published(result);
            else ModBase.RunInUi(() => {
                ModMinecraft.McInstanceListForceRefresh = true;
                ModMinecraft.McInstanceListLoader.Start(ModMinecraft.McFolderSelected, IsForceRestart: true);
                ModMain.Hint("已永久清理确认的 MMC 隔离实例。", ModMain.HintType.Green);
            });
        }) { InterruptOnCancel = false };
        var combo = new MmcOperationCombo<string>(fence, "永久清理 MMC 隔离实例：" + selected.Name, [worker]);
        try {
            if (RegisterTask is not null) RegisterTask(combo);
            else { ModLoader.LoaderTaskbarAdd(combo); ModBase.RunInUi(() => ModMain.FrmMain?.BtnExtraDownload.ShowRefresh()); }
            combo.Start(selected.OperationDirectory); return combo;
        } catch { fence.AbandonUnstarted(); throw; }
    }
    private static void Report(Exception error, string caption) {
        if (ErrorObserved is not null) ErrorObserved(error, caption); else MeloongCore.Logger.Error(error, caption, MeloongCore.LogBehavior.Alert);
    }
}
