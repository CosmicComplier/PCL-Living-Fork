using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using MeloongCore;
using MeloongCore.Extensions;
using Newtonsoft.Json.Linq;

namespace PCLCS;

public sealed partial class MinecraftInstallEngine<TLoader, TNetFile, TInstance, TState, TCard, TOptiFine, TForgeLike, TForge, TNeo, TLite, TLib, TResource, TRequest> {
    private List<string> OptiFineSources<TIn>(TOptiFine info, MinecraftInstallTask<TIn, List<TNetFile>, TLoader> task, bool saving) {
        List<string> sources = new();
        string inherit = info.Inherit;
        if (Same(inherit, "1.8") || Same(inherit, "1.9")) inherit += ".0";
        sources.Add(info.IsPreview ? "https://bmclapi2.bangbang93.com/optifine/" + inherit + "/HD_U_" + info.DisplayName.Replace(info.Inherit + " ", "").Replace(" ", "/")
            : "https://bmclapi2.bangbang93.com/optifine/" + inherit + "/HD_U/" + info.DisplayName.Replace(info.Inherit + " ", ""));
        try {
            string page = host.RetryRequest(new MinecraftDownloadRequest { Url = "https://optifine.net/adloadx?f=" + info.FileName, Encoding = new UTF8Encoding(false), Accept = "text/html", SimulateBrowserHeaders = true });
            task.Progress = 0.8; sources.Add("https://optifine.net/" + page.RegexSearch("downloadx\\?f=[^\"']+").First());
            Logger.Info("OptiFine " + info.DisplayName + " 官方下载地址：" + sources.Last());
        } catch (Exception error) { Logger.Warn(error, "获取 OptiFine " + info.DisplayName + " 官方下载地址失败"); }
        if (saving) task.Progress = 0.9;
        return sources;
    }
    public List<TLoader> McDownloadOptiFineSaveLoader(TOptiFine info, string targetFolder) => new() {
        Task<TOptiFine, List<TNetFile>>("获取 OptiFine 下载地址", task => task.Output = new() { File(OptiFineSources(info, task, true).ToArray(), targetFolder, new MinecraftDownloadCheck { MinSize = 64 * 1024 }) }, 6),
        Download("下载 OptiFine 主文件", new(), 10, block: true)
    };
    private void WaitAndCopyVanilla<TIn>(string loaderName, string mcFolder, string inherit, TLoader client, string clientFolder, MinecraftInstallTask<TIn, bool, TLoader> task) {
        List<TLoader> targets = host.Loaders.Children(client).Where(loader => Same(host.Loaders.Name(loader), McDownloadClientLibName) || Same(host.Loaders.Name(loader), McDownloadClientJsonName)).Where(loader => host.Loaders.State(loader) != host.States.Finished).ToList();
        if (targets.Any()) Logger.Info(loaderName + " 安装正在等待原版文件下载完成");
        while (targets.Any() && !task.IsCanceled) { targets = targets.Where(loader => host.Loaders.State(loader) != host.States.Finished).ToList(); Thread.Sleep(50); }
        if (task.IsCanceled) return;
        lock (VanillaSyncLock) {
            string clientName = PathUtils.GetLastPart(clientFolder);
            DirectoryUtils.Create(mcFolder + "versions\\" + inherit);
            if (!FileUtils.Exists(mcFolder + "versions\\" + inherit + "\\" + inherit + ".json")) FileUtils.Copy(clientFolder + clientName + ".json", mcFolder + "versions\\" + inherit + "\\" + inherit + ".json");
            if (!FileUtils.Exists(mcFolder + "versions\\" + inherit + "\\" + inherit + ".jar")) FileUtils.Copy(clientFolder + clientName + ".jar", mcFolder + "versions\\" + inherit + "\\" + inherit + ".jar");
        }
    }
    public List<TLoader> McDownloadOptiFineLoader(TOptiFine info, string mcFolder, TLoader clientDownloadLoader, string clientFolder) {
        string id = info.InstanceName, versionFolder = mcFolder + "versions\\" + id + "\\";
        bool newer = info.Inherit.Contains("w") || McVersion.VersionToDrop(info.Inherit) >= 140;
        string target = newer ? host.RequestTempFolder(true) + "OptiFine.jar" : mcFolder + "libraries\\optifine\\OptiFine\\" + PathUtils.GetFileNameWithoutExtension(info.FileName).Replace("OptiFine_", "").Replace("preview_", "") + "\\" + info.FileName.Replace("OptiFine_", "OptiFine-").Replace("preview_", "");
        List<TLoader> loaders = new() {
            Task<string, List<TNetFile>>("获取 OptiFine 主文件下载地址", task => task.Output = new() { File(OptiFineSources(info, task, false).ToArray(), target, new MinecraftDownloadCheck { MinSize = 300 * 1024 }) }, 8),
            Download("下载 OptiFine 主文件", new(), 8),
            Task<List<TNetFile>, bool>("等待原版下载", task => WaitAndCopyVanilla("OptiFine", mcFolder, info.Inherit, clientDownloadLoader, clientFolder, task), 0.1, false)
        };
        if (newer) {
            Logger.Info("检测为新版 OptiFine：" + info.Inherit);
            loaders.Add(Task<List<TNetFile>, bool>("安装 OptiFine（方式 A）", task => {
                string home = host.RequestTempFolder(true), folder = home + ".minecraft\\";
                try {
                    if (DirectoryUtils.Exists(folder + "versions\\" + info.Inherit)) DirectoryUtils.Delete(folder + "versions\\" + info.Inherit);
                    DirectoryUtils.Create(folder + "versions\\" + info.Inherit + "\\"); host.CreateLauncherProfiles(folder);
                    FileUtils.Copy(mcFolder + "versions\\" + info.Inherit + "\\" + info.Inherit + ".json", folder + "versions\\" + info.Inherit + "\\" + info.Inherit + ".json");
                    FileUtils.Copy(mcFolder + "versions\\" + info.Inherit + "\\" + info.Inherit + ".jar", folder + "versions\\" + info.Inherit + "\\" + info.Inherit + ".jar"); task.Progress = 0.06;
                    bool wrapper = true;
                    while (true) {
                        try { McDownloadOptiFineInstall(home, target, task, wrapper); break; }
                        catch (Exception error) {
                            if (wrapper) { Logger.Warn(error, "使用 JavaWrapper 安装 OptiFine 失败，将不使用 JavaWrapper 并重试"); wrapper = false; }
                            else throw new Exception("运行 OptiFine 安装器失败", error);
                        }
                    }
                    task.Progress = 0.96; FileUtils.Delete(folder + "launcher_profiles.json"); DirectoryUtils.Copy(folder, mcFolder); task.Progress = 0.98;
                    FileUtils.Delete(target); DirectoryUtils.Delete(home);
                } catch (Exception error) { throw new Exception("安装 OptiFine（方式 A）失败", error); }
            }, 8));
        } else {
            Logger.Info("检测为旧版 OptiFine：" + info.Inherit);
            loaders.Add(Task<List<TNetFile>, bool>("安装 OptiFine（方式 B）", task => {
                try {
                    FileUtils.Copy(mcFolder + "versions\\" + info.Inherit + "\\" + info.Inherit + ".jar", versionFolder + id + ".jar"); task.Progress = 0.7;
                    TInstance instance = host.NewInstance(mcFolder + "versions\\" + info.Inherit);
                    string date = Same(info.ReleaseTime, "") ? instance.ReleaseTime.ToString("yyyy'-'MM'-'dd") : info.ReleaseTime.Replace("/", "-");
                    string json = "{\n    \"id\": \"" + id + "\",\n    \"inheritsFrom\": \"" + info.Inherit + "\",\n    \"time\": \"" + date + "T23:33:33+08:00\",\n    \"releaseTime\": \"" + date + "T23:33:33+08:00\",\n    \"type\": \"release\",\n    \"libraries\": [\n        {\"name\": \"optifine:OptiFine:" + PathUtils.GetFileNameWithoutExtension(info.FileName).Replace("OptiFine_", "").Replace("preview_", "") + "\"},\n        {\"name\": \"net.minecraft:launchwrapper:1.12\"}\n    ],\n    \"mainClass\": \"net.minecraft.launchwrapper.Launch\",";
                    task.Progress = 0.8;
                    json += instance.IsOldJson ? "\n    \"minimumLauncherVersion\": 18,\n    \"minecraftArguments\": \"" + instance.JsonObject["minecraftArguments"] + "  --tweakClass optifine.OptiFineTweaker\"\n}"
                        : "\n    \"minimumLauncherVersion\": \"21\",\n    \"arguments\": {\n        \"game\": [\n            \"--tweakClass\",\n            \"optifine.OptiFineTweaker\"\n        ]\n    }\n}";
                    FileUtils.Write(versionFolder + id + ".json", json);
                } catch (Exception error) { throw new Exception("安装 OptiFine（方式 B）失败", error); }
            }, 1));
        }
        return loaders;
    }
}
