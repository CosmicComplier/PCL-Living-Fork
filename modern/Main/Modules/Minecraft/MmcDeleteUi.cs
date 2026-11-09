using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using PCLCS;

namespace PCL;

// Original LF host integration; repository LICENCE applies.
internal static class MmcDeleteUi {
    private static int busy;
    internal static bool HasActiveOperation => Volatile.Read(ref busy) != 0;
    internal static Action<ModLoader.LoaderCombo<string>> RegisterTask { get; set; }
    internal static Action<MmcInstanceLifecycleResult> Published { get; set; }
    internal static Func<string, string, bool, int> Confirm { get; set; }
    internal static Func<ImmutableArray<MmcLifecycleCatalogEntry>, int?> SelectEntry { get; set; }
    internal static Func<FrameworkElement, int> ShowCatalog { get; set; }
    internal static Action<Exception, string> ErrorObserved { get; set; }
    internal static Action<MmcLifecycleCheckpoint, string> CheckpointObserved { get; set; }
    internal static MmcOperationCancellationFence AcquireOperationFence() {
        if (Interlocked.CompareExchange(ref busy, 1, 0) != 0) throw new IOException("另一项 MMC 删除、清理或恢复任务尚未完成。");
        return new MmcOperationCancellationFence(() => Interlocked.Exchange(ref busy, 0));
    }

    internal static void PromptDelete(ModMinecraft.McInstance instance, bool permanentRequested, Action completed = null) {
        ArgumentNullException.ThrowIfNull(instance);
        // Disk observation and record pins never run in a UI callback or while a
        // confirmation dialog holds an instance/admission/configuration gate.
        ModBase.RunInNewThread(() => {
            try {
                instance.Config.AssertAttached();
                var source = MmcInstanceInspector.Read(instance.PathVersion);
                if (!source.IsConsistent || source.Paths is null || source.Definition is null || source.PrimarySha256 is null ||
                    instance.Config.ContextStamp.Kind != MmcConfigurationContextKind.Managed || instance.Config.ContextStamp.InstanceId != source.Definition.InstanceId)
                    throw new IOException(source.Error ?? "实例状态已变化或存在未完成操作，请先打开 MMC 隔离区与恢复列表。");
                string name = Path.GetFileName(source.Paths.InstanceRoot);
                string text = (permanentRequested ? "MMC 实例会先移入可恢复隔离区。永久清理请从“MMC 隔离区与恢复”明确选择。\n\n" : "") +
                    "将版本“" + name + "”移到 MMC 隔离区？\n存档、模组和配置会随实例保留，可从版本列表的“MMC 隔离区与恢复”恢复。\n" +
                    "此操作不会释放隔离内容占用的磁盘空间。";
                ModBase.RunInUi(() => {
                    try { if (Ask(text, "删除 MMC 实例", true) == 1) StartDeleteSnapshot(source, completed); }
                    catch (Exception error) { Report(error, "开始删除 MMC 实例失败"); }
                });
            } catch (Exception error) { Report(error, "无法删除 MMC 实例"); }
        }, "Preview MMC deletion");
    }

    internal static void PromptCatalog(string minecraftRoot) {
        string selectedRoot = Path.GetFullPath(minecraftRoot);
        ModBase.RunInNewThread(() => {
            try {
                var request = new MmcLifecycleCatalogPageRequest(selectedRoot);
                for (;;) {
                    // Each return to the list re-reads its page. No record pin or admission
                    // lease survives into the selection/confirmation dialogs.
                    var page = MmcInstanceLifecycleCatalog.ReadPage(request);
                    MmcLifecycleCatalogView view = null;
                    MmcLifecycleCatalogEntry preview;
                    try {
                        preview = ModBase.RunInUiWait(() => {
                            AssertCatalogRoot(selectedRoot);
                            if (SelectEntry is not null) {
                                int? index = SelectEntry(page.Entries);
                                return index is >= 0 && index < page.Entries.Length ? page.Entries[index.Value] : null;
                            }
                            view = new MmcLifecycleCatalogView(request, page);
                            int choice = ShowCatalog is not null ? ShowCatalog(view) : ModMain.MyMsgBoxConfirmContent(
                                "选择一条记录后查看具体操作；取消操作可返回当前页。", "MMC 隔离区与恢复", "查看所选记录", () => view, IsWarn: false);
                            request = view.CurrentRequest;
                            AssertCatalogRoot(selectedRoot);
                            return choice == 1 && !view.IsReading ? view.Selected : null;
                        });
                    } finally { view?.CloseAndDrainAsync().GetAwaiter().GetResult(); }
                    if (preview is null) return;
                    bool started = ModBase.RunInUiWait(() => {
                        AssertCatalogRoot(selectedRoot);
                        return ReviewCatalogEntry(preview);
                    });
                    if (started) return;
                }
            } catch (Exception error) { Report(error, "读取 MMC 隔离区与恢复列表失败"); }
        }, "Read MMC recovery catalog");
    }

