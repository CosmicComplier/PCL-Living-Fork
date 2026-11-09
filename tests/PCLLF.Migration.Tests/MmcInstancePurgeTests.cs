using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using PCLCS;
using PCLLF.MMCC.Core;

// Original LF host regressions. Every writable path belongs to this process's GUID tiny fixture.
internal static class MmcInstancePurgeTests {
    internal static IEnumerable<(string Name, Action Run)> Cases() {
        yield return ("MMC explicit purge removes one completed private quarantine and preserves shared/source/Java bytes", Complete);
        yield return ("MMC purge refuses wrong owner, stale generation, primary, confirmation and ordinary-delete intent", IdentityAndIntent);
        yield return ("MMC purge preview covers changed files, replaced files and new empty directories", StalePreview);
        yield return ("MMC purge refuses pending transactions, unknown sessions and prepared producers", PendingWork);
        yield return ("MMC purge cancellation before its durable decision leaves all player bytes restorable", EarlyCancellation);
        yield return ("MMC catalog recovery withdraws a valid undecided purge preparation and reenables quarantine cleanup", CatalogPreparationRecovery);
        yield return ("MMC purge ignores late cancellation and forward-recovers after primary/marker removal", DecidedRecovery);
        yield return ("MMC purge preserves newly added files, empty directories, foreign primaries and links after decision", ForeignContent);
        yield return ("MMC purge keeps remaining entries on a real writer/readonly failure and safely resumes", LockedFiles);
        yield return ("MMC purge verifies and deletes the same native handle and never sweeps a new empty directory", SameHandleAndNewDirectory);
        yield return ("MMC purge decisions disable every restore path even when their plan becomes damaged", DamagedDecision);
        yield return ("MMC killed purge producers retain exact predecision/no-delete and postdecision/forward recovery", KilledProducer);
    }
    private static MmcInstancePurgeRequest Confirm(MmcInstancePurgePreview preview) =>
        new(preview, preview.SnapshotFingerprint, MmcInstancePurgeIntent.PermanentlyDeleteQuarantinedInstance);
    private static void Complete() {
        using Fixture fixture = new(); var deleted = fixture.Quarantine(); string root = deleted.CurrentRoot!;
        Equal(true, deleted.Quarantined); Equal(true, Directory.Exists(root)); Equal(false, Directory.Exists(fixture.Source));
        var preview = fixture.Preview(deleted);
        Equal(true, preview.Entries.Any(entry => entry.RelativePath == "minecraft/saves/world/level.dat" && entry.Role == MmcInstancePurgeRole.Game));
        Equal(true, preview.Entries.Any(entry => entry.RelativePath == "minecraft/empty" && entry.Directory));
        Equal(true, preview.Entries.Any(entry => entry.Role == MmcInstancePurgeRole.SavedSource));
        Equal(true, preview.Entries.Any(entry => entry.RelativePath == "PCL/InstanceDefinition.json"));
        Equal(true, preview.Entries.Any(entry => entry.RelativePath == "PCL/MmcLifecyclePending.json"));
        var result = fixture.Backend.PurgeAsync(Confirm(preview)).GetAwaiter().GetResult(); Succeeded(result);
        Equal(true, result.DecisionRecorded); Equal(true, result.Completed); Equal(0, result.RemainingPaths.Length);
        Equal(false, Directory.Exists(root)); Equal(false, Directory.Exists(fixture.Source)); fixture.Preserved();
        var entry = MmcInstanceLifecycleCatalog.Read(fixture.Minecraft).Entries.Single();
        Equal(MmcLifecycleCatalogState.Purged, entry.State); Equal(false, entry.CanRestore); Equal(false, entry.CanPurge);
        var recovered = fixture.Backend.RecoverAsync(deleted.OperationDirectory!).GetAwaiter().GetResult();
        Equal(MmcInstanceBackendStatus.Succeeded, recovered.Status); Equal(true, recovered.Purged); Equal(false, recovered.Quarantined);
        var restore = fixture.Backend.RestoreDeletedAsync(deleted.OperationDirectory!).GetAwaiter().GetResult();
        Equal(true, restore.Purged); Equal(false, Directory.Exists(fixture.Source));
    }
    private static void IdentityAndIntent() {
        using Fixture fixture = new(); var deleted = fixture.Quarantine(); var preview = fixture.Preview(deleted); string before = HashTree(deleted.CurrentRoot!);
        foreach (var request in new[] {
            new MmcInstancePurgePreviewRequest(deleted.OperationDirectory!, Guid.NewGuid(), 1, preview.PrimarySha256),
            new MmcInstancePurgePreviewRequest(deleted.OperationDirectory!, preview.InstanceId, 2, preview.PrimarySha256),
            new MmcInstancePurgePreviewRequest(deleted.OperationDirectory!, preview.InstanceId, 1, new string('a', 64)) })
            Denied(() => fixture.Backend.PreviewPurgeAsync(request).GetAwaiter().GetResult());
        Equal(MmcInstanceBackendStatus.Rejected, fixture.Backend.PurgeAsync(Confirm(preview) with { ConfirmedPreviewFingerprint = new string('b', 64) }).GetAwaiter().GetResult().Status);
        Equal(MmcInstanceBackendStatus.Rejected, fixture.Backend.PurgeAsync(Confirm(preview) with { Intent = 0 }).GetAwaiter().GetResult().Status);
        Equal(before, HashTree(deleted.CurrentRoot!)); Equal(false, File.Exists(Path.Combine(deleted.OperationDirectory!, "purge-plan.record")));
        // A foreign quarantine-looking directory is never sufficient ownership evidence.
        string foreign = Path.Combine(Path.GetDirectoryName(fixture.Source)!, ".pcllf-mmc-quarantine-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(foreign); File.WriteAllText(Path.Combine(foreign, "foreign.txt"), "keep foreign");
        Equal(false, MmcInstanceLifecycleVisibility.IsCompletedQuarantine(foreign)); Equal("keep foreign", File.ReadAllText(Path.Combine(foreign, "foreign.txt")));
    }
    private static void StalePreview() {
        foreach (string mode in new[] { "file", "empty-directory", "same-bytes-replacement" }) {
            using Fixture fixture = new(); var deleted = fixture.Quarantine(); var preview = fixture.Preview(deleted); string root = deleted.CurrentRoot!;
            string path = Path.Combine(root, "minecraft", "saves", "world", "level.dat");
            if (mode == "file") File.WriteAllText(path, "new player bytes");
            if (mode == "empty-directory") Directory.CreateDirectory(Path.Combine(root, "minecraft", "new-empty"));
            if (mode == "same-bytes-replacement") { string next = path + ".next"; File.Copy(path, next); File.Move(next, path, true); }
            string before = HashTree(root);
            var result = fixture.Backend.PurgeAsync(Confirm(preview)).GetAwaiter().GetResult();
            Equal(MmcInstanceBackendStatus.Rejected, result.Status); Equal(false, result.DecisionRecorded); Equal(before, HashTree(root));
            Equal(false, File.Exists(Path.Combine(deleted.OperationDirectory!, "PurgeCommitDecided.record")));
        }
    }
    private static void PendingWork() {
        foreach (string relative in new[] { "PCL/MmcTransactions/unknown/pending.txt", "PCL/MmcSessions/unknown/session.txt", "PCL/MmcRuntime/.staging/unknown/producer.txt", "PCL/MmcImportPending.json" }) {
            using Fixture fixture = new(); var deleted = fixture.Quarantine(); string path = Path.Combine(deleted.CurrentRoot!, relative.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, "unresolved work"); string before = HashTree(deleted.CurrentRoot!);
            Denied(() => fixture.Preview(deleted)); Equal(before, HashTree(deleted.CurrentRoot!));
            Equal(false, File.Exists(Path.Combine(deleted.OperationDirectory!, "purge-plan.record")));
        }
    }
    private static void EarlyCancellation() {
        foreach (var phase in new[] { MmcInstancePurgeCheckpoint.PlanPrepared, MmcInstancePurgeCheckpoint.BeforeDecision }) {
            using Fixture fixture = new(); var deleted = fixture.Quarantine(); var preview = fixture.Preview(deleted); string before = HashTree(deleted.CurrentRoot!);
            using CancellationTokenSource cancellation = new();
            var result = fixture.Backend.PurgeAsync(Confirm(preview) with { Checkpoint = (current, _) => { if (current == phase) cancellation.Cancel(); } }, cancellation.Token).GetAwaiter().GetResult();
            Equal(MmcInstanceBackendStatus.Cancelled, result.Status); Equal(false, result.DecisionRecorded); Equal(before, HashTree(deleted.CurrentRoot!));
            Equal(false, File.Exists(Path.Combine(deleted.OperationDirectory!, "purge-plan.record")));
            Equal(true, MmcInstanceLifecycleCatalog.Read(fixture.Minecraft).Entries.Single().CanRestore);
            Equal(MmcInstanceBackendStatus.Succeeded, fixture.Backend.RestoreDeletedAsync(deleted.OperationDirectory!).GetAwaiter().GetResult().Status);
            Equal("world bytes", File.ReadAllText(Path.Combine(fixture.Source, "minecraft", "saves", "world", "level.dat")));
        }
    }
    private static void CatalogPreparationRecovery() {
        using Fixture fixture = new(); var deleted = fixture.Quarantine(); var preview = fixture.Preview(deleted); string before = HashTree(deleted.CurrentRoot!);
        var prepared = fixture.Backend.PurgeAsync(Confirm(preview) with { Checkpoint = (phase, _) => {
            if (phase == MmcInstancePurgeCheckpoint.PlanPrepared) throw new MmcLifecycleInterruptionException("owned stop before irreversible decision");
        } }).GetAwaiter().GetResult();
        Equal(MmcInstanceBackendStatus.NeedsRecovery, prepared.Status); Equal(false, prepared.DecisionRecorded); Equal(before, HashTree(deleted.CurrentRoot!));
        var catalog = MmcInstanceLifecycleCatalog.Read(fixture.Minecraft).Entries.Single();
        Equal(MmcLifecycleCatalogState.Quarantined, catalog.State); Equal(true, catalog.CanRecover); Equal(false, catalog.CanPurge);
        var recovered = MmcInstanceLifecycleCatalog.Execute(catalog, false);
        Equal(MmcInstanceBackendStatus.Succeeded, recovered.Status); Equal(true, recovered.Quarantined); Equal(false, recovered.PurgeDecided);
        Equal(before, HashTree(deleted.CurrentRoot!)); Equal(false, File.Exists(Path.Combine(deleted.OperationDirectory!, MmcInstancePurgeJournal.PlanName)));
        var after = MmcInstanceLifecycleCatalog.Read(fixture.Minecraft).Entries.Single(); Equal(true, after.CanPurge); Equal(false, after.CanRecover);
        Denied(() => MmcInstanceLifecycleCatalog.Execute(catalog, false));
    }
    private static void DecidedRecovery() {
        using (Fixture fixture = new()) {
            var deleted = fixture.Quarantine(); var preview = fixture.Preview(deleted); using CancellationTokenSource cancellation = new();
            var result = fixture.Backend.PurgeAsync(Confirm(preview) with { Checkpoint = (phase, _) => { if (phase == MmcInstancePurgeCheckpoint.CommitDecided) cancellation.Cancel(); } }, cancellation.Token).GetAwaiter().GetResult();
            Succeeded(result); Equal(true, cancellation.IsCancellationRequested);
        }
        foreach (string stop in new[] { "PCL/InstanceDefinition.json", "PCL/MmcLifecyclePending.json" }) {
            using Fixture fixture = new(); var deleted = fixture.Quarantine(); var preview = fixture.Preview(deleted);
            var interrupted = fixture.Backend.PurgeAsync(Confirm(preview) with { Checkpoint = (phase, relative) => {
                if (phase == MmcInstancePurgeCheckpoint.EntryDeleted && relative == stop) throw new MmcLifecycleInterruptionException("owned purge interruption");
            } }).GetAwaiter().GetResult();
            Equal(MmcInstanceBackendStatus.NeedsRecovery, interrupted.Status); Equal(true, interrupted.DecisionRecorded);
            Equal(false, File.Exists(Path.Combine(deleted.CurrentRoot!, stop.Replace('/', Path.DirectorySeparatorChar))));
            var catalog = MmcInstanceLifecycleCatalog.Read(fixture.Minecraft).Entries.Single();
            Equal(MmcLifecycleCatalogState.Purging, catalog.State); Equal(false, catalog.CanRestore); Equal(true, catalog.CanRecover);
            // Reuse of the original display name must not cause deletion of that new instance.
            Directory.CreateDirectory(fixture.Source); File.WriteAllText(Path.Combine(fixture.Source, "foreign.txt"), "new owner at old name");
            var restored = fixture.Backend.RestoreDeletedAsync(deleted.OperationDirectory!).GetAwaiter().GetResult();
            Equal(true, restored.Purged); Equal(false, restored.Quarantined); Equal("new owner at old name", File.ReadAllText(Path.Combine(fixture.Source, "foreign.txt")));
            Equal(false, Directory.Exists(deleted.CurrentRoot!)); fixture.Preserved();
        }
    }
    private static void ForeignContent() {
        foreach (string mode in new[] { "file", "empty-directory", "foreign-primary", "link" }) {
            using Fixture fixture = new(); using Fixture other = new(); var deleted = fixture.Quarantine(); var preview = fixture.Preview(deleted);
            string root = deleted.CurrentRoot!, extra = Path.Combine(root, "minecraft", "unexpected"); byte[]? original = null;
            var result = fixture.Backend.PurgeAsync(Confirm(preview) with { Checkpoint = (phase, _) => {
                if (phase != MmcInstancePurgeCheckpoint.CommitDecided) return;
                if (mode == "file") File.WriteAllText(extra, "foreign bytes");
                if (mode == "empty-directory") Directory.CreateDirectory(extra);
                if (mode == "link") Junction(extra, Path.Combine(fixture.Root, "preserved"));
                if (mode == "foreign-primary") {
                    other.Import(); extra = Path.Combine(root, "PCL", "InstanceDefinition.json"); original = File.ReadAllBytes(extra);
                    File.WriteAllBytes(extra, File.ReadAllBytes(Path.Combine(other.Source, "PCL", "InstanceDefinition.json")));
                }
            } }).GetAwaiter().GetResult();
            Equal(MmcInstanceBackendStatus.NeedsRecovery, result.Status); Equal(true, result.DecisionRecorded); Equal(false, result.Completed);
            Equal(true, result.RemainingPaths.Contains(Path.GetRelativePath(root, extra).Replace('\\', '/'))); Equal(true, File.Exists(Path.Combine(root, "minecraft", "saves", "world", "level.dat")));
            Equal(false, MmcInstanceLifecycleCatalog.Read(fixture.Minecraft).Entries.Single().CanRestore); fixture.Preserved();
            if (mode == "file") { Equal("foreign bytes", File.ReadAllText(extra)); File.Delete(extra); }
            if (mode is "empty-directory" or "link") Directory.Delete(extra, false);
            if (mode == "foreign-primary") File.WriteAllBytes(extra, original!);
            Succeeded(fixture.Backend.RecoverPurgeAsync(deleted.OperationDirectory!).GetAwaiter().GetResult()); fixture.Preserved();
        }
    }
    private static void LockedFiles() {
        foreach (string mode in new[] { "writer", "readonly" }) {
            using Fixture fixture = new(); var deleted = fixture.Quarantine(); var preview = fixture.Preview(deleted); FileStream? writer = null;
            string path = Path.Combine(deleted.CurrentRoot!, "minecraft", "saves", "world", "level.dat");
            try {
                var result = fixture.Backend.PurgeAsync(Confirm(preview) with { Checkpoint = (phase, _) => {
                    if (phase != MmcInstancePurgeCheckpoint.CommitDecided) return;
                    if (mode == "writer") writer = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete);
                    else File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.ReadOnly);
                } }).GetAwaiter().GetResult();
                Equal(MmcInstanceBackendStatus.NeedsRecovery, result.Status); Equal(true, result.DecisionRecorded);
                Equal(true, result.RemainingPaths.Contains("minecraft/saves/world/level.dat")); Equal("world bytes", File.ReadAllText(path));
                Equal(false, MmcInstanceLifecycleCatalog.Read(fixture.Minecraft).Entries.Single().CanRestore);
            } finally { writer?.Dispose(); if (File.Exists(path)) File.SetAttributes(path, FileAttributes.Normal); }
            Succeeded(fixture.Backend.RecoverPurgeAsync(deleted.OperationDirectory!).GetAwaiter().GetResult());
        }
    }
    private static void SameHandleAndNewDirectory() {
        using Fixture fixture = new(); var deleted = fixture.Quarantine(); var preview = fixture.Preview(deleted); bool denied = false, injected = false;
        string root = deleted.CurrentRoot!, late = Path.Combine(root, "minecraft", "late-empty");
        var result = fixture.Backend.PurgeAsync(Confirm(preview) with { Checkpoint = (phase, relative) => {
            if (phase != MmcInstancePurgeCheckpoint.BeforeEntryDelete || relative != "minecraft/saves/world/level.dat") return;
            string path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
            Denied(() => File.WriteAllText(path, "must not replace pinned bytes")); denied = true;
            Denied(() => File.Move(path, path + ".foreign")); Directory.CreateDirectory(late); injected = true;
        } }).GetAwaiter().GetResult();
        Equal(true, denied); Equal(true, injected); Equal(MmcInstanceBackendStatus.NeedsRecovery, result.Status);
        Equal(false, result.Completed); Equal(true, Directory.Exists(late)); Equal(true, result.RemainingPaths.Contains("minecraft/late-empty"));
        Equal(false, File.Exists(Path.Combine(deleted.OperationDirectory!, "purged.record")));
        Directory.Delete(late, false); Succeeded(fixture.Backend.RecoverPurgeAsync(deleted.OperationDirectory!).GetAwaiter().GetResult());
    }
    private static void DamagedDecision() {
        using Fixture fixture = new(); var deleted = fixture.Quarantine(); var preview = fixture.Preview(deleted); string before = HashTree(deleted.CurrentRoot!);
        var result = fixture.Backend.PurgeAsync(Confirm(preview) with { Checkpoint = (phase, _) => {
            if (phase == MmcInstancePurgeCheckpoint.CommitDecided) throw new MmcLifecycleInterruptionException("owned stop before first deletion");
        } }).GetAwaiter().GetResult();
        Equal(true, result.DecisionRecorded); File.AppendAllText(Path.Combine(deleted.OperationDirectory!, "purge-plan.record"), "damaged");
        var entry = MmcInstanceLifecycleCatalog.Read(fixture.Minecraft).Entries.Single(); Equal(false, entry.CanRestore); Equal(false, entry.CanRecover);
        Equal(MmcInstanceBackendStatus.NeedsRecovery, fixture.Backend.RestoreDeletedAsync(deleted.OperationDirectory!).GetAwaiter().GetResult().Status);
        Equal(before, HashTree(deleted.CurrentRoot!)); Equal(false, Directory.Exists(fixture.Source));
    }
    internal static bool TryRunChild(string[] args, out int code) {
        if (MmcPurgeTxFPrimitiveTests.TryRunChild(args, out code)) return true;
        code = 0; if (args.Length == 0 || args[0] != "--mmc-purge-child") return false;
        try {
            if (args.Length != 8) throw new IOException("Bad owned purge child arguments.");
            string root = Path.GetFullPath(args[1]); string leaf = Path.GetFileName(root);
            if (!leaf.StartsWith("pcllf-mmc-purge-", StringComparison.Ordinal) || !Guid.TryParseExact(leaf[16..], "N", out _) ||
                !Path.GetFullPath(args[2]).StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || Path.GetDirectoryName(args[7]) != root)
                throw new IOException("Purge child is outside its parent-created GUID fixture.");
            var backend = new MmcInstanceLifecycleBackend(); var preview = backend.PreviewPurgeAsync(new(args[2], Guid.Parse(args[3]), long.Parse(args[4]), args[5])).GetAwaiter().GetResult();
            var result = backend.PurgeAsync(Confirm(preview) with { Checkpoint = (phase, relative) => {
                if (args[6] == "prepared" && phase == MmcInstancePurgeCheckpoint.PlanPrepared ||
                    args[6] == "decided" && phase == MmcInstancePurgeCheckpoint.CommitDecided ||
                    args[6] == "primary-removed" && phase == MmcInstancePurgeCheckpoint.EntryDeleted && relative == "PCL/InstanceDefinition.json") {
                    File.WriteAllText(args[7], args[2]); using ManualResetEventSlim hold = new(); hold.Wait();
                }
            } }).GetAwaiter().GetResult(); Console.Error.WriteLine(result.Failure); code = 95;
        } catch (Exception error) { Console.Error.WriteLine(error); code = 96; }
        return true;
    }
    private static void KilledProducer() {
        foreach (string phase in new[] { "prepared", "decided", "primary-removed" }) {
            using Fixture fixture = new(); var deleted = fixture.Quarantine(); var preview = fixture.Preview(deleted); string ready = Path.Combine(fixture.Root, "ready.txt"); string before = HashTree(deleted.CurrentRoot!);
            var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (string value in new[] { "--mmc-purge-child", fixture.Root, deleted.OperationDirectory!, preview.InstanceId.ToString("D"), "1", preview.PrimarySha256, phase, ready }) start.ArgumentList.Add(value);
            using var child = Process.Start(start) ?? throw new IOException("Owned purge child did not start."); var output = child.StandardOutput.ReadToEndAsync(); var errors = child.StandardError.ReadToEndAsync();
            try {
                var timer = Stopwatch.StartNew();
                while (!File.Exists(ready)) { if (child.HasExited) throw new IOException("Owned purge child exited: " + errors.GetAwaiter().GetResult()); if (timer.ElapsedMilliseconds > 20000) throw new TimeoutException("Owned purge child did not reach its exact checkpoint."); Thread.Sleep(2); }
                Equal(MmcInstanceBackendStatus.NeedsRecovery, fixture.Backend.RecoverAsync(deleted.OperationDirectory!).GetAwaiter().GetResult().Status);
                child.Kill(true); if (!child.WaitForExit(5000)) throw new IOException("Owned purge child did not exit.");
                MmcInstanceLifecycleResult recovered;
                if (phase == "prepared") {
                    var entry = MmcInstanceLifecycleCatalog.Read(fixture.Minecraft).Entries.Single(); Equal(true, entry.CanRecover); Equal(false, entry.CanPurge);
                    recovered = MmcInstanceLifecycleCatalog.Execute(entry, false);
                    Equal(true, MmcInstanceLifecycleCatalog.Read(fixture.Minecraft).Entries.Single().CanPurge);
                } else recovered = fixture.Backend.RecoverAsync(deleted.OperationDirectory!).GetAwaiter().GetResult();
                Equal(MmcInstanceBackendStatus.Succeeded, recovered.Status);
                if (phase == "prepared") { Equal(true, recovered.Quarantined); Equal(false, recovered.PurgeDecided); Equal(before, HashTree(deleted.CurrentRoot!)); }
                else { Equal(true, recovered.Purged); Equal(false, recovered.Quarantined); Equal(false, Directory.Exists(deleted.CurrentRoot!)); }
                fixture.Preserved();
            } finally { if (!child.HasExited) { child.Kill(true); child.WaitForExit(5000); } }
        }
    }
    private static void Junction(string link, string target) {
        var start = new ProcessStartInfo("cmd.exe") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add("/d"); start.ArgumentList.Add("/c"); start.ArgumentList.Add("mklink"); start.ArgumentList.Add("/J"); start.ArgumentList.Add(link); start.ArgumentList.Add(target);
        using var process = Process.Start(start) ?? throw new IOException("Owned fixture junction did not start."); string output = process.StandardOutput.ReadToEnd(), error = process.StandardError.ReadToEnd();
        if (!process.WaitForExit(5000) || process.ExitCode != 0) throw new IOException("Owned fixture junction failed: " + output + error);
    }
    private static string HashTree(string root) => MmcDurableFiles.Hash(Encoding.UTF8.GetBytes(string.Join("\n",
        Directory.EnumerateFileSystemEntries(root, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal).Select(path => Path.GetRelativePath(root, path) + ":" +
        (Directory.Exists(path) ? "directory" : Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path))))))));
    private static void Succeeded(MmcInstancePurgeResult result) { if (result.Status != MmcInstanceBackendStatus.Succeeded || !result.Completed) throw new Exception(result.Failure); }
    private static void Denied(Action action) { try { action(); } catch (IOException) { return; } throw new Exception("Expected exact ownership, snapshot or handle rejection."); }
    private static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}, got {actual}."); }
    private sealed class Fixture : IDisposable {
        internal readonly string Root = Path.Combine(Path.GetTempPath(), "pcllf-mmc-purge-" + Guid.NewGuid().ToString("N"));
        internal string Minecraft => Path.Combine(Root, "game");
        internal string Source => Path.Combine(Minecraft, "versions", "source");
        internal readonly MmcInstanceLifecycleBackend Backend = new();
        private readonly HttpClient client = new(new NoNetwork());
        private readonly Dictionary<string, byte[]> preserve = [];
        internal Fixture() {
            Directory.CreateDirectory(Root); Directory.CreateDirectory(Path.Combine(Root, "preserved"));
            foreach (string leaf in new[] { "shared-cache.bin", "java.exe", "other-instance.bin" }) { string path = Path.Combine(Root, "preserved", leaf); File.WriteAllText(path, "keep " + leaf); preserve.Add(path, File.ReadAllBytes(path)); }
            byte[] index = Encoding.UTF8.GetBytes("{\"objects\":{}}"); string hash = Convert.ToHexStringLower(SHA1.HashData(index));
            using (var zip = ZipFile.Open(Path.Combine(Root, "pack.zip"), ZipArchiveMode.Create)) {
                Add("pack/mmc-pack.json", """{"formatVersion":1,"components":[{"uid":"net.minecraft","version":"1.7.10"}]}""");
                Add("pack/patches/net.minecraft.json", $$"""{"formatVersion":1,"uid":"net.minecraft","version":"1.7.10","mainClass":"fixture.Game","mainJar":{"name":"fixture:game:1","MMC-hint":"local","MMC-filename":"main.jar"},"assetIndex":{"id":"fixture","url":"https://fixture.invalid/index.json","sha1":"{{hash}}","size":{{index.Length}}},"compatibleJavaMajors":[8]}""");
                Add("pack/libraries/main.jar", "local runtime bytes"); Add("pack/minecraft/saves/world/level.dat", "world bytes");
                void Add(string name, string text) { using var writer = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false)); writer.Write(text); }
            }
            string pack = Path.Combine(Root, "pack.zip"); preserve.Add(pack, File.ReadAllBytes(pack));
        }
        internal MmcInstanceBackendResult Import() {
            var backend = new MmcInstanceBackend<MmcArtifactDownloadSpec>(new(Path.Combine(Root, "metadata"), new Uri("https://metadata.invalid/"), client),
                new(Path.Combine(Root, "libs"), Path.Combine(Root, "assets"), Path.Combine(Root, "cache")), value => value,
                async (files, token) => { foreach (var file in files) await File.WriteAllTextAsync(file.DownloadPath, "{\"objects\":{}}", new UTF8Encoding(false), token); });
            var imported = backend.ImportAsync(new(Path.Combine(Root, "pack.zip"), Source, "source", new("windows", "x86_64", 8))).GetAwaiter().GetResult();
            if (imported.Status != MmcInstanceBackendStatus.Succeeded) throw new Exception(imported.Failure);
            foreach (string store in new[] { "metadata", "libs", "assets", "cache" }) {
                string directory = Path.Combine(Root, store);
                if (Directory.Exists(directory)) foreach (string path in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)) preserve.TryAdd(path, File.ReadAllBytes(path));
            }
            Directory.CreateDirectory(Path.Combine(Source, "minecraft", "empty")); return imported;
        }
        internal MmcInstanceLifecycleResult Quarantine() {
            var imported = Import(); var deleted = Backend.DeleteAsync(new(Source, imported.Definition!.InstanceId, imported.Definition.Generation, MmcSavedSourceStore.ReadPrimary(imported.Paths!).Sha256)).GetAwaiter().GetResult();
            if (deleted.Status != MmcInstanceBackendStatus.Succeeded || !deleted.Quarantined) throw new Exception(deleted.Failure); return deleted;
        }
        internal MmcInstancePurgePreview Preview(MmcInstanceLifecycleResult deleted) => Backend.PreviewPurgeAsync(new(deleted.OperationDirectory!, deleted.Definition!.InstanceId,
            deleted.Definition.Generation, MmcSavedSourceStore.ReadPrimary(MmcInstancePaths.Resolve(deleted.CurrentRoot!)).Sha256)).GetAwaiter().GetResult();
        internal void Preserved() { foreach (var pair in preserve) Equal(true, File.ReadAllBytes(pair.Key).SequenceEqual(pair.Value)); }
        public void Dispose() {
            client.Dispose(); InstanceConfigProviders.FlushAndDisposeWithin(Root);
            string full = Path.GetFullPath(Root); if (Path.GetDirectoryName(full) != Path.TrimEndingDirectorySeparator(Path.GetTempPath()) || !Guid.TryParseExact(Path.GetFileName(full)[16..], "N", out _)) throw new IOException("Unsafe fixture cleanup root.");
            if (Directory.Exists(full)) Directory.Delete(full, true);
        }
    }
    private sealed class NoNetwork : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => throw new IOException("Purge tiny fixture has no network.");
    }
}
