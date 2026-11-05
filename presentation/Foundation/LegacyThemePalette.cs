using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using PCLLF.Presentation.Controls;

namespace PCLLF.Presentation.Foundation;

// LF ModTheme.vb at 50b028da5fcad2226d74d901a41d8c43a249bfb2 (LF LICENCE).
// The sole palette notification/dark state is shared by native and old-name controls.
public static class LegacyThemePalette {
    public static bool IsDarkTheme { get; private set; }
    private static event EventHandler? PaletteChanged;
    public static readonly DependencyProperty BuiltInBackgroundProperty = DependencyProperty.RegisterAttached(
        "BuiltInBackground", typeof(bool), typeof(LegacyThemePalette), new PropertyMetadata(false));
    public static readonly DependencyProperty BackgroundAssetProperty = DependencyProperty.RegisterAttached(
        "BackgroundAsset", typeof(string), typeof(LegacyThemePalette), new PropertyMetadata(""));
    private static bool chromeRegistered;

    public static SolidColorBrush Brush(string key) => Application.Current?.TryFindResource(key) as SolidColorBrush ?? new SolidColorBrush(Colors.Gray);
    public static ControlColor Color(string key) {
        Color color = Brush(key).Color;
        return new ControlColor(color.A, color.R, color.G, color.B);
    }
    internal static void Prepare(bool dark) => IsDarkTheme = dark;
    public static string DefaultBackgroundResource() => "Branding/" + (IsDarkTheme ? "DefaultBackgroundNight.png" : "DefaultBackground.png");
    public static void MarkCustomBackground(System.Windows.Controls.Panel target) {
        target.SetValue(BuiltInBackgroundProperty, false);
        target.SetValue(BackgroundAssetProperty, "");
    }
    public static double BackgroundThemeOpacityFactor(UIElement target) {
        if (!IsDarkTheme) return 1.0;
        if (target is System.Windows.Controls.Panel && (bool)target.GetValue(BuiltInBackgroundProperty)) return 1.0;
        return 0.32;
    }
    internal static void PutBrush(object key, Color value) {
        ResourceDictionary resources = Application.Current!.Resources;
        if (resources[key] is SolidColorBrush existing && !existing.IsFrozen) existing.Color = value;
        else resources[key] = new SolidColorBrush(value);
    }
    internal static void PutColor(string suffix, Color value) {
        PutBrush("ColorBrush" + suffix, value);
        Application.Current!.Resources["ColorObject" + suffix] = value;
    }
    // Same loaded-only subscription lifetime and refresh order as LF; cached pages detach.
    public static void Watch(FrameworkElement element, Action refresh) {
        bool attached = false;
        EventHandler handler = (_, _) => refresh();
        element.Loaded += (_, _) => {
            if (attached) return;
            attached = true;
            PaletteChanged += handler;
            refresh();
        };
        element.Unloaded += (_, _) => {
            if (!attached) return;
            attached = false;
            PaletteChanged -= handler;
        };
    }
    internal static void Notify(Action<int> animationSuppression) {
        animationSuppression(1);
        try { PaletteChanged?.Invoke(null, EventArgs.Empty); }
        finally { animationSuppression(-1); }
    }
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
    private static void ApplyWindowChrome(Window window) {
        try {
            IntPtr handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero) return;
            int value = IsDarkTheme ? 1 : 0;
            if (DwmSetWindowAttribute(handle, 20, ref value, 4) != 0) DwmSetWindowAttribute(handle, 19, ref value, 4);
        } catch (DllNotFoundException) {
        } catch (EntryPointNotFoundException) { }
    }
    internal static void ApplyWindowChrome() {
        if (!chromeRegistered) {
            chromeRegistered = true;
            EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent,
                new RoutedEventHandler((sender, _) => ApplyWindowChrome((Window)sender)));
        }
        foreach (Window window in Application.Current!.Windows) ApplyWindowChrome(window);
    }
}
