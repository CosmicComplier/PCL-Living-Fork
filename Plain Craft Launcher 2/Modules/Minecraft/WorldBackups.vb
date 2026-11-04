Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq
Imports System.Threading
Imports Newtonsoft.Json.Linq

'保留 LF 的共享调用入口和事件类型；备份格式、事务及工作流由 C# 实现维护。
Friend NotInheritable Class WorldBackups
    Friend Shared ReadOnly Service As New PCLCS.WorldBackupEngine(Of McInstance)(CreateHost())
    '与引擎使用同一把锁和同一份状态，供原消费者及测试入口使用。
    Private Shared ReadOnly Gate As Object = Service.Gate
    Private Shared ReadOnly Property Busy As Integer
        Get
            Return Service.BusyCount
        End Get
    End Property
    Friend Shared Property TestRoot As String
        Get
            Return Service.TestRoot
        End Get
        Set(Value As String)
            Service.TestRoot = Value
        End Set
    End Property
    Friend Shared Event Changed()
    Friend Shared ReadOnly Property IsBusy As Boolean
        Get
            Return Service.IsBusy
        End Get
    End Property

    Private Shared Function CreateHost() As PCLCS.WorldBackupHost(Of McInstance)
        Return New PCLCS.WorldBackupHost(Of McInstance) With {
            .RemoveExtendedPrefix = AddressOf PathUtils.RemoveExtendedPrefix,
            .ProfileAddress = Function(Name) LocalGameData.Address(Name),
            .GameFolder = Function(Instance) Instance.PathIndie,
            .VersionPath = Function(Instance) Instance.PathVersion,
            .Retention = Function(Instance) Settings.Get(Of Integer)("VersionWorldBackupRetention", Instance:=Instance),
            .AutoEnabled = Function(Instance) Settings.Get(Of Boolean)("VersionWorldBackupAutoEnabled", Instance:=Instance),
            .Watchers = AddressOf SnapshotWatchers,
            .WorldLabel = Function(Folder) WorldMetadata.ReadDetails(Folder)?.Name,
            .FormatSize = Function(Length) StringUtils.FormatByteSize(Length),
            .Warn = Sub(Ex, Message) Logger.Warn(Ex, Message),
            .UiBusy = Function() WorldBackupsUi.IsBusy,
            .Dispatch = Sub(Work) RunInUi(Work),
            .NotifyChanged = AddressOf PublishChanged,
            .OpenFolder = Sub(Folder) OpenExplorer(Folder),
            .Hint = AddressOf PublishHint
        }
    End Function

    Private Shared Function SnapshotWatchers() As IReadOnlyList(Of PCLCS.WorldBackupWatcher(Of McInstance))
        Dim Result As New List(Of PCLCS.WorldBackupWatcher(Of McInstance))
        For Each Item In McWatcherList.ToArray()
            If Item Is Nothing Then
                Result.Add(Nothing)
            Else
                Result.Add(New PCLCS.WorldBackupWatcher(Of McInstance) With {
                    .Process = Item.GameProcess,
                    .Instance = Item.Instance,
                    .State = CType(CInt(Item.State), PCLCS.WorldBackupWatcherState)
                })
            End If
        Next
        Return Result
    End Function

    Private Shared Sub PublishChanged()
        If ChangedEvent IsNot Nothing Then RunInUi(Sub() RaiseEvent Changed())
    End Sub
    Friend Shared Sub PublishHint(Text As String, IsError As Boolean)
        If IsError Then
            Hint(Text, HintType.Red)
        Else
            Hint(Text)
        End If
    End Sub

    Friend Shared Function Store(GameFolder As String) As String
        Return Service.Store(GameFolder)
    End Function
    Friend Shared Function HasActiveGame(GameFolder As String, Optional ExitedProcess As Process = Nothing) As Boolean
        Return Service.HasActiveGame(GameFolder, ExitedProcess)
    End Function
    Private Shared Function Metadata(GameFolder As String, Id As String) As JObject
        Return Service.Metadata(GameFolder, Id)
    End Function
    Friend Shared Function Points(GameFolder As String) As List(Of JObject)
        Return Service.Points(GameFolder)
    End Function
    Private Shared Sub ReadArchive(Folder As String, Point As JObject, Token As CancellationToken, Optional Destination As String = Nothing)
        Service.ReadArchive(Folder, Point, Token, Destination)
    End Sub
    Friend Shared Function IsManualPoint(Point As JObject) As Boolean
        Return Service.IsManualPoint(Point)
    End Function
    Friend Shared Function CaptureWorld(GameFolder As String, Identifier As String, Reason As String, Retention As Integer, Token As CancellationToken, Optional ExitedProcess As Process = Nothing) As String
        Return Service.CaptureWorld(GameFolder, Identifier, Reason, Retention, Token, ExitedProcess)
    End Function
    Friend Shared Function CaptureWorldWithProgress(GameFolder As String, Identifier As String, Reason As String, Retention As Integer, Token As CancellationToken, Progress As Action(Of Double), Optional ExitedProcess As Process = Nothing) As String
        Return Service.CaptureWorldWithProgress(GameFolder, Identifier, Reason, Retention, Token, Progress, ExitedProcess)
    End Function
    Friend Shared Function CaptureAll(Instance As McInstance, Reason As String, Token As CancellationToken, Optional ExitedProcess As Process = Nothing) As List(Of String)
        Return Service.CaptureAll(Instance, Reason, Token, ExitedProcess)
    End Function
    Friend Shared Function CaptureAllWithProgress(Instance As McInstance, Reason As String, Token As CancellationToken, Progress As Action(Of Double)) As List(Of String)
        Return Service.CaptureAllWithProgress(Instance, Reason, Token, Progress)
    End Function
    Friend Shared Function CaptureFolder(GameFolder As String, Reason As String, Retention As Integer, Token As CancellationToken, Optional ExitedProcess As Process = Nothing) As List(Of String)
        Return Service.CaptureFolder(GameFolder, Reason, Retention, Token, ExitedProcess)
    End Function
    Friend Shared Function CaptureFolderWithProgress(GameFolder As String, Reason As String, Retention As Integer, Token As CancellationToken, Progress As Action(Of Double), Optional ExitedProcess As Process = Nothing) As List(Of String)
        Return Service.CaptureFolderWithProgress(GameFolder, Reason, Retention, Token, Progress, ExitedProcess)
    End Function
    Friend Shared Function HasPendingRecovery(GameFolder As String) As Boolean
        Return Service.HasPendingRecovery(GameFolder)
    End Function
    Friend Shared Sub RecoverPending(GameFolder As String, Token As CancellationToken)
        Service.RecoverPending(GameFolder, Token)
    End Sub
    Friend Shared Sub RecoverBeforeLaunch(GameFolder As String, Token As CancellationToken)
        Service.RecoverBeforeLaunch(GameFolder, Token)
    End Sub
    Friend Shared Function Restore(GameFolder As String, Id As String, Retention As Integer, Token As CancellationToken, Optional BeforeCommit As Action(Of Integer) = Nothing) As String
        Return Service.Restore(GameFolder, Id, Retention, Token, BeforeCommit)
    End Function
    Friend Shared Function RestoreWithProgress(GameFolder As String, Id As String, Retention As Integer, Token As CancellationToken, Progress As Action(Of Double), Optional BeforeCommit As Action(Of Integer) = Nothing) As String
        Return Service.RestoreWithProgress(GameFolder, Id, Retention, Token, Progress, BeforeCommit)
    End Function
    Friend Shared Function DeletePoints(GameFolder As String, Ids As IEnumerable(Of String)) As String
        Return Service.DeletePoints(GameFolder, Ids)
    End Function
    Friend Shared Sub OpenFolder(GameFolder As String)
        Service.OpenFolder(GameFolder)
    End Sub
    Friend Shared Sub NotifyChanged()
        Service.NotifyChanged()
    End Sub
    Friend Shared Sub Track(Instance As McInstance, Game As Process)
        Service.Track(Instance, Game)
    End Sub
    Friend Shared Sub OnProcessStopped(Instance As McInstance, Game As Process, NormalExit As Boolean)
        Service.OnProcessStopped(Instance, Game, NormalExit)
    End Sub
    Friend Shared Sub OnNormalExit(Instance As McInstance, Game As Process)
        Service.OnNormalExit(Instance, Game)
    End Sub
    Friend Shared Function BeforeMinecraftChange(Instance As McInstance, OldMinecraft As String, NewMinecraft As String, Token As CancellationToken) As List(Of String)
        Return Service.BeforeMinecraftChange(Instance, OldMinecraft, NewMinecraft, Token)
    End Function
