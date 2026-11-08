using System.Collections.Immutable;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using PCLCS;
using PCLLF.MMCC.Core;

internal static class MmcInstancePackageUpdateTests {
    internal static IEnumerable<(string Name, Action Run)> Cases() {
        yield return ("MMC ZIP updates reuse three-way author ownership across personal changes, equal-declaration local revisions, undo and later updates", Continuity);
        yield return ("MMC ZIP updates explicitly resolve removed user roots and retain surviving user patches without adopting new author local bytes", Components);
        yield return ("MMC ZIP update stale choices, changed candidate bytes and active or pending states never alter live data", Refusals);
        yield return ("MMC ZIP update cancellation and decided interruption retain recoverable package and ownership authority", Transactions);
        yield return ("MMC ZIP Minecraft changes use actual world backups and reject implicit game-tree migration", WorldSafety);
        yield return ("MMC external candidate stores block old whole-root and PCL cleanup even without an active versions child", ExternalStoreGuard);
    }
    private static void Continuity() {
        using Fixture fixture = new();
        string a = fixture.Pack("A", "runtimeA", new() { ["config/changed.cfg"] = "author A", ["config/deleted.cfg"] = "deleted A",
            ["config/remove.cfg"] = "remove A", ["config/retain.cfg"] = "retain A" }, new() { ["mods/disabled.jar"] = Mod("disabled", "1"), ["mods/unchanged.jar"] = Mod("unchanged", "1") });
        fixture.Import(a); var original = fixture.Primary();
        fixture.Put("minecraft/config/changed.cfg", "personal content"); fixture.Delete("minecraft/config/deleted.cfg"); fixture.Put("minecraft/config/retain.cfg", "personal retained removal");
        File.Move(fixture.Path("minecraft/mods/disabled.jar"), fixture.Path("minecraft/mods/disabled.jar.disabled"));
        fixture.Put("minecraft/config/personal.cfg", "personal addition"); fixture.Put("minecraft/saves/world/level.dat", "current world");
        fixture.Put(".minecraft/saves/alternate/level.dat", "alternate world"); fixture.Put("minecraft/config/xaero/current.cfg", "current waypoints");
        using var provider = new InstanceConfigProvider(fixture.VersionRoot);
        MmcIniConfiguration.QueuePreferenceBatch(provider, new Dictionary<string, string?> { ["MmcJvmArgsMode"] = "Custom", ["VersionAdvanceJvm"] = "" }, new JObject { ["unknown-user"] = "current setting" }); provider.Save();
        string b = fixture.Pack("B", "runtimeB", new() { ["config/changed.cfg"] = "author B", ["config/deleted.cfg"] = "deleted B", ["config/new.cfg"] = "new B",
            ["saves/template/level.dat"] = "author world template" }, new() { ["mods/disabled.jar"] = Mod("disabled", "2"), ["mods/unchanged.jar"] = Mod("unchanged", "1") });
        var preview = Prepare(fixture, b); var selected = Preserve(fixture, preview);
        var upgraded = fixture.Update().CommitAsync(new(selected, selected.PreviewSha256)).GetAwaiter().GetResult(); Require(upgraded);
        Equal(2L, upgraded.Definition!.Generation); Equal("runtimeB", fixture.Read("libraries/runtime.jar"));
        Equal(original.Definition.ArtifactLocks.Single(value => value.TargetRelativePath == "libraries/runtime.jar").SourceRevision,
            upgraded.Definition.ArtifactLocks.Single(value => value.TargetRelativePath == "libraries/runtime.jar").SourceRevision);
        Equal(false, original.Definition.ArtifactLocks.Single(value => value.TargetRelativePath == "libraries/runtime.jar").ArtifactKey ==
            upgraded.Definition.ArtifactLocks.Single(value => value.TargetRelativePath == "libraries/runtime.jar").ArtifactKey);
        Equal(upgraded.Definition.Origin.PackageSha256, upgraded.Definition.SourceBundle.PackageLocalScopeSha256);
        Equal("personal content", fixture.Read("minecraft/config/changed.cfg")); Equal(false, File.Exists(fixture.Path("minecraft/config/deleted.cfg")));
        Equal(false, File.Exists(fixture.Path("minecraft/config/remove.cfg"))); Equal("personal retained removal", fixture.Read("minecraft/config/retain.cfg"));
        Equal(true, File.Exists(fixture.Path("minecraft/mods/disabled.jar.disabled"))); Equal(false, File.Exists(fixture.Path("minecraft/mods/disabled.jar")));
        Equal(false, Directory.Exists(fixture.Path("minecraft/saves/template"))); Equal("current world", fixture.Read("minecraft/saves/world/level.dat"));
        Equal("alternate world", fixture.Read(".minecraft/saves/alternate/level.dat")); Equal("current waypoints", fixture.Read("minecraft/config/xaero/current.cfg"));
        Equal("", MmcIniConfiguration.CapturePreferences(provider).Ini["VersionAdvanceJvm"]); Equal("current setting", provider.Read<string>("unknown-user", null, false));
        File.Delete(a); File.Delete(b); Directory.Delete(fixture.CacheRoot, true); Ready(fixture);
        fixture.Delete("libraries/runtime.jar"); var repaired = fixture.Backend().RepairAsync(new(fixture.VersionRoot, new("windows", "x86_64", 8), fixture.Primary().Definition.InstanceId)).GetAwaiter().GetResult();
        if (repaired.Status != MmcInstanceBackendStatus.Succeeded) throw new Exception(repaired.Failure); Equal("runtimeB", fixture.Read("libraries/runtime.jar")); Ready(fixture);
        string c = fixture.Pack("C", "runtimeC", new() { ["config/changed.cfg"] = "author C", ["config/deleted.cfg"] = "deleted C", ["config/personal.cfg"] = "new author collision" },
            new() { ["mods/disabled.jar"] = Mod("disabled", "3"), ["mods/unchanged.jar"] = Mod("unchanged", "1") });
        var updateC = fixture.Update().CommitAsync(NewCommit(Preserve(fixture, Prepare(fixture, c)))).GetAwaiter().GetResult(); Require(updateC);
        Equal("personal content", fixture.Read("minecraft/config/changed.cfg")); Equal("personal addition", fixture.Read("minecraft/config/personal.cfg"));
        Equal(false, File.Exists(fixture.Path("minecraft/config/deleted.cfg")));
        byte[] owned = MmcInstancePackageOwnership.Read(MmcInstancePaths.Resolve(fixture.VersionRoot), fixture.Primary().Definition)!;
        var ownership = JObject.Parse(Encoding.UTF8.GetString(owned));
        Equal("personal-added", ownership["files"]!["config/personal.cfg"]!["local"]!.Value<string>("ownership"));
        Restore(fixture, updateC.UndoPoint!); Equal("runtimeB", fixture.Read("libraries/runtime.jar"));
        var againB = Prepare(fixture, c); Equal(PackUpdateOwnership.PersonalModified, againB.Files.Single(value => value.RelativePath == "config/changed.cfg").Ownership);
        Restore(fixture, upgraded.UndoPoint!); Equal("runtimeA", fixture.Read("libraries/runtime.jar"));
        Equal(null, fixture.Primary().Definition.PackageOwnershipReference); Equal(null, fixture.Primary().Definition.SourceBundle.PackageLocalScopeSha256); Ready(fixture);
        var againA = Prepare(fixture, c); Equal(PackUpdateOwnership.PersonalModified, againA.Files.Single(value => value.RelativePath == "config/changed.cfg").Ownership);
        Equal("current world", fixture.Read("minecraft/saves/world/level.dat"));
    }
    private static void Components() {
        using Fixture fixture = new(); fixture.Import(fixture.Pack("old", "runtimeA", new(), oldRoot: true));
        fixture.UserPatch("net.minecraft", "-Dkeep-user=true"); fixture.UserPatch("fixture.old", "-Dremoved-user=true");
        string incoming = fixture.Pack("new", "runtimeB", new(), addedRoot: true);
        var preview = Prepare(fixture, incoming);
        Equal(true, preview.RequiresChoices); Equal(true, preview.Components.Any(value => value.Uid == "fixture.old" && value.Removed && value.Conflict));
        var cannotKeep = fixture.Update().SelectAsync(preview, preview.PreviewSha256, new([new("fixture.old", true)], [])).GetAwaiter().GetResult();
        Equal(MmcInstanceBackendStatus.Rejected, cannotKeep.Status);
        var selected = fixture.Update().SelectAsync(preview, preview.PreviewSha256, new([new("fixture.old", false)], [])).GetAwaiter().GetResult();
        if (selected.Preview is null) throw new Exception(selected.Failure); Equal(false, selected.Preview.RequiresChoices);
        var result = fixture.Update().CommitAsync(NewCommit(selected.Preview)).GetAwaiter().GetResult(); Require(result);
        Equal("runtimeA", fixture.Read("libraries/runtime.jar")); // Retained user-patch local authority is not rebound by the new author package scope.
        Equal(true, result.BoundPlan!.CompiledProfile.JvmArguments.Any(value => value.Value == "-Dkeep-user=true"));
        Equal(false, result.Definition!.ComponentOverrides.Any(value => value.Uid == "fixture.old"));
        var saved = MmcSavedSourceStore.LoadAsync(MmcInstancePaths.Resolve(fixture.VersionRoot), result.Definition).GetAwaiter().GetResult();
        Equal(true, saved.Resolved.Resolution.Profile!.Components.Any(value => value.Uid == "fixture.added"));
        Equal(true, saved.Resolved.Resolution.Profile.Components.Any(value => value.Uid == "fixture.dep")); Ready(fixture);
    }
    private static void Refusals() {
        using Fixture fixture = new(); fixture.Import(fixture.Pack("A", "runtimeA", new() { ["config/a.cfg"] = "old" }));
        string b = fixture.Pack("B", "runtimeB", new() { ["config/a.cfg"] = "new" }); var preview = Preserve(fixture, Prepare(fixture, b));
        string before = fixture.Primary().Sha256;
        foreach (string marker in new[] { "PCL/MmcImportPending.json", "PCL/MmcSessions/uncertain/receipt.record" }) {
            fixture.Put(marker, "foreign pending");
            Equal(MmcInstanceBackendStatus.NeedsRecovery, fixture.Update().CommitAsync(NewCommit(preview)).GetAwaiter().GetResult().Status);
            Equal(before, fixture.Primary().Sha256); Equal("foreign pending", fixture.Read(marker)); fixture.Delete(marker);
            if (marker.Contains("MmcSessions", StringComparison.Ordinal)) Directory.Delete(fixture.Path("PCL/MmcSessions/uncertain"));
        }
        fixture.Put("minecraft/config/a.cfg", "late user edit");
        Equal(MmcInstanceBackendStatus.NeedsRecovery, fixture.Update().CommitAsync(NewCommit(preview)).GetAwaiter().GetResult().Status);
        Equal(before, fixture.Primary().Sha256); Equal("late user edit", fixture.Read("minecraft/config/a.cfg"));
        fixture.Put("minecraft/config/a.cfg", "old"); fixture.Put("libraries/runtime.jar", "unknown private replacement");
        Equal(MmcInstanceBackendStatus.NeedsRecovery, fixture.Update().CommitAsync(NewCommit(preview)).GetAwaiter().GetResult().Status);
        Equal("unknown private replacement", fixture.Read("libraries/runtime.jar")); fixture.Put("libraries/runtime.jar", "runtimeA");
        string candidateFile = Path.Combine(preview.WorkingRoot, "versions", "candidate", "minecraft", "config", "a.cfg");
        File.WriteAllText(candidateFile, "candidate changed");
        Equal(MmcInstanceBackendStatus.NeedsRecovery, fixture.Update().CommitAsync(NewCommit(preview)).GetAwaiter().GetResult().Status); Equal(before, fixture.Primary().Sha256);
    }
    private static void Transactions() {
        using Fixture fixture = new(); fixture.Import(fixture.Pack("A", "runtimeA", new() { ["config/a.cfg"] = "A" }));
        foreach (string mode in new[] { "cancel", "interrupt-before", "interrupt-decided", "cancel-decided" }) {
            string b = fixture.Pack(mode, "runtimeB", new() { ["config/a.cfg"] = mode }); var preview = Preserve(fixture, Prepare(fixture, b));
            var before = fixture.Primary(); bool decided = mode.EndsWith("decided", StringComparison.Ordinal); using CancellationTokenSource cancellation = new();
            var result = fixture.Update().CommitAsync(NewCommit(preview) with { TransactionCheckpoint = (phase, _) => {
                if (phase != (decided ? MmcTransactionCheckpoint.CommitDecided : MmcTransactionCheckpoint.Prepared)) return;
                if (mode.StartsWith("cancel", StringComparison.Ordinal)) cancellation.Cancel(); else throw new MmcTransactionInterruptionException("controlled ZIP update interruption");
            } }, cancellation.Token).GetAwaiter().GetResult();
            if (mode.StartsWith("interrupt", StringComparison.Ordinal)) {
                Equal(MmcInstanceBackendStatus.NeedsRecovery, result.Status); if (result.UndoPoint is null) throw new Exception(result.Failure);
                using var quiet = MmcConfigurationCoordinator.Quiesce(fixture.VersionRoot); using var gate = MmcAdmissionGate.Acquire(fixture.VersionRoot, before.Definition.InstanceId, TimeSpan.Zero);
                var recovered = MmcFileTransactions.RecoverPending(gate); Equal(1, recovered.Count); Equal(decided ? MmcTransactionOutcome.Committed : MmcTransactionOutcome.Aborted, recovered[0].Outcome);
            } else Equal(decided ? MmcInstanceBackendStatus.Succeeded : MmcInstanceBackendStatus.Cancelled, result.Status);
            Equal(before.Definition.Generation + (decided ? 1 : 0), fixture.Primary().Definition.Generation);
            if (!decided) Equal(before.Sha256, fixture.Primary().Sha256);
            else Equal(true, fixture.Primary().Definition.PackageOwnershipReference is not null);
            Ready(fixture);
        }
    }
    private static void WorldSafety() {
        using Fixture fixture = new(); fixture.Import(fixture.Pack("A", "runtimeA", new()));
        fixture.Put("minecraft/saves/world/level.dat", "world data");
        string next = fixture.Pack("newmc", "runtimeB", new(), minecraft: "1.8.9"); var preview = Preserve(fixture, Prepare(fixture, next));
        Equal(true, preview.ChangesMinecraftVersion); string before = fixture.Primary().Sha256;
        Equal(MmcInstanceBackendStatus.NeedsRecovery, fixture.Update(false).CommitAsync(NewCommit(preview)).GetAwaiter().GetResult().Status); Equal(before, fixture.Primary().Sha256);
        var result = fixture.Update().CommitAsync(NewCommit(preview)).GetAwaiter().GetResult(); Require(result);
        Equal(1, result.WorldBackupIds.Length); Equal("world data", fixture.Read("minecraft/saves/world/level.dat"));
        Equal(true, fixture.WorldBackups.Points(fixture.GameRoot).Any(value => value.Value<string>("id") == result.WorldBackupIds[0]));
        string leaf = fixture.Pack("otherleaf", "runtimeC", new(), minecraft: "1.8.9", leaf: ".minecraft");
        var changed = fixture.Update().PrepareAsync(fixture.Request(leaf)).GetAwaiter().GetResult();
        Equal(MmcInstanceBackendStatus.Rejected, changed.Status); Equal("world data", fixture.Read("minecraft/saves/world/level.dat"));
    }
    private static void ExternalStoreGuard() {
        using Fixture fixture = new();
        var request = new MmcInstancePackageUpdateRequest(fixture.VersionRoot, Guid.NewGuid(), 1, new string('a', 64), Path.Combine(fixture.Root, "unused.zip"), new("windows", "x86_64", 8));
        var job = MmcPackageUpdateJob.Create(request, new string('b', 64));
        Throws(() => MmcInstanceCapabilities.RequireLegacyGameFolder(Path.GetDirectoryName(Path.GetDirectoryName(fixture.VersionRoot)!)!, "清除游戏目录"));
        Throws(() => MmcInstanceCapabilities.RequireLegacyGameFolder(Path.GetDirectoryName(job.Root)!, "清除候选目录"));
        Throws(() => MmcInstanceCapabilities.RequireLegacyContainingInstance(Path.Combine(job.Root, "new-file"), "旧文件操作"));
        var discarded = fixture.Update().DiscardAsync(job.Root, job.Owner.OperationId).GetAwaiter().GetResult(); Require(discarded);
        Equal(false, Directory.Exists(job.Root));
        MmcInstanceCapabilities.RequireLegacyGameFolder(Path.GetDirectoryName(Path.GetDirectoryName(fixture.VersionRoot)!)!, "普通目录清除检查");
    }
    private static MmcInstancePackageUpdateCommitRequest NewCommit(MmcInstancePackageUpdatePreview preview) => new(preview, preview.PreviewSha256);
    private static MmcInstancePackageUpdatePreview Prepare(Fixture fixture, string archive) {
        var result = fixture.Update().PrepareAsync(fixture.Request(archive)).GetAwaiter().GetResult();
        return result.Status == MmcInstanceBackendStatus.Succeeded && result.Preview is not null ? result.Preview : throw new Exception(result.Failure);
    }
    private static MmcInstancePackageUpdatePreview Preserve(Fixture fixture, MmcInstancePackageUpdatePreview preview) {
        var components = preview.Components.Where(value => value.HasUserOverride && value.Conflict).Select(value => new MmcPackageComponentDecision(value.Uid, !value.Removed)).ToImmutableArray();
        var files = preview.Files.Where(value => value.Conflict && value.CanChoose).Select(value => new MmcPackageFileDecision(value.ChangeId, PackUpdateChoice.PreservePersonal)).ToImmutableArray();
        var result = fixture.Update().SelectAsync(preview, preview.PreviewSha256, new(components, files, true)).GetAwaiter().GetResult();
        if (result.Preview is null) throw new Exception(result.Failure); if (result.Preview.RequiresChoices || !result.Preview.Errors.IsEmpty) throw new Exception(string.Join("; ", result.Preview.Errors)); return result.Preview;
    }
    private static void Restore(Fixture fixture, MmcInstanceHistoryPoint point) {
        var current = fixture.Primary(); var result = new MmcInstanceHistoryBackend().RestoreAsync(new(fixture.VersionRoot, current.Definition.InstanceId, current.Definition.Generation,
            current.Sha256, point.PointId, point.ManifestSha256)).GetAwaiter().GetResult(); if (result.Status != MmcInstanceBackendStatus.Succeeded) throw new Exception(result.Failure);
    }
    private static void Ready(Fixture fixture) {
        var current = fixture.Primary(); var result = fixture.Backend().PrepareLaunchAsync(new(fixture.VersionRoot, new("windows", "x86_64", 8), current.Definition.InstanceId, current.Definition.Generation) {
            ExpectedPrimarySha256 = current.Sha256, RepairIncomplete = false }).GetAwaiter().GetResult(); if (result.Status != MmcInstanceBackendStatus.Succeeded) throw new Exception(result.Failure);
    }
    private static byte[] Mod(string id, string version) {
        using MemoryStream stream = new(); using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true)) {
            using var writer = new StreamWriter(zip.CreateEntry("mcmod.info").Open(), new UTF8Encoding(false)); writer.Write(new JArray(new JObject { ["modid"] = id, ["name"] = id, ["version"] = version, ["mcversion"] = "1.7.10" }).ToString());
        } return stream.ToArray();
    }
    private static void Require(MmcInstancePackageUpdateResult result) { if (result.Status != MmcInstanceBackendStatus.Succeeded) throw new Exception(result.Failure); }
    private static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}, got {actual}."); }
    private static void Throws(Action action) { try { action(); } catch (Exception error) when (error is IOException or InvalidOperationException) { return; } throw new Exception("Expected a fresh cleanup/ownership refusal."); }
    private sealed class Fixture : IDisposable {
        internal string Root { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pcllf-mmc-zip-update-" + Guid.NewGuid().ToString("N"));
        internal string VersionRoot => System.IO.Path.Combine(Root, "game", "versions", "live");
        internal string GameRoot => System.IO.Path.Combine(VersionRoot, "minecraft");
        internal string CacheRoot => System.IO.Path.Combine(Root, "cache");
        internal WorldBackupEngine<string> WorldBackups { get; }
        private readonly HttpClient client = new(new Metadata());
        private readonly byte[] index = Encoding.UTF8.GetBytes("{\"objects\":{}}");
        internal Fixture() {
            Directory.CreateDirectory(Root);
            WorldBackups = new(new() { RemoveExtendedPrefix = value => value, ProfileAddress = name => System.IO.Path.Combine(Root, "profile", name),
                GameFolder = value => value, VersionPath = _ => VersionRoot, Retention = _ => 3, AutoEnabled = _ => false, Watchers = () => [], WorldLabel = _ => null,
                FormatSize = value => value.ToString(), Warn = (_, _) => { }, UiBusy = () => false, Dispatch = action => action(), NotifyChanged = () => { }, OpenFolder = _ => { }, Hint = (_, _) => { } });
        }
        internal string Pack(string tag, string local, Dictionary<string, string> files, Dictionary<string, byte[]>? binaries = null,
            bool oldRoot = false, bool addedRoot = false, string minecraft = "1.7.10", string leaf = "minecraft") {
            string archive = System.IO.Path.Combine(Root, tag + ".zip");
            using var zip = ZipFile.Open(archive, ZipArchiveMode.Create); var roots = new JArray(new JObject { ["uid"] = "net.minecraft", ["version"] = minecraft },
                new JObject { ["uid"] = "net.minecraftforge", ["version"] = "10.13.4.1614" });
            if (oldRoot) roots.Add(new JObject { ["uid"] = "fixture.old", ["version"] = "1" }); if (addedRoot) roots.Add(new JObject { ["uid"] = "fixture.added", ["version"] = "1" });
            Add("mmc-pack.json", Encoding.UTF8.GetBytes(new JObject { ["formatVersion"] = 1, ["components"] = roots }.ToString()));
            Add("instance.cfg", Encoding.UTF8.GetBytes("name=Fixture\n"));
            Add("patches/net.minecraft.json", Encoding.UTF8.GetBytes($$"""{"formatVersion":1,"uid":"net.minecraft","version":"{{minecraft}}","mainClass":"fixture.Main","mainJar":{"name":"fixture:runtime:1","MMC-hint":"local","MMC-filename":"runtime.jar"},"assetIndex":{"id":"fixture","url":"https://fixture.invalid/assets.json","sha1":"{{Convert.ToHexStringLower(SHA1.HashData(index))}}","size":{{index.Length}}},"compatibleJavaMajors":[8]}"""));
            Add("patches/net.minecraftforge.json", Encoding.UTF8.GetBytes("""{"formatVersion":1,"uid":"net.minecraftforge","version":"10.13.4.1614"}"""));
            if (oldRoot) Add("patches/fixture.old.json", Encoding.UTF8.GetBytes("""{"formatVersion":1,"uid":"fixture.old","version":"1","+jvmArgs":["-Dold=true"]}"""));
            if (addedRoot) Add("patches/fixture.added.json", Encoding.UTF8.GetBytes("""{"formatVersion":1,"uid":"fixture.added","version":"1","requires":[{"uid":"fixture.dep","equals":"1"}],"+jvmArgs":["-Dadded=true"]}"""));
            Add("libraries/runtime.jar", Encoding.UTF8.GetBytes(local)); Add("readme.txt", Encoding.UTF8.GetBytes("immutable author documentation"));
            foreach (var item in files) Add(leaf + "/" + item.Key, Encoding.UTF8.GetBytes(item.Value));
            foreach (var item in binaries ?? new()) Add(leaf + "/" + item.Key, item.Value);
            return archive;
            void Add(string name, byte[] bytes) { using var output = zip.CreateEntry(name).Open(); output.Write(bytes); }
        }
        private MmcMetadataStore Store() => new(System.IO.Path.Combine(CacheRoot, "metadata"), new Uri("https://metadata.invalid/"), client);
        private MmcArtifactCacheRoots Caches() => new(System.IO.Path.Combine(CacheRoot, "libraries"), System.IO.Path.Combine(CacheRoot, "assets"), System.IO.Path.Combine(CacheRoot, "sources"));
        private async Task Downloads(ImmutableArray<MmcArtifactDownloadSpec> files, CancellationToken token) { foreach (var file in files) await File.WriteAllBytesAsync(file.DownloadPath, index, token); }
        internal MmcInstanceBackend<MmcArtifactDownloadSpec> Backend() => new(Store(), Caches(), value => value, Downloads);
        internal MmcInstancePackageUpdateBackend<MmcArtifactDownloadSpec> Update(bool backups = true) => new(Store(), Caches(), value => value, Downloads,
            backups ? (root, token) => WorldBackups.CaptureFolder(root, "Minecraft版本更改前", 3, token) : null);
        internal void Import(string archive) { var result = Backend().ImportAsync(new(archive, VersionRoot, "live", new("windows", "x86_64", 8))).GetAwaiter().GetResult(); if (result.Status != MmcInstanceBackendStatus.Succeeded) throw new Exception(result.Failure); }
        internal MmcInstancePackageUpdateRequest Request(string archive) { var current = Primary(); return new(VersionRoot, current.Definition.InstanceId, current.Definition.Generation, current.Sha256, archive, new("windows", "x86_64", 8)); }
        internal MmcStoredPrimaryDefinition Primary() => MmcSavedSourceStore.ReadPrimary(MmcInstancePaths.Resolve(VersionRoot));
        internal string Path(string relative) => System.IO.Path.Combine(VersionRoot, relative.Replace('/', System.IO.Path.DirectorySeparatorChar));
        internal void Put(string relative, string value) { string path = Path(relative); Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!); File.WriteAllText(path, value); }
        internal void Delete(string relative) => File.Delete(Path(relative));
        internal string Read(string relative) => File.ReadAllText(Path(relative));
        internal void UserPatch(string uid, string argument) {
            var current = Primary(); var record = current.Definition.SourceBundle.MetadataRecords.Single(value => value.Kind == MmcSourceRecordKind.Component && value.Uid == uid);
            var patch = JObject.Parse(Encoding.UTF8.GetString(MmcSavedSourceStore.ReadBounded(MmcInstancePaths.Resolve(VersionRoot), record.ObjectRelativePath, 16 * 1024 * 1024))); patch["+jvmArgs"] = new JArray(argument);
            var changer = new MmcInstanceDefinitionChangeBackend<MmcArtifactDownloadSpec>(Store(), Caches(), value => value, Downloads);
            var preview = changer.PreviewAsync(new(VersionRoot, current.Definition.InstanceId, current.Definition.Generation, current.Sha256, new("windows", "x86_64", 8), [new(uid, PatchContentUtf8: patch.ToString())])).GetAwaiter().GetResult();
            if (preview.Preview is null) throw new Exception(preview.Failure); var result = changer.CommitAsync(new(preview.Preview, preview.Preview.CandidateSha256)).GetAwaiter().GetResult(); if (result.Status != MmcInstanceBackendStatus.Succeeded) throw new Exception(result.Failure);
        }
        public void Dispose() { client.Dispose(); InstanceConfigProviders.FlushAndDisposeWithin(Root); if (Directory.Exists(Root)) Directory.Delete(Root, true); }
        private sealed class Metadata : HttpMessageHandler {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => request.RequestUri!.AbsolutePath.EndsWith("fixture.dep/1.json", StringComparison.Ordinal)
                ? Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"formatVersion":1,"uid":"fixture.dep","version":"1"}""", Encoding.UTF8, "application/json") })
                : throw new IOException("Unexpected controlled metadata URI: " + request.RequestUri);
        }
    }
}
