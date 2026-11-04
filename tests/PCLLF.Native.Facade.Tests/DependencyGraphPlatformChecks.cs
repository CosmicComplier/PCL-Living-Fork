using System.Windows.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PCLLF.Native.Facade.Tests;

internal static partial class DependencyCases<TContext,TMod,TDependency,TEdge,TIssue,TImpact,TReport,TOnlineProject,TOnlineDependency,TOnlineFile,TOptions,TCache> {
    private static (TOnlineFile Peer,TOnlineProject Match) GraphPlatform(TReport report, string selected, string config) {
        TMod focus = report.Mods.Single(mod => mod.Id == "extra"); int errors = report.Issues.Count, mods = report.Mods.Count; focus.Dependencies.Add(GRequired("local_only"));
        TOnlineFile file = new() { FullPath = selected, Enabled = true, IsSupplementTarget = true };
        TOnlineProject exact = new() { Platform = "Modrinth", ProjectId = "ExactOwner", IsExact = true };
        foreach (var item in new[] { ("ConfigP", "Config library", config), ("RendererP", "Renderer", Path.Combine(GraphRoot, "renderer.jar")) }) {
            TOnlineDependency dependency = new() { Platform = "Modrinth", Kind = "required", ProjectId = item.Item1, ProjectName = item.Item2 }; dependency.EnabledPaths.Add(item.Item3); exact.Dependencies.Add(dependency);
        }
        TOnlineProject guess = new() { IsExact = false }; guess.Dependencies.Add(new TOnlineDependency { Kind = "required", ProjectName = "Filename guess" }); file.Matches.Add(exact); file.Matches.Add(guess); report.OnlineFiles.Add(file); report.OnlineAttempted = true;
        List<Border> nodes = GNodes(GView(report, selected));
        G(116, nodes.Count(node => GT(node) == "Config library" && GD(node).IsPlatformRequirement) == 1 && nodes.Count(node => GT(node) == "Renderer") == 1 && !nodes.Any(node => GT(node) == "Filename guess")); G(117, report.Issues.Count == errors && report.Mods.Count == mods);
        List<TOnlineFile> old = report.OnlineFiles; report.OnlineFiles = new();
        try { G(120, GNodes(GView(report, selected)).Count(node => GT(node) == "local_only") == 1); } finally { report.OnlineFiles = old; }
        G(122, !nodes.Any(node => GT(node) == "local_only")); G(123, !nodes.Any(node => GT(node) is "Fabric Loader" or "Minecraft"));
        report.OnlineTimedOut = true; try { G(126, GNodes(GView(report, selected)).Count(node => GT(node) == "local_only") == 1); } finally { report.OnlineTimedOut = false; }
        TOnlineFile peer = new() { FullPath = Path.Combine(GraphRoot, "consumer.jar"), Enabled = true }; TOnlineProject peerMatch = new() { IsExact = true }; peer.Matches.Add(peerMatch); report.OnlineFiles.Add(peer);
        G(131, !GNodes(GView(report, selected)).Any(node => GT(node) == "Consumer"));
        TOnlineDependency conflict = new() { Platform = "Modrinth", Kind = "incompatible", ProjectName = "Modern Iris" }; conflict.EnabledPaths.Add(Path.Combine(GraphRoot, "modern_iris.jar")); exact.Dependencies.Add(conflict);
        G(134, GNodes(GView(report, selected)).Count(node => GT(node) == "Modern Iris" && GD(node).IsConflict) == 1);
        string disabled = Path.Combine(GraphRoot, "disabled_config.jar.disabled"); TMod disabledMod = GModel("disabled_config", "Disabled config"); disabledMod.RootPath = disabled; disabledMod.Enabled = false; report.Mods.Add(disabledMod);
        foreach (string platform in new[] { "Modrinth", "CurseForge" }) { TOnlineDependency dependency = new() { Platform = platform, Kind = "required", ProjectId = platform + "Config", ProjectName = "Disabled config" }; dependency.MatchedPaths.Add(disabled); exact.Dependencies.Add(dependency); }
        Border[] attention = GNodes(GView(report, selected)).Where(node => GT(node) == "Disabled config").ToArray();
        G(139, attention.Length == 1 && GD(attention[0]).NeedsAttention && GD(attention[0]).Details!.Contains("Modrinth", StringComparison.Ordinal) && GD(attention[0]).Details!.Contains("CurseForge", StringComparison.Ordinal));
        // The original optional snapshot input uses this exact mutable list type.
        string snapshot = JsonConvert.SerializeObject(report.OnlineFiles); List<TOnlineFile> replay = JsonConvert.DeserializeObject<List<TOnlineFile>>(snapshot)!;
        Native.Check(replay.GetType() == report.GetType().GetField("OnlineFiles", Native.Flags)!.FieldType && replay[0].GetType().Assembly == Native.Main, "Read-only graph snapshot changed the original closed Main list/DTO identity.");
        // Replay the original snapshot-input path through the real mapper and
        // view without disturbing the live objects used by later impact checks.
        TReport snapshotReport = new() { Context = report.Context, OnlineAttempted = true, OnlineFiles = replay };
        snapshotReport.Mods.AddRange(report.Mods); GMap(snapshotReport);
        Native.Check(GNodes(GView(snapshotReport, selected)).Any(node => GT(node) == "Selected mod" && string.Equals(GD(node).FileName, selected, StringComparison.OrdinalIgnoreCase)), "Actual snapshot input did not render the selected Main model.");
        // Keep live report object identities for the later impact assertions.
        return (peer, peerMatch);
    }

