using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using PCLLF.MMCC.Core;
using JsonSerializer = System.Text.Json.JsonSerializer;
using JsonException = System.Text.Json.JsonException;

namespace PCLCS;

// Original LF host implementation; governed by the repository LICENCE.
public sealed class MmcInstanceHistoryBackend {
    public const int CoverageVersion = MmcInstanceHistoryStore.CoverageVersion;
    private readonly MmcArtifactMaterializer materializer;
    public MmcInstanceHistoryBackend(MmcArtifactLimits? limits = null) => materializer = new(limits);

    // All synchronous leases stay on one dedicated worker; progress/checkpoints must not await UI.
    public Task<MmcInstanceHistoryCaptureResult> CaptureAsync(MmcInstanceHistoryCaptureRequest request, CancellationToken cancellationToken = default) =>
        Task.Run(() => Capture(request, cancellationToken), CancellationToken.None);
    public Task<MmcInstanceHistoryRestoreResult> RestoreAsync(MmcInstanceHistoryRestoreRequest request, CancellationToken cancellationToken = default) =>
        Task.Run(() => Restore(request, cancellationToken), CancellationToken.None);
    public Task<ImmutableArray<MmcInstanceHistoryPoint>> ListAsync(string versionRoot, CancellationToken cancellationToken = default) =>
        Task.Run(() => MmcInstanceHistoryStore.List(MmcInstancePaths.Resolve(versionRoot), cancellationToken), CancellationToken.None);

    private static MmcInstanceHistoryCaptureResult Capture(MmcInstanceHistoryCaptureRequest request, CancellationToken token) {
        try {
            ArgumentNullException.ThrowIfNull(request); token.ThrowIfCancellationRequested();
            string root = Canonical(request.VersionRoot);
            using var quiet = MmcConfigurationCoordinator.Quiesce(root, token);
            using var lease = MmcAdmissionGate.Acquire(root, request.ExpectedInstanceId, TimeSpan.Zero, token);
            var primary = MmcInstanceHistoryStore.AssertCurrent(lease, request.ExpectedInstanceId, request.ExpectedGeneration, request.ExpectedPrimarySha256);
            var paths = MmcInstancePaths.Resolve(root, primary.Definition.Layout.GameRootLeaf);
            var saved = MmcSavedSourceStore.LoadAsync(paths, primary.Definition, token).GetAwaiter().GetResult();
            _ = MmcInstanceCloneBackend.CompileEnvironment(saved.Resolved.Resolution, primary.Definition, request.LegacyEnvironmentCandidates);
            var point = MmcInstanceHistoryStore.Capture(lease, primary, saved, request.Label, token, request.Checkpoint);
            return new(MmcInstanceBackendStatus.Succeeded, point);
        } catch (OperationCanceledException error) { return new(MmcInstanceBackendStatus.Cancelled, Failure: error.Message); }
        catch (Exception error) when (Expected(error)) { return new(MmcInstanceBackendStatus.NeedsRecovery, Failure: error.Message); }
    }

