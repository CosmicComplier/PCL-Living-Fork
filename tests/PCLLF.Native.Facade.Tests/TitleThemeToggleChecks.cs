using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PCL;
using PCLLF.Presentation.Foundation;

namespace PCLLF.Native.Facade.Tests;

internal static class TitleThemeToggleChecks {
    internal static void Run(FixtureEnvironment environment) {
        var app = System.Windows.Application.Current; var previousMain = app.MainWindow; var previousMode = app.ShutdownMode;
        var previousSetup = ModMain.FrmSetupUI;
        int previousTheme = Settings.Get<int>("UiLauncherColorMode");
        var button = new MyIconButton { Width = 28, Height = 28, Theme = MyIconButton.Themes.White, LogoScale = .72 };
        Native.Call(typeof(FormMain), "ConfigureThemeToggle", null, button);
        var header = new Grid { Width = 300, Height = 48, Background = System.Windows.Media.Brushes.SteelBlue };
        header.Children.Add(button);
        var window = new Window { Content = header, Width = 350, Height = 90, Left = -30000, Top = -30000, ShowActivated = false, ShowInTaskbar = false, WindowStyle = WindowStyle.None };
        try {
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            ModMain.FrmSetupUI = new PageSetupUI();
            window.Show(); window.UpdateLayout();
            Settings.Set("UiLauncherColorMode", 0);
            string moon = button.Logo;
            Click(button);
            Native.Check(Settings.Get<int>("UiLauncherColorMode") == 1 && LegacyThemePalette.IsDarkTheme && button.Logo != moon && button.ToolTip?.ToString() == "切换到浅色主题", "Title toggle failed to enter dark mode or refresh its icon.");
            Native.Check(((ComboBox)ModMain.FrmSetupUI.FindName("ComboLauncherColorMode")).SelectedIndex == 1, "Personalization selector did not follow the title toggle.");
            Click(button);
            Native.Check(Settings.Get<int>("UiLauncherColorMode") == 0 && !LegacyThemePalette.IsDarkTheme && button.Logo == moon, "Second title toggle failed to restore light mode.");
            ModBase.IniClearCache("Setup"); Settings.Entries["UiLauncherColorMode"].ValueCache.Clear();
            Native.Check(Settings.Get<int>("UiLauncherColorMode") == 0 && ModBase.ReadIni("Setup", "UiLauncherColorMode", "") == "0", "Title theme selection was not persisted.");
            Settings.Set("UiLauncherColorMode", 1);
            Native.Check(button.ToolTip?.ToString() == "切换到浅色主题", "External theme changes did not refresh the title toggle.");
            environment.AssertNoForbiddenIo();
            Console.WriteLine("PASS title theme toggle: genuine icon input, two-way palette switching, settings-selector sync, persisted readback and external theme changes.");
        } finally { window.Content = null; window.Close(); app.MainWindow = previousMain; app.ShutdownMode = previousMode; ModMain.FrmSetupUI = previousSetup; Settings.Set("UiLauncherColorMode", previousTheme); }
    }
    private static void Click(UIElement button) {
        foreach (var routed in new[] { UIElement.MouseLeftButtonDownEvent, UIElement.MouseLeftButtonUpEvent })
            button.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left) { RoutedEvent = routed });
    }
}
