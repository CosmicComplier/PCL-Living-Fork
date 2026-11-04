using PCLCS;

namespace PCL
{
    public partial class Settings
    {
        private const string SharedPreferencesMarker = "LivingPclSharedPreferencesMigrated";
        private static string NormalizeFolderList(string Value)
        {
            return LegacySettingsEngine<ModMinecraft.McInstance, Setting, PclAccountMigrationPlan>.NormalizeFolderList(Value);
        }
        internal static string NormalizeSharedPclPreference(Setting Entry, string Value)
        {
            return LegacySettingsEngine<ModMinecraft.McInstance, Setting, PclAccountMigrationPlan>.NormalizeSharedPreference(Entry, Value);
        }
        internal static void MigrateSharedPclPreferences(Microsoft.Win32.RegistryKey OldStore, Microsoft.Win32.RegistryKey PclStore)
        {
            Backend.MigrateSharedPreferences(OldStore is null ? null : new LegacySettingsRegistryStore(OldStore), PclStore is null ? null : new LegacySettingsRegistryStore(PclStore));
        }
        internal static void ImportLegacyPclPreferences()
        {
            Backend.ImportLegacyPreferences();
        }
        internal static void ResetRegistryPreference(Microsoft.Win32.RegistryKey Store, Setting Entry)
        {
            LegacySettingsEngine<ModMinecraft.McInstance, Setting, PclAccountMigrationPlan>.ResetRegistryPreference(new LegacySettingsRegistryStore(Store), Entry);
        }
    }
}