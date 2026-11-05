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
    private readonly TPlan plan;
    private static TKind Kind(int value) => (TKind)Enum.ToObject(typeof(TKind), value);
    private static bool Same(string? a, string? b) => (a ?? "") == (b ?? "");
    public ModpackUpdatePreviewController(ModpackDialogController<TButton, TScroll> view, TPlan plan, string currentMinecraft, Dictionary<ModLoaders, string> currentLoaders,
        Dictionary<TKind, int> counts, Action<int> setKind, Func<string, UIElement> hint) {
        this.view = view; this.counts = counts; this.setKind = setKind; this.plan = plan; Changes = plan.Changes.ToList();
        bool changed = !Same(currentMinecraft, plan.Package!.Minecraft) || !currentLoaders.OrderBy(pair => pair.Key).SequenceEqual(plan.Package.Loaders.OrderBy(pair => pair.Key));
        if (changed) view.Header.Children.Add(hint("游戏版本或加载器将更新，请确认兼容性。存档会保留，更新前自动备份。"));
        AddNotices("需要核对的文件与来源", plan.Warnings);
        if (plan.RuntimeWarnings.Count > 0) view.Header.Children.Add(hint("保留模组包含已知兼容性风险或尚未确认的声明，请展开下方兼容性详情核对。"));
        AddNotices("保留模组的兼容性", plan.RuntimeWarnings);
        if (plan.DependencyCheck is PackDependencyGraphResult graph) {
            view.Header.Children.Add(hint("以下依赖结果按默认方案检查；调整作者版本或个人覆盖后，确认时会重新检查完整组合。"));
            if (graph.HasErrors) view.Header.Children.Add(hint("默认组合存在依赖或冲突问题。请调整作者版本和个人覆盖的选择；有确定冲突的组合不会提交。"));
            else if (graph.HasUnknown) view.Header.Children.Add(hint("完整组合中有无法确认的声明，提交前需要另行确认；未知不表示兼容。"));
            AddNotices("默认更新组合的依赖与冲突", graph.Errors);
            AddNotices("默认更新组合的待确认声明", graph.Warnings);
        }
        foreach (int value in new[] { 0, 1, 2, 3 }) {
            counts[Kind(value)] = Changes.Count(item => Classify(item) == value);
            view.AddFilter(KindName(value) + "（" + counts[Kind(value)] + "）", value, counts[Kind(value)] > 0, ShowKind);
        }
        ShowKind(new[] { 3, 1, 0, 2 }.FirstOrDefault(value => counts[Kind(value)] > 0));
    }
    public static int Classify(TChange change) => change.DisplayOnly || change.Conflict ? 3 : change.Incoming is null ? 2 : change.Before is null ? 0 : 1;
    public static string KindName(int value) => value switch { 0 => "新增", 1 => "更新", 2 => "移除", _ => "个人文件与冲突" };
    public static string OwnershipName(PackUpdateOwnership ownership) => ownership switch {
        PackUpdateOwnership.AuthorUnchanged => "作者文件",
        PackUpdateOwnership.AuthorRenamed => "作者文件，玩家仅改名",
        PackUpdateOwnership.PersonalModified => "个人修改",
        PackUpdateOwnership.PersonalAdded => "个人新增，保留",
        PackUpdateOwnership.Disabled => "玩家已禁用",
        PackUpdateOwnership.Deleted => "玩家已删除",
        PackUpdateOwnership.OptionalAbsent => "未安装的可选组件",
        PackUpdateOwnership.Ambiguous => "存在多个关联候选，待确认",
        _ => "身份或归属无法确认"
    };
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
        string description = value == 3 ? "个人新增文件会保留。真实冲突默认保留个人版本，可逐项选择使用作者版本；待确认的文件不会自动删除。" : value == 2 ? "作者在新版中移除了以下文件，更新前会备份。" : value == 0 ? "新版将添加以下文件。" : "以下文件将替换为作者提供的新版，更新前会备份；仅改名的旧文件会从实际本地路径处理。";
        view.Rows.Children.Add(ModpackDialogController<TButton, TScroll>.Label(description, 12, color: "ColorBrushGray2", margin: new Thickness(4, 0, 7, 10)));
        foreach (var group in Changes.Where(item => Classify(item) == value).GroupBy(item => FolderName(item.Relative!)).OrderBy(item => item.Key)) {
            view.Rows.Children.Add(ModpackDialogController<TButton, TScroll>.Label(group.Key + "（" + group.Count() + "）", 13, true, margin: new Thickness(4, 3, 7, 9)));
            foreach (TChange change in group.OrderBy(item => item.Relative, StringComparer.OrdinalIgnoreCase)) {
                StackPanel text = new(); TextBlock name = ModpackDialogController<TButton, TScroll>.Label(Path.GetFileName(change.Relative), 13, true);
                name.TextWrapping = TextWrapping.NoWrap; name.TextTrimming = TextTrimming.CharacterEllipsis; text.Children.Add(name);
                string? folder = Path.GetDirectoryName(change.Relative!.Replace('/', Path.DirectorySeparatorChar));
                text.Children.Add(ModpackDialogController<TButton, TScroll>.Label((string.IsNullOrEmpty(folder) ? "根目录" : folder) + " · " + OwnershipName(change.Ownership), 12, false, "ColorBrushGray2", new Thickness(0, 4, 0, 0)));
                if (!string.IsNullOrWhiteSpace(change.LocalRelative) && !string.Equals(change.LocalRelative, change.DestinationRelative, StringComparison.OrdinalIgnoreCase))
                    text.Children.Add(ModpackDialogController<TButton, TScroll>.Label("本地：" + change.LocalRelative + "\r\n" + (change.Incoming is null ? "作者在新版中移除此文件" : "作者目标：" + change.DestinationRelative), 12, false, "ColorBrushGray2", new Thickness(0, 4, 0, 0)));
                if (change.Candidates.Count > 0)
                    text.Children.Add(ModpackDialogController<TButton, TScroll>.Label("待核对的本地候选：\r\n" + string.Join("\r\n", change.Candidates), 12, false, "ColorBrushGray2", new Thickness(0, 4, 0, 0)));
                if (!string.IsNullOrWhiteSpace(change.Reason))
                    text.Children.Add(ModpackDialogController<TButton, TScroll>.Label(change.Reason, 12, false, "ColorBrushGray2", new Thickness(0, 4, 0, 0)));
                if (change.Conflict && !change.DisplayOnly && change.AllowAuthor && change.Ownership != PackUpdateOwnership.Ambiguous)
                    AddChoice(text, change);
                Border box = ModpackDialogController<TButton, TScroll>.Frame(text); box.ToolTip = change.Relative + "\r\n" + change.LocalRelative + "\r\n" + change.Reason; view.Rows.Children.Add(box);
            }
        }
        if (counts[Kind(value)] == 0) view.Rows.Children.Add(ModpackDialogController<TButton, TScroll>.Label("此分类没有文件。", 13, margin: new Thickness(8)));
    }
    private void AddNotices(string title, IEnumerable<string> notices) {
        string[] lines = notices.Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.Ordinal).ToArray();
        if (lines.Length == 0) return;
        StackPanel content = new();
        foreach (string line in lines) content.Children.Add(ModpackDialogController<TButton, TScroll>.Label(line, 12, margin: new Thickness(4, 2, 4, 5)));
        Expander details = new() { Header = title + "（" + lines.Length + "）", Margin = new Thickness(0, 6, 0, 0),
            Content = new ScrollViewer { Content = content, MaxHeight = 110, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled } };
        details.SetResourceReference(Control.ForegroundProperty, "ColorBrush1");
        view.Header.Children.Add(details);
    }
    private void AddChoice(StackPanel text, TChange change) {
        string group = "PackUpdateChoice_" + Guid.NewGuid().ToString("N");
        StackPanel choices = new() { Margin = new Thickness(0, 8, 0, 0) };
        RadioButton preserve = new() { Content = "保留当前版本", GroupName = group, IsChecked = change.Choice == PackUpdateChoice.PreservePersonal, Margin = new Thickness(0, 0, 0, 5), Tag = PackUpdateChoice.PreservePersonal };
        RadioButton author = new() { Content = change.Incoming is null ? "使用作者方案（移除此文件）" : change.Ownership == PackUpdateOwnership.Disabled ? "使用作者版本（保持禁用）" : "使用作者版本", GroupName = group, IsChecked = change.Choice == PackUpdateChoice.UseAuthor, Tag = PackUpdateChoice.UseAuthor };
        preserve.SetResourceReference(Control.ForegroundProperty, "ColorBrush1"); author.SetResourceReference(Control.ForegroundProperty, "ColorBrush1");
        preserve.Checked += (_, _) => { change.Choice = PackUpdateChoice.PreservePersonal; plan.AllowUnverifiedDependencies = false; };
        author.Checked += (_, _) => { change.Choice = PackUpdateChoice.UseAuthor; plan.AllowUnverifiedDependencies = false; };
        choices.Children.Add(preserve); choices.Children.Add(author); text.Children.Add(choices);
    }
}
