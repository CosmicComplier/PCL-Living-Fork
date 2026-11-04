Imports PCLLF.Presentation.Accounts

Public Class PageLoginNide
    Private ReadOnly Native As LegacyServerLoginPage(Of McLoginLegacy, McLoginMs, McLoginServer)
    Public Sub New()
        Native = CreateController(Me)
        InitializeComponent()
    End Sub
    Private Shared Function CreateController(Page As PageLoginNide) As LegacyServerLoginPage(Of McLoginLegacy, McLoginMs, McLoginServer)
        Return New LegacyServerLoginPage(Of McLoginLegacy, McLoginMs, McLoginServer)(LoginPageRuntime.Accounts, New LegacyServerLoginView With {
            .Name = Function() Page.ComboName.Text, .Password = Function() Page.TextPass.Password,
            .SetName = Sub(Value) Page.ComboName.Text = Value, .SetPassword = Sub(Value) Page.TextPass.Password = Value,
            .SetNames = Sub(Value) Page.ComboName.ItemsSource = Value, .SetRemember = Sub(Value) Page.CheckRemember.Checked = Value,
            .LinkContent = Function() Page.BtnLink.Content, .SetLinkContent = Sub(Value) Page.BtnLink.Content = Value,
            .ShowRegister = Sub(Value) Page.BtnLink.Visibility = If(Value, Visibility.Visible, Visibility.Collapsed)}, False)
    End Function
    Public Sub Reload(KeepInput As Boolean)
        Native.Reload(KeepInput)
    End Sub
    Public Shared Function GetLoginData() As McLoginServer
        Return CreateController(FrmLoginNide).GetLoginData(Function() FrmLoginNide IsNot Nothing, Function() FrmLoginNide.ComboName.Text, Function() FrmLoginNide.TextPass.Password)
    End Function
    Public Shared Function IsVaild(LoginData As McLoginServer) As String
        Return CreateController(FrmLoginNide).IsVaild(LoginData)
    End Function
    Public Function IsVaild() As String
        Return IsVaild(GetLoginData())
    End Function
    Private Sub ComboName_TextChanged(sender As Object, e As TextChangedEventArgs) Handles ComboName.TextChanged
        Native.NameChanged(sender.Text)
    End Sub
    Private Sub TextPass_PasswordChanged(sender As Object, e As RoutedEventArgs) Handles TextPass.PasswordChanged
        Native.PasswordChanged()
    End Sub
    Private Sub ComboName_SelectionChanged(sender As MyComboBox, e As SelectionChangedEventArgs) Handles ComboName.SelectionChanged
        Native.SelectionChanged(sender.SelectedIndex)
    End Sub
    Private Sub CheckBoxChange(sender As MyCheckBox, e As Object) Handles CheckRemember.Change
        Native.RememberChanged(sender.Tag, sender.Checked)
    End Sub
    Private Sub ComboName_TextChanged() Handles ComboName.TextChanged
        Native.UpdateLinkText()
    End Sub
    Private Sub Btn_Click(sender As Object, e As EventArgs) Handles BtnLink.Click
        Native.LinkClick()
    End Sub
End Class
