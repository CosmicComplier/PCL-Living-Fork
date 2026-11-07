using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Inspection = PCLCS.ModDependencyInspection<PCLCS.PackUpdateModInspection.InspectionContext, PCLCS.PackUpdateModInspection.Mod, PCLCS.PackUpdateModInspection.Dependency, PCLCS.PackUpdateModInspection.Edge, PCLCS.PackUpdateModInspection.Issue, PCLCS.PackUpdateModInspection.Impact, PCLCS.PackUpdateModInspection.Report, PCLCS.PackUpdateModInspection.Project, PCLCS.PackUpdateModInspection.OnlineDependency, PCLCS.PackUpdateModInspection.OnlineFile>;

namespace PCLCS;

/// <summary>Declaration checks for the actual final combination, not a runtime compatibility guarantee.</summary>
public sealed class PackDependencyGraphResult {
    public bool HasErrors, HasUnknown;
    public List<string> Errors = new(), Warnings = new();
    public List<PackDependencyProblem> Problems = new();
    public List<string> UnverifiedFiles = new();
    internal PackUpdateModInspection.Report? Report;
}
public sealed class PackDependencyProblem {
    public ModDependencyIssueKind Kind;
    public string OwnerPath = "", RequirementId = "", Message = "";
    public bool IsError;
    public List<string> RelatedPaths = new();
}

