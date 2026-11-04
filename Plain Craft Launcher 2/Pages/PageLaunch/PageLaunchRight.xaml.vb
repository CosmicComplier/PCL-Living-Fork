Imports System.Windows.Threading
Imports PCLLF.Presentation.Launch
Imports RecentWorldController = PCLCS.RecentWorldPageController(Of PCL.ModMinecraft.McInstance, PCL.RecentWorld, PCL.ModAnimation.AniData)
Imports RecentWorldHost = PCLCS.RecentWorldPageHost(Of PCL.ModMinecraft.McInstance, PCL.RecentWorld)
Imports RecentWorldAnimations = PCLCS.RecentWorldAnimationHost(Of PCL.ModAnimation.AniData)

Public Class PageLaunchRight
    Implements IRefreshable, IDispatcherUnhandledException
    Private RecentController As RecentWorldController
    Private ReadOnly Native As LegacyLaunchRight(Of McInstance, RecentWorld, AniData)
    '保留真实下载任务与输入身份；读取/版本/缓存/内容处理在 C# 中。
    Private OnlineLoader As New LoaderTask(Of (Address As String, ShouldRefresh As Boolean), Integer)("下载主页", AddressOf OnlineLoaderSub) With {.ReloadTimeout = 10 * 60 * 1000}
    Public Sub New()
        Native = New LegacyLaunchRight(Of McInstance, RecentWorld, AniData)(CreateView(), CreateHost(), LaunchPageRuntime.Animations(), Function() RecentWorldService)
        InitializeComponent()
    End Sub
    Private Function CreateView() As LegacyLaunchRightView
        Return New LegacyLaunchRightView With {
            .Root = Function() Me, .RecentCard = Function() CardRecentWorlds, .DependencyCard = Function() CardDependencyChecks,
            .DependencyAnchor = Function() PanDependencyOptionsAnchor, .AutoFillCheck = Function() CheckAutoFillDependencies,
            .LogPanel = Function() PanLog, .DependencyPanel = Function() PanDependencyChecks, .DependencyTransform = Function() TransDependencyChecks,
            .CustomPanel = Function() PanCustom, .DependencyCheck = Function() CheckModDependencies.Checked, .PageId = Function() PageUuid.ToString(),
            .ScrollToHome = Sub() PanBack.ScrollToHome(), .ResizeDependencyCard = Sub() CardDependencyChecks.TriggerForceResize()}
    End Function
    Private Function CreateHost() As LegacyLaunchRightHost
        Return New LegacyLaunchRightHost With {
            .ReadString = Function(Key) Settings.Get(Of String)(Key), .ReadInteger = Function(Key) Settings.Get(Of Integer)(Key), .WriteSetting = Sub(Key, Value) Settings.Set(Key, Value),
            .Debug = Function() ModeDebug, .IsLaunchPage = Function() FrmMain.PageCurrent.Page = FormMain.PageType.Launch,
            .BasePath = Function() Paths.Base, .TempPath = Function() PathTemp,
            .RunInThread = Sub(Work) RunInThread(Work), .Dispatch = Sub(Work) RunInUi(Work), .DispatchWait = Sub(Work) RunInUiWait(Work),
            .DispatchWhenLoaded = Sub(Work) RunInUi(Work, True), .ChangeAnimationControl = Sub(Delta) AniControlEnabled += Delta,
            .DisablePageAnimation = Sub(Control) DisabledPageAnimControls.Add(Control), .RefreshSettings = Sub(Control) SettingService.RefreshSettings(Control),
            .Hint = Sub(Text) Hint(Text), .ChangeToLaunchPage = Sub() FrmMain.PageChange(FormMain.PageType.Launch),
            .HtmlReplace = Function(Content) ArgumentReplace(Content, AddressOf WebUtility.HtmlEncode), .XmlReplace = Function(Content) ArgumentReplace(Content, AddressOf StringUtils.XmlEscape),
            .Request = Function(Address) NetRequestByClientRetry(Address), .ParseXaml = Function(Content) CType(GetObjectFromXML(Content), UIElement),
            .RetryMessage = Function(Text, Title) MyMsgBox(Text, Title, "重试", "取消"),
            .StartOnline = Sub(Input) OnlineLoader.Start(Input), .ResetOnlineInput = Sub(Input) OnlineLoader.Input = Input}
    End Function
    Private ReadOnly Property RecentWorldService As RecentWorldController
        Get
            If RecentController Is Nothing Then
                Dim View As New PCLCS.RecentWorldPageView With {
                    .Root = Me, .Card = CardRecentWorlds, .Item = ItemRecentWorld, .Transform = TransRecentWorld,
                    .PageId = Function() PageUuid.ToString(), .CardTitle = Function() CardRecentWorlds.MainTextBlock,
                    .SetTitle = Sub(Value) ItemRecentWorld.Title = Value, .SetInfo = Sub(Value) ItemRecentWorld.Info = Value}
                RecentController = New RecentWorldController(View, CreateRecentWorldHost(), CreateRecentWorldAnimations())
            End If
            Return RecentController
        End Get
    End Property
    Private Function CreateRecentWorldHost() As RecentWorldHost
        Return New RecentWorldHost With {
            .SelectedInstance = Function() McInstanceSelected,
            .RecoverExitedSessions = AddressOf QuickPlayWorlds.RecoverExitedSessions,
            .DiscoverExistingWorlds = AddressOf QuickPlayWorlds.DiscoverExistingWorlds,
            .HasRunningSessions = AddressOf QuickPlayWorlds.HasRunningSessions,
            .Recent = AddressOf QuickPlayWorlds.Recent,
            .WorldPath = AddressOf QuickPlayWorlds.WorldPath,
            .LoadImage = Function(FileName, Width) DirectCast(InstanceResourceStore.LoadImage(FileName, Width), ImageSource),
            .GrassImage = Function() CType(New MyBitmap(PathImage & "Blocks/Grass.png"), ImageSource),
            .Background = Sub(Work, Name) RunInNewThread(Work, Name),
            .Dispatch = Sub(Work) RunInUi(Work),
            .Warn = Sub(ErrorValue, Message) Logger.Warn(ErrorValue, Message),
            .Error = Sub(ErrorValue, Message) Logger.Error(ErrorValue, Message, LogBehavior.Toast),
            .HasRunningGame = Function() HasRunningMinecraft,
            .LaunchLoading = Function() McLaunchLoader.State = LoadState.Loading,
            .AnimationEnabled = Function() AniControlEnabled = 0,
            .Enabled = Function() Settings.Get(Of Boolean)("LaunchRecentWorlds"),
            .ApplyDependencyOptionsState = AddressOf ApplyDependencyOptionsState,
            .CreateInstance = Function(VersionPath) New McInstance(VersionPath),
            .GameFolder = Function(Value) Value.PathIndie,
            .SamePath = Function(Left, Right) PathUtils.IsSame(Left, Right),
            .StartWorld = AddressOf QuickPlayWorlds.Start,
            .HintError = Sub(Message) Hint(Message, HintType.Red)}
    End Function
    Private Shared Function CreateRecentWorldAnimations() As RecentWorldAnimations
        Return New RecentWorldAnimations With {
            .Opacity = Function(Target, Value, Time) AaOpacity(Target, Value, Time),
            .TranslateX = AddressOf RecentWorldTranslateX,
            .Code = Function(Work, Delay, After) AaCode(Sub() Work(), Delay, After),
            .Start = Sub(Animations, Name) AniStart(Animations, Name),
            .Stop = Sub(Name) AniStop(Name), .IsRunning = AddressOf AniIsRun}
    End Function
    Private Shared Function RecentWorldTranslateX(Target As FrameworkElement, Value As Double, Time As Integer, Ease As PCLCS.RecentWorldSlideEase) As AniData
        If Ease = PCLCS.RecentWorldSlideEase.OutBackWeak Then Return AaTranslateX(Target, Value, Time, Ease:=New AniEaseOutBack(AniEasePower.Weak))
        Return AaTranslateX(Target, Value, Time, Ease:=New AniEaseInFluent(AniEasePower.Weak))
    End Function

    Private Sub InitRecentWorldCard() Handles Me.Initialized
        Native.InitializeRecentCard()
    End Sub
    Private Sub RecentPageEntered() Handles Me.PageEnter
        Native.PageEntered()
    End Sub
    Private Sub Init() Handles Me.Loaded
        Native.Loaded()
    End Sub
    Friend Sub RefreshRecentWorlds()
        Native.RefreshRecentWorlds()
    End Sub
    Friend Sub ShowRecentWorld(Item As Tuple(Of RecentWorld, ImageSource))
        Native.ShowRecentWorld(Item)
    End Sub
    Friend Sub UpdateRecentWorldVisibility()
        Native.UpdateRecentWorldVisibility()
    End Sub
    Friend Sub ApplyRecentWorldState(GameActive As Boolean, Animate As Boolean)
        Native.ApplyRecentWorldState(GameActive, Animate)
    End Sub
    Friend Sub RefreshDependencyOptions()
        Native.RefreshDependencyOptions()
    End Sub
    Private Sub DependencyCheckChanged(Sender As Object, User As Boolean) Handles CheckModDependencies.Change
        Native.DependencyChanged()
    End Sub
    Friend Sub ApplyDependencyOptionsState(GameActive As Boolean, Animate As Boolean, RecentVisible As Boolean)
        Native.ApplyDependencyOptionsState(GameActive, Animate, RecentVisible)
    End Sub
    Private Sub RecentWorldClick(Sender As Object, e As MouseButtonEventArgs) Handles ItemRecentWorld.Click
        Native.RecentWorldClick(Sender, e)
    End Sub
    Friend Sub BeginRecentWorldLaunch(Start As Func(Of Boolean), Optional Animate As Boolean = True)
        Native.BeginRecentWorldLaunch(Start, Animate)
    End Sub
    Private Sub ReleaseRecentWorlds() Handles Me.Unloaded
        Native.Unloaded()
    End Sub
    Private Sub Refresh() Handles Me.Loaded
        Native.Refresh()
    End Sub
    Private Sub OnlineLoaderSub(Task As LoaderTask(Of (Address As String, ShouldRefresh As Boolean), Integer))
        Native.OnlineLoad(Function() Task.Input)
    End Sub
    Public Sub ForceRefresh() Implements IRefreshable.Refresh
        Native.ForceRefresh()
    End Sub
    Private Sub DispatcherUnhandledException(sender As Object, e As DispatcherUnhandledExceptionEventArgs) Implements IDispatcherUnhandledException.DispatcherUnhandledException
        Native.DispatcherUnhandledException(e)
    End Sub
End Class
