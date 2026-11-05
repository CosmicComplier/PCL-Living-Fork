using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PCL;
using static PCLLF.Native.Facade.Tests.LocalFeatureChecks;

namespace PCLLF.Native.Facade.Tests;
internal static class LocalFeatureRecentChecks
{
    private static void R(int line, bool value) => Check("recent", line, value);
    internal static void Run(Scope scope)
    {
        PageLaunchRight? page = null;
        var tick = Native.Method(Native.Type("PCL.ModAnimation"), "AniTimer"); var last = Native.Field(Native.Type("PCL.ModAnimation"), "AniLastTick"); object? saved = last.GetValue(null);
        try {
            page = new PageLaunchRight(); object world = New("PCL.RecentWorld");
            string game = Path.Combine(scope.Root, "world-card"), version = Path.Combine(game, "versions/example");
            Native.Set(world, "VersionPath", version); Native.Set(world, "GameFolder", game); Native.Set(world, "Identifier", "World"); Native.Set(world, "Name", "World"); Native.Set(world, "PlayedUtc", DateTime.UtcNow);
            // Original fixture paths were nonexistent D:\Fixture values. These are
            // equally nonexistent owned GUID values, so an accidental IO remains contained.
            Type tuple = typeof(Tuple<,>).MakeGenericType(world.GetType(), typeof(ImageSource));
            object record = Activator.CreateInstance(tuple, world, null)!;
            Native.Call(page.GetType(), "ShowRecentWorld", page, record);
            MyCard card = Part<MyCard>(page, "CardRecentWorlds"); TranslateTransform transform = Part<TranslateTransform>(page, "TransRecentWorld");
            page.PageOnEnter(); R(22, ReferenceEquals(card.RenderTransform, transform));
            void Step(int frames) { for (int i = 0; i < frames; i++) { last.SetValue(null, long.MaxValue); Native.Call(tick.DeclaringType!, tick.Name, null, 16d); } }
            var apply = Native.Method(page.GetType(), "ApplyRecentWorldState"); R(27, apply is not null);
            Step(50); Native.Call(page.GetType(), "ApplyRecentWorldState", page, true, false); Native.Call(page.GetType(), "ApplyRecentWorldState", page, false, true);
            R(30, card.Visibility == Visibility.Visible && transform.X < 0 && card.Opacity <= 0.1);
            Step(5); R(32, transform.X > -24 && card.Opacity > 0);
            Step(35); R(34, Math.Abs(transform.X) <= 0.05 && card.Opacity >= 0.99);
            Native.Call(page.GetType(), "ApplyRecentWorldState", page, true, true);
            R(36, !ToolTipService.GetIsEnabled(Part<FrameworkElement>(page, "ItemRecentWorld")));
            Step(30); R(38, card.Visibility == Visibility.Collapsed);
            Native.Call(page.GetType(), "ApplyRecentWorldState", page, false, true); Step(35); R(40, card.Visibility == Visibility.Visible && card.Opacity >= 0.99);
            Native.Call(page.GetType(), "ReleaseRecentWorlds", page); Native.Call(page.GetType(), "ShowRecentWorld", page, record); Native.Call(page.GetType(), "ApplyRecentWorldState", page, false, true); Step(35);
            R(44, card.Visibility == Visibility.Visible); R(45, card.Width <= 260 && card.Height <= 80);
            Canvas? icon = Part<MyListItem>(page, "ItemRecentWorld").Children.OfType<Canvas>().FirstOrDefault(item => item.Background is ImageBrush);
            R(47, icon is not null && icon.Clip is RectangleGeometry { RadiusX: > 0 });
            int launches = 0; bool hidden = false;
            Func<bool> start = () => { launches++; hidden = card.Visibility == Visibility.Collapsed; return false; };
            Native.Call(page.GetType(), "BeginRecentWorldLaunch", page, start, true); Native.Call(page.GetType(), "BeginRecentWorldLaunch", page, start, true); Native.Call(page.GetType(), "ApplyRecentWorldState", page, false, true);
            Step(5); R(55, launches == 0 && card.Opacity < 1 && !ToolTipService.GetIsEnabled(Part<FrameworkElement>(page, "ItemRecentWorld")));
            Step(55); R(57, launches == 1 && hidden);
            R(58, card.Visibility == Visibility.Visible && card.Opacity >= 0.99 && ToolTipService.GetIsEnabled(Part<FrameworkElement>(page, "ItemRecentWorld")));
            Native.Call(page.GetType(), "BeginRecentWorldLaunch", page, start, true); Native.Call(page.GetType(), "ReleaseRecentWorlds", page); Step(40); R(62, launches == 1);
        }
        finally {
            try { if (page is not null) Native.Call(page.GetType(), "ReleaseRecentWorlds", page); }
            finally {
                try { page?.PageOnForceExit(); }
                finally { last.SetValue(null, saved); }
            }
        }
        Console.WriteLine("PASS local-features recent card: actual Main lifecycle/animation ticks, stable transform, running/exit/cancel/reload, compact/rounded visuals, once-only deferred rejected launch and unload cancellation.");
    }
}
