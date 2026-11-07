using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json.Linq;
using PCL;

namespace PCLLF.Native.Facade.Tests;

internal static class DeferredUpdateButtonChecks {
    internal static void Run(FixtureEnvironment environment) {
        var state = Native.Field(typeof(ModLauncherUpdate), "deferredRelease");
        object? previousState = state.GetValue(null);
        var saved = new Dictionary<string, List<DictionaryEntry>>();
        foreach (string key in new[] { "ToolLauncherUpdateIgnoredTag", "ToolLauncherUpdateChannel" }) {
            saved[key] = new List<DictionaryEntry>();
            foreach (DictionaryEntry item in (IDictionary)Settings.Entries[key].ValueCache) saved[key].Add(item);
        }
        var app = System.Windows.Application.Current;
        Window? oldMain = app.MainWindow; ShutdownMode oldMode = app.ShutdownMode;
        int opens = 0;
        var button = new MyIconButton { Width = 28, Height = 28, Theme = MyIconButton.Themes.White };
        string next = "v" + (Version.Parse(ModBase.ReleaseBaselineVersion.Split('-')[0]).Major + 1) + ".0.0";
        object source = Native.Enum("PCL.ModLauncherUpdate+UpdateSource", "GitHub")!;
        string Tag() => (string)Native.Static("PCL.ModLauncherUpdate", "DeferredUpdateTag")!;
        void Refresh() => Native.Call(typeof(FormMain), "RefreshDeferredUpdateButton", null, button);
        var window = new Window { Width = 160, Height = 60, Content = button, Left = -30000, Top = -30000, ShowActivated = false, ShowInTaskbar = false, WindowStyle = WindowStyle.None };
        try {
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            state.SetValue(null, null);
            Native.Cache("ToolLauncherUpdateChannel", 0); Native.Cache("ToolLauncherUpdateIgnoredTag", "");
            Native.Call(typeof(FormMain), "ConfigureDeferredUpdateButton", null, button, (Action)(() => opens++));
            window.Show(); window.UpdateLayout();
            Native.Check(button.Visibility == Visibility.Collapsed, "An empty update choice displayed a deferred-update button.");
            Native.Cache("ToolLauncherUpdateIgnoredTag", next); Refresh();
            Native.Check(button.IsVisible && button.CornerRadius.TopLeft >= 14 && button.ToolTip?.ToString()?.Contains(next) == true,
                "The persisted deferred update did not restore its round native title button.");
            foreach (var routed in new[] { UIElement.MouseLeftButtonDownEvent, UIElement.MouseLeftButtonUpEvent })
                button.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left) { RoutedEvent = routed });
            Native.Check(opens == 1, "The native deferred-update button did not invoke reopening exactly once.");
            var release = new JObject { ["tag_name"] = next };
            Native.Static("PCL.ModLauncherUpdate", "RememberDeferredUpdate", release, source);
            release["tag_name"] = "v0.0.0";
            Native.Check(Tag() == next, "Deferred release metadata was not isolated from subsequent catalogue mutation.");
            Native.Static("PCL.ModLauncherUpdate", "ClearDeferredUpdate"); Refresh();
            Native.Check(Tag() == "" && button.Visibility == Visibility.Collapsed, "A successful no-update check left the deferred button visible from stale preferences.");
            Native.Static("PCL.ModLauncherUpdate", "RememberDeferredUpdate", new JObject { ["tag_name"] = next + "-pre-1" }, source); Refresh();
            Native.Check(button.Visibility == Visibility.Collapsed, "Stable-only updates exposed a deferred prerelease.");
            Native.Cache("ToolLauncherUpdateChannel", 1); Refresh();
            Native.Check(button.IsVisible && Tag() == next + "-pre-1", "Enabling prereleases did not restore the eligible deferred update.");
            state.SetValue(null, null); Native.Cache("ToolLauncherUpdateIgnoredTag", "v" + ModBase.ReleaseBaselineVersion); Refresh();
            Native.Check(button.Visibility == Visibility.Collapsed, "The installed build was still offered as a deferred update.");
            Native.Cache("ToolLauncherUpdateIgnoredTag", "invalid"); Refresh();
            Native.Check(button.Visibility == Visibility.Collapsed, "An invalid saved update tag produced an actionable button.");
            Native.Check(!window.IsActive, "Deferred-update fixture took input focus.");
            environment.AssertNoForbiddenIo();
            Console.WriteLine("PASS deferred update: persisted choice restores round native button; real click reopens once; confirmed no-update hides it; metadata isolation, stable/prerelease/current/invalid tag guards; no HTTP or real update.");
        } finally {
            window.Content = null; window.Close(); app.MainWindow = oldMain; app.ShutdownMode = oldMode;
            state.SetValue(null, previousState);
            foreach (var pair in saved) {
                IDictionary cache = Settings.Entries[pair.Key].ValueCache; cache.Clear();
                foreach (var item in pair.Value) cache.Add(item.Key, item.Value);
            }
        }
    }
}
