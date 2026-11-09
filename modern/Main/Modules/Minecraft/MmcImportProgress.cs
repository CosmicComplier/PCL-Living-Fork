using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MeloongCore;
using PCLCS;

namespace PCL;

// Original LF host presentation; repository LICENCE applies. These are observations of
// one genuine import producer, never schedulable children or portable Core state.
internal enum MmcImportStage { ReadArchive, ResolveComponents, SelectJava, ExtractPackage, PrepareRuntime, InstallPackage, PublishInstance }
internal enum MmcImportStageState { Waiting, Running, Finished, Skipped, Cancelling, Canceled, Failed, NotRun }
internal sealed record MmcImportStageSnapshot(MmcImportStage Stage, string Name, MmcImportStageState State,
    string Detail, double? Progress, long CompletedBytes, long TotalBytes);

internal interface IMmcImportProgressOperation {
    MmcImportProgress ImportProgress { get; }
    bool ProducerCompleted { get; }
}

internal sealed class MmcImportProgress {
    private sealed class Stage(string name, double weight) {
        internal readonly string Name = name;
        internal readonly double Weight = weight;
        internal MmcImportStageState State;
        internal string Detail;
        internal double Fraction;
        internal double? Measured;
        internal long Completed, Total;
    }
    private readonly object gate = new();
    private readonly Stage[] stages = [new("读取并核对整合包", .12), new("解析整合包组件", .05), new("选择整合包 Java", .03),
        new("解压整合包文件", .35), new("准备游戏运行环境", .20), new("安装整合包文件", .10), new("保存 MMC 实例环境", .15)];
    private int current = -1, downloadBatch, cancellationRequested;
    private int? revisiting;
    private long packageBytes;
    private bool decisionStarted, retryValidation;
    private Action<double, string> updateWorker;

    internal bool CancellationRequested => Volatile.Read(ref cancellationRequested) != 0;
    internal bool DecisionStarted { get { lock (gate) return decisionStarted; } }
    internal double Fraction { get { lock (gate) return FractionUnderGate(); } }
    private double FractionUnderGate() => Math.Min(.999, stages.Sum(stage => stage.Weight * stage.Fraction));

    internal void Attach<T>(ModLoader.LoaderTask<string, T> worker) {
        lock (gate) { updateWorker = (value, name) => { worker.Progress = value; worker.Name = name; }; PublishUnderGate(); }
    }

    internal void Begin(MmcImportStage stage, string detail = null) {
        lock (gate) { BeginUnderGate(stage, detail); PublishUnderGate(); }
    }
    private bool BeginUnderGate(MmcImportStage stage, string detail = null) {
        int index = (int)stage;
        // The backend rechecks metadata after the Main Java probe; repeated earlier
        // boundaries must neither restart completed rows nor complete later work.
        if (index < current) return false;
        if (index > current) {
            FinishRevisitUnderGate();
            if (current >= 0) { stages[current].State = MmcImportStageState.Finished; stages[current].Fraction = 1; }
            for (int skipped = current + 1; skipped < index; skipped++) {
                stages[skipped].State = MmcImportStageState.Skipped;
                stages[skipped].Detail = "本次操作未单独执行此步骤";
                stages[skipped].Fraction = 1;
            }
            current = index;
            stages[current].State = MmcImportStageState.Running;
        }
        if (detail is not null) stages[current].Detail = detail;
        return true;
    }

