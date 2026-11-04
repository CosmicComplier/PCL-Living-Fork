using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace PCLCS;

public sealed partial class PackUpdateFlowEngine<TFile, TPackage, TChange, TPlan, TNetFile, TVersion, TInstance, TTask, TCombo, TWorker, TProgress>
    where TFile : PackUpdateFileData where TPackage : PackUpdatePackageData<TFile>
    where TChange : PackUpdateChangeData<TFile> where TPlan : PackUpdatePlanData<TPackage, TChange>
    where TVersion : ResourceVersionModelData where TInstance : class where TTask : class where TCombo : class where TWorker : class where TProgress : class {
    public void Execute(TInstance instance, bool online, string? archivePath, TTask task) {
        CancellationToken token = host.Cancellation(task); string stage = host.TempFolder();
        host.SetProgress(task, 0.03);
        if (update.Pending(host.VersionFolder(instance)).Any()) {
            if (host.Message("上次整合包更新被中断，需要先恢复更新前的文件。", "恢复整合包", "恢复", "取消", true) != 1) throw new OperationCanceledException();
            instance = RecoverInstance(instance);
        }
        string root = host.GameRoot(instance);
        JObject? baseline = update.ReadManifest(host.VersionFolder(instance));
        if (baseline is null) {
            if (host.Message("此版本尚无作者文件记录。请选择当前版本原始整合包，以识别作者文件和个人修改。", "建立整合包记录", "选择原始包", "取消", false) != 1) throw new OperationCanceledException();
            TPackage original = update.ReadPackage(SelectArchive("选择当前版本原始整合包"), Path.Combine(stage, "original"), token);
            if (!Same(host.Version(instance).VanillaName, original.Minecraft)) throw new IOException("原始整合包的 Minecraft 版本与当前版本不同，请选择正确的旧包。");
            baseline = PackUpdateEngine<TFile, TPackage, TChange, TPlan, TNetFile>.Manifest(original);
        }
        JObject? source = null;
        if (online) archivePath = ChooseOnline(baseline, stage, token, ref source, host.Progress(task, 0.03, 0.12));
        host.SetProgress(task, 0.12);
        TPackage package = update.ReadPackage(archivePath!, Path.Combine(stage, "next"), token);
        package.Source = source;
        TPlan plan = update.Plan(root, baseline, package);
        host.SetProgress(task, 0.15);
        update.DownloadChanges(plan, stage, token, (files, cancel) => host.Download(files, cancel, host.Progress(task, 0.15, 0.65)));
        host.SetProgress(task, 0.65);
        bool runtimeChanged = !Same(host.Version(instance).VanillaName, package.Minecraft) || host.Version(instance).ModLoaderVersions.Count != package.Loaders.Count ||
            package.Loaders.Any(pair => !host.Version(instance).ModLoaderVersions.ContainsKey(pair.Key) || !Same(host.Version(instance).ModLoaderVersions[pair.Key], pair.Value));
        string summary = package.Name + " " + package.Version + "\r\n" + $"更新 {plan.Changes.Count(item => !item.Conflict)} 项，保留个人修改 {plan.Changes.Count(item => item.Conflict)} 项。" + "\r\n" +
            (runtimeChanged ? "Minecraft 或加载器将更新；存档会保留，请确认新版本兼容。" : "");
        if (host.Preview(summary, plan, () => host.Version(instance)) != 1) throw new OperationCanceledException();
        Dictionary<string, string> extras = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string?> runtimeBefore = new(StringComparer.OrdinalIgnoreCase);
        if (runtimeChanged) {
            foreach (string relative in new[] { host.Name(instance) + ".json", host.Name(instance) + ".jar", "PCL/config.json" })
                runtimeBefore[relative] = PackUpdateEngine<TFile, TPackage, TChange, TPlan, TNetFile>.Digest(update.Target(host.VersionFolder(instance), relative));
            string buildFolder = Path.Combine(stage, "runtime", host.Name(instance)) + "\\";
            var request = new PackUpdateRuntimeRequest { Name = host.Name(instance), BuildFolder = buildFolder, Minecraft = package.Minecraft!, Loaders = package.Loaders,
                MinecraftFolder = Path.GetDirectoryName(Path.GetDirectoryName(host.VersionFolder(instance).TrimEnd('\\'))) + "\\" };
            TCombo build = host.CreateRuntime(request);
            Action prepareRuntime = host.RuntimeWorkload(build, buildFolder, host.Progress(task, 0.65, 0.85));
            using (token.Register(() => host.CancelCombo(build))) prepareRuntime();
            foreach (string extension in new[] { ".json", ".jar" }) {
                string payload = Path.Combine(buildFolder, host.Name(instance) + extension);
                if (File.Exists(payload)) extras.Add(host.Name(instance) + extension, payload);
            }
            if (!extras.ContainsKey(host.Name(instance) + ".json")) throw new IOException("新版本游戏组件未生成。");
            string existingConfig = Path.Combine(host.VersionFolder(instance), "PCL", "config.json");
            host.EnsureWithin(host.VersionFolder(instance), existingConfig);
            JObject config = File.Exists(existingConfig) ? JObject.Parse(File.ReadAllText(existingConfig)) : new JObject();
            JObject newConfig = JObject.Parse(File.ReadAllText(Path.Combine(buildFolder, "PCL", "config.json")));
            config["MinecraftInstallManifest"] = newConfig["MinecraftInstallManifest"]?.DeepClone();
            string configPayload = Path.Combine(stage, "updated-config.json");
            File.WriteAllText(configPayload, config.ToString()); extras.Add("PCL/config.json", configPayload);
        }
        token.ThrowIfCancellationRequested();
        if (host.HasRunningGame() || host.HasRunningSessions() || host.LaunchLoading()) throw new IOException("游戏已开始运行，未替换文件。");
        foreach (var pair in runtimeBefore)
            if (!Same(PackUpdateEngine<TFile, TPackage, TChange, TPlan, TNetFile>.Digest(update.Target(host.VersionFolder(instance), pair.Key)), pair.Value)) throw new IOException("游戏组件或版本设置在预览后发生变化，请重新检查更新。");
        host.SetProgress(task, 0.85);
        host.BeforeMinecraftChange(instance, host.Version(instance).VanillaName, package.Minecraft!, token);
        host.SetProgress(task, 0.87); host.CaptureHistory(instance, "整合包更新前", token);
        host.SetProgress(task, 0.9);
        int operationCount = plan.Changes.Count(item => !item.Conflict) + extras.Count + 1;
        string backup = update.Commit(plan, host.VersionFolder(instance), token, extras, index => host.SetProgress(task, 0.9 + 0.08 * index / Math.Max(1, operationCount)));
        host.SetProgress(task, 0.98); host.SetOutput(task, backup);
        ReloadInstance(instance); host.Info("整合包更新完成，原文件备份：" + backup);
    }
}
