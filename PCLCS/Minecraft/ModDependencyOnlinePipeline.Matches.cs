using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PCLCS;

// LF ModDependencyOnline.vb matching, cache JSON and project-only relation mapping.
// Preserve the real mutable DTOs and LF policy; platform IDs do not enter the local loader graph.
public sealed partial class ModDependencyOnlinePipeline<TContext, TMod, TDependency, TEdge, TIssue, TImpact, TReport, TOnlineProject, TOnlineDependency, TOnlineFile, TOptions, TCache> {
    // VB binary string conditions equate Nothing and ""; collection comparers deliberately remain unchanged.
    private static bool SourceOrdinal(string? left, string? right) => string.Equals(left ?? "", right ?? "", StringComparison.Ordinal);

    private static bool CachedMatches(FileData file, string platform, Session work) {
        JToken? value = work.Cache.Read("file|" + platform + "|" + file.Info.Sha1, work.Now());
        if (value is null) return false;
        file.Info.Matches.AddRange(value.ToObject<List<TOnlineProject>>()!);
        return true;
    }
    private static void StoreMatches(FileData file, string platform, Session work) {
        List<TOnlineProject> values = file.Info.Matches.Where(item => SourceOrdinal(item.Platform, platform)).ToList();
        work.Cache.Write("file|" + platform + "|" + file.Info.Sha1, JArray.FromObject(values), work.Now(), work.Lifetime(values.Count > 0));
    }
    private static void MatchModrinth(List<FileData> files, Session work) {
        List<FileData> pending = files.Where(item => !CachedMatches(item, "Modrinth", work)).ToList();
        if (pending.Count == 0) return;
        JObject? response = work.Ask("Modrinth", "https://api.modrinth.com/v2/version_files", HttpMethod.Post,
            new JObject(new JProperty("algorithm", "sha1"), new JProperty("hashes", new JArray(pending.Select(item => item.Info.Sha1).Distinct()))).ToString(Formatting.None)) as JObject;
        if (response is null) return;
        foreach (FileData file in pending) {
            JObject? version = response[file.Info.Sha1] as JObject;
            if (version is not null && ValidModrinthId(S(version["project_id"])) && ValidModrinthId(S(version["id"])) &&
                A(version["files"]).Any(item => S(item["hashes"]?["sha1"]).Equals(file.Info.Sha1, StringComparison.OrdinalIgnoreCase) &&
                    (item["size"] is null || SourceOrdinal(S(item["size"]), file.Length.ToString(CultureInfo.InvariantCulture))))) {
                TOnlineProject match = new() { Platform = "Modrinth", ProjectId = S(version["project_id"]), VersionId = S(version["id"]),
                    ProjectName = S(version["project_id"]), VersionName = Caption(S(version["version_number"])), IsExact = true, MatchBasis = "SHA1 精确文件匹配",
                    Website = "https://modrinth.com/mod/" + Uri.EscapeDataString(S(version["project_id"])) };
                foreach (JToken requirement in A(version["dependencies"]).Take(64)) {
                    string id = S(requirement["project_id"]), versionId = S(requirement["version_id"]), kind = S(requirement["dependency_type"]);
                    if (!new[] { "required", "optional", "incompatible", "embedded" }.Contains(kind)) continue;
                    match.Dependencies.Add(new TOnlineDependency { Platform = "Modrinth", ProjectId = ValidModrinthId(id) ? id : "",
                        VersionId = ValidModrinthId(versionId) ? versionId : "", FileName = Caption(S(requirement["file_name"])), Kind = kind });
                }
                file.Info.Matches.Add(match);
            }
            StoreMatches(file, "Modrinth", work);
        }
    }
    private static void MatchCurseForge(List<FileData> files, Session work) {
        List<FileData> pending = files.Where(item => !CachedMatches(item, "CurseForge", work)).ToList();
        if (pending.Count == 0) return;
        foreach (FileData file in pending) { work.Check(); Fingerprint(file, work); }
        JToken? response = work.Ask("CurseForge", "https://api.curseforge.com/v1/fingerprints/432", HttpMethod.Post,
            new JObject(new JProperty("fingerprints", new JArray(pending.Select(item => item.Fingerprint).Distinct()))).ToString(Formatting.None));
        if (response is null) return;
        foreach (JToken raw in A(response["data"]?["exactMatches"])) {
            JObject? version = raw["file"] as JObject;
            string id = S(raw["id"]);
            if (version is null || !ValidCurseForgeId(id) || !SourceOrdinal(S(version["modId"]), id) || !ValidCurseForgeId(S(version["id"]))) continue;
            if (version["gameId"] is not null && !SourceOrdinal(S(version["gameId"]), "432")) continue;
            foreach (FileData file in pending) {
                if (!SourceOrdinal(S(version["fileFingerprint"]), file.Fingerprint.ToString(CultureInfo.InvariantCulture)) ||
                    (version["fileLength"] is not null && !SourceOrdinal(S(version["fileLength"]), file.Length.ToString(CultureInfo.InvariantCulture))) ||
                    !A(version["hashes"]).Any(hash => (SourceOrdinal(S(hash["algo"]), "1") && S(hash["value"]).Equals(file.Info.Sha1, StringComparison.OrdinalIgnoreCase)) ||
                        (SourceOrdinal(S(hash["algo"]), "2") && S(hash["value"]).Equals(file.Md5, StringComparison.OrdinalIgnoreCase)))) continue;
                TOnlineProject match = new() { Platform = "CurseForge", ProjectId = id, VersionId = S(version["id"]), ProjectName = id,
                    VersionName = Caption(S(version["displayName"])), IsExact = true, MatchBasis = "指纹及内容哈希精确匹配", Website = "https://www.curseforge.com/projects/" + id };
                foreach (JToken requirement in A(version["dependencies"]).Take(64)) {
                    string dependencyId = S(requirement["modId"]);
                    string kind;
                    switch (S(requirement["relationType"])) {
                        case "2": kind = "optional"; break;
                        case "3": kind = "required"; break;
                        case "5": kind = "incompatible"; break;
                        case "1": case "6": kind = "embedded"; break;
                        default: continue;
                    }
                    if (ValidCurseForgeId(dependencyId)) match.Dependencies.Add(new TOnlineDependency { Platform = "CurseForge", ProjectId = dependencyId, Kind = kind });
                }
                if (!file.Info.Matches.Any(item => SourceOrdinal(item.Platform, "CurseForge") && SourceOrdinal(item.ProjectId, id) && SourceOrdinal(item.VersionId, match.VersionId))) file.Info.Matches.Add(match);
            }
        }
        foreach (FileData file in pending) StoreMatches(file, "CurseForge", work);
    }

