using MeloongCore;
using Newtonsoft.Json.Linq;
using PCL;
using System.Windows.Threading;

namespace PCLLF.Native.Facade.Tests;

internal static partial class NativeUpdateChecks
{
    private static object Transport()
    {
        object transport = Activator.CreateInstance(Native.Type(Updater + "+UpdateTransport"), true)!;
        Native.Set(transport, "ReadJson", new Func<string, JObject>(url => throw new InvalidOperationException("Unexpected updater JSON request: " + url)));
        Native.Set(transport, "ReadReleaseList", new Func<string, JArray>(url => throw new InvalidOperationException("Unexpected updater release-list request: " + url)));
        Native.Set(transport, "ReadText", new Func<string, string>(url => throw new InvalidOperationException("Unexpected updater manifest request: " + url)));
        Native.Set(transport, "Download", new Action<string, string, FileChecker>((_, _, _) => throw new InvalidOperationException("Unexpected updater download.")));
        return transport;
    }

    private static void WorkflowAndCancellation(string root, string source)
    {
        const string tag = "v99.0.0";
        const string name = "PCL-Living-Fork-99.0.0.exe";
        string hash = Hash(source);
        long size = new FileInfo(source).Length;
        JObject info = Release(tag, size);
        object transport = Transport();
        int reads = 0, downloads = 0;
        Native.Set(transport, "ReadText", new Func<string, string>(url =>
        {
            reads++; Native.Check(url == Cnb + tag + "/SHA256SUMS.txt", "Workflow changed its confirmed manifest endpoint.");
            return hash + "  " + name;
        }));
        string staged = Path.Combine(root, "workflow-success.exe");
        Native.Set(transport, "Download", new Action<string, string, FileChecker>((url, target, checker) =>
        {
            downloads++;
            Native.Check(url == Cnb + tag + "/" + name && Native.Full(target) == Native.Full(staged) &&
                checker.Hash == hash && checker.ActualSize == size, "Workflow lost confirmed asset URL/hash/size/target identity.");
            File.Copy(source, target);
        }));
        Native.Check((string)Call("DownloadUpdate", info, staged, transport)! == hash && Hash(staged) == hash && reads == 1 && downloads == 1,
            "Actual Main did not validate and stage the exact confirmed candidate.");
        Pass("UPD-01", "actual Main DownloadUpdate validates confirmed release URL/name/size/SHA and real Main bytes via the production transport seam");

        // Inspection only: preserve the actual LF updater -> LoaderCombo ->
        // LoaderDownload/NetFile graph, without opening an HTTP connection.
        string loaderTarget = Path.Combine(root, "not-started.exe");
        var combo = (ModLoader.LoaderCombo<int>)Call("CreateUpdateDownloadTask", Cnb + tag + "/" + name, loaderTarget,
            new FileChecker { Hash = hash, ActualSize = size })!;
        Native.Check(combo.GetType().Assembly == Native.Main && combo.Name == "下载启动器更新：v99.0.0（CNB）" && combo.Loaders.Count == 1 &&
            combo.Loaders[0] is ModNet.LoaderDownload download && download.GetType().Assembly == Native.Main &&
            download.Name == "下载启动器更新" && download.Files.Count == 1 && Native.Full(download.Files.Single().LocalPath) == Native.Full(loaderTarget) &&
            !File.Exists(loaderTarget), "Actual updater download task lost its real loader/filename/source contract.");
        Pass("UPD-02", "real updater constructs its actual Main Combo/LoaderDownload/NetFile with original name/source; task deliberately not started over HTTP");

        // A confirmed candidate may move sources, but not change release or hash.
        JObject otherRelease = Release("v98.0.0", size, false);
        object wrongVersion = Transport();
        Native.Set(wrongVersion, "ReadJson", new Func<string, JObject>(url =>
        {
            Native.Check(url == "https://api.github.com/repos/DDZmumo/PCL-Living-Fork/releases/tags/" + tag,
                "Source switch requested an unconfirmed release endpoint.");
            return otherRelease;
        }));
        Reject(() => Call("ReleaseForDownloadSource", info, Native.Enum(Updater + "+UpdateSource", "GitHub"), wrongVersion),
            "Switching sources changed the confirmed release version.");
        object mismatch = Transport(); int fallbacks = 0, attempts = 0, manifestReads = 0;
        Native.Set(mismatch, "ReadText", new Func<string, string>(url =>
        {
            manifestReads++;
            Native.Check(url == (manifestReads == 1 ? Cnb : GitHub) + tag + "/SHA256SUMS.txt", "Fallback read an unexpected hash endpoint.");
            return (manifestReads == 1 ? hash : new string('0', 64)) + "  " + name;
        }));
        Native.Set(mismatch, "ReadJson", new Func<string, JObject>(url =>
        {
            fallbacks++;
            Native.Check(url.EndsWith("/releases/tags/" + tag, StringComparison.Ordinal), "Fallback changed confirmed version endpoint.");
            return Release(tag, size, false);
        }));
        Native.Set(mismatch, "Download", new Action<string, string, FileChecker>((_, _, _) => { attempts++; throw new IOException("Controlled unavailable first-source download."); }));
        string mismatchTarget = Path.Combine(root, "hash-mismatch.exe");
        Reject(() => Call("DownloadUpdate", info, mismatchTarget, mismatch), "Different fallback-source hash was accepted.");
        Native.Check(fallbacks == 1 && attempts == 1 && manifestReads == 2 && !File.Exists(mismatchTarget),
            "Fallback hash mismatch downloaded or staged a second candidate.");
        Pass("UPD-03", "actual source switch rejects different version; actual fallback rejects a different SHA before its second download");

        CancellationRetainsLease(root, info, hash, name);
    }

