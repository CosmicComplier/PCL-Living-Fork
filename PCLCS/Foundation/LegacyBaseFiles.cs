using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using MeloongCore;
using MeloongCore.Extensions;
using Microsoft.VisualBasic.CompilerServices;

namespace PCLCS;

public static partial class LegacyBaseOperations {
    private static string[] ReadIniLines(string fileName) {
#if NETFRAMEWORK
        return FileUtils.ReadAsLines(fileName, true);
#else
        return ReadLegacyText(fileName).SplitLines(true);
#endif
    }
    private static string ReadLegacyText(string fileName) {
#if NETFRAMEWORK
        return FileUtils.ReadAsString(fileName);
#else
        // Reuse Shared's BOM/strict-UTF8 detection with the same single IO read. Its only named
        // encoding lookup is GB18030; modern .NET requires the existing code-page provider there.
        byte[] bytes = FileUtils.ReadAsBytes(fileName);
        try { return bytes.GetString(); }
        catch (ArgumentException ex) when (ex.ParamName == "name") {
            Encoding encoding = CodePagesEncodingProvider.Instance.GetEncoding("GB18030") ?? Encoding.GetEncoding("GB18030");
            return encoding.GetString(bytes);
        }
#endif
    }
    public static void IniClearCache(string fileName, ConcurrentDictionary<string, ConcurrentDictionary<string, string>> cache) {
        if (!fileName.Contains(@":\")) fileName = $@"{Paths.Base}PCL\{fileName}.ini";
        cache.Remove(fileName);
    }
    public static ConcurrentDictionary<string, string>? IniGetContent(string fileName, ConcurrentDictionary<string, ConcurrentDictionary<string, string>> cache) {
        try {
            if (!fileName.Contains(@":\")) fileName = $@"{Paths.Base}PCL\{fileName}.ini";
            if (cache.TryGetValue(fileName, out ConcurrentDictionary<string, string>? content)) return content;
            if (!FileUtils.Exists(fileName)) return null;
            ConcurrentDictionary<string, string> ini = new();
            foreach (string line in ReadIniLines(fileName)) {
                int index = line.IndexOfF(":");
                if (index > 0) ini[line.Substring(0, index)] = line.Substring(checked(index + 1));
            }
            cache[fileName] = ini; return ini;
        } catch (Exception ex) { Logger.Error(ex, $"生成 ini 文件缓存失败（{fileName}）", LogBehavior.Toast); return null; }
    }
    public static string? ReadIni(string fileName, string key, string? defaultValue, ConcurrentDictionary<string, ConcurrentDictionary<string, string>> cache) {
        ConcurrentDictionary<string, string>? content = IniGetContent(fileName, cache);
        if (content is null) return defaultValue;
        return content.GetOrDefault(key, defaultValue);
    }
    public static bool HasIniKey(string fileName, string key, ConcurrentDictionary<string, ConcurrentDictionary<string, string>> cache) {
        ConcurrentDictionary<string, string>? content = IniGetContent(fileName, cache);
        return content is not null && content.ContainsKey(key);
    }
    public static void WriteIni(string fileName, string key, string? value, ConcurrentDictionary<string, ConcurrentDictionary<string, string>> cache) {
        try {
            if (key.Contains(":")) throw new Exception($"尝试写入 ini 文件 {fileName} 的键名中包含了冒号：{key}");
            key = key.ReplaceLineEndings(""); value = value?.ReplaceLineEndings("");
            ConcurrentDictionary<string, string> content = IniGetContent(fileName, cache) ?? new ConcurrentDictionary<string, string>();
            if (value is null) { if (!content.Remove(key)) return; }
            else {
                if (content.TryGetValue(key, out string? existing) && Operators.CompareString(existing, value, false) == 0) return;
                content[key] = value;
            }
            StringBuilder fileContent = new();
            foreach (var pair in content) fileContent.Append(pair.Key).Append(":").Append(pair.Value).Append("\r\n");
            if (!fileName.Contains(@":\")) fileName = $@"{Paths.Base}PCL\{fileName}.ini";
            fileName = fileName.Contains(@":\") ? fileName : Paths.Base + fileName;
            FileUtils.Write(fileName, fileContent.ToString());
        } catch (Exception ex) { Logger.Error(ex, $"写入文件失败（{fileName} → {key}:{value}）", LogBehavior.Toast); }
    }
    public static bool CheckPermission(string? folder, Func<int> newUuid) {
        try {
            if (string.IsNullOrEmpty(folder)) return false;
            folder = PathUtils.AddSlashSuffix(folder!);
            if (folder.EndsWithF(@":\System Volume Information\", true) || folder.EndsWithF(@":\$RECYCLE.BIN\", true)) return false;
            if (!DirectoryUtils.Exists(folder)) return false;
            string file = $"{folder}CheckPermission{newUuid()}.txt";
            FileUtils.Write(file, "临时文件，用于检测该文件夹的权限是否正常。"); FileUtils.Delete(file); return true;
        } catch (Exception ex) { Logger.Warn(ex, $"没有对文件夹 {folder} 的权限，请尝试以管理员权限运行 PCL"); return false; }
    }
    public static void CheckPermissionWithException(string? folder, Func<int> newUuid) {
        if (string.IsNullOrWhiteSpace(folder)) throw new ArgumentNullException("文件夹名不能为空！");
        folder = PathUtils.AddSlashSuffix(folder!);
        if (folder.EndsWithF(@":\System Volume Information\", true) || folder.EndsWithF(@":\$RECYCLE.BIN\", true)) throw new UnauthorizedAccessException("没有对系统文件夹的权限！");
        if (!DirectoryUtils.Exists(folder)) throw new DirectoryNotFoundException("文件夹不存在！");
        string file = $"{folder}CheckPermission{newUuid()}.txt";
        FileUtils.Write(file, "临时文件，用于检测该文件夹的权限是否正常。"); FileUtils.Delete(file);
    }
    public static void ExtractResources(string filePath, string resourceName, Func<string, object?> readResource) {
        object? resource = readResource(resourceName);
        if (resource is byte[] bytes) {
            if (FileUtils.Exists(filePath) && CryptographyUtils.ComputeFileHash(filePath) == CryptographyUtils.ComputeHash(bytes)) return;
            Logger.Info($"将资源写入到文件：{resourceName} → {filePath}"); FileUtils.Write(filePath, bytes);
        } else if (resource is string content) {
            if (FileUtils.Exists(filePath) && Operators.CompareString(ReadLegacyText(filePath), content, false) == 0) return;
            Logger.Info($"将资源写入到文件：{resourceName} → {filePath}"); FileUtils.Write(filePath, content);
        } else throw new Exception($"资源 {resourceName} 的类型不支持：{resource!.GetType().Name}");
    }
}
