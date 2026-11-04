Imports PCLLF.Presentation.Accounts

Public Class PageLoginLegacy
    Private ReadOnly Native As LegacyOfflineLoginPage(Of McLoginLegacy, McLoginMs, McLoginServer)
    Public IsReloaded As Boolean = False
    Public Sub New()
        Native = CreateController(Me)
        InitializeComponent()
        Skin.Loader = PageLaunchLeft.SkinLegacy
    End Sub
    Private Shared Function CreateController(Page As PageLoginLegacy) As LegacyOfflineLoginPage(Of McLoginLegacy, McLoginMs, McLoginServer)
        Return New LegacyOfflineLoginPage(Of McLoginLegacy, McLoginMs, McLoginServer)(LoginPageRuntime.Accounts, New LegacyOfflineLoginView With {
            .IsReloaded = Function() Page.IsReloaded, .SetReloaded = Sub(Value) Page.IsReloaded = Value,
            .Name = Function() Page.ComboName.Text, .SetName = Sub(Value) Page.ComboName.Text = Value,
            .SetNames = Sub(Value) Page.ComboName.ItemsSource = Value,
            .StartSkin = Sub() Page.Skin.Loader.Start(), .RestartSkin = Sub() PageLaunchLeft.SkinLegacy.Start(IsForceRestart:=True),
            .ShowChineseHint = Sub(Value) Page.HintChinese.Visibility = If(Value, Visibility.Visible, Visibility.Collapsed),
            .InvalidateMeasureWhenLoaded = Sub() RunInUi(Sub() Page.PanMain.InvalidateMeasure(), True)})
    End Function
    Private Sub PageLoginLegacy_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Native.Loaded()
    End Sub
    Public Sub Reload(KeepInput As Boolean)
        Native.Reload(KeepInput)
    End Sub
    Public Shared Function GetLoginData() As McLoginData
        Return CreateController(FrmLoginLegacy).GetLoginData(FrmLoginLegacy IsNot Nothing, Function() FrmLoginLegacy.ComboName.Text)
    End Function
    Public Shared Function IsVaild(LoginData As McLoginLegacy) As String
        Return CreateController(FrmLoginLegacy).IsVaild(LoginData)
    End Function
    Public Function IsVaild() As String
        Return IsVaild(GetLoginData())
    End Function
    Private Sub ComboName_PreviewKeyDown(sender As Object, e As Input.KeyEventArgs) Handles ComboName.PreviewKeyDown
        If e.Key = Key.Space Then
            Dim Index = CType(ComboName.Template.FindName("PART_EditableTextBox", ComboName), MyTextBox).CaretIndex
            If LegacyOfflineLoginPage(Of McLoginLegacy, McLoginMs, McLoginServer).SuppressSpace(Index, ComboName.Text.Length) Then e.Handled = True
        End If
    End Sub
    Private Sub ComboLegacy_TextChanged(sender As Object, e As TextChangedEventArgs) Handles ComboName.TextChanged
        Native.TextChanged()
    End Sub
    Private Sub Skin_Click() Handles Skin.Click
        Native.SkinClick()
    End Sub
End Class
