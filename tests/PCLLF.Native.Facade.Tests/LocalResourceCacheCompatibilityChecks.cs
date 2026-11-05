using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using PCLCS;
using Local = PCLCS.LocalResourceFileData<PCLCS.ResourceProjectModel, PCLCS.ResourceVersionModel>;

namespace PCLLF.Native.Facade.Tests;
// Uses production ToJson/FromJson/LoadOnline with
// synthetic in-memory project metadata and a fixture-owned file; no real network.
internal static class LocalResourceCacheCompatibilityChecks
{
    internal static void Run(string fixtureRoot)
    {
        string folder = Path.Combine(fixtureRoot, "local-mod-cache-compat");
        Directory.CreateDirectory(folder);
        string file = Path.Combine(folder, "fixture.jar");
        File.WriteAllText(file, "synthetic local mod hash fixture");
        var services = new LocalResourceFileServices<ResourceProjectModel, ResourceVersionModel>
        {
            ReadHashCache = _ => "fixture-sha1",
            ReadProject = data => new ResourceProjectModel(data, ProjectServices())
        };
        foreach (ModLoaders loaders in new[] { ModLoaders.None, ModLoaders.Forge, ModLoaders.Fabric, ModLoaders.Forge | ModLoaders.LiteLoader })
        {
            var version = new McVersion("1.20.1", true);
            foreach (ModLoaders flag in new[] { ModLoaders.Forge, ModLoaders.LiteLoader, ModLoaders.Fabric })
                if (loaders.HasFlag(flag)) version.ModLoaderVersions[flag] = "fixture";
            string numericKey = "fixture-sha11.20.1" + ((int)loaders).ToString(System.Globalization.CultureInfo.InvariantCulture);
            string namedKey = "fixture-sha11.20.1" + loaders.ToString();
            foreach (bool numeric in new[] { true, false })
            {
                var item = new Local(new FileInfo(file), services);
                JObject payload = Payload(file, services, "cached project");
                JObject cache = new() { ["version"] = Versions.LocalModCacheVersion, ["unrelated-extension"] = "keep", [numeric ? numericKey : namedKey] = payload };
                int networkStarts = 0;
                var host = new LocalResourceLoaderHost<Local, ResourceProjectModel, ResourceVersionModel>
                {
                    Info = _ => { },
                    StartThread = (_, _) => { networkStarts++; throw new InvalidOperationException("Fresh compatible cache unexpectedly reached network boundary."); }
                };
                var engine = new LocalResourceLoaderEngine<Local, ResourceProjectModel, ResourceVersionModel>(host);
                engine.LoadOnline(new LocalResourceOnlineTask<Local> { Input = () => (new List<Local> { item }, cache, version), IsCanceled = () => false });
                Require(item.Project?.RawName == "cached project" && item.Project.LogoUrl == "https://example.invalid/fixture.png", "Cached project/icon was not restored for " + loaders + "/" + numeric);
                Require(item.OnlineDataLoaded && networkStarts == 0, "Fresh cache caused online work.");
                Require((string?)cache["unrelated-extension"] == "keep", "Cache migration discarded an unrelated entry.");
                // Required when the agreed repair normalizes named-key cache in memory.
                Require(cache.ContainsKey(numericKey), "Compatible read did not retain/recover the old numeric key.");
            }
            var preferred = new Local(new FileInfo(file), services);
            JObject both = new() { [numericKey] = Payload(file, services, "numeric winner"), [namedKey] = Payload(file, services, "named fallback") };
            var selection = new LocalResourceLoaderEngine<Local, ResourceProjectModel, ResourceVersionModel>(new() { Info = _ => { }, StartThread = (_, _) => throw new InvalidOperationException("Cache unexpectedly requested network.") });
            selection.LoadOnline(new LocalResourceOnlineTask<Local> { Input = () => (new List<Local> { preferred }, both, version), IsCanceled = () => false });
            Require(preferred.Project?.RawName == "numeric winner", "Legacy numeric key did not win when both formats exist.");
        }
    }
    private static ResourceProjectServices ProjectServices() => new() { StoreCache = _ => { } };
    private static JObject Payload(string file, LocalResourceFileServices<ResourceProjectModel, ResourceVersionModel> services, string title)
    {
        // Build from the real Modrinth model path, then serialize through the
        // production local cache writer rather than hand-authoring its schema.
        JObject api = new() { ["id"] = "fixture", ["slug"] = "fixture", ["title"] = title, ["description"] = "synthetic", ["updated"] = DateTime.UtcNow, ["downloads"] = 0, ["icon_url"] = "https://example.invalid/fixture.png", ["project_type"] = "mod", ["categories"] = new JArray(), ["game_versions"] = new JArray("1.20.1") };
        var local = new Local(new FileInfo(file), services) { Project = new ResourceProjectModel(api, ProjectServices()), OnlineDataLoaded = true };
        return local.ToJson();
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
