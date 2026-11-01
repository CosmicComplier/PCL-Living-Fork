Public Class MyMsgText

    Private ReadOnly MyConverter As MyMsgBoxConverter
    Private ReadOnly Uuid As Integer = GetUuid()

    Public Sub New(Converter As MyMsgBoxConverter)
        Try

            InitializeComponent()
            Btn1.Name = Btn1.Name & GetUuid()
            Btn2.Name = Btn2.Name & GetUuid()
            Btn3.Name = Btn3.Name & GetUuid()
            MyConverter = Converter
            LabTitle.Text = Converter.Title
            LabCaption.Text = Converter.Text
            If Converter.ContentFactory IsNot Nothing Then
                PanCaption.Content = Converter.ContentFactory()
                PanCaption.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
                PanCaption.Padding = New Thickness(0)
                MinWidth = 0
                RefreshContentBounds()
            End If
            Btn1.Text = Converter.Button1
            If Converter.IsWarn Then
                Btn1.ColorType = MyButton.ColorState.Red
                LabTitle.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushRedLight")
            End If
            Btn2.Text = Converter.Button2
            Btn3.Text = Converter.Button3
            Btn2.Visibility = If(Converter.Button2 = "", Visibility.Collapsed, Visibility.Visible)
            Btn3.Visibility = If(Converter.Button3 = "", Visibility.Collapsed, Visibility.Visible)
            ShapeLine.StrokeThickness = GetWPFSize(1)

        Catch ex As Exception
            Logger.Error(ex, "普通弹窗初始化失败", LogBehavior.Toast)
        End Try
    End Sub

    Private Sub RefreshContentBounds() Handles Me.SizeChanged
        If MyConverter Is Nothing OrElse MyConverter.ContentFactory Is Nothing Then Return
        Dim Host = FrmMain?.PanMsg
        Dim Body = TryCast(PanCaption.Content, FrameworkElement)
        Dim Graph = TryCast(Body, ModDependencyGraphView)
        Dim Impacts = TryCast(Body, ModDependencyImpactView)
        Width = Math.Min(If(Graph IsNot Nothing, Graph.PreferredWidth, If(Impacts IsNot Nothing, Impacts.PreferredWidth, 980)), Math.Max(0, If(Host Is Nothing, 1000, Host.ActualWidth) - 50))
        MaxHeight = Math.Max(0, If(Host Is Nothing, 740, Host.ActualHeight) - 50)
        If Body IsNot Nothing Then Body.MaxHeight = Math.Max(0, MaxHeight - 156)
    End Sub

    Private Sub ContentHostLoaded() Handles Me.Loaded
        If MyConverter Is Nothing OrElse MyConverter.ContentFactory Is Nothing Then Return
        If FrmMain?.PanMsg IsNot Nothing Then AddHandler FrmMain.PanMsg.SizeChanged, AddressOf ContentHostResized
        RefreshContentBounds()
    End Sub
    Private Sub ContentHostResized(Sender As Object, e As SizeChangedEventArgs)
        RefreshContentBounds()
    End Sub
    Private Sub ContentHostUnloaded() Handles Me.Unloaded
        If MyConverter IsNot Nothing AndAlso MyConverter.ContentFactory IsNot Nothing AndAlso FrmMain?.PanMsg IsNot Nothing Then RemoveHandler FrmMain.PanMsg.SizeChanged, AddressOf ContentHostResized
    End Sub

    Private Sub Load(sender As Object, e As EventArgs) Handles MyBase.Loaded
        Try

            'UI 初始化
            If Btn2.IsVisible AndAlso Not Btn1.ColorType = MyButton.ColorState.Red Then Btn1.ColorType = MyButton.ColorState.Highlight
            Btn1.Focus()
            '动画
            Opacity = 0
            AniStart(AaColor(FrmMain.PanMsg, Grid.BackgroundProperty, If(MyConverter.IsWarn, New MyColor(140, 80, 0, 0), New MyColor(90, 0, 0, 0)) - FrmMain.PanMsg.Background, 200), "PanMsg Background")
            AniStart({
                AaOpacity(Me, 1, 120, 60),
                AaDouble(Sub(i) TransformPos.Y += i, -TransformPos.Y, 300, 60, New AniEaseOutBack(AniEasePower.Weak)),
                AaDouble(Sub(i) TransformRotate.Angle += i, -TransformRotate.Angle, 300, 60, New AniEaseOutFluent(AniEasePower.Weak))
            }, "MyMsgBox " & Uuid)
            '记录日志
            Logger.Info($"普通弹窗：{LabTitle.Text}{vbCrLf}{LabCaption.Text}")

        Catch ex As Exception
            Logger.Error(ex, "普通弹窗加载失败", LogBehavior.Toast)
        End Try
    End Sub
    Private Sub Close()
        '结束线程阻塞
        If MyConverter.ForceWait OrElse Not MyConverter.Button2 = "" Then MyConverter.WaitFrame.Continue = False
        Interop.ComponentDispatcher.PopModal()
        '动画
        AniStart({
            AaCode(
            Sub()
                If Not WaitingMyMsgBox.Any() Then
                    AniStart(AaColor(FrmMain.PanMsg, Grid.BackgroundProperty, New MyColor(0, 0, 0, 0) - FrmMain.PanMsg.Background, 200, Ease:=New AniEaseOutFluent(AniEasePower.Weak)))
                End If
            End Sub, 30),
            AaOpacity(Me, -Opacity, 80, 20),
            AaDouble(Sub(i) TransformPos.Y += i, 20 - TransformPos.Y, 150, 0, New AniEaseOutFluent),
            AaDouble(Sub(i) TransformRotate.Angle += i, 6 - TransformRotate.Angle, 150, 0, New AniEaseInFluent(AniEasePower.Weak)),
            AaCode(Sub() CType(Parent, Grid).Children.Remove(Me), , True)
        }, "MyMsgBox " & Uuid)
    End Sub

    Public Sub Btn1_Click() Handles Btn1.Click
        If MyConverter.IsExited Then Return
        If MyConverter.Button1Action IsNot Nothing Then
            MyConverter.Button1Action()
        Else
            MyConverter.IsExited = True
            MyConverter.Result = 1
            Close()
        End If
    End Sub
    Public Sub Btn2_Click() Handles Btn2.Click
        If MyConverter.IsExited Then Return
        If MyConverter.Button2Action IsNot Nothing Then
            MyConverter.Button2Action()
        Else
            MyConverter.IsExited = True
            MyConverter.Result = 2
            Close()
        End If
    End Sub
    Public Sub Btn3_Click() Handles Btn3.Click
        If MyConverter.IsExited Then Return
        If MyConverter.Button3Action IsNot Nothing Then
            MyConverter.Button3Action()
        Else
            MyConverter.IsExited = True
            MyConverter.Result = 3
            Close()
        End If
    End Sub

    Private Sub Drag(sender As Object, e As MouseButtonEventArgs) Handles PanBorder.MouseLeftButtonDown, LabTitle.MouseLeftButtonDown
        On Error Resume Next
        If e.GetPosition(ShapeLine).Y <= 2 Then FrmMain.DragMove()
    End Sub

End Class
