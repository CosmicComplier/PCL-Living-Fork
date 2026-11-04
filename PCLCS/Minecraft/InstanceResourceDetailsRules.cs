using System;

namespace PCLCS;

/// <summary>LF 原本地资源大小显示规则；保留当前文化与 GB 单位上限。</summary>
public static class InstanceResourceDetailsRules {
    // LF f4aa0fae62c3ee8af24c81e61844c8fdd13c8983 InstanceResourceStore.FormatSize。
    public static string FormatSize(long size) {
        if (size < 1024) return size.ToString() + " B";
        int unit = Math.Min(3, (int)Math.Floor(Math.Log(size, 1024)));
        string[] units = { " B", " KB", " MB", " GB" };
        return (size / Math.Pow(1024, unit)).ToString("0.#") + units[unit];
    }
}
