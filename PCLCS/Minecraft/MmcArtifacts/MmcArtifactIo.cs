using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PCLCS;

// Original LF host implementation; governed by the repository LICENCE.
internal sealed class MmcArtifactWorkspace : IDisposable {
    private readonly string owner;
    private readonly string runtimeRoot;
    private bool disposed;
    internal string Root { get; }

    internal MmcArtifactWorkspace(MmcInstancePaths paths) {
        runtimeRoot = paths.RuntimeRoot;
        string id = Guid.NewGuid().ToString("N");
        Root = paths.ResolveRelative(MmcPathRole.Runtime, ".staging/" + id);
        owner = "PCLLF.MmcArtifactWorkspace/v1\n" + id + "\n" + paths.InstanceRoot;
        if (Directory.Exists(Root) || File.Exists(Root)) throw new IOException("Artifact workspace already exists.");
        MmcArtifactIo.Directory(Root);
        using var stream = new FileStream(Path.Combine(Root, "owner.txt"), FileMode.CreateNew, FileAccess.Write, FileShare.None);
        byte[] bytes = Encoding.UTF8.GetBytes(owner);
        stream.Write(bytes); stream.Flush(true);
    }

    internal string NewFile() {
        if (disposed) throw new ObjectDisposedException(nameof(MmcArtifactWorkspace));
        string result = Path.Combine(Root, "files", Guid.NewGuid().ToString("N"));
        MmcArtifactIo.Directory(Path.GetDirectoryName(result)!);
        MmcInstancePaths.EnsureNoLinks(result);
        return result;
    }

    public void Dispose() {
        if (disposed) return;
        if (!Directory.Exists(Root)) { disposed = true; return; }
        string parent = Path.GetDirectoryName(Root)!;
        if (!string.Equals(Path.GetDirectoryName(parent), runtimeRoot, StringComparison.OrdinalIgnoreCase) ||
            Path.GetFileName(parent) != ".staging" || !Guid.TryParseExact(Path.GetFileName(Root), "N", out _))
            throw new IOException("Artifact cleanup ownership path changed.");
        MmcInstancePaths.EnsureNoLinks(Root);
        string marker = Path.Combine(Root, "owner.txt");
        MmcInstancePaths.EnsureNoLinks(marker);
        if (!File.Exists(marker) || new FileInfo(marker).Length > 4096 || File.ReadAllText(marker) != owner)
            throw new IOException("Artifact cleanup ownership marker changed; workspace preserved.");
        DeleteOwnedDirectory(Root);
        disposed = true;
    }

    private static void DeleteOwnedDirectory(string path) {
        MmcInstancePaths.EnsureNoLinks(path);
        foreach (string child in System.IO.Directory.EnumerateFileSystemEntries(path)) {
            MmcInstancePaths.EnsureNoLinks(child);
            if ((File.GetAttributes(child) & FileAttributes.Directory) != 0) DeleteOwnedDirectory(child);
            else File.Delete(child);
        }
        System.IO.Directory.Delete(path, false);
    }
}

internal sealed class MmcArtifactIo {
    private static readonly uint[] CrcTable = BuildCrcTable();
    internal static uint Crc32(ReadOnlySpan<byte> bytes) {
        uint crc = uint.MaxValue;
        foreach (byte value in bytes) crc = CrcTable[(crc ^ value) & 255] ^ (crc >> 8);
        return ~crc;
    }
    private readonly MmcArtifactLimits limits;
    private readonly IProgress<MmcArtifactProgress>? progress;
    private long copied;
    internal MmcArtifactIo(MmcArtifactLimits limits, IProgress<MmcArtifactProgress>? progress) { this.limits = limits; this.progress = progress; }

    internal static void Directory(string path) {
        MmcInstancePaths.EnsureNoLinks(path);
        System.IO.Directory.CreateDirectory(path);
        MmcInstancePaths.EnsureNoLinks(path);
    }