End Class

Friend NotInheritable Class WorldBackupsUi
    Friend Shared ReadOnly Service As New PCLCS.WorldBackupWorkflow(Of McInstance)(WorldBackups.Service, CreateHost())
    Private Shared ReadOnly Property Busy As Integer
        Get
            Return Service.BusyCount
        End Get
    End Property
    Friend Shared ReadOnly Property IsBusy As Boolean
        Get
            Return Service.IsBusy
        End Get
    End Property

    Private Shared Function CreateHost() As PCLCS.WorldBackupWorkflowHost(Of McInstance)
        Return New PCLCS.WorldBackupWorkflowHost(Of McInstance) With {
            .GameFolder = Function(Instance) Instance.PathIndie,
            .VersionPath = Function(Instance) Instance.PathVersion,
            .InstanceName = Function(Instance) Instance.Name,
            .Retention = Function(Instance) Settings.Get(Of Integer)("VersionWorldBackupRetention", Instance:=Instance),
            .LauncherBusy = Function() McLaunchLoader.State = LoadState.Loading OrElse ModpackUpdateFlow.IsUpdating OrElse EnvironmentHistoryUi.IsBusy OrElse McInstanceLock.IsAnyLocked OrElse PageInstanceMod.UpdatingInstanceModFolders.Any(),
            .SamePath = Function(Left, Right) PathUtils.IsSame(Left, Right),
            .Confirm = Function(Text, Title, Accept) MyMsgBox(Text, Title, Accept, "取消", IsWarn:=True),
            .Hint = AddressOf WorldBackups.PublishHint,
            .Dispatch = Sub(Work) RunInUi(Work),
            .StartWork = AddressOf StartWork
        }
    End Function

    Friend Shared Sub Start(Instance As McInstance, Optional RestoreId As String = Nothing, Optional DeleteIds As IEnumerable(Of String) = Nothing, Optional WorldIdentifier As String = Nothing)
        Service.Start(Instance, RestoreId, DeleteIds, WorldIdentifier)
    End Sub

    '原 Loader/Worker、版本锁、停止事件和任务栏生命周期仍由 LF 平台管理。
    Private Shared Function StartWork(Instance As McInstance, Work As PCLCS.WorldBackupWork) As Boolean
        Dim StepLoader As New LoaderTask(Of String, String)(Work.StepName,
            Sub(Task)
                Dim Token = Task.CreateCancellationToken()
                Work.Progress = PCLCS.FileTaskProgress.ForTask(Function() Task.Progress, Sub(Value) Task.Progress = Value)
                Work.Execute(Token, Sub(Output) Task.Output = Output)
            End Sub)
        Dim Loader As New LoaderCombo(Of String)(Work.Title, {StepLoader}) With {.OnStateChanged = AddressOf LoaderStateChangedHintOnly}
        Dim Worker As New RedoableWorker(LoaderFileOperation.CreateWorkload(Loader, Instance.PathVersion))
        Dim BrowseOnly = Work.SnapshotReservation()
        If Not If(BrowseOnly, McInstanceLock.TryAcquireSnapshot(Worker, Instance.PathVersion), McInstanceLock.TryAcquire(Worker, Instance.PathVersion)) Then Return False
        AddHandler Worker.Stopped, Sub() Work.Stopped()
        Worker.Start()
        LoaderTaskbarAdd(Loader)
        FrmMain.BtnExtraDownload.ShowRefresh()
        Return True
    End Function
End Class
