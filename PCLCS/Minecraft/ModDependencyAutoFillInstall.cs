using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace PCLCS;

public sealed partial class ModDependencyAutoFillEngine<TContext, TMod, TDependency, TEdge, TIssue, TImpact, TReport, TOnlineProject, TOnlineDependency, TOnlineFile, TVersion, TNetFile, TItem, TPlan, TActivation, TOptions, TCache> {
    public DependencyDownloadBatch<TNetFile> CreateDownloadTask(TPlan plan, List<TNetFile> files) {
        DependencyDownloadBatch<TNetFile> batch = new();
        for (int index = 0; index < files.Count; index++) batch.Steps.Add(new DependencyDownloadStep<TNetFile> {
            Name = "下载 " + plan.Items[index].Name + " " + host.Display(plan.Items[index].Version), File = files[index], Block = false,
            ProgressWeight = Math.Max(1, host.Size(plan.Items[index].Version) / 1024.0 / 1024.0)
        });
        batch.Name = "补全模组依赖：" + (plan.Caption ?? new DirectoryInfo(plan.Root).Parent!.Name);
        return batch;
    }
    private string StagedFile(TPlan plan, string stage, int index) => Path.Combine(stage, index + "-" + host.FileName(plan.Items[index].Version));
    public int Install(TPlan plan, CancellationToken token, Action<List<TNetFile>, CancellationToken>? download = null) {
        for (int attempt = 0; attempt <= 15; attempt++) {
            token.ThrowIfCancellationRequested();
            try { return InstallAttempt(plan, token, download); }
            catch (DependencyCandidateMismatchException error) {
                if (error.Index < 0 || error.Index >= plan.Items.Count) throw;
                TItem entry = plan.Items[error.Index];
                TItem? nextVersion = null;
                string requirements = string.Join("|", Required(entry.Raw, entry.Platform, "").Select(value => Key(value.Platform, value.Project) + ":" + value.Version).OrderBy(value => value, StringComparer.Ordinal));
                while (entry.Alternatives.Count > 0) {
                    TItem candidate = entry.Alternatives[0]; entry.Alternatives.RemoveAt(0);
                    string otherRequirements = string.Join("|", Required(candidate.Raw, candidate.Platform, "").Select(value => Key(value.Platform, value.Project) + ":" + value.Version).OrderBy(value => value, StringComparer.Ordinal));
                    if (!SameText(otherRequirements, requirements)) continue;
                    nextVersion = candidate; break;
                }
                if (nextVersion is null) throw new IOException(error.Message + "\r\n没有找到其他可验证的兼容候选。", error);
                host.Info("前置候选不兼容，尝试较早版本：" + entry.Name + " " + host.Display(nextVersion.Version));
                entry.Version = nextVersion.Version; entry.Raw = nextVersion.Raw;
            }
        }
        throw new IOException("兼容版本尝试次数已达上限，未自动安装。");
    }
    private int CandidateIndex(TPlan plan, string stage, IEnumerable<string> paths) {
        for (int index = 0; index < plan.Items.Count; index++) {
            string candidatePath = StagedFile(plan, stage, index);
            if (paths.Any(value => SameFilePath(value, candidatePath))) return index;
        }
        return -1;
    }
    private int InstallAttempt(TPlan plan, CancellationToken token, Action<List<TNetFile>, CancellationToken>? download) {
        token.ThrowIfCancellationRequested();
        if (plan.Items.Count == 0 && plan.Enables.Count == 0) return 0;
        TReport before = Inspect(plan.Root, plan.Report.Context, token);
        if (!SameText(Fingerprint(before), plan.Stamp)) throw new IOException("模组文件已变化，请重新启动以检查依赖。");
        string stage = Path.Combine(plan.Root, ".pcl-import-" + Guid.NewGuid().ToString("N"));
        List<Tuple<string, string?>> created = new();
        List<TActivation> enabled = new();
        host.EnsureWithin(plan.Root, stage); Directory.CreateDirectory(stage);
        try {
            List<TNetFile> files = new();
            for (int index = 0; index < plan.Items.Count; index++) {
                TItem entry = plan.Items[index];
                if (!Compatible(entry.Version, before.Context)) throw new IOException("前置文件的版本或下载信息已经变化。");
                files.Add(host.ToNetFile(entry.Version, StagedFile(plan, stage, index), before.Context.MinecraftVersion, Loader(before.Context)));
            }
            download ??= (downloads, cancellation) => host.Download(plan, downloads, cancellation);
            if (files.Count > 0) download(files, token);
            for (int index = 0; index < plan.Items.Count; index++) {
                token.ThrowIfCancellationRequested();
                TItem entry = plan.Items[index]; string fullPath = StagedFile(plan, stage, index);
                if (!File.Exists(fullPath) || new FileInfo(fullPath).Length != host.Size(entry.Version) || !SameText(host.FileHash(fullPath, host.Hash(entry.Version)!.Length == 32), host.Hash(entry.Version)))
                    throw new IOException("下载的前置文件校验失败，未加入模组目录。");
            }
            TReport added = Inspect(stage, before.Context, token);
            if (added.MetadataIncomplete) throw new IOException("前置文件的元数据未能完整识别，未自动安装。");
            TReport ready = WithEnabledFiles(before, plan.Enables.Select(item => item.Source), token);
            HashSet<string> existingIds = new(ready.Mods.Where(value => value.Enabled && !value.IsNested).Select(value => value.Id), StringComparer.Ordinal);
            Dictionary<string, string> addedIds = new(StringComparer.Ordinal);
            List<int> chosen = new();
            for (int index = 0; index < plan.Items.Count; index++) {
                string fullPath = StagedFile(plan, stage, index);
                List<TMod> mods = added.Mods.Where(value => SameFilePath(value.RootPath, fullPath) && !value.IsNested).ToList();
                if (mods.Count == 0) throw new IOException("下载文件没有可识别的模组声明。");
                if (!mods.Any(value => !SameText(value.Side, "server"))) throw new IOException("下载的前置仅支持服务端，未加入客户端模组目录。");
                if (mods.Any(value => existingIds.Contains(value.Id))) {
                    string hash = host.FileHash(fullPath, false);
                    if (mods.Any(value => addedIds.ContainsKey(value.Id) && !SameText(addedIds[value.Id], hash))) throw new IOException("多个前置文件声明同一个模组 ID，未自动安装。");
                    continue;
                }
                chosen.Add(index);
                foreach (TMod value in mods) { existingIds.Add(value.Id); addedIds[value.Id] = host.FileHash(fullPath, false); }
            }
            TReport combined = new() { Context = before.Context, Builtins = before.Builtins, MetadataIncomplete = before.MetadataIncomplete };
            combined.Mods.AddRange(ready.Mods);
            combined.Mods.AddRange(added.Mods.Where(value => chosen.Any(index => SameFilePath(value.RootPath, StagedFile(plan, stage, index)))));
            BuildReport(combined, token);
            // Keep LF's existing Windows path test; this port adds no cross-platform policy.
            List<TEdge> unmet = combined.Edges.Where(value => value.Owner.RootPath.StartsWith(stage + "\\", StringComparison.OrdinalIgnoreCase) && value.Evaluated &&
                value.Requirement.KindValue == ModDependencyKind.Required && value.StateValue != ModDependencyMatchState.Satisfied).ToList();
            unmet.AddRange(combined.Edges.Where(value => plan.Enables.Any(item => SameFilePath(item.Source, value.Owner.RootPath)) && value.Evaluated && value.Requirement.KindValue == ModDependencyKind.Required && value.StateValue != ModDependencyMatchState.Satisfied));
            if (unmet.Count > 0) throw host.CandidateMismatch(CandidateIndex(plan, stage, unmet.Select(value => value.Owner.RootPath)), "前置文件仍有未满足或无法确认的必需依赖，未自动安装：\r\n" +
                string.Join("\r\n", unmet.Take(5).Select(value => value.Owner.Display + " 需要 " + value.Requirement.Display)));
            HashSet<string> previousIssues = new(before.Issues.Select(value => value.KindValue + "|" + value.Message), StringComparer.Ordinal);
            List<TIssue> introduced = combined.Issues.Where(value => (value.IsError || (value.KindValue == ModDependencyIssueKind.MissingRequired && value.Owner.RootPath.StartsWith(stage + "\\", StringComparison.OrdinalIgnoreCase))) &&
                !previousIssues.Contains(value.KindValue + "|" + value.Message)).ToList();
            if (introduced.Count > 0) throw host.CandidateMismatch(CandidateIndex(plan, stage, introduced.Select(value => value.Owner.RootPath).Concat(introduced.SelectMany(value => value.RelatedMods).Select(value => value.RootPath))),
                "前置文件会引入依赖缺失、版本不匹配或冲突，未自动安装：\r\n" + string.Join("\r\n", introduced.Take(5).Select(value => value.Message)));
            TReport current = Inspect(plan.Root, before.Context, token);
            if (!SameText(Fingerprint(current), plan.Stamp)) throw new IOException("下载期间模组文件发生变化，已停止自动安装。");
            foreach (TActivation entry in plan.Enables) {
                host.EnsureWithin(plan.Root, entry.Source); host.EnsureWithin(plan.Root, entry.Destination);
                if (File.Exists(entry.Destination) || Directory.Exists(entry.Destination) || !SameText(host.FileHash(entry.Source, false), entry.Sha1)) throw new IOException("已禁用的前置或目标文件已变化，未自动启用。");
            }
            foreach (TActivation entry in plan.Enables) {
                token.ThrowIfCancellationRequested(); File.Move(entry.Source, entry.Destination); enabled.Add(entry);
            }
            foreach (int index in chosen) {
                token.ThrowIfCancellationRequested();
                TItem entry = plan.Items[index]; string destination = Path.Combine(plan.Root, host.FileName(entry.Version)!);
                host.EnsureWithin(plan.Root, destination);
                if (File.Exists(destination) || Directory.Exists(destination)) throw new IOException("已有同名文件，未覆盖：" + host.FileName(entry.Version));
                File.Move(StagedFile(plan, stage, index), destination); created.Add(Tuple.Create(destination, host.Hash(entry.Version)));
            }
            token.ThrowIfCancellationRequested();
            return checked(created.Count + enabled.Count);
        } catch {
            foreach (Tuple<string, string?> entry in created)
                if (File.Exists(entry.Item1) && SameText(host.FileHash(entry.Item1, entry.Item2!.Length == 32), entry.Item2)) File.Delete(entry.Item1);
            foreach (TActivation entry in enabled.AsEnumerable().Reverse())
                if (!File.Exists(entry.Source) && File.Exists(entry.Destination) && SameText(host.FileHash(entry.Destination, false), entry.Sha1)) File.Move(entry.Destination, entry.Source);
            throw;
        } finally {
            try { host.DeleteStage(plan.Root, stage); }
            catch (IOException error) { host.Warn(error, "前置下载临时文件暂时被占用，已保留在 " + stage); }
        }
    }
}
