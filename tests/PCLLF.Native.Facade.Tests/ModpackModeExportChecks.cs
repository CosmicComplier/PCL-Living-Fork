using System.Collections;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;
using PCL;
using PCLCS;

namespace PCLLF.Native.Facade.Tests;
internal static partial class ModpackModeChecks {
    private static void CheckExportMode(Scope scope, PageInstanceExport export) {
        foreach (bool enabled in new[] { true,false }) {
            string destination = Path.Combine(scope.Root,"mode-export-" + enabled), config = Path.Combine(destination,"PCL","Setup.ini");
            Put(config,"Unrelated:preserve\r\nLivingModpackMode:" + (!enabled) + "\r\n"); Call("ConfigureExport",destination,true,enabled); string text = File.ReadAllText(config);
            M(194,text.Contains("Unrelated:preserve",StringComparison.Ordinal) && text.Contains("LivingModpackExported:True",StringComparison.Ordinal) && text.Contains("LivingModpackMode:" + enabled,StringComparison.Ordinal));
        }
        string without = Path.Combine(scope.Root,"no-launcher"); Call("ConfigureExport",without,false,true); M(198,!Directory.Exists(without)); M(199,Equals(Cached("LivingModpackMode"),false));
        Native.Check(!export.IsLoaded, "Mode export page was loaded or started a real export.");
    }
    private static void CheckComponents(Scope scope, FrameworkElement overview) {
        string mods = Path.Combine(scope.Root,"component-mods"); Directory.CreateDirectory(mods);
        foreach (var item in new[] { ("renamed.jar","fabric-api","0.155.2"),("unrelated.jar","fixture-only","1.0") }) {
            using ZipArchive zip = ZipFile.Open(Path.Combine(mods,item.Item1),ZipArchiveMode.Create); using StreamWriter writer = new(zip.CreateEntry("fabric.mod.json").Open()); writer.Write("{\"id\":\"" + item.Item2 + "\",\"version\":\"" + item.Item3 + "\"}");
        }
        Put(Path.Combine(mods,"broken.jar"),"broken-archive"); Put(Path.Combine(mods,"inactive.jar.disabled"),"inactive");
        Type summary = Native.Type("PCL.InstalledComponentSummary"); IDictionary local = (IDictionary)Native.Call(summary,"ReadLocalMods",null,mods)!; M(208,local.Count == 1 && (string)local["fabric-api"]! == "0.155.2");
        McVersion version = new("26.2",true); version.ModLoaderVersions[ModLoaders.Fabric] = "0.19.3";
        object rows = Native.Call(summary,"Describe",null,version,null,local)!; object[] items = ((IEnumerable)rows).Cast<object>().ToArray(); M(212,string.Join(',',items.Select(item => (string)Native.Get(item,"Name")!)) == "Minecraft,Fabric,Fabric API");
        Native.Check(items.All(item => item.GetType().Assembly == Native.Main), "Installed-component cards do not use genuine Main records.");
        scope.SetCache("LivingModpackMode",true); Call("ApplyInstanceVisibility"); Native.Call(overview.GetType(),"ShowInstalledComponents",overview,rows);
        UIElementCollection cards = Part<Panel>(overview,"PanInstalledComponents").Children; M(217,cards.Count == 3 && cards.Cast<UIElement>().All(card => !card.IsHitTestVisible));
    }
}
