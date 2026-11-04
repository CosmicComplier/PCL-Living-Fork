Imports PCLLF.Presentation.Accounts

Public Class PageLoginMsSkin
    Private ReadOnly Native As LegacyLoggedInPage(Of McLoginLegacy, McLoginMs, McLoginServer)
    Public Sub New()
        Native = CreateController(Me)
        InitializeComponent()
        Skin.Loader = PageLaunchLeft.SkinMs
    End Sub
    Private Shared Function CreateController(Page As PageLoginMsSkin) As LegacyLoggedInPage(Of McLoginLegacy, McLoginMs, McLoginServer)
        Return New LegacyLoggedInPage(Of McLoginLegacy, McLoginMs, McLoginServer)(LoginPageRuntime.Accounts, New LegacyLoggedInView With {
            .SetName = Sub(Value) Page.TextName.Text = Value,
            .StartSkin = Sub() Page.Skin.Loader.Start(),
            .EditButton = Function() Page.BtnEdit, .ExitButton = Function() Page.BtnExit,
            .ButtonsPanel = Function() Page.PanButtons,
            .EditMenuOpen = Function() Page.BtnEdit.ContextMenu.IsOpen, .SkinMenuOpen = Function() Page.BtnSkin.ContextMenu.IsOpen,
            .DataMouseOver = Function() Page.PanData.IsMouseOver,
            .OpenEditMenu = Sub() Page.BtnEdit.ContextMenu.IsOpen = True, .OpenSkinMenu = Sub() Page.BtnSkin.ContextMenu.IsOpen = True,
            .SelectSkin = AddressOf LoginPageRuntime.SelectSkin, .ReloadSkinCache = Sub(Address) MySkin.ReloadCache(Address),
            .SaveSkin = Sub() Page.Skin.BtnSkinSave_Click(), .RefreshSkin = Sub() Page.Skin.RefreshClick(),
            .ChangeCape = Sub() Page.Skin.BtnSkinCape_Click()}, "MsV2")
    End Function
    Private Sub PageLoginLegacy_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Native.Loaded()
    End Sub
    Public Sub Reload(KeepInput As Boolean)
        Native.Reload(KeepInput)
    End Sub
    Public Shared Function GetLoginData() As McLoginMs
        Return CreateController(FrmLoginMsSkin).GetMsLoginData()
    End Function
    Private Sub ShowPanel(sender As Object, e As MouseEventArgs) Handles PanData.MouseEnter
        Native.ShowPanel()
    End Sub
    Public Sub HidePanel(sender As Object, e As EventArgs) Handles PanData.MouseLeave
        Native.HidePanel()
    End Sub
    Private Sub BtnEdit_Click(sender As Object, e As EventArgs) Handles BtnEdit.Click
        Native.OpenEditMenu()
    End Sub
    Private Sub BtnExit_Click() Handles BtnExit.Click
        Native.ExitLogin()
    End Sub
    Private Sub BtnSkin_Click(sender As Object, e As RoutedEventArgs) Handles BtnSkin.Click
        Native.SkinClick()
    End Sub
    Public Sub BtnSkinEdit_Click(sender As Object, e As RoutedEventArgs)
        Native.SkinEditClick(AddressOf GetLoginData)
    End Sub
    Public Sub BtnSkinSave_Click(sender As Object, e As RoutedEventArgs)
        Native.SaveSkin()
    End Sub
    Public Sub BtnSkinRefresh_Click(sender As Object, e As RoutedEventArgs)
        Native.RefreshSkin()
    End Sub
    Public Sub BtnSkinCape_Click(sender As Object, e As RoutedEventArgs)
        Native.ChangeCape()
    End Sub
    Public Sub BtnSkinCapeRefresh_Click(sender As Object, e As RoutedEventArgs)
        Native.RefreshCape(AddressOf GetLoginData)
    End Sub
End Class
