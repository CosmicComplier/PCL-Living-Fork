using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualBasic.CompilerServices;

namespace PCLCS.Startup;

// Original LF Modules/ThirdParty/DragHelper.vb attribution:
// https://blog.csdn.net/simpleman2000/article/details/140952294
// Port the existing source and Win32 ABI; no CE drop policy or new long-name/recovery behavior.
[StructLayout(LayoutKind.Sequential)]
public struct LegacyDropPoint { public int X, Y; }
[StructLayout(LayoutKind.Sequential)]
public struct LegacyDropFilter { public uint Size, Status; }
public delegate bool LegacyChangeFilterEx(IntPtr handle, uint message, uint action, ref LegacyDropFilter filter);
public delegate uint LegacyQueryDropFile(IntPtr drop, uint index, StringBuilder? value, int capacity);
public delegate bool LegacyQueryDropPoint(IntPtr drop, ref LegacyDropPoint point);
public sealed class LegacyDragNativeApi {
    public Func<Version> Version = () => Environment.OSVersion.Version;
    public LegacyChangeFilterEx FilterEx = Native.ChangeWindowMessageFilterEx;
    public Func<uint, uint, bool> Filter = Native.ChangeWindowMessageFilter;
    public Action<IntPtr, bool> AcceptFiles = Native.DragAcceptFiles;
    public LegacyQueryDropFile QueryFile = Native.DragQueryFile;
    public LegacyQueryDropPoint QueryPoint = Native.DragQueryPoint;
    public Action<IntPtr> Finish = Native.DragFinish;
    public Func<IntPtr, int> Revoke = Native.RevokeDragDrop;
    public Func<bool> IsAdmin = Native.IsUserAnAdmin;
    public Func<int> LastError = Marshal.GetLastWin32Error;
    private static class Native {
        [DllImport("user32.dll", SetLastError = true)] internal static extern bool ChangeWindowMessageFilterEx(IntPtr handle, uint message, uint action, ref LegacyDropFilter filter);
        [DllImport("user32.dll", SetLastError = true)] internal static extern bool ChangeWindowMessageFilter(uint message, uint flags);
        [DllImport("shell32.dll")] internal static extern void DragAcceptFiles(IntPtr handle, bool accept);
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)] internal static extern uint DragQueryFile(IntPtr drop, uint index, StringBuilder? value, int capacity);
        [DllImport("shell32.dll")] internal static extern bool DragQueryPoint(IntPtr drop, ref LegacyDropPoint point);
        [DllImport("shell32.dll")] internal static extern void DragFinish(IntPtr drop);
        [DllImport("ole32.dll")] internal static extern int RevokeDragDrop(IntPtr handle);
        [DllImport("shell32.dll")] internal static extern bool IsUserAnAdmin();
    }
}
public static class LegacyWindowDragNative {
    private const uint CopyGlobal = 0x49, CopyData = 0x4A, DropFiles = 0x233, Allow = 1, Add = 1;
    public static void ChangeMessageFilter(IntPtr handle, LegacyDragNativeApi? native = null) {
        LegacyDragNativeApi api = native ?? new(); Version version = api.Version();
        bool vista = version >= new Version(6, 0), nt61 = version >= new Version(6, 1);
        if (!vista) return;
        LegacyDropFilter filter = new() { Size = 8 };
        foreach (uint message in new[] { DropFiles, CopyGlobal, CopyData }) {
            bool error;
            if (nt61) error = !api.FilterEx(handle, message, Allow, ref filter); else error = !api.Filter(message, Add);
            if (error) throw new Win32Exception(api.LastError());
        }
    }
    public static bool TryGetDropInfo(int message, IntPtr wParam, ref string[]? files, ref LegacyDropPoint point, LegacyDragNativeApi? native = null) {
        LegacyDragNativeApi api = native ?? new(); files = null; point = new();
        if (message != DropFiles) return false;
        uint fileCount = api.QueryFile(wParam, uint.MaxValue, null, 0); files = new string[Conversions.ToInteger(fileCount)];
        uint last = checked((uint)checked(Conversions.ToInteger(fileCount) - 1));
        for (uint index = 0; index <= last; index = checked(index + 1)) {
            StringBuilder text = new(260); uint count = api.QueryFile(wParam, index, text, text.Capacity); if (count > 0) files[index] = text.ToString();
        }
        api.QueryPoint(wParam, ref point); api.Finish(wParam); return true;
    }
}
