using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using MeloongCore.Extensions;
using Newtonsoft.Json.Linq;

namespace PCLCS;

// LF ModDownload.vb, under LF LICENCE. Its difference from fixed PCL 5ef81e49 is
// removal of the 19-line update hint/settings block; do not reintroduce it.
// Selective C# flow reference: CE 1a7653127d578efdb2a9388999a3fdc67d9e49d5
// Plain Craft Launcher 2/Modules/Minecraft/ModDownload.cs (CE LICENSE/PCL LICENCE).
// Keep LF's exact "无" choice, cancellation, settings, timings and URL transformations.
public sealed class MinecraftDownloadSources {
    public MinecraftDownloadSourceHost Host { get; }
    private bool preferMojang;
    private List<int>? allDrops;
    public MinecraftDownloadSources(MinecraftDownloadSourceHost host) { Host = host; }
    public List<int>? AllDrops {
        get {
            if (allDrops is null) allDrops = Host.SettingString("CacheDrops")!.ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(value => Conversions.ToInteger(Conversion.Val(value))).ToList();
            return allDrops.Any() ? allDrops : null;
        }
        set { allDrops = value; Host.SetSetting("CacheDrops", string.Join(",", value!)); }
    }
    public bool DlSourcePreferMojang => Host.SettingInteger("ToolDownloadSource") == 2 || (Host.SettingInteger("ToolDownloadSource") == 1 && preferMojang);
    public bool DlVersionListPreferMojang => Host.SettingInteger("ToolDownloadVersion") == 2 || (Host.SettingInteger("ToolDownloadVersion") == 1 && preferMojang);
    public IEnumerable<string> DlSourceOrder(IEnumerable<string> official, IEnumerable<string> mirrors) => DlSourcePreferMojang ? official.Union(mirrors) : mirrors.Union(official);
    public IEnumerable<string> DlVersionListOrder(IEnumerable<string> official, IEnumerable<string> mirrors) => DlVersionListPreferMojang ? official.Union(mirrors) : mirrors.Union(official);
    public IEnumerable<string> DlSourceAssetsGet(string original) {
        original = original.Replace("http://resources.download.minecraft.net", "https://resources.download.minecraft.net");
        return DlSourceOrder(new[] { original }, new[] { original.Replace("https://piston-data.mojang.com", "https://bmclapi2.bangbang93.com/assets")
            .Replace("https://piston-meta.mojang.com", "https://bmclapi2.bangbang93.com/assets").Replace("https://resources.download.minecraft.net", "https://bmclapi2.bangbang93.com/assets") });
    }
    public IEnumerable<string> DlSourceLibraryGet(string original) {
        string[] mirrors = { original.Replace("https://piston-data.mojang.com", "https://bmclapi2.bangbang93.com/maven").Replace("https://piston-meta.mojang.com", "https://bmclapi2.bangbang93.com/maven").Replace("https://libraries.minecraft.net", "https://bmclapi2.bangbang93.com/maven"),
            original.Replace("https://piston-data.mojang.com", "https://bmclapi2.bangbang93.com/libraries").Replace("https://piston-meta.mojang.com", "https://bmclapi2.bangbang93.com/libraries").Replace("https://libraries.minecraft.net", "https://bmclapi2.bangbang93.com/libraries") };
        if (new[] { "minecraftforge", "fabricmc", "neoforged" }.Any(original.Contains)) return mirrors;
        return DlSourceOrder(new[] { original }, mirrors);
    }
    public IEnumerable<string> DlSourceLauncherOrMetaGet(string? original) {
        if (original is null) throw new Exception("无对应的 json 下载地址");
        return DlSourceOrder(new[] { original }, new[] { original.Replace("https://piston-data.mojang.com", "https://bmclapi2.bangbang93.com")
            .Replace("https://piston-meta.mojang.com", "https://bmclapi2.bangbang93.com").Replace("https://launcher.mojang.com", "https://bmclapi2.bangbang93.com").Replace("https://launchermeta.mojang.com", "https://bmclapi2.bangbang93.com") });
    }
    public string DlSourceModGet(string original) => original.Replace("api.modrinth.com", "mod.mcimirror.top/modrinth").Replace("staging-api.modrinth.com", "mod.mcimirror.top/modrinth")
        .Replace("cdn.modrinth.com", "mod.mcimirror.top").Replace("api.curseforge.com", "mod.mcimirror.top/curseforge").Replace("edge.forgecdn.net", "mod.mcimirror.top")
        .Replace("mediafilez.forgecdn.net", "mod.mcimirror.top").Replace("media.forgecdn.net", "mod.mcimirror.top");
    public JToken? DlModRequest(string url, HttpMethod? method = null, string? content = null, string? contentType = null) {
        List<Tuple<string, int>> urls = new();
        string mcim = DlSourceModGet(url);
        if (mcim != url) {
            switch (Host.SettingInteger("ToolDownloadMod")) {
                case 0: urls.Add(Tuple.Create(mcim, 10)); urls.Add(Tuple.Create(mcim, 10)); urls.Add(Tuple.Create(url, 30)); break;
                case 1: urls.Add(Tuple.Create(url, url.Contains("modrinth") ? 20 : 10)); urls.Add(Tuple.Create(mcim, 10)); urls.Add(Tuple.Create(url, 30)); urls.Add(Tuple.Create(mcim, 30)); break;
                default: urls.Add(Tuple.Create(url, url.Contains("modrinth") ? 20 : 10)); urls.Add(Tuple.Create(url, 30)); urls.Add(Tuple.Create(mcim, 30)); break;
            }
        } else { urls.Add(Tuple.Create(url, url.Contains("modrinth") ? 20 : 10)); urls.Add(Tuple.Create(url, 30)); }
        List<Exception> errors = new();
        foreach (Tuple<string, int> source in urls) {
            try { return Host.Request(new MinecraftDownloadRequest { Url = source.Item1, Method = method, Content = content, ContentType = contentType, Timeout = checked(source.Item2 * 1000), Encoding = Encoding.UTF8, RequireJson = true }).DeserializeJson(); }
            catch (Exception error) { errors.Add(error); }
        }
        throw new AggregateException(errors);
    }
    public void RunVersionList<T>(MinecraftDownloadSourceTask<T> main, MinecraftDownloadSourceTask<T> official, MinecraftDownloadSourceTask<T> mirror) {
        List<KeyValuePair<MinecraftDownloadSourceTask<T>, int>> tasks;
        switch (Host.SettingInteger("ToolDownloadVersion")) {
            case 0: tasks = new() { new(mirror, 30), new(official, 90) }; break;
            case 1: tasks = new() { new(official, 5), new(mirror, 35) }; break;
            default: tasks = new() { new(official, 60), new(mirror, 120) }; break;
        }
        DlSourceLoader(main, tasks, main.IsForceRestarting());
    }
    public void DlSourceLoader<T>(MinecraftDownloadSourceTask<T> main, List<KeyValuePair<MinecraftDownloadSourceTask<T>, int>> tasks, bool forceRestart = false) {
        int waitCycle = 0;
        while (true) {
            bool beforeAllFailed = true;
            foreach (KeyValuePair<MinecraftDownloadSourceTask<T>, int> sub in tasks) {
                if (waitCycle == 0) {
                    if (forceRestart) continue;
                    if ((sub.Key.Input() is null ^ main.Input() is null) || (sub.Key.Input() is not null && !sub.Key.Input()!.Equals(main.Input()))) continue;
                }
                if (!sub.Key.IsFailed()) beforeAllFailed = false;
                if (sub.Key.IsFinished()) { main.SetOutput(sub.Key.Output()); DlSourceLoaderCancel(tasks); return; }
                else if (beforeAllFailed && waitCycle < checked(sub.Value * 100)) waitCycle = checked(sub.Value * 100);
            }
            if (waitCycle == 0) {
                tasks.First().Key.Start(main.Input(), forceRestart);
                foreach (KeyValuePair<MinecraftDownloadSourceTask<T>, int> task in tasks.Skip(1)) task.Key.SetWaiting();
            }
            for (int index = 0; index < tasks.Count; index++) {
                if (waitCycle != checked(tasks[index].Value * 100)) continue;
                if (index < tasks.Count - 1 && !tasks.All(task => task.Key.IsFailed())) tasks[index + 1].Key.Start(main.Input(), forceRestart);
                else {
                    Exception? error = null;
                    foreach (KeyValuePair<MinecraftDownloadSourceTask<T>, int> task in tasks) {
                        task.Key.ClearInput();
                        if (task.Key.Error() is not null && (error is null || task.Key.Error()!.Message == "无")) error = task.Key.Error();
                    }
                    error ??= new TimeoutException("下载源连接超时"); DlSourceLoaderCancel(tasks); throw error;
                }
                break;
            }
            Host.Sleep(10); waitCycle = checked(waitCycle + 1);
            if (main.IsCanceled()) { DlSourceLoaderCancel(tasks); return; }
        }
    }
    public void DlSourceLoaderCancel<T>(List<KeyValuePair<MinecraftDownloadSourceTask<T>, int>> tasks) {
        foreach (KeyValuePair<MinecraftDownloadSourceTask<T>, int> task in tasks) if (task.Key.IsLoading()) task.Key.Cancel();
    }
    public void RunClientVersionList<T>(MinecraftDownloadSourceTask<T> main, MinecraftDownloadSourceTask<T> official, MinecraftDownloadSourceTask<T> mirror, Func<T, JObject> value) {
        RunVersionList(main, official, mirror); ExtractDrops(value(main.Output()));
    }
    public void ExtractDrops(JObject json) {
        List<int> drops = new();
        foreach (JObject version in json["versions"]!) {
            string id = version["id"]!.ToString(); if (id.Contains("-")) continue;
            int drop = Host.VersionToDrop(id); if (drop == 209) continue; drops.Add(drop);
        }
        AllDrops = drops.Distinct().OrderByDescending(value => value).ToList();
    }
    public MinecraftDownloadResult<JObject> DlClientListMojangMain() {
        long start = Host.Clock();
        JObject json = (JObject)Host.RetryRequest(new MinecraftDownloadRequest { Url = "https://launchermeta.mojang.com/mc/game/version_manifest.json", RequireJson = true }).DeserializeJson()!;
        try {
            JArray versions = (JArray)json["versions"]!;
            if (versions.Count < 200) throw new Exception("获取到的版本列表长度不足（" + json + "）");
            if (!preferMojang) { long delta = checked(Host.Clock() - start); preferMojang = delta < 4000; Host.Info("Mojang 官方源加载耗时：" + delta + "ms，" + (preferMojang ? "可优先使用官方源" : "不优先使用官方源")); }
            return new("Mojang 官方源", true, json);
        } catch (Exception error) { throw new Exception("Minecraft 官方源版本列表解析失败", error); }
    }
    public MinecraftDownloadResult<JObject> DlClientListBmclapiMain(string? input, Func<JObject?> currentMainValue) {
        JObject json = (JObject)Host.RetryRequest(new MinecraftDownloadRequest { Url = "https://bmclapi2.bangbang93.com/mc/game/version_manifest.json", RequireJson = true }).DeserializeJson()!;
        try {
            JArray versions = (JArray)json["versions"]!;
            if (versions.Count < 200) throw new Exception("获取到的版本列表长度不足（" + json + "）");
            string cache = Host.TempPath() + "Cache\\download.json";
            if (Host.FileExists(cache)) versions.Merge(Host.ReadJson(cache));
            if (!string.IsNullOrEmpty(input) && currentMainValue() is not null && !currentMainValue()!["versions"]!.Any(value => Operators.ConditionalCompareObjectEqual(value["id"], input, false)))
                throw new Exception("BMCLAPI 源未包含目标版本 " + input);
            return new("BMCLAPI", false, json);
        } catch (Exception error) { throw new Exception("Minecraft BMCLAPI 版本列表解析失败（" + json.ToString() + "）", error); }
    }
    public string? DlClientListGet<T>(string id, MinecraftDownloadSourceTask<T> loader, Func<T, JObject> value) {
        try {
            id = id.Replace("_", "-"); if (id != "1.0" && id.EndsWithF(".0")) id = Strings.Left(id, id.Length - 2);
            if (loader.IsFinished()) {
                foreach (JObject version in value(loader.Output())["versions"]!) if (Operators.ConditionalCompareObjectEqual(version["id"], id, false)) return version["url"]!.ToString();
                loader.WaitForExit(id, true);
            } else if (loader.IsLoading()) loader.WaitForExit(id, false);
            else if (loader.IsFailed() || loader.IsCanceledState() || loader.IsWaiting()) loader.WaitForExit(id, true);
            foreach (JObject version in value(loader.Output())["versions"]!) if (Operators.ConditionalCompareObjectEqual(version["id"], id, false)) return version["url"]!.ToString();
            Host.Warn("未发现版本 " + id + " 的 json 下载地址，版本列表返回为：\r\n" + value(loader.Output())); return null;
        } catch (Exception error) { Host.WarnException(error, "获取版本 " + id + " 的 json 下载地址失败"); return null; }
    }
}
