using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace PCLCS;

public sealed partial class ModDependencyOnlinePipeline<TContext, TMod, TDependency, TEdge, TIssue, TImpact, TReport, TOnlineProject, TOnlineDependency, TOnlineFile, TOptions, TCache> {
    // LF ModDependencyOnline.vb 221-352: bounded local file hashes and streamed
    // CurseForge MurmurHash2. No INI cache, metadata-policy changes or live request.
    private static List<FileData> ReadFiles(string modsFolder, Session work) {
        List<FileData> result = new();
        List<string> paths = new();
        string root = Path.GetFullPath(modsFolder);
        if (!Directory.Exists(root) || (File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0) return result;
        paths.AddRange(Directory.EnumerateFiles(root, "*", SearchOption.TopDirectoryOnly).Where(IsModFile));
        string? legacy = work.Report.Context?.LegacyForgeVersionFolder;
        if (!string.IsNullOrEmpty(legacy) && Regex.IsMatch(legacy, "^[0-9.]+$")) {
            string folder = Path.Combine(root, legacy);
            if (Directory.Exists(folder) && (File.GetAttributes(folder) & FileAttributes.ReparsePoint) == 0)
                paths.AddRange(Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly).Where(IsModFile));
        }
        Dictionary<string, bool> targets = paths.ToDictionary(value => value,
            value => work.TargetFile is not null ? SameFilePath(value, work.TargetFile) :
                IsEnabledFile(value) && (work.Options.ComparePlatformDependencies || NeedsSupplement(value, work.Report)), StringComparer.OrdinalIgnoreCase);
        if (!targets.Values.Any(value => value)) return result;
        long total = 0;
        foreach (string fullPath in paths.OrderByDescending(value => targets[value]).ThenBy(value => value, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, Math.Min(512, work.Options.MaxFiles)))) {
            work.Check();
            try {
                FileInfo info = new(fullPath);
                if ((info.Attributes & FileAttributes.ReparsePoint) != 0) continue;
                if (info.Length > Math.Max(1, work.Options.MaxFileBytes) || info.Length > checked(Math.Max(1, work.Options.MaxTotalBytes) - total)) {
                    work.Note("部分文件超过联网哈希读取上限，未据此判定缺失依赖。");
                    continue;
                }
                total = checked(total + info.Length);
                result.Add(HashFile(info, targets[fullPath], work));
            } catch (OperationCanceledException) { throw; }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException) {
                work.Note("部分本地文件无法读取，未据此判定缺失依赖。");
            }
        }
        if (paths.Count > Math.Max(1, Math.Min(512, work.Options.MaxFiles))) work.Note("联网补充达到文件数量上限；本次平台映射可能不完整。");
        return result;
    }

    private static bool IsModFile(string value) {
        if (value.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase) || value.EndsWith(".old", StringComparison.OrdinalIgnoreCase))
            value = Path.GetFileNameWithoutExtension(value)!;
        return new[] { ".jar", ".zip", ".litemod" }.Contains(Path.GetExtension(value)!.ToLowerInvariant());
    }

    private static bool IsEnabledFile(string value) => !value.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase) && !value.EndsWith(".old", StringComparison.OrdinalIgnoreCase);

    private static string? ExplicitTarget(string? value, string modsFolder, TReport report, bool comparePlatformDependencies) {
        if (string.IsNullOrWhiteSpace(value)) return null;
        try {
            string fullPath = Path.GetFullPath(value!), root = Path.GetFullPath(modsFolder), parent = Path.GetDirectoryName(fullPath)!;
            if (!File.Exists(fullPath) || !Directory.Exists(root) || !IsModFile(fullPath)) return null;
            bool allowed = SameFilePath(parent.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            string? legacy = report.Context?.LegacyForgeVersionFolder;
            if (!allowed && !string.IsNullOrEmpty(legacy) && Regex.IsMatch(legacy, "^[0-9.]+$")) allowed = SameFilePath(parent, Path.Combine(root, legacy));
            if (!allowed || (File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0 || (File.GetAttributes(parent) & FileAttributes.ReparsePoint) != 0 ||
                (File.GetAttributes(fullPath) & FileAttributes.ReparsePoint) != 0) return null;
            return comparePlatformDependencies || NeedsSupplement(fullPath, report) ? fullPath : null;
        } catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException || error is NotSupportedException || error is SecurityException) {
            return null;
        }
    }

    private static bool NeedsSupplement(string fullPath, TReport report) {
        List<TMod> mods = report.Mods.Where(item => SameFilePath(item.RootPath, fullPath)).ToList();
        if (mods.Count == 0 || mods.Any(item => item.Format == "LiteLoader" || item.Format == "Forge (旧版)")) return true;
        string name = Path.GetFileName(fullPath)!;
        return report.Notices.Any(value => value.StartsWith(name + "：", StringComparison.OrdinalIgnoreCase) || value.StartsWith(name + "!", StringComparison.OrdinalIgnoreCase));
    }

    private static FileData HashFile(FileInfo info, bool target, Session work) {
        long length = info.Length;
        DateTime written = info.LastWriteTimeUtc;
        FileData result = new() {
            Length = length,
            Info = new TOnlineFile { FullPath = info.FullName, IsSupplementTarget = target,
                Enabled = !info.Name.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase) && !info.Name.EndsWith(".old", StringComparison.OrdinalIgnoreCase) }
        };
        using (FileStream input = new(info.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (SHA1 sha = SHA1.Create())
        using (MD5 md5 = MD5.Create()) {
            byte[] buffer = new byte[65536];
            while (true) {
                work.Check();
                int count = input.Read(buffer, 0, buffer.Length);
                if (count == 0) break;
                sha.TransformBlock(buffer, 0, count, buffer, 0);
                md5.TransformBlock(buffer, 0, count, buffer, 0);
                for (int index = 0; index < count; index++)
                    if (!IsWhitespace(buffer[index])) result.FilteredLength = checked(result.FilteredLength + 1);
            }
            sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            md5.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            result.Info.Sha1 = Hex(sha.Hash!);
            result.Md5 = Hex(md5.Hash!);
        }
        info.Refresh();
        if (info.Length != length || info.LastWriteTimeUtc != written) throw new IOException("File changed during hashing.");
        return result;
    }

    private static string Hex(byte[] bytes) => BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    private static bool IsWhitespace(byte value) => value == 9 || value == 10 || value == 13 || value == 32;

    // Same seed, whitespace filter and MurmurHash2 as LF LocalResourceFile.CurseForgeHash.
    private static uint Fingerprint(FileData file, Session work) {
        if (file.HasFingerprint) return file.Fingerprint;
        if (work.Options.Fingerprint is not null) file.Fingerprint = work.Options.Fingerprint(file.Info.FullPath);
        else {
            uint hash = 1U ^ checked((uint)file.FilteredLength), word = 0;
            int count = 0;
            using (FileStream input = File.OpenRead(file.Info.FullPath)) {
                byte[] buffer = new byte[65536];
                while (true) {
                    work.Check();
                    int read = input.Read(buffer, 0, buffer.Length);
                    if (read == 0) break;
                    for (int index = 0; index < read; index++) {
                        if (IsWhitespace(buffer[index])) continue;
                        word |= (uint)buffer[index] << (8 * count);
                        count = checked(count + 1);
                        if (count != 4) continue;
                        word = unchecked(word * 0x5BD1E995U);
                        word ^= word >> 24;
                        word = unchecked(word * 0x5BD1E995U);
                        hash = unchecked(hash * 0x5BD1E995U) ^ word;
                        word = 0U;
                        count = 0;
                    }
                }
            }
            if (count > 0) hash = unchecked((hash ^ word) * 0x5BD1E995U);
            hash ^= hash >> 13;
            hash = unchecked(hash * 0x5BD1E995U);
            file.Fingerprint = hash ^ (hash >> 15);
        }
        file.HasFingerprint = true;
        return file.Fingerprint;
    }
}
