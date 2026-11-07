using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PCLCS;

/// <summary>
/// Local mod favorites as persisted under UiModFavorite in the instance Setup.ini.
/// The stored form is '|'-separated lowercase EnabledName keys in first-favorite
/// order; comparison is OrdinalIgnoreCase. Overflow refuses the new key and keeps
/// the existing value untouched (no truncation, no eviction).
/// </summary>
public static class LegacyModFavorites {
    public const string SettingKey = "UiModFavorite";
    public const int MaxCount = 2000;
    public const int MaxLength = 32 * 1024;
    private const char Separator = '|';

    public static string Normalize(string? key) => (key ?? "").Trim().ToLowerInvariant();

    /// <summary>Splits a stored value into normalized keys, dropping duplicates and empty parts.</summary>
    public static List<string> Parse(string? value) {
        List<string> keys = new();
        if (string.IsNullOrEmpty(value)) return keys;
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        foreach (string raw in value!.Split(Separator)) {
            string key = Normalize(raw);
            if (key.Length == 0 || !seen.Add(key)) continue;
            keys.Add(key);
        }
        return keys;
    }

    public static string Serialize(IEnumerable<string> keys) => string.Join(Separator,
        keys.Select(Normalize).Where(key => key.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase));

    public static bool Contains(IEnumerable<string> keys, string? key) {
        string normalized = Normalize(key);
        return normalized.Length > 0 && keys.Any(item => string.Equals(Normalize(item), normalized, StringComparison.Ordinal));
    }

    public static int ByteLength(string serialized) => Encoding.UTF8.GetByteCount(serialized);

    /// <summary>
    /// Adds one key while honoring the 2000-entry and 32 KiB limits. On overflow the
    /// supplied list and serialized form are returned unchanged and false is returned.
    /// </summary>
    public static bool TryAdd(IReadOnlyList<string> existing, string? key, out List<string> result, out string serialized, out int count, out int length) {
        result = existing.Select(Normalize).Where(item => item.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        string normalized = Normalize(key);
        if (normalized.Length == 0 || Contains(result, normalized)) {
            serialized = Serialize(result); count = result.Count; length = ByteLength(serialized);
            return true;
        }
        List<string> candidate = new(result) { normalized };
        string text = Serialize(candidate);
        count = candidate.Count; length = ByteLength(text);
        if (count > MaxCount || length > MaxLength) {
            serialized = Serialize(result);
            return false;
        }
        result = candidate; serialized = text;
        return true;
    }

    /// <summary>Removes one key; removal is always allowed and never hits the write limits.</summary>
    public static List<string> Remove(IReadOnlyList<string> existing, string? key) {
        string normalized = Normalize(key);
        return existing.Select(Normalize).Where(item => item.Length > 0 && !string.Equals(item, normalized, StringComparison.Ordinal))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static string ToggleLogLine(string? key, bool favorited) => "ModFavorite:toggle:" + Normalize(key) + ":" + (favorited ? "true" : "false");
    public static string OverflowLogLine(int count, int length) => "ModFavorite:overflow:" + count + ":" + length;
}
