using System.Runtime.CompilerServices;
namespace PCL;
public partial class PageSetupUI {
    private global::PCL.MyComboBox _lfComboLauncherColorMode;
    internal virtual global::PCL.MyComboBox ComboLauncherColorMode {
        get => _lfComboLauncherColorMode;
        [MethodImpl(MethodImplOptions.Synchronized)]
        set {
            _lfComboLauncherColorMode = value;
        }
    }
    private global::PCL.MySlider _lfSliderPanelOpacity;
    internal virtual global::PCL.MySlider SliderPanelOpacity {
        get => _lfSliderPanelOpacity;
        [MethodImpl(MethodImplOptions.Synchronized)]
        set {
            _lfSliderPanelOpacity = value;
        }
    }
}
