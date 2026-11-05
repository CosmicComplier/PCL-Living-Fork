using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using MeloongCore;
using MeloongCore.Extensions;

namespace PCLCS;

public sealed partial class ModpackExportPipeline<TInstance, TLocal, TProject, TVersion>
    where TInstance : class where TLocal : LocalResourceFileData<TProject, TVersion>
    where TProject : ResourceProjectModelData where TVersion : ResourceVersionModelData {
    private readonly ModpackExportHost<TInstance, TLocal, TProject, TVersion> host;
    public ModpackExportPipeline(ModpackExportHost<TInstance, TLocal, TProject, TVersion> host) { this.host = host; }
    public static bool Like(string input, string rule) => LikeOperator.LikeString(input.Lower(), rule.Lower(), CompareMethod.Binary);
    public static IEnumerable<string> StandardizeLines(IEnumerable<string> raw, bool suffixStar) {
        foreach (string source in raw) {
            string line = source.Trim();
            if (line == "" || line.StartsWithF("#") || line.StartsWithF("=")) continue;
            line = line.Replace("/", "\\");
            yield return line + (line.EndsWithF("\\") && suffixStar ? "*" : "");
        }
    }
    public void Copy(ModpackExportRequest<TInstance> request, ModpackExportCopyTask<TLocal> task) {
        task.SetOutput(new List<TLocal>());
        List<string> versionOnlyFolders = new();
        int progress = 0;
        void SearchFolder(DirectoryInfo folder) {
            foreach (DirectoryInfo sub in folder.EnumerateDirectories("*", SearchOption.TopDirectoryOnly)) {
                if (sub.Name.Equals("PackUpdateBackups", StringComparison.OrdinalIgnoreCase) && folder.Name.Equals("PCL", StringComparison.OrdinalIgnoreCase)) continue;
                if (PathUtils.IsSame(sub.FullName, Path.Combine(host.SelectedMinecraftFolder(), "PCL", "ResourceRecycle"))) continue;
                if (new[] { "assets", "versions", "libraries" }.Contains(sub.Name) && PathUtils.IsSame(request.GameRoot, folder.FullName)) continue;
                if (new[] { "structureCacheV1", ".fabric", ".git", "avatar-cache", "cosmetic-cache" }.Contains(sub.Name)) continue;
                SearchFolder(sub);
            }
            foreach (FileInfo entry in folder.EnumerateFiles("*", SearchOption.TopDirectoryOnly)) {
                if ((entry.Name.Equals("ModpackOrigin.json", StringComparison.OrdinalIgnoreCase) || entry.Name.Equals("CloneOrigin.json", StringComparison.OrdinalIgnoreCase)) && folder.Name.Equals("PCL", StringComparison.OrdinalIgnoreCase)) continue;
                string relative = entry.FullName.AfterFirst(request.GameRoot);
                bool keep = false;
                foreach (string rule in request.Rules) {
                    bool revert = rule.StartsWithF("!");
                    if (Like(relative, rule.TrimStart('!'))) keep = !revert;
                }
                if (!keep) continue;
                string target = request.OverridesFolder + relative;
                FileUtils.Copy(entry.FullName, target);
                if (request.CheckHostedAssets && new[] { ".zip", ".rar", ".jar", ".disabled", ".old" }.Contains(entry.Extension.Lower()) &&
                    new[] { "mods", "packs", "openloader", "resource" }.Any(relative.Contains)) {
                    TLocal mod = host.CreateLocal(target);
                    _ = mod.ModrinthHash; _ = mod.CurseForgeHash;
                    task.Output().Add(mod);
                }
                progress++;
                if (progress == 25) { task.SetProgress(task.Progress() + (0.84 - task.Progress()) * 0.012); progress = 0; }
            }
        }
        SearchFolder(DirectoryUtils.GetInfo(request.GameRoot));
        host.InfoLog($"复制 overrides 文件完成，有 {task.Output().Count} 个文件需要联网检查"); task.SetProgress(0.85);
        if (request.IncludeJava) {
            Java? java = host.SelectJava(request.Instance, false, task.Cancellation(), task.ProgressProvider(0.85, 0.93));
            if (java is null) throw new FileNotFoundException("版本文件夹中没有找到可导出的 Java。");
            string folder = PathUtils.RemoveLastPart(java.Folder);
            string relative = folder.AfterFirst(host.VersionFolder(request.Instance), true), destination = Path.Combine(request.OverridesFolder, relative);
            versionOnlyFolders.Add(relative); DirectoryUtils.Copy(folder, destination);
            host.InfoLog($"已复制版本文件夹中的 Java：{folder} → {destination}");
        }
        task.SetProgress(0.95);
        string baseFolder = request.IncludeLauncher ? request.CacheFolder : request.CacheFolder + @"modpack\";
        foreach (string line in request.ExtraFiles) {
            if (line.EndsWithF("\\") || line.EndsWithF("/")) {
                if (DirectoryUtils.Exists(line)) DirectoryUtils.Copy(line, baseFolder + PathUtils.GetLastPart(line) + "\\");
                else host.HintRed($"未找到配置文件中指定的文件夹：{line}");
            } else {
                if (FileUtils.Exists(line)) FileUtils.Copy(line, baseFolder + PathUtils.GetLastPart(line));
                else host.HintRed($"未找到配置文件中指定的单个文件：{line}");
            }
        }
        task.SetProgress(0.97);
        string pcl = host.VersionFolder(request.Instance) + @"PCL\";
        if (DirectoryUtils.Exists(pcl)) foreach (string file in DirectoryUtils.EnumerateFiles(pcl, true)) {
            string relative = PathUtils.RemoveExtendedPrefix(file).Substring(pcl.Length);
            if (relative.StartsWith(@"PackUpdateBackups\", StringComparison.OrdinalIgnoreCase) || relative.Equals("ModpackOrigin.json", StringComparison.OrdinalIgnoreCase) || relative.Equals("CloneOrigin.json", StringComparison.OrdinalIgnoreCase)) continue;
            FileUtils.Copy(file, request.OverridesFolder + @"PCL\" + relative);
        }
        host.WriteBoolIni(request.OverridesFolder + @"PCL\Setup.ini", "IsStar", false);
        host.ConfigureIsolation(request.OverridesFolder, request.VersionIsolation, versionOnlyFolders);
        if (request.IncludeLauncher) {
            FileUtils.Copy(host.Executable(), request.CacheFolder + host.ExecutableName());
            if (FileUtils.Exists(host.Executable() + ".config")) FileUtils.Copy(host.Executable() + ".config", request.CacheFolder + host.ExecutableName() + ".config");
        }
        if (request.IncludeCustom) {
            foreach (string folder in new[] { @"PCL\Pictures\", @"PCL\Musics\", @"PCL\Help\" })
                if (DirectoryUtils.Exists(host.BaseFolder() + folder)) DirectoryUtils.Copy(host.BaseFolder() + folder, request.CacheFolder + folder);
            foreach (string file in new[] { @"PCL\Custom.xaml", @"PCL\Setup.ini", @"PCL\hints.txt", @"PCL\Logo.png" })
                if (FileUtils.Exists(host.BaseFolder() + file)) FileUtils.Copy(host.BaseFolder() + file, request.CacheFolder + file);
        }
        host.ConfigureMode(request.CacheFolder, request.IncludeLauncher, request.IncludeModpackMode);
    }
}
