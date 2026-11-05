using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PCLLF.Presentation.Controls;

// LF original LazyLoadBehavior.vb; CE pinned source compared without changing LF handler/failure lifetime.
public static class LazyLoadBehaviorView {
    public static void OnFirstEnterScrollViewerViewport(DependencyObject element, Action? action) => element.SetValue(IsInViewportProperty, action);
    public static readonly DependencyProperty IsInViewportProperty = DependencyProperty.RegisterAttached(
        "IsInViewport", typeof(Action), typeof(LazyLoadBehaviorView), new PropertyMetadata(null, OnIsInViewportChanged));
    private static void OnIsInViewportChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) {
        if (sender is not FrameworkElement element || args.NewValue is null) return;
        bool handled = false;
        EventHandler? handler = null;
        handler = (_, _) => {
            if (handled || element.RenderSize.Width < double.Epsilon || !element.IsVisible) return;
            ScrollViewer? scroll = FindParentScrollViewer(element);
            if (scroll is null) return;
            if (!new Rect(0, 0, scroll.ViewportWidth, scroll.ViewportHeight).IntersectsWith(
                element.TransformToAncestor(scroll).TransformBounds(new Rect(new Point(0, 0), element.RenderSize)))) return;
            handled = true;
            ((Action)args.NewValue)();
            // Deliberately after the callback: LF leaves an inert handled handler subscribed when it throws.
            element.LayoutUpdated -= handler;
        };
        element.LayoutUpdated += handler;
    }
    private static ScrollViewer? FindParentScrollViewer(DependencyObject? element) {
        while (element is not null) {
            if (element is ScrollViewer scroll) return scroll;
            element = VisualTreeHelper.GetParent(element);
        }
        return null;
    }
}