    private MmcInstanceHistoryRestoreResult Restore(MmcInstanceHistoryRestoreRequest request, CancellationToken token) {
        MmcInstanceHistoryPoint? undo = null; bool decided = false;
        try {
            ArgumentNullException.ThrowIfNull(request); token.ThrowIfCancellationRequested();
            if (!MmcDurableFiles.IsHash(request.ExpectedPointManifestSha256)) throw new IOException("History restore requires a fixed trusted point reference.");
            string root = Canonical(request.VersionRoot);
            using var quiet = MmcConfigurationCoordinator.Quiesce(root, token);
            using var lease = MmcAdmissionGate.Acquire(root, request.ExpectedInstanceId, TimeSpan.Zero, token);
            var primary = MmcInstanceHistoryStore.AssertCurrent(lease, request.ExpectedInstanceId, request.ExpectedGeneration, request.ExpectedPrimarySha256);
            var current = primary.Definition; var paths = MmcInstancePaths.Resolve(root, current.Layout.GameRootLeaf);
            var point = MmcInstanceHistoryStore.Read(paths, request.PointId, request.ExpectedPointManifestSha256, token);
            if (point.Definition.InstanceId != current.InstanceId || point.Definition.Layout != current.Layout)
                throw new IOException("The selected point belongs to another identity or game-tree layout.");
            var compiled = MmcInstanceCloneBackend.CompileEnvironment(point.Saved.Resolved.Resolution, point.Definition, request.LegacyEnvironmentCandidates);
            var currentSaved = MmcSavedSourceStore.LoadAsync(paths, current, token).GetAwaiter().GetResult();
            // Complete rollback environment, including all current original lock bindings, before any live mutation.
            undo = MmcInstanceHistoryStore.Capture(lease, primary, currentSaved, "恢复前自动保存", token, request.Checkpoint);
            request.Checkpoint?.Invoke(MmcInstanceHistoryCheckpoint.BeforeRestorePreparation); token.ThrowIfCancellationRequested();
            using var sources = new MmcArtifactWorkspace(paths);
            var active = Active(compiled);
            var declared = active.Where(value => value.Scope == MmcArtifactScope.InstanceLocal).Select(value => "libraries/" + value.RelativePath).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var preserved = MmcPackageFilePreservation.CreateLibraryArtifacts(point.Saved.Baseline).Where(value => !declared.Contains("libraries/" + value.RelativePath)).ToImmutableArray();
            var locks = MmcPublishedArtifactLocks.Create(point.Definition);
            var lookup = locks.ToDictionary(value => value.ArtifactKey, StringComparer.Ordinal);
            var artifactRequest = new MmcArtifactRequest(paths, compiled.RuntimePlanFingerprint, active, preserved, locks, MmcArtifactIntent.RepairExisting, compiled.Assets);
            var access = new MmcArtifactSourceAccess((artifact, ct) => {
                ct.ThrowIfCancellationRequested();
                return ValueTask.FromResult<MmcArtifactSource?>(lookup.TryGetValue(artifact.Key, out var original)
                    ? new(point.SourcePaths.InstanceRoot, point.SourcePaths.ResolveRelative(MmcPathRole.Instance, original.RecoveryObjectRelativePath)) : null);
            });
            using var prepared = materializer.PrepareAsync(artifactRequest, access, token).GetAwaiter().GetResult();
            // The materializer may append a missing immutable CAS object. Observe sources only
            // after it drains, so the transaction never asserts that such an object is absent.
            var sourceMutations = StageSources(paths, point, sources, token);
            var observations = MmcArtifactBindingAdapter.Create(artifactRequest, compiled, prepared);
            long next = checked(current.Generation + 1); string name = Path.GetFileName(root);
            byte[] projection = MmcVersionProjection.Render(name, current.InstanceId, next, compiled,
                prepared.OrderedBindings.ToDictionary(value => value.Artifact.Key, value => value.TargetRelativePath),
                prepared.NativeDirectory is null ? null : Relative(paths, prepared.NativeDirectory),
                prepared.AssetsRoot is null ? "PCL/MmcRuntime/" + prepared.ViewKey + "/assets" : Relative(paths, prepared.AssetsRoot));
            var bound = MmcPlanBinder.BindRestore(current, point.Definition, compiled, observations.Facts,
                new(current.InstanceId, current.Generation, current.InstalledProjection!.BoundFingerprint, point.Definition.InstalledProjection!.BoundFingerprint, MmcDurableFiles.Hash(projection)) {
                    NativeDerivation = observations.NativeCompletion
                }, observations.DerivedOutputs, observations.AssetIndexes, packageBaseline: point.Saved.BaselineInput);
            if (bound.HasErrors || bound.Definition is null || bound.Plan is null)
                throw new IOException("History binding rejected its immutable authority: " + string.Join("; ", bound.Diagnostics.Select(value => value.Code)));
            byte[] definitionBytes = Encoding.UTF8.GetBytes(MmcDefinitionCodec.Serialize(bound.Definition));
            var currentEnvironment = MmcInstanceHistoryFiles.Environment(paths, current, token);
            var environment = StageEnvironment(paths, point, currentEnvironment, sources, token);
            var mutations = sourceMutations.Concat(prepared.StagedMutations.Select(value => new MmcFileMutation(value.TargetRelativePath,
                    value.ExpectedMissing ? null : value.ExpectedExistingSha256, null, value.NewSha256, value.NewSourceRelativePath)))
                .Concat(environment).Concat(StagePrivateLibraries(paths, point, current, sources, token))
                .Append(new(name + ".json", current.InstalledProjection.OutputJsonSha256, projection, MmcDurableFiles.Hash(projection)))
                .Append(new("PCL/InstanceDefinition.json", primary.Sha256, definitionBytes, MmcDurableFiles.Hash(definitionBytes))).ToList();
            Guid restoration = Guid.NewGuid();
            byte[] receipt = JsonSerializer.SerializeToUtf8Bytes(new { Protocol = "PCLLF.MmcHistoryRestore/v1", OperationId = restoration,
                InstanceId = current.InstanceId, PreviousGeneration = current.Generation, NewGeneration = next,
                PreviousPrimarySha256 = primary.Sha256, PointId = point.Point.PointId, point.Point.ManifestSha256,
                SelectedPrimarySha256 = point.Point.PrimarySha256, UndoPointId = undo.PointId, UndoManifestSha256 = undo.ManifestSha256,
                NewPrimarySha256 = MmcDurableFiles.Hash(definitionBytes) });
            mutations.Add(new("PCL/MmcHistory/restores/" + restoration.ToString("N") + ".json", null, receipt, MmcDurableFiles.Hash(receipt)));
            request.Checkpoint?.Invoke(MmcInstanceHistoryCheckpoint.BeforeRestoreTransaction);
            MmcInstanceHistoryStore.AssertCurrent(lease, current.InstanceId, current.Generation, primary.Sha256);
            _ = MmcInstanceHistoryStore.Read(paths, request.PointId, request.ExpectedPointManifestSha256, token);
            if (!currentEnvironment.SequenceEqual(MmcInstanceHistoryFiles.Environment(paths, current, token))) throw new IOException("Current environment changed while restore was preparing.");
            using var scope = quiet.OpenStructuralWrite(lease, current.Generation);
            SetManifest(scope, point.Saved.Resolved.Resolution.Profile!, compiled, current.InstanceId, next);
            var result = scope.Execute(new(current.InstanceId, current.Generation, next, Merge(mutations)), token, (phase, index) => {
                if (phase == MmcTransactionCheckpoint.CommitDecided) decided = true;
                request.TransactionCheckpoint?.Invoke(phase, index);
                if (phase == MmcTransactionCheckpoint.BeforeDecision) {
                    if (!point.Manifest.EnvironmentFiles.SequenceEqual(MmcInstanceHistoryFiles.Environment(paths, bound.Definition, token)))
                        throw new IOException("Restored environment namespace differs before the commit decision.");
                    MmcInstanceHistoryFiles.Verify(paths, point.Manifest.SourceFiles.Where(value => value.RelativePath != "PCL/InstanceDefinition.json"), token);
                    MmcInstanceHistoryFiles.Verify(paths, prepared.OrderedBindings.Select(value => new MmcHistoryFile(value.TargetRelativePath, value.Sha256, value.SizeBytes))
                        .Concat(observations.DerivedOutputs.Select(value => new MmcHistoryFile(value.TargetRelativePath, value.Sha256, value.SizeBytes)))
                        .GroupBy(value => value.RelativePath, StringComparer.OrdinalIgnoreCase).Select(group => group.First()), token);
                }
            });
            if (result.Outcome != MmcTransactionOutcome.Committed)
                return new(token.IsCancellationRequested && result.Outcome == MmcTransactionOutcome.Aborted ? MmcInstanceBackendStatus.Cancelled : MmcInstanceBackendStatus.NeedsRecovery,
                    UndoPoint: undo, Failure: result.Diagnostic);
            decided = true;
            request.Checkpoint?.Invoke(MmcInstanceHistoryCheckpoint.RestoreCompleted);
            return new(MmcInstanceBackendStatus.Succeeded, bound.Definition, bound.Plan, undo);
        } catch (OperationCanceledException error) { return new(decided ? MmcInstanceBackendStatus.NeedsRecovery : MmcInstanceBackendStatus.Cancelled, UndoPoint: undo, Failure: error.Message); }
        catch (Exception error) when (Expected(error)) { return new(MmcInstanceBackendStatus.NeedsRecovery, UndoPoint: undo, Failure: error.Message); }
    }

