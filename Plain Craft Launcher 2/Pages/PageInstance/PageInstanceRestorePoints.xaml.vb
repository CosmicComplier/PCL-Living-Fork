Imports HistoryController = PCLCS.EnvironmentHistoryPageController(Of PCL.ModMinecraft.McInstance)
Imports HistoryView = PCLCS.EnvironmentHistoryPageView(Of PCL.ModMinecraft.McInstance)
Imports HistoryHost = PCLCS.EnvironmentHistoryPageHost(Of PCL.ModMinecraft.McInstance)

Public Class PageInstanceRestorePoints
    Implements IRefreshable
    Private ReadOnly Controller As HistoryController
    Private ReadOnly PointsLoader As LoaderTask(Of String, List(Of JObject))
    Private ReadOnly HistoryChangedHandler As EnvironmentHistoryUi.ChangedEventHandler = AddressOf Refresh

    Public Sub New()
        InitializeComponent()
        Controller = New HistoryController(CreateView(), CreateHost())
        PointsLoader = New LoaderTask(Of String, List(Of JObject))("读取还原点", Sub(Task) Task.Output = EnvironmentHistory.Points(Task.Input))
        Init()
    End Sub
    Private Function CreateView() As HistoryView
        Return New HistoryView With {
            .Root = Me, .CardRetention = CardRetention, .PanRetention = PanRetention, .CardList = CardList,
            .RestoreButton = BtnRestore, .Entries = PanEntries, .Empty = LabEmpty, .Select = LabSelect,
            .Search = Function() SearchBox.Text, .SetSearch = Sub(Value) SearchBox.Text = Value,
            .RetentionText = Function() TextRetention.Text, .SetRetentionText = Sub(Value) TextRetention.Text = Value,
            .SetAllText = Sub(Value) FilterAll.Text = Value, .SetHealthyText = Sub(Value) FilterHealthy.Text = Value,
            .AutoCleanup = Function() CheckAutoCleanup.Checked, .SetAutoCleanup = Sub(Value) CheckAutoCleanup.Checked = Value,
            .RetentionHasFocus = Function() TextRetention.IsKeyboardFocusWithin, .SearchHasFocus = Function() SearchBox.IsKeyboardFocusWithin,
            .CheckAll = Sub() FilterAll.Checked = True, .ForceRetentionResize = Sub() CardRetention.TriggerForceResize(),
            .ScrollHome = Sub() PanBack.ScrollToHome(),
            .CreateRow = Function() New RowAdapter(New MyLocalModItem With {.SelectionBar = CardSelect}),
            .AdaptRow = Function(Sender) New RowAdapter(DirectCast(Sender, MyLocalModItem)),
            .CreateIcon = AddressOf CreateIcon,
            .RestoreLogo = Logo.IconButtonRefresh, .DetailsLogo = Logo.IconButtonInfo,
            .OpenLogo = Logo.IconButtonOpen, .DeleteLogo = Logo.IconButtonDelete,
            .RefreshSelectionBar =
                Function(Count, Previous, Animate)
                    LocalResourceUi.RefreshSelectionBar(CardSelect, TransSelect, Count, Previous, "Restore Point Selection " & PageUuid, Animate)
                    Return Previous
                End Function}
    End Function
    Private Function CreateHost() As HistoryHost
        Return New HistoryHost With {
            .CurrentInstance = Function() PageInstanceLeft.Instance, .VersionPath = Function(Instance) Instance.PathVersion,
            .AutoCleanup = Function(Instance) Settings.Get(Of Boolean)("VersionEnvironmentHistoryAutoCleanup", Instance:=Instance),
            .Retention = Function(Instance) Settings.Get(Of Integer)("VersionEnvironmentHistoryRetention", Instance:=Instance),
            .SetAutoCleanup = Sub(Instance, Value) Settings.Set("VersionEnvironmentHistoryAutoCleanup", Value, Instance:=Instance),
            .SetRetention = Sub(Instance, Value) Settings.Set("VersionEnvironmentHistoryRetention", Value, Instance:=Instance),
            .StartLoader = Sub(VersionFolder) PointsLoader.Start(VersionFolder, IsForceRestart:=True), .CancelLoader = Sub() PointsLoader.Cancel(),
            .LoaderFailed = Function() PointsLoader.State = LoadState.Failed, .AnimationEnabled = Function() AniControlEnabled = 0,
            .BindChanged = AddressOf BindChanged,
            .Start = Sub(Instance, Restore, PointId, DeleteIds) EnvironmentHistoryUi.Start(Instance, Restore, PointId, DeleteIds),
            .HintError = Sub(Message) Hint(Message, HintType.Red), .OpenFolder = Sub(VersionFolder) EnvironmentHistory.OpenFolder(VersionFolder),
            .ShowDetails = Sub(Text, Title) MyMsgBox(Text, Title)}
    End Function
    Private Sub BindChanged(Attach As Boolean)
        If Attach Then
            AddHandler EnvironmentHistoryUi.Changed, HistoryChangedHandler
        Else
            RemoveHandler EnvironmentHistoryUi.Changed, HistoryChangedHandler
        End If
    End Sub
    Private Shared Function CreateIcon(Logo As String, Scale As Double?, Tip As String) As PCLCS.EnvironmentHistoryPageIcon
        Dim Button As New MyIconButton With {.Logo = Logo}
        If Scale.HasValue Then Button.LogoScale = Scale.Value
        Button.ToolTip = Tip
        Return New PCLCS.EnvironmentHistoryPageIcon(Button,
            Sub(Clicked)
                AddHandler Button.Click, Sub() Clicked()
            End Sub)
    End Function
    Private NotInheritable Class RowAdapter
        Implements PCLCS.IEnvironmentHistoryPageRow
        Private ReadOnly Row As MyLocalModItem
        Public Sub New(Row As MyLocalModItem)
            Me.Row = Row
        End Sub
        Public ReadOnly Property Element As FrameworkElement Implements PCLCS.IEnvironmentHistoryPageRow.Element
            Get
                Return Row
            End Get
        End Property
        Public ReadOnly Property Point As JObject Implements PCLCS.IEnvironmentHistoryPageRow.Point
            Get
                Return Row.RestorePoint
            End Get
        End Property
        Public Property Checked As Boolean Implements PCLCS.IEnvironmentHistoryPageRow.Checked
            Get
                Return Row.Checked
            End Get
            Set(Value As Boolean)
                Row.Checked = Value
            End Set
        End Property
        Public Sub Bind(Point As JObject) Implements PCLCS.IEnvironmentHistoryPageRow.Bind
            Row.BindRestorePoint(Point)
        End Sub
        Public Sub SetButtons(Buttons As List(Of PCLCS.EnvironmentHistoryPageIcon)) Implements PCLCS.IEnvironmentHistoryPageRow.SetButtons
            Row.Buttons = Buttons.Select(Function(Button) DirectCast(Button.Element, MyIconButton)).ToList()
        End Sub
        Public Sub Subscribe(Click As Action, Changed As Action(Of Object)) Implements PCLCS.IEnvironmentHistoryPageRow.Subscribe
            AddHandler Row.Click, Sub() Click()
            AddHandler Row.Changed, Sub(Sender, e) Changed(Sender)
        End Sub
    End Class

    '同名状态入口仅转发到控制器，原条目与选择集合没有页面副本。
    Private Property Entries As List(Of JObject)
        Get
            Return Controller.Entries
        End Get
        Set(Value As List(Of JObject))
            Controller.Entries = Value
        End Set
    End Property
    Private ReadOnly Property SelectedIds As HashSet(Of String)
        Get
            Return Controller.SelectedIds
        End Get
    End Property
    Private Property Rendering As Boolean
        Get
            Return Controller.Rendering
        End Get
        Set(Value As Boolean)
            Controller.Rendering = Value
        End Set
    End Property
    Private Property BarCount As Integer
        Get
            Return Controller.BarCount
        End Get
        Set(Value As Integer)
            Controller.BarCount = Value
        End Set
    End Property
    Private Property HealthyOnly As Boolean
        Get
            Return Controller.HealthyOnly
        End Get
        Set(Value As Boolean)
            Controller.HealthyOnly = Value
        End Set
    End Property
    Private Property Owner As McInstance
        Get
            Return Controller.Owner
        End Get
        Set(Value As McInstance)
            Controller.Owner = Value
        End Set
    End Property
    Private Property PendingFocusId As String
        Get
            Return Controller.PendingFocusId
        End Get
        Set(Value As String)
            Controller.PendingFocusId = Value
        End Set
    End Property
    Private Property BindingRetention As Boolean
        Get
            Return Controller.BindingRetention
        End Get
        Set(Value As Boolean)
            Controller.BindingRetention = Value
        End Set
    End Property
    Private Property RetentionDirty As Boolean
        Get
            Return Controller.RetentionDirty
        End Get
        Set(Value As Boolean)
            Controller.RetentionDirty = Value
        End Set
    End Property

    Friend Sub FocusPoint(Id As String)
        Controller.FocusPoint(Id)
    End Sub
    Private Sub Init()
        DisabledPageAnimControls.Add(CardSelect)
        LocalResourceUi.ApplySelectionIcons(BtnRestore, BtnDelete, BtnDeselect)
        BtnRestore.Logo = Logo.IconButtonRefresh
        FilterAll.LabText.Margin = New Thickness(-2, 0, 8, 0)
        FilterHealthy.LabText.Margin = New Thickness(-2, 0, 8, 0)
        PageLoaderInit(Load, PanLoad, PanBack, Nothing, PointsLoader, AddressOf LoadedPoints, AutoRun:=False)
    End Sub
    Private Sub PageLoaded() Handles Me.Loaded
        Controller.PageLoaded()
    End Sub
    Private Sub PageUnloaded() Handles Me.Unloaded
        Controller.PageUnloaded()
    End Sub
    Public Sub Refresh() Implements IRefreshable.Refresh
        Controller.Refresh()
    End Sub
    Private Sub BindRetention()
        Controller.BindRetention()
    End Sub
    Private Sub AutoCleanupChanged(Sender As Object, User As Boolean) Handles CheckAutoCleanup.Change
        Controller?.AutoCleanupChanged(User)
    End Sub
    Private Sub RetentionEdited() Handles TextRetention.TextChanged
        Controller?.RetentionEdited()
    End Sub
    Private Sub RetentionLostFocus() Handles TextRetention.LostKeyboardFocus
        Controller.CommitRetention()
    End Sub
    Private Sub RetentionKey(Sender As Object, e As KeyEventArgs) Handles TextRetention.PreviewKeyDown
        Controller.RetentionKey(Sender, e)
    End Sub
    Friend Function CommitRetention() As Boolean
        Return Controller.CommitRetention()
    End Function
    Private Sub LoadedPoints()
        Controller.LoadedPoints(Function() PointsLoader.Output)
    End Sub
    Private Sub SearchChanged() Handles SearchBox.TextChanged
        Controller?.RenderList()
    End Sub
    Private Sub FilterChanged(Sender As MyRadioButton, User As Boolean) Handles FilterAll.Check, FilterHealthy.Check
        Controller?.FilterChanged(Sender Is FilterHealthy)
    End Sub
    Friend Sub RenderList()
        Controller.RenderList()
    End Sub
    Private Function PointButtons(Point As JObject) As List(Of MyIconButton)
        Return Controller.PointButtons(Point).Select(Function(Button) DirectCast(Button.Element, MyIconButton)).ToList()
    End Function
    Private Sub SelectionChanged(Sender As Object, e As RouteEventArgs)
        Controller.SelectionChanged(Sender)
    End Sub
    Private Sub RefreshSelection()
        Controller.RefreshSelection()
    End Sub
    Friend Sub ChangeAllSelected(Value As Boolean)
        Controller.ChangeAllSelected(Value)
    End Sub
    Private Sub SelectAll() Handles BtnSelectAll.Click
        Controller.SelectAll()
    End Sub
    Private Sub Deselect() Handles BtnDeselect.Click
        Controller.Deselect()
    End Sub
    Private Sub SelectionKeys(Sender As Object, e As KeyEventArgs) Handles Me.PreviewKeyDown
        Controller.SelectionKeys(Sender, e)
    End Sub
    Private Sub DeleteSelected() Handles BtnDelete.Click
        Controller.DeleteSelected()
    End Sub
    Private Sub CreatePoint() Handles BtnCreate.Click
        Controller.CreatePoint()
    End Sub
    Private Sub RestorePoint() Handles BtnRestore.Click
        Controller.RestorePoint()
    End Sub
    Private Sub OpenFolder() Handles BtnFolder.Click
        Controller.OpenFolder()
    End Sub
    Private Sub Retry() Handles Load.Click
        Controller.Retry()
    End Sub
End Class
