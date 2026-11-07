using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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
        List<ResourceVersion> fixtureVersions = new() {
            new() { Id = "new-version", Version = "14.1.2", Display = "14.1.2", ReleaseDate = new DateTime(2026, 11, 6), ReleaseType = ResourceVersion.ReleaseTypes.Release, GameVersions = new() { "26.2" } },
            new() { Id = "current-version", Version = "12.3.0", Display = "12.3.0", ReleaseDate = new DateTime(2026, 10, 1), ReleaseType = ResourceVersion.ReleaseTypes.Release, GameVersions = new() { "1.21.11" } },
            new() { Id = "beta-version", Version = "14.2.0-beta.1", Display = "14.2.0-beta.1", ReleaseDate = new DateTime(2026, 11, 7), ReleaseType = ResourceVersion.ReleaseTypes.Beta, GameVersions = new() { "26.2" } },
            new() { Id = "alpha-version", Version = "15.0.0-alpha.1", Display = "15.0.0-alpha.1", ReleaseDate = new DateTime(2026, 11, 8), ReleaseType = ResourceVersion.ReleaseTypes.Alpha, GameVersions = new() { "26.3" } },
            new() { Id = "unknown-version", Version = "16.0.0-preview", Display = "16.0.0-preview", ReleaseDate = new DateTime(2026, 11, 9), ReleaseTypeCode = 0, GameVersions = new() { "26.3" } }
        };
        List<ResourceVersion> sourceVersions = fixtureVersions;
        Native.Set(host, "Versions", new Func<string, ResourcePlatforms, bool, List<ResourceVersion>>((_, _, _) => sourceVersions.ToList()));
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
            instance.Logo = @"PCL\Logo.png"; // Missing relative custom artwork must not break the record/catalog.
            page.Refresh();
            VisualAssetChecks.Pump(() => ((TextBlock)page.FindName("LabListStatus")).Text.StartsWith("已获取", StringComparison.Ordinal), "Recorded pack catalog did not refresh.", 5000);
            Native.Check(version.Text == "12.3.0" && ((MyButton)page.FindName("BtnOnline")).IsEnabled, "Recognized pack lost its version or online update entry.");
            var versions = (StackPanel)page.FindName("PanVersions");
            var release = (MyRadioButton)page.FindName("RadioRelease");
            var beta = (MyRadioButton)page.FindName("RadioBeta");
            var alpha = (MyRadioButton)page.FindName("RadioAlpha");
            var all = (MyRadioButton)page.FindName("RadioAll");
            var search = (MySearchBox)page.FindName("TextSearchVersion");
            var empty = (TextBlock)page.FindName("LabListEmpty");
            MyListItem[] Rows() => versions.Children.OfType<MyListItem>().ToArray();
            void CheckCurrent(string? title) {
                var marked = Rows().Where(Current).ToArray();
                Native.Check(marked.Length == (title is null ? 0 : 1) && (title is null || marked[0].Title == title && HasBadge(marked[0], "当前版本")), "Filtering lost or misidentified the current version frame/badge.");
            }
            void CheckCounts(int releases, int betas, int alphas, int total) => Native.Check(
                release.Text == "正式版（" + releases + "）" && beta.Text == "测试版（" + betas + "）" && alpha.Text == "早期测试版（" + alphas + "）" && all.Text == "全部（" + total + "）",
                "Release channel counts did not follow the current search scope.");
            void RefreshManifest(JObject data) {
                File.WriteAllText(Path.Combine(instance.PathVersion, "PCL", "ModpackOrigin.json"), data.ToString());
                page.Refresh();
                VisualAssetChecks.Pump(() => ((TextBlock)page.FindName("LabListStatus")).Text.StartsWith("已获取", StringComparison.Ordinal), "Changed origin/catalog did not refresh.", 5000);
            }
            Native.Check(release.Checked && Rows().Select(row => row.Title).SequenceEqual(new[] { "14.1.2", "12.3.0" }), "Release was not the default channel or the current version was pinned above a newer release.");
            CheckCurrent("12.3.0"); CheckCounts(2, 1, 1, 5);
            beta.Checked = true;
            Native.Check(!release.Checked && Rows().Length == 1 && Rows()[0].Title == "14.2.0-beta.1", "Beta did not select only beta versions.");
            CheckCurrent(null);
            alpha.Checked = true;
            Native.Check(Rows().Length == 1 && Rows()[0].Title == "15.0.0-alpha.1", "Alpha did not select only early test versions.");
            all.Checked = true;
            Native.Check(Rows().Select(row => row.Title).SequenceEqual(new[] { "16.0.0-preview", "15.0.0-alpha.1", "14.2.0-beta.1", "14.1.2", "12.3.0" }) && HasBadge(Rows()[0], "发布类型未知"), "All channels lost date order or mislabeled an unknown release type.");
            CheckCurrent("12.3.0");
            search.Text = "26.2";
            CheckCounts(1, 1, 0, 2);
            Native.Check(Rows().Length == 2, "Minecraft search did not intersect the selected channel.");
            alpha.Checked = true;
            Native.Check(Rows().Length == 0 && empty.Visibility == Visibility.Visible && empty.Text == "没有找到符合搜索条件的早期测试版。", "An empty searched channel did not explain its own result.");
            search.Text = "BETA"; beta.Checked = true;
            CheckCounts(0, 1, 0, 1);
            Native.Check(Rows().Length == 1 && Rows()[0].Title == "14.2.0-beta.1" && empty.Visibility == Visibility.Collapsed, "Version search lost case-insensitivity or retained the empty state.");
            search.Text = "12.3"; release.Checked = true;
            CheckCounts(1, 0, 0, 1); CheckCurrent("12.3.0");
            all.Checked = true; CheckCurrent("12.3.0");
            search.Text = ""; release.Checked = true;
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(BitmapSource.Create(2, 1, 96, 96, PixelFormats.Bgra32, null, new byte[] { 40, 160, 255, 255, 40, 160, 255, 255 }, 8)));
            using (var imageFile = File.Create(Path.Combine(instance.PathVersion, "PCL", "valid-logo.png"))) encoder.Save(imageFile);
            instance.Logo = @"PCL\valid-logo.png"; page.Refresh();
            VisualAssetChecks.Pump(() => ((TextBlock)page.FindName("LabListStatus")).Text.StartsWith("已获取", StringComparison.Ordinal), "Relative custom logo blocked the catalog.", 5000);
            Native.Check(((Image)page.FindName("ImgPack")).Source is BitmapSource bitmap && bitmap.PixelWidth == 2 && bitmap.PixelHeight == 1, "Custom logo was not resolved relative to the selected instance.");
            File.WriteAllText(Path.Combine(instance.PathVersion, "PCL", "broken-logo.png"), "invalid image");
            instance.Logo = @"PCL\broken-logo.png"; page.Refresh();
            VisualAssetChecks.Pump(() => ((TextBlock)page.FindName("LabListStatus")).Text.StartsWith("已获取", StringComparison.Ordinal), "Broken artwork blocked the catalog.", 5000);
            Native.Check(version.Text == "12.3.0" && versions.Children.Count == 2, "Artwork failure was misreported as a record error.");
            search.Text = "12.3";
            Native.Check(versions.Children.Count == 1, "Pack version search did not filter rows.");
            CheckCurrent("12.3.0"); search.Text = "";
            var betaOrigin = (JObject)manifest.DeepClone();
            betaOrigin["source"]!["version"] = "beta-version"; betaOrigin["version"] = "14.1.2";
            RefreshManifest(betaOrigin);
            CheckCurrent(null); // The displayed origin name intentionally matches a different release ID.
            beta.Checked = true; CheckCurrent("14.2.0-beta.1");
            betaOrigin["version"] = "14.2.0-beta.1"; RefreshManifest(betaOrigin);
            Capture(page, "pack-update-beta-current.png", 820, 820);
            var unknownOrigin = (JObject)manifest.DeepClone();
            unknownOrigin["source"]!["version"] = "unknown-version"; unknownOrigin["version"] = "16.0.0-preview";
            release.Checked = true; RefreshManifest(unknownOrigin);
            CheckCounts(2, 1, 1, 5); CheckCurrent(null);
            all.Checked = true; CheckCurrent("16.0.0-preview");
            Native.Check(HasBadge(Rows()[0], "发布类型未知"), "A current version of unknown type was assigned a release channel.");
            var localOrigin = (JObject)manifest.DeepClone();
            localOrigin["source"]!["version"] = "no-longer-listed";
            release.Checked = true; RefreshManifest(localOrigin);
            CheckCounts(2, 1, 1, 6); CheckCurrent(null);
            Native.Check(((TextBlock)page.FindName("LabListStatus")).Text.Contains("本地记录", StringComparison.Ordinal), "A local-only current record did not explain why it is outside release channels.");
            all.Checked = true; CheckCurrent("12.3.0");
            Native.Check(Rows().Last().Info.Contains("本地整合包记录", StringComparison.Ordinal) && Rows().Last().Info.Contains("发布类型未记录", StringComparison.Ordinal), "A current version missing from its source catalog was mislabeled as a release.");
            search.Text = "1.21.11";
            CheckCounts(1, 0, 0, 2); CheckCurrent("12.3.0");
            search.Text = "no such version";
            CheckCounts(0, 0, 0, 0);
            Native.Check(Rows().Length == 0 && empty.Visibility == Visibility.Visible && empty.Text == "没有找到符合搜索条件的版本。", "Local-only records escaped the search filter.");
            search.Text = ""; release.Checked = true;
            sourceVersions = fixtureVersions.Where(item => item.ReleaseTypeCode is 2 or 3).ToList();
            RefreshManifest(manifest);
            Native.Check(release.Checked && Rows().Length == 0 && empty.Text == "暂无正式版。", "A catalog without releases changed the default channel or gave an inaccurate empty state.");
            CheckCounts(0, 1, 1, 3);
            sourceVersions = fixtureVersions;
            RefreshManifest(manifest); CheckCurrent("12.3.0");
            bool rejected = false;
            try { Native.Call(engine.GetType(), "ChooseOnline", engine, manifest, root, CancellationToken.None, null, null, "foreign-version"); }
            catch (IOException) { rejected = true; }
            Native.Check(rejected, "A selected version absent from the recorded project was accepted.");
            var speed = new PageSpeedLeft();
            Native.Check(speed.FindName("LabReadSpeed") is TextBlock && speed.FindName("LabWriteSpeed") is TextBlock, "Task manager lacks separate file read/write displays.");
            Capture(page, "pack-update-page.png", 820, 820);
            alpha.Checked = true; Capture(page, "pack-update-alpha.png", 820, 820);
            all.Checked = true; Capture(page, "pack-update-all.png", 820, 1000);
            search.Text = "26.2"; alpha.Checked = true; Capture(page, "pack-update-channel-empty.png", 820, 820);
            search.Text = ""; release.Checked = true;
            CheckNarrowFilters(page);
            Capture(page, "pack-update-channels-narrow.png", 430, 1000);
            Capture(speed, "file-io-sidebar.png", 200, 550);
            Console.WriteLine("PASS pack update sidebar: release/beta/alpha/all, searched counts/intersections/empty states, date order, exact-ID current frame/badge, unknown/local origins, relative artwork and narrow native filters; actual instance navigation, local update and read/write task labels.");
        } finally { Native.Set(host, "Project", previousProject); Native.Set(host, "Versions", previousVersions); PageInstanceLeft.Instance = previous; }
    }
    private static bool Current(MyListItem row) => row.Children.OfType<Border>().Any(border => Equals(border.Tag, "current-pack-version"));
    private static bool HasBadge(MyListItem row, string text) => row.Inlines.OfType<InlineUIContainer>().Any(inline => inline.Child is Border { Child: TextBlock label } && label.Text == text);
    private static void CheckNarrowFilters(PageInstancePackUpdate page) {
        var surface = new Border { Child = page };
        try {
            surface.Measure(new Size(430, 1000)); surface.Arrange(new Rect(0, 0, 430, 1000)); surface.UpdateLayout();
            var filters = (WrapPanel)page.FindName("PanReleaseTypes");
            var buttons = filters.Children.OfType<MyRadioButton>().ToArray();
            Native.Check(buttons.Any(button => button.TranslatePoint(new Point(), filters).Y > buttons[0].TranslatePoint(new Point(), filters).Y) && buttons.All(button => button.ActualWidth >= button.LabText.DesiredSize.Width && button.ActualWidth <= filters.ActualWidth), "Native release filters did not wrap with readable labels in the actual 430px page layout.");
        } finally { surface.Child = null; }
    }
    private static void Capture(FrameworkElement element, string name, int width, int height) {
        string? folder = Environment.GetEnvironmentVariable("PCLLF_MODPACK_PREVIEW_DIR");
        if (string.IsNullOrWhiteSpace(folder)) return;
        var surface = new Border { Background = Brushes.White, Child = element };
        Window? window = null; var app = System.Windows.Application.Current; var previousMain = app.MainWindow; var previousMode = app.ShutdownMode;
        try {
        if (element is PageInstancePackUpdate page) {
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            window = new Window { Content = surface, Width = width, Height = height, Left = -30000, Top = -30000, ShowInTaskbar = false, ShowActivated = false, WindowStyle = WindowStyle.None };
            window.Show();
            VisualAssetChecks.Pump(() => page.IsLoaded && ((TextBlock)page.FindName("LabListStatus")).Text.StartsWith("已获取", StringComparison.Ordinal), "Visible pack page did not finish loading.", 5000);
        }
        surface.Measure(new Size(width, height)); surface.Arrange(new Rect(0, 0, width, height)); surface.UpdateLayout();
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(surface);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        Directory.CreateDirectory(folder); using var output = File.Create(Path.Combine(folder, name)); encoder.Save(output);
        } finally {
            surface.Child = null;
            try { if (window is not null) { window.Content = null; window.Close(); VisualAssetChecks.Drain(); } }
            finally { app.MainWindow = previousMain; app.ShutdownMode = previousMode; }
        }
    }
}
