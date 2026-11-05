using System.Diagnostics;
using Newtonsoft.Json.Linq;
using PCL;
using PCLCS;

namespace PCLLF.Native.Facade.Tests;

// LF's real Main process/wait adapters, LoaderCombo, Watcher and local-session
// callbacks stay in use. The OS executable boundary is an explicitly controlled
// cmd.exe script, not Minecraft; login, Java discovery, remote files and HWND
// discovery are outside this fixture and are never reported as game validation.
// Behavior provenance: LF f4aa0fae62c3ee8af24c81e61844c8fdd13c8983 and
// 50b028da5fcad2226d74d901a41d8c43a249bfb2/ModLaunch.vb, ModWatcher.vb,
// PersonalGameSettings.vb and QuickPlayWorlds.vb; upstream PCL
// 5ef81e490ae46b1100d61e33d0183094c7a80ca8. No CE code is copied here.
internal static class LaunchProcessChecks
{
    internal static void Run(FixtureEnvironment environment)
    {
        RunExit(environment, 0);
        RunExit(environment, 23);
        RunCancel(environment);
        environment.AssertNoForbiddenIo();
    }

    private static void RunExit(FixtureEnvironment environment, int code)
    {
        using LaunchProcessChecksScope scope = new(environment, code == 0 ? "normal" : "abnormal", code);
        try
        {
            scope.Prepare();
            scope.Start();
            scope.Wait(() => ModLaunch.McLaunchWatcher is not null, "real Main watcher construction");
            Process child = scope.Child;
            Native.Check(!child.HasExited && ReferenceEquals(ModLaunch.McLaunchProcess, child) &&
                ReferenceEquals(ModLaunch.McLaunchWatcher.GameProcess, child), "Run/Wait did not pass the same owned process into the genuine Main watcher.");
            scope.CheckTracked();
            child.StandardInput.WriteLine("begin"); child.StandardInput.Flush();
            scope.Wait(() => scope.Plan.State == ModBase.LoadState.Finished && ModLaunch.McLaunchWatcher.State == ModWatcher.Watcher.MinecraftState.Running && scope.Hidden == 1 &&
                ModLaunch.McLaunchWatcher.LatestLog.Contains(scope.OutputMarker) && ModLaunch.McLaunchWatcher.LatestLog.Contains(scope.ErrorMarker),
                "real log-progress Running state and launch completion");
            Native.Check(ModWatcher.HasRunningMinecraft && scope.Hidden == 1 && scope.Restored == 0,
                "Actual launch End/group visibility behavior changed before process exit.");
            Native.Check(Settings.Get<int>("SystemLaunchCount") == scope.InitialLaunchCount + 1, "Actual End did not persist exactly one launch-count increment.");
            scope.CheckLaunchCountPersisted(true);
            Native.Check(ModLaunch.McLaunchWatcher.LatestLog.Any(line => line == scope.OutputMarker) &&
                ModLaunch.McLaunchWatcher.LatestLog.Any(line => line == scope.ErrorMarker), "Actual Watcher did not receive both controlled stdout and stderr.");
            child.StandardInput.WriteLine("finish"); child.StandardInput.Flush(); child.StandardInput.Close();
            scope.WaitStopped();
            Native.Check(child.HasExited && child.ExitCode == code, "Owned process did not return its required real exit code.");
            Native.Check(ModLaunch.McLaunchWatcher.State == (code == 0 ? ModWatcher.Watcher.MinecraftState.Ended : ModWatcher.Watcher.MinecraftState.Crashed),
                "Actual Watcher exit classification changed.");
            Native.Check(!ModWatcher.HasRunningMinecraft && scope.Restored == 1 && scope.ProcessStopped == 1 &&
                scope.PersonalFinished == 1 && scope.QuickFinished == 1 && scope.HealthyCalls == 1 && scope.LastHealthy == (code == 0),
                "Actual exactly-once stopped/local-session/healthy callbacks or global restore did not complete.");
            scope.CheckFinished(code == 0);
            if (code != 0)
                Native.Check(scope.CrashHints == 1 && scope.Analyzers == 1 && scope.AnalysisCompleted == 1 && scope.AnalysisDialogs == 1,
                    "The real delayed crash analyzer did not collect/prepare/analyze/output under owned roots.");
            else Native.Check(scope.CrashHints == 0 && scope.Analyzers == 0, "Successful controlled exit unexpectedly entered crash analysis.");
            Dictionary<string, string> profileBefore = scope.ProfileSnapshot();
            int callbacks = scope.ProcessStopped + scope.PersonalFinished + scope.QuickFinished + scope.HealthyCalls;
            Native.Static("PCL.PersonalGameSettings", "RecoverExitedSessions");
            Native.Static("PCL.QuickPlayWorlds", "RecoverExitedSessions");
            Native.Check(callbacks == scope.ProcessStopped + scope.PersonalFinished + scope.QuickFinished + scope.HealthyCalls &&
                !scope.HasSessionFiles() && scope.SameProfile(profileBefore), "Cold recovery rewrote completed profile data or left a stale process journal.");
            Console.WriteLine("PASS native launch-process " + (code == 0 ? "normal0" : "abnormal23") +
                ": genuine Main adapters/combo/process/Watcher, both redirected channels, actual local-session cleanup and " +
                (code == 0 ? "healthy checkpoint; controlled cmd fixture, not Minecraft." : "unhealthy checkpoint plus real delayed crash analysis; controlled cmd fixture, not Minecraft."));
        }
        catch (Exception error) { scope.RecordPrimary(error); throw; }
    }

    private static void RunCancel(FixtureEnvironment environment)
    {
        using LaunchProcessChecksScope scope = new(environment, "cancel-after-start", 0);
        try
        {
            scope.Prepare(); scope.CancelAfterStart = true; scope.Start();
            scope.Wait(() => scope.Plan.State is ModBase.LoadState.Canceled or ModBase.LoadState.Failed, "actual launch cancellation");
            scope.Wait(() => scope.Child.HasExited, "original Run cancellation kill before fixture cleanup");
            scope.WaitStopped();
            Native.Check(scope.Plan.State == ModBase.LoadState.Canceled && scope.Child.HasExited &&
                scope.ProcessStarts == 1 && ModLaunch.McLaunchProcess is null && ModLaunch.McLaunchWatcher is null,
                "Canceled real process step stored its output, started Wait, or left the owned process alive.");
            Native.Check(scope.ProcessStopped == 0 && scope.PersonalFinished == 0 && scope.QuickFinished == 0 &&
                scope.Analyzers == 0 && Settings.Get<int>("SystemLaunchCount") == scope.InitialLaunchCount && !scope.HasSessionFiles(), "Canceled pre-Watcher launch manufactured stopped/finish/crash callbacks or tracked journals.");
            scope.CheckLaunchCountPersisted(false);
            Console.WriteLine("PASS native launch-process cancel-after-start: real Loader.Cancel and original Run post-start cancellation kill/discard; no real game/authorization claim.");
        }
        catch (Exception error) { scope.RecordPrimary(error); throw; }
    }
}
