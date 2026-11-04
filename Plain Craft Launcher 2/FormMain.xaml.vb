Imports System.ComponentModel
Imports System.Windows.Interop

Imports PCLCS.Startup
Imports PCLLF.Presentation.Windowing
Imports PCLLF.Presentation.Controls
Imports PCLLF.Presentation.Animation
Imports WindowCore = PCLLF.Presentation.Windowing.LegacyMainWindowController(Of PCL.FormMain.PageStackData, PCL.ModMinecraft.McInstance, PCL.ModAnimation.AniData)
Imports WindowHost = PCLLF.Presentation.Windowing.LegacyMainWindowHost(Of PCL.FormMain.PageStackData, PCL.ModMinecraft.McInstance, PCL.ModAnimation.AniData)

Public Class FormMain

#Region "基础"

    '更新日志
    Private Sub ShowUpdateLog()
        Controller.ShowUpdateLog()
    End Sub

    '窗口加载
    Private IsWindowLoadFinished As Boolean = False
    Private ReadOnly Controller As New WindowCore(CreateWindowView(), CreateWindowHost())
    Public Sub New()
        Controller.ConstructBeforeXaml()
        InitializeComponent()
        Controller.ConstructAfterXaml()
    End Sub
    Private Sub FormMain_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Controller.Loaded()
    End Sub
    '根据打开次数触发的事件
    '升级与降级事件
    Private Sub UpgradeSub(LastVersionCode As Integer)
        Controller.Upgrade(LastVersionCode)
    End Sub
    Private Sub DowngradeSub(LastVersionCode As Integer)
        Controller.Downgrade(LastVersionCode)
    End Sub

#End Region

#Region "自定义窗口"

    '关闭
    Private Sub FormMain_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        Controller.Closing(e)
    End Sub
    ''' <summary>
    ''' 正常关闭程序。程序将在执行此方法后约 0.3s 退出。
    ''' </summary>
    ''' <param name="SendWarning">是否在还有下载任务未完成时发出警告。</param>
    Public Sub EndProgram(SendWarning As Boolean)
        Controller.EndProgram(SendWarning)
    End Sub
    Private Shared IsLogShown As Boolean = False
    Public Shared Sub EndProgramForce(Optional ReturnCode As ProcessReturnValues = ProcessReturnValues.Success)
        LegacyMainTermination.EndProgramForce(CInt(ReturnCode), CreateTerminationHost())
    End Sub
    Private Sub BtnTitleClose_Click(sender As Object, e As RoutedEventArgs) Handles BtnTitleClose.Click
        Controller.EndProgram(True)
    End Sub

    '移动
    Private Sub FormDragMove(sender As Object, e As MouseButtonEventArgs) Handles PanTitle.MouseLeftButtonDown, PanMsg.MouseLeftButtonDown
        Controller.DragMove(sender)
    End Sub

    '改变大小
    ''' <summary>
    ''' 是否可以向注册表储存尺寸改变信息。以此避免初始化时误储存。
    ''' </summary>
    Public IsSizeSaveable As Boolean = False
    Private Sub FormMain_SizeChanged() Handles Me.SizeChanged, Me.Loaded
        Controller.SizeChanged()
    End Sub

    '最小化
    Private Sub BtnTitleMin_Click() Handles BtnTitleMin.Click
        Controller.Minimize()
    End Sub

    '背景图片与标题栏
    Public Shared Sub UpdateBackgroundAndTitleBar(Value) '从设置更新
        LegacyMainWindowStatics.UpdateBackground(Function() FrmMain, Sub() FrmMain.UpdateBackgroundAndTitleBar())
    End Sub
    Public Sub UpdateBackgroundAndTitleBar()
        Controller.UpdateBackgroundAndTitleBar()
    End Sub

    Friend Sub UpdateTitleBar()
        Controller.UpdateTitleBar()
    End Sub

#End Region

#Region "窗体事件"

    '按键事件
    Private Sub FormMain_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        Controller.KeyDown(e)
    End Sub
    Private Sub FormMain_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles Me.MouseDown
        Controller.MouseDown(e)
    End Sub
    Private Sub TriggerPageBack()
        Controller.TriggerPageBack()
    End Sub

    '切回窗口
    Private Sub FormMain_Activated() Handles Me.Activated
        Controller.Activated()
    End Sub

    '文件拖放
    Private Sub HandleDrag(sender As Object, e As DragEventArgs)
        Static PrevData As IDataObject, PrevEffects As DragDropEffects
        Controller.HandleDrag(sender, e, PrevData, PrevEffects)
    End Sub
    Private Sub FrmMain_Drop(sender As Object, e As DragEventArgs) Handles Me.Drop
        Controller.Drop(sender, e)
    End Sub
    Private Sub FileDrag(FilePathList As IEnumerable(Of String))
        Controller.FileDrag(FilePathList)
    End Sub

    '接受到 Windows 窗体事件
    Public IsSystemTimeChanged As Boolean = False
    Private Function WndProc(hwnd As IntPtr, msg As Integer, wParam As IntPtr, lParam As IntPtr, ByRef handled As Boolean) As IntPtr
        Return Controller.WndProc(hwnd, msg, wParam, lParam, handled)
    End Function

    '窗口隐藏与置顶
    Private _Hidden As Boolean = False
    Public Property Hidden As Boolean
        Get
            Return _Hidden
        End Get
        Set(value As Boolean)
            Controller.SetHidden(value)
        End Set
    End Property
    ''' <summary>
    ''' 把当前窗口拖到最前面。
    ''' </summary>
    Public Sub ShowWindowToTop()
        Controller.ShowWindowToTop()
    End Sub

