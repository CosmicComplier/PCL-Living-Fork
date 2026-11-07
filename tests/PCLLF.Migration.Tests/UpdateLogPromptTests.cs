using PCLCS;

internal static class UpdateLogPromptTests {
    internal static IEnumerable<(string Name, Action Run)> Cases() {
        yield return ("update notes dismiss only the current build and ignore the old permanent bool", VersionScope);
        yield return ("development note dismissal expires with baseline code or build kind", DevelopmentScope);
        yield return ("manual note viewing can clear only the current build dismissal", ManualRestore);
    }
    private static void VersionScope() {
        string current = Identity("1.4.0", "1.4.0");
        bool Show(string stored) => LegacyUpdateLogPrompt.ShouldShow(key => {
            Check(key == "SystemUpdateLogDismissedVersion", "The old permanent preference was queried.");
            return stored;
        }, current);
        Check(Show("") && Show("False") && Show(Identity("1.3.5", "1.3.5")), "Missing, legacy or old-version values must show.");
        Check(!Show(current), "The dismissed build must be suppressed.");
        Check(LegacyUpdateLogPrompt.ShouldShow(_ => throw new IOException("synthetic"), current), "Read errors must show.");
        Check(LegacyUpdateLogPrompt.ShouldShow(_ => "", ""), "An unknown identity must never suppress notes.");
    }
    private static void DevelopmentScope() {
        string current = Identity("dev_edition", "1.4.0");
        Check(!LegacyUpdateLogPrompt.ShouldShow(_ => current, Identity("dev_edition", "1.4.0")), "Rebuilds of one dev baseline must share the dismissal.");
        foreach (string updated in new[] {
            Identity("dev_edition", "1.4.1"), Identity("dev_edition", "1.4.0", code: 416),
            Identity("dev_edition", "1.4.0", kind: 100), Identity("1.4.0", "1.4.0"),
            Identity("dev_edition", "1.4.0", preview: true)
        }) Check(LegacyUpdateLogPrompt.ShouldShow(_ => current, updated), "An updated build identity must show again.");
    }
    private static void ManualRestore() {
        string current = Identity("1.4.0", "1.4.0"), other = Identity("1.4.1", "1.4.1");
        Check(LegacyUpdateLogPrompt.UpdateDismissal("", current, true) == current, "Checking must save this identity.");
        Check(LegacyUpdateLogPrompt.UpdateDismissal(current, current, false) == "", "Unchecking must restore this build's automatic notes.");
        Check(LegacyUpdateLogPrompt.UpdateDismissal(other, current, false) == other, "Unchecking must preserve another build's dismissal.");
    }
    private static string Identity(string version, string baseline, int code = 415, int kind = 0, bool preview = false)
        => LegacyUpdateLogPrompt.BuildIdentity(version, baseline, preview, code, kind);
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
}
