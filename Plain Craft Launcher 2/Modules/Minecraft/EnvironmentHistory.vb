Imports System.IO
Imports System.Text.RegularExpressions

'内容寻址存储使多次还原点共用未变化的文件；还原点仅保存在本机，不随整合包导出。
Friend NotInheritable Class EnvironmentHistory
    Private Shared ReadOnly Gate As New Object
    Friend Shared ReadOnly Service As New PCLCS.EnvironmentHistoryEngine(Of McInstance)(Gate, CreateHost())
    Friend Shared ReadOnly GameEnvironmentFolders As String() = PCLCS.EnvironmentHistoryRules.GameEnvironmentFolders
    Friend Shared Property TestRoot As String
        Get
            Return Service.TestRoot
        End Get
        Set(Value As String)
            Service.TestRoot = Value
        End Set
    End Property
    Private Shared ReadOnly Property Restoring As Integer
        Get
            Return Service.RestoringCount
        End Get
    End Property
    Private Shared Function CreateHost() As PCLCS.EnvironmentHistoryHost(Of McInstance)
        Return New PCLCS.EnvironmentHistoryHost(Of McInstance) With {
            .DataRoot = Function() LocalGameData.Root, .Hash = AddressOf LocalGameData.Hash,
            .AtomicWrite = Sub(FileName, Bytes) LocalGameData.AtomicWrite(FileName, Bytes, Nothing, False),
            .Target = AddressOf ModpackUpdate.Target, .RelativePath = AddressOf ModpackUpdate.RelativePath,
            .AddSlashSuffix = Function(Value) PathUtils.AddSlashSuffix(Value),
            .RemoveExtendedPrefix = AddressOf PathUtils.RemoveExtendedPrefix,
            .SafeName = AddressOf InstanceResourceStore.SafeName,
            .Digest = Function(FileName) ModpackUpdate.Digest(FileName),
            .EnsureNoLinks = AddressOf InstanceCloner.EnsureNoLinks,
            .EnsureWithin = Sub(Root, FileName) InstanceResourceStore.EnsureWithin(Root, FileName),
            .IsParentOf = Function(Parent, Child) PathUtils.IsParentOf(Parent, Child),
            .SamePath = Function(Left, Right) PathUtils.IsSame(Left, Right),
            .ApplyTransaction = Sub(GameRoot, VersionFolder, Operations, Token, BeforeApply) ModpackUpdate.ApplyTransaction(GameRoot, VersionFolder, Operations, Token, BeforeApply),
            .VersionPath = Function(Instance) Instance.PathVersion, .GameFolder = Function(Instance) Instance.PathIndie,
            .VanillaName = Function(Instance) Instance.Version.VanillaName,
            .LibraryPaths = Function(Instance) McLibListGet(Instance, True).Select(Function(Library) Library.LocalPath),
            .ModLoaderVersions = Function(Instance) Instance.Version.ModLoaderVersions.Select(Function(Pair) Pair.Key.ToString() & " " & Pair.Value),
            .JsonObject = Function(Instance) Instance.JsonObject,
            .AutoCleanup = Function(Instance) Settings.Get(Of Boolean)("VersionEnvironmentHistoryAutoCleanup", Instance:=Instance),
            .Retention = Function(Instance) Settings.Get(Of Integer)("VersionEnvironmentHistoryRetention", Instance:=Instance),
            .Info = Sub(Message) Logger.Info(Message), .Warn = Sub(ErrorValue, Message) Logger.Warn(ErrorValue, Message),
            .OpenFolder = Sub(Folder) OpenExplorer(Folder), .Hint = Sub(Message) Hint(Message)}
    End Function
    Friend Shared Function CoverageVersion(Point As JObject) As Integer
        Return Service.CoverageVersion(Point)
    End Function
    Friend Shared Function FoldersForCoverage(Version As Integer) As IEnumerable(Of String)
        Return Service.FoldersForCoverage(Version)
    End Function
    Private Shared Function Store(VersionFolder As String) As String
        Return Service.Store(VersionFolder)
    End Function
    Friend Shared Function IsGameFile(Relative As String) As Boolean
        Return Service.IsGameFile(Relative)
    End Function
    Friend Shared Function IsGameFileCovered(Relative As String, Version As Integer) As Boolean
        Return Service.IsGameFileCovered(Relative, Version)
    End Function
    Friend Shared Function Files(VersionFolder As String, GameRoot As String) As Dictionary(Of String, String)
        Return Service.Files(VersionFolder, GameRoot)
    End Function
    Private Shared Function FilesWithCoverage(VersionFolder As String, GameRoot As String, Version As Integer) As Dictionary(Of String, String)
        Return Service.FilesWithCoverage(VersionFolder, GameRoot, Version)
    End Function
    Friend Shared Function Capture(VersionFolder As String, GameRoot As String, Label As String, Token As CancellationToken,
        Optional Dependencies As Dictionary(Of String, String) = Nothing, Optional Description As String = "") As String
        Return Service.Capture(VersionFolder, GameRoot, Label, Token, Dependencies, Description)
    End Function
    Friend Shared Function CaptureInstance(Instance As McInstance, Label As String, Token As CancellationToken) As String
        Return Service.CaptureInstance(Instance, Label, Token)
    End Function
    Friend Shared Function Points(VersionFolder As String) As List(Of JObject)
        Return Service.Points(VersionFolder)
    End Function
    Friend Shared Function IsManualPoint(Point As JObject) As Boolean
        Return Service.IsManualPoint(Point)
    End Function
    Friend Shared Sub ApplyRetention(Instance As McInstance, NewestId As String, Token As CancellationToken)
        Service.ApplyRetention(Instance, NewestId, Token)
    End Sub
    Friend Shared Function PruneAutomatic(VersionFolder As String, Count As Integer, NewestId As String, Token As CancellationToken) As Integer
        Return Service.PruneAutomatic(VersionFolder, Count, NewestId, Token)
    End Function
    Friend Shared Sub MarkHealthy(VersionFolder As String, Id As String, Healthy As Boolean)
        Service.MarkHealthy(VersionFolder, Id, Healthy)
    End Sub
    Friend Shared Function Restore(VersionFolder As String, GameRoot As String, McRoot As String, Id As String, Token As CancellationToken,
        Optional BeforeApply As Action(Of Integer) = Nothing, Optional CaptureBefore As Func(Of String) = Nothing) As String
        Return Service.Restore(VersionFolder, GameRoot, McRoot, Id, Token, BeforeApply, CaptureBefore)
    End Function
    Friend Shared Sub OpenFolder(VersionFolder As String)
        Service.OpenFolder(VersionFolder)
    End Sub
    Friend Shared Function DeletePoints(VersionFolder As String, Ids As IEnumerable(Of String)) As String
        Return Service.DeletePoints(VersionFolder, Ids)
    End Function
