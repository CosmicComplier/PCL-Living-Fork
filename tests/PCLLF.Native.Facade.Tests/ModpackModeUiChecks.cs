using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PCL;

namespace PCLLF.Native.Facade.Tests;
internal static partial class ModpackModeChecks {
    private static (PageInstanceExport Export, FrameworkElement Overview) CheckUi(Scope scope) {
        object marker = Entry("LivingModpackExported"), entry = Entry("LivingModpackMode");
        M(62, Native.Get(marker, "Source")!.ToString() == "Normal" && Equals(Native.Get(marker, "DefaultValue"), false));
        scope.SetCache("LivingModpackMode", true); M(64, !Enabled); scope.SetCache("LivingModpackMode", false); scope.SetCache("LivingModpackExported", true);
        CheckChangeConfirmation(); M(68, Native.Get(entry, "Source")!.ToString() == "Normal" && Equals(Native.Get(entry, "DefaultValue"), false));
        PageSetupLeft left = scope.Keep(new PageSetupLeft()); PageLaunchLeft launch = scope.Keep(new PageLaunchLeft()); scope.Global("FrmSetupLeft", left); scope.Global("FrmLaunchLeft", launch);
        Dictionary<string,FrameworkElement> instances = new(StringComparer.Ordinal);
        foreach (string name in new[] { "PageInstanceLeft", "PageInstanceOverall", "PageInstanceMod", "PageInstanceModDisabled", "PageInstanceSetup", "PageSetupLaunch" }) { FrameworkElement view = scope.New(name); instances.Add(name, view); scope.Global("Frm" + name.Substring(4), view); }
        FrameworkElement overview = instances["PageInstanceOverall"];
        MyListItem fakeHeader = new() { Title = "26.2-Fabric 0.19.3", Info = "synthetic header details", Height = 42, Logo = "pack://application:,,,/LivingPCL;component/Images/Blocks/Fabric.png" };
        Native.Set(overview, "InstanceHeaderItem", fakeHeader); Native.Set(overview, "OriginalHeaderInfo", "synthetic header details"); Part<Panel>(overview, "PanDisplayItem").Children.Add(fakeHeader);
        StackPanel header = new(); MyRadioButton[] buttons = Enumerable.Range(0,5).Select(_ => new MyRadioButton()).ToArray(); foreach (MyRadioButton button in buttons) header.Children.Add(button);
        Dictionary<string,object?> preferences = Native.Entries.Keys.Cast<string>().Where(key => key.StartsWith("UiHidden", StringComparison.Ordinal)).ToDictionary(key => key, Cached, StringComparer.Ordinal);
        FrameworkElement? compact = null;
        foreach (bool force in new[] { false, true }) {
            Native.Field(Native.Type("PCL.PageSetupUI"), "_HiddenForceShow").SetValue(null, force); scope.SetCache("LivingModpackMode", true); CheckModWarnings(true);
            M(102, (bool)Call("CanChangeMinecraft", "26.2", "26.2")! && !(bool)Call("CanChangeMinecraft", "26.2", "26.1")!);
            M(104, (string)Call("DisplayTitle", "LivingPCL")! == "LivingPCL - 整合包模式" && (string)Call("DisplayTitle", "Custom")! == "Custom - 整合包模式");
            Call("ApplyVisibility", new object?[] { null }); Call("ApplyHeader", header, buttons[1], buttons[2], buttons[3], buttons[4]);
            M(107, buttons[1].Visibility == Visibility.Collapsed && buttons[4].Visibility == Visibility.Collapsed && buttons[3].Visibility == Visibility.Visible && header.Visibility == Visibility.Visible);
            M(108, Part<UIElement>(left,"ItemUI").Visibility == Visibility.Collapsed && Part<UIElement>(left,"ItemLink").Visibility == Visibility.Collapsed && Part<UIElement>(left,"ItemLaunch").Visibility == Visibility.Visible && Part<UIElement>(left,"ItemSystem").Visibility == Visibility.Visible);
            M(109, Part<UIElement>(launch,"BtnVersion").Visibility == Visibility.Visible); M(110, Grid.GetColumnSpan(Part<UIElement>(launch,"BtnMore")) == 1);
            M(111, Part<UIElement>(overview,"BtnManageDelete").Visibility == Visibility.Collapsed && Part<UIElement>(overview,"BtnDisplayStar").Visibility == Visibility.Collapsed && fakeHeader.Info == "");
            Canvas[] logos = fakeHeader.Children.OfType<Canvas>().Where(logo => logo.Background is ImageBrush).ToArray(); Native.Check(logos.Length > 0, "The real header did not create its bitmap logo."); foreach (Canvas logo in logos) M(112, ((ImageBrush)logo.Background).Stretch == Stretch.Uniform);
            M(113, Part<UIElement>(instances["PageInstanceLeft"],"ItemExport").Visibility == Visibility.Collapsed && Part<UIElement>(instances["PageInstanceLeft"],"ItemModify").Visibility == Visibility.Visible);
            M(116, Convert.ToInt32(Call("NormalizeInstancePage", Native.Enum("PCL.FormMain+PageSubType", "InstanceModify"))) == 2 && Convert.ToInt32(Call("NormalizeInstancePage", Native.Enum("PCL.FormMain+PageSubType", "InstanceExport"))) == 0);
            foreach (string name in new[] { "BtnManageDownload", "BtnHintDownload" }) M(117, Part<UIElement>(instances["PageInstanceMod"],name).Visibility == Visibility.Collapsed);
            foreach (string name in new[] { "PageInstanceSetup", "PageSetupLaunch" }) { FrameworkElement combo = Part<FrameworkElement>(instances[name],"ComboArgumentIndieV2"); M(120, combo.Visibility == Visibility.Visible && combo.IsEnabled && Part<UIElement>(instances[name],"LabArgumentIndie").Visibility == Visibility.Visible && ((Grid)combo.Parent).RowDefinitions[0].Height.Value > 0); }
            Native.Call(overview.GetType(), "BtnManageDelete_Click", overview, null, null);
            foreach (int id in new[] { 1,2,4 }) M(125, (bool)Call("BlocksPage", Enum.ToObject(Native.Type("PCL.FormMain+PageType"),id))!);
            foreach (int id in new[] { 0,3,5,6,7 }) M(129, !(bool)Call("BlocksPage", Enum.ToObject(Native.Type("PCL.FormMain+PageType"),id))!);
            FrameworkElement candidate = (FrameworkElement)left.PageGet((FormMain.PageSubType)2); if (compact is null) compact = scope.Keep(candidate); else Native.Check(ReferenceEquals(compact,candidate), "Compact settings page cache lost its actual object identity."); M(133, candidate.GetType().FullName == "PCL.PageSetupModpack");
            PageSelectLeft selector = scope.Keep(new PageSelectLeft()); Native.Call(selector.GetType(), "RenderPackFolderList", selector); MyListItem[] rows = Part<Panel>(selector,"PanList").Children.OfType<MyListItem>().ToArray(); M(137, rows.Length == 1 && rows[0].ContextMenu is null && !(rows[0].Buttons?.Any() ?? false));
            string expectedRoot = (string)Native.Static("PCL.MinecraftFolderPolicy", "PackFolder", scope.Launcher)!; M(139, ((ModMinecraft.McFolder)rows[0].Tag).Location == expectedRoot);
            foreach (string relative in new[] { @"versions\Allowed\", @"versions\..\outside\", @"versions\Nested\Child\" }) M(142, (bool)Call("AllowsInstance", expectedRoot + relative)! == (relative == @"versions\Allowed\"));
            object? sharedBefore = Cached("LaunchFolders"); PageSelectLeft.AddFolder(Path.Combine(scope.Root,"synthetic-pack"), "synthetic", false); M(146, Equals(Cached("LaunchFolders"), sharedBefore));
        }
        Native.Call(compact!.GetType(), "PageLoaded", compact); string brand = (string)Native.Field(Native.Type("PCL.ModBase"),"LauncherDisplayName").GetRawConstantValue()!;
        M(149, Part<MyCheckBox>(compact,"CheckModpackMode").Checked && Part<TextBlock>(compact,"LabVersion").Text.StartsWith(brand + " ",StringComparison.Ordinal)); M(150, !string.IsNullOrEmpty(Part<MyCard>(compact,"CardLauncher").MainTextBlock.Text)); M(151, Grid.GetColumn(Part<UIElement>(compact,"CheckModpackMode")) == 1);
        foreach (string name in new[] { "BtnSystemUpdate", "ComboSystemActivity", "BtnSystemSettingImp", "BtnSystemSettingExp", "CardDebug" }) M(152, compact.FindName(name) is null); M(153, compact.FindName("TextSystemCache") is not null);
        scope.SetCache("LivingModpackMode", false); CheckModWarnings(false); M(156, (bool)Call("CanChangeMinecraft", "26.2", "26.1")!); M(157, (string)Call("DisplayTitle", "LivingPCL")! == "LivingPCL");
        Native.Field(Native.Type("PCL.PageSetupUI"),"_HiddenForceShow").SetValue(null,false); Call("ApplyVisibility", new object?[] { null }); Call("ApplyHeader",header,buttons[1],buttons[2],buttons[3],buttons[4]);
        M(161, buttons[1].Visibility == Visibility.Visible && buttons[4].Visibility == Visibility.Visible); M(162, Part<UIElement>(left,"ItemUI").Visibility == Visibility.Visible && Part<UIElement>(launch,"BtnVersion").Visibility == Visibility.Visible);
        M(163, Part<UIElement>(overview,"BtnManageDelete").Visibility == Visibility.Visible && Part<UIElement>(overview,"BtnDisplayStar").Visibility == Visibility.Visible && fakeHeader.Info == "synthetic header details"); foreach (string name in new[] { "PageInstanceSetup", "PageSetupLaunch" }) M(164, Part<UIElement>(instances[name],"ComboArgumentIndieV2").Visibility == Visibility.Visible);
        PageSetupSystem system = scope.Keep((PageSetupSystem)left.PageGet((FormMain.PageSubType)3)); M(166, system.GetType().FullName == "PCL.PageSetupSystem" && system.FindName("CheckModpackMode") is not null); system.Reload(); M(171, Part<UIElement>(system,"CheckModpackMode").Visibility == Visibility.Visible);
        CheckUpdateSelectionWiring(system);
        scope.SetCache("LivingModpackExported",false); Native.Call(system.GetType(),"RefreshModpackSwitch",system); M(174, Part<UIElement>(system,"CheckModpackMode").Visibility == Visibility.Collapsed && Part<UIElement>(system,"LabModpackMode").Visibility == Visibility.Collapsed);
        // Source-site 177 adapts only Native to fixed LF50 50b028da5fcad2226d74d901a41d8c43a249bfb2:
        // Pages/PageSetup/PageSetupSystem.xaml places LabLauncherVersion at row 0.
        // The retained f4 comparison tree and original PowerShell gold stay at row 2; never accept both.
        scope.SetCache("LivingModpackExported",true); Native.Call(system.GetType(),"RefreshModpackSwitch",system); M(177, Part<TextBlock>(system,"LabLauncherVersion").Text.StartsWith(brand + " ",StringComparison.Ordinal) && Grid.GetRow(Part<UIElement>(system,"LabLauncherVersion")) == 0);
        foreach (var preference in preferences) M(178, Equals(preference.Value,Cached(preference.Key)));
        DependencyObject? normalCard = Part<FrameworkElement>(system,"LabLauncherVersion").Parent; while (normalCard is FrameworkElement parent && normalCard.GetType().FullName != "PCL.MyCard") normalCard = parent.Parent; M(236, normalCard is MyCard); M(237, system.FindName("PanDonate") is null);
        PageInstanceExport export = scope.Keep(new PageInstanceExport()); MyCheckBox packOption = Part<MyCheckBox>(export,"CheckOptionsModpackMode"); M(181, packOption.Checked && (bool)Native.Get(packOption.Tag,"DefaultChecked")!);
        Part<MyCheckBox>(export,"CheckOptionsPcl").Checked = false; M(183, ((FrameworkElement)packOption.Parent).Visibility == Visibility.Collapsed); Part<MyCheckBox>(export,"CheckOptionsPcl").Checked = true; M(185, ((FrameworkElement)packOption.Parent).Visibility == Visibility.Visible);
        return (export,overview);
    }
    private static void CheckUpdateSelectionWiring(PageSetupSystem system) {
        var controller = (PCLCS.LegacyPageSetupSystemController)Native.Property(system.GetType(), "Controller").GetValue(system)!;
        var view = controller.View;
        var host = (PCLCS.LegacySetupSystemHost)Native.Field(controller.GetType(), "host").GetValue(controller)!;
        var source = view.ComboLauncherUpdateSource; var channel = view.ComboLauncherUpdateChannel;
        var confirmation = view.ConfirmPrereleaseUpdates; var preferred = host.PreferredUpdateChannel; var change = host.ChangeUpdateChannel;
        Native.Check(source is not null && channel is not null && confirmation is not null && preferred is not null && change is not null,
            "The actual Native settings page must begin with its complete update-selection delegates.");
        Native.Check(ReferenceEquals(view.Page(), system) && ReferenceEquals(system.FindName("ComboLauncherUpdateSource"), source!()) &&
            ReferenceEquals(system.FindName("ComboLauncherUpdateChannel"), channel!()),
            "Update-selection negatives require the actual Main BAML page and its real controls.");
        int sourceIndex = source!().SelectedIndex, channelIndex = channel!().SelectedIndex;
        void RequireInvalidWiring(string scenario) {
            bool rejected = false;
            try { controller.RefreshUpdateChannel(); }
            catch (InvalidOperationException) { rejected = true; }
            Native.Check(rejected, "Actual Main update-selection controller accepted " + scenario + ".");
            Native.Check(source!().SelectedIndex == sourceIndex && channel!().SelectedIndex == channelIndex,
                "Rejected update-selection wiring changed the real controls: " + scenario);
        }
        try {
            view.ComboLauncherUpdateSource = null!;
            RequireInvalidWiring("one missing view delegate with the real host still connected");
            view.ComboLauncherUpdateSource = source!;
            view.ComboLauncherUpdateSource = null!; view.ComboLauncherUpdateChannel = null!; view.ConfirmPrereleaseUpdates = null!;
            host.PreferredUpdateChannel = null!; host.ChangeUpdateChannel = null!;
            RequireInvalidWiring("all five missing delegates while the actual LF50 BAML controls exist");
        } finally {
            // Restore the exact original delegates on the same real objects,
            // including when an assertion or the production callback throws.
            view.ComboLauncherUpdateSource = source!; view.ComboLauncherUpdateChannel = channel!; view.ConfirmPrereleaseUpdates = confirmation!;
            host.PreferredUpdateChannel = preferred!; host.ChangeUpdateChannel = change!;
        }
        Native.Check(ReferenceEquals(view.ComboLauncherUpdateSource, source) && ReferenceEquals(view.ComboLauncherUpdateChannel, channel) &&
            ReferenceEquals(view.ConfirmPrereleaseUpdates, confirmation) && ReferenceEquals(host.PreferredUpdateChannel, preferred) &&
            ReferenceEquals(host.ChangeUpdateChannel, change), "Actual update-selection delegates were not restored by identity.");
        controller.RefreshUpdateChannel();
        Console.WriteLine("PASS native modpack-mode update-selection wiring: actual Main BAML rejects partial/all-missing callbacks and retains its exact full delegates.");
    }
    private static void CheckChangeConfirmation() {
        foreach (bool current in new[] { false,true }) foreach (bool accepted in new[] { false,true }) { int calls = 0; Func<bool,bool> confirm = desired => { calls++; M(48,desired != current); return accepted; }; bool result = (bool)Call("ConfirmChange",current,!current,confirm)!; M(50,calls == 1 && result == (accepted ? !current : current)); }
        int unchangedCalls = 0; Func<bool,bool> unused = _ => { unchangedCalls++; return true; }; bool same = (bool)Call("ConfirmChange",false,false,unused)!; M(52,unchangedCalls == 0); M(53,!same);
    }
    private static void CheckModWarnings(bool enabled) {
        foreach (string action in new[] { "update","delete","install" }) foreach (bool accept in new[] { false,true }) { int calls = 0; Func<string,bool> confirm = name => { calls++; M(39,name == action); return accept; }; bool result = (bool)Call("ConfirmModAction",action,confirm)!; M(41,result == (enabled ? accept : true) && calls == (enabled ? 1 : 0)); }
    }
}