    internal void Archive(MmcArchiveProgress value) {
        lock (gate) {
            if (!BeginUnderGate(MmcImportStage.ReadArchive, value.Phase == "package-hash" ? "核对原包 SHA256" : "读取整合包配置")) return;
            var stage = stages[current]; stage.Completed = value.CompletedBytes; stage.Total = value.TotalBytes;
            stage.Measured = Ratio(value.CompletedBytes, value.TotalBytes);
            if (stage.Measured is { } measured) stage.Fraction = Math.Max(stage.Fraction,
                value.Phase == "package-hash" ? .85 * measured : .85 + .14 * measured);
            PublishUnderGate();
        }
    }
    internal void SourceRead(MmcArchiveSource source) {
        var metadata = source.Metadata.Select(value => source.Package.RelativeRoot + value.RelativePath).ToHashSet(StringComparer.OrdinalIgnoreCase);
        lock (gate) packageBytes = source.ArchiveEntries.Where(value => !value.IsDirectory && !metadata.Contains(value.Path)).Sum(value => value.SizeBytes);
    }
    internal void RecheckSource() {
        lock (gate) {
            // RetryInitialImportAsync revalidates its durable preview and opens /
            // hashes the original again. Show that genuine second pass explicitly;
            // its API has no byte events, so do not keep the finished Java row busy
            // or reuse the previous archive pass's 100% measurement.
            if (current >= 0) { stages[current].State = MmcImportStageState.Finished; stages[current].Fraction = 1; }
            retryValidation = true;
            RevisitUnderGate(MmcImportStage.ReadArchive, "重新核对原包与导入记录");
            PublishUnderGate();
        }
    }
    private void RevisitUnderGate(MmcImportStage stage, string detail) {
        FinishRevisitUnderGate(); revisiting = (int)stage;
        var value = stages[revisiting.Value]; value.State = MmcImportStageState.Running; value.Detail = detail;
        value.Measured = null; value.Completed = value.Total = 0;
    }
    private void FinishRevisitUnderGate() {
        if (revisiting is not { } index) return;
        stages[index].State = MmcImportStageState.Finished; stages[index].Fraction = 1; revisiting = null;
    }

    internal void Backend(MmcInstanceBackendProgress value) {
        lock (gate) {
            switch (value.Phase) {
                case "package-hash": case "metadata":
                    Archive(new(value.Phase, value.CompletedBytes, value.TotalBytes)); return;
                case "metadata-resolution":
                    if (retryValidation) { retryValidation = false; RevisitUnderGate(MmcImportStage.ResolveComponents, "核对原包组件与运行环境"); }
                    else BeginUnderGate(MmcImportStage.ResolveComponents);
                    break;
                case "package-staging": BeginUnderGate(MmcImportStage.ExtractPackage, "解压并校验原包文件"); break;
                case "package-extract":
                    if (BeginUnderGate(MmcImportStage.ExtractPackage, "解压并校验原包文件")) BytesUnderGate(value.CompletedBytes, packageBytes);
                    break;
                case "preparing-runtime": BeginUnderGate(MmcImportStage.PrepareRuntime, "检查游戏运行文件与缓存"); break;
                case "materializing-runtime":
                    if (BeginUnderGate(MmcImportStage.PrepareRuntime, "整理游戏运行文件")) {
                        var stage = stages[current]; stage.Fraction = Math.Max(stage.Fraction, .5); stage.Measured = null; stage.Completed = stage.Total = 0;
                    }
                    break;
                case "copy": case "native-extract":
                    if (BeginUnderGate(MmcImportStage.PrepareRuntime, value.Phase == "native-extract" ? "解压本地运行库" : "整理游戏运行文件")) {
                        stages[current].Completed = value.CompletedBytes; stages[current].Total = 0; stages[current].Measured = null;
                    }
                    break;
                case "installing-package": BeginUnderGate(MmcImportStage.InstallPackage, "核对并放置整合包文件"); break;
                case "private-payload-install":
                    if (BeginUnderGate(MmcImportStage.InstallPackage, "核对并放置整合包文件")) BytesUnderGate(value.CompletedBytes, value.TotalBytes);
                    break;
                case "publishing-instance": BeginUnderGate(MmcImportStage.PublishInstance, "保存实例配置"); break;
                case "publication-sharing-wait": BeginUnderGate(MmcImportStage.PublishInstance, "等待文件占用释放"); break;
                case "private-import-moved": BeginUnderGate(MmcImportStage.PublishInstance, "完成实例发布"); break;
                case "private-payload-verify":
                    if (BeginUnderGate(MmcImportStage.PublishInstance, "核对已保存的整合包文件")) BytesUnderGate(value.CompletedBytes, value.TotalBytes, .8);
                    break;
                case "private-import-marker-cleanup": BeginUnderGate(MmcImportStage.PublishInstance, "完成实例发布"); break;
                case "completed":
                    if (BeginUnderGate(MmcImportStage.PublishInstance, "刷新版本列表")) {
                        stages[current].Fraction = Math.Max(stages[current].Fraction, .99); stages[current].Measured = null;
                        stages[current].Completed = stages[current].Total = 0;
                    }
                    break;
                default: return;
            }
            PublishUnderGate();
        }
    }
    private void BytesUnderGate(long completed, long total, double weight = 1) {
        var stage = stages[current]; stage.Completed = completed; stage.Total = total; stage.Measured = Ratio(completed, total);
        if (stage.Measured is { } value) stage.Fraction = Math.Max(stage.Fraction, Math.Min(.99, value * weight));
    }
    private static double? Ratio(long completed, long total) => total > 0 ? Math.Clamp((double)completed / total, 0, 1) : null;
    private void PublishUnderGate() { if (current >= 0) updateWorker?.Invoke(FractionUnderGate(), stages[revisiting ?? current].Name); }

