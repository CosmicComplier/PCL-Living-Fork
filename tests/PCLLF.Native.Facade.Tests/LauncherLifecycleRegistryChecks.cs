using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using PCL;

namespace PCLLF.Native.Facade.Tests;
internal static partial class LauncherLifecycleChecks
{
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int RegCreateKeyExW(IntPtr key, string name, int reserved, string? cls, int options, int access, IntPtr security, out SafeRegistryHandle result, out int disposition);

    // Genuine Windows RegistryKeys are passed to Main's original entry points.
    // This is NOT a replacement store or a PCL/PCLDebug account namespace.
    private static void SharedPreferenceRegistry()
    {
        string id = Guid.NewGuid().ToString("N"), path = @"Software\LivingPCL-LifecycleTests\" + id;
        int result = RegCreateKeyExW(new IntPtr(unchecked((int)0x80000001)), path, 0, null, 0, 0x2001f, IntPtr.Zero, out SafeRegistryHandle handle, out int disposition);
        if (result != 0) { handle.Dispose(); throw new Win32Exception(result, "Cannot create own lifecycle Registry fixture."); }
        if (disposition != 1) { handle.Dispose(); throw new InvalidOperationException("Refusing an unknown existing lifecycle Registry namespace."); }
        using (handle)
        using (RegistryKey root = RegistryKey.FromHandle(handle))
        {
            root.SetValue("FixtureOwner", id, RegistryValueKind.String);
            try
            {
                using RegistryKey legacy = root.CreateSubKey("Legacy", true), official = root.CreateSubKey("Official", true);
                void Migrate(RegistryKey? old = null) => SettingsCall("MigrateSharedPclPreferences", old ?? legacy, official);
                void Unmark() => legacy.DeleteValue("LivingPclSharedPreferencesMigrated", false);
                foreach (string key in new[] { "LaunchFolders", "ToolDownloadThread", "ToolDownloadSpeed" })
                    Check("audit:58", (string)SettingsCall("RegistryFolderFor", Settings.Entries[key])! == "PCL", key);
                Check("audit:60", !(bool)SettingsCall("IsCompatiblePclSetting", Settings.Entries["ToolLauncherUpdateIgnoredTag"])!);
                legacy.SetValue("LaunchFolders", @"Local>C:\Local\"); official.SetValue("LaunchFolders", @"Official>D:\Official\");
                legacy.SetValue("ToolDownloadThread", "12"); official.SetValue("ToolDownloadThread", "16"); legacy.SetValue("ToolDownloadSpeed", "8"); Migrate();
                Check("audit:68", (string)official.GetValue("LaunchFolders")! == @"Official>D:\Official\");
                Check("audit:69", (string)official.GetValue("ToolDownloadThread")! == "16"); Check("audit:70", (string)official.GetValue("ToolDownloadSpeed")! == "8");
                legacy.SetValue("LaunchFolders", @"Other>E:\Other\"); Migrate(); Check("audit:73", (string)official.GetValue("LaunchFolders")! == @"Official>D:\Official\");
                foreach (string current in new[] { @"New>D:\New\", @"Renamed>D:\Renamed\", "" })
                {
                    Unmark(); official.SetValue("LaunchFolders", current); Migrate(); Check("audit:78", (string)official.GetValue("LaunchFolders")! == current);
                }
                Unmark(); official.DeleteValue("LaunchFolders", false);
                const string original = @"Local>C:/Local|Duplicate>c:\Local\|Network>\\server\share\Pack";
                legacy.SetValue("LaunchFolders", original); Migrate();
                Check("audit:84", (string)official.GetValue("LaunchFolders")! == @"Local>C:\Local\|Network>\\server\share\Pack\");
                Check("audit:85", (string)legacy.GetValue("LaunchFolders")! == original);
                foreach ((string key, string value) in new[] { ("ToolDownloadThread", "-1"), ("ToolDownloadThread", "256"), ("ToolDownloadSpeed", "43"), ("SystemDebugAnim", "31"), ("LaunchArgumentPriority", "3"), ("ToolModLocalNameStyle", "2"), ("LaunchArgumentJavaAll", "[{\"Path\":null}]"), ("LaunchArgumentJavaSelect", "{\"Path\":42}"), ("LaunchFolders", @"Bad>relative\path") })
                    Check("audit:88", SettingsCall("NormalizeSharedPclPreference", Settings.Entries[key], value) is null, key);
                foreach ((string key, string value) in new[] { ("ToolDownloadThread", "255"), ("ToolDownloadSpeed", "42"), ("SystemDebugAnim", "30"), ("ToolDownloadTranslateV2", "4"), ("LaunchArgumentJavaAll", "[{\"Path\":\"C:\\\\Java\\\\\"}]"), ("LaunchArgumentJavaSelect", "{\"Path\":\"C:\\\\Java\\\\\"}") })
                    Check("audit:91", (string?)SettingsCall("NormalizeSharedPclPreference", Settings.Entries[key], value) == value, key);
                foreach (var setting in Settings.Entries.Values)
                    if ((bool)SettingsCall("IsCompatiblePclSetting", setting)!)
                        Check("audit:96", SettingsCall("NormalizeSharedPclPreference", setting, Convert.ToString(setting.DefaultValue, CultureInfo.InvariantCulture)) is not null, setting.Key);
                Unmark(); legacy.SetValue("ToolDownloadThread", "-1"); official.DeleteValue("ToolDownloadThread", false); Migrate();
                Check("audit:103", official.GetValue("ToolDownloadThread", null) is null);
                Unmark(); legacy.SetValue("ToolDownloadThread", "9"); legacy.SetValue("LivingPclSharedPreferencesMigrated", 0, RegistryValueKind.DWord); official.SetValue("ToolDownloadSpeed", 16, RegistryValueKind.DWord);
                bool failed = false;
                using (RegistryKey readOnly = root.OpenSubKey("Legacy", false)!) { try { Migrate(readOnly); } catch { failed = true; } }
                Check("audit:111", failed && official.GetValue("ToolDownloadThread", null) is null);
                Check("audit:112", (int)official.GetValue("ToolDownloadSpeed")! == 16 && official.GetValueKind("ToolDownloadSpeed") == RegistryValueKind.DWord);
                Check("audit:113", (string)legacy.GetValue("ToolDownloadThread")! == "9" && (int)legacy.GetValue("LivingPclSharedPreferencesMigrated")! == 0);
                SettingsCall("ResetRegistryPreference", official, Settings.Entries["LaunchFolders"]); Check("audit:116", (string)official.GetValue("LaunchFolders")! == "");
                SettingsCall("ResetRegistryPreference", official, Settings.Entries["ToolDownloadThread"]); Check("audit:118", Convert.ToString(official.GetValue("ToolDownloadThread"), CultureInfo.InvariantCulture) == Convert.ToString(Settings.Entries["ToolDownloadThread"].DefaultValue, CultureInfo.InvariantCulture));
                SettingsCall("ResetRegistryPreference", official, Settings.Entries["ToolDownloadThread"]); Check("audit:120", official.GetValue("ToolDownloadThread", null) is not null);
                Unmark(); legacy.SetValue("LaunchFolders", "malformed-relative-folder"); Migrate(); Check("audit:124", (string)official.GetValue("LaunchFolders")! == "");
            }
            finally
            {
                Native.Check((string?)root.GetValue("FixtureOwner", null) == id, "Lifecycle Registry ownership marker changed; refusing cleanup.");
                Check("audit:128", path.StartsWith(@"Software\LivingPCL-LifecycleTests\", StringComparison.Ordinal) && path.EndsWith(id, StringComparison.Ordinal));
                using RegistryKey verify = Registry.CurrentUser.OpenSubKey(path, false) ?? throw new InvalidOperationException("Owned lifecycle Registry root disappeared.");
                Native.Check((string?)verify.GetValue("FixtureOwner", null) == id, "Current lifecycle Registry path is foreign; refusing cleanup.");
                Registry.CurrentUser.DeleteSubKeyTree(path, false);
            }
        }
        Console.WriteLine("PASS native lifecycle Registry preferences: actual Main methods and actual GUID-only RegistryKeys, marker-last write failure rollback/kind, normalization/idempotence/reset; PCL/PCLDebug namespaces never opened.");
    }
}
