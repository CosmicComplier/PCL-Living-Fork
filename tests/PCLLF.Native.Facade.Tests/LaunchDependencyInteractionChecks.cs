using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using PCL;
using PCLCS;

namespace PCLLF.Native.Facade.Tests;

internal static partial class DependencyCases<TContext, TMod, TDependency, TEdge, TIssue, TImpact, TReport, TOnlineProject, TOnlineDependency, TOnlineFile, TOptions, TCache>
{
    private static void LaunchLoginPort()
    {
        Type input = Native.Type("PCL.ModLaunch+McLoginLegacy"), result = Native.Type("PCL.ModLaunch+McLoginResult");
        Type loaderType = typeof(ModLoader.LoaderTask<,>).MakeGenericType(input, result);
        object loader = Activator.CreateInstance(loaderType)!;
        MethodInfo factory = Native.Method(Native.Type("PCL.ModLaunch"), "LoginPort").MakeGenericMethod(input);
        object port = factory.Invoke(null, [loader])!;
        object live = Native.Property(port.GetType(), "Output").GetValue(port)!;
        LD(39, ReferenceEquals(Native.Get(port, "Source"), loader));
        string[] names = ["Name", "Uuid", "AccessToken", "Type", "ClientToken", "ProfileJson"];
        foreach (string name in names) Native.Property(live.GetType(), name).SetValue(live, "port-" + name);
        FieldInfo output = Native.Field(loaderType, "Output"); object actual = output.GetValue(loader)!;
        foreach (string name in names) LD(43, (string)Native.Get(actual, name)! == "port-" + name, name);
        object direct = Activator.CreateInstance(result)!;
        foreach (string name in names) Native.Set(direct, name, "direct-" + name);
        output.SetValue(loader, direct);
        foreach (string name in names) LD(47, (string)Native.Property(live.GetType(), name).GetValue(live)! == "direct-" + name, name);
        LegacyLaunchLoginResult replacement = new();
        foreach (string name in names) Native.Property(replacement.GetType(), name).SetValue(replacement, "whole-" + name);
        Native.Property(port.GetType(), "Output").SetValue(port, replacement); actual = output.GetValue(loader)!;
        foreach (string name in names) LD(52, (string)Native.Get(actual, name)! == "whole-" + name, name);
        LD(53, ReferenceEquals(live, Native.Property(port.GetType(), "Output").GetValue(port)));
        object actualInput = Activator.CreateInstance(input)!; Native.Set(loader, "Input", actualInput);
        LD(55, ReferenceEquals(actualInput, Native.Property(port.GetType(), "Input").GetValue(port)));
        Native.Property(port.GetType(), "State").SetValue(port, LegacyLaunchState.Loading);
        Native.Property(port.GetType(), "Progress").SetValue(port, 0.375);
        LD(60, (double)Native.Property(loaderType, "Progress").GetValue(loader)! == 0.375 && Native.Property(loaderType, "State").GetValue(loader)!.ToString() == "Loading" && Native.Get(loader, "LastRunningThread") is null);
        Native.Property(port.GetType(), "State").SetValue(port, LegacyLaunchState.Waiting);
        LD(62, Native.Property(loaderType, "State").GetValue(loader)!.ToString() == "Waiting" && (double)Native.Property(port.GetType(), "Progress").GetValue(port)! == 0 && Native.Get(loader, "LastRunningThread") is null);
    }

    private static T LaunchPart<T>(FrameworkElement view, string name) where T : class
        => view.FindName(name) as T ?? throw new InvalidOperationException("Actual native BAML part missing: " + view.GetType().Name + "." + name);

    private static void LaunchToggles()
    {
        var checkEntry = Settings.Entries["LaunchModDependencyCheck"];
        var autoEntry = Settings.Entries["LaunchModDependencyAutoFill"];
        object previousCheck = checkEntry.ValueCache[""], previousAuto = autoEntry.ValueCache[""];
        Window? window = null;
        try
        {
            Native.Cache("LaunchModDependencyCheck", true); Native.Cache("LaunchModDependencyAutoFill", true);
            Native.Static("PCL.ModSecret", "ThemeRefresh", 0);
            PageLaunchRight page = new();
            MyCheckBox master = LaunchPart<MyCheckBox>(page, "CheckModDependencies"), auto = LaunchPart<MyCheckBox>(page, "CheckAutoFillDependencies");
            MyCard card = LaunchPart<MyCard>(page, "CardDependencyChecks"), recent = LaunchPart<MyCard>(page, "CardRecentWorlds");
            TD(20, master.Checked && auto.Checked && auto.Visibility == Visibility.Visible);
            FrameworkElement anchor = LaunchPart<FrameworkElement>(page, "PanDependencyOptionsAnchor");
            ((Panel)anchor.Parent).Children.Remove(anchor); ((Panel)recent.Parent).Children.Remove(recent);
            Grid host = new(); host.Children.Add(anchor); host.Children.Add(recent);
            card.Visibility = recent.Visibility = Visibility.Visible; card.Opacity = recent.Opacity = 1;
            window = new Window { Content = host, Width = 320, Height = 300, Left = -30000, Top = -30000, ShowInTaskbar = false, ShowActivated = false };
            window.Show(); window.UpdateLayout(); TD(27, master.IsLoaded);
            Point origin = new(0, 0);
            foreach (bool value in new[] { false, true, false, true })
            {
                // Original deliberate no-op Settings.Set path: the cache already
                // equals the click. This tests live control reactions, not disk.
                Native.Cache("LaunchModDependencyCheck", value); master.SetChecked(value, true); window.UpdateLayout();
                TD(33, auto.Visibility == (value ? Visibility.Visible : Visibility.Collapsed));
                double gap = recent.TranslatePoint(origin, host).Y - card.TranslatePoint(origin, host).Y - card.ActualHeight;
                TD(35, Math.Abs(gap - 10) <= 0.1, "actual gap=" + gap);
                if (value) TD(36, auto.TranslatePoint(origin, window).Y >= master.TranslatePoint(origin, window).Y + master.ActualHeight);
            }
            Native.Cache("LaunchModDependencyAutoFill", false); auto.SetChecked(false, true);
            Native.Call(page.GetType(), "RefreshDependencyOptions", page);
            TD(40, !auto.Checked && master.Checked && auto.Visibility == Visibility.Visible);
            Native.Cache("LaunchModDependencyCheck", false); master.SetChecked(false, true);
            Native.Call(page.GetType(), "RefreshDependencyOptions", page);
            TD(43, !master.Checked && auto.Visibility == Visibility.Collapsed);
            Native.Cache("LaunchModDependencyAutoFill", false);
            PageLaunchRight reopened = new();
            TD(46, !LaunchPart<MyCheckBox>(reopened, "CheckModDependencies").Checked && !LaunchPart<MyCheckBox>(reopened, "CheckAutoFillDependencies").Checked && LaunchPart<MyCheckBox>(reopened, "CheckAutoFillDependencies").Visibility == Visibility.Collapsed);
            TD(47, checkEntry.DefaultValue is true && autoEntry.DefaultValue is true);
        }
        finally
        {
            if (window is not null) { window.Content = null; window.Close(); }
            Native.Cache("LaunchModDependencyCheck", previousCheck); Native.Cache("LaunchModDependencyAutoFill", previousAuto);
            LaunchDrainUi();
        }
        Console.WriteLine("PASS toggles original 20..47: actual first-page BAML, real loaded checkbox clicks, exact 10px gap, default-on and player-choice persistence without settings callbacks/disk writes.");
    }

    private static void LaunchDrainUi()
    {
        DispatcherFrame frame = new();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }
}
