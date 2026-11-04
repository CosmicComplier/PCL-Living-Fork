Imports System.Windows.Markup
Imports SearchCore = PCLCS.ResourceSearchPageController(Of PCL.ModMinecraft.McInstance, PCL.ModMinecraft.McInstance.McInstanceState, PCL.ModMinecraft.McInstanceCardType, PCL.ResourceProject, PCL.ResourceSearcher.SearchResult, PCL.ResourceSearcher.SearchRequest)

<ContentProperty("SearchTags")>
Public Class PageResource
    Public Shared TargetInstance As McInstance = Nothing
    Public Shared TargetName As String = Nothing
    Public Loader As LoaderTask(Of ResourceSearcher.SearchRequest, Integer) = ResourceSearcher.GetLoader(AddressOf LoaderInput)
    Public Storage As New ResourceSearcher.SearchResult
    Public Const PAGE_SIZE As Integer = 40
    Public Page As Integer = 0
    Private Controller As SearchCore
    Private ReadOnly Property Service As SearchCore
        Get
            If Controller Is Nothing Then Controller = New SearchCore(CreateHost())
            Return Controller
        End Get
    End Property
    Private Function CreateHost() As PCLCS.ResourceSearchPageHost(Of McInstance, ResourceProject, ResourceSearcher.SearchResult, ResourceSearcher.SearchRequest)
        Return New PCLCS.ResourceSearchPageHost(Of McInstance, ResourceProject, ResourceSearcher.SearchResult, ResourceSearcher.SearchRequest) With {
            .View = AddressOf CreateView,
            .TargetInstance = Function() TargetInstance, .SetTargetInstance = Sub(Value) TargetInstance = Value,
            .TargetName = Function() TargetName, .SetTargetName = Sub(Value) TargetName = Value,
            .Storage = Function() Storage, .SetStorage = Sub(Value) Storage = Value,
            .Page = Function() Page, .SetPage = Sub(Value) Page = Value,
            .Request = Function(Type, Store, Count) New ResourceSearcher.SearchRequest(Type, Store, Count),
            .Input = Function() Loader.Input, .ShouldStart = Function(Input) Loader.ShouldStart(Input),
            .Start = Sub() Loader.Start(), .ResetFinishedTime = Sub() Loader.LastFinishedTime = 0,
            .InitializeLoader = Sub() DirectCast(Parent, MyPageRight).PageLoaderInit(Load, PanLoad, PanContent, PanAlways, Loader, AddressOf Load_OnFinish, AddressOf LoaderInput),
            .Restart = Sub() DirectCast(Parent, MyPageRight).PageLoaderRestart(),
            .BackToTop = Sub() FrmMain.BackToTop(), .InstallPack = Sub() ModpackInstall(),
            .LoaderName = Sub(Value) Loader.Name = Value,
            .Failed = Function() Loader.State = LoadState.Failed, .Error = Function() Loader.Error,
            .AllDrops = Function() AllDrops,
            .Item = Function(Content, Tag, Selected) New MyComboBoxItem With {.Content = Content, .Tag = Tag, .IsSelected = Selected},
            .ProjectItem = Function(Project, ShowVersion, ShowLoader) Project.ToResourceItem(ShowVersion, ShowLoader),
            .RunInThread = Sub(Work) RunInThread(Work), .RunInUi = Sub(Work) RunInUi(Work)}
    End Function
    Private Function CreateView() As PCLCS.ResourceSearchPageView
        Return New PCLCS.ResourceSearchPageView With {
            .Name = TextSearchName, .Version = TextSearchVersion, .Source = ComboSearchSource, .Tag = ComboSearchTag, .Loader = ComboSearchLoader,
            .InstallPack = BtnSearchInstallModPack, .Pages = CardPages, .First = BtnPageFirst, .Left = BtnPageLeft, .Right = BtnPageRight,
            .Error = HintError, .PageNumber = LabPage, .Projects = PanProjects,
            .SearchTitle = Sub(Value) PanAlways.Title = Value, .LoadingText = Sub(Value) Load.Text = Value, .ErrorText = Sub(Value) HintError.Text = Value,
            .ScrollHome = Sub() ScrollToHome(), .ScrollTop = Sub() ScrollToTop()}
    End Function
    Public ReadOnly Property SearchTags As ItemCollection
        Get
            Return ComboSearchTag.Items
        End Get
    End Property
    Public Property TypeName As String
        Get
            Return Service.TypeName
        End Get
        Set(Value As String)
            Service.TypeName = Value
        End Set
    End Property
    Public Property TypeNameSpaced As String
        Get
            Return Service.TypeNameSpaced
        End Get
        Set(Value As String)
            Service.TypeNameSpaced = Value
        End Set
    End Property
    Public Property PageType As ResourceTypes
        Get
            Return Service.PageType
        End Get
        Set(Value As ResourceTypes)
            Service.PageType = Value
        End Set
    End Property
    Private Sub PageResource_Loaded(sender As Object, e As EventArgs) Handles Me.Loaded
        Service.Loaded()
    End Sub
    Private Function LoaderInput() As ResourceSearcher.SearchRequest
        Return Service.LoaderInput()
    End Function
    Private Sub Load_OnFinish()
        Service.LoadOnFinish()
    End Sub
    Private Sub Load_State(sender As Object, state As MyLoading.MyLoadingState, oldState As MyLoading.MyLoadingState) Handles Load.StateChanged
        Service.LoadState()
    End Sub
    Private Sub BtnPageFirst_Click(sender As Object, e As RoutedEventArgs) Handles BtnPageFirst.Click
        Service.FirstPage()
    End Sub
    Private Sub BtnPageLeft_Click(sender As Object, e As RoutedEventArgs) Handles BtnPageLeft.Click
        Service.PreviousPage()
    End Sub
    Private Sub BtnPageRight_Click(sender As Object, e As RoutedEventArgs) Handles BtnPageRight.Click
        Service.NextPage()
    End Sub
    Private Sub ChangePage(NewPage As Integer)
        Service.ChangePage(NewPage)
    End Sub
    Private Sub StartNewSearch() Handles BtnSearchRun.Click
        Service.StartNewSearch()
    End Sub
    Private Sub EnterTrigger(sender As Object, e As KeyEventArgs) Handles TextSearchName.KeyDown, TextSearchVersion.KeyDown
        Service.KeyDown(e.Key)
    End Sub
    Private Sub ResetFilter() Handles BtnSearchReset.Click
        Service.ResetFilter()
    End Sub
    Private Sub TextSearchVersion_TextChanged() Handles TextSearchVersion.TextChanged
        Service.VersionTextChanged()
    End Sub
    Private Sub UpdateSearchLoaderVisibility() Handles TextSearchVersion.DropDownClosed
        Service.UpdateSearchLoaderVisibility()
    End Sub
    Private Sub BtnSearchInstallModPack_Click(sender As Object, e As EventArgs) Handles BtnSearchInstallModPack.Click
        Service.InstallExistingPack()
    End Sub
End Class
