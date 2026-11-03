using System;
using System.IO;

namespace PCLCS;

/// <summary>保留 LF 默认 Framework 路径检查；完整归约仍使用系统 Path。</summary>
public static class InstanceResourcePathCompatibility {
    public static string GetFullPath(string path) {
#if NETFRAMEWORK
        return Path.GetFullPath(path);
#else
        if (Path.DirectorySeparatorChar != '\\') return Path.GetFullPath(path);
        if (path is null) return Path.GetFullPath(path!);
        // Framework 在 extended 路径的 NUL、空白和长度检查之前直接返回。
        if (IsExtended(path)) return path;
        if (path.IndexOf('\0') >= 0) throw InvalidCharacters();
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("The path is not of a legal form.");

        bool checkCharacters = !IsDevice(path);
        string normalized = Path.GetFullPath(SkipLeadingRootSpaces(path)).TrimEnd(TrimEndCharacters);
        ValidateNormalizedPath(normalized, checkCharacters);
        if (!IsDevice(normalized)) {
            if (normalized.IndexOfAny(new[] { '?', '*' }) >= 0) throw InvalidCharacters();
            int colon = normalized.IndexOf(':');
            if (colon >= 0 && (colon != 1 || !IsDriveLetter(normalized[0]) || normalized.IndexOf(':', colon + 1) >= 0))
                throw new NotSupportedException("The given path's format is not supported.");
        }
        return normalized;
#endif
    }

    // GetDirectoryName 的设备根和受限长度边界等待扩展 CLR4 oracle；此批不重写其解析器。
    public static string? GetDirectoryName(string? path) => Path.GetDirectoryName(path);

#if !NETFRAMEWORK
    // 根据下列 MIT 来源的公开行为独立补充检查，未移植其内部归约／短名解析器：
    // microsoft/referencesource ec9fa9ae770d522a5b5f0607898044b7478574a3
    // mscorlib/system/io/path.cs、pathinternal.cs、security/permissions/fileiopermission.cs。
    // dotnet/coreclr abbb8f685929c7aeaa087dae46fedc1bc2af4b17
    // src/mscorlib/src/System/IO/Path.cs、LongPathHelper.cs。
    // LF 默认 UseLegacyPathHandling／BlockLongPaths 均为 false；Framework 自身处理其原有开关。
    private static readonly char[] TrimEndCharacters = { '\t', '\n', '\v', '\f', '\r', ' ', '\u0085', '\u00a0' };

    private static void ValidateNormalizedPath(string path, bool checkCharacters) {
        if (path.Length >= 32767) throw new PathTooLongException();
        int componentStart = 0;
        for (int index = 0; index < path.Length; index++) {
            char value = path[index];
            if (checkCharacters && (value < 32 || value is '"' or '<' or '>' or '|')) throw InvalidCharacters();
            if (value == '\\') {
                if (index - componentStart > 255) throw new PathTooLongException();
                componentStart = index + 1;
            }
        }
        if (path.Length - componentStart > 255) throw new PathTooLongException();
    }

    private static string SkipLeadingRootSpaces(string path) {
        int start = 0;
        while (start < path.Length && path[start] == ' ') start++;
        if (start == 0 || start == path.Length) return path;
        bool rooted = IsSeparator(path[start]);
        bool drive = start + 1 < path.Length && IsDriveLetter(path[start]) && path[start + 1] == ':';
        return rooted || drive ? path.Substring(start) : path;
    }

    private static bool IsExtended(string path) => path.StartsWith(@"\\?\", StringComparison.Ordinal) || path.StartsWith(@"\??\", StringComparison.Ordinal);

    private static bool IsDevice(string path) => IsExtended(path) ||
        (path.Length >= 4 && IsSeparator(path[0]) && IsSeparator(path[1]) && (path[2] == '.' || path[2] == '?') && IsSeparator(path[3]));

    private static bool IsSeparator(char value) => value == '\\' || value == '/';
    private static bool IsDriveLetter(char value) => value is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
    private static ArgumentException InvalidCharacters() => new("Illegal characters in path.");
#endif
}
