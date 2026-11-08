using System.Collections.Immutable;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using PCLCS;
using PCLLF.MMCC.Core;

internal static class MmcInstanceBackendTests {
    internal static IEnumerable<(string Name, Action Run)> Cases() {
        yield return ("MMC import preparation measures the full archive and keeps opaque docs outside live settings", SourcePreparation);
        yield return ("MMC backend imports real files and repairs offline after original ZIP deletion without touching player data", ImportRepair);
        yield return ("MMC backend rejects damaged authority and retains interrupted import ownership", FailureIsolation);
        yield return ("MMC launch preparation keeps complete generations and repairs only missing managed inputs offline", LaunchPreparation);
        yield return ("MMC launch preparation rejects stale identity, new variants and damaged support without adopting bytes", LaunchRefusals);
    }
    private static void SourcePreparation() => SourcePreparationAsync().GetAwaiter().GetResult();
    private static async Task SourcePreparationAsync() {
        using Fixture fixture = new();
        using var source = await MmcArchiveSourceReader.ReadAsync(fixture.Archive);
        var reservation = MmcOwnedImportReservation.Create(fixture.VersionRoot, Guid.NewGuid(), source.PackageSha256);
        var paths = MmcInstancePaths.Resolve(reservation.TemporaryRoot, source.Package.GameRootLeaf);
        using var prepared = await MmcPackageFileStager.PrepareInitialAsync(source, paths);
        Equal(source.ArchiveEntries.Count(entry => !entry.IsDirectory), prepared.Files.Length);
        var baseline = Baseline(source, prepared);
        byte[] bytes = MmcPackageFileBaselineCodec.Serialize(baseline);
        var resolved = MmcComponentResolver.Resolve(source.Package, []); Equal(true, resolved.IsComplete);
        using var metadata = MmcSourceBundleWriter.Prepare(source, new(resolved, []), paths);
        var reference = metadata.StageAuthorBaseline(bytes, prepared.Files.Length); metadata.StageOpaqueOriginals(prepared);
        var proof = MmcPackageFileBaselineCodec.Read(new(bytes), reference, source.PackageSha256, source.Package.RelativeRoot);
        Equal(false, proof.HasErrors); Equal(true, proof.Baseline is not null);
        Equal(true, prepared.InitialGameMutations.Any(item => item.RelativePath == "minecraft/libraries/fixture.jar"));
        Equal(false, prepared.InitialGameMutations.Any(item => item.RelativePath == "PCL/config.json" || item.RelativePath == "README.txt"));
        Equal(true, metadata.Mutations.Any(item => item.RelativePath.Contains("/package-files/", StringComparison.Ordinal)));
        Equal(false, Directory.Exists(fixture.VersionRoot));
        Directory.CreateDirectory(fixture.VersionRoot); File.WriteAllText(Path.Combine(fixture.VersionRoot, "player.txt"), "keep");
        bool refused = false; try { MmcOwnedImportReservation.Create(fixture.VersionRoot, Guid.NewGuid(), source.PackageSha256); } catch (IOException) { refused = true; }
        Equal(true, refused); Equal("keep", File.ReadAllText(Path.Combine(fixture.VersionRoot, "player.txt")));
    }
    internal static MmcPackageFileBaseline Baseline(MmcArchiveSource source, MmcPreparedPackageFiles prepared) => new(1, source.PackageSha256, source.Package.RelativeRoot,
        source.ArchiveEntries.Select(entry => new MmcPackageArchiveDescriptor(entry.Path, entry.IsDirectory, entry.SizeBytes, entry.Crc32)).ToImmutableArray(),
        prepared.Files.Select(file => new MmcPackageFileRecord(file.ArchiveRelativePath!, Enum.Parse<PCLLF.MMCC.Core.MmcPackageFileRole>(file.Role.ToString()), file.TargetRelativePath,
            file.Sha256, file.Sha1, file.SizeBytes, file.Crc32)).ToImmutableArray());
    private static void ImportRepair() => ImportRepairAsync().GetAwaiter().GetResult();
    private static async Task ImportRepairAsync() {
        using Fixture fixture = new(); var backend = fixture.Backend();
        var imported = await backend.ImportAsync(new(fixture.Archive, fixture.VersionRoot, "fixture", new("windows", "x86_64", 8)));
        if (imported.Status != MmcInstanceBackendStatus.Succeeded) throw new Exception(imported.Failure);
        Equal(MmcInstanceBackendStatus.Succeeded, imported.Status);
        var paths = imported.Paths!;
        Equal("launcher library", File.ReadAllText(Path.Combine(paths.LocalLibraryRoot, "fixture.jar")));
        Equal("different game library", File.ReadAllText(Path.Combine(paths.GameRoot, "libraries", "fixture.jar")));
        Equal(1, imported.Definition!.PackageFileLocks.Length); Equal("libraries/unreferenced.jar", imported.Definition.PackageFileLocks[0].TargetRelativePath); Equal(1, fixture.Downloads);
        Equal(false, File.Exists(Path.Combine(paths.InstanceRoot, "PCL", "MmcImportPending.json")));
        JObject settings = JObject.Parse(File.ReadAllText(Path.Combine(paths.InstanceRoot, "PCL", "config.json")));
        Equal(false, settings.ContainsKey("foreign-player-pref"));
        using (var provider = new InstanceConfigProvider(paths.InstanceRoot)) { provider.Set("unknown-personal-pref", "keep", false); provider.Save(); }
        string playerConfig = Path.Combine(paths.GameRoot, "config", "example.cfg"); File.WriteAllText(playerConfig, "edited player config");
        string save = Path.Combine(paths.GameRoot, "saves", "world", "level.dat"); Directory.CreateDirectory(Path.GetDirectoryName(save)!); File.WriteAllText(save, "keep world");
        File.Delete(fixture.Archive); Directory.Delete(fixture.CacheRoot, true);
        foreach (var locked in imported.Definition.ArtifactLocks) File.Delete(paths.ResolveRelative(MmcPathRole.Instance, locked.TargetRelativePath));
        foreach (var locked in imported.Definition.PackageFileLocks) File.Delete(paths.ResolveRelative(MmcPathRole.Instance, locked.TargetRelativePath));
        File.Delete(Path.Combine(paths.InstanceRoot, "fixture.json"));
        var repaired = await fixture.Backend(offline: true).RepairAsync(new(paths.InstanceRoot, new("windows", "x86_64", 8), imported.Definition.InstanceId));
        if (repaired.Status != MmcInstanceBackendStatus.Succeeded) throw new Exception(repaired.Failure);
        Equal(MmcInstanceBackendStatus.Succeeded, repaired.Status); Equal(2L, repaired.Definition!.Generation); Equal(1, fixture.Downloads);
        Equal("launcher library", File.ReadAllText(Path.Combine(paths.LocalLibraryRoot, "fixture.jar")));
        Equal("unreferenced original", File.ReadAllText(Path.Combine(paths.LocalLibraryRoot, "unreferenced.jar")));
        Equal("edited player config", File.ReadAllText(playerConfig)); Equal("keep world", File.ReadAllText(save));
        Equal("different game library", File.ReadAllText(Path.Combine(paths.GameRoot, "libraries", "fixture.jar")));
        Equal("keep", JObject.Parse(File.ReadAllText(Path.Combine(paths.InstanceRoot, "PCL", "config.json"))).Value<string>("unknown-personal-pref"));
    }
    private static void FailureIsolation() => FailureIsolationAsync().GetAwaiter().GetResult();
    private static void LaunchPreparation() => LaunchPreparationAsync().GetAwaiter().GetResult();
    private static async Task LaunchPreparationAsync() {
        using Fixture fixture = new();
        var imported = await fixture.Backend().ImportAsync(new(fixture.Archive, fixture.VersionRoot, "fixture", new("windows", "x86_64", 8)));
        if (imported.Status != MmcInstanceBackendStatus.Succeeded) throw new Exception(imported.Failure);
        Guid id = imported.Definition!.InstanceId;
        File.Delete(fixture.Archive); Directory.Delete(fixture.CacheRoot, true);
        var backend = fixture.Backend(offline: true);
        var request = new MmcInstancePrepareLaunchRequest(fixture.VersionRoot, new("windows", "x86_64", 8), id, 1);
        string primary = Path.Combine(fixture.VersionRoot, "PCL", "InstanceDefinition.json"); byte[] before = File.ReadAllBytes(primary);
        var ready = await backend.PrepareLaunchAsync(request);
        if (ready.Status != MmcInstanceBackendStatus.Succeeded) throw new Exception(ready.Failure);
        Equal(false, ready.Repaired); Equal(1L, ready.Snapshot!.Definition.Generation); Equal(true, before.SequenceEqual(File.ReadAllBytes(primary)));
        Equal(imported.BoundPlan!.BoundFingerprint, ready.Snapshot.BoundPlan.BoundFingerprint);
        Equal(true, ready.Snapshot.Targets.All(value => File.Exists(value.ActualPath)));
        Equal(true, ready.Snapshot.PackageConfiguration.Values.ContainsKey("JvmArgs"));
        using (var lease = MmcAdmissionGate.Acquire(fixture.VersionRoot, id, TimeSpan.Zero))
            MmcInstanceBackend<MmcArtifactDownloadSpec>.ValidatePreparedLaunch(lease, ready.Snapshot);
        byte[] supportBytes = Encoding.UTF8.GetBytes("owned reviewed support"); string digest = Convert.ToHexStringLower(SHA256.HashData(supportBytes));
        var support = await MmcInstanceBackend<MmcArtifactDownloadSpec>.PrepareLaunchSupportAsync(ready.Snapshot, [new("wrapper.jar", supportBytes, digest)]);
        Equal(digest, Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(support[0].ActualPath))));
        Equal(true, before.SequenceEqual(File.ReadAllBytes(primary)));
        File.Delete(Path.Combine(fixture.VersionRoot, "fixture.json"));
        foreach (var target in ready.Snapshot.Targets) File.Delete(target.ActualPath);
        var missing = await backend.PrepareLaunchAsync(request with { RepairIncomplete = false });
        Equal(MmcInstanceBackendStatus.NeedsRecovery, missing.Status); Equal(false, missing.Repaired);
        var repaired = await backend.PrepareLaunchAsync(request);
        if (repaired.Status != MmcInstanceBackendStatus.Succeeded) throw new Exception(repaired.Failure);
        Equal(true, repaired.Repaired); Equal(2L, repaired.Snapshot!.Definition.Generation); Equal(1, fixture.Downloads);
        var stable = await backend.PrepareLaunchAsync(request with { ExpectedGeneration = 2 });
        if (stable.Status != MmcInstanceBackendStatus.Succeeded) throw new Exception(stable.Failure);
        Equal(false, stable.Repaired); Equal(2L, stable.Snapshot!.Definition.Generation);
        using var staleLease = MmcAdmissionGate.Acquire(fixture.VersionRoot, id, TimeSpan.Zero);
        bool refused = false; try { MmcInstanceBackend<MmcArtifactDownloadSpec>.ValidatePreparedLaunch(staleLease, ready.Snapshot); } catch (IOException) { refused = true; }
        Equal(true, refused);
    }
    private static void LaunchRefusals() => LaunchRefusalsAsync().GetAwaiter().GetResult();
    private static async Task LaunchRefusalsAsync() {
        using Fixture fixture = new();
        var imported = await fixture.Backend().ImportAsync(new(fixture.Archive, fixture.VersionRoot, "fixture", new("windows", "x86_64", 8)));
        if (imported.Status != MmcInstanceBackendStatus.Succeeded) throw new Exception(imported.Failure);
        Guid id = imported.Definition!.InstanceId; var backend = fixture.Backend(offline: true);
        var request = new MmcInstancePrepareLaunchRequest(fixture.VersionRoot, new("windows", "x86_64", 8), id, 1);
        Equal(MmcInstanceBackendStatus.NeedsRecovery, (await backend.PrepareLaunchAsync(request with { ExpectedInstanceId = Guid.NewGuid() })).Status);
        var variant = await backend.PrepareLaunchAsync(request with { Environment = new("windows", "arm64", 8) });
        Equal(MmcInstanceBackendStatus.Rejected, variant.Status); Equal("MMC_INSTANCE_VARIANT_REQUIRED", variant.Diagnostics.Single().Code);
        var ready = await backend.PrepareLaunchAsync(request);
        if (ready.Status != MmcInstanceBackendStatus.Succeeded) throw new Exception(ready.Failure);
        byte[] bytes = Encoding.UTF8.GetBytes("reviewed bridge"); string digest = Convert.ToHexStringLower(SHA256.HashData(bytes));
        var inputs = new[] { new MmcInstanceLaunchSupportInput("bridge.jar", bytes, digest) };
        var support = await MmcInstanceBackend<MmcArtifactDownloadSpec>.PrepareLaunchSupportAsync(ready.Snapshot!, inputs);
        File.WriteAllText(support[0].ActualPath, "external bytes");
        bool refused = false; try { await MmcInstanceBackend<MmcArtifactDownloadSpec>.PrepareLaunchSupportAsync(ready.Snapshot!, inputs); } catch (IOException) { refused = true; }
        Equal(true, refused); Equal("external bytes", File.ReadAllText(support[0].ActualPath));
        string active = ready.Snapshot!.Targets.First().ActualPath; File.WriteAllText(active, "changed after preparation");
        using var lease = MmcAdmissionGate.Acquire(fixture.VersionRoot, id, TimeSpan.Zero);
        refused = false; try { MmcInstanceBackend<MmcArtifactDownloadSpec>.ValidatePreparedLaunch(lease, ready.Snapshot); } catch (IOException) { refused = true; }
        Equal(true, refused); Equal(1L, MmcSavedSourceStore.ReadPrimary(ready.Snapshot.Paths).Definition.Generation);
    }
    private static async Task FailureIsolationAsync() {
        using (Fixture fixture = new()) {
            var imported = await fixture.Backend().ImportAsync(new(fixture.Archive, fixture.VersionRoot, "fixture", new("windows", "x86_64", 8)));
            if (imported.Status != MmcInstanceBackendStatus.Succeeded) throw new Exception(imported.Failure);
            string primary = Path.Combine(fixture.VersionRoot, "PCL", "InstanceDefinition.json"); JObject json = JObject.Parse(File.ReadAllText(primary));
            json["artifactLocks"]![0]!["adoptedSha256"] = new string('0', 64); File.WriteAllText(primary, json.ToString());
            var failed = await fixture.Backend(offline: true).RepairAsync(new(fixture.VersionRoot, new("windows", "x86_64", 8)));
            Equal(MmcInstanceBackendStatus.NeedsRecovery, failed.Status); Equal("launcher library", File.ReadAllText(Path.Combine(fixture.VersionRoot, "libraries", "fixture.jar")));
        }
        using (Fixture fixture = new()) {
            string existing = Path.Combine(Path.GetDirectoryName(fixture.VersionRoot)!, "existing", "player.dat"); Directory.CreateDirectory(Path.GetDirectoryName(existing)!); File.WriteAllText(existing, "untouched");
            var failed = await fixture.Backend().ImportAsync(new(fixture.Archive, fixture.VersionRoot, "fixture", new("windows", "x86_64", 8)) {
                TransactionCheckpoint = (phase, _) => { if (phase == MmcTransactionCheckpoint.Prepared) throw new MmcTransactionInterruptionException("controlled interruption"); }
            });
            Equal(MmcInstanceBackendStatus.NeedsRecovery, failed.Status); Equal(false, Directory.Exists(fixture.VersionRoot)); Equal("untouched", File.ReadAllText(existing));
            Equal(true, File.Exists(Path.Combine(failed.WorkingRoot!, "PCL", "MmcImportPending.json")));
            using CancellationTokenSource cancellation = new(); cancellation.Cancel();
            Equal(MmcInstanceBackendStatus.Cancelled, (await fixture.Backend().ImportAsync(new(fixture.Archive, fixture.VersionRoot, "fixture", new("windows", "x86_64", 8)), cancellation.Token)).Status);
            Equal(false, Directory.Exists(fixture.VersionRoot));
        }
        using (Fixture fixture = new()) {
            using CancellationTokenSource cancellation = new();
            var cancelled = await fixture.Backend(afterDownload: cancellation.Cancel).ImportAsync(new(fixture.Archive, fixture.VersionRoot, "fixture", new("windows", "x86_64", 8)), cancellation.Token);
            Equal(MmcInstanceBackendStatus.Cancelled, cancelled.Status); Equal(false, Directory.Exists(fixture.VersionRoot));
            Equal(true, File.Exists(Path.Combine(cancelled.WorkingRoot!, "PCL", "MmcImportPending.json")));
        }
    }
    private static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}, got {actual}."); }
    private sealed class Fixture : IDisposable {
        public string Scope { get; } = Path.Combine(Path.GetTempPath(), "pcllf-mmc-backend-" + Guid.NewGuid().ToString("N"));
        public string Archive => Path.Combine(Scope, "fixture.zip");
        public string VersionRoot => Path.Combine(Scope, "minecraft", "versions", "fixture");
        public string CacheRoot => Path.Combine(Scope, "cache");
        public int Downloads;
        private readonly List<HttpClient> clients = [];
        private static readonly byte[] AssetIndex = Encoding.UTF8.GetBytes("{\"objects\":{}}");
        public Fixture() {
            Directory.CreateDirectory(Scope);
            using var zip = ZipFile.Open(Archive, ZipArchiveMode.Create);
            Add(zip, "README.txt", "opaque outer documentation");
            zip.CreateEntry("pack/minecraft/");
            Add(zip, "pack/mmc-pack.json", "{\"formatVersion\":1,\"components\":[{\"uid\":\"net.minecraft\",\"version\":\"1.7.10\"}]}");
            Add(zip, "pack/instance.cfg", "name=Fixture\nOverrideJavaArgs=true\nJvmArgs=-Dfixture=yes\n");
            Add(zip, "pack/patches/net.minecraft.json", """{"formatVersion":1,"uid":"net.minecraft","version":"1.7.10","mainClass":"fixture.Game","mainJar":{"name":"fixture:game:1","MMC-hint":"local","MMC-filename":"fixture.jar"},"assetIndex":{"id":"fixture","url":"https://fixture.invalid/assets.json","sha1":"0000000000000000000000000000000000000000","size":1},"compatibleJavaMajors":[8]}"""
                .Replace(new string('0', 40), Convert.ToHexStringLower(SHA1.HashData(AssetIndex))).Replace("\"size\":1", "\"size\":" + AssetIndex.Length));
            Add(zip, "pack/libraries/fixture.jar", "launcher library"); Add(zip, "pack/libraries/unreferenced.jar", "unreferenced original");
            Add(zip, "pack/minecraft/libraries/fixture.jar", "different game library"); Add(zip, "pack/minecraft/config/example.cfg", "player config");
            Add(zip, "pack/PCL/config.json", "{\"foreign-player-pref\":\"must-not-import\"}");
        }
        private static void Add(ZipArchive zip, string path, string content) { using var output = new StreamWriter(zip.CreateEntry(path).Open(), new UTF8Encoding(false)); output.Write(content); }
        public MmcInstanceBackend<MmcArtifactDownloadSpec> Backend(bool offline = false, Action? afterDownload = null) {
            HttpClient client = new(new NoNetwork()); clients.Add(client);
            MmcMetadataStore metadata = new(Path.Combine(CacheRoot, "metadata"), new Uri("https://metadata.invalid/"), client);
            return new(metadata, new(Path.Combine(CacheRoot, "libraries"), Path.Combine(CacheRoot, "assets"), Path.Combine(CacheRoot, "sources")), value => value,
                async (batch, token) => {
                    if (offline) throw new IOException("Controlled offline fixture refused a download.");
                    foreach (var item in batch) {
                        if (!item.Urls.Contains("https://fixture.invalid/assets.json")) throw new Exception("Unexpected controlled download source.");
                        await File.WriteAllBytesAsync(item.DownloadPath, AssetIndex, token); Downloads++;
                        afterDownload?.Invoke();
                    }
                });
        }
        private sealed class NoNetwork : HttpMessageHandler {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => throw new IOException("No live metadata network in this fixture.");
        }
        public void Dispose() { foreach (var client in clients) client.Dispose(); InstanceConfigProviders.FlushAndDisposeWithin(Scope); if (Directory.Exists(Scope)) Directory.Delete(Scope, true); }
    }
}
