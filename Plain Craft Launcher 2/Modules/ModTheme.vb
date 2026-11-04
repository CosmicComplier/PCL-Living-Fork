Imports PCLLF.Presentation.Foundation

'原名薄桥，palette事件、状态、DWM与背景业务只有 C# 单份实现。
Friend Module ModTheme
    Private ReadOnly BuiltInBackgroundProperty As DependencyProperty = LegacyThemePalette.BuiltInBackgroundProperty.AddOwner(GetType(ModTheme))
    Private ReadOnly BackgroundAssetProperty As DependencyProperty = LegacyThemePalette.BackgroundAssetProperty.AddOwner(GetType(ModTheme))
    Friend ReadOnly Property IsDarkTheme As Boolean
        Get
            Return LegacyThemePalette.IsDarkTheme
        End Get
    End Property
    Friend Function DefaultBackgroundResource() As String
        Return LegacyThemePalette.DefaultBackgroundResource()
    End Function
    Friend Sub UseDefaultBackground(Target As Panel)
        ModSecret.PaletteService.UseDefaultBackground(Target)
    End Sub
    Friend Sub MarkCustomBackground(Target As Panel)
        LegacyThemePalette.MarkCustomBackground(Target)
    End Sub
    Friend Sub RefreshDefaultBackground(Target As Panel)
        ModSecret.PaletteService.RefreshDefaultBackground(Target)
    End Sub
    Friend Function BackgroundThemeOpacityFactor(Target As UIElement) As Double
        Return LegacyThemePalette.BackgroundThemeOpacityFactor(Target)
    End Function
    Friend Function ThemeBrush(Key As String) As SolidColorBrush
        Return LegacyThemePalette.Brush(Key)
    End Function
    Friend Function ThemeColor(Key As String) As MyColor
        Return New MyColor(LegacyThemePalette.Brush(Key))
    End Function
    Friend Sub RefreshPanelOpacity()
        ModSecret.PaletteService.RefreshPanelOpacity()
    End Sub
    Friend Sub PrepareThemeTones(Value As Boolean)
        ModSecret.PaletteService.PrepareThemeTones(Value)
    End Sub
    Friend Sub ApplyThemePalette()
        ModSecret.PaletteService.ApplyThemePalette()
    End Sub
    Friend Sub WatchTheme(Element As FrameworkElement, Refresh As Action)
        LegacyThemePalette.Watch(Element, Refresh)
    End Sub
    Friend Sub NotifyThemeChanged()
        ModSecret.PaletteService.NotifyThemeChanged()
    End Sub
End Module
