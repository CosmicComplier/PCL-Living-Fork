using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace PCLLF.Presentation.Foundation;

/// <summary>Original LF WPF dispatch, sizing and visible-corner rules; the legacy UI-thread/DPI fields stay authoritative.</summary>
public static class LegacyBaseUi {
    public static bool RunInUi(int uiThreadId) => Thread.CurrentThread.ManagedThreadId == uiThreadId;
    public static T RunInUiWait<T>(Func<T> action, int uiThreadId) {
        if (RunInUi(uiThreadId)) return action();
        return Application.Current.Dispatcher.Invoke(action);
    }
    public static void RunInUiWait(Action action, int uiThreadId) {
        if (RunInUi(uiThreadId)) action();
        else Application.Current.Dispatcher.Invoke(action);
    }
    public static void RunInUi(Action action, bool forceWaitUntilLoaded, int uiThreadId) {
        if (forceWaitUntilLoaded) Application.Current.Dispatcher.InvokeAsync(action, DispatcherPriority.Loaded);
        else if (RunInUi(uiThreadId)) action();
        else Application.Current.Dispatcher.InvokeAsync(action);
    }
    public static double GetPixelSize(double wpfSize, int dpi) => wpfSize / 96 * dpi;
    public static double GetWpfSize(double pixelSize, int dpi) => pixelSize * 96 / dpi;
    public static bool IsVisibleInForm(FrameworkElement element, FrameworkElement mainWindow) {
        if (!element.IsVisible) return false;
        Rect bounds = element.TransformToAncestor(mainWindow).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
        Rect rect = new(0, 0, mainWindow.ActualWidth, mainWindow.ActualHeight);
        return rect.Contains(bounds.TopLeft) || rect.Contains(bounds.BottomRight);
    }
    public static bool IsTextTrimmed(TextBlock textBlock, int dpi) {
        Typeface typeface = new(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch);
        FormattedText formatted = new(textBlock.Text, Thread.CurrentThread.CurrentCulture, textBlock.FlowDirection, typeface, textBlock.FontSize, textBlock.Foreground, dpi);
        return formatted.Width > textBlock.ActualWidth;
    }
    public static Visibility ToVisibility(bool isVisible) => isVisible ? Visibility.Visible : Visibility.Collapsed;
}
