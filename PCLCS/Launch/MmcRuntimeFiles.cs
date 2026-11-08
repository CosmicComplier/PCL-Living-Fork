using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PCLCS;

// Original LF host implementation; governed by the repository LICENCE.
internal static class MmcRuntimeFiles {
    internal static readonly StringComparison PathComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    internal static string JavaPath(string path) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path)).Replace('\\', '/');
    internal static void Hash(string? hash) {
        if (hash is null || hash.Length != 64 || !hash.All(Uri.IsHexDigit)) throw new MmcRuntimeLaunchException("MMC_RUNTIME_DIGEST", "A verified runtime identity is missing or malformed.");
    }
    internal static void SamePath(string actual, string expected) {
        if (!Path.IsPathFullyQualified(actual) || !Path.GetFullPath(actual).Equals(Path.GetFullPath(expected), PathComparison))
            throw new MmcRuntimeLaunchException("MMC_RUNTIME_PATH", "A runtime path does not match its approved instance role.");
        MmcInstancePaths.EnsureNoLinks(expected);
    }
    internal static void Owned(string actual, string parent) {
        if (!Path.IsPathFullyQualified(actual) || !Path.GetFullPath(actual).StartsWith(Path.TrimEndingDirectorySeparator(Path.GetFullPath(parent)) + Path.DirectorySeparatorChar, PathComparison))
            throw new MmcRuntimeLaunchException("MMC_RUNTIME_PATH", "A runtime support path is outside the approved owned view.");
        MmcInstancePaths.EnsureNoLinks(actual);
    }
    internal static async Task<MmcRuntimeInputObservation> ObserveAsync(string identity, string path, string expectedHash,
        long? expectedSize, CancellationToken token, bool owned = true) {
        Hash(expectedHash);
        if (!Path.IsPathFullyQualified(path)) throw new MmcRuntimeLaunchException("MMC_RUNTIME_PATH", "Runtime inputs require an approved absolute path.");
        if (owned) MmcInstancePaths.EnsureNoLinks(path);
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.Asynchronous | FileOptions.SequentialScan);
        string observed = await HashStreamAsync(stream, token).ConfigureAwait(false);
        if (!observed.Equals(expectedHash, StringComparison.OrdinalIgnoreCase) || expectedSize is not null && stream.Length != expectedSize)
            throw new MmcRuntimeLaunchException("MMC_RUNTIME_INPUT_CHANGED", "An actual runtime input differs from its adopted byte identity.");
        return new(identity, Path.GetFullPath(path), observed, stream.Length);
    }
    internal static async Task<string> HashStreamAsync(Stream stream, CancellationToken token) {
        if (!stream.CanSeek || stream.Length > 4L * 1024 * 1024 * 1024) throw new MmcRuntimeLaunchException("MMC_RUNTIME_INPUT_BUDGET", "Runtime input exceeds its verified byte budget.");
        stream.Position = 0;
        using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[] buffer = new byte[65536]; long read = 0;
        while (true) {
            int count = await stream.ReadAsync(buffer.AsMemory(), token).ConfigureAwait(false);
            if (count == 0) break;
            read += count;
            if (read > 4L * 1024 * 1024 * 1024) throw new MmcRuntimeLaunchException("MMC_RUNTIME_INPUT_BUDGET", "Runtime input grew beyond its byte budget.");
            digest.AppendData(buffer, 0, count);
        }
        token.ThrowIfCancellationRequested();
        return Convert.ToHexStringLower(digest.GetHashAndReset());
    }
    internal static string Fingerprint(IEnumerable<string?> values) {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> length = stackalloc byte[4];
        foreach (string? value in values) {
            byte[] bytes = Encoding.UTF8.GetBytes(value ?? ""); BinaryPrimitives.WriteInt32LittleEndian(length, bytes.Length);
            hash.AppendData(length); hash.AppendData(bytes);
        }
        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }
}
