using System;
using System.Collections.Generic;
using System.Threading;
using MeloongCore;

namespace PCLCS.Loaders;

// LF f4aa0fae62c3ee8af24c81e61844c8fdd13c8983 ModLoader.vb and
// LoaderFileOperation.vb are the behavior source under the LF repository LICENCE.
// PCL 5ef81e490ae46b1100d61e33d0183094c7a80ca8 supplies the original loader;
// LF's file guards/late closure/StartIfNeeded/InterruptOnCancel additions remain.
// CE 1a7653127d578efdb2a9388999a3fdc67d9e49d5 ModLoader.cs was reviewed
// as C# organization reference (CE Apache LICENSE/PCL LICENCE). Its Task/Abort
// scheduler and token state do not replace LF's real Thread/identity semantics.
public enum LegacyLoaderKind { Other, Task, Combo, Worker, Download }
public enum LegacyLoaderFolderRunType { RunOnUpdated, ForceRun, UpdateOnly }

public sealed class LegacyLoaderHost<TLoader, TState, TLoadingState> {
    public TState Waiting = default!, Loading = default!, Finished = default!, Failed = default!, Canceled = default!;
    public TLoadingState UiRun = default!, UiStop = default!, UiError = default!;
    public Func<int> NewUuid = null!;
    public Func<long> Clock = null!;
    public Func<bool> DebugFinishDelay = null!;
    public Func<int, int, int> RandomInteger = null!;
    public Action<Action> RunInUi = null!, RunInUiWait = null!, RunInThread = null!;
    public Action RefreshExtraDownload = null!;
    public Func<object, LegacyLoaderKind> Kind = null!;
    public Func<object, bool> IsDownloadInput = null!;
}

public interface ILegacyLoaderTaskData<TTask, TIn, TOut> {
    ThreadPriority CoreThreadPriority { get; set; }
    Action<TTask>? CoreLoadDelegate { get; set; }
    Func<object?>? CoreInputDelegate { get; set; }
    int CoreReloadTimeout { get; set; }
    long CoreLastFinishedTime { get; set; }
    Thread? CoreLastRunningThread { get; set; }
    bool CoreInterruptOnCancel { get; set; }
    TIn CoreInput { get; set; }
    TOut CoreOutput { get; set; }
}
public interface ILegacyLoaderComboData<TLoader, TIn> {
    List<TLoader> CoreLoaders { get; set; }
    TIn CoreInput { get; set; }
}
public sealed class LegacyLoaderTaskbarHost<TLoader> {
    public Func<ConcurrentList<TLoader>> ReadTasks = null!;
    public Func<double> ReadProgress = null!;
    public Action<double> WriteProgress = null!;
    public Func<int> ReadLastState = null!;
    public Action<int> WriteLastState = null!;
    public Action<TLoader> RemoveTaskFromSpeedPage = null!, RefreshTaskOnSpeedPage = null!;
    public Action<double> SetExtraDownloadProgress = null!, SetTaskbarValue = null!;
    public Action<int> SetTaskbarState = null!;
}
