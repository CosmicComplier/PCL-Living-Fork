Imports PCLLF.Presentation.Accounts

Public Class PageLoginNideSkin
    Private ReadOnly Native As LegacyLoggedInPage(Of McLoginLegacy, McLoginMs, McLoginServer)
    Public Sub New()
        Native = CreateController(Me)
        InitializeComponent()
        Skin.Loader = PageLaunchLeft.SkinNide
    End Sub
    Private Shared Function CreateController(Page As PageLoginNideSkin) As LegacyLoggedInPage(Of McLoginLegacy, McLoginMs, McLoginServer)
        Return New LegacyLoggedInPage(Of McLoginLegacy, McLoginMs, McLoginServer)(LoginPageRuntime.Accounts, New LegacyLoggedInView With {
            .SetName = Sub(Value) Page.TextName.Text = Value,
            .StartSkin = Sub() Page.Skin.Loader.Start(),
            .EditButton = Function() Page.BtnEdit, .ExitButton = Function() Page.BtnExit,
            .SetEmail = Sub(Value) Page.TextEmail.Text = Value,
            .ShowEmail = Sub(Value) Page.TextEmail.Visibility = If(Value, Visibility.Visible, Visibility.Collapsed),
            .SaveSkin = Sub() Page.Skin.BtnSkinSave_Click(), .RefreshSkin = Sub() Page.Skin.RefreshClick(),
            .ChangeCape = Sub() Page.Skin.BtnSkinCape_Click()}, "Nide")
    End Function
    Private Sub PageLoginLegacy_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Native.Loaded()
    End Sub
    Public Sub Reload(KeepInput As Boolean)
        Native.Reload(KeepInput)
    End Sub
    Public Shared Function GetLoginData() As McLoginServer
        Return CreateController(FrmLoginNideSkin).GetServerLoginData()
    End Function
    Private Sub PageLoginNideSkin_MouseEnter(sender As Object, e As MouseEventArgs) Handles PanData.MouseEnter
        Native.ShowPanel()
    End Sub
    Private Sub PageLoginNideSkin_MouseLeave(sender As Object, e As MouseEventArgs) Handles PanData.MouseLeave
        Native.HidePanel()
    End Sub
    Public Shared Sub ExitLogin() Handles BtnExit.Click
        CreateController(FrmLoginNideSkin).ExitLogin()
    End Sub
    Private Sub Skin_Click(sender As Object, e As MouseButtonEventArgs) Handles Skin.Click
        Native.SkinClick()
    End Sub
End Class
