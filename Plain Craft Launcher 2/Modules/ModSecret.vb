Imports ThemeHost = PCLLF.Presentation.Foundation.LegacyThemeHost(Of PCL.ModBase.MyColor)
Imports ThemeSlot = PCLLF.Presentation.Foundation.LegacyThemeColorSlot(Of PCL.ModBase.MyColor)
Imports ThemeService = PCLLF.Presentation.Foundation.LegacyThemeService(Of PCL.ModBase.MyColor)

'由于包含加解密等安全信息，本文件中的部分代码已被删除

Friend Module ModSecret

    '标注 PCL 的不同分支，仅用于替换标记
    Public Const VersionBranchMain As String = "OpenSource"
    '在开源版的注册表与常规版的注册表隔离，以防数据冲突
    Public Const RegFolder As String = "PCLDebug"
    '用于微软登录的 ClientId
    Public ReadOnly Property OAuthClientId As String
        Get
            Return BuildCredentials.ReadMicrosoftClientId()
        End Get
    End Property
    'CurseForge API Key
    Public ReadOnly Property CurseForgeAPIKey As String
        Get
            Return BuildCredentials.ReadCurseForgeKey()
        End Get
    End Property

    Friend Function SecretCdnSign(UrlWithMark As String) As String
        Return PCLCS.LegacySecretNetwork.CdnSign(UrlWithMark)
    End Function
    Friend Sub SecretHeadersSign(Url As String, ByRef Req As HttpRequestMessage, Optional SimulateBrowserHeaders As Boolean = False, Optional IncludeCredentials As Boolean = True)
        NetworkSigner.SignHeaders(Url, Req, SimulateBrowserHeaders, IncludeCredentials)
    End Sub
    '固定默认外观；旧自定义主题配置保留存储，不在本版本启用调色。
    Public Const DefaultThemeHue As Integer = 225
    Public Const DefaultThemeSat As Integer = 100
    Public Const DefaultThemeDelta As Integer = 115
    Public Const DefaultThemeLight As Integer = 40
    Public Const DefaultBackgroundOpacity As Integer = 748

    Public Color1 As New MyColor(52, 61, 74)
    Public Color2 As New MyColor(11, 91, 203)
    Public Color3 As New MyColor(19, 112, 243)
    Public Color4 As New MyColor(72, 144, 245)
    Public Color5 As New MyColor(150, 192, 249)
    Public Color6 As New MyColor(213, 230, 253)
    Public Color7 As New MyColor(222, 236, 253)
    Public Color8 As New MyColor(234, 242, 254)
    Public ColorBg0 As New MyColor(150, 192, 249)
    Public ColorBg1 As New MyColor(190, Color7)
    Public ColorGray1 As New MyColor(64, 64, 64)
    Public ColorGray2 As New MyColor(115, 115, 115)
    Public ColorGray3 As New MyColor(140, 140, 140)
    Public ColorGray4 As New MyColor(166, 166, 166)
    Public ColorGray5 As New MyColor(204, 204, 204)
    Public ColorGray6 As New MyColor(235, 235, 235)
    Public ColorGray7 As New MyColor(240, 240, 240)
    Public ColorGray8 As New MyColor(245, 245, 245)
    Public ColorSemiTransparent As New MyColor(1, Color8)

    Public ColorHue As Integer = DefaultThemeHue, ColorSat As Integer = DefaultThemeSat, ColorLightAdjust As Integer = DefaultThemeLight - 20, ColorHueTopbarDelta As OneOf(Of Integer, Integer()) = DefaultThemeDelta - 90


    Private ReadOnly NetworkSigner As New PCLCS.LegacySecretNetwork(New PCLCS.LegacySecretNetworkHost With {
        .VersionName = Function() VersionBaseName, .BuildType = Function() CInt(BuildType), .VersionCode = Function() VersionCode,
        .CurseForgeKey = Function() CurseForgeAPIKey, .IsCurseForgeRequest = AddressOf BuildCredentials.IsCurseForgeRequest})
    Private ReadOnly IdentityService As New PCLCS.LegacyDeviceIdentify(New PCLCS.LegacyDeviceIdentifyHost With {
        .ApplicationStartTick = Function() ApplicationStartTick, .Milliseconds = AddressOf GetTimeMs,
        .AvailablePhysicalMemory = Function() My.Computer.Info.AvailablePhysicalMemory,
        .ReadHardware = Function() RegistryUtils.TryRead("HKEY_LOCAL_MACHINE\SYSTEM\HardwareConfig", "LastConfig", "Unknown"),
        .ReadSeed = Function() Settings.Get(Of String)("Identify"), .WriteSeed = Sub(Value) Settings.Set("Identify", Value),
        .SourceIdentify = AddressOf Settings.GetPclSourceIdentify, .Warn = Sub(Text) Logger.Warn(Text)})
    Private ReadOnly PaletteService As New ThemeService(CreateThemeHost())
    Private Function ThemeField(Read As Func(Of MyColor), Write As Action(Of MyColor)) As ThemeSlot
        Return New ThemeSlot With {.Read = Read, .Write = Write}
    End Function
    Private Function CreateThemeHost() As ThemeHost
        Return New ThemeHost With {
            .Color1 = ThemeField(Function() Color1, Sub(Value) Color1 = Value),
            .Color2 = ThemeField(Function() Color2, Sub(Value) Color2 = Value),
            .Color3 = ThemeField(Function() Color3, Sub(Value) Color3 = Value),
            .Color4 = ThemeField(Function() Color4, Sub(Value) Color4 = Value),
            .Color5 = ThemeField(Function() Color5, Sub(Value) Color5 = Value),
            .Color6 = ThemeField(Function() Color6, Sub(Value) Color6 = Value),
            .Color7 = ThemeField(Function() Color7, Sub(Value) Color7 = Value),
            .Color8 = ThemeField(Function() Color8, Sub(Value) Color8 = Value),
            .Bg0 = ThemeField(Function() ColorBg0, Sub(Value) ColorBg0 = Value),
            .Bg1 = ThemeField(Function() ColorBg1, Sub(Value) ColorBg1 = Value),
            .Gray4 = ThemeField(Function() ColorGray4, Sub(Value) ColorGray4 = Value),
            .SemiTransparent = ThemeField(Function() ColorSemiTransparent, Sub(Value) ColorSemiTransparent = Value),
            .DefaultHue = DefaultThemeHue, .DefaultSaturation = DefaultThemeSat, .DefaultLight = DefaultThemeLight, .DefaultDelta = DefaultThemeDelta,
            .Hue = Function() ColorHue, .Saturation = Function() ColorSat, .Light = Function() ColorLightAdjust,
            .SetHue = Sub(Value) ColorHue = Value, .SetSaturation = Sub(Value) ColorSat = Value, .SetLight = Sub(Value) ColorLightAdjust = Value,
            .Delta = Function() ColorHueTopbarDelta, .SetDelta = Sub(Value) ColorHueTopbarDelta = Value,
            .Hsl = Function(H, S, L) New MyColor().FromHSL2(H, S, L), .Rgb = Function(R, G, B) New MyColor(R, G, B),
            .Alpha = Function(A, Color) New MyColor(A, Color), .Channels = Function(Color) New MyColor(Color.A, Color.R, Color.G, Color.B),
            .Resources = Function() Application.Current.Resources, .RunInUi = Sub(Work) RunInUi(Work),
            .MainLoaded = Function() FrmMain IsNot Nothing AndAlso FrmMain.IsLoaded,
            .Colorful = Function() Settings.Get(Of Boolean)("UiBackgroundColorful"), .BackgroundMode = Function() Settings.Get(Of Integer)("UiBackgroundMode"),
            .SetTitleBackground = Sub(Brush) FrmMain.PanTitle.Background = Brush, .TitleBackground = Function() FrmMain.PanTitle.Background,
            .MainPanel = Function() FrmMain.PanForm, .BackgroundImage = Function() FrmMain.ImgBack,
            .ErrorToast = Sub(Err, Text) Logger.Error(Err, Text, LogBehavior.Toast)}
    End Function
    Public Sub ThemeRefresh(Optional NewTheme As Integer = -1)
        PaletteService.Refresh(NewTheme)
    End Sub
    Public Sub ThemeRefreshMain()
        PaletteService.RefreshMain()
    End Sub
    Friend Sub ApplyMainBackground(Panel As Panel, Image As UIElement)
        PaletteService.ApplyBackground(Panel, Image)
    End Sub
    Friend Sub UpdateCheckByButton()
        ModLauncherUpdate.CheckForUpdates(False)
    End Sub
#If LEGACY_LINK Then
#Region "旧联机配置预留"

    ''' <summary>
    ''' 联网获取的配置信息。
    ''' 若获取失败或仍在获取中，可能为 Nothing。
    ''' </summary>
    Public ServerConfig As JObject

    Public ServerLoader As New LoaderTask(Of Integer, Integer)("PCL 配置更新", Sub() Logger.Info("该版本中不包含更新通知功能……"), Priority:=ThreadPriority.BelowNormal) With
        {.ReloadTimeout = 1000 * 60 * 60} '超时 1 小时

#End Region
#End If

    Friend Function GetIdentify() As String
        Return IdentityService.Read()
    End Function
End Module
