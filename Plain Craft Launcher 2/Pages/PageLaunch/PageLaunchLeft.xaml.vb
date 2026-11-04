Imports System.Windows.Threading
Imports PCLLF.Presentation.Launch

Public Class PageLaunchLeft
    Private ReadOnly Native As LegacyLaunchLeft(Of McInstance, AniData)
    Public Sub New()
        Native = New LegacyLaunchLeft(Of McInstance, AniData)(CreateView(), CreateHost(), LaunchPageRuntime.Animations())
        InitializeComponent()
    End Sub
    Private Function CreateView() As LegacyLaunchLeftView
        Return New LegacyLaunchLeftView With {
            .AprilPosition = Function() AprilPosTrans, .Input = Function() PanInput, .Launching = Function() PanLaunching,
            .LaunchingHint = Function() PanLaunchingHint, .LaunchingInfo = Function() PanLaunchingInfo, .LoginPanel = Function() PanLogin, .LoginChildren = Function() PanLogin,
            .Method = Function() LabLaunchingMethod, .Name = Function() LabLaunchingName, .Stage = Function() LabLaunchingStage,
            .Title = Function() LabLaunchingTitle, .Progress = Function() LabLaunchingProgress, .ProgressLeft = Function() LabLaunchingProgressLeft,
            .Download = Function() LabLaunchingDownload, .DownloadLeft = Function() LabLaunchingDownloadLeft, .Hint = Function() LabLaunchingHint,
            .SingleTypeLabel = Function() LabTypeOne, .FinishedColumn = Function() ProgressLaunchingFinished, .UnfinishedColumn = Function() ProgressLaunchingUnfinished,
            .TypePanel = Function() PanType, .SingleTypePanel = Function() PanTypeOne, .MicrosoftRadio = Function() RadioLoginType5, .OfflineRadio = Function() RadioLoginType0,
            .SingleTypePath = Function() PathTypeOne, .MicrosoftChecked = Function() RadioLoginType5.Checked,
            .LaunchButtonEnabled = Function() BtnLaunch.IsEnabled, .LaunchButtonLoaded = Function() BtnLaunch.IsLoaded, .LaunchButtonText = Function() BtnLaunch.Text,
            .CheckNamedRadio = AddressOf CheckNamedRadio, .RequireNamedRadio = Sub(Name) CType(FindName(Name), MyRadioButton).Checked = True, .SetLoadingRun = Sub(Value) LoadLaunching.State.LoadingState = If(Value, MyLoading.MyLoadingState.Run, MyLoading.MyLoadingState.Stop),
            .RenderDispatchWait = Sub(Work) Dispatcher.Invoke(Work, DispatcherPriority.Render)}
    End Function
    Private Function CreateHost() As LegacyLaunchLeftHost(Of McInstance)
        Return New LegacyLaunchLeftHost(Of McInstance) With {
            .ReadString = Function(Key) Settings.Get(Of String)(Key), .ReadInteger = Function(Key) Settings.Get(Of Integer)(Key), .ReadBoolean = Function(Key) Settings.Get(Of Boolean)(Key),
            .ReadInstanceInteger = Function(Key) Settings.Get(Of Integer)(Key, Instance:=McInstanceSelected), .ReadInstanceString = Function(Key) Settings.Get(Of String)(Key, Instance:=McInstanceSelected),
            .WriteSetting = Sub(Key, Value) Settings.Set(Key, Value), .ReadIniVersion = Function(Address) ReadIni(Address, "Version"),
            .LoginType = Function() CInt(Settings.Get(Of McLoginType)("LoginType")), .SetLoginType = Sub(Value) Settings.Set("LoginType", CType(Value, McLoginType)),
            .ChangeAnimationControl = Sub(Delta) AniControlEnabled += Delta,
            .Selected = Function() McInstanceSelected, .SetSelected = Sub(Value) McInstanceSelected = Value, .SetSetupInstance = Sub(Value) PageInstanceLeft.Instance = Value,
            .CreateInstance = Function(Value) New McInstance(Value), .CheckInstance = Function(Value) Value.Check(), .LoadInstance = Sub(Value) Value.Load(),
            .InstancePath = Function(Value) Value.PathVersion, .InstanceName = Function(Value) Value.Name, .InstanceLogo = Function(Value) Value.Logo,
            .Instances = Function() McInstanceList.Values, .Folder = Function() McFolderSelected, .SetFolder = Sub(Value) McFolderSelected = Value,
            .FirstFolderLocation = Function() McFolderList.First().Location, .BasePath = Function() Paths.Base,
            .IsParentPath = Function(Address) PathUtils.IsParentOf(McFolderSelected, Address),
            .WaitForInstanceFolder = Sub(Address) LoaderFolderRun(McInstanceListLoader, Address, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\", WaitForExit:=True),
            .InstanceListState = Function() CType(CInt(McInstanceListLoader.State), LegacyLaunchPanelState), .FolderListState = Function() CType(CInt(McFolderListLoader.State), LegacyLaunchPanelState),
            .LaunchState = Function() CType(CInt(McLaunchLoader.State), LegacyLaunchPanelState), .WaitForFolders = Sub(Force) McFolderListLoader.WaitForExit(IsForceRestart:=Force),
            .CreateCurrentFolder = Sub() PageSelectLeft.CreateMcFolderInCurrentPath(), .InstallModpack = Function(Address) LaunchPageRuntime.Job(ModpackInstall(Address)),
            .BindListStates = AddressOf BindListStates, .Dispatch = Sub(Work) RunInUi(Work), .RunInThread = Sub(Work) RunInThread(Work),
            .RunInNewThread = Sub(Work, Name, Priority) RunInNewThread(Work, Name, Priority), .RandomInteger = Function(Minimum, Maximum) RandomInteger(Minimum, Maximum),
            .LoginAble = AddressOf McLoginAble, .StartAutoLogin = Sub() McLoginLoader.Start(), .CommandLine = Function() Environment.CommandLine, .RandomHint = AddressOf PageOtherTest.GetRandomHint,
            .ReadPage = AddressOf LaunchPageRuntime.ReadPage, .CreatePage = AddressOf LaunchPageRuntime.CreatePage, .StorePage = AddressOf LaunchPageRuntime.StorePage,
            .ReloadPage = Sub(Page, KeepInput) CType(Page, Object).Reload(KeepInput),
            .ShieldLogo = Function() Logo.IconButtonShield, .OfflineLogo = Function() Logo.IconButtonOffline, .CardLogo = Function() Logo.IconButtonCard,
            .ExportScriptSafe = Function() CurrentLaunchOptions?.SaveBatch IsNot Nothing, .ExportScript = Function() CurrentLaunchOptions.SaveBatch IsNot Nothing,
            .ResetLaunchProcessAndWatcher = AddressOf ResetLaunchProcessAndWatcher,
            .RightPageAllowsLaunch = Function() FrmMain.PageRight Is Nothing OrElse FrmMain.PageRight.PageState = MyPageRight.PageStates.ContentStay OrElse FrmMain.PageRight.PageState = MyPageRight.PageStates.ContentEnter,
            .AprilEnabled = Function() IsAprilEnabled, .AprilGiveUp = Function() IsAprilGiveup, .SetAprilGiveUp = Sub(Value) IsAprilGiveup = Value,
            .ResetGlobalAprilTransforms = AddressOf ResetGlobalAprilTransforms, .RefreshAprilButton = Sub() FrmMain.BtnExtraApril.ShowRefresh(),
            .ModpackMode = Function() ModModpackMode.Enabled, .HiddenForceShow = Function() PageSetupUI.HiddenForceShow,
            .SelectedInstanceLocked = Function() McInstanceLock.IsLocked(McInstanceSelected), .AllowsInstance = AddressOf ModModpackMode.AllowsInstance,
            .ApplyModpackLayout = Sub() ModModpackMode.ApplyLaunchLayout(FrmLaunchLeft),
            .GlobalButtons = New LegacyLaunchButtonView With {
                .Text = Sub(Value) FrmLaunchLeft.BtnLaunch.Text = Value, .VersionText = Sub(Value) FrmLaunchLeft.LabVersion.Text = Value,
                .EnableLaunch = Sub(Value) FrmLaunchLeft.BtnLaunch.IsEnabled = Value, .EnableVersion = Sub(Value) FrmLaunchLeft.BtnVersion.IsEnabled = Value,
                .VersionVisibility = Sub(Value) FrmLaunchLeft.BtnVersion.Visibility = Value, .MoreVisibility = Sub(Value) FrmLaunchLeft.BtnMore.Visibility = Value,
                .ReadVersionVisibility = Function() FrmLaunchLeft.BtnVersion.Visibility},
            .HintError = Sub(Text) Hint(Text, HintType.Red), .LaunchLog = Sub(Text) McLaunchLog(Text), .StartGame = Sub() McLaunchStart(),
            .NavigateInstances = Sub() FrmMain.PageChange(FormMain.PageType.InstanceSelect), .NavigateDownloadInstall = Sub() FrmMain.PageChange(FormMain.PageType.Download, FormMain.PageSubType.DownloadInstall),
            .NavigateInstanceSetup = Sub() FrmMain.PageChange(FormMain.PageType.InstanceSetup, 0), .TestNavigate = AddressOf TestNavigate, .EndProgramForce = AddressOf FormMain.EndProgramForce,
            .HasRealLaunch = Function() McLaunchLoaderReal IsNot Nothing, .HasWatcher = Function() McLaunchWatcher IsNot Nothing,
            .RealLaunch = Function() LaunchPageRuntime.Job(McLaunchLoaderReal, Function() McLaunchLoaderReal.GetLoaderList(False)), .KillWatcher = Sub() McLaunchWatcher.Kill(),
            .LaunchProcess = Function() McLaunchProcess, .NetSpeed = Function() NetManager.Speed,
            .NetTasks = Function() NetManager.Tasks.Select(Function(Value) LaunchPageRuntime.Item(Value)), .FormatByteSize = AddressOf StringUtils.FormatByteSize}
    End Function
    Private Sub BindListStates(Work As Action)
        AddHandler McInstanceListLoader.LoadingStateChanged, Sub(NewState, OldState) Work()
        AddHandler McFolderListLoader.LoadingStateChanged, Sub(NewState, OldState) Work()
    End Sub
    Private Sub CheckNamedRadio(Name As String)
        Dim Control As MyRadioButton = FindName(Name)
        If Control IsNot Nothing Then Control.Checked = True
    End Sub
    Private Shared Sub ResetLaunchProcessAndWatcher()
        McLaunchProcess = Nothing
        McLaunchWatcher = Nothing
    End Sub
    Private Shared Sub ResetGlobalAprilTransforms()
        FrmLaunchLeft.AprilScaleTrans.ScaleX = 1
        FrmLaunchLeft.AprilScaleTrans.ScaleY = 1
        FrmLaunchLeft.AprilPosTrans.X = 0
        FrmLaunchLeft.AprilPosTrans.Y = 0
    End Sub
    Private Shared Sub TestNavigate(Kind As Integer, SubKind As Integer)
        Select Case Kind
            Case 0
                If SubKind = 0 Then
                    FrmMain.PageChange(FormMain.PageType.Download)
                Else
                    FrmMain.PageChange(FormMain.PageType.Download, FormMain.PageSubType.DownloadMod)
                End If
            Case 1 : FrmMain.PageChange(FormMain.PageType.Setup)
            Case 2 : FrmMain.PageChange(FormMain.PageType.Other)
        End Select
    End Sub
    Public Sub PageLaunchLeft_Loaded() Handles Me.Loaded
        Native.Loaded()
    End Sub
    Public Sub PageChangeToLaunching()
        Native.PageChangeToLaunching()
    End Sub
    Public Sub PageChangeToLogin()
        Native.PageChangeToLogin()
    End Sub
    Public Sub RefreshPage(KeepInput As Boolean, Anim As Boolean)
        Native.RefreshPage(KeepInput, Anim)
    End Sub
    Private Sub RadioLoginType_Change(sender As Object, raiseByMouse As Boolean) Handles RadioLoginType0.Check, RadioLoginType5.Check
        Native.RadioChanged(raiseByMouse)
    End Sub
    Public Shared SkinMs As New LoaderTask(Of (String, String), String)("Loader Skin Ms", AddressOf SkinMsLoad, AddressOf SkinMsInput, ThreadPriority.AboveNormal)
    Private Shared Function SkinMsInput() As (String, String)
        Return LaunchPageRuntime.Skins.Input("Ms")
    End Function
    Private Shared Sub SkinMsLoad(Data As LoaderTask(Of (String, String), String))
        LaunchPageRuntime.Skins.Official(Data, "Ms")
    End Sub
    Public Shared SkinLegacy As New LoaderTask(Of (String, String), String)("Loader Skin Legacy", AddressOf SkinLegacyLoad, AddressOf SkinLegacyInput, ThreadPriority.AboveNormal)
    Private Shared Function SkinLegacyInput() As (String, String)
        Return LaunchPageRuntime.Skins.Input("Legacy")
    End Function
    Private Shared Sub SkinLegacyLoad(Data As LoaderTask(Of (String, String), String))
        LaunchPageRuntime.Skins.Offline(Data)
    End Sub
    Public Shared SkinNide As New LoaderTask(Of (String, String), String)("Loader Skin Nide", AddressOf SkinNideLoad, AddressOf SkinNideInput, ThreadPriority.AboveNormal)
    Private Shared Function SkinNideInput() As (String, String)
        Return LaunchPageRuntime.Skins.Input("Nide")
    End Function
    Private Shared Sub SkinNideLoad(Data As LoaderTask(Of (String, String), String))
        LaunchPageRuntime.Skins.Official(Data, "Nide")
    End Sub
    Public Shared SkinAuth As New LoaderTask(Of (String, String), String)("Loader Skin Auth", AddressOf SkinAuthLoad, AddressOf SkinAuthInput, ThreadPriority.AboveNormal)
    Private Shared Function SkinAuthInput() As (String, String)
        Return LaunchPageRuntime.Skins.Input("Auth")
    End Function
    Private Shared Sub SkinAuthLoad(Data As LoaderTask(Of (String, String), String))
        LaunchPageRuntime.Skins.Official(Data, "Auth")
    End Sub
    '必须位于四个真实加载器之后，保留 LF 的静态初始化顺序与列表身份。
    Public Shared SkinLoaders As New List(Of LoaderTask(Of (String, String), String)) From {SkinMs, SkinLegacy, SkinNide, SkinAuth}
    Private Sub BtnVersion_Click() Handles BtnVersion.Click
        Native.VersionClick()
    End Sub
    Public Sub LaunchButtonClick() Handles BtnLaunch.Click
        Native.LaunchButtonClick()
    End Sub
    Public Sub RefreshButtonsUI() Handles BtnLaunch.Loaded
        Native.RefreshButtonsUI()
    End Sub
    Public Sub BtnCancel_Click() Handles BtnCancel.Click
        Native.CancelClick()
    End Sub
    Private Sub BtnMore_Click() Handles BtnMore.Click
        Native.MoreClick()
    End Sub
    Public Sub LaunchingRefresh()
        Native.LaunchingRefresh()
    End Sub
    Private Sub PanLaunchingInfo_SizeChangedW(sender As Object, e As SizeChangedEventArgs) Handles PanLaunchingInfo.SizeChanged
        Native.WidthChanged(e)
    End Sub
    Private Sub PanLaunchingInfo_SizeChangedH(sender As Object, e As SizeChangedEventArgs) Handles PanLaunchingInfo.SizeChanged
        Native.HeightChanged(e)
    End Sub
End Class
