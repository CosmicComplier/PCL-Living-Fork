using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Windows.Threading;
using MeloongCore;
using Newtonsoft.Json.Linq;
using PCL;
using PCLCS;
using static PCLLF.Native.Facade.Tests.ModpackUpdateChecks;

namespace PCLLF.Native.Facade.Tests;

// Completes the archive/UI fixtures with the SAME production ModpackUpdateFlow,
// LoaderFileOperation guard, taskbar combo, RedoableWorker, instance lease,
// environment snapshot and commit. Only existing file-picker/confirmation/temp
// and notification primitives are controlled; none of the algorithm is copied.
internal static class ModpackUpdateFlowChecks
{
    internal static void Run(FixtureEnvironment environment, string fixtureRoot)
    {
        ModpackUpdateCleanup.VerifyIsolatedChild();
        using FileTaskScope scope = new(environment);
        string root = Path.Combine(fixtureRoot, "flow"); Directory.CreateDirectory(root);
        string minecraft = Path.Combine(root, "minecraft"); scope.SelectMinecraft(minecraft);
        _ = FileTaskShell.Install();
        using LiveFlow flow = new(root);
        ModMinecraft.McInstance blocked = Instance(minecraft, "guard-1.20.1");
        string next = Pack(root, "flow-next", new() { ["config/author.txt"] = "new-author", ["config/personal.txt"] = "new-default" }, fabric: false);
        flow.Archives.Enqueue(next);
        int selected = flow.PickerCalls;
        RedoableWorker holder = new((Action)(() => { }));
        Check(ModMinecraft.McInstanceLock.TryAcquire(holder, blocked.PathVersion), "FLOW-01-reserve", "cannot acquire real instance reservation");
        try
        {
            flow.Start(blocked);
            Check(flow.PickerCalls == selected && flow.Worker is null && flow.RedHints.Count == 1 && !flow.Updating, "FLOW-01", "locked instance reached picker/worker or retained Active");
        }
        finally
        {
            holder.Start(); Check(holder.WaitIfRunning(5000), "FLOW-cleanup-holder", "reservation holder did not stop");
            Until(() => !ModMinecraft.McInstanceLock.IsLocked(blocked), "FLOW-cleanup-lease");
        }
        flow.Archives.Clear();

        // No author-origin exists: refusal must precede parsing the old package,
        // environment snapshots and any write. The genuine worker owns the lease.
        ModMinecraft.McInstance missing = Instance(minecraft, "no-origin-1.20.1");
        flow.Archives.Enqueue(next); flow.Confirm = (_, _, _, _, _) => 2;
        flow.Preview = _ => throw new InvalidOperationException("Refused baseline must not reach preview.");
        flow.Start(missing); flow.WaitStopped();
        Check(flow.Messages.Count == 1 && flow.Messages[0] == "建立整合包记录" && flow.Step!.State == ModBase.LoadState.Canceled && !flow.Worker!.LastSucceeded, "FLOW-02", "baseline refusal was not real cancellation");
        Check(Update("ReadManifest", missing.PathVersion) is null && !File.Exists(Path.Combine(missing.PathIndie, "config/author.txt")), "FLOW-02-bytes", "baseline refusal wrote author files/manifest");

        // The workflow first asks for the next archive, then asks for the current
        // original archive while establishing origin. Keep that actual sequence.
        ModMinecraft.McInstance declined = Instance(minecraft, "declined-1.20.1");
        Put(Path.Combine(declined.PathIndie, "config/author.txt"), "old-author");
        Put(Path.Combine(declined.PathIndie, "config/personal.txt"), "my-edit");
        string old = Pack(root, "flow-old", new() { ["config/author.txt"] = "old-author", ["config/personal.txt"] = "old-default" }, fabric: false);
        flow.Messages.Clear(); flow.Archives.Enqueue(next); flow.Archives.Enqueue(old);
        flow.Confirm = (_, _, _, _, _) => 1;
        object? shown = null;
        flow.Preview = plan => { shown = plan; return 2; };
        flow.Start(declined); flow.WaitStopped();
        Check(shown is not null && shown.GetType().Assembly == Native.Main && Changes(shown).Any(Conflict), "FLOW-03-plan", "preview did not receive actual personal-preserving plan");
        Check(flow.Step!.State == ModBase.LoadState.Canceled && !flow.Worker!.LastSucceeded && flow.Messages.SequenceEqual(new[] { "建立整合包记录" }), "FLOW-03", "preview rejection did not cancel after establishing the correct origin");
        Check(File.ReadAllText(Path.Combine(declined.PathIndie, "config/author.txt")) == "old-author" && File.ReadAllText(Path.Combine(declined.PathIndie, "config/personal.txt")) == "my-edit" && Update("ReadManifest", declined.PathVersion) is null, "FLOW-03-bytes", "refused preview committed files or persisted origin");

        // Cancel the REAL taskbar combo while the existing confirmation primitive
        // is held. Its file-workload guard must wait for the real step finally;
        // a terminal loader state alone must not release Active/the instance lock.
        ModMinecraft.McInstance canceled = Instance(minecraft, "canceled-1.20.1");
        SeedOrigin(canceled, old, root, "cancel-origin");
        string canceledOrigin = File.ReadAllText(Path.Combine(canceled.PathVersion, "PCL", "ModpackOrigin.json"));
        flow.Archives.Enqueue(next);
        using ManualResetEventSlim entered = new(), release = new();
        flow.Preview = _ =>
        {
            entered.Set();
            if (!release.Wait(10000)) throw new TimeoutException("Cancellation confirmation fixture was not released.");
            return 1;
        };
        try
        {
            flow.Start(canceled);
            Until(() => entered.IsSet, "FLOW-04-enter");
            flow.Combo!.Cancel();
            Until(() => flow.Step!.State == ModBase.LoadState.Canceled, "FLOW-04-cancel-token");
            Check(flow.Worker!.Running && flow.Updating && ModMinecraft.McInstanceLock.IsLocked(canceled), "FLOW-04", "cancel released instance/Active before actual workload returned");
            Check(File.ReadAllText(Path.Combine(canceled.PathIndie, "config/author.txt")) == "old-author", "FLOW-04-bytes", "held confirmation already wrote target bytes");
        }
        finally { release.Set(); }
        flow.WaitStopped();
        Check(!flow.Worker!.LastSucceeded && File.ReadAllText(Path.Combine(canceled.PathIndie, "config/author.txt")) == "old-author" && File.ReadAllText(Path.Combine(canceled.PathIndie, "config/personal.txt")) == "my-edit" && File.ReadAllText(Path.Combine(canceled.PathVersion, "PCL", "ModpackOrigin.json")) == canceledOrigin, "FLOW-05", "real canceled flow changed author/personal/origin bytes");
        Check(!Directory.Exists(Path.Combine(canceled.PathVersion, "PCL", "PackUpdateBackups")), "FLOW-05-backup", "cancellation before commit created a transaction");

        // Accepted flow: use a same-runtime pack so the REAL history capture,
        // commit and reload run, without downloading/installing Minecraft itself.
        ModMinecraft.McInstance accepted = Instance(minecraft, "accepted-1.20.1");
        SeedOrigin(accepted, old, root, "accept-origin");
        PageInstanceLeft.Instance = accepted;
        Native.Field(typeof(ModMinecraft), "_McInstanceSelected").SetValue(null, accepted);
        flow.Archives.Enqueue(next);
        int previews = 0;
        flow.Preview = plan =>
        {
            previews++;
            Check(Changes(plan).Any(Conflict) && Changes(plan).Any(change => !Conflict(change)), "FLOW-06-preview", "accepted workflow discarded personal/author distinction");
            Check(File.ReadAllText(Path.Combine(accepted.PathIndie, "config/author.txt")) == "old-author", "FLOW-06-confirm", "target changed before confirmation");
            return 1;
        };
        int infos = flow.Infos.Count;
        flow.Start(accepted); flow.WaitStopped();
        Check(flow.Worker!.LastSucceeded && flow.Step!.State == ModBase.LoadState.Finished && previews == 1 && flow.Infos.Count == infos + 1, "FLOW-06", "accepted real workflow did not finish: " + flow.Step.Error);
        Check(File.ReadAllText(Path.Combine(accepted.PathIndie, "config/author.txt")) == "new-author" && File.ReadAllText(Path.Combine(accepted.PathIndie, "config/personal.txt")) == "my-edit", "FLOW-07", "accepted workflow lost author update or personal modification");
        string backup = flow.Step.Output;
        Check(!string.IsNullOrEmpty(backup) && Native.Full(backup).StartsWith(Native.Full(accepted.PathVersion) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && File.Exists(Path.Combine(backup, "transaction.json")), "FLOW-07-backup", "real step did not publish owned durable backup");
        Check(JObject.Parse(File.ReadAllText(Path.Combine(backup, "transaction.json")))["complete"]?.Value<bool>() == true, "FLOW-07-journal", "successful flow did not publish completed journal");
        Check(((IEnumerable<JObject>)Native.Static("PCL.EnvironmentHistory", "Points", accepted.PathVersion)!).Any(point => point["label"]?.ToString() == "整合包更新前"), "FLOW-08-history", "real pre-update environment snapshot missing");
        Check(PageInstanceLeft.Instance is not null && !ReferenceEquals(PageInstanceLeft.Instance, accepted) && Native.Full(PageInstanceLeft.Instance.PathVersion) == Native.Full(accepted.PathVersion) && ReferenceEquals(PageInstanceLeft.Instance, ModMinecraft.McInstanceSelected), "FLOW-08-reload", "actual selected/page instance was not reloaded together");
        Check(!((IEnumerable<string>)Update("Pending", accepted.PathVersion)!).Any() && !flow.Updating && !ModMinecraft.McInstanceLock.IsLocked(accepted), "FLOW-08-release", "successful actual workload left pending state/Active/lease");
        scope.FinishActualTasks();
        environment.AssertNoForbiddenIo();
        Console.WriteLine("PASS native modpack-update workflow: FLOW-01..08 (genuine admission lease, baseline/preview refusals, guarded cancellation, history/commit/reload).");
    }

    private static ModMinecraft.McInstance Instance(string minecraft, string name)
    {
        string folder = Path.Combine(minecraft, "versions", name);
        Put(Path.Combine(folder, name + ".json"), new JObject
        {
            ["id"] = name, ["clientVersion"] = "1.20.1", ["type"] = "release", ["mainClass"] = "fixture.Main",
            ["libraries"] = new JArray(), ["arguments"] = new JObject { ["game"] = new JArray(), ["jvm"] = new JArray() }
        }.ToString());
        Put(Path.Combine(folder, "PCL", "Setup.ini"), "VersionArgumentIndieV2:True\r\n");
        // Store a REAL config value before JsonConfigProvider captures this file.
        Put(Path.Combine(folder, "PCL", "config.json"), "{\"VersionArgumentIndieV2\":true}");
        ModMinecraft.McInstance instance = new(folder);
        FileTaskScope.CacheInstance(instance);
        instance.Load();
        Check(instance.State != ModMinecraft.McInstance.McInstanceState.Error && instance.Version.VanillaName == "1.20.1" && instance.Version.ModLoaderVersions.Count == 0 && Native.Full(instance.PathIndie) == Native.Full(folder), "FLOW-fixture-instance", "actual JSON parser/settings did not produce isolated original 1.20.1: " + instance.Info);
        return instance;
    }
    private static void SeedOrigin(ModMinecraft.McInstance instance, string archive, string root, string stage)
    {
        object original = Read(archive, Path.Combine(root, stage));
        Put(Path.Combine(instance.PathVersion, "PCL", "ModpackOrigin.json"), ((JObject)Update("Manifest", original)!).ToString());
        Put(Path.Combine(instance.PathIndie, "config/author.txt"), "old-author");
        Put(Path.Combine(instance.PathIndie, "config/personal.txt"), "my-edit");
    }
    private static void Pump(DateTime deadline)
    {
        if (DateTime.UtcNow >= deadline) throw new TimeoutException("Actual modpack UI drain exceeded its shared deadline.");
        DispatcherFrame frame = new();
        bool drained = false;
        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        // Same real bounded drain as CloneFixture: a Send-priority timer can
        // stop this frame even if normal UI traffic keeps ContextIdle starved.
        DispatcherTimer timer = new(DispatcherPriority.Send, dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(Math.Max(1, (deadline - DateTime.UtcNow).TotalMilliseconds))
        };
        EventHandler expired = (_, _) => frame.Continue = false;
        timer.Tick += expired;
        DispatcherOperation idle = dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => { drained = true; frame.Continue = false; }));
        timer.Start();
        try { Dispatcher.PushFrame(frame); }
        finally
        {
            timer.Stop(); timer.Tick -= expired;
            if (idle.Status == DispatcherOperationStatus.Pending) idle.Abort();
        }
        if (!drained || DateTime.UtcNow >= deadline) throw new TimeoutException("Actual modpack posted UI work did not drain before its shared deadline.");
    }
    private static void Until(Func<bool> done, string id) => Until(done, id, DateTime.UtcNow.AddSeconds(10));
    private static void Until(Func<bool> done, string id, DateTime deadline)
    {
        while (!done()) { Check(DateTime.UtcNow < deadline, id, "actual workload/callback did not reach expected state"); Pump(deadline); Thread.Sleep(2); }
        Pump(deadline);
    }

    private sealed class LiveFlow : IDisposable
    {
        private readonly object host;
        private readonly Stack<Action> restore = new();
        private readonly List<RedoableWorker> workers = new();
        private readonly List<ModLoader.LoaderTask<string, string>> steps = new();
        private readonly Dictionary<RedoableWorker, ModLoader.LoaderCombo<string>> combos = new();
        private readonly ConcurrentDictionary<RedoableWorker, byte> stopped = new();
        private readonly ConcurrentBag<Thread> workloadThreads = new();
        private readonly string root;
        private int stage;
        internal readonly Queue<string> Archives = new();
        internal readonly List<string> Messages = new(), Infos = new(), RedHints = new();
        internal int PickerCalls;
        internal ModLoader.LoaderTask<string, string>? Step;
        internal ModLoader.LoaderCombo<string>? Combo;
        internal RedoableWorker? Worker;
        internal Func<string, string, string, string, bool, int> Confirm = (_, _, _, _, _) => 1;
        internal Func<object, int> Preview = _ => 2;
        private static bool AnyLocked => (bool)Native.Property(typeof(ModMinecraft.McInstanceLock), "IsAnyLocked").GetValue(null)!;
        internal bool Updating => (bool)Native.Property(Native.Type("PCL.ModpackUpdateFlow"), "IsUpdating").GetValue(null)!;
        internal LiveFlow(string root)
        {
            this.root = root;
            object engine = Native.Field(Native.Type("PCL.ModpackUpdateFlow"), "Engine").GetValue(null)!;
            Check(engine.GetType().Assembly.GetName().Name == "PCLCS", "FLOW-engine-identity", "facade is not using actual migrated flow");
            host = Native.Get(engine, "host")!;
            Replace("SelectFile", new Func<string, string>(_ => { PickerCalls++; return Archives.Count == 0 ? throw new InvalidOperationException("Unexpected file picker.") : Archives.Dequeue(); }));
            Replace("Message", new Func<string, string, string, string, bool, int>((text, title, first, second, warning) => { Messages.Add(title); return Confirm(text, title, first, second, warning); }));
            Replace("HintRed", new Action<string>(RedHints.Add));
            Replace("Hint", new Action<string>(Infos.Add));
            Replace("Info", new Action<string>(Infos.Add));
            Replace("TempFolder", new Func<string>(() => { string folder = Path.Combine(this.root, "stage-" + Interlocked.Increment(ref stage)); Directory.CreateDirectory(folder); return folder; }));
            FieldInfo preview = Native.Field(host.GetType(), "Preview");
            Type actualPlan = preview.FieldType.GetGenericArguments()[1];
            Delegate callback = (Delegate)typeof(LiveFlow).GetMethod(nameof(CreatePreview), BindingFlags.NonPublic | BindingFlags.Instance)!.MakeGenericMethod(actualPlan).Invoke(this, null)!;
            Replace("Preview", callback);
            var createStep = (Func<string, Action<ModLoader.LoaderTask<string, string>>, ModLoader.LoaderTask<string, string>>)Native.Get(host, "CreateStep")!;
            Replace("CreateStep", new Func<string, Action<ModLoader.LoaderTask<string, string>>, ModLoader.LoaderTask<string, string>>((name, work) =>
            {
                Step = createStep(name, current => { workloadThreads.Add(Thread.CurrentThread); work(current); });
                steps.Add(Step); return Step;
            }));
            var createWorker = (Func<ModLoader.LoaderCombo<string>, string, RedoableWorker>)Native.Get(host, "CreateWorker")!;
            Replace("CreateWorker", new Func<ModLoader.LoaderCombo<string>, string, RedoableWorker>((combo, folder) =>
            {
                Combo = combo; Worker = createWorker(combo, folder); workers.Add(Worker); combos.Add(Worker, combo); return Worker;
            }));
            var onStopped = (Action<RedoableWorker, Action>)Native.Get(host, "OnStopped")!;
            Replace("OnStopped", new Action<RedoableWorker, Action>((worker, release) => onStopped(worker, () =>
            {
                release(); // The actual Active release, after the actual lease's earlier handler.
                stopped.TryAdd(worker, 0);
            })));
            Check(!Updating, "FLOW-initial-active", "another real update is already active");
        }
        private Func<string, TPlan, Func<McVersion>, int> CreatePreview<TPlan>() => (_, plan, _) => Preview(plan!);
        private void Replace(string name, object replacement)
        {
            FieldInfo field = Native.Field(host.GetType(), name); object? previous = field.GetValue(host);
            Check(field.FieldType.IsInstanceOfType(replacement), "FLOW-host-" + name, "platform replacement has incorrect real delegate type");
            restore.Push(() => field.SetValue(host, previous)); field.SetValue(host, replacement);
        }
        internal void Start(ModMinecraft.McInstance instance)
        {
            Native.Check(Thread.CurrentThread.GetApartmentState() == ApartmentState.STA, "Native update fixture requires actual WPF STA.");
            Native.Static("PCL.ModpackUpdateFlow", "Start", instance, false);
        }
        internal void WaitStopped()
        {
            Check(Worker is not null && Step is not null && Combo is not null, "FLOW-worker", "actual workflow did not create its real worker/step/combo");
            DateTime deadline = DateTime.UtcNow.AddSeconds(10);
            Until(() => !Worker!.Running && stopped.ContainsKey(Worker) && !Updating && !AnyLocked && workloadThreads.All(thread => !thread.IsAlive) && steps.All(step => step.LastRunningThread?.IsAlive != true), "FLOW-stopped", deadline);
            Check(Step!.State is ModBase.LoadState.Finished or ModBase.LoadState.Canceled or ModBase.LoadState.Failed, "FLOW-step-terminal", "real worker stopped with an unfinished step");
            Pump(deadline);
        }
        public void Dispose()
        {
            try
            {
                foreach (RedoableWorker worker in workers)
                    if (worker.Running) { combos[worker].Cancel(); worker.Cancel(); }
                bool Quiescent() => workers.All(worker => !worker.Running && stopped.ContainsKey(worker)) &&
                    workloadThreads.All(thread => !thread.IsAlive) && steps.All(step => step.LastRunningThread?.IsAlive != true) &&
                    !Updating && !AnyLocked && ModMinecraft.McInstanceListLoader.State != ModBase.LoadState.Loading &&
                    ModMinecraft.McInstanceListLoader.LastRunningThread?.IsAlive != true;
                DateTime deadline = DateTime.UtcNow.AddSeconds(10);
                while (true)
                {
                    // Actual producer threads must return before their terminal
                    // UI posts can be considered drained; idle/Busy alone is not a barrier.
                    Pump(deadline);
                    if (Quiescent()) { Pump(deadline); if (Quiescent()) break; }
                    Check(DateTime.UtcNow < deadline, "FLOW-cleanup", "real worker/stopped/lease/thread/UI completion timed out");
                    Thread.Sleep(2);
                }
            }
            catch (Exception error)
            {
                ModpackUpdateCleanup.TerminateChild("FLOW cleanup could not establish stopped/lease/thread/UI completion; roots and callbacks remain isolated. " + error);
            }
            // No finally: a timeout must never pop production delegates and then
            // allow FileTaskScope to restore roots while real work is still alive.
            while (restore.Count > 0) restore.Pop()();
        }
    }
}

