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

#Region "网络鉴权"

    Friend Function SecretCdnSign(UrlWithMark As String) As String
        If Not UrlWithMark.EndsWithF("{CDN}") Then Return UrlWithMark
        Return UrlWithMark.Replace("{CDN}", "").Replace(" ", "%20")
    End Function
    ''' <summary>
    ''' 设置 Headers 的 UA、Referer。
    ''' </summary>
    Friend Sub SecretHeadersSign(Url As String, ByRef Req As HttpRequestMessage, Optional SimulateBrowserHeaders As Boolean = False, Optional IncludeCredentials As Boolean = True)
        If Not Req.Headers.UserAgent.Any Then
            If Url.Contains("baidupcs.com") OrElse Url.Contains("baidu.com") Then
                Req.Headers.Add("User-Agent", "LogStatistic")  '#4951
            ElseIf SimulateBrowserHeaders Then
                Req.Headers.Add("User-Agent", $"PCL-Living-Fork/{VersionBaseName}.{CInt(BuildType)} Mozilla/5.0 AppleWebKit/537.36 Chrome/63.0.3239.132 Safari/537.36")
            Else
                Req.Headers.Add("User-Agent", $"PCL-Living-Fork/{VersionBaseName}.{CInt(BuildType)}")
            End If
        End If
        If Not SimulateBrowserHeaders Then Req.Headers.Add("Referer", $"http://{VersionCode}.open.pcl2.server/")
        If IncludeCredentials AndAlso BuildCredentials.IsCurseForgeRequest(Req.RequestUri) AndAlso Not Req.Headers.Contains("x-api-key") Then
            Dim ApiKey = CurseForgeAPIKey
            If Not String.IsNullOrEmpty(ApiKey) Then Req.Headers.Add("x-api-key", ApiKey)
        End If
    End Sub

#End Region

#Region "主题"

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

    Public Sub ThemeRefresh(Optional NewTheme As Integer = -1)
        If Application.Current Is Nothing Then Return
        If Not Application.Current.Dispatcher.CheckAccess() Then
            RunInUiWait(Sub() ThemeRefresh(NewTheme)) : Return
        End If
        Try
            PrepareThemeTones(Settings.Get(Of Integer)("UiLauncherColorMode") = 1)
            ColorHue = DefaultThemeHue
            ColorSat = DefaultThemeSat
            ColorLightAdjust = DefaultThemeLight - 20
            ColorHueTopbarDelta = DefaultThemeDelta - 90

            Color1 = New MyColor().FromHSL2(ColorHue, ColorSat * 0.2, 25 + ColorLightAdjust * 0.3)
            Color2 = New MyColor().FromHSL2(ColorHue, ColorSat, 45 + ColorLightAdjust)
            Color3 = New MyColor().FromHSL2(ColorHue, ColorSat, 55 + ColorLightAdjust)
            Color4 = New MyColor().FromHSL2(ColorHue, ColorSat, 65 + ColorLightAdjust)
            Color5 = New MyColor().FromHSL2(ColorHue, ColorSat, 80 + ColorLightAdjust * 0.4)
            Color6 = New MyColor().FromHSL2(ColorHue, ColorSat, 91 + ColorLightAdjust * 0.1)
            Color7 = New MyColor().FromHSL2(ColorHue, ColorSat, 95)
            Color8 = New MyColor().FromHSL2(ColorHue, ColorSat, 97)
            ColorBg0 = Color4 * 0.4 + Color5 * 0.4 + ColorGray4 * 0.2
            ColorBg1 = New MyColor(190, Color7)

            ApplyThemePalette()
            ThemeRefreshMain()
            NotifyThemeChanged()
        Catch ex As Exception
            Logger.Error(ex, "刷新主题颜色失败", LogBehavior.Toast)
        End Try
    End Sub
    Friend Function CreateTitleBackground() As Brush
        Dim Result = New LinearGradientBrush With {.EndPoint = New Point(1, 0), .StartPoint = New Point(0, 0)}
        Dim Deltas = ColorHueTopbarDelta.Switch(Function(d) New Integer() {-d, 0, d}, Function(d) d)
        Dim Saturation = If(IsDarkTheme, 72, ColorSat), HueScale = If(IsDarkTheme, 0.5, 1.0)
        Result.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = New MyColor().FromHSL2(ColorHue + Deltas(0) * HueScale, Saturation, If(IsDarkTheme, 20, 48 + ColorLightAdjust))})
        Result.GradientStops.Add(New GradientStop With {.Offset = 0.5, .Color = New MyColor().FromHSL2(ColorHue + Deltas(1) * HueScale, Saturation, If(IsDarkTheme, 24, 54 + ColorLightAdjust))})
        Result.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = New MyColor().FromHSL2(ColorHue + Deltas(2) * HueScale, Saturation, If(IsDarkTheme, 20, 48 + ColorLightAdjust))})
        Result.Freeze()
        Return Result
    End Function
    Public Sub ThemeRefreshMain()
        RunInUi(
        Sub()
            If FrmMain Is Nothing OrElse Not FrmMain.IsLoaded Then Return
            '顶部条背景
            FrmMain.PanTitle.Background = CreateTitleBackground()
            ApplyMainBackground(FrmMain.PanForm, FrmMain.ImgBack)
        End Sub)
    End Sub

    Friend Sub ApplyMainBackground(Panel As Panel, Image As UIElement)
        Dim Plain = Settings.Get(Of Integer)("UiBackgroundMode") = 1
        If TypeOf Image Is Panel Then RefreshDefaultBackground(DirectCast(Image, Panel))
        Image.Visibility = If(Plain, Visibility.Collapsed, Visibility.Visible)
        Image.Opacity = Settings.Get(Of Integer)("UiBackgroundOpacity") / 1000 * BackgroundThemeOpacityFactor(Image)
        If Plain Then
            Panel.Background = ThemeBrush(If(IsDarkTheme, "ColorBrushBackground", "ColorBrushWhite"))
        ElseIf Settings.Get(Of Boolean)("UiBackgroundColorful") Then
            Dim Brush As New LinearGradientBrush With {.EndPoint = New Point(0.1, 1), .StartPoint = New Point(0.9, 0)}
            Brush.GradientStops.Add(New GradientStop With {.Offset = -0.1, .Color = New MyColor().FromHSL2(ColorHue - 15, If(IsDarkTheme, 15, ColorSat * 0.8), If(IsDarkTheme, 11, 91))})
            Brush.GradientStops.Add(New GradientStop With {.Offset = 0.4, .Color = New MyColor().FromHSL2(ColorHue, If(IsDarkTheme, 15, ColorSat * 0.8), If(IsDarkTheme, 14, 91))})
            Brush.GradientStops.Add(New GradientStop With {.Offset = 1.1, .Color = New MyColor().FromHSL2(ColorHue + 15, If(IsDarkTheme, 15, ColorSat * 0.8), If(IsDarkTheme, 11, 91))})
            Panel.Background = Brush
        Else
            Panel.Background = ThemeBrush("ColorBrushBackground")
        End If
    End Sub

#End Region

#Region "更新"

    Friend Sub UpdateCheckByButton()
        ModLauncherUpdate.CheckForUpdates(False)
    End Sub

#End Region

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

#Region "兼容设备标识"

    ''' <summary>
    ''' 获取设备识别码。
    ''' </summary>
    Friend Function GetIdentify() As String
        Try
            If ApplicationStartTick < 1 Then Return "0000-0000-0000-0000"
            Dim Hardware As String = "Unknown"
            Try
                Hardware = Convert.ToString(RegistryUtils.TryRead("HKEY_LOCAL_MACHINE\SYSTEM\HardwareConfig", "LastConfig", "Unknown"), Globalization.CultureInfo.InvariantCulture)
            Catch
            End Try
            Dim Seed = Settings.Get(Of String)("Identify")
            If Seed.Length < 3 Then
                Seed = CStr(GetTimeMs()) & CStr(My.Computer.Info.AvailablePhysicalMemory)
                Settings.Set("Identify", Seed)
            End If
            Return Settings.GetPclSourceIdentify(Hardware, Seed)
        Catch
            Logger.Warn("无法读取官方 PCL 设备标识。")
            Return "0000-0000-0000-0000"
        End Try
    End Function

#End Region

End Module
