using PCLLF.Presentation.Foundation;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Shapes;

namespace PCLLF.Presentation.Controls;

// LF MyRadioBox.xaml(.vb). CE 1a7653127d578efdb2a9388999a3fdc67d9e49d5
// Controls/MyRadioBox.xaml.cs was compared; LF permits an empty radio group and keeps its original event/DP behavior.
[ContentProperty(nameof(Inlines))]
public partial class MyRadioBoxView : Grid {
    public int Uuid;
    public event EventHandler<ControlPreviewEventArgs>? PreviewCheck, PreviewChange, Check, Changed;
    protected virtual bool OnPreviewCheck(bool user) { ControlPreviewEventArgs args = new(user); PreviewCheck?.Invoke(this, args); return args.Handled; }
    protected virtual void OnPreviewChange(bool user) => PreviewChange?.Invoke(this, new(user));
    protected virtual void OnCheck(bool user) => Check?.Invoke(this, new(user));
    protected virtual void OnChanged(bool user) => Changed?.Invoke(this, new(user));
    private static ControlServiceHost Services => ControlServices.Current;
    private static ControlAnimations Animations => Services.Animations;
    public MyRadioBoxView() {
        Uuid = Services.NewUuid();
        if (GetType().Assembly == typeof(MyRadioBoxView).Assembly) InitializeComponent();
        else if (!_contentLoaded) { _contentLoaded = true; CompiledControlLoader.Load(this, typeof(MyRadioBoxView), "controls/myradioboxview.xaml"); }
        MouseLeftButtonUp += (_, _) => MouseUpRadio(); MouseLeftButtonDown += (_, _) => MouseDownRadio(); MouseLeave += (_, _) => MouseLeaveRadio();
        IsEnabledChanged += (_, _) => EnabledChanged(); MouseEnter += (_, _) => MouseEnterAnimation(); MouseLeave += (_, _) => MouseLeaveAnimation();
    
        if (IsInitialized) LegacyThemePalette.Watch(this, EnabledChanged);
        else Initialized += (_, _) => LegacyThemePalette.Watch(this, EnabledChanged);
    }
    public bool Checked { get => (bool)GetValue(CheckedProperty); set => SetChecked(value, false); }
    public static readonly DependencyProperty CheckedProperty = DependencyProperty.Register(nameof(Checked), typeof(bool), typeof(MyRadioBoxView),
        new PropertyMetadata(false, (sender, _) => { MyRadioBoxView view = (MyRadioBoxView)sender; if (!view.IsLoaded) view.SyncUI(); }));
    public void SetChecked(bool value, bool user) {
        try {
            if (value && user && OnPreviewCheck(user)) { MouseLeaveRadio(); return; }
            bool changed = false;
            if (IsLoaded && value != Checked) OnPreviewChange(user);
            if (value != Checked) { SetValue(CheckedProperty, value); changed = true; }
            if (Parent is null) return;
            if (Parent is not Panel panel) throw new MissingMemberException(Parent.GetType().FullName, "Children");
            List<MyRadioBoxView> radios = new(); int count = 0;
            foreach (UIElement control in panel.Children) if (control is MyRadioBoxView radio) { radios.Add(radio); if (radio.Checked) count++; }
            if (count > 1) {
                if (Checked) { foreach (MyRadioBoxView radio in radios) if (radio.Checked && !radio.Equals(this)) radio.Checked = false; }
                else { bool first = false; foreach (MyRadioBoxView radio in radios) if (radio.Checked) { if (first) radio.Checked = false; else first = true; } }
            }
            if (changed) { if (Checked) OnCheck(user); OnChanged(user); Services.RaiseCustomEvent(this); SyncUI(); }
        } catch (Exception error) { Services.Error(error, "单选框勾选改变错误"); }
    }
    private void SyncUI() {
        if (Services.AnimationEnabled() && IsLoaded) {
            if (Checked) {
                if (ShapeDot.Opacity < .01) ShapeDot.Opacity = 1;
                Animations.Start(new[] {
                    Animations.Scale(ShapeBorder, 10 - ShapeBorder.Width, new(150, ease: ControlEase.OutFluent, power: ControlEasePower.Weak), true),
                    Animations.Scale(ShapeBorder, 8, new(300, 90, ControlEase.OutBack), true)
                }, "MyRadioBox Border " + Uuid);
                Animations.Start(new[] {
                    Animations.Scale(ShapeDot, 9 - ShapeDot.Width, new(390, ease: ControlEase.OutBack, power: ControlEasePower.Weak), true),
                    Animations.Opacity(ShapeDot, 1 - ShapeDot.Opacity, new(75, 90))
                }, "MyRadioBox Dot " + Uuid);
                Animations.Start(Animations.ColorResource(ShapeBorder, Ellipse.StrokeProperty, IsMouseOver ? "ColorBrush3" : IsEnabled ? "ColorBrush2" : "ColorBrushGray5", new(150)), "MyRadioBox BorderColor " + Uuid);
            } else {
                Animations.Start(Animations.Scale(ShapeBorder, 18 - ShapeBorder.Width, new(150, ease: ControlEase.OutFluent), true), "MyRadioBox Border " + Uuid);
                Animations.Start(new[] {
                    Animations.Scale(ShapeDot, -ShapeDot.Width, new(150, ease: ControlEase.InFluent), true),
                    Animations.Opacity(ShapeDot, -ShapeDot.Opacity, new(75, 30))
                }, "MyRadioBox Dot " + Uuid);
                Animations.Start(Animations.ColorResource(ShapeBorder, Ellipse.StrokeProperty, IsMouseOver ? "ColorBrush3" : IsEnabled ? "ColorBrush1" : "ColorBrushGray5", new(150)), "MyRadioBox BorderColor " + Uuid);
            }
        } else {
            Animations.Stop("MyRadioBox Border " + Uuid); Animations.Stop("MyRadioBox Dot " + Uuid); Animations.Stop("MyRadioBox BorderColor " + Uuid);
            if (Checked) { ShapeDot.Width = 9; ShapeDot.Height = 9; ShapeDot.Opacity = 1; ShapeDot.Margin = new Thickness(5.5, 0, 0, 0); }
            else { ShapeDot.Width = 0; ShapeDot.Height = 0; ShapeDot.Margin = new Thickness(10, 0, 0, 0); }
            ShapeBorder.SetResourceReference(Ellipse.StrokeProperty, IsEnabled ? (Checked ? "ColorBrush2" : "ColorBrush1") : "ColorBrushGray5");
        }
    }
    public InlineCollection Inlines => LabText.Inlines;
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(MyRadioBoxView),
        new PropertyMetadata((sender, args) => { if (sender is not null) ((MyRadioBoxView)sender).LabText.Text = (string)args.NewValue; }));
    private bool mouseDowned;
    private void MouseUpRadio() {
        if (!mouseDowned) return; Services.Info("按下单选框：" + Text); SetChecked(true, true); mouseDowned = false;
        Animations.Start(Animations.ColorResource(ShapeBorder, Ellipse.FillProperty, "ColorBrushHalfWhite", new(100)), "MyRadioBox Background " + Uuid);
    }
    private void MouseDownRadio() {
        mouseDowned = true; Focus(); Animations.Start(Animations.ColorResource(ShapeBorder, Ellipse.FillProperty, "ColorBrushBg1", new(100)), "MyRadioBox Background " + Uuid);
        if (!Checked) Animations.Start(Animations.Scale(ShapeBorder, 16.5 - ShapeBorder.Width, new(1000, ease: ControlEase.OutFluent, power: ControlEasePower.Strong), true), "MyRadioBox Border " + Uuid);
    }
    private void MouseLeaveRadio() {
        if (!mouseDowned) return; mouseDowned = false;
        Animations.Start(Animations.ColorResource(ShapeBorder, Ellipse.FillProperty, "ColorBrushHalfWhite", new(100)), "MyRadioBox Background " + Uuid);
        if (!Checked) Animations.Start(Animations.Scale(ShapeBorder, 18 - ShapeBorder.Width, new(400, ease: ControlEase.OutFluent, power: ControlEasePower.Strong), true), "MyRadioBox Border " + Uuid);
    }
    private void EnabledChanged() {
        if (IsLoaded && Services.AnimationEnabled()) {
            if (IsEnabled) MouseLeaveAnimation();
            else {
                Animations.Start(Animations.ColorTo(ShapeBorder, Ellipse.StrokeProperty, Services.ThemeColor(ControlThemeColor.Gray5), new(200)), "MyRadioBox BorderColor " + Uuid);
                Animations.Start(Animations.ColorTo(LabText, TextBlock.ForegroundProperty, Services.ThemeColor(ControlThemeColor.Gray5), new(200)), "MyRadioBox TextColor " + Uuid);
            }
        } else {
            Animations.Stop("MyRadioBox BorderColor " + Uuid); Animations.Stop("MyRadioBox TextColor " + Uuid);
            LabText.SetResourceReference(TextBlock.ForegroundProperty, IsEnabled ? "ColorBrush1" : "ColorBrushGray5");
            ShapeBorder.SetResourceReference(Ellipse.StrokeProperty, IsEnabled ? Checked ? "ColorBrush2" : "ColorBrush1" : "ColorBrushGray5");
        }
    }
    private void MouseEnterAnimation() {
        Animations.Start(Animations.ColorResource(ShapeBorder, Ellipse.StrokeProperty, "ColorBrush3", new(100)), "MyRadioBox BorderColor " + Uuid);
        Animations.Start(Animations.ColorResource(LabText, TextBlock.ForegroundProperty, "ColorBrush3", new(100)), "MyRadioBox TextColor " + Uuid);
    }
    private void MouseLeaveAnimation() {
        if (!IsEnabled) return;
        if (IsLoaded && Services.AnimationEnabled()) {
            Animations.Start(Animations.ColorResource(ShapeBorder, Ellipse.StrokeProperty, IsEnabled ? Checked ? "ColorBrush2" : "ColorBrush1" : "ColorBrushGray5", new(200)), "MyRadioBox BorderColor " + Uuid);
            Animations.Start(Animations.ColorResource(LabText, TextBlock.ForegroundProperty, IsEnabled ? "ColorBrush1" : "ColorBrushGray5", new(200)), "MyRadioBox TextColor " + Uuid);
        } else {
            Animations.Stop("MyRadioBox BorderColor " + Uuid); Animations.Stop("MyRadioBox TextColor " + Uuid);
            ShapeBorder.SetResourceReference(Ellipse.StrokeProperty, IsEnabled ? Checked ? "ColorBrush2" : "ColorBrush1" : "ColorBrushGray5");
            LabText.SetResourceReference(TextBlock.ForegroundProperty, IsEnabled ? "ColorBrush1" : "ColorBrushGray5");
        }
    }
}
