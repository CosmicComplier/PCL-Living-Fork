using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using MeloongCore;
using MeloongCore.Extensions;
using Newtonsoft.Json.Linq;

namespace PCLCS;

// LF Pages/PageDownload/ModDownloadLib.vb at f4aa0fae62c3ee8af24c81e61844c8fdd13c8983;
// LF LICENCE. Fixed PCL 5ef81e490ae46b1100d61e33d0183094c7a80ca8 differs by LF's
// explicit Minecraft-folder binding, worker file-workload guard and safety snapshots.
// Fixed CE 1a7653127d578efdb2a9388999a3fdc67d9e49d5
// Plain Craft Launcher 2/Pages/PageDownload/ModDownloadLib.cs was reviewed
// (CE Plain Craft Launcher 2/LICENCE; LF LICENCE). LF installation, Java-class-range discovery,
// argument merging, process output, locking and cleanup remain authoritative;
// CE Cleanroom, Quilt/new loaders, localized texts and changed Java policy are excluded.
public sealed partial class MinecraftInstallEngine<TLoader, TNetFile, TInstance, TState, TCard, TOptiFine, TForgeLike, TForge, TNeo, TLite, TLib, TResource, TRequest>
    where TLoader : class where TNetFile : class
    where TInstance : LegacyMcInstance<TInstance, TState, TCard>
    where TState : struct, Enum where TCard : struct, Enum
    where TOptiFine : MinecraftOptiFineEntryData
    where TForgeLike : MinecraftForgelikeEntryData
    where TForge : TForgeLike, IMinecraftForgeVersionFields
    where TNeo : TForgeLike, IMinecraftNeoForgeFields
    where TLite : MinecraftLiteLoaderEntryData
    where TLib : LegacyMinecraftLibrary, new()
    where TResource : ResourceVersionModelData
    where TRequest : MinecraftInstallRequestData<TForge, TNeo, TResource, TLite>, new() {
    private readonly MinecraftInstallHost<TLoader, TNetFile, TInstance, TForge, TNeo, TResource, TLib> host;
    public object InstallSyncLock = new(), VanillaSyncLock = new();
    public bool IsForgeRecommendedRefreshed;
    public const string McDownloadClientLibName = "下载原版支持库文件", McDownloadClientJsonName = "下载原版 json 文件";
    public MinecraftInstallEngine(MinecraftInstallHost<TLoader, TNetFile, TInstance, TForge, TNeo, TResource, TLib> host) { this.host = host; }
    private static bool Same(string? first, string? second) => string.Equals(first ?? "", second ?? "", StringComparison.Ordinal);
    private static MinecraftDownloadLoaderSettings Options(double? weight = null, bool? show = null, bool? block = null) => new() { ProgressWeight = weight, Show = show, Block = block };
    private TLoader Task<TIn, TOut>(string name, Action<MinecraftInstallTask<TIn, TOut, TLoader>> work, double? weight = null, bool? show = null, bool? block = null) => host.Loaders.NewTask(name, work, Options(weight, show, block));
    private TLoader Combo<TIn>(string name, List<TLoader> steps, double? weight = null, bool? show = null, bool? block = null) => host.Loaders.Combo<TIn>(name, steps, Options(weight, show, block));
    private TLoader Download(string name, List<TNetFile> files, double? weight = null, bool? show = null, bool? block = null) => host.Loaders.Download(name, files, Options(weight, show, block));
    private TNetFile File(IEnumerable<string> urls, string path, MinecraftDownloadCheck? checker = null) => host.NetFile(urls, path, checker);
    private int Message(string text, string title, bool warn = false) => host.Message(new ModpackMessage { Text = text, Title = title, IsWarn = warn });
    private void Hint(string text, int kind = 0) => host.Hint(text, kind);
    private bool Duplicate(string name, string message = "该版本正在下载中！") {
        if (!host.Loaders.TaskbarNames().ToList().Any(value => Same(value, name))) return false;
        Hint(message, 1); return true;
    }
    private void Begin<TInput>(TLoader loader, TInput input, bool installState = false) {
        host.Loaders.OnStateChanged(loader, installState ? McInstallState : LoaderStateChangedHintOnly);
        host.Loaders.Start(loader, input); host.Loaders.AddTaskbar<TInput>(loader);
        host.RefreshDownload(); host.RibbleDownload();
    }
    public List<TLoader> McDownloadClientLoader(string id, string? jsonUrl, string versionFolder) {
        string instanceName = PathUtils.GetLastPart(versionFolder);
        List<TLoader> loaders = new();
        TInstance? newInstance = null;
        if (jsonUrl is null) loaders.Add(Task<string, List<TNetFile>>("获取原版 json 文件下载地址", task => {
            string? address = host.ClientJsonUrl(id);
            task.Output = new() { File(host.LauncherUrls(address), versionFolder + instanceName + ".json") };
        }, 2, false));
        loaders.Add(Download(McDownloadClientJsonName, new() { File(host.LauncherUrls(jsonUrl ?? ""), versionFolder + instanceName + ".json", new MinecraftDownloadCheck { IsJson = true }) }, 3));
        loaders.Add(Task<string, string>("读取 json 文件", _ => { newInstance = host.NewInstance(versionFolder); newInstance.Load(); }, 0, false));
        List<TLoader> libraries = new() {
            Task<string, List<TNetFile>>("分析原版支持库文件（副加载器）", task => { Logger.Info("开始分析原版支持库文件：" + versionFolder); task.Output = host.LibraryFiles(newInstance!); }, 1, false),
            Download("下载原版支持库文件（副加载器）", new(), 13, false)
        };
        loaders.Add(Combo<string>(McDownloadClientLibName, libraries, 14, block: false));
        List<TLoader> assets = new() {
            Task<string, List<TNetFile>>("分析资源文件索引地址（副加载器）", task => {
                try { TNetFile? index = host.AssetIndex(newInstance!); task.Output = index is null ? new() : new() { index }; }
                catch (Exception error) { throw new Exception("分析资源文件索引地址失败", error); }
                try { JObject json = (JObject)FileUtils.ReadAsJson(versionFolder + instanceName + ".json")!; json["clientVersion"] = id; FileUtils.Write(versionFolder + instanceName + ".json", json.ToString()); }
                catch (Exception error) { throw new Exception("添加客户端版本失败", error); }
            }, 1, false),
            Download("下载资源文件索引（副加载器）", new(), 3, false),
            Task<string, List<TNetFile>>("分析所需资源文件（副加载器）", task => task.Output = host.AssetFiles(newInstance!, true, task.Native), 0.01, false),
            Download("下载资源文件（副加载器）", new(), 14, false)
        };
        loaders.Add(Combo<string>("下载原版资源文件", assets, 18, block: false));
        return loaders;
    }
    public List<TLoader> McDownloadFabricLoader(string fabricVersion, string minecraftName, string mcFolder) {
        string id = "fabric-loader-" + fabricVersion + "-" + minecraftName, versionFolder = mcFolder + "versions\\" + id + "\\";
        minecraftName = minecraftName.Replace("∞", "infinite");
        return new() {
            Task<string, List<TNetFile>>("获取 Fabric 主文件下载地址", task => task.Output = new() { File(new[] {
                "https://bmclapi2.bangbang93.com/fabric-meta/v2/versions/loader/" + minecraftName + "/" + fabricVersion + "/profile/json",
                "https://meta.fabricmc.net/v2/versions/loader/" + minecraftName + "/" + fabricVersion + "/profile/json"
            }, versionFolder + id + ".json", new MinecraftDownloadCheck { IsJson = true }) }, 0.5),
            Download("下载 Fabric 主文件", new(), 2.5)
        };
    }
    public List<TLoader> McDownloadLiteLoaderLoader(TLite info, string mcFolder) => new() {
        Task<string, string>("安装 LiteLoader", _ => {
            try {
                string name = info.Inherit + "-LiteLoader", folder = mcFolder + "versions\\" + name + "\\";
                JObject json = new();
                json.Add("id", name);
                json.Add("time", DateTime.ParseExact(info.ReleaseTime, "yyyy/MM/dd HH:mm", System.Globalization.CultureInfo.InvariantCulture));
                json.Add("releaseTime", DateTime.ParseExact(info.ReleaseTime, "yyyy/MM/dd HH:mm", System.Globalization.CultureInfo.InvariantCulture));
                json.Add("type", "release");
                json.Add("arguments", ("{\"game\":[\"--tweakClass\",\"" + info.JsonToken["tweakClass"] + "\"]}").DeserializeJson());
                json.Add("libraries", info.JsonToken["libraries"]);
                ((JContainer)json["libraries"]!).Add(("{\"name\": \"com.mumfrey:liteloader:" + info.JsonToken["version"] + "\",\"url\": \"https://dl.liteloader.com/versions/\"}").DeserializeJson());
                json.Add("mainClass", "net.minecraft.launchwrapper.Launch"); json.Add("minimumLauncherVersion", 18);
                json.Add("inheritsFrom", info.Inherit); json.Add("jar", info.Inherit);
                FileUtils.Write(folder + name + ".json", json.ToString());
            } catch (Exception error) { throw new Exception("安装新 LiteLoader 版本失败", error); }
        }, 1)
    };
}
