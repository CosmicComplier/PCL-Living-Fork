using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PCLCS;

public sealed class ModpackUpdatePreviewController<TFile, TPackage, TChange, TPlan, TKind, TButton, TScroll>
    where TFile : PackUpdateFileData where TPackage : PackUpdatePackageData<TFile> where TChange : PackUpdateChangeData<TFile> where TPlan : PackUpdatePlanData<TPackage, TChange>
    where TKind : struct, Enum where TButton : FrameworkElement where TScroll : ScrollViewer {
    public readonly List<TChange> Changes;
    private readonly Dictionary<TKind, int> counts;
    private readonly ModpackDialogController<TButton, TScroll> view;
    private readonly Action<int> setKind;
    private static TKind Kind(int value) => (TKind)Enum.ToObject(typeof(TKind), value);
    private static bool Same(string? a, string? b) => (a ?? "") == (b ?? "");
    public ModpackUpdatePreviewController(ModpackDialogController<TButton, TScroll> view, TPlan plan, string currentMinecraft, Dictionary<ModLoaders, string> currentLoaders,
        Dictionary<TKind, int> counts, Action<int> setKind, Func<string, UIElement> hint) {
        this.view = view; this.counts = counts; this.setKind = setKind; Changes = plan.Changes.ToList();
        bool changed = !Same(currentMinecraft, plan.Package!.Minecraft) || !currentLoaders.OrderBy(pair => pair.Key).SequenceEqual(plan.Package.Loaders.OrderBy(pair => pair.Key));
        if (changed) view.Header.Children.Add(hint("游戏版本或加载器将更新，请确认兼容性。存档会保留，更新前自动备份。"));
        foreach (int value in new[] { 0, 1, 2, 3 }) {
            counts[Kind(value)] = Changes.Count(item => Classify(item) == value);
            view.AddFilter(KindName(value) + "（" + counts[Kind(value)] + "）", value, counts[Kind(value)] > 0, ShowKind);
        }
        ShowKind(new[] { 3, 1, 0, 2 }.FirstOrDefault(value => counts[Kind(value)] > 0));
    }
    public static int Classify(TChange change) => change.Conflict ? 3 : change.Incoming is null ? 2 : change.Before is null ? 0 : 1;
    public static string KindName(int value) => value switch { 0 => "新增", 1 => "更新", 2 => "移除", _ => "保留个人修改" };
    public static string RuntimeSummary(TPackage package, string minecraft, Dictionary<ModLoaders, string> loaders) {
        List<string> values = new() { "Minecraft " + (Same(minecraft, package.Minecraft) ? minecraft : minecraft + " → " + package.Minecraft) };
        foreach (ModLoaders loader in loaders.Keys.Union(package.Loaders.Keys).OrderBy(value => value)) {
            loaders.TryGetValue(loader, out string? before); package.Loaders.TryGetValue(loader, out string? after);
            values.Add(loader + " " + (Same(before, after) ? after : (before ?? "无") + " → " + (after ?? "移除")));
        }
        return string.Join(" · ", values);
    }
    public static string FolderName(string relative) {
        string[] parts = relative.Split('/'); if (parts.Length == 1) return "根目录文件";
        return parts[0].ToLowerInvariant() switch { "mods" => "模组", "config" or "defaultconfigs" => "配置", "resourcepacks" => "资源包", "shaderpacks" => "光影包", "datapacks" => "数据包", _ => parts[0] };
    }
    public void ShowKind(int value) {
        setKind(value); view.BeginFilter(value);
        string description = value == 3 ? "以下文件保留当前状态，本次更新不会覆盖。" : value == 2 ? "作者在新版中移除了以下文件，更新前会备份。" : value == 0 ? "新版将添加以下文件。" : "以下文件将替换为作者提供的新版，更新前会备份。";
        view.Rows.Children.Add(ModpackDialogController<TButton, TScroll>.Label(description, 12, color: "ColorBrushGray2", margin: new Thickness(4, 0, 7, 10)));
        foreach (var group in Changes.Where(item => Classify(item) == value).GroupBy(item => FolderName(item.Relative!)).OrderBy(item => item.Key)) {
            view.Rows.Children.Add(ModpackDialogController<TButton, TScroll>.Label(group.Key + "（" + group.Count() + "）", 13, true, margin: new Thickness(4, 3, 7, 9)));
            foreach (TChange change in group.OrderBy(item => item.Relative, StringComparer.OrdinalIgnoreCase)) {
                StackPanel text = new(); TextBlock name = ModpackDialogController<TButton, TScroll>.Label(Path.GetFileName(change.Relative), 13, true);
                name.TextWrapping = TextWrapping.NoWrap; name.TextTrimming = TextTrimming.CharacterEllipsis; text.Children.Add(name);
                string? folder = Path.GetDirectoryName(change.Relative!.Replace('/', Path.DirectorySeparatorChar));
                text.Children.Add(ModpackDialogController<TButton, TScroll>.Label((string.IsNullOrEmpty(folder) ? "根目录" : folder) + (value == 3 ? " · " + change.Reason : ""), 12, false, "ColorBrushGray2", new Thickness(0, 4, 0, 0)));
                Border box = ModpackDialogController<TButton, TScroll>.Frame(text); box.ToolTip = change.Relative + "\r\n" + change.Reason; view.Rows.Children.Add(box);
            }
        }
        if (counts[Kind(value)] == 0) view.Rows.Children.Add(ModpackDialogController<TButton, TScroll>.Label("此分类没有文件。", 13, margin: new Thickness(8)));
    }
}
