using System;
using System.Globalization;

namespace PCLCS;

/// <summary>Suppresses automatic version notes only for the build the user dismissed.</summary>
public static class LegacyUpdateLogPrompt {
    public const string Key = "SystemUpdateLogDismissedVersion";
    public const string CheckBoxText = "下次更新前不再弹出";

    // dev_edition shares a dismissal within one baseline/code/build kind. A new
    // release, baseline, internal version code or build kind starts a new scope;
    // rebuilding the same development baseline does not repeat the notice.
    public static string BuildIdentity(string version, string baseline, bool prerelease, int versionCode, int buildType) =>
        string.Join("|", version, baseline, prerelease ? "preview" : "stable",
            versionCode.ToString(CultureInfo.InvariantCulture), buildType.ToString(CultureInfo.InvariantCulture));

    public static bool ShouldShow(Func<string, string> readString, string identity) {
        if (readString is null || string.IsNullOrEmpty(identity)) return true;
        try { return !string.Equals(readString(Key), identity, StringComparison.Ordinal); }
        catch { return true; }
    }

    // Unchecking a manually opened dialog only clears this build's dismissal.
    public static string UpdateDismissal(string? stored, string identity, bool suppress) =>
        suppress ? identity : string.Equals(stored, identity, StringComparison.Ordinal) ? "" : stored ?? "";
}
