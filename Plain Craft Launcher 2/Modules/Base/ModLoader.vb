Imports PCLCS.Loaders

' LF 的公开类型、事件委托、字段和原 UI/调度接线保留；完整加载器算法由 C# 承载。
Public Module ModLoader
    Private ReadOnly NativeLoaderHost As New LegacyLoaderHost(Of LoaderBase, LoadState, MyLoading.MyLoadingState) With {
        .Waiting = LoadState.Waiting, .Loading = LoadState.Loading, .Finished = LoadState.Finished,
        .Failed = LoadState.Failed, .Canceled = LoadState.Canceled,
        .UiRun = MyLoading.MyLoadingState.Run, .UiStop = MyLoading.MyLoadingState.Stop, .UiError = MyLoading.MyLoadingState.Error,
        .NewUuid = Function() GetUuid(), .Clock = Function() GetTimeMs(),
        .DebugFinishDelay = Function() Settings.Get(Of Boolean)("SystemDebugDelay"),
        .RandomInteger = Function(Min, Max) RandomInteger(Min, Max),
        .RunInUi = Sub(Work) RunInUi(Work), .RunInUiWait = Sub(Work) RunInUiWait(Work), .RunInThread = Sub(Work) RunInThread(Work),
        .RefreshExtraDownload = Sub() FrmMain.BtnExtraDownload.ShowRefresh(),
        .Kind = AddressOf NativeLoaderKind, .IsDownloadInput = Function(Input) TypeOf Input Is List(Of NetFile)}

    Private Function NativeLoaderKind(Loader As Object) As LegacyLoaderKind
        If GeneralUtils.IsGenericInstanceOf(Loader, GetType(LoaderTask(Of ,))) Then Return LegacyLoaderKind.Task
        If GeneralUtils.IsGenericInstanceOf(Loader, GetType(LoaderCombo(Of ))) Then Return LegacyLoaderKind.Combo
        If TypeOf Loader Is LoaderWorker Then Return LegacyLoaderKind.Worker
        If TypeOf Loader Is LoaderDownload Then Return LegacyLoaderKind.Download
        Return LegacyLoaderKind.Other
    End Function

    Public MustInherit Class LoaderBase
        Inherits LegacyLoaderBase(Of LoaderBase, LoadState, MyLoading.MyLoadingState)
        Implements ILoadingTrigger

        Protected Sub New()
            MyBase.New(NativeLoaderHost)
        End Sub
        Public Shadows ReadOnly Property IsLoader As Boolean Implements ILoadingTrigger.IsLoader
            Get
                Return MyBase.IsLoader
            End Get
        End Property
        Public Shadows Property LoadingState As MyLoading.MyLoadingState Implements ILoadingTrigger.LoadingState
            Get
                Return MyBase.LoadingState
            End Get
            Set(value As MyLoading.MyLoadingState)
                MyBase.LoadingState = value
            End Set
        End Property
        Public Event OnStateChangedThread(Loader As LoaderBase, NewState As LoadState, OldState As LoadState)
        Public Event OnStateChangedUi(Loader As LoaderBase, NewState As LoadState, OldState As LoadState)
        Public Event PreviewFinish(Loader As LoaderBase)
        Public Event LoadingStateChanged(NewState As MyLoading.MyLoadingState, OldState As MyLoading.MyLoadingState) Implements ILoadingTrigger.LoadingStateChanged
        Public Event ProgressChanged(NewProgress As Double, OldProgress As Double) Implements ILoadingTrigger.ProgressChanged
        Protected Overrides Sub ConnectUiHandler(Handler As Action(Of LoaderBase, LoadState, LoadState))
            AddHandler OnStateChangedUi, Sub(Loader, NewState, OldState) Handler(Loader, NewState, OldState)
        End Sub
        Protected Overrides Sub ConnectThreadHandler(Handler As Action(Of LoaderBase, LoadState, LoadState))
            AddHandler OnStateChangedThread, Sub(Loader, NewState, OldState) Handler(Loader, NewState, OldState)
        End Sub
        Protected Overrides Sub RaiseStateUi(Loader As LoaderBase, NewState As LoadState, OldState As LoadState)
            RaiseEvent OnStateChangedUi(Loader, NewState, OldState)
        End Sub
        Protected Overrides Sub RaiseStateThread(Loader As LoaderBase, NewState As LoadState, OldState As LoadState)
            RaiseEvent OnStateChangedThread(Loader, NewState, OldState)
        End Sub
        Protected Overrides Sub RaiseLoadingState(NewState As MyLoading.MyLoadingState, OldState As MyLoading.MyLoadingState)
            RaiseEvent LoadingStateChanged(NewState, OldState)
        End Sub
        Protected Overrides Sub RaiseProgress(NewProgress As Double, OldProgress As Double)
            RaiseEvent ProgressChanged(NewProgress, OldProgress)
        End Sub
        Protected Overrides Sub RaisePreviewFinishCore(Loader As LoaderBase)
            RaiseEvent PreviewFinish(Loader)
        End Sub
        Protected Overrides Sub ApplyFileWorkloadGuard(Wrap As Func(Of Action, Action))
            GuardFileWorkload(Wrap)
        End Sub
        Friend Overridable Sub GuardFileWorkload(Wrap As Func(Of Action, Action))
        End Sub
        Public MustOverride Overrides Sub Start(Optional Input As Object = Nothing, Optional IsForceRestart As Boolean = False)
        Public MustOverride Overrides Sub Cancel()
        Public MustOverride Overrides Sub Failed(Ex As Exception)
    End Class

    Public Class LoaderTask(Of TIn, TOut)
        Inherits LoaderBase
        Implements ILegacyLoaderTaskData(Of LoaderTask(Of TIn, TOut), TIn, TOut)

        Protected Friend ThreadPriority As ThreadPriority
        Protected Friend LoadDelegate As Action(Of LoaderTask(Of TIn, TOut))
        Protected Friend InputDelegate As Func(Of Object)
        Public ReloadTimeout As Integer = -1
        Public LastFinishedTime As Long = 0
        Public LastRunningThread As Thread = Nothing
        Friend Property InterruptOnCancel As Boolean = True
        Public Input As TIn = Nothing
        Public Output As TOut = Nothing
        Private ReadOnly Native As LegacyLoaderTaskEngine(Of LoaderTask(Of TIn, TOut), TIn, TOut, LoaderBase, LoadState, MyLoading.MyLoadingState)

        Public Sub New()
            Native = New LegacyLoaderTaskEngine(Of LoaderTask(Of TIn, TOut), TIn, TOut, LoaderBase, LoadState, MyLoading.MyLoadingState)(Me)
        End Sub
        Public Sub New(Name As String, LoadDelegate As Action(Of LoaderTask(Of TIn, TOut)), Optional InputDelegate As Func(Of TIn) = Nothing, Optional Priority As ThreadPriority = ThreadPriority.Normal)
            Me.New()
            Me.Name = Name
            Me.LoadDelegate = LoadDelegate
            Me.InputDelegate = InputDelegate
            ThreadPriority = Priority
        End Sub
        Public ReadOnly Property IsCanceled As Boolean
            Get
                Return Native.IsCanceled
            End Get
        End Property
        Public Function IsCanceledWithThread(Thread As Thread) As Boolean
            Return Native.IsCanceledWithThread(Thread)
        End Function
        Public Function StartGetInput(Optional Input As TIn = Nothing, Optional InputDelegate As Func(Of TIn) = Nothing) As TIn
            Return Native.StartGetInput(Input, InputDelegate)
        End Function
        Public Function ShouldStart(ByRef Input As TIn, Optional IsForceRestart As Boolean = False, Optional IgnoreReloadTimeout As Boolean = False) As Boolean
            Return Native.ShouldStart(Input, IsForceRestart, IgnoreReloadTimeout)
        End Function
        Protected Overrides Function ProbeTaskStart(Input As Object, IgnoreReloadTimeout As Boolean) As Boolean
            Return Native.ShouldStartForCombo(Input, IgnoreReloadTimeout)
        End Function
        Protected Overrides Function ReadTaskOutput() As Object
            Return Output
        End Function
        Public Overrides Sub Start(Optional Input As Object = Nothing, Optional IsForceRestart As Boolean = False)
            Native.Start(Input, IsForceRestart)
        End Sub
        Public Overrides Sub Failed(ex As Exception)
            Native.Failed(ex)
        End Sub
        Public Overrides Sub Cancel()
            Native.Cancel()
        End Sub
        Friend Overrides Sub GuardFileWorkload(Wrap As Func(Of Action, Action))
            Native.GuardFileWorkload(Wrap)
        End Sub
        Private Property CoreThreadPriority As ThreadPriority Implements ILegacyLoaderTaskData(Of LoaderTask(Of TIn, TOut), TIn, TOut).CoreThreadPriority
            Get
                Return ThreadPriority
            End Get
            Set(value As ThreadPriority)
                ThreadPriority = value
            End Set
        End Property
        Private Property CoreLoadDelegate As Action(Of LoaderTask(Of TIn, TOut)) Implements ILegacyLoaderTaskData(Of LoaderTask(Of TIn, TOut), TIn, TOut).CoreLoadDelegate
            Get
                Return LoadDelegate
            End Get
            Set(value As Action(Of LoaderTask(Of TIn, TOut)))
                LoadDelegate = value
            End Set
        End Property
        Private Property CoreInputDelegate As Func(Of Object) Implements ILegacyLoaderTaskData(Of LoaderTask(Of TIn, TOut), TIn, TOut).CoreInputDelegate
            Get
                Return InputDelegate
            End Get
            Set(value As Func(Of Object))
                InputDelegate = value
            End Set
        End Property
        Private Property CoreReloadTimeout As Integer Implements ILegacyLoaderTaskData(Of LoaderTask(Of TIn, TOut), TIn, TOut).CoreReloadTimeout
            Get
                Return ReloadTimeout
            End Get
            Set(value As Integer)
                ReloadTimeout = value
            End Set
        End Property
        Private Property CoreLastFinishedTime As Long Implements ILegacyLoaderTaskData(Of LoaderTask(Of TIn, TOut), TIn, TOut).CoreLastFinishedTime
            Get
                Return LastFinishedTime
            End Get
            Set(value As Long)
                LastFinishedTime = value
            End Set
        End Property
        Private Property CoreLastRunningThread As Thread Implements ILegacyLoaderTaskData(Of LoaderTask(Of TIn, TOut), TIn, TOut).CoreLastRunningThread
            Get
                Return LastRunningThread
            End Get
            Set(value As Thread)
                LastRunningThread = value
            End Set
        End Property
        Private Property CoreInterruptOnCancel As Boolean Implements ILegacyLoaderTaskData(Of LoaderTask(Of TIn, TOut), TIn, TOut).CoreInterruptOnCancel
            Get
                Return InterruptOnCancel
            End Get
            Set(value As Boolean)
                InterruptOnCancel = value
            End Set
        End Property
        Private Property CoreInput As TIn Implements ILegacyLoaderTaskData(Of LoaderTask(Of TIn, TOut), TIn, TOut).CoreInput
            Get
                Return Input
            End Get
            Set(value As TIn)
                Input = value
            End Set
        End Property
        Private Property CoreOutput As TOut Implements ILegacyLoaderTaskData(Of LoaderTask(Of TIn, TOut), TIn, TOut).CoreOutput
            Get
                Return Output
            End Get
            Set(value As TOut)
                Output = value
            End Set
        End Property
    End Class

    Public Class LoaderWorker
        Inherits LoaderBase
        Public WithEvents Worker As IWorker
        Private ReadOnly Native As LegacyLoaderWorkerEngine(Of LoaderBase, LoadState, MyLoading.MyLoadingState)
        Public Sub New(Name As String, Worker As IWorker)
            Native = New LegacyLoaderWorkerEngine(Of LoaderBase, LoadState, MyLoading.MyLoadingState)(Me, Function() Me.Worker)
            Me.Name = Name
            Me.Worker = Worker
            Native.BindInitialProgress(Worker)
        End Sub
        Public Overrides Sub Start(Optional Input As Object = Nothing, Optional IsForceRestart As Boolean = False)
            Native.Start(Input, IsForceRestart)
        End Sub
        Private Sub Worker_Succeeded() Handles Worker.Succeeded
            Native.Succeeded()
        End Sub
        Public Overrides Sub Cancel() Handles Worker.Canceled
            Native.Cancel()
        End Sub
        Public Overrides Sub Failed(Ex As Exception) Handles Worker.Failed
            Native.Failed(Ex)
        End Sub
    End Class

    Public Class LoaderCombo(Of InputType)
        Inherits LoaderBase
        Implements ILegacyLoaderComboData(Of LoaderBase, InputType)
        Public Loaders As New List(Of LoaderBase)
        Public Input As InputType
        Private ReadOnly Native As LegacyLoaderComboEngine(Of LoaderCombo(Of InputType), InputType, LoaderBase, LoadState, MyLoading.MyLoadingState)
        Public Sub New(Name As String, Loaders As IEnumerable(Of LoaderBase))
            Native = New LegacyLoaderComboEngine(Of LoaderCombo(Of InputType), InputType, LoaderBase, LoadState, MyLoading.MyLoadingState)(Me)
            Native.Initialize(Name, Loaders)
        End Sub
        Public Overrides Property Progress As Double
            Get
                Return Native.Progress
            End Get
            Set(value As Double)
                Native.Progress = value
            End Set
        End Property
        Public Overrides Sub InitParent(Parent As LoaderBase)
            Native.InitParent(Parent)
        End Sub
        Public Overrides Sub Start(Optional Input As Object = Nothing, Optional IsForceRestart As Boolean = False)
            Native.Start(Input, IsForceRestart)
        End Sub
        Public Overrides Sub Cancel()
            Native.Cancel()
        End Sub
        Public Overrides Sub Failed(Ex As Exception)
            Native.Failed(Ex)
        End Sub
        Friend Overrides Sub GuardFileWorkload(Wrap As Func(Of Action, Action))
            Native.GuardFileWorkload(Wrap)
        End Sub
        Public Shared Sub GetLoaderList(Loader As Object, ByRef List As List(Of LoaderBase), Optional RequireShow As Boolean = True)
            LegacyLoaderLists.Append(Loader, List, RequireShow, NativeLoaderHost.Kind)
        End Sub
        Public Sub GetLoaderList(ByRef List As List(Of LoaderBase), Optional RequireShow As Boolean = True)
            Native.GetLoaderList(List, RequireShow)
        End Sub
        Public Function GetLoaderList(Optional RequireShow As Boolean = True) As List(Of LoaderBase)
            Return Native.GetLoaderList(RequireShow)
        End Function
        Private Property CoreLoaders As List(Of LoaderBase) Implements ILegacyLoaderComboData(Of LoaderBase, InputType).CoreLoaders
            Get
                Return Loaders
            End Get
            Set(value As List(Of LoaderBase))
                Loaders = value
            End Set
        End Property
        Private Property CoreInput As InputType Implements ILegacyLoaderComboData(Of LoaderBase, InputType).CoreInput
            Get
                Return Input
            End Get
            Set(value As InputType)
                Input = value
            End Set
        End Property
    End Class

    Public LoaderTaskbar As New ConcurrentList(Of LoaderBase)
    Public LoaderTaskbarProgress As Double = 0
    Private LoaderTaskbarProgressLast As Shell.TaskbarItemProgressState = Shell.TaskbarItemProgressState.None
    Private ReadOnly NativeTaskbar As New LegacyLoaderTaskbar(Of LoaderBase, LoadState, MyLoading.MyLoadingState)(NativeLoaderHost,
        New LegacyLoaderTaskbarHost(Of LoaderBase) With {
            .ReadTasks = Function() LoaderTaskbar,
            .ReadProgress = Function() LoaderTaskbarProgress, .WriteProgress = Sub(Value) LoaderTaskbarProgress = Value,
            .ReadLastState = Function() CInt(LoaderTaskbarProgressLast), .WriteLastState = Sub(Value) LoaderTaskbarProgressLast = CType(Value, Shell.TaskbarItemProgressState),
            .RemoveTaskFromSpeedPage = Sub(Loader) FrmSpeedLeft?.TaskRemove(Loader),
            .RefreshTaskOnSpeedPage = Sub(Loader) FrmSpeedLeft?.TaskRefresh(Loader),
            .SetExtraDownloadProgress = Sub(Value) FrmMain.BtnExtraDownload.Progress = Value,
            .SetTaskbarValue = Sub(Value) FrmMain.TaskbarItemInfo.ProgressValue = Value,
            .SetTaskbarState = Sub(Value) FrmMain.TaskbarItemInfo.ProgressState = CType(Value, Shell.TaskbarItemProgressState)})
    Public Sub LoaderTaskbarAdd(Of T)(Loader As LoaderCombo(Of T))
        NativeTaskbar.Add(Loader)
    End Sub
    Public Sub LoaderTaskbarProgressRefresh()
        NativeTaskbar.Refresh()
    End Sub
    Public Function LoaderTaskbarProgressGet() As Double
        Return NativeTaskbar.GetProgress()
    End Function
    Public Enum LoaderFolderRunType
        RunOnUpdated
        ForceRun
        UpdateOnly
    End Enum
    Private ReadOnly NativeFolders As New LegacyLoaderFolders(Of LoaderBase, LoadState, MyLoading.MyLoadingState)
    Public Function LoaderFolderRun(Loader As LoaderBase, FolderPath As String, Type As LoaderFolderRunType, Optional MaxDepth As Integer = 0, Optional ExtraPath As String = "", Optional WaitForExit As Boolean = False) As Boolean
        Return NativeFolders.Run(Loader, FolderPath, CType(Type, LegacyLoaderFolderRunType), MaxDepth, ExtraPath, WaitForExit)
    End Function
End Module
