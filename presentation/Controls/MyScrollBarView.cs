using System;
using System.Windows.Controls.Primitives;

namespace PCLLF.Presentation.Controls;

// LF Controls/MyScrollBar.vb. CE 1a7653127d578efdb2a9388999a3fdc67d9e49d5 corresponding C# compared.
public class MyScrollBarView : ScrollBar {
    public int Uuid;
    public MyScrollBarView() {
        Uuid = ControlServices.Current.NewUuid();
        IsEnabledChanged += (_, _) => RefreshColor(); GotMouseCapture += (_, _) => RefreshColor(); LostMouseCapture += (_, _) => RefreshColor();
        MouseEnter += (_, _) => RefreshColor(); MouseLeave += (_, _) => RefreshColor(); IsVisibleChanged += (_, _) => RefreshColor();
    }
    private void RefreshColor() {
        try {
            double opacity; string color; int time;
            if (!IsVisible) { opacity = 0; time = 20; color = "ColorBrush4"; }
            else if (IsMouseCaptureWithin) { opacity = 1; color = "ColorBrush4"; time = 50; }
            else if (IsMouseOver) { opacity = .9; color = "ColorBrush3"; time = 130; }
            else { opacity = .5; color = "ColorBrush4"; time = 180; }
            var services = ControlServices.Current;
            if (IsLoaded && services.AnimationEnabled()) services.Animations.Start(new[] {
                services.Animations.ColorResource(this, ForegroundProperty, color, new(time)), services.Animations.Opacity(this, opacity - Opacity, new(time))
            }, "MyScrollBar Color " + Uuid);
            else { services.Animations.Stop("MyScrollBar Color " + Uuid); SetResourceReference(ForegroundProperty, color); Opacity = opacity; }
        } catch (Exception error) { ControlServices.Current.Warn(error, "滚动条颜色改变出错"); }
    }
}
