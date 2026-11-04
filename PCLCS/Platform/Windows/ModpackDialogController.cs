using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace PCLCS;

// LF-only ModpackUpdateViews.vb, LF LICENCE. No PCL/CE equivalent update-dialog
// transaction UI is adopted. Real LF MyButton/MyScrollViewer identity comes from the typed host.
public sealed class ModpackDialogHost<TButton, TScroll> where TButton : FrameworkElement where TScroll : ScrollViewer {
    public Func<string> AnimationName = null!;
    public Func<TButton> CreateButton = null!;
    public Action<TButton, string> SetButtonText = null!;
    public Action<TButton, bool> HighlightButton = null!;
    public Action<TButton, Action> OnClick = null!;
    public Func<TScroll> CreateScroll = null!;
    public Func<int> AnimationControl = null!;
    public Action<string> StopAnimation = null!;
    public Action<UIElement, double, int, string> AnimateOpacity = null!;
}
public class ModpackDialogController<TButton, TScroll> : Grid where TButton : FrameworkElement where TScroll : ScrollViewer {
    public double PreferredWidth => 740;
    public readonly StackPanel Header = new();
    public readonly UniformGrid Filters = new();
    public readonly StackPanel Rows = new();
    public readonly TScroll Scroller;
    public readonly string AnimationKey;
    private readonly ModpackDialogHost<TButton, TScroll> host;
    public ModpackDialogController(string title, string subtitle, int filterCount, ModpackDialogHost<TButton, TScroll> host) {
        this.host = host; AnimationKey = host.AnimationName();
        Height = 420; RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); Header.Margin = new Thickness(3, 0, 7, 0);
        Header.Children.Add(Label(title, 15, true)); Header.Children.Add(Label(subtitle, 12, false, "ColorBrushGray2", new Thickness(0, 5, 0, 0))); Children.Add(Header);
        Filters.Columns = filterCount; Filters.Margin = new Thickness(0, 13, 4, 7);
        Filters.SizeChanged += (_, _) => Filters.Columns = filterCount == 4 && Filters.ActualWidth < 580 ? 2 : filterCount == 3 && Filters.ActualWidth < 430 ? 1 : filterCount;
        SetRow(Filters, 1); Children.Add(Filters);
        Scroller = host.CreateScroll(); Scroller.Content = Rows; Scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Auto; Scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        SetRow(Scroller, 2); Children.Add(Scroller); Unloaded += (_, _) => host.StopAnimation(AnimationKey);
    }
    public void AddFilter(string text, int key, bool enabled, Action<int> selectFilter) {
        TButton button = host.CreateButton(); host.SetButtonText(button, text); button.Tag = key; button.Height = 34; button.Margin = new Thickness(3, 0, 3, 6); button.IsEnabled = enabled;
        host.OnClick(button, () => selectFilter(key)); Filters.Children.Add(button);
    }
    public void BeginFilter(int key) {
        foreach (TButton button in Filters.Children) host.HighlightButton(button, (int)button.Tag == key);
        Rows.Children.Clear(); Scroller.ScrollToTop(); host.StopAnimation(AnimationKey);
        if (IsLoaded && host.AnimationControl() == 0) { Rows.Opacity = 0; host.AnimateOpacity(Rows, 1, 120, AnimationKey); } else Rows.Opacity = 1;
    }
    public static TextBlock Label(string? text, double size, bool bold = false, string color = "ColorBrush1", Thickness margin = default) {
        TextBlock value = new() { Text = text, FontSize = size, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal, TextWrapping = TextWrapping.Wrap, Margin = margin };
        value.SetResourceReference(TextBlock.ForegroundProperty, color); return value;
    }
    public static Border Frame(UIElement child) {
        Border box = new() { Child = child, CornerRadius = new CornerRadius(5), BorderThickness = new Thickness(1), Padding = new Thickness(12, 10, 12, 10), Margin = new Thickness(3, 0, 7, 8), Background = Brushes.White };
        box.SetResourceReference(Border.BorderBrushProperty, "ColorBrushGray5"); return box;
    }
}
