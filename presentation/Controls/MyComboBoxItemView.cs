using PCLLF.Presentation.Foundation;
using System;
using System.Windows.Controls;

namespace PCLLF.Presentation.Controls;

// LF Controls/MyComboBoxItem.vb; original key/template, selection and pointer color transitions retained.
public class MyComboBoxItemView : ComboBoxItem {
    public int Uuid;
    private string? backColor;
    private double fontOpacity;
    public MyComboBoxItemView() {
        Uuid = ControlServices.Current.NewUuid(); Style = (System.Windows.Style)FindResource("MyComboBoxItem");
        Unselected += (_, _) => RefreshColor(); MouseMove += (_, _) => RefreshColor(); MouseLeave += (_, _) => RefreshColor();
        Selected += (_, _) => RefreshColor(); IsEnabledChanged += (_, _) => RefreshColor();
        MouseLeftButtonUp += (_, _) => ControlServices.Current.Info("选择下拉列表项：" + ToString());
    
        if (IsInitialized) LegacyThemePalette.Watch(this, RefreshColor);
        else Initialized += (_, _) => LegacyThemePalette.Watch(this, RefreshColor);
    }
    private void RefreshColor() {
        string color; double opacity; int time;
        if (IsSelected) { color = "ColorBrush6"; opacity = 1; time = 100; }
        else if (IsMouseOver) { color = "ColorBrush8"; opacity = 1; time = 100; }
        else { color = "ColorBrushTransparent"; opacity = IsEnabled ? 1 : .4; time = 300; }
        if (backColor == color && fontOpacity == opacity) return; backColor = color; fontOpacity = opacity;
        var services = ControlServices.Current;
        if (IsLoaded && services.AnimationEnabled()) services.Animations.Start(new[] {
            services.Animations.ColorResource(this, BackgroundProperty, backColor, new(time)), services.Animations.Opacity(this, fontOpacity - Opacity, new(time))
        }, "ComboBoxItem Color " + Uuid);
        else { services.Animations.Stop("ComboBoxItem Color " + Uuid); SetResourceReference(BackgroundProperty, backColor); Opacity = fontOpacity; }
    }
    public override string ToString() => Content.ToString()!;
    public static implicit operator string(MyComboBoxItemView value) => value.Content.ToString()!;
}
