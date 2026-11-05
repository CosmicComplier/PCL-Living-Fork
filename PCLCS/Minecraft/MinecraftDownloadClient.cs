using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace PCLCS;

public sealed class MinecraftDownloadClient<TInstance, TNetFile, TLoader, TChecker>
    where TInstance : class where TNetFile : class {
    private readonly MinecraftDownloadSources sources;
    private readonly MinecraftDownloadClientHost<TInstance, TNetFile, TLoader, TChecker> host;
    public MinecraftDownloadClient(MinecraftDownloadSources sources, MinecraftDownloadClientHost<TInstance, TNetFile, TLoader, TChecker> host) { this.sources = sources; this.host = host; }
    public TNetFile? DlClientJarGet(TInstance instance, bool returnNothingOnFileUseable) {
        try { while (!string.IsNullOrEmpty(host.InheritName(instance))) instance = host.CreateInstance(host.InheritName(instance)!); }
        catch (Exception error) { host.WarnException(error, "获取底层继承版本失败"); }
        if (host.JsonObject(instance)["downloads"] is null || host.JsonObject(instance)["downloads"]!["client"] is null || host.JsonObject(instance)["downloads"]!["client"]!["url"] is null)
            throw new Exception("底层版本 " + host.Name(instance) + " 中无 jar 文件下载信息");
        TChecker checker = host.Checker(new MinecraftDownloadCheck { MinSize = 1024,
            ActualSize = (long)(host.JsonObject(instance)["downloads"]!["client"]!["size"] ?? new JValue(-1)), Hash = (string?)host.JsonObject(instance)["downloads"]!["client"]!["sha1"] });
        if (returnNothingOnFileUseable && host.Check(checker, host.VersionPath(instance) + host.Name(instance) + ".jar") is null) return null;
        string jarUrl = (string)host.JsonObject(instance)["downloads"]!["client"]!["url"]!;
        return host.NetFile(sources.DlSourceLauncherOrMetaGet(jarUrl), host.VersionPath(instance) + host.Name(instance) + ".jar", checker);
    }
    public TNetFile? DlClientAssetIndexGet(TInstance instance) {
        while (!string.IsNullOrEmpty(host.InheritName(instance))) instance = host.CreateInstance(host.InheritName(instance)!);
        JToken index = host.AssetIndex(instance);
        string address = host.GameFolder() + "assets\\indexes\\" + index["id"]!.ToString() + ".json";
        host.Info("版本 " + host.Name(instance) + " 对应的资源文件索引为 " + index["id"]);
        string? url = (string?)(index["url"] ?? new JValue(""));
        if (string.IsNullOrEmpty(url)) return null;
        return host.NetFile(sources.DlSourceLauncherOrMetaGet(url), address, host.Checker(new MinecraftDownloadCheck { IsJson = true }));
    }
    public List<TLoader> DlClientFix(TInstance instance, bool checkAssetsHash, bool downloadAssetIndexInBackground) {
        List<TLoader> loaders = new();
        List<TLoader> libraries = new() {
            host.Task("分析缺失支持库文件", task => task.SetOutput(host.LibraryFiles(instance)), new MinecraftDownloadLoaderSettings { ProgressWeight = 1 }),
            host.Download("下载支持库文件", new List<TNetFile>(), new MinecraftDownloadLoaderSettings { ProgressWeight = 15 })
        };
        loaders.Add(host.Combo("下载支持库文件（主加载器）", libraries, new MinecraftDownloadLoaderSettings { Block = false, Show = false, ProgressWeight = 16 }));
        if (host.IgnoreFileCheck(instance)) host.WarnNone("设置要求跳过所有 Assets 检查，文件可能不是最新的");
        else {
            List<TLoader> assets = new();
            string? tempMain = null, realMain = null;
            assets.Add(host.Task("分析资源文件索引地址", task => {
                try {
                    TNetFile? indexFile = DlClientAssetIndexGet(instance);
                    if (indexFile is null) { task.SetOutput(new List<TNetFile>()); host.Warn("未找到版本 " + host.Name(instance) + " 的合适的资源索引下载地址，游戏 assets 可能缺失"); return; }
                    string background = host.TempPath() + "Cache\\" + host.LocalName(indexFile) + "_background";
                    if (host.FileExists(background) && host.LastWriteTime(background).Date > host.LastWriteTime(host.LocalPath(indexFile)).Date) {
                        host.Info("发现后台更新的资源索引缓存文件：" + background); host.MoveFile(background, host.LocalPath(indexFile));
                    }
                    if (downloadAssetIndexInBackground) task.SetOutput(host.CheckFile(indexFile, host.LocalPath(indexFile)) is null ? new List<TNetFile>() : new List<TNetFile> { indexFile });
                    else {
                        realMain = host.LocalPath(indexFile); tempMain = host.TempPath() + "Cache\\" + host.LocalName(indexFile);
                        host.SetLocalPath(indexFile, tempMain); task.SetOutput(new List<TNetFile> { indexFile });
                    }
                } catch (Exception error) { throw new Exception("分析资源文件索引地址失败", error); }
            }, new MinecraftDownloadLoaderSettings { ProgressWeight = 0.5, Show = false }));
            assets.Add(host.Download("下载资源文件索引", new List<TNetFile>(), new MinecraftDownloadLoaderSettings { ProgressWeight = 2 }));
            assets.Add(host.Task("获取资源文件列表", task => {
                if (tempMain is not null) host.MoveFile(tempMain, realMain!);
                task.SetOutput(host.AssetFiles(instance, checkAssetsHash, task.Native));
            }, new MinecraftDownloadLoaderSettings { ProgressWeight = 0.01 }));
            assets.Add(host.Download("下载资源文件", new List<TNetFile>(), new MinecraftDownloadLoaderSettings { ProgressWeight = 25 }));
            loaders.Add(host.Combo("下载资源文件（主加载器）", assets, new MinecraftDownloadLoaderSettings { Block = false, Show = false, ProgressWeight = 27 }));
            if (downloadAssetIndexInBackground) {
                string? realBackground = null, tempBackground = null;
                List<TLoader> update = new();
                update.Add(host.Task("后台分析资源文件索引地址", task => {
                    TNetFile? indexFile = DlClientAssetIndexGet(instance);
                    if (indexFile is null) { host.Warn("未找到版本 " + host.Name(instance) + " 的合适的资源索引下载地址，游戏 assets 可能缺失"); task.Cancel(); throw new OperationCanceledException(); }
                    realBackground = host.LocalPath(indexFile); tempBackground = host.TempPath() + "Cache\\" + host.LocalName(indexFile) + "_background";
                    host.SetLocalPath(indexFile, tempBackground); task.SetOutput(new List<TNetFile> { indexFile });
                    DateTime lastUpdate = host.LastWriteTime(realBackground).Date;
                    if (host.FileExists(realBackground) && Math.Abs((lastUpdate - host.Now().Date).TotalDays) < 1) { host.Info("无需更新资源文件索引，上次更新于 " + lastUpdate); task.Cancel(); }
                }, new MinecraftDownloadLoaderSettings()));
                update.Add(host.Download("后台下载资源文件索引", new List<TNetFile>(), new MinecraftDownloadLoaderSettings()));
                TLoader updater = host.Combo("后台更新资源文件索引", update, new MinecraftDownloadLoaderSettings());
                host.Info("开始后台检查资源文件索引"); host.Start(updater);
            }
        }
        return loaders;
    }
}
