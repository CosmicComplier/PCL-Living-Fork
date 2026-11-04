using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using PCLLF.Presentation.Animation;
using PCLLF.Presentation.Controls;

namespace PCLLF.Modern.Wpf.Tests;

// LF native frame acceptance. Platform log/custom-event callbacks record observations;
// the host, factories, engine, WPF targets and control animation plans are production code.
internal static class AnimationTests {
    private const string DriverPass = "PASS native animation ThreadDriver: real foreground Animation worker, AboveNormal, synchronous STA frames and terminal callback.";
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Near(double expected, double actual, string message) => Require(Math.Abs(expected - actual) < .000001, message + ": " + actual);
    private sealed class Session {
        internal readonly List<Exception> Errors = new(), Fatals = new(), Warnings = new();
        internal readonly List<string> ErrorMessages = new();
        internal readonly AnimationNativeRuntime Runtime;
        internal Session() {
            Runtime = new AnimationNativeRuntime(AnimationNativeHost.Create(() => 9, () => false, () => 0,
                (_, _) => { }, _ => { }, (error, message) => { Errors.Add(error); ErrorMessages.Add(message); },
                (error, _) => Fatals.Add(error)));
        }
        internal void Frame(double milliseconds) {
            // Use the actual frame routine without starting LF's unbounded foreground worker.
            Runtime.LastTick = long.MaxValue;
            Runtime.Engine.AniTimer(milliseconds);
        }
        internal void Frames(int count) { for (int index = 0; index < count; index++) Frame(16); }
        internal void Healthy() => Require(Errors.Count == 0 && Fatals.Count == 0 && Warnings.Count == 0 && !Runtime.Running,
            "Native animation reported a fault or started a background worker.");
    }

    internal static void Run() {
        Assembly assembly = typeof(AnimationNativeRuntime).Assembly;
        Require(assembly.GetName().Name == "PCLLF.Presentation" && assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName == ".NETCoreApp,Version=v10.0" &&
            typeof(MyButtonView).Assembly == assembly && typeof(MyRadioButtonView).Assembly == assembly && typeof(MyCheckBoxView).Assembly == assembly,
            "Native animation or controls did not come from the actual modern LF presentation assembly.");
        NumericAndCancel(); DelaysAndReplacement(); ColorsAndResources(); CallbackFailure(); NativeControls();
    }

    internal static void RunThreadDriverProcess(string repositoryRoot) =>
        RunThreadDriverProcessAsync(repositoryRoot).GetAwaiter().GetResult();

    private static async Task RunThreadDriverProcessAsync(string repositoryRoot) {
        string executable = Environment.ProcessPath ?? throw new InvalidOperationException("Cannot locate the actual WPF test host.");
        ProcessStartInfo start = new(executable) {
            UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true,
            CreateNoWindow = true, WorkingDirectory = repositoryRoot
        };
        if (string.Equals(Path.GetFileNameWithoutExtension(executable), "dotnet", StringComparison.OrdinalIgnoreCase))
            start.ArgumentList.Add(typeof(AnimationTests).Assembly.Location);
        start.ArgumentList.Add(repositoryRoot);
        start.ArgumentList.Add("--animation-driver-child");
        using Process child = new() { StartInfo = start };
        bool started = false;
        try {
            started = child.Start();
            Require(started, "The real animation driver child did not start.");
            Task<string> output = child.StandardOutput.ReadToEndAsync(), errors = child.StandardError.ReadToEndAsync();
            try {
                await Task.WhenAll(child.WaitForExitAsync(), output, errors).WaitAsync(TimeSpan.FromSeconds(15)).ConfigureAwait(false);
            } catch (TimeoutException) {
                if (!child.HasExited) child.Kill(entireProcessTree: true);
                await child.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
                throw new TimeoutException("The real foreground animation driver child exceeded 15 seconds.\n" +
                    (output.IsCompletedSuccessfully ? output.Result : "stdout incomplete") + "\n" +
                    (errors.IsCompletedSuccessfully ? errors.Result : "stderr incomplete"));
            }
            string stdout = await output.ConfigureAwait(false), stderr = await errors.ConfigureAwait(false);
            Require(child.ExitCode == 0 && stdout.Contains(DriverPass, StringComparison.Ordinal) && string.IsNullOrWhiteSpace(stderr),
                "Actual animation driver child failed (exit " + child.ExitCode + ").\n" + stdout + "\n" + stderr);
            Console.Write(stdout);
        } finally {
            if (started && !child.HasExited) {
                child.Kill(entireProcessTree: true);
                Require(child.WaitForExit(2000), "The animation child did not exit after its process tree was killed.");
            }
        }
    }

