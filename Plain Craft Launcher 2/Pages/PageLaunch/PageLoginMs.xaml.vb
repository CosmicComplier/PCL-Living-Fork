Imports PCLLF.Presentation.Accounts

Public Class PageLoginMs
    Private ReadOnly Native As LegacyMicrosoftLoginPage(Of McLoginLegacy, McLoginMs, McLoginServer)
    Public Sub New()
        Native = CreateController(Me)
        InitializeComponent()
    End Sub
    Private Shared Function CreateController(Page As PageLoginMs) As LegacyMicrosoftLoginPage(Of McLoginLegacy, McLoginMs, McLoginServer)
        Return New LegacyMicrosoftLoginPage(Of McLoginLegacy, McLoginMs, McLoginServer)(LoginPageRuntime.Accounts, New LegacyMicrosoftLoginView With {
            .SelectedIndex = Function() Page.ComboAccounts.SelectedIndex, .SelectIndex = Sub(Value) Page.ComboAccounts.SelectedIndex = Value,
            .ClearItems = Sub() Page.ComboAccounts.Items.Clear(), .AddNewAccount = Sub() Page.ComboAccounts.Items.Add(New MyComboBoxItem With {.Content = "添加新账号"}),
            .AddAccount = Sub(Name, Token) Page.AddAccount(Name, Token),
            .SelectedItem = Function() Page.ComboAccounts.SelectedItem, .HasPresenter = Function() Page.ComboAccounts.ContentPresenter IsNot Nothing,
            .IsAccountItem = Function(Item) TypeOf Item Is MyListItem, .IsNewAccountItem = Function(Item) TypeOf Item Is MyComboBoxItem,
            .AccountName = Function(Item) CType(Item, MyListItem).Title, .AccountToken = Function(Item) CStr(CType(Item, MyListItem).Tag),
            .NewAccountContent = Function(Item) CType(Item, MyComboBoxItem).Content, .SetPresenterContent = Sub(Value) Page.ComboAccounts.ContentPresenter.Content = Value,
            .EnableAccounts = Sub(Value) Page.ComboAccounts.IsEnabled = Value, .EnableLogin = Sub(Value) Page.BtnLogin.IsEnabled = Value,
            .SetLoginText = Sub(Value) Page.BtnLogin.Text = Value})
    End Function
    Private Sub AddAccount(Name As String, Token As Func(Of String))
        Dim Item As MyListItem = CType(FindResource("ComboBoxItemTemplateWithDelete"), DataTemplate).LoadContent()
        Item.Tag = Token()
        Item.Title = Name
        CType(Item.Buttons(0), MyIconButton).Tag = Name
        ComboAccounts.Items.Add(Item)
    End Sub
    Public Sub Reload(KeepInput As Boolean)
        Native.Reload(KeepInput)
    End Sub
    Private Sub ComboAccounts_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboAccounts.SelectionChanged
        Native.SelectionChanged()
    End Sub
    Public Shared Function GetLoginData() As McLoginMs
        Return CreateController(FrmLoginMs).GetLoginData(FrmLoginMs IsNot Nothing)
    End Function
    Public Shared Function IsVaild(LoginData As McLoginMs) As String
        Return CreateController(FrmLoginMs).IsVaild(LoginData)
    End Function
    Public Function IsVaild() As String
        Return IsVaild(GetLoginData())
    End Function
    Private Sub BtnLogin_Click(sender As Object, e As EventArgs) Handles BtnLogin.Click
        Native.LoginClick(AddressOf GetLoginData)
    End Sub
End Class
