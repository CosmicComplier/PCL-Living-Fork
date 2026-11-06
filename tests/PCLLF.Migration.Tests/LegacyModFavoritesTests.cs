using System;
using System.Collections.Generic;
using System.Linq;
using PCLCS;

// Pure storage rules for UiModFavorite: '|'-separated lowercase EnabledName keys,
// first-favorite order, 2000-entry / 32 KiB add limits, unrestricted removal.
internal static class LegacyModFavoritesTests {
    internal static IEnumerable<(string Name, Action Run)> Cases() {
        yield return ("mod favorites parse missing/empty values and normalize case without reordering", ParseOrder);
        yield return ("mod favorites add is case-insensitive, idempotent and stores lowercase keys", AddAndSerialize);
        yield return ("mod favorites reject the 2001st key and keep the stored value unchanged", CountLimit);
        yield return ("mod favorites reject a serialization over 32 KiB and keep the stored value unchanged", LengthLimit);
        yield return ("mod favorites removal is never limited by the add limits", RemovalAlwaysAllowed);
        yield return ("mod favorites log lines use the frozen ModFavorite prefixes", LogLines);
    }

    private static void ParseOrder() {
        Equal(0, LegacyModFavorites.Parse(null).Count);
        Equal(0, LegacyModFavorites.Parse("").Count);
        Sequence<string>(Array.Empty<string>(), LegacyModFavorites.Parse("||"));
        Sequence(new[] { "beta.jar", "alpha.jar" }, LegacyModFavorites.Parse("Beta.JAR|alpha.jar|BETA.jar|  "));
        Equal(true, LegacyModFavorites.Contains(LegacyModFavorites.Parse("Beta.JAR"), "bEtA.jar"));
        Equal(false, LegacyModFavorites.Contains(LegacyModFavorites.Parse("beta.jar"), "alpha.jar"));
    }

    private static void AddAndSerialize() {
        List<string> existing = new() { "Alpha.JAR" };
        Equal(true, LegacyModFavorites.TryAdd(existing, "beta.JAR", out List<string> added, out string text, out int count, out int length));
        Sequence(new[] { "alpha.jar", "beta.jar" }, added);
        Equal("alpha.jar|beta.jar", text);
        Equal(2, count);
        Equal(LegacyModFavorites.ByteLength(text), length);
        // Re-adding an existing key (case-insensitively) is a no-op, not an overflow.
        Equal(true, LegacyModFavorites.TryAdd(added, "ALPHA.jar", out List<string> same, out string sameText, out _, out _));
        Sequence(new[] { "alpha.jar", "beta.jar" }, same);
        Equal("alpha.jar|beta.jar", sameText);
        // Empty/whitespace keys never enter storage.
        Equal(true, LegacyModFavorites.TryAdd(added, "   ", out List<string> untouched, out _, out _, out _));
        Sequence(new[] { "alpha.jar", "beta.jar" }, untouched);
    }

    private static void CountLimit() {
        List<string> existing = Keys(LegacyModFavorites.MaxCount - 1);
        Equal(true, LegacyModFavorites.TryAdd(existing, "k1999", out List<string> full, out _, out int fullCount, out _));
        Equal(LegacyModFavorites.MaxCount, fullCount);
        string stored = LegacyModFavorites.Serialize(full);
        Equal(false, LegacyModFavorites.TryAdd(full, "k-overflow", out List<string> rejected, out string rejectedText, out int rejectedCount, out int rejectedLength));
        Sequence(full, rejected);
        Equal(LegacyModFavorites.MaxCount + 1, rejectedCount);
        Equal(LegacyModFavorites.ByteLength(stored + "|k-overflow"), rejectedLength);
        // The rejected add left the persisted value untouched (no truncation/eviction).
        Equal(stored, rejectedText);
        Equal(stored, LegacyModFavorites.Serialize(rejected));
    }

    private static void LengthLimit() {
        // Two ~20 KB keys stay under 32 KiB and are accepted; the third pushes over.
        string first = new('a', 20_000), second = new('b', 12_000), third = new('c', 1_000);
        List<string> existing = new() { first };
        Equal(true, LegacyModFavorites.TryAdd(existing, second, out List<string> two, out string twoText, out _, out int twoLength));
        Equal(true, twoLength <= LegacyModFavorites.MaxLength);
        Equal(false, LegacyModFavorites.TryAdd(two, third, out List<string> rejected, out string rejectedText, out int count, out int length));
        Sequence(two, rejected);
        Equal(twoText, rejectedText);
        Equal(3, count);
        Equal(true, length > LegacyModFavorites.MaxLength);
        // Existing entries are neither truncated nor evicted by the rejected add.
        Equal(twoText, LegacyModFavorites.Serialize(rejected));
    }

    private static void RemovalAlwaysAllowed() {
        List<string> existing = Keys(LegacyModFavorites.MaxCount);
        Equal(false, LegacyModFavorites.TryAdd(existing, "overflow", out List<string> full, out _, out _, out _));
        List<string> removed = LegacyModFavorites.Remove(full, "k0");
        Equal(LegacyModFavorites.MaxCount - 1, removed.Count);
        Equal("k1", removed[0]);
        // A long stored value can always be shrunk.
        List<string> longKeys = new() { new string('x', 40_000) };
        Equal(0, LegacyModFavorites.Remove(longKeys, longKeys[0]).Count);
    }

    private static void LogLines() {
        Equal("ModFavorite:toggle:alpha.jar:true", LegacyModFavorites.ToggleLogLine("Alpha.JAR", true));
        Equal("ModFavorite:toggle:alpha.jar:false", LegacyModFavorites.ToggleLogLine("Alpha.JAR", false));
        Equal("ModFavorite:overflow:2001:33000", LegacyModFavorites.OverflowLogLine(2001, 33000));
    }

    private static List<string> Keys(int count) => Enumerable.Range(0, count).Select(index => "k" + index).ToList();

    private static void Equal<T>(T expected, T actual) {
        if (!Equals(expected, actual)) throw new Exception($"Expected {expected}, got {actual}.");
    }
    private static void Sequence<T>(IEnumerable<T> expected, IEnumerable<T> actual) {
        if (!expected.SequenceEqual(actual)) throw new Exception("Sequence mismatch: " + string.Join(",", actual));
    }
}
