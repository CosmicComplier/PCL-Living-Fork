using System.Collections.Immutable;
using System.Text;
using PCLLF.MMCC.Core;

namespace PCLCS;

// Original LF host implementation, under the repository LICENCE.
public sealed partial class MmcInstanceBackend<TNetFile> where TNetFile : notnull {
    public async Task<MmcInstanceBackendResult> RepairAsync(MmcInstanceRepairRequest request, CancellationToken cancellationToken = default,
        IProgress<MmcInstanceBackendProgress>? progress = null) {
        try {
            ArgumentNullException.ThrowIfNull(request); cancellationToken.ThrowIfCancellationRequested();
            var initialPaths = MmcInstancePaths.Resolve(request.VersionRoot);
            var primary = MmcSavedSourceStore.ReadPrimary(initialPaths); var definition = primary.Definition;
            if (request.ExpectedInstanceId is { } expectedId && expectedId != definition.InstanceId) throw new IOException("The selected instance identity changed.");
            var paths = MmcInstancePaths.Resolve(request.VersionRoot, definition.Layout.GameRootLeaf);
            using (var readGate = MmcAdmissionGate.Acquire(paths.InstanceRoot, definition.InstanceId, TimeSpan.Zero, cancellationToken)) {
                MmcFileTransactions.AssertNoPending(readGate); MmcSessions.AssertNoActive(readGate);
                if (MmcSavedSourceStore.ReadPrimary(paths).Sha256 != primary.Sha256) throw new IOException("Primary definition changed before repair preparation.");
            }
            string projectionPath = paths.ResolveRelative(MmcPathRole.Instance, Path.GetFileName(paths.InstanceRoot) + ".json");
            string? oldProjection = MmcDurableFiles.Observe(projectionPath);
            if (oldProjection is not null && oldProjection != definition.InstalledProjection!.OutputJsonSha256 && !request.AllowRebuildDivergedProjection)
                throw new IOException("The compatibility JSON was externally changed; inspect it before explicitly rebuilding from the definition.");
            progress?.Report(new("loading-immutable-sources"));
            var saved = await MmcSavedSourceStore.LoadAsync(paths, definition, cancellationToken).ConfigureAwait(false);
            var compiled = Compile(saved.Resolved.Resolution, request.Environment);
            if (compiled.ProfileFingerprint != definition.InstalledProjection!.ProfileFingerprint) throw new IOException("Recompiled source identity differs from the published profile.");
            var locks = definition.ArtifactLocks.Select(value => new MmcArtifactLock(value.ArtifactKey, value.SourceRevision, value.AdoptedSha256, value.SizeBytes, value.RecoveryObjectRelativePath))
                .Concat(definition.PackageFileLocks.Select(value => new MmcArtifactLock(value.ArtifactKey, value.SourceRevision, value.AdoptedSha256, value.SizeBytes, value.RecoveryObjectRelativePath))).ToImmutableArray();
            var preserved = MmcPackageFilePreservation.CreateLibraryArtifacts(saved.Baseline);
            var artifactRequest = new MmcArtifactRequest(paths, compiled.RuntimePlanFingerprint, ActiveArtifacts(compiled), preserved, locks,
                MmcArtifactIntent.RepairExisting, compiled.Assets);
            // All transport/preparation stays outside admission. Only already-locked bytes can satisfy repair.
            var fetched = await prefetch.PrefetchAsync(compiled, artifactRequest, cancellationToken).ConfigureAwait(false);
            using var prepared = await materializer.PrepareAsync(artifactRequest, fetched.CreateAccess(), cancellationToken, ArtifactProgress(progress)).ConfigureAwait(false);
            using var localTargets = new MmcArtifactWorkspace(paths);
            var localRepairs = await RepairPreservedTargetsAsync(paths, definition, prepared, localTargets, cancellationToken).ConfigureAwait(false);
            var observations = MmcArtifactBindingAdapter.Create(artifactRequest, compiled, prepared);
            long generation = checked(definition.Generation + 1);
            string name = Path.GetFileName(paths.InstanceRoot);
            byte[] projection = RenderProjection(name, definition.InstanceId, generation, compiled, prepared, paths);
            var options = observations.ApplyTo(new(definition.Generation, generation, MmcBindingIntent.RepairExisting, Hash(projection), generation));
            var bound = MmcPlanBinder.Bind(definition, compiled, observations.Facts, options, definition.ArtifactLocks,
                observations.DerivedOutputs, observations.AssetIndexes, publishedBaseline: definition, packageBaseline: saved.BaselineInput);
            RequireBinding(bound);
            byte[] nextDefinition = Encoding.UTF8.GetBytes(MmcDefinitionCodec.Serialize(bound.Definition!));
            var mutations = ArtifactMutations(prepared).Concat(localRepairs).Append(new MmcFileMutation(name + ".json", oldProjection, projection, Hash(projection)))
                .Append(new MmcFileMutation("PCL/InstanceDefinition.json", primary.Sha256, nextDefinition, Hash(nextDefinition)));
            progress?.Report(new("publishing-repair"));
            using var quiet = MmcConfigurationCoordinator.Quiesce(paths.InstanceRoot, cancellationToken);
            using var gate = MmcAdmissionGate.Acquire(paths.InstanceRoot, definition.InstanceId, TimeSpan.Zero, cancellationToken);
            if (MmcSavedSourceStore.ReadPrimary(paths).Sha256 != primary.Sha256 || MmcDurableFiles.Observe(projectionPath) != oldProjection)
                throw new IOException("Definition or projection changed while repair was preparing; no prepared changes were published.");
            VerifyPreparedInputs(paths, prepared);
            VerifySourceRecords(paths, definition);
            using var settings = quiet.OpenProjectionRepair(gate, definition.Generation, primary.Sha256);
            SetManifest(settings, saved.Resolved.Resolution.Profile!, compiled, definition.InstanceId, generation);
            MmcTransactionResult result = settings.Execute(new(definition.InstanceId, definition.Generation, generation, MergeMutations(mutations)), cancellationToken, request.TransactionCheckpoint);
            if (result.Outcome != MmcTransactionOutcome.Committed)
                return new(cancellationToken.IsCancellationRequested ? MmcInstanceBackendStatus.Cancelled : MmcInstanceBackendStatus.NeedsRecovery, WorkingRoot: paths.InstanceRoot, Failure: result.Diagnostic);
            progress?.Report(new("completed"));
            return new(MmcInstanceBackendStatus.Succeeded, bound.Definition, bound.Plan, paths, saved.PackageConfiguration, paths.InstanceRoot, []);
        } catch (OperationCanceledException error) { return new(MmcInstanceBackendStatus.Cancelled, WorkingRoot: request.VersionRoot, Failure: error.Message); }
        catch (Exception error) when (IsExpectedFailure(error)) { return new(MmcInstanceBackendStatus.NeedsRecovery, WorkingRoot: request.VersionRoot, Failure: error.Message); }
    }

