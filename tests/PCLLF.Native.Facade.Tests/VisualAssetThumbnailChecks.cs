using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PCLCS;

namespace PCLLF.Native.Facade.Tests;

// Direct adaptation of the original 172-line ResourceThumbnailControllerChecks,
// retaining every original assertion and six real async requests. The controlled
// platform decoder is a gate/fault primitive, never a second thumbnail algorithm.
internal static class VisualAssetThumbnailChecks
{
    private sealed class Tile
    {
        internal readonly Grid Root = new() { Width = 160, Height = 80 };
        internal readonly Image Thumbnail = new();
        internal readonly Border Selection = new() { RenderTransform = new ScaleTransform(.8, .8) };
        internal readonly Border Check = new();
        internal bool Checked, Animate, Pressed;
        internal InstanceResourceEntryData? Entry;
        internal ResourceThumbnailController Controller = null!;
        internal int Changes, ClickCount, Dispatches;
    }
    private sealed class Work
    {
        internal readonly ManualResetEventSlim Initial = new(false), Replacement = new(false);
        internal readonly IOException Failure = new("synthetic thumbnail fault");
        internal readonly ImageSource Normal = Bitmap(20), NewImage = Bitmap(200);
        internal int Active, Maximum, Calls, Completed, ReplacementStarted, Warnings, BadWidth;
        internal int Expected, FailedLoads, DispatchQueued, DispatchApplied, PendingDispatch;
        internal Exception? Observed, DispatchError;
        internal ImageSource Load(string name, int width)
        {
            Interlocked.Increment(ref Calls); if (width != 640) Interlocked.Increment(ref BadWidth);
            int active = Interlocked.Increment(ref Active), old;
            do { old = Volatile.Read(ref Maximum); if (active <= old) break; }
            while (Interlocked.CompareExchange(ref Maximum, active, old) != old);
            try
            {
                if (name == "fault") throw Failure;
                if (name == "replacement") { Interlocked.Increment(ref ReplacementStarted); if (!Replacement.Wait(8000)) throw new TimeoutException("Replacement decoder gate timed out."); return NewImage; }
                if (!Initial.Wait(8000)) throw new TimeoutException("Initial decoder gate timed out."); return Normal;
            }
            catch { Interlocked.Increment(ref FailedLoads); throw; }
            finally { Interlocked.Decrement(ref Active); Interlocked.Increment(ref Completed); }
        }
        internal bool Quiescent => Volatile.Read(ref Completed) == Expected && Volatile.Read(ref Active) == 0 && Volatile.Read(ref DispatchQueued) == Expected && Volatile.Read(ref DispatchApplied) == Expected && Volatile.Read(ref PendingDispatch) == 0 && Volatile.Read(ref Warnings) == Volatile.Read(ref FailedLoads);
    }
    private static ImageSource Bitmap(byte value)
    {
        BitmapSource result = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null, new byte[] { value, 0, 0, 255 }, 4); result.Freeze(); return result;
    }
    private static Tile Create(Work work)
    {
        Tile tile = new(); tile.Root.Children.Add(tile.Thumbnail);
        ResourceThumbnailController.View view = new(tile.Root, tile.Thumbnail, new TextBlock(), new TextBlock(), tile.Selection, tile.Check, () => tile.Checked, value => tile.Checked = value, () => tile.Entry, value => tile.Entry = value);
        ResourceThumbnailController.Host host = new(work.Load, action =>
        {
            Interlocked.Increment(ref work.DispatchQueued); Interlocked.Increment(ref work.PendingDispatch);
            tile.Root.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                try { action(); }
                catch (Exception error) { work.DispatchError = error; }
                finally { tile.Dispatches++; Interlocked.Increment(ref work.DispatchApplied); Interlocked.Decrement(ref work.PendingDispatch); }
            }));
        }, (error, _) => { Interlocked.Exchange(ref work.Observed, error); Interlocked.Increment(ref work.Warnings); }, (_, _, _, pressed) => tile.Pressed = pressed, () => tile.Animate, () => { });
        tile.Controller = new(view, host); tile.Controller.Changed += () => tile.Changes++; tile.Controller.ClickRequested += count => tile.ClickCount = count; return tile;
    }
    private static void Wait(int line, Func<bool> done, string why, int timeout = 4000)
    {
        VisualAssetChecks.Pump(done, why, timeout); VisualAssetChecks.Check("thumbnail", line, done(), why);
    }
    internal static void Run(VisualAssetChecks.Scope scope)
    {
        ThreadPool.GetMinThreads(out int workers, out int completion);
        Work work = new(); Canvas viewport = new();
        Window window = new() { Content = viewport, Width = 500, Height = 600, Left = -20000, Top = -20000, ShowActivated = false, ShowInTaskbar = false, WindowStyle = WindowStyle.None };
        Window? main = Application.Current.MainWindow; ShutdownMode shutdown = Application.Current.ShutdownMode;
        List<Tile> tiles = new();
        try
        {
            Native.Check(ThreadPool.SetMinThreads(Math.Max(workers, 8), completion), "Thumbnail pool fixture failed to set its original controlled minimum.");
            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            for (int index = 0; index < 4; index++)
            {
                Tile tile = Create(work); tiles.Add(tile); viewport.Children.Add(tile.Root); Canvas.SetTop(tile.Root, index * 90);
                tile.Controller.Bind(new InstanceResourceEntryData { FullPath = "old-" + index, Name = "tile-" + index, Description = "synthetic controller view" });
            }
            window.Show(); window.UpdateLayout();
            Wait(118, () => tiles.All(tile => tile.Root.IsLoaded) && viewport.ActualHeight > 0, "offscreen controller views Loaded");
            foreach (Tile tile in tiles) { work.Expected++; tile.Controller.UpdateViewport(viewport); }
            Wait(121, () => Volatile.Read(ref work.Active) == 2, "two blocked thumbnail decodes"); VisualAssetChecks.Observe(100);
            VisualAssetChecks.Check("thumbnail", 123, Volatile.Read(ref work.Calls) == 2 && Volatile.Read(ref work.Maximum) == 2);
            Tile first = tiles[0]; first.Controller.ReleaseThumbnail(); first.Controller.Bind(new InstanceResourceEntryData { FullPath = "replacement", Name = "new", ModifiedUtc = DateTime.UtcNow });
            work.Expected++; first.Controller.UpdateViewport(viewport); work.Initial.Set();
            Wait(130, () => Volatile.Read(ref work.Completed) >= 4 && Volatile.Read(ref work.ReplacementStarted) == 1 && first.Dispatches >= 1 && ReferenceEquals(tiles[1].Thumbnail.Source, work.Normal) && ReferenceEquals(tiles[2].Thumbnail.Source, work.Normal) && ReferenceEquals(tiles[3].Thumbnail.Source, work.Normal), "queued actual decoder requests complete", 6000);
            VisualAssetChecks.Check("thumbnail", 133, first.Thumbnail.Source is null);
            work.Replacement.Set(); Wait(135, () => ReferenceEquals(first.Thumbnail.Source, work.NewImage), "new revision paints");
            VisualAssetChecks.Check("thumbnail", 137, work.Maximum == 2 && work.BadWidth == 0 && work.Warnings == 0);
            first.Controller.Checked = true; first.Controller.CheckChanged(); VisualAssetChecks.Check("thumbnail", 140, first.Controller.Checked && first.Changes == 1 && first.Selection.Opacity == 1);
            first.Animate = true;
            MouseButtonEventArgs down = new(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left) { RoutedEvent = UIElement.MouseLeftButtonDownEvent }; first.Root.RaiseEvent(down);
            VisualAssetChecks.Check("thumbnail", 145, down.Handled && first.ClickCount == down.ClickCount && first.Pressed);
            MouseButtonEventArgs up = new(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left) { RoutedEvent = UIElement.MouseLeftButtonUpEvent }; first.Root.RaiseEvent(up);
            VisualAssetChecks.Check("thumbnail", 149, !first.Pressed);
            Canvas.SetTop(tiles[1].Root, 1000); window.UpdateLayout(); tiles[1].Controller.UpdateViewport(viewport);
            VisualAssetChecks.Check("thumbnail", 151, tiles[1].Thumbnail.Source is null);
            viewport.Children.Remove(tiles[2].Root); Wait(153, () => !tiles[2].Root.IsLoaded && tiles[2].Thumbnail.Source is null, "unloaded thumbnail release");
            Tile fault = tiles[3]; fault.Controller.Bind(new InstanceResourceEntryData { FullPath = "fault", Name = "fault" }); int faultDispatch = fault.Dispatches + 1;
            work.Expected++; fault.Controller.UpdateViewport(viewport);
            Wait(159, () => Volatile.Read(ref work.Warnings) == 1 && Volatile.Read(ref work.Active) == 0 && fault.Dispatches >= faultDispatch, "actual fault completion and warning");
            VisualAssetChecks.Check("thumbnail", 161, ReferenceEquals(work.Observed, work.Failure) && fault.Thumbnail.Source is null);
            int calls = work.Calls; fault.Controller.UpdateViewport(viewport); fault.Controller.UpdateViewport(viewport); VisualAssetChecks.Observe(100);
            VisualAssetChecks.Check("thumbnail", 165, work.Calls == calls);
            Native.Check(work.DispatchError is null, "Actual thumbnail UI completion failed: " + work.DispatchError);
            VisualAssetChecks.Check("gallery", 87, true, "Original controller Run completed with every original observable.");
        }
        finally
        {
            // Real Task.Run requests include those waiting for the static two-slot
            // semaphore. Release gates, unload, and collect every scheduled decode
            // plus every terminal Dispatcher callback and warning before restoring.
            work.Initial.Set(); work.Replacement.Set();
            try
            {
                window.Content = null; window.Close();
                VisualAssetChecks.Pump(() => work.Quiescent, "all scheduled thumbnail work and terminal UI callbacks", 8000); VisualAssetChecks.Drain();
                VisualAssetChecks.Check("thumbnail", 168, work.Quiescent);
                Native.Check(work.DispatchError is null, "Thumbnail completion callback failure: " + work.DispatchError);
            }
            catch (Exception error) { VisualAssetChecks.FailClosed(scope.Environment, "Controlled thumbnail async work did not quiesce", error); }
            work.Initial.Dispose(); work.Replacement.Dispose();
            Application.Current.MainWindow = main; Application.Current.ShutdownMode = shutdown;
            Native.Check(ThreadPool.SetMinThreads(workers, completion), "Thumbnail fixture could not restore exact original pool minimum.");
        }
    }
}
