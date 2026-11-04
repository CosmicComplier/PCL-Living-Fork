using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json.Linq;

namespace PCLCS;

public sealed partial class PackUpdateEngine<TFile, TPackage, TChange, TPlan, TNetFile>
    where TFile : PackUpdateFileData where TPackage : PackUpdatePackageData<TFile>
    where TChange : PackUpdateChangeData<TFile> where TPlan : PackUpdatePlanData<TPackage, TChange> {
    public const string ManifestName = @"PCL\ModpackOrigin.json";
    private readonly PackUpdateHost<TFile, TPackage, TChange, TPlan, TNetFile> host;
    public PackUpdateEngine(PackUpdateHost<TFile, TPackage, TChange, TPlan, TNetFile> host) { this.host = host; }
    private static bool Same(string? left, string? right) => (left ?? "") == (right ?? "");
    public static string RelativePath(string value) {
        if (string.IsNullOrWhiteSpace(value) || Path.IsPathRooted(value)) throw new IOException("整合包包含无效路径。");
        value = value.Replace("\\", "/");
        foreach (string part in value.Split('/')) {
            if (part == "" || part == "." || part == ".." || part.EndsWith(".") || part.EndsWith(" ") || part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                Regex.IsMatch(part, @"\A(?:CON|PRN|AUX|NUL|COM[0-9]|LPT[0-9])(?:\.|$)", RegexOptions.IgnoreCase)) throw new IOException("整合包包含无效路径：" + value);
        }
        return value;
    }
    public string Target(string root, string relative) {
        string result = Path.Combine(root, RelativePath(relative).Replace('/', Path.DirectorySeparatorChar));
        host.EnsureWithin(root, result);
        if (Directory.Exists(result)) throw new IOException("文件位置已被文件夹占用：" + relative);
        return result;
    }
    public static string? Digest(string fileName, string algorithm = "sha1") {
        if (!File.Exists(fileName)) return null;
        using (Stream input = File.OpenRead(fileName))
        using (HashAlgorithm hasher = Same(algorithm, "md5") ? (HashAlgorithm)MD5.Create() : Same(algorithm, "sha512") ? (HashAlgorithm)SHA512.Create() : SHA1.Create())
            return algorithm + ":" + BitConverter.ToString(hasher.ComputeHash(input)).Replace("-", "").ToLowerInvariant();
    }
    public static bool Matches(string fileName, string? expected) => expected is not null && Same(Digest(fileName, expected.Split(':')[0]), expected);
    public static string ValidHash(string algorithm, string? value) {
        int length = Same(algorithm, "md5") ? 32 : Same(algorithm, "sha512") ? 128 : 40;
        if (!Regex.IsMatch(value ?? "", @"\A[0-9a-fA-F]{" + length + @"}\z")) throw new IOException("整合包文件缺少有效校验值。");
        return algorithm + ":" + value!.ToLowerInvariant();
    }
    public static bool IsPrivate(string relative) {
        string lower = relative.ToLowerInvariant();
        return lower.StartsWith("pcl/") || lower.StartsWith(".pcl-") || lower == ".pclignore";
    }
    public static bool IsPersonal(string relative) {
        string lower = relative.ToLowerInvariant();
        return lower == "options.txt" || lower.StartsWith("optionsof") || lower.StartsWith("optionsshaders") || lower == "servers.dat" || lower == "servers.dat_old" ||
            new[] { "saves/", "screenshots/", "xaerowaypoints/", "xaeroworldmap/", "journeymap/", "voxelmap/", "schematics/" }.Any(prefix => lower.StartsWith(prefix)) ||
            lower.Contains("waypoint") || lower.StartsWith("config/xaero") || lower.StartsWith("config/journeymap") || lower.StartsWith("config/voxelmap");
    }
    public bool PersonalAreaExists(string root, string relative) {
        string[] parts = relative.Split('/');
        if (parts.Length < 2) return false;
        string prefix = parts[0].ToLowerInvariant();
        string area = prefix == "saves" && parts.Length > 2 ? parts[0] + "/" + parts[1] : parts[0];
        string folder = Path.Combine(root, area.Replace('/', Path.DirectorySeparatorChar));
        host.EnsureWithin(root, folder);
        return IsPersonal(relative) && Directory.Exists(folder);
    }
    public static JObject JsonEntry(ZipArchiveEntry entry) {
        if (entry.Length > 16 * 1024 * 1024) throw new IOException("整合包清单过大。");
        using (StreamReader reader = new(entry.Open(), Encoding.UTF8)) return JObject.Parse(reader.ReadToEnd());
    }
    public TPackage ReadPackage(string archivePath, string stage, CancellationToken token, Func<string, HttpMethod?, string?, JToken>? request = null) {
        if (request is null) request = host.Request;
        TPackage result = host.CreatePackage();
        using (ZipArchive archive = ZipFile.OpenRead(archivePath)) {
            ZipArchiveEntry? entry = archive.Entries.FirstOrDefault(item => item.FullName == "modrinth.index.json" || item.FullName == "manifest.json");
            if (entry is null) {
                var entries = archive.Entries.Where(item => item.FullName.Split('/').Length == 2 && new[] { "modrinth.index.json", "manifest.json" }.Contains(item.Name)).ToList();
                if (entries.Count != 1) throw new IOException("请选择 CurseForge 或 Modrinth 格式的整合包文件。");
                entry = entries[0];
            }
            string prefix = entry.FullName.Substring(0, entry.FullName.Length - entry.Name.Length);
            JObject json = JsonEntry(entry);
            result.Name = json["name"]?.ToString() ?? "整合包";
            result.Version = json["versionId"]?.ToString() ?? json["version"]?.ToString() ?? "";
            List<string> overrideFolders = new();
            if (entry.Name == "modrinth.index.json") {
                result.Minecraft = json["dependencies"]?["minecraft"]?.ToString();
                foreach (var pair in new Dictionary<string, ModLoaders> { { "fabric-loader", ModLoaders.Fabric }, { "forge", ModLoaders.Forge }, { "neoforge", ModLoaders.NeoForge } })
                    if (json["dependencies"]?[pair.Key] is not null) result.Loaders[pair.Value] = json["dependencies"]![pair.Key]!.ToString();
                foreach (JProperty dependency in ((JObject)json["dependencies"]!).Properties())
                    if (!new[] { "minecraft", "fabric-loader", "forge", "neoforge" }.Contains(dependency.Name)) throw new IOException("暂不支持此整合包加载器：" + dependency.Name);
                foreach (JToken item in (json["files"] as JArray) ?? new JArray()) {
                    token.ThrowIfCancellationRequested();
                    if (item["env"]?["client"]?.ToString() == "unsupported") continue;
                    TFile value = host.CreateFile();
                    value.Relative = RelativePath(item["path"]!.ToString());
                    value.Hash = ValidHash("sha1", item["hashes"]?["sha1"]?.ToString());
                    value.OptionalFile = item["env"]?["client"]?.ToString() == "optional";
                    value.Urls = ((JArray)item["downloads"]!).Select(url => url.ToString()).ToList();
                    foreach (string url in value.Urls) {
                        Match match = Regex.Match(url, @"\Ahttps://cdn\.modrinth\.com/data/([A-Za-z0-9]+)/versions/");
                        if (match.Success) { value.Identity = "modrinth:" + match.Groups[1].Value; break; }
                    }
                    AddFile(result, value);
                }
                overrideFolders.Add(prefix + "overrides/"); overrideFolders.Add(prefix + "client-overrides/");
            } else {
                result.Minecraft = json["minecraft"]?["version"]?.ToString();
                foreach (JToken item in (json["minecraft"]?["modLoaders"] as JArray) ?? new JArray()) {
                    string id = item["id"]!.ToString(); int split = id.IndexOf('-');
                    if (split < 1) throw new IOException("无效的整合包加载器。");
                    ModLoaders kind = id.Substring(0, split) switch { "forge" => ModLoaders.Forge, "neoforge" => ModLoaders.NeoForge, "fabric" => ModLoaders.Fabric, _ => throw new IOException("暂不支持此整合包加载器：" + id) };
                    result.Loaders[kind] = id.Substring(split + 1);
                }
                var files = ((json["files"] as JArray) ?? new JArray()).ToList();
                if (files.Any()) {
                    JArray response = (JArray)request("https://api.curseforge.com/v1/mods/files", HttpMethod.Post,
                        new JObject(new JProperty("fileIds", new JArray(files.Select(item => (int)item["fileID"]!)))).ToString())["data"]!;
                    foreach (JToken item in files) {
                        token.ThrowIfCancellationRequested();
                        JToken? data = response.FirstOrDefault(value => value["id"]!.ToString() == item["fileID"]!.ToString());
                        if (data is null || data["modId"]!.ToString() != item["projectID"]!.ToString()) throw new IOException("整合包所需文件不可用，未修改当前版本。");
                        ResourceVersionModelData version = host.ParseVersion((JObject)data, ResourceTypes.Mod);
                        if (!version.DownloadAvailable) throw new IOException("整合包所需文件不允许下载：" + version.FileName);
                        var modules = ((data["modules"] as JArray) ?? new JArray()).Select(module => module["name"]!.ToString()).ToList();
                        string folder = version.FileName!.EndsWith(".jar", StringComparison.OrdinalIgnoreCase) ? "mods" : modules.Contains("shaders") ? "shaderpacks" : modules.Contains("pack.mcmeta") ? "resourcepacks" : "mods";
                        TFile value = host.CreateFile();
                        value.Relative = RelativePath(folder + "/" + version.FileName);
                        value.Hash = ValidHash(version.Hash?.Length == 32 ? "md5" : "sha1", version.Hash);
                        value.Identity = "curseforge:" + item["projectID"]!.ToString();
                        value.OptionalFile = item["required"] is not null && !(bool)item["required"]!;
                        value.Urls = version.DownloadUrls!.ToList();
                        AddFile(result, value);
                    }
                }
                string overridePath = (json["overrides"]?.ToString() ?? "overrides").TrimEnd('/');
                overrideFolders.Add(prefix + RelativePath(overridePath) + "/");
            }
            if (string.IsNullOrWhiteSpace(result.Minecraft)) throw new IOException("整合包缺少 Minecraft 版本。");
            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase); long total = 0;
            foreach (string folder in overrideFolders) {
                seen.Clear();
                foreach (ZipArchiveEntry item in archive.Entries.Where(value => value.FullName.StartsWith(folder, StringComparison.Ordinal) && !value.FullName.EndsWith("/"))) {
                    token.ThrowIfCancellationRequested();
                    string relative = RelativePath(item.FullName.Substring(folder.Length));
                    if (IsPrivate(relative)) continue;
                    if (!seen.Add(relative)) throw new IOException("整合包包含重复路径：" + relative);
                    total += item.Length;
                    if (total > 20L * 1024 * 1024 * 1024 || item.Length > 2L * 1024 * 1024 * 1024) throw new IOException("整合包覆写文件过大。");
                    string payload = Target(stage, "payload/" + Guid.NewGuid().ToString("N"));
                    Directory.CreateDirectory(Path.GetDirectoryName(payload)!);
                    using (Stream input = item.Open()) using (Stream output = File.Create(payload)) input.CopyTo(output);
                    TFile value = host.CreateFile(); value.Relative = relative; value.Payload = payload; value.Hash = Digest(payload);
                    result.Files[relative] = value;
                }
            }
        }
        return result;
    }
    public void AddFile(TPackage package, TFile value) {
        if (IsPrivate(value.Relative!)) return;
        foreach (string url in value.Urls) {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? address) || address.Scheme != "https" || address.UserInfo != "") throw new IOException("整合包包含无效下载地址。");
        }
        if (package.Files.ContainsKey(value.Relative!)) throw new IOException("整合包包含重复路径：" + value.Relative);
        package.Files.Add(value.Relative!, value);
    }
    public static JObject Manifest(TPackage package) {
        JObject files = new();
        foreach (TFile item in package.Files.Values) files[item.Relative!] = new JObject(new JProperty("hash", item.Hash), new JProperty("identity", item.Identity));
        return new JObject(new JProperty("schema", 1), new JProperty("name", package.Name), new JProperty("version", package.Version), new JProperty("minecraft", package.Minecraft),
            new JProperty("loaders", JObject.FromObject(package.Loaders)), new JProperty("files", files), new JProperty("source", package.Source));
    }
    public JObject? ReadManifest(string versionFolder) {
        string fileName = Target(versionFolder, ManifestName);
        if (!File.Exists(fileName)) return null;
        if (new FileInfo(fileName).Length > 16 * 1024 * 1024) throw new IOException("整合包作者清单过大。");
        JObject result = JObject.Parse(File.ReadAllText(fileName));
        if ((int)result["schema"]! != 1 || result["files"] is not JObject) throw new IOException("不支持此版本的整合包作者清单。");
        return result;
    }
    public JObject? Detect(string versionFolder) {
        JObject? recorded = ReadManifest(versionFolder);
        if (recorded is not null) return recorded;
        foreach (string name in new[] { "modrinth.index.json", "manifest.json" }) {
            string address = Target(versionFolder, name);
            if (!File.Exists(address)) continue;
            if (new FileInfo(address).Length > 16 * 1024 * 1024) throw new IOException("整合包清单过大。");
            JObject data = JObject.Parse(File.ReadAllText(address));
            string? mc = name == "modrinth.index.json" ? data["dependencies"]?["minecraft"]?.ToString() : data["minecraft"]?["version"]?.ToString();
            if (string.IsNullOrWhiteSpace(mc) || data["files"] is not JArray) continue;
            return new JObject(new JProperty("name", data["name"]?.ToString() ?? "整合包"), new JProperty("version", data["versionId"]?.ToString() ?? data["version"]?.ToString()), new JProperty("minecraft", mc), new JProperty("legacy", true));
        }
        return null;
    }
    public void RecordInstalled(string archivePath, string versionFolder, JObject? source = null) {
        string stage = host.TempFolder();
        try {
            TPackage package = ReadPackage(archivePath, stage, CancellationToken.None); package.Source = source;
            string root = host.InstanceGameRoot(versionFolder);
            foreach (string relative in package.Files.Keys.ToList()) if (!Matches(Target(root, relative), package.Files[relative].Hash)) package.Files.Remove(relative);
            host.AtomicWrite(Target(versionFolder, ManifestName), Encoding.UTF8.GetBytes(Manifest(package).ToString()));
        } catch (Exception ex) { host.Warn(ex, "整合包已安装，但记录作者文件清单失败；更新时可重新提供原始整合包"); }
    }
}
