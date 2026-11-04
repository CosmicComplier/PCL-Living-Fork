namespace PCLLF.Native.Facade.Tests;

internal static class RegistryChecks
{
    internal static void Run(FixtureEnvironment fixture)
    {
        string[] microsoft =
        [
            "LoginMsJson", "CacheMsOAuthRefresh", "CacheMsAccess", "CacheMsProfileJson", "CacheMsUuid", "CacheMsName",
            "CacheMsV2Migrated", "CacheMsV2OAuthRefresh", "CacheMsV2Access", "CacheMsV2ProfileJson", "CacheMsV2Uuid", "CacheMsV2Name", "CacheMsV2Expires"
        ];
        string[] unrelated =
        [
            "Identify", "LoginType", "CacheAuthAccess", "CacheNidePass", "LaunchFolders", "UiLauncherTheme", "ToolLauncherUpdateIgnoredTag"
        ];
        // These are the real single schema objects, including their real Registry
        // source. Do not manufacture a Normal-source entry or accept misspelled
        // names solely because the production mapper recognizes a prefix.
        foreach (string key in microsoft)
        {
            PCL.Settings.Setting setting = PCL.Settings.Entries[key];
            Native.Check(setting.Source == PCL.Settings.Sources.Registry, "Actual Microsoft setting lost its Registry source: " + key);
            Native.Check((bool)Native.Static("PCL.Settings", "IsMicrosoftAccountSetting", setting)!, "Real Main failed Microsoft key classification: " + key);
            Native.Check((string)Native.Static("PCL.Settings", "RegistryValueName", setting)! == key + "_LF", "Real Main lost exact _LF Registry mapping: " + key);
        }
        foreach (string key in unrelated)
        {
            PCL.Settings.Setting setting = PCL.Settings.Entries[key];
            Native.Check(!(bool)Native.Static("PCL.Settings", "IsMicrosoftAccountSetting", setting)!, "Real Main misclassified an unrelated actual schema key: " + key);
            Native.Check((string)Native.Static("PCL.Settings", "RegistryValueName", setting)! == key, "Real Main renamed an unrelated Registry value: " + key);
        }
        string[] actual = PCL.Settings.Entries.Values
            .Where(setting => (bool)Native.Static("PCL.Settings", "IsMicrosoftAccountSetting", setting)!)
            .Select(setting => setting.Key).OrderBy(key => key, StringComparer.Ordinal).ToArray();
        Native.Check(actual.SequenceEqual(microsoft.OrderBy(key => key, StringComparer.Ordinal)), "The actual Main Microsoft Registry schema differs from the required thirteen names.");
        fixture.AssertNoForbiddenIo();
        // Never call Main.HasSaved or ReadPclAccountRegistrySnapshot: both perform actual IO.
    }
}
