using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;
using PCLLF.Presentation.Controls;
using static PCLLF.Native.Facade.Tests.BackupResourceChecks;

namespace PCLLF.Native.Facade.Tests;

// Original fixed Windows/path/JSON/UTF16 golds from Test-InstanceResources, not a second implementation.
internal static class BackupResourceContractChecks
{
    private static void Error(string method, Type type, string? message, params object?[] arguments)
    {
        Exception observed = Reject(() => Store(method, arguments), method);
        Native.Check(observed.GetType() == type && (message is null || observed.Message == message), "Original resource exception contract changed for " + method + ": " + observed);
    }
    internal static void Run(string root)
    {
        var clipboard = Native.Field(Native.Type("PCL.Controls.Behaviors.ClipboardInterceptor"), "EnableSafeClipboardProperty").GetValue(null);
        Native.Check(ReferenceEquals(clipboard, ClipboardInterceptorView.EnableSafeClipboardProperty), "Original BAML clipboard owner created a second DP.");
        DependencyProperty property = (DependencyProperty)clipboard!;
        foreach (var sample in new (FrameworkElement Control, int Added)[] { (new TextBox(), 3), (new RichTextBox(), 3), (new DataGrid(), 1) }) {
            int before = sample.Control.CommandBindings.Count;
            Native.Check(!(bool)sample.Control.GetValue(property), "Clipboard attached-property default changed."); sample.Control.SetValue(property, true);
            Native.Check(sample.Control.CommandBindings.Count == before + sample.Added, "Clipboard callback lost original command bindings."); sample.Control.SetValue(property, true);
            Native.Check(sample.Control.CommandBindings.Count == before + sample.Added, "Same clipboard value repeated the change callback."); sample.Control.SetValue(property, false); sample.Control.SetValue(property, true);
            Native.Check(sample.Control.CommandBindings.Count == before + sample.Added * 2, "Clipboard false/true lost original accumulation."); sample.Control.CommandBindings.Clear();
        }
        Type oldState = Native.Type("PCL.MyPageRight+PageStates");
        foreach (string name in new[] { "PCL.MyPageRight", "PCL.PageInstanceMod" }) {
            Type type = Native.Type(name);
            Native.Check(type.GetProperty("PageState")!.PropertyType == oldState && type.GetMethod("get_PageState")!.ReturnType == oldState && type.GetMethod("set_PageState")!.GetParameters()[0].ParameterType == oldState, "Original name-only page-state ABI is ambiguous or changed.");
        }
        object page = New("PCL.MyPageRight");
        var state = Native.Property(page.GetType(), "PageState"); var nativeState = typeof(MyPageRightView).GetProperty("NativePageState")!; object saved = state.GetValue(page)!;
        try {
            state.SetValue(page, Enum.Parse(oldState, "ContentStay")); Native.Check(nativeState.GetValue(page)!.ToString() == "ContentStay", "Original page-state write did not reach native storage.");
            nativeState.SetValue(page, Enum.Parse(nativeState.PropertyType, "ContentExit")); Native.Check(state.GetValue(page)!.ToString() == "ContentExit", "Native page-state write did not reach original enum API.");
        } finally { state.SetValue(page, saved); }
        string[] names = ["Saves", "Screenshots", "ResourcePacks", "ShaderPacks", "Schematics"], titles = ["存档", "截图", "资源包", "光影包", "投影/原理图"], folders = ["saves", "screenshots", "resourcepacks", "shaderpacks", "schematics"];
        Type kindType = Native.Type("PCL.InstanceResourceKind");
        Native.Check(Enum.GetUnderlyingType(kindType) == typeof(int) && Enum.GetNames(kindType).SequenceEqual(names), "Original resource enum identity or names changed.");
        object KindValue(int value) => Enum.ToObject(kindType, value);
        for (int i = 0; i < 5; i++) {
            object kind = KindValue(i); Native.Check(kind.ToString() == names[i] && (string)Store("Title", kind)! == titles[i] && (string)Store("FolderName", kind)! == folders[i], "Resource numeric/title/folder mapping changed.");
            Native.Check((string)Store("Root", "relative\\mixed/slash", kind)! == Path.Combine("relative\\mixed/slash", folders[i]), "Resource Root changed original Path.Combine behavior.");
        }
        foreach (int value in new[] { -1, 5, int.MinValue, int.MaxValue }) {
            foreach (string method in new[] { "Title", "FolderName" }) Error(method, typeof(IndexOutOfRangeException), null, KindValue(value));
            Error("Root", typeof(IndexOutOfRangeException), null, null, KindValue(value));
        }
        Error("Root", typeof(ArgumentNullException), null, null, KindValue(0));
        const string invalid = "文件名无效，请使用不含路径和特殊字符的名称。", system = "此名称为系统保留名称。", launcher = "此名称为启动器临时目录保留。";
        string supplementary = char.ConvertFromUtf32(0x1f4be);
        foreach (string name in new[] { "World name", "截图 2026-11-03", "CONSOLE", "COM0", "COM10", "LPT0", "LPT10", "XCON", ".pcl", ".pclx-test", ".hidden", "pack..name", new string('a', 120), string.Concat(Enumerable.Repeat(supplementary, 60)) }) Native.Check((string)Store("SafeName", name)! == name, "Accepted resource name changed.");
        Error("SafeName", typeof(IOException), invalid, new object?[] { null });
        foreach (string name in new[] { "", " ", "\t", "\u00a0", " name", "name ", "file.", ".", "..", new string('a', 121), string.Concat(Enumerable.Repeat(supplementary, 60)) + "a", "CON.", " CON", ".pcl-bad/" }) Error("SafeName", typeof(IOException), invalid, name);
        foreach (char character in Enumerable.Range(0, 32).Select(x => (char)x).Concat("<>:\"/\\|?*")) Error("SafeName", typeof(IOException), invalid, "a" + character + "b");
        foreach (string name in new[] { "CON", "PRN", "AUX", "NUL" }.Concat(Enumerable.Range(1, 9).SelectMany(i => new[] { "COM" + i, "LPT" + i }))) {
            Error("SafeName", typeof(IOException), system, name); Error("SafeName", typeof(IOException), system, name.ToLowerInvariant() + ".txt");
        }
        foreach (string name in new[] { ".pcl-", ".PCL-test" }) Error("SafeName", typeof(IOException), launcher, name);
        CultureInfo original = CultureInfo.CurrentCulture;
        try {
            foreach (string culture in new[] { "en-US", "fr-FR" }) {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                foreach (var sample in new (long Value, string Text)[] { (-1, "-1 B"), (0, "0 B"), (1023, "1023 B"), (1024, "1 KB"), (1536, "1.5 KB"), (1048576, "1 MB"), (1073741824, "1 GB"), (1099511627776, "1024 GB"), (long.MinValue, "-9223372036854775808 B"), (long.MaxValue, "8589934592 GB") }) Native.Check((string)Store("FormatSize", sample.Value)! == (culture == "fr-FR" ? sample.Text.Replace('.', ',') : sample.Text), "Resource size/current-culture format changed.");
            }
        } finally { CultureInfo.CurrentCulture = original; }
        string[] extensions = [".png", ".jpg", ".jpeg", ".bmp", ".zip", ".litematic", ".schem", ".schematic", ".nbt"];
        string[][] accepted = [[], [".png", ".jpg", ".jpeg", ".bmp"], [".zip"], [".zip"], [".litematic", ".schem", ".schematic", ".nbt"]];
        for (int i = 0; i < 5; i++) {
            foreach (string extension in extensions) foreach (string name in new[] { "resource" + extension, "relative.folder/FILE" + extension.ToUpperInvariant(), extension }) Native.Check((bool)Store("AcceptFile", KindValue(i), name)! == accepted[i].Contains(extension), "Resource extension whitelist changed.");
            foreach (string name in new[] { "resource", "resource.txt", "resource.jar", "file.png:stream", "file.zip\\", "file.nbt.", "file.jpg " }) Native.Check(!(bool)Store("AcceptFile", KindValue(i), name)!, "Resource extension accepted an unlisted type/suffix.");
            Error("AcceptFile", typeof(NullReferenceException), null, KindValue(i), null);
        }
        foreach (int invalidKind in new[] { -1, 5, int.MinValue, int.MaxValue }) { Native.Check(!(bool)Store("AcceptFile", KindValue(invalidKind), "valid.png")!, "Unknown resource kind accepted an extension."); Error("AcceptFile", typeof(NullReferenceException), null, KindValue(invalidKind), null); }
        foreach (char character in Enumerable.Range(0, 32).Select(x => (char)x).Concat("\"<>|")) foreach (int value in new[] { 0, 1, -1 }) Error("AcceptFile", typeof(ArgumentException), null, KindValue(value), "bad" + character + "name.png");
        foreach (string name in new[] { "bad?name.png", "bad*name.PNG", "drive:name.jpg", "file.png:stream.jpg" }) Native.Check((bool)Store("AcceptFile", KindValue(1), name)!, "Extension extraction incorrectly applied ADS/name validation.");
        foreach (string device in new[] { @"\\?\C:\bad|name.png", @"\??\C:\bad|name.png", @"\\.\C:\bad|name.png" }) {
            // Real modern Main runs under its own default path policy, not a CLR4 PowerShell switch.
            Native.Check((bool)Store("AcceptFile", KindValue(1), device)!, "Modern LF device extension policy changed."); Error("AcceptFile", typeof(ArgumentException), null, KindValue(1), "  " + device);
        }
        PackDetails(root, supplementary);
        Console.WriteLine("PASS backup-resources contracts: actual attached DP/page state, enum/field maps, independent Windows names/extensions/messages, cultures and JSON/UTF16 edge cases.");
    }

