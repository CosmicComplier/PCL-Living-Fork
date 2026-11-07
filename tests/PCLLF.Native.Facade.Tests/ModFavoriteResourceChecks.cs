using System.Collections;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;
using PCL;
using PCLCS;

namespace PCLLF.Native.Facade.Tests;

internal static partial class ModFavoriteChecks {
    private static void ResourceRows(Scope s, PageFavorites favorites) {
        foreach (ResourcePlatforms platform in new[] { ResourcePlatforms.Modrinth, ResourcePlatforms.CurseForge })
        foreach (string kindName in new[] { "ResourcePacks", "ShaderPacks" }) {
            bool shader = kindName == "ShaderPacks";
            object kind = Native.Enum("PCL.InstanceResourceKind", kindName)!;
            string root = Path.Combine(s.Root, "resource-" + platform + "-" + kindName);
            Directory.CreateDirectory(root);
            var files = new List<string>();
            for (int i = 0; i < 3; i++) {
                string file = Path.Combine(root, "Different filename " + i + ".zip");
                using (ZipArchive zip = ZipFile.Open(file, ZipArchiveMode.Create))
                using (var writer = new StreamWriter(zip.CreateEntry(shader ? "shaders/fixture.glsl" : "pack.mcmeta").Open()))
                    writer.Write(shader ? "// " + i : "{\"pack\":{\"pack_format\":15,\"description\":\"Fixture " + i + "\"}}");
                files.Add(file); s.ModBytes.Add(file, File.ReadAllBytes(file));
            }
            string id = platform == ResourcePlatforms.Modrinth ? shader ? "Shader123" : "Pack123" : shader ? "43" : "42";
            string[] hashes = files.Select(file => Convert.ToHexString(SHA1.HashData(File.ReadAllBytes(file))).ToLowerInvariant()).ToArray();
            JObject project = platform == ResourcePlatforms.Modrinth
                ? new JObject { ["id"] = id, ["slug"] = "fixture", ["title"] = "Matched resource", ["description"] = "An authoritative fixture", ["project_type"] = shader ? "shader" : "resourcepack", ["date_modified"] = "2025-01-01T00:00:00Z", ["downloads"] = 7, ["categories"] = new JArray(), ["game_versions"] = new JArray("1.20.1") }
                : new JObject { ["id"] = int.Parse(id), ["slug"] = "fixture", ["name"] = "Matched resource", ["summary"] = "An authoritative fixture", ["isAvailable"] = true, ["links"] = new JObject { ["websiteUrl"] = "https://www.curseforge.com/minecraft/" + (shader ? "shaders" : "texture-packs") + "/fixture" }, ["dateReleased"] = "2025-01-01T00:00:00Z", ["downloadCount"] = 7, ["logo"] = new JObject(), ["categories"] = new JArray() };
            bool wrongIdentity = false;
            JObject Version(int i) => platform == ResourcePlatforms.Modrinth
                ? new JObject { ["id"] = "File" + i, ["project_id"] = id, ["name"] = "Version " + i, ["version_number"] = i + ".0", ["date_published"] = "2025-01-01T00:00:00Z", ["version_type"] = "release", ["downloads"] = 1, ["game_versions"] = new JArray("1.20.1"), ["loaders"] = new JArray(shader ? "iris" : "minecraft"), ["dependencies"] = new JArray(), ["files"] = new JArray(new JObject { ["filename"] = "Author " + i + ".zip", ["url"] = "https://cdn.modrinth.com/data/" + id + "/author.zip", ["size"] = new FileInfo(files[i]).Length, ["primary"] = true, ["hashes"] = new JObject { ["sha1"] = hashes[i] } }) }
                : new JObject { ["id"] = 1234567 + i, ["gameId"] = 432, ["modId"] = wrongIdentity ? 999 : int.Parse(id), ["displayName"] = "Version " + i, ["fileName"] = "Author " + i + ".zip", ["fileDate"] = "2025-01-01T00:00:00Z", ["releaseType"] = 1, ["downloadCount"] = 1, ["gameVersions"] = new JArray("1.20.1"), ["downloadUrl"] = "https://edge.forgecdn.net/files/1234/567/author.zip", ["fileFingerprint"] = 11 + i, ["dependencies"] = new JArray(), ["isAvailable"] = true, ["fileLength"] = new FileInfo(files[i]).Length, ["hashes"] = new JArray(new JObject { ["algo"] = 1, ["value"] = hashes[i] }) };
            int requests = 0;
            Func<string,HttpMethod,string,JToken> request = (url, _, _) => {
                requests++;
                if (url == "https://api.modrinth.com/v2/version_files") return platform == ResourcePlatforms.Modrinth ? new JObject { [hashes[0]] = Version(0), [hashes[1]] = Version(1) } : new JObject();
                if (url.StartsWith("https://api.modrinth.com/v2/projects?", StringComparison.Ordinal)) return new JArray(project);
                if (url == "https://api.modrinth.com/v2/version_files/update_individual") return new JObject();
                if (url == "https://api.curseforge.com/v1/fingerprints/432") return new JObject { ["data"] = new JObject { ["exactMatches"] = platform == ResourcePlatforms.CurseForge ? new JArray(Enumerable.Range(0,2).Select(i => new JObject { ["id"] = int.Parse(id), ["file"] = Version(i) })) : new JArray() } };
                if (url == "https://api.curseforge.com/v1/mods") return new JObject { ["data"] = new JArray(project) };
                if (url.StartsWith("https://api.curseforge.com/v1/mods/" + id + "/files?", StringComparison.Ordinal)) return new JObject { ["data"] = new JArray(), ["pagination"] = new JObject { ["totalCount"] = 0 } };
                throw new InvalidOperationException("Unexpected resource matching request: " + url);
            };
            var items = (IList)Native.Static("PCL.InstanceResourceStore", "Scan", root, root, kind, CancellationToken.None)!;
            IList Check() => (IList)Native.Static("PCL.InstanceResourceUpdates", "Check", root, items, kind, "1.20.1", CancellationToken.None, request,
                new Func<string,uint>(file => (uint)(Array.IndexOf(files.ToArray(),file)+11)))!;
            IList infos = Check();
            var matched = infos.Cast<object>().Where(info => Native.Get(info,"Project") is ResourceProject).ToArray();
            Native.Check(matched.Length == 2 && infos.Count == 3, "Exact resource matcher did not retain both recognized project copies and the unmatched entry.");
            var resourceProject = (ResourceProject)Native.Get(matched[0],"Project")!;
            Native.Check(resourceProject.Platform == platform && resourceProject.Id == id && !ReferenceEquals(Native.Get(matched[0],"ProjectMetadata"),project), "Matched project lost provider identity or kept a shared response object.");
            Native.Check(matched.All(info => !((bool)Native.Property(info.GetType(),"HasUpdate").GetValue(info)!)), "No-update fixture unexpectedly gained an update.");
            var page = (FrameworkElement)Activator.CreateInstance(Native.Type("PCL.PageInstanceResources"),Native.Flags,null,new[] { kind },null)!;
            Native.Call(page.GetType(),"ShowResources",page,root,root,items);
            Native.Call(page.GetType(),"ApplyOnlineInfo",page,infos);
            var panel = Part<Panel>(page,"PanList"); var rows = panel.Children.OfType<MyLocalModItem>().ToArray();
            foreach (var row in rows) s.Observe(row);
            var recognized = rows.Where(row => Part<MyIconButton>(row,"BtnFavorite").Visibility == Visibility.Visible).ToArray();
            Native.Check(recognized.Length == 2, "Recognized resource packs/shaders without an update still have no native favorite action.");
            var first = Part<MyIconButton>(recognized[0],"BtnFavorite"); var second = Part<MyIconButton>(recognized[1],"BtnFavorite");
            var unknown = Part<MyIconButton>(rows.Single(row => !recognized.Contains(row)),"BtnFavorite");
            int before = s.Store.Snapshot.Count, lookups = requests;
            Click(unknown); Native.Check(s.Store.Snapshot.Count == before, "An unmatched resource became a filename favorite.");
            var cloud = resourceProject.ToResourceItem(true,true).Init(); s.Observe(cloud); var cloudFavorite = Part<MyIconButton>(cloud,"BtnFavorite");
            Click(first);
            Native.Check(s.Store.Snapshot.Count == before+1 && first.Logo != s.EmptyIcon && second.Logo == first.Logo && cloudFavorite.Logo == first.Logo,
                "Resource management and download rows did not synchronize by project across different file versions: " + platform + "/" + kindName + "; count=" + s.Store.Snapshot.Count + "; first=" + (first.Logo != s.EmptyIcon) + "; second=" + (second.Logo == first.Logo) + "; cloud=" + (cloudFavorite.Logo == first.Logo));
            var saved = Part<Panel>(favorites,"PanResults").Children.Cast<FrameworkElement>().Select(MyVirtualizingElement.TryInit).OfType<MyResourceItem>()
                .Single(row => row.Tag is ResourceProject value && value.Platform == platform && value.Id == id);
            var savedFavorite = Part<MyIconButton>(saved,"BtnFavorite"); s.Observe(saved);
            Native.Check(savedFavorite.Logo == first.Logo, "The live favorites page did not mirror the resource/download state.");
            using (var held = new FileStream(s.File+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)) {
                SendClick(second); SendClick(cloudFavorite); SendClick(savedFavorite); Drain();
                Native.Check(Pending == 1 && s.Store.Snapshot.Count == before+1 && !first.IsEnabled && !second.IsEnabled && !cloudFavorite.IsEnabled && !savedFavorite.IsEnabled,
                    "Three interfaces admitted duplicate same-project writes or failed to synchronize the pending action.");
            }
            Wait(() => Pending == 0);
            Native.Check(s.Store.Snapshot.Count == before && first.Logo == s.EmptyIcon && second.Logo == first.Logo && cloudFavorite.Logo == first.Logo,
                "Removing a project from resource management did not clear both copies and the download row.");
            Click(cloudFavorite);
            var restored = Part<Panel>(favorites,"PanResults").Children.Cast<FrameworkElement>().Select(MyVirtualizingElement.TryInit).OfType<MyResourceItem>()
                .Single(row => row.Tag is ResourceProject value && value.Platform == platform && value.Id == id);
            Click(Part<MyIconButton>(restored,"BtnFavorite"));
            Native.Check(s.Store.Snapshot.Count == before && first.Logo == s.EmptyIcon && second.Logo == first.Logo,
                "A favorites-page removal did not synchronize back to resource management.");
            Native.Check(requests == lookups && panel.Children.Cast<MyLocalModItem>().SequenceEqual(rows) && rows.All(row => !row.Checked),
                "Favorite actions re-queried the platform, rebuilt resource rows or changed file selection.");
            wrongIdentity = true;
            if (platform == ResourcePlatforms.Modrinth) project["project_id"] = "Different123";
            IList mismatched = Check();
            Native.Check(mismatched.Cast<object>().All(info => Native.Get(info,"Project") is null), "Project/version identity mismatch was accepted as a favorite.");
            Native.Call(page.GetType(),"ApplyOnlineInfo",page,mismatched);
            Native.Check(rows.All(row => Part<MyIconButton>(row,"BtnFavorite").Visibility == Visibility.Collapsed), "Old favorite buttons survived an invalidated resource match.");
        }
        Console.WriteLine("PASS resource favorites: real MR/CF matching and native pack/shader rows synchronize with download and favorites; two versions per project, pending-write deduplication, unknown/mismatched identities, no extra requests or row selection.");
    }
}
