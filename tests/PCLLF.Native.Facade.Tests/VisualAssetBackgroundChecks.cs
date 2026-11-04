using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PCL;

namespace PCLLF.Native.Facade.Tests;
internal static class VisualAssetBackgroundChecks
{
    internal static void Run(VisualAssetChecks.Scope scope)
    {
        object entry = Native.Entries["UiBackgroundMode"]!;
        VisualAssetChecks.Check("background", 17, Native.Get(entry, "Source")!.ToString() == "Normal" && Equals(Native.Get(entry, "DefaultValue"), 0) && Native.Get(entry, "OnChanged") is not null);
        PageSetupUI page = new();
        var combo = VisualAssetChecks.Find<ComboBox>(page, "ComboBackgroundMode");
        VisualAssetChecks.Check("background", 21, (string)Native.Static("PCL.SettingService", "GetKey", combo)! == "UiBackgroundMode" && combo.Items.Count == 2);
        Grid panel = new(); Canvas image = new() { Background = Brushes.CornflowerBlue, Opacity = .748 };
        panel.Children.Add(image); panel.Children.Add(page);
        object? savedOpacity = Native.Get(Native.Entries["UiBackgroundOpacity"]!, "ValueCache") is System.Collections.IDictionary opacity ? opacity[""] : null;
        object? savedColorful = ((System.Collections.IDictionary)Native.Get(Native.Entries["UiBackgroundColorful"]!, "ValueCache")!)[""];
        Brush originalImage = image.Background;
        try
        {
            foreach (int mode in new[] { 1, 0, 1, 0 })
            {
                Native.Cache("UiBackgroundMode", mode);
                int suppression = ModAnimation.AniControlEnabled;
                try { ModAnimation.AniControlEnabled = checked(suppression + 1); Native.Static("PCL.SettingService", "RefreshSettings", page); }
                finally { ModAnimation.AniControlEnabled = suppression; }
                Native.Static("PCL.ModSecret", "ApplyMainBackground", panel, image);
                page.BackgroundRefreshUI(mode == 0, 0);
                if (mode == 1)
                {
                    VisualAssetChecks.Check("background", 34, image.Visibility == Visibility.Collapsed && panel.Background is SolidColorBrush plain && plain.Color == Colors.White);
                    VisualAssetChecks.Check("background", 35, VisualAssetChecks.Find<FrameworkElement>(page, "PanBackgroundOpacity").Visibility == Visibility.Collapsed && VisualAssetChecks.Find<FrameworkElement>(page, "CheckBackgroundColorful").Visibility == Visibility.Collapsed);
                }
                else
                {
                    VisualAssetChecks.Check("background", 37, image.Visibility == Visibility.Visible && panel.Background is LinearGradientBrush);
                    VisualAssetChecks.Check("background", 38, VisualAssetChecks.Find<FrameworkElement>(page, "PanBackgroundOpacity").Visibility == Visibility.Visible);
                }
                VisualAssetChecks.Check("background", 40, Equals(((System.Collections.IDictionary)Native.Get(Native.Entries["UiBackgroundOpacity"]!, "ValueCache")!)[""], savedOpacity) && Equals(((System.Collections.IDictionary)Native.Get(Native.Entries["UiBackgroundColorful"]!, "ValueCache")!)[""], savedColorful) && ReferenceEquals(image.Background, originalImage));
                foreach (string name in new[] { "CardLauncher", "CardBackground", "CardMusic" })
                    _ = Native.Property(page.FindName(name).GetType(), "MainTextBlock").GetValue(page.FindName(name));
                VisualAssetChecks.Render(panel, Path.Combine(scope.Root, "background-mode-" + mode + ".png"), 800, 660);
            }
            Native.Cache("UiBackgroundMode", 1); Native.Cache("UiBackgroundColorful", false);
            Native.Static("PCL.ModSecret", "ApplyMainBackground", panel, image);
            VisualAssetChecks.Check("background", 51, panel.Background is SolidColorBrush white && white.Color == Colors.White);
            Native.Cache("UiBackgroundMode", 0); Native.Static("PCL.ModSecret", "ApplyMainBackground", panel, image);
            VisualAssetChecks.Check("background", 53, panel.Background is SolidColorBrush gray && gray.Color == Color.FromRgb(245, 245, 245) && image.Visibility == Visibility.Visible);
        }
        finally
        {
            // Never Show this settings page: its real Loaded handler can enumerate user backgrounds/music.
            // Original Test-BackgroundModes likewise only Measures/Arranges the genuine page.
            page.PageOnForceExit(); panel.Children.Clear();
        }
    }
}
