using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using MeloongCore.Extensions;
using Newtonsoft.Json.Linq;

namespace PCLCS;

public sealed partial class LocalResourceLoaderEngine<TLocal, TProject, TVersion>
    where TLocal : LocalResourceFileData<TProject, TVersion>
    where TProject : ResourceProjectModelData where TVersion : ResourceVersionModelData {
    private void LoadModrinth(List<TLocal> mods, string vanillaVersion, LocalResourceOnlineTask<TLocal> task, Thread currentTaskThread) {
        var hashes = mods.Select(m => m.ModrinthHash).ToList();
        JObject versions = (JObject)host.Request("https://api.modrinth.com/v2/version_files", HttpMethod.Post,
            "{\"hashes\": [\"" + string.Join("\",\"", hashes) + "\"], \"algorithm\": \"sha1\"}", "application/json");
        host.Info($"从 Modrinth 获取到 {versions.Count} 个本地 Mod 的对应信息");
        if (versions.Count == 0) return;
        Dictionary<string, List<TLocal>> mapping = new();
        Dictionary<TLocal, List<string>> loaders = new();
        foreach (TLocal entry in mods) {
            if (!versions.ContainsKey(entry.ModrinthHash)) continue;
            if ((string?)versions[entry.ModrinthHash]!["files"]![0]!["hashes"]!["sha1"] != entry.ModrinthHash) continue;
            string projectId = versions[entry.ModrinthHash]!["project_id"]!.ToString();
            if (host.ProjectCache().ContainsKey(projectId) && entry.Project is null) entry.Project = host.ProjectCache()[projectId];
            if (!mapping.ContainsKey(projectId)) mapping[projectId] = new List<TLocal>();
            mapping[projectId].Add(entry);
            TVersion file = host.ReadVersion((JObject)versions[entry.ModrinthHash]!, ResourceTypes.Mod);
            loaders[entry] = versions[entry.ModrinthHash]!["loaders"]!.Select(l => l.ToString().Lower()).Distinct().OrderBy(l => l).ToList();
            if (entry.ProjectVersion is null || entry.ProjectVersion.ReleaseDate < file.ReleaseDate) entry.ProjectVersion = file;
            else entry.ProjectVersion.Version = file.Version;
        }
        if (task.IsCanceledWithThread(currentTaskThread)) return;
        host.Info($"需要从 Modrinth 获取 {mapping.Count} 个本地 Mod 的工程信息");
        if (!mapping.Any()) return;
        JArray projects = (JArray)host.Request("https://api.modrinth.com/v2/projects?ids=[\"" + string.Join("\",\"", mapping.Keys) + "\"]", null, null, null);
        foreach (JToken json in projects) {
            TProject project = host.CreateProject((JObject)json);
            foreach (TLocal entry in mapping[project.Id!]) entry.Project = project;
        }
        host.Info("已从 Modrinth 获取本地 Mod 信息，继续获取更新信息");
        var toUpdate = mapping.SelectMany(l => l.Value).ToList();
        if (!toUpdate.Any()) return;
        string updateData = string.Join(",", toUpdate.Select(m => "{\"hash\": \"" + m.ModrinthHash + "\", \"loaders\": [\"" + string.Join("\",\"", loaders[m]) + "\"], \"game_versions\": [\"" + vanillaVersion + "\"]}"));
        JObject updates = (JObject)host.Request("https://api.modrinth.com/v2/version_files/update_individual", HttpMethod.Post,
            "{\"algorithm\": \"sha1\", \"hashes\": [" + updateData + "]}", "application/json");
        foreach (TLocal entry in mods) {
            if (!updates.ContainsKey(entry.ModrinthHash) || entry.ProjectVersion is null) continue;
            TVersion update = host.ReadVersion((JObject)updates[entry.ModrinthHash]!, ResourceTypes.Mod);
            if (!update.DownloadAvailable) continue;
            var updateLoaders = updates[entry.ModrinthHash]!["loaders"]!.Select(l => l.ToString().Lower()).Distinct().OrderBy(l => l).ToList();
            if (!updateLoaders.SequenceEqual(loaders[entry])) continue;
            host.Trace(() => $"本地文件 {entry.ProjectVersion.FileName} 在 Modrinth 上的最新版为 {update.FileName}");
            if (entry.ProjectVersion.ReleaseDate >= update.ReleaseDate || (entry.ProjectVersion.Hash ?? "") == (update.Hash ?? "")) continue;
            string changelog = $"https://modrinth.com/mod/{updates[entry.ModrinthHash]!["project_id"]}/changelog?g={vanillaVersion}";
            if (entry.UpdateFile is not null && (update.Hash ?? "") == (entry.UpdateFile.Hash ?? "")) {
                entry.ChangelogUrls.Add(changelog);
                update.DownloadUrls!.AddRange(entry.UpdateFile.DownloadUrls!);
                entry.UpdateFile = update;
            } else if (entry.UpdateFile is null || update.ReleaseDate >= entry.UpdateFile.ReleaseDate) {
                entry.ChangelogUrls = new List<string> { changelog };
                entry.UpdateFile = update;
            }
        }
        host.Info("从 Modrinth 获取本地 Mod 信息结束");
    }
}
