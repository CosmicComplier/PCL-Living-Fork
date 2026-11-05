using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PCLCS;

public sealed partial class PackUpdateEngine<TFile, TPackage, TChange, TPlan, TNetFile>
    where TFile : PackUpdateFileData where TPackage : PackUpdatePackageData<TFile>
    where TChange : PackUpdateChangeData<TFile> where TPlan : PackUpdatePlanData<TPackage, TChange> {
    public TPlan Plan(string root, JObject? baseline, TPackage package) {
        if (baseline is null) throw new IOException("此版本没有作者文件清单，请先选择当前使用的原版整合包建立记录。");
        TPlan result = host.CreatePlan(); result.Root = root; result.Package = package; result.Baseline = baseline;
        var old = ((JObject)baseline["files"]!).Properties().ToDictionary(item => item.Name, item => item.Value, StringComparer.OrdinalIgnoreCase);
        foreach (string relative in old.Keys.Union(package.Files.Keys, StringComparer.OrdinalIgnoreCase)) {
            if (IsPrivate(relative)) continue;
            string fileName = Target(root, relative);
            old.TryGetValue(relative, out JToken? previous); package.Files.TryGetValue(relative, out TFile? incoming);
            string? before = Digest(fileName), oldHash = previous?["hash"]?.ToString();
            if (incoming is not null && incoming.OptionalFile && previous is null && before is null) {
                TChange change = host.CreateChange(); change.Relative = relative; change.Incoming = incoming; change.Conflict = true; change.Reason = "保留未安装的可选组件";
                result.Changes.Add(change); continue;
            }
            // An unchanged upstream must not restore a player's deleted, disabled or modified file.
            if (incoming is not null && (Same(incoming.Hash, oldHash) || Matches(fileName, incoming.Hash))) continue;
            if (incoming is null && before is null) continue;
            bool conflict = before is not null && IsPersonal(relative) || PersonalAreaExists(root, relative) || previous is not null && !Matches(fileName, oldHash) || previous is null && before is not null;
            TChange value = host.CreateChange(); value.Relative = relative; value.Before = before; value.Incoming = incoming; value.Conflict = conflict;
            value.Reason = conflict ? before is null ? "保留玩家删除或禁用" : "保留个人文件或修改" : incoming is null ? "移除作者已删除的文件" : before is null ? "新增" : "更新";
            result.Changes.Add(value);
        }
        HashSet<string> protectedIds = new(StringComparer.Ordinal);
        foreach (TChange change in result.Changes.Where(item => item.Conflict)) {
            string? id = old.ContainsKey(change.Relative!) ? old[change.Relative!]["identity"]?.ToString() : change.Incoming?.Identity;
            if (!string.IsNullOrEmpty(id)) protectedIds.Add(id!);
        }
        foreach (TChange change in result.Changes.Where(item => item.Incoming is not null && protectedIds.Contains(item.Incoming.Identity ?? ""))) {
            change.Conflict = true; change.Reason = "保留此项目的个人版本，跳过同项目新文件";
        }
        return result;
    }
    public void DownloadChanges(TPlan plan, string stage, CancellationToken token, Action<List<TNetFile>, CancellationToken>? download = null) {
        List<TNetFile> files = new();
        foreach (TChange change in plan.Changes.Where(item => !item.Conflict && item.Incoming is not null)) {
            TFile value = change.Incoming!;
            if (value.Payload is not null) continue;
            value.Payload = Target(stage, "download/" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.GetDirectoryName(value.Payload)!);
            files.Add(host.CreateDownload(value.Urls, value.Payload, value.Hash!.Split(':')[1]));
        }
        if (download is null) download = host.Download;
        if (files.Any()) download(files, token);
        foreach (TChange change in plan.Changes.Where(item => !item.Conflict && item.Incoming is not null)) {
            token.ThrowIfCancellationRequested();
            if (!Matches(change.Incoming!.Payload!, change.Incoming.Hash)) throw new IOException("整合包更新文件校验失败：" + change.Relative);
        }
        PreserveModCollisions(plan);
    }
    public static HashSet<string> ModIds(string fileName) {
        HashSet<string> result = new(StringComparer.Ordinal);
        try {
            using (ZipArchive archive = ZipFile.OpenRead(fileName)) {
                foreach (string name in new[] { "fabric.mod.json", "quilt.mod.json", "META-INF/mods.toml", "META-INF/neoforge.mods.toml", "mcmod.info" }) {
                    ZipArchiveEntry? entry = archive.GetEntry(name);
                    if (entry is null || entry.Length > 1024 * 1024) continue;
                    using (StreamReader reader = new(entry.Open())) {
                        string text = reader.ReadToEnd();
                        if (name.EndsWith(".toml")) {
                            foreach (Match match in Regex.Matches(text, "(?m)^\\s*modId\\s*=\\s*['\" ]*([a-zA-Z0-9_-]+)")) result.Add(match.Groups[1].Value);
                        } else {
                            JToken data = JToken.Parse(text);
                            if (name == "mcmod.info") {
                                JArray? items = data as JArray ?? data["modList"] as JArray;
                                foreach (JToken item in items ?? new JArray()) if (item["modid"] is not null) result.Add(item["modid"]!.ToString());
                            } else {
                                string? id = name == "quilt.mod.json" ? data["quilt_loader"]?["id"]?.ToString() : data["id"]?.ToString();
                                if (!string.IsNullOrEmpty(id)) result.Add(id!);
                            }
                        }
                    }
                }
            }
        } catch (InvalidDataException) { } catch (JsonException) { }
        return result;
    }
    public void PreserveModCollisions(TPlan plan) {
        string folder = Path.Combine(plan.Root!, "mods");
        host.EnsureWithin(plan.Root!, folder);
        if (!Directory.Exists(folder)) return;
        Dictionary<string, HashSet<string>> retained = new(StringComparer.OrdinalIgnoreCase);
        foreach (string fileName in Directory.GetFiles(folder)) {
            host.EnsureWithin(plan.Root!, fileName);
            if (!new[] { ".jar", ".disabled", ".old" }.Contains(Path.GetExtension(fileName).ToLowerInvariant())) continue;
            string relative = "mods/" + Path.GetFileName(fileName);
            if (plan.Changes.Any(item => !item.Conflict && item.Relative!.Equals(relative, StringComparison.OrdinalIgnoreCase))) continue;
            retained[relative] = ModIds(fileName);
        }
        foreach (TChange change in plan.Changes.Where(item => !item.Conflict && item.Incoming is not null && item.Relative!.StartsWith("mods/", StringComparison.OrdinalIgnoreCase))) {
            HashSet<string> ids = ModIds(change.Incoming!.Payload!);
            var collision = retained.FirstOrDefault(item => item.Value.Overlaps(ids));
            if (collision.Value is null) continue;
            change.Conflict = true; change.Reason = "保留个人模组 " + collision.Key + "，跳过重复模组";
            foreach (TChange other in plan.Changes.Where(item => item.Incoming is null && !item.Conflict && item.Relative!.StartsWith("mods/", StringComparison.OrdinalIgnoreCase))) {
                if (ModIds(Target(plan.Root!, other.Relative!)).Overlaps(ids)) {
                    other.Conflict = true; other.Reason = "此模组的新版与个人模组冲突，保留旧版";
                }
            }
        }
    }
}
