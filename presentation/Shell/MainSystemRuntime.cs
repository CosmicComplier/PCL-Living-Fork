using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using MeloongCore;
using Microsoft.VisualBasic.CompilerServices;
using PCLCS;

namespace PCLLF.Presentation.Shell;

public static class MainWindowInterop {
    // Exact VB Declare ABI; .NET 10 marks this still-supported marshal kind obsolete for a future release.
#pragma warning disable CS0618
    [DllImport("user32", EntryPoint = "FindWindowA", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
    public static extern IntPtr FindWindow([MarshalAs(UnmanagedType.VBByRefStr)] ref string? className, [MarshalAs(UnmanagedType.VBByRefStr)] ref string? windowName);
#pragma warning restore CS0618
    [DllImport("user32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
    public static extern int SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32", EntryPoint = "PostMessageA", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, long wParam, long lParam);
    public static void ShowToTop(IntPtr handle) {
        try { PostMessage(handle, 400 * 16 + 2, 0, 0); SetForegroundWindow(handle); }
        catch (Exception ex) { Logger.Error(ex, "设置窗口置顶失败", LogBehavior.Toast); }
    }
    public static void SetGpuPreference(string executable) {
        const string key = @"HKEY_CURRENT_USER\Software\Microsoft\DirectX\UserGpuPreferences";
        const string value = "GpuPreference=2;";
        if (RegistryUtils.Read(key, executable).result?.ToString() == value) {
            Logger.Info($"无需调整显卡设置：{executable}"); return;
        }
        RegistryUtils.Write(key, executable, value);
        Logger.Info($"已调整显卡设置：{executable}");
    }
}

public sealed class MainTaskTempHost {
    public Func<string> OsDrive = null!, TempPath = null!;
    public Func<int> NewUuid = null!;
    public Func<int, int, int> RandomInteger = null!;
    public Action<string> CheckPermissionWithException = null!;
    public Func<string, bool> CheckPermission = null!;
}

public sealed class MainTaskTempRuntime {
    private readonly MainTaskTempHost host;
    // Keep LF's single-run flags and waiting boundary; no unrelated concurrency redesign.
    private bool cleared, clearing;
    public MainTaskTempRuntime(MainTaskTempHost host) { this.host = host; }
    public void TryClear() {
        if (!cleared) {
            cleared = true; clearing = true;
            try {
                Logger.Info("开始清理任务缓存文件夹");
                DirectoryUtils.Delete($"{host.OsDrive()}ProgramData\\PCL\\TaskTemp\\");
                DirectoryUtils.Delete($"{host.TempPath()}TaskTemp\\");
                Logger.Info("已清理任务缓存文件夹");
            } catch (Exception ex) { Logger.Warn(ex, "清理任务缓存文件夹失败"); }
            finally { clearing = false; }
        } else if (clearing) { while (clearing) Thread.Sleep(1); }
    }
    public string Request(bool requireNonSpace = false) {
        TryClear();
        string folder;
        try {
            folder = $"{host.TempPath()}TaskTemp\\{host.NewUuid()}-{host.RandomInteger(0, 1000000)}\\";
            if (!requireNonSpace || !folder.Contains(" ")) {
                DirectoryUtils.Create(folder); host.CheckPermissionWithException(folder); return folder;
            }
        } catch { }
        folder = $"{host.OsDrive()}ProgramData\\PCL\\TaskTemp\\{host.NewUuid()}-{host.RandomInteger(0, 1000000)}\\";
        DirectoryUtils.Create(folder);
        _ = host.CheckPermission(folder); // Original fallback deliberately does not throw on a false result.
        return folder;
    }
}

public sealed class MainFeedbackHost {
    public Func<string> OsFullName = null!, BasePath = null!, IssuesUrl = null!;
    public Func<string?> MinecraftFolder = null!;
    public Func<ulong> AvailablePhysicalMemory = null!, TotalPhysicalMemory = null!;
    public Func<int> Dpi = null!;
    public Func<string, string, string, string, int> Message = null!;
    public Action<string> Explorer = null!, Website = null!;
}

public sealed class MainFeedbackRuntime {
    private readonly MainFeedbackHost host;
    public MainFeedbackRuntime(MainFeedbackHost host) { this.host = host; }
    public void Feedback(bool showMsgbox = true, bool forceOpenLog = false) {
        // LF On Error Resume Next isolates these statements: opening logs must not prevent opening issues.
        try { Info(); } catch { }
        bool openLog;
        try {
            openLog = forceOpenLog || showMsgbox && host.Message("若你在汇报一个 Bug，请点击 打开文件夹 按钮，并上传 Log(1~5).txt 中包含错误信息的文件。\r\n游戏崩溃一般与启动器无关，请不要因为游戏崩溃而提交反馈。", "反馈提交提醒", "打开文件夹", "不需要") == 1;
        } catch {
            // VB Resume Next after a failing If condition enters its first body statement.
            // Verified by the separate original-language fault sample; do not skip the log action.
            openLog = true;
        }
        try { if (openLog) host.Explorer(host.BasePath() + "PCL\\Log1.txt"); } catch { }
        try { host.Website(host.IssuesUrl()); } catch { }
    }
    public void Info() {
        try {
            Logger.Warn("诊断信息：\r\n" + "操作系统：" + host.OsFullName() + "\r\n" +
                "剩余内存：" + Number(Math.Floor(host.AvailablePhysicalMemory() / 1024.0 / 1024)) + " M / " +
                Number(Math.Floor(host.TotalPhysicalMemory() / 1024.0 / 1024)) + " M\r\n" +
                "DPI：" + Conversions.ToString(host.Dpi()) + "（" + Number(Math.Round(host.Dpi() / 96.0, 2) * 100) + "%）\r\n" +
                "MC 文件夹：" + (host.MinecraftFolder() ?? "Nothing") + "\r\n" + "文件位置：" + host.BasePath());
        } catch { }
    }
    private static string Number(double value) {
#if NETFRAMEWORK
        return Conversions.ToString(value);
#else
        return LegacyPackMetadataNumberFormatting.Format(value, 15, CultureInfo.CurrentCulture);
#endif
    }
}
