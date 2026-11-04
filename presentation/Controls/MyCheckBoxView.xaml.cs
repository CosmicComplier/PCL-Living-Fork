using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;

namespace PCLLF.Presentation.Controls;

// LF Controls/MyCheckBox.xaml(.vb) is the behavior and visual source, under ../../LICENCE.
// CE 1a7653127d578efdb2a9388999a3fdc67d9e49d5 Controls/MyCheckBox.xaml.cs
// is a reference for C# constructor/DP syntax only; its bool? and three-state logic are not reused.
// Any CE-derived reference remains subject to its root LICENSE and PCL application LICENCE.
[ContentProperty("Inlines")]
public partial class MyCheckBoxView : Grid {
    public delegate void ChangeEventHandler(object sender, bool user);
    public delegate void PreviewChangeEventHandler(object sender, ControlPreviewEventArgs args);

    public event ChangeEventHandler? Change;
    public event PreviewChangeEventHandler? PreviewChange;
    public int Uuid;

    private const int AnimationTimeOfCheck = 150;
    private const int AnimationTimeOfMouseIn = 100;
    private const int AnimationTimeOfMouseOut = 200;
    private bool mouseDowned;
    private bool allowMouseDown = true;
    private static ControlServiceHost Services => ControlServices.Current;
    private static ControlAnimations Animations => Services.Animations;

    public MyCheckBoxView() {
        Uuid = Services.NewUuid();
        InitializeComponent();
        MouseLeftButtonUp += (_, _) => CheckboxMouseUp();
        MouseLeftButtonDown += (_, _) => CheckboxMouseDown();
        MouseLeave += (_, _) => CheckboxMouseLeave();
        IsEnabledChanged += (_, _) => CheckboxIsEnabledChanged();
        MouseEnter += (_, _) => CheckboxMouseEnterAnimation();
        MouseLeave += (_, _) => CheckboxMouseLeaveAnimation();
    }

    public bool Checked {
        get => (bool)GetValue(CheckedProperty);
        set => SetChecked(value, false);
    }

    public static readonly DependencyProperty CheckedProperty = DependencyProperty.Register("Checked", typeof(bool),
        typeof(MyCheckBoxView), new PropertyMetadata(false, (sender, _) => {
            // XAML can set the DP without calling the CLR setter; preserve LF's initial visual sync.
            MyCheckBoxView control = (MyCheckBoxView)sender;
            if (!control.IsLoaded) control.SyncUI();
        }));

    public void SetChecked(bool value, bool user) {
        try {
            if (value == Checked) return;
            if (value && user && RaisePreviewChange(user)) {
                mouseDowned = true;
                CheckboxMouseLeave();
                mouseDowned = false;
                return;
            }
            SetValue(CheckedProperty, value);
            if (IsLoaded) OnChange(user);
            SyncUI();
            Services.RaiseCustomEvent(this);
        } catch (Exception error) {
            Services.Warn(error, "设置 Checked 失败");
        }
    }

    protected virtual void OnChange(bool user) => Change?.Invoke(this, user);
    protected virtual bool RaisePreviewChange(bool user) {
        ControlPreviewEventArgs args = new(user);
        PreviewChange?.Invoke(this, args);
        return args.Handled;
    }

