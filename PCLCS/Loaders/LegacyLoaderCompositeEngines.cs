using System;
using System.Collections.Generic;
using System.Linq;
using MeloongCore;

namespace PCLCS.Loaders;

public sealed class LegacyLoaderWorkerEngine<TLoader, TState, TLoadingState>
    where TLoader : LegacyLoaderBase<TLoader, TState, TLoadingState> {
    private readonly TLoader owner;
    private readonly Func<IWorker> readWorker;
    private LegacyLoaderHost<TLoader, TState, TLoadingState> Host => owner.Host;
    public LegacyLoaderWorkerEngine(TLoader owner, Func<IWorker> readWorker) { this.owner = owner; this.readWorker = readWorker; }
    public void BindInitialProgress(IWorker Worker) => Worker.Progress.ProgressChanged += raw => owner.Progress = raw.actual + raw.skiped;
    public void Start(object? Input = null, bool IsForceRestart = false) {
        lock (owner.LockState) owner.State = Host.Loading;
        Logger.Trace(() => $"Worker {owner.Name} 已启动");
        owner.IsForceRestarting = IsForceRestart;
        readWorker().Start();
    }
    public void Succeeded() {
        Logger.Trace(() => $"Worker {owner.Name} 已完成");
        owner.NotifyPreviewFinish();
        lock (owner.LockState) owner.State = Host.Finished;
    }
    public void Cancel() {
        lock (owner.LockState) {
            if (!owner.IsState(owner.State, Host.Loading)) return;
            owner.State = Host.Canceled;
        }
        Logger.Trace(() => $"Worker {owner.Name} 已取消，当前已完成 {Math.Round(owner.Progress * 100)}%");
        readWorker().Cancel();
    }
    public void Failed(Exception Ex) {
        lock (owner.LockState) {
            if (owner.AtOrAfterFinished) return;
            owner.State = Host.Failed;
        }
        Logger.Warn(Ex, $"Worker {owner.Name} 出错，已完成 {Math.Round(owner.Progress * 100)}%");
        owner.Error = Ex;
        readWorker().Cancel();
    }
}