/// <summary>
/// Checks the enabled files as one graph. The caller supplies the proposed final
/// files, including staged replacements instead of the originals they replace.
/// Author provenance does not suppress real dependency or conflict declarations.
/// </summary>
public static class PackDependencyGraph {
    public static PackDependencyGraphResult Check(IEnumerable<string> enabledJars, string? minecraft,
        Dictionary<ModLoaders, string> loaders, CancellationToken token = default, string? javaVersion = null) {
        if (enabledJars is null) throw new ArgumentNullException(nameof(enabledJars));
        if (loaders is null) throw new ArgumentNullException(nameof(loaders));
        token.ThrowIfCancellationRequested();
        PackDependencyGraphResult result = new();
        PackUpdateModInspection.InspectionContext context = new() { MinecraftVersion = minecraft, JavaVersion = javaVersion, LoaderKnown = loaders.Count > 0, IgnoreJavaRequirements = true };
        foreach (var loader in loaders) {
            string? id = loader.Key switch {
                ModLoaders.Fabric => "fabricloader", ModLoaders.Forge => "forge",
                ModLoaders.NeoForge => "neoforge", ModLoaders.LiteLoader => "liteloader", _ => null
            };
            if (id is not null) context.LoaderVersions[id] = loader.Value;
        }
        if (context.LoaderVersions.ContainsKey("neoforge") && minecraft == "1.20.1")
            context.LoaderVersions["forge"] = context.LoaderVersions["neoforge"];
        var report = new PackUpdateModInspection.Report { Context = context };
        StringComparer paths = Path.DirectorySeparatorChar == '\\' ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        HashSet<string> seen = new(paths);
        int count = 0;
        foreach (string path in enabledJars) {
            token.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(path)) { result.HasUnknown = true; result.Warnings.Add("最终模组列表中存在空路径，无法确认该文件。"); continue; }
            if (path.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".old", StringComparison.OrdinalIgnoreCase)) continue;
            string fullPath;
            try { fullPath = Path.GetFullPath(path); }
            catch (Exception error) when (error is ArgumentException or NotSupportedException or PathTooLongException) {
                result.HasUnknown = true; result.Warnings.Add("模组路径无法确认：" + path + "（" + error.Message + "）"); continue;
            }
            if (!seen.Add(fullPath)) continue;
            var single = Inspection.ReadSingleArchive(fullPath, context, token);
            if (count++ == 0) report.Builtins = single.Builtins;
            report.Mods.AddRange(single.Mods);
            report.MetadataIncomplete |= single.MetadataIncomplete;
            if (single.MetadataIncomplete) result.UnverifiedFiles.Add(fullPath);
            // A single-file report cannot resolve peers in other files. Only keep
            // parser notices; rebuild all Issues/Edges from the combined Mods.
            report.Notices.AddRange(single.Notices);
        }
        token.ThrowIfCancellationRequested();
        Inspection.BuildReport(report, token);
        result.Report = report;
        result.Problems.AddRange(report.Issues.Select(issue => new PackDependencyProblem {
            Kind = issue.KindValue, IsError = issue.IsError, OwnerPath = issue.Owner?.RootPath ?? "",
            RequirementId = issue.Requirement?.Id ?? "", Message = issue.Message, RelatedPaths = issue.RelatedMods.Select(mod => mod.RootPath).Distinct(paths).ToList()
        }));
        result.Errors.AddRange(report.Issues.Where(issue => issue.IsError).Select(issue => issue.Message));
        result.Warnings.AddRange(report.Notices);
        result.Warnings.AddRange(report.Issues.Where(issue => !issue.IsError).Select(issue => issue.Message));
        var unknownRequired = report.Edges.Where(edge => edge.Evaluated && !edge.ConditionBypassed &&
            edge.Requirement.KindValue == ModDependencyKind.Required &&
            (edge.Requirement.IsUnknown || edge.StateValue == ModDependencyMatchState.Unknown)).ToArray();
        result.HasUnknown |= report.MetadataIncomplete || unknownRequired.Length > 0;
        result.UnverifiedFiles.AddRange(unknownRequired.Select(edge => edge.Owner.RootPath));
        result.Warnings.AddRange(unknownRequired.Select(edge => "文件 " + Path.GetFileName(edge.Owner.RootPath) + "：" +
            edge.Owner.Display + " 需要 " + edge.Requirement.Display + "，当前无法完整确认。"));
        if (count > 0 && (string.IsNullOrWhiteSpace(minecraft) || !context.LoaderKnown)) {
            result.HasUnknown = true;
            result.UnverifiedFiles.AddRange(seen);
            result.Warnings.Add("尚未完整确认目标 Minecraft／加载器，无法证明最终组合的声明兼容性。");
        }
        // Reuse the shared loader's known-error classification. Alias providers
        // may be alternatives, so overlap alone is not a universal hard error.
        var active = report.Mods.Where(mod => mod.Enabled && !mod.IsNested && !mod.IsMetadataOnly && !mod.IsGeneratedLibrary &&
            !mod.IsCandidate && !mod.IncompatibleLoader && mod.Side != "server").ToArray();
        foreach (var group in active.SelectMany(mod => mod.Provides.Keys.Select(id => (Id: id, Mod: mod)))
            .Concat(active.Select(mod => (Id: mod.Id, Mod: mod))).GroupBy(pair => pair.Id, StringComparer.Ordinal)) {
            var providers = group.Select(pair => pair.Mod).GroupBy(mod => mod.RootPath, paths).Select(owner => owner.First()).ToArray();
            if (providers.Length < 2 || providers.All(mod => mod.Id == group.Key)) continue;
            result.HasUnknown = true;
            result.UnverifiedFiles.AddRange(providers.Select(mod => mod.RootPath));
            result.Warnings.Add("提供别名 " + group.Key + " 存在多个顶层提供者（" + string.Join("、", providers.Select(mod => Path.GetFileName(mod.RootPath))) + "），需按实际加载器确认，未将其视作同一作者项目。");
        }
        result.Errors = result.Errors.Where(text => !string.IsNullOrWhiteSpace(text)).Distinct(StringComparer.Ordinal).ToList();
        result.Warnings = result.Warnings.Where(text => !string.IsNullOrWhiteSpace(text)).Distinct(StringComparer.Ordinal).ToList();
        result.HasErrors = result.Errors.Count > 0;
        if (result.HasUnknown && result.Warnings.Count == 0) result.Warnings.Add("最终模组组合中存在无法确认的必需依赖或元数据，需进一步核对。");
        return result;
    }
}