    internal static void RunThreadDriverChild() {
        // This watchdog also ends a child whose UI Dispatcher is blocked inside
        // the original synchronous UiWait; it never changes the native driver.
        using System.Threading.Timer watchdog = new(_ => {
            Console.Error.WriteLine("Actual animation driver child exceeded its 10 second watchdog.");
            Console.Error.Flush(); Environment.Exit(1);
        }, null, TimeSpan.FromSeconds(10), Timeout.InfiniteTimeSpan);
        Dispatcher dispatcher = Application.Current.Dispatcher;
        int uiThread = Thread.CurrentThread.ManagedThreadId;
        ConcurrentQueue<string> faults = new();
        AnimationHost host = AnimationNativeHost.Create(() => 9, () => false, () => 0, (_, _) => { }, _ => { },
            (error, message) => faults.Enqueue(message + ": " + error),
            (error, message) => faults.Enqueue(message + ": " + error));
        Thread observedWorker = null;
        Action<Action, string, ThreadPriority> nativeStart = host.StartThread;
        host.StartThread = (work, name, priority) => nativeStart(() => {
            Volatile.Write(ref observedWorker, Thread.CurrentThread);
            work();
        }, name, priority);
        AnimationNativeRuntime runtime = new(host);
        Border target = new() { Opacity = 0 };
        bool middle = false; int frames = 0, callbacks = 0;
        DependencyPropertyDescriptor opacity = DependencyPropertyDescriptor.FromProperty(UIElement.OpacityProperty, typeof(Border));
        EventHandler changed = (_, _) => {
            Require(dispatcher.CheckAccess() && Thread.CurrentThread.ManagedThreadId == uiThread && Thread.CurrentThread.GetApartmentState() == ApartmentState.STA,
                "The actual driver changed WPF opacity outside its STA/UI dispatcher.");
            frames++;
            if (target.Opacity > 0 && target.Opacity < 1) middle = true;
        };
        opacity.AddValueChanged(target, changed);
        try {
            runtime.Engine.AniStart(new[] { runtime.Factories.AaOpacity(target, 1, 1200), runtime.Factories.AaCode(() => {
                Require(dispatcher.CheckAccess() && Thread.CurrentThread.ManagedThreadId == uiThread && Thread.CurrentThread.GetApartmentState() == ApartmentState.STA,
                    "The real After callback left its STA/UI dispatcher.");
                callbacks++;
            }, after: true) }, "actual-driver");
            runtime.Engine.AniStart();
            Require(runtime.Running, "The real no-argument AniStart did not publish Running.");
            PumpUntil(() => callbacks == 1 && !runtime.Engine.AniIsRun("actual-driver"));
            Thread worker = Volatile.Read(ref observedWorker);
            Require(worker != null && worker.IsAlive && worker.Name == "Animation" && worker.Priority == ThreadPriority.AboveNormal && !worker.IsBackground &&
                worker.ManagedThreadId != uiThread && runtime.Running, "The real native worker lost its original name, priority, foreground or running semantics.");
            Near(1, target.Opacity, "The actual ThreadDriver did not reach its terminal WPF value");
            Require(middle && frames >= 2 && callbacks == 1 && faults.IsEmpty, "The real driver skipped middle frames, repeated its callback or logged a fault: " + string.Join("\n", faults));
            Console.WriteLine(DriverPass);
        } finally { opacity.RemoveValueChanged(target, changed); }
    }

