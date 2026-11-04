using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MeloongCore;
using MeloongCore.Extensions;

namespace PCLCS;

/// <summary>Only the platform inputs used by LF's original folder-list workload; list reads and writes retain their actual references.</summary>
public sealed class LegacyMinecraftFolderLoaderHost<TFolder> {
    public Func<string> BasePath = () => Paths.Base;
    public Func<string> AppDataPath = () => Paths.AppData;
    public Func<bool> ModpackEnabled = null!;
    public Func<List<TFolder>> PackFolders = null!;
    public Func<List<TFolder>> Folders = null!;
    public Action<List<TFolder>> StoreFolders = null!;
    public Func<string, string?> ReadString = null!;
    public Func<string, bool> ReadBoolean = null!;
    public Action<string, string?> WriteString = null!;
    public Action<string> CreateLauncherProfiles = LegacyMinecraftLauncherProfiles.Create;
    public Action<string> CheckPermission = null!;
    public Action<string, bool> Hint = null!;
    public Action<string, string, bool> Message = null!;
    public Action<int, int> RandomDelay = null!;
    public Action<Exception, string> Warn = (ex, message) => Logger.Warn(ex, message);
    public Action<Exception, string> Error = (ex, message) => Logger.Error(ex, message);
}

/// <summary>The complete LF folder-list workload, callable without any launcher instance, library or download DTOs.</summary>
public sealed class LegacyMinecraftFolderLoader<TFolder, TFolderKind>(LegacyMinecraftFolderLoaderHost<TFolder> host)
    where TFolder : LegacyMinecraftFolder<TFolderKind>, new() where TFolderKind : struct, Enum {
    private TFolder Folder(string name, string location, int type) => new() {
        Name = name, Location = location, Type = LegacyMinecraftCodes.Value<TFolderKind>(type)
    };

    public void Load() {
        try {
            if (host.ModpackEnabled()) {
                host.StoreFolders(host.PackFolders());
                DirectoryUtils.Create(host.Folders()[0].Location + "versions\\");
                host.CreateLauncherProfiles(host.Folders()[0].Location);
                return;
            }
            List<TFolder> cacheFolders = [];
            try {
                if (DirectoryUtils.Exists(host.BasePath() + "versions\\")) cacheFolders.Add(Folder("当前文件夹", host.BasePath(), 0));
                foreach (DirectoryInfo folder in DirectoryUtils.GetInfo(host.BasePath()).GetDirectories()) {
                    if (DirectoryUtils.Exists(folder.FullName + "versions\\") || folder.Name == ".minecraft") {
                        cacheFolders.Add(Folder("当前文件夹", PathUtils.AddSlashSuffix(PathUtils.ForCompare(folder.FullName)), 0));
                    }
                }
            } catch (Exception ex) {
                host.Warn(ex, "扫描 PCL 所在文件夹中是否有 MC 文件夹失败");
            }
            string mojangPath = host.AppDataPath() + ".minecraft\\";
            if ((!cacheFolders.Any() || !PathUtils.IsSame(mojangPath, cacheFolders[0].Location)) && DirectoryUtils.Exists(mojangPath + "versions\\"))
                cacheFolders.Add(Folder("官方启动器文件夹", mojangPath, 0));

            foreach (string folder in host.ReadString("LaunchFolders")!.ToString().Split('|', true)) {
                if (folder == "") continue;
                if (!folder.Contains(">") || !folder.EndsWithF("\\")) {
                    host.Hint("无效的 Minecraft 文件夹：" + folder, true);
                    continue;
                }
                string folderName = folder.BeforeFirst(">");
                string folderPath = folder.AfterFirst(">");
                try {
                    host.CheckPermission(folderPath);
                    bool renamed = false;
                    foreach (TFolder originalFolder in cacheFolders) {
                        if (PathUtils.IsSame(originalFolder.Location, folderPath)) {
                            originalFolder.Name = folderName;
                            originalFolder.Type = LegacyMinecraftCodes.Value<TFolderKind>(1);
                            renamed = true;
                        }
                    }
                    if (!renamed) cacheFolders.Add(Folder(folderName, PathUtils.AddSlashSuffix(PathUtils.ForCompare(folderPath)), 2));
                } catch (Exception ex) {
                    host.Message("失效的 Minecraft 文件夹：\r\n" + folderPath + "\r\n\r\n" + ex.GetDisplay(false), "Minecraft 文件夹失效", true);
                    host.Warn(ex, $"无法访问 Minecraft 文件夹 {folderPath}");
                }
            }
            List<string> newSetup = [];
            foreach (TFolder folder in cacheFolders) {
                if (LegacyMinecraftCodes.Number(folder.Type) != 0) newSetup.Add(folder.Name + ">" + folder.Location);
            }
            if (!newSetup.Any()) newSetup.Add("");
            if (!host.ModpackEnabled()) host.WriteString("LaunchFolders", newSetup.Join('|'));
            if (!cacheFolders.Any()) {
                DirectoryUtils.Create(host.BasePath() + ".minecraft\\versions\\");
                cacheFolders.Add(Folder("当前文件夹", host.BasePath() + ".minecraft\\", 0));
            }
            foreach (TFolder folder in cacheFolders) host.CreateLauncherProfiles(folder.Location);
            if (host.ReadBoolean("SystemDebugDelay")) host.RandomDelay(200, 2000);
            host.StoreFolders(host.ModpackEnabled() ? host.PackFolders() : cacheFolders);
        } catch (Exception ex) {
            host.Error(ex, "加载 Minecraft 文件夹列表失败");
        }
    }

}

/// <summary>The existing LF launcher_profiles.json writer shared by the native folder loader and the legacy public entry point.</summary>
public static class LegacyMinecraftLauncherProfiles {
    public static void Create(string folder) {
        try {
            if (FileUtils.Exists(folder + "launcher_profiles.json")) return;
            // Keep the original LF text and GB18030 file encoding, including its fixed clientToken.
            string json = "{\n    \"profiles\":  {\n        \"PCL\": {\n            \"icon\": \"Grass\",\n            \"name\": \"PCL\",\n            \"lastVersionId\": \"latest-release\",\n            \"type\": \"latest-release\",\n            \"lastUsed\": \"" +
                DateTime.Now.ToString("yyyy'-'MM'-'dd") + "T" + DateTime.Now.ToString("HH':'mm':'ss") +
                ".0000Z\"\n        }\n    },\n    \"selectedProfile\": \"PCL\",\n    \"clientToken\": \"23323323323323323323323323323333\"\n}";
            FileUtils.Write(folder + "launcher_profiles.json", json, encoding: LauncherProfileEncoding());
            Logger.Info($"已创建 launcher_profiles.json：{folder}");
        } catch (Exception ex) {
            Logger.Error(ex, $"创建 launcher_profiles.json 失败（{folder}）");
        }
    }

    private static Encoding LauncherProfileEncoding() {
#if NETFRAMEWORK
        return Encoding.GetEncoding("GB18030");
#else
        // The LF profile format remains GB18030 on modern .NET without changing the process-wide encoding registry.
        return CodePagesEncodingProvider.Instance.GetEncoding(54936) ?? Encoding.GetEncoding("GB18030");
#endif
    }

}
