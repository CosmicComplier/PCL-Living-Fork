using PCLLF.Presentation.Foundation;
using System;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualBasic.CompilerServices;
using Path = System.Windows.Shapes.Path;

namespace PCLLF.Presentation.Controls;

// LF Controls/MyMenuItem.vb. Keep the real MenuItem DP/checked/routed-event behavior and original style key/type.
// CE 1a7653127d578efdb2a9388999a3fdc67d9e49d5 Controls/MyMenuItem.cs compared; no template/theme substitution.
public class MyMenuItemView : MenuItem {
    public int Uuid;
    private string? colorName;
    protected virtual int LauncherTransparency => throw new InvalidOperationException("The product UiLauncherTransparent settings host is required.");
    public MyMenuItemView() {
        Uuid = ControlServices.Current.NewUuid();
        Loaded += (_, _) => {
            if (Icon is not null) { Path? path = (Path?)GetTemplateChild("Icon"); if (path is not null) path.Data = (Geometry)new GeometryConverter().ConvertFromString(Conversions.ToString(Icon)!)!; }
            if (Parent is ContextMenu menu) menu.Opacity = LauncherTransparency / 1000.0 + .4;
        };
        MouseEnter += (_, _) => RefreshColor(); MouseLeave += (_, _) => RefreshColor(); IsEnabledChanged += (_, _) => RefreshColor();
        Click += (_, _) => ControlServices.Current.RaiseCustomEvent(this);
    
        if (IsInitialized) LegacyThemePalette.Watch(this, RefreshColor);
        else Initialized += (_, _) => LegacyThemePalette.Watch(this, RefreshColor);
    }
    private void RefreshColor() {
        string back, fore; int time;
        if (!IsEnabled) { back = "ColorBrushTransparent"; fore = "ColorBrushGray5"; time = 200; }
        else if (IsMouseOver) { back = "ColorBrush6"; fore = "ColorBrush2"; time = 100; }
        else { back = "ColorBrushTransparent"; fore = "ColorBrush1"; time = 200; }
        if (colorName == back) return; colorName = back;
        var services = ControlServices.Current;
        if (IsLoaded && services.AnimationEnabled()) services.Animations.Start(new[] {
            services.Animations.ColorResource(this, BackgroundProperty, back, new(time)), services.Animations.ColorResource(this, ForegroundProperty, fore, new(time))
        }, "MyMenuItem Color " + Uuid);
        else { services.Animations.Stop("MyMenuItem Color " + Uuid); SetResourceReference(BackgroundProperty, back); SetResourceReference(ForegroundProperty, fore); }
    }
}
