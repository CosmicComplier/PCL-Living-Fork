Imports ModifyCore = PCLCS.DownloadModifyPageController(Of PCL.ModMinecraft.McInstance, PCL.ModMinecraft.McInstance.McInstanceState, PCL.ModMinecraft.McInstanceCardType, PCL.ModDownload.DlOptiFineListEntry, PCL.ModDownload.DlForgelikeEntry, PCL.ModDownload.DlForgeVersionEntry, PCL.ModDownload.DlNeoForgeListEntry, PCL.ModDownload.DlLiteLoaderListEntry, PCL.ResourceVersion, PCL.ModDownloadLib.McInstallRequest, PCL.ModLoader.LoaderBase, PCL.MyLoading, PCL.ModAnimation.AniData)
Imports InstallKind = PCLCS.DownloadInstallComponent

' LF's original XAML, virtual dispatch, loaders and fields. All workflows and comparison decisions are C#.
Public Class PageDownloadModify
    Inherits PageDownloadInstall
    Private InitLoader As LoaderTask(Of McInstance, Object)
    Private ForgeVersionLoader As LoaderTask(Of String, List(Of DlForgeVersionEntry))
    Private InitialTarget As McComponents
    Private IsVanillaVersionReliable As Boolean
    Private OriginalName As String
    Private CustomLogo As String
    Private ReadOnly PendingComponents As New Dictionary(Of MyLoading, String)
    Private IsRestoringComponents As Boolean
    Friend PackUpdateButtons As WrapPanel
    Friend PackUpdateName As TextBlock
    Friend PackUpdateInfo As TextBlock
    Private ModifyController As ModifyCore
    Private ReadOnly Property ModifyService As ModifyCore
        Get
            If ModifyController Is Nothing Then ModifyController = New ModifyCore(CreateModifyHost(), CreateModifyAnimations())
            Return ModifyController
        End Get
    End Property
    Private Function CreateModifyHost() As PCLCS.DownloadModifyPageHost(Of DlOptiFineListEntry, DlForgeVersionEntry, DlNeoForgeListEntry, DlLiteLoaderListEntry, ResourceVersion, McInstallRequest, McInstance, LoaderBase, MyLoading)
        Return New PCLCS.DownloadModifyPageHost(Of DlOptiFineListEntry, DlForgeVersionEntry, DlNeoForgeListEntry, DlLiteLoaderListEntry, ResourceVersion, McInstallRequest, McInstance, LoaderBase, MyLoading) With {
            .Selection = New SelectionAdapter(Me), .State = New StateAdapter(Me), .View = AddressOf CreateModifyView,
            .Comparison = AddressOf CreateComparison, .ComparisonCard = Function(Panel) NativeCard(DirectCast(Panel.Parent, MyCard)),
            .PackButtons = Function() PackUpdateButtons, .PackName = Function() PackUpdateName, .PackInfo = Function() PackUpdateInfo,
            .SetPackButtons = Sub(Value) PackUpdateButtons = Value, .SetPackName = Sub(Value) PackUpdateName = Value, .SetPackInfo = Sub(Value) PackUpdateInfo = Value,
            .Card = Function(Title, Margin) New MyCard With {.Title = Title, .Margin = Margin}, .UpdateButton = AddressOf CreateUpdateButton,
            .StartPackUpdate = Sub(Online) ModpackUpdateFlow.Start(PageInstanceLeft.Instance, Online), .OpenPackBackups = Sub() ModpackUpdateFlow.OpenBackups(PageInstanceLeft.Instance),
            .BaseControlInit = AddressOf ControlInit, .InitializeLoader = AddressOf InitializeNativeLoader, .Restart = Sub() PageLoaderRestart(), .Cancel = Sub() InitLoader.Cancel(),
            .BaseClearSelected = AddressOf ClearSelected, .BaseReloadSelected = AddressOf InvokeBaseReload, .BaseShowMinecraftList = AddressOf InvokeBaseShowMinecraft,
            .BasePrepareSelection = AddressOf PrepareSelectPage, .BaseReloadLists = AddressOf ReloadComponentLists, .BaseLogo = AddressOf InvokeBaseLogo,
            .SelectName = AddressOf GetSelectName, .Components = AddressOf GetSelectedComponents,
            .SelectMinecraft = Sub(Version) MinecraftSelected(McDownloadListItem(Version, Nothing, False).Init(), Nothing),
            .MinecraftRow = Function(Version) McDownloadListItem(Version, Nothing, False).Init(),
            .RowTitle = Function(Row) DirectCast(Row, MyListItem).Title, .RowLogo = Function(Row) DirectCast(Row, MyListItem).Logo,
            .InitInput = Function() InitLoader.Input, .UiWait = Sub(Work) RunInUiWait(Sub() Work()), .DetectPack = Function(Path) ModpackUpdate.Detect(Path),
            .NewInstance = Function(Path) New McInstance(Path),
            .CatalogLoaders = Function() New Dictionary(Of String, LoaderBase) From {{"Minecraft", DlClientListLoader}, {"OptiFine", DlOptiFineListLoader}, {"LiteLoader", DlLiteLoaderListLoader}, {"NeoForge", DlNeoForgeListLoader}, {"Fabric", DlFabricListLoader}, {"Fabric API", DlFabricApiLoader}, {"OptiFabric", DlOptiFabricLoader}},
            .CreateForgeLoader = Function(Version) New LoaderTask(Of String, List(Of DlForgeVersionEntry))("DlForgeVersion " & Version, AddressOf DlForgeVersionMain),
            .States = New PCLCS.MinecraftInstallStates With {.Waiting = CInt(LoadState.Waiting), .Loading = CInt(LoadState.Loading), .Finished = CInt(LoadState.Finished), .Failed = CInt(LoadState.Failed), .Canceled = CInt(LoadState.Canceled)},
            .StartLoader = Sub(Loader, Input, Force) Loader.Start(Input, Force), .LoaderState = Function(Loader) CInt(Loader.State),
            .LoaderError = Function(Loader) Loader.Error, .LoaderProgress = Function(Loader) Loader.Progress,
            .ForgeInput = Function(Loader) DirectCast(Loader, LoaderTask(Of String, List(Of DlForgeVersionEntry))).Input,
            .ForgeOutput = Function(Loader) DirectCast(Loader, LoaderTask(Of String, List(Of DlForgeVersionEntry))).Output,
            .BindForge = Sub(Loader) LoadForge.State = Loader, .SwapRestoreParent = Sub(Button, Swapped) DirectCast(Button.Parent, MyCard).IsSwapped = Swapped,
            .LoadingControl = AddressOf ReadLoading, .IsLoader = Function(Loading) Loading.State.IsLoader, .ReadLoader = Function(Loading) DirectCast(Loading.State, LoaderBase),
            .ComponentError = AddressOf ReadComponentError,
            .ClientOutput = Function() DlClientListLoader.Output.Value, .FabricOutput = Function() DlFabricListLoader.Output.Value, .OptiFineHashes = AddressOf ReadOptiFineHashes,
            .OptiFineOutput = Function() DlOptiFineListLoader.Output.Value, .NeoForgeOutput = Function() DlNeoForgeListLoader.Output.Value,
            .LiteLoaderOutput = Function() DlLiteLoaderListLoader.Output.Value, .FabricApiOutput = Function() DlFabricApiLoader.Output, .OptiFabricOutput = Function() DlOptiFabricLoader.Output,
            .OptiFineMirrorFinished = Function() DlOptiFineListBmclapiLoader.State = LoadState.Finished,
            .WaitOptiFineMirror = Sub() DlOptiFineListBmclapiLoader.WaitForExit(), .CopyOptiFineMirror = Sub() DlOptiFineListLoader.Output = DlOptiFineListBmclapiLoader.Output,
            .LocalSha1 = Function(File) New LocalResourceFile(File).ModrinthHash,
            .ChooseMod = Function(Names, Title) MyMsgBoxSelect(Names.Select(Function(Name) CType(New MyRadioBox With {.Text = Name}, IMyRadio)), Title),
            .ReadIniBoolean = Function(File, Key) ReadIni(File, Key, False), .ModpackEnabled = Function() ModModpackMode.Enabled,
            .CanChangeMinecraft = AddressOf ModModpackMode.CanChangeMinecraft, .MinecraftFolder = Function() McFolderSelected, .Gray4 = Function() ColorGray4,
            .Hint = AddressOf NativeHint, .Message = Function(Request) MyMsgBox(Request.Text, Request.Title, Request.Button1, Request.Button2, Request.Button3, Request.IsWarn),
            .Modify = Function(Request, Instance) McModificationStart(Request, Instance),
            .InstancePage = Function() PageInstanceLeft.Instance, .SetInstancePage = Sub(Value) PageInstanceLeft.Instance = Value,
            .SelectedInstance = Function() McInstanceSelected, .SetSelectedInstance = Sub(Value) McInstanceSelected = Value,
            .ReloadJava = AddressOf PageInstanceLeft.ReloadCurrentJava,
            .RefreshInstances = Sub() LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\"),
            .PageBack = Sub() FrmMain.PageBack()}
    End Function
    Private Function CreateModifyView() As PCLCS.DownloadModifyPageView
        Return New PCLCS.DownloadModifyPageView With {
            .Select = PanSelect, .SelectNameCard = CardSelectName, .AllBack = PanAllBack, .Back = PanBack,
            .Minecraft = PanMinecraft, .BackButton = BtnBack, .Logo = ImgLogo, .ChangeMinecraft = PanMinecraftChange,
            .Start = BtnStart, .NameRestore = BtnOriginalNameRestore, .Name = TextSelectName, .NameLabel = LabSelectName, .MinecraftVersion = LabMinecraftVersion,
            .ScrollHome = Sub() PanBack.ScrollToHome(), .MinecraftOldLogo = Sub(Value) ImgMinecraftOld.Source = Value, .MinecraftNewLogo = Sub(Value) ImgMinecraftNew.Source = Value,
            .NameValidated = Function() TextSelectName.IsValidated,
            .ValidateName = Sub(Ignore) TextSelectName.ValidateRules = New ObjectModel.Collection(Of Validate) From {New ValidateFolderName(McFolderSelected & "versions", IgnoreList:=New List(Of String) From {Ignore})},
            .StartText = Sub(Value) BtnStart.Text = Value, .SetStartLogo = Sub() BtnStart.Logo = FrmInstanceLeft.ItemModify.Logo}
    End Function
    Private Function CreateComparison(Kind As Integer) As PCLCS.DownloadModifyComparison
        Select Case Kind
            Case -1 : Return NativeComparison(CardMinecraft, PanMinecraftCompare, PanMinecraftInfo, BtnMinecraftRestore, LabMinecraftVersion, LabMinecraftOld)
            Case 0 : Return NativeComparison(CardOptiFine, PanOptiFineCompare, PanOptiFineInfo, BtnOptiFineRestore, LabOptiFine, LabOptiFineOld, BtnOptiFineClear)
            Case 1 : Return NativeComparison(CardLiteLoader, PanLiteLoaderCompare, PanLiteLoaderInfo, BtnLiteLoaderRestore, LabLiteLoader, LabLiteLoaderOld, BtnLiteLoaderClear)
            Case 2 : Return NativeComparison(CardForge, PanForgeCompare, PanForgeInfo, BtnForgeRestore, LabForge, LabForgeOld, BtnForgeClear)
            Case 3 : Return NativeComparison(CardNeoForge, PanNeoForgeCompare, PanNeoForgeInfo, BtnNeoForgeRestore, LabNeoForge, LabNeoForgeOld, BtnNeoForgeClear)
            Case 4 : Return NativeComparison(CardFabric, PanFabricCompare, PanFabricInfo, BtnFabricRestore, LabFabric, LabFabricOld, BtnFabricClear)
            Case 5 : Return NativeComparison(CardFabricApi, PanFabricApiCompare, PanFabricApiInfo, BtnFabricApiRestore, LabFabricApi, LabFabricApiOld, BtnFabricApiClear)
            Case 6 : Return NativeComparison(CardOptiFabric, PanOptiFabricCompare, PanOptiFabricInfo, BtnOptiFabricRestore, LabOptiFabric, LabOptiFabricOld, BtnOptiFabricClear)
            Case Else : Throw New ArgumentOutOfRangeException(NameOf(Kind))
        End Select
    End Function
    Private Shared Function NativeCard(Card As MyCard) As PCLCS.DownloadModifyComparison
        Return New PCLCS.DownloadModifyComparison With {.Card = Card, .MainSwap = Function() Card.MainSwap, .SetSwapped = Sub(Value) Card.IsSwapped = Value}
    End Function
    Private Shared Function NativeComparison(Card As MyCard, Compare As Grid, Current As FrameworkElement, Restore As MyIconButton, Label As TextBlock, OldLabel As TextBlock, Optional Clear As FrameworkElement = Nothing) As PCLCS.DownloadModifyComparison
        Dim View = NativeCard(Card)
        View.Compare = Compare
        View.Current = Current
        View.Restore = Restore
        View.Label = Label
        View.OldLabel = OldLabel
        View.Clear = Clear
        Return View
    End Function
    Private Shared Function CreateUpdateButton(Text As String, Click As Action) As FrameworkElement
        Dim Button As New MyButton With {.Text = Text, .MinWidth = 140, .Height = 35, .Margin = New Thickness(0, 13, 20, 0), .Padding = New Thickness(13, 0, 13, 0)}
        AddHandler Button.Click, Sub() Click()
        Return Button
    End Function
    Private Sub InitializeNativeLoader()
        InitLoader = New LoaderTask(Of McInstance, Object)("初始化版本修改", AddressOf LoadInstance)
        PageLoaderInit(LoadMinecraft, PanLoad, PanAllBack, Nothing, InitLoader, AddressOf LoadInstanceFinished, Function() PageInstanceLeft.Instance)
    End Sub
    Private Shared Function WrapTask(Task As LoaderTask(Of McInstance, Object)) As PCLCS.DownloadModifyTask(Of McInstance)
        Return New PCLCS.DownloadModifyTask(Of McInstance) With {.Input = Function() Task.Input, .Output = Function() Task.Output,
            .SetOutput = Sub(Value) Task.Output = Value, .Canceled = Function() Task.IsCanceled, .ForceRestart = Function() Task.IsForceRestarting, .Progress = Sub(Value) Task.Progress = Value}
    End Function
    Private Function ReadLoading(Kind As InstallKind) As MyLoading
        Select Case Kind
            Case InstallKind.OptiFine : Return LoadOptiFine
            Case InstallKind.LiteLoader : Return LoadLiteLoader
            Case InstallKind.Forge : Return LoadForge
            Case InstallKind.NeoForge : Return LoadNeoForge
            Case InstallKind.Fabric : Return LoadFabric
            Case InstallKind.FabricApi : Return LoadFabricApi
            Case InstallKind.OptiFabric : Return LoadOptiFabric
            Case Else : Throw New ArgumentOutOfRangeException(NameOf(Kind))
        End Select
    End Function
    Private Function ReadComponentError(Kind As InstallKind) As String
        Select Case Kind
            Case InstallKind.OptiFine : Return LoadOptiFineGetError()
            Case InstallKind.LiteLoader : Return LoadLiteLoaderGetError()
            Case InstallKind.Forge : Return LoadForgeGetError()
            Case InstallKind.NeoForge : Return LoadNeoForgeGetError()
            Case InstallKind.Fabric : Return LoadFabricGetError()
            Case InstallKind.FabricApi : Return LoadFabricApiGetError()
            Case InstallKind.OptiFabric : Return LoadOptiFabricGetError()
            Case Else : Throw New ArgumentOutOfRangeException(NameOf(Kind))
        End Select
    End Function
    Private Function ReadOptiFineHashes() As JObject
        Static Hashes As JObject = FileUtils.ReadAsJson("Minecraft\OptiFineHashes.json", type:=GetType(Resource))
        Return Hashes
    End Function
    Private Sub InvokeBaseReload()
        MyBase.ReloadSelected()
    End Sub
    Private Sub InvokeBaseShowMinecraft()
        MyBase.ShowMinecraftVersionList()
    End Sub
    Private Function InvokeBaseLogo() As String
        Return MyBase.GetSelectLogo()
    End Function
    Private Shared Sub NativeHint(Text As String, Kind As PCLCS.SelectionPageHint)
        Select Case Kind
            Case PCLCS.SelectionPageHint.Red : Hint(Text, HintType.Red)
            Case PCLCS.SelectionPageHint.Blue : Hint(Text, HintType.Blue)
            Case Else : Hint(Text, HintType.Green)
        End Select
    End Sub
    Public Overrides Sub LoaderInit() '父级 Handles Me.Initialized
        ModifyService.LoaderInit()
    End Sub
    Public Overrides Sub Init() '父级 Handles Me.Loaded
        ModifyService.Init()
    End Sub
    Private Sub PageUnloaded() Handles Me.Unloaded
        ModifyService.Unloaded()
    End Sub
    Private Sub LoadInstance(Task As LoaderTask(Of McInstance, Object))
        ModifyService.LoadInstance(WrapTask(Task))
    End Sub
    Private Function SelectMod(Of T)(ComponentName As String, Matches As List(Of (File As FileInfo, Sha1 As String, Version As T))) As (File As FileInfo, Sha1 As String, Version As T)?
        Return ModifyService.SelectMod(ComponentName, Matches)
    End Function
    Private Sub LoadInstanceFinished(Task As LoaderTask(Of McInstance, Object))
        ModifyService.LoadInstanceFinished(WrapTask(Task))
    End Sub
    Public Overrides Function GetSelectLogo() As String
        Return ModifyService.GetSelectLogo()
    End Function
    Public Overrides Sub ShowMinecraftVersionList()
        ModifyService.ShowMinecraftVersionList()
    End Sub
    Public Overrides Sub ReloadSelected()
        ModifyService.ReloadSelected()
    End Sub
    Friend Sub RefreshModpackRestrictions()
        ModifyService.RefreshModpackRestrictions()
    End Sub
    Private Sub RefreshComparisons()
        ModifyService.RefreshComparisons()
    End Sub
    Private Sub ShowComparison(ComparePanel As Grid, CurrentPanel As FrameworkElement, OldVersion As String, NewVersion As String, NewText As String,
                               RestoreButton As MyIconButton, CanRestore As Boolean, Optional ClearButton As FrameworkElement = Nothing)
        ModifyService.ShowComparison(ComparePanel, CurrentPanel, OldVersion, NewVersion, NewText, RestoreButton, CanRestore, ClearButton)
    End Sub
    Private Sub RestoreComponent(sender As Object, e As EventArgs) Handles BtnMinecraftRestore.Click, BtnOptiFineRestore.Click, BtnForgeRestore.Click,
                BtnNeoForgeRestore.Click, BtnFabricRestore.Click, BtnFabricApiRestore.Click, BtnOptiFabricRestore.Click, BtnLiteLoaderRestore.Click
        ModifyService.RestoreComponent(DirectCast(sender, MyIconButton))
    End Sub
    Private Sub CardMinecraft_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) Handles CardMinecraft.MouseLeftButtonUp
        ModifyService.MinecraftClick(Sub() e.Handled = True)
    End Sub
    Private Sub UpdateStartButton() Handles TextSelectName.TextChanged, TextSelectName.ValidateChanged
        ModifyService.UpdateStartButton()
    End Sub
    Private Sub UpdateOriginalNameVisibility() Handles TextSelectName.TextChanged
        ModifyService.UpdateOriginalNameVisibility()
    End Sub
    Private Sub BtnOriginalNameRestore_Click(sender As Object, e As EventArgs) Handles BtnOriginalNameRestore.Click
        ModifyService.RestoreOriginalName()
    End Sub
    Public Overrides Sub StartInstall(Request As McInstallRequest)
        ModifyService.StartInstall(Request)
    End Sub
    Friend Sub ShowPackInfo(Data As JObject)
        ModifyService.ShowPackInfo(Data)
    End Sub
    Private Function CreateModifyAnimations() As PCLCS.DownloadModifyAnimation(Of AniData)
        Return New PCLCS.DownloadModifyAnimation(Of AniData) With {.Native = CreateAnimations(),
            .Double = Function(Setter, Delta, Duration, Delay, Ease) AaDouble(Sub(Value) Setter(Value), Delta, Duration, Delay, ReadEase(Ease))}
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
    Private NotInheritable Class StateAdapter
        Inherits PCLCS.DownloadModifyPageState(Of LoaderBase, MyLoading)
        Private ReadOnly Owner As PageDownloadModify
        Public Sub New(Owner As PageDownloadModify)
            Me.Owner = Owner
        End Sub
        Public Overrides Property ForgeVersionLoader As LoaderBase
            Get
                Return Owner.ForgeVersionLoader
            End Get
            Set(Value As LoaderBase)
                Owner.ForgeVersionLoader = DirectCast(Value, LoaderTask(Of String, List(Of DlForgeVersionEntry)))
            End Set
        End Property
        Public Overrides Property InitialTarget As McComponents
            Get
                Return Owner.InitialTarget
            End Get
            Set(Value As McComponents)
                Owner.InitialTarget = Value
            End Set
        End Property
        Public Overrides Property IsVanillaVersionReliable As Boolean
            Get
                Return Owner.IsVanillaVersionReliable
            End Get
            Set(Value As Boolean)
                Owner.IsVanillaVersionReliable = Value
            End Set
        End Property
        Public Overrides Property OriginalName As String
            Get
                Return Owner.OriginalName
            End Get
            Set(Value As String)
                Owner.OriginalName = Value
            End Set
        End Property
        Public Overrides Property CustomLogo As String
            Get
                Return Owner.CustomLogo
            End Get
            Set(Value As String)
                Owner.CustomLogo = Value
            End Set
        End Property
        Public Overrides Property IsRestoringComponents As Boolean
            Get
                Return Owner.IsRestoringComponents
            End Get
            Set(Value As Boolean)
                Owner.IsRestoringComponents = Value
            End Set
        End Property
        Public Overrides ReadOnly Property PendingComponents As Dictionary(Of MyLoading, String)
            Get
                Return Owner.PendingComponents
            End Get
        End Property
    End Class
    Private NotInheritable Class SelectionAdapter
        Inherits PCLCS.DownloadInstallSelection(Of DlOptiFineListEntry, DlForgeVersionEntry, DlNeoForgeListEntry, DlLiteLoaderListEntry, ResourceVersion)
        Private ReadOnly Owner As PageDownloadModify
        Public Sub New(Owner As PageDownloadModify)
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
