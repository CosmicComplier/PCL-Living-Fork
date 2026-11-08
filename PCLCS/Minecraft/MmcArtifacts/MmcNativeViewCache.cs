using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace PCLCS;

// Original LF host implementation; governed by the repository LICENCE.
public sealed partial class MmcArtifactMaterializer {
    private static string NativeSourceSet(PreparedInput[] inputs) => Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(
        new { Version = "bounded-native-v1", Sources = inputs.Select(x => new { x.Artifact.Key, x.Artifact.SourceRevision,
            x.Artifact.ExtractExcludes, x.Artifact.NativeClassifier, x.Lock.AdoptedSha256, x.Lock.SizeBytes }).ToArray() })));

    private async Task<ImmutableArray<MmcNativeFileBinding>?> ReuseNativeCacheAsync(MmcNativeViewCache? cache,
        PreparedInput[] sources, string viewKey, MmcInstancePaths paths, Dictionary<string, string> targetClaims, CancellationToken token) {
        if (sources.Length == 0 || cache is null || cache.ViewKey != viewKey || cache.SourceSetFingerprint != NativeSourceSet(sources)) return null;
        if (cache.CompleteFiles.IsDefault || cache.CompleteFiles.Length > limits.MaxNativeEntries) throw new IOException("Trusted native cache record exceeds its output budget.");
        var keys = sources.Select(x => x.Artifact.Key).ToHashSet(StringComparer.Ordinal);
        var facts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var result = ImmutableArray.CreateBuilder<MmcNativeFileBinding>();
        long bytes = 0;
        foreach (var fact in cache.CompleteFiles) {
            token.ThrowIfCancellationRequested();
            MmcArtifactIo.Relative(fact.RelativePath);
            if (!keys.Contains(fact.SourceArtifactKey) || !MmcArtifactIo.Digest(fact.Sha256, 64) || fact.SizeBytes < 0)
                throw new IOException("Trusted native cache record has invalid source or digest facts.");
            bytes = checked(bytes + fact.SizeBytes);
            if (bytes > limits.MaxNativeBytes) throw new IOException("Trusted native cache exceeds its byte budget.");
            string relative = "PCL/MmcRuntime/" + viewKey + "/natives/" + fact.RelativePath;
            string expected = fact.Sha256.ToLowerInvariant();
            if (facts.TryGetValue(relative, out string? old) && old != expected) throw new IOException("Trusted native cache target facts conflict.");
            facts[relative] = expected;
            string target = paths.ResolveRelative(MmcPathRole.Instance, relative);
            if (!File.Exists(target)) return null;
            var actual = await MmcArtifactIo.HashAsync(target, limits.MaxNativeBytes, token).ConfigureAwait(false);
            if (actual.Sha256 != expected || actual.Size != fact.SizeBytes) return null;
            result.Add(new(fact.SourceArtifactKey, fact.RelativePath, target, actual.Sha256, actual.Size));
        }
        foreach (var claim in facts) {
            if (targetClaims.TryGetValue(claim.Key, out string? old) && old != claim.Value) throw new IOException("Trusted native cache collides with another runtime input.");
            targetClaims[claim.Key] = claim.Value;
        }
        return result.ToImmutable();
    }
}