    internal static string AuthorizedSource(MmcArtifactSource source) {
        if (!Path.IsPathFullyQualified(source.ApprovedRoot) || !Path.IsPathFullyQualified(source.FilePath))
            throw new IOException("Artifact source must have an absolute approved boundary.");
        if (new[] { source.ApprovedRoot, source.FilePath }.Any(x => x.StartsWith("\\\\?\\", StringComparison.Ordinal) || x.StartsWith("\\\\.\\", StringComparison.Ordinal)))
            throw new IOException("Artifact sources cannot use device namespaces.");
        string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(source.ApprovedRoot));
        string file = Path.GetFullPath(source.FilePath);
        if (!file.StartsWith(root + Path.DirectorySeparatorChar, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            throw new IOException("Artifact source escapes its approved boundary.");
        MmcInstancePaths.EnsureNoLinks(root); MmcInstancePaths.EnsureNoLinks(file);
        if ((File.GetAttributes(file) & FileAttributes.Directory) != 0) throw new IOException("Artifact source is a directory.");
        return file;
    }

    internal static void Relative(string path) {
        if (string.IsNullOrEmpty(path) || path.Length > 32767 || Path.IsPathRooted(path) || path[0] is '/' or '\\')
            throw new IOException("Artifact logical path is invalid.");
        string[] segments = path.Replace('\\', '/').Split('/');
        if (segments.Length > 128 || segments.Any(x => x.Length is 0 or > 255 || x is "." or ".."))
            throw new IOException("Artifact logical path exceeds its segment budget.");
    }

    internal static bool Digest(string value, int length) => value.Length == length && value.All(Uri.IsHexDigit);

    internal static async Task<(string Sha256, long Size)> HashAsync(string path, long maximum, CancellationToken token, string? expectedSha1 = null) {
        MmcInstancePaths.EnsureNoLinks(path);
        await using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 131072, true);
        if (input.Length > maximum) throw new IOException("Artifact byte budget exceeded.");
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        using var sha1 = expectedSha1 is null ? null : IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
        byte[] buffer = new byte[131072]; long size = 0;
        int read;
        while ((read = await input.ReadAsync(buffer, token).ConfigureAwait(false)) != 0) {
            size = checked(size + read);
            if (size > maximum) throw new IOException("Artifact actual byte budget exceeded.");
            hash.AppendData(buffer, 0, read);
            sha1?.AppendData(buffer, 0, read);
        }
        if (expectedSha1 is not null && !Convert.ToHexStringLower(sha1!.GetHashAndReset()).Equals(expectedSha1, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Artifact content does not match its declared SHA1.");
        return (Convert.ToHexStringLower(hash.GetHashAndReset()), size);
    }

    internal async Task<(string Sha256, long Size)> CopyAsync(string source, string target, string key,
        string? expectedSha256, string? expectedSha1, long? expectedSize, CancellationToken token) {
        MmcInstancePaths.EnsureNoLinks(source); MmcInstancePaths.EnsureNoLinks(target);
        Directory(Path.GetDirectoryName(target)!);
        await using var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 131072, true);
        if (input.Length > limits.MaxArtifactBytes) throw new IOException("Artifact declared byte budget exceeded: " + key);
        await using var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None, 131072, true);
        using var sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        using var sha1 = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
        byte[] buffer = new byte[131072]; long size = 0;
        int read;
        while ((read = await input.ReadAsync(buffer, token).ConfigureAwait(false)) != 0) {
            size = checked(size + read); copied = checked(copied + read);
            if (size > limits.MaxArtifactBytes || copied > limits.MaxCopiedBytes) throw new IOException("Artifact actual copy budget exceeded: " + key);
            sha256.AppendData(buffer, 0, read); sha1.AppendData(buffer, 0, read);
            await output.WriteAsync(buffer.AsMemory(0, read), token).ConfigureAwait(false);
            progress?.Report(new("copy", key, copied));
        }
        await output.FlushAsync(token).ConfigureAwait(false); output.Flush(true);
        string digest = Convert.ToHexStringLower(sha256.GetHashAndReset());
        string digest1 = Convert.ToHexStringLower(sha1.GetHashAndReset());
        if ((expectedSha256 is not null && !digest.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase)) ||
            (expectedSha1 is not null && !digest1.Equals(expectedSha1, StringComparison.OrdinalIgnoreCase)) ||
            (expectedSize is not null && expectedSize != size))
            throw new IOException("Artifact content does not match the locked/declared digest or size: " + key);
        return (digest, size);
    }

    internal async Task<(string Sha256, long Size)> ExtractAsync(Stream input, string target, string key,
        long expectedSize, uint expectedCrc32, long maximum, CancellationToken token) {
        MmcInstancePaths.EnsureNoLinks(target); Directory(Path.GetDirectoryName(target)!);
        await using var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None, 131072, true);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[] buffer = new byte[131072]; long size = 0; uint crc = uint.MaxValue;
        int read;
        while ((read = await input.ReadAsync(buffer, token).ConfigureAwait(false)) != 0) {
            size = checked(size + read); copied = checked(copied + read);
            if (size > maximum || size > expectedSize || copied > limits.MaxCopiedBytes) throw new IOException("Native actual output budget or declared size exceeded.");
            hash.AppendData(buffer, 0, read);
            for (int index = 0; index < read; index++) crc = CrcTable[(crc ^ buffer[index]) & 255] ^ (crc >> 8);
            await output.WriteAsync(buffer.AsMemory(0, read), token).ConfigureAwait(false);
            progress?.Report(new("native-extract", key, copied));
        }
        if (size != expectedSize || ~crc != expectedCrc32) throw new IOException("Native actual output differs from its declared size or CRC32.");
        await output.FlushAsync(token).ConfigureAwait(false); output.Flush(true);
        return (Convert.ToHexStringLower(hash.GetHashAndReset()), size);
    }

    private static uint[] BuildCrcTable() {
        var table = new uint[256];
        for (uint index = 0; index < 256; index++) {
            uint value = index;
            for (int bit = 0; bit < 8; bit++) value = (value & 1) != 0 ? 0xedb88320U ^ (value >> 1) : value >> 1;
            table[index] = value;
        }
        return table;
    }
}
