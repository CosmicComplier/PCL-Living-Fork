Imports System.Windows
Imports System.Windows.Media
Imports System.Runtime.InteropServices
Imports System.Windows.Interop

'Host palette for the existing PCL controls. Images, skins and world previews keep their original colors.
Friend Module ModTheme
    Friend Property IsDarkTheme As Boolean
        Get
            Return Dark
        End Get
        Private Set(Value As Boolean)
            Dark = Value
        End Set
    End Property
    Private Dark As Boolean
    Private Event PaletteChanged As EventHandler
    Private ReadOnly BuiltInBackgroundProperty As DependencyProperty = DependencyProperty.RegisterAttached("BuiltInBackground", GetType(Boolean), GetType(ModTheme), New PropertyMetadata(False))
    Private ReadOnly BackgroundAssetProperty As DependencyProperty = DependencyProperty.RegisterAttached("BackgroundAsset", GetType(String), GetType(ModTheme), New PropertyMetadata(""))

    Friend Function DefaultBackgroundResource() As String
        Return "Branding/" & If(IsDarkTheme, "DefaultBackgroundNight.png", "DefaultBackground.png")
    End Function
    Friend Sub UseDefaultBackground(Target As Panel)
        Target.SetValue(BuiltInBackgroundProperty, True)
        Target.SetValue(BackgroundAssetProperty, "")
        RefreshDefaultBackground(Target)
    End Sub
    Friend Sub MarkCustomBackground(Target As Panel)
        Target.SetValue(BuiltInBackgroundProperty, False)
        Target.SetValue(BackgroundAssetProperty, "")
    End Sub
    Friend Sub RefreshDefaultBackground(Target As Panel)
        If Not CBool(Target.GetValue(BuiltInBackgroundProperty)) Then Return
        Dim Resource = DefaultBackgroundResource()
        If CStr(Target.GetValue(BackgroundAssetProperty)) = Resource Then Return
        Dim Picture As ImageBrush = New MyBitmap(PathImage & Resource)
        Dim Current = TryCast(Target.Background, ImageBrush)
        If Current IsNot Nothing Then
            If Current.IsFrozen Then Current = Current.CloneCurrentValue()
            Current.ImageSource = Picture.ImageSource
            Target.Background = Current
        Else
            Target.Background = Picture
        End If
        Target.SetValue(BackgroundAssetProperty, Resource)
    End Sub
    Friend Function BackgroundThemeOpacityFactor(Target As UIElement) As Double
        If Not IsDarkTheme Then Return 1.0
        'The night illustration already has its own exposure; custom photos retain automatic dimming.
        If TypeOf Target Is Panel AndAlso CBool(Target.GetValue(BuiltInBackgroundProperty)) Then Return 1.0
        Return 0.32
    End Function
    Private ChromeRegistered As Boolean
    <DllImport("dwmapi.dll")>
    Private Function DwmSetWindowAttribute(Window As IntPtr, Attribute As Integer, ByRef Value As Integer, Size As Integer) As Integer
    End Function
    Private Sub ApplyWindowChrome(Window As Window)
        Try
            Dim Handle = New WindowInteropHelper(Window).Handle
            If Handle = IntPtr.Zero Then Return
            Dim Value = If(IsDarkTheme, 1, 0)
            If DwmSetWindowAttribute(Handle, 20, Value, 4) <> 0 Then DwmSetWindowAttribute(Handle, 19, Value, 4)
        Catch ex As DllNotFoundException
        Catch ex As EntryPointNotFoundException
        End Try
    End Sub

    Friend Function ThemeBrush(Key As String) As SolidColorBrush
        Return If(TryCast(Application.Current?.TryFindResource(Key), SolidColorBrush), New SolidColorBrush(Colors.Gray))
    End Function
    Friend Function ThemeColor(Key As String) As MyColor
        Return New MyColor(ThemeBrush(Key))
    End Function
    Private Function Tone(Value As String) As MyColor
        Return New MyColor(Value)
    End Function
    Private Sub PutBrush(Key As Object, Value As Color)
        Dim Existing = TryCast(Application.Current.Resources(Key), SolidColorBrush)
        If Existing IsNot Nothing AndAlso Not Existing.IsFrozen Then
            Existing.Color = Value
        Else
            Application.Current.Resources(Key) = New SolidColorBrush(Value)
        End If
    End Sub
    Private Sub PutColor(Suffix As String, Value As MyColor)
        PutBrush("ColorBrush" & Suffix, CType(Value, Color))
        Application.Current.Resources("ColorObject" & Suffix) = CType(Value, Color)
    End Sub
    Friend Sub RefreshPanelOpacity()
        If Application.Current Is Nothing Then Return
        If Not Application.Current.Dispatcher.CheckAccess() Then
            RunInUiWait(AddressOf RefreshPanelOpacity) : Return
        End If
        Dim Value = Math.Max(0, Math.Min(1000, Settings.Get(Of Integer)("UiPanelOpacity")))
        Dim Alpha = CByte(Math.Round(Value * 255.0 / 1000))
        For Each Role In {"Card", "BackgroundTransparentSidebar"}
            Dim Current = ThemeBrush("ColorBrush" & Role).Color
            PutColor(Role, New MyColor(Alpha, Current.R, Current.G, Current.B))
        Next
    End Sub

    Friend Sub PrepareThemeTones(Value As Boolean)
        IsDarkTheme = Value
        Dim Grays = If(Value,
            {"#DCE2EB", "#B4BECD", "#A1ACBD", "#7F8B9E", "#586275", "#3C4656", "#303846", "#272F3B"},
            {"#404040", "#737373", "#8C8C8C", "#A6A6A6", "#CCCCCC", "#EBEBEB", "#F0F0F0", "#F5F5F5"})
        ColorGray1 = Tone(Grays(0)) : ColorGray2 = Tone(Grays(1)) : ColorGray3 = Tone(Grays(2)) : ColorGray4 = Tone(Grays(3))
        ColorGray5 = Tone(Grays(4)) : ColorGray6 = Tone(Grays(5)) : ColorGray7 = Tone(Grays(6)) : ColorGray8 = Tone(Grays(7))
    End Sub
    Friend Sub ApplyThemePalette()
        If IsDarkTheme Then
            Color1 = Tone("#DCE2EB") : Color2 = Tone("#AABFFF") : Color3 = Tone("#91AAFF") : Color4 = Tone("#ADC2FF")
            Color5 = Tone("#6583B7") : Color6 = Tone("#394A69") : Color7 = Tone("#2B3851") : Color8 = Tone("#252F44")
            ColorBg0 = Tone("#7894C5") : ColorBg1 = Tone("#BE252F44")
        End If
        ColorSemiTransparent = New MyColor(1, Color8)
        Dim Main = {Color1, Color2, Color3, Color4, Color5, Color6, Color7, Color8}
        Dim Grays = {ColorGray1, ColorGray2, ColorGray3, ColorGray4, ColorGray5, ColorGray6, ColorGray7, ColorGray8}
        For Index = 0 To 7
            PutColor((Index + 1).ToString(), Main(Index))
            PutColor("Gray" & (Index + 1), Grays(Index))
        Next
        PutColor("Bg0", ColorBg0) : PutColor("Bg1", ColorBg1) : PutColor("SemiTransparent", ColorSemiTransparent)
        Dim Roles As New Dictionary(Of String, String()) From {
            {"White", {"#FFFFFF", "#252932"}}, {"HalfWhite", {"#55FFFFFF", "#55252932"}}, {"SemiWhite", {"#BBFFFFFF", "#BB252932"}},
            {"Card", {"#F5FFFFFF", "#F5252932"}}, {"Dialog", {"#FBFBFB", "#252A34"}}, {"Popup", {"#FFFFFF", "#2B303B"}},
            {"Background", {"#F5F5F5", "#171B22"}}, {"BackgroundTransparentSidebar", {"#F1FFFFFF", "#F11C2028"}},
            {"TextSecondary", {"#5C5C5C", "#B4BECD"}}, {"OnAccent", {"#FFFFFF", "#141B2B"}},
            {"TitleForeground", {"#FFFFFF", "#DCE2EB"}}, {"TitleSelected", {"#FFFFFF", "#303B52"}},
            {"RedBack", {"#80FBDDDD", "#804E252C"}}, {"RedLight", {"#FF4C4C", "#FF9A9F"}}, {"RedDark", {"#CE2111", "#FF8B91"}},
            {"InfoBack", {"#D9ECFF", "#24364A"}}, {"InfoBorder", {"#1172D4", "#6AAFF3"}}, {"InfoText", {"#0F64B8", "#A9D5FF"}},
            {"WarningBack", {"#FFEBD7", "#413421"}}, {"WarningBorder", {"#F57A00", "#DAAE58"}}, {"WarningText", {"#D86C00", "#F4CE8A"}},
            {"DangerBack", {"#FFDDDF", "#43272D"}}, {"DangerBorder", {"#D82929", "#EA818B"}}, {"DangerText", {"#BF0B0B", "#FFB3BA"}},
            {"SuccessText", {"#218A43", "#8ED8AF"}}, {"Shadow", {"#343D4A", "#000000"}}}
        For Each Role In Roles
            PutColor(Role.Key, Tone(Role.Value(If(IsDarkTheme, 1, 0))))
        Next
        RefreshPanelOpacity()
        ModernTooltipTheme.Install()
        'Default WPF controls, popups and secondary windows also inherit the host palette.
        PutBrush(SystemColors.WindowBrushKey, ThemeBrush("ColorBrushWhite").Color)
        PutBrush(SystemColors.WindowTextBrushKey, CType(Color1, Color))
        PutBrush(SystemColors.ControlBrushKey, ThemeBrush("ColorBrushWhite").Color)
        PutBrush(SystemColors.ControlTextBrushKey, CType(Color1, Color))
        PutBrush(SystemColors.GrayTextBrushKey, CType(ColorGray4, Color))
        PutBrush(SystemColors.HighlightBrushKey, CType(Color3, Color))
        PutBrush(SystemColors.HighlightTextBrushKey, ThemeBrush("ColorBrushOnAccent").Color)
        PutBrush(SystemColors.MenuBrushKey, ThemeBrush("ColorBrushPopup").Color)
        PutBrush(SystemColors.MenuTextBrushKey, CType(Color1, Color))
        PutBrush(SystemColors.InfoBrushKey, ThemeBrush("ColorBrushPopup").Color)
        PutBrush(SystemColors.InfoTextBrushKey, CType(Color1, Color))
        PutBrush(SystemColors.ControlLightBrushKey, CType(ColorGray6, Color))
        PutBrush(SystemColors.ControlLightLightBrushKey, ThemeBrush("ColorBrushWhite").Color)
        PutBrush(SystemColors.ControlDarkBrushKey, CType(ColorGray5, Color))
        PutBrush(SystemColors.ControlDarkDarkBrushKey, CType(ColorGray7, Color))
        If Not ChromeRegistered Then
            ChromeRegistered = True
            EventManager.RegisterClassHandler(GetType(Window), FrameworkElement.LoadedEvent, New RoutedEventHandler(Sub(Sender, Args) ApplyWindowChrome(DirectCast(Sender, Window))))
        End If
        For Each Window As Window In Application.Current.Windows
            ApplyWindowChrome(Window)
        Next
    End Sub

    'Subscriptions only last while a control is loaded; cached/unloaded pages do not leak event handlers.
    Friend Sub WatchTheme(Element As FrameworkElement, Refresh As Action)
        Dim Attached As Boolean
        Dim Handler As EventHandler = Sub() Refresh()
        AddHandler Element.Loaded, Sub()
            If Attached Then Return
            Attached = True : AddHandler PaletteChanged, Handler
            Refresh()
        End Sub
        AddHandler Element.Unloaded, Sub()
            If Not Attached Then Return
            Attached = False : RemoveHandler PaletteChanged, Handler
        End Sub
    End Sub
    Friend Sub NotifyThemeChanged()
        AniControlEnabled += 1
        Try
            RaiseEvent PaletteChanged(Nothing, EventArgs.Empty)
        Finally
            AniControlEnabled -= 1
        End Try
    End Sub
End Module