#End Region

#Region "切换页面"

    '页面种类与属性
    ''' <summary>
    ''' 页面种类。
    ''' 该枚举在自定义事件中使用，是公开 API 的一部分。
    ''' </summary>
    Public Enum PageType
        ''' <summary>
        ''' 启动。
        ''' </summary>
        Launch = 0
        ''' <summary>
        ''' 下载。
        ''' </summary>
        Download = 1
        ''' <summary>
        ''' 联机。
        ''' </summary>
        Link = 2
        ''' <summary>
        ''' 设置。
        ''' </summary>
        Setup = 3
        ''' <summary>
        ''' 更多。
        ''' </summary>
        Other = 4
        ''' <summary>
        ''' 版本选择。这是一个副页面。
        ''' </summary>
        InstanceSelect = 5
        ''' <summary>
        ''' 任务管理。这是一个副页面。
        ''' </summary>
        TaskManager = 6
        ''' <summary>
        ''' 版本设置。这是一个副页面。
        ''' </summary>
        InstanceSetup = 7
        ''' <summary>
        ''' 社区资源详情。这是一个副页面。
        ''' </summary>
        ResourceDetail = 8
        ''' <summary>
        ''' 帮助详情。这是一个副页面。
        ''' </summary>
        HelpDetail = 9
    End Enum
    ''' <summary>
    ''' 次要页面种类。其数值必须与 StackPanel 中的下标一致。
    ''' 该枚举在自定义事件中使用，是公开 API 的一部分。
    ''' </summary>
    Public Enum PageSubType
        [Default] = 0
        DownloadInstall = 0
        DownloadMod = 2
        DownloadPack = 3
        DownloadDataPack = 4
        DownloadResourcePack = 5
        DownloadShader = 6
        DownloadMap = 7
        SetupLaunch = 0
        SetupLink = 1
        SetupUI = 2
        SetupSystem = 3
        LinkMain = 0
        OtherHelp = 0
        OtherAbout = 1
        OtherTest = 2
        InstanceOverall = 0
        InstanceSetup = 1
        InstanceModify = 2
        InstanceMod = 3
        InstanceModDisabled = 4
        InstanceExport = 5
        InstanceSaves = 6
        InstanceScreenshots = 7
        InstanceResourcePacks = 8
        InstanceShaderPacks = 9
        InstanceSchematics = 10
        InstanceKeybindings = 11
        InstanceRestorePoints = 12
        InstanceBackups = 13
        InstanceClone = 14
    End Enum
    ''' <summary>
    ''' 获取次级页面的名称。若并非次级页面则返回空字符串，故可以以此判断是否为次级页面。
    ''' </summary>
    Private Function PageNameGet(Stack As PageStackData) As String
        Return Controller.PageNameGet(Stack)
    End Function
    ''' <summary>
    ''' 刷新次级页面的名称。
    ''' </summary>
    Public Sub PageNameRefresh(Type As PageStackData)
        Controller.PageNameRefresh(Type)
    End Sub
    ''' <summary>
    ''' 刷新次级页面的名称。
    ''' </summary>
    Public Sub PageNameRefresh()
        Controller.PageNameRefresh()
    End Sub

    '页面状态存储
    ''' <summary>
    ''' 当前的主页面。
    ''' </summary>
    Public PageCurrent As PageStackData = PageType.Launch
    ''' <summary>
    ''' 上一个主页面。
    ''' </summary>
    Public PageLast As PageStackData = PageType.Launch
    ''' <summary>
    ''' 当前的子页面。
    ''' </summary>
    Public ReadOnly Property PageCurrentSub As PageSubType
        Get
            Return CType(Controller.PageCurrentSub, PageSubType)
        End Get
    End Property
    ''' <summary>
    ''' 上层页面的编号堆栈，用于返回。
    ''' </summary>
    Public PageStack As New List(Of PageStackData)
    Public Class PageStackData
        Implements ILegacyMainPageStack

        Public Page As PageType
        Public Additional As Object

        Public Overrides Function Equals(other As Object) As Boolean
            Return LegacyMainPageIdentity.Equals(Me, other)
        End Function
        Private Property CorePageCode As Integer Implements ILegacyMainPageStack.PageCode
            Get
                Return CInt(Page)
            End Get
            Set(value As Integer)
                Page = CType(value, PageType)
            End Set
        End Property
        Private ReadOnly Property CoreAdditional As Object Implements ILegacyMainPageStack.Additional
            Get
                Return Additional
            End Get
        End Property
        Public Shared Operator =(left As PageStackData, right As PageStackData) As Boolean
            Return EqualityComparer(Of PageStackData).Default.Equals(left, right)
        End Operator
        Public Shared Operator <>(left As PageStackData, right As PageStackData) As Boolean
            Return Not left = right
        End Operator
        Public Shared Widening Operator CType(Value As PageType) As PageStackData
            Return New PageStackData With {.Page = Value}
        End Operator
        Public Shared Widening Operator CType(Value As PageStackData) As PageType
            Return Value.Page
        End Operator
    End Class
    Public PageLeft As MyPageLeft, PageRight As MyPageRight

    '引发实际页面切换的入口
    Private IsChangingPage As Boolean = False
    ''' <summary>
    ''' 切换页面，并引起对应选择 UI 的改变。
    ''' </summary>
    Public Sub PageChange(Stack As PageStackData, Optional SubType As PageSubType = PageSubType.Default)
        Controller.PageChange(Stack, CInt(SubType))
    End Sub
    ''' <summary>
    ''' 通过点击导航栏改变页面。
    ''' </summary>
    Private Sub BtnTitleSelect_Click(sender As MyRadioButton, raiseByMouse As Boolean) Handles BtnTitleSelect0.Check, BtnTitleSelect1.Check, BtnTitleSelect2.Check, BtnTitleSelect3.Check, BtnTitleSelect4.Check
        Controller.TitleSelect(sender)
    End Sub
    ''' <summary>
    ''' 通过点击返回按钮或手动触发返回来改变页面。
    ''' </summary>
    Public Sub PageBack() Handles BtnTitleInner.Click
        Controller.PageBack()
    End Sub

    '实际处理页面切换
    ''' <summary>
    ''' 切换现有页面的实际方法。
    ''' </summary>
    Private Sub PageChangeActual(Stack As PageStackData, Optional SubType As PageSubType = -1)
        Controller.PageChangeActual(Stack, CInt(SubType))
    End Sub
    Private Sub PageChangeAnim(TargetLeft As FrameworkElement, TargetRight As FrameworkElement)
        Controller.PageChangeAnim(TargetLeft, TargetRight)
    End Sub
    ''' <summary>
    ''' 退出子界面。
    ''' </summary>
    Private Sub PageChangeExit()
        Controller.PageChangeExit()
    End Sub

    '左边栏改变
    Private Sub PanMainLeft_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles PanMainLeft.SizeChanged
        Controller.LeftSizeChanged(e)
    End Sub
    Private Sub PanMainLeft_Resize(NewWidth As Double)
        Controller.ResizeLeft(NewWidth)
    End Sub

