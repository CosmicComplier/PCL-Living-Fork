using System.Collections.Immutable;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using PCLCS;
using PCLLF.MMCC.Core;

internal static class MmcArtifactTests {
    internal static IEnumerable<(string Name, Action Run)> Cases() {
        yield return ("MMC artifacts stage owned copies without publishing shared execution paths", () => Run(SharedCopies));
        yield return ("MMC artifacts keep local launcher libraries separate from game libraries", () => Run(LocalScopes));
        yield return ("MMC artifacts preserve inactive sources without executing them", () => Run(InactiveSources));
        yield return ("MMC artifact offline repair restores locked original bytes", () => Run(OfflineRepair));
        yield return ("MMC artifact repair rejects unknown adoption and altered sources", () => Run(RepairRejects));
        yield return ("MMC artifact declared hashes sizes and copy budgets reject invalid bytes", () => Run(IntegrityAndBudgets));
        yield return ("MMC artifact cancellation cleans only its owned stage", () => Run(Cancellation));
        yield return ("MMC artifact source boundaries and runtime target collisions fail closed", () => Run(BoundariesAndCollisions));
        yield return ("MMC artifact view keys depend on content not absolute target paths", () => Run(ViewIdentity));
        yield return ("MMC artifact damaged originals require a staged repair transaction", () => Run(OriginalRepair));
        yield return ("MMC native jars extract into owned staged views with excludes and digests", () => Run(NativeExtraction));
        yield return ("MMC native jars reject traversal duplicate targets and links", () => Run(NativeUnsafe));
        yield return ("MMC native jars enforce entry declared and actual output budgets", () => Run(NativeBudgets));
        yield return ("MMC native collisions across jars reject different contents", () => Run(NativeCollisions));
    }

    private static void Run(Func<Task> action) => action().GetAwaiter().GetResult();
    private static async Task SharedCopies() {
        using var fixture = new Fixture();
        byte[] bytes = Encoding.UTF8.GetBytes("verified library");
        var artifact = Artifact("shared", "example/library/1/library.jar", bytes);
        fixture.Source(artifact.Key, bytes);
        using var prepared = await new MmcArtifactMaterializer().PrepareAsync(fixture.Request([artifact]), fixture.Access());
        var binding = prepared.OrderedBindings.Single();
        Equal(false, File.Exists(binding.TargetPath));
        Equal(true, binding.TargetPath.StartsWith(fixture.Paths.RuntimeRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal));
        Equal(false, binding.TargetPath.StartsWith(fixture.Cache, StringComparison.Ordinal));
        Equal(Hash(bytes), Hash(File.ReadAllBytes(binding.VerifiedInputPath)));
        fixture.Source(artifact.Key, Encoding.UTF8.GetBytes("ordinary launcher overwrites global cache"));
        Equal(Hash(bytes), Hash(File.ReadAllBytes(binding.VerifiedInputPath)));
        Equal(Hash(bytes), Hash(File.ReadAllBytes(fixture.Resolve(prepared.AdoptedLocks.Single().RecoveryObjectRelativePath))));
        Equal(true, prepared.StagedMutations.All(m => m.NewSourceRelativePath.StartsWith("PCL/MmcRuntime/.staging/", StringComparison.Ordinal)));
    }

    private static async Task LocalScopes() {
        using var fixture = new Fixture();
        byte[] bytes = Encoding.UTF8.GetBytes("launcher local");
        var artifact = Artifact("local", "early.jar", bytes, MmcArtifactScope.InstanceLocal);
        fixture.Write(Path.Combine(fixture.Paths.LocalLibraryRoot, "early.jar"), bytes);
        string gameFile = Path.Combine(fixture.Paths.GameRoot, "libraries", "early.jar");
        fixture.Write(gameFile, Encoding.UTF8.GetBytes("game data with identical filename"));
        using var prepared = await new MmcArtifactMaterializer().PrepareAsync(fixture.Request([artifact]));
        Equal(Path.Combine(fixture.Paths.LocalLibraryRoot, "early.jar"), prepared.OrderedBindings.Single().TargetPath);
        Equal(Hash(bytes), prepared.AdoptedLocks.Single().AdoptedSha256);
        Equal("game data with identical filename", File.ReadAllText(gameFile));
    }

