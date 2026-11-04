using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualBasic.CompilerServices;
using MeloongCore.Extensions;
using Newtonsoft.Json.Linq;

namespace PCLCS;

public sealed class MinecraftDownloadCatalog<TOptiFine, TForge, TNeoForge, TLiteLoader>
    where TOptiFine : MinecraftOptiFineEntryData
    where TForge : MinecraftForgelikeEntryData, IMinecraftForgeVersionFields
    where TNeoForge : MinecraftForgelikeEntryData, IMinecraftNeoForgeFields
    where TLiteLoader : MinecraftLiteLoaderEntryData {
    private readonly MinecraftDownloadSources sources;
    private MinecraftDownloadSourceHost Source => sources.Host;
    private readonly MinecraftDownloadCatalogHost<TOptiFine, TForge, TNeoForge, TLiteLoader> host;
    public MinecraftDownloadCatalog(MinecraftDownloadSources sources, MinecraftDownloadCatalogHost<TOptiFine, TForge, TNeoForge, TLiteLoader> host) { this.sources = sources; this.host = host; }
    public MinecraftDownloadResult<List<TOptiFine>> DlOptiFineListOfficialMain() {
        string result = Source.RetryRequest(new MinecraftDownloadRequest { Url = "https://optifine.net/downloads", Encoding = Encoding.Default });
        if (result.Length < 200) throw new Exception("获取到的版本列表长度不足（" + result + "）");
        try {
            List<string> forge = result.RegexSearch("(?<=colForge'>)[^<]*").ToList();
            List<string> releaseTime = result.RegexSearch("(?<=colDate'>)[^<]+").ToList();
            List<string> name = result.RegexSearch("(?<=OptiFine_)[0-9A-Za-z_.]+(?=.jar\")").ToList();
            if (releaseTime.Count != name.Count) throw new Exception("版本与发布时间数据无法对应");
            if (forge.Count != name.Count) throw new Exception("版本与 Forge 兼容数据无法对应");
            if (releaseTime.Count < 10) throw new Exception("获取到的版本数量不足（" + result + "）");
            List<TOptiFine> versions = new();
            for (int index = 0; index < releaseTime.Count; index++) {
                name[index] = name[index].Replace("_", " ");
                TOptiFine entry = host.OptiFine();
                entry.DisplayName = name[index].Replace("HD U ", "").Replace(".0 ", " ");
                entry.ReleaseTime = string.Join("/", new[] { releaseTime[index].Split('.')[2], releaseTime[index].Split('.')[1], releaseTime[index].Split('.')[0] });
                entry.IsPreview = name[index].ContainsIgnoreCase("pre"); entry.Inherit = name[index].ToString().Split(' ')[0];
                entry.FileName = (name[index].ContainsIgnoreCase("pre") ? "preview_" : "") + "OptiFine_" + name[index].Replace(" ", "_") + ".jar";
                entry.RequiredForgeVersion = forge[index].Replace("Forge ", "").Replace("#", "");
                if (entry.RequiredForgeVersion.Contains("N/A")) entry.RequiredForgeVersion = null;
                entry.InstanceName = entry.Inherit + "-OptiFine_" + name[index].AfterFirst(" ").Replace(" ", "_"); versions.Add(entry);
            }
            return new("OptiFine 官方源", true, versions);
        } catch (Exception error) { throw new Exception("OptiFine 官方源版本列表解析失败（" + result + "）", error); }
    }
    public MinecraftDownloadResult<List<TOptiFine>> DlOptiFineListBmclapiMain() {
        JArray json = (JArray)Source.RetryRequest(new MinecraftDownloadRequest { Url = "https://bmclapi2.bangbang93.com/optifine/versionList", RequireJson = true }).DeserializeJson()!;
        try {
            List<TOptiFine> versions = new();
            foreach (JObject token in json) {
                TOptiFine entry = host.OptiFine();
                entry.DisplayName = (token["mcversion"]!.ToString() + token["type"]!.ToString().Replace("HD_U", "").Replace("_", " ") + " " + token["patch"]!.ToString()).Replace(".0 ", " ");
                entry.ReleaseTime = ""; entry.IsPreview = token["patch"]!.ToString().ContainsIgnoreCase("pre"); entry.Inherit = token["mcversion"]!.ToString();
                entry.FileName = token["filename"]!.ToString(); entry.RequiredForgeVersion = (token["forge"] ?? new JValue("")).ToString().Replace("Forge ", "").Replace("#", "");
                if (entry.RequiredForgeVersion.Contains("N/A")) entry.RequiredForgeVersion = null;
                entry.InstanceName = entry.Inherit + "-OptiFine_" + (token["type"]!.ToString() + " " + token["patch"]!.ToString()).Replace(".0 ", " ").Replace(" ", "_").Replace(entry.Inherit + "_", ""); versions.Add(entry);
            }
            return new("BMCLAPI", false, versions);
        } catch (Exception error) { throw new Exception("OptiFine BMCLAPI 版本列表解析失败（" + json.ToString() + "）", error); }
    }
    public MinecraftDownloadResult<List<string>> DlForgeListOfficialMain() {
        string result = Source.RetryRequest(new MinecraftDownloadRequest { Url = "https://files.minecraftforge.net/maven/net/minecraftforge/forge/index_1.2.4.html", Encoding = Encoding.Default, Accept = "text/html", SimulateBrowserHeaders = true });
        if (result.Length < 200) throw new Exception("获取到的版本列表长度不足（" + result + "）");
        List<string> names = result.RegexSearch("(?<=a href=\"index_)[0-9.]+((_pre|[_-]snapshot[_-]?)[0-9]?)?(?=.html)").ToList(); names.Add("1.2.4");
        if (names.Count < 10) throw new Exception("获取到的版本数量不足（" + result + "）");
        return new("Forge 官方源", true, names);
    }
    public MinecraftDownloadResult<List<string>> DlForgeListBmclapiMain() {
        JArray result = (JArray)Source.RetryRequest(new MinecraftDownloadRequest { Url = "https://bmclapi2.bangbang93.com/forge/minecraft", Encoding = Encoding.Default, RequireJson = true }).DeserializeJson()!;
        List<string> names = result.Select(value => value.ToString()).ToList(); if (names.Count < 10) throw new Exception("获取到的版本数量不足"); return new("BMCLAPI", false, names);
    }
    public List<TForge> DlForgeVersionOfficialMain(string input) {
        string result;
        try { result = Source.LoaderRequest("https://files.minecraftforge.net/maven/net/minecraftforge/forge/index_" + input.Replace("-", "_") + ".html", true); }
        catch (Exception error) { if (Source.ErrorDisplay(error, false).Contains("(404)")) throw new Exception("无"); else throw; }
        if (result.Length < 1000) throw new Exception("获取到的版本列表长度不足（" + result + "）");
        List<TForge> versions = new();
        try {
            string[] codes = result.Substring(0, result.LastIndexOfF("</table>")).Split("<td class=\"download-version");
            for (int index = 1; index < codes.Length; index++) {
                string code = codes[index];
                try {
                    string name = code.RegexSeek("(?<=[^(0-9)]+)[0-9\\.]+")!;
                    bool recommended = code.Contains("fa promo-recommended");
                    string? branch = code.RegexSeek($"(?<=-{name}-)[^-\"]+(?=-[a-z]+.[a-z]{{3}})"); if (string.IsNullOrWhiteSpace(branch)) branch = null;
                    string time = code.RegexSeek("(?<=\"download-time\" title=\")[^\"]+")!;
                    string[] split = time.Split(" -:".ToCharArray());
                    DateTime date = new(Conversions.ToInteger(split[0]), Conversions.ToInteger(split[1]), Conversions.ToInteger(split[2]), Conversions.ToInteger(split[3]), Conversions.ToInteger(split[4]), Conversions.ToInteger(split[5]), 0, DateTimeKind.Utc);
                    string releaseTime = date.ToLocalTime().ToString("yyyy'/'MM'/'dd HH':'mm");
                    string md5, category;
                    if (code.Contains("classifier-installer\"")) { code = code.Substring(code.IndexOfF("installer.jar")); md5 = code.RegexSeek("(?<=MD5:</strong> )[^<]+")!; category = "installer"; }
                    else if (code.Contains("classifier-universal\"")) { code = code.Substring(code.IndexOfF("universal.zip")); md5 = code.RegexSeek("(?<=MD5:</strong> )[^<]+")!; category = "universal"; }
                    else if (code.Contains("client.zip")) { code = code.Substring(code.IndexOfF("client.zip")); md5 = code.RegexSeek("(?<=MD5:</strong> )[^<]+")!; category = "client"; }
                    else continue;
                    TForge entry = host.Forge(name, branch, input); entry.Category = category; entry.IsRecommended = recommended;
                    entry.Hash = MeloongCore.Extensions.StringExtensions.ReplaceLineEndings(md5, ""); entry.ReleaseTime = releaseTime; versions.Add(entry);
                } catch (Exception error) { throw new Exception("Forge 官方源版本信息提取失败（" + code + "）", error); }
            }
        } catch (Exception error) { throw new Exception("Forge 官方源版本列表解析失败（" + result + "）", error); }
        if (!versions.Any()) throw new Exception("无"); return versions;
    }
    public List<TForge> DlForgeVersionBmclapiMain(string input) {
        JArray json = (JArray)Source.RetryRequest(new MinecraftDownloadRequest { Url = "https://bmclapi2.bangbang93.com/forge/minecraft/" + input.Replace("-", "_"), RequireJson = true }).DeserializeJson()!;
        List<TForge> versions = new();
        try {
            string? recommended = Source.ForgeRecommended(input);
            foreach (JObject token in json) {
                string? hash = null; string category = "unknown"; int priority = -1;
                foreach (JObject file in token["files"]!) {
                    switch (file["category"]!.ToString()) {
                        case "installer": if (file["format"]!.ToString() == "jar") { hash = (string?)file["hash"]; category = "installer"; priority = 2; } break;
                        case "universal": if (priority <= 1 && file["format"]!.ToString() == "zip") { hash = (string?)file["hash"]; category = "universal"; priority = 1; } break;
                        case "client": if (priority <= 0 && file["format"]!.ToString() == "zip") { hash = (string?)file["hash"]; category = "client"; priority = 0; } break;
                    }
                }
                string? branch = (string?)token["branch"]; string name = (string)token["version"]!;
                TForge entry = host.Forge(name, branch, input); entry.Hash = hash; entry.Category = category; entry.IsRecommended = string.Equals(recommended ?? "", name ?? "", StringComparison.Ordinal);
                _ = token["modified"]!.ToString().Split(new[] { '-', 'T', ':', '.', ' ', '/' });
                entry.ReleaseTime = token["modified"]!.ToObject<DateTime>().ToLocalTime().ToString("yyyy'/'MM'/'dd HH':'mm"); versions.Add(entry);
            }
        } catch (Exception error) { throw new Exception("Forge BMCLAPI 版本列表解析失败（" + json.ToString() + "）", error); }
        if (!versions.Any()) throw new Exception("无"); return versions;
    }
    public MinecraftDownloadResult<List<TNeoForge>> DlNeoForgeListMain(bool mirror) {
        string latest = Source.RetryRequest(new MinecraftDownloadRequest { Url = mirror ? "https://bmclapi2.bangbang93.com/neoforge/meta/api/maven/details/releases/net/neoforged/neoforge" : "https://maven.neoforged.net/api/maven/versions/releases/net/neoforged/neoforge", RequireJson = true });
        string legacy = Source.RetryRequest(new MinecraftDownloadRequest { Url = mirror ? "https://bmclapi2.bangbang93.com/neoforge/meta/api/maven/details/releases/net/neoforged/forge" : "https://maven.neoforged.net/api/maven/versions/releases/net/neoforged/forge", RequireJson = true });
        if (latest.Length < 100 || legacy.Length < 100) throw new Exception("获取到的版本列表长度不足（" + latest + "）");
        try { return new(mirror ? "BMCLAPI" : "NeoForge 官方源", true, GetNeoForgeEntries(latest, legacy)); }
        catch (Exception error) { throw new Exception("NeoForge " + (mirror ? "BMCLAPI" : "官方源") + "版本列表解析失败（" + latest + "\r\n\r\n" + legacy + "）", error); }
    }
    public List<TNeoForge> GetNeoForgeEntries(string latest, string legacy) {
        IEnumerable<string> names = (legacy + latest).RegexSearch("(?<=\")(1\\.20\\.1-)?\\d+\\.[^\\.]+\\.\\d+(\\.\\d+)?(-(beta|alpha)(\\.\\d+)?)?(\\+snapshot-\\d+)?(?=\")");
        List<TNeoForge> versions = names.Where(name => name != "47.1.82").Select(host.NeoForge).OrderByDescending(value => value).ToList();
        if (!versions.Any()) throw new Exception("无"); return versions;
    }
    public MinecraftDownloadResult<List<TLiteLoader>> DlLiteLoaderListMain(bool mirror) {
        JObject result = (JObject)Source.RetryRequest(new MinecraftDownloadRequest { Url = mirror ? "https://bmclapi2.bangbang93.com/maven/com/mumfrey/liteloader/versions.json" : "https://dl.liteloader.com/versions/versions.json", RequireJson = true }).DeserializeJson()!;
        try {
            JObject json = (JObject)result["versions"]!; List<TLiteLoader> versions = new();
            foreach (KeyValuePair<string, JToken?> pair in json) {
                if (pair.Key.StartsWithF("1.6") || pair.Key.StartsWithF("1.5")) continue;
                JToken real = (pair.Value!["artefacts"] ?? pair.Value["snapshots"])!["com.mumfrey:liteloader"]!["latest"]!;
                TLiteLoader entry = host.LiteLoader(); entry.Inherit = pair.Key; entry.IsLegacy = Conversions.ToDouble(pair.Key.Split('.')[1]) < 8;
                entry.IsPreview = real["stream"]!.ToString().ToLowerInvariant() == "snapshot";
                entry.FileName = "liteloader-installer-" + pair.Key + (pair.Key == "1.8" || pair.Key == "1.9" ? ".0" : "") + "-00-SNAPSHOT.jar";
                entry.MD5 = (string)real["md5"]!; entry.ReleaseTime = Source.LocalTime(Source.GetDate(real["timestamp"]!)).ToString("yyyy'/'MM'/'dd HH':'mm"); entry.JsonToken = real; versions.Add(entry);
            }
            return new(mirror ? "BMCLAPI" : "LiteLoader 官方源", !mirror, versions);
        } catch (Exception error) { throw new Exception("LiteLoader " + (mirror ? "BMCLAPI" : "官方源") + "版本列表解析失败（" + result.ToString() + "）", error); }
    }
    public MinecraftDownloadResult<JObject> DlFabricListMain(bool mirror) {
        JObject result = (JObject)Source.RetryRequest(new MinecraftDownloadRequest { Url = mirror ? "https://bmclapi2.bangbang93.com/fabric-meta/v2/versions" : "https://meta.fabricmc.net/v2/versions", RequireJson = true }).DeserializeJson()!;
        try {
            MinecraftDownloadResult<JObject> output = new(mirror ? "BMCLAPI" : "Fabric 官方源", !mirror, result);
            if (output.Value["game"] is null || output.Value["loader"] is null || output.Value["installer"] is null) throw new Exception("获取到的列表缺乏必要项"); return output;
        } catch (Exception error) { throw new Exception("Fabric " + (mirror ? "BMCLAPI" : "官方源") + "版本列表解析失败（" + result.ToString() + "）", error); }
    }
    public List<T> DlFabricApiList<T>(Func<string, ResourcePlatforms, bool, List<T>> versions) => versions("fabric-api", ResourcePlatforms.Modrinth, false);
    public List<T> DlOptiFabricList<T>(Func<string, ResourcePlatforms, bool, List<T>> versions) => versions("322385", ResourcePlatforms.CurseForge, false);
}
