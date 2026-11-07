using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Newtonsoft.Json.Linq;
using PCL;
using PCLCS;

namespace PCLLF.Native.Facade.Tests;

internal static class ModpackSidebarChecks {
    internal static void Run(string root) {
        var previous = PageInstanceLeft.Instance;
        object engine = Native.Field(Native.Type("PCL.ModpackUpdateFlow"), "Engine").GetValue(null)!;
        object host = Native.Get(engine, "host")!;
        object previousProject = Native.Get(host, "Project")!, previousVersions = Native.Get(host, "Versions")!;
        Native.Set(host, "Project", new Func<string, ResourcePlatforms, ResourceProjectModelData>((id, platform) => new ResourceProject(new JObject {
            ["Id"] = id, ["Slug"] = id, ["Types"] = (int)ResourceTypes.ModPack, ["Platform"] = (int)platform,
            ["RawName"] = "Fixture pack", ["Description"] = "Fixture", ["Website"] = "https://example.invalid/pack", ["LastUpdate"] = DateTime.UtcNow, ["DownloadCount"] = 1, ["Tags"] = new JArray()
        })));
        Native.Set(host, "Versions", new Func<string, ResourcePlatforms, bool, List<ResourceVersion>>((_, _, _) => new() {
            new() { Id = "new-version", Version = "14.1.2", Display = "14.1.2", ReleaseDate = new DateTime(2026, 11, 6), ReleaseType = ResourceVersion.ReleaseTypes.Release, GameVersions = new() { "26.2" } },
            new() { Id = "current-version", Version = "12.3.0", Display = "12.3.0", ReleaseDate = new DateTime(2026, 10, 1), ReleaseType = ResourceVersion.ReleaseTypes.Release, GameVersions = new() { "1.21.11" } }
        }));
        try {
            var instance = new ModMinecraft.McInstance(Path.Combine(root, "sidebar-instance"));
            Directory.CreateDirectory(instance.PathVersion);
            FileTaskScope.CacheInstance(instance);
            PageInstanceLeft.Instance = instance;
            var left = new PageInstanceLeft();
            var item = (MyListItem)left.FindName("ItemPackUpdate");
            Native.Check(item.Title == "整合包更新" && Convert.ToInt32(item.Tag) == 15, "Pack update sidebar entry is missing.");
            var page = (PageInstancePackUpdate)Native.Call(typeof(PageInstanceLeft), "PageGet", left, FormMain.PageSubType.InstancePackUpdate)!;
            page.Refresh();
            var version = (TextBlock)page.FindName("LabVersion");
            VisualAssetChecks.Pump(() => version.Text != "正在读取…", "Pack update sidebar did not finish loading its instance record.", 5000);
            Native.Check(((MyButton)page.FindName("BtnLocal")).IsEnabled && !((MyButton)page.FindName("BtnOnline")).IsEnabled, "Missing origin did not expose local repair or incorrectly enabled online lookup.");
            object package = Activator.CreateInstance(Native.Type("PCL.PackUpdatePackage"), true)!;
            Native.Set(package, "Name", "Fabulously Optimized"); Native.Set(package, "Version", "12.3.0"); Native.Set(package, "Minecraft", "1.21.11");
            Native.Set(package, "Source", new JObject { ["platform"] = "Modrinth", ["project"] = "fixture-project", ["version"] = "current-version" });
            JObject manifest = (JObject)Native.Static("PCL.ModpackUpdate", "Manifest", package)!;
            Directory.CreateDirectory(Path.Combine(instance.PathVersion, "PCL"));
            File.WriteAllText(Path.Combine(instance.PathVersion, "PCL", "ModpackOrigin.json"), manifest.ToString());
            page.Refresh();
            VisualAssetChecks.Pump(() => ((TextBlock)page.FindName("LabListStatus")).Text.StartsWith("已获取", StringComparison.Ordinal), "Recorded pack catalog did not refresh.", 5000);
            Native.Check(version.Text == "12.3.0" && ((MyButton)page.FindName("BtnOnline")).IsEnabled, "Recognized pack lost its version or online update entry.");
            var versions = (StackPanel)page.FindName("PanVersions");
            Native.Check(versions.Children.Count == 2 && ((MyListItem)versions.Children[0]).Title == "14.1.2" && ((MyListItem)versions.Children[1]).Children.OfType<Border>().Any(border => Equals(border.Tag, "current-pack-version")), "Catalog order or current version marker is incorrect.");
            ((MySearchBox)page.FindName("TextSearchVersion")).Text = "12.3";
            Native.Check(versions.Children.Count == 1, "Pack version search did not filter rows.");
            ((MySearchBox)page.FindName("TextSearchVersion")).Text = "";
            bool rejected = false;
            try { Native.Call(engine.GetType(), "ChooseOnline", engine, manifest, root, CancellationToken.None, null, null, "foreign-version"); }
            catch (IOException) { rejected = true; }
            Native.Check(rejected, "A selected version absent from the recorded project was accepted.");
            var speed = new PageSpeedLeft();
            Native.Check(speed.FindName("LabReadSpeed") is TextBlock && speed.FindName("LabWriteSpeed") is TextBlock, "Task manager lacks separate file read/write displays.");
            Capture(page, "pack-update-page.png", 820, 600);
            Capture(speed, "file-io-sidebar.png", 200, 550);
            Console.WriteLine("PASS pack update sidebar: actual instance navigation, missing-record state, local update entry and read/write task labels.");
        } finally { Native.Set(host, "Project", previousProject); Native.Set(host, "Versions", previousVersions); PageInstanceLeft.Instance = previous; }
    }
    private static void Capture(FrameworkElement element, string name, int width, int height) {
        string? folder = Environment.GetEnvironmentVariable("PCLLF_MODPACK_PREVIEW_DIR");
        if (string.IsNullOrWhiteSpace(folder)) return;
        var surface = new Border { Background = Brushes.White, Child = element };
        Window? window = null; var app = System.Windows.Application.Current; var previousMain = app.MainWindow; var previousMode = app.ShutdownMode;
        if (element is PageInstancePackUpdate page) {
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            window = new Window { Content = surface, Width = width, Height = height, Left = -30000, Top = -30000, ShowInTaskbar = false, ShowActivated = false, WindowStyle = WindowStyle.None };
            window.Show();
            VisualAssetChecks.Pump(() => page.IsLoaded && ((TextBlock)page.FindName("LabListStatus")).Text.StartsWith("已获取", StringComparison.Ordinal), "Visible pack page did not finish loading.", 5000);
        }
        try {
        surface.Measure(new Size(width, height)); surface.Arrange(new Rect(0, 0, width, height)); surface.UpdateLayout();
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(surface);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        Directory.CreateDirectory(folder); using var output = File.Create(Path.Combine(folder, name)); encoder.Save(output);
        surface.Child = null;
        } finally { if (window is not null) { window.Content = null; window.Close(); } app.MainWindow = previousMain; app.ShutdownMode = previousMode; }
    }
}
