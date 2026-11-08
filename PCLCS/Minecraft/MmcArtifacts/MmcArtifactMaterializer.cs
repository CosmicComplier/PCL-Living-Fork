using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;
using System.Threading;
using System.Threading.Tasks;
using PCLLF.MMCC.Core;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace PCLCS;

// Original LF host implementation; governed by the repository LICENCE.
/// <summary>
/// Builds verified original objects and an owned staging plan. It never publishes local libraries,
/// runtime views or managed definitions. The caller must commit through the instance transaction
/// executor, under its structural/session gate, then revalidate actual inputs before launch.
/// </summary>
public sealed partial class MmcArtifactMaterializer {
    private readonly MmcArtifactLimits limits;
    public MmcArtifactMaterializer(MmcArtifactLimits? limits = null) {
        this.limits = limits ?? new();
        if (this.limits.MaxArtifactBytes <= 0 || this.limits.MaxCopiedBytes <= 0 || this.limits.MaxArtifacts <= 0 ||
            this.limits.MaxNativeEntries <= 0 || this.limits.MaxNativeBytes <= 0 || this.limits.MaxAssetObjects <= 0 || this.limits.MaxAssetIndexBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(limits));
    }

    public async Task<MmcPreparedArtifacts> PrepareAsync(MmcArtifactRequest request, MmcArtifactSourceAccess? access = null,
        CancellationToken cancellationToken = default, IProgress<MmcArtifactProgress>? progress = null) {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrEmpty(request.LogicalContext) || request.LogicalContext.Length > 4096) throw new ArgumentException("A bounded logical compiler/runtime context is required.");
        cancellationToken.ThrowIfCancellationRequested();
        access ??= new();
        MmcInstancePaths paths = MmcInstancePaths.Resolve(request.Paths.InstanceRoot, request.Paths.GameRootLeaf);
        var artifacts = request.Artifacts.IsDefault ? ImmutableArray<MmcArtifactReference>.Empty : request.Artifacts;
        if (request.Assets is not null && request.Assets.Artifact is null) throw new IOException("Asset index metadata must be resolved before materialization.");
        if (request.Assets?.Artifact is { } requiredIndex && !artifacts.Any(x => x.Key == requiredIndex.Key)) artifacts = artifacts.Add(requiredIndex);
        var preserved = request.AdditionalPreservedArtifacts.IsDefault ? ImmutableArray<MmcArtifactReference>.Empty : request.AdditionalPreservedArtifacts;
        if ((long)artifacts.Length + preserved.Length > limits.MaxArtifacts || (!request.ExistingLocks.IsDefault && request.ExistingLocks.Length > limits.MaxArtifacts))
            throw new IOException("Artifact count budget exceeded.");
        var locks = new Dictionary<string, MmcArtifactLock>(StringComparer.Ordinal);
        foreach (var value in request.ExistingLocks.IsDefault ? [] : request.ExistingLocks) {
            ValidateLock(value);
            if (!locks.TryAdd(value.ArtifactKey, value)) throw new IOException("Duplicate adopted artifact key.");
        }
        var workspace = new MmcArtifactWorkspace(paths);
        try {
            var io = new MmcArtifactIo(limits, progress);
            var mutations = new Dictionary<string, MmcArtifactStagedMutation>(StringComparer.OrdinalIgnoreCase);
            var inputs = new Dictionary<string, PreparedInput>(StringComparer.Ordinal);
            long adoptedBytes = 0;
            foreach (var artifact in artifacts.Concat(preserved)) {
                cancellationToken.ThrowIfCancellationRequested();
                ValidateReference(artifact);
                if (inputs.TryGetValue(artifact.Key, out var old)) {
                    if (ReferenceFingerprint(old.Artifact) != ReferenceFingerprint(artifact)) throw new IOException("An artifact key has inconsistent declarations.");
                    continue;
                }
                locks.TryGetValue(artifact.Key, out var adopted);
                if (adopted is not null && adopted.SourceRevision != artifact.SourceRevision) throw new IOException("Artifact source revision disagrees with its adopted lock.");
                if (adopted is not null && ((artifact.Sha256 is not null && !artifact.Sha256.Equals(adopted.AdoptedSha256, StringComparison.OrdinalIgnoreCase)) ||
                    (artifact.Size is not null && artifact.Size != adopted.SizeBytes))) throw new IOException("Declared artifact metadata disagrees with its adopted lock.");
                if (adopted is null && request.Intent == MmcArtifactIntent.RepairExisting) throw new IOException("Repair cannot adopt an unrecorded artifact: " + artifact.Key);
                long? declaredBytes = adopted?.SizeBytes ?? artifact.Size;
                if (declaredBytes is not null && declaredBytes > limits.MaxCopiedBytes - adoptedBytes) throw new IOException("Aggregate artifact byte budget exceeded.");
                var prepared = await PrepareInputAsync(artifact, adopted, paths, workspace, io, access, mutations, progress, cancellationToken).ConfigureAwait(false);
                adoptedBytes = checked(adoptedBytes + prepared.Lock.SizeBytes);
                if (adoptedBytes > limits.MaxCopiedBytes) throw new IOException("Aggregate artifact actual byte budget exceeded.");
                inputs.Add(artifact.Key, prepared);
            }
            var assetPlan = await PrepareAssetInputsAsync(request.Assets, artifacts, inputs, locks, request.Intent,
                paths, workspace, io, access, mutations, progress, cancellationToken).ConfigureAwait(false);
            string viewKey = ViewKey(request.LogicalContext, artifacts.Select(x => inputs[x.Key]).Concat(
                assetPlan is null ? [] : assetPlan.Objects.Select(x => inputs[x.Key])));
            var bindings = ImmutableArray.CreateBuilder<MmcArtifactBinding>();
            var targetClaims = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var artifact in artifacts) {
                PreparedInput input = inputs[artifact.Key];
                string relative = TargetRelative(input.Artifact, viewKey);
                if (targetClaims.TryGetValue(relative, out string? previousDigest) && previousDigest != input.Lock.AdoptedSha256)
                    throw new IOException("Different artifacts collide at a runtime target.");
                targetClaims[relative] = input.Lock.AdoptedSha256;
                string target = paths.ResolveRelative(MmcPathRole.Instance, relative);
                string verified = await StageTargetAsync(input.OriginalInput, relative, input.Lock.AdoptedSha256,
                    input.Lock.SizeBytes, artifact.Key, paths, workspace, io, mutations, cancellationToken).ConfigureAwait(false);
                bindings.Add(new(artifact, input.Lock.AdoptedSha256, input.Lock.SizeBytes, relative, target, verified, input.ObservedSha1));
            }
            var nativeInputs = artifacts.Where(x => x.Role == MmcArtifactRole.Native).Select(x => inputs[x.Key]).ToArray();
            var cachedNativeFiles = await ReuseNativeCacheAsync(request.TrustedNativeCache, nativeInputs, viewKey, paths, targetClaims, cancellationToken).ConfigureAwait(false);
            var nativeFiles = cachedNativeFiles ?? await ExtractNativesAsync(nativeInputs,
                viewKey, paths, workspace, io, mutations, targetClaims, cancellationToken).ConfigureAwait(false);
            var assetFiles = await StageAssetsAsync(assetPlan, inputs, viewKey, paths, workspace, io, mutations, targetClaims, cancellationToken).ConfigureAwait(false);
            string? assetsRoot = assetPlan is null ? null : paths.ResolveRelative(MmcPathRole.Runtime, viewKey + "/assets");
            string? mappedRoot = assetPlan is { MapToResources: true, Virtual: false } ? paths.ResolveRelative(MmcPathRole.Game, "resources") : null;
            string? gameAssetsRoot = assetPlan is null ? null : mappedRoot ?? paths.ResolveRelative(MmcPathRole.Runtime, viewKey + "/assets/virtual/" + assetPlan.IndexId);
            return new(workspace, viewKey, bindings.ToImmutable(), inputs.Values.Select(x => x.Lock).ToImmutableArray(),
                mutations.Values.ToImmutableArray(), true, true,
                nativeFiles.IsEmpty ? null : paths.ResolveRelative(MmcPathRole.Runtime, viewKey + "/natives"), assetsRoot,
                nativeFiles, assetFiles, gameAssetsRoot, mappedRoot,
                inputs.Values.Select(x => new MmcArtifactObservation(x.Artifact.Key, x.Lock.AdoptedSha256, x.ObservedSha1, x.Lock.SizeBytes)).ToImmutableArray(),
                nativeInputs.Length == 0 ? null : new(viewKey, NativeSourceSet(nativeInputs), nativeFiles.Select(x => new MmcNativeFileFact(x.SourceArtifactKey, x.RelativePath, x.Sha256, x.SizeBytes)).ToImmutableArray()),
                cachedNativeFiles is not null);
        } catch (Exception failure) {
            try { workspace.Dispose(); }
            catch (Exception cleanup) { throw new AggregateException("Artifact preparation failed; owned workspace cleanup also failed.", failure, cleanup); }
            throw;
        }
    }

    private sealed record PreparedInput(MmcArtifactReference Artifact, MmcArtifactLock Lock, string OriginalInput, string ObservedSha1);

    private async Task<PreparedInput> PrepareInputAsync(MmcArtifactReference artifact, MmcArtifactLock? adopted,
        MmcInstancePaths paths, MmcArtifactWorkspace workspace, MmcArtifactIo io, MmcArtifactSourceAccess access,
        Dictionary<string, MmcArtifactStagedMutation> mutations, IProgress<MmcArtifactProgress>? progress, CancellationToken token) {
        string? expected = adopted?.AdoptedSha256 ?? artifact.Sha256;
        long? expectedSize = adopted?.SizeBytes ?? artifact.Size;
        long maximum = artifact.Role == MmcArtifactRole.AssetIndex ? Math.Min(limits.MaxAssetIndexBytes, limits.MaxArtifactBytes) : limits.MaxArtifactBytes;
        if (expectedSize > maximum) throw new IOException("Artifact or asset-index byte budget exceeded.");
        string? working = null;
        (string Sha256, long Size, string Sha1) observed = (string.Empty, 0, string.Empty);
        var failures = new List<Exception>();
        if (adopted is not null) {
            string original = paths.ResolveRelative(MmcPathRole.Instance, OriginalRelative(adopted.AdoptedSha256));
            if (File.Exists(original)) {
                try {
                    observed = await MmcArtifactIo.HashAsync(original, maximum, token, artifact.Sha1).ConfigureAwait(false);
                    if (observed.Sha256 == adopted.AdoptedSha256.ToLowerInvariant() && observed.Size == adopted.SizeBytes) working = original;
                } catch (IOException error) { failures.Add(error); }
            }
        }
        if (working is null) {
            var candidates = new List<MmcArtifactSource>();
            if (access.TryGetCachedAsync is not null) {
                var cached = await access.TryGetCachedAsync(artifact, token).ConfigureAwait(false);
                if (cached is not null) candidates.Add(cached);
            }
            if (artifact.Scope == MmcArtifactScope.InstanceLocal) {
                string local = paths.ResolveRelative(MmcPathRole.LocalLibraries, artifact.RelativePath);
                if (File.Exists(local)) candidates.Add(new(paths.LocalLibraryRoot, local));
            }
            foreach (var source in candidates) {
                token.ThrowIfCancellationRequested();
                string work = workspace.NewFile();
                try {
                    string trusted = MmcArtifactIo.AuthorizedSource(source);
                    observed = await io.CopyAsync(trusted, work, artifact.Key, expected, artifact.Sha1, expectedSize, token, maximum).ConfigureAwait(false);
                    working = work; break;
                } catch (IOException error) { failures.Add(error); }
            }
            if (working is null && access.FetchAsync is not null && !artifact.DownloadUrls.IsDefaultOrEmpty) {
                var fetched = await access.FetchAsync(artifact, progress, token).ConfigureAwait(false);
                if (fetched is not null) {
                    string trusted = MmcArtifactIo.AuthorizedSource(fetched);
                    string work = workspace.NewFile();
                    observed = await io.CopyAsync(trusted, work, artifact.Key, expected, artifact.Sha1, expectedSize, token, maximum).ConfigureAwait(false);
                    working = work;
                }
            }
        }
        if (working is null) throw new IOException("No verified source can recover artifact " + artifact.Key + ". Existing adopted bytes will not be replaced by unknown content.", failures.LastOrDefault());
        token.ThrowIfCancellationRequested();
        string originalRelative = OriginalRelative(observed.Sha256);
        string originalTarget = paths.ResolveRelative(MmcPathRole.Instance, originalRelative);
        string originalInput;
        if (File.Exists(originalTarget)) {
            var previous = await MmcArtifactIo.HashAsync(originalTarget, limits.MaxArtifactBytes, token).ConfigureAwait(false);
            if (previous.Sha256 == observed.Sha256 && previous.Size == observed.Size) originalInput = originalTarget;
            else {
                // A corrupted object is never overwritten here. Repair is a normal staged transaction mutation.
                string correct = workspace.NewFile();
                await io.CopyAsync(working, correct, artifact.Key, observed.Sha256, artifact.Sha1, observed.Size, token).ConfigureAwait(false);
                AddMutation(paths, mutations, correct, originalRelative, observed.Sha256, observed.Size, false, previous.Sha256);
                originalInput = correct;
            }
        } else {
            MmcArtifactIo.Directory(Path.GetDirectoryName(originalTarget)!);
            string publish = working;
            MmcInstancePaths.EnsureNoLinks(originalTarget);
            try { File.Move(publish, originalTarget, false); }
            catch (IOException) when (File.Exists(originalTarget)) {
                var raced = await MmcArtifactIo.HashAsync(originalTarget, limits.MaxArtifactBytes, token).ConfigureAwait(false);
                if (raced.Sha256 != observed.Sha256 || raced.Size != observed.Size) throw new IOException("A conflicting original object appeared; it was preserved.");
            }
            originalInput = originalTarget;
        }
        var locked = new MmcArtifactLock(artifact.Key, artifact.SourceRevision, observed.Sha256, observed.Size, originalRelative);
        return new(artifact, locked, originalInput, observed.Sha1);
    }

    private async Task<string> StageTargetAsync(string verifiedSource, string relative, string sha256, long size,
        string key, MmcInstancePaths paths, MmcArtifactWorkspace workspace, MmcArtifactIo io,
        Dictionary<string, MmcArtifactStagedMutation> mutations, CancellationToken token) {
        string target = paths.ResolveRelative(MmcPathRole.Instance, relative);
        if (mutations.TryGetValue(relative, out var duplicate)) {
            if (duplicate.NewSha256 != sha256) throw new IOException("Different artifacts collide at a runtime target.");
            return duplicate.StagedPath;
        }
        string? old = null;
        if (File.Exists(target)) {
            var current = await MmcArtifactIo.HashAsync(target, limits.MaxArtifactBytes, token).ConfigureAwait(false);
            old = current.Sha256;
            if (old == sha256 && current.Size == size) return target;
        }
        string staged = workspace.NewFile();
        await io.CopyAsync(verifiedSource, staged, key, sha256, null, size, token).ConfigureAwait(false);
        AddMutation(paths, mutations, staged, relative, sha256, size, old is null, old);
        return staged;
    }

    private static void AddMutation(MmcInstancePaths paths, Dictionary<string, MmcArtifactStagedMutation> mutations,
        string staged, string relative, string sha256, long size, bool missing, string? old) {
        string sourceRelative = Path.GetRelativePath(paths.InstanceRoot, staged).Replace('\\', '/');
        if (!sourceRelative.StartsWith("PCL/MmcRuntime/.staging/", StringComparison.Ordinal)) throw new IOException("Staged source escapes the artifact workspace.");
        var mutation = new MmcArtifactStagedMutation(staged, sourceRelative, relative, sha256, size, missing, old);
        if (mutations.TryGetValue(relative, out var previous) && (previous.NewSha256 != sha256 || previous.SizeBytes != size))
            throw new IOException("Different artifacts collide at an original/runtime target.");
        mutations[relative] = mutation;
    }

    private static string OriginalRelative(string digest) => "PCL/MmcArtifacts/sha256/" + digest.ToLowerInvariant();
    private static string TargetRelative(MmcArtifactReference artifact, string viewKey) {
        if (artifact.Scope == MmcArtifactScope.InstanceLocal) return "libraries/" + artifact.RelativePath.Replace('\\', '/');
        string root = "PCL/MmcRuntime/" + viewKey + "/";
        return root + (artifact.Role == MmcArtifactRole.AssetIndex ? "assets/" : artifact.Role == MmcArtifactRole.LoggingConfiguration ? "" : "libraries/") + artifact.RelativePath.Replace('\\', '/');
    }
    private static string ReferenceFingerprint(MmcArtifactReference value) => JsonSerializer.Serialize(value);
    private static string ViewKey(string context, IEnumerable<PreparedInput> inputs) {
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(new { Format = "PCLLF.MmcRuntimeView/v3", NativeExtraction = "bounded-native-v1", Assets = "owned-assets-v1", Context = context,
            Inputs = inputs.Select(x => new { x.Artifact.Key, x.Artifact.SourceRevision, x.Artifact.Role, x.Artifact.Scope,
                x.Artifact.RelativePath, x.Artifact.NativeClassifier, x.Artifact.ExtractExcludes, x.Lock.AdoptedSha256, x.Lock.SizeBytes }).ToArray() });
        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }
    private static void ValidateReference(MmcArtifactReference artifact) {
        if (string.IsNullOrEmpty(artifact.Key) || artifact.Key.Length > 4096 || string.IsNullOrEmpty(artifact.SourceRevision) || artifact.SourceRevision.Length > 4096)
            throw new IOException("Artifact identity or source revision is invalid.");
        if (!Enum.IsDefined(artifact.Role) || !Enum.IsDefined(artifact.Scope)) throw new IOException("Unknown artifact role or scope.");
        MmcArtifactIo.Relative(artifact.RelativePath);
        if ((artifact.Sha256 is not null && !MmcArtifactIo.Digest(artifact.Sha256, 64)) ||
            (artifact.Sha1 is not null && !MmcArtifactIo.Digest(artifact.Sha1, 40)) || artifact.Size < 0)
            throw new IOException("Artifact declared digest or size is invalid.");
    }
    private static void ValidateLock(MmcArtifactLock value) {
        if (string.IsNullOrEmpty(value.ArtifactKey) || string.IsNullOrEmpty(value.SourceRevision) ||
            !MmcArtifactIo.Digest(value.AdoptedSha256, 64) || value.SizeBytes < 0 ||
            value.RecoveryObjectRelativePath != OriginalRelative(value.AdoptedSha256)) throw new IOException("Adopted artifact lock is invalid.");
    }
}