    private void SyncUI() {
        if (Services.AnimationEnabled() && IsLoaded) {
            allowMouseDown = false;
            if (Checked) {
                Animations.Start(new[] {
                    Animations.Scale(ShapeBorder, 12 - ShapeBorder.Width,
                        new ControlAnimationSpec(AnimationTimeOfCheck, ease: ControlEase.OutFluent), true),
                    Animations.ScaleTransform(ShapeCheck, 1 - ((ScaleTransform)ShapeCheck.RenderTransform).ScaleX,
                        new ControlAnimationSpec(AnimationTimeOfCheck * 2, Convert.ToInt32(AnimationTimeOfCheck * 0.7), ControlEase.OutBack, ControlEasePower.Weak)),
                    Animations.Scale(ShapeBorder, 6,
                        new ControlAnimationSpec(AnimationTimeOfCheck * 2, Convert.ToInt32(AnimationTimeOfCheck * 0.7), ControlEase.OutBack), true)
                }, "MyCheckBox Scale " + Uuid);
                Animations.Start(new[] {
                    Animations.ColorResource(ShapeBorder, Border.BorderBrushProperty,
                        IsEnabled ? IsMouseOver ? "ColorBrush3" : "ColorBrush2" : "ColorBrushGray5",
                        new ControlAnimationSpec(AnimationTimeOfCheck))
                }, "MyCheckBox BorderColor " + Uuid);
                Animations.Start(new[] {
                    Animations.Code(() => allowMouseDown = true, AnimationTimeOfCheck * 2)
                }, "MyCheckBox AllowMouseDown " + Uuid);
            } else {
                Animations.Start(new[] {
                    Animations.Scale(ShapeBorder, 12 - ShapeBorder.Width,
                        new ControlAnimationSpec(AnimationTimeOfCheck, ease: ControlEase.OutFluent), true),
                    Animations.ScaleTransform(ShapeCheck, -((ScaleTransform)ShapeCheck.RenderTransform).ScaleX,
                        new ControlAnimationSpec(Convert.ToInt32(AnimationTimeOfCheck * 0.9), ease: ControlEase.InFluent, power: ControlEasePower.Weak)),
                    Animations.Scale(ShapeBorder, 6,
                        new ControlAnimationSpec(AnimationTimeOfCheck * 2, Convert.ToInt32(AnimationTimeOfCheck * 0.7), ControlEase.OutBack), true)
                }, "MyCheckBox Scale " + Uuid);
                Animations.Start(new[] {
                    Animations.ColorResource(ShapeBorder, Border.BorderBrushProperty,
                        IsEnabled ? IsMouseOver ? "ColorBrush3" : "ColorBrush1" : "ColorBrushGray5",
                        new ControlAnimationSpec(AnimationTimeOfCheck))
                }, "MyCheckBox BorderColor " + Uuid);
                Animations.Start(new[] {
                    Animations.Code(() => allowMouseDown = true, AnimationTimeOfCheck * 2)
                }, "MyCheckBox AllowMouseDown " + Uuid);
            }
        } else {
            Animations.Stop("MyCheckBox Scale " + Uuid);
            Animations.Stop("MyCheckBox BorderColor " + Uuid);
            Animations.Stop("MyCheckBox AllowMouseDown " + Uuid);
            if (Checked) {
                ((ScaleTransform)ShapeCheck.RenderTransform).ScaleX = 1;
                ((ScaleTransform)ShapeCheck.RenderTransform).ScaleY = 1;
                ShapeBorder.SetResourceReference(Border.BorderBrushProperty, IsEnabled ? "ColorBrush2" : "ColorBrushGray5");
            } else {
                ((ScaleTransform)ShapeCheck.RenderTransform).ScaleX = 0;
                ((ScaleTransform)ShapeCheck.RenderTransform).ScaleY = 0;
                ShapeBorder.SetResourceReference(Border.BorderBrushProperty, IsEnabled ? "ColorBrush1" : "ColorBrushGray5");
            }
        }
    }

