using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json.Linq;
using PCL;
using static PCLLF.Native.Facade.Tests.BackupResourceChecks;

namespace PCLLF.Native.Facade.Tests;

// All observable Test-SaveBackupIntegration assertions, using actual Native Main/BAML.
internal static class BackupResourcePageChecks
{
    internal static void Run(Scope scope)
    {
        string game = Path.Combine(scope.Root, "page/minecraft/versions/example"), saves = Path.Combine(game, "saves");
        ModMinecraft.McInstance owner = Instance(game);
        foreach (string name in new[] { "Folder A", "Folder B" }) Put(Path.Combine(saves, name, "level.dat"), "synthetic-world");
        List<JObject> points = [Point("Folder A"), Point("Folder B"), Point("Removed Folder")];
        FrameworkElement page = (FrameworkElement)New("PCL.PageInstanceResources", Kind("Saves"));
        Native.Property(page.GetType(), "Owner").SetValue(page, owner);
        object controller = Native.Get(page, "Controller")!;
        Native.Check(controller.GetType().Assembly.GetName().Name == "PCLCS", "Resources are not controlled by the real migrated backend.");
        Native.Set(controller, "WorldBackupPoints", points);
        Native.Call(page.GetType(), "ShowResources", page, saves, saves, Scan(saves, "Saves"));
        IDictionary groups = (IDictionary)Native.Get(controller, "BackupGroups")!;
        StackPanel worlds = Part<StackPanel>(page, "PanWorlds");
        Native.Check(groups.Count == 3 && worlds.Children.Count == 3, "World and orphan-backup groups were not integrated.");
        FrameworkElement Element(string name) => (FrameworkElement)ReadProperty(groups[name]!, "Element");
        FrameworkElement group = Element("Folder A");
        MyCard card = Part<MyCard>(group, "CardBackups"); card.IsSwapped = false; card.BuildContent();
        MyLocalModItem backupRow = (MyLocalModItem)Part<StackPanel>(group, "PanEntries").Children[0];
        backupRow.Checked = true;
        FrameworkElement restore = Part<FrameworkElement>(page, "BtnBackupRestore"), export = Part<FrameworkElement>(page, "BtnExport");
        Native.Check(restore.Visibility == Visibility.Visible && restore.IsEnabled && export.Visibility == Visibility.Collapsed, "Backup selection did not route the shared footer to backup actions.");
        Native.Check((int)ReadProperty(Native.Get(controller, "SelectedPaths")!, "Count") == 0, "World and backup destructive selections were mixed.");
        MyLocalModItem worldRow = worlds.Children.OfType<StackPanel>().First().Children.OfType<MyLocalModItem>().First();
        worldRow.Checked = true;
        Native.Check(((IEnumerable)ReadProperty(group, "SelectedIds")).Cast<string>().Count() == 0 && restore.Visibility == Visibility.Collapsed && export.Visibility == Visibility.Visible, "World selection retained backup deletion targets.");
        Native.Call(page.GetType(), "ChangeAllSelected", page, true);
        Native.Check((int)ReadProperty(Native.Get(controller, "SelectedPaths")!, "Count") == 2, "Select-all included group containers or backup records.");
        MySearchBox search = Part<MySearchBox>(page, "SearchBox"); search.Text = "Removed Folder";
        Native.Check(worlds.Children.Count == 1 && Part<FrameworkElement>(page, "PanEmpty").Visibility == Visibility.Collapsed, "Backup-only worlds became inaccessible to search.");
        FrameworkElement orphan = Element("Removed Folder");
        Native.Check(!Part<FrameworkElement>(orphan, "BtnCreate").IsEnabled && Part<MyCard>(orphan, "CardBackups").Title.Contains("Removed Folder", StringComparison.Ordinal), "Orphan group permits backing up a missing world or lost its title.");
        search.Text = "";
        PageInstanceLeft left = new();
        Native.Check(left.FindName("ItemBackups") is null, "Separate backup sidebar still exists.");
        object legacy = Native.Enum("PCL.FormMain+PageSubType", "InstanceBackups")!;
        object target = Native.Call(left.GetType(), "PageGet", left, legacy)!;
        object navigation = Native.Call(left.GetType(), "NavigationItem", left, legacy)!;
        Native.Check(target.GetType() == page.GetType() && (string)ReadProperty(navigation, "Title") == "存档", "Legacy backup navigation no longer resolves to saves.");
        Part<FrameworkElement>(page, "PanLoad").Visibility = Visibility.Collapsed;
        Part<FrameworkElement>(page, "PanAllBack").Visibility = Visibility.Visible;
        Part<FrameworkElement>(page, "PanAllBack").Opacity = 1;
        group = Element("Folder A");
        Part<MyCard>(group, "CardBackups").IsSwapped = false; Part<MyCard>(group, "CardBackups").BuildContent();
        foreach (object adapter in groups.Values.Cast<object>()) _ = Part<MyCard>((FrameworkElement)ReadProperty(adapter, "Element"), "CardBackups").MainTextBlock;
        _ = Part<MyCard>(Part<FrameworkElement>(page, "BackupSettings"), "CardSettings").MainTextBlock;
        Grid panel = new() { Background = new SolidColorBrush(Color.FromRgb(228, 239, 250)) };
        panel.Children.Add(page); panel.Measure(new Size(780, 760)); panel.Arrange(new Rect(0, 0, 780, 760)); panel.UpdateLayout();
        Native.Check(worlds.Children.Count == 3, "Actual integrated saves/backup controls failed layout.");
        panel.Children.Clear();
        Console.WriteLine("PASS backup-resources page: every original save/backup integration assertion, shared actions, selection exclusivity, orphan search, native headers/settings and legacy navigation.");
    }
}
