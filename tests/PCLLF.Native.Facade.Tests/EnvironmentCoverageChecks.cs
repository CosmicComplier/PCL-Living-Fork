using Newtonsoft.Json.Linq;
using static PCLLF.Native.Facade.Tests.EnvironmentHistoryChecks;

namespace PCLLF.Native.Facade.Tests;

internal static class EnvironmentCoverageChecks
{
    internal static void Run(Scope scope)
    {
        string mc = Path.Combine(scope.Root, "coverage/minecraft"), legacy = Path.Combine(mc, "versions/legacy"); string[] extra = ["coremods", "configureddefaults", "global_packs", "openloader"];
        Put(Path.Combine(legacy, "legacy.json"), "{\"id\":\"legacy\"}"); Put(Path.Combine(legacy, "config/setting.toml"), "old-setting");
        string id = Capture(legacy, legacy, "legacy fixture", description: "legacy"), record = PointFile(legacy, id); JObject point = JObject.Parse(File.ReadAllText(record)); point.Remove("coverageVersion"); Put(record, point.ToString()); History("MarkHealthy", legacy, id, true);
        foreach (string folder in extra) Put(Path.Combine(legacy, folder, "preserve.dat"), "untracked-" + folder);
        Put(Path.Combine(legacy, "config/setting.toml"), "changed-setting"); Put(Path.Combine(legacy, "mods/added.jar"), "added"); Restore(legacy, legacy, mc, id);
        foreach (string folder in extra) { string file = Path.Combine(legacy, folder, "preserve.dat"); Check("C030", File.Exists(file) && File.ReadAllText(file) == "untracked-" + folder); }
        Check("C031", File.ReadAllText(Path.Combine(legacy, "config/setting.toml")) == "old-setting" && !File.Exists(Path.Combine(legacy, "mods/added.jar")));
        Check("C033", Changes(Build(legacy, legacy)).Count == 0);
        string modern = Path.Combine(mc, "versions/modern"); Put(Path.Combine(modern, "modern.json"), "{\"id\":\"modern\"}");
        string newId = Capture(modern, modern, "empty extra directories", description: "modern"), newRecord = PointFile(modern, newId); JObject newPoint = JObject.Parse(File.ReadAllText(newRecord));
        Check("C037", (int)newPoint["coverageVersion"]! == 2); History("MarkHealthy", modern, newId, true);
        foreach (string folder in extra) Put(Path.Combine(modern, folder, "added.dat"), "added-" + folder);
        object report = Build(modern, modern); foreach (string folder in extra) Check("C041", Changes(report).Any(change => change.Relative == "g/" + folder + "/added.dat"));
        Restore(modern, modern, mc, newId); foreach (string folder in extra) Check("C043", !File.Exists(Path.Combine(modern, folder, "added.dat")));
        newPoint["coverageVersion"] = 99; Put(newRecord, newPoint.ToString()); Put(Path.Combine(modern, "coremods/preserve-unknown.jar"), "do-not-touch");
        Reject("C046", () => Restore(modern, modern, mc, newId)); Check("C047", File.ReadAllText(Path.Combine(modern, "coremods/preserve-unknown.jar")) == "do-not-touch"); NotRestoring();
        Console.WriteLine("PASS environment-history coverage: old implicit scope, explicit v2 empty directories, baseline-specific comparisons and unknown coverage rejected without mutation.");
    }
}
