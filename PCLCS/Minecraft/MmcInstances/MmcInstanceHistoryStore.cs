using System.Collections.Immutable;
using System.Text;
using PCLLF.MMCC.Core;

namespace PCLCS;

// Original LF host implementation; governed by the repository LICENCE.
internal static class MmcInstanceHistoryStore {
    internal const int CoverageVersion = 3;
    private const string Store = "PCL/MmcHistory/";
    private static string Id(Guid id) => id != Guid.Empty ? id.ToString("N") : throw new IOException("History point identity is empty.");
    internal static string PointRoot(MmcInstancePaths paths, Guid id) => paths.ResolveRelative(MmcPathRole.Instance, Store + "points/" + Id(id));
    private static string IndexPath(MmcInstancePaths paths, Guid id) => paths.ResolveRelative(MmcPathRole.Instance, Store + "index/" + Id(id) + ".record");
    private static MmcInstancePaths SourcePaths(string pointRoot, string leaf) => MmcInstancePaths.Resolve(Path.Combine(pointRoot, "versions", "snapshot"), leaf);

    internal static MmcInstanceHistoryPoint Capture(MmcAdmissionLease lease, MmcStoredPrimaryDefinition primary,
        MmcSavedInstanceSources saved, string label, CancellationToken token, Action<MmcInstanceHistoryCheckpoint>? checkpoint) {
        lease.AssertHeld(); ValidateLabel(label); var paths = MmcInstancePaths.Resolve(lease.VersionRoot, primary.Definition.Layout.GameRootLeaf);
        var source = MmcInstanceHistoryFiles.Sources(paths, primary, saved, token);
        var environment = MmcInstanceHistoryFiles.Environment(paths, primary.Definition, token);
        if (source.Length + environment.Length > MmcInstanceHistoryFiles.MaximumFiles || source.Concat(environment).Sum(file => file.SizeBytes) > MmcInstanceHistoryFiles.MaximumBytes)
            throw new IOException("Combined history snapshot exceeds its budget.");
        // The exclusive GUID directory is private until its index reference is published.
        // No directory rename is needed while Windows/indexers may still observe copied files.
        Guid id = Guid.NewGuid(); string root = PointRoot(paths, id);
        MmcArtifactIo.Directory(Path.GetDirectoryName(root)!); MmcOwnedImportReservation.CreateExclusiveDirectory(root);
        MmcDurableFiles.WriteNew(Path.Combine(root, "owner.txt"), Encoding.UTF8.GetBytes("PCLLF.MmcHistory/v1\n" + id.ToString("N") + "\n" + primary.Definition.InstanceId.ToString("D")));
        var target = SourcePaths(root, paths.GameRootLeaf); MmcArtifactIo.Directory(target.InstanceRoot);
        MmcInstanceHistoryFiles.Copy(paths, target, source.Concat(environment), token);
        MmcInstanceHistoryFiles.Verify(target, source.Concat(environment), token);
        checkpoint?.Invoke(MmcInstanceHistoryCheckpoint.SourcesCaptured);
        // Final source namespace and byte checks happen before any visible index is published.
        if (!source.SequenceEqual(MmcInstanceHistoryFiles.Sources(paths, primary, saved, token)) ||
            !environment.SequenceEqual(MmcInstanceHistoryFiles.Environment(paths, primary.Definition, token)))
            throw new IOException("The history source changed during capture; the private point was preserved.");
        AssertCurrent(lease, primary.Definition.InstanceId, primary.Definition.Generation, primary.Sha256);
        var manifest = new MmcHistoryManifest(1, CoverageVersion, id, primary.Definition.InstanceId, primary.Definition.Generation,
            primary.Sha256, label, DateTime.UtcNow, paths.GameRootLeaf, source, environment);
        string digest = MmcDurableFiles.WriteRecord(Path.Combine(root, "manifest.record"), manifest);
        token.ThrowIfCancellationRequested();
        string index = IndexPath(paths, id); MmcArtifactIo.Directory(Path.GetDirectoryName(index)!);
        string temporary = index + ".pending-" + Guid.NewGuid().ToString("N");
        MmcDurableFiles.WriteRecord(temporary, new MmcHistoryIndex(1, id, primary.Definition.InstanceId, digest));
        File.Move(temporary, index, false);
        var loaded = Read(paths, id, digest, CancellationToken.None);
        checkpoint?.Invoke(MmcInstanceHistoryCheckpoint.PointPublished);
        return loaded.Point;
    }

