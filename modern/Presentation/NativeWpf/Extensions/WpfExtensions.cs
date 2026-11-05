// Fixed LF Shared c774ae4c85fd94e0eaa171db21e78a65870df19b Wpf/Extensions/WpfExtensions.cs; Apache-2.0 (MeloongCore/LICENSE.txt).
// Modern Presentation source group only; no legacy MeloongCore.Wpf assembly reference.
using System.Windows.Controls;

namespace MeloongCore.Wpf.Extensions;
public static class WpfExtensions {

    public static bool Any(this UIElementCollection? arr) 
        => arr?.Count > 0;

}