    private static IDisposable? Acquire(params string[] names)
        => (IDisposable?)Native.Static("PCL.LauncherSessionLock", "TryAcquire", names, 0);

    private static bool CanAcquireOnAnotherThread(params string[] names)
    {
        Task<bool> probe = Task.Run(() =>
        {
            using IDisposable? lease = Acquire(names);
            return lease is not null;
        });
        Native.Check(probe.Wait(5000), "Actual Main lease contention probe timed out.");
        return probe.GetAwaiter().GetResult();
    }

    private static void CancellationRetainsLease(string root, JObject info, string hash, string name)
    {
        string prefix = "PclNativeUpdater-" + Guid.NewGuid().ToString("N");
        string first = prefix + "-first", held = prefix + "-held";
        string target = Path.Combine(root, "worker-canceled.exe");
        using ManualResetEventSlim entered = new(false), cancellationObserved = new(false), release = new(false), stopped = new(false);
        int attempts = 0, manifestReads = 0, fallbackReads = 0, canceled = 0, succeeded = 0;
        Exception? workerFailure = null;
        OperationCanceledException? original = null, forwarded = null;
        object transport = Transport();
        Native.Set(transport, "ReadText", new Func<string, string>(url =>
        {
            Interlocked.Increment(ref manifestReads);
            Native.Check(url == Cnb + "v99.0.0/SHA256SUMS.txt", "Canceled workflow changed its confirmed manifest URL.");
            return hash + "  " + name;
        }));
        Native.Set(transport, "ReadJson", new Func<string, JObject>(_ => { Interlocked.Increment(ref fallbackReads); throw new InvalidOperationException("Cancellation must not request another source."); }));
        Native.Set(transport, "ReadReleaseList", new Func<string, JArray>(_ => { Interlocked.Increment(ref fallbackReads); throw new InvalidOperationException("Cancellation must not enumerate releases."); }));
        var worker = new RedoableWorker(new Action<CancellationToken?>(token =>
        {
            CancellationToken cancel = token ?? throw new InvalidOperationException("Actual worker did not supply its cancellation token.");
            using IDisposable lease = Acquire(held) ?? throw new InvalidOperationException("GUID updater fixture lease was already held.");
            Native.Set(transport, "Download", new Action<string, string, FileChecker>((url, destination, checker) =>
            {
                Interlocked.Increment(ref attempts);
                Native.Check(url == Cnb + "v99.0.0/" + name && Native.Full(destination) == Native.Full(target) && checker.Hash == hash,
                    "Canceled workflow lost confirmed download candidate identity.");
                entered.Set();
                Native.Check(cancel.WaitHandle.WaitOne(5000), "Real loader cancellation did not reach the actual worker token.");
                cancellationObserved.Set();
                Native.Check(release.Wait(5000), "Cancellation fixture was not released for finally cleanup.");
                original = new OperationCanceledException("Controlled transport sees the real worker cancellation token.", cancel);
                throw original;
            }));
            try { Call("DownloadUpdate", info, target, transport); }
            catch (OperationCanceledException error) { forwarded = error; throw; }
        }));
        worker.Canceled += () => Interlocked.Increment(ref canceled);
        worker.Succeeded += () => Interlocked.Increment(ref succeeded);
        worker.Failed += error => workerFailure = error;
        worker.Stopped += () => stopped.Set();
        var loader = new ModLoader.LoaderWorker("Native updater transport cancellation fixture", worker);
        try
        {
            loader.Start();
            Native.Check(entered.Wait(5000), "Actual Main loader/Shared worker did not enter DownloadUpdate.");
            Native.Check(!CanAcquireOnAnotherThread(held), "Running workload did not hold its actual Main lease.");
            Native.Check(!CanAcquireOnAnotherThread(first, held) && CanAcquireOnAnotherThread(first),
                "Failed multiple-lease acquisition leaked its first GUID mutex.");
            loader.Cancel();
            Native.Check(cancellationObserved.Wait(5000) && worker.Running && loader.State == ModBase.LoadState.Canceled,
                "Actual Main loader did not cancel its real running worker before workload teardown.");
            Native.Check(!CanAcquireOnAnotherThread(held), "Cancel prematurely released the lease before workload finally.");
        }
        finally
        {
            loader.Cancel(); release.Set();
            Native.Check(worker.WaitIfRunning(5000) && stopped.Wait(5000), "Canceled updater workload did not fully stop.");
            DrainPostedUi();
        }
        Native.Check(workerFailure is null && canceled == 1 && succeeded == 0 && !worker.Running &&
            original is not null && ReferenceEquals(original, forwarded) && original.CancellationToken.IsCancellationRequested &&
            manifestReads == 1 && attempts == 1 && fallbackReads == 0 && !File.Exists(target),
            "Main swallowed/translated cancellation, retried, published a candidate or signaled wrong worker terminal events: " + workerFailure);
        Native.Check(CanAcquireOnAnotherThread(first, held), "Completed cancellation failed to release its actual GUID session leases.");
        Pass("UPD-04", "real Main LoaderWorker.Cancel -> real Shared worker token -> actual Main DownloadUpdate same OCE/no fallback; GUID Main session lease held through cancellation and released after finally");
        Console.WriteLine("SCOPE native updater UPD-04 composes real production worker/loader/lease primitives around the genuine backend transport seam; DownloadAndUpdate UI/helper launch and partial HTTP merge cancellation are not exercised.");
    }

    private static void DrainPostedUi()
    {
        DispatcherFrame frame = new();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }
}
