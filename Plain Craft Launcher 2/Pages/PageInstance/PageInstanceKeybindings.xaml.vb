Imports KeybindingController = PCLCS.KeybindingPageController(Of PCL.ModMinecraft.McInstance, PCL.InstanceKeybinding)
Imports KeybindingView = PCLCS.KeybindingPageView(Of PCL.InstanceKeybinding)
Imports KeybindingHost = PCLCS.KeybindingPageHost(Of PCL.ModMinecraft.McInstance)

Public Class PageInstanceKeybindings
    Implements IRefreshable

    Private ReadOnly Controller As KeybindingController
    Private ReadOnly BindingsLoader As LoaderTask(Of String, List(Of InstanceKeybinding))
    Private KeyboardOwner As FormMain
    Private KeyboardKeyHandler As KeyEventHandler
    Private KeyboardMouseHandler As MouseButtonEventHandler

    Public Sub New()
        InitializeComponent()
        DisabledPageAnimControls.Add(CardCapture)
        BtnClear.Logo = Logo.IconButtonDelete
        BtnCancel.Logo = Logo.IconButtonCross
        BtnFilterAll.LabText.Margin = New Thickness(-2, 0, 8, 0)
        BtnFilterConflicts.LabText.Margin = New Thickness(-2, 0, 8, 0)
        Controller = New KeybindingController(CreateView(), CreateHost())
        BindingsLoader = New LoaderTask(Of String, List(Of InstanceKeybinding))("读取版本按键", AddressOf ReadBindings)
        PageLoaderInit(Load, PanLoad, PanBack, Nothing, BindingsLoader, AddressOf BindingsLoaded, AutoRun:=False)
    End Sub

    Private Function CreateView() As KeybindingView
        Return New KeybindingView With {
            .Root = Me, .CardBindings = CardBindings, .Bindings = PanBindings,
            .CancelButton = BtnCancel, .ClearButton = BtnClear, .Empty = LabEmpty, .Capture = LabCapture,
            .Search = Function() SearchBox.Text,
            .SetAllText = Sub(Value) BtnFilterAll.Text = Value,
            .SetConflictsText = Sub(Value) BtnFilterConflicts.Text = Value,
            .ScrollHome = Sub() PanBack.ScrollToHome(),
            .CreateButton = AddressOf CreateNativeButton,
            .CreateGroup = AddressOf CreateNativeGroup,
            .RefreshCaptureBar =
                Function(Count, Previous, Animate)
                    LocalResourceUi.RefreshSelectionBar(CardCapture, TransCapture, Count, Previous, "Keybinding Capture " & PageUuid, Animate)
                    Return Previous
                End Function}
    End Function
    Private Shared Function CreateNativeButton(Entry As InstanceKeybinding, Text As String, Conflict As Boolean, Tooltip As String, Clicked As Action) As PCLCS.KeybindingPageButton
        Dim Button As New MyButton With {.Text = Text, .Width = 132, .Height = 32, .Padding = New Thickness(10, 0, 10, 0), .VerticalAlignment = VerticalAlignment.Center,
            .ColorType = If(Conflict, MyButton.ColorState.Red, MyButton.ColorState.Normal), .Tag = Entry, .ToolTip = Tooltip}
        AddHandler Button.Click, Sub() Clicked()
        Return New PCLCS.KeybindingPageButton(Button, Sub(Value) Button.Text = Value,
            Sub(Value) Button.ColorType = If(Value = PCLCS.KeybindingPageButtonState.Capturing, MyButton.ColorState.Highlight,
                If(Value = PCLCS.KeybindingPageButtonState.Conflict, MyButton.ColorState.Red, MyButton.ColorState.Normal)))
    End Function
    Private Shared Function CreateNativeGroup(Title As String, Normalized As String) As PCLCS.KeybindingPageGroup
        Dim Group As New MyCard With {.Title = Title, .Margin = New Thickness(0, 8, 0, 12), .Tag = Normalized}
        Return New PCLCS.KeybindingPageGroup(Group,
            Sub(Rows)
                Dim Header = Group.MainTextBlock
                Group.Children.Add(Rows)
            End Sub)
    End Function
    Private Function CreateHost() As KeybindingHost
        Return New KeybindingHost With {
            .CurrentInstance = Function() PageInstanceLeft.Instance,
            .GameFolder = Function(Value) Value.PathIndie,
            .VersionPath = Function(Value) Value.PathVersion,
            .AllowsInstance = Function(Value) ModModpackMode.AllowsInstance(Value.PathVersion),
            .IsLocked = Function(Value) McInstanceLock.IsLocked(Value),
            .VersionValid = Function(Value) Value.Version.Valid,
            .VersionMajor = Function(Value) Value.Version.Vanilla.Major,
            .HasRunningGame = Function() HasRunningMinecraft,
            .IsCurrentPage = Function() FrmMain?.PageRight Is Me,
            .AnimationEnabled = Function() AniControlEnabled = 0,
            .LoaderFailed = Function() BindingsLoader.State = LoadState.Failed,
            .StartLoader = Sub(Folder) BindingsLoader.Start(Folder, IsForceRestart:=True),
            .CancelLoader = Sub() BindingsLoader.Cancel(),
            .BindExternalEvents = AddressOf BindExternalEvents,
            .BindingForVersion = AddressOf PersonalGameSettings.BindingForVersion,
            .Save = AddressOf InstanceKeybindings.Save,
            .CaptureEditedBinding = AddressOf PersonalGameSettings.CaptureEditedBinding,
            .Message = Function(Text, Title, First, Second, Warning) MyMsgBox(Text, Title, First, Second, IsWarn:=Warning),
            .Hint = Sub(Text, Success) Hint(Text, If(Success, HintType.Green, HintType.Red)),
            .Warn = Sub(ErrorValue, Message) Logger.Warn(ErrorValue, Message),
            .Error = Sub(ErrorValue, Message) Logger.Error(ErrorValue, Message, LogBehavior.Toast)}
    End Function
    Private Sub BindExternalEvents(Attach As Boolean, KeyHandler As KeyEventHandler, MouseHandler As MouseButtonEventHandler)
        If Attach Then
            If FrmMain IsNot Nothing AndAlso KeyboardOwner Is Nothing Then
                KeyboardOwner = FrmMain
                KeyboardKeyHandler = KeyHandler
                KeyboardMouseHandler = MouseHandler
                AddHandler KeyboardOwner.PreviewKeyDown, KeyboardKeyHandler
                AddHandler KeyboardOwner.PreviewMouseDown, KeyboardMouseHandler
            End If
        ElseIf KeyboardOwner IsNot Nothing Then
            RemoveHandler KeyboardOwner.PreviewKeyDown, KeyboardKeyHandler
            RemoveHandler KeyboardOwner.PreviewMouseDown, KeyboardMouseHandler
            KeyboardOwner = Nothing
            KeyboardKeyHandler = Nothing
            KeyboardMouseHandler = Nothing
        End If
    End Sub

    '原测试反射入口直接访问控制器状态，不维护页面状态副本。
    Private Property Entries As List(Of InstanceKeybinding)
        Get
            Return Controller.Entries
        End Get
        Set(Value As List(Of InstanceKeybinding))
            Controller.Entries = Value
        End Set
    End Property
    Private ReadOnly Property KeyButtons As Dictionary(Of String, PCLCS.KeybindingPageButton)
        Get
            Return Controller.KeyButtons
        End Get
    End Property
    Private Property Owner As McInstance
        Get
            Return Controller.Owner
        End Get
        Set(Value As McInstance)
            Controller.Owner = Value
        End Set
    End Property
    Private Property GameFolder As String
        Get
            Return Controller.GameFolder
        End Get
        Set(Value As String)
            Controller.GameFolder = Value
        End Set
    End Property
    Private Property Captured As InstanceKeybinding
        Get
            Return Controller.Captured
        End Get
        Set(Value As InstanceKeybinding)
            Controller.Captured = Value
        End Set
    End Property
    Private Property ConflictsOnly As Boolean
        Get
            Return Controller.ConflictsOnly
        End Get
        Set(Value As Boolean)
            Controller.ConflictsOnly = Value
        End Set
    End Property

    Private Sub PageLoaded() Handles Me.Loaded
        Controller.PageLoaded()
    End Sub
    Private Sub PageUnloaded() Handles Me.Unloaded
        Controller.PageUnloaded()
    End Sub
    Public Sub RefreshBindings() Implements IRefreshable.Refresh
        Controller.RefreshBindings()
    End Sub
    Private Sub ReadBindings(Loader As LoaderTask(Of String, List(Of InstanceKeybinding)))
        Loader.Output = InstanceKeybindings.ReadBindings(Loader.Input, Loader.CreateCancellationToken())
    End Sub
    Private Sub BindingsLoaded()
        Controller.BindingsLoaded(BindingsLoader.Output)
    End Sub
    Private Sub FilterChanged() Handles SearchBox.TextChanged
        Controller?.RenderList()
    End Sub
    Private Sub FilterChecked(Sender As MyRadioButton, User As Boolean) Handles BtnFilterAll.Check, BtnFilterConflicts.Check
        Controller?.FilterChecked(Sender Is BtnFilterConflicts)
    End Sub
    Private Sub RenderList()
        Controller.RenderList()
    End Sub
    Private Function CreateBindingRow(Entry As InstanceKeybinding) As Grid
        Return Controller.CreateBindingRow(Entry)
    End Function
    Private Function DisplayKey(Value As String) As String
        Return KeybindingController.DisplayKey(Value)
    End Function
    Private Sub BeginCapture(Entry As InstanceKeybinding)
        Controller.BeginCapture(Entry)
    End Sub
    Private Sub CancelCapture() Handles BtnCancel.Click
        Controller.CancelCapture()
    End Sub
    Private Sub UpdateCaptureUi()
        Controller.UpdateCaptureUi()
    End Sub
    Private Sub ClearBinding() Handles BtnClear.Click
        Controller.ClearBinding()
    End Sub
    Private Sub CaptureKey(Sender As Object, e As KeyEventArgs)
        Controller.CaptureKey(Sender, e)
    End Sub
    Private Sub HandleCapturedMouse(Sender As Object, e As MouseButtonEventArgs)
        Controller.HandleCapturedMouse(Sender, e)
    End Sub
    Private Sub SaveBinding(Value As String)
        Controller.SaveBinding(Value)
    End Sub
    Private Function KeyboardName(Value As Key) As String
        Return KeybindingController.KeyboardName(Value)
    End Function
    Private Sub RetryLoad() Handles Load.Click
        Controller.RetryLoad()
    End Sub
End Class