    private static async Task InactiveSources() {
        using var fixture = new Fixture();
        byte[] active = [1, 2], inactive = [3, 4];
        var a = Artifact("active", "active.jar", active);
        var b = Artifact("inactive-variant", "inactive.jar", inactive, MmcArtifactScope.InstanceLocal);
        fixture.Source(a.Key, active); fixture.Source(b.Key, inactive);
        var request = fixture.Request([a]) with { AdditionalPreservedArtifacts = [b] };
        using var prepared = await new MmcArtifactMaterializer().PrepareAsync(request, fixture.Access());
        Equal(1, prepared.OrderedBindings.Length); Equal(2, prepared.AdoptedLocks.Length);
        var record = prepared.AdoptedLocks.Single(x => x.ArtifactKey == b.Key);
        Equal(Hash(inactive), Hash(File.ReadAllBytes(fixture.Resolve(record.RecoveryObjectRelativePath))));
        Equal(false, File.Exists(Path.Combine(fixture.Paths.LocalLibraryRoot, "inactive.jar")));
    }

    private static async Task OfflineRepair() {
        using var fixture = new Fixture();
        byte[] bytes = [10, 20, 30];
        var a = Artifact("local", "repair.jar", bytes, MmcArtifactScope.InstanceLocal);
        fixture.Source(a.Key, bytes);
        ImmutableArray<MmcArtifactLock> locks;
        using (var imported = await new MmcArtifactMaterializer().PrepareAsync(fixture.Request([a]), fixture.Access())) locks = imported.AdoptedLocks;
        fixture.ClearSources();
        using var repaired = await new MmcArtifactMaterializer().PrepareAsync(fixture.Request([a], locks, MmcArtifactIntent.RepairExisting));
        Equal(1, repaired.StagedMutations.Length);
        Equal("libraries/repair.jar", repaired.StagedMutations[0].TargetRelativePath);
        Equal(Hash(bytes), Hash(File.ReadAllBytes(repaired.StagedMutations[0].StagedPath)));
        Equal(false, File.Exists(Path.Combine(fixture.Paths.LocalLibraryRoot, "repair.jar")));
        Equal(locks.Single().AdoptedSha256, repaired.AdoptedLocks.Single().AdoptedSha256);
    }

    private static async Task RepairRejects() {
        using var fixture = new Fixture();
        byte[] bytes = [1, 2, 3];
        var a = Artifact("library", "a.jar", bytes) with { Sha256 = null, Sha1 = null, Size = null };
        fixture.Source(a.Key, bytes);
        ImmutableArray<MmcArtifactLock> locks;
        using (var adopted = await new MmcArtifactMaterializer().PrepareAsync(fixture.Request([a]), fixture.Access())) locks = adopted.AdoptedLocks;
        await Throws<IOException>(() => new MmcArtifactMaterializer().PrepareAsync(fixture.Request([a], [], MmcArtifactIntent.RepairExisting), fixture.Access()));
        fixture.Write(fixture.Resolve(locks[0].RecoveryObjectRelativePath), [9, 9, 9]);
        fixture.Source(a.Key, [9, 9, 9]);
        await Throws<IOException>(() => new MmcArtifactMaterializer().PrepareAsync(fixture.Request([a], locks, MmcArtifactIntent.RepairExisting), fixture.Access()));
        Equal(Hash(bytes), locks[0].AdoptedSha256);
        Equal(0, fixture.ActiveStages());
    }

    private static async Task IntegrityAndBudgets() {
        using var fixture = new Fixture();
        byte[] bytes = [1, 2, 3];
        var a = Artifact("bad", "a.jar", bytes);
        fixture.Source(a.Key, [4, 5, 6]);
        await Throws<IOException>(() => new MmcArtifactMaterializer().PrepareAsync(fixture.Request([a]), fixture.Access()));
        fixture.Source(a.Key, bytes);
        await Throws<IOException>(() => new MmcArtifactMaterializer(new(MaxArtifactBytes: 2)).PrepareAsync(fixture.Request([a]), fixture.Access()));
        await Throws<IOException>(() => new MmcArtifactMaterializer().PrepareAsync(fixture.Request([a with { Size = 4 }]), fixture.Access()));
        Equal(0, fixture.ActiveStages());
    }

