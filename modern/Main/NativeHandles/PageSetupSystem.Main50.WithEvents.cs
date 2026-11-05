using System.Runtime.CompilerServices;
namespace PCL;
public partial class PageSetupSystem {
    private global::PCL.MyComboBox _lfComboLauncherUpdateSource;
    internal virtual global::PCL.MyComboBox ComboLauncherUpdateSource {
        get => _lfComboLauncherUpdateSource;
        [MethodImpl(MethodImplOptions.Synchronized)]
        set {
            _lfComboLauncherUpdateSource = value;
        }
    }
    private global::PCL.MyComboBox _lfComboLauncherUpdateChannel;
    internal virtual global::PCL.MyComboBox ComboLauncherUpdateChannel {
        get => _lfComboLauncherUpdateChannel;
        [MethodImpl(MethodImplOptions.Synchronized)]
        set {
            if (_lfComboLauncherUpdateChannel is not null) _lfComboLauncherUpdateChannel.SelectionChanged -= UpdateChannelChanged;
            _lfComboLauncherUpdateChannel = value;
            if (_lfComboLauncherUpdateChannel is not null) _lfComboLauncherUpdateChannel.SelectionChanged += UpdateChannelChanged;
        }
    }
}