    private static async Task<ImmutableArray<MmcFileMutation>> RepairPreservedTargetsAsync(MmcInstancePaths paths, MmcInstanceDefinition definition,
        MmcPreparedArtifacts prepared, MmcArtifactWorkspace workspace, CancellationToken token) {
        var result = ImmutableArray.CreateBuilder<MmcFileMutation>(); var io = new MmcArtifactIo(new(), null);
        var already = prepared.StagedMutations.Select(value => value.TargetRelativePath).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var value in definition.PackageFileLocks) {
            token.ThrowIfCancellationRequested(); if (already.Contains(value.TargetRelativePath)) continue;
            string target = paths.ResolveRelative(MmcPathRole.Instance, value.TargetRelativePath); string? old = MmcDurableFiles.Observe(target);
            if (old == value.AdoptedSha256) continue;
            string source = OriginalInput(paths, prepared, value.RecoveryObjectRelativePath);
            string staged = workspace.NewFile();
            await io.CopyAsync(source, staged, value.ArtifactKey, value.AdoptedSha256, value.ObservedSha1, value.SizeBytes, token).ConfigureAwait(false);
            result.Add(new(value.TargetRelativePath, old, null, value.AdoptedSha256, Relative(paths, staged)));
        }
        return result.ToImmutable();
    }
    private static string OriginalInput(MmcInstancePaths paths, MmcPreparedArtifacts prepared, string relative) =>
        prepared.StagedMutations.FirstOrDefault(value => value.TargetRelativePath == relative)?.StagedPath ?? paths.ResolveRelative(MmcPathRole.Instance, relative);
    private static void VerifyPreparedInputs(MmcInstancePaths paths, MmcPreparedArtifacts prepared) {
        foreach (var binding in prepared.OrderedBindings)
            if (MmcDurableFiles.Observe(binding.VerifiedInputPath) != binding.Sha256) throw new IOException("Prepared execution input changed before publication.");
        foreach (var value in prepared.AdoptedLocks)
            if (MmcDurableFiles.Observe(OriginalInput(paths, prepared, value.RecoveryObjectRelativePath)) != value.AdoptedSha256)
                throw new IOException("Locked original input changed before publication.");
    }
    private static void VerifySourceRecords(MmcInstancePaths paths, MmcInstanceDefinition definition) {
        foreach (var value in definition.SourceBundle.MetadataRecords)
            if (MmcDurableFiles.Observe(paths.ResolveRelative(MmcPathRole.Instance, value.ObjectRelativePath)) != value.ContentSha256)
                throw new IOException("Saved source changed before publication.");
        if (definition.AuthorFileBaseline is not { } baseline || MmcDurableFiles.Observe(paths.ResolveRelative(MmcPathRole.Instance, baseline.ObjectRelativePath)) != baseline.ContentSha256)
            throw new IOException("Measured author baseline changed before publication.");
    }
}
