using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using MeloongCore;
using Newtonsoft.Json.Linq;

namespace PCLCS;

public sealed partial class PackUpdateEngine<TFile, TPackage, TChange, TPlan, TNetFile>
    where TFile : PackUpdateFileData where TPackage : PackUpdatePackageData<TFile>
    where TChange : PackUpdateChangeData<TFile> where TPlan : PackUpdatePlanData<TPackage, TChange> {
    // The complete original transaction boundary: persist backups/log before writing,
    // keep the current before/after guards and reverse rollback under the caller's real workload lock.
    public string Commit(TPlan plan, string versionFolder, CancellationToken token, Dictionary<string, string>? extraFiles = null, Action<int>? beforeApply = null) {
        token.ThrowIfCancellationRequested();
        ValidateChoices(plan, token);
        List<Tuple<string, string?, string?>> operations = new();
        foreach (TChange change in plan.Changes.Where(item => item.ApplyAuthor)) {
            string? local = change.LocalRelative;
            if (local is not null && !Same(Digest(Target(plan.Root!, local)), change.Before)) throw new IOException("文件已被其他程序修改，请重新检查更新：" + local);
            if (change.Incoming is null) {
                if (local is not null) operations.Add(Tuple.Create(Target(plan.Root!, local), (string?)null, change.Before));
                continue;
            }
            string fileName = Target(plan.Root!, change.DestinationRelative);
            if (!Same(Digest(fileName), change.TargetBefore)) throw new IOException("文件已被其他程序修改，请重新检查更新：" + change.DestinationRelative);
            operations.Add(Tuple.Create(fileName, change.Incoming.Payload, change.TargetBefore));
            if (local is not null && !local.Equals(change.DestinationRelative, StringComparison.OrdinalIgnoreCase))
                operations.Add(Tuple.Create(Target(plan.Root!, local), (string?)null, change.Before));
        }
        if (extraFiles is not null) foreach (var pair in extraFiles)
            operations.Add(Tuple.Create(Target(versionFolder, pair.Key), (string?)pair.Value, Digest(Target(versionFolder, pair.Key))));
        string box = Target(versionFolder, "PCL/PackUpdateBackups/" + Guid.NewGuid().ToString("N") + "/transaction.json");
        Directory.CreateDirectory(Path.GetDirectoryName(box)!);
        string statePayload = Path.Combine(Path.GetDirectoryName(box)!, "next-manifest.json");
        JObject nextManifest = BuildNextManifest(plan, token);
        File.WriteAllText(statePayload, nextManifest.ToString());
        string statePath = Target(versionFolder, ManifestName);
        operations.Add(Tuple.Create(statePath, (string?)statePayload, Digest(statePath)));
        return ApplyTransaction(plan.Root!, versionFolder, operations, token, beforeApply, box);
    }
    public string ApplyTransaction(string gameRoot, string versionFolder, List<Tuple<string, string?, string?>> operations, CancellationToken token, Action<int>? beforeApply = null, string? box = null) =>
        ApplyTransactionWithProgress(gameRoot, versionFolder, operations, token, null, beforeApply, box);

    public string ApplyTransactionWithProgress(string gameRoot, string versionFolder, List<Tuple<string, string?, string?>> operations, CancellationToken token, Action<double>? progress, Action<int>? beforeApply = null, string? box = null) {
        token.ThrowIfCancellationRequested();
        if (operations.GroupBy(op => Path.GetFullPath(op.Item1), StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
            throw new IOException("更新存在重复的目标操作，请先确认文件关联，未修改当前实例。");
        if (box is null) box = Target(versionFolder, "PCL/PackUpdateBackups/" + Guid.NewGuid().ToString("N") + "/transaction.json");
        Directory.CreateDirectory(Path.GetDirectoryName(box)!);
        int checkedCount = 0, count = Math.Max(1, operations.Count);
        foreach (var op in operations) {
            if (PathUtils.IsParentOf(versionFolder, op.Item1)) host.EnsureWithin(versionFolder, op.Item1);
            else host.EnsureWithin(gameRoot, op.Item1);
            if (!Same(FileTaskProgress.Digest(op.Item1, token, FileTaskProgress.Slice(progress, 0.15 * checkedCount / count, 0.15 * checked(checkedCount + 1) / count)), op.Item3)) throw new IOException("文件已变化，请重新检查环境。");
            checkedCount = checked(checkedCount + 1);
        }
        JObject log = new(new JProperty("complete", false), new JProperty("gameRoot", Path.GetFullPath(gameRoot)), new JProperty("files", new JArray()));
        JArray logFiles = (JArray)log["files"]!;
        for (int index = 0; index < operations.Count; index++) {
            token.ThrowIfCancellationRequested();
            var op = operations[index]; string backup = Path.Combine(Path.GetDirectoryName(box)!, index + ".bak");
            if (op.Item3 is not null) FileTaskProgress.Copy(op.Item1, backup, token, FileTaskProgress.Slice(progress, 0.15 + 0.3 * index / count, 0.15 + 0.3 * checked(index + 1) / count));
            logFiles.Add(new JObject(new JProperty("target", op.Item1), new JProperty("backup", op.Item3 is null ? null : backup), new JProperty("before", op.Item3), new JProperty("after", op.Item2 is null ? null : Digest(op.Item2))));
        }
        token.ThrowIfCancellationRequested();
        File.WriteAllText(box, log.ToString());
        List<int> applied = new();
        try {
            for (int index = 0; index < operations.Count; index++) {
                beforeApply?.Invoke(index); token.ThrowIfCancellationRequested();
                var op = operations[index];
                Action<double>? fileProgress = FileTaskProgress.Slice(progress, 0.45 + 0.54 * index / count, 0.45 + 0.54 * checked(index + 1) / count);
                if (!Same(FileTaskProgress.Digest(op.Item1, token, FileTaskProgress.Slice(fileProgress, 0, 0.5)), op.Item3)) throw new IOException("提交期间文件发生变化，已停止更新。");
                applied.Add(index);
                if (op.Item2 is null) File.Delete(op.Item1); else ReplaceFromWithProgress(op.Item2, op.Item1, token, FileTaskProgress.Slice(fileProgress, 0.5, 1));
                fileProgress?.Invoke(1);
            }
            token.ThrowIfCancellationRequested();
            log["complete"] = true;
            host.AtomicWrite(box, Encoding.UTF8.GetBytes(log.ToString()));
            progress?.Invoke(1);
        } catch {
            foreach (int index in applied.AsEnumerable().Reverse()) {
                JToken item = logFiles[index]; string destination = item["target"]!.ToString();
                if (item["backup"]!.Type == JTokenType.Null) {
                    if (Same(Digest(destination), item["after"]?.ToString())) File.Delete(destination);
                } else ReplaceFrom(item["backup"]!.ToString(), destination);
            }
            log["complete"] = true; log["rolledBack"] = true;
            File.WriteAllText(box, log.ToString());
            throw;
        }
        return Path.GetDirectoryName(box)!;
    }
    public static void ReplaceFrom(string source, string destination) => ReplaceFromWithProgress(source, destination, CancellationToken.None, null);

    public static void ReplaceFromWithProgress(string source, string destination, CancellationToken token, Action<double>? progress) {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        string temporary = destination + ".pcl-update-" + Guid.NewGuid().ToString("N");
        try {
            FileTaskProgress.Copy(source, temporary, token, progress);
            if (File.Exists(destination)) File.Replace(temporary, destination, null); else File.Move(temporary, destination);
        } finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    public List<string> Pending(string versionFolder) {
        string folder = Path.Combine(versionFolder, "PCL", "PackUpdateBackups");
        host.EnsureWithin(versionFolder, folder);
        List<string> result = new();
        if (!Directory.Exists(folder)) return result;
        foreach (string child in Directory.GetDirectories(folder)) {
            host.EnsureWithin(versionFolder, child);
            string logFile = Path.Combine(child, "transaction.json");
            host.EnsureWithin(versionFolder, logFile);
            if (File.Exists(logFile) && new FileInfo(logFile).Length <= 16 * 1024 * 1024 && !(bool)JObject.Parse(File.ReadAllText(logFile))["complete"]!) result.Add(logFile);
        }
        return result;
    }
    public void Recover(string versionFolder, string gameRoot) {
        foreach (string logFile in Pending(versionFolder)) {
            JObject log = JObject.Parse(File.ReadAllText(logFile)); JArray items = (JArray)log["files"]!;
            if (!PathUtils.IsSame(log["gameRoot"]!.ToString(), gameRoot)) throw new IOException("更新备份对应的游戏目录已经变化，请手动检查备份。");
            foreach (JToken item in items) {
                string destination = item["target"]!.ToString();
                if (PathUtils.IsParentOf(versionFolder, destination)) host.EnsureWithin(versionFolder, destination); else host.EnsureWithin(gameRoot, destination);
                string? current = Digest(destination), before = item["before"]!.Type == JTokenType.Null ? null : item["before"]!.ToString(), after = item["after"]!.Type == JTokenType.Null ? null : item["after"]!.ToString();
                if (!Same(current, before) && !Same(current, after)) throw new IOException("中断后文件又发生变化，已保留现场：" + destination);
                if (item["backup"]!.Type != JTokenType.Null) {
                    string backup = item["backup"]!.ToString();
                    host.EnsureWithin(Path.GetDirectoryName(logFile)!, backup);
                    if (!Same(Digest(backup), before)) throw new IOException("更新备份校验失败。");
                }
            }
            foreach (JToken item in items.Reverse()) {
                string destination = item["target"]!.ToString();
                if (item["backup"]!.Type == JTokenType.Null) { if (File.Exists(destination)) File.Delete(destination); }
                else ReplaceFrom(item["backup"]!.ToString(), destination);
            }
            log["complete"] = true; log["rolledBack"] = true;
            host.AtomicWrite(logFile, Encoding.UTF8.GetBytes(log.ToString()));
        }
    }
}
