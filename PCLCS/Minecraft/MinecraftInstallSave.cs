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
    public void McDownloadOptiFineSave(TOptiFine info) {
        try {
            string? target = host.SaveFile(info.FileName, "jar", "OptiFine Jar");
            if (target is null || Duplicate("OptiFine " + info.DisplayName + " 下载")) return;
            Begin(Combo<TOptiFine>("OptiFine " + info.DisplayName + " 下载", McDownloadOptiFineSaveLoader(info, target)), info);
        } catch (Exception error) { Logger.Error(error, "开始 OptiFine 下载失败"); }
    }
    public void McDownloadLiteLoaderSave(TLite info) {
        try {
            string id = info.Inherit;
            string? target = host.SaveFile(info.FileName.Replace("-SNAPSHOT", ""), "jar", "LiteLoader 安装器");
            if (target is null || Duplicate("LiteLoader " + id + " 下载")) return;
            List<string> addresses = new();
            if (info.IsLegacy) {
                switch (info.Inherit) {
                    case "1.7.10": addresses.Add("https://dl.liteloader.com/redist/1.7.10/liteloader-installer-1.7.10-04.jar"); break;
                    case "1.7.2": addresses.Add("https://dl.liteloader.com/redist/1.7.2/liteloader-installer-1.7.2-04.jar"); break;
                    case "1.6.4": addresses.Add("https://dl.liteloader.com/redist/1.6.4/liteloader-installer-1.6.4-01.jar"); break;
                    case "1.6.2": addresses.Add("https://dl.liteloader.com/redist/1.6.2/liteloader-installer-1.6.2-04.jar"); break;
                    case "1.5.2": addresses.Add("https://dl.liteloader.com/redist/1.5.2/liteloader-installer-1.5.2-01.jar"); break;
                    default: throw new NotSupportedException("未知的 Minecraft 版本（" + info.Inherit + "）");
                }
            } else addresses.Add("http://jenkins.liteloader.com/job/LiteLoaderInstaller%20" + info.Inherit + "/lastSuccessfulBuild/artifact/" + (Same(info.Inherit, "1.8") ? "ant/dist/" : "build/libs/") + info.FileName);
            List<TLoader> loaders = new() { Download("下载主文件", new() { File(addresses.ToArray(), target, new MinecraftDownloadCheck { MinSize = 1024 * 1024 }) }, 15) };
            Begin(Combo<TLite>("LiteLoader " + id + " 安装器下载", loaders), info);
        } catch (Exception error) { Logger.Error(error, "开始 LiteLoader 安装器下载失败"); }
    }
    public void McDownloadForgelikeSave(TForgeLike info) {
        try {
            string? target = host.SaveFile(info.LoaderName + "-" + info.Inherit + "-" + info.VersionName + "." + info.FileExtension, info.FileExtension, info.LoaderName + " 安装器");
            if (target is null) return;
            string display = info.LoaderName + " " + info.Inherit + " - " + info.VersionName;
            if (Duplicate(display + " 下载")) return;
            Begin(Combo<TForgeLike>(display + " 下载", new() { Download("下载主文件", ForgelikeFiles(info, target, false), 6) }), info);
        } catch (Exception error) { Logger.Error(error, "开始 " + info.LoaderName + " 安装器下载失败"); }
    }
    public void McDownloadFabricLoaderSave(JObject info) {
        try {
            string url = info["url"]!.ToString(), name = PathUtils.GetLastPart(url), version = PathUtils.GetLastPart(info["version"]!.ToString());
            string? target = host.SaveFile(name, "jar", "Fabric 安装器");
            if (target is null || Duplicate("Fabric " + version + " 安装器下载")) return;
            List<TLoader> loaders = new() { Download("下载主文件", new() { File(new[] { url }, target, new MinecraftDownloadCheck { MinSize = 1024 * 64 }) }, 15) };
            Begin(Combo<JObject>("Fabric " + version + " 安装器下载", loaders), info);
        } catch (Exception error) { Logger.Error(error, "开始 Fabric 安装器下载失败"); }
    }
    public void SaveMinecraft(Func<(string Title, JToken Json)> selection, bool server) {
        try {
            var entry = selection();
            string id = entry.Title, jsonUrl = entry.Json["url"]!.ToString();
            string? folder = host.SelectFolder();
            if (folder is null) return;
            folder += id + "\\";
            if (Duplicate("Minecraft " + id + (server ? " 服务端下载" : " 下载"), server ? "该服务端正在下载中！" : "该版本正在下载中！")) return;
            List<TLoader> loaders = new() { Download("下载版本 JSON 文件", new() { File(host.LauncherUrls(jsonUrl), folder + id + ".json", new MinecraftDownloadCheck { IsJson = true }) }, 2) };
            if (server) {
                loaders.Add(Task<string, List<TNetFile>>("构建服务端", task => {
                    TInstance instance = host.NewInstance(folder);
                    if (instance.JsonObject["downloads"] is null || instance.JsonObject["downloads"]!["server"] is null || instance.JsonObject["downloads"]!["server"]!["url"] is null) {
                        FileUtils.Delete(folder + id + ".json");
                        if (!DirectoryUtils.GetInfo(folder).GetFileSystemInfos().Any()) DirectoryUtils.Delete(folder);
                        task.Output = new(); Hint("Mojang 没有给 Minecraft " + id + " 提供官方服务端下载，没法下，撤退！", 1); Thread.Sleep(2000); task.Cancel(); return;
                    }
                    string jarUrl = (string)instance.JsonObject["downloads"]!["server"]!["url"]!;
                    MinecraftDownloadCheck checker = new() { MinSize = 1024, ActualSize = (long)(instance.JsonObject["downloads"]!["server"]!["size"] ?? new JValue(-1)), Hash = (string?)instance.JsonObject["downloads"]!["server"]!["sha1"] };
                    task.Output = new() { File(host.LauncherUrls(jarUrl), folder + id + "-server.jar", checker) };
                    string bat = "@echo off\ntitle " + id + " 原版服务端\necho 如果服务端立即停止，请右键编辑该脚本，将下一行开头的 java 替换为适合该 Minecraft 版本的完整 java.exe 的路径。\necho 你可以在 PCL 的 [设置 → 启动选项] 中查看已安装的 java，所需的 java.exe 一般在其中的 bin 文件夹下。\necho ------------------------------\necho 如果提示 \"You need to agree to the EULA in order to run the server\"，请打开 eula.txt，按说明阅读并同意 Minecraft EULA 后，将该文件最后一行中的 eula=false 改为 eula=true。\necho ------------------------------\n\"java\" -server -XX:+UseG1GC -Xmx6144M -Xms1024M -jar " + id + "-server.jar nogui\necho ----------------------\necho 服务端已停止。\npause";
                    FileUtils.Write(folder + "Launch Server.bat", bat, Encoding.Default.Equals(Encoding.UTF8) ? Encoding.UTF8 : Encoding.GetEncoding("GB18030"));
                    FileUtils.Delete(folder + id + ".json");
                }, 0.5, false));
                loaders.Add(Download("下载服务端文件", new(), 5));
            } else {
                loaders.Add(Task<string, List<TNetFile>>("分析版本 JAR 文件下载地址", task => task.Output = new() { host.ClientJar(host.NewInstance(folder), false)! }, 0.5, false));
                loaders.Add(Download("下载版本 JAR 文件", new(), 5));
            }
            Begin(Combo<string>("Minecraft " + id + (server ? " 服务端下载" : " 下载"), loaders), id);
        } catch (Exception error) { Logger.Error(error, server ? "开始 Minecraft 服务端下载失败" : "开始 Minecraft 下载失败"); }
    }
    public void McDownloadForgeRecommendedRefresh() {
        if (IsForgeRecommendedRefreshed) return;
        IsForgeRecommendedRefreshed = true;
        host.RunInNewThread(() => {
            try {
                Logger.Info("刷新 Forge 推荐版本缓存开始");
                string result = host.RetryRequest(new MinecraftDownloadRequest { Url = "https://bmclapi2.bangbang93.com/forge/promos", RequireJson = true });
                if (result.Length < 1000) throw new Exception("获取的结果过短（" + result + "）");
                JContainer json = (JContainer)result.DeserializeJson()!;
                List<string> recommended = new();
                foreach (JObject version in json) {
                    if (version["name"] is null || version["build"] is null) continue;
                    string name = (string)version["name"]!;
                    if (!name.EndsWithF("-recommended")) continue;
                    recommended.Add("\"" + name.Replace("-recommended", "\":\"" + version["build"]!["version"] + "\""));
                }
                if (recommended.Count < 5) throw new Exception("获取的推荐版本数过少（" + result + "）");
                FileUtils.Write(host.TempFolder() + "Cache\\ForgeRecommendedList.json", "{" + string.Join(",", recommended) + "}"); Logger.Info("刷新 Forge 推荐版本缓存成功");
            } catch (Exception error) { Logger.Warn(error, "刷新 Forge 推荐版本缓存失败"); }
        }, "ForgeRecommendedRefresh");
    }
    public string? McDownloadForgeRecommendedGet(string? mcVersionName) {
        try {
            if (mcVersionName is null) return null;
            string? list = FileUtils.TryReadAsString(host.TempFolder() + "Cache\\ForgeRecommendedList.json");
            if (string.IsNullOrEmpty(list)) { Logger.Info("没有 Forge 推荐版本缓存文件"); return null; }
            JObject? json = list!.DeserializeJson<JObject>();
            if (json is null || !(mcVersionName ?? "").Contains(".") || !json.ContainsKey(mcVersionName!)) return null;
            return (json[mcVersionName!] ?? new JValue("")).ToString();
        } catch (Exception error) { Logger.Error(error, "获取 Forge 推荐版本失败（" + (mcVersionName ?? "null") + "）"); return null; }
    }
}
