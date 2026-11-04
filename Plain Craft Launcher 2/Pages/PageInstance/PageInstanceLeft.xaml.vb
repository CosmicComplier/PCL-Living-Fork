Imports NavigationController = PCLCS.InstanceNavigationController(Of PCL.ModMinecraft.McInstance, PCL.FormMain.PageSubType, PCL.MyListItem, PCL.MyIconButton, PCL.MyPageRight, PCL.PageInstanceResources)
Imports NavigationHost = PCLCS.InstanceNavigationHost(Of PCL.ModMinecraft.McInstance, PCL.FormMain.PageSubType, PCL.MyListItem, PCL.MyIconButton, PCL.MyPageRight, PCL.PageInstanceResources)

Public Class PageInstanceLeft
    Implements IRefreshable
    Public Shared Instance As McInstance = Nothing
    Public PageID As FormMain.PageSubType = FormMain.PageSubType.Default
    Private ReadOnly ResourcePages As New Dictionary(Of FormMain.PageSubType, PageInstanceResources)
    Private KeybindingsPage As PageInstanceKeybindings
    Private RestorePointsPage As PageInstanceRestorePoints
    Private ClonePage As PageInstanceClone
    Private ReadOnly NavigationView As New PCLCS.InstanceNavigationView(Of MyListItem)
    Private PageController As NavigationController
    Private ReadOnly Property Controller As NavigationController
        Get
            NavigationView.Items = PanItem
            NavigationView.Overall = ItemOverall
            NavigationView.Setup = ItemSetup
            NavigationView.Modify = ItemModify
            NavigationView.Export = ItemExport
            NavigationView.Mod = ItemMod
            NavigationView.ModDisabled = ItemModDisabled
            NavigationView.Keybindings = ItemKeybindings
            NavigationView.RestorePoints = ItemRestorePoints
            NavigationView.Clone = ItemClone
            NavigationView.Resources = {ItemSaves, ItemScreenshots, ItemResourcePacks, ItemShaderPacks, ItemSchematics}
            NavigationView.Refreshables = {ItemSaves, ItemScreenshots, ItemResourcePacks, ItemShaderPacks, ItemSchematics, ItemKeybindings, ItemRestorePoints, ItemClone}
            If PageController Is Nothing Then PageController = New NavigationController(NavigationView, CreateHost())
            Return PageController
        End Get
    End Property
    Private Function CreateHost() As NavigationHost
        Return New NavigationHost With {
            .Instance = Function() Instance,
            .Locked = Function(Target) McInstanceLock.IsLocked(Target),
            .Modable = Function(Target) Target.Modable,
            .PageId = Function() PageID, .SetPageId = Sub(Value) PageID = Value,
            .CurrentSubPage = Function() FrmMain.PageCurrentSub,
            .Normalize = AddressOf ModModpackMode.NormalizeInstancePage,
            .ReadPage = New Dictionary(Of Integer, Func(Of MyPageRight)) From {
                {0, Function() FrmInstanceOverall}, {1, Function() FrmInstanceSetup}, {2, Function() FrmDownloadModify},
                {3, Function() FrmInstanceMod}, {4, Function() FrmInstanceModDisabled}, {5, Function() FrmInstanceExport},
                {11, Function() KeybindingsPage}, {12, Function() RestorePointsPage}, {14, Function() ClonePage}},
            .WritePage = New Dictionary(Of Integer, Action(Of MyPageRight)) From {
                {0, Sub(Value) FrmInstanceOverall = DirectCast(Value, PageInstanceOverall)}, {1, Sub(Value) FrmInstanceSetup = DirectCast(Value, PageInstanceSetup)},
                {2, Sub(Value) FrmDownloadModify = DirectCast(Value, PageDownloadModify)}, {3, Sub(Value) FrmInstanceMod = DirectCast(Value, PageInstanceMod)},
                {4, Sub(Value) FrmInstanceModDisabled = DirectCast(Value, PageInstanceModDisabled)}, {5, Sub(Value) FrmInstanceExport = DirectCast(Value, PageInstanceExport)},
                {11, Sub(Value) KeybindingsPage = DirectCast(Value, PageInstanceKeybindings)}, {12, Sub(Value) RestorePointsPage = DirectCast(Value, PageInstanceRestorePoints)},
                {14, Sub(Value) ClonePage = DirectCast(Value, PageInstanceClone)}},
            .CreatePage = New Dictionary(Of Integer, Func(Of MyPageRight)) From {
                {0, Function() New PageInstanceOverall}, {1, Function() New PageInstanceSetup}, {2, Function() New PageDownloadModify},
                {3, Function() New PageInstanceMod}, {4, Function() New PageInstanceModDisabled}, {5, Function() New PageInstanceExport},
                {11, Function() New PageInstanceKeybindings}, {12, Function() New PageInstanceRestorePoints}, {14, Function() New PageInstanceClone}},
            .ResourcePages = ResourcePages,
            .CreateResource = Function(Kind) New PageInstanceResources(CType(Kind, InstanceResourceKind)),
            .RefreshSpecial = New Dictionary(Of Integer, Action(Of MyPageRight)) From {
                {6, Sub(Page) DirectCast(Page, PageInstanceResources).RefreshResources()}, {7, Sub(Page) DirectCast(Page, PageInstanceResources).RefreshResources()},
                {8, Sub(Page) DirectCast(Page, PageInstanceResources).RefreshResources()}, {9, Sub(Page) DirectCast(Page, PageInstanceResources).RefreshResources()},
                {10, Sub(Page) DirectCast(Page, PageInstanceResources).RefreshResources()}, {11, Sub(Page) DirectCast(Page, PageInstanceKeybindings).RefreshBindings()},
                {12, Sub(Page) DirectCast(Page, PageInstanceRestorePoints).Refresh()}, {14, Sub(Page) DirectCast(Page, PageInstanceClone).Refresh()}},
            .ResetSetup = Sub(Page) DirectCast(Page, PageInstanceSetup).Reset(),
            .RefreshMods = Sub() PageInstanceMod.Refresh(),
            .RefreshExport = Sub() FrmInstanceExport?.RefreshAll(),
            .RestartModify = Sub() FrmDownloadModify?.PageLoaderRestart(),
            .HintRed = Sub(Message) Hint(Message, HintType.Red),
            .Confirm = Function(Caption, Title, Button2, Warn) MyMsgBox(Caption, Title,, Button2, IsWarn:=Warn),
            .Error = Sub(ErrorValue, Message) Logger.Error(ErrorValue, Message),
            .AdjustAnimation = Sub(Value) AniControlEnabled += Value,
            .Transitions = CreateTransitions(),
            .HiddenRefresh = AddressOf PageSetupUI.HiddenRefresh,
            .BindBeforeMinecraftChange = Sub() InstanceClonerUi.BeforeMinecraftChangeHandler = Sub(Target, Previous, NextVersion, Token) WorldBackups.BeforeMinecraftChange(Target, Previous, NextVersion, Token),
            .Logo = Function(Item) Item.Logo, .SetLogo = Sub(Item, Value) Item.Logo = Value,
            .LogoScale = Function(Item) Item.LogoScale, .SetLogoScale = Sub(Item, Value) Item.LogoScale = Value,
            .Buttons = Function(Item) Item.Buttons, .SetButtons = Sub(Item, Value) Item.Buttons = Value,
            .CreateButton = Function() New MyIconButton,
            .ButtonLogo = Function(Button) Button.Logo, .SetButtonLogo = Sub(Button, Value) Button.Logo = Value,
            .SetButtonScale = Sub(Button, Value) Button.LogoScale = Value,
            .OnClick = Sub(Button, Work) AddHandler Button.Click, Sub() Work(),
            .SetChecked = Sub(Item, Value) Item.Checked = Value,
            .RestoreLogo = Function() Logo.IconButtonRefresh,
            .ModpackEnabled = Function() ModModpackMode.Enabled}
    End Function
    Private Shared Function CreateTransitions() As PCLCS.InstanceNavigationTransitionHost(Of MyPageRight)
        Return New PCLCS.InstanceNavigationTransitionHost(Of MyPageRight) With {
            .StopAnimation = Sub(Name) AniStop(Name),
            .PageRight = Function() FrmMain.PageRight, .SetPageRight = Sub(Value) FrmMain.PageRight = Value,
            .PanelChild = Function() CType(FrmMain.PanMainRight.Child, MyPageRight),
            .SetPanelChild = Sub(Value) FrmMain.PanMainRight.Child = Value,
            .PageExit = Sub(Page) Page.PageOnExit(), .PageForceExit = Sub(Page) Page.PageOnForceExit(), .PageEnter = Sub(Page) Page.PageOnEnter(),
            .AnimatePage = Sub(First, FirstDelay, Second, SecondDelay, After, Name) AniStart({AaCode(Sub() First(), FirstDelay), AaCode(Sub() Second(), SecondDelay, After)}, Name)}
    End Function
    Private Sub PageInstanceLeft_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Controller.Loaded()
    End Sub
    Private Sub ResourceButtonsInit() Handles Me.Initialized
        Controller.InitializeButtons()
    End Sub
    Public Sub RefreshModDisabled() Handles Me.Loaded
        Controller.RefreshModDisabled()
    End Sub
    Private Sub PageCheck(sender As FrameworkElement, e As RouteEventArgs) Handles ItemOverall.Check, ItemMod.Check, ItemModDisabled.Check, ItemSetup.Check, ItemModify.Check, ItemExport.Check, ItemSaves.Check, ItemScreenshots.Check, ItemResourcePacks.Check, ItemShaderPacks.Check, ItemSchematics.Check, ItemKeybindings.Check, ItemRestorePoints.Check, ItemClone.Check
        Controller.Check(sender)
    End Sub
    Friend Function NavigationItem(ID As FormMain.PageSubType) As MyListItem
        Return Controller.NavigationItem(ID)
    End Function
    Public Function PageGet(Optional ID As FormMain.PageSubType = -1)
        Return Controller.PageGet(ID)
    End Function
    Public Sub PageChange(ID As FormMain.PageSubType)
        Controller.PageChange(ID)
    End Sub
    Private Shared Sub PageChangeRun(Target As MyPageRight)
        PCLCS.InstanceNavigationTransitions.Run(Target, CreateTransitions())
    End Sub
    Public Sub Refresh_Click(sender As Object, e As EventArgs)
        Controller.RefreshTag(sender.Tag)
    End Sub
    Public Sub Refresh() Implements IRefreshable.Refresh
        Controller.Refresh()
    End Sub
    Public Sub Refresh(SubType As FormMain.PageSubType)
        Controller.Refresh(SubType)
    End Sub
    Public Sub Reset(sender As Object, e As EventArgs)
        Controller.Reset()
    End Sub
    Public Shared CurrentJavaWorker As New RedoableWorker(Of Java)(Function(c, p) SelectOrDownloadJava(PageInstanceLeft.Instance, False, c, p))
    Public Shared Sub ReloadCurrentJava() Handles Me.Loaded
        PCLCS.InstanceNavigationRules.ReloadJava(Function() AniControlEnabled, Function() Instance, CurrentJavaWorker)
    End Sub
End Class
