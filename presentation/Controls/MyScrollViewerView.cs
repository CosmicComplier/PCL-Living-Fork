using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.VisualBasic.CompilerServices;
using MeloongCore;

namespace PCLLF.Presentation.Controls;

// LF MyScrollViewer.vb. Native scroll target/interpolation, popup exclusion and keyboard focus handling retained.
public class MyScrollViewerView : ScrollViewer {
    public double DeltaMult { get; set; } = 1;
    private double realOffset;
    public MyScrollBarView? ScrollBar;
    protected virtual MyScrollBarView? VerticalBar => ScrollBar;
    protected virtual void AssignBar(MyScrollBarView? value) => ScrollBar = value;
    public MyScrollBarView? CurrentScrollBar => VerticalBar;
    protected virtual bool HasMainWindow => Application.Current?.MainWindow is not null;
    protected virtual void RefreshBack() => throw new InvalidOperationException("The LF main-window back-button host is required.");
    public MyScrollViewerView() {
        PreviewMouseWheel += Wheel;
        ScrollChanged += (_, args) => { realOffset = VerticalOffset; if (HasMainWindow && (Conversions.ToBoolean(args.VerticalChange) || Conversions.ToBoolean(args.ViewportHeightChange))) RefreshBack(); };
        IsVisibleChanged += (_, _) => RefreshBack();
        Loaded += (_, _) => AssignBar((MyScrollBarView?)GetTemplateChild("PART_VerticalScrollBar"));
        PreviewGotKeyboardFocus += (_, args) => { if (args.NewFocus is MySliderView) args.Handled = true; };
    }
    private void Wheel(object sender, MouseWheelEventArgs args) {
        if (args.Delta == 0 || ActualHeight == 0 || ScrollableHeight == 0) return;
        Type sourceType = args.Source.GetType();
        object? parent = Content is FrameworkElement element ? element.TemplatedParent : NewLateBinding.LateGet(Content, null, "TemplatedParent", Array.Empty<object>(), null, null, null);
        if (parent is null && (
            (typeof(ComboBox).IsAssignableFrom(sourceType) && ((ComboBox)args.Source).IsDropDownOpen) ||
            (typeof(TextBox).IsAssignableFrom(sourceType) && ((TextBox)args.Source).AcceptsReturn) || typeof(ComboBoxItem).IsAssignableFrom(sourceType) ||
            (args.Source is Visual visual && PresentationSource.FromVisual(visual)?.RootVisual?.GetType().FullName == "System.Windows.Controls.Primitives.PopupRoot"))) return;
        args.Handled = true; PerformVerticalOffsetDelta(-args.Delta);
    }
    public void PerformVerticalOffsetDelta(double delta) => ControlServices.Current.Animations.Start(
        ControlServices.Current.Animations.Double(value => { realOffset = (realOffset + value).Clamp(0, ExtentHeight - ActualHeight); ScrollToVerticalOffset(realOffset); },
            delta * DeltaMult, ControlAnimationSpec.WithRawPower(300, 6)));
}
