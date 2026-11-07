using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Rectangle = System.Windows.Shapes.Rectangle;
using System.Windows.Threading;
using PCL;
using PCLLF.Presentation.Foundation;

namespace PCLLF.Native.Facade.Tests;

internal static class LauncherUpdateCardBackgroundChecks {
    internal static void Run(PageSetupUpdate page, string root) {
        var card = (MyCard)page.FindName("CardLauncherUpdate");
        var background = (Grid)card.BorderChild;
        Native.Check(background.GetType().FullName == "PCL.LauncherUpdateCardBackground" && !background.IsHitTestVisible && background.Clip is RectangleGeometry { RadiusX: 5, RadiusY: 5 },
            "The update identity card lacks its isolated, clipped, click-through background.");
        var watcher = (DispatcherTimer)Native.Get(background, "settingsWatcher")!;
        var clocks = (List<AnimationClock>)Native.Get(background, "clocks")!;
        bool Moving() => (bool)Native.Get(background, "moving")!;
        int suppression = ModAnimation.AniControlEnabled;
        IDictionary cache = Settings.Entries["SystemDebugAnim"].ValueCache;
        var savedSetting = new List<DictionaryEntry>();
        foreach (DictionaryEntry entry in cache) savedSetting.Add(entry);
        bool previousDark = LegacyThemePalette.IsDarkTheme;
        var resources = System.Windows.Application.Current.Resources;
        var savedResources = resources.Keys.Cast<object>().ToDictionary(key => key, key => (Value: resources[key], Brush: (resources[key] as SolidColorBrush)?.CloneCurrentValue()));
        object service = Native.Field(Native.Type("PCL.ModSecret"), "PaletteService").GetValue(null)!;
        object themeHost = Native.Get(service, "host")!;
        var slots = Enumerable.Range(1, 8).SelectMany(index => new[] { "Color" + index, "Gray" + index }).Concat(new[] { "Bg0", "Bg1", "SemiTransparent" })
            .Select(name => Native.Get(themeHost, name)!).Select(slot => (Write: (Delegate)Native.Get(slot, "Write")!, Value: ((Delegate)Native.Get(slot, "Read")!).DynamicInvoke())).ToArray();
        try {
            ModAnimation.AniControlEnabled = 0; Native.Cache("SystemDebugAnim", 9);
            VisualAssetChecks.Pump(() => watcher.IsEnabled && Moving() == SystemParameters.ClientAreaAnimation, "Visible update background did not follow the native animation preference.");
            foreach (bool dark in new[] { false, true, false }) {
                foreach (var slot in slots) slot.Write.DynamicInvoke(slot.Value);
                Native.Static("PCL.ModTheme", "PrepareThemeTones", dark);
                Native.Static("PCL.ModTheme", "ApplyThemePalette");
                Native.Static("PCL.ModTheme", "NotifyThemeChanged");
                page.UpdateLayout();
                var baseBrush = (LinearGradientBrush)background.Background;
                Native.Check((baseBrush.GradientStops[0].Color.B < 100) == dark, "A live theme switch left the identity card using the previous brightness.");
                Native.Check(baseBrush.GradientStops.All(stop => Blue(stop.Color)) && background.Children.OfType<Rectangle>().All(layer => ((GradientBrush)layer.Fill).GradientStops.All(stop => Blue(stop.Color))),
                    "The identity card gradient left the requested blue/white hue family.");
                Native.Check(Contrast(LegacyThemePalette.Brush("ColorBrush1").Color, baseBrush.GradientStops[0].Color) >= 4.5,
                    "Native identity text lost readable contrast over the themed card background.");
                ModpackStageLayoutChecks.Save(page, Path.Combine(root, dark ? "launcher-update-card-dark.png" : "launcher-update-card-light.png"), (int)page.ActualWidth, (int)page.ActualHeight);
                foreach (var clock in clocks) clock.Controller.SeekAlignedToLastTick(TimeSpan.Zero, TimeSeekOrigin.BeginTime);
                var start = Pixels(background);
                foreach (var clock in clocks) clock.Controller.SeekAlignedToLastTick(TimeSpan.FromSeconds(4), TimeSeekOrigin.BeginTime);
                var middle = Pixels(background);
                double change = start.Zip(middle, (a, b) => Math.Abs(a - b)).Average();
                Native.Check(change > 4, "Gradient motion remains imperceptible across four seconds: " + change);
                ModpackStageLayoutChecks.Save(page, Path.Combine(root, dark ? "launcher-update-card-dark-moving.png" : "launcher-update-card-light-moving.png"), (int)page.ActualWidth, (int)page.ActualHeight);
            }
            Native.Cache("SystemDebugAnim", 30);
            VisualAssetChecks.Pump(() => !Moving() && clocks.All(clock => clock.IsPaused), "Disabling animations while visible did not pause background clocks.");
            Native.Cache("SystemDebugAnim", 4);
            VisualAssetChecks.Pump(() => (double)Native.Get(background, "speed")! == 0.5 || !SystemParameters.ClientAreaAnimation,
                "Changing native animation speed while visible did not reach the background clocks.");
            if (SystemParameters.ClientAreaAnimation) {
                VisualAssetChecks.Pump(Moving, "Re-enabling animations did not resume the existing flow clocks.");
                TimeSpan? before = clocks[0].CurrentTime;
                VisualAssetChecks.Pump(() => clocks[0].CurrentTime > before, "Enabled gradient clocks did not advance.");
            }
            ModAnimation.AniControlEnabled = suppression + 1;
            VisualAssetChecks.Pump(() => !Moving(), "Native animation suppression did not pause the gradient.");
            ModAnimation.AniControlEnabled = 0;
            page.Visibility = Visibility.Collapsed;
            Native.Check(!watcher.IsEnabled && !Moving(), "A hidden settings page retained its background watcher or motion.");
            page.Visibility = Visibility.Visible;
            VisualAssetChecks.Pump(() => watcher.IsEnabled, "Returning to the settings page did not restore the background watcher.");
            card.BorderChild = null!;
            VisualAssetChecks.Pump(() => !background.IsLoaded && !watcher.IsEnabled && !Moving(), "An unloaded identity background retained live animation work.");
            card.BorderChild = background;
            VisualAssetChecks.Pump(() => background.IsLoaded && watcher.IsEnabled, "Reloading the identity card did not restore its background.");
            Console.WriteLine("PASS native launcher identity background: live light/dark/light blue palettes, readable text, rounded click-through layer, animation disable/speed/suppression, Windows reduced motion and hide/unload/reload lifetime.");
        }
        finally {
            page.Visibility = Visibility.Visible;
            if (card.BorderChild is null) card.BorderChild = background;
            foreach (var slot in slots) slot.Write.DynamicInvoke(slot.Value);
            Native.Call(typeof(LegacyThemePalette), "Prepare", null, previousDark);
            foreach (object key in resources.Keys.Cast<object>().Where(key => !savedResources.ContainsKey(key)).ToArray()) resources.Remove(key);
            foreach (var pair in savedResources) {
                resources[pair.Key] = pair.Value.Value;
                if (pair.Value.Value is SolidColorBrush original && !original.IsFrozen && pair.Value.Brush is SolidColorBrush saved) { original.Color = saved.Color; original.Opacity = saved.Opacity; }
            }
            Native.Static("PCL.ModTheme", "NotifyThemeChanged");
            cache.Clear(); foreach (DictionaryEntry entry in savedSetting) cache.Add(entry.Key, entry.Value);
            ModAnimation.AniControlEnabled = suppression;
        }
    }
    private static byte[] Pixels(FrameworkElement element) {
        var image = new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth), (int)Math.Ceiling(element.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        image.Render(element); byte[] bytes = new byte[image.PixelWidth * image.PixelHeight * 4]; image.CopyPixels(bytes, image.PixelWidth * 4, 0); return bytes;
    }
    private static bool Blue(Color color) => color.B >= color.G && color.G >= color.R;
    private static double Contrast(Color foreground, Color background) {
        static double Luminance(Color color) {
            static double Channel(byte value) { double channel = value / 255d; return channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4); }
            return 0.2126 * Channel(color.R) + 0.7152 * Channel(color.G) + 0.0722 * Channel(color.B);
        }
        double a = Luminance(foreground), b = Luminance(background);
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }
}
