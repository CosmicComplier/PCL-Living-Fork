Imports CloneEngine = PCLCS.InstanceCloneEngine(Of PCL.ModMinecraft.McInstance, PCL.InstanceCloneOptions, PCL.InstanceCloneResult, PCL.InstanceCloneMigrationChange, PCL.InstanceCloneMigrationPlan)
Imports CloneWorkflow = PCLCS.InstanceCloneWorkflow(Of PCL.ModMinecraft.McInstance, PCL.InstanceCloneOptions, PCL.InstanceCloneResult, PCL.InstanceCloneMigrationChange, PCL.InstanceCloneMigrationPlan)

Friend NotInheritable Class InstanceCloneOptions
    Inherits PCLCS.InstanceCloneOptionsData
End Class
Friend NotInheritable Class InstanceCloneResult
    Inherits PCLCS.InstanceCloneResultData
End Class
Friend NotInheritable Class InstanceCloneMigrationChange
    Inherits PCLCS.InstanceCloneMigrationChangeData
End Class
Friend NotInheritable Class InstanceCloneMigrationPlan
    Inherits PCLCS.InstanceCloneMigrationPlanData(Of InstanceCloneMigrationChange)
End Class

'复制采用独立文件和同卷临时目录；提交前重新校验来源，绝不重命名或修改来源文件。
Friend NotInheritable Class InstanceCloner
    Friend Const OriginName As String = "PCL/CloneOrigin.json"
    Private Shared ReadOnly Gate As New Object
    Friend Shared ReadOnly Service As New CloneEngine(Gate, CreateHost(), Function() New InstanceCloneOptions,
        Function() New InstanceCloneResult, Function() New InstanceCloneMigrationChange, Function() New InstanceCloneMigrationPlan)
    Private Shared Function CreateHost() As PCLCS.InstanceCloneHost(Of McInstance)
        Return New PCLCS.InstanceCloneHost(Of McInstance) With {
            .RemoveExtendedPrefix = AddressOf PathUtils.RemoveExtendedPrefix,
            .RelativePath = AddressOf ModpackUpdate.RelativePath, .SafeName = AddressOf InstanceResourceStore.SafeName,
            .AddSlashSuffix = Function(Value) PathUtils.AddSlashSuffix(Value),
            .EnsureWithin = Sub(Root, Address) InstanceResourceStore.EnsureWithin(Root, Address),
            .SamePath = Function(Left, Right) PathUtils.IsSame(Left, Right),
            .HasRunningGame = Function() HasRunningMinecraft, .HasRunningSessions = AddressOf QuickPlayWorlds.HasRunningSessions,
            .LaunchLoading = Function() McLaunchLoader.State = LoadState.Loading, .WorldBackupBusy = Function() WorldBackups.IsBusy,
            .HasActiveGame = AddressOf WorldBackups.HasActiveGame, .HasPending = Function(VersionFolder) ModpackUpdate.Pending(VersionFolder).Any(),
            .VersionPath = Function(Instance) Instance.PathVersion, .VanillaName = Function(Instance) Instance.Version.VanillaName,
            .IsFool = Function(Instance) Instance.Version.IsFool, .IsSnapshot = Function(Instance) Instance.Version.IsSnapshot,
            .HasModLoaders = Function(Instance) Instance.Version.ModLoaders.Flags.Any, .ApiType = Function(Instance) Instance.DisplayType = McInstanceCardType.API,
            .IsolationMode = Function() Settings.Get(Of Integer)("LaunchArgumentIndieV2"),
            .CaptureEnvironment = Function(VersionFolder, GameRoot, Label, Token) EnvironmentHistory.Capture(VersionFolder, GameRoot, Label, Token),
            .ApplyTransaction = Sub(GameRoot, VersionFolder, Operations, Token, BeforeApply) ModpackUpdate.ApplyTransaction(GameRoot, VersionFolder, Operations, Token, BeforeApply)}
    End Function
    Friend Shared Function NormalizeRoot(Value As String) As String
        Return Service.NormalizeRoot(Value)
    End Function
    Friend Shared Sub EnsureNoLinks(Value As String)
        Service.EnsureNoLinks(Value)
    End Sub
    Friend Shared Function GameRootForInstance(Instance As McInstance) As String
        Return Service.GameRootForInstance(Instance)
    End Function
    Friend Shared Function Digest(Address As String, Token As CancellationToken) As String
        Return Service.Digest(Address, Token)
    End Function
    Friend Shared Function CloneInstance(Instance As McInstance, NewName As String, Options As InstanceCloneOptions, Token As CancellationToken) As InstanceCloneResult
        Return Service.CloneInstance(Instance, NewName, Options, Token)
    End Function
    Friend Shared Function Clone(VersionFolder As String, GameRoot As String, NewName As String, Options As InstanceCloneOptions, Token As CancellationToken,
        Optional AfterFileCopied As Action(Of Integer) = Nothing) As InstanceCloneResult
        Return Service.Clone(VersionFolder, GameRoot, NewName, Options, Token, AfterFileCopied)
    End Function
    Friend Shared Function CloneInstanceWithProgress(Instance As McInstance, NewName As String, Options As InstanceCloneOptions, Token As CancellationToken, Progress As Action(Of Double)) As InstanceCloneResult
        Return Service.CloneInstanceWithProgress(Instance, NewName, Options, Token, Progress)
    End Function
    Friend Shared Function CloneWithProgress(VersionFolder As String, GameRoot As String, NewName As String, Options As InstanceCloneOptions, Token As CancellationToken, Progress As Action(Of Double),
        Optional AfterFileCopied As Action(Of Integer) = Nothing) As InstanceCloneResult
        Return Service.CloneWithProgress(VersionFolder, GameRoot, NewName, Options, Token, Progress, AfterFileCopied)
    End Function
    Friend Shared Function SourceVersionFolder(CloneFolder As String) As String
        Return Service.SourceVersionFolder(CloneFolder)
    End Function
    Friend Shared Function PreviewMigration(CloneFolder As String, SourceGameRoot As String, Token As CancellationToken) As InstanceCloneMigrationPlan
        Return Service.PreviewMigration(CloneFolder, SourceGameRoot, Token)
    End Function
    Friend Shared Function ApplyMigration(Plan As InstanceCloneMigrationPlan, Token As CancellationToken, Optional CaptureBefore As Func(Of String) = Nothing,
        Optional BeforeMinecraftChange As Action(Of String, String) = Nothing, Optional BeforeApply As Action(Of Integer) = Nothing) As String
        Return Service.ApplyMigration(Plan, Token, CaptureBefore, BeforeMinecraftChange, BeforeApply)
    End Function