    private static void NumericAndCancel() {
        Session session = new(); AnimationNativeRuntime runtime = session.Runtime;
        Border target = new() { Opacity = 0 }; int callbacks = 0;
        runtime.Engine.AniStart(new[] { runtime.Factories.AaOpacity(target, 1, 40), runtime.Factories.AaCode(() => callbacks++, after: true) }, "cancel");
        session.Frame(10); Near(.25, target.Opacity, "Opacity did not reach its actual intermediate frame");
        Require(runtime.Groups["cancel"].Data[0].TimeFinished == 10 && runtime.Groups["cancel"].Data[0].TimePercent == .25,
            "The engine did not persist the real struct frame state.");
        runtime.Engine.AniStop("cancel"); session.Frame(100);
        Near(.25, target.Opacity, "Stopping a group changed its current WPF value");
        Require(callbacks == 0 && !runtime.Engine.AniIsRun("cancel"), "Stopping a group retained its pending callback.");
        runtime.Engine.AniStart(runtime.Factories.AaOpacity(target, .75, 30), "finish"); session.Frame(15);
        Near(.625, target.Opacity, "Replacement opacity did not advance through the real factory");
        session.Frame(15); Near(1, target.Opacity, "Opacity did not reach its terminal value");
        Require(!runtime.Engine.AniIsRun("finish"), "A completed numeric group remained active.");
        runtime.Engine.AniControlEnabled = 2; runtime.Engine.AniControlEnabled = 3;
        Require(runtime.Engine.AniControlEnabled == 3, "The original absolute animation-control setter became an increment.");
        runtime.Engine.AniControlEnabled = 0; session.Healthy();
        Console.WriteLine("PASS native animation 1/5: actual WPF middle/end frames, struct state, stop/callback cancellation and absolute control state.");
    }

    private static void DelaysAndReplacement() {
        Session session = new(); AnimationNativeRuntime runtime = session.Runtime; Border target = new() { Opacity = 0 }; int after = 0;
        runtime.Engine.AniStart(new[] { runtime.Factories.AaOpacity(target, 1, 40, 20), runtime.Factories.AaCode(() => after++, 10, true) }, "after");
        session.Frame(20); Near(0, target.Opacity, "The exact delay boundary ran a frame early");
        session.Frame(20); Near(.5, target.Opacity, "The delayed frame did not reach its midpoint");
        session.Frame(20); Near(1, target.Opacity, "The delayed frame did not finish"); Require(after == 0, "After ran in its predecessor's completion tick.");
        session.Frame(10); Require(after == 0, "The After callback ignored its own exact delay boundary.");
        session.Frame(1); Require(after == 1 && !runtime.Engine.AniIsRun("after"), "The delayed After callback did not run once and remove its group.");

        target.Opacity = 0; int rebuilds = 0;
        runtime.Engine.AniStart(runtime.Factories.AaCode(() => {
            rebuilds++; runtime.Engine.AniStart(runtime.Factories.AaOpacity(target, 1, 40), "rebuild");
        }), "rebuild");
        AnimationGroup original = runtime.Groups["rebuild"];
        session.Frame(20);
        Require(rebuilds == 1 && runtime.Groups.ContainsKey("rebuild") && !ReferenceEquals(original, runtime.Groups["rebuild"]),
            "An old completed callback group deleted its same-name replacement.");
        Near(.5, target.Opacity, "The replacement group did not execute in the original LF tick order");
        session.Frame(20); Near(1, target.Opacity, "The callback-created group did not finish");
        Require(!runtime.Engine.AniIsRun("rebuild") && rebuilds == 1, "A replacement callback ran twice or left its group behind.");
        session.Healthy(); Console.WriteLine("PASS native animation 2/5: delay and After boundaries plus live same-name callback rebuilding.");
    }

