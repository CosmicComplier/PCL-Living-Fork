using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PCLCS;

public static class PclScreenshotControllerChecks {
    private sealed class Tile {
        internal readonly Grid Root = new Grid { Width = 160, Height = 80 };
        internal readonly Image Thumbnail = new Image();
        internal readonly Border Selection = new Border { RenderTransform = new ScaleTransform(.8, .8) };
        internal readonly Border Check = new Border();
        internal bool Checked, Animate, Pressed;
        internal InstanceResourceEntryData Entry;
        internal ResourceThumbnailController Controller;
        internal int Changes, ClickCount, Dispatches;
    }
    private sealed class Work {
        internal readonly ManualResetEventSlim Initial = new ManualResetEventSlim(false);
        internal readonly ManualResetEventSlim Replacement = new ManualResetEventSlim(false);
        internal readonly IOException Failure = new IOException("synthetic thumbnail fault");
        internal readonly ImageSource Normal = Bitmap(20), NewImage = Bitmap(200);
        internal int Active, Maximum, Calls, Completed, ReplacementStarted, Warnings, BadWidth;
        internal Exception Observed;
        internal ImageSource Load(string name, int width) {
            Interlocked.Increment(ref Calls);
            if (width != 640) Interlocked.Increment(ref BadWidth);
            int active = Interlocked.Increment(ref Active), old;
            do { old = Volatile.Read(ref Maximum); if (active <= old) break; }
            while (Interlocked.CompareExchange(ref Maximum, active, old) != old);
            try {
                if (name == "fault") throw Failure;
                if (name == "replacement") {
                    Interlocked.Increment(ref ReplacementStarted);
                    if (!Replacement.Wait(8000)) throw new TimeoutException("Replacement loader gate timed out.");
                    return NewImage;
                }
                if (!Initial.Wait(8000)) throw new TimeoutException("Initial loader gate timed out.");
                return Normal;
            } finally { Interlocked.Decrement(ref Active); Interlocked.Increment(ref Completed); }
        }
    }
    private static ImageSource Bitmap(byte value) {
        BitmapSource result = BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null,
            new byte[] { value, 0, 0, 255 }, 4);
        result.Freeze();
        return result;
    }
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private static void PumpUntil(Func<bool> condition, int timeout, string message) {
        if (condition()) return;
        DispatcherFrame frame = new DispatcherFrame();
        Stopwatch elapsed = Stopwatch.StartNew();
        bool done = false;
        DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Background);
        timer.Interval = TimeSpan.FromMilliseconds(10);
        timer.Tick += delegate { done = condition(); if (done || elapsed.ElapsedMilliseconds >= timeout) frame.Continue = false; };
        timer.Start();
        try { Dispatcher.PushFrame(frame); } finally { timer.Stop(); }
        Require(done, message);
    }
    private static void PumpFor(int milliseconds) {
        Stopwatch elapsed = Stopwatch.StartNew();
        PumpUntil(delegate { return elapsed.ElapsedMilliseconds >= milliseconds; }, milliseconds + 2000,
            "The dispatcher did not process the observation interval.");
    }
    private static Tile Create(Work work) {
        Tile tile = new Tile();
        TextBlock name = new TextBlock(), info = new TextBlock();
        tile.Root.Children.Add(tile.Thumbnail);
        ResourceThumbnailController.View view = new ResourceThumbnailController.View(tile.Root,
            tile.Thumbnail, name, info, tile.Selection, tile.Check,
            delegate { return tile.Checked; }, delegate(bool value) { tile.Checked = value; },
            delegate { return tile.Entry; }, delegate(InstanceResourceEntryData value) { tile.Entry = value; });
        ResourceThumbnailController.Host host = new ResourceThumbnailController.Host(work.Load,
            delegate(Action action) { tile.Root.Dispatcher.BeginInvoke(new Action(delegate { action(); tile.Dispatches++; }), DispatcherPriority.Normal); },
            delegate(Exception error, string message) {
                Interlocked.Exchange(ref work.Observed, error); Interlocked.Increment(ref work.Warnings);
            },
            delegate(Border back, bool hovered, bool selected, bool pressed) { tile.Pressed = pressed; },
            delegate { return tile.Animate; }, delegate { });
        tile.Controller = new ResourceThumbnailController(view, host);
        tile.Controller.Changed += delegate { tile.Changes++; };
        tile.Controller.ClickRequested += delegate(int count) { tile.ClickCount = count; };
        return tile;
    }
    public static void Run() {
        int workers, completion;
        ThreadPool.GetMinThreads(out workers, out completion);
        // Allow four worker delegates to expose a missing semaphore rather than thread-pool starvation.
        ThreadPool.SetMinThreads(Math.Max(workers, 8), completion);
        Work work = new Work();
        Canvas viewport = new Canvas();
        Window window = new Window { Content = viewport, Width = 500, Height = 600,
            Left = -20000, Top = -20000, ShowActivated = false, ShowInTaskbar = false,
            WindowStyle = WindowStyle.None };
        Application app = Application.Current;
        Window previousMain = app.MainWindow;
        ShutdownMode shutdown = app.ShutdownMode;
        app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        List<Tile> tiles = new List<Tile>();
        int submitted = 0;
        try {
            for (int index = 0; index < 4; index++) {
                Tile tile = Create(work);
                tiles.Add(tile); viewport.Children.Add(tile.Root); Canvas.SetTop(tile.Root, index * 90);
                tile.Controller.Bind(new InstanceResourceEntryData { FullPath = "old-" + index,
                    Name = "tile-" + index, Description = "synthetic controller view" });
            }
            window.Show(); window.UpdateLayout();
            PumpUntil(delegate { return tiles.TrueForAll(delegate(Tile tile) { return tile.Root.IsLoaded; }) && viewport.ActualHeight > 0; },
                4000, "The offscreen controller views did not load.");
            foreach (Tile tile in tiles) { tile.Controller.UpdateViewport(viewport); submitted++; }
            PumpUntil(delegate { return Volatile.Read(ref work.Active) == 2; }, 4000, "Two thumbnail decodes did not start.");
            PumpFor(100);
            Require(Volatile.Read(ref work.Calls) == 2 && Volatile.Read(ref work.Maximum) == 2,
                "More than two thumbnail decoders ran while the first two were blocked.");
            Tile first = tiles[0];
            first.Controller.ReleaseThumbnail();
            first.Controller.Bind(new InstanceResourceEntryData { FullPath = "replacement", Name = "new", ModifiedUtc = DateTime.UtcNow });
            first.Controller.UpdateViewport(viewport); submitted++;
            work.Initial.Set();
            PumpUntil(delegate { return Volatile.Read(ref work.Completed) >= 4 && Volatile.Read(ref work.ReplacementStarted) == 1 && first.Dispatches >= 1 &&
                object.ReferenceEquals(tiles[1].Thumbnail.Source, work.Normal) && object.ReferenceEquals(tiles[2].Thumbnail.Source, work.Normal) &&
                object.ReferenceEquals(tiles[3].Thumbnail.Source, work.Normal); }, 6000, "Queued thumbnail requests did not complete.");
            Require(first.Thumbnail.Source == null, "A released/rebound revision repainted the old thumbnail.");
            work.Replacement.Set();
            PumpUntil(delegate { return object.ReferenceEquals(first.Thumbnail.Source, work.NewImage); }, 4000,
                "The new revision did not publish its thumbnail.");
            Require(work.Maximum == 2 && work.BadWidth == 0 && work.Warnings == 0,
                "Concurrency, decode width or successful dispatch changed.");
            first.Controller.Checked = true; first.Controller.CheckChanged();
            Require(first.Controller.Checked && first.Changes == 1 && first.Selection.Opacity == 1,
                "Checkbox selection did not update the view and change callback.");
            first.Animate = true;
            MouseButtonEventArgs down = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left);
            down.RoutedEvent = UIElement.MouseLeftButtonDownEvent; first.Root.RaiseEvent(down);
            Require(down.Handled && first.ClickCount == down.ClickCount && first.Pressed,
                "Mouse selection did not preserve click count, handled state or press feedback.");
            MouseButtonEventArgs up = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left);
            up.RoutedEvent = UIElement.MouseLeftButtonUpEvent; first.Root.RaiseEvent(up);
            Require(!first.Pressed, "Pointer release did not clear press feedback.");
            Canvas.SetTop(tiles[1].Root, 1000); window.UpdateLayout(); tiles[1].Controller.UpdateViewport(viewport);
            Require(tiles[1].Thumbnail.Source == null, "Leaving the viewport did not release the thumbnail.");
            viewport.Children.Remove(tiles[2].Root);
            PumpUntil(delegate { return !tiles[2].Root.IsLoaded && tiles[2].Thumbnail.Source == null; }, 4000,
                "Unloading did not release the thumbnail.");
            Tile fault = tiles[3];
            fault.Controller.Bind(new InstanceResourceEntryData { FullPath = "fault", Name = "fault" });
            int faultDispatch = fault.Dispatches + 1;
            fault.Controller.UpdateViewport(viewport); submitted++;
            PumpUntil(delegate { return Volatile.Read(ref work.Warnings) == 1 && Volatile.Read(ref work.Active) == 0 && fault.Dispatches >= faultDispatch; }, 4000,
                "A decoder fault did not reach the UI completion and warning path.");
            Require(object.ReferenceEquals(work.Observed, work.Failure) && fault.Thumbnail.Source == null,
                "Fault identity or thumbnail state changed.");
            int calls = work.Calls;
            fault.Controller.UpdateViewport(viewport); fault.Controller.UpdateViewport(viewport); PumpFor(100);
            Require(work.Calls == calls, "A completed fault retried on every viewport update.");
        } finally {
            work.Initial.Set(); work.Replacement.Set(); window.Close();
            try { PumpUntil(delegate { return Volatile.Read(ref work.Completed) >= submitted && Volatile.Read(ref work.Active) == 0; },
                6000, "Thumbnail workers did not drain after gate release."); PumpFor(30); }
            finally { app.MainWindow = previousMain; app.ShutdownMode = shutdown; ThreadPool.SetMinThreads(workers, completion); }
        }
    }
}