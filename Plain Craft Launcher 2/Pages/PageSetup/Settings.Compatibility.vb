Partial Public Class Settings
    Private Const SharedPreferencesMarker As String = "LivingPclSharedPreferencesMigrated"
    Private Shared Function NormalizeFolderList(Value As String) As String
        Return PCLCS.LegacySettingsEngine(Of McInstance, Setting, PclAccountMigrationPlan).NormalizeFolderList(Value)
    End Function
    Friend Shared Function NormalizeSharedPclPreference(Entry As Setting, Value As String) As String
        Return PCLCS.LegacySettingsEngine(Of McInstance, Setting, PclAccountMigrationPlan).NormalizeSharedPreference(Entry, Value)
    End Function
    Friend Shared Sub MigrateSharedPclPreferences(OldStore As Microsoft.Win32.RegistryKey, PclStore As Microsoft.Win32.RegistryKey)
        Backend.MigrateSharedPreferences(If(OldStore Is Nothing, Nothing, New PCLCS.LegacySettingsRegistryStore(OldStore)), If(PclStore Is Nothing, Nothing, New PCLCS.LegacySettingsRegistryStore(PclStore)))
    End Sub
    Friend Shared Sub ImportLegacyPclPreferences()
        Backend.ImportLegacyPreferences()
    End Sub
    Friend Shared Sub ResetRegistryPreference(Store As Microsoft.Win32.RegistryKey, Entry As Setting)
        PCLCS.LegacySettingsEngine(Of McInstance, Setting, PclAccountMigrationPlan).ResetRegistryPreference(New PCLCS.LegacySettingsRegistryStore(Store), Entry)
    End Sub
End Class
