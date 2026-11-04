using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PCLLF.Presentation.Foundation;
namespace PCL;
internal static class ModTheme {
    private static readonly DependencyProperty BuiltInBackgroundProperty = LegacyThemePalette.BuiltInBackgroundProperty.AddOwner(typeof(ModTheme));
    private static readonly DependencyProperty BackgroundAssetProperty = LegacyThemePalette.BackgroundAssetProperty.AddOwner(typeof(ModTheme));
    internal static bool IsDarkTheme => LegacyThemePalette.IsDarkTheme;
    internal static string DefaultBackgroundResource() => LegacyThemePalette.DefaultBackgroundResource();
    internal static void UseDefaultBackground(Panel target) => ModSecret.PaletteService.UseDefaultBackground(target);
    internal static void MarkCustomBackground(Panel target) => LegacyThemePalette.MarkCustomBackground(target);
    internal static void RefreshDefaultBackground(Panel target) => ModSecret.PaletteService.RefreshDefaultBackground(target);
    internal static double BackgroundThemeOpacityFactor(UIElement target) => LegacyThemePalette.BackgroundThemeOpacityFactor(target);
    internal static SolidColorBrush ThemeBrush(string key) => LegacyThemePalette.Brush(key);
    internal static ModBase.MyColor ThemeColor(string key) => new(LegacyThemePalette.Brush(key));
    internal static void RefreshPanelOpacity() => ModSecret.PaletteService.RefreshPanelOpacity();
    internal static void PrepareThemeTones(bool value) => ModSecret.PaletteService.PrepareThemeTones(value);
    internal static void ApplyThemePalette() => ModSecret.PaletteService.ApplyThemePalette();
    internal static void WatchTheme(FrameworkElement element, Action refresh) => LegacyThemePalette.Watch(element, refresh);
    internal static void NotifyThemeChanged() => ModSecret.PaletteService.NotifyThemeChanged();
}
