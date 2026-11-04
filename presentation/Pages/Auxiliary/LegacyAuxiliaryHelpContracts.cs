// Behavior source: LF Pages/PageSetup and Pages/PageOther; repository LICENCE.
// LF lineage f4aa0fae62c3ee8af24c81e61844c8fdd13c8983; upstream PCL 5ef81e490ae46b1100d61e33d0183094c7a80ca8.
// Existing LF policies are preserved; no new native/performance/MMC behavior.
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using PCLLF.Presentation.Controls;

namespace PCLCS;
public sealed class LegacyAuxiliaryHelpHost<TEntry, TEvent>
    where TEntry : class
{
    public LegacySetupPageHost Common = null!;
    public Func<bool> Release = null!, Debug = null!, LoaderFinished = null!;
    public Action WaitForLoader = null!, RefreshHelp = null!;
    public Func<List<TEntry>> Entries = null!;
    public Func<TEntry, List<string>> Types = null!;
    public Func<TEntry, bool> ShowPublic = null!, ShowSnapshot = null!, ShowSearch = null!, IsEvent = null!;
    public Func<TEntry, string> Title = null!, Description = null!, Search = null!, RawPath = null!;
    public Func<TEntry, string?> Xaml = null!;
    public Func<TEntry, TEvent> EventType = null!;
    public Func<TEntry, string> EventData = null!;
    public Action<TEvent, string> RaiseEvent = null!;
    public Func<string, TEntry> NewEntry = null!;
    public Func<TEntry, MyListItemView> ToItem = null!;
    public Func<MyCardView> NewCard = null!;
    public Action<MyPageRightView, MyLoadingView, FrameworkElement, FrameworkElement, Action<List<TEntry>>> InitializeLoader = null!;
    public Func<LegacyAuxiliaryHelpDetailController<TEntry, TEvent>> NewDetailController = null!;
    public Action<TEntry, LegacyAuxiliaryHelpDetailController<TEntry, TEvent>> NavigateDetail = null!;
    public Func<string, Func<string, string>, string> ReplaceArguments = null!;
    public Func<string, UIElement> ParseXaml = null!;
}

public sealed class LegacyAuxiliaryHelpView
{
    public Func<MyPageRightView> Page = null!;
    public Func<ScrollViewer> Scroll = null!;
    public Func<MyLoadingView> Loading = null!;
    public Func<FrameworkElement> LoadingPanel = null!;
    public Func<StackPanel> List = null!, SearchList = null!;
    public Func<MyCardView> SearchCard = null!;
    public Func<string> Query = null!;
}

public sealed class LegacyAuxiliaryHelpDetailView<TEntry>
    where TEntry : class
{
    public Func<MyPageRightView> Page = null!;
    public Func<TEntry> Entry = null!;
    public Action<TEntry> SetEntry = null!;
    public Func<ScrollViewer> Scroll = null!;
    public Func<StackPanel> Content = null!;
}
