Imports System.Collections.ObjectModel
Imports DetailCore = PCLCS.ResourceDetailPageController(Of PCL.ResourceProject, PCL.ResourceVersion, PCL.MyCard, PCL.MyResourceItem, PCL.ModMinecraft.McInstance, PCL.ModMinecraft.McInstance.McInstanceState, PCL.ModMinecraft.McInstanceCardType, PCL.ModLoader.LoaderBase, PCL.ModNet.NetFile, PCL.ModAnimation.AniData)

Public Class PageDownloadResourceDetail
    Public Shared CachedFolder As New Dictionary(Of ResourceTypes, String)
    Private Shared ReadOnly NativeLoaders As New ResourceDetailLoaders
    Private ResourceFileLoader As New LoaderTask(Of Integer, List(Of ResourceVersion))("Resource File",
        Sub(Task) Service.LoadFiles(Function() Task.IsCanceled, Sub(Value) Task.Output = Value))
    Private Controller As DetailCore
    Private ReadOnly Property Service As DetailCore
        Get
            If Controller Is Nothing Then Controller = New DetailCore(CreateHost(), CreateDownloadHost(), CreateAnimations())
            Return Controller
        End Get
    End Property
    Private Function CreateHost() As PCLCS.ResourceDetailPageHost(Of ResourceProject, ResourceVersion, MyCard, MyResourceItem)
        Return New PCLCS.ResourceDetailPageHost(Of ResourceProject, ResourceVersion, MyCard, MyResourceItem) With {
            .View = Function() New PCLCS.ResourceDetailPageView With {.Filter = PanFilter, .Results = PanResults, .Intro = PanIntro,
                .FilterCard = CardFilter, .Alternative = HintAlternative, .Wiki = BtnIntroWiki,
                .AlternativeText = Sub(Value) HintAlternative.Text = Value, .WebText = Sub(Value) BtnIntroWeb.Text = Value, .ScrollHome = Sub() PanBack.ScrollToHome()},
            .Argument = Function(Index) FrmMain.PageCurrent.Additional(Index), .Arguments = Function() TryCast(FrmMain.PageCurrent.Additional, Object()),
            .SetArgument = Sub(Index, Value) FrmMain.PageCurrent.Additional(Index) = Value,
            .HasArguments = Function() FrmMain.PageCurrent.Additional IsNot Nothing,
            .Failed = Function() ResourceFileLoader.State = LoadState.Failed, .Error = Function() ResourceFileLoader.Error,
            .Files = Function() ResourceFileLoader.Output, .ProjectFiles = Function(Id, Platform) ResourceVersion.FromProjectId(Id, Platform),
            .Initialize = Sub() PageLoaderInit(Load, PanLoad, PanMain, CardIntro, ResourceFileLoader, AddressOf Load_OnFinish),
            .Restart = Sub(Force) PageLoaderRestart(IsForceRestart:=Force),
            .FilterButton = AddressOf CreateFilterButton, .FilterText = Function(Button) DirectCast(Button, MyRadioButton).Text,
            .FilterChecked = Sub(Button, Value) DirectCast(Button, MyRadioButton).Checked = Value,
            .SwappedHeight = Function() MyCard.SwapedHeight, .Card = AddressOf CreateVersionCard,
            .SetBuilder = Sub(Card, Build) Card.SetContentBuilder(Build),
            .CardTitle = Function(Card) Card.Title, .BuildCard = Sub(Card) Card.BuildContent(), .SwapCard = Sub(Card, Value) Card.IsSwapped = Value,
            .HasProject = Function(Id) ResourceProject.Cache.ContainsKey(Id), .CachedProject = Function(Id) ResourceProject.Cache(Id),
            .ProjectRow = Function(Project) Project.ToResourceItem(False, False),
            .YellowHint = Function(Text, Margin) New MyHint With {.Text = Text, .Theme = MyHint.Themes.Yellow, .Margin = Margin},
            .IsVersionRow = Function(Item) TypeOf Item Is MyListItem,
            .VersionRow = AddressOf CreateVersionRow,
            .Header = Function(Project) Project.ToResourceItem(True, True).Init(),
            .HeaderInteractive = Sub(Item, Value) Item.CanInteraction = Value,
            .HeaderLogo = Function(Item) Item.PathLogo.ActualSource, .HeaderName = Function(Item) (Item.LabTitle.Text, Item.LabTitleRaw.Text),
            .Website = Sub(Url) OpenWebsite(Url), .Clipboard = Sub(Value) ClipboardSet(Value)}
    End Function
    Private Shared Function CreateDownloadHost() As PCLCS.ResourceDetailDownloadHost(Of LoaderBase, NetFile, ResourceVersion, McInstance)
        Return New PCLCS.ResourceDetailDownloadHost(Of LoaderBase, NetFile, ResourceVersion, McInstance) With {
            .Loaders = NativeLoaders, .States = New PCLCS.MinecraftInstallStates With {.Waiting = CInt(LoadState.Waiting), .Loading = CInt(LoadState.Loading),
                .Finished = CInt(LoadState.Finished), .Failed = CInt(LoadState.Failed), .Canceled = CInt(LoadState.Canceled)},
            .CachedFolders = CachedFolder, .MinecraftFolder = Function() McFolderSelected, .SelectedInstance = Function() McInstanceSelected,
            .Instances = Function() McInstanceList.Values.SelectMany(Function(List) List),
            .InstanceListFinished = Function() McInstanceListLoader.State = LoadState.Finished,
            .LoadInstanceList = Sub() LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\", WaitForExit:=True),
            .RefreshDownload = Sub() FrmMain.BtnExtraDownload.ShowRefresh(), .RibbleDownload = Sub() FrmMain.BtnExtraDownload.Ribble(),
            .TaskManager = Sub() FrmMain.PageChange(FormMain.PageType.TaskManager), .RefreshCurrentResources = AddressOf RefreshCurrentResources,
            .TranslateSetting = Function() Settings.Get(Of Integer)("ToolDownloadTranslateV2"),
            .NameValidator = AddressOf CreateNameValidator,
            .NetFile = Function(Version, Target, Reason, GameVersion, Loaders) Version.ToNetFile(Target, CType(CInt(Reason), ResourceVersion.DownloadReason), GameVersion, Loaders),
            .InstallPack = Sub(Target, Name, Logo, Source) ModpackInstall(Target, Name, Logo, Source),
            .MapDownloadFolder = AddressOf MapDownloadInstaller.GetDownloadFolder, .InstallMap = AddressOf MapDownloadInstaller.Install,
            .InterruptOnCancel = Sub(Loader, Value) DirectCast(Loader, LoaderTask(Of Integer, String)).InterruptOnCancel = Value,
            .StateHint = AddressOf LoaderStateChangedHintOnly, .ClearFailedFolder = Sub(Loader) McInstallFailedClearFolder(Loader),
            .Hint = AddressOf DownloadHint,
            .SaveFile = Function(Name, Folder, Extension, Description) Dialogs.SaveFile("选择保存位置", Name, Folder, {(Extension, Description)}),
            .Explorer = Sub(Target) OpenExplorer(Target),
            .RunInNewThread = Sub(Work, Name) RunInNewThread(Work, Name), .RunInUi = Sub(Work) RunInUi(Work)}
    End Function
    Friend Shared Function ChooseModDownloadFolder(Target As McInstance, Selected As McInstance, Cached As String, Suitable As Func(Of McInstance, Boolean)) As String
        Return DetailCore.ChooseModDownloadFolder(Target, Selected, Cached, Suitable)
    End Function
    Private Property DownloadTargetInstance As McInstance
        Get
            Return Service.DownloadTargetInstance
        End Get
        Set(value As McInstance)
            Service.DownloadTargetInstance = value
        End Set
    End Property
    Private Shared Sub DownloadHint(Text As String, Kind As Integer)
        Hint(Text, If(Kind = 1, HintType.Red, If(Kind = 2, HintType.Green, HintType.Blue)))
    End Sub
    Private Shared Sub RefreshCurrentResources()
        If TypeOf FrmMain?.PageRight Is PageInstanceResources Then DirectCast(FrmMain.PageRight, PageInstanceResources).RefreshResources()
    End Sub
    Private Shared Function CreateNameValidator(Folder As String) As PCLCS.DownloadNameValidator
        Dim Rule As New ValidateFolderName(Folder)
        Return New PCLCS.DownloadNameValidator With {.Validate = Function(Name) Rule.Validate(Name),
            .Prompt = Function(DefaultInput) MyMsgBoxInput("输入版本名称", "", DefaultInput, New Collection(Of Validate) From {Rule})}
    End Function
    Private Shared Function CreateFilterButton(Text As String, Changed As Action(Of String)) As FrameworkElement
        Dim Button As New MyRadioButton With {.Text = Text, .Margin = New Thickness(2, 0, 2, 0), .ColorType = MyRadioButton.ColorState.Highlight}
        Button.LabText.Margin = New Thickness(-2, 0, 10, 0)
        AddHandler Button.Check, Sub(Sender As MyRadioButton, User As Boolean) Changed(Sender.Text)
        Return Button
    End Function
    Private Shared Function CreateVersionCard(Title As String, Stack As StackPanel) As MyCard
        Dim Card As New MyCard With {.Title = Title, .Margin = New Thickness(0, 0, 0, 15)}
        Card.Children.Add(Stack)
        Card.SwapControl = Stack
        Return Card
    End Function
    Private Shared Function CreateVersionRow(Version As ResourceVersion, Click As Action(Of FrameworkElement, EventArgs), SaveAs As Action(Of Object, EventArgs), BadDisplay As Boolean) As FrameworkElement
        Dim Save As MyIconButton.ClickEventHandler = Nothing
        If SaveAs IsNot Nothing Then Save = Sub(Sender, Args) SaveAs(Sender, Args)
        Return Version.ToListItem(Sub(Sender, Args) Click(Sender, Args), Save, IsBadDisplay:=BadDisplay)
    End Function

    Private Class CardSorter
        Inherits PCLCS.ResourceFileCardSorter
        Public Sub New(Optional Topmost As String = "")
            MyBase.New(Topmost)
        End Sub
    End Class
    Private Property Project As ResourceProject
        Get
            Return Service.Project
        End Get
        Set(value As ResourceProject)
            Service.Project = value
        End Set
    End Property
    Private Property TargetResourceType As ResourceTypes
        Get
            Return Service.TargetResourceType
        End Get
        Set(value As ResourceTypes)
            Service.TargetResourceType = value
        End Set
    End Property
    Private Property TargetVersion As String
        Get
            Return Service.TargetVersion
        End Get
        Set(value As String)
            Service.TargetVersion = value
        End Set
    End Property
    Private Property TargetLoaders As ModLoaders
        Get
            Return Service.TargetLoaders
        End Get
        Set(value As ModLoaders)
            Service.TargetLoaders = value
        End Set
    End Property
    Public Sub LoadPageArguments() Handles Me.Loaded
        Service.LoadPageArguments()
    End Sub
    Private Sub PageDownloadResourceDetail_Inited(sender As Object, e As EventArgs) Handles Me.Initialized
        Service.Initialized()
    End Sub
    Private Sub Load_State(sender As Object, state As MyLoading.MyLoadingState, oldState As MyLoading.MyLoadingState) Handles Load.StateChanged
        Service.LoadState()
    End Sub
    Private Function GetResults() As List(Of ResourceVersion)
        Return Service.GetResults()
    End Function
    Private Sub Load_OnFinish()
        Service.LoadOnFinish()
    End Sub
    Private Sub UpdateFilterResult()
        Service.UpdateFilterResult()
    End Sub
    Private Sub InitializeStackResourceVersions(Stack As StackPanel)
        Service.InitializeStackResourceVersions(Stack)
    End Sub
    Private Function GetGroupedVersionName(Name As String, GroupedByDrop As Boolean, FoldOld As Boolean) As String
        Return DetailCore.GetGroupedVersionName(Name, GroupedByDrop, FoldOld)
    End Function
    Public Sub Init() Handles Me.PageEnter
        Service.Init()
    End Sub
    Public Sub Install_Click(sender As MyListItem, e As EventArgs)
        Service.InstallClick(sender, e)
    End Sub
    Public Sub Save_Click(sender As Object, e As EventArgs)
        Service.SaveClick(sender, e)
    End Sub
    Friend Shared Function CreateMapInstallLoader(Target As String, SavesFolder As String) As LoaderTask(Of Integer, String)
        Return DirectCast(DetailCore.CreateMapInstallLoader(CreateDownloadHost(), Target, SavesFolder), LoaderTask(Of Integer, String))
    End Function
    Private Sub BtnIntroWeb_Click(sender As Object, e As EventArgs) Handles BtnIntroWeb.Click
        Service.IntroWeb()
    End Sub
    Private Sub BtnIntroWiki_Click(sender As Object, e As EventArgs) Handles BtnIntroWiki.Click
        Service.IntroWiki()
    End Sub
    Private Sub BtnIntroCopy_Click(sender As Object, e As EventArgs) Handles BtnIntroCopy.Click
        Service.IntroCopy()
    End Sub
    Private Sub HintAlternative_Click() Handles HintAlternative.Click
        Service.AlternativeClick()
    End Sub
    Private Class ResourceDetailLoaders
        Implements PCLCS.IMinecraftInstallLoaders(Of LoaderBase, NetFile)
        Private Shared Function ApplyOptions(Loader As LoaderBase, Options As PCLCS.MinecraftDownloadLoaderSettings) As LoaderBase
            If Options.ProgressWeight.HasValue Then Loader.ProgressWeight = Options.ProgressWeight.Value
            If Options.Show.HasValue Then Loader.Show = Options.Show.Value
            If Options.Block.HasValue Then Loader.Block = Options.Block.Value
            Return Loader
        End Function
        Public Function NewTask(Of TInput, TOutput)(Name As String, Work As Action(Of PCLCS.MinecraftInstallTask(Of TInput, TOutput, LoaderBase)),
            Options As PCLCS.MinecraftDownloadLoaderSettings) As LoaderBase Implements PCLCS.IMinecraftInstallLoaders(Of LoaderBase, NetFile).NewTask
            Return ApplyOptions(New LoaderTask(Of TInput, TOutput)(Name, Sub(Task) Work(WrapTask(Of TInput, TOutput)(Task))), Options)
        End Function
        Public Function WrapTask(Of TInput, TOutput)(Loader As LoaderBase) As PCLCS.MinecraftInstallTask(Of TInput, TOutput, LoaderBase) Implements PCLCS.IMinecraftInstallLoaders(Of LoaderBase, NetFile).WrapTask
            Dim Task = DirectCast(Loader, LoaderTask(Of TInput, TOutput))
            Return New PCLCS.MinecraftInstallTask(Of TInput, TOutput, LoaderBase) With {
                .Native = Task, .ReadInput = Function() Task.Input,
                .ReadOutput = Function() Task.Output, .WriteOutput = Sub(Value) Task.Output = Value,
                .ReadProgress = Function() Task.Progress, .WriteProgress = Sub(Value) Task.Progress = Value,
                .ReadCanceled = Function() Task.IsCanceled, .ReadCanceledState = Function() Task.State = LoadState.Canceled,
                .Cancellation = Function() Task.CreateCancellationToken(),
                .ProgressProvider = Function(FromPercentage, ToPercentage) Task.CreateSyncProgressProvider(FromPercentage, ToPercentage),
                .Cancel = AddressOf Task.Cancel}
        End Function
        Public Function Combo(Of TInput)(Name As String, Steps As List(Of LoaderBase), Options As PCLCS.MinecraftDownloadLoaderSettings) As LoaderBase Implements PCLCS.IMinecraftInstallLoaders(Of LoaderBase, NetFile).Combo
            Return ApplyOptions(New LoaderCombo(Of TInput)(Name, Steps), Options)
        End Function
        Public Function Download(Name As String, Files As List(Of NetFile), Options As PCLCS.MinecraftDownloadLoaderSettings) As LoaderBase Implements PCLCS.IMinecraftInstallLoaders(Of LoaderBase, NetFile).Download
            Return ApplyOptions(New LoaderDownload(Name, Files), Options)
        End Function
        Public Sub Start(Of TInput)(Loader As LoaderBase, Input As TInput) Implements PCLCS.IMinecraftInstallLoaders(Of LoaderBase, NetFile).Start
            DirectCast(Loader, LoaderCombo(Of TInput)).Start(Input)
        End Sub
        Public Sub AddTaskbar(Of TInput)(Loader As LoaderBase) Implements PCLCS.IMinecraftInstallLoaders(Of LoaderBase, NetFile).AddTaskbar
            LoaderTaskbarAdd(DirectCast(Loader, LoaderCombo(Of TInput)))
        End Sub
        Public Sub OnStateChanged(Loader As LoaderBase, Changed As Action(Of LoaderBase)) Implements PCLCS.IMinecraftInstallLoaders(Of LoaderBase, NetFile).OnStateChanged
            Loader.OnStateChanged = Changed
        End Sub
        Public Function Name(Loader As LoaderBase) As String Implements PCLCS.IMinecraftInstallLoaders(Of LoaderBase, NetFile).Name
            Return Loader.Name
        End Function
        Public Function State(Loader As LoaderBase) As Integer Implements PCLCS.IMinecraftInstallLoaders(Of LoaderBase, NetFile).State
            Return CInt(Loader.State)
        End Function
        Public Function ErrorValue(Loader As LoaderBase) As Exception Implements PCLCS.IMinecraftInstallLoaders(Of LoaderBase, NetFile).Error
            Return Loader.Error
        End Function
        Public Function Children(Loader As LoaderBase) As IEnumerable(Of LoaderBase) Implements PCLCS.IMinecraftInstallLoaders(Of LoaderBase, NetFile).Children
            Return DirectCast(Loader, LoaderCombo(Of String)).GetLoaderList()
        End Function
        Public Function TaskbarNames() As IEnumerable(Of String) Implements PCLCS.IMinecraftInstallLoaders(Of LoaderBase, NetFile).TaskbarNames
            Return LoaderTaskbar.Select(Function(Loader) Loader.Name)
        End Function
    End Class
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
