using System.Diagnostics;
using System.Text.RegularExpressions;
using MeloongCore;
using PCL;
using TaskLoader = PCL.ModLoader.LoaderTask<string, string>;
using ComboLoader = PCL.ModLoader.LoaderCombo<string>;

namespace PCLLF.Native.Facade.Tests;
internal static partial class LauncherLifecycleChecks
{
    // Shared exact Lock-family entry for the separate ReviewFixes-All owner.
    // This does not claim Transaction/Recovery/Redirects/Workflow coverage.
    internal static void ReviewLocks(FixtureEnvironment environment)
    {
        var previous = Seen.ToArray(); int previousCount = assertions;
        Seen.Clear(); assertions = 0;
        string root = Path.Combine(environment.Data, "lifecycle-review-locks-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            FileOperationLifetime(Path.Combine(root, "flat"), false);
            FileOperationLifetime(Path.Combine(root, "nested"), true);
            FileOperationPreCanceled(Path.Combine(root, "pre-cancel"));
            string[] missing = LauncherLifecycleOriginalChecks.Messages.Keys.Where(key => key.StartsWith("review:", StringComparison.Ordinal)).Except(Seen).ToArray();
            Native.Check(missing.Length == 0, "Review Lock-family IDs not reached: " + string.Join(", ", missing));
            environment.AssertNoForbiddenIo();
            Console.WriteLine("PASS actual Main ReviewFixes Lock family: flat+nested cancellation/finally lease, late child and pre-canceled guard; producer/UI completion verified.");
        }
        finally
        {
            Native.Check(Path.GetDirectoryName(root) == environment.Data && Path.GetFileName(root).StartsWith("lifecycle-review-locks-", StringComparison.Ordinal), "Unsafe review-lock root.");
            if (Directory.Exists(root)) Directory.Delete(root, true);
            Seen.Clear(); foreach (string id in previous) Seen.Add(id); assertions = previousCount;
        }
    }

