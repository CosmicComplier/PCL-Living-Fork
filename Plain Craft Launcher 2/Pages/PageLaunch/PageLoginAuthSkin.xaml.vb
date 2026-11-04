Imports PCLLF.Presentation.Accounts

Public Class PageLoginAuthSkin
    Private ReadOnly Native As LegacyLoggedInPage(Of McLoginLegacy, McLoginMs, McLoginServer)
    Public Sub New()
        Native = CreateController(Me)
        InitializeComponent()
        Skin.Loader = PageLaunchLeft.SkinAuth
    End Sub
    Private Shared Function CreateController(Page As PageLoginAuthSkin) As LegacyLoggedInPage(Of McLoginLegacy, McLoginMs, McLoginServer)
        Return New LegacyLoggedInPage(Of McLoginLegacy, McLoginMs, McLoginServer)(LoginPageRuntime.Accounts, New LegacyLoggedInView With {
            .SetName = Sub(Value) Page.TextName.Text = Value,
            .StartSkin = Sub() Page.Skin.Loader.Start(),
            .EditButton = Function() Page.BtnEdit, .ExitButton = Function() Page.BtnExit,
            .SetEmail = Sub(Value) Page.TextEmail.Text = Value,
            .ShowEmail = Sub(Value) Page.TextEmail.Visibility = If(Value, Visibility.Visible, Visibility.Collapsed),
            .SaveSkin = Sub() Page.Skin.BtnSkinSave_Click(), .RefreshSkin = Sub() Page.Skin.RefreshClick(),
            .ChangeCape = Sub() Page.Skin.BtnSkinCape_Click()}, "Auth")
    End Function
    Private Sub PageLoginLegacy_Loaded() Handles Me.Loaded
        Native.Loaded()
    End Sub
    Public Sub Reload(KeepInput As Boolean)
        Native.Reload(KeepInput)
    End Sub
    Public Shared Function GetLoginData() As McLoginServer
        Return CreateController(FrmLoginAuthSkin).GetServerLoginData()
    End Function
    Private Sub PageLoginAuthSkin_MouseEnter(sender As Object, e As MouseEventArgs) Handles PanData.MouseEnter
        Native.ShowPanel()
    End Sub
    Private Sub PageLoginAuthSkin_MouseLeave(sender As Object, e As MouseEventArgs) Handles PanData.MouseLeave
        Native.HidePanel()
    End Sub
    Private Sub BtnEdit_Click(sender As Object, e As EventArgs) Handles BtnEdit.Click
        Native.EditClick(AddressOf GetLoginData, Sub() Reload(True))
    End Sub
    Public Shared Sub ExitLogin() Handles BtnExit.Click
        CreateController(FrmLoginAuthSkin).ExitLogin()
    End Sub
    Private Sub Skin_Click(sender As Object, e As MouseButtonEventArgs) Handles Skin.Click
        Native.SkinClick()
    End Sub
End Class
