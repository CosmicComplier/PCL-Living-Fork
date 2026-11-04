Imports CloneSelection = PCLCS.InstanceCloneSelectionPresenter(Of PCL.ModMinecraft.McInstance, PCL.ModMinecraft.McInstanceCardType, PCL.InstanceCloneGroupNode, PCL.InstanceCloneGroupSet)
Imports CloneSelectionHost = PCLCS.InstanceCloneSelectionHost(Of PCL.ModMinecraft.McInstance, PCL.ModMinecraft.McInstanceCardType, PCL.InstanceCloneGroupNode)
Imports SelectionCore = PCLCS.InstanceSelectionPageController(Of PCL.ModMinecraft.McInstance, PCL.ModMinecraft.McInstance.McInstanceState, PCL.ModMinecraft.McInstanceCardType, PCL.InstanceCloneGroupNode, PCL.InstanceCloneGroupSet)

Public Class PageSelectRight
    Private Shared ReadOnly ClonePresenter As New CloneSelection(InstanceCloneGroups.Service, CreateCloneHost())
    Public ShowHidden As Boolean = False
    Private Controller As SelectionCore
    Private ReadOnly Property Service As SelectionCore
        Get
            If Controller Is Nothing Then Controller = New SelectionCore(CreateHost(Me), ClonePresenter)
            Return Controller
        End Get
    End Property
    Private Shared Function CreateHost(Owner As PageSelectRight) As PCLCS.InstanceSelectionPageHost(Of McInstance, McInstanceCardType)
        Return New PCLCS.InstanceSelectionPageHost(Of McInstance, McInstanceCardType) With {
            .View = Function() Owner.CreateView(), .ShowHidden = Function() Owner.ShowHidden,
            .ModpackEnabled = Function() ModModpackMode.Enabled, .HiddenDownload = Function() Settings.Get(Of Boolean)("UiHiddenPageDownload"),
            .HiddenForceShow = Function() PageSetupUI.HiddenForceShow, .LoaderFailed = Function() McInstanceListLoader.State = LoadState.Failed,
            .Shift = Function() My.Computer.Keyboard.ShiftKeyDown,
            .InitializeLoader = Sub(Finished) Owner.PageLoaderInit(Owner.Load, Owner.PanLoad, Owner.PanAllBack, Nothing, McInstanceListLoader, Sub() Finished(), AutoRun:=False),
            .RefreshInstances = AddressOf RefreshNativeInstances,
            .Instances = Function() McInstanceList, .SetInstances = Sub(Value) McInstanceList = Value,
            .SelectedInstance = Function() McInstanceSelected, .SetSelectedInstance = Sub(Value) McInstanceSelected = Value,
            .InstancePage = Function() PageInstanceLeft.Instance, .SetInstancePage = Sub(Value) PageInstanceLeft.Instance = Value,
            .MinecraftFolder = Function() McFolderSelected, .NewInstance = Function(Path) New McInstance(Path),
            .Locked = Function(Instance) McInstanceLock.IsLocked(Instance), .AllowsInstance = AddressOf ModModpackMode.AllowsInstance,
            .ForceRefresh = Sub() McInstanceListForceRefresh = True, .PageBack = Sub() FrmMain.PageBack(),
            .OpenDownload = Sub() FrmMain.PageChange(FormMain.PageType.Download, FormMain.PageSubType.DownloadInstall),
            .OpenInstanceFolder = AddressOf PageInstanceOverall.OpenInstanceFolder,
            .OpenInstanceSetup = Sub(Instance)
                                     PageInstanceLeft.Instance = Instance
                                     FrmMain.PageChange(FormMain.PageType.InstanceSetup, 0)
                                 End Sub,
            .Hint = AddressOf NativeHint,
            .Message = Function(Request) MyMsgBox(Request.Text, Request.Title, Request.Button1, Request.Button2, Request.Button3, Request.IsWarn),
            .Button = AddressOf CreateNativeButton,
            .SetButtons = Sub(Row, Buttons) DirectCast(Row, MyListItem).Buttons = Buttons.Cast(Of MyIconButton).ToArray(),
            .BindClick = Sub(Row, Click) AddHandler DirectCast(Row, MyListItem).Click, Sub(Sender, Args) Click(Sender, Args),
            .BindRightClick = Sub(Row, Click) AddHandler Row.MouseRightButtonUp, Sub() Click(),
            .WriteIni = Sub(File, Key, Value) WriteIni(File, Key, Value)}
    End Function
    Private Function CreateView() As PCLCS.InstanceSelectionPageView
        Return New PCLCS.InstanceSelectionPageView With {.Main = PanMain, .Back = PanBack, .Empty = PanEmpty, .Download = BtnEmptyDownload,
            .EmptyTitle = LabEmptyTitle, .EmptyContent = LabEmptyContent, .ScrollHome = Sub() PanBack.ScrollToHome(), .ForceResize = Sub() PanLoad.TriggerForceResize()}
    End Function
    Private Shared Sub RefreshNativeInstances(Force As Boolean)
        LoaderFolderRun(McInstanceListLoader, McFolderSelected, If(Force, LoaderFolderRunType.ForceRun, LoaderFolderRunType.RunOnUpdated), MaxDepth:=1, ExtraPath:="versions\")
    End Sub
    Private Shared Sub NativeHint(Text As String, Kind As PCLCS.SelectionPageHint)
        Select Case Kind
            Case PCLCS.SelectionPageHint.Red : Hint(Text, HintType.Red)
            Case PCLCS.SelectionPageHint.Blue : Hint(Text, HintType.Blue)
            Case Else : Hint(Text, HintType.Green)
        End Select
    End Sub
    Private Shared Function CreateNativeButton(Kind As String, ToolTip As String, Scale As Double, Visibility As Visibility, Click As Action) As FrameworkElement
        Dim Button As New MyIconButton With {.LogoScale = Scale, .Visibility = Visibility, .ToolTip = ToolTip}
        Select Case Kind
            Case "like-fill" : Button.Logo = Logo.IconButtonLikeFill
            Case "like-line" : Button.Logo = Logo.IconButtonLikeLine
            Case "delete" : Button.Logo = Logo.IconButtonDelete
            Case "setup" : Button.Logo = Logo.IconButtonSetup
            Case "open" : Button.Logo = Logo.IconButtonOpen
            Case Else : Throw New ArgumentOutOfRangeException(NameOf(Kind))
        End Select
        AddHandler Button.Click, Sub() Click()
        Return Button
    End Function
    Private Sub PageSelectRight_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Service.Loaded()
    End Sub
    Private Sub LoaderInit() Handles Me.Initialized
        Service.Initialize()
    End Sub
    Private Sub Load_Click(sender As Object, e As MouseButtonEventArgs) Handles Load.Click
        Service.LoadClick()
    End Sub
    Private Shared Function CreateCloneHost() As CloneSelectionHost
        Return New CloneSelectionHost With {
            .HiddenCategory = McInstanceCardType.Hidden, .StarCategory = McInstanceCardType.Star,
            .IsCollapsedCategory = Function(Kind) Kind = McInstanceCardType.Rubbish OrElse Kind = McInstanceCardType.Error OrElse Kind = McInstanceCardType.Fool,
            .IsLocked = Function(Instance) McInstanceLock.IsLocked(Instance),
            .AllowsInstance = Function(Instance) ModModpackMode.AllowsInstance(Instance.PathVersion),
            .VersionPath = Function(Instance) Instance.PathVersion, .Name = Function(Instance) Instance.Name,
            .Info = Function(Instance) Instance.Info, .SelectedPath = Function() McInstanceSelected?.PathVersion,
            .CreateRow = Function(Instance, Initialized) Instance.ToListItem(Sub(Item, Args) Initialized(Item, Args)),
            .InitializeRow = Sub(Item, Args) McInstanceListContent(DirectCast(Item, MyListItem), Args),
            .SetRowInfo = Sub(Item, Info) DirectCast(Item, MyListItem).Info = Info,
            .CreateCard = Function(Title, Margin) New CloneCard(New MyCard With {.Title = Title, .Margin = Margin}),
            .AdaptCard = Function(Element) New CloneCard(DirectCast(Element, MyCard)), .SwappedHeight = MyCard.SwapedHeight}
    End Function
    Private NotInheritable Class CloneCard
        Implements PCLCS.IInstanceCloneSelectionCard(Of InstanceCloneGroupNode)
        Private ReadOnly Card As MyCard
        Public Sub New(Card As MyCard)
            Me.Card = Card
        End Sub
        Public ReadOnly Property Element As FrameworkElement Implements PCLCS.IInstanceCloneSelectionCard(Of InstanceCloneGroupNode).Element
            Get
                Return Card
            End Get
        End Property
        Public Property IsSwapped As Boolean Implements PCLCS.IInstanceCloneSelectionCard(Of InstanceCloneGroupNode).IsSwapped
            Get
                Return Card.IsSwapped
            End Get
            Set(Value As Boolean)
                Card.IsSwapped = Value
            End Set
        End Property
        Public Sub SetContent(Content As Panel) Implements PCLCS.IInstanceCloneSelectionCard(Of InstanceCloneGroupNode).SetContent
            Card.Children.Add(Content)
            Card.SwapControl = Content
        End Sub
        Public Sub SetContentBuilder(Items As IEnumerable(Of InstanceCloneGroupNode), Builder As Func(Of InstanceCloneGroupNode, FrameworkElement)) Implements PCLCS.IInstanceCloneSelectionCard(Of InstanceCloneGroupNode).SetContentBuilder
            Card.SetContentBuilder(Items, Function(Node) DirectCast(Builder(Node), UIElement))
        End Sub
        Public Sub BuildContent() Implements PCLCS.IInstanceCloneSelectionCard(Of InstanceCloneGroupNode).BuildContent
            Card.BuildContent()
        End Sub
    End Class


    Private Sub McInstanceListUI(Loader As LoaderTask(Of String, Integer))
        Service.Render()
    End Sub
    Friend Shared Function BuildCloneChoice(Node As InstanceCloneGroupNode, InstancesByPath As Dictionary(Of String, McInstance), SelectedPath As String) As FrameworkElement
        Return ClonePresenter.BuildCloneChoice(Node, InstancesByPath, SelectedPath)
    End Function
    Public Shared Sub McInstanceListContent(sender As MyListItem, e As EventArgs)
        SelectionCore.RowContent(sender, CreateHost(Nothing))
    End Sub
    Public Shared Sub Item_Click(sender As MyListItem, e As EventArgs)
        SelectionCore.ItemClick(sender, CreateHost(Nothing))
    End Sub
    Private Sub BtnDownload_Click(sender As Object, e As EventArgs) Handles BtnEmptyDownload.Click
        Service.OpenDownload()
    End Sub
    Public Shared Sub DeleteInstance(Item As MyListItem, Instance As McInstance)
        SelectionCore.Delete(Item, Instance, CreateHost(Nothing))
    End Sub
    Friend Shared Function ResolveDeleteTarget(Item As MyListItem, Expected As McInstance) As McInstance
        Return SelectionCore.ResolveDeleteTarget(Item, Expected)
    End Function
    Friend Shared Function WithoutDeletedInstance(Cards As IDictionary(Of McInstanceCardType, List(Of McInstance)), DeletedFolder As String) As Dictionary(Of McInstanceCardType, List(Of McInstance))
        Return SelectionCore.WithoutDeletedInstance(Cards, DeletedFolder)
    End Function
    Friend Shared Function SelectionAfterDelete(Current As McInstance, DeletedFolder As String) As McInstance
        Return SelectionCore.SelectionAfterDelete(Current, DeletedFolder)
    End Function
    Public Sub BtnEmptyDownload_Loaded() Handles BtnEmptyDownload.Loaded
        Service.EmptyDownloadLoaded()
    End Sub
End Class
