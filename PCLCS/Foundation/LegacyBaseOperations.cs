using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MeloongCore;
using MeloongCore.Extensions;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using ThrottleDebounce;

namespace PCLCS;

public static partial class LegacyBaseOperations {
    public static string CachePath(Func<string?> readCache) => PathUtils.AddSlashSuffix(
        (string.IsNullOrEmpty(readCache()) ? Path.GetTempPath() + @"PCL\" : readCache()!).Replace("/", @"\"));

    // Keep every original branch read: these values can be backed by mutable legacy fields.
    public static string PurePath(Func<string> tempPath, Func<string> osDrive) {
        if (Paths.Base.IsAsciiOnly()) return Paths.Base + @"PCL\";
        if (Paths.AppDataThenName.IsAsciiOnly()) return Paths.AppDataThenName;
        if (tempPath().IsAsciiOnly()) return tempPath();
        return osDrive() + @"ProgramData\PCL\";
    }
    public static double Val(object? value) {
        if (value is null) return 0;
        try { return value is string text && text == "&" ? 0 : Conversion.Val(value); }
        catch { return 0; }
    }
    public static T GetResultWithTimeout<T>(Task<T> targetTask, CancellationTokenSource tokenSource, int timeoutMs) {
        Task delayTask = Task.Delay(timeoutMs);
        if (ReferenceEquals(Task.WhenAny(targetTask, delayTask).ConfigureAwait(false).GetAwaiter().GetResult(), delayTask)) {
            tokenSource.Cancel();
            throw new TimeoutException($"任务超时（{timeoutMs} ms）");
        }
        return targetTask.Run();
    }
    public static void GetResultWithTimeout(Task targetTask, CancellationTokenSource tokenSource, int timeoutMs) {
        Task delayTask = Task.Delay(timeoutMs);
        if (ReferenceEquals(Task.WhenAny(targetTask, delayTask).ConfigureAwait(false).GetAwaiter().GetResult(), delayTask)) {
            tokenSource.Cancel();
            throw new TimeoutException($"任务超时（{timeoutMs} ms）");
        }
        targetTask.Run();
    }
    public static int GetUuid(ref int uuid, ref object? uuidLock) {
        if (uuidLock is null) uuidLock = new object();
        lock (uuidLock) { uuid = checked(uuid + 1); return uuid; }
    }
    public static long GetTimeMs() => Stopwatch.GetTimestamp() / (Stopwatch.Frequency / 1000L);
    public static long GetUnixTimestampUtc() => ToUnixTimestampUtc(DateTime.Now);
    public static long ToUnixTimestampUtc(DateTime time) => Conversions.ToLong((time.ToUniversalTime().Ticks - 621355968000000000L) / 10000000d);
    public static DateTime GetDate(int timeStamp) => DateTimeOffset.FromUnixTimeSeconds(timeStamp).LocalDateTime;
    public static DateTime GetLocalTime(DateTime utcDate) => DateTime.SpecifyKind(utcDate, DateTimeKind.Utc).ToLocalTime();
    public static int RunAsAdmin(string argument, string executablePath) {
        Process process = StartProcess(new ProcessStartInfo(executablePath) { Verb = "runas", Arguments = argument, UseShellExecute = true });
        process.WaitForExit(); return process.ExitCode;
    }
    // The old net48 constructor defaults to shell execution; modern .NET defaults to false.
    public static Process StartProcess(string fileName, string arguments = "") => StartProcess(new ProcessStartInfo { FileName = fileName, Arguments = arguments, UseShellExecute = true });
    public static Process StartProcess(ProcessStartInfo startInfo) {
        startInfo.FileName = PathUtils.ToShortPath(startInfo.FileName);
        if (!string.IsNullOrEmpty(startInfo.WorkingDirectory)) startInfo.WorkingDirectory = PathUtils.ToShortPath(startInfo.WorkingDirectory);
        Logger.Info($"启动进程：{startInfo.FileName} {startInfo.Arguments}");
        return Process.Start(startInfo)!;
    }
    public static Thread RunInNewThread(Action action, string? name, ThreadPriority priority, Func<int> newUuid) {
        Thread thread = new(() => {
            try { action(); }
            catch (Exception ex) { Logger.Error(ex, $"{name}：线程执行失败"); }
        }) { Name = name ?? "Runtime New Invoke " + newUuid() + "#", Priority = priority };
        thread.Start(); return thread;
    }
    public static void RunInThread(Action action, Func<bool> onUiThread, Func<int> newUuid) {
        if (onUiThread()) RunInNewThread(action, "Invoke " + newUuid(), ThreadPriority.Normal, newUuid);
        else action();
    }
    public static List<T> SortByComparison<T>(IList<T> list, Func<T, T, bool> sortRule) {
        List<T> result = new();
        while (list.Any()) {
            T highest = list[0];
            // VB For evaluates its end once, even if an injected comparator mutates the source.
            int last = checked(list.Count - 1);
            for (int i = 1; i <= last; i = checked(i + 1)) if (sortRule(list[i], highest)) highest = list[i];
            list.Remove(highest); result.Add(highest);
        }
        return result;
    }
    public static object? GetProgramArgument(string name) => GetProgramArgument(name, "");
    public static object? GetProgramArgument(string name, object? defaultValue) {
        string[] arguments = Interaction.Command().Split(" ");
        for (int i = 0, last = checked(arguments.Length - 1); i <= last; i = checked(i + 1)) {
            if ((arguments[i] ?? "") == ("-" + name)) {
                if (arguments.Length == checked(i + 1) || arguments[checked(i + 1)].StartsWithF("-")) return true;
                return arguments[checked(i + 1)];
            }
        }
        return defaultValue;
    }
    public static void OpenWebsite(string url, LegacyBaseHost host, Func<int> newUuid) {
        try {
            if (!url.StartsWithF("http", true) && !url.StartsWithF("minecraft://", true) && !url.StartsWithF("minecraft-preview://", true))
                throw new Exception(url + " 不是一个有效的网址，它必须以 http 开头！");
            Logger.Info($"正在打开网页：{url}"); StartProcess(url);
        } catch (Exception ex) {
            Logger.Warn(ex, $"无法打开网页（{url}）"); ClipboardSet(url, false, host, newUuid);
            host.Message("可能由于浏览器未正确配置，PCL 无法为你打开网页。\r\n网址已经复制到剪贴板，若有需要可以手动粘贴访问。\r\n" + $"网址：{url}", "无法打开网页");
        }
    }
    public static void OpenExplorer(string location) {
        try {
            location = location.Replace("/", @"\").Trim(' ', '"');
            Logger.Info($"正在打开资源管理器：{location}");
            if (location.EndsWithF(@"\")) StartProcess(location);
            else StartProcess("explorer", $"/select,\"{location}\"");
        } catch (Exception ex) { Logger.Error(ex, "打开资源管理器失败，请尝试关闭安全软件（如 360 安全卫士）", LogBehavior.Alert); }
    }
    public static void ClipboardSet(string? text, bool showSuccessHint, LegacyBaseHost host, Func<int> newUuid) {
        RunInThread(() => {
            try {
                Retrier.Attempt(delay: attempt => TimeSpan.FromMilliseconds(200), maxAttempts: 4, isRetryAllowed: ex => true,
                    action: attempt => host.DispatchWait(() => { host.ClearClipboard(); if (!string.IsNullOrEmpty(text)) host.WriteClipboardText(text!); }));
                if (showSuccessHint) host.SuccessHint("已成功复制！");
            } catch (Exception ex) { Logger.Error(ex, "可能由于剪贴板被其他程序占用，文本复制失败", LogBehavior.Toast); }
        }, host.OnUiThread, newUuid);
    }
    public static string? ClipboardGetText(LegacyBaseHost host) {
        string? result = null;
        host.DispatchWait(() => {
            try { if (host.ClipboardContainsText()) result = host.ReadClipboardText(); }
            catch (Exception ex) { Logger.Warn(ex, "获取剪贴板文本失败"); }
        });
        return result;
    }
    public static T RandomOne<T>(ICollection<T> objects, Func<int, int, int> randomInteger) => objects.ElementAtOrDefault(randomInteger(0, checked(objects.Count - 1)))!;
    public static int RandomInteger(int min, int max, Random random) => Conversions.ToInteger(Math.Floor(checked(max - min + 1) * random.NextDouble()) + min);
    public static IEnumerable<T> Shuffle<T>(IEnumerable<T> raw, Func<int, int, int> randomInteger) {
        List<T> copy = new(raw);
        while (copy.Any()) { int index = randomInteger(0, checked(copy.Count - 1)); yield return copy[index]; copy.RemoveAt(index); }
    }
}
