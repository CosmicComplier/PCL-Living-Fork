Imports PCLCS

' Original LF XAML fields/Handles and public entry points remain typed; business lives in C#.
Public Class PageSetupUI
    Private Shared _HiddenForceShow As Boolean = False
    Friend Shared Function ReadHiddenForceShow() As Boolean
        Return _HiddenForceShow
    End Function
    Friend Shared Sub WriteHiddenForceShow(Value As Boolean)
        _HiddenForceShow = Value
    End Sub
    Private _controller As LegacyPageSetupUIController
    Friend ReadOnly Property Controller As LegacyPageSetupUIController
        Get
            If _controller Is Nothing Then
                _controller = New LegacyPageSetupUIController(SetupPageRuntime.UI, New LegacyPageSetupUIView With {
                    .Page = Function() Me,
                    .PanBack = Function() PanBack,
                    .PanMain = Function() PanMain,
                    .CardLauncher = Function() CardLauncher,
                    .SliderLauncherTransparent = Function() SliderLauncherTransparent,
                    .PanOfficialTheme = Function() PanOfficialTheme,
                    .BtnOfficialTheme = Function() BtnOfficialTheme,
                    .CardBackground = Function() CardBackground,
                    .ComboBackgroundMode = Function() ComboBackgroundMode,
                    .PanBackgroundSuit = Function() PanBackgroundSuit,
                    .PanBackgroundOpacity = Function() PanBackgroundOpacity,
                    .SliderBackgroundOpacity = Function() SliderBackgroundOpacity,
                    .PanBackgroundBlur = Function() PanBackgroundBlur,
                    .SliderBackgroundBlur = Function() SliderBackgroundBlur,
                    .CheckBackgroundColorful = Function() CheckBackgroundColorful,
                    .BtnBackgroundOpen = Function() BtnBackgroundOpen,
                    .BtnBackgroundRefresh = Function() BtnBackgroundRefresh,
                    .BtnBackgroundClear = Function() BtnBackgroundClear,
                    .CardMusic = Function() CardMusic,
                    .PanMusicVolume = Function() PanMusicVolume,
                    .SliderMusicVolume = Function() SliderMusicVolume,
                    .PanMusicDetail = Function() PanMusicDetail,
                    .CheckMusicRandom = Function() CheckMusicRandom,
                    .CheckMusicAuto = Function() CheckMusicAuto,
                    .CheckMusicStart = Function() CheckMusicStart,
                    .CheckMusicStop = Function() CheckMusicStop,
                    .BtnMusicOpen = Function() BtnMusicOpen,
                    .BtnMusicRefresh = Function() BtnMusicRefresh,
                    .BtnMusicClear = Function() BtnMusicClear,
                    .CardLogo = Function() CardLogo,
                    .RadioLogoType0 = Function() RadioLogoType0,
                    .RadioLogoType1 = Function() RadioLogoType1,
                    .RadioLogoType2 = Function() RadioLogoType2,
                    .RadioLogoType3 = Function() RadioLogoType3,
                    .CheckLogoLeft = Function() CheckLogoLeft,
                    .PanLogoText = Function() PanLogoText,
                    .TextLogoText = Function() TextLogoText,
                    .PanLogoChange = Function() PanLogoChange,
                    .BtnLogoChange = Function() BtnLogoChange,
                    .BtnLogoDelete = Function() BtnLogoDelete,
                    .CardCustom = Function() CardCustom,
                    .RadioCustomType0 = Function() RadioCustomType0,
                    .RadioCustomType3 = Function() RadioCustomType3,
                    .RadioCustomType1 = Function() RadioCustomType1,
                    .RadioCustomType2 = Function() RadioCustomType2,
                    .HintCustom = Function() HintCustom,
                    .HintCustomWarn = Function() HintCustomWarn,
                    .PanCustomLocal = Function() PanCustomLocal,
                    .BtnCustomRefresh = Function() BtnCustomRefresh,
                    .BtnCustomFile = Function() BtnCustomFile,
                    .BtnCustomTutorial = Function() BtnCustomTutorial,
                    .BtnCustomFolder = Function() BtnCustomFolder,
                    .PanCustomNet = Function() PanCustomNet,
                    .TextCustomNet = Function() TextCustomNet,
                    .PanCustomPreset = Function() PanCustomPreset,
                    .CheckRecentWorlds = Function() CheckRecentWorlds,
                    .CardSwitch = Function() CardSwitch,
                    .CheckHiddenPageDownload = Function() CheckHiddenPageDownload,
                    .CheckHiddenPageLink = Function() CheckHiddenPageLink,
                    .CheckHiddenPageSetup = Function() CheckHiddenPageSetup,
                    .CheckHiddenPageOther = Function() CheckHiddenPageOther,
                    .CheckHiddenSetupLaunch = Function() CheckHiddenSetupLaunch,
                    .CheckHiddenSetupLink = Function() CheckHiddenSetupLink,
                    .CheckHiddenSetupUI = Function() CheckHiddenSetupUI,
                    .CheckHiddenSetupSystem = Function() CheckHiddenSetupSystem,
                    .CheckHiddenOtherHelp = Function() CheckHiddenOtherHelp,
                    .CheckHiddenOtherAbout = Function() CheckHiddenOtherAbout,
                    .CheckHiddenOtherTest = Function() CheckHiddenOtherTest,
                    .CheckLauncherEmail = Function() CheckLauncherEmail,
                    .CheckHiddenFunctionSelect = Function() CheckHiddenFunctionSelect,
                    .CheckHiddenFunctionModUpdate = Function() CheckHiddenFunctionModUpdate,
                    .CheckHiddenFunctionHidden = Function() CheckHiddenFunctionHidden,
                    .CheckHiddenFunctionModify = Function() CheckHiddenFunctionModify
                })
            End If
            Return _controller
        End Get
    End Property

    Private Sub PageSetupUI_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Controller.Loaded()
    End Sub

    Public Sub Refresh()
        Controller.Refresh()
    End Sub

    Public Sub Reset()
        Controller.Reset()
    End Sub

    Private Sub BtnUIBgOpen_Click(sender As Object, e As EventArgs) Handles BtnBackgroundOpen.Click
        Controller.OpenBackground()
    End Sub

    Private Sub BtnBackgroundRefresh_Click(sender As Object, e As EventArgs) Handles BtnBackgroundRefresh.Click
        Controller.RefreshBackground()
    End Sub

    Public Sub BackgroundRefreshUI(Show As Boolean, Count As Integer)
        Controller.BackgroundRefreshUI(Show, Count)
    End Sub

    Private Sub BtnBackgroundClear_Click(sender As Object, e As EventArgs) Handles BtnBackgroundClear.Click
        Controller.ClearBackground()
    End Sub

    Public Shared Sub BackgroundRefresh(IsHint As Boolean, Refresh As Boolean)
        SetupPageRuntime.UI.BackgroundRefresh(IsHint, Refresh)
    End Sub

    Private Sub BtnLogoChange_Click(sender As Object, e As EventArgs) Handles BtnLogoChange.Click
        Controller.ChangeLogo()
    End Sub

    Private Sub RadioLogoType3_Check(sender As Object, e As RouteEventArgs) Handles RadioLogoType3.PreviewCheck
        Controller.PreviewCustomLogo(e.RaiseByMouse, e.Handled)
    End Sub

    Private Sub BtnLogoDelete_Click(sender As Object, e As EventArgs) Handles BtnLogoDelete.Click
        Controller.DeleteLogo()
    End Sub

    Private Sub BtnMusicOpen_Click(sender As Object, e As EventArgs) Handles BtnMusicOpen.Click
        Controller.OpenMusic()
    End Sub

    Private Sub BtnMusicRefresh_Click(sender As Object, e As EventArgs) Handles BtnMusicRefresh.Click
        Controller.RefreshMusic()
    End Sub

    Public Sub MusicRefreshUI()
        Controller.MusicRefreshUI()
    End Sub

    Private Sub BtnMusicClear_Click(sender As Object, e As EventArgs) Handles BtnMusicClear.Click
        Controller.ClearMusic()
    End Sub

    Private Sub CheckMusicStart_Change() Handles CheckMusicStart.Change
        Controller.MusicStartChanged()
    End Sub

    Private Sub CheckMusicStop_Change() Handles CheckMusicStop.Change
        Controller.MusicStopChanged()
    End Sub

    Private Sub BtnCustomFile_Click(sender As Object, e As EventArgs) Handles BtnCustomFile.Click
        Controller.CreateCustomFile()
    End Sub

    Private Sub BtnCustomRefresh_Click() Handles BtnCustomRefresh.Click
        Controller.RefreshCustom()
    End Sub

    Private Sub BtnCustomTutorial_Click(sender As Object, e As EventArgs) Handles BtnCustomTutorial.Click
        Controller.CustomTutorial()
    End Sub

    Public Shared Sub OnMainPageTypeChanged()
        SetupPageRuntime.UI.OnMainPageTypeChanged()
    End Sub

    Private Sub BtnCustomFolder_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnCustomFolder.Click
        Controller.OpenCustomFolder()
    End Sub

    Public Shared Sub HiddenRefresh() Handles Me.Loaded
        SetupPageRuntime.UI.HiddenRefresh()
    End Sub

    Private Sub HiddenSetupMain() Handles CheckHiddenPageSetup.Change
        Controller.HiddenSetupMain()
    End Sub

    Private Sub HiddenSetupSub() Handles CheckHiddenSetupLaunch.Change, CheckHiddenSetupSystem.Change, CheckHiddenSetupLink.Change, CheckHiddenSetupUI.Change
        Controller.HiddenSetupSub()
    End Sub

    Private Sub HiddenOtherMain() Handles CheckHiddenPageOther.Change
        Controller.HiddenOtherMain()
    End Sub

    Private Sub HiddenOtherSub(sender As Object, user As Boolean) Handles CheckHiddenOtherHelp.Change, CheckHiddenOtherAbout.Change, CheckHiddenOtherTest.Change
        Controller.HiddenOtherSub(sender, user)
    End Sub

    Private Sub HiddenHint(sender As Object, user As Boolean) Handles CheckHiddenFunctionHidden.Change, CheckHiddenPageSetup.Change, CheckHiddenSetupUI.Change
        Controller.HiddenHint(sender, user)
    End Sub

    Private Sub SliderLoad()
        Controller.SliderLoad()
    End Sub

    Public Shared Property HiddenForceShow As Boolean
        Get
            Return SetupPageRuntime.UI.HiddenForceShow
        End Get
        Set(value As Boolean)
            SetupPageRuntime.UI.HiddenForceShow = value
        End Set
    End Property

End Class