    public InlineCollection Inlines => LabText.Inlines;
    public string? Text {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string),
        typeof(MyCheckBoxView), new PropertyMetadata((sender, args) => {
            if (sender is not null) ((MyCheckBoxView)sender).LabText.Text = (string)args.NewValue;
        }));

    private void CheckboxMouseUp() {
        if (!mouseDowned) return;
        Services.Info($"按下复选框（{!Checked}）：{Text}");
        mouseDowned = false;
        SetChecked(!Checked, true);
        Animations.Start(Animations.ColorResource(ShapeBorder, Border.BackgroundProperty, "ColorBrushHalfWhite",
            new ControlAnimationSpec(100)), "MyCheckBox Background " + Uuid);
    }

    private void CheckboxMouseDown() {
        if (!allowMouseDown) return;
        mouseDowned = true;
        Focus();
        Animations.Start(Animations.ColorResource(ShapeBorder, Border.BackgroundProperty, "ColorBrushBg1",
            new ControlAnimationSpec(100)), "MyCheckBox Background " + Uuid);
        if (Checked) {
            Animations.Start(new[] {
                Animations.Scale(ShapeBorder, 16.5 - ShapeBorder.Width,
                    new ControlAnimationSpec(1000, ease: ControlEase.OutFluent, power: ControlEasePower.Strong), true),
                Animations.ScaleTransform(ShapeCheck, 0.9 - ((ScaleTransform)ShapeCheck.RenderTransform).ScaleX,
                    new ControlAnimationSpec(1000, ease: ControlEase.OutFluent, power: ControlEasePower.Strong))
            }, "MyCheckBox Scale " + Uuid);
        } else {
            Animations.Start(Animations.Scale(ShapeBorder, 16.5 - ShapeBorder.Width,
                new ControlAnimationSpec(1000, ease: ControlEase.OutFluent, power: ControlEasePower.Strong), true), "MyCheckBox Scale " + Uuid);
        }
    }

    private void CheckboxMouseLeave() {
        if (!mouseDowned) return;
        mouseDowned = false;
        Animations.Start(Animations.ColorResource(ShapeBorder, Border.BackgroundProperty, "ColorBrushHalfWhite",
            new ControlAnimationSpec(100)), "MyCheckBox Background " + Uuid);
        if (Checked) {
            Animations.Start(new[] {
                Animations.Scale(ShapeBorder, 18 - ShapeBorder.Width,
                    new ControlAnimationSpec(400, ease: ControlEase.OutFluent, power: ControlEasePower.Strong), true),
                Animations.ScaleTransform(ShapeCheck, 1 - ((ScaleTransform)ShapeCheck.RenderTransform).ScaleX,
                    new ControlAnimationSpec(500, ease: ControlEase.OutFluent, power: ControlEasePower.Strong))
            }, "MyCheckBox Scale " + Uuid);
        } else {
            Animations.Start(Animations.Scale(ShapeBorder, 18 - ShapeBorder.Width,
                new ControlAnimationSpec(400, ease: ControlEase.OutFluent, power: ControlEasePower.Strong), true), "MyCheckBox Scale " + Uuid);
        }
    }

    private void CheckboxIsEnabledChanged() {
        if (IsLoaded && Services.AnimationEnabled()) {
            if (IsEnabled) CheckboxMouseLeaveAnimation();
            else {
                Animations.Start(new[] {
                    Animations.ColorTo(ShapeBorder, Border.BorderBrushProperty, Services.ThemeColor(ControlThemeColor.Gray5),
                        new ControlAnimationSpec(AnimationTimeOfMouseOut))
                }, "MyCheckBox BorderColor " + Uuid);
                Animations.Start(new[] {
                    Animations.ColorTo(LabText, TextBlock.ForegroundProperty, Services.ThemeColor(ControlThemeColor.Gray5),
                        new ControlAnimationSpec(AnimationTimeOfMouseOut))
                }, "MyCheckBox TextColor " + Uuid);
            }
        } else {
            Animations.Stop("MyCheckBox TextColor " + Uuid);
            Animations.Stop("MyCheckBox BorderColor " + Uuid);
            LabText.SetResourceReference(TextBlock.ForegroundProperty, IsEnabled ? "ColorBrush1" : "ColorBrushGray5");
            ShapeBorder.SetResourceReference(Border.BorderBrushProperty, IsEnabled ? Checked ? "ColorBrush2" : "ColorBrush1" : "ColorBrushGray5");
        }
    }

    private void CheckboxMouseEnterAnimation() {
        Animations.Start(new[] {
            Animations.ColorResource(LabText, TextBlock.ForegroundProperty, "ColorBrush3", new ControlAnimationSpec(AnimationTimeOfMouseIn))
        }, "MyCheckBox TextColor " + Uuid);
        Animations.Start(new[] {
            Animations.ColorResource(ShapeBorder, Border.BorderBrushProperty, "ColorBrush3", new ControlAnimationSpec(AnimationTimeOfMouseIn))
        }, "MyCheckBox BorderColor " + Uuid);
    }

    private void CheckboxMouseLeaveAnimation() {
        // LF's custom event may disable the control before its MouseLeave handler runs.
        if (!IsEnabled) return;
        Animations.Start(new[] {
            Animations.ColorResource(LabText, TextBlock.ForegroundProperty, IsEnabled ? "ColorBrush1" : "ColorBrushGray5",
                new ControlAnimationSpec(AnimationTimeOfMouseOut))
        }, "MyCheckBox TextColor " + Uuid);
        Animations.Start(new[] {
            Animations.ColorResource(ShapeBorder, Border.BorderBrushProperty,
                IsEnabled ? Checked ? "ColorBrush2" : "ColorBrush1" : "ColorBrushGray5", new ControlAnimationSpec(AnimationTimeOfMouseOut))
        }, "MyCheckBox BorderColor " + Uuid);
    }

    public void RefreshSetting(string newValue) { Checked = bool.Parse(newValue); }
    public string GetCurrentSetting() => Checked.ToString();
}
