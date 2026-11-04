using System.Collections;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;
using PCL;
using PCLCS;

namespace PCLLF.Native.Facade.Tests;

internal static partial class DependencyCases<TContext, TMod, TDependency, TEdge, TIssue, TImpact, TReport, TOnlineProject, TOnlineDependency, TOnlineFile, TOptions, TCache>
{
    private static void LaunchActivation(string parent)
    {
        string root = Path.Combine(parent, "PclDependencyActivation-" + Guid.NewGuid().ToString("N"));
        string mods = Path.Combine(root, "mods"); Directory.CreateDirectory(mods);
        try
        {
            TContext context = new() { MinecraftVersion = "1.21.11", JavaVersion = "21.0.2", LoaderKnown = true };
            context.LoaderVersions.Add("fabricloader", "0.19.3");
            string[] ids = ["fabric-resource-conditions-api-v1", "fabric-events-interaction-v0", "fabric-networking-api-v1"];
            Dictionary<string, object> entries = new(StringComparer.Ordinal); JArray jars = new();
            for (int i = 0; i < ids.Length; i++)
            {
                string child = LaunchFabric(Path.Combine(root, "child" + i + ".jar"), ids[i], version: "5.0.35+test");
                string name = "META-INF/jars/child" + i + ".jar"; entries[name] = File.ReadAllBytes(child); jars.Add(new JObject { ["file"] = name });
            }
            entries["fabric.mod.json"] = new JObject
            {
                ["schemaVersion"] = 1, ["id"] = "fabric-api", ["name"] = "Fabric API", ["version"] = "0.141.6+1.21.11", ["jars"] = jars,
                ["depends"] = new JObject { ["minecraft"] = ">=1.21.11", ["fabricloader"] = ">=0.18.0" }
            }.ToString(Newtonsoft.Json.Formatting.None);
            string disabled = Jar(mods, "fabric-api-test.jar.disabled", entries);
            LaunchFabric(Path.Combine(mods, "lamb.jar"), "lamb", new JObject { [ids[0]] = ">=5.0.35" }, "1.0", "LambDynamicLights (Runtime)");
            LaunchFabric(Path.Combine(mods, "opti.jar"), "opti", new JObject { [ids[1]] = "*", [ids[2]] = "*" }, "1.0", "OptiGUI");
            TReport ScanActivation() => Scan(mods, context);
            object PlanActivation() => ActivationPlan(mods, ScanActivation());
            TReport report = ScanActivation(); object plan = PlanActivation();
            AD(35, PlanList(plan, "Enables").Count == 1 && PlanList(plan, "Items").Count == 0 && ((ICollection)Native.Get(PlanList(plan, "Enables")[0]!, "Requirements")!).Count == 3, string.Join("; ", PlanNotes(plan)));
            AD(36, (string)Native.Property(plan.GetType(), "ActionText").GetValue(plan)! == "启用并启动");
            AD(37, !report.Mods.Any(mod => mod.RootPath == disabled && mod.Enabled));
            Native.Static("PCL.ModSecret", "ThemeRefresh", 0);
            FrameworkElement view = (FrameworkElement)LaunchConstruct("PCL.ModDependencyCheckView", report, plan);
            Panel groups = (Panel)Native.Get(view, "Groups")!;
            AD(41, !groups.Children.OfType<MyCard>().Any(card => card.Title.StartsWith("缺少依赖", StringComparison.Ordinal)) &&
                groups.Children.OfType<MyCard>().Any(card => card.Title.StartsWith("可启用", StringComparison.Ordinal) && card.Title.Contains("1 个文件", StringComparison.Ordinal)));
            string sourceHash = Hash(disabled, "SHA256"), destination = (string)Native.Get(PlanList(plan, "Enables")[0]!, "Destination")!;
            using (CancellationTokenSource cancel = new())
            {
                cancel.Cancel(); LaunchReject("activation", 48, () => LaunchInstall(plan, cancel.Token));
            }
            AD(49, File.Exists(disabled) && !File.Exists(destination));
            int count = LaunchInstall(plan);
            AD(51, count == 1 && !ScanActivation().HasErrors && !File.Exists(disabled) && Hash(destination, "SHA256") == sourceHash);
            File.Move(destination, disabled); plan = PlanActivation();
            string collision = (string)Native.Get(PlanList(plan, "Enables")[0]!, "Destination")!;
            File.WriteAllText(collision, "existing-do-not-overwrite");
            LaunchReject("activation", 54, () => LaunchInstall(plan));
            AD(55, File.ReadAllText(collision) == "existing-do-not-overwrite" && File.Exists(disabled)); File.Delete(collision);
            plan = PlanActivation(); string extra = LaunchFabric(Path.Combine(root, "extra.jar"), "extra", version: "1.0", displayName: "Extra");
            object item = LaunchNew("PCL.ModDependencyAutoFill+Item"); Native.Set(item, "Name", "Extra");
            ResourceVersion version = new()
            {
                Id = "extra", ResourceType = ResourceTypes.Mod, ModLoaders = ModLoaders.Fabric, GameVersions = ["1.21.11"],
                FileName = "conflict.jar", Size = checked((int)new FileInfo(extra).Length), Hash = Hash(extra), DownloadUrls = ["https://cdn.modrinth.com/data/extra.jar"]
            };
            Native.Set(item, "Version", version); PlanList(plan, "Items").Add(item);
            string conflict = Path.Combine(mods, "conflict.jar"); Directory.CreateDirectory(conflict);
            Action<List<ModNet.NetFile>, CancellationToken> download = (files, token) => { foreach (var file in files) { token.ThrowIfCancellationRequested(); File.Copy(extra, file.LocalPath); } };
            LaunchReject("activation", 63, () => LaunchInstall(plan, download: download));
            AD(64, File.Exists(disabled) && !File.Exists(destination) && Hash(disabled, "SHA256") == sourceHash); Directory.Delete(conflict);
            List<ModNet.NetFile> files = [version.ToNetFile(Path.Combine(root, "target.jar"))];
            var task = (ModLoader.LoaderCombo<int>)Native.Call(LaunchAutoFill, "CreateDownloadTask", null, plan, files)!;
            AD(67, task.GetType().Assembly == Native.Main && task.Loaders.Count == 1 && task.Loaders[0] is ModNet.LoaderDownload && task.Loaders[0].Name.Contains("Extra", StringComparison.Ordinal));
            task.Cancel(); AD(69, task.State.ToString() == "Canceled" && task.Loaders[0].State.ToString() == "Canceled");
            context.MinecraftVersion = "1.20.1"; AD(70, PlanList(PlanActivation(), "Enables").Count == 0); context.MinecraftVersion = "1.21.11";
            string duplicate = Path.Combine(mods, "another-api.jar.disabled"); File.Copy(disabled, duplicate);
            AD(71, PlanList(PlanActivation(), "Enables").Count == 0); File.Delete(duplicate);
            Console.WriteLine("PASS activation original 35..71: true nested ZIP providers grouped by outer JAR, actual named Main download task cancellation, byte-preserving enable/recheck, no-overwrite and post-enable mixed rollback/ambiguity guards.");
        }
        finally
        {
            string full = Path.GetFullPath(root), prefix = Path.GetFullPath(Path.GetTempPath()).TrimEnd('\\') + "\\";
            AD(85, full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(full), "\\APclDependencyActivation-[a-f0-9]{32}\\z"));
            if (Directory.Exists(full)) Directory.Delete(full, true);
        }
    }
}
