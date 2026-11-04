Imports PCLCS

' Original LF XAML fields/Handles and public entry points remain typed; business lives in C#.
Public Class PageSetupSystem
    Private _controller As LegacyPageSetupSystemController
    Friend ReadOnly Property Controller As LegacyPageSetupSystemController
        Get
            If _controller Is Nothing Then
                _controller = New LegacyPageSetupSystemController(SetupPageRuntime.SystemHost, New LegacyPageSetupSystemView With {
                    .Page = Function() Me,
                    .PanBack = Function() PanBack,
                    .PanMain = Function() PanMain,
                    .ComboDownloadSource = Function() ComboDownloadSource,
                    .ComboDownloadVersion = Function() ComboDownloadVersion,
                    .SliderDownloadThread = Function() SliderDownloadThread,
                    .SliderDownloadSpeed = Function() SliderDownloadSpeed,
                    .ComboDownloadMod = Function() ComboDownloadMod,
                    .ComboDownloadTranslateV2 = Function() ComboDownloadTranslateV2,
                    .ComboModLocalNameStyle = Function() ComboModLocalNameStyle,
                    .CheckHelpChinese = Function() CheckHelpChinese,
                    .LabLauncherVersion = Function() LabLauncherVersion,
                    .ComboSystemActivity = Function() ComboSystemActivity,
                    .TextSystemCache = Function() TextSystemCache,
                    .BtnSystemUpdate = Function() BtnSystemUpdate,
                    .BtnSystemSettingExp = Function() BtnSystemSettingExp,
                    .BtnSystemSettingImp = Function() BtnSystemSettingImp,
                    .LabModpackMode = Function() LabModpackMode,
                    .CheckModpackMode = Function() CheckModpackMode,
                    .CardDebug = Function() CardDebug,
                    .PanDebugAnim = Function() PanDebugAnim,
                    .SliderDebugAnim = Function() SliderDebugAnim,
                    .CheckDebugSkipCopy = Function() CheckDebugSkipCopy,
                    .CheckDebugMode = Function() CheckDebugMode,
                    .CheckDebugDelay = Function() CheckDebugDelay
                })
            End If
            Return _controller
        End Get
    End Property

    Private Sub PageSetupSystem_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Controller.Loaded()
    End Sub

    Public Sub Reload()
        Controller.Reload()
    End Sub

    Friend Sub RefreshModpackSwitch()
        Controller.RefreshModpackSwitch()
    End Sub

    Private Sub ModpackModeChanged(sender As Object, User As Boolean) Handles CheckModpackMode.Change
        Controller.ModpackModeChanged(sender, User)
    End Sub

    Public Sub Reset()
        Controller.Reset()
    End Sub

    Private Sub SliderLoad()
        Controller.SliderLoad()
    End Sub

    Private Sub SliderDownloadThread_PreviewChange(sender As Object, e As RouteEventArgs) Handles SliderDownloadThread.PreviewChange
        Controller.DownloadThreadPreview()
    End Sub

    Private Sub CheckDebugMode_Change() Handles CheckDebugMode.Change
        Controller.DebugModeChanged()
    End Sub

    Private Sub BtnSystemUpdate_Click(sender As Object, e As EventArgs) Handles BtnSystemUpdate.Click
        Controller.CheckUpdate()
    End Sub

    Private Sub BtnSystemSettingExp_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnSystemSettingExp.Click
        Controller.ExportSettings()
    End Sub

    Private Sub BtnSystemSettingImp_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnSystemSettingImp.Click
        Controller.ImportSettings()
    End Sub

End Class
