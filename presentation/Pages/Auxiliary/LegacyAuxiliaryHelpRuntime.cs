// Behavior source: LF Pages/PageSetup and Pages/PageOther; repository LICENCE.
// LF lineage f4aa0fae62c3ee8af24c81e61844c8fdd13c8983; upstream PCL 5ef81e490ae46b1100d61e33d0183094c7a80ca8.
// Existing LF policies are preserved; no new native/performance/MMC behavior.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MeloongCore;
using MeloongCore.Extensions;
using PCLLF.Presentation.Controls;

namespace PCLCS;
public sealed class LegacyAuxiliaryHelpRuntime<TEntry, TEvent>
    where TEntry : class
{
    public readonly LegacyAuxiliaryHelpHost<TEntry, TEvent> Host;
    public LegacyAuxiliaryHelpRuntime(LegacyAuxiliaryHelpHost<TEntry, TEvent> host) => Host = host;
    public void OnItemClick(TEntry entry)
    {
        try
        {
            if (Host.IsEvent(entry))
                Host.RaiseEvent(Host.EventType(entry), Host.EventData(entry));
            else
                EnterHelpPage(entry);
        }
        catch (Exception error)
        {
            Logger.Error(error, "处理帮助项目点击时发生意外错误");
        }
    }

    public void EnterHelpPage(string location) => Host.Common.InThread(() =>
    {
        if (!Host.LoaderFinished())
            Host.WaitForLoader();
        TEntry entry = Host.NewEntry(location);
        PostDetail(entry);
    });
    public void EnterHelpPage(TEntry entry) => Host.Common.InThread(() =>
    {
        if (!Host.LoaderFinished())
            Host.WaitForLoader();
        PostDetail(entry);
    });
    private void PostDetail(TEntry entry) => Host.Common.InUi(() =>
    {
        LegacyAuxiliaryHelpDetailController<TEntry, TEvent> detail = Host.NewDetailController();
        if (detail.Init(entry))
            Host.NavigateDetail(entry, detail);
        else
            Logger.Warn("已取消进入帮助项目，这一般是由于 xaml 初始化失败，且用户在弹窗中手动放弃");
    });
}

