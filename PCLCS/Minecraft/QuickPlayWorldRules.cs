using System;
using System.Collections.Generic;
using System.IO;

namespace PCLCS;

/// <summary>原 Quick Play 的存档路径检查与启动参数移除；不扩展版本或启动规则。</summary>
public static class QuickPlayWorldRules {
    public static string WorldPath(string gameFolder, string? identifier, Action<string, string> ensureWithin) {
        if (string.IsNullOrWhiteSpace(identifier) || identifier == "." || identifier == ".." || LegacyFileName(identifier!) != identifier || identifier!.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new IOException("存档文件夹名称无效。");
        CheckLegacyPathCharacters(gameFolder);
        string root = Path.Combine(gameFolder, "saves");
        string world = Path.Combine(root, identifier!);
        ensureWithin(root, world);
        if (!Directory.Exists(world) || !File.Exists(Path.Combine(world, "level.dat")))
            throw new IOException("该存档已移动、删除或尚未完整生成，请刷新存档列表。");
        return world;
    }

    private static string LegacyFileName(string path) {
        CheckLegacyPathCharacters(path);
        return Path.GetFileName(path);
    }

    private static void CheckLegacyPathCharacters(string? path) {
#if !NETFRAMEWORK
        // Framework 在 GetFileName／Combine 前抛 ArgumentException；日志只捕获 IOException。
        // 现代 Windows 保留这个错误边界；Framework 自身仍负责其原始系统文案。
        if (path is null || Path.DirectorySeparatorChar != '\\') return;
        bool legacy = AppContext.TryGetSwitch("Switch.System.IO.UseLegacyPathHandling", out bool enabled) && enabled;
        bool device = path.Length >= 4 &&
            ((path[0] == '\\' && (path[1] == '\\' || path[1] == '?') && path[2] == '?' && path[3] == '\\') ||
             (IsSeparator(path[0]) && IsSeparator(path[1]) && (path[2] == '.' || path[2] == '?') && IsSeparator(path[3])));
        // 现代 GetInvalidPathChars 的集合也已变化，不能用它替代旧集合。
        if (legacy || !device) {
            foreach (char value in path) {
                if (value < 32 || value is '"' or '<' or '>' or '|')
                    throw new ArgumentException("Illegal characters in path.");
            }
        }
#endif
    }

#if !NETFRAMEWORK
    private static bool IsSeparator(char value) => value == '\\' || value == '/';
#endif

    public static void RemoveArguments(List<string> arguments, IEnumerable<string> names) {
        HashSet<string> setNames = new(names, StringComparer.Ordinal);
        int index = 0;
        while (index < arguments.Count) {
            if (setNames.Contains(arguments[index])) {
                arguments.RemoveAt(index);
                if (index < arguments.Count && !arguments[index].StartsWith("--", StringComparison.Ordinal)) arguments.RemoveAt(index);
            } else {
                index++;
            }
        }
    }
}
