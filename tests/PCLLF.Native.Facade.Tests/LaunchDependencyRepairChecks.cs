using System.Collections;
using Newtonsoft.Json.Linq;
using PCL;

namespace PCLLF.Native.Facade.Tests;

internal static partial class DependencyCases<TContext, TMod, TDependency, TEdge, TIssue, TImpact, TReport, TOnlineProject, TOnlineDependency, TOnlineFile, TOptions, TCache>
{
    private static object LaunchRepair(LaunchFixture f)
    {
        TReport report = f.Scan(); LD(82, report.HasErrors);
        Native.Call(LaunchSession, "Accept", null, f.Mods, report);
        LD(84, LaunchAccepted(f.Mods, f.Scan()));
        f.Context.JavaVersion = "21.0.2"; LD(85, !LaunchAccepted(f.Mods, f.Scan())); f.Context.JavaVersion = null;
        Native.Call(LaunchSession, "SettingsChanged", null); LD(87, !LaunchAccepted(f.Mods, f.Scan()));
        LD(88, Native.Call(Inspector, "MatchVersion", null, "1.21.1", "[1.21.1,1.21.1]", "maven")!.ToString() == "Satisfied");
        foreach ((string version, string expected) in new[] { ("0.8.7", "Satisfied"), ("0.8.11", "Satisfied"), ("0.8.12", "Satisfied"), ("0.8.14", "Satisfied"), ("0.9.0", "Unsatisfied") })
            LD(89, Native.Call(Inspector, "MatchVersion", null, version, "0.8.x", "fabric")!.ToString() == expected, version);
        foreach ((string version, string expected) in new[] { ("1.21.1", "Satisfied"), ("1.21-beta.1", "Satisfied"), ("1.21.2-alpha.1", "Unsatisfied"), ("1.21.2", "Unsatisfied"), ("1.20.6", "Unsatisfied") })
            LD(90, Native.Call(Inspector, "MatchVersion", null, version, ">=1.21- <1.21.2-", "fabric")!.ToString() == expected, version);
        object plan = f.Plan(); LD(92, PlanList(plan, "Items").Count == 2, string.Join("; ", PlanNotes(plan)));
        LD(93, !f.Requests.Any(url => url.Contains("project/optional", StringComparison.Ordinal)));

        string pinned = LaunchFabric(Path.Combine(f.Mods, "pinned-consumer.jar"), "pinned_consumer", new JObject { ["lib_core"] = ">=1.0.0" });
        JObject pinnedData = LaunchFixture.Version(pinned, "pinned01", "pinnedv1", new JArray(LaunchFixture.Required("libproj1", "libver01")));
        string pinnedHash = Hash(pinned); f.VersionMap[pinnedHash] = pinnedData; f.PinFixture = true;
        object pinnedPlan = f.Plan();
        LD(98, PlanList(pinnedPlan, "Items").Count == 2 && PlanList(pinnedPlan, "Items").Cast<object>().Any(item => (string)Native.Get(item, "ProjectId")! == "libproj1" && PlanVersion(item).Id == "libver02"));
        f.ParentData["dependencies"]![0]!["version_id"] = "libver01"; pinnedPlan = f.Plan();
        LD(101, PlanList(pinnedPlan, "Items").Count == 2 && PlanList(pinnedPlan, "Items").Cast<object>().Any(item => (string)Native.Get(item, "ProjectId")! == "libproj1" && PlanVersion(item).Id == "libver02"));
        f.ParentData["dependencies"]![0]!["version_id"] = null;
        f.PinFixture = false; f.VersionMap.Remove(pinnedHash); File.Delete(pinned);
        f.CurseForge = true;
        object cfPlan = f.Plan();
        LD(116, PlanList(cfPlan, "Items").Count == 2 && PlanList(cfPlan, "Items").Cast<object>().All(item => (string)Native.Get(item, "Platform")! == "CurseForge"), string.Join("; ", PlanNotes(cfPlan)));
        f.CurseForge = false;

        Action<List<ModNet.NetFile>, CancellationToken> download = (files, token) =>
        {
            for (int i = 0; i < files.Count; i++) { token.ThrowIfCancellationRequested(); File.Copy(i == 0 ? f.Library : f.Base, files[i].LocalPath); }
        };
        string before = Hash(f.Source, "SHA256");
        Action<List<ModNet.NetFile>, CancellationToken> badDownload = (files, _) => { foreach (var file in files) File.WriteAllText(file.LocalPath, "bad"); };
        LaunchReject("launch", 122, () => LaunchInstall(plan, download: badDownload));
        using (CancellationTokenSource cancel = new())
        {
            cancel.Cancel(); LaunchReject("launch", 123, () => LaunchInstall(plan, cancel.Token, download));
        }
        string collision = Path.Combine(f.Mods, "basepr01.jar"); Directory.CreateDirectory(collision);
        LaunchReject("launch", 125, () => LaunchInstall(plan, download: download));
        LD(126, !File.Exists(Path.Combine(f.Mods, "libproj1.jar"))); Directory.Delete(collision);
        int count = LaunchInstall(plan, download: download);
        LD(128, count == 2 && !f.Scan().HasErrors && Hash(f.Source, "SHA256") == before);
        LD(129, PlanList(f.Plan(), "Items").Count == 0);
        File.Delete(Path.Combine(f.Mods, "libproj1.jar")); File.Delete(Path.Combine(f.Mods, "basepr01.jar"));
        f.BadLoader = true; object unavailable = f.Plan(); LD(131, PlanList(unavailable, "Items").Count == 0); f.BadLoader = false;

