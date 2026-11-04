Imports FlowEngine = PCLCS.PackUpdateFlowEngine(Of PCL.PackUpdateFile, PCL.PackUpdatePackage, PCL.PackUpdateChange, PCL.PackUpdatePlan, PCL.ModNet.NetFile, PCL.ResourceVersion, PCL.ModMinecraft.McInstance, PCL.ModLoader.LoaderTask(Of String, String), PCL.ModLoader.LoaderCombo(Of String), MeloongCore.RedoableWorker, MeloongCore.ProgressProvider)

Friend NotInheritable Class ModpackUpdateFlow
    Private Shared Active As Integer
    Private Shared ReadOnly Engine As New FlowEngine(ModpackUpdate.Engine, CreateHost())
    Private Shared Function CreateHost() As PCLCS.PackUpdateFlowHost(Of PackUpdatePackage, PackUpdatePlan, NetFile, ResourceVersion, McInstance, LoaderTask(Of String, String), LoaderCombo(Of String), RedoableWorker, ProgressProvider)
        Return New PCLCS.PackUpdateFlowHost(Of PackUpdatePackage, PackUpdatePlan, NetFile, ResourceVersion, McInstance, LoaderTask(Of String, String), LoaderCombo(Of String), RedoableWorker, ProgressProvider) With {
            .ReadActive = Function() PCLCS.PackUpdateFlowState.Read(Active),
            .CompareExchangeActive = Function(Value, Comparand) PCLCS.PackUpdateFlowState.CompareExchange(Active, Value, Comparand),
            .ExchangeActive = Function(Value) PCLCS.PackUpdateFlowState.Exchange(Active, Value),
            .EnvironmentBusy = Function() EnvironmentHistoryUi.IsBusy,
            .HasRunningGame = Function() HasRunningMinecraft,
            .HasRunningSessions = AddressOf QuickPlayWorlds.HasRunningSessions,
            .LaunchLoading = Function() McLaunchLoader.State = LoadState.Loading,
            .IsLocked = Function(Instance) McInstanceLock.IsLocked(Instance),
            .Hint = Sub(Message) Hint(Message),
            .HintRed = Sub(Message) Hint(Message, HintType.Red),
            .SelectFile = Function(Title) Dialogs.SelectFile(Title, False, filter:={({"zip", "mrpack"}, "整合包文件")}).FirstOrDefault(),
            .CreateStep = Function(Name, Work) New LoaderTask(Of String, String)(Name, Work),
            .CreateCombo = Function(Name, StepLoader) New LoaderCombo(Of String)(Name, {StepLoader}) With {.OnStateChanged = AddressOf LoaderStateChangedHintOnly},
            .CreateWorker = Function(Loader, Folder) New RedoableWorker(LoaderFileOperation.CreateWorkload(Loader, Folder)),
            .TryAcquire = Function(Worker, Folder) McInstanceLock.TryAcquire(Worker, Folder),
            .OnStopped = Sub(Worker, Work) AddHandler Worker.Stopped, Sub() Work(),
            .StartWorker = Sub(Worker) Worker.Start(),
            .AddTaskbar = Sub(Loader) LoaderTaskbarAdd(Loader),
            .RefreshButton = Sub() FrmMain.BtnExtraDownload.ShowRefresh(),
            .Name = Function(Instance) Instance.Name,
            .VersionFolder = Function(Instance) Instance.PathVersion,
            .GameRoot = Function(Instance) Instance.PathIndie,
            .Version = Function(Instance) Instance.Version,
            .UiWait = Sub(Work) RunInUiWait(Sub() Work()),
            .InputProjectAddress = Function() MyMsgBoxInput("整合包项目地址", "CurseForge 或 Modrinth 整合包项目地址", "", Button2:="取消"),
            .Request = Function(Url) DlModRequest(Url),
            .EnsureWithin = Sub(Root, FileName) InstanceResourceStore.EnsureWithin(Root, FileName),
            .Message = Function(Caption, Title, Button1, Button2, Warn) MyMsgBox(Caption, Title, Button1, Button2, IsWarn:=Warn),
            .Preview = AddressOf Preview,
            .SelectVersion = AddressOf SelectOnlineVersion,
            .Project = Function(Id, Platform) ResourceProject.FromProjectId(Id, Platform),
            .RemoveVersionCache = Sub(Id) ResourceVersion.ProjectFilesCache.Remove(Id),
            .Versions = AddressOf ResourceVersion.FromProjectId,
            .ToUpdateFile = Function(Version, Destination, Game, Loaders) Version.ToNetFile(Destination, ResourceVersion.DownloadReason.Update, Game, Loaders),
            .Download = AddressOf ModpackUpdate.DownloadFilesWithProgress,
            .TempFolder = Function() RequestTaskTempFolder(),
            .Cancellation = Function(Task) Task.CreateCancellationToken(),
            .SetProgress = Sub(Task, Value) Task.Progress = Value,
            .Progress = Function(Task, Start, Finish) Task.CreateSyncProgressProvider(Start, Finish),
            .SetOutput = Sub(Task, Value) Task.Output = Value,
            .CreateRuntime = AddressOf CreateRuntime,
            .RuntimeWorkload = Function(Build, Folder, Progress) LoaderFileOperation.CreateWorkloadWithProgress(Build, Folder, Progress),
            .CancelCombo = Sub(Build) Build.Cancel(),
            .BeforeMinecraftChange = Sub(Instance, OldMinecraft, NewMinecraft, Token) WorldBackups.BeforeMinecraftChange(Instance, OldMinecraft, NewMinecraft, Token),
            .CaptureHistory = Sub(Instance, Caption, Token) EnvironmentHistory.CaptureInstance(Instance, Caption, Token),
            .ResetSettings = Sub(Instance) Instance.ResetSettingsCache(),
            .LoadInstance = Function(Folder) New McInstance(Folder).Load(),
            .PageInstance = Function() PageInstanceLeft.Instance,
            .SetPageInstance = Sub(Value) PageInstanceLeft.Instance = Value,
            .SelectedInstance = Function() McInstanceSelected,
            .SetSelectedInstance = Sub(Value) McInstanceSelected = Value,
            .Explorer = AddressOf OpenExplorer,
            .Info = Sub(Message) Logger.Info(Message)}
    End Function
    Friend Shared ReadOnly Property IsUpdating As Boolean
        Get
            Return Engine.IsUpdating
        End Get
    End Property
    Friend Shared Sub Start(Instance As McInstance, Online As Boolean)
        Engine.Start(Instance, Online)
    End Sub
    Friend Shared Sub OpenBackups(Instance As McInstance)
        Engine.OpenBackups(Instance)
    End Sub
    Private Shared Function SelectArchive(Title As String) As String
        Return Engine.SelectArchive(Title)
    End Function
    Private Shared Function ChooseOnline(Baseline As JObject, Stage As String, Token As CancellationToken, ByRef Source As JObject, Progress As ProgressProvider) As String
        Return Engine.ChooseOnline(Baseline, Stage, Token, Source, Progress)
    End Function
    Private Shared Sub Execute(Instance As McInstance, Online As Boolean, ArchivePath As String, Task As LoaderTask(Of String, String))
        Engine.Execute(Instance, Online, ArchivePath, Task)
    End Sub
    Friend Shared Function RecoverInstance(Instance As McInstance) As McInstance
        Return Engine.RecoverInstance(Instance)
    End Function
    Private Shared Function ReloadInstance(Instance As McInstance) As McInstance
        Return Engine.ReloadInstance(Instance)
    End Function
    '实际模态控件及运行时 Loader 构造，流程决策与取消/等待编排由 C# 持有。
    Private Shared Function Preview(Summary As String, Plan As PackUpdatePlan, Version As Func(Of McVersion)) As Integer
        Return MyMsgBoxConfirmContent(Summary, "整合包更新预览", "保留个人修改并更新",
            Function() New ModpackUpdatePreviewView(Plan, Version().VanillaName, Version().ModLoaderVersions), IsWarn:=False)
    End Function
    Private Shared Function SelectOnlineVersion(Versions As List(Of ResourceVersion), Name As String, CurrentVersion As String) As ResourceVersion
        Dim View As New ModpackVersionSelectView(Versions, Name, CurrentVersion)
        Return If(MyMsgBoxConfirmContent("请选择要下载并预览的整合包版本。", "选择整合包版本", "下载并预览", Function() View, IsWarn:=False) = 1, View.Selected, Nothing)
    End Function
    Private Shared Function CreateRuntime(Request As PCLCS.PackUpdateRuntimeRequest) As LoaderCombo(Of String)
        Return New LoaderCombo(Of String)("准备整合包游戏组件", McInstallLoadersGet(New McInstallRequest With {
            .NewInstanceName = Request.Name, .InstanceFolder = Request.BuildFolder, .Components = New McComponents(Request.Minecraft, Request.Loaders), .IsIntermediateOutput = True,
            .MinecraftFolder = Request.MinecraftFolder}))
    End Function
End Class
