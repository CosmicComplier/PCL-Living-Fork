using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace PCLCS;

// Original LF host implementation; repository LICENCE applies.
internal static class MmcCanonicalPaths {
    internal static string ForAdmission(string root) {
        if (!OperatingSystem.IsWindows()) return root;
        Stack<string> missing = new(); string existing = root;
        while (MmcInstancePaths.Attributes(existing) is null) {
            missing.Push(Path.GetFileName(existing)); existing = Path.GetDirectoryName(existing) ?? throw new IOException("无法解析准入目录。");
        }
        MmcInstancePaths.CheckDirectoryRole(existing);
        using SafeFileHandle handle = CreateFileW(existing, 0, 7, IntPtr.Zero, 3, 0x02000000, IntPtr.Zero);
        if (handle.IsInvalid) throw new IOException("无法取得准入目录的规范路径。", new Win32Exception(Marshal.GetLastWin32Error()));
        StringBuilder text = new(512); uint length = GetFinalPathNameByHandleW(handle, text, (uint)text.Capacity, 0);
        if (length == 0) throw new IOException("无法读取准入目录的规范路径。", new Win32Exception(Marshal.GetLastWin32Error()));
        if (length >= text.Capacity) {
            if (length > 32768) throw new IOException("规范目录路径过长。");
            text = new(checked((int)length + 1)); length = GetFinalPathNameByHandleW(handle, text, (uint)text.Capacity, 0);
            if (length == 0 || length >= text.Capacity) throw new IOException("规范目录路径读取失败。");
        }
        string resolved = text.ToString();
        if (resolved.StartsWith("\\\\?\\UNC\\", StringComparison.OrdinalIgnoreCase)) resolved = "\\\\" + resolved[8..];
        else if (resolved.StartsWith("\\\\?\\", StringComparison.Ordinal)) resolved = resolved[4..];
        while (missing.Count != 0) resolved = Path.Combine(resolved, missing.Pop());
        return MmcInstancePaths.ValidateVersionRoot(resolved);
    }
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFileW(string name, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetFinalPathNameByHandleW(SafeFileHandle handle, StringBuilder path, uint length, uint flags);
}