        byte[] sourceBytes = File.ReadAllBytes(f.Source); Dictionary<string, JObject> savedMap = new(f.VersionMap);
        File.Delete(f.Source); LaunchFabric(f.Source, "consumer", new JObject { ["lib_core"] = ">=2.0.0" });
        JObject incompatible = LaunchFixture.Version(f.Source, "parent01", "parentv1", new JArray(LaunchFixture.Required("libproj1")));
        f.VersionMap[Hash(f.Source)] = incompatible;
        object invalidPlan = f.Plan(); LaunchReject("launch", 137, () => LaunchInstall(invalidPlan, download: download));
        LD(138, !File.Exists(Path.Combine(f.Mods, "libproj1.jar")));
        File.WriteAllBytes(f.Source, sourceBytes); LaunchRestoreMap(f, savedMap);
        savedMap = new(f.VersionMap); File.Delete(f.Source); LaunchFabric(f.Source, "consumer", new JObject { ["lib_core"] = ">=1.0.0 <2.0.0" });
        JObject range = LaunchFixture.Version(f.Source, "parent01", "parentv1", new JArray(LaunchFixture.Required("libproj1")));
        f.VersionMap[Hash(f.Source)] = range;
        string newer = Jar(Path.Combine(f.Root, "packages"), "newer.jar", Entries(("fabric.mod.json", """{"schemaVersion":1,"id":"lib_core","name":"Core library","version":"2.0.0","depends":{"base_core":">=1.0.0"}}""")));
        f.FallbackData = LaunchFixture.Version(newer, "libproj1", "libver02", new JArray(LaunchFixture.Required("basepr01")));
        f.FallbackData["date_published"] = "2026-11-02T00:00:00Z"; f.FallbackData["files"]![0]!["filename"] = "lib-newer.jar";
        object fallbackPlan = f.Plan(); int fallbackAttempts = 0;
        Action<List<ModNet.NetFile>, CancellationToken> fallbackDownload = (files, token) =>
        {
            fallbackAttempts++;
            foreach (var file in files)
            {
                token.ThrowIfCancellationRequested();
                string source = file.LocalPath.EndsWith("-lib-newer.jar", StringComparison.Ordinal) ? newer : file.LocalPath.EndsWith("-basepr01.jar", StringComparison.Ordinal) ? f.Base : f.Library;
                File.Copy(source, file.LocalPath);
            }
        };
        LaunchInstall(fallbackPlan, download: fallbackDownload);
        LD(148, fallbackAttempts == 2 && !f.Scan().HasErrors && !File.Exists(Path.Combine(f.Mods, "lib-newer.jar")));
        File.Delete(Path.Combine(f.Mods, "libproj1.jar")); File.Delete(Path.Combine(f.Mods, "basepr01.jar"));
        File.WriteAllBytes(f.Source, sourceBytes); LaunchRestoreMap(f, savedMap); f.FallbackData = null;
        plan = f.Plan(); LaunchFabric(Path.Combine(f.Mods, "new.jar"), "new_mod");
        LaunchReject("launch", 150, () => LaunchInstall(plan, download: download)); File.Delete(Path.Combine(f.Mods, "new.jar"));
        string disabled = Path.Combine(f.Mods, "libproj1.jar.disabled"); File.Copy(f.Library, disabled);
        object disabledPlan = f.Plan(); LD(151, PlanNotes(disabledPlan).Any(note => note.Contains("已安装但被禁用", StringComparison.Ordinal)));
        LD(152, !string.Join(" ", PlanNotes(disabledPlan)).Contains("libproj1 已安装", StringComparison.Ordinal));
        File.Delete(disabled);
        Console.WriteLine("PASS launch original 39..152: real Main login/session and complete controlled Modrinth/CurseForge repair; real checksum/cancel/collision/rollback/range-fallback/stale/disabled cases, no HTTP.");
        return unavailable;
    }

    private static void LaunchRestoreMap(LaunchFixture fixture, Dictionary<string, JObject> saved)
    {
        fixture.VersionMap.Clear(); foreach (var item in saved) fixture.VersionMap.Add(item.Key, item.Value);
    }
}
