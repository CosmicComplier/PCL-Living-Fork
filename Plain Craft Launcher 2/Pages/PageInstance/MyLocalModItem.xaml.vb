Imports System.Windows.Forms
Imports PCLLF.Presentation.Mods
Imports PCLLF.Presentation.Controls
Imports NativeItem = PCLLF.Presentation.Mods.LegacyLocalModItem(Of PCL.LocalResourceFile, PCL.ResourceProject, PCL.ResourceVersion, PCL.InstanceResourceEntry, PCL.InstanceResourceOnlineInfo, PCL.MyIconButton)
Imports NativeHost = PCLLF.Presentation.Mods.LegacyLocalModItemHost(Of PCL.LocalResourceFile, PCL.ResourceProject, PCL.ResourceVersion, PCL.InstanceResourceEntry, PCL.InstanceResourceOnlineInfo, PCL.MyIconButton)

Public Class MyLocalModItem
    Public Uuid As Integer = GetUuid()
    Friend ResourceEntry As InstanceResourceEntry
    Friend RestorePoint As JObject
    Private RestorePointDisplay As PCLCS.EnvironmentHistoryPagePointView
    Friend ResourceUpdateRequested As Action
    Friend ResourceDetailsRequested As Action
    Friend SelectionBar As FrameworkElement
    Public ButtonHandler As Action(Of MyLocalModItem, EventArgs)
    Public ButtonStack As FrameworkElement
    Friend ReadOnly Controller As NativeItem
    Public Sub New()
        PresentationRuntime.EnsureConfigured()
        Controller = New NativeItem(CreateView(), CreateHost(), ControlServices.Current.Animations)
        InitializeComponent()
        LazyLoadBehavior.OnFirstEnterScrollViewerViewport(Me, AddressOf FirstEnterScrollViewerViewport)
    End Sub
    Private Function CreateView() As LegacyLocalModItemView
        Return New LegacyLocalModItemView With {
            .LoaderWarningBorder = Function() LoaderWarningBorder, .Root = Function() Me, .Title = Function() LabTitle, .Subtitle = Function() LabSubtitle, .Description = Function() LabInfo,
            .Tags = Function() PanTags, .UpdateButton = Function() BtnUpdate, .ResourceLogoElement = Function() ResourceLogo,
            .PathLogoElement = Function() PathLogo, .ResourceLogo = Function() ResourceLogo,
            .PaddingRight = Function() ColumnPaddingRight, .TitleColumn = Function() ColumnTitle, .SubtitleColumn = Function() ColumnSubtitle, .ExtendColumn = Function() ColumnExtend,
            .SetPathLogo = Sub(Value) PathLogo.Source = Value, .SetPathFallback = Sub(Value) PathLogo.FallbackSource = Value}
    End Function
    Private Function CreateHost() As NativeHost
        Return New NativeHost With {
            .Uuid = Function() Uuid, .NameStyle = Function() Settings.Get(Of Integer)("ToolModLocalNameStyle"),
            .Entry = Function() Entry, .SetEntry = Sub(Value) Entry = Value, .Resource = Function() ResourceEntry, .SetResource = Sub(Value) ResourceEntry = Value,
            .Point = Function() RestorePoint, .SetPoint = Sub(Value) RestorePoint = Value, .PointView = AddressOf RestorePointView,
            .Icon = Function(Value) Value.Icon, .ImagePath = Function() PathImage, .Bitmap = Function(Address) CType(New MyBitmap(Address), ImageSource), .WpfSize = Function(Value) GetWPFSize(Value),
            .VisibleInForm = Function() IsVisibleInForm(), .IsLocalItemDirectlyOver = Function() TypeOf Mouse.DirectlyOver Is MyLocalModItem,
            .TitleTrimmed = Function() LabTitle.IsTextTrimmed, .SubtitleTrimmed = Function() LabSubtitle.IsTextTrimmed,
            .RaiseChanged = AddressOf RaiseChanged, .RaiseCheck = AddressOf RaiseCheck, .RaiseClick = AddressOf RaiseClick,
            .ButtonStack = Function() ButtonStack, .SetButtonStack = Sub(Value) ButtonStack = Value, .SelectionBar = Function() SelectionBar, .DefaultSelectionBar = Function() FrmInstanceMod?.CardSelect,
            .HasButtonHandler = Function() ButtonHandler IsNot Nothing, .InvokeButtonHandler = Sub(Sender, Args) ButtonHandler(CType(Sender, MyLocalModItem), Args), .ClearButtonHandler = Sub() ButtonHandler = Nothing,
            .ResourceDetails = Sub() ResourceDetailsRequested?.Invoke(), .ResourceUpdate = Sub() ResourceUpdateRequested?.Invoke(),
            .Controller = Function(Element) CType(Element, MyLocalModItem).Controller, .UpdateMods = Sub(Values) FrmInstanceMod.UpdateMods(Values), .Website = AddressOf OpenWebsite,
            .Message = Function(Text, Caption, Button1, Button2, Button3) MyMsgBox(Text, Caption, Button1, Button2, Button3),
            .FormatTime = AddressOf StringUtils.FormatTimeSpan}
    End Function
    Private Function RestorePointView() As PCLCS.EnvironmentHistoryPagePointView
        If RestorePointDisplay Is Nothing Then
            RestorePointDisplay = New PCLCS.EnvironmentHistoryPagePointView With {
                .Root = Me, .UpdateButton = BtnUpdate, .ResourceLogo = ResourceLogo, .PathLogo = PathLogo,
                .Point = Function() RestorePoint,
                .SetTitle = Sub(Value) Title = Value, .SetSubTitle = Sub(Value) SubTitle = Value,
                .SetDescription = Sub(Value) Description = Value, .SetTags = Sub(Value) Tags = Value,
                .Title = Function() Title, .SubTitle = Function() SubTitle, .Description = Function() Description,
                .SetPathLogo = Sub(Value) PathLogo.Source = Value, .ImageRoot = Function() PathImage}
        End If
        Return RestorePointDisplay
    End Function

    Friend ReadOnly Property LoaderWarning As String
        Get
            Return Controller.LoaderWarning
        End Get
    End Property
    Friend Sub RefreshLoaderWarning(Folder As String, Context As ModDependencyInspector.InspectionContext)
        Controller.RefreshLoaderWarning(Function() ModDependencyInspector.LoaderWarningFor(Folder, Entry.File.FullName, Context))
    End Sub
    Private Sub SetLoaderWarning(Message As String)
        Controller.SetLoaderWarning(Message)
    End Sub
    Friend Sub BindRestorePoint(Value As JObject)
        Controller.BindRestorePoint(Value)
    End Sub
    Friend Sub BindResource(Value As InstanceResourceEntry, Info As InstanceResourceOnlineInfo)
        Controller.BindResource(Value, Info)
    End Sub
    Public Property Title As String
        Get
            Return Controller.Title
        End Get
        Set(value As String)
            Controller.Title = value
        End Set
    End Property
    Public Property SubTitle As String
        Get
            Return Controller.SubTitle
        End Get
        Set(value As String)
            Controller.SubTitle = value
        End Set
    End Property
    Public Property Description As String
        Get
            Return Controller.Description
        End Get
        Set(value As String)
            Controller.Description = value
        End Set
    End Property
    Public WriteOnly Property Tags As List(Of String)
        Set(Tags As List(Of String))
            Controller.SetTags(Tags)
        End Set
    End Property
    Public Property Entry As LocalResourceFile
        Get
            Return Tag
        End Get
        Set(value As LocalResourceFile)
            Tag = value
        End Set
    End Property
    Public Event Click(sender As Object, e As MouseButtonEventArgs)
    Public Event Check(sender As Object, e As RouteEventArgs)
    Public Event Changed(sender As Object, e As RouteEventArgs)
    Private Sub RaiseClick(sender As Object, e As MouseButtonEventArgs)
        RaiseEvent Click(sender, e)
    End Sub
    Private Function RaiseCheck() As Boolean
        Dim Args As New RouteEventArgs(False)
        RaiseEvent Check(Me, Args)
        Return Args.Handled
    End Function
    Private Function RaiseChanged() As Boolean
        Dim Args As New RouteEventArgs(False)
        RaiseEvent Changed(Me, Args)
        Return Args.Handled
    End Function
    Private Sub Button_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles Me.PreviewMouseLeftButtonUp
        Controller.MouseUp(sender, e)
    End Sub
    Private Sub Button_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles Me.PreviewMouseLeftButtonDown
        Controller.MouseDown()
    End Sub
    Private Sub Button_MouseLeave(sender As Object, e As Object) Handles Me.MouseLeave, Me.PreviewMouseLeftButtonUp
        Controller.MouseLeave()
    End Sub
    Private Sub Button_MouseSwipeStart(sender As Object, e As Object) Handles Me.MouseLeftButtonDown
        Controller.SwipeStart()
    End Sub
    Private Sub Button_MouseSwipe(sender As Object, e As Object) Handles Me.MouseEnter, Me.MouseLeave, Me.MouseLeftButtonUp
        Controller.Swipe(e)
    End Sub
    Public Property Checked As Boolean
        Get
            Return Controller.Checked
        End Get
        Set(value As Boolean)
            Controller.Checked = value
        End Set
    End Property
    Public ReadOnly Property RectBack As Border
        Get
            Return Controller.RectBack
        End Get
    End Property
    Public Property Buttons As IEnumerable(Of MyIconButton)
        Get
            Return Controller.Buttons
        End Get
        Set(value As IEnumerable(Of MyIconButton))
            Controller.Buttons = value
        End Set
    End Property
    Public ReadOnly Property RectCheck As Border
        Get
            Return Controller.RectCheck
        End Get
    End Property
    Private Function GetUpdateCompareDescription() As String
        Return Controller.GetUpdateCompareDescription()
    End Function
    Private Sub FirstEnterScrollViewerViewport()
        Controller.FirstViewport()
    End Sub
    Public Sub Refresh()
        Controller.Refresh()
    End Sub
    Public Sub RefreshColor(sender As Object, e As EventArgs) Handles Me.MouseEnter, Me.MouseLeave, Me.MouseLeftButtonDown, Me.MouseLeftButtonUp, Me.Changed
        Controller.RefreshColor(sender, e)
    End Sub
    Private Sub InitLate(sender As Object, e As EventArgs)
        Controller.InitLate(sender, e)
    End Sub
    Private Sub BtnUpdate_PreviewMouseRightButtonUp(sender As Object, e As MouseButtonEventArgs) Handles BtnUpdate.PreviewMouseRightButtonUp
        e.Handled = True
        Controller.ShowUpdateLog()
    End Sub
    Private Sub ShowUpdateLog()
        Controller.ShowUpdateLog()
    End Sub
    Private Sub BtnUpdate_Click(sender As Object, e As EventArgs) Handles BtnUpdate.Click
        Controller.UpdateClick()
    End Sub
    Private Sub ReleaseResourceInteraction() Handles Me.Unloaded
        Controller.ReleaseResourceInteraction()
    End Sub
    Private Sub PanTitle_SizeChanged() Handles PanTitle.SizeChanged
        Controller.ResizeTitle()
    End Sub
End Class
