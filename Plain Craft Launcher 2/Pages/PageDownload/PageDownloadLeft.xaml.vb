Imports NavigationCore = PCLCS.DownloadNavigationController(Of PCL.MyPageRight, PCL.FormMain.PageSubType, PCL.ModAnimation.AniData)

Public Class PageDownloadLeft
    Implements IRefreshable
    Public PageID As FormMain.PageSubType = FormMain.PageSubType.DownloadInstall
    Private MapPage As PageDownloadMap
    Private Controller As NavigationCore
    Private ReadOnly Property Service As NavigationCore
        Get
            If Controller Is Nothing Then Controller = New NavigationCore(CreateHost(), CreateAnimations())
            Return Controller
        End Get
    End Property
    Private Function CreateHost() As PCLCS.DownloadNavigationHost(Of MyPageRight, FormMain.PageSubType)
        Dim Host = CreateFrameHost()
        Host.PageId = Function() PageID
        Host.SetPageId = Sub(Value) PageID = Value
        Host.CurrentSub = Function() FrmMain.PageCurrentSub
        Host.CachedPage = AddressOf ReadCached
        Host.StorePage = AddressOf StoreCached
        Host.NewPage = AddressOf NewPage
        Host.ClearProjectCache = Sub() ResourceProject.Cache.Clear()
        Host.ClearVersionCache = Sub() ResourceVersion.ProjectFilesCache.Clear()
        Host.ForceLoader = AddressOf ForceLoader
        Host.ResetStorage = Sub(Page) ResourceContent(Page).Storage = New ResourceSearcher.SearchResult
        Host.SetResultPage = Sub(Page, Value) ResourceContent(Page).Page = Value
        Host.Restart = Sub(Page) Page.PageLoaderRestart()
        Host.CheckItem = Sub(Code, Value) NativeItem(Code).Checked = Value
        Host.InstallChecked = Function() ItemInstall.Checked
        Host.ExitInstallSelection = Sub() FrmDownloadInstall.ExitSelectPage()
        Host.RefreshHint = Sub() Hint("正在刷新……", Log:=False)
        Host.SenderTag = Function(Sender) Sender.Tag
        Host.AddMapRefreshButton = AddressOf AddMapRefreshButton
        Return Host
    End Function
    Private Shared Function CreateFrameHost() As PCLCS.DownloadNavigationHost(Of MyPageRight, FormMain.PageSubType)
        Return New PCLCS.DownloadNavigationHost(Of MyPageRight, FormMain.PageSubType) With {
            .PageRight = Function() FrmMain.PageRight, .VisibleRight = Function() DirectCast(FrmMain.PanMainRight.Child, MyPageRight),
            .SetPageRight = Sub(Page) FrmMain.PageRight = Page, .SetVisibleRight = Sub(Page) FrmMain.PanMainRight.Child = Page,
            .OnExit = Sub(Page) Page.PageOnExit(), .OnForceExit = Sub(Page) Page.PageOnForceExit(), .OnEnter = Sub(Page) Page.PageOnEnter()}
    End Function
    Private Function ReadCached(Code As Integer) As MyPageRight
        Select Case Code
            Case 7 : Return MapPage
            Case 0 : Return FrmDownloadInstall
            Case 2 : Return FrmDownloadMod
            Case 3 : Return FrmDownloadPack
            Case 5 : Return FrmDownloadResourcePack
            Case 6 : Return FrmDownloadShader
            Case 4 : Return FrmDownloadDataPack
            Case Else : Throw New ArgumentOutOfRangeException(NameOf(Code))
        End Select
    End Function
    Private Sub StoreCached(Code As Integer, Page As MyPageRight)
        Select Case Code
            Case 7 : MapPage = DirectCast(Page, PageDownloadMap)
            Case 0 : FrmDownloadInstall = DirectCast(Page, PageDownloadInstall)
            Case 2 : FrmDownloadMod = DirectCast(Page, PageDownloadMod)
            Case 3 : FrmDownloadPack = DirectCast(Page, PageDownloadPack)
            Case 5 : FrmDownloadResourcePack = DirectCast(Page, PageDownloadResourcePack)
            Case 6 : FrmDownloadShader = DirectCast(Page, PageDownloadShader)
            Case 4 : FrmDownloadDataPack = DirectCast(Page, PageDownloadDataPack)
            Case Else : Throw New ArgumentOutOfRangeException(NameOf(Code))
        End Select
    End Sub
    Private Shared Function NewPage(Code As Integer) As MyPageRight
        Select Case Code
            Case 7 : Return New PageDownloadMap
            Case 0 : Return New PageDownloadInstall
            Case 2 : Return New PageDownloadMod
            Case 3 : Return New PageDownloadPack
            Case 5 : Return New PageDownloadResourcePack
            Case 6 : Return New PageDownloadShader
            Case 4 : Return New PageDownloadDataPack
            Case Else : Throw New ArgumentOutOfRangeException(NameOf(Code))
        End Select
    End Function
    Private Function NativeItem(Code As Integer) As MyListItem
        Select Case Code
            Case 7 : Return ItemMap
            Case 0 : Return ItemInstall
            Case 2 : Return ItemMod
            Case 3 : Return ItemPack
            Case 5 : Return ItemResourcePack
            Case 6 : Return ItemShader
            Case 4 : Return ItemDataPack
            Case Else : Throw New ArgumentOutOfRangeException(NameOf(Code))
        End Select
    End Function
    Private Shared Function ResourceContent(Page As MyPageRight) As PageResource
        Select Case True
            Case TypeOf Page Is PageDownloadMap : Return DirectCast(Page, PageDownloadMap).Content
            Case TypeOf Page Is PageDownloadMod : Return DirectCast(Page, PageDownloadMod).Content
            Case TypeOf Page Is PageDownloadPack : Return DirectCast(Page, PageDownloadPack).Content
            Case TypeOf Page Is PageDownloadResourcePack : Return DirectCast(Page, PageDownloadResourcePack).Content
            Case TypeOf Page Is PageDownloadShader : Return DirectCast(Page, PageDownloadShader).Content
            Case TypeOf Page Is PageDownloadDataPack : Return DirectCast(Page, PageDownloadDataPack).Content
            Case Else : Throw New InvalidCastException("下载页面不包含资源搜索视图。")
        End Select
    End Function
    Private Shared Sub ForceLoader(Code As Integer)
        Select Case Code
            Case 0 : DlClientListLoader.Start(IsForceRestart:=True)
            Case 1 : DlOptiFineListLoader.Start(IsForceRestart:=True)
            Case 2 : DlForgeListLoader.Start(IsForceRestart:=True)
            Case 3 : DlNeoForgeListLoader.Start(IsForceRestart:=True)
            Case 4 : DlLiteLoaderListLoader.Start(IsForceRestart:=True)
            Case 5 : DlFabricListLoader.Start(IsForceRestart:=True)
            Case 6 : DlFabricApiLoader.Start(IsForceRestart:=True)
            Case 7 : DlOptiFabricLoader.Start(IsForceRestart:=True)
            Case Else : Throw New ArgumentOutOfRangeException(NameOf(Code))
        End Select
    End Sub
    Private Sub AddMapRefreshButton(Click As Action(Of Object))
        Dim Button As New MyIconButton With {.Tag = 7, .ToolTip = "刷新", .LogoScale = 0.85, .Logo = Logo.IconButtonRefresh}
        AddHandler Button.Click, Sub(Sender, Args) Click(Sender)
        ItemMap.Buttons = {Button}
    End Sub
    Private Sub PageCheck(sender As FrameworkElement, e As RouteEventArgs) Handles ItemInstall.Check, ItemMod.Check, ItemPack.Check, ItemResourcePack.Check, ItemShader.Check, ItemDataPack.Check, ItemMap.Check
        Service.Check(sender)
    End Sub
    Private Sub MapButtonInit() Handles Me.Initialized
        Service.InitializeMapButton()
    End Sub
    Public Function PageGet(Optional ID As FormMain.PageSubType = -1)
        Return Service.Get(ID)
    End Function
    Public Sub PageChange(ID As FormMain.PageSubType)
        Service.Change(ID)
    End Sub
    Private Shared Sub PageChangeRun(Target As MyPageRight)
        NavigationCore.Run(Target, CreateFrameHost(), CreateAnimations())
    End Sub
    Public Sub Refresh_Click(sender As Object, e As EventArgs)
        Service.RefreshClick(sender)
    End Sub
    Public Sub Refresh() Implements IRefreshable.Refresh
        Service.Refresh()
    End Sub
    Public Sub Refresh(SubType As FormMain.PageSubType)
        Service.Refresh(SubType)
    End Sub
    Private Sub ItemInstall_Click(sender As Object, e As MouseButtonEventArgs) Handles ItemInstall.Click
        Service.InstallClick()
    End Sub
    Private Shared Function CreateAnimations() As PCLCS.DownloadPageAnimationHost(Of AniData)
        Return New PCLCS.DownloadPageAnimationHost(Of AniData) With {
            .Disabled = Function() AniControlEnabled, .SetDisabled = Sub(Value) AniControlEnabled = Value,
            .Opacity = Function(Target, Delta, Duration, Delay, Ease) AaOpacity(Target, Delta, Duration, Delay, ReadEase(Ease)),
            .TranslateX = Function(Target, Delta, Duration, Delay, Ease) AaTranslateX(Target, Delta, Duration, Delay, ReadEase(Ease)),
            .TranslateY = Function(Target, Delta, Duration, Delay, Ease) AaTranslateY(Target, Delta, Duration, Delay, ReadEase(Ease)),
            .Scale = Function(Target, Delta, Duration, Delay, Ease, After) AaScaleTransform(Target, Delta, Duration, Delay, ReadEase(Ease), After),
            .Color = Function(Target, [Property], Color, Duration, Delay, Ease) AaColor(Target, [Property], Color, Duration, Delay, ReadEase(Ease)),
            .Code = Function(Work, Delay, After) AaCode(Sub() Work(), Delay, After),
            .Start = Sub(Plan, Name, Refresh) AniStart(Plan.ToList(), Name, Refresh), .Stop = AddressOf AniStop}
    End Function
    Private Shared Function ReadEase(Value As PCLCS.DownloadPageEase) As AniEase
        Select Case Value
            Case PCLCS.DownloadPageEase.OutFluent : Return New AniEaseOutFluent()
            Case PCLCS.DownloadPageEase.OutFluentExtraStrong : Return New AniEaseOutFluent(AniEasePower.ExtraStrong)
            Case Else : Return Nothing
        End Select
    End Function
End Class
