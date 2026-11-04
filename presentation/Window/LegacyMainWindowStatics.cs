using System;
using System.Windows;
namespace PCLLF.Presentation.Windowing;
public static class LegacyMainWindowStatics {
    public static void UpdateBackground(Func<Window?> current, Action refresh) {
        if (current() is null || !current()!.IsLoaded) return;
        refresh();
    }
}