    private static List<MmcFileMutation> StageSources(MmcInstancePaths paths, MmcHistoryLoadedPoint point, MmcArtifactWorkspace workspace, CancellationToken token) {
        var mutations = new List<MmcFileMutation>(); var io = new MmcArtifactIo(new(), null);
        foreach (var file in point.Manifest.SourceFiles.Where(value => value.RelativePath != "PCL/InstanceDefinition.json")) {
            string? old = MmcDurableFiles.Observe(paths.ResolveRelative(MmcPathRole.Instance, file.RelativePath));
            if (old == file.Sha256) continue;
            string output = workspace.NewFile();
            io.CopyAsync(point.SourcePaths.ResolveRelative(MmcPathRole.Instance, file.RelativePath), output, file.RelativePath, file.Sha256, null, file.SizeBytes, token).GetAwaiter().GetResult();
            mutations.Add(new(file.RelativePath, old, null, file.Sha256, Relative(paths, output)));
        }
        return mutations;
    }
    private static IEnumerable<MmcFileMutation> StageEnvironment(MmcInstancePaths paths, MmcHistoryLoadedPoint point,
        ImmutableArray<MmcHistoryFile> current, MmcArtifactWorkspace workspace, CancellationToken token) {
        var before = current.ToDictionary(value => value.RelativePath, StringComparer.OrdinalIgnoreCase);
        var after = point.Manifest.EnvironmentFiles.ToDictionary(value => value.RelativePath, StringComparer.OrdinalIgnoreCase);
        var io = new MmcArtifactIo(new(), null);
        foreach (string relative in before.Keys.Union(after.Keys, StringComparer.OrdinalIgnoreCase)) {
            token.ThrowIfCancellationRequested(); before.TryGetValue(relative, out var old); after.TryGetValue(relative, out var next);
            if (old?.Sha256 == next?.Sha256) continue;
            string? staged = null;
            if (next is not null) {
                staged = workspace.NewFile();
                io.CopyAsync(point.SourcePaths.ResolveRelative(MmcPathRole.Instance, next.RelativePath), staged, relative, next.Sha256, null, next.SizeBytes, token).GetAwaiter().GetResult();
            }
            yield return new(relative, old?.Sha256, null, next?.Sha256, staged is null ? null : Relative(paths, staged));
        }
    }
    private static IEnumerable<MmcFileMutation> StagePrivateLibraries(MmcInstancePaths paths, MmcHistoryLoadedPoint point,
        MmcInstanceDefinition current, MmcArtifactWorkspace workspace, CancellationToken token) {
        var before = MmcInstanceHistoryFiles.PrivateLibraries(current);
        var after = MmcInstanceHistoryFiles.PrivateLibraries(point.Definition);
        var io = new MmcArtifactIo(new(), null);
        foreach (string relative in before.Keys.Union(after.Keys, StringComparer.OrdinalIgnoreCase)) {
            string? old = MmcDurableFiles.Observe(paths.ResolveRelative(MmcPathRole.Instance, relative));
            if (before.TryGetValue(relative, out var owned) && old is not null && old != owned.Sha256)
                throw new IOException("Current private library has unknown bytes; preserve it before restoring.");
            if (!after.TryGetValue(relative, out var next)) { if (old is not null) yield return new(relative, old, null, null); continue; }
            if (old == next.Sha256) continue;
            string staged = workspace.NewFile();
            io.CopyAsync(point.SourcePaths.ResolveRelative(MmcPathRole.Instance, "PCL/MmcArtifacts/sha256/" + next.Sha256), staged,
                relative, next.Sha256, null, next.SizeBytes, token).GetAwaiter().GetResult();
            yield return new(relative, old, null, next.Sha256, Relative(paths, staged));
        }
    }
    private static MmcFileMutation[] Merge(IEnumerable<MmcFileMutation> input) {
        Dictionary<string, MmcFileMutation> result = new(StringComparer.OrdinalIgnoreCase);
        foreach (var value in input) {
            if (result.TryGetValue(value.RelativePath, out var old)) {
                if (old.ExpectedOldSha256 != value.ExpectedOldSha256 || old.ExpectedNewSha256 != value.ExpectedNewSha256)
                    throw new IOException("History source/environment/runtime targets conflict.");
            } else result.Add(value.RelativePath, value);
            if (result.Count > MmcInstanceHistoryFiles.MaximumFiles) throw new IOException("History restoration exceeds its transaction budget.");
        }
        return result.Values.ToArray();
    }
    private static void SetManifest(MmcConfigurationWriteScope scope, MmcResolvedProfile resolved, MmcCompiledProfile compiled, Guid id, long generation) {
        var manifest = scope.ConfigSnapshot["MinecraftInstallManifest"] as JObject ?? new(); JObject loaders = new();
        foreach (var component in resolved.Components.Where(value => value.Enabled && value.Version is not null)) {
            string? key = component.Uid switch { "net.minecraftforge" => "Forge", "net.neoforged" => "NeoForge", "net.fabricmc.fabric-loader" => "Fabric", "com.mumfrey.liteloader" => "LiteLoader", _ => null };
            if (key is not null) loaders[key] = component.Version;
        }
        manifest["SchemaVersion"] = 1; manifest["Components"] = new JObject { ["MinecraftVersion"] = compiled.MinecraftVersion, ["ModLoaderVersions"] = loaders };
        manifest["ManagedFiles"] = new JArray(); manifest["LastModifiedUtc"] = DateTime.UtcNow;
        var hint = JObject.Parse(MmcVersionProjection.ManagementHint(id, generation, compiled).ToJsonString()); manifest["pcllfManagement"] = hint;
        scope.SetConfig("MinecraftInstallManifest", manifest);
        var config = scope.ConfigSnapshot;
        if (config.ContainsKey("pcllfManagement")) scope.SetConfig("pcllfManagement", hint);
        if (config.ContainsKey("generation")) scope.SetConfig("generation", generation);
        var ini = LegacyBaseOperations.ParseIni(scope.IniSnapshotForRelocation);
        if (ini.ContainsKey("MmcDefinitionGeneration")) scope.SetIni("MmcDefinitionGeneration", generation.ToString(System.Globalization.CultureInfo.InvariantCulture));
        scope.SetIni("VersionArgumentIndieV2", "True");
    }
    private static ImmutableArray<MmcArtifactReference> Active(MmcCompiledProfile compiled) => compiled.Classpath.Concat(compiled.NativeArtifacts)
        .Concat(compiled.DownloadOnlyArtifacts).Concat(compiled.AuxiliaryArtifacts).GroupBy(value => value.Key, StringComparer.Ordinal).Select(group => group.First()).ToImmutableArray();
    private static string Relative(MmcInstancePaths paths, string file) => Path.GetRelativePath(paths.InstanceRoot, file).Replace('\\', '/');
    private static string Canonical(string root) => MmcCanonicalPaths.ForAdmission(MmcInstancePaths.ValidateVersionRoot(root));
    private static bool Expected(Exception value) => value is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or AggregateException or JsonException;
}
