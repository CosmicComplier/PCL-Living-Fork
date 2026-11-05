using PCLLF.Presentation.Foundation;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.VisualBasic.CompilerServices;

namespace PCLLF.Presentation.Controls;

// LF Controls/MySlider.xaml(.vb); CE 1a7653127d578efdb2a9388999a3fdc67d9e49d5
// Controls/MySlider.xaml.cs was compared for syntax. Keep LF preview/user flags, rounding and global drag-host order.
public partial class MySliderView : Border {
    public int Uuid;
    public delegate void ChangeEventHandler(object sender, bool user);
    public event ChangeEventHandler? Change;
    public event EventHandler<ControlPreviewEventArgs>? PreviewChange;
    protected virtual void OnChange(bool user) => Change?.Invoke(this, user);
    protected virtual bool OnPreviewChange() { ControlPreviewEventArgs args = new(false); PreviewChange?.Invoke(this, args); return args.Handled; }
    private static ControlServiceHost Services => ControlServices.Current;
    private static ControlAnimations Animations => Services.Animations;
    protected virtual object? CurrentDragControl => null;
    protected virtual void SetDragControl(object value) => throw new InvalidOperationException("The product main-window drag host is required.");
    protected virtual void PerformMainDrag() => throw new InvalidOperationException("The product main-window drag host is required.");
    // LF passes the original physical DPI value to FormattedText, not a newly normalized ratio.
    protected virtual double PopupDpi => VisualTreeHelper.GetDpi(this).PixelsPerDip * 96;
    public MySliderView() {
        Uuid = Services.NewUuid();
        if (GetType().Assembly == typeof(MySliderView).Assembly) InitializeComponent();
        else if (!_contentLoaded) { _contentLoaded = true; CompiledControlLoader.Load(this, typeof(MySliderView), "controls/mysliderview.xaml"); }
        SizeChanged += RefreshWidth; MouseLeftButtonDown += DragStart;
        IsEnabledChanged += (_, _) => RefreshColor(); MouseEnter += (_, _) => RefreshColor(); MouseLeave += (_, _) => RefreshColor();
        MouseEnter += (_, _) => Focus(); KeyDown += SliderKeyDown;
    
        if (IsInitialized) LegacyThemePalette.Watch(this, RefreshColor);
        else Initialized += (_, _) => LegacyThemePalette.Watch(this, RefreshColor);
    }
    private int maxValue = 100;
    public int MaxValue { get => maxValue; set { if (value == maxValue) return; maxValue = value; RefreshWidth(null, null); } }
    private bool changeByKey;
    private int currentValue;
    public int Value {
        get => currentValue;
        set {
            try {
                int next = value < 0 ? 0 : value > MaxValue ? MaxValue : value;
                if (currentValue == next) return;
                int old = currentValue; currentValue = next;
                if (Services.AnimationEnabled() && OnPreviewChange()) { currentValue = old; DragStop(); return; }
                if (IsLoaded && Services.AnimationEnabled()) {
                    if (ActualWidth < ShapeDot.Width) return;
                    double newWidth = (double)currentValue / MaxValue * (ActualWidth - ShapeDot.Width);
                    double delta = Math.Abs(LineFore.Width / (ActualWidth - ShapeDot.Width) - (double)currentValue / MaxValue);
                    double time = (1 - Math.Pow(1 - delta, 3)) * 300 + (changeByKey ? 100 : 0);
                    ControlAnimationSpec spec = new(time, ease: time > 50 ? ControlEase.OutFluent : ControlEase.Linear);
                    Animations.Start(new[] {
                        Animations.Width(LineFore, Math.Max(0, newWidth + (newWidth < .5 ? 0 : .5)) - LineFore.Width, spec),
                        Animations.Width(LineBack, Math.Max(0, ActualWidth - ShapeDot.Width - newWidth + (ActualWidth - ShapeDot.Width - newWidth < .5 ? 0 : .5)) - LineBack.Width, spec),
                        Animations.X(ShapeDot, newWidth - ShapeDot.Margin.Left, spec)
                    }, "MySlider Progress " + Uuid);
                } else RefreshWidth(null, null);
                if (Services.AnimationEnabled()) OnChange(false);
            } catch (Exception error) { Services.Error(error, "滑动条进度改变出错"); }
        }
    }
    private void RefreshWidth(object? sender, SizeChangedEventArgs? args) {
        if (args is not null) PanMain.Width = args.NewSize.Width;
        Animations.Stop("MySlider Progress " + Uuid);
        double width = (double)currentValue / MaxValue * (ActualWidth - ShapeDot.Width);
        LineFore.Width = Math.Max(0, width + (width < .5 ? 0 : .5));
        LineBack.Width = Math.Max(0, ActualWidth - ShapeDot.Width - width + (ActualWidth - ShapeDot.Width - width < .5 ? 0 : .5));
        ShapeDot.Margin = new Thickness(width, ShapeDot.Margin.Top, ShapeDot.Margin.Right, ShapeDot.Margin.Bottom);
    }
    public Delegate? GetHintText;
    private void DragStart(object sender, MouseButtonEventArgs args) {
        args.Handled = true; SetDragControl(this); RefreshColor(); PerformMainDrag();
        Animations.Start(Animations.ScaleTransform(ShapeDot, 1.3 - ((ScaleTransform)ShapeDot.RenderTransform).ScaleX, new(40, ease: ControlEase.OutFluent)), "MySlider Scale " + Uuid);
        RefreshPopup(); Animations.Stop("MySlider KeyPopup " + Uuid);
    }
    public void DragDoing() {
        double percent = (Mouse.GetPosition(PanMain).X - ShapeDot.Width / 2) / (ActualWidth - ShapeDot.Width);
        // Same IComparable<double> clamp semantics as Shared (including NaN).
        percent = percent.CompareTo(0.0) < 0 ? 0 : percent.CompareTo(1.0) > 0 ? 1 : percent;
        int next = Convert.ToInt32(Math.Round(percent * MaxValue)); if (next != Value) Value = next; RefreshPopup();
    }
    public void DragStop() {
        RefreshColor(); Animations.Start(Animations.ScaleTransform(ShapeDot, 1 - ((ScaleTransform)ShapeDot.RenderTransform).ScaleX, new(200, ease: ControlEase.OutFluent)), "MySlider Scale " + Uuid);
        Popup.IsOpen = false;
    }
    public void RefreshPopup() {
        if (GetHintText is null) return;
        Popup.IsOpen = true; TextHint.Text = Conversions.ToString(GetHintText.DynamicInvoke(Value));
        Typeface typeface = new(TextHint.FontFamily, TextHint.FontStyle, TextHint.FontWeight, TextHint.FontStretch);
        FormattedText formatted = new(TextHint.Text, Thread.CurrentThread.CurrentCulture, TextHint.FlowDirection, typeface, TextHint.FontSize, TextHint.Foreground, PopupDpi);
        TextHint.Width = formatted.Width;
    }
    private void RefreshColor() {
        try {
            string foreground, fill; int time;
            if (IsEnabled) {
                if ((CurrentDragControl is not null && CurrentDragControl.Equals(this)) || IsMouseOver) { foreground = "ColorBrush3"; fill = "ColorBrush3"; time = 40; }
                else { foreground = "ColorBrushBg0"; fill = "ColorBrushBg0"; time = 100; }
            } else { foreground = "ColorBrushGray5"; fill = "ColorBrushGray5"; time = 200; }
            if (IsLoaded && Services.AnimationEnabled()) {
                Animations.Start(new[] { Animations.ColorResource(this, BorderBrushProperty, foreground, new(time)), Animations.ColorResource(ShapeDot, Ellipse.FillProperty, fill, new(time)) }, "MySlider Color " + Uuid);
            } else { Animations.Stop("MySlider Color " + Uuid); SetResourceReference(BorderBrushProperty, foreground); ShapeDot.SetResourceReference(Ellipse.FillProperty, fill); }
        } catch (Exception error) { Services.Warn(error, "滑动条颜色改变出错"); }
    }
    public uint ValueByKey { get; set; } = 1;
    private void SliderKeyDown(object sender, KeyEventArgs args) {
        if (ReferenceEquals(this, CurrentDragControl)) return;
        if (args.Key == Key.Left) { changeByKey = true; Value = checked((int)((long)Value - ValueByKey)); changeByKey = false; args.Handled = true; }
        else if (args.Key == Key.Right) { changeByKey = true; Value = checked((int)((long)Value + ValueByKey)); changeByKey = false; args.Handled = true; }
        else return;
        if (GetHintText is not null) {
            RefreshPopup(); Animations.Stop("MySlider KeyPopup " + Uuid);
            Animations.Start(Animations.Code(() => Popup.IsOpen = false, Convert.ToInt32(Math.Round(700 * Animations.Speed))), "MySlider KeyPopup " + Uuid);
        }
    }
}