    private static void ColorsAndResources() {
        Session session = new(); AnimationNativeRuntime runtime = session.Runtime;
        ControlColor white = runtime.Host.ReadColor(null);
        Require(white.A == 255 && white.R == 255 && white.G == 255 && white.B == 255, "The native host lost LF's null-to-white color behavior.");
        Border target = new() { Background = new SolidColorBrush(Color.FromRgb(10, 20, 30)) };
        runtime.Engine.AniStart(runtime.Factories.AaColor(target, Border.BackgroundProperty, new ControlColor(0, -1, 1, 0), 4), "signed");
        session.Frame(1); ControlColor remainder = (ControlColor)runtime.Groups["signed"].Data[0].ValueLast;
        Near(-.25, remainder.R, "Negative sub-byte color remainder was clamped"); Near(.25, remainder.G, "Positive sub-byte color remainder was lost");
        session.Frame(1); remainder = (ControlColor)runtime.Groups["signed"].Data[0].ValueLast;
        Near(-.5, remainder.R, "Signed color remainder did not accumulate before banker rounding");
        session.Frame(1); Require(((SolidColorBrush)target.Background).Color == Color.FromRgb(9, 21, 30), "Native color rounding did not carry its fractional channels.");
        session.Frame(1); Require(!runtime.Engine.AniIsRun("signed") && ((SolidColorBrush)target.Background).Color == Color.FromRgb(9, 21, 30), "The signed color frame did not terminate correctly.");

        const string key = "PclMigration.Animation.Target"; ResourceDictionary resources = Application.Current.Resources;
        bool existed = resources.Contains(key); object previous = existed ? resources[key] : null;
        Window previousMain = Application.Current.MainWindow, probe = null;
        try {
            probe = new Window { Content = target, Width = 100, Height = 100, Left = -20000, Top = -20000, ShowActivated = false, ShowInTaskbar = false, WindowStyle = WindowStyle.None };
            probe.Show(); probe.UpdateLayout(); PumpUntil(() => target.IsLoaded);
            SolidColorBrush endpoint = new(Color.FromRgb(120, 80, 40)); resources[key] = endpoint;
            target.Background = new SolidColorBrush(Color.FromRgb(200, 180, 160));
            runtime.Engine.AniStart(runtime.Factories.AaColor(target, Border.BackgroundProperty, key, 40), "resource");
            session.Frame(20); Require(((SolidColorBrush)target.Background).Color == Color.FromRgb(160, 130, 100), "The actual resource-color factory lost its signed midpoint.");
            session.Frame(20); Require(ReferenceEquals(target.Background, endpoint) && !runtime.Engine.AniIsRun("resource"), "Terminal color did not restore its original resource reference.");
            SolidColorBrush replacement = new(Color.FromRgb(20, 30, 40)); resources[key] = replacement;
            Require(ReferenceEquals(target.Background, replacement), "The terminal dynamic resource stopped following later palette changes.");
        } finally {
            try { if (probe != null) probe.Close(); }
            finally { Application.Current.MainWindow = previousMain; if (existed) resources[key] = previous; else resources.Remove(key); }
        }
        session.Healthy(); Console.WriteLine("PASS native animation 3/5: original null color, signed fractional remainder, resource middle/end values and live terminal resource binding.");
    }

    private static void CallbackFailure() {
        Session session = new(); AnimationNativeRuntime runtime = session.Runtime; Exception failure = new InvalidOperationException("synthetic native callback failure"); int recovered = 0;
        runtime.Engine.AniStart(new[] { runtime.Factories.AaCode(() => throw failure), runtime.Factories.AaCode(() => recovered++, after: true) }, "fault");
        session.Frame(1);
        Require(session.Errors.Count == 1 && ReferenceEquals(session.Errors[0], failure) && session.ErrorMessages[0].StartsWith("执行动画失败", StringComparison.Ordinal) &&
            runtime.Groups["fault"].Data.Count == 1 && runtime.Groups["fault"].Data[0].IsAfter && recovered == 0,
            "Callback failure lost the original exception or changed the remaining After state.");
        session.Frame(1);
        Require(recovered == 1 && !runtime.Engine.AniIsRun("fault") && session.Errors.Count == 1 && session.Fatals.Count == 0 && !runtime.Running,
            "The engine did not continue its original frame state after a logged callback error.");
        Console.WriteLine("PASS native animation 4/5: actual callback fault identity/logging, remaining frame state and subsequent recovery.");
    }

