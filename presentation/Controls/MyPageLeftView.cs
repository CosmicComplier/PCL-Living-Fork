using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PCLLF.Presentation.Controls;

// LF MyPageLeft.vb; preserve its traversal and eager realization within the actual named animation root.
public class MyPageLeftView : Grid {
    private readonly int uuid;
    private static ControlAnimations Animations => ControlServices.Current.Animations;
    public MyPageLeftView() { uuid = ControlServices.Current.NewUuid(); }
    public string? AnimatedControl { get => (string?)GetValue(AnimatedControlProperty); set => SetValue(AnimatedControlProperty, value); }
    public static readonly DependencyProperty AnimatedControlProperty = DependencyProperty.Register(nameof(AnimatedControl), typeof(string), typeof(MyPageLeftView), new PropertyMetadata(null));
    public void TriggerShowAnimation() {
        if (AnimatedControl is null) {
            if (RenderTransform is not ScaleTransform) { RenderTransform = new ScaleTransform(.96, .96); RenderTransformOrigin = new Point(.5, .5); }
            Opacity = 0; Animations.Start(new[] { Animations.ScaleTransform(this, 1 - ((ScaleTransform)RenderTransform).ScaleX, new(400, ease: ControlEase.OutBack, power: ControlEasePower.Weak)), Animations.Opacity(this, 1, new(100)) }, "PageLeft PageChange " + uuid);
        } else {
            List<ControlAnimation> frames = new(); int id = 0, delay = 0;
            foreach (FrameworkElement original in AllControls(true)) {
                FrameworkElement element = MyVirtualizingElementView.TryInit(original);
                if (element.Visibility == Visibility.Collapsed) { element.Opacity = 1; element.RenderTransform = new TranslateTransform(0, 0); if (element is MyListItemView item) item.IsMouseOverAnimationEnabled = true; }
                else {
                    element.Opacity = 0; element.RenderTransform = new TranslateTransform(-25, 0); if (element is MyListItemView item) item.IsMouseOverAnimationEnabled = false;
                    frames.Add(Animations.Opacity(element, element is TextBlock ? .6 : 1, new(100, delay, ControlEase.OutFluent, ControlEasePower.Weak)));
                    frames.Add(Animations.TranslateX(element, 5, new(200, delay, ControlEase.OutFluent)));
                    frames.Add(Animations.TranslateX(element, 20, new(300, delay, ControlEase.OutBack, ControlEasePower.Weak)));
                    if (element is MyListItemView list) frames.Add(Animations.Code(() => { list.IsMouseOverAnimationEnabled = true; list.RefreshColor(this, new EventArgs()); }, delay + 280));
                    delay = checked(delay + Math.Max(15 - id, 7) * 2); id++;
                }
            }
            Animations.Start(frames, "PageLeft PageChange " + uuid);
        }
    }
    public void TriggerHideAnimation() {
        if (AnimatedControl is null) {
            if (RenderTransform is not ScaleTransform) { RenderTransform = new ScaleTransform(1, 1); RenderTransformOrigin = new Point(.5, .5); }
            Animations.Start(new[] { Animations.ScaleTransform(this, .95 - ((ScaleTransform)RenderTransform).ScaleX, new(110, ease: ControlEase.InFluent, power: ControlEasePower.Weak)), Animations.Opacity(this, -Opacity, new(80, 30)) }, "PageLeft PageChange " + uuid);
        } else {
            List<ControlAnimation> frames = new(); int id = 0; List<FrameworkElement> controls = AllControls();
            foreach (FrameworkElement element in controls) {
                int delay = Convert.ToInt32(Math.Round(70.0 / controls.Count * id));
                frames.Add(Animations.Opacity(element, -element.Opacity, new(50, delay))); frames.Add(Animations.TranslateX(element, -6, new(50, delay))); id++;
            }
            Animations.Start(frames, "PageLeft PageChange " + uuid);
        }
    }
    private List<FrameworkElement> AllControls(bool ignore = false) { List<FrameworkElement> result = new(); Collect((FrameworkElement)FindName(AnimatedControl!)!, result, ignore); return result; }
    private void Collect(FrameworkElement element, List<FrameworkElement> result, bool ignore) {
        if (!ignore && element.Visibility == Visibility.Collapsed) return;
        if (element is MyTextButtonView or MyListItemView) result.Add(element);
        else if (element is ContentControl content) Collect((FrameworkElement)content.Content, result, ignore);
        else if (element is Panel panel) foreach (FrameworkElement child in panel.Children) Collect(child, result, ignore);
        else result.Add(element);
    }
}
