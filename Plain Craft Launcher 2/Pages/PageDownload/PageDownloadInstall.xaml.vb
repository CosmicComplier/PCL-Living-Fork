Imports InstallCore = PCLCS.DownloadInstallPageController(Of PCL.ModDownload.DlOptiFineListEntry, PCL.ModDownload.DlForgelikeEntry, PCL.ModDownload.DlForgeVersionEntry, PCL.ModDownload.DlNeoForgeListEntry, PCL.ModDownload.DlLiteLoaderListEntry, PCL.ResourceVersion, PCL.ModDownloadLib.McInstallRequest, PCL.ModAnimation.AniData)
Imports InstallKind = PCLCS.DownloadInstallComponent

' LF XAML/event/loader/field adapter. Selection, compatibility, rendering and install flow are in InstallCore.
Public Class PageDownloadInstall
    Public IsInSelectPage As Boolean = False
    Public VanillaName As String
    Public VanillaData As JObject
    Public VanillaIcon As String
    Public SelectedOptiFine As DlOptiFineListEntry = Nothing
    Public SelectedLiteLoader As DlLiteLoaderListEntry = Nothing
    Public SelectedForge As DlForgeVersionEntry = Nothing
    Public SelectedNeoForge As DlNeoForgeListEntry = Nothing
    Public SelectedFabric As String = Nothing
    Public SelectedFabricApi As ResourceVersion = Nothing
    Public SelectedOptiFabric As ResourceVersion = Nothing
    Public IsSelectNameEdited As Boolean = False
    Public MinecraftVersions As JArray
    Public Shared VersionWaitingSelect As String = Nothing
    Public AutoSelectedFabricApi As Boolean = False
    Public AutoSelectedOptiFabric As Boolean = False

    Private Controller As InstallCore
    Private ReadOnly Property Service As InstallCore
        Get
            If Controller Is Nothing Then Controller = New InstallCore(CreateHost(), CreateAnimations())
            Return Controller
        End Get
    End Property
    Private ReadOnly Property VanillaDrop As Integer
        Get
            Return Service.VanillaDrop
        End Get
    End Property
    Private Function CreateHost() As PCLCS.DownloadInstallPageHost(Of DlOptiFineListEntry, DlForgeVersionEntry, DlNeoForgeListEntry, DlLiteLoaderListEntry, ResourceVersion, McInstallRequest)
        Return New PCLCS.DownloadInstallPageHost(Of DlOptiFineListEntry, DlForgeVersionEntry, DlNeoForgeListEntry, DlLiteLoaderListEntry, ResourceVersion, McInstallRequest) With {
            .State = New SelectionAdapter(Me), .View = AddressOf CreateView, .Component = AddressOf CreateComponent,
            .Loading = AddressOf CreateLoading, .LoadingRun = CInt(MyLoading.MyLoadingState.Run),
            .LoadingError = CInt(MyLoading.MyLoadingState.Error), .LoadingUnloaded = CInt(MyLoading.MyLoadingState.Unloaded),
            .OptiFine = New PCLCS.DownloadInstallSource(Of List(Of DlOptiFineListEntry)) With {.Finished = Function() DlOptiFineListLoader.State = LoadState.Finished, .Output = Function() DlOptiFineListLoader.Output.Value, .Start = Sub() DlOptiFineListLoader.Start()},
            .LiteLoader = New PCLCS.DownloadInstallSource(Of List(Of DlLiteLoaderListEntry)) With {.Finished = Function() DlLiteLoaderListLoader.State = LoadState.Finished, .Output = Function() DlLiteLoaderListLoader.Output.Value, .Start = Sub() DlLiteLoaderListLoader.Start()},
            .NeoForge = New PCLCS.DownloadInstallSource(Of List(Of DlNeoForgeListEntry)) With {.Finished = Function() DlNeoForgeListLoader.State = LoadState.Finished, .Output = Function() DlNeoForgeListLoader.Output.Value, .Start = Sub() DlNeoForgeListLoader.Start()},
            .Fabric = New PCLCS.DownloadInstallSource(Of JObject) With {.Finished = Function() DlFabricListLoader.State = LoadState.Finished, .Output = Function() DlFabricListLoader.Output.Value, .Start = Sub() DlFabricListLoader.Start()},
            .FabricApi = New PCLCS.DownloadInstallSource(Of List(Of ResourceVersion)) With {.Finished = Function() DlFabricApiLoader.State = LoadState.Finished, .Output = Function() DlFabricApiLoader.Output, .Start = Sub() DlFabricApiLoader.Start()},
            .OptiFabric = New PCLCS.DownloadInstallSource(Of List(Of ResourceVersion)) With {.Finished = Function() DlOptiFabricLoader.State = LoadState.Finished, .Output = Function() DlOptiFabricLoader.Output, .Start = Sub() DlOptiFabricLoader.Start()},
            .Forge = AddressOf CreateForgeSource, .BindLoaders = AddressOf BindNativeLoaders,
            .RefreshRecommended = AddressOf McDownloadForgeRecommendedRefresh,
            .InitializeLoader = Sub() PageLoaderInit(LoadMinecraft, PanLoad, PanAllBack, Nothing, DlClientListLoader, AddressOf LoadMinecraft_OnFinish),
            .InitializeNameValidation = AddressOf InitializeNativeValidation,
            .SuppressStartAnimation = AddressOf SuppressNativeStartAnimation, .StartShow = Sub(Value) BtnStart.Show = Value,
            .AnimationControls = Function(Parent) GetAllAnimControls(Parent), .StartForge = AddressOf StartNativeForge,
            .ClientFinished = Function() DlClientListLoader.State = LoadState.Finished, .ClientList = Function() DlClientListLoader.Output.Value,
            .FoolName = Function(Value) GetMcFoolName(Value), .ReloadSelected = AddressOf ReloadSelected,
            .ShowMinecraftList = AddressOf ShowMinecraftVersionList, .SelectLogo = AddressOf GetSelectLogo,
            .SelectMinecraft = Sub(Row, Args) MinecraftSelected(DirectCast(Row, MyListItem), Args),
            .MinecraftRow = AddressOf CreateMinecraftRow,
            .OptiFineRow = Function(Entry, Click) OptiFineDownloadListItem(Entry, Sub(Sender, Args) Click(Sender, Args), False),
            .LiteLoaderRow = Function(Entry, Click) LiteLoaderDownloadListItem(Entry, Sub(Sender, Args) Click(Sender, Args), False),
            .ForgeRow = Function(Entry, Click) ForgeDownloadListItem(Entry, Sub(Sender, Args) Click(Sender, Args), False),
            .NeoForgeRow = Function(Entry, Click) NeoForgeDownloadListItem(Entry, Sub(Sender, Args) Click(Sender, Args), False),
            .FabricRow = Function(Entry, Click) FabricDownloadListItem(Entry, Sub(Sender, Args) Click(Sender, Args)),
            .FabricApiRow = Function(Entry, Click) FabricApiDownloadListItem(Entry, Sub(Sender, Args) Click(Sender, Args)),
            .OptiFabricRow = Function(Entry, Click) OptiFabricDownloadListItem(Entry, Sub(Sender, Args) Click(Sender, Args)),
            .ForgePreload = Sub(Panel, Entries, Click) ForgeDownloadListItemPreload(DirectCast(Panel, StackPanel), Entries, Sub(Sender, Args) Click(Sender, Args), False),
            .NeoForgePreload = Sub(Panel, Entries, Click) NeoForgeDownloadListItemPreload(DirectCast(Panel, StackPanel), Entries, Sub(Sender, Args) Click(Sender, Args), False),
            .FabricBuilder = AddressOf BuildNativeFabric, .BuildFabric = Sub() CardFabric.BuildContent(),
            .Materialize = Function(Row) MyVirtualizingElement.TryInit(Row),
            .RowTitle = Function(Row) DirectCast(Row, MyListItem).Title, .RowLogo = Function(Row) DirectCast(Row, MyListItem).Logo,
            .MinecraftCard = AddressOf CreateMinecraftCard, .Gray1 = Function() ColorGray1, .Gray4 = Function() ColorGray4,
            .MinecraftFolder = Function() McFolderSelected, .IsolationSetting = Function() Settings.Get(Of Integer)("LaunchArgumentIndieV2"),
            .Message = Function(Request) MyMsgBox(Request.Text, Request.Title, Request.Button1, Request.Button2),
            .StartRequested = AddressOf StartInstall, .Install = AddressOf McInstallStart}
    End Function
    Private Function CreateView() As PCLCS.DownloadInstallPageView
        Return New PCLCS.DownloadInstallPageView With {
            .Inner = PanInner, .Back = PanBack, .Start = BtnStart, .NameHint = HintSelectName,
            .Select = PanSelect, .Minecraft = PanMinecraft, .Name = TextSelectName, .MinecraftVersion = LabMinecraftVersion,
            .HintFabricApi = HintFabricAPI, .HintOptiFabric = HintOptiFabric, .HintOptiFabricOld = HintOptiFabricOld, .HintModOptiFine = HintModOptiFine,
            .Logo = Sub(Value) ImgLogo.Source = Value, .MinecraftLogo = Sub(Value) ImgMinecraft.Source = Value,
            .NameHintText = Sub(Value) HintSelectName.Text = Value, .NameValidated = Function() TextSelectName.IsValidated,
            .NameValidationResult = Function() TextSelectName.ValidateResult, .ValidateName = Sub() TextSelectName.Validate(), .ScrollHome = Sub() PanBack.ScrollToHome()}
    End Function
    Private Function CreateComponent(Kind As InstallKind) As PCLCS.DownloadInstallComponentView
        Select Case Kind
            Case InstallKind.OptiFine : Return NativeComponent(CardOptiFine, PanOptiFineInfo, PanOptiFine, BtnOptiFineClear, ImgOptiFine, LabOptiFine)
            Case InstallKind.LiteLoader : Return NativeComponent(CardLiteLoader, PanLiteLoaderInfo, PanLiteLoader, BtnLiteLoaderClear, ImgLiteLoader, LabLiteLoader)
            Case InstallKind.Forge : Return NativeComponent(CardForge, PanForgeInfo, PanForge, BtnForgeClear, ImgForge, LabForge)
            Case InstallKind.NeoForge : Return NativeComponent(CardNeoForge, PanNeoForgeInfo, PanNeoForge, BtnNeoForgeClear, ImgNeoForge, LabNeoForge)
            Case InstallKind.Fabric : Return NativeComponent(CardFabric, PanFabricInfo, PanFabric, BtnFabricClear, ImgFabric, LabFabric)
            Case InstallKind.FabricApi : Return NativeComponent(CardFabricApi, PanFabricApiInfo, PanFabricApi, BtnFabricApiClear, ImgFabricApi, LabFabricApi)
            Case InstallKind.OptiFabric : Return NativeComponent(CardOptiFabric, PanOptiFabricInfo, PanOptiFabric, BtnOptiFabricClear, ImgOptiFabric, LabOptiFabric)
            Case Else : Throw New ArgumentOutOfRangeException(NameOf(Kind))
        End Select
    End Function
    Private Shared Function NativeComponent(Card As MyCard, Info As Grid, Versions As StackPanel, Clear As FrameworkElement, Image As FrameworkElement, Label As TextBlock) As PCLCS.DownloadInstallComponentView
        Return New PCLCS.DownloadInstallComponentView With {.Card = Card, .MainSwap = Function() Card.MainSwap, .Info = Info,
            .Versions = Versions, .Clear = Clear, .Image = Image, .Label = Label,
            .IsSwapped = Function() Card.IsSwapped, .SetSwapped = Sub(Value) Card.IsSwapped = Value}
    End Function
    Private Function CreateLoading(Kind As InstallKind) As PCLCS.DownloadInstallLoadingView
        Select Case Kind
            Case InstallKind.OptiFine : Return NativeLoading(LoadOptiFine)
            Case InstallKind.LiteLoader : Return NativeLoading(LoadLiteLoader)
            Case InstallKind.Forge : Return NativeLoading(LoadForge)
            Case InstallKind.NeoForge : Return NativeLoading(LoadNeoForge)
            Case InstallKind.Fabric : Return NativeLoading(LoadFabric)
            Case InstallKind.FabricApi : Return NativeLoading(LoadFabricApi)
            Case InstallKind.OptiFabric : Return NativeLoading(LoadOptiFabric)
            Case Else : Throw New ArgumentOutOfRangeException(NameOf(Kind))
        End Select
    End Function
    Private Shared Function NativeLoading(Loader As MyLoading) As PCLCS.DownloadInstallLoadingView
        If Loader Is Nothing Then Return Nothing
        Return New PCLCS.DownloadInstallLoadingView With {.IsLoader = Function() Loader.State.IsLoader,
            .LoadingState = Function() CInt(Loader.State.LoadingState), .Error = Function() DirectCast(Loader.State, LoaderBase).Error}
    End Function
    Private Function CreateForgeSource() As PCLCS.DownloadInstallForgeSource(Of DlForgeVersionEntry)
        Dim Loader = DirectCast(LoadForge.State, LoaderTask(Of String, List(Of DlForgeVersionEntry)))
        Return New PCLCS.DownloadInstallForgeSource(Of DlForgeVersionEntry) With {
            .Input = Function() Loader.Input, .Output = Function() Loader.Output, .Finished = Function() Loader.State = LoadState.Finished}
    End Function
    Private Sub BindNativeLoaders()
        LoadOptiFine.State = DlOptiFineListLoader
        LoadLiteLoader.State = DlLiteLoaderListLoader
        LoadFabric.State = DlFabricListLoader
        LoadFabricApi.State = DlFabricApiLoader
        LoadNeoForge.State = DlNeoForgeListLoader
        LoadOptiFabric.State = DlOptiFabricLoader
    End Sub
    Private Sub InitializeNativeValidation()
        TextSelectName.ValidateRules = New ObjectModel.Collection(Of Validate) From {New ValidateFolderName(McFolderSelected & "versions")}
        TextSelectName.Validate()
    End Sub
    Private Sub SuppressNativeStartAnimation(Value As Boolean)
        If Value Then
            DisabledPageAnimControls.Add(BtnStart)
        Else
            DisabledPageAnimControls.Remove(BtnStart)
        End If
    End Sub
    Private Sub StartNativeForge(Version As String)
        Dim Loader = New LoaderTask(Of String, List(Of DlForgeVersionEntry))("DlForgeVersion " & Version, AddressOf DlForgeVersionMain)
        LoadForge.State = Loader
        Loader.Start(Version)
    End Sub
    Private Shared Function CreateMinecraftRow(Entry As JObject, Click As Action(Of FrameworkElement, MouseButtonEventArgs)) As FrameworkElement
        Dim Callback As MyListItem.ClickEventHandler = Nothing
        If Click IsNot Nothing Then Callback = Sub(Sender, Args) Click(Sender, Args)
        Return McDownloadListItem(Entry, Callback, False)
    End Function
    Private Sub BuildNativeFabric(Versions As IEnumerable(Of JObject), Click As Action(Of FrameworkElement, EventArgs))
        CardFabric.SwapControl = PanFabric
        CardFabric.SetContentBuilder(Versions, Function(Version) FabricDownloadListItem(Version, Sub(Sender, Args) Click(Sender, Args)))
    End Sub
    Private Shared Function CreateMinecraftCard(Title As String, Parent As Panel, Versions As List(Of JObject), Click As Action(Of FrameworkElement, MouseButtonEventArgs), Latest As Boolean) As FrameworkElement
        Dim Card As New MyCard With {.Title = Title, .Margin = If(Latest, New Thickness(0, 15, 0, 15), New Thickness(0, 0, 0, 15))}
        Dim Stack As New StackPanel With {.Margin = New Thickness(20, MyCard.SwapedHeight, 18, 0), .VerticalAlignment = VerticalAlignment.Top, .RenderTransform = New TranslateTransform(0, 0)}
        Card.Children.Add(Stack)
        Card.SwapControl = Stack
        Card.SetContentBuilder(Versions, Function(Version) McDownloadListItem(Version, Sub(Sender, Args) Click(Sender, Args), False))
        If Latest Then
            Card.BuildContent()
        Else
            Card.IsSwapped = True
        End If
        Return Card
    End Function
    Public Sub ControlInit()
        Service.ControlInit()
    End Sub
    Public Overridable Sub LoaderInit() Handles Me.Initialized
        Service.LoaderInit()
    End Sub
    Public Overridable Sub Init() Handles Me.Loaded
        Service.Init()
    End Sub
    Public Sub PrepareSelectPage()
        Service.PrepareSelectPage()
    End Sub
    Private Sub EnterSelectPage()
        Service.EnterSelectPage()
    End Sub
    Public Sub ReloadComponentLists()
        Service.ReloadComponentLists()
    End Sub
    Public Sub ExitSelectPage() Handles BtnBack.Click
        Service.ExitSelectPage()
    End Sub
    Public Overridable Sub MinecraftSelected(sender As MyListItem, e As MouseButtonEventArgs)
        Service.MinecraftSelected(sender, e)
    End Sub
    Public Overridable Sub ReloadSelected() Handles CardOptiFine.Swap, LoadOptiFine.StateChanged, CardForge.Swap, LoadForge.StateChanged, CardNeoForge.Swap, LoadNeoForge.StateChanged, CardFabric.Swap, LoadFabric.StateChanged, CardFabricApi.Swap, LoadFabricApi.StateChanged, CardOptiFabric.Swap, LoadOptiFabric.StateChanged, CardLiteLoader.Swap, LoadLiteLoader.StateChanged
        Service.ReloadSelected()
    End Sub
    Public Sub ClearSelected()
        Service.ClearSelected()
    End Sub
    Private Sub SetPanelVisibility(Panel As Grid, IsVisible As Boolean)
        Service.SetPanelVisibility(Panel, IsVisible)
    End Sub
    Public Overridable Function GetSelectLogo() As String
        Return Service.GetSelectLogo()
    End Function
    Public Function GetSelectName() As String
        Return Service.GetSelectName()
    End Function
    Private Sub SelectNameUpdate()
        Service.SelectNameUpdate()
    End Sub
    Private Sub TextSelectName_TextChanged(sender As Object, e As TextChangedEventArgs) Handles TextSelectName.TextChanged
        Service.NameTextChanged()
    End Sub
    Private Sub TextSelectName_ValidateChanged(sender As Object, e As EventArgs) Handles TextSelectName.ValidateChanged
        Service.NameValidateChanged()
    End Sub
    Private Sub LoadMinecraft_OnFinish()
        Service.LoadMinecraftOnFinish()
    End Sub
    Public Overridable Sub ShowMinecraftVersionList()
        Service.ShowMinecraftVersionList()
    End Sub
    Private Sub TrySelectWaitingVersion()
        Service.TrySelectWaitingVersion()
    End Sub
    Public Function LoadOptiFineGetError() As String
        Return Service.LoadOptiFineGetError()
    End Function
    Public Shared Function IsOptiFineSuitForForge(OptiFine As DlOptiFineListEntry, Forge As DlForgeVersionEntry) As Boolean
        Return InstallCore.IsOptiFineSuitForForge(OptiFine, Forge)
    End Function
    Private Sub CardOptiFine_PreviewSwap(sender As Object, e As RouteEventArgs) Handles CardOptiFine.PreviewSwap
        Service.PreviewSwap(InstallKind.OptiFine, Sub() e.Handled = True)
    End Sub
    Private Sub OptiFine_Loaded() Handles LoadOptiFine.StateChanged
        Service.OptiFineLoaded()
    End Sub
    Private Sub OptiFine_Selected(sender As MyListItem, e As EventArgs)
        Service.OptiFineSelected(sender, e)
    End Sub
    Private Sub OptiFine_Clear(sender As Object, e As EventArgs) Handles BtnOptiFineClear.Click
        Service.OptiFineClear()
    End Sub
    Public Function LoadLiteLoaderGetError() As String
        Return Service.LoadLiteLoaderGetError()
    End Function
    Private Sub CardLiteLoader_PreviewSwap(sender As Object, e As RouteEventArgs) Handles CardLiteLoader.PreviewSwap
        Service.PreviewSwap(InstallKind.LiteLoader, Sub() e.Handled = True)
    End Sub
    Private Sub LiteLoader_Loaded() Handles LoadLiteLoader.StateChanged
        Service.LiteLoaderLoaded()
    End Sub
    Private Sub LiteLoader_Selected(sender As MyListItem, e As EventArgs)
        Service.LiteLoaderSelected(sender, e)
    End Sub
    Private Sub LiteLoader_Clear(sender As Object, e As EventArgs) Handles BtnLiteLoaderClear.Click
        Service.LiteLoaderClear()
    End Sub
    Public Function LoadForgeGetError() As String
        Return Service.LoadForgeGetError()
    End Function
    Private Sub CardForge_PreviewSwap(sender As Object, e As RouteEventArgs) Handles CardForge.PreviewSwap
        Service.PreviewSwap(InstallKind.Forge, Sub() e.Handled = True)
    End Sub
    Private Sub Forge_Loaded() Handles LoadForge.StateChanged
        Service.ForgeLoaded()
    End Sub
    Private Sub Forge_Selected(sender As MyListItem, e As EventArgs)
        Service.ForgeSelected(sender, e)
    End Sub
    Private Sub Forge_Clear(sender As Object, e As EventArgs) Handles BtnForgeClear.Click
        Service.ForgeClear()
    End Sub
    Public Function LoadNeoForgeGetError() As String
        Return Service.LoadNeoForgeGetError()
    End Function
    Private Sub CardNeoForge_PreviewSwap(sender As Object, e As RouteEventArgs) Handles CardNeoForge.PreviewSwap
        Service.PreviewSwap(InstallKind.NeoForge, Sub() e.Handled = True)
    End Sub
    Private Sub NeoForge_Loaded() Handles LoadNeoForge.StateChanged
        Service.NeoForgeLoaded()
    End Sub
    Private Sub NeoForge_Selected(sender As MyListItem, e As EventArgs)
        Service.NeoForgeSelected(sender, e)
    End Sub
    Private Sub NeoForge_Clear(sender As Object, e As EventArgs) Handles BtnNeoForgeClear.Click
        Service.NeoForgeClear()
    End Sub
    Public Function LoadFabricGetError() As String
        Return Service.LoadFabricGetError()
    End Function
    Private Sub CardFabric_PreviewSwap(sender As Object, e As RouteEventArgs) Handles CardFabric.PreviewSwap
        Service.PreviewSwap(InstallKind.Fabric, Sub() e.Handled = True)
    End Sub
    Private Sub Fabric_Loaded() Handles LoadFabric.StateChanged
        Service.FabricLoaded()
    End Sub
    Public Sub Fabric_Selected(sender As MyListItem, e As EventArgs)
        Service.FabricSelected(sender, e)
    End Sub
    Private Sub Fabric_Clear(sender As Object, e As EventArgs) Handles BtnFabricClear.Click
        Service.FabricClear()
    End Sub
    Public Shared Function IsFabricApiCompatible(FabricApi As ResourceVersion, MinecraftVersion As String) As Boolean
        Return InstallCore.IsFabricApiCompatible(FabricApi, MinecraftVersion)
    End Function
    Public Function LoadFabricApiGetError() As String
        Return Service.LoadFabricApiGetError()
    End Function
    Private Sub CardFabricApi_PreviewSwap(sender As Object, e As RouteEventArgs) Handles CardFabricApi.PreviewSwap
        Service.PreviewSwap(InstallKind.FabricApi, Sub() e.Handled = True)
    End Sub
    Private Sub FabricApi_Loaded() Handles LoadFabricApi.StateChanged
        Service.FabricApiLoaded()
    End Sub
    Private Sub FabricApi_Selected(sender As MyListItem, e As EventArgs)
        Service.FabricApiSelected(sender, e)
    End Sub
    Private Sub FabricApi_Clear(sender As Object, e As EventArgs) Handles BtnFabricApiClear.Click
        Service.FabricApiClear()
    End Sub
    Public Shared Function IsOptiFabricCompatible(ModFile As ResourceVersion, MinecraftVersion As String) As Boolean
        Return InstallCore.IsOptiFabricCompatible(ModFile, MinecraftVersion)
    End Function
    Public Function LoadOptiFabricGetError() As String
        Return Service.LoadOptiFabricGetError()
    End Function
    Private Sub CardOptiFabric_PreviewSwap(sender As Object, e As RouteEventArgs) Handles CardOptiFabric.PreviewSwap
        Service.PreviewSwap(InstallKind.OptiFabric, Sub() e.Handled = True)
    End Sub
    Private Sub OptiFabric_Loaded() Handles LoadOptiFabric.StateChanged
        Service.OptiFabricLoaded()
    End Sub
    Private Sub OptiFabric_Selected(sender As MyListItem, e As EventArgs)
        Service.OptiFabricSelected(sender, e)
    End Sub
    Private Sub OptiFabric_Clear(sender As Object, e As EventArgs) Handles BtnOptiFabricClear.Click
        Service.OptiFabricClear()
    End Sub
    Public Function GetSelectedComponents() As McComponents
        Return Service.GetSelectedComponents()
    End Function
    Private Sub TextSelectName_KeyDown(sender As Object, e As KeyEventArgs) Handles TextSelectName.KeyDown
        Service.NameKeyDown(e.Key)
    End Sub
    Private Sub BtnStart_Click() Handles BtnStart.Click
        Service.StartClick()
    End Sub
    Public Overridable Sub StartInstall(Request As McInstallRequest)
        Service.StartInstall(Request)
    End Sub
    Private Function GetLoaderError(Loader As MyLoading) As String
        Return Service.GetLoaderError(NativeLoading(Loader))
    End Function

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

    Private NotInheritable Class SelectionAdapter
        Inherits PCLCS.DownloadInstallSelection(Of DlOptiFineListEntry, DlForgeVersionEntry, DlNeoForgeListEntry, DlLiteLoaderListEntry, ResourceVersion)
        Private ReadOnly Owner As PageDownloadInstall
        Public Sub New(Owner As PageDownloadInstall)
            Me.Owner = Owner
        End Sub
        Public Overrides Property IsInSelectPage As Boolean
            Get
                Return Owner.IsInSelectPage
            End Get
            Set(Value As Boolean)
                Owner.IsInSelectPage = Value
            End Set
        End Property
        Public Overrides Property VanillaName As String
            Get
                Return Owner.VanillaName
            End Get
            Set(Value As String)
                Owner.VanillaName = Value
            End Set
        End Property
        Public Overrides Property VanillaData As JObject
            Get
                Return Owner.VanillaData
            End Get
            Set(Value As JObject)
                Owner.VanillaData = Value
            End Set
        End Property
        Public Overrides Property VanillaIcon As String
            Get
                Return Owner.VanillaIcon
            End Get
            Set(Value As String)
                Owner.VanillaIcon = Value
            End Set
        End Property
        Public Overrides Property SelectedOptiFine As DlOptiFineListEntry
            Get
                Return Owner.SelectedOptiFine
            End Get
            Set(Value As DlOptiFineListEntry)
                Owner.SelectedOptiFine = Value
            End Set
        End Property
        Public Overrides Property SelectedLiteLoader As DlLiteLoaderListEntry
            Get
                Return Owner.SelectedLiteLoader
            End Get
            Set(Value As DlLiteLoaderListEntry)
                Owner.SelectedLiteLoader = Value
            End Set
        End Property
        Public Overrides Property SelectedForge As DlForgeVersionEntry
            Get
                Return Owner.SelectedForge
            End Get
            Set(Value As DlForgeVersionEntry)
                Owner.SelectedForge = Value
            End Set
        End Property
        Public Overrides Property SelectedNeoForge As DlNeoForgeListEntry
            Get
                Return Owner.SelectedNeoForge
            End Get
            Set(Value As DlNeoForgeListEntry)
                Owner.SelectedNeoForge = Value
            End Set
        End Property
        Public Overrides Property SelectedFabric As String
            Get
                Return Owner.SelectedFabric
            End Get
            Set(Value As String)
                Owner.SelectedFabric = Value
            End Set
        End Property
        Public Overrides Property SelectedFabricApi As ResourceVersion
            Get
                Return Owner.SelectedFabricApi
            End Get
            Set(Value As ResourceVersion)
                Owner.SelectedFabricApi = Value
            End Set
        End Property
        Public Overrides Property SelectedOptiFabric As ResourceVersion
            Get
                Return Owner.SelectedOptiFabric
            End Get
            Set(Value As ResourceVersion)
                Owner.SelectedOptiFabric = Value
            End Set
        End Property
        Public Overrides Property IsSelectNameEdited As Boolean
            Get
                Return Owner.IsSelectNameEdited
            End Get
            Set(Value As Boolean)
                Owner.IsSelectNameEdited = Value
            End Set
        End Property
        Public Overrides Property MinecraftVersions As JArray
            Get
                Return Owner.MinecraftVersions
            End Get
            Set(Value As JArray)
                Owner.MinecraftVersions = Value
            End Set
        End Property
        Public Overrides Property VersionWaitingSelect As String
            Get
                Return PageDownloadInstall.VersionWaitingSelect
            End Get
            Set(Value As String)
                PageDownloadInstall.VersionWaitingSelect = Value
            End Set
        End Property
        Public Overrides Property AutoSelectedFabricApi As Boolean
            Get
                Return Owner.AutoSelectedFabricApi
            End Get
            Set(Value As Boolean)
                Owner.AutoSelectedFabricApi = Value
            End Set
        End Property
        Public Overrides Property AutoSelectedOptiFabric As Boolean
            Get
                Return Owner.AutoSelectedOptiFabric
            End Get
            Set(Value As Boolean)
                Owner.AutoSelectedOptiFabric = Value
            End Set
        End Property
    End Class
End Class