public sealed class LegacyLoaderComboEngine<TCombo, TIn, TLoader, TState, TLoadingState>
    where TLoader : LegacyLoaderBase<TLoader, TState, TLoadingState>
    where TCombo : TLoader, ILegacyLoaderComboData<TLoader, TIn> {
    private readonly TCombo owner;
    private LegacyLoaderHost<TLoader, TState, TLoadingState> Host => owner.Host;
    public LegacyLoaderComboEngine(TCombo owner) => this.owner = owner;
    public void Initialize(string Name, IEnumerable<TLoader?> Loaders) {
        owner.CoreLoaders.Clear();
        foreach (TLoader? loader in Loaders) {
            if (loader is null) continue;
            owner.CoreLoaders.Add(loader);
            loader.AddThreadHandler(SubTaskStateChanged);
            loader.HasOnStateChangedThread = true;
        }
        owner.InitParent(null);
        owner.Name = Name;
    }
    public void InitParent(TLoader? Parent) {
        owner.Parent = Parent;
        foreach (TLoader child in owner.CoreLoaders) child.InitParent(owner);
    }
    public double Progress {
        get {
            if (owner.IsState(owner.State, Host.Waiting)) return 0;
            if (!owner.IsState(owner.State, Host.Loading)) return 1;
            double total = 0, finished = 0;
            foreach (TLoader child in owner.CoreLoaders) {
                total += child.ProgressWeight; finished += child.ProgressWeight * child.Progress;
            }
            return total == 0 ? 0 : finished / total;
        }
        set => throw new Exception("多重加载器不支持设置进度");
    }
    public void GuardFileWorkload(Func<Action, Action> Wrap) {
        foreach (TLoader child in owner.CoreLoaders) child.GuardWorkload(Wrap);
    }
    public void Start(object? Input = null, bool IsForceRestart = false) {
        owner.IsForceRestarting = IsForceRestart;
        lock (owner.LockState) {
            if (owner.IsState(owner.State, Host.Loading)) return;
            owner.State = Host.Loading;
        }
        owner.CoreInput = Microsoft.VisualBasic.CompilerServices.Conversions.ToGenericParameter<TIn>(Input)!;
        if (IsForceRestart) foreach (TLoader child in owner.CoreLoaders) child.State = Host.Waiting;
        Host.RunInThread(Update);
    }
    public void Cancel() {
        lock (owner.LockState) {
            if (owner.IsState(owner.State, Host.Loading) || owner.IsState(owner.State, Host.Waiting)) owner.State = Host.Canceled;
            else return;
        }
        Host.RunInThread(() => { foreach (TLoader child in owner.CoreLoaders) child.Cancel(); });
    }
    public void Failed(Exception Ex) {
        owner.Error = Ex;
        lock (owner.LockState) {
            if (owner.AtOrAfterFinished) return;
            owner.State = Host.Failed;
        }
        foreach (TLoader child in owner.CoreLoaders) child.Cancel();
        Host.RefreshExtraDownload();
    }
    private void SubTaskStateChanged(TLoader Loader, TState NewState, TState OldState) {
        if (owner.IsState(NewState, Host.Loading) || owner.IsState(NewState, Host.Waiting)) return;
        if (owner.IsState(NewState, Host.Finished)) Update();
        else if (owner.IsState(NewState, Host.Canceled)) Cancel();
        else if (owner.IsState(NewState, Host.Failed)) Failed(new Exception(Loader.Name + "失败", Loader.Error));
    }
    private void Update() {
        if (owner.IsState(owner.State, Host.Finished) || owner.IsState(owner.State, Host.Failed) || owner.IsState(owner.State, Host.Canceled)) return;
        bool isFinished = true, blocked = false;
        object? input = owner.CoreInput;
        foreach (TLoader child in owner.CoreLoaders) {
            LegacyLoaderKind kind = Host.Kind(child);
            bool restart = false;
            if (owner.IsState(child.State, Host.Finished)) {
                if (kind == LegacyLoaderKind.Task) {
                    object? candidate = input is not null && child.GetType().GenericTypeArguments.First() == input.GetType() ? input : null;
                    if (child.ShouldStartForCombo(candidate, true)) {
                        Logger.Info($"由于输入条件变更，重启已完成的加载器 {child.Name}"); restart = true;
                    } else input = child.TaskOutput;
                }
                if (!restart) { if (child.Block && !isFinished) blocked = true; continue; }
            } else if (owner.IsState(child.State, Host.Loading)) {
                if (kind == LegacyLoaderKind.Task) {
                    object? candidate = input is not null && child.GetType().GenericTypeArguments.First() == input.GetType() ? input : null;
                    if (child.ShouldStartForCombo(candidate, true)) {
                        Logger.Warn($"由于输入条件变更，重启进行中的加载器 {child.Name}"); restart = true;
                    }
                }
                if (!restart) { isFinished = false; blocked = true; continue; }
            }
            isFinished = false;
            if (blocked) continue;
            if (input is not null) {
                if (kind is LegacyLoaderKind.Task or LegacyLoaderKind.Combo)
                    child.Start(child.GetType().GenericTypeArguments.First() == input.GetType() ? input : null, owner.IsForceRestarting);
                else if (kind == LegacyLoaderKind.Worker) child.Start(IsForceRestart: owner.IsForceRestarting);
                else if (kind == LegacyLoaderKind.Download) child.Start(Host.IsDownloadInput(input) ? input : null, owner.IsForceRestarting);
                else throw new Exception("未知的加载器类型（" + child.GetType().Name + "）");
            } else child.Start(IsForceRestart: owner.IsForceRestarting);
            if (child.Block) blocked = true;
        }
        if (isFinished) {
            owner.NotifyPreviewFinish(); owner.State = Host.Finished; Host.RefreshExtraDownload();
        }
    }
    public List<TLoader> GetLoaderList(bool RequireShow = true) {
        List<TLoader> list = new(); AppendLoaderList(owner, ref list, RequireShow); return list;
    }
    public void GetLoaderList(ref List<TLoader> List, bool RequireShow = true) => AppendLoaderList(owner, ref List, RequireShow);
    public void AppendLoaderList(object Loader, ref List<TLoader> List, bool RequireShow = true) =>
        LegacyLoaderLists.Append(Loader, ref List, RequireShow, Host.Kind);
}

public static class LegacyLoaderLists {
    // Keep the public legacy Object input's genuine VB late member access.
    public static void Append<TLoader>(object Loader, ref List<TLoader> List, bool RequireShow,
        Func<object, LegacyLoaderKind> Kind) {
        var children = (System.Collections.IEnumerable)Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateGet(
            Loader, null, "Loaders", Array.Empty<object>(), null, null, null)!;
        foreach (object child in children) {
            bool show = Microsoft.VisualBasic.CompilerServices.Conversions.ToBoolean(
                Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateGet(child, null, "Show", Array.Empty<object>(), null, null, null));
            if (show || !RequireShow) List.Add(Microsoft.VisualBasic.CompilerServices.Conversions.ToGenericParameter<TLoader>(child));
            // LF deliberately restores the default RequireShow for recursive combinations.
            if (Kind(child) == LegacyLoaderKind.Combo) Append(child, ref List, true, Kind);
        }
    }
}
