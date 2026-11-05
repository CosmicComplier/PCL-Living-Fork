using System;
using System.Windows.Interop;
using PCLCS.Startup;

namespace PCLLF.Presentation.Windowing;

public sealed class LegacyDragHelperHost {
    public Func<HwndSource> Source = null!;
    public Action<LegacyDropPoint> WritePoint = null!;
    public Action<string[]?> WriteFiles = null!;
    public Action EmitDrop = null!;
}
public sealed class LegacyDragHelperRuntime(LegacyDragHelperHost host, LegacyDragNativeApi? native = null) {
    private readonly LegacyDragNativeApi api = native ?? new();
    public void AddHook(HwndSourceHook hook) {
        RemoveDragHook(hook); host.Source().AddHook(hook); IntPtr handle = host.Source().Handle;
        if (api.IsAdmin()) api.Revoke(handle); api.AcceptFiles(handle, true); LegacyWindowDragNative.ChangeMessageFilter(handle, api);
    }
    public void RemoveDragHook(HwndSourceHook hook) { host.Source().RemoveHook(hook); api.AcceptFiles(host.Source().Handle, false); }
    public IntPtr WndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled) {
        string[]? files = null; LegacyDropPoint point = new();
        if (LegacyWindowDragNative.TryGetDropInfo(message, wParam, ref files, ref point, api)) { host.WritePoint(point); host.WriteFiles(files); host.EmitDrop(); handled = true; }
        return IntPtr.Zero;
    }
    public static void ChangeMessageFilter(IntPtr handle, LegacyDragNativeApi? native = null) => LegacyWindowDragNative.ChangeMessageFilter(handle, native);
    public static bool TryGetDropInfo(int message, IntPtr wParam, ref string[]? files, ref LegacyDropPoint point, LegacyDragNativeApi? native = null) =>
        LegacyWindowDragNative.TryGetDropInfo(message, wParam, ref files, ref point, native);
}
