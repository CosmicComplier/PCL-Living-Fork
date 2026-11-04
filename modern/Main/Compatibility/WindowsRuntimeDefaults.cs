using System;
using System.Runtime.InteropServices;
using System.Text;
namespace PCL.Compatibility;
// LF used CLR4 Encoding.Default for its Windows ACP/GBK launch decision. Modern
// Encoding.Default is always UTF8. Query the original Windows ACP locally, never
// register a process-global provider or change explicit UTF8/GB18030 selections.
internal static class WindowsRuntimeDefaults {
    private static readonly Lazy<Encoding> ansi = new(() => OperatingSystem.IsWindows() ? ForWindowsCodePage(checked((int)GetACP())) : Encoding.Default);
    internal static Encoding WindowsDefaultEncoding => ansi.Value;
    internal static Encoding ForWindowsCodePage(int codePage) => codePage == 65001 ? new UTF8Encoding(false) : CodePagesEncodingProvider.Instance.GetEncoding(codePage) ?? Encoding.GetEncoding(codePage);
    [DllImport("kernel32.dll", ExactSpelling = true)] private static extern uint GetACP();
}
