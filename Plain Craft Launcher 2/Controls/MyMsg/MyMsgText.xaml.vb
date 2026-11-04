Option Strict On
Imports PCLLF.Presentation.Dialogs

Public Class MyMsgText
    Inherits Grid
    Private ReadOnly MyConverter As MyMsgBoxConverter
    Private ReadOnly Uuid As Integer = GetUuid()
    Private ReadOnly Native As IMessageTextController
    Public Sub New(Converter As MyMsgBoxConverter)
        Native = New MessageTextController(Of MyMsgBoxType, Validate)(New MessageTextParts With {
            .Root = Me, .Button1 = Function() Btn1, .Button2 = Function() Btn2, .Button3 = Function() Btn3,
            .Title = Function() LabTitle, .Line = Function() ShapeLine, .Position = Function() TransformPos, .Rotation = Function() TransformRotate,
            .Caption = Function() PanCaption, .CaptionText = Function() LabCaption}, Function() MyConverter, MessageDialogPlatform.CreateHost(), Function() Uuid)
        Try
            InitializeComponent()
            Native.RenameButtons()
            MyConverter = Converter
            Native.Initialize()
        Catch ex As Exception
            Native.InitializationFailed(ex)
        End Try
    End Sub
    Private Sub RefreshContentBounds() Handles Me.SizeChanged
        Native.RefreshContentBounds()
    End Sub
    Private Sub ContentHostLoaded() Handles Me.Loaded
        Native.ContentHostLoaded()
    End Sub
    Private Sub ContentHostUnloaded() Handles Me.Unloaded
        Native.ContentHostUnloaded()
    End Sub
    Private Sub Load(sender As Object, e As EventArgs) Handles MyBase.Loaded
        Native.Load()
    End Sub
    Public Sub Btn1_Click() Handles Btn1.Click
        Native.Button1Click()
    End Sub
    Public Sub Btn2_Click() Handles Btn2.Click
        Native.Button2Click()
    End Sub
    Public Sub Btn3_Click() Handles Btn3.Click
        Native.Button3Click()
    End Sub
    Private Sub Drag(sender As Object, e As MouseButtonEventArgs) Handles PanBorder.MouseLeftButtonDown, LabTitle.MouseLeftButtonDown
        Native.Drag(e)
    End Sub
End Class

'仅原真实LF平台、旧自定义正文类型及接口事件投影；无布局/选择/按钮策略或状态副本。
Friend NotInheritable Class MessageDialogPlatform
    Friend Shared Function CreateHost() As MessageDialogHost
        Return New MessageDialogHost With {
            .NewUuid = AddressOf GetUuid, .MessagePanel = Function() FrmMain?.PanMsg, .HasWaiting = Function() WaitingMyMsgBox.Any(),
            .DragMove = Sub() FrmMain.DragMove(), .PixelToWpf = AddressOf GetWPFSize,
            .Animations = Function() PCLLF.Presentation.Controls.ControlServices.Current.Animations,
            .ModpackWidth = Function(Body) TryCast(Body, ModpackDialogView)?.PreferredWidth,
            .DependenciesWidth = Function(Body) TryCast(Body, ModDependencyCheckView)?.PreferredWidth,
            .GraphWidth = Function(Body) TryCast(Body, ModDependencyGraphView)?.PreferredWidth,
            .ImpactsWidth = Function(Body) TryCast(Body, ModDependencyImpactView)?.PreferredWidth,
            .VersionSelection = AddressOf VersionSelection, .Radio = AddressOf Radio}
    End Function
    Private Shared Function VersionSelection(Body As Object) As ModalSelectionPort
        Dim Actual = TryCast(Body, ModpackVersionSelectView)
        If Actual Is Nothing Then Return Nothing
        Return New ModalSelectionPort(Function() Actual.Selected IsNot Nothing,
            Sub(Handler) AddHandler Actual.SelectionChanged, Sub() Handler())
    End Function
    Private Shared Function Radio(Element As FrameworkElement) As ModalRadioPort
        Dim Actual = DirectCast(Element, IMyRadio)
        Return New ModalRadioPort(Element, Sub(Handler) AddHandler Actual.Check, Sub(Sender, Args) Handler(DirectCast(Sender, IMyRadio)))
    End Function
End Class