    private static void AssertCatalogRoot(string root) {
        if (!MeloongCore.PathUtils.IsSame(root, ModMinecraft.McFolderSelected)) throw new IOException("游戏文件夹已切换，请重新打开 MMC 隔离区与恢复列表。");
    }
    private static bool ReviewCatalogEntry(MmcLifecycleCatalogEntry preview) {
        if (!preview.CanRestore && !preview.CanRecover) { ModMain.MyMsgBox(preview.Description, preview.Name, "返回列表"); return false; }
        if (preview.CanPurge) {
            string question = "“" + preview.Name + "”仍完整保留在隔离区。\n可恢复实例，也可另行核对后永久清理。";
            int choice = Confirm is not null ? Confirm(question, "MMC 隔离实例", false) :
                ModMain.MyMsgBox(question, "MMC 隔离实例", "恢复实例", "返回列表", "永久清理");
            AssertCatalogRoot(preview.MinecraftRoot);
            if (choice == 3) { MmcPurgeUi.Prompt(preview); return true; }
            if (choice != 1) return false;
            StartCatalogAction(preview, true); return true;
        }
        bool restore = preview.CanRestore && !preview.CanRecover;
        bool purging = preview.State == MmcLifecycleCatalogState.Purging;
        string action = purging ? "继续已确认的永久清理" : restore ? "恢复到原来的版本名称" : "恢复未完成的操作";
        if (Ask("为“" + preview.Name + "”" + action + "？\n" + preview.Description + "\n已有同名内容不会被覆盖。", "MMC 实例操作确认", purging) != 1) return false;
        AssertCatalogRoot(preview.MinecraftRoot);
        StartCatalogAction(preview, restore); return true;
    }

    internal static ModLoader.LoaderCombo<string> StartCatalogAction(MmcLifecycleCatalogEntry preview, bool restore) =>
        Start(restore ? "恢复 MMC 实例：" + preview.Name : "恢复 MMC 未完成操作：" + preview.Name, preview.OperationDirectory,
            (token, checkpoint) => MmcInstanceLifecycleCatalog.Execute(preview, restore, token, checkpoint), null, recovery: true);

    private static ModLoader.LoaderCombo<string> StartDeleteSnapshot(MmcInstanceInspection source, Action completed) {
        var request = new MmcInstanceDeleteRequest(source.Paths.InstanceRoot, source.Definition.InstanceId, source.Definition.Generation, source.PrimarySha256);
        return Start("删除 MMC 实例：" + Path.GetFileName(source.Paths.InstanceRoot), source.Paths.InstanceRoot,
            (token, checkpoint) => new MmcInstanceLifecycleBackend().DeleteAsync(request with { Checkpoint = checkpoint }, token).GetAwaiter().GetResult(), completed, recovery: false);
    }

    private static ModLoader.LoaderCombo<string> Start(string name, string input,
        Func<CancellationToken, Action<MmcLifecycleCheckpoint, string>, MmcInstanceLifecycleResult> execute, Action completed, bool recovery) {
        var fence = AcquireOperationFence();
        var worker = new MmcOperationWorker<string, MmcInstanceLifecycleResult>(fence, recovery ? "恢复 MMC 实例" : "将 MMC 实例移入隔离区", task => {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(task.CreateCancellationToken(), fence.Token);
            var result = execute(linked.Token, (phase, directory) => {
                CheckpointObserved?.Invoke(phase, directory);
                if (phase == MmcLifecycleCheckpoint.BeforeDecision) fence.EnterDecision(linked.Token);
            });
            task.Output = result;
            bool recoveredAbort = recovery && result.Status == MmcInstanceBackendStatus.Cancelled && !linked.IsCancellationRequested && result.Failure is null;
            if (result.Status == MmcInstanceBackendStatus.Cancelled && !recoveredAbort) throw new OperationCanceledException(linked.Token);
            if (result.Status != MmcInstanceBackendStatus.Succeeded && !recoveredAbort)
                throw new IOException("MMC 操作尚未完成，内容已保留。请从 MMC 隔离区与恢复继续处理：" + result.Failure);
            if (Published is not null) Published(result);
            else ModBase.RunInUi(() => {
                if (!recovery && ModMinecraft.McInstanceSelected is { } selected && MeloongCore.PathUtils.IsSame(selected.PathVersion, input) &&
                    selected.Config.ContextStamp.Kind == MmcConfigurationContextKind.Managed && selected.Config.ContextStamp.InstanceId == result.Definition?.InstanceId)
                    ModMinecraft.McInstanceSelected = null;
                ModMinecraft.McInstanceListForceRefresh = true;
                ModMinecraft.McInstanceListLoader.Start(ModMinecraft.McFolderSelected, IsForceRestart: true);
                completed?.Invoke();
                ModMain.Hint(recoveredAbort ? "已撤回未提交的操作，原实例已保留。" : result.Quarantined ? "已移到 MMC 隔离区，文件仍保留，可从版本列表恢复。" : "MMC 实例已恢复。", ModMain.HintType.Green);
            });
        }) { InterruptOnCancel = false };
        var combo = new MmcOperationCombo<string>(fence, name, [worker]);
        try {
            if (RegisterTask is not null) RegisterTask(combo);
            else { ModLoader.LoaderTaskbarAdd(combo); ModBase.RunInUi(() => ModMain.FrmMain?.BtnExtraDownload.ShowRefresh()); }
            combo.Start(input); return combo;
        } catch { fence.AbandonUnstarted(); throw; }
    }

    internal static string Label(MmcLifecycleCatalogEntry entry) => entry.Name + " · " + (entry.State switch {
        MmcLifecycleCatalogState.Quarantined => "可恢复", MmcLifecycleCatalogState.Incomplete => "未完成", MmcLifecycleCatalogState.Completed => "已完成",
        MmcLifecycleCatalogState.Conflict => "存在冲突", MmcLifecycleCatalogState.Purging => "清理未完成", MmcLifecycleCatalogState.Purged => "已永久清理", _ => "无法验证"
    });
    private static int Ask(string text, string title, bool warning) => Confirm is not null ? Confirm(text, title, warning) :
        ModMain.MyMsgBox(text, title, "继续", "取消", IsWarn: warning);
    private static void Report(Exception error, string text) {
        if (ErrorObserved is not null) ErrorObserved(error, text);
        else MeloongCore.Logger.Error(error, text, MeloongCore.LogBehavior.Alert);
    }
}
