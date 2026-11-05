using System;
using System.IO;
using System.Threading;
using MeloongCore;

namespace PCLCS;

public sealed partial class LegacyLaunchEngine<TInstance, TState, TCard, TOptions, TData, TServer, TMicrosoft, TOffline, TWatcher>
    where TInstance : LegacyMcInstance<TInstance, TState, TCard>
    where TState : struct, Enum where TCard : struct, Enum where TOptions : class, ILegacyLaunchOptions<TInstance>, new()
    where TData : class, ILegacyLaunchLoginData where TServer : TData, ILegacyLaunchServerData
    where TMicrosoft : TData, ILegacyLaunchMicrosoftData where TOffline : TData, ILegacyLaunchOfflineData where TWatcher : class {
    public void McLaunchDependencyCheck(LegacyLaunchTask<int, int> loader) {
        if (!Setting<bool>("LaunchModDependencyCheck")) return;
        if (CurrentLaunchOptions?.SaveBatch != null) return;
        if (McInstanceSelected == null || !McInstanceSelected.Modable) return;
        TInstance target = McInstanceSelected;
        string modsFolder = Path.Combine(target.PathIndie, "mods");
        LegacyLaunchDependencyContext context = host.DependencyContext(target, McLaunchJavaSelected?.Version?.ToString()!, modsFolder);
        CancellationToken token = loader.CreateCancellationToken();
        Thread checkThread = Thread.CurrentThread;
        LegacyLaunchDependencyReport report = context.Inspect(token);
        if (loader.IsCanceled || token.IsCancellationRequested) return;
        if (report.WasAccepted()) return;
        LegacyLaunchDependencyPlan plan = Setting<bool>("LaunchModDependencyAutoFill") ? report.BuildAutoFill(token) : report.BuildActivation(token);
        plan.Caption = target.Name;
        if (loader.IsCanceled || token.IsCancellationRequested) return;
        bool canRepair = plan.ItemsCount > 0 || plan.EnablesCount > 0;
        if (!canRepair && !report.HasErrors) return;
        int choice = 0;
        host.DispatchWait(() => {
            if (loader.IsCanceledWithThread(checkThread) || token.IsCancellationRequested) return;
            choice = host.ConfirmDependencies(report, plan, false);
        });
        if (loader.IsCanceled || token.IsCancellationRequested) return;
        if (choice == 2) throw new OperationCanceledException();
        if (choice == 3) { report.Accept(); return; }
        if (choice == 1 && canRepair) {
            InstallLaunchDependencies(target, plan, token);
            token.ThrowIfCancellationRequested();
            report = context.Inspect(token);
            if (!report.HasErrors) return;
            host.DispatchWait(() => { if (!token.IsCancellationRequested) choice = host.ConfirmDependencies(report, null, true); });
            token.ThrowIfCancellationRequested();
            if (choice == 2) throw new OperationCanceledException();
            if (choice == 3) { report.Accept(); return; }
        }
        host.DispatchWait(() => {
            if (loader.IsCanceledWithThread(checkThread) || token.IsCancellationRequested) return;
            host.OpenMods(target);
        });
        throw new OperationCanceledException();
    }

    public void InstallLaunchDependencies(TInstance instance, LegacyLaunchDependencyPlan plan, CancellationToken token) {
        token.ThrowIfCancellationRequested();
        if (!PathUtils.IsSame(plan.Root, Path.Combine(instance.PathIndie, "mods"))) throw new IOException("当前版本的游戏目录已变化，请重新检查依赖。");
        int added = 0; Exception? failure = null;
        RedoableWorker worker = new((CancellationToken? cancellation) => {
            try { added = plan.Install(cancellation.GetValueOrDefault()); }
            catch (Exception ex) { failure = ex; throw; }
        });
        if (!host.AcquireInstance(worker, instance.PathVersion)) throw new IOException("此版本正在被其他操作使用，请稍后重试。");
        using (ManualResetEventSlim finished = new(false)) {
            worker.Stopped += () => finished.Set();
            worker.Start(token); finished.Wait();
        }
        token.ThrowIfCancellationRequested();
        if (failure != null) throw new IOException("模组依赖补全失败：" + failure.Message, failure);
        string text = plan.EnablesCount > 0 ? "已启用 " + plan.EnablesCount + " 个前置" +
            (added > plan.EnablesCount ? "，已安装 " + (added - plan.EnablesCount) + " 个前置" : "") :
            added > 0 ? "已安装 " + added + " 个必需前置" : "所需前置已存在";
        Hint(text + "，正在复查", LegacyLaunchHint.Green);
    }
}
