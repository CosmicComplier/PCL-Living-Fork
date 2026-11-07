using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using PCL;

namespace PCLLF.Native.Facade.Tests;

internal static class ThemeTransitionChecks {
    internal static void Run(FixtureEnvironment environment) {
        int suppression = ModAnimation.AniControlEnabled;
        System.Collections.IDictionary cache = Settings.Entries["SystemDebugAnim"].ValueCache;
        var saved = new List<System.Collections.DictionaryEntry>();
        foreach (System.Collections.DictionaryEntry entry in cache) saved.Add(entry);
        var app = System.Windows.Application.Current;
        Window? previousMain = app.MainWindow;
        ShutdownMode previousMode = app.ShutdownMode;
        var root = new Grid { Background = Brushes.White };
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(48) });
        root.RowDefinitions.Add(new RowDefinition());
        root.Children.Add(new TextBlock { Text = "PCL Living Fork", Foreground = Brushes.Navy });
        var window = new Window { Width = 500, Height = 280, Content = root, Left = -30000, Top = -30000, ShowActivated = false, ShowInTaskbar = false, WindowStyle = WindowStyle.None };
        Image? Overlay() => root.Children.OfType<Image>().SingleOrDefault(image => Equals(image.Tag, "theme-transition"));
        void Apply(Brush brush) => Native.Static("PCL.ThemeTransition", "Apply", root, (Action)(() => root.Background = brush));
        try {
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            ModAnimation.AniControlEnabled = 0; Native.Cache("SystemDebugAnim", 9);
            window.Show(); window.UpdateLayout();
            Apply(Brushes.MidnightBlue);
            if (SystemParameters.ClientAreaAnimation) {
                Image first = Overlay() ?? throw new InvalidOperationException("Theme switch has no crossfade frame.");
                Native.Check(!first.IsHitTestVisible && !first.Focusable && Grid.GetRowSpan(first) == 2 && ReferenceEquals(root.Background, Brushes.MidnightBlue),
                    "Crossfade delayed actual palette changes, intercepted input, or failed to cover all rows.");
                VisualAssetChecks.Pump(() => first.HasAnimatedProperties && first.Opacity is > 0 and < .95, "Theme crossfade did not interpolate opacity.");
                Apply(Brushes.White);
                Image replacement = Overlay() ?? throw new InvalidOperationException("Interrupted theme switch lost its composited frame.");
                Native.Check(!ReferenceEquals(first, replacement) && first.Source is null && !first.HasAnimatedProperties,
                    "Repeated toggling retained the previous snapshot/clock.");
                VisualAssetChecks.Pump(() => Overlay() is null, "Theme crossfade did not release its frame after completion.");
                Native.Check(replacement.Source is null && !replacement.HasAnimatedProperties, "Completed crossfade retained bitmap memory or opacity animation.");
                Apply(Brushes.MidnightBlue);
                root.Visibility = Visibility.Collapsed;
                Native.Check(Overlay() is null, "Hiding the page retained the transition snapshot.");
                root.Visibility = Visibility.Visible; window.UpdateLayout();
                Apply(Brushes.White);
                window.Width += 10; window.UpdateLayout();
                Native.Check(Overlay() is null, "Resizing left a stale stretched theme snapshot.");
                Apply(Brushes.MidnightBlue);
                window.Content = null;
                VisualAssetChecks.Pump(() => !root.IsLoaded && Overlay() is null, "Unloading retained the transition snapshot or pending animation.");
                window.Content = root; window.UpdateLayout();
                try {
                    Native.Static("PCL.ThemeTransition", "Apply", root, (Action)(() => throw new InvalidOperationException("theme fixture sentinel")));
                    throw new Exception("Theme transition swallowed a failed palette change.");
                } catch (InvalidOperationException error) when (error.Message == "theme fixture sentinel") {
                    Native.Check(Overlay() is null, "A failed palette change left a snapshot over the interface.");
                }
            } else Native.Check(Overlay() is null, "Windows reduced-motion setting did not suppress the theme transition.");
            Native.Cache("SystemDebugAnim", 30);
            Apply(Brushes.White);
            Native.Check(Overlay() is null && ReferenceEquals(root.Background, Brushes.White), "Disabled animations delayed theme changes or created a snapshot.");
            Native.Cache("SystemDebugAnim", 9); ModAnimation.AniControlEnabled = 1;
            Apply(Brushes.MidnightBlue);
            Native.Check(Overlay() is null, "Native animation suppression was ignored by theme transitions.");
            Native.Check(!window.IsActive, "Offscreen transition fixture took desktop focus.");
            environment.AssertNoForbiddenIo();
            Console.WriteLine("PASS theme crossfade: real WPF opacity interpolation, immediate underlying changes, rapid-toggle replacement, completed/hidden/resized/unloaded cleanup, native disabled/reduced motion and input passthrough.");
        } finally {
            window.Content = null; window.Close(); app.MainWindow = previousMain; app.ShutdownMode = previousMode;
            cache.Clear(); foreach (var entry in saved) cache.Add(entry.Key, entry.Value);
            ModAnimation.AniControlEnabled = suppression;
        }
    }
}
