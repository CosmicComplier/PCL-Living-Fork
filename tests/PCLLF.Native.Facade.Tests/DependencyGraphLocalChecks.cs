using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Newtonsoft.Json.Linq;
using PCLCS;
using ShapePath = System.Windows.Shapes.Path;
using Polygon = System.Windows.Shapes.Polygon;

namespace PCLLF.Native.Facade.Tests;

internal static partial class DependencyCases<TContext,TMod,TDependency,TEdge,TIssue,TImpact,TReport,TOnlineProject,TOnlineDependency,TOnlineFile,TOptions,TCache> {
    private static (TReport Report,string Selected,string Config) GraphLocal() {
        string selected = GJar("extra", "Selected mod", extra: Obj("{\"provides\":[\"extra-alias\"],\"depends\":{\"minecraft\":\">=1.21\",\"fabricloader\":\">=0.18\",\"renderer\":\">=1.0\"},\"recommends\":{\"recommendation\":\"*\"},\"breaks\":{\"old_iris\":\"<2.0\",\"modern_iris\":\"<1.0\"}}"));
        string renderer = GJar("renderer", "Renderer", "1.2", Obj("{\"icon\":\"icon.png\"}")); GJar("old_iris", "Old Iris"); GJar("modern_iris", "Modern Iris", "3.0");
        GJar("consumer", "Consumer", extra: Obj("{\"depends\":{\"extra\":\"*\",\"extra-alias\":\"*\"}}")); GJar("optional_consumer", "Optional consumer", extra: Obj("{\"suggests\":{\"extra\":\"*\"}}")); GJar("reverse_conflict", "Reverse conflict", extra: Obj("{\"breaks\":{\"extra\":\"*\"}}")); GJar("unrelated_renderer_user", "Unrelated renderer user", extra: Obj("{\"depends\":{\"renderer\":\"*\"}}")); string config = GJar("recommendation", "Config library", "1.1");
        TReport report = Scan(GraphRoot, GContext()); TMod focus = report.Mods.Single(mod => mod.Id == "extra"); TDependency loader = focus.Dependencies.Single(dep => dep.Id == "fabricloader"); focus.Dependencies.Add(loader); focus.Dependencies.Add(new TDependency { Id = "fabricloader", Versions = new JValue(">=0.19") });
        TDependency unless = GRequired("renderer"); focus.Dependencies.Add(new TDependency { Id = "modmenu", Unless = unless }); report.Mods.Single(mod => mod.Id == "optional_consumer").Dependencies.Add(new TDependency { Id = "extra", Unless = unless }); focus.Dependencies.Add(GRequired("extra-alias"));
        TMod shadow = GModel("renderer", "Bundled renderer fallback", "0.1"); shadow.Format = "Fabric"; shadow.IsNested = true; shadow.RootPath = selected; shadow.MetadataPath = "nested/shadow"; report.Mods.Add(shadow);
        foreach (string format in new[] { "Fabric", "Quilt", "Forge", "NeoForge" }) { TMod nested = GModel("internal_" + format.ToLowerInvariant(), "Internal " + format + " component", "2.0"); nested.Format = format; nested.IsNested = true; nested.RootPath = renderer; nested.MetadataPath = "nested/" + format; report.Mods.Add(nested); focus.Dependencies.Add(GRequired(nested.Id!)); }
        GRebuild(report); Grid graph = GView(report, Extended(selected)); List<Border> nodes = GNodes(graph); string[] titles = nodes.Select(GT).ToArray();
        foreach (string title in new[] { "Selected mod", "Minecraft", "Fabric Loader", "Renderer", "Consumer", "Old Iris", "Reverse conflict" }) G(80, titles.Contains(title));
        G(81, !titles.Contains("Unrelated renderer user")); G(82, nodes.Count == 7 && titles.Count(title => title == "Consumer") == 1);
        Border[] containers = nodes.Where(node => GT(node) == "Renderer").ToArray(); G(84, containers.Length == 1 && !titles.Any(title => title.StartsWith("Internal ", StringComparison.Ordinal) && title.EndsWith(" component", StringComparison.Ordinal)));
        foreach (string format in new[] { "fabric", "quilt", "forge", "neoforge" }) G(85, GD(containers[0]).Details!.Contains("internal_" + format, StringComparison.Ordinal));
        Native.Call(graph.GetType(), "LoadIconsForPreview", graph); Image image = (Image)((Panel)containers[0].Child).Children[0]; G(88, image.Source is BitmapSource { IsFrozen: true, PixelWidth: 32 });
        G(90, Native.Static("PCL.ModDependencyIcon", "Read", renderer, "../icon.png") is null && Native.Static("PCL.ModDependencyIcon", "Read", renderer, "fabric.mod.json") is null);
        using (FileStream exclusive = new(renderer, FileMode.Open, FileAccess.Read, FileShare.None)) { }
        G(91, nodes.Count(node => GD(node).IsConflict) == 2); Border[] loaderNodes = nodes.Where(node => GT(node) == "Fabric Loader").ToArray(); G(93, loaderNodes.Length == 1 && GD(loaderNodes[0]).Condition!.Contains(">=0.18", StringComparison.Ordinal) && GD(loaderNodes[0]).Condition!.Contains(">=0.19", StringComparison.Ordinal));
        Border center = nodes.Single(node => GT(node) == "Selected mod"); foreach (Border box in nodes) G(95, !GD(box).IsConflict || (Canvas.GetLeft(box) == Canvas.GetLeft(center) && Canvas.GetTop(box) > Canvas.GetTop(center) + center.Height));
        Canvas canvas = GCanvas(graph); G(97, canvas.Children.OfType<ShapePath>().Count() == 6 && canvas.Children.OfType<Polygon>().Count() == 6);
        foreach (Border box in nodes) { double x = Canvas.GetLeft(box), y = Canvas.GetTop(box); G(98, x >= 0 && y >= 0 && x + box.Width <= canvas.Width && y + box.Height <= canvas.Height); }
        for (int i = 0; i < nodes.Count; i++) for (int j = i + 1; j < nodes.Count; j++) { Border a = nodes[i], b = nodes[j]; G(102, !new Rect(Canvas.GetLeft(a), Canvas.GetTop(a), a.Width, a.Height).IntersectsWith(new Rect(Canvas.GetLeft(b), Canvas.GetTop(b), b.Width, b.Height))); }
        return (report, selected, config);
    }
}