internal static class ModpackUpdateCleanup
{
    private static bool verifiedChild;
    internal static void VerifyIsolatedChild()
    {
        string[] args = Environment.GetCommandLineArgs();
        int option = Array.IndexOf(args, "--fixture");
        Native.Check(Assembly.GetEntryAssembly() == typeof(ModpackUpdateCleanup).Assembly &&
            typeof(ModpackUpdateCleanup).Assembly.GetName().Name == "PCLLF.Native.Facade.Tests" &&
            args.Contains("--isolated", StringComparer.Ordinal) && option >= 0 && option + 1 < args.Length,
            "Modpack work must run in the genuine isolated Native test child.");
        string root = args[option + 1];
        Program.ValidateRoot(root);
        Native.Check(Native.Full(AppContext.BaseDirectory) == Native.Full(Path.Combine(root, "output")), "Modpack child output escaped its GUID fixture.");
        verifiedChild = true; // Established before starting any real worker/download.
    }
    [DoesNotReturn]
    internal static void TerminateChild(string diagnostics)
    {
        if (!verifiedChild) throw new InvalidOperationException("No isolated test-child proof; no asynchronous modpack work may start. " + diagnostics);
        Console.Error.WriteLine("FAIL native modpack cleanup; terminating ONLY the current verified isolated test child (PID " + Environment.ProcessId + "): " + diagnostics);
        Console.Error.Flush(); Console.Out.Flush();
        // Exit does not unwind managed finally blocks. The outer host owns this
        // child's GUID directory and reports this nonzero exit; no product,
        // shell, parent process or other child is enumerated/terminated.
        Environment.Exit(93);
        Environment.FailFast("The isolated modpack test child unexpectedly survived Environment.Exit.");
    }
}