    private static async Task Cancellation() {
        using var fixture = new Fixture();
        byte[] bytes = new byte[1024 * 1024];
        var a = Artifact("cancel", "large.jar", bytes);
        fixture.Source(a.Key, bytes);
        string sibling = Path.Combine(fixture.Paths.RuntimeRoot, "preserve-view", "keep"); fixture.Write(sibling, [7]);
        using var cancel = new CancellationTokenSource();
        await Throws<OperationCanceledException>(() => new MmcArtifactMaterializer().PrepareAsync(fixture.Request([a]), fixture.Access(), cancel.Token,
            new InlineProgress(_ => cancel.Cancel())));
        Equal(0, fixture.ActiveStages()); Equal(true, File.Exists(sibling)); Equal(true, File.Exists(fixture.Sources[a.Key]));
    }

    private static async Task BoundariesAndCollisions() {
        using var fixture = new Fixture();
        var a = Artifact("a", "same.jar", [1]); var b = Artifact("b", "same.jar", [2]);
        fixture.Source(a.Key, [1]); fixture.Source(b.Key, [2]);
        await Throws<IOException>(() => new MmcArtifactMaterializer().PrepareAsync(fixture.Request([a, b]), fixture.Access()));
        var wrongBoundary = new MmcArtifactSourceAccess((reference, token) => ValueTask.FromResult<MmcArtifactSource?>(new(fixture.Root, fixture.Sources[reference.Key])));
        await Throws<IOException>(() => new MmcArtifactMaterializer().PrepareAsync(fixture.Request([a]), wrongBoundary));
        await Throws<IOException>(() => new MmcArtifactMaterializer().PrepareAsync(fixture.Request([a with { RelativePath = "../escape.jar" }]), fixture.Access()));
        Equal(0, fixture.ActiveStages());
    }

    private static async Task ViewIdentity() {
        using var first = new Fixture(); using var second = new Fixture();
        var a = Artifact("stable", "same.jar", [1, 2]);
        first.Source(a.Key, [1, 2]); second.Source(a.Key, [1, 2]);
        using var p = await new MmcArtifactMaterializer().PrepareAsync(first.Request([a]), first.Access());
        using var q = await new MmcArtifactMaterializer().PrepareAsync(second.Request([a]), second.Access());
        Equal(p.ViewKey, q.ViewKey);
        Equal(false, p.OrderedBindings[0].TargetPath == q.OrderedBindings[0].TargetPath);
        using var changed = await new MmcArtifactMaterializer().PrepareAsync(first.Request([a]) with { LogicalContext = "changed-java-or-compiler" }, first.Access());
        Equal(false, changed.ViewKey == p.ViewKey);
    }

    private static async Task OriginalRepair() {
        using var fixture = new Fixture();
        byte[] bytes = [1, 2, 3]; var a = Artifact("repair-object", "a.jar", bytes);
        fixture.Source(a.Key, bytes);
        ImmutableArray<MmcArtifactLock> locks;
        using (var initial = await new MmcArtifactMaterializer().PrepareAsync(fixture.Request([a]), fixture.Access())) locks = initial.AdoptedLocks;
        string original = fixture.Resolve(locks[0].RecoveryObjectRelativePath);
        fixture.Write(original, [9, 9, 9]);
        using var repaired = await new MmcArtifactMaterializer().PrepareAsync(fixture.Request([a], locks, MmcArtifactIntent.RepairExisting), fixture.Access());
        Equal(Hash([9, 9, 9]), Hash(File.ReadAllBytes(original)));
        var mutation = repaired.StagedMutations.Single(m => m.TargetRelativePath == locks[0].RecoveryObjectRelativePath);
        Equal(Hash(bytes), Hash(File.ReadAllBytes(mutation.StagedPath)));
        Equal(Hash([9, 9, 9]), mutation.ExpectedExistingSha256);
    }