public sealed class LegacyAuxiliaryHelpController<TEntry, TEvent>
    where TEntry : class
{
    private readonly LegacyAuxiliaryHelpRuntime<TEntry, TEvent> runtime;
    private LegacyAuxiliaryHelpHost<TEntry, TEvent> Host => runtime.Host;

    public readonly LegacyAuxiliaryHelpView View;
    public LegacyAuxiliaryHelpController(LegacyAuxiliaryHelpRuntime<TEntry, TEvent> runtime, LegacyAuxiliaryHelpView view)
    {
        this.runtime = runtime;
        View = view;
    }

    public void Loaded() => View.Scroll().ScrollToHome();
    public void Initialized() => Host.InitializeLoader(View.Page(), View.Loading(), View.LoadingPanel(), View.Scroll(), HelpListLoad);
    public void HelpListLoad(List<TEntry> entries)
    {
        try
        {
            View.List().Children.Clear();
            View.Scroll().ScrollToHome();
            var types = new List<string>();
            foreach (TEntry item in entries)
            {
                if (Host.Release() && !Host.ShowPublic(item))
                    continue;
                if (!Host.Release() && !Host.ShowSnapshot(item))
                    continue;
                foreach (string type in Host.Types(item))
                    if (!types.Contains(type))
                        types.Add(type);
            }

            if (types.Contains("指南"))
            {
                types.Remove("指南");
                types.Insert(0, "指南");
            }

            foreach (string type in types)
            {
                var typeItems = new List<TEntry>();
                foreach (TEntry item in entries)
                {
                    if (Host.Release() && !Host.ShowPublic(item))
                        continue;
                    if (!Host.Release() && !Host.ShowSnapshot(item))
                        continue;
                    if (Host.Types(item).Contains(type))
                        typeItems.Add(item);
                }

                MyCardView card = Host.NewCard();
                card.Title = type;
                card.Margin = new Thickness(0, 0, 0, 15);
                var stack = new StackPanel
                {
                    Margin = new Thickness(20, MyCardView.SwapedHeight, 18, 0),
                    VerticalAlignment = VerticalAlignment.Top,
                    RenderTransform = new TranslateTransform(0, 0)
                };
                card.Children.Add(stack);
                card.SwapControl = stack;
                card.SetContentBuilder(typeItems, entry => Host.ToItem(entry));
                if (type == "指南")
                    card.BuildContent();
                else
                    card.IsSwapped = true;
                View.List().Children.Add(card);
            }
        }
        catch (Exception error)
        {
            Logger.Error(error, "加载帮助列表 UI 失败");
        }
    }

    private sealed class Source : ILegacyBaseSearchSource
    {
        public string[] Aliases { get; }
        public double Weight { get; }

        public Source(string text, double weight)
        {
            Aliases = new[]
            {
                text
            };
            Weight = weight;
        }
    }

    private sealed class SearchEntry : ILegacyBaseSearchEntry<Source>
    {
        public TEntry Item = null!;
        public List<Source> Sources { get; set; } = null!;
        public double Similarity { get; set; }
        public bool AbsoluteRight { get; set; }
    }

    public void SearchRun()
    {
        ControlAnimations animations = ControlServices.Current.Animations;
        if (string.IsNullOrWhiteSpace(View.Query()))
        {
            animations.Start(new[] { animations.Opacity(View.SearchCard(), -View.SearchCard().Opacity, new ControlAnimationSpec(100)), animations.Code(() =>
            {
                View.SearchCard().Height = 0;
                View.SearchCard().Visibility = Visibility.Collapsed;
                View.List().Visibility = Visibility.Visible;
            }, after: true), animations.Opacity(View.List(), 1 - View.List().Opacity, new ControlAnimationSpec(150, 30)) }, "FrmOtherHelp Search Switch");
        }
        else
        {
            var queries = new List<SearchEntry>();
            foreach (TEntry entry in Host.Entries())
            {
                if (!Host.ShowSearch(entry) || Host.Release() && !Host.ShowPublic(entry))
                    continue;
                if (!Host.ShowSearch(entry) || !Host.Release() && !Host.ShowSnapshot(entry))
                    continue;
                queries.Add(new SearchEntry { Item = entry, Sources = new List<Source> { new Source(Host.Title(entry), 1), new Source(Host.Description(entry), 0.5), new Source(Host.Search(entry), 1.5) } });
            }

            List<SearchEntry> results = LegacyBaseSearch.Search<SearchEntry, Source>(queries, View.Query(), 5, 0.08);
            View.SearchList().Children.Clear();
            if (!results.Any())
            {
                View.SearchCard().Title = "无搜索结果";
                View.SearchList().Visibility = Visibility.Collapsed;
            }
            else
            {
                View.SearchCard().Title = "搜索结果";
                foreach (SearchEntry result in results)
                {
                    MyListItemView item = Host.ToItem(result.Item);
                    if (Host.Debug())
                        item.Info = (result.AbsoluteRight ? "完全匹配，" : "") + "相似度：" + LegacySetupText.Number(Math.Round(result.Similarity, 3)) + "，" + item.Info;
                    View.SearchList().Children.Add(item);
                }

                View.SearchList().Visibility = Visibility.Visible;
            }

            animations.Start(new[] { animations.Opacity(View.List(), -View.List().Opacity, new ControlAnimationSpec(100)), animations.Code(() =>
            {
                View.List().Visibility = Visibility.Collapsed;
                View.SearchCard().Visibility = Visibility.Visible;
                View.SearchCard().TriggerForceResize();
            }, after: true), animations.Opacity(View.SearchCard(), 1 - View.SearchCard().Opacity, new ControlAnimationSpec(150, 30)) }, "FrmOtherHelp Search Switch");
        }
    }

    public void Refresh() => Host.RefreshHelp();
}

public sealed class LegacyAuxiliaryHelpDetailController<TEntry, TEvent>
    where TEntry : class
{
    private readonly LegacyAuxiliaryHelpHost<TEntry, TEvent> host;
    public readonly LegacyAuxiliaryHelpDetailView<TEntry> View;
    public LegacyAuxiliaryHelpDetailController(LegacyAuxiliaryHelpHost<TEntry, TEvent> host, LegacyAuxiliaryHelpDetailView<TEntry> view)
    {
        this.host = host;
        View = view;
    }

    public void Refresh() => Init(host.NewEntry(host.RawPath(View.Entry())));
    public void Loaded() => View.Scroll().ScrollToTop();
    public bool Init(TEntry entry)
    {
        string content = host.Xaml(entry) ?? "";
        if (content == "")
            throw new Exception("帮助 xaml 文件为空");
        try
        {
            content = host.ReplaceArguments(content, StringUtils.XmlEscape);
            while (content.Contains("xmlns"))
                content = content.RegexReplace("xmlns[^\"']*(\"|')[^\"']*(\"|')", "").Replace("xmlns", "");
            content = "<StackPanel xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:sys=\"clr-namespace:System;assembly=mscorlib\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:local=\"clr-namespace:PCL;assembly=Plain Craft Launcher 2\" xmlns:core=\"clr-namespace:MeloongCore;assembly=MeloongCore\" xmlns:corewpf=\"clr-namespace:MeloongCore.Wpf;assembly=MeloongCore.Wpf\">" + content + "</StackPanel>";
            View.SetEntry(entry);
            View.Content().Children.Clear();
            View.Content().Children.Add(host.ParseXaml(content));
            return true;
        }
        catch (Exception error)
        {
            Logger.Info("自定义信息内容：\r\n" + content);
            Logger.Error(error, "加载帮助 XAML 文件失败", LogBehavior.Alert);
            return false;
        }
    }
}