    private static void GraphPinned(TContext context) {
        TReport report = new() { Context = context, OnlineAttempted = true };
        foreach (var item in new[] { ("iris", "Iris", "1.10.7"), ("sodium", "Sodium", "0.8.12") }) { TMod mod = GModel(item.Item1, item.Item2, item.Item3); report.Mods.Add(mod); report.OnlineFiles.Add(new TOnlineFile { FullPath = mod.RootPath, Enabled = true, IsSupplementTarget = true }); }
        foreach (string platform in new[] { "Modrinth", "CurseForge" }) {
            TOnlineProject owner = new() { Platform = platform, ProjectId = platform + "Iris", VersionId = "IrisVersion", IsExact = true }, sodium = new() { Platform = platform, ProjectId = platform + "Sodium", VersionId = "InstalledVersion", IsExact = true };
            owner.Dependencies.Add(new TOnlineDependency { Platform = platform, Kind = "required", ProjectId = sodium.ProjectId, ProjectName = "Sodium", VersionId = platform == "Modrinth" ? "PinnedOlderVersion" : "" }); report.OnlineFiles[0].Matches.Add(owner); report.OnlineFiles[1].Matches.Add(sodium);
        }
        GMap(report); List<Border> nodes = GNodes(GView(report, report.Mods[0].RootPath!)); Border[] sodiumNodes = nodes.Where(node => GT(node) == "Sodium").ToArray();
        G(155, sodiumNodes.Length == 1 && nodes.Count == 2); G(156, !GD(sodiumNodes[0]).NeedsAttention && GD(sodiumNodes[0]).Condition!.Contains("0.8.12", StringComparison.Ordinal) && GD(sodiumNodes[0]).Details!.Contains("PinnedOlderVersion", StringComparison.Ordinal) && GD(sodiumNodes[0]).Details!.Contains("CurseForge", StringComparison.Ordinal));
        G(157, report.OnlineFiles[0].Matches[0].Dependencies[0].EnabledPaths.Count == 0); G(159, GNodes(GView(report, report.Mods[1].RootPath!)).Count(node => GT(node) == "Iris" && GD(node).IsCandidate) == 1);
        foreach (string platform in new[] { "Modrinth", "CurseForge" }) report.OnlineFiles[0].Matches[0].Dependencies.Add(new TOnlineDependency { Platform = platform, Kind = "required", ProjectId = platform + "Unrelated", ProjectName = "Same display title" });
        Border[] unknown = GNodes(GView(report, report.Mods[0].RootPath!)).Where(node => GT(node) == "Same display title").ToArray(); G(164, unknown.Length == 2 && unknown.All(node => !GD(node).NeedsAttention && GD(node).IsCandidate));
        foreach (string name in new[] { "external-one.jar", "external-two.jar" }) report.OnlineFiles[0].Matches[0].Dependencies.Add(new TOnlineDependency { Platform = "Modrinth", Kind = "required", ProjectId = "", FileName = name });
        G(169, GNodes(GView(report, report.Mods[0].RootPath!)).Count(node => GT(node) is "external-one.jar" or "external-two.jar") == 2);
    }
}