    private static async Task NativeExtraction() {
        using var fixture = new Fixture();
        byte[] jar = Jar(("META-INF/notice", [9]), ("x64/example.dll", [1, 2, 3]));
        var a = Artifact("native", "example/native.jar", jar) with { Role = MmcArtifactRole.Native, NativeClassifier = "windows-64", ExtractExcludes = ["META-INF/"] };
        fixture.Source(a.Key, jar);
        using var prepared = await new MmcArtifactMaterializer().PrepareAsync(fixture.Request([a]), fixture.Access());
        Equal(true, prepared.NativeExtractionComplete); Equal(1, prepared.NativeFiles.Length);
        var native = prepared.NativeFiles.Single();
        Equal("x64/example.dll", native.RelativePath); Equal(Hash([1, 2, 3]), native.Sha256);
        Equal(false, File.Exists(native.TargetPath));
        var mutation = prepared.StagedMutations.Single(x => x.TargetRelativePath.EndsWith("/natives/x64/example.dll", StringComparison.Ordinal));
        Equal(Hash([1, 2, 3]), Hash(File.ReadAllBytes(mutation.StagedPath)));
        Equal(true, prepared.NativeDirectory!.StartsWith(fixture.Paths.RuntimeRoot, StringComparison.Ordinal));
    }

    private static async Task NativeUnsafe() {
        foreach (var entries in new[] {
            new[] { ("../escape.dll", new byte[] { 1 }) },
            new[] { ("META-INF/../../escape.dll", new byte[] { 1 }) },
            new[] { ("a.dll", new byte[] { 1 }), ("A.dll", new byte[] { 1 }) },
            new[] { ("folder", new byte[] { 1 }), ("folder/a.dll", new byte[] { 2 }) }
        }) {
            using var fixture = new Fixture(); byte[] jar = Jar(entries);
            var a = Artifact("unsafe-native", "native.jar", jar) with { Role = MmcArtifactRole.Native, ExtractExcludes = ["META-INF/"] };
            fixture.Source(a.Key, jar);
            await Throws<IOException>(() => new MmcArtifactMaterializer().PrepareAsync(fixture.Request([a]), fixture.Access()));
            Equal(0, fixture.ActiveStages());
        }
        using var linkFixture = new Fixture();
        byte[] linked;
        using (var bytes = new MemoryStream()) {
            using (var zip = new ZipArchive(bytes, ZipArchiveMode.Create, true)) {
                var entry = zip.CreateEntry("link.dll"); entry.ExternalAttributes = unchecked((int)(0xa1ffU << 16));
                using var content = entry.Open(); content.Write([1]);
            }
            linked = bytes.ToArray();
        }
        var linkArtifact = Artifact("link-native", "native.jar", linked) with { Role = MmcArtifactRole.Native };
        linkFixture.Source(linkArtifact.Key, linked);
        await Throws<IOException>(() => new MmcArtifactMaterializer().PrepareAsync(linkFixture.Request([linkArtifact]), linkFixture.Access()));
    }

    private static async Task NativeBudgets() {
        using var fixture = new Fixture(); byte[] jar = Jar(("one.dll", [1, 2]), ("two.dll", [3, 4]));
        var a = Artifact("bounded-native", "native.jar", jar) with { Role = MmcArtifactRole.Native };
        fixture.Source(a.Key, jar);
        await Throws<IOException>(() => new MmcArtifactMaterializer(new(MaxNativeEntries: 1)).PrepareAsync(fixture.Request([a]), fixture.Access()));
        await Throws<IOException>(() => new MmcArtifactMaterializer(new(MaxNativeBytes: 3)).PrepareAsync(fixture.Request([a]), fixture.Access()));
        byte[] malformed = Jar(("bomb.dll", new byte[1024]));
        int central = FindSignature(malformed, [0x50, 0x4b, 0x01, 0x02]);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(malformed.AsSpan(central + 24, 4), 1);
        var bomb = Artifact("actual-output-native", "bomb.jar", malformed) with { Role = MmcArtifactRole.Native };
        fixture.Source(bomb.Key, malformed);
        await Throws<IOException>(() => new MmcArtifactMaterializer(new(MaxNativeBytes: 16)).PrepareAsync(fixture.Request([bomb]), fixture.Access()));
        Equal(0, fixture.ActiveStages());
    }