End Class

Friend NotInheritable Class InstanceClonerUi
    Friend Shared Event Changed()
    Friend Shared ReadOnly Service As New CloneWorkflow(InstanceCloner.Service, CreateHost())
    Friend Shared Property BeforeMinecraftChangeHandler As Action(Of McInstance, String, String, CancellationToken)
        Get
            Return Service.BeforeMinecraftChangeHandler
        End Get
        Set(Value As Action(Of McInstance, String, String, CancellationToken))
            Service.BeforeMinecraftChangeHandler = Value
        End Set
    End Property
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
    Private Shared Function CreateHost() As PCLCS.InstanceCloneWorkflowHost(Of McInstance)
        Return New PCLCS.InstanceCloneWorkflowHost(Of McInstance) With {
            .VersionPath = Function(Instance) Instance.PathVersion, .Name = Function(Instance) Instance.Name,
            .CreateInstance = Function(VersionFolder) New McInstance(VersionFolder),
            .HasRunningGame = Function() HasRunningMinecraft, .HasRunningSessions = AddressOf QuickPlayWorlds.HasRunningSessions,
            .LaunchLoading = Function() McLaunchLoader.State = LoadState.Loading, .WorldBackupBusy = Function() WorldBackups.IsBusy,
            .HasActiveGame = AddressOf WorldBackups.HasActiveGame,
            .EnvironmentBusy = Function() EnvironmentHistoryUi.IsBusy, .Updating = Function() ModpackUpdateFlow.IsUpdating,
            .AnyLocked = Function() McInstanceLock.IsAnyLocked, .UpdatingModFolders = Function() PageInstanceMod.UpdatingInstanceModFolders.Any(),
            .Hint = Sub(Message, Success) Hint(Message, If(Success, HintType.Green, HintType.Red)),
            .Confirm = Function(Text, Title, First, Second, Warning) MyMsgBox(Text, Title, First, Second, IsWarn:=Warning),
            .Dispatch = Sub(Work) RunInUi(Work), .ResetSettingsCache = Sub(Instance) Instance.ResetSettingsCache(),
            .CaptureInstance = AddressOf EnvironmentHistory.CaptureInstance,
            .RefreshInstances = Sub() LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\"),
            .NotifyChanged = AddressOf PublishChanged, .StartWork = AddressOf Run}
    End Function
    Private Shared Sub PublishChanged()
        RaiseEvent Changed()
    End Sub
    Private Shared Function CanStart(Instance As McInstance) As Boolean
        Return Service.CanStart(Instance)
    End Function
    Friend Shared Sub Start(Instance As McInstance, NewName As String, Options As InstanceCloneOptions)
        Service.Start(Instance, NewName, Options)
    End Sub
    Friend Shared Sub Migrate(CloneInstance As McInstance)
        Service.Migrate(CloneInstance)
    End Sub
    '原 Loader、双实例锁、废弃 Worker 及停止／任务栏生命周期。
    Private Shared Sub Run(Instance As McInstance, Work As PCLCS.InstanceCloneWork)
        Dim StepLoader As New LoaderTask(Of String, String)(Work.StepName,
            Sub(Task)
                Work.Progress = PCLCS.FileTaskProgress.ForTask(Function() Task.Progress, Sub(Value) Task.Progress = Value)
                Work.Execute(Task.CreateCancellationToken(), Sub(Output) Task.Output = Output)
            End Sub)
        Dim Loader As New LoaderCombo(Of String)(Work.Title, {StepLoader}) With {.OnStateChanged = AddressOf LoaderStateChangedHintOnly}
        Dim Abandoned As Boolean
        Dim Workload = LoaderFileOperation.CreateWorkload(Loader, Instance.PathVersion)
        Dim Worker As New RedoableWorker(Sub()
                                            If Not Abandoned Then Workload()
                                        End Sub)
        If Not If(Work.LockSource, McInstanceLock.TryAcquire(Worker, Instance.PathVersion), McInstanceLock.TryAcquireSnapshot(Worker, Instance.PathVersion)) Then
            Work.ReleaseBusy()
            Return
        End If
        If Work.LockSource Then
            Try
                If Not McInstanceLock.TryAcquire(Worker, InstanceCloner.SourceVersionFolder(Instance.PathVersion)) Then
                    Abandoned = True
                    Worker.Start()
                    Work.ReleaseBusy()
                    Return
                End If
            Catch ex As Exception
                Abandoned = True
                Worker.Start()
                Work.ReleaseBusy()
                Work.SourceError(ex)
                Return
            End Try
        End If
        AddHandler Worker.Stopped, Sub() Work.Stopped()
        Worker.Start()
        LoaderTaskbarAdd(Loader)
        FrmMain.BtnExtraDownload.ShowRefresh()
    End Sub
End Class
