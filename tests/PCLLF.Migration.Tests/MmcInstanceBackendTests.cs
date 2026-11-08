using System.Collections.Immutable;
using System.IO.Compression;
using System.Net;
using System.Text;
using PCLCS;
using PCLLF.MMCC.Core;

internal static class MmcInstanceBackendTests {
    internal static IEnumerable<(string Name, Action Run)> Cases() {
        yield return ("MMC import preparation measures the full archive and keeps opaque docs outside live settings", SourcePreparation);
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
    private static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}, got {actual}."); }
    private sealed class Fixture : IDisposable {
        public string Scope { get; } = Path.Combine(Path.GetTempPath(), "pcllf-mmc-backend-" + Guid.NewGuid().ToString("N"));
        public string Archive => Path.Combine(Scope, "fixture.zip");
        public string VersionRoot => Path.Combine(Scope, "minecraft", "versions", "fixture");
        public Fixture() {
            Directory.CreateDirectory(Scope);
            using var zip = ZipFile.Open(Archive, ZipArchiveMode.Create);
            Add(zip, "README.txt", "opaque outer documentation");
            zip.CreateEntry("pack/minecraft/");
            Add(zip, "pack/mmc-pack.json", "{\"formatVersion\":1,\"components\":[{\"uid\":\"net.minecraft\",\"version\":\"1.7.10\"}]}");
            Add(zip, "pack/instance.cfg", "name=Fixture\nOverrideJavaArgs=true\nJvmArgs=-Dfixture=yes\n");
            Add(zip, "pack/patches/net.minecraft.json", """{"formatVersion":1,"uid":"net.minecraft","version":"1.7.10","mainClass":"fixture.Game","mainJar":{"name":"fixture:game:1","MMC-hint":"local","MMC-filename":"fixture.jar"},"assetIndex":{"id":"fixture","url":"https://fixture.invalid/assets.json","sha1":"0000000000000000000000000000000000000000","size":1},"compatibleJavaMajors":[8]}""");
            Add(zip, "pack/libraries/fixture.jar", "launcher library"); Add(zip, "pack/libraries/unreferenced.jar", "unreferenced original");
            Add(zip, "pack/minecraft/libraries/fixture.jar", "different game library"); Add(zip, "pack/minecraft/config/example.cfg", "player config");
            Add(zip, "pack/PCL/config.json", "{\"foreign-player-pref\":\"must-not-import\"}");
        }
        private static void Add(ZipArchive zip, string path, string content) { using var output = new StreamWriter(zip.CreateEntry(path).Open(), new UTF8Encoding(false)); output.Write(content); }
        public void Dispose() { InstanceConfigProviders.FlushAndDisposeWithin(Scope); if (Directory.Exists(Scope)) Directory.Delete(Scope, true); }
    }
}