    private static MouseButtonEventArgs RaiseMouse(UIElement control, RoutedEvent routedEvent) {
        Require(PresentationSource.FromVisual(control) != null, "Native control mouse fixture has no live presentation source.");
        MouseButtonEventArgs args = new(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left) { RoutedEvent = routedEvent };
        control.RaiseEvent(args); return args;
    }
    private static void Click(UIElement control) { RaiseMouse(control, UIElement.MouseLeftButtonDownEvent); RaiseMouse(control, UIElement.MouseLeftButtonUpEvent); }
    private static void Leave(UIElement control) => control.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount) { RoutedEvent = UIElement.MouseLeaveEvent });
    private static void PumpUntil(Func<bool> ready) {
        if (ready()) return;
        DispatcherFrame frame = new(); Stopwatch elapsed = Stopwatch.StartNew(); DispatcherTimer timer = new(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(10) };
        timer.Tick += (_, _) => { if (ready() || elapsed.ElapsedMilliseconds >= 4000) frame.Continue = false; };
        timer.Start(); try { Dispatcher.PushFrame(frame); } finally { timer.Stop(); }
        Require(ready(), "Native modern WPF views did not finish loading.");
    }
    private static void NativeControls() {
        Session session = new(); AnimationNativeRuntime runtime = session.Runtime; Application app = Application.Current;
        Window previousMain = app.MainWindow; ShutdownMode shutdown = app.ShutdownMode; Window window = null;
        ControlServiceHost prior = null; try { prior = ControlServices.Current; } catch (InvalidOperationException) { }
        Dictionary<string, object> saved = new();
        // A fixed narrow palette supplies the actual LF resource names used by these three views.
        // Complete native LF theme/main-window acceptance remains a separate gate.
        Dictionary<string, SolidColorBrush> palette = new() {
            ["ColorBrushSemiTransparent"] = new(Color.FromArgb(1, 255, 255, 255)), ["ColorBrushHalfWhite"] = new(Color.FromArgb(128, 255, 255, 255)),
            ["ColorBrush1"] = new(Color.FromRgb(200, 180, 160)), ["ColorBrush2"] = new(Color.FromRgb(100, 120, 140)), ["ColorBrush3"] = new(Color.FromRgb(40, 60, 80)),
            ["ColorBrush6"] = new(Color.FromRgb(150, 160, 170)), ["ColorBrush7"] = new(Color.FromRgb(210, 220, 230)), ["ColorBrushBg1"] = new(Color.FromRgb(245, 245, 245)),
            ["ColorBrushGray4"] = new(Color.FromRgb(90, 90, 90)), ["ColorBrushGray5"] = new(Color.FromRgb(130, 130, 130)),
            ["ColorBrushRedLight"] = new(Color.FromRgb(240, 50, 40)), ["ColorBrushRedDark"] = new(Color.FromRgb(200, 30, 20)), ["ColorBrushRedBack"] = new(Color.FromArgb(50, 200, 30, 20))
        };
        int customEvents = 0;
        try {
            foreach (KeyValuePair<string, SolidColorBrush> pair in palette) { if (app.Resources.Contains(pair.Key)) saved[pair.Key] = app.Resources[pair.Key]; app.Resources[pair.Key] = pair.Value; }
            ControlServices.Configure(runtime.CreateControlServices(kind => runtime.Host.ReadColor(palette[kind switch {
                ControlThemeColor.Gray4 => "ColorBrushGray4", ControlThemeColor.Gray5 => "ColorBrushGray5", ControlThemeColor.Accent8 => "ColorBrush3", _ => "ColorBrushSemiTransparent"
            }]), _ => { }, (error, _) => session.Warnings.Add(error), (error, _) => session.Errors.Add(error), _ => customEvents++));
            Require(ControlServices.Current.Animations is ControlAnimationHost<AnimationData>, "Modern controls did not use their real native frame adapter.");
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            MyButtonView button = new() { Text = "Modern native button", Height = 36 };
            MyRadioButtonView first = new() { Text = "First", Logo = "M0,0 L8,8" }, second = new() { Text = "Second", Logo = "M0,0 L8,8", ColorType = MyRadioButtonView.ColorState.Highlight };
            MyCheckBoxView check = new() { Text = "Modern native checkbox", Height = 30 };
            StackPanel panel = new(), radios = new() { Orientation = Orientation.Horizontal };
            radios.Children.Add(first); radios.Children.Add(second); panel.Children.Add(button); panel.Children.Add(radios); panel.Children.Add(check);
            window = new Window { Content = panel, Width = 500, Height = 300, Left = -20000, Top = -20000, ShowActivated = false, ShowInTaskbar = false, WindowStyle = WindowStyle.None };
            window.Show(); window.UpdateLayout(); PumpUntil(() => button.IsLoaded && first.IsLoaded && second.IsLoaded && check.IsLoaded); session.Frames(30);
            int clicks = 0; object clickSender = null; MouseButtonEventArgs clickedArgs = null;
            button.Click += (sender, args) => { clicks++; clickSender = sender; clickedArgs = args; };
            RaiseMouse(button, UIElement.MouseLeftButtonDownEvent); session.Frame(32);
            Require(((ScaleTransform)button.RealRenderTransform).ScaleX > .9 && ((ScaleTransform)button.RealRenderTransform).ScaleX < 1, "Native button press frames did not alter its real scale.");
            MouseButtonEventArgs up = RaiseMouse(button, UIElement.MouseLeftButtonUpEvent); session.Frames(25);
            Require(clicks == 1 && ReferenceEquals(clickSender, button) && ReferenceEquals(clickedArgs, up) && customEvents == 1, "Modern button input did not preserve its original sender/event/custom callbacks.");
            Near(1, ((ScaleTransform)button.RealRenderTransform).ScaleX, "Native button release did not reach its final scale");
            RaiseMouse(button, UIElement.MouseLeftButtonDownEvent); Leave(button); RaiseMouse(button, UIElement.MouseLeftButtonUpEvent); session.Frames(60);
            Require(clicks == 1, "Leaving the native button did not cancel its pending click."); Near(1, ((ScaleTransform)button.RealRenderTransform).ScaleX, "Native button leave animation did not restore scale");

            first.SetChecked(true, false, true); session.Frames(15); bool cancel = true; int previews = 0, checks = 0;
            second.PreviewClick += (_, args) => { previews++; args.Handled = cancel; };
            second.Check += (_, user) => { if (user) checks++; };
            Click(second); Require(first.Checked && !second.Checked && previews == 1 && checks == 0, "Native radio preview cancellation changed selection.");
            Leave(second); session.Frames(15); cancel = false; Click(second); session.Frame(60);
            Color middle = ((SolidColorBrush)second.Background).Color;
            Require(!first.Checked && second.Checked && previews == 2 && checks == 1 && middle.A > 1 && middle.A < 255, "Native radio group/user handling or selected-color midpoint changed.");
            session.Frames(15); Require(ReferenceEquals(second.Background, palette["ColorBrush3"]), "Native radio selection did not reach its resource-bound final color.");

            int changes = 0; bool cancelCheck = false;
            check.PreviewChange += (_, args) => { args.Handled = cancelCheck; };
            check.Change += (_, _) => changes++;
            check.SetChecked(true, true); Click(check);
            Require(check.Checked && changes == 1 && runtime.Engine.AniIsRun("MyCheckBox AllowMouseDown " + check.Uuid), "Native checkbox lost its delayed mouse gate.");
            session.Frames(12); double partial = ((ScaleTransform)check.ShapeCheck.RenderTransform).ScaleX;
            Require(partial > 0 && partial < 1.5, "Native checkbox did not execute the real delayed check-mark frames.");
            session.Frames(35); Near(1, ((ScaleTransform)check.ShapeCheck.RenderTransform).ScaleX, "Native checkbox did not finish checking"); Near(18, check.ShapeBorder.Width, "Native checkbox border did not return to its original size");
            Click(check); session.Frames(35); Require(!check.Checked && changes == 2, "The native delayed callback did not restore checkbox mouse input.");
            Near(0, ((ScaleTransform)check.ShapeCheck.RenderTransform).ScaleX, "Native checkbox did not finish unchecking");
            cancelCheck = true; check.SetChecked(true, true); session.Frames(40); Require(!check.Checked && changes == 2, "Native checkbox preview cancellation emitted Change or checked the box.");
            session.Healthy(); Console.WriteLine("PASS native animation 5/5: three actual C# WPF views, native press/color/check-mark middle/end frames, cancellation and delayed mouse restoration.");
        } finally {
            try { if (window != null) window.Close(); }
            finally {
                foreach (string name in runtime.Groups.Keys.ToArray()) runtime.Engine.AniStop(name);
                foreach (string key in palette.Keys) { if (saved.TryGetValue(key, out object value)) app.Resources[key] = value; else app.Resources.Remove(key); }
                if (prior != null) ControlServices.Configure(prior);
                app.MainWindow = previousMain; app.ShutdownMode = shutdown;
            }
        }
    }
}
