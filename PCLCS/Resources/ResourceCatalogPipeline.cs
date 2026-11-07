using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using MeloongCore;
using MeloongCore.Extensions;

namespace PCLCS;

public sealed class ResourceCatalogHost<TProject, TVersion> where TProject : ResourceProjectModelData where TVersion : ResourceVersionModelData {
    public Func<Dictionary<string, TProject>> ProjectCache = null!;
    public Func<Dictionary<string, List<TVersion>>> VersionCache = null!;
    public Func<JObject, TProject> CreateProject = null!;
    public Func<TVersion> CreateVersion = null!;
    public Func<string, HttpMethod?, string?, string?, JToken> Request = null!;
    public Action<string> Info = null!, Warn = null!;
}

// LF ResourceProject/ResourceVersion fetch and dependency-resolution pipeline, LF LICENCE.
// Fixed CE 1a7653127d578efdb2a9388999a3fdc67d9e49d5 ModComp.cs/CompFilesGet
// supplied same-source C# reference only. Its ConcurrentDictionary and shorter CF
// request are not adopted. Retry pageSize/failure behavior remains; project and
// version caches include the provider so equal raw IDs cannot return another platform.
public sealed class ResourceCatalogPipeline<TProject, TVersion>
    where TProject : ResourceProjectModelData where TVersion : ResourceVersionModelData {
    private readonly ResourceCatalogHost<TProject, TVersion> host;
    public ResourceCatalogPipeline(ResourceCatalogHost<TProject, TVersion> host) { this.host = host; }

    private static void ValidatePlatform(ResourcePlatforms platform) {
        if (platform.Flags().Count() != 1) throw new ArgumentException($"必须指定单一平台，当前值为 {platform}");
    }
    public TProject FromProjectId(string projectId, ResourcePlatforms platform) {
        ValidatePlatform(platform);
        string key = ResourceProjectKey.Get(platform, projectId);
        if (host.ProjectCache().TryGetValue(key, out TProject? cached) && cached.Platform == platform) return cached;
        return host.CreateProject((JObject)(platform == ResourcePlatforms.CurseForge
            ? host.Request("https://api.curseforge.com/v1/mods/" + projectId, null, null, null)["data"]!
            : host.Request("https://api.modrinth.com/v2/project/" + projectId, null, null, null)));
    }
    public IEnumerable<TProject> FromProjectIds(IEnumerable<string> projectIds, ResourcePlatforms platform) {
        ValidatePlatform(platform);
        List<string> missingProjectIds = new();
        foreach (string projectId in projectIds) {
            string key = ResourceProjectKey.Get(platform, projectId);
            if (host.ProjectCache().TryGetValue(key, out TProject? cached) && cached.Platform == platform) yield return cached;
            else missingProjectIds.Add(projectId);
        }
        if (!missingProjectIds.Any()) yield break;
        JArray projects;
        if (platform.HasFlagF(ResourcePlatforms.CurseForge)) {
            projects = (JArray)host.Request("https://api.curseforge.com/v1/mods", HttpMethod.Post,
                "{\"modIds\": [" + string.Join(",", missingProjectIds) + "]}", "application/json")["data"]!;
        } else projects = (JArray)host.Request("https://api.modrinth.com/v2/projects?ids=[\"" + string.Join("\",\"", missingProjectIds) + "\"]", null, null, null);
        foreach (JToken project in projects) yield return host.CreateProject((JObject)project);
    }

    public List<TVersion> VersionsFromProjectId(string projectId, ResourcePlatforms platform, bool loadDependencies = true) {
        ValidatePlatform(platform);
        TProject targetProject = FromProjectId(projectId, platform);
        string key = ResourceProjectKey.Get(platform, projectId);
        if (!host.VersionCache().ContainsKey(key)) {
            host.Info($"开始获取项目版本列表：{projectId}");
            JArray? resultJsonArray = null;
            if (platform == ResourcePlatforms.CurseForge) {
                for (int retryCount = 0; retryCount <= 2; retryCount++) {
                    JObject resultJson = (JObject)host.Request($"https://api.curseforge.com/v1/mods/{projectId}/files?pageSize=" + (10000 + retryCount), null, null, null);
                    if (resultJson["pagination"]!["resultCount"]!.ToObject<int>() == resultJson["pagination"]!["totalCount"]!.ToObject<int>()) {
                        resultJsonArray = (JArray)resultJson["data"]!; break;
                    } else if (retryCount < 2) {
                        host.Warn($"CurseForge 返回的资源版本列表存在缺失，即将进行第 {retryCount + 1} 次重试");
                        host.Info($"返回的原始内容如下：\r\n{resultJson}");
                    } else {
                        host.Info($"CurseForge 返回的资源版本列表存在缺失，返回的原始内容如下：\r\n{resultJson}");
                        throw new Exception("CurseForge 返回的资源版本列表存在缺失");
                    }
                }
            } else resultJsonArray = (JArray)host.Request($"https://api.modrinth.com/v2/project/{projectId}/version?include_changelog=false", null, null, null);
            HashSet<string?> seen = new();
            host.VersionCache()[key] = resultJsonArray!.Select(value => ResourceVersionRules.FromPlatformJson((JObject)value, targetProject.Types, host.CreateVersion))
                .Where(value => value.DownloadAvailable).Where(value => seen.Add(value.Id)).ToList();
        }
        List<TVersion> result = host.VersionCache()[key];
        if (!loadDependencies) return result;
        List<string> dependencies = result.SelectMany(file => file.RawDependencies).Distinct().ToList();
        if (dependencies.Any()) {
            host.Info($"项目 {projectId} 中需要加载的前置：{string.Join("，", dependencies)}");
            foreach (TProject dependencyProject in FromProjectIds(dependencies, targetProject.Platform)) {
                foreach (TVersion file in result) {
                    if (file.RawDependencies.Contains(dependencyProject.Id!) && !string.Equals(dependencyProject.Id ?? "", projectId ?? "", StringComparison.Ordinal) && !file.Dependencies.Contains(dependencyProject.Id!))
                        file.Dependencies.Add(dependencyProject.Id!);
                }
            }
        }
        return result;
    }
}
