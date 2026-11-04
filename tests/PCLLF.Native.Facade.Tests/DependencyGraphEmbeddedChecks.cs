using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json.Linq;
using PCLCS;
using ShapePath = System.Windows.Shapes.Path;

namespace PCLLF.Native.Facade.Tests;

internal static partial class DependencyCases<TContext,TMod,TDependency,TEdge,TIssue,TImpact,TReport,TOnlineProject,TOnlineDependency,TOnlineFile,TOptions,TCache> {
    private static void GraphBundles(TContext context) {
        TReport bundles = new() { Context = context };
        foreach (string name in new[] { "api_bundle", "menu_bundle" }) { TMod outer = GModel(name, name), library = GModel("shared_api", "Bundled API"); library.RootPath = outer.RootPath; library.MetadataPath = "nested/library"; library.IsNested = true; outer.Dependencies.Add(GRequired("shared_api")); bundles.Mods.Add(outer); bundles.Mods.Add(library); }
        GRebuild(bundles); foreach (string name in new[] { "api_bundle", "menu_bundle" }) G(182, GNodes(GView(bundles, Path.Combine(GraphRoot, name + ".jar"))).Count == 1);
        TMod external = GModel("external_consumer", "External consumer"); external.RootPath = Path.Combine(GraphRoot, "external.jar"); external.Dependencies.Add(GRequired("shared_api")); bundles.Mods.Add(external); GRebuild(bundles);
        Border[] choice = GNodes(GView(bundles, external.RootPath)).Where(node => GD(node).Identity?.StartsWith("choice:", StringComparison.Ordinal) == true).ToArray(); G(188, choice.Length == 1 && GT(choice[0]).Contains("api_bundle", StringComparison.Ordinal) && GT(choice[0]).Contains("menu_bundle", StringComparison.Ordinal));
        Border[] possible = GNodes(GView(bundles, Path.Combine(GraphRoot, "api_bundle.jar"))).Where(node => GT(node) == "External consumer").ToArray(); G(191, possible.Length == 1 && GD(possible[0]).IsCandidate);
    }

    private static void GraphNested(TContext context) {
        TReport report = new() { Context = context, OnlineAttempted = true };
        TMod host = GModel("controller_host", "Controller host"); host.Format = "Fabric"; TDependency hostNeed = GRequired("shared_config"); hostNeed.Versions = new JValue(">=3.8.0"); host.Dependencies.Add(hostNeed);
        TMod config = GModel("shared_config", "Config library", "3.8.1"); config.Format = "Fabric"; config.RootPath = host.RootPath; config.MetadataPath = "nested/shared-config"; config.IsNested = true;
        TMod disabled = GModel("shared_config", "Config library", "3.8.2"); disabled.Format = "Fabric"; disabled.RootPath = Path.Combine(GraphRoot, "config.jar.disabled"); disabled.Enabled = false;
        TMod consumer = GModel("zoom_consumer", "Zoom consumer"); consumer.Format = "Fabric"; TDependency consumerNeed = GRequired("shared_config"); consumerNeed.Versions = new JValue(">=3.6.6"); consumer.Dependencies.Add(consumerNeed);
        report.Mods.AddRange([host, config, disabled, consumer]); GRebuild(report);
        foreach (TMod mod in new[] { host, disabled, consumer }) {
            TOnlineFile file = new() { FullPath = mod.RootPath, Enabled = mod.Enabled, IsSupplementTarget = true }; TOnlineProject project = new() { Platform = "Modrinth", ProjectId = ReferenceEquals(mod, disabled) ? "config01" : mod.Id!.Replace("_", ""), IsExact = true, ProjectName = mod.Name };
            if (!ReferenceEquals(mod, disabled)) project.Dependencies.Add(new TOnlineDependency { Platform = "Modrinth", ProjectId = "config01", Kind = "required", ProjectName = "Config library" }); file.Matches.Add(project); report.OnlineFiles.Add(file);
        }
        GMap(report); G(211, !report.HasErrors);
        Grid graph = GView(report, host.RootPath!); List<Border> nodes = GNodes(graph); Border center = nodes.Single(node => GT(node) == "Controller host"); Border[] embedded = nodes.Where(node => GD(node).IsEmbedded).ToArray();
        G(214, nodes.Count == 2 && embedded.Length == 1 && GT(embedded[0]) == "Config library" && GD(embedded[0]).Condition == "3.8.1" && !GD(embedded[0]).NeedsAttention); G(215, Canvas.GetTop(embedded[0]) > Canvas.GetTop(center) + center.Height);
        Canvas canvas = GCanvas(graph); TextBlock[] headers = canvas.Children.OfType<TextBlock>().Where(item => item.Text == "已内嵌模组（1）").ToArray(); ShapePath[] arrows = canvas.Children.OfType<ShapePath>().Where(item => item.Data is PathGeometry geometry && geometry.Figures[0].Segments[0] is LineSegment).ToArray(); G(219, headers.Length == 1 && arrows.Length == 1);
        PathFigure line = ((PathGeometry)arrows[0].Data).Figures[0]; var end = ((LineSegment)line.Segments[0]).Point; G(221, line.StartPoint.X == end.X && end.Y > line.StartPoint.Y && Math.Abs(line.StartPoint.Y - Canvas.GetTop(center) - center.Height) <= 1 && end.Y < Canvas.GetTop(headers[0]));
        Border[] providers = GNodes(GView(report, consumer.RootPath!)).Where(node => GT(node) == "Controller host").ToArray(); G(223, providers.Length == 1 && !GD(providers[0]).NeedsAttention && GD(providers[0]).Condition!.Contains("Config library", StringComparison.Ordinal));
        Border[] disabledCenters = GNodes(GView(report, disabled.RootPath)).Where(node => GT(node) == "Config library").ToArray(); G(225, disabledCenters.Length == 1 && GD(disabledCenters[0]).Condition!.Contains("已禁用", StringComparison.Ordinal) && GD(disabledCenters[0]).Details!.Contains("Controller host", StringComparison.Ordinal));
        report.OnlineAttempted = false; TMod clash = GModel("host_clash", "Host conflict"); TDependency clashNeed = GRequired(clash.Id!); clashNeed.KindValue = ModDependencyKind.Incompatible; host.Dependencies.Add(clashNeed); report.Mods.Add(clash); GRebuild(report);
        List<Border> mixed = GNodes(GView(report, host.RootPath!)); Border[] bundled = mixed.Where(node => GD(node).IsEmbedded).ToArray(), conflicts = mixed.Where(node => GD(node).IsConflict).ToArray(); G(232, bundled.Length == 1 && conflicts.Length == 1 && Canvas.GetTop(conflicts[0]) > Canvas.GetTop(bundled[0]) + bundled[0].Height);
        host.Dependencies.Remove(clashNeed); report.Mods.Remove(clash); config.Version = "3.0.0"; GRebuild(report); G(236, report.HasErrors); host.Enabled = false; config.Enabled = false; GRebuild(report); G(238, report.HasErrors);
    }
}
