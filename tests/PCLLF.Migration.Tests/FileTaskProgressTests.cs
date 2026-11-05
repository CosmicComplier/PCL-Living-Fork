using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using MeloongCore;
using Newtonsoft.Json.Linq;
using PCLCS;

// Real file-task regressions shared by the three existing feature families.
// Production engines perform all copying, hashing, publication, rollback and leases.
internal static class FileTaskProgressTests {
    private static readonly byte[] Payload = CreatePayload();
    private static readonly UTF8Encoding Utf8 = new(false);

    internal static IEnumerable<(string Name, Action Run)> WorldCases() {
        yield return ("file-task helpers stream real bytes and close before terminal callbacks", ByteHelpers);
        yield return ("world file tasks keep lists nonblocking and roll back failed restores", Worlds);
        yield return ("real worker snapshot leases survive cancellation and release only their owner", Leases);
    }
    internal static IEnumerable<(string Name, Action Run)> EnvironmentCases() {
        yield return ("environment file tasks cancel shared-library staging without publication", Environment);
        yield return ("real pack transactions roll back cancellation and throwing progress callbacks", Transactions);
    }
    internal static IEnumerable<(string Name, Action Run)> CloneCases() {
        yield return ("clone file tasks stream phases and publish atomically after cancellation boundaries", Clone);
    }
    private static byte[] CreatePayload() {
        byte[] result = new byte[4 * 1024 * 1024 + 19];
        new Random(824).NextBytes(result);
        return result;
    }
    private static void Require(bool value, string text) { if (!value) throw new Exception(text); }
    private static T Throws<T>(Action call) where T : Exception { try { call(); } catch (T error) { return error; } throw new Exception("Expected " + typeof(T).Name); }
    private static void Progress(List<double> values, string name) {
        Require(values.Count > 3 && values.Any(x => x > 0 && x < 1), name + " lacks actual byte progress");
        Require(values.Last() == 1, name + " lacks terminal completion");
        for (int i = 0; i < values.Count; i++) Require(values[i] >= 0 && values[i] <= 1 && (i == 0 || values[i] >= values[i - 1]), name + " regressed at " + i);
    }
    private static string? Digest(string file) => FileTaskProgress.Digest(file, CancellationToken.None, null);
    private static void ByteHelpers() {
        using Fixture f = new(); string source = f.Bytes("source.bin", Payload), copied = f.Path("copy.bin");
        Require(FileTaskProgress.Size([source, f.Text("empty", ""), f.Path("missing")]) == Payload.LongLength + 2, "empty/missing weight changed");
        List<double> values = []; bool partial = false, finalClosed = false;
        FileTaskProgress.Copy(source, copied, CancellationToken.None, value => {
            values.Add(value);
            if (value > 0 && value < 1) { long length = new FileInfo(copied).Length; partial |= length > 0 && length < Payload.LongLength; }
            if (value == 1) { try { using FileStream probe = new(copied, FileMode.Open, FileAccess.ReadWrite, FileShare.None); finalClosed = true; } catch (IOException) { } }
        });
        Progress(values, "copy"); Require(partial && finalClosed, "copy did not report real partial bytes / final disposed handles");
        Require(Digest(copied) == "sha1:" + Convert.ToHexString(SHA1.HashData(Payload)).ToLowerInvariant(), "actual copied bytes changed");
        values.Clear(); FileTaskProgress.Digest(source, CancellationToken.None, values.Add); Progress(values, "digest");
        using CancellationTokenSource cancel = new(); string partialPath = f.Path("canceled.bin");
        Throws<OperationCanceledException>(() => FileTaskProgress.Copy(source, partialPath, cancel.Token, value => { if (value > .2) cancel.Cancel(); }));
        Require(new FileInfo(partialPath).Length < Payload.LongLength && Digest(source) == Digest(copied), "helper cancellation corrupted source");
        using (FileStream released = new(partialPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        double current = .6; Action<double> report = FileTaskProgress.ForTask(() => current, value => current = value);
        report(.2); Require(current == .6, "task progress regressed"); report(1); Require(current == .99, "workload completed loader before it stopped");
        values.Clear(); var slice = FileTaskProgress.Slice(values.Add, .2, .8)!; slice(-1); slice(2);
        Require(values.SequenceEqual([.2, .8]), "slice clamp changed");
        Require(FileTaskProgress.Digest(f.Path("absent"), cancel.Token, _ => throw new Exception("absent digest reported")) is null, "missing-file check moved after token check");
    }
    private static InstanceCloneEngine<string, InstanceCloneOptionsData, InstanceCloneResultData, InstanceCloneMigrationChangeData, InstanceCloneMigrationPlanData<InstanceCloneMigrationChangeData>> CloneEngine() {
        var host = new InstanceCloneHost<string> {
            RemoveExtendedPrefix = value => value, RelativePath = Relative, SafeName = InstanceResourceNameRules.SafeName,
            AddSlashSuffix = value => Path.TrimEndingDirectorySeparator(value) + Path.DirectorySeparatorChar,
            EnsureWithin = EnsureWithin, SamePath = Same,
            HasRunningGame = () => false, HasRunningSessions = () => false, LaunchLoading = () => false, WorldBackupBusy = () => false,
            HasActiveGame = _ => false, HasPending = _ => false, VersionPath = value => value,
            VanillaName = _ => "1.20.1", IsFool = _ => false, IsSnapshot = _ => false, HasModLoaders = _ => true, ApiType = _ => false, IsolationMode = () => 4,
            CaptureEnvironment = (_, _, _, _) => throw new Exception("Unexpected history request in clone"),
            ApplyTransaction = (_, _, _, _, _) => throw new Exception("Unexpected migration transaction in clone")
        };
        return new(new object(), host, () => new(), () => new(), () => new(), () => new());
    }
    private static string Version(Fixture f) {
        string version = f.Directory("minecraft/versions/original");
        f.Text("minecraft/versions/original/original.json", "{\"id\":\"original\",\"type\":\"release\",\"mainClass\":\"fixture.Main\",\"libraries\":[]}");
        f.Text("minecraft/versions/original/PCL/Setup.ini", "VersionArgumentIndieV2:True");
        f.Bytes("minecraft/versions/original/mods/large.jar", Payload);
        return version;
    }
    private static void Clone() {
        using Fixture f = new(); string source = Version(f), versions = Path.GetDirectoryName(source)!;
        var engine = CloneEngine(); string? before = Digest(Path.Combine(source, "mods", "large.jar"));
        List<double> values = []; bool partial = false;
        var result = engine.CloneWithProgress(source, source, "measured", null, CancellationToken.None, value => {
            values.Add(value); if (value < 1) Require(!Directory.Exists(Path.Combine(versions, "measured")), "partial clone became visible");
            if (value > .22 && value < .6) foreach (string stage in Directory.GetDirectories(versions, ".pcl-clone-*")) {
                string path = Path.Combine(stage, "mods", "large.jar"); if (File.Exists(path)) { long length = new FileInfo(path).Length; partial |= length > 0 && length < Payload.LongLength; }
            }
        });
        Progress(values, "clone"); Require(partial && values.Any(x => x > .02 && x < .22) && values.Any(x => x > .85 && x < .99), "copy/hash/final validation did not stream");
        Require(before == Digest(Path.Combine(result.VersionFolder!, "mods", "large.jar")), "clone changed actual payload");
        int phase = 0;
        foreach (double threshold in new[] { .06, .35, .98 }) {
            using CancellationTokenSource cancel = new(); values.Clear(); string name = "canceled-" + phase++;
            Throws<OperationCanceledException>(() => engine.CloneWithProgress(source, source, name, null, cancel.Token, value => { values.Add(value); if (value >= threshold) cancel.Cancel(); }));
            Require(!values.Contains(1) && !Directory.Exists(Path.Combine(versions, name)) && Directory.GetDirectories(versions, ".pcl-clone-*").Length == 0, "clone cancellation published or leaked staging");
            Require(before == Digest(Path.Combine(source, "mods", "large.jar")), "clone cancellation changed original");
        }
    }
    private static WorldBackupEngine<string> WorldEngine(Fixture f) {
        var address = new LocalGameDataStore(() => f.Path("profile"), EnsureWithin);
        return new(new WorldBackupHost<string> {
            RemoveExtendedPrefix = value => value, ProfileAddress = address.Address,
            GameFolder = value => value, VersionPath = value => value, Retention = _ => 5, AutoEnabled = _ => true,
            Watchers = () => Array.Empty<WorldBackupWatcher<string>?>(), WorldLabel = _ => null, FormatSize = n => n.ToString(),
            Warn = (_, _) => { }, UiBusy = () => false, Dispatch = action => action(), NotifyChanged = () => { }, OpenFolder = _ => { }, Hint = (_, _) => { }
        });
    }
    private static void Nonblocking(Action<Action<double>> write, Func<int> read, int expected) {
        using ManualResetEventSlim entered = new(false), release = new(false);
        int paused = 0;
        Task writer = Task.Run(() => write(value => { if (value > .1 && Interlocked.Exchange(ref paused, 1) == 0) { entered.Set(); if (!release.Wait(5000)) throw new Exception("fixture release timed out"); } }));
        try {
            Require(entered.Wait(5000), "writer did not enter actual I/O");
            Task<int> reader = Task.Run(read); Require(reader.Wait(1000), "published list blocked on snapshot Gate/lease"); Require(reader.Result == expected, "reader saw unpublished point");
        } finally { release.Set(); Require(writer.Wait(5000), "snapshot writer failed to stop"); }
    }
    private static void Worlds() {
        using Fixture f = new(); string game = f.Directory("game");
        f.Text("game/saves/World/level.dat", "fixture"); string data = f.Bytes("game/saves/World/data.bin", Payload);
        var engine = WorldEngine(f); List<double> values = [];
        string id = engine.CaptureWorldWithProgress(game, "World", "manual", 5, CancellationToken.None, values.Add); Progress(values, "world capture");
        Nonblocking(progress => engine.CaptureWorldWithProgress(game, "World", "manual", 5, CancellationToken.None, progress), () => engine.Points(game).Count, 1);
        int points = engine.Points(game).Count;
        using (CancellationTokenSource cancel = new()) {
            Throws<OperationCanceledException>(() => engine.CaptureWorldWithProgress(game, "World", "manual", 5, cancel.Token, value => { if (value > .1) cancel.Cancel(); }));
            Require(points == engine.Points(game).Count && Directory.GetDirectories(engine.Store(game), ".pcl-backup-*").Length == 0, "canceled world snapshot published/leaked staging");
        }
        File.WriteAllText(data, "new world", Utf8); string? current = Digest(data); values.Clear();
        Throws<IOException>(() => engine.RestoreWithProgress(game, id, 5, CancellationToken.None, values.Add, phase => { if (phase == 1) throw new IOException("commit fault"); }));
        Require(current == Digest(data) && !engine.HasPendingRecovery(game), "world commit failure did not restore original tree");
        Require(!values.Contains(1), "failed world restore reported complete");
        values.Clear(); engine.RestoreWithProgress(game, id, 5, CancellationToken.None, values.Add);
        Progress(values, "world restore"); Require(File.ReadAllBytes(data).SequenceEqual(Payload), "world restore bytes differ");
        f.Directory("game/saves/not-world"); f.Directory("game/saves/.pcl-pending"); values.Clear();
        engine.CaptureFolderWithProgress(game, "all", 5, CancellationToken.None, values.Add);
        Progress(values, "all worlds"); Require(values.Any(x => x > .5 && x < .65), "non-world folders distorted weight");
    }
    private sealed class PackFile : PackUpdateFileData { }
    private sealed class Pack : PackUpdatePackageData<PackFile> { }
    private sealed class PackChange : PackUpdateChangeData<PackFile> { }
    private sealed class PackPlan : PackUpdatePlanData<Pack, PackChange> { }
    private static PackUpdateEngine<PackFile, Pack, PackChange, PackPlan, object> PackEngine() => new(new() {
        EnsureWithin = EnsureWithin, AtomicWrite = (path, bytes) => LocalGameDataStore.AtomicWrite(path, bytes, null, false)
    });
    private static EnvironmentHistoryEngine<string> History(Fixture f, PackUpdateEngine<PackFile, Pack, PackChange, PackPlan, object> pack) => new(new object(), new() {
        DataRoot = () => f.Path("profile"), Hash = LocalGameDataStore.Hash, AtomicWrite = (path, bytes) => LocalGameDataStore.AtomicWrite(path, bytes, null, false),
        Target = (root, relative) => { string path = Path.Combine(root, Relative(relative).Replace('/', Path.DirectorySeparatorChar)); EnsureWithin(root, path); return path; },
        RelativePath = Relative, AddSlashSuffix = path => Path.TrimEndingDirectorySeparator(path) + Path.DirectorySeparatorChar,
        RemoveExtendedPrefix = path => path, SafeName = InstanceResourceNameRules.SafeName, Digest = Digest,
        EnsureNoLinks = path => EnsureWithin(f.Root, path), EnsureWithin = EnsureWithin, IsParentOf = IsParent, SamePath = Same,
        VersionPath = value => value, GameFolder = value => value, VanillaName = _ => "1.20.1", LibraryPaths = _ => Array.Empty<string>(), ModLoaderVersions = _ => Array.Empty<string>(),
        JsonObject = _ => new JObject(new JProperty("id", "original")), AutoCleanup = _ => false, Retention = _ => 5,
        ApplyTransaction = (g, v, ops, t, before) => pack.ApplyTransaction(g, v, ops, t, before),
        ApplyTransactionWithProgress = (g, v, ops, t, progress, before) => pack.ApplyTransactionWithProgress(g, v, ops, t, progress, before),
        Info = _ => { }, OpenFolder = _ => { }, Hint = _ => { }, Warn = (_, _) => { }
    });
    private static void Environment() {
        using Fixture f = new(); string version = Version(f), mc = Path.GetDirectoryName(Path.GetDirectoryName(version)!)!;
        var engine = History(f, PackEngine());
        string shared = f.Bytes("minecraft/libraries/fixture/library.jar", Payload);
        Dictionary<string, string> dependencies = new() { ["libraries/fixture/library.jar"] = shared };
        List<double> values = [];
        string id = engine.CaptureWithProgress(version, version, "manual", CancellationToken.None, values.Add, dependencies);
        Progress(values, "environment capture");
        Nonblocking(progress => engine.CaptureWithProgress(version, version, "manual", CancellationToken.None, progress), () => engine.Points(version).Count, 1);
        int points = engine.Points(version).Count;
        using (CancellationTokenSource cancel = new()) {
            Throws<OperationCanceledException>(() => engine.CaptureWithProgress(version, version, "manual", cancel.Token, value => { if (value > .1) cancel.Cancel(); }));
            Require(engine.Points(version).Count == points && Directory.GetFiles(engine.Store(version), "*.tmp-*", SearchOption.AllDirectories).Length == 0, "canceled environment snapshot published/leaked staging");
        }
        File.Delete(shared); string current = f.Text("minecraft/versions/original/mods/large.jar", "current environment"); string? before = Digest(current);
        using (CancellationTokenSource cancel = new()) {
            values.Clear();
            Throws<OperationCanceledException>(() => engine.RestoreWithProgress(version, version, mc, id, cancel.Token, value => { values.Add(value); if (value > .621 && value < .65) cancel.Cancel(); }));
            Require(!File.Exists(shared) && !values.Contains(1) && Digest(current) == before, "canceled shared library restore published partial content");
            Require(Directory.GetFiles(Path.GetDirectoryName(shared)!, "*.pcl-restore-*").Length == 0 && engine.RestoringCount == 0, "shared library staging/busy leaked");
        }
        values.Clear(); engine.RestoreWithProgress(version, version, mc, id, CancellationToken.None, values.Add);
        Progress(values, "environment restore"); Require(File.ReadAllBytes(shared).SequenceEqual(Payload) && File.ReadAllBytes(current).SequenceEqual(Payload), "environment actual transaction restored wrong bytes");
        Require(engine.RestoringCount == 0, "environment restore retained busy state");
    }
    private static void Transactions() {
        using Fixture f = new(); string game = f.Directory("game"), version = f.Directory("game/versions/fixture");
        var engine = PackEngine(); string a = f.Bytes("game/config/a.toml", Payload), b = f.Bytes("game/config/b.toml", Payload);
        byte[] incoming = (byte[])Payload.Clone(); incoming[17] ^= 1;
        string input = f.Bytes("incoming.bin", incoming); string? original = Digest(a);
        List<Tuple<string, string?, string?>> ops = [Tuple.Create(a, (string?)input, original), Tuple.Create(b, (string?)input, original)];
        using (CancellationTokenSource cancel = new()) {
            Throws<OperationCanceledException>(() => engine.ApplyTransactionWithProgress(game, version, ops, cancel.Token, null, index => { if (index == 1) cancel.Cancel(); }));
            Require(Digest(a) == original && Digest(b) == original && engine.Pending(version).Count == 0, "mid-transaction cancellation skipped uncancelable rollback");
        }
        Throws<IOException>(() => engine.ApplyTransactionWithProgress(game, version, ops, CancellationToken.None, value => { if (value > .6 && value < .7) throw new IOException("progress consumer fault"); }));
        Require(Digest(a) == original && Digest(b) == original && Directory.GetFiles(game, "*.pcl-update-*", SearchOption.AllDirectories).Length == 0, "callback failure leaked temporary/mutated files");
        List<double> values = []; engine.ApplyTransactionWithProgress(game, version, ops, CancellationToken.None, values.Add);
        Progress(values, "pack transaction"); Require(Digest(a) == Digest(input) && Digest(b) == Digest(input), "real transaction wrote wrong payload");
    }
    private static void Leases() {
        using Fixture f = new(); string path = f.Directory("version");
        var leases = new ConcurrentDictionary<string, LegacyMinecraftInstanceLease>(StringComparer.OrdinalIgnoreCase);
        var locks = new LegacyMinecraftInstanceLocks(leases);
        using ManualResetEventSlim entered = new(false), release = new(false), stopped = new(false);
        var worker = new RedoableWorker((Action)(() => { entered.Set(); if (!release.Wait(5000)) throw new Exception("lease fixture release timed out"); }));
        Require(locks.TryAcquireSnapshot(worker, path), "snapshot reservation refused"); worker.Stopped += () => stopped.Set(); worker.Start();
        try {
            Require(entered.Wait(5000) && locks.IsLocked(path) && !locks.BlocksNavigation(path), "snapshot lock does not permit browsing");
            Require(!locks.TryAcquire(new RedoableWorker(() => { }), path), "snapshot admitted conflicting writer");
            worker.Cancel(); Require(locks.IsLocked(path), "Cancel released lock before real workload/finally ended");
        } finally { release.Set(); Require(stopped.Wait(5000), "snapshot worker did not stop"); }
        Require(!locks.IsLocked(path), "stopped snapshot retained lease");
        using ManualResetEventSlim writeStopped = new(false);
        var write = new RedoableWorker(() => { }); Require(locks.TryAcquire(write, path) && locks.BlocksNavigation(path), "write lock did not block navigation"); write.Stopped += () => writeStopped.Set(); write.Start();
        Require(writeStopped.Wait(5000) && !locks.IsLocked(path), "write lock not released");
        using ManualResetEventSlim oldEntered = new(false), oldRelease = new(false), oldStopped = new(false), nextStopped = new(false);
        var old = new RedoableWorker(() => { oldEntered.Set(); if (!oldRelease.Wait(5000)) throw new Exception("old lease release timed out"); });
        Require(locks.TryAcquireSnapshot(old, path), "old reservation unavailable"); old.Stopped += () => oldStopped.Set(); old.Start();
        RedoableWorker? next = null;
        try {
            Require(oldEntered.Wait(5000), "old worker did not enter"); Require(leases.TryRemove(PathUtils.ForCompare(path), out _), "fixture lease handoff failed");
            next = new RedoableWorker(() => { }); Require(locks.TryAcquire(next, path), "replacement lease refused");
            oldRelease.Set(); Require(oldStopped.Wait(5000), "old worker did not stop");
            Require(locks.IsLocked(path) && locks.BlocksNavigation(path), "late old Stopped removed newer lease");
        } finally {
            oldRelease.Set(); Require(oldStopped.Wait(5000), "old worker cleanup timed out");
            if (next != null) { next.Stopped += () => nextStopped.Set(); next.Start(); Require(nextStopped.Wait(5000), "replacement worker cleanup timed out"); }
        }
        Require(!locks.IsAnyLocked, "fixture retained locks");
    }
    private static bool Same(string first, string second) => string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(first)), Path.TrimEndingDirectorySeparator(Path.GetFullPath(second)), OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    private static bool IsParent(string root, string path) => Path.GetFullPath(path).StartsWith(Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)) + Path.DirectorySeparatorChar, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    private static void EnsureWithin(string root, string path) { if (!Same(root, path) && !IsParent(root, path)) throw new IOException("Fixture escaped root"); }
    private static string Relative(string value) { string normalized = value.Replace('\\', '/'); if (string.IsNullOrWhiteSpace(value) || Path.IsPathRooted(value) || normalized.Split('/').Any(p => p is "" or "." or "..")) throw new IOException("Invalid fixture relative path"); return normalized; }
    private sealed class Fixture : IDisposable {
        public string Root { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "PCLLF-FileTasks-" + Guid.NewGuid().ToString("N"));
        public Fixture() => System.IO.Directory.CreateDirectory(Root);
        public string Path(string value) { string result = System.IO.Path.Combine(Root, value); EnsureWithin(Root, result); return result; }
        public string Directory(string value) { string result = Path(value); System.IO.Directory.CreateDirectory(result); return result; }
        public string Bytes(string name, byte[] bytes) { string path = Path(name); System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!); File.WriteAllBytes(path, bytes); return path; }
        public string Text(string name, string value) => Bytes(name, Utf8.GetBytes(value));
        public void Dispose() {
            if (!Regex.IsMatch(System.IO.Path.GetFileName(Root), @"\APCLLF-FileTasks-[a-f0-9]{32}\z") || !IsParent(System.IO.Path.GetTempPath(), Root)) throw new Exception("Unsafe fixture cleanup");
            if (System.IO.Directory.Exists(Root)) System.IO.Directory.Delete(Root, true);
        }
    }
}
