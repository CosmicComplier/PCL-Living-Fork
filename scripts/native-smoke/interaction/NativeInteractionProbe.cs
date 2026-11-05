// External UIAutomation/message observer for the genuine published LF50 Main.
// No product assembly is loaded, reflected, injected, or replaced. See root LICENCE.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Automation;
using Microsoft.Win32;

namespace PclLfNativeInteraction;

internal sealed class Probe
{
    private const string Notice = "PCL Living Fork 1.3.5-pre-3 版本说明";
    private const string Caption = "PCL Living Fork 1.3.5-pre-3 是基于 PCL 官方开源版的第三方版本。\r\n\r\n- 首页可开关模组依赖检查与自动补全\r\n- 整合包更新按分类预览，下载显示实际进度\r\n- 还原点可选设置自动清理上限，手动记录单独保留";
    private readonly Dictionary<string, string> args;
    private readonly Dictionary<string, object?> evidence = new();
    private readonly List<object> steps = new();
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly int pid;
    private readonly IntPtr hwnd;
    private readonly string fixture, owner, exe, output;
    private readonly Process process;
    private bool themeChanged;
    private bool identityProved;
    private AutomationElement Root => AutomationElement.FromHandle(hwnd);

    private Probe(Dictionary<string, string> arguments)
    {
        args = arguments; pid = int.Parse(args["pid"], CultureInfo.InvariantCulture);
        hwnd = new IntPtr(long.Parse(args["hwnd"], CultureInfo.InvariantCulture));
        fixture = Path.GetFullPath(args["fixture"]); owner = args["owner"];
        exe = Path.GetFullPath(args["exe"]);
        if (!Guid.TryParseExact(owner, "N", out _) || args["pass"] is not ("1" or "2")) throw new InvalidOperationException("Invalid fixture identity/pass.");
        output = Path.Combine(fixture, "interaction-" + args["pass"] + ".json");
        process = Process.GetProcessById(pid);
        evidence["Result"] = "pending"; evidence["FixtureId"] = owner; evidence["Pass"] = args["pass"];
        evidence["ProcessId"] = pid; evidence["MainHandle"] = hwnd.ToInt64(); evidence["MainSha256"] = args["sha256"];
        evidence["Steps"] = steps;
        evidence["Limit"] = "Only the owned published Main in the CI windowstation. Layout/screenshot and observable UIA state are evidence; human mouse/keyboard/font feel, drag/drop, real accounts/services/game/hardware remain unverified.";
    }