    internal static MmcHistoryLoadedPoint Read(MmcInstancePaths paths, Guid id, string? expectedManifestHash, CancellationToken token) {
        var (manifest, hash, source) = Header(paths, id, expectedManifestHash);
        MmcInstanceHistoryFiles.Verify(source, manifest.SourceFiles.Concat(manifest.EnvironmentFiles), token);
        var primary = MmcSavedSourceStore.ReadPrimary(source);
        if (primary.Sha256 != manifest.PrimarySha256 || primary.Definition.InstanceId != manifest.InstanceId || primary.Definition.Generation != manifest.Generation ||
            primary.Definition.Layout.GameRootLeaf != manifest.GameRootLeaf)
            throw new IOException("History definition differs from its fixed snapshot identity.");
        var saved = MmcSavedSourceStore.LoadAsync(source, primary.Definition, token).GetAwaiter().GetResult();
        if (!manifest.SourceFiles.SequenceEqual(MmcInstanceHistoryFiles.Sources(source, primary, saved, token)))
            throw new IOException("History omits or adds immutable source authority.");
        return new(Describe(manifest, hash), manifest, source, primary.Definition, saved);
    }
    private static (MmcHistoryManifest Manifest, string Hash, MmcInstancePaths Source) Header(MmcInstancePaths paths, Guid id, string? expectedManifestHash) {
        var index = MmcDurableFiles.ReadRecord<MmcHistoryIndex>(IndexPath(paths, id)).Value;
        if (index.SchemaVersion != 1 || index.PointId != id || index.InstanceId == Guid.Empty || !MmcDurableFiles.IsHash(index.ManifestSha256) ||
            expectedManifestHash is not null && index.ManifestSha256 != expectedManifestHash)
            throw new IOException("The selected history index/reference is stale, damaged or unsupported.");
        string root = PointRoot(paths, id);
        var (manifest, hash) = MmcDurableFiles.ReadRecord<MmcHistoryManifest>(Path.Combine(root, "manifest.record"));
        if (hash != index.ManifestSha256 || manifest.SchemaVersion != 1 || manifest.CoverageVersion != CoverageVersion || manifest.PointId != id ||
            manifest.InstanceId != index.InstanceId || manifest.Generation < 1 || !MmcDurableFiles.IsHash(manifest.PrimarySha256) ||
            manifest.GameRootLeaf is not ("minecraft" or ".minecraft") || manifest.SourceFiles.IsDefault || manifest.EnvironmentFiles.IsDefault ||
            manifest.CreatedUtc.Kind != DateTimeKind.Utc || manifest.SourceFiles.Length + manifest.EnvironmentFiles.Length > MmcInstanceHistoryFiles.MaximumFiles)
            throw new IOException("History coverage or its hash-bound manifest is unsupported or damaged.");
        ValidateLabel(manifest.Label);
        var source = SourcePaths(root, manifest.GameRootLeaf);
        if (manifest.EnvironmentFiles.Any(file => file is null || !MmcInstanceHistoryFiles.IsEnvironment(source, file.RelativePath)))
            throw new IOException("History contains player data or settings outside its declared environment coverage.");
        long bytes = 0; HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        foreach (var file in manifest.SourceFiles.Concat(manifest.EnvironmentFiles)) {
            if (file is null || !MmcDurableFiles.IsHash(file.Sha256) || file.SizeBytes < 0 || file.SizeBytes > MmcDurableFiles.MaximumFileBytes || !seen.Add(file.RelativePath) ||
                (bytes = checked(bytes + file.SizeBytes)) > MmcInstanceHistoryFiles.MaximumBytes)
                throw new IOException("History file inventory shape or byte budget is invalid.");
            source.ResolveRelative(MmcPathRole.Instance, file.RelativePath);
        }
        return (manifest, hash, source);
    }
    private static MmcInstanceHistoryPoint Describe(MmcHistoryManifest manifest, string hash) => new(manifest.PointId, hash, manifest.InstanceId,
        manifest.Generation, manifest.PrimarySha256, manifest.CoverageVersion, manifest.Label, manifest.CreatedUtc, manifest.GameRootLeaf,
        manifest.SourceFiles.Length + manifest.EnvironmentFiles.Length, manifest.SourceFiles.Concat(manifest.EnvironmentFiles).Sum(file => file.SizeBytes));

    internal static ImmutableArray<MmcInstanceHistoryPoint> List(MmcInstancePaths paths, CancellationToken token) {
        token.ThrowIfCancellationRequested();
        string directory = paths.ResolveRelative(MmcPathRole.Instance, Store + "index");
        if (!Directory.Exists(directory)) return [];
        var points = ImmutableArray.CreateBuilder<MmcInstanceHistoryPoint>();
        foreach (string path in Directory.EnumerateFiles(directory, "*.record")) {
            token.ThrowIfCancellationRequested();
            if (!Guid.TryParseExact(Path.GetFileNameWithoutExtension(path), "N", out var id) || points.Count >= 10000)
                throw new IOException("History index identity/count is invalid.");
            // Browsing validates the trusted index and bounded manifest, without rehashing every
            // multi-gigabyte point. Capture/restore perform the full object and source proof.
            var (manifest, hash, _) = Header(paths, id, null); points.Add(Describe(manifest, hash));
        }
        return points.OrderByDescending(point => point.CreatedUtc).ToImmutableArray();
    }
    internal static MmcStoredPrimaryDefinition AssertCurrent(MmcAdmissionLease lease, Guid id, long generation, string hash) {
        lease.AssertHeld(); MmcFileTransactions.AssertNoPending(lease); MmcSessions.AssertNoActive(lease);
        var paths = MmcInstancePaths.Resolve(lease.VersionRoot); var current = MmcSavedSourceStore.ReadPrimary(paths);
        var management = MmcManagementProbe.Read(lease.VersionRoot);
        if (id == Guid.Empty || generation < 1 || !MmcDurableFiles.IsHash(hash) || current.Definition.InstanceId != id || current.Definition.Generation != generation ||
            current.Sha256 != hash || management.State != MmcManagementState.ManagedMmc || management.Envelope?.InstanceId != id || management.Envelope.Generation != generation ||
            MmcDurableFiles.Observe(paths.ResolveRelative(MmcPathRole.Instance, Path.GetFileName(paths.InstanceRoot) + ".json")) != current.Definition.InstalledProjection!.OutputJsonSha256)
            throw new IOException("History needs the exact current healthy publication and no active session or pending operation.");
        return current;
    }
    private static void ValidateLabel(string label) {
        if (label is null || label.Length > 256 || label.Any(char.IsControl)) throw new IOException("History label is invalid or exceeds its budget.");
    }
}