End Class

Friend NotInheritable Class EnvironmentHistoryUi
    Friend Shared Event Changed()
    Friend Shared ReadOnly Service As New PCLCS.EnvironmentHistoryWorkflow(Of McInstance)(EnvironmentHistory.Service, CreateHost())
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
    Private Shared Function CreateHost() As PCLCS.EnvironmentHistoryWorkflowHost(Of McInstance)
        Return New PCLCS.EnvironmentHistoryWorkflowHost(Of McInstance) With {
            .HasRunningGame = Function() HasRunningMinecraft, .HasRunningSessions = AddressOf QuickPlayWorlds.HasRunningSessions,
            .LaunchLoading = Function() McLaunchLoader.State = LoadState.Loading,
            .Updating = Function() ModpackUpdateFlow.IsUpdating, .AnyLocked = Function() McInstanceLock.IsAnyLocked,
            .UpdatingModFolders = Function() PageInstanceMod.UpdatingInstanceModFolders.Any(),
            .VersionPath = Function(Instance) Instance.PathVersion, .GameFolder = Function(Instance) Instance.PathIndie,
            .Name = Function(Instance) Instance.Name,
            .HasPending = Function(VersionFolder) ModpackUpdate.Pending(VersionFolder).Any(),
            .RecoverInstance = AddressOf ModpackUpdateFlow.RecoverInstance,
            .SamePath = Function(Left, Right) PathUtils.IsSame(Left, Right),
            .Confirm = Function(Text, Title, First, Second, Warning) MyMsgBox(Text, Title, First, Second, IsWarn:=Warning),
            .SelectPoint = AddressOf SelectPoint, .HintError = Sub(Message) Hint(Message, HintType.Red),
            .Dispatch = Sub(Work) RunInUi(Work), .DispatchWait = Sub(Work) RunInUiWait(Work),
            .NotifyChanged = AddressOf PublishChanged,
            .ResetSettingsCache = Sub(Instance) Instance.ResetSettingsCache(),
            .CreateLoadedInstance = Function(VersionFolder) New McInstance(VersionFolder).Load(),
            .ClearIniCache = Sub(FileName) IniClearCache(FileName),
            .PageInstance = Function() PageInstanceLeft.Instance, .SelectedInstance = Function() McInstanceSelected,
            .SetPageInstance = Sub(Value) PageInstanceLeft.Instance = Value, .SetSelectedInstance = Sub(Value) McInstanceSelected = Value,
            .RefreshPageName = Sub() FrmMain.PageNameRefresh(), .StartWork = AddressOf StartWork}
    End Function
    Private Shared Function SelectPoint(Choices As IEnumerable(Of PCLCS.EnvironmentHistoryChoice)) As Integer?
        Dim Choice As Integer? = Nothing
        RunInUiWait(Sub() Choice = MyMsgBoxSelect(Choices.Select(Function(Item) CType(New MyRadioBox With {
            .Text = Item.Text, .ToolTip = Item.ToolTip}, IMyRadio)), "选择环境还原点", "下一步", "取消"))
        Return Choice
    End Function
    Private Shared Sub PublishChanged()
        RaiseEvent Changed()
    End Sub
    Friend Shared Sub Start(Instance As McInstance, Restore As Boolean, Optional PointId As String = Nothing, Optional DeleteIds As IEnumerable(Of String) = Nothing)
        Service.Start(Instance, Restore, PointId, DeleteIds)
    End Sub
    Private Shared Function StartWork(Instance As McInstance, Work As PCLCS.EnvironmentHistoryWork) As Boolean
        Dim StepLoader As New LoaderTask(Of String, String)(Work.StepName,
            Sub(Task)
                Work.Execute(Task.CreateCancellationToken(), Sub(Output) Task.Output = Output)
            End Sub)
        Dim Loader As New LoaderCombo(Of String)(Work.Title, {StepLoader}) With {.OnStateChanged = AddressOf LoaderStateChangedHintOnly}
        Dim Worker As New RedoableWorker(LoaderFileOperation.CreateWorkload(Loader, Instance.PathVersion))
        If Not McInstanceLock.TryAcquire(Worker, Instance.PathVersion) Then Return False
        AddHandler Worker.Stopped, Sub() Work.Stopped()
        Worker.Start()
        LoaderTaskbarAdd(Loader)
        FrmMain.BtnExtraDownload.ShowRefresh()
        Return True
    End Function
End Class