    private static void PackDetails(string root, string supplementary)
    {
        string folder = Path.Combine(root, "metadata-detail"), file = Path.Combine(folder, "pack.mcmeta"); object entry = New("PCL.InstanceResourceEntry"); Native.Set(entry, "FullPath", folder); Native.Set(entry, "IsDirectory", true);
        string Read() { object?[] values = [entry, "seed"]; Store("ReadPackInfo", values); return (string)values[1]!; }
        foreach (var sample in new[] {
            ("{\"pack\":{\"description\":\"first\\r\\nsecond\\u2028third\",\"pack_format\":46}}", "first second third · 资源格式 46"),
            ("{\"pack\":{\"description\":{\"text\":\"\\nX\\u0085Y\"},\"pack_format\":null}}", " X Y · 资源格式 "),
            ("{\"pack\":{\"description\":{\"text\":1234}}}", "1234"),
            ("{}", "seed"), ("{\"pack\":[]}", "seed"), ("{\"pack\":{\"description\":[]}}", "seed"), ("not JSON", "seed") }) {
            Put(file, sample.Item1); Native.Check(Read() == sample.Item2, "Resource pack ref-detail/newline/swallowed-error contract changed.");
        }
        string longDescription = new string('a', 179) + supplementary; Put(file, new JObject { ["pack"] = new JObject { ["description"] = longDescription } }.ToString()); string actual = Read();
        Native.Check(actual == longDescription[..180] && char.IsHighSurrogate(actual[179]), "Resource detail lost its original 180 UTF16-unit boundary.");
        foreach (string json in new[] { "{\"pack\":null}", "{\"pack\":{\"description\":null}}" }) { Put(file, json); Error("ReadPackInfo", typeof(InvalidOperationException), null, entry, "seed"); }
    }
}
