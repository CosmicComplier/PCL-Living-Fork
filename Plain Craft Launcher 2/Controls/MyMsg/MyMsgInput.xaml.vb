Option Strict On
Imports PCLLF.Presentation.Dialogs

Public Class MyMsgInput
    Inherits Grid
    Private ReadOnly MyConverter As MyMsgBoxConverter
    Private ReadOnly Uuid As Integer = GetUuid()
    Private ReadOnly Native As IMessageInputController
    Public Sub New(Converter As MyMsgBoxConverter)
        Native = New MessageInputController(Of MyMsgBoxType, Validate)(New MessageInputParts With {
            .Root = Me, .Button1 = Function() Btn1, .Button2 = Function() Btn2, .Title = Function() LabTitle, .Line = Function() ShapeLine,
            .Position = Function() TransformPos, .Rotation = Function() TransformRotate, .Input = Function() TextArea,
            .TextPanel = Function() PanText, .CaptionText = Function() LabText, .ApplyRules = Sub() TextArea.ValidateRules = MyConverter.ValidateRules},
            Function() MyConverter, MessageDialogPlatform.CreateHost(), Function() Uuid)
        Try
            InitializeComponent()
            Native.RenameButtons()
            MyConverter = Converter
            Native.Initialize()
        Catch ex As Exception
            Native.InitializationFailed(ex)
        End Try
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
    Private Sub TextCaption_ValidateChanged(sender As Object, e As EventArgs) Handles TextArea.ValidateChanged
        Native.ValidateChanged()
    End Sub
    Private Sub Drag(sender As Object, e As MouseButtonEventArgs) Handles PanBorder.MouseLeftButtonDown, LabTitle.MouseLeftButtonDown
        Native.Drag(e)
    End Sub
End Class
