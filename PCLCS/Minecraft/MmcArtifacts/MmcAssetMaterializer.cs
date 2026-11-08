using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PCLLF.MMCC.Core;
using JsonSerializer = System.Text.Json.JsonSerializer;
using JsonException = System.Text.Json.JsonException;

namespace PCLCS;

// Original LF host implementation; governed by the repository LICENCE.
public sealed partial class MmcArtifactMaterializer {
    private const string AssetObjectKeyPrefix = "PCLLF:asset-object:v1:";

    /// <summary>Download adapters map these source-only references to the existing global assets cache.</summary>
    public static bool IsAssetObjectReference(MmcArtifactReference artifact) => artifact.Key.StartsWith(AssetObjectKeyPrefix, StringComparison.Ordinal) &&
        artifact.Role == MmcArtifactRole.DownloadOnly && artifact.Coordinate is null && artifact.RelativePath.StartsWith("objects/", StringComparison.Ordinal);

    private sealed record AssetEntry(string Name, MmcArtifactReference Object);
    private sealed record AssetPlan(string IndexId, bool Virtual, bool MapToResources,
        ImmutableArray<AssetEntry> Entries, ImmutableArray<MmcArtifactReference> Objects);

    private async Task<AssetPlan?> PrepareAssetInputsAsync(MmcAssetIndex? requestedAssets, ImmutableArray<MmcArtifactReference> artifacts,
        Dictionary<string, PreparedInput> inputs, Dictionary<string, MmcArtifactLock> locks, MmcArtifactIntent intent,
        MmcInstancePaths paths, MmcArtifactWorkspace workspace, MmcArtifactIo io, MmcArtifactSourceAccess access,
        Dictionary<string, MmcArtifactStagedMutation> mutations, IProgress<MmcArtifactProgress>? progress, CancellationToken token) {
        var indexes = artifacts.Where(x => x.Role == MmcArtifactRole.AssetIndex).ToArray();
        if (indexes.Length == 0) return null;
        if (indexes.Length != 1) throw new IOException("Exactly one effective asset index is required.");
        var index = indexes[0];
        if (index.Scope != MmcArtifactScope.Shared || !index.RelativePath.StartsWith("indexes/", StringComparison.Ordinal) ||
            !index.RelativePath.EndsWith(".json", StringComparison.Ordinal)) throw new IOException("Asset index location is not canonical.");
        string id = index.RelativePath[8..^5]; MmcArtifactIo.Relative(id);
        paths.ResolveRelative(MmcPathRole.Runtime, "assets-validation/virtual/" + id);
        if (requestedAssets is not null && (requestedAssets.Id != id || requestedAssets.Artifact?.Key != index.Key))
            throw new IOException("Asset index declaration and selected input disagree.");
        PreparedInput source = inputs[index.Key];
        if (source.Lock.SizeBytes > limits.MaxAssetIndexBytes) throw new IOException("Asset index metadata budget exceeded.");
        byte[] json;
        await using (var file = new FileStream(source.OriginalInput, FileMode.Open, FileAccess.Read, FileShare.Read, 131072, true)) {
            if (file.Length > limits.MaxAssetIndexBytes) throw new IOException("Asset index metadata budget exceeded.");
            using var memory = new MemoryStream(); byte[] block = new byte[32768]; int read;
            while ((read = await file.ReadAsync(block, token).ConfigureAwait(false)) != 0) {
                if (memory.Length + read > limits.MaxAssetIndexBytes) throw new IOException("Asset index actual metadata budget exceeded.");
                memory.Write(block, 0, read);
            }
            json = memory.ToArray();
        }
        if (Convert.ToHexStringLower(SHA256.HashData(json)) != source.Lock.AdoptedSha256) throw new IOException("Asset index changed before parsing.");
        AssetPlan plan;
        try {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 16 });
            JsonElement root = document.RootElement;
            CheckObject(root, ["objects", "virtual", "map_to_resources"], "asset index");
            bool virtualAssets = Boolean(root, "virtual"), map = Boolean(root, "map_to_resources");
            if (!root.TryGetProperty("objects", out var objects) || objects.ValueKind != JsonValueKind.Object) throw new IOException("Asset index objects must be an object.");
            var entries = ImmutableArray.CreateBuilder<AssetEntry>();
            var unique = new Dictionary<string, MmcArtifactReference>(StringComparer.Ordinal);
            var names = new HashSet<string>(StringComparer.Ordinal);
            var targetRoles = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            long prefixCharacters = 0;
            foreach (var entry in objects.EnumerateObject()) {
                token.ThrowIfCancellationRequested();
                if (entries.Count >= limits.MaxAssetObjects || !names.Add(entry.Name)) throw new IOException("Asset object count or duplicate name is invalid.");
                if (entry.Name.Contains('\\')) throw new IOException("Asset logical names must use forward separators.");
                MmcArtifactIo.Relative(entry.Name);
                paths.ResolveRelative(MmcPathRole.Runtime, "assets-validation/" + entry.Name);
                string[] segments = entry.Name.Split('/'); int prefixLength = 0;
                for (int position = 0; position < segments.Length; position++) {
                    prefixLength += segments[position].Length + (position == 0 ? 0 : 1); prefixCharacters += prefixLength;
                    if (prefixCharacters > 32 * 1024 * 1024) throw new IOException("Asset target prefix budget exceeded.");
                    string prefix = string.Join("/", segments, 0, position + 1); bool directory = position < segments.Length - 1;
                    if (targetRoles.TryGetValue(prefix, out bool oldRole) && (oldRole != directory || !directory)) throw new IOException("Asset logical file target or directory role collision.");
                    if (targetRoles.Count >= limits.MaxAssetObjects * 2L && !targetRoles.ContainsKey(prefix)) throw new IOException("Asset implicit target node budget exceeded.");
                    targetRoles[prefix] = directory;
                }
                CheckObject(entry.Value, ["hash", "size"], "asset object");
                if (!entry.Value.TryGetProperty("hash", out var hashValue) || hashValue.ValueKind != JsonValueKind.String ||
                    hashValue.GetString() is not { } hash || !MmcArtifactIo.Digest(hash, 40) ||
                    !entry.Value.TryGetProperty("size", out var sizeValue) || sizeValue.ValueKind != JsonValueKind.Number || !sizeValue.TryGetInt64(out long size) || size < 0)
                    throw new IOException("Asset object requires a SHA1 and nonnegative integer size.");
                hash = hash.ToLowerInvariant();
                if (unique.TryGetValue(hash, out var repeated) && repeated.Size != size) throw new IOException("One asset hash has conflicting declared sizes.");
                if (repeated is null) {
                    string key = AssetObjectKeyPrefix + Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(
                        new[] { index.Key, source.Lock.AdoptedSha256, hash, size.ToString(CultureInfo.InvariantCulture) })));
                    var provenance = new MmcFieldSource(index.ComponentUid, index.Provenance.ComponentVersion, index.RelativePath,
                        source.Lock.AdoptedSha256, "objects." + entry.Name, index.Provenance.Position);
                    repeated = new(key, index.ComponentUid, source.Lock.AdoptedSha256, MmcArtifactRole.DownloadOnly, MmcArtifactScope.Shared, null,
                        "objects/" + hash[..2] + "/" + hash, ["https://resources.download.minecraft.net/" + hash[..2] + "/" + hash], hash, null, size, null, [], provenance);
                    unique.Add(hash, repeated);
                }
                entries.Add(new(entry.Name, repeated));
            }
            plan = new(id, virtualAssets, map, entries.ToImmutable(), unique.Values.ToImmutableArray());
        } catch (JsonException error) { throw new IOException("Asset index JSON is malformed.", error); }
        long adoptedBytes = inputs.Values.Sum(x => x.Lock.SizeBytes);
        foreach (var artifact in plan.Objects) {
            if (inputs.Count >= limits.MaxArtifacts) throw new IOException("Combined library and asset artifact count budget exceeded.");
            locks.TryGetValue(artifact.Key, out var adopted);
            if (adopted is null && intent == MmcArtifactIntent.RepairExisting) throw new IOException("Repair cannot first-adopt an asset object.");
            if (adopted is not null && (adopted.SourceRevision != artifact.SourceRevision || adopted.SizeBytes != artifact.Size)) throw new IOException("Asset object lock disagrees with its index source.");
            if (artifact.Size > limits.MaxCopiedBytes - adoptedBytes) throw new IOException("Aggregate asset object byte budget exceeded.");
            var prepared = await PrepareInputAsync(artifact, adopted, paths, workspace, io, access, mutations, progress, token).ConfigureAwait(false);
            adoptedBytes = checked(adoptedBytes + prepared.Lock.SizeBytes);
            if (adoptedBytes > limits.MaxCopiedBytes) throw new IOException("Aggregate asset object actual byte budget exceeded.");
            inputs.Add(artifact.Key, prepared);
        }
        return plan;
    }

    private async Task<ImmutableArray<MmcAssetFileBinding>> StageAssetsAsync(AssetPlan? plan, Dictionary<string, PreparedInput> inputs,
        string viewKey, MmcInstancePaths paths, MmcArtifactWorkspace workspace, MmcArtifactIo io,
        Dictionary<string, MmcArtifactStagedMutation> mutations, Dictionary<string, string> targetClaims, CancellationToken token) {
        if (plan is null) return [];
        foreach (var artifact in plan.Objects) {
            var input = inputs[artifact.Key];
            string relative = "PCL/MmcRuntime/" + viewKey + "/assets/" + artifact.RelativePath;
            Claim(relative, input.Lock.AdoptedSha256);
            await StageTargetAsync(input.OriginalInput, relative, input.Lock.AdoptedSha256, input.Lock.SizeBytes,
                artifact.Key, paths, workspace, io, mutations, token).ConfigureAwait(false);
        }
        var result = ImmutableArray.CreateBuilder<MmcAssetFileBinding>();
        foreach (var entry in plan.Entries) {
            var input = inputs[entry.Object.Key];
            string? virtualPath = null, mappedPath = null;
            if (plan.Virtual || plan.MapToResources) {
                string relative = plan.Virtual ? "PCL/MmcRuntime/" + viewKey + "/assets/virtual/" + plan.IndexId + "/" + entry.Name :
                    paths.GameRootLeaf + "/resources/" + entry.Name;
                Claim(relative, input.Lock.AdoptedSha256);
                await StageTargetAsync(input.OriginalInput, relative, input.Lock.AdoptedSha256, input.Lock.SizeBytes,
                    entry.Object.Key, paths, workspace, io, mutations, token).ConfigureAwait(false);
                if (plan.Virtual) virtualPath = paths.ResolveRelative(MmcPathRole.Instance, relative);
                else mappedPath = paths.ResolveRelative(MmcPathRole.Instance, relative);
            }
            result.Add(new(entry.Name, entry.Object.Key, input.ObservedSha1, input.Lock.AdoptedSha256, input.Lock.SizeBytes,
                paths.ResolveRelative(MmcPathRole.Runtime, viewKey + "/assets/" + entry.Object.RelativePath), virtualPath, mappedPath));
        }
        return result.ToImmutable();

        void Claim(string target, string digest) {
            if (targetClaims.TryGetValue(target, out string? previous) && previous != digest) throw new IOException("Asset outputs collide with another runtime input.");
            targetClaims[target] = digest;
        }
    }

    private static void CheckObject(JsonElement value, string[] allowed, string label) {
        if (value.ValueKind != JsonValueKind.Object) throw new IOException(label + " must be an object.");
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in value.EnumerateObject())
            if (!names.Add(property.Name) || !allowed.Contains(property.Name, StringComparer.Ordinal)) throw new IOException(label + " has a duplicate or unsupported field: " + property.Name);
    }
    private static bool Boolean(JsonElement value, string name) {
        if (!value.TryGetProperty(name, out var field)) return false;
        if (field.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) throw new IOException("Asset index flag must be a boolean: " + name);
        return field.GetBoolean();
    }
}