    [STAThread]
    private static int Main(string[] values)
    {
        Probe? probe = null;
        try
        {
            if (!OperatingSystem.IsWindows() || !Environment.Is64BitProcess || Environment.GetEnvironmentVariable("CI") != "true" || Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true" || Environment.GetEnvironmentVariable("RUNNER_ENVIRONMENT") != "github-hosted") throw new InvalidOperationException("PENDING: isolated GitHub-hosted x64 Windows only.");
            Native.SetThreadDpiAwarenessContext(new IntPtr(-4));
            if (values.Length != 18) throw new InvalidOperationException("Expected nine named fixture arguments.");
            Dictionary<string, string> arguments = new(StringComparer.Ordinal);
            for (int i = 0; i < values.Length; i += 2) arguments.Add(values[i].TrimStart('-'), values[i + 1]);
            probe = new Probe(arguments); probe.Guard(true); probe.identityProved = true; probe.Run(); probe.evidence["Result"] = "passed";
            return 0;
        }
        catch (Exception error)
        {
            if (probe != null) { probe.evidence["Result"] = error.Message.StartsWith("PENDING:", StringComparison.Ordinal) ? "pending" : "failed"; probe.evidence["Error"] = error.GetType().Name + ": " + error.Message; }
            if (probe?.identityProved == true)
            {
                // Failure evidence only, after the same live owner/image/profile
                // guard; never act on an unknown window or mask the primary error.
                try { probe.evidence["FailureUi"] = probe.Nodes().Where(probe.Visible).Take(200).Select(node => {
                    var current = node.Current;
                    return new { Name = current.Name.Length <= 256 ? current.Name : current.Name[..256],
                        current.AutomationId, current.ClassName, Type = current.ControlType.ProgrammaticName,
                        Bounds = current.BoundingRectangle.ToString(CultureInfo.InvariantCulture) };
                }).ToArray(); }
                catch (Exception diagnostic) { probe.evidence["FailureUiError"] = diagnostic.GetType().Name; }
            }
            Console.Error.WriteLine(error.GetType().Name + ": " + error.Message); return 1;
        }
        finally
        {
            if (probe != null)
            {
                if (probe.themeChanged)
                {
                    try { probe.SetTheme("浅色", 0x24); probe.evidence["ThemeRollback"] = "restored-through-owned-real-UI"; }
                    catch (Exception error) { probe.evidence["ThemeRollback"] = error.Message; probe.evidence["Result"] = "failed"; }
                }
                // Guard the destination again. A changed marker preserves evidence instead of writing elsewhere.
                if (File.Exists(Path.Combine(probe.fixture, "owner.txt")) && File.ReadAllText(Path.Combine(probe.fixture, "owner.txt")) == probe.owner)
                {
                    probe.evidence["ElapsedMilliseconds"] = probe.clock.ElapsedMilliseconds;
                    File.WriteAllText(probe.output, JsonSerializer.Serialize(probe.evidence, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
                }
                probe.process.Dispose();
            }
        }
    }

    private void Guard(bool hash = false)
    {
        if (clock.Elapsed.TotalSeconds > 80) throw new InvalidOperationException("PENDING: bounded interaction budget exhausted.");
        string workspace = Path.GetFullPath(args["workspace"]).TrimEnd(Path.DirectorySeparatorChar);
        if (!fixture.StartsWith(workspace + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !exe.StartsWith(fixture + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || File.ReadAllText(Path.Combine(fixture, "owner.txt")) != owner) throw new InvalidOperationException("Fixture path/owner differs.");
        foreach (string name in new[] { "Software\\PCL", "Software\\PCLDebug" })
        {
            using RegistryKey? store = Registry.CurrentUser.OpenSubKey(name, false);
            if (store?.GetValue("__PclLfNativeSmokeOwner") as string != owner) throw new InvalidOperationException("Synthetic profile ownership differs.");
        }
        process.Refresh();
        if (process.HasExited || Native.Owner(hwnd) != pid || !Native.IsWindowVisible(hwnd) || Native.Title(hwnd) != "LivingPCL" || !Native.Class(hwnd).StartsWith("HwndWrapper", StringComparison.Ordinal)) throw new InvalidOperationException("Actual Main PID/HWND/title/class is no longer owned and visible.");
        StringBuilder path = new(32768); uint count = (uint)path.Capacity;
        if (!Native.QueryFullProcessImageName(process.Handle, 0, path, ref count) || !string.Equals(path.ToString(), exe, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Actual process handle image differs from owned EXE.");
        if (hash && !string.Equals(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(exe))), args["sha256"], StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Owned installed EXE SHA differs.");
        Native.CheckNamedWindows(pid, hwnd);
        using (StreamReader reader = new(new FileStream(Path.Combine(Path.GetDirectoryName(exe)!, "PCL", "Log1.txt"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)))
        {
            string log = reader.ReadToEnd();
            if (Regex.IsMatch(log, @"(?m)^\d{2}:\d{2}:\d{2}\.\d{3} E ")) throw new InvalidOperationException("Actual Main emitted an Error log during interaction.");
            foreach (Match title in Regex.Matches(log, @"(?:普通|选择|输入)弹窗：([^\r\n]+)"))
                if (title.Groups[1].Value.Trim() != Notice) throw new InvalidOperationException("PENDING: unexpected actual dialog, never auto-clicked: " + title.Groups[1].Value.Trim());
        }
        if (Root.Current.ProcessId != pid || Root.Current.NativeWindowHandle != hwnd.ToInt64()) throw new InvalidOperationException("UIA root does not represent actual Main HWND.");
    }

    private List<AutomationElement> Nodes()
    {
        Guard(); List<AutomationElement> result = new();
        Walk(Root, 0, result); return result;
    }
    private static void Walk(AutomationElement node, int depth, List<AutomationElement> result)
    {
        if (depth > 40 || result.Count > 2500) throw new InvalidOperationException("PENDING: bounded owned UIA tree exceeded.");
        result.Add(node);
        for (AutomationElement? child = TreeWalker.RawViewWalker.GetFirstChild(node); child != null; child = TreeWalker.RawViewWalker.GetNextSibling(child)) Walk(child, depth + 1, result);
    }
    private bool Visible(AutomationElement node)
    {
        var current = node.Current; var r = current.BoundingRectangle;
        return current.ProcessId == pid && !current.IsOffscreen && !r.IsEmpty && r.Width > 0 && r.Height > 0;
    }
    private AutomationElement Unique(Func<AutomationElement, bool> test, string description)
    {
        var found = Nodes().Where(n => Visible(n) && test(n)).ToList();
        if (found.Count != 1) throw new InvalidOperationException("PENDING: expected one owned visible " + description + "; found " + found.Count + ".");
        AssertAncestor(found[0]); return found[0];
    }
    private AutomationElement Text(string text, string id, bool top = false, bool left = false) => Unique(n => n.Current.ControlType == ControlType.Text && n.Current.Name == text && n.Current.AutomationId == id && (!top || n.Current.BoundingRectangle.Bottom <= Native.Client(hwnd).Top + 57) && (!left || n.Current.BoundingRectangle.Right < Native.Client(hwnd).Left + 220), "Text(" + id + "," + text + ")");
    private AutomationElement Combo() => Unique(n => n.Current.ControlType == ControlType.ComboBox && n.Current.AutomationId == "ComboLauncherColorMode", "ComboLauncherColorMode");
    private void AssertAncestor(AutomationElement? node)
    {
        for (int i = 0; i < 45 && node != null; i++)
        {
            if (node.Current.ProcessId != pid) throw new InvalidOperationException("UIA ancestor belongs to another process.");
            int handle = node.Current.NativeWindowHandle;
            if (handle != 0) { if (new IntPtr(handle) != hwnd) throw new InvalidOperationException("UIA ancestor is outside the owned Main HWND."); return; }
            node = TreeWalker.RawViewWalker.GetParent(node);
        }
        throw new InvalidOperationException("PENDING: UIA target has no provable Main HWND ancestor.");
    }
    private void Click(AutomationElement node, string step)
    {
        Guard(); AssertAncestor(node); var r = node.Current.BoundingRectangle; var client = Native.Client(hwnd);
        if (!Visible(node) || !client.Contains(r)) throw new InvalidOperationException("UIA click target is outside actual Main client.");
        Native.Point point = new() { X = (int)(r.Left + r.Width / 2), Y = (int)(r.Top + r.Height / 2) };
        if (!Native.ScreenToClient(hwnd, ref point)) throw new InvalidOperationException("Client conversion failed.");
        IntPtr location = Native.Pack(point.X, point.Y);
        Native.Send(hwnd, 0x0200, IntPtr.Zero, location); Native.Send(hwnd, 0x0201, new IntPtr(1), location); Native.Send(hwnd, 0x0202, IntPtr.Zero, location);
        steps.Add(new { Action = step, Input = "WM_MOUSEMOVE/WM_LBUTTONDOWN/WM_LBUTTONUP to owned Main client only", Text = node.Current.Name, X = point.X, Y = point.Y });
    }
    private void Wait(Func<bool> check, string description, int timeout = 7000)
    {
        Stopwatch timer = Stopwatch.StartNew();
        while (timer.ElapsedMilliseconds < timeout) { Guard(); if (check()) return; Thread.Sleep(100); }
        throw new InvalidOperationException("PENDING: actual Main did not demonstrate " + description + " within bound.");
    }
    private void Stable(string step)
    {
        Stopwatch timer = Stopwatch.StartNew(); string? previous = null; int same = 0, samples = 0;
        while (timer.ElapsedMilliseconds < 7000)
        {
            var nodes = Nodes().Where(Visible).Select(n => { var c = n.Current; var r = c.BoundingRectangle; return c.ClassName + ":" + c.AutomationId + ":" + Math.Round(r.X, 1) + "," + Math.Round(r.Y, 1) + "," + Math.Round(r.Width, 1) + "," + Math.Round(r.Height, 1); }).OrderBy(s => s, StringComparer.Ordinal);
            string signature = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", nodes)))); samples++;
            same = signature == previous ? same + 1 : 0; previous = signature;
            if (same >= 3 && timer.ElapsedMilliseconds >= 900) { steps.Add(new { Action = step, LayoutStableSamples = same + 1, Samples = samples, LayoutSha256 = signature }); return; }
            Thread.Sleep(180);
        }
        throw new InvalidOperationException("PENDING: bounded owned UIA layout did not stabilize: " + step);
    }
    private string Selection(AutomationElement combo)
    {
        if (!combo.TryGetCurrentPattern(SelectionPattern.Pattern, out object pattern)) throw new InvalidOperationException("PENDING: actual theme ComboBox lacks SelectionPattern.");
        var selected = ((SelectionPattern)pattern).Current.GetSelection();
        if (selected.Length != 1 || selected[0].Current.ProcessId != pid) throw new InvalidOperationException("Theme selection is not uniquely owned.");
        return selected[0].Current.Name;
    }
    private void Focus()
    {
        Guard(); var combo = Combo(); combo.SetFocus();
        Wait(() => Combo().Current.HasKeyboardFocus && AutomationElement.FocusedElement.Current.ProcessId == pid, "owned theme ComboBox keyboard focus");
        steps.Add(new { Action = "focus", Target = "ComboLauncherColorMode", ObservedKeyboardFocus = true });
    }
    private void SetTheme(string selected, int key)
    {
        Focus(); Guard(); Native.Send(hwnd, 0x0100, new IntPtr(key), new IntPtr(1)); Native.Send(hwnd, 0x0101, new IntPtr(key), new IntPtr(unchecked((int)0xC0000001)));
        Wait(() => Selection(Combo()) == selected, "real theme selection " + selected); Stable("theme-" + selected);
        string expected = selected == "深色" ? "1" : "0";
        Wait(() => ReadThemeSetting() == expected, "owned Setup.ini UiLauncherColorMode=" + expected);
        steps.Add(new { Action = "theme", Selection = selected, OwnedIniValue = expected, Input = key == 0x23 ? "VK_END" : "VK_HOME" });
    }
    private string? ReadThemeSetting()
    {
        Guard(); string file = Path.Combine(Path.GetDirectoryName(exe)!, "PCL", "Setup.ini");
        using StreamReader reader = new(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete));
        List<string> values = new(); string? line;
        while ((line = reader.ReadLine()) != null) if (line.StartsWith("UiLauncherColorMode:", StringComparison.Ordinal)) values.Add(line[20..]);
        if (values.Count > 1) throw new InvalidOperationException("Ambiguous owned theme setting.");
        return values.SingleOrDefault();
    }
    private double Capture(string step)
    {
        Guard(); var r = Native.Window(hwnd); int width = (int)r.Width, height = (int)r.Height;
        if (width < 1 || height < 1 || width > 4096 || height > 4096) throw new InvalidOperationException("Unsupported own window screenshot bounds.");
        using Bitmap bitmap = new(width, height); using Graphics graphics = Graphics.FromImage(bitmap);
        IntPtr hdc = graphics.GetHdc(); bool printed;
        try { printed = Native.PrintWindow(hwnd, hdc, 2); } finally { graphics.ReleaseHdc(hdc); }
        if (!printed) throw new InvalidOperationException("PENDING: PrintWindow failed for actual owned Main.");
        List<double> luma = new(); HashSet<int> colors = new();
        for (int y = height / 4; y < height - 30; y += 13) for (int x = width / 3; x < width - 30; x += 13) { Color color = bitmap.GetPixel(x, y); colors.Add(color.ToArgb()); luma.Add(.2126 * color.R + .7152 * color.G + .0722 * color.B); }
        if (colors.Count < 3) throw new InvalidOperationException("PENDING: own Main screenshot is uniform/blank.");
        string file = Path.Combine(fixture, "interaction-" + args["pass"] + "-" + step + ".png"); bitmap.Save(file, ImageFormat.Png);
        double mean = luma.Average(); steps.Add(new { Action = "screenshot", Checkpoint = step, File = Path.GetFileName(file), Width = width, Height = height, SampleColors = colors.Count, MeanLuma = mean }); return mean;
    }
    private void Run()
    {
        if (args["notice"] != Notice) throw new InvalidOperationException("PENDING: only the reviewed 1.3.5-pre-3 information notice may be dismissed.");
        Stable("known-notice"); var title = Text(Notice, "LabTitle"); var caption = Text(Caption, "LabCaption"); var button = Text("确定", "LabText");
        if (caption.Current.BoundingRectangle.Top <= title.Current.BoundingRectangle.Top || button.Current.BoundingRectangle.Top <= caption.Current.BoundingRectangle.Top) throw new InvalidOperationException("Information notice geometry/unique button differs.");
        Capture("notice"); Click(button, "dismiss-exact-information-notice");
        Wait(() => !Nodes().Any(n => Visible(n) && n.Current.Name == Notice), "only reviewed notice dismissed");
        Stable("launch"); Capture("launch");
        Click(Text("设置", "LabText", top: true), "main-settings"); Stable("settings");
        Click(Text("个性化", "LabTitle", left: true), "personalization"); Stable("personalization");
        Text("外观主题", "");
        if (Selection(Combo()) != "浅色") throw new InvalidOperationException("PENDING: initial synthetic theme differs from default light.");
        double light = Capture("light");
        // Mark before input so a partial dark transition also attempts a real UI rollback.
        themeChanged = true; SetTheme("深色", 0x23); double dark = Capture("dark");
        if (light - dark < 15) throw new InvalidOperationException("PENDING: actual Main did not render an observable light/dark change.");
        SetTheme("浅色", 0x24); themeChanged = false; double restored = Capture("light-restored");
        if (Math.Abs(light - restored) > 10) throw new InvalidOperationException("PENDING: restored theme screenshot differs materially.");
        Focus(); Guard(); Native.Send(hwnd, 0x0112, new IntPtr(0xF020), IntPtr.Zero);
        Wait(() => Native.IsIconic(hwnd), "same owned Main minimized");
        Native.Send(hwnd, 0x0112, new IntPtr(0xF120), IntPtr.Zero);
        Wait(() => !Native.IsIconic(hwnd), "same owned Main restored"); Stable("restored-window"); Focus(); Capture("restored-window");
        var scroll = Unique(n => n.Current.AutomationId == "PanBack" && n.TryGetCurrentPattern(ScrollPattern.Pattern, out _) && n.Current.BoundingRectangle.Left > Native.Client(hwnd).Left + 150, "personalization PanBack ScrollPattern");
        ScrollPattern pattern = (ScrollPattern)scroll.GetCurrentPattern(ScrollPattern.Pattern);
        if (!pattern.Current.VerticallyScrollable || pattern.Current.VerticalScrollPercent > 1) throw new InvalidOperationException("PENDING: personalization has no provable top/scrollable content.");
        double before = pattern.Current.VerticalScrollPercent; var rect = scroll.Current.BoundingRectangle;
        if (!Native.Client(hwnd).Contains(rect)) throw new InvalidOperationException("Scroll target is outside actual Main client.");
        Native.Point point = new() { X = (int)(rect.Left + rect.Width / 2), Y = (int)(rect.Top + rect.Height / 2) }; var local = point;
        if (!Native.ScreenToClient(hwnd, ref local)) throw new InvalidOperationException("Scroll client conversion failed.");
        Guard(); Native.Send(hwnd, 0x0200, IntPtr.Zero, Native.Pack(local.X, local.Y)); Native.Send(hwnd, 0x020A, new IntPtr(unchecked((int)0xFE980000)), Native.Pack(point.X, point.Y));
        Wait(() => pattern.Current.VerticalScrollPercent > before + .1, "real wheel message changed ScrollPattern offset"); Stable("wheel-down");
        double after = pattern.Current.VerticalScrollPercent; Capture("wheel-down");
        Guard(); Native.Send(hwnd, 0x020A, new IntPtr(0x01680000), Native.Pack(point.X, point.Y));
        Wait(() => pattern.Current.VerticalScrollPercent < after - .1, "real reverse wheel changed ScrollPattern offset"); Stable("wheel-up"); Capture("wheel-up");
        steps.Add(new { Action = "wheel", BeforePercent = before, AfterPercent = after, ReturnedPercent = pattern.Current.VerticalScrollPercent, Input = "owned WM_MOUSEWHEEL +/-360" });
        // Return to the safe launch page; no download/login/update/link/UAC/clear action exists here.
        Click(Text("启动", "LabText", top: true), "return-launch"); Stable("launch-returned"); Capture("launch-returned");
        Guard(true); if (ReadThemeSetting() != "0") throw new InvalidOperationException("Owned theme setting changed after real UI rollback."); evidence["ThemeRestored"] = true;
        evidence["ObservedScope"] = "Exact local notice, Main settings/personalization/launch, real theme and INI restore, focus, same-HWND minimize/restore, measured wheel offsets and stable owned-window captures.";
    }
}

internal static class Native
{
    [StructLayout(LayoutKind.Sequential)] internal struct Point { internal int X, Y; }
    [StructLayout(LayoutKind.Sequential)] private struct Rect { internal int Left, Top, Right, Bottom; }
    private delegate bool EnumCallback(IntPtr hwnd, IntPtr state);
    [DllImport("user32.dll")] internal static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);
    [DllImport("user32.dll")] internal static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] internal static extern bool IsIconic(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out Rect rect);
    [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr hwnd, out Rect rect);
    [DllImport("user32.dll")] private static extern bool ClientToScreen(IntPtr hwnd, ref Point point);
    [DllImport("user32.dll")] internal static extern bool ScreenToClient(IntPtr hwnd, ref Point point);
    [DllImport("user32.dll")] internal static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int count);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr hwnd, StringBuilder text, int count);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumCallback callback, IntPtr state);
    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SendMessageTimeout(IntPtr hwnd, uint message, IntPtr first, IntPtr second, uint flags, uint timeout, out IntPtr result);
    [DllImport("kernel32.dll", EntryPoint = "QueryFullProcessImageNameW", CharSet = CharSet.Unicode, SetLastError = true)] internal static extern bool QueryFullProcessImageName(IntPtr process, uint flags, StringBuilder path, ref uint size);
    internal static int Owner(IntPtr hwnd) { GetWindowThreadProcessId(hwnd, out uint pid); return (int)pid; }
    internal static string Title(IntPtr hwnd) { StringBuilder text = new(1024); GetWindowText(hwnd, text, text.Capacity); return text.ToString().Trim(); }
    internal static string Class(IntPtr hwnd) { StringBuilder text = new(256); GetClassName(hwnd, text, text.Capacity); return text.ToString(); }
    internal static System.Windows.Rect Window(IntPtr hwnd) { if (!GetWindowRect(hwnd, out Rect r)) throw new InvalidOperationException("Window bounds unavailable."); return new(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top); }
    internal static System.Windows.Rect Client(IntPtr hwnd) { if (!GetClientRect(hwnd, out Rect r)) throw new InvalidOperationException("Client bounds unavailable."); Point p = new(); if (!ClientToScreen(hwnd, ref p)) throw new InvalidOperationException("Client origin unavailable."); return new(p.X, p.Y, r.Right, r.Bottom); }
    internal static IntPtr Pack(int x, int y) { if (x < short.MinValue || x > short.MaxValue || y < short.MinValue || y > short.MaxValue) throw new InvalidOperationException("Message coordinates exceed signed client contract."); return new IntPtr(unchecked((int)(((uint)(ushort)y << 16) | (ushort)x))); }
    internal static void Send(IntPtr hwnd, uint message, IntPtr first, IntPtr second) { if (SendMessageTimeout(hwnd, message, first, second, 2, 1200, out _) == IntPtr.Zero) throw new InvalidOperationException("PENDING: bounded owned HWND message was not processed: " + message); }
    internal static void CheckNamedWindows(int pid, IntPtr main)
    {
        bool unexpected = false;
        EnumWindows((window, _) => { if (Owner(window) == pid && IsWindowVisible(window) && window != main && Title(window).Length != 0) unexpected = true; return true; }, IntPtr.Zero);
        if (unexpected) throw new InvalidOperationException("PENDING: unexpected named actual product window; never auto-clicked.");
    }
}