    private static void ResolveVersionProjects(List<FileData> files, Session work) {
        List<TOnlineDependency> requirements = files.SelectMany(file => file.Info.Matches).Where(match => SourceOrdinal(match.Platform, "Modrinth")).SelectMany(match => match.Dependencies)
            .Where(item => SourceOrdinal(item.ProjectId, "") && ValidModrinthId(item.VersionId)).ToList();
        List<string> ids = new();
        foreach (TOnlineDependency requirement in requirements) {
            JToken? cached = work.Cache.Read("version|Modrinth|" + requirement.VersionId, work.Now());
            if (cached is not null) requirement.ProjectId = S(cached);
            else if (!ids.Contains(requirement.VersionId)) ids.Add(requirement.VersionId);
        }
        if (ids.Count == 0) return;
        JToken? response = work.Ask("Modrinth", "https://api.modrinth.com/v2/versions?ids=" + JsonIds(ids.Take(100)), HttpMethod.Get, null);
        if (response is null) return;
        foreach (JToken version in A(response)) {
            string id = S(version["id"]), project = S(version["project_id"]);
            if (!ids.Contains(id) || !ValidModrinthId(project)) continue;
            work.Cache.Write("version|Modrinth|" + id, new JValue(project), work.Now(), work.Lifetime(true));
            foreach (TOnlineDependency requirement in requirements.Where(item => SourceOrdinal(item.VersionId, id))) requirement.ProjectId = project;
        }
    }
    private static void ResolveProjects(List<FileData> files, string platform, Session work) {
        List<TOnlineProject> matches = files.SelectMany(file => file.Info.Matches).Where(match => SourceOrdinal(match.Platform, platform)).ToList();
        List<TOnlineDependency> requirements = matches.SelectMany(match => match.Dependencies).ToList();
        List<string> ids = matches.Select(match => match.ProjectId).Concat(requirements.Select(item => item.ProjectId)).Where(id => !SourceOrdinal(id, "")).Distinct().Take(100).ToList();
        Dictionary<string, JObject> projects = new(StringComparer.Ordinal);
        List<string> pending = new();
        foreach (string id in ids) {
            JObject? cached = work.Cache.Read("project|" + platform + "|" + id, work.Now()) as JObject;
            if (cached is not null) projects[id] = cached;
            else pending.Add(id);
        }
        if (pending.Count > 0) {
            JToken? response;
            if (SourceOrdinal(platform, "Modrinth")) response = work.Ask(platform, "https://api.modrinth.com/v2/projects?ids=" + JsonIds(pending), HttpMethod.Get, null);
            else {
                response = work.Ask(platform, "https://api.curseforge.com/v1/mods", HttpMethod.Post,
                    new JObject(new JProperty("modIds", new JArray(pending.Select(id => int.Parse(id, CultureInfo.InvariantCulture))))).ToString(Formatting.None));
                response = response?["data"];
            }
            foreach (JToken project in A(response)) {
                string id = S(project["id"]);
                if (!pending.Contains(id)) continue;
                bool valid = SourceOrdinal(platform, "Modrinth") ? SourceOrdinal(S(project["project_type"]), "mod") : IsCurseForgeModProject(project);
                string title = Caption(SourceOrdinal(platform, "Modrinth") ? S(project["title"]) : S(project["name"]));
                string website = SourceOrdinal(platform, "Modrinth") ? "https://modrinth.com/mod/" + Uri.EscapeDataString(id) :
                    IsCurseForgeWebsite(S(project["links"]?["websiteUrl"])) ? S(project["links"]!["websiteUrl"]) : "https://www.curseforge.com/projects/" + id;
                JObject slim = new(new JProperty("valid", valid), new JProperty("title", title), new JProperty("website", website));
                projects[id] = slim;
                work.Cache.Write("project|" + platform + "|" + id, slim, work.Now(), work.Lifetime(true));
            }
        }
        foreach (FileData file in files) file.Info.Matches.RemoveAll(match => SourceOrdinal(match.Platform, platform) && projects.ContainsKey(match.ProjectId) && !projects[match.ProjectId]["valid"]!.Value<bool>());
        foreach (TOnlineProject match in matches) {
            if (projects.TryGetValue(match.ProjectId, out JObject? project) && !SourceOrdinal(S(project!["title"]), "")) {
                match.ProjectName = S(project!["title"]);
                match.Website = S(project!["website"]);
            }
        }
        foreach (TOnlineDependency requirement in requirements) {
            if (projects.TryGetValue(requirement.ProjectId, out JObject? project) && project!["valid"]!.Value<bool>()) {
                requirement.ProjectName = S(project!["title"]);
                requirement.Website = S(project!["website"]);
            } else if (!SourceOrdinal(requirement.ProjectId, "")) {
                requirement.ProjectName = requirement.ProjectId;
                requirement.Website = SourceOrdinal(platform, "Modrinth") ? "https://modrinth.com/mod/" + Uri.EscapeDataString(requirement.ProjectId) : "https://www.curseforge.com/projects/" + requirement.ProjectId;
            }
        }
    }
    private static void SearchCandidates(List<FileData> files, Session work) {
        foreach (FileData file in files.Where(item => item.Info.IsSupplementTarget && item.Info.Matches.Count == 0).Take(Math.Max(0, Math.Min(4, work.Options.MaxSearchFiles)))) {
            string query = SearchQuery(file.Info.FullPath);
            if (query.Length < 3) continue;
            foreach (string platform in new[] { "Modrinth", "CurseForge" }) {
                work.Check();
                string key = "search|" + platform + "|" + file.Info.Sha1 + "|" + query;
                JToken? cached = work.Cache.Read(key, work.Now());
                if (cached is not null) { file.Info.Candidates.AddRange(cached.ToObject<List<TOnlineProject>>()!); continue; }
                JToken? response;
                IEnumerable<JToken> values;
                if (SourceOrdinal(platform, "Modrinth")) {
                    response = work.Ask(platform, "https://api.modrinth.com/v2/search?query=" + Uri.EscapeDataString(query) + "&limit=3&facets=" +
                        Uri.EscapeDataString("[[\"project_type:mod\"]]"), HttpMethod.Get, null);
                    values = A(response?["hits"]);
                } else {
                    response = work.Ask(platform, "https://api.curseforge.com/v1/mods/search?gameId=432&classId=6&pageSize=3&searchFilter=" + Uri.EscapeDataString(query), HttpMethod.Get, null);
                    values = A(response?["data"]);
                }
                if (response is null) continue;
                List<TOnlineProject> candidates = new();
                foreach (JToken project in values.Take(Math.Max(1, Math.Min(3, work.Options.CandidatesPerPlatform)))) {
                    string id = SourceOrdinal(platform, "Modrinth") ? S(project["project_id"]) : S(project["id"]);
                    if (SourceOrdinal(platform, "Modrinth") ? !ValidModrinthId(id) || !SourceOrdinal(S(project["project_type"]), "mod") : !ValidCurseForgeId(id) || !IsCurseForgeModProject(project)) continue;
                    candidates.Add(new TOnlineProject { Platform = platform, ProjectId = id, IsExact = false, MatchBasis = "文件名搜索候选",
                        ProjectName = Caption(SourceOrdinal(platform, "Modrinth") ? S(project["title"]) : S(project["name"])),
                        Website = SourceOrdinal(platform, "Modrinth") ? "https://modrinth.com/mod/" + Uri.EscapeDataString(id) :
                            IsCurseForgeWebsite(S(project["links"]?["websiteUrl"])) ? S(project["links"]!["websiteUrl"]) : "https://www.curseforge.com/projects/" + id });
                }
                file.Info.Candidates.AddRange(candidates);
                work.Cache.Write(key, JArray.FromObject(candidates), work.Now(), work.Lifetime(false));
            }
        }
    }
    private static void MapRelations(TReport report) {
        foreach (TOnlineFile file in report.OnlineFiles) {
            file.Status = file.Matches.Count > 0 ? "已按内容精确匹配到下载源版本。" : "未找到精确匹配；候选项目需要手动核对。";
            foreach (TOnlineProject match in file.Matches.Where(item => item.IsExact)) {
                foreach (TOnlineDependency requirement in match.Dependencies) {
                    requirement.MatchedPaths.Clear(); requirement.EnabledPaths.Clear();
                    if (!SourceOrdinal(requirement.ProjectId, "")) {
                        foreach (TOnlineFile provider in report.OnlineFiles.Where(item => item.Matches.Any(project => project.IsExact && SourceOrdinal(project.Platform, requirement.Platform) &&
                            SourceOrdinal(project.ProjectId, requirement.ProjectId) && (SourceOrdinal(requirement.VersionId, "") || SourceOrdinal(project.VersionId, requirement.VersionId))))) {
                            requirement.MatchedPaths.Add(provider.FullPath);
                            if (provider.Enabled) requirement.EnabledPaths.Add(provider.FullPath);
                        }
                    }
                    if (requirement.EnabledPaths.Count > 0) requirement.Status = "精确映射到启用文件：" + string.Join("、", requirement.EnabledPaths.Select(value => Path.GetFileName(value)));
                    else if (requirement.MatchedPaths.Count > 0) requirement.Status = "仅映射到禁用文件，需核对：" + string.Join("、", requirement.MatchedPaths.Select(value => Path.GetFileName(value)));
                    else requirement.Status = "未在本次精确匹配中找到，需核对；未据此判定缺失或版本错误";
                }
            }
        }
    }
}
