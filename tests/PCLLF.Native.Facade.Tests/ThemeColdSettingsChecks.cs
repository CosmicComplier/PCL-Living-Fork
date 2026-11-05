using System.Collections;
using System.Windows.Controls;
using PCL;

namespace PCLLF.Native.Facade.Tests;

internal static class ThemeColdSettingsChecks
{
    internal static void Run(FixtureEnvironment environment)
    {
        IDictionary cache = Settings.Entries["UiLauncherColorMode"].ValueCache;
        var saved = new List<DictionaryEntry>();
        foreach (DictionaryEntry entry in cache) saved.Add(entry);
        int suppression = ModAnimation.AniControlEnabled;
        try
        {
            foreach (int mode in new[] { 0, 1 })
            {
                Native.Cache("UiLauncherColorMode", mode);
                var page = new PageSetupUI();
                var choice = (ComboBox)page.FindName("ComboLauncherColorMode");
                Native.Check(!page.IsLoaded && choice.SelectedIndex == mode, "Cold setup BAML does not select the saved launcher color mode before Loaded.");
                Native.Check(ModAnimation.AniControlEnabled == suppression && Settings.Get<int>("UiLauncherColorMode") == mode,
                    "Cold setup theme selection leaked animation suppression or changed the saved preference.");
            }
            environment.AssertNoForbiddenIo();
            Console.WriteLine("PASS cold setup theme: real BAML selects both saved modes before Loaded; preference and animation suppression preserved, no forbidden I/O.");
        }
        finally
        {
            cache.Clear();
            foreach (DictionaryEntry entry in saved) cache.Add(entry.Key, entry.Value);
            ModAnimation.AniControlEnabled = suppression;
        }
    }
}
