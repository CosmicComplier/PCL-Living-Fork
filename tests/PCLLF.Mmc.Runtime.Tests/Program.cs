using System;
using System.Linq;

if (args.Length > 0 && args[0] == "--command-line-probe") { Console.WriteLine(System.Runtime.InteropServices.Marshal.PtrToStringUni(NativeCommandLine.GetCommandLineW())!.Length + 1); return 0; }
string[] groups = ["bridge", "argv", "macros", "rejections", "classpath", "long-arguments"];
if (args is ["--list-groups"]) { foreach (string group in groups) Console.WriteLine(group); return 0; }
int selected = -1;
if (args.Length != 0) {
    if (args is not ["--group", var group] || (selected = Array.IndexOf(groups, group)) < 0) { Console.Error.WriteLine("Use --group bridge|argv|macros|rejections|classpath|long-arguments, --list-groups, or no arguments for all checks."); return 2; }
}
var cases = MmcRuntimeLaunchTests.Cases().ToArray();
int passed = 0;
for (int index = 0; index < cases.Length; index++) {
    if (selected >= 0 && selected != index) continue;
    try { cases[index].Run(); Console.WriteLine("PASS " + groups[index] + ": " + cases[index].Name); passed++; }
    catch (Exception error) { Console.Error.WriteLine("FAIL " + groups[index] + ": " + error); return 1; }
}
Console.WriteLine(passed + " selected MMC runtime checks passed. This is controlled JVM evidence, not Minecraft acceptance.");
return 0;

internal static class NativeCommandLine {
    [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    internal static extern IntPtr GetCommandLineW();
}
