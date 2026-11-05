using System.Buffers.Binary;
using System.Collections;
using System.IO.Compression;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PCL;
using PCLCS;
using static PCLLF.Native.Facade.Tests.BackupResourceChecks;

namespace PCLLF.Native.Facade.Tests;

// Actual C# Main pages, events, catalog and controller; no replacement sort algorithm.
internal static class ResourceSortChecks
{
    internal static void Run(string parent)
    {
        string root = Path.Combine(parent, "sort-pages"); Directory.CreateDirectory(root);
        foreach (string kind in new[] { "ResourcePacks", "ShaderPacks" }) CheckPackages(root, kind);
        CheckWorlds(root);
        CheckScreenshots(root);
        Console.WriteLine("PASS native resource-sort: material/shader default and creation order, real combo/filter/search/selection, LastPlayed worlds/unknowns, screenshots and recycle guard.");
    }
    private static void CheckPackages(string root, string kind)
    {
        string folder = Path.Combine(root, kind); Directory.CreateDirectory(folder);
        string newer = Path.Combine(folder, "needle-newer.zip"), older = Path.Combine(folder, "needle-older.zip"), middle = Path.Combine(folder, "folder-pack");
        foreach (string file in new[] { newer, older }) {
            using ZipArchive archive = ZipFile.Open(file, ZipArchiveMode.Create);
            using StreamWriter text = new(archive.CreateEntry(kind == "ResourcePacks" ? "pack.mcmeta" : "shaders/fixture.vsh").Open());
            text.Write(kind == "ResourcePacks" ? "{\"pack\":{\"pack_format\":46,\"description\":\"sort fixture\"}}" : "// fixture");
        }
        Directory.CreateDirectory(middle);
        if (kind == "ResourcePacks") File.WriteAllText(Path.Combine(middle, "pack.mcmeta"), "{\"pack\":{\"pack_format\":46,\"description\":\"folder\"}}");
        else Directory.CreateDirectory(Path.Combine(middle, "shaders"));
        File.SetCreationTimeUtc(older, Time(2001)); Directory.SetCreationTimeUtc(middle, Time(2002)); File.SetCreationTimeUtc(newer, Time(2003));
        IList entries = Scan(folder, kind);
        var page = Page(kind); Show(page, folder, entries);
        ComboBox combo = Part<ComboBox>(page, "ComboSort");
        Native.Check(combo.Items.Cast<ComboBoxItem>().Count(item => item.Visibility != Visibility.Collapsed) == 3, "Non-world resources expose LastPlayed modes.");
        string[] original = Names(page, "PanList");
        MyLocalModItem selected = Part<Panel>(page, "PanList").Children.OfType<MyLocalModItem>().First(); selected.Checked = true;
        object entry = Native.Get(selected, "ResourceEntry")!;
        combo.SelectedIndex = 1;
        Order(page, "PanList", "needle-newer.zip", "folder-pack", "needle-older.zip");
        Native.Check(((HashSet<string>)Native.Get(Controller(page), "SelectedPaths")!).Contains(((InstanceResourceEntryData)entry).FullPath!) && selected.Checked,
            "Resource sorting lost the actual selected row or path.");
        foreach (int hiddenWorldMode in new[] { 3, 4 }) {
            combo.SelectedIndex = hiddenWorldMode;
            Native.Check(combo.SelectedIndex == 1 && (int)Native.Property(Controller(page).GetType(), "SortMode").GetValue(Controller(page))! == 1,
                "A hidden world-only combo item escaped the non-world resource page guard.");
            Order(page, "PanList", "needle-newer.zip", "folder-pack", "needle-older.zip");
            Native.Check(selected.Checked && ((HashSet<string>)Native.Get(Controller(page), "SelectedPaths")!).Contains(((InstanceResourceEntryData)entry).FullPath!),
                "Rejecting a hidden LastPlayed item lost the selected resource.");
        }
        combo.SelectedIndex = 2; Order(page, "PanList", "needle-older.zip", "folder-pack", "needle-newer.zip");
        Part<MyRadioButton>(page, "BtnFilterFolders").Checked = true; Order(page, "PanList", "folder-pack");
        Part<MyRadioButton>(page, "BtnFilterAll").Checked = true;
        MySearchBox search = Part<MySearchBox>(page, "SearchBox"); search.Text = "needle";
        Native.Call(Controller(page).GetType(), "SearchChanged", Controller(page));
        Order(page, "PanList", "needle-older.zip", "needle-newer.zip");
        combo.SelectedIndex = 1; Order(page, "PanList", "needle-newer.zip", "needle-older.zip");
        search.Text = ""; Native.Call(Controller(page).GetType(), "SearchChanged", Controller(page));
        Show(page, folder, Scan(folder, kind));
        Order(page, "PanList", "needle-newer.zip", "folder-pack", "needle-older.zip");
        Native.Check(combo.SelectedIndex == 1, "Reloading resource results reset its selected sort mode.");
        combo.SelectedIndex = 0; Native.Check(Names(page, "PanList").SequenceEqual(original), "Default no longer preserves the original catalog order.");
        Render(page, Path.Combine(folder, "compact-header.png"));
        Rect comboBounds = combo.TransformToAncestor(page).TransformBounds(new Rect(combo.RenderSize));
        Rect listBounds = Part<Panel>(page, "PanResourceList").TransformToAncestor(page).TransformBounds(new Rect(Part<Panel>(page, "PanResourceList").RenderSize));
        Native.Check(comboBounds.Height > 0 && comboBounds.Right <= page.ActualWidth + 1 && comboBounds.Bottom <= listBounds.Top + 1,
            "Compact resource header clipped the combo or overlapped content.");
        Native.Set(Controller(page), "IsRecycleMode", true);
        Show(page, folder, entries);
        Native.Check(!combo.IsEnabled && (combo.ToolTip?.ToString() ?? "").Contains("删除", StringComparison.Ordinal), "Recycle records incorrectly expose installation/play-time sorting.");
        Native.Set(Controller(page), "IsRecycleMode", false); Show(page, folder, entries);
        Native.Check(combo.IsEnabled, "Leaving recycle mode failed to restore resource sorting.");
    }
    private static void CheckWorlds(string root)
    {
        string folder = Path.Combine(root, "Saves"); Directory.CreateDirectory(folder);
        string older = World(folder, "older-play", Time(2017)), newer = World(folder, "newer-play", Time(2019)), unknown = World(folder, "unknown-play", null);
        Directory.SetLastWriteTimeUtc(newer, Time(2001)); Directory.SetLastWriteTimeUtc(older, Time(2025)); Directory.SetLastWriteTimeUtc(unknown, Time(2026));
        IList worlds = Scan(folder, "Saves");
        Native.Check(worlds.Count == 3 && worlds.Cast<InstanceResourceEntryData>().Single(entry => entry.Name == "unknown-play").LastPlayedUtc is null,
            "Missing world metadata vanished or acquired a made-up play time.");
        FrameworkElement page = Page("Saves");
        object host = Native.Get(Controller(page), "Host")!;
        Native.Set(host, "QuickPlaySupported", new Func<ModMinecraft.McInstance, bool>(_ => false));
        Show(page, folder, worlds);
        ComboBox combo = Part<ComboBox>(page, "ComboSort");
        Native.Check(combo.Items.Cast<ComboBoxItem>().Count(item => item.Visibility != Visibility.Collapsed) == 5, "Worlds lack LastPlayed choices.");
        combo.SelectedIndex = 3; Order(page, "PanWorlds", "newer-play", "older-play", "unknown-play");
        combo.SelectedIndex = 4; Order(page, "PanWorlds", "older-play", "newer-play", "unknown-play");
        Native.Call(Controller(page).GetType(), "SearchChanged", Controller(page));
        Order(page, "PanWorlds", "older-play", "newer-play", "unknown-play");
        Native.Check(Directory.GetLastWriteTimeUtc(newer) == Time(2001), "Sorting wrote the world folder or used its modification time as LastPlayed.");
    }
    private static void CheckScreenshots(string root)
    {
        string folder = Path.Combine(root, "Screenshots"); Directory.CreateDirectory(folder);
        foreach (var value in new[] { ("old.png", 2001), ("new.png", 2003) }) {
            string file = Path.Combine(folder, value.Item1);
            BitmapSource source = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null, new byte[] { 10, 20, 30, 255 }, 4);
            PngBitmapEncoder encoder = new(); encoder.Frames.Add(BitmapFrame.Create(source)); using (FileStream stream = File.Create(file)) encoder.Save(stream);
            File.SetCreationTimeUtc(file, Time(value.Item2));
        }
        FrameworkElement page = Page("Screenshots"); Show(page, folder, Scan(folder, "Screenshots"));
        ComboBox combo = Part<ComboBox>(page, "ComboSort"); combo.SelectedIndex = 1;
        Order(page, "PanScreenshots", "new.png", "old.png"); combo.SelectedIndex = 2;
        Order(page, "PanScreenshots", "old.png", "new.png");
    }
    private static FrameworkElement Page(string kind) => (FrameworkElement)New("PCL.PageInstanceResources", Kind(kind));
    private static object Controller(FrameworkElement page) => Native.Get(page, "Controller")!;
    private static void Show(FrameworkElement page, string root, IList entries) => Native.Call(page.GetType(), "ShowResources", page, root, root, entries);
    private static string[] Names(FrameworkElement page, string part) => Part<Panel>(page, part).Children.Cast<FrameworkElement>().Select(element => {
        if (part == "PanWorlds" && element is Panel panel) element = panel.Children.OfType<MyLocalModItem>().First();
        object entry = Native.Get(element, part == "PanScreenshots" ? "Entry" : "ResourceEntry")!;
        return ((InstanceResourceEntryData)entry).Name!;
    }).ToArray();
    private static void Order(FrameworkElement page, string part, params string[] expected) => Native.Check(Names(page, part).SequenceEqual(expected),
        "Real " + part + " order differs: " + string.Join(",", Names(page, part)));
    private static DateTime Time(int year) => new(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static string World(string root, string name, DateTime? played) {
        string folder = Path.Combine(root, name); Directory.CreateDirectory(folder);
        if (played is null) { File.WriteAllText(Path.Combine(folder, "level.dat"), "damaged world metadata"); return folder; }
        using FileStream file = File.Create(Path.Combine(folder, "level.dat")); using GZipStream zip = new(file, CompressionMode.Compress);
        Tag(10, ""); Tag(10, "Data"); Tag(4, "LastPlayed");
        Span<byte> time = stackalloc byte[8]; BinaryPrimitives.WriteInt64BigEndian(time, new DateTimeOffset(played.Value).ToUnixTimeMilliseconds()); zip.Write(time);
        zip.WriteByte(0); zip.WriteByte(0); return folder;
        void Tag(byte kind, string value) { zip.WriteByte(kind); byte[] text = Encoding.UTF8.GetBytes(value); zip.WriteByte((byte)(text.Length >> 8)); zip.WriteByte((byte)text.Length); zip.Write(text); }
    }
    private static void Render(FrameworkElement page, string file) {
        Grid host = new(); host.Children.Add(page);
        try {
            host.Measure(new Size(470, 650)); host.Arrange(new Rect(0, 0, 470, 650)); host.UpdateLayout();
            RenderTargetBitmap pixels = new(470, 650, 96, 96, PixelFormats.Pbgra32); pixels.Render(host);
            PngBitmapEncoder png = new(); png.Frames.Add(BitmapFrame.Create(pixels)); using FileStream target = File.Create(file); png.Save(target);
        } finally { host.Children.Clear(); }
    }
}
