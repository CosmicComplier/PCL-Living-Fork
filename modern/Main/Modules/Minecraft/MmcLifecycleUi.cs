using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using PCLCS;

namespace PCL;

// Original LF host integration; repository LICENCE applies.
internal static class MmcLifecycleUi {
    private static int busy;
    internal static bool HasActiveOperation => Volatile.Read(ref busy) != 0;
    internal static Action<ModLoader.LoaderCombo<string>> RegisterTask { get; set; }
    internal static Action<MmcInstanceLifecycleResult> Published { get; set; }

    internal static void PromptRename(ModMinecraft.McInstance instance, Action<ModMinecraft.McInstance> completed) {
        var source = ReadSource(instance);
        string name = ModMain.MyMsgBoxInput("重命名版本", "", instance.Name,
            new Collection<Validate> { new ValidateFolderName(Path.GetDirectoryName(source.Paths.InstanceRoot),
                IgnoreList: [instance.Name]) });
        if (string.IsNullOrWhiteSpace(name) || name == instance.Name) return;
        StartRename(instance, name, completed);
    }

    internal static ModLoader.LoaderCombo<string> StartRename(ModMinecraft.McInstance instance, string name,
        Action<ModMinecraft.McInstance> completed = null) {
        var source = ReadSource(instance);
        string parent = Path.GetDirectoryName(source.Paths.InstanceRoot)!;
        string error = new ValidateFolderName(parent).Validate(name);
        if (!string.IsNullOrEmpty(error)) throw new IOException("版本名称无效：" + error);
        var request = new MmcInstanceRelocationRequest(source.Paths.InstanceRoot, Path.Combine(parent, name),
            source.Definition.InstanceId, source.Definition.Generation, source.PrimarySha256, MmcInstanceRelocationKind.Rename);
        if (Interlocked.CompareExchange(ref busy, 1, 0) != 0) throw new IOException("另一项 MMC 实例管理任务尚未完成。");
        var fence = new MmcOperationCancellationFence();
        var worker = new MmcOperationWorker<string, MmcInstanceLifecycleResult>(fence, "保存新的版本名称", task => {
            try {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(task.CreateCancellationToken(), fence.Token);
                var result = new MmcInstanceLifecycleBackend().RelocateAsync(request with {
                    Checkpoint = (phase, _) => { if (phase == MmcLifecycleCheckpoint.BeforeDecision) fence.EnterDecision(linked.Token); }
                }, linked.Token).GetAwaiter().GetResult();
                task.Output = result;
                if (result.Status == MmcInstanceBackendStatus.Cancelled) throw new OperationCanceledException(linked.Token);
                if (result.Status != MmcInstanceBackendStatus.Succeeded || result.CurrentRoot is null)
                    throw new IOException("重命名未完成，现场已保留以便恢复：" + result.Failure);
                if (Published is not null) Published(result);
                else ModBase.RunInUi(() => {
                    var renamed = new ModMinecraft.McInstance(result.CurrentRoot).Load();
                    if (ModMinecraft.McInstanceSelected is { } selected && MeloongCore.PathUtils.IsSame(selected.PathVersion, instance.PathVersion))
                        ModMinecraft.McInstanceSelected = renamed;
                    ModMinecraft.McInstanceListForceRefresh = true;
                    ModMinecraft.McInstanceListLoader.Start(ModMinecraft.McFolderSelected, IsForceRestart: true);
                    completed?.Invoke(renamed);
                    ModMain.Hint("重命名成功！", ModMain.HintType.Green);
                });
            } finally { fence.Complete(); Interlocked.Exchange(ref busy, 0); }
        }) { InterruptOnCancel = false };
        var combo = new MmcOperationCombo<string>(fence, "重命名 MMC 实例：" + instance.Name, [worker]);
        try {
            if (RegisterTask is not null) RegisterTask(combo);
            else { ModLoader.LoaderTaskbarAdd(combo); ModBase.RunInUi(() => ModMain.FrmMain?.BtnExtraDownload.ShowRefresh()); }
            combo.Start(source.Paths.InstanceRoot); return combo;
        } catch { fence.Complete(); Interlocked.Exchange(ref busy, 0); throw; }
    }

    private static MmcInstanceInspection ReadSource(ModMinecraft.McInstance instance) {
        ArgumentNullException.ThrowIfNull(instance);
        var source = MmcInstanceInspector.Read(instance.PathVersion);
        if (!source.IsConsistent || source.Definition is null || source.Paths is null || source.PrimarySha256 is null)
            throw new IOException(source.Error ?? "MMC 实例需要先修复，再重命名。");
        return source;
    }
}
