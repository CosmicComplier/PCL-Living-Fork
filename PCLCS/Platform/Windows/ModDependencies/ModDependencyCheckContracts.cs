using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PCLCS;

/// <summary>Only the existing LF custom controls and typed platform reads cross this boundary.</summary>
public sealed class ModDependencyCheckViewHost<TVersion, TIssue, TCard> where TCard : FrameworkElement {
    public Func<TVersion, string?> VersionDisplay = null!;
    public Func<TIssue, string> IssueKey = null!;
    public Func<string, Thickness, TCard> CreateCard = null!;
    public Action<TCard, UIElement> AddCardChild = null!;
    public Action<TCard, UIElement> CollapseDetails = null!;
    public Action<TCard> EnsureCaption = null!;
    public Func<UIElement, ScrollViewer> CreateScroll = null!;
    public Func<string, Thickness, FrameworkElement> CreateWarningHint = null!;
    public Func<ImageSource> DefaultIcon = null!;
}

public class ModDependencyImpactItemData {
    public string? Name, FileName;
    public ImageSource? Icon;
    public System.Collections.Generic.List<string> Reasons = new();
}
