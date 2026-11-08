using System.Collections.Immutable;
using System.Security.Cryptography;

namespace PCLCS;

// Original LF host implementation, under the repository LICENCE.
public enum MmcPackageFileRole { Metadata, Game, LocalLibrary, InstanceExtra, ForeignLauncherMetadata }
public sealed record MmcPackageFileObservation(string RelativePath, MmcPackageFileRole Role,
    string Sha256, string Sha1, long SizeBytes, uint Crc32, string? StagedPath);

/// <summary>Fixed original package bytes and staged initial-import data; never writes an existing player file.</summary>
public sealed class MmcPreparedPackageFiles : IDisposable {
    private readonly MmcArtifactWorkspace workspace;
    public string StageRoot => workspace.Root;
    public ImmutableArray<MmcPackageFileObservation> Files { get; }
    public ImmutableArray<MmcFileMutation> InitialGameMutations { get; }
    public IReadOnlyDictionary<string, MmcArtifactSource> LocalLibrarySources { get; }

    internal MmcPreparedPackageFiles(MmcArtifactWorkspace workspace, ImmutableArray<MmcPackageFileObservation> files,
        ImmutableArray<MmcFileMutation> mutations, Dictionary<string, MmcArtifactSource> libraries) {
        this.workspace = workspace; Files = files; InitialGameMutations = mutations;
        LocalLibrarySources = new System.Collections.ObjectModel.ReadOnlyDictionary<string, MmcArtifactSource>(libraries);
    }
    public void Dispose() => workspace.Dispose();
}

public static class MmcPackageFileStager {
    /// <summary>Caller must reserve a new owned instance root before preparing an initial import.</summary>
    public static async Task<MmcPreparedPackageFiles> PrepareInitialAsync(MmcArchiveSource source,
        MmcInstancePaths paths, CancellationToken cancellationToken = default,
        IProgress<MmcArtifactProgress>? progress = null) {
        ArgumentNullException.ThrowIfNull(source); ArgumentNullException.ThrowIfNull(paths);
        if (paths.GameRootLeaf != source.Package.GameRootLeaf) throw new InvalidDataException("Initial package and target game-root layouts differ.");
        MmcArtifactWorkspace workspace = new(paths);
        try {
            var files = ImmutableArray.CreateBuilder<MmcPackageFileObservation>();
            var mutations = ImmutableArray.CreateBuilder<MmcFileMutation>();
            Dictionary<string, MmcArtifactSource> libraries = new(StringComparer.OrdinalIgnoreCase);
            var rawMetadata = source.Metadata.ToDictionary(m => m.RelativePath, StringComparer.OrdinalIgnoreCase);
            MmcArtifactIo io = new(new(), progress);
            foreach (MmcArchiveFile file in source.Files) {
                cancellationToken.ThrowIfCancellationRequested();
                MmcPackageFileRole role = Role(file.RelativePath, source.Package.GameRootLeaf);
                if (rawMetadata.TryGetValue(file.RelativePath, out var metadata)) {
                    files.Add(new(file.RelativePath, MmcPackageFileRole.Metadata, metadata.Sha256,
                        Convert.ToHexStringLower(SHA1.HashData(metadata.Bytes.AsSpan())), file.SizeBytes, file.Crc32, null));
                    continue;
                }
                string staged = workspace.NewFile();
                await using Stream input = await source.OpenFileAsync(file.RelativePath, cancellationToken).ConfigureAwait(false);
                var observed = await io.ExtractAsync(input, staged, file.RelativePath, file.SizeBytes, file.Crc32,
                    4L * 1024 * 1024 * 1024, cancellationToken, "package-extract").ConfigureAwait(false);
                files.Add(new(file.RelativePath, role, observed.Sha256, observed.Sha1, observed.Size, file.Crc32, staged));
                if (role == MmcPackageFileRole.LocalLibrary) {
                    libraries.Add(file.RelativePath["libraries/".Length..], new(workspace.Root, staged));
                } else if (role != MmcPackageFileRole.ForeignLauncherMetadata) {
                    string target = paths.ResolveRelative(MmcPathRole.Instance, file.RelativePath);
                    if (MmcInstancePaths.Attributes(target) is not null)
                        throw new IOException("Initial import target already exists; preserving existing player data: " + file.RelativePath);
                    mutations.Add(new(file.RelativePath, null, null, observed.Sha256,
                        Path.GetRelativePath(paths.InstanceRoot, staged).Replace('\\', '/')));
                }
            }
            return new(workspace, files.ToImmutable(), mutations.ToImmutable(), libraries);
        } catch { workspace.Dispose(); throw; }
    }

    private static MmcPackageFileRole Role(string relative, string gameRoot) {
        if (relative.StartsWith("libraries/", StringComparison.OrdinalIgnoreCase)) return MmcPackageFileRole.LocalLibrary;
        if (relative.StartsWith("PCL/", StringComparison.OrdinalIgnoreCase)) return MmcPackageFileRole.ForeignLauncherMetadata;
        if (relative.StartsWith(gameRoot + "/", StringComparison.OrdinalIgnoreCase)) return MmcPackageFileRole.Game;
        return MmcPackageFileRole.InstanceExtra;
    }
}
