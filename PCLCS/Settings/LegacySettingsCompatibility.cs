using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;

namespace PCLCS;

public sealed partial class LegacySettingsEngine<TInstance, TEntry, TPlan>
    where TInstance : class
    where TEntry : LegacySettingData<TInstance>
    where TPlan : LegacyAccountMigrationPlan {
    private const string sharedPreferencesMarker = "LivingPclSharedPreferencesMigrated";

    public static string? NormalizeFolderList(string? value) {
        try {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string item in (value ?? "").Split('|')) {
                if (VbStringEqual(item, "")) continue;
                int separator = item.IndexOf('>');
                if (separator <= 0 || separator == item.Length - 1) return null;
                string location = item.Substring(separator + 1).Replace("/", "\\");
                if (!Regex.IsMatch(location, @"\A(?:[A-Za-z]:\\|\\\\[^\\]+\\[^\\]+(?:\\|$))")) return null;
                // Reuse the existing, Windows-validated CLR4 path compatibility
                // boundary; do not introduce a second modern normalization rule.
                location = InstanceResourcePathCompatibility.GetFullPath(location).TrimEnd('\\') + "\\";
                if (seen.Add(location)) result.Add(item.Substring(0, separator) + ">" + location);
            }
            return string.Join("|", result);
        } catch { return null; }
    }

    public static string? NormalizeSharedPreference(TEntry entry, string value) {
        try {
            if (entry.Key == "LaunchFolders") return NormalizeFolderList(value);
            object checkedValue = Coerce(value, entry.Type);
            if (entry.Type == typeof(int)) {
                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number)) return null;
                int maximum;
                switch (entry.Key) {
                    case "ToolDownloadThread": maximum = 255; break;
                    case "ToolDownloadSpeed": maximum = 42; break;
                    case "SystemDebugAnim": maximum = 30; break;
                    case "LaunchSkinType": case "ToolDownloadTranslateV2": maximum = 4; break;
                    case "LaunchArgumentVisible": maximum = 5; break;
                    case "ToolModLocalNameStyle": maximum = 1; break;
                    case "LaunchArgumentPriority": case "ToolDownloadSource": case "ToolDownloadVersion": case "ToolDownloadMod": case "ToolDownloadTranslate": maximum = 2; break;
                    default: return null;
                }
                if (number < 0 || number > maximum) return null;
            } else if (entry.Key == "LaunchArgumentJavaAll") {
                foreach (JToken item in JArray.Parse(value)) {
                    if (item.Type != JTokenType.Object || item["Path"]?.Type != JTokenType.String || string.IsNullOrWhiteSpace((string?)item["Path"])) return null;
                }
            } else if (entry.Key == "LaunchArgumentJavaSelect" && !VbStringEqual(value, "")) {
                JObject item = JObject.Parse(value);
                if (item["Path"]?.Type != JTokenType.String || string.IsNullOrWhiteSpace((string?)item["Path"])) return null;
            }
            return Convert.ToString(checkedValue, CultureInfo.InvariantCulture);
        } catch { return null; }
    }

    public void MigrateSharedPreferences(ILegacySettingsStore? oldStore, ILegacySettingsStore? pclStore) {
        if (oldStore is null || pclStore is null) return;
        if (string.Equals(Convert.ToString(oldStore.GetValue(sharedPreferencesMarker, "False")), bool.TrueString, StringComparison.OrdinalIgnoreCase)) return;
        // RegistryValueKind is only a kind tag through ILegacySettingsStore here.
        // The actual Registry adapter stays Windows-only; no Registry API is used.
#pragma warning disable CA1416 // Inert RegistryValueKind metadata only.
        var changes = new List<Tuple<string, object?, RegistryValueKind>>();
#pragma warning restore CA1416
        try {
            foreach (TEntry entry in entries().Values.Where(IsCompatibleSetting)) {
                object? existing = pclStore.GetValue(entry.Key, null, true);
                // An explicitly empty PCL value is still authoritative.
                if (existing is not null) continue;
                object? raw = oldStore.GetValue(entry.Key, null, true);
                if (raw is not string rawString) continue;
                string? value = NormalizeSharedPreference(entry, rawString);
                if (value is null) continue;
#pragma warning disable CA1416 // Inert RegistryValueKind metadata only.
                RegistryValueKind existingKind = existing is null ? RegistryValueKind.String : pclStore.GetValueKind(entry.Key);
                changes.Add(Tuple.Create(entry.Key, existing, existingKind));
                pclStore.SetValue(entry.Key, value, RegistryValueKind.String);
#pragma warning restore CA1416
                if (!Equals(pclStore.GetValue(entry.Key, null, true), value)) throw new IOException("共享偏好保存校验失败");
            }
#pragma warning disable CA1416 // Inert RegistryValueKind metadata only.
            oldStore.SetValue(sharedPreferencesMarker, bool.TrueString, RegistryValueKind.String);
#pragma warning restore CA1416
            if (!Equals(oldStore.GetValue(sharedPreferencesMarker, null), bool.TrueString)) throw new IOException("共享偏好迁移标记校验失败");
        } catch {
#pragma warning disable CA1416 // Inert RegistryValueKind metadata only.
            for (int index = changes.Count - 1; index >= 0; index--) {
                Tuple<string, object?, RegistryValueKind> change = changes[index];
#pragma warning restore CA1416
                try {
#pragma warning disable CA1416 // Inert RegistryValueKind metadata only.
                    if (change.Item2 is null) pclStore.DeleteValue(change.Item1, false);
                    else pclStore.SetValue(change.Item1, change.Item2, change.Item3);
#pragma warning restore CA1416
                } catch {
                    // LF deliberately keeps this rollback best-effort. Original
                    // registry data remains for a later retry/manual recovery.
                }
            }
            throw;
        }
    }

    public void ImportLegacyPreferences() {
        if (VbStringEqual(host.RegistryFolder(), "PCL")) return;
        try {
            using ILegacySettingsStore? oldStore = host.OpenRegistry(host.RegistryFolder(), true, false);
            using ILegacySettingsStore? pclStore = host.OpenRegistry("PCL", true, true);
            MigrateSharedPreferences(oldStore, pclStore);
        } catch {
            // Retain original records, retry at next startup, log no config data.
        }
    }

    public static void ResetRegistryPreference(ILegacySettingsStore store, TEntry entry) {
        if (!IsCompatibleSetting(entry)) {
            store.DeleteValue(entry.Key, false);
            return;
        }
        string value = Convert.ToString(entry.DefaultValue, CultureInfo.InvariantCulture)!;
#pragma warning disable CA1416 // Inert RegistryValueKind metadata only.
        store.SetValue(entry.Key, value, RegistryValueKind.String);
#pragma warning restore CA1416
        if (!VbStringEqual(Convert.ToString(store.GetValue(entry.Key, null)), value)) throw new IOException("默认设置保存校验失败");
    }
}
