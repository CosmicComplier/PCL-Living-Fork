Imports PCLLF.Presentation.Accounts

Public Class MyMsgLogin
    Private ReadOnly Native As LegacyDeviceLoginDialog(Of MyMsgBoxConverter)
    Shared Sub New()
        PresentationRuntime.EnsureConfigured()
    End Sub
    Public Sub New(Converter As MyMsgBoxConverter)
        Native = New LegacyDeviceLoginDialog(Of MyMsgBoxConverter)(New LegacyDeviceLoginHost(Of MyMsgBoxConverter) With {
            .NewUuid = Function() GetUuid(), .Content = Function(Value) CType(Value.Content, JObject),
            .IsExited = Function(Value) Value.IsExited, .IsWarn = Function(Value) Value.IsWarn,
            .SetExited = Sub(Value, State) Value.IsExited = State, .SetResult = Sub(Value, Result) Value.Result = Result,
            .RestartException = Function() New RestartException, .OAuthClientId = Function() OAuthClientId,
            .HasWaitingMessages = Function() WaitingMyMsgBox.Any(), .Control = Function() Me, .MessagePanel = Function() FrmMain.PanMsg,
            .Position = Function() TransformPos, .Rotation = Function() TransformRotate,
            .Title = Function() LabTitle.Text, .Caption = Function() LabCaption.Text,
            .SetTitle = Sub(Value) LabTitle.Text = Value, .SetCaption = Sub(Value) LabCaption.Text = Value,
            .SetWebEvent = Sub(Value) CustomEventService.SetEventData(Btn1, Value), .SetCopyEvent = Sub(Value) CustomEventService.SetEventData(Btn2, Value),
            .ButtonName = AddressOf ReadButtonName, .SetButtonName = AddressOf WriteButtonName,
            .SetLineThickness = Sub() ShapeLine.StrokeThickness = GetWPFSize(1), .RemoveControl = Sub() CType(Parent, Grid).Children.Remove(Me),
            .ShowWindowToTop = Sub() FrmMain.ShowWindowToTop(), .RunInUi = Sub(Work) RunInUi(Work),
            .RunInNewThread = Sub(Work, Name) RunInNewThread(Work, Name), .OpenWebsite = Sub(Value) OpenWebsite(Value),
            .ClipboardSet = Sub(Value) ClipboardSet(Value), .LaunchLog = Sub(Value) McLaunchLog(Value), .Hint = AddressOf LoginPageRuntime.PublishHint,
            .Request = Function(Url, Method, Content, ContentType, Timeout, MakeLog, RequireJson) NetRequestByClient(Url, Method, Content:=Content, ContentType:=ContentType, Timeout:=Timeout, MakeLog:=MakeLog, RequireJson:=RequireJson)})
        Try
            InitializeComponent()
            Native.Initialize(Converter)
        Catch ex As Exception
            Logger.Error(ex, "登录弹窗初始化失败", LogBehavior.Toast)
        End Try
    End Sub
    Private Function ReadButtonName(Index As Integer) As String
        Select Case Index
            Case 1 : Return Btn1.Name
            Case 2 : Return Btn2.Name
            Case Else : Return Btn3.Name
        End Select
    End Function
    Private Sub WriteButtonName(Index As Integer, Value As String)
        Select Case Index
            Case 1 : Btn1.Name = Value
            Case 2 : Btn2.Name = Value
            Case Else : Btn3.Name = Value
        End Select
    End Sub
    Private Sub Load(sender As Object, e As EventArgs) Handles MyBase.Loaded
        Native.Loaded()
    End Sub
    '原按钮由 XAML CustomEventService 打开网页；该兼容入口保持原空处理。
    Public Sub Btn1_Click() Handles Btn1.Click
    End Sub
    Public Sub Btn3_Click() Handles Btn3.Click
        Native.CancelClick()
    End Sub
    Private Sub Drag(sender As Object, e As MouseButtonEventArgs) Handles PanBorder.MouseLeftButtonDown, LabTitle.MouseLeftButtonDown
        On Error Resume Next
        If LegacyDeviceLoginDialog(Of MyMsgBoxConverter).ShouldDrag(e.GetPosition(ShapeLine).Y) Then FrmMain.DragMove()
    End Sub
End Class
