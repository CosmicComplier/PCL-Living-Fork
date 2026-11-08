using System.Collections.Immutable;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using PCLCS;
using PCLLF.MMCC.Core;

internal static class MmcInstanceCloneTests {
    internal static IEnumerable<(string Name, Action Run)> Cases() {
        yield return ("MMC clone snapshot copies current personal bytes and preserves both game roles with source-specific options", Snapshot);
    }
    private static void Snapshot() {
        using Fixture fixture = new();
        var source = fixture.Import(); var sourcePaths = source.Paths!;
        Put(sourcePaths.InstanceRoot, "minecraft/config/personal.cfg", "edited current config");
        Put(sourcePaths.InstanceRoot, "minecraft/saves/world/level.dat", "active world"); Put(sourcePaths.InstanceRoot, "minecraft/screenshots/one.png", "active screenshot");
        Put(sourcePaths.InstanceRoot, ".minecraft/saves/world/level.dat", "inactive world"); Put(sourcePaths.InstanceRoot, ".minecraft/screenshots/one.png", "inactive screenshot");
        Put(sourcePaths.InstanceRoot, ".minecraft/libraries/private.jar", "inactive game library"); Put(sourcePaths.InstanceRoot, ".minecraft/map-data/current.bin", "unknown personal neighbor");
        Put(sourcePaths.InstanceRoot, "minecraft/servers.dat", "active servers"); Put(sourcePaths.InstanceRoot, ".minecraft/servers.dat", "inactive servers");
        Put(sourcePaths.InstanceRoot, "PCL/MmcRuntime/do-not-copy/cache.bin", "derived garbage"); Put(sourcePaths.InstanceRoot, "accounts.json", "do not adopt account data");
        using var provider = new InstanceConfigProvider(sourcePaths.InstanceRoot);
        using var quiet = MmcConfigurationCoordinator.Quiesce(sourcePaths.InstanceRoot);
        MmcIniConfiguration.QueuePreferenceBatch(provider, new Dictionary<string, string?> { ["MmcJvmArgsMode"] = "Custom", ["VersionAdvanceJvm"] = "" });
        var reservation = new MmcInstanceCloneReservation(fixture.SourceRoot, fixture.TargetRoot, source.Definition!.Origin.PackageSha256);
        var targetPaths = MmcInstancePaths.Resolve(reservation.TemporaryRoot, source.Definition.Layout.GameRootLeaf);
        List<MmcAdmissionLease> leases = [];
        try {
            foreach (string root in new[] { sourcePaths.InstanceRoot, reservation.TemporaryRoot, reservation.FinalRoot }.Order(StringComparer.OrdinalIgnoreCase))
                leases.Add(MmcAdmissionGate.Acquire(root, root == sourcePaths.InstanceRoot ? source.Definition.InstanceId : reservation.InstanceId, TimeSpan.Zero));
            var inventory = MmcInstanceCloneSnapshot.Capture(sourcePaths, source.Definition, new(), CancellationToken.None);
            var prefs = MmcInstanceCloneSnapshot.Preferences(quiet, source.Definition.InstanceId);
            Equal("Custom", LegacyBaseOperations.ParseIni(prefs.Ini)["MmcJvmArgsMode"]); Equal("", LegacyBaseOperations.ParseIni(prefs.Ini)["VersionAdvanceJvm"]);
            Equal(false, inventory.Files.Any(file => file.RelativePath.Contains("/saves/", StringComparison.OrdinalIgnoreCase) || file.RelativePath.Contains("/screenshots/", StringComparison.OrdinalIgnoreCase) || file.RelativePath.EndsWith("/servers.dat", StringComparison.OrdinalIgnoreCase)));
            Equal(false, inventory.Files.Any(file => file.RelativePath.StartsWith("PCL/MmcRuntime/", StringComparison.OrdinalIgnoreCase) || file.RelativePath == "accounts.json" || file.RelativePath == "PCL/MmcImportPayload.json"));
            var manifest = new MmcClonePreparedManifest(1, reservation.InstanceId, source.Definition.InstanceId, source.Definition.Generation,
                MmcSavedSourceStore.ReadPrimary(sourcePaths).Sha256, prefs.Fingerprint, sourcePaths.InstanceRoot, reservation.FinalRoot, new(), inventory);
            reservation.Prepare(manifest, leases.Single(lease => lease.VersionRoot == reservation.TemporaryRoot));
            MmcInstanceCloneSnapshot.Copy(sourcePaths, targetPaths, inventory, CancellationToken.None, null, null);
            MmcInstanceCloneSnapshot.Verify(sourcePaths, inventory, source.Definition, new(), CancellationToken.None);
            MmcInstanceCloneSnapshot.VerifyCopied(targetPaths, inventory, CancellationToken.None);
            Equal("edited current config", File.ReadAllText(Path.Combine(targetPaths.GameRoot, "config", "personal.cfg")));
            Equal("inactive game library", File.ReadAllText(Path.Combine(targetPaths.InstanceRoot, ".minecraft", "libraries", "private.jar")));
            Equal("unknown personal neighbor", File.ReadAllText(Path.Combine(targetPaths.InstanceRoot, ".minecraft", "map-data", "current.bin")));
            var all = MmcInstanceCloneSnapshot.Capture(sourcePaths, source.Definition, new(true, true, true), CancellationToken.None);
            Equal(6, all.Files.Count(file => file.RelativePath.Contains("/saves/", StringComparison.OrdinalIgnoreCase) || file.RelativePath.Contains("/screenshots/", StringComparison.OrdinalIgnoreCase) || file.RelativePath.EndsWith("/servers.dat", StringComparison.OrdinalIgnoreCase)));
            bool denied = false; try { MmcInstanceCloneReservation.ValidateRoots(fixture.SourceRoot, Path.Combine(fixture.SourceRoot, "minecraft", "versions", "nested")); } catch (IOException) { denied = true; } Equal(true, denied);
            Equal(false, Directory.Exists(fixture.TargetRoot)); Equal(true, File.Exists(Path.Combine(reservation.TemporaryRoot, "PCL", "MmcImportPending.json")));
        } finally { for (int index = leases.Count - 1; index >= 0; index--) leases[index].Dispose(); }
    }
    private static void Put(string root, string relative, string value) { string path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)); Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, value); }
    private static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}, got {actual}."); }
    private sealed class Fixture : IDisposable {
        internal readonly string Root = Path.Combine(Path.GetTempPath(), "pcllf-mmc-clone-" + Guid.NewGuid().ToString("N"));
        internal string SourceRoot => Path.Combine(Root, "game", "versions", "source");
        internal string TargetRoot => Path.Combine(Root, "game", "versions", "clone");
        private readonly HttpClient client = new(new NoNetwork());
        internal Fixture() {
            Directory.CreateDirectory(Root); byte[] index = Encoding.UTF8.GetBytes("{\"objects\":{}}"); string sha1 = Convert.ToHexStringLower(SHA1.HashData(index));
            using var zip = ZipFile.Open(Path.Combine(Root, "fixture.zip"), ZipArchiveMode.Create);
            Add("pack/mmc-pack.json", """{"formatVersion":1,"components":[{"uid":"net.minecraft","version":"1.7.10"}]}""");
            Add("pack/instance.cfg", "name=Source\n");
            Add("pack/patches/net.minecraft.json", $$"""{"formatVersion":1,"uid":"net.minecraft","version":"1.7.10","mainClass":"fixture.Game","mainJar":{"name":"fixture:game:1","MMC-hint":"local","MMC-filename":"fixture.jar"},"assetIndex":{"id":"fixture","url":"https://fixture.invalid/index.json","sha1":"{{sha1}}","size":{{index.Length}}},"compatibleJavaMajors":[8]}""");
            Add("pack/libraries/fixture.jar", "active local bytes"); Add("pack/libraries/orphan.jar", "preserved local bytes"); Add("pack/minecraft/config/personal.cfg", "author config");
            Add("outer-doc.txt", "opaque original"); Add("pack/PCL/config.json", "{\"foreign\":true}");
            void Add(string name, string text) { using var writer = new StreamWriter(zip.CreateEntry(name).Open(), new UTF8Encoding(false)); writer.Write(text); }
        }
        internal MmcInstanceBackendResult Import() {
            var backend = new MmcInstanceBackend<MmcArtifactDownloadSpec>(new(Path.Combine(Root, "metadata"), new Uri("https://metadata.invalid/"), client),
                new(Path.Combine(Root, "global-libraries"), Path.Combine(Root, "global-assets"), Path.Combine(Root, "global-sources")), value => value,
                async (files, token) => { foreach (var file in files) await File.WriteAllTextAsync(file.DownloadPath, "{\"objects\":{}}", new UTF8Encoding(false), token); });
            var result = backend.ImportAsync(new(Path.Combine(Root, "fixture.zip"), SourceRoot, "source", new("windows", "x86_64", 8))).GetAwaiter().GetResult();
            if (result.Status != MmcInstanceBackendStatus.Succeeded) throw new Exception(result.Failure); return result;
        }
        public void Dispose() { client.Dispose(); InstanceConfigProviders.FlushAndDisposeWithin(Root); if (Directory.Exists(Root)) Directory.Delete(Root, true); }
    }
    private sealed class NoNetwork : HttpMessageHandler { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => throw new IOException("Clone fixture is offline."); }
}
