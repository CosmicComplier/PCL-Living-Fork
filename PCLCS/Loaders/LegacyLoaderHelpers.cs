using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using MeloongCore;
using Microsoft.VisualBasic.CompilerServices;

namespace PCLCS.Loaders;

/// <summary>Taskbar uses the real legacy task collection, progress and shell state through one view.</summary>
public sealed class LegacyLoaderTaskbar<TLoader, TState, TLoadingState>
    where TLoader : LegacyLoaderBase<TLoader, TState, TLoadingState> {
    private readonly LegacyLoaderHost<TLoader, TState, TLoadingState> host;
    private readonly LegacyLoaderTaskbarHost<TLoader> view;
    public LegacyLoaderTaskbar(LegacyLoaderHost<TLoader, TState, TLoadingState> host, LegacyLoaderTaskbarHost<TLoader> view) { this.host = host; this.view = view; }
    public void Add(TLoader Loader) {
        view.RemoveTaskFromSpeedPage(Loader);
        view.ReadTasks().Add(Loader);
        Logger.Info($"{Loader.Name} 已加入任务列表");
    }
    public double GetProgress() {
        try {
            if (!view.ReadTasks().Any()) return 1;
            return MathUtils.Clamp(view.ReadTasks().Select(loader => loader.Progress).Average(), 0d, 1d);
        } catch (Exception error) { Logger.Error(error, "获取任务栏进度出错"); return 0.5; }
    }
    public void Refresh() {
        try {
            double next = GetProgress();
            foreach (TLoader task in view.ReadTasks()) {
                if (view.ReadTasks().All(loader => !loader.IsState(loader.State, host.Loading)) ||
                    task.IsState(task.State, host.Waiting) || task.IsState(task.State, host.Canceled)) {
                    view.RefreshTaskOnSpeedPage(task);
                    view.ReadTasks().Remove(task);
                    Logger.Info($"{task.Name} 已移出任务列表");
                }
            }
            view.WriteProgress(next <= 0 || next >= 1 || view.ReadProgress() > next ? next : view.ReadProgress() * 0.9 + next * 0.1);
            host.RunInUi(() => view.SetExtraDownloadProgress(view.ReadProgress()));
            int state;
            // WPF TaskbarItemProgressState: None = 0, Indeterminate = 1, Normal = 2.
            if (!view.ReadTasks().Any() || view.ReadProgress() == 1) state = 0;
            else if (view.ReadProgress() < 0.015) state = 1;
            else { state = 2; view.SetTaskbarValue(view.ReadProgress()); }
            if (view.ReadLastState() != state) {
                view.WriteLastState(state); view.SetTaskbarState(state); host.RefreshExtraDownload();
            }
        } catch (Exception error) { Logger.Error(error, "刷新任务栏进度显示失败"); }
    }
}

public sealed class LegacyLoaderFolders<TLoader, TState, TLoadingState>
    where TLoader : LegacyLoaderBase<TLoader, TState, TLoadingState> {
    private readonly Dictionary<TLoader, Stamp> stamps = new();
    private struct Stamp {
        internal DateTime? LastCheckTime;
        internal string? FolderPath;
        public override bool Equals(object? obj) => obj is Stamp other &&
            EqualityComparer<DateTime?>.Default.Equals(LastCheckTime, other.LastCheckTime) &&
            Operators.CompareString(FolderPath, other.FolderPath, false) == 0;
        public override int GetHashCode() => base.GetHashCode();
    }
    public bool Run(TLoader Loader, string? FolderPath, LegacyLoaderFolderRunType Type, int MaxDepth = 0, string? ExtraPath = "", bool WaitForExit = false) {
        Stamp value = new() { FolderPath = FolderPath + ExtraPath, LastCheckTime = null };
        try {
            DirectoryInfo info = DirectoryUtils.GetInfo(FolderPath + ExtraPath);
            value.LastCheckTime = info.Exists ? GetActualLastWriteTimeUtc(info, MaxDepth) : null;
            if (Type == LegacyLoaderFolderRunType.RunOnUpdated && stamps.ContainsKey(Loader)) {
                if (info.Exists) {
                    if (stamps[Loader].LastCheckTime is not null && value.Equals(stamps[Loader])) return false;
                } else if (stamps[Loader].LastCheckTime is null) return false;
            }
        } catch (Exception error) { Logger.Warn(error, "文件夹加载器启动检测出错"); }
        stamps[Loader] = value;
        if (Type == LegacyLoaderFolderRunType.UpdateOnly) return false;
        if (WaitForExit) Loader.WaitForExit(FolderPath, IsForceRestart: true);
        else Loader.Start(FolderPath, IsForceRestart: true);
        return true;
    }
    private static DateTime GetActualLastWriteTimeUtc(DirectoryInfo FolderInfo, int MaxDepth) {
        DateTime time = FolderInfo.LastWriteTimeUtc;
        if (MaxDepth > 0) foreach (DirectoryInfo folder in FolderInfo.EnumerateDirectories()) {
            DateTime childTime = GetActualLastWriteTimeUtc(folder, checked(MaxDepth - 1));
            if (childTime > time) time = childTime;
        }
        return time;
    }
}

/// <summary>The file lease lasts through the actual workload's finally/rollback, including cancellation.</summary>
public static class LegacyLoaderFileOperation {
    public static Action CreateWorkload<TLoader, TState, TLoadingState>(TLoader Loader, string? Input, ProgressProvider? Progress = null)
        where TLoader : LegacyLoaderBase<TLoader, TState, TLoadingState> {
        object gate = new(); int running = 0; bool closed = false;
        Loader.GuardWorkload(work => () => {
            lock (gate) {
                if (closed) throw new OperationCanceledException();
                running = checked(running + 1);
            }
            try { work(); }
            finally {
                lock (gate) { running = checked(running - 1); Monitor.PulseAll(gate); }
            }
        });
        return () => {
            try {
                lock (Loader.LockState) {
                    if (Loader.IsState(Loader.State, Loader.Host.Canceled)) throw new OperationCanceledException();
                    Loader.Start(Input);
                }
                Loader.WaitForExit(StartIfNeeded: false, p: Progress);
                Progress?.Set(1);
            } finally {
                lock (gate) {
                    closed = true;
                    while (running != 0) Monitor.Wait(gate);
                }
            }
        };
    }
}
