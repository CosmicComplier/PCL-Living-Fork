using System;
using System.Linq;

string[] groups = ["bridge", "argv", "macros", "rejections", "classpath"];
if (args is ["--list-groups"]) { foreach (string group in groups) Console.WriteLine(group); return 0; }
int selected = -1;
if (args.Length != 0) {
    if (args is not ["--group", var group] || (selected = Array.IndexOf(groups, group)) < 0) { Console.Error.WriteLine("Use --group bridge|argv|macros|rejections|classpath, --list-groups, or no arguments for all checks."); return 2; }
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