    internal async Task DownloadAsync(ImmutableArray<ModNet.NetFile> files, CancellationToken token, Action<ModNet.LoaderDownload> created) {
        if (files.IsEmpty) { token.ThrowIfCancellationRequested(); return; }
        int batch;
        lock (gate) {
            BeginUnderGate(MmcImportStage.PrepareRuntime); batch = ++downloadBatch;
            stages[current].Detail = "下载游戏运行文件（第 " + batch + " 批）"; stages[current].Measured = 0;
            stages[current].Completed = stages[current].Total = 0; PublishUnderGate();
        }
        var progress = new ProgressProvider();
        progress.ProgressChanged += _ => {
            var raw = progress.GetRaw();
            lock (gate) {
                if (current != (int)MmcImportStage.PrepareRuntime || batch != downloadBatch) return;
                double measured = Math.Clamp(raw.actual + raw.skiped, 0, 1);
                stages[current].Measured = measured;
                stages[current].Fraction = Math.Max(stages[current].Fraction, .45 * measured);
                PublishUnderGate();
            }
        };
        await MmcDownloads.RunBatchAsync(files, token, progress, created).ConfigureAwait(false);
        // A subsequent batch may follow. Only materializing-runtime proves all
        // prefetch work has returned, so a completed download never finishes this row.
    }

    // Fence.Cancel calls this under its own gate. Do not acquire the presentation
    // gate there: progress listeners can synchronously ask the fence to cancel.
    internal void CancellationAccepted() => Interlocked.Exchange(ref cancellationRequested, 1);
    internal void EnteredDecision() { lock (gate) decisionStarted = true; }
    internal MmcImportStageSnapshot[] Snapshot(ModBase.LoadState rootState) {
        lock (gate) return stages.Select((stage, index) => {
            var state = stage.State;
            string detail = stage.Detail;
            int active = revisiting ?? current;
            if (rootState == ModBase.LoadState.Finished && index == active) state = MmcImportStageState.Finished;
            else if (rootState == ModBase.LoadState.Finished && state == MmcImportStageState.Waiting) {
                state = MmcImportStageState.Skipped; detail = "本次操作未单独执行此步骤";
            }
            else if (index == active) {
                if (rootState == ModBase.LoadState.Failed) state = MmcImportStageState.Failed;
                else if (rootState == ModBase.LoadState.Canceled) state = MmcImportStageState.Canceled;
                else if (CancellationRequested) { state = MmcImportStageState.Cancelling; detail = "正在取消，等待文件操作结束"; }
            } else if (rootState is ModBase.LoadState.Failed or ModBase.LoadState.Canceled && state == MmcImportStageState.Waiting) state = MmcImportStageState.NotRun;
            return new MmcImportStageSnapshot((MmcImportStage)index, stage.Name, state, detail,
                state == MmcImportStageState.Running ? stage.Measured : null, stage.Completed, stage.Total);
        }).ToArray();
    }
}

// Preserve the scheduler's concrete generic input contract and one-producer fence.
// Token cancellation is immediate; terminal Loader cancellation follows the genuine
// worker's return, so Taskbar cannot remove an operation while its I/O is draining.
internal sealed class MmcImportOperationCombo<TIn>(MmcOperationCancellationFence fence, MmcImportProgress progress,
    string name, IEnumerable<ModLoader.LoaderBase> children) : ModLoader.LoaderCombo<TIn>(name, children), IMmcImportProgressOperation {
    public MmcImportProgress ImportProgress => progress;
    public bool ProducerCompleted => fence.IsCompleted;
    public override void Cancel() {
        fence.Cancel(() => {
            if (fence.IsCompleted) base.Cancel();
            else if (fence.Token.IsCancellationRequested) progress.CancellationAccepted();
        });
        if (fence.IsCompleted) base.Cancel();
    }
}