#End Region

#Region "控件拖动"

    '在时钟中调用，使得即使鼠标在窗口外松开，也可以释放控件
    Public Sub DragTick()
        Controller.DragTick()
    End Sub
    '在鼠标移动时调用，以改变 Slider 位置
    Public Sub DragDoing() Handles PanBack.MouseMove
        Controller.DragDoing()
    End Sub
    Public Sub DragStop()
        Controller.DragStop()
    End Sub

#End Region

#Region "附加按钮"

    '音乐
    Private Sub BtnExtraMusic_Click(sender As Object, e As EventArgs) Handles BtnExtraMusic.Click
        Controller.MusicClick()
    End Sub
    Private Sub BtnExtraMusic_RightClick(sender As Object, e As EventArgs) Handles BtnExtraMusic.RightClick
        Controller.MusicRightClick()
    End Sub

    '任务管理
    Private Sub BtnExtraDownload_Click(sender As Object, e As EventArgs) Handles BtnExtraDownload.Click
        Controller.DownloadClick()
    End Sub
    Private Function BtnExtraDownload_ShowCheck() As Boolean
        Return Controller.DownloadShowCheck()
    End Function

    '投降
    Public Sub AprilGiveup() Handles BtnExtraApril.Click
        Controller.AprilGiveup()
    End Sub
    Public Function BtnExtraApril_ShowCheck() As Boolean
        Return Controller.AprilShowCheck()
    End Function

    '关闭 Minecraft
    Public Sub BtnExtraShutdown_Click() Handles BtnExtraShutdown.Click
        Controller.ShutdownClick()
    End Sub
    Public Function BtnExtraShutdown_ShowCheck() As Boolean
        Return Controller.ShutdownShowCheck()
    End Function

    ''' <summary>
    ''' 返回顶部。
    ''' </summary>
    Public Sub BackToTop() Handles BtnExtraBack.Click
        Controller.BackToTop()
    End Sub
    Private Function BtnExtraBack_ShowCheck() As Boolean
        Return Controller.BackShowCheck()
    End Function
    Private Function BtnExtraBack_GetRealChild() As MyScrollViewer
        Return DirectCast(Controller.RealScroll(), MyScrollViewer)
    End Function

#End Region

    '愚人节鼠标位置
    Public lastMouseArg As MouseEventArgs = Nothing
    Private Sub FormMain_MouseMove(sender As Object, e As MouseEventArgs) Handles Me.MouseMove
        Controller.MouseMove(e)
    End Sub

    'Only real fields, compiled BAML parts, existing modules and typed event/platform primitives.
    Private Function CreateWindowView() As LegacyMainWindowView
        Return New LegacyMainWindowView With {
            .Window = Me,
            .PanBack = Function() PanBack, .PanForm = Function() PanForm, .PanMain = Function() PanMain,
            .PanTitle = Function() PanTitle, .PanTitleMain = Function() PanTitleMain, .PanTitleInner = Function() PanTitleInner, .PanMsg = Function() PanMsg,
            .BorderForm = Function() BorderForm, .PanMainLeft = Function() PanMainLeft, .PanMainRight = Function() PanMainRight,
            .RectForm = Function() RectForm, .RectLeftBackground = Function() RectLeftBackground, .RectLeftShadow = Function() RectLeftShadow,
            .ImgBack = Function() ImgBack, .PanTitleSelect = Function() PanTitleSelect,
            .LabTitleInner = Function() LabTitleInner, .LabTitleLogo = Function() LabTitleLogo, .ShapeTitleLogo = Function() ShapeTitleLogo,
            .ImageTitleLogo = Function() ImageTitleLogo, .TransformPos = Function() TransformPos, .TransformRotate = Function() TransformRotate,
            .Resizer = AddressOf ResizerPart,
            .ExtraDownload = Function() ExtraPort(BtnExtraDownload), .ExtraBack = Function() ExtraPort(BtnExtraBack),
            .ExtraApril = Function() ExtraPort(BtnExtraApril), .ExtraShutdown = Function() ExtraPort(BtnExtraShutdown)
        }
    End Function
    Private Function CreateWindowHost() As WindowHost
        Dim Result As New WindowHost With {
            .AnimationSuppression = Function() AniControlEnabled, .WriteAnimationSuppression = Sub(Value) AniControlEnabled = Value,
            .DescribeSubType = Function(Value) CType(Value, PageSubType).ToString(),
            .Loaded = Function() IsWindowLoadFinished, .WriteLoaded = Sub(Value) IsWindowLoadFinished = Value,
            .SizeSaveable = Function() IsSizeSaveable, .WriteSizeSaveable = Sub(Value) IsSizeSaveable = Value,
            .Hidden = Function() _Hidden, .WriteHidden = Sub(Value) _Hidden = Value,
            .ChangingPage = Function() IsChangingPage, .WriteChangingPage = Sub(Value) IsChangingPage = Value,
            .WriteSystemTimeChanged = Sub(Value) IsSystemTimeChanged = Value,
            .CurrentPage = Function() PageCurrent, .WriteCurrentPage = Sub(Value) PageCurrent = Value,
            .LastPage = Function() PageLast, .WriteLastPage = Sub(Value) PageLast = Value, .PageStack = Function() PageStack,
            .CurrentLeft = Function() PageLeft, .WriteLeft = Sub(Value) PageLeft = DirectCast(Value, MyPageLeft),
            .CurrentRight = Function() PageRight, .WriteRight = Sub(Value) PageRight = DirectCast(Value, MyPageRight),
            .AssignGlobalWindow = Sub() FrmMain = Me, .CreateLaunchLeft = Sub() FrmLaunchLeft = New PageLaunchLeft,
            .CreateLaunchRight = Sub() FrmLaunchRight = New PageLaunchRight,
            .ReadInteger = Function(Key) Settings.Get(Of Integer)(Key), .ReadString = Function(Key) Settings.Get(Of String)(Key),
            .ReadBoolean = Function(Key) Settings.Get(Of Boolean)(Key), .HasSaved = Function(Key) Settings.HasSaved(Key),
            .DefaultValue = Function(Key) Settings.GetDefault(Key), .WriteSetting = Sub(Key, Value) Settings.Set(Key, Value), .ResetSetting = Sub(Key) Settings.Reset(Key),
            .HasIniKey = Function(File, Key) HasIniKey(File, Key), .ThemeRefresh = Sub(Value) ThemeRefresh(Value),
            .ThemeRefreshMain = Sub() ThemeRefreshMain(), .RefreshHidden = Sub() PageSetupUI.HiddenRefresh(),
            .RefreshBackground = Sub(First, Second) PageSetupUI.BackgroundRefresh(First, Second), .RefreshMusic = Sub(First, Second) MusicRefreshPlay(First, Second),
            .CreateResizer = Function(Target) New MyResizer(Target), .WorkingAreaHeight = Function() My.Computer.Screen.WorkingArea.Height,
            .WorkingAreaWidth = Function() My.Computer.Screen.WorkingArea.Width, .PixelToWpf = Function(Value) GetWPFSize(Value),
            .HasStartWindow = Function() FrmStart IsNot Nothing, .CloseStartWindow = Sub(Value) FrmStart.Close(Value),
            .IsAdmin = Function() WindowsUtils.HasAdminRole(), .IsDebug = Function() ModeDebug,
            .PackEnabled = Function() ModModpackMode.Enabled, .ApplyPackVisibility = Sub(Target) ModModpackMode.ApplyVisibility(DirectCast(Target, FormMain)),
            .PackBlocksPage = Function(Value) ModModpackMode.BlocksPage(CType(Value, PageType)),
            .NormalizeSetupPage = Function(Value) CInt(ModModpackMode.NormalizeSetupPage(CType(Value, PageSubType))),
            .NormalizeInstancePage = Function(Value) CInt(ModModpackMode.NormalizeInstancePage(CType(Value, PageSubType))),
            .DisplayPackTitle = Function(Value) ModModpackMode.DisplayTitle(Value), .LogoSettings = AddressOf LogoSettingsPort,
            .BuildType = CInt(BuildType), .VersionCode = VersionCode, .Dpi = Function() DPI,
            .VersionBaseName = Function() VersionBaseName, .VersionBranchMain = Function() VersionBranchMain,
            .LauncherName = Function() LauncherDisplayName, .ReleasesUrl = Function() LauncherReleasesUrl, .IssuesUrl = Function() LauncherIssuesUrl,
            .TempFolder = Function() PathTemp, .ExecutablePath = Function() PathExe,
            .Clock = Function() GetTimeMs(), .StartTick = Function() ApplicationStartTick, .WriteStartTick = Sub(Value) ApplicationStartTick = Value,
            .Handle = Function() Handle, .WriteHandle = Sub(Value) Handle = Value, .SetForeground = Sub(Value) SetForegroundWindow(Value),
            .ApplicationOpenTime = Function() ApplicationOpenTime,
            .Ui = Sub(Work) RunInUi(Work), .UiWait = Sub(Work) RunInUiWait(Work), .UiPosted = Sub(Work, Force) RunInUi(Work, Force),
            .NewThread = Sub(Work, Name, Priority) RunInNewThread(Work, Name, Priority),
            .RandomInteger = Function(Min, Max) ModBase.RandomInteger(Min, Max), .Hint = AddressOf MainHint,
            .Message = Function(Value) MyMsgBox(Value.Text, Value.Title, Value.Button1, Value.Button2, Value.Button3, Button3Action:=Value.ThirdAction),
            .OpenWebsite = Sub(Value) OpenWebsite(Value),
            .StartAnimationLoop = Sub() AniStart(), .StartMainTimer = Sub() TimerMainStart(), .JavaInitialize = Sub() JavaInit(),
            .ClearTaskTemp = Sub() TryClearTaskTemp(), .CheckUpdates = Sub() ModLauncherUpdate.CheckForUpdates(True),
            .StartFolderLoader = Sub() McFolderListLoader.Start(0), .StartClientList = Sub() DlClientListMojangLoader.Start(1),
            .CreateDragHelper = AddressOf CreateDragPort, .WindowHook = New HwndSourceHook(AddressOf WndProc), .DragHandler = New DragEventHandler(AddressOf HandleDrag),
            .FirstDialog = AddressOf FirstDialogPort, .GlobalMessageCount = Function() FrmMain.PanMsg.Children.Count,
            .WaitingMessages = Function() WaitingMyMsgBox.Any(), .GlobalNavigate = Sub(Value) FrmMain.PageChange(CType(Value, PageType)),
            .WriteLastMouseArg = Sub(Value) lastMouseArg = Value,
            .IsLaunching = Function() McLaunchLoader.State = LoadState.Loading, .CancelLaunch = Sub() FrmLaunchLeft.BtnCancel_Click(),
            .LaunchButton = Sub() FrmLaunchLeft.LaunchButtonClick(),
            .HiddenVersions = Function() FrmSelectRight.ShowHidden, .WriteHiddenVersions = Sub(Value) FrmSelectRight.ShowHidden = Value,
            .HiddenForceShow = Function() PageSetupUI.HiddenForceShow, .WriteHiddenForceShow = Sub(Value) PageSetupUI.HiddenForceShow = Value,
            .ReloadInstanceList = AddressOf ReloadInstanceList,
            .ReloadCurrentJava = Sub() PageInstanceLeft.ReloadCurrentJava(), .ReloadMods = Sub() FrmInstanceMod.ReloadModList(),
            .ExitInstallSelection = Sub() FrmDownloadInstall.ExitSelectPage(), .IsInstallSelection = Function() FrmDownloadInstall.IsInSelectPage,
            .CanRefresh = Function(Value) TypeOf Value Is IRefreshable, .Refresh = Sub(Value) DirectCast(Value, IRefreshable).Refresh(),
            .AprilEnabled = Function() IsAprilEnabled, .AprilGivenUp = Function() IsAprilGiveup, .WriteAprilGivenUp = Sub(Value) IsAprilGiveup = Value,
            .AprilScale = Function() FrmLaunchLeft.AprilScaleTrans,
            .SettingsInstance = Function() PageInstanceLeft.Instance, .SelectedInstance = Function() McInstanceSelected,
            .InstanceName = Function(Value) Value.Name, .InstanceLocked = Function(Value) McInstanceLock.IsLocked(Value),
            .InstanceBlocksNavigation = Function(Value) McInstanceLock.BlocksNavigation(Value),
            .WriteInstanceSetting = Sub(Key, Value, Target) Settings.Set(Key, Value, Instance:=Target),
            .ValidateHttp = Function(Value) New ValidateHttp().Validate(Value), .ReloadInstanceSetup = Sub() FrmInstanceSetup.Reload(),
            .RefreshLaunchPage = Sub() FrmLaunchLeft.RefreshPage(True, False), .RefreshCustomHome = Sub() FrmLaunchRight.ForceRefresh(),
            .InstallMods = Function(Value) PageInstanceMod.InstallMods(Value), .InstallModpack = Sub(Value) ModpackInstall(Value), .CreateCrashAnalyzer = AddressOf CrashPort,
            .EnsureLeftPage = AddressOf EnsureLeftPage, .EnsureRightPage = AddressOf EnsureRightPage,
            .LeftPage = AddressOf LeftPagePort, .RightPage = AddressOf RightPageElement, .NewBlankLeft = Function() New MyPageLeft,
            .HelpTitle = Function(Value) DirectCast(Value, HelpEntry).Title,
            .HasDownloadingTask = Function() HasDownloadingTask(), .HasLaunchOperation = Function() McLaunchLoaderReal IsNot Nothing,
            .HasRunningMinecraft = Function() HasRunningMinecraft,
            .TaskbarCancels = Function() LoaderTaskbar.Select(Function(Value) CType(Sub() Value.Cancel(), Action)),
            .WatcherKills = Function() McWatcherList.Select(Function(Value) CType(Sub() Value.Kill(), Action)), .CancelLaunchOperation = Sub() McLaunchLoaderReal.Cancel(),
            .MusicPause = Sub() MusicControlPause(), .MusicNext = Sub() MusicControlNext(),
            .DragControl = Function() DirectCast(DragControl, MySliderView), .ClearDragControl = Sub() DragControl = Nothing,
            .ForceExit = Sub() EndProgramForce(), .Animations = CreateMainAnimations()
        }
#If LEGACY_LINK Then
        Result.LegacyLink = True
        Result.LinkTryExit = Function(Force, Silent) If(FrmLinkMain?.TryExit(Force, Silent), False)
#End If
        Return Result
    End Function
    Private Function CreateMainAnimations() As LegacyMainAnimations(Of AniData)
        Return New LegacyMainAnimations(Of AniData) With {
            .Opacity = Function(Target, Value, Time, Delay, Ease, After) AaOpacity(Target, Value, Time, Delay, DirectCast(Ease, AniEase), After),
            .X = Function(Target, Value, Time, Delay, Ease, After) AaX(Target, Value, Time, Delay, DirectCast(Ease, AniEase), After),
            .Width = Function(Target, Value, Time, Delay, Ease, After) AaWidth(Target, Value, Time, Delay, DirectCast(Ease, AniEase), After),
            .Double = Function(Work, Value, Time, Delay, Ease, After) AaDouble(Sub(Delta) Work(CDbl(Delta)), Value, Time, Delay, DirectCast(Ease, AniEase), After),
            .Code = Function(Work, Delay, After) AaCode(Sub() Work(), Delay, After),
            .OutFluent = Function(Power) New AniEaseOutFluent(CType(CInt(Power), AniEasePower)),
            .InFluent = Function(Power) New AniEaseInFluent(CType(CInt(Power), AniEasePower)),
            .OutBack = Function(Power) New AniEaseOutBack(CType(CInt(Power), AniEasePower)),
            .InoutFluent = Function(Power) New AniEaseInoutFluent(CType(CInt(Power), AniEasePower)),
            .Start = Sub(Frames, Name, Refresh) AniStart(Frames, Name, Refresh), .Stop = Sub(Name) AniStop(Name), .Speed = Function() AniSpeed
        }
    End Function
    Private Shared Function CreateTerminationHost() As LegacyMainTerminationHost
        Return New LegacyMainTerminationHost With {
            .MarkEnding = Sub() IsProgramEnding = True, .DisableAnimations = Sub() AniControlEnabled += 1,
            .Feedback = Sub() FeedbackInfo(), .OpenLog = Sub() StartProcess(Paths.Base & "PCL\Log1.txt"),
            .SaveAll = Sub() ConfigUtils.SaveAll(), .FlushLog = Sub() Logger.Instance.Flush(), .Kill = Sub() Process.GetCurrentProcess().Kill(),
            .LogShown = Function() IsLogShown, .WriteLogShown = Sub(Value) IsLogShown = Value, .IssuesUrl = Function() LauncherIssuesUrl,
            .Exit = Sub(Value) Environment.Exit(Value), .DescribeCode = Function(Value) CType(Value, ProcessReturnValues).ToString()
        }
    End Function
    Private Function ResizerPart(Index As Integer) As UIElement
        Select Case Index
            Case 0 : Return ResizerB
            Case 1 : Return ResizerL
            Case 2 : Return ResizerLB
            Case 3 : Return ResizerLT
            Case 4 : Return ResizerR
            Case 5 : Return ResizerRB
            Case 6 : Return ResizerRT
            Case Else : Return ResizerT
        End Select
    End Function
    Private Shared Function ExtraPort(Button As MyExtraButton) As LegacyMainExtraButton
        Return New LegacyMainExtraButton With {
            .SetShowCheck = Sub(Work) Button.ShowCheck = Function() Work(), .ShowRefresh = Sub() Button.ShowRefresh(), .Show = Function() Button.Show
        }
    End Function
    Private Shared Sub MainHint(Text As String, Kind As LegacyMainHint)
        Select Case Kind
            Case LegacyMainHint.Red : Hint(Text, HintType.Red)
            Case LegacyMainHint.Green : Hint(Text, HintType.Green)
            Case Else : Hint(Text)
        End Select
    End Sub
    Private Shared Sub ReloadInstanceList(Force As Boolean)
        LoaderFolderRun(McInstanceListLoader, McFolderSelected, If(Force, LoaderFolderRunType.ForceRun, LoaderFolderRunType.RunOnUpdated), MaxDepth:=1, ExtraPath:="versions\")
    End Sub
    Private Shared Function LogoSettingsPort() As LegacyMainLogoSettings
        If FrmSetupUI Is Nothing Then Return Nothing
        Return New LegacyMainLogoSettings With {
            .CheckLeft = FrmSetupUI.CheckLogoLeft, .Text = FrmSetupUI.PanLogoText, .Change = FrmSetupUI.PanLogoChange,
            .ForceResize = Sub() FrmSetupUI.CardLogo.TriggerForceResize()
        }
    End Function
    Private Shared Sub EnsureLeftPage(Page As Integer)
        Select Case CType(Page, PageType)
            Case PageType.Download : If FrmDownloadLeft Is Nothing Then FrmDownloadLeft = New PageDownloadLeft
            Case PageType.Setup : If FrmSetupLeft Is Nothing Then FrmSetupLeft = New PageSetupLeft
            Case PageType.Other : If FrmOtherLeft Is Nothing Then FrmOtherLeft = New PageOtherLeft
            Case PageType.InstanceSelect : If FrmSelectLeft Is Nothing Then FrmSelectLeft = New PageSelectLeft
            Case PageType.TaskManager : If FrmSpeedLeft Is Nothing Then FrmSpeedLeft = New PageSpeedLeft
            Case PageType.InstanceSetup : If FrmInstanceLeft Is Nothing Then FrmInstanceLeft = New PageInstanceLeft
        End Select
    End Sub
    Private Shared Sub EnsureRightPage(Page As Integer)
        Select Case CType(Page, PageType)
            Case PageType.InstanceSelect : If FrmSelectRight Is Nothing Then FrmSelectRight = New PageSelectRight
            Case PageType.TaskManager : If FrmSpeedRight Is Nothing Then FrmSpeedRight = New PageSpeedRight
            Case PageType.ResourceDetail : If FrmDownloadResourceDetail Is Nothing Then FrmDownloadResourceDetail = New PageDownloadResourceDetail
#If LEGACY_LINK Then
            Case PageType.Link : If FrmLinkMain Is Nothing Then FrmLinkMain = New PageLinkMain
#End If
        End Select
    End Sub
    Private Shared Function LeftPagePort(Page As Integer) As LegacyMainLeftPage
        Select Case CType(Page, PageType)
            Case PageType.Launch : Return New LegacyMainLeftPage With {.Element = FrmLaunchLeft}
            Case PageType.Download
                Return New LegacyMainLeftPage With {.Element = FrmDownloadLeft, .PageId = Function() CInt(FrmDownloadLeft.PageID),
                    .PageGet = Function(SubType) FrmDownloadLeft.PageGet(CType(SubType, PageSubType)), .Item = Function(SubType) DirectCast(FrmDownloadLeft.PanItem.Children(SubType), MyListItem)}
            Case PageType.Setup
                Return New LegacyMainLeftPage With {.Element = FrmSetupLeft, .PageId = Function() CInt(FrmSetupLeft.PageID),
                    .PageGet = Function(SubType) FrmSetupLeft.PageGet(CType(SubType, PageSubType)), .Item = Function(SubType) DirectCast(FrmSetupLeft.PanItem.Children(SubType), MyListItem)}
            Case PageType.Other
                Return New LegacyMainLeftPage With {.Element = FrmOtherLeft, .PageId = Function() CInt(FrmOtherLeft.PageID),
                    .PageGet = Function(SubType) FrmOtherLeft.PageGet(CType(SubType, PageSubType)), .Item = Function(SubType) DirectCast(FrmOtherLeft.PanItem.Children(SubType), MyListItem)}
            Case PageType.InstanceSelect : Return New LegacyMainLeftPage With {.Element = FrmSelectLeft}
            Case PageType.TaskManager : Return New LegacyMainLeftPage With {.Element = FrmSpeedLeft}
            Case PageType.InstanceSetup
                Return New LegacyMainLeftPage With {.Element = FrmInstanceLeft, .PageId = Function() CInt(FrmInstanceLeft.PageID),
                    .SyncSelection = Sub(SubType) FrmInstanceLeft.SyncSelection(CType(SubType, PageSubType)),
                    .PageGet = Function(SubType) FrmInstanceLeft.PageGet(CType(SubType, PageSubType)), .Item = Function(SubType) FrmInstanceLeft.NavigationItem(CType(SubType, PageSubType))}
            Case Else : Throw New InvalidOperationException("未知主窗左页：" & Page)
        End Select
    End Function
    Private Shared Function RightPageElement(Page As Integer) As MyPageRightView
        Select Case CType(Page, PageType)
            Case PageType.Launch : Return FrmLaunchRight
            Case PageType.InstanceSelect : Return FrmSelectRight
            Case PageType.TaskManager : Return FrmSpeedRight
            Case PageType.ResourceDetail : Return FrmDownloadResourceDetail
#If LEGACY_LINK Then
            Case PageType.Link : Return FrmLinkMain
#End If
            Case Else : Throw New InvalidOperationException("未知主窗右页：" & Page)
        End Select
    End Function
    Private Shared Function CrashPort() As LegacyMainCrashAnalyzer
        Dim Analyzer As New CrashAnalyzer
        Return New LegacyMainCrashAnalyzer With {
            .Import = Sub(Value) Analyzer.Import(Value), .Prepare = Function() Analyzer.Prepare(), .Analyze = Sub() Analyzer.Analyze(),
            .Output = Sub(Manual, Files) Analyzer.Output(Manual, Files)
        }
    End Function
    Private Shared Function CreateDragPort() As LegacyMainDragHelperPort
        Dim Helper As New DragHelper
        Return New LegacyMainDragHelperPort With {
            .SetSource = Sub(Value) Helper.HwndIntPtrSource = Value, .AddHook = Sub() Helper.AddHook(), .RemoveHook = Sub() Helper.RemoveDragHook(),
            .Files = Function() Helper.DropFilePaths, .ConnectDrop = Sub(Work) ConnectDrag(Helper, Work)
        }
    End Function
    Private Shared Sub ConnectDrag(Helper As DragHelper, Work As Action)
        AddHandler Helper.DragDrop, Sub(sender, e) Work()
    End Sub
    Private Function FirstDialogPort() As LegacyMainDialog
        Dim Row = PanMsg.Children(0)
        If TypeOf Row Is MyMsgText Then
            Dim Typed = DirectCast(Row, MyMsgText)
            Return New LegacyMainDialog With {.First = Sub() Typed.Btn1_Click(), .Second = Sub() Typed.Btn2_Click(), .Third = Sub() Typed.Btn3_Click(),
                .SecondVisibility = Function() Typed.Btn2.Visibility, .ThirdVisibility = Function() Typed.Btn3.Visibility}
        ElseIf TypeOf Row Is MyMsgSelect Then
            Dim Typed = DirectCast(Row, MyMsgSelect)
            Return New LegacyMainDialog With {.IsInputOrSelect = True, .First = Sub() Typed.Btn1_Click(), .Second = Sub() Typed.Btn2_Click(),
                .SecondVisibility = Function() Typed.Btn2.Visibility}
        ElseIf TypeOf Row Is MyMsgInput Then
            Dim Typed = DirectCast(Row, MyMsgInput)
            Return New LegacyMainDialog With {.IsInputOrSelect = True, .First = Sub() Typed.Btn1_Click(), .Second = Sub() Typed.Btn2_Click(),
                .SecondVisibility = Function() Typed.Btn2.Visibility}
        ElseIf TypeOf Row Is MyMsgLogin Then
            Dim Typed = DirectCast(Row, MyMsgLogin)
            Return New LegacyMainDialog With {.First = Sub() Typed.Btn1_Click(), .Third = Sub() Typed.Btn3_Click(),
                .Second = Sub() Microsoft.VisualBasic.CompilerServices.NewLateBinding.LateCall(Row, Nothing, "Btn2_Click", New Object() {}, Nothing, Nothing, Nothing, True),
                .SecondVisibility = Function() Typed.Btn2.Visibility, .ThirdVisibility = Function() Typed.Btn3.Visibility}
        Else
            Throw New InvalidCastException("未知主窗弹窗类型：" & Row.GetType().FullName)
        End If
    End Function

End Class