    private static void VersionCancellation(string parent)
    {
        string root = Path.Combine(parent, "LivingPCL-CancelScan-" + Guid.NewGuid().ToString("N")), versions = Path.Combine(root, "versions");
        string kept = Path.Combine(versions, "kept"), canceled = Path.Combine(versions, "26.3"), empty = Path.Combine(versions, "empty");
        Type scanner = Native.Type("PCL.McInstanceDirectoryScanner"); Native.Check(scanner.Assembly == Native.Main, "Missing actual Main version scanner; BCL fallback forbidden.");
        bool HasFiles(DirectoryInfo dir) => (bool)Native.Call(scanner, "HasFiles", null, dir)!;
        DirectoryInfo[] Directories(string folder) => (DirectoryInfo[])Native.Call(scanner, "GetDirectories", null, folder)!;
        try
        {
            foreach (string dir in new[] { kept, canceled, empty }) Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(kept, "kept.json"), "{}"); File.WriteAllText(Path.Combine(canceled, "26.3.json"), "{}");
            DirectoryInfo[] snapshot = Directories(versions); DirectoryInfo removed = snapshot.Single(dir => dir.Name == "26.3");
            Check("version:51", removed.Exists); File.Delete(Path.Combine(canceled, "26.3.json")); Directory.Delete(canceled);
            Check("version:54", !HasFiles(removed)); Check("version:58", HasFiles(snapshot.Single(dir => dir.Name == "kept"))); Check("version:59", !HasFiles(snapshot.Single(dir => dir.Name == "empty")));
            for (int i = 0; i < 10; i++)
            {
                Directory.CreateDirectory(canceled); File.WriteAllText(Path.Combine(canceled, "26.3.json"), "{}"); DirectoryInfo stale = new(canceled); Check("version:63", stale.Exists);
                File.Delete(Path.Combine(canceled, "26.3.json")); Directory.Delete(canceled); Check("version:54", !HasFiles(stale), "replay=" + i);
            }
            string missing = Path.Combine(root, "removed-versions"); Directory.CreateDirectory(missing); Directory.Delete(missing); Check("version:69", Directories(missing).Length == 0);
            // Original scanner samples above remain unchanged. Additional real
            // Main loader/file guard + actual Worker/lease proves cancellation
            // and final cleanup, rather than calling stale snapshots 'a worker'.
            Directory.CreateDirectory(canceled); File.WriteAllText(Path.Combine(canceled, "26.3.json"), "{}");
            using ManualResetEventSlim entered = new(false), stopped = new(false);
            int released = 0, actualThread = 0, scans = 0; int callerThread = Environment.CurrentManagedThreadId;
            ProducerScope scope = new();
            try
            {
                var step = new TaskLoader("actual canceled version scan", task =>
                {
                    scope.Observe(Thread.CurrentThread); actualThread = Environment.CurrentManagedThreadId;
                    DirectoryInfo[] liveSnapshot = Directories(versions); entered.Set();
                    try
                    {
                        Stopwatch timeout = Stopwatch.StartNew(); while (Volatile.Read(ref released) == 0) { if (timeout.ElapsedMilliseconds >= 5000) throw new TimeoutException("Version scan release missing."); Thread.SpinWait(1000); }
                        if (liveSnapshot.Any(dir => dir.Name == "26.3" && HasFiles(dir))) throw new InvalidOperationException("Deleted scanner child reappeared.");
                        Interlocked.Increment(ref scans); task.CreateCancellationToken().ThrowIfCancellationRequested();
                    }
                    finally { File.WriteAllText(Path.Combine(root, "scan-cleanup.txt"), "complete"); }
                });
                var combo = new ComboLoader("version scan cancellation", [step]); scope.Root(combo);
                Action work = (Action)Native.Static("PCL.LoaderFileOperation", "CreateWorkload", combo, root)!;
                var worker = new RedoableWorker((Action)(() => { scope.Observe(Thread.CurrentThread); work(); }));
                var instance = new ModMinecraft.McInstance(root);
                Check("version:worker-lease", ModMinecraft.McInstanceLock.TryAcquire(worker, instance.PathVersion));
                worker.Stopped += () => stopped.Set(); scope.Stopped(stopped);
                worker.Start();
                try
                {
                    Check("version:worker-enter", entered.Wait(5000) && actualThread != callerThread);
                    File.Delete(Path.Combine(canceled, "26.3.json")); Directory.Delete(canceled); scope.Observe(step.LastRunningThread);
                    combo.Cancel();
                    Check("version:worker-hold", !stopped.Wait(250) && ModMinecraft.McInstanceLock.IsLocked(instance) && !File.Exists(Path.Combine(root, "scan-cleanup.txt")));
                }
                finally { Volatile.Write(ref released, 1); }
                Check("version:worker-end", stopped.Wait(5000) && worker.WaitIfRunning(5000) && scans == 1 && !ModMinecraft.McInstanceLock.IsLocked(instance) && File.ReadAllText(Path.Combine(root, "scan-cleanup.txt")) == "complete");
            }
            finally { Volatile.Write(ref released, 1); scope.Dispose(); }
        }
        finally
        {
            string full = Path.GetFullPath(root), prefix = Path.GetFullPath(Path.GetTempPath()).TrimEnd('\\') + "\\";
            Check("version:21", full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && Regex.IsMatch(Path.GetFileName(full), "^LivingPCL-CancelScan-[a-f0-9]{32}$"));
            if (Directory.Exists(full))
            {
                string childPrefix = full.TrimEnd('\\') + "\\";
                foreach (string file in Directory.GetFiles(full, "*", SearchOption.AllDirectories))
                {
                    Check("version:25", file.StartsWith(childPrefix, StringComparison.OrdinalIgnoreCase));
                    Native.Check((File.GetAttributes(file) & FileAttributes.ReparsePoint) == 0, "Unexpected linked scanner fixture file."); File.Delete(file);
                }
                foreach (string directory in Directory.GetDirectories(full, "*", SearchOption.AllDirectories).OrderByDescending(path => path.Length))
                {
                    Check("version:29", directory.StartsWith(childPrefix, StringComparison.OrdinalIgnoreCase));
                    Native.Check((File.GetAttributes(directory) & FileAttributes.ReparsePoint) == 0, "Unexpected linked scanner fixture directory."); Directory.Delete(directory, false);
                }
                Directory.Delete(full, false);
            }
        }
        Check("version:77", true);
        Console.WriteLine("PASS native lifecycle version cancellation: real Main stale snapshot/10replays/siblings/deleted root plus actual canceled loader/thread/worker lease and completed finally; no download/game.");
    }

    private static void FileOperationLifetime(string folder, bool nested)
    {
        Directory.CreateDirectory(folder);
        using ManualResetEventSlim entered = new(false), stopped = new(false), cleaned = new(false);
        int released = 0, executions = 0;
        ProducerScope scope = new();
        try
        {
            Action<CancellationToken> copy = token =>
            {
                scope.Observe(Thread.CurrentThread); Interlocked.Increment(ref executions); entered.Set();
                try { Stopwatch limit = Stopwatch.StartNew(); while (Volatile.Read(ref released) == 0) { if (limit.ElapsedMilliseconds >= 5000) throw new TimeoutException("File fixture release missing."); Thread.SpinWait(10000); } token.ThrowIfCancellationRequested(); }
                finally { File.WriteAllText(Path.Combine(folder, "cleanup.txt"), "complete"); cleaned.Set(); }
            };
            var step = new TaskLoader("guarded file fixture", task =>
            {
                scope.Observe(Thread.CurrentThread); CancellationToken token = task.CreateCancellationToken(); if (!nested) { copy(token); return; }
                var leaf = new ModLoader.LoaderTask<int, int>("nested copy", current => { scope.Observe(Thread.CurrentThread); copy(current.CreateCancellationToken()); }) { Show = false };
                var inner = new ComboLoader("nested preparation", [new ModLoader.LoaderCombo<int>("hidden nested task", [leaf])]); scope.Root(inner);
                using (token.Register(inner.Cancel)) ((Action)Native.Static("PCL.LoaderFileOperation", "CreateWorkload", inner, folder)!)();
            });
            var combo = new ComboLoader("file fixture", [step]); scope.Root(combo);
            Action actual = (Action)Native.Static("PCL.LoaderFileOperation", "CreateWorkload", combo, folder)!;
            var worker = new RedoableWorker((Action)(() => { scope.Observe(Thread.CurrentThread); actual(); }));
            var instance = new ModMinecraft.McInstance(folder); Check("review:75", ModMinecraft.McInstanceLock.TryAcquire(worker, instance.PathVersion));
            worker.Stopped += () => stopped.Set(); scope.Stopped(stopped);
            try
            {
                worker.Start(); Check("review:78", entered.Wait(5000)); scope.Observe(step.LastRunningThread); combo.Cancel();
                Check("review:80", !stopped.Wait(250)); Check("review:81", ModMinecraft.McInstanceLock.IsLocked(instance)); Check("review:82", !cleaned.IsSet);
            }
            finally { Volatile.Write(ref released, 1); }
            Check("review:85", stopped.Wait(5000)); Check("review:86", cleaned.Wait(5000)); Check("review:89", !ModMinecraft.McInstanceLock.IsLocked(instance));
            step.Start(folder, true); scope.Observe(step.LastRunningThread);
            Check("review:late-thread", SpinWait.SpinUntil(() => step.State != ModBase.LoadState.Loading, 5000));
            Check("review:94", executions == 1);
        }
        finally { Volatile.Write(ref released, 1); scope.Dispose(); }
    }

    private static void FileOperationPreCanceled(string folder)
    {
        Directory.CreateDirectory(folder); using ManualResetEventSlim stopped = new(false); ProducerScope scope = new();
        try
        {
            var step = new TaskLoader("pre-canceled fixture", _ => File.WriteAllText(Path.Combine(folder, "unexpected.txt"), "bad"));
            var combo = new ComboLoader("pre-canceled fixture", [step]); scope.Root(combo); Check("review:99", Native.Main.GetType("PCL.LoaderFileOperation") is not null);
            Action actual = (Action)Native.Static("PCL.LoaderFileOperation", "CreateWorkload", combo, folder)!;
            var worker = new RedoableWorker((Action)(() => { scope.Observe(Thread.CurrentThread); actual(); })); var instance = new ModMinecraft.McInstance(folder);
            Check("review:104", ModMinecraft.McInstanceLock.TryAcquire(worker, instance.PathVersion)); worker.Stopped += () => stopped.Set(); scope.Stopped(stopped);
            combo.Cancel(); worker.Start(); Check("review:107", stopped.Wait(5000));
            Check("review:108", !File.Exists(Path.Combine(folder, "unexpected.txt"))); Check("review:109", !ModMinecraft.McInstanceLock.IsLocked(instance));
        }
        finally { scope.Dispose(); }
    }
}
