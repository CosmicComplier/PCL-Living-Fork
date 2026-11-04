using PCLLF.Presentation.Foundation;
namespace PCL;
internal static class ModernTooltipTheme {
    private static readonly LegacyTooltipThemeAdapter Theme = LegacyTooltipThemeAdapter.ForNativeTooltip();
    internal static void Install() => Theme.Install();
}
