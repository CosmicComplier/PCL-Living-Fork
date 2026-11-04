Imports RowCore = PCLCS.ResourceItemController(Of PCL.ResourceProject, PCL.ResourceSearcher.SearchResult, PCL.ResourceSearcher.SearchRequest, PCL.ModMinecraft.McInstance, PCL.ModMinecraft.McInstance.McInstanceState, PCL.ModMinecraft.McInstanceCardType, PCL.ModAnimation.AniData)

Public Class MyResourceItem
    Public Uuid As Integer = GetUuid()
    Private Controller As RowCore
    Private ReadOnly Property Service As RowCore
        Get
            If Controller Is Nothing Then Controller = New RowCore(CreateHost(), CreateAnimations())
            Return Controller
        End Get
    End Property
    Private Function CreateHost() As PCLCS.ResourceItemHost(Of ResourceProject, ResourceSearcher.SearchRequest, McInstance)
        Return New PCLCS.ResourceItemHost(Of ResourceProject, ResourceSearcher.SearchRequest, McInstance) With {
            .View = Function() New PCLCS.ResourceItemView With {.Root = Me, .Title = LabTitle, .RawTitle = LabTitleRaw, .Info = LabInfo, .InfoToolTip = ToolTipInfo, .Tags = PanTags},
            .Uuid = Function() Uuid, .CurrentPage = Function() CInt(FrmMain.PageCurrent.Page),
            .DownloadPage = CInt(FormMain.PageType.Download), .ResourceDetailPage = CInt(FormMain.PageType.ResourceDetail), .InstanceSetupPage = CInt(FormMain.PageType.InstanceSetup),
            .DetailAvailable = Function() FrmDownloadResourceDetail IsNot Nothing, .DownloadAvailable = Function() FrmDownloadLeft IsNot Nothing,
            .ExpandedCards = Function() FrmDownloadResourceDetail.PanResults.Children.Cast(Of MyCard)().Select(Function(Card) (Card.Title, Card.IsSwapped)),
            .CurrentSearch = AddressOf ReadCurrentSearch, .CurrentDownloadContext = AddressOf ReadCurrentDownloadContext,
            .CurrentArguments = Function() TryCast(FrmMain.PageCurrent.Additional, Object()),
            .CurrentInstance = Function() PageInstanceLeft.Instance,
            .OpenDetail = Sub(Arguments) FrmMain.PageChange(New FormMain.PageStackData With {.Page = FormMain.PageType.ResourceDetail, .Additional = Arguments}),
            .RaiseClick = AddressOf RaiseNativeClick, .TextTrimmed = AddressOf IsTextTrimmed,
            .Brush = Function(Color) New MyColor(Color), .PixelSize = Function(Value) GetWPFSize(Value)}
    End Function
    Private Shared Function ReadCurrentDownloadContext() As (Search As ResourceSearcher.SearchRequest, Instance As McInstance)
        Dim ResourcePage = TryCast(FrmDownloadLeft.PageGet(FrmMain.PageCurrentSub).FindName("Content"), PageResource)
        Return (ResourcePage?.Loader?.Input, ResourcePage?.DownloadTargetInstance)
    End Function
    Private Function ReadCurrentSearch() As ResourceSearcher.SearchRequest
        Dim ResourcePage = TryCast(FrmDownloadLeft.PageGet(FrmMain.PageCurrentSub).FindName("Content"), PageResource)
        Return ResourcePage?.Loader?.Input
    End Function
    Private Sub RaiseNativeClick(Sender As Object, Args As MouseButtonEventArgs)
        RaiseEvent Click(Sender, Args)
    End Sub
    Public Property Title As String
        Get
            Return Service.Title
        End Get
        Set(value As String)
            Service.Title = value
        End Set
    End Property
    Public Property SubTitle As String
        Get
            Return Service.SubTitle
        End Get
        Set(value As String)
            Service.SubTitle = value
        End Set
    End Property
    Public Property Description As String
        Get
            Return Service.Description
        End Get
        Set(value As String)
            Service.Description = value
        End Set
    End Property
    Private Sub LabInfo_MouseEnter(sender As Object, e As MouseEventArgs) Handles LabInfo.MouseEnter
        Service.ShowDescriptionToolTip()
    End Sub
    Public WriteOnly Property Tags As List(Of String)
        Set(Tags As List(Of String))
            Service.SetTags(Tags)
        End Set
    End Property
    Public Event Click(sender As Object, e As MouseButtonEventArgs)
    Private Sub Button_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles Me.PreviewMouseLeftButtonUp
        Service.MouseUp(sender, e)
    End Sub
    Private Sub MyResourceItem_Click(sender As MyResourceItem, e As EventArgs) Handles Me.Click
        Service.Click(sender)
    End Sub
    Friend Shared Function BuildDetailArguments(Project As ResourceProject, Page As FormMain.PageType, Search As ResourceSearcher.SearchRequest, Existing As Object(), Instance As McInstance) As Object()
        Return RowCore.BuildDetailArguments(Project, CInt(Page), Search, Existing, Instance, CInt(FormMain.PageType.Download), CInt(FormMain.PageType.ResourceDetail), CInt(FormMain.PageType.InstanceSetup))
    End Function
    Private Sub Button_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles Me.PreviewMouseLeftButtonDown
        Service.MouseDown()
    End Sub
    Private Sub Button_MouseLeave(sender As Object, e As Object) Handles Me.MouseLeave, Me.PreviewMouseLeftButtonUp
        Service.MouseLeave()
    End Sub
    Public ReadOnly Property RectBack As Border
        Get
            Return Service.RectBack
        End Get
    End Property
    Public Property CanInteraction As Boolean
        Get
            Return Service.CanInteraction
        End Get
        Set(AutoPropertyValue As Boolean)
            Service.CanInteraction = AutoPropertyValue
        End Set
    End Property
    Public Sub RefreshColor(sender As Object, e As EventArgs) Handles Me.MouseEnter, Me.MouseLeave, Me.MouseLeftButtonDown, Me.MouseLeftButtonUp
        Service.RefreshColor()
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