    private static async Task NativeCollisions() {
        using var fixture = new Fixture();
        byte[] left = Jar(("same.dll", [1])), right = Jar(("same.dll", [2]));
        var a = Artifact("native-a", "a.jar", left) with { Role = MmcArtifactRole.Native };
        var b = Artifact("native-b", "b.jar", right) with { Role = MmcArtifactRole.Native };
        fixture.Source(a.Key, left); fixture.Source(b.Key, right);
        await Throws<IOException>(() => new MmcArtifactMaterializer().PrepareAsync(fixture.Request([a, b]), fixture.Access()));
        Equal(0, fixture.ActiveStages());
    }

    private static byte[] Jar(params (string Name, byte[] Content)[] entries) {
        using var bytes = new MemoryStream();
        using (var zip = new ZipArchive(bytes, ZipArchiveMode.Create, true)) {
            foreach (var item in entries) { var entry = zip.CreateEntry(item.Name); using var content = entry.Open(); content.Write(item.Content); }
        }
        return bytes.ToArray();
    }
    private static int FindSignature(byte[] bytes, byte[] signature) {
        for (int index = 0; index <= bytes.Length - signature.Length; index++) if (bytes.AsSpan(index, signature.Length).SequenceEqual(signature)) return index;
        throw new Exception("Fixture ZIP signature missing.");
    }

    private static MmcArtifactReference Artifact(string key, string relative, byte[] bytes, MmcArtifactScope scope = MmcArtifactScope.Shared) =>
        new(key, "example.component", "revision-1", MmcArtifactRole.Classpath, scope, null, relative,
            scope == MmcArtifactScope.Shared ? ["https://example.invalid/" + key] : [],
            Convert.ToHexStringLower(SHA1.HashData(bytes)), Hash(bytes), bytes.Length, null, [],
            new("example.component", "1", "self-authored-fixture", "source-hash", "libraries", 0));
    private static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
    private static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}, got {actual}."); }
    private static async Task Throws<T>(Func<Task> action) where T : Exception {
        try { await action(); } catch (T) { return; }
        throw new Exception("Expected " + typeof(T).Name);
    }
    private sealed class InlineProgress(Action<MmcArtifactProgress> callback) : IProgress<MmcArtifactProgress> { public void Report(MmcArtifactProgress value) => callback(value); }
    private sealed class Fixture : IDisposable {
        public string Scope { get; } = Path.Combine(Path.GetTempPath(), "pcllf-mmc-artifacts-" + Guid.NewGuid().ToString("N"));
        public string Root { get; }
        public string Cache { get; }
        public MmcInstancePaths Paths { get; }
        public Dictionary<string, string> Sources { get; } = new(StringComparer.Ordinal);
        public Fixture() {
            Root = Path.Combine(Scope, "versions", "sample"); Cache = Path.Combine(Scope, "cache");
            Directory.CreateDirectory(Root); Directory.CreateDirectory(Cache); Paths = MmcInstancePaths.Resolve(Root);
        }
        public void Source(string key, byte[] bytes) { string path = Path.Combine(Cache, Hash(Encoding.UTF8.GetBytes(key))); Write(path, bytes); Sources[key] = path; }
        public void Write(string path, byte[] bytes) { Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllBytes(path, bytes); }
        public string Resolve(string relative) => Paths.ResolveRelative(MmcPathRole.Instance, relative);
        public MmcArtifactRequest Request(ImmutableArray<MmcArtifactReference> artifacts, ImmutableArray<MmcArtifactLock> locks = default,
            MmcArtifactIntent intent = MmcArtifactIntent.AdoptDeclaredVariant) => new(Paths, "compiler-context-v1", artifacts, [], locks, intent);
        public MmcArtifactSourceAccess Access() => new((artifact, token) => ValueTask.FromResult<MmcArtifactSource?>(Sources.TryGetValue(artifact.Key, out string? path) ? new(Cache, path) : null));
        public void ClearSources() { foreach (string path in Sources.Values) File.Delete(path); Sources.Clear(); }
        public int ActiveStages() { string parent = Path.Combine(Paths.RuntimeRoot, ".staging"); return Directory.Exists(parent) ? Directory.GetDirectories(parent).Length : 0; }
        public void Dispose() { if (Directory.Exists(Scope)) Directory.Delete(Scope, true); }
    }
}
