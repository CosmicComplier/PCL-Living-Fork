Public Class PageSetupModpack
    Private Refreshing As Boolean
    Private PageController As PCLCS.ModpackSetupPageController(Of MyCheckBox)
    Private ReadOnly Property Controller As PCLCS.ModpackSetupPageController(Of MyCheckBox)
        Get
            If PageController Is Nothing Then PageController = New PCLCS.ModpackSetupPageController(Of MyCheckBox)(
                New PCLCS.ModpackSetupPageHost(Of MyCheckBox) With {
                    .Refreshing = Function() Refreshing,
                    .SetRefreshing = Sub(Value) Refreshing = Value,
                    .AdjustAnimation = Sub(Value) AniControlEnabled += Value,
                    .Animation = Function() AniControlEnabled,
                    .TouchTitle = AddressOf TouchTitle,
                    .VersionText = LabVersion,
                    .Brand = Function() LauncherDisplayName,
                    .Version = Function() VersionBaseName,
                    .RefreshSettings = Sub() SettingService.RefreshSettings(Me),
                    .Enabled = Function() ModModpackMode.Enabled,
                    .SetChecked = Sub(Value) CheckModpackMode.Checked = Value,
                    .ScrollHome = Sub() PanBack.ScrollToHome(),
                    .Toggle = Sub(Sender, User) ModModpackMode.Toggle(Sender, User),
                    .HintWithoutLog = Sub(Message) Hint(Message, Log:=False)})
            Return PageController
        End Get
    End Property
    Private Sub TouchTitle()
        Dim Title = CardLauncher.MainTextBlock
    End Sub
    Private Sub PageLoaded() Handles Me.Loaded
        Controller.Loaded()
    End Sub
    Private Sub ModeChanged(sender As Object, User As Boolean) Handles CheckModpackMode.Change
        Controller.ModeChanged(CheckModpackMode, User)
    End Sub
    Private Sub CacheChanged() Handles TextSystemCache.ValidatedTextChanged
        Controller.CacheChanged()
    End Sub
End Class
