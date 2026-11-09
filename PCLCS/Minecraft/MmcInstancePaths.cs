using System;
using System.IO;
using System.Linq;

namespace PCLCS;

// Original LF host implementation; governed by the repository LICENCE.
public enum MmcPathRole { Instance, Game, LocalLibraries, Runtime }

/// <summary>
/// Read-only path snapshot for a new managed MMC instance. Validation does not acquire a lease or
/// authorize a later write: callers must retain the operation gate and revalidate at actual I/O.
/// </summary>
public sealed class MmcInstancePaths {
    public string VersionRoot { get; }
    public string InstanceRoot => VersionRoot;
    public string GameRoot { get; }
    public string LocalLibraryRoot { get; }
    public string RuntimeRoot { get; }
    public string GameRootLeaf { get; }
    public bool HasInactiveGameTree { get; }

    private MmcInstancePaths(string root, string leaf, bool hasInactiveTree) {
        VersionRoot = root; GameRootLeaf = leaf; HasInactiveGameTree = hasInactiveTree;
        GameRoot = Path.Combine(root, leaf);
        LocalLibraryRoot = Path.Combine(root, "libraries");
        RuntimeRoot = Path.Combine(root, "PCL", "MmcRuntime");
    }

    /// <summary>Supply the persisted leaf for installed instances; null selects the source-package default.</summary>
    public static MmcInstancePaths Resolve(string versionRoot, string? gameRootLeaf = null) {
        string root = ValidateVersionRoot(versionRoot);
        string minecraft = Path.Combine(root, "minecraft"), dotMinecraft = Path.Combine(root, ".minecraft");
        bool hasMinecraft = CheckDirectoryRole(minecraft), hasDotMinecraft = CheckDirectoryRole(dotMinecraft);
        if (gameRootLeaf is not null and not ("minecraft" or ".minecraft")) throw new IOException("游戏目录叶名不受支持。");
        string leaf = gameRootLeaf ?? (hasMinecraft ? "minecraft" : hasDotMinecraft ? ".minecraft" : "minecraft");
        CheckDirectoryRole(Path.Combine(root, "libraries"));
        CheckDirectoryRole(Path.Combine(root, "PCL"));
        CheckDirectoryRole(Path.Combine(root, "PCL", "MmcRuntime"));
        return new(root, leaf, leaf == "minecraft" ? hasDotMinecraft : hasMinecraft);
    }

    /// <summary>Returns a checked, role-relative target without creating it or merging library scopes.</summary>
    public string ResolveRelative(MmcPathRole role, string relative) {
        string target = ComputeRelativePath(role, relative);
        EnsureNoLinks(target);
        return target;
    }

    /// <summary>
    /// Private initial-payload bookkeeping only. Keeps all logical path validation; the reservation
    /// must check the source at VerifyPayloadFile and the destination parent at actual placement.
    /// This does not authorize I/O and must not replace ResolveRelative at other call sites.
    /// </summary>
    internal string ResolvePrivatePayloadPath(string relative) => ComputeRelativePath(MmcPathRole.Instance, relative);

    private string ComputeRelativePath(MmcPathRole role, string relative) {
        ArgumentNullException.ThrowIfNull(relative);
        if (relative.Length == 0 || Path.IsPathRooted(relative) || relative[0] is '/' or '\\') throw new IOException("必须提供相对路径。");
        string[] parts = relative.Replace('\\', '/').Split('/');
        foreach (string part in parts) ValidateSegment(part);
        string root = role switch {
            MmcPathRole.Instance => InstanceRoot, MmcPathRole.Game => GameRoot,
            MmcPathRole.LocalLibraries => LocalLibraryRoot, MmcPathRole.Runtime => RuntimeRoot,
            _ => throw new ArgumentOutOfRangeException(nameof(role))
        };
        string target = Path.GetFullPath(Path.Combine(root, Path.Combine(parts)));
        string prefix = root + Path.DirectorySeparatorChar;
        if (!target.StartsWith(prefix, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            throw new IOException("目标路径超出了所选目录角色。");
        return target;
    }

    internal static string ValidateVersionRoot(string versionRoot) {
        if (string.IsNullOrWhiteSpace(versionRoot) || !Path.IsPathFullyQualified(versionRoot)) throw new IOException("版本目录必须是绝对路径。");
        if (versionRoot.StartsWith("\\\\?\\", StringComparison.Ordinal) || versionRoot.StartsWith("\\\\.\\", StringComparison.Ordinal))
            throw new IOException("不接受设备命名空间路径。");
        string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(versionRoot));
        string volumeRoot = Path.GetPathRoot(root)!;
        foreach (string segment in root[volumeRoot.Length..].Replace('\\', '/').Split('/')) ValidateSegment(segment);
        string? parent = Path.GetDirectoryName(root);
        if (!string.Equals(Path.GetFileName(parent), "versions", OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            throw new IOException("版本必须位于 Minecraft 的 versions 目录中。");
        CheckDirectoryRole(root);
        return root;
    }

    internal static bool CheckDirectoryRole(string path) {
        EnsureNoLinks(path);
        FileAttributes? attributes = Attributes(path);
        if (attributes is null) return false;
        if ((attributes.Value & FileAttributes.Directory) == 0) throw new IOException("目录角色被普通文件占用。");
        return true;
    }

    internal static void EnsureNoLinks(string path) {
        string? current = Path.GetFullPath(path);
        while (current is not null) {
            FileAttributes? attributes = Attributes(current);
            if (attributes is not null && (attributes.Value & FileAttributes.ReparsePoint) != 0)
                throw new IOException("MMC 路径包含链接或重解析点。");
            if (attributes is not null && (attributes.Value & FileAttributes.Directory) == 0 && current != path)
                throw new IOException("路径的父目录被普通文件占用。");
            current = Path.GetDirectoryName(current);
        }
    }

    internal static FileAttributes? Attributes(string path) {
        try { return File.GetAttributes(path); }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
    }

    private static void ValidateSegment(string segment) {
        if (string.IsNullOrEmpty(segment) || segment is "." or ".." || segment.EndsWith(' ') || segment.EndsWith('.') ||
            segment.Any(c => c < 32 || "<>:\"|?*".Contains(c))) throw new IOException("路径含有非法或不明确的片段。");
        string stem = segment.Split('.')[0];
        if (new[] { "CON", "PRN", "AUX", "NUL", "CONIN$", "CONOUT$" }.Contains(stem, StringComparer.OrdinalIgnoreCase) ||
            (stem.Length == 4 && (stem.StartsWith("COM", StringComparison.OrdinalIgnoreCase) || stem.StartsWith("LPT", StringComparison.OrdinalIgnoreCase)) &&
                (stem[3] is >= '1' and <= '9' or '\u00b9' or '\u00b2' or '\u00b3')))
            throw new IOException("路径包含 Windows 保留设备名。");
    }
}
