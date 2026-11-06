using System;
using System.Collections.Generic;
using System.Text;

namespace PCLCS;

/// <summary>
/// The update-log prompt preference as plain text in the launcher Setup.ini.
/// The dialog, Settings engine and platform store stay unchanged; this type only
/// owns the key/line shape so a failed write can be detected and reverted.
/// </summary>
public static class LegacyUpdateLogPrompt {
    public const string Key = "SystemUpdateLogPrompt";
    public const bool Default = true;
    /// <summary>Reads the persisted preference through the existing typed settings port.</summary>
    public static bool ShouldShow(Func<string, bool> readBoolean) {
        if (readBoolean is null) return Default;
        try { return readBoolean(Key); } catch { return Default; }
    }
    /// <summary>Replaces (or appends) this key's line while preserving every other Setup.ini line.</summary>
    public static string WriteIniText(string? iniText, bool value) {
        string line = Key + ":" + (value ? "True" : "False");
        List<string> lines = new();
        bool replaced = false;
        foreach (string raw in (iniText ?? "").Replace("\r\n", "\n").Split('\n')) {
            if (raw.Length == 0) continue;
            int index = raw.IndexOf(':');
            if (!replaced && index > 0 && string.Equals(raw.Substring(0, index), Key, StringComparison.Ordinal)) {
                lines.Add(line); replaced = true;
            } else lines.Add(raw);
        }
        if (!replaced) lines.Add(line);
        StringBuilder text = new();
        foreach (string item in lines) text.Append(item).Append("\r\n");
        return text.ToString();
    }
    /// <summary>Reads this key back from Setup.ini text; null means the key is absent or unparsable.</summary>
    public static bool? ReadIniText(string? iniText) {
        foreach (string raw in (iniText ?? "").Replace("\r\n", "\n").Split('\n')) {
            int index = raw.IndexOf(':');
            if (index <= 0 || !string.Equals(raw.Substring(0, index), Key, StringComparison.Ordinal)) continue;
            return bool.TryParse(raw.Substring(index + 1), out bool value) ? value : null;
        }
        return null;
    }
}
