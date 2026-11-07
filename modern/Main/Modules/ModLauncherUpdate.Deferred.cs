using System.Threading;
using Newtonsoft.Json.Linq;

namespace PCL;

public static partial class ModLauncherUpdate {
    private sealed class DeferredRelease {
        internal readonly JObject Release;
        internal readonly UpdateSource Source;
        internal DeferredRelease(JObject release, UpdateSource source) { Release = release; Source = source; }
    }
    // null: startup has not checked yet; a null Release: checked, nothing deferred.
    // An immutable holder publishes the release/source together across worker/UI threads.
    private static DeferredRelease deferredRelease;

    internal static string DeferredUpdateTag() {
        var pending = Volatile.Read(ref deferredRelease);
        string tag = pending is null ? Settings.Get<string>("ToolLauncherUpdateIgnoredTag") : pending.Release?["tag_name"]?.ToString();
        return NewerReleaseForChannel(tag, ModBase.VersionBaseName, PreferredUpdateChannel()) ? tag : "";
    }
    internal static void RememberDeferredUpdate(JObject release, UpdateSource source) {
        Volatile.Write(ref deferredRelease, new DeferredRelease((JObject)release.DeepClone(), source));
        RefreshDeferredUpdateIndicator();
    }
    private static void ClearDeferredUpdate() {
        Volatile.Write(ref deferredRelease, new DeferredRelease(null, UpdateSource.Cnb));
        RefreshDeferredUpdateIndicator();
    }
    private static void RefreshDeferredUpdateIndicator() => ModBase.RunInUi(() => {
        if (ModMain.FrmMain?.BtnTitleUpdate is MyIconButton button) FormMain.RefreshDeferredUpdateButton(button);
    });
    internal static void ReopenDeferredUpdate() {
        var pending = Volatile.Read(ref deferredRelease);
        if (pending?.Release is not null)
            RequestSelectedUpdate((JObject)pending.Release.DeepClone(), pending.Source);
        else CheckForUpdates(false); // A saved ignored tag may predate this process's catalogue.
    }
}
