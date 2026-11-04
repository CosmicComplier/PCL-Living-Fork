using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PCLLF.Presentation.Animation;
using PCLLF.Presentation.Controls;

namespace PCLLF.Modern.Wpf.Tests;

// Real C027 controls/BAML, WPF layout/input/rendering and the production native
// animation engine. Only product settings/log/custom-event boundaries are fixtures.
internal static class CommonControlsAcceptance {
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void PumpUntil(Func<bool> ready, string message) {
        if (ready()) return;
        DispatcherFrame frame = new(); Stopwatch elapsed = Stopwatch.StartNew();
        DispatcherTimer timer = new(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(10) };
        timer.Tick += (_, _) => { if (ready() || elapsed.ElapsedMilliseconds >= 4000) frame.Continue = false; };
        timer.Start(); try { Dispatcher.PushFrame(frame); } finally { timer.Stop(); }
        Require(ready(), message);
    }
    private static MouseButtonEventArgs MouseEvent(UIElement control, RoutedEvent routedEvent) {
        Require(PresentationSource.FromVisual(control) != null, "C027 input requires an actual live presentation source.");
        MouseButtonEventArgs args = new(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left) { RoutedEvent = routedEvent };
        control.RaiseEvent(args); return args;
    }
    private static MouseButtonEventArgs Click(UIElement control) {
        MouseEvent(control, UIElement.MouseLeftButtonDownEvent); return MouseEvent(control, UIElement.MouseLeftButtonUpEvent);
    }
    private static void Leave(UIElement control) => control.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount) { RoutedEvent = UIElement.MouseLeaveEvent });
    private sealed class Session {
        internal readonly AnimationNativeRuntime Runtime;
        internal readonly List<Exception> Faults = new();
        internal readonly List<DependencyObject> CustomEvents = new();
        internal readonly int UiThread = Environment.CurrentManagedThreadId;
        internal Session() => Runtime = new AnimationNativeRuntime(AnimationNativeHost.Create(() => 9, () => false, () => 0,
            (_, _) => { }, _ => { }, (error, _) => Faults.Add(error), (error, _) => Faults.Add(error)));
        internal void Frame(double milliseconds) { Runtime.LastTick = long.MaxValue; Runtime.Engine.AniTimer(milliseconds); }
        internal void Frames(int count = 55) { for (int i = 0; i < count; i++) Frame(16); }
        internal void Healthy() => Require(Faults.Count == 0 && !Runtime.Running, "C027 control frames logged a fault or started an unbounded worker.");
        internal void Stop() { foreach (string name in Runtime.Groups.Keys.ToArray()) Runtime.Engine.AniStop(name); }
    }
    private sealed class SettingHint : MyHintView {
        private readonly Dictionary<string, bool> settings;
        internal int Reads, Writes;
        internal SettingHint(Dictionary<string, bool> settings, string key) { this.settings = settings; RelativeSetup = key; Text = "Isolated hint"; }
        protected override bool ReadDismissed(string key) {
            Require(Dispatcher.CheckAccess(), "Hint setting read left the UI thread."); Reads++;
            return settings.TryGetValue(key, out bool dismissed) && dismissed;
        }
        protected override void WriteDismissed(string key) {
            Require(Dispatcher.CheckAccess(), "Hint setting write left the UI thread."); Writes++; settings[key] = true;
        }
    }
    private static void Display(Window window, FrameworkElement content) {
        window.Content = content; window.UpdateLayout();
        PumpUntil(() => content.IsLoaded, "C027 fixture did not load its real visual content.");
        window.UpdateLayout();
    }
    internal static void Run() {
        Application app = Application.Current ?? throw new InvalidOperationException("C027 requires the existing standard WPF Application.");
        Require(app.Dispatcher.CheckAccess() && Thread.CurrentThread.GetApartmentState() == ApartmentState.STA,
            "C027 acceptance must run on the actual STA UI thread.");
        Require(typeof(MyCardView).Assembly.GetCustomAttributes(typeof(TargetFrameworkAttribute), false)
            .Cast<TargetFrameworkAttribute>().Single().FrameworkName == ".NETCoreApp,Version=v10.0", "C027 did not load the actual modern presentation assembly.");
        Session session = new(); ControlServiceHost prior = null;
        try { prior = ControlServices.Current; } catch (InvalidOperationException) { }
        Window priorMain = app.MainWindow, window = null; ShutdownMode priorShutdown = app.ShutdownMode;
        Dictionary<string, object> palette = new() {
            ["ColorBrushSemiTransparent"] = new SolidColorBrush(Color.FromArgb(1, 255, 255, 255)),
            ["ColorBrush1"] = new SolidColorBrush(Color.FromRgb(52, 61, 74)), ["ColorBrush2"] = new SolidColorBrush(Color.FromRgb(60, 110, 180)),
            ["ColorBrush3"] = new SolidColorBrush(Color.FromRgb(80, 130, 190)), ["ColorBrush4"] = new SolidColorBrush(Color.FromRgb(100, 145, 200)),
            ["ColorBrush5"] = new SolidColorBrush(Color.FromRgb(120, 160, 210)), ["ColorBrush6"] = new SolidColorBrush(Color.FromRgb(180, 210, 240)),
            ["ColorBrush7"] = new SolidColorBrush(Color.FromRgb(222, 236, 253)), ["ColorBrush8"] = new SolidColorBrush(Colors.White),
            ["ColorBrushBg1"] = new SolidColorBrush(Color.FromArgb(190, 222, 236, 253)),
            ["ColorBrushGray4"] = new SolidColorBrush(Color.FromRgb(90, 90, 90)), ["ColorBrushGray5"] = new SolidColorBrush(Color.FromRgb(130, 130, 130)),
            ["ColorObject1"] = Color.FromRgb(52, 61, 74), ["ColorObject4"] = Color.FromRgb(100, 145, 200)
        };
        Dictionary<string, object> saved = new();
        try {
            foreach (var pair in palette) { if (app.Resources.Contains(pair.Key)) saved[pair.Key] = app.Resources[pair.Key]; app.Resources[pair.Key] = pair.Value; }
            ControlServices.Configure(session.Runtime.CreateControlServices(kind => session.Runtime.Host.ReadColor(palette[kind switch {
                ControlThemeColor.Gray4 => "ColorBrushGray4", ControlThemeColor.Gray5 => "ColorBrushGray5", ControlThemeColor.Accent8 => "ColorBrush8", _ => "ColorBrushSemiTransparent"
            }]), _ => { }, (error, _) => session.Faults.Add(error), (error, _) => session.Faults.Add(error), value => session.CustomEvents.Add(value)));
            Require(ControlServices.Current.Animations is ControlAnimationHost<AnimationData>, "C027 used a replacement/no-op animation host.");
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            window = new Window { Width = 520, Height = 360, Left = -20000, Top = -20000, ShowActivated = false, ShowInTaskbar = false, WindowStyle = WindowStyle.None };
            app.MainWindow = window; window.Show();
            Card(window, session); Hint(window, session); Icon(window, session, palette); ListItems(window, session); Virtualization(window, session); Shadow(window, session);
            session.Healthy();
        } finally {
            try { if (window != null) window.Close(); }
            finally {
                session.Stop();
                foreach (string key in palette.Keys) { if (saved.TryGetValue(key, out object value)) app.Resources[key] = value; else app.Resources.Remove(key); }
                if (prior != null) ControlServices.Configure(prior);
                app.MainWindow = priorMain; app.ShutdownMode = priorShutdown;
            }
        }
        Console.WriteLine("PASS C027 native common controls: actual BAML/layout/input/rendering and real frames; full LF platform settings/help/online-image and legacy-wrapper consumers remain separate gates.");
    }
    private static void Card(Window window, Session session) {
        StackPanel content = new() { Margin = new Thickness(0, 40, 0, 0) };
        MyCardView card = new() { Title = "Native card", CanSwap = true, SwapControl = content, UseAnimation = false, IsSwapped = true };
        card.Children.Add(content); int builds = 0, swaps = 0; bool cancel = false; object preview = null;
        card.SetContentBuilder(new[] { "first", "second" }, text => { builds++; return new TextBlock { Text = text, Height = 24 }; });
        card.PreviewSwap += (_, args) => { Require(card.IsSwapped && args.RaiseByMouse, "Card preview no longer precedes expansion."); preview = args; args.Handled = cancel; };
        card.Swap += (_, args) => { Require(ReferenceEquals(args, preview) && !card.IsSwapped, "Card swap lost the same preview args or new state."); swaps++; };
        Display(window, card);
        Require(card.Height == MyCardView.SwapedHeight && content.Visibility == Visibility.Collapsed && builds == 0 && card.MainSwap != null,
            "Initial card collapse initialized lazy content or lost its original height/arrow.");
        // A collapsed card accepts its full header without moving the physical cursor.
        Click(card); window.UpdateLayout(); session.Frames();
        Require(!card.IsSwapped && swaps == 1 && builds == 2 && content.Children.Count == 3 && content.Visibility == Visibility.Visible &&
            ((FrameworkElement)content.Children[2]).Height == 18 && session.CustomEvents.Count(value => ReferenceEquals(value, card)) == 1,
            "Actual card input lost expansion, one-shot builder/spacer or custom-event ordering.");
        card.IsSwapped = true; cancel = true; Click(card); session.Frames();
        Require(card.IsSwapped && swaps == 1 && builds == 2 && card.Height == MyCardView.SwapedHeight,
            "Card preview cancellation rebuilt content, raised Swap or changed collapsed state.");
        int attempts = 0; StackPanel retryContent = new(); MyCardView retry = new() { SwapControl = retryContent, UseAnimation = false };
        retry.SetContentBuilder(stack => { attempts++; stack.Children.Add(new TextBlock()); if (attempts == 1) throw new InvalidOperationException("synthetic builder failure"); });
        bool failed = false; try { retry.BuildContent(); } catch (InvalidOperationException) { failed = true; }
        Require(failed && retryContent.Children.Count == 0, "A failed card builder retained partial native children.");
        retry.BuildContent(); retry.BuildContent(); Require(attempts == 2 && retryContent.Children.Count == 1, "A failed card builder could not retry or was replayed after success.");
        session.Healthy(); Console.WriteLine("PASS C027 card: real initial collapse/input expansion, same preview/swap args, veto, native lazy content/spacer and failed-build retry.");
    }
    private static void Hint(Window window, Session session) {
        Dictionary<string, bool> settings = new(StringComparer.Ordinal); string key = "C027.Hint." + Guid.NewGuid().ToString("N");
        SettingHint hint = new(settings, key) { CanClose = true, Theme = MyHintView.Themes.Blue }; StackPanel panel = new(); panel.Children.Add(hint);
        SettingHint callbackHint = new(settings, key + ".callback"); panel.Children.Add(callbackHint); Display(window, panel);
        Require(hint.Reads == 1 && hint.Writes == 0 && hint.Visibility == Visibility.Visible && callbackHint.Reads == 0 &&
            ((SolidColorBrush)hint.Background).Color == (Color)ColorConverter.ConvertFromString("#d9ecff"), "Hint settings/CanClose or original LF palette changed.");
        int bodyClicks = 0; hint.Click += (_, _) => bodyClicks++;
        Click(hint.BtnClose);
        Require(hint.Writes == 1 && settings[key] && !hint.IsHitTestVisible && bodyClicks == 0 && session.Runtime.Engine.AniIsRun("MyCard Dispose " + hint.Uuid),
            "Close input did not persist once before real disposal or leaked into the hint body click.");
        session.Frames(); Require(hint.Visibility == Visibility.Collapsed, "The close-button animation did not collapse the actual hint.");
        SettingHint reopened = new(settings, key) { CanClose = true }; panel.Children.Add(reopened); window.UpdateLayout();
        PumpUntil(() => reopened.Reads == 1, "A second hint did not read its isolated persisted setting.");
        Require(reopened.Visibility == Visibility.Collapsed && reopened.Writes == 0, "A dismissed hint reappeared or rewrote settings on load.");
        int callbacks = 0; object disposed = null;
        callbackHint.DisposeAnimated(false, value => { Require(Environment.CurrentManagedThreadId == session.UiThread, "Hint completion left STA."); callbacks++; disposed = value; });
        Require(callbacks == 0 && !callbackHint.IsHitTestVisible, "Hint completion ran before its real animation frames.");
        session.Frames(); callbackHint.DisposeAnimated(false, _ => callbacks++);
        Require(callbacks == 1 && ReferenceEquals(disposed, callbackHint) && callbackHint.Visibility == Visibility.Collapsed,
            "Hint dispose lost callback identity, terminal visibility or its repeated-dispose guard.");
        session.Healthy(); Console.WriteLine("PASS C027 hint: actual close input, isolated setting write/reload, LF colors and real terminal callback/guard.");
    }
    private static void Icon(Window window, Session session, Dictionary<string, object> palette) {
        MyIconButtonView icon = new() { Width = 40, Height = 40, Logo = "M0,0 L8,8", Theme = MyIconButtonView.Themes.Custom, Foreground = new SolidColorBrush(Colors.DarkBlue) };
        Display(window, icon); int clicks = 0; object senderSeen = null; EventArgs argsSeen = null; bool handledDuringClick = true;
        icon.Click += (sender, args) => { clicks++; senderSeen = sender; argsSeen = args; handledDuringClick = ((MouseButtonEventArgs)args).Handled; };
        MouseEvent(icon, UIElement.MouseLeftButtonDownEvent); session.Frame(60);
        double partial = ((ScaleTransform)icon.PanBack.RenderTransform).ScaleX;
        Require(partial > .8 && partial < 1, "Icon press did not execute real native scale frames.");
        MouseButtonEventArgs up = MouseEvent(icon, UIElement.MouseLeftButtonUpEvent); session.Frames();
        Require(clicks == 1 && ReferenceEquals(senderSeen, icon) && ReferenceEquals(argsSeen, up) && !handledDuringClick && up.Handled &&
            session.CustomEvents.Count(value => ReferenceEquals(value, icon)) == 1 && Math.Abs(((ScaleTransform)icon.PanBack.RenderTransform).ScaleX - 1) < .0001,
            "Icon sender/args/handled/custom order or real release scale changed.");
        MouseEvent(icon, UIElement.MouseLeftButtonDownEvent); Leave(icon); MouseEvent(icon, UIElement.MouseLeftButtonUpEvent); session.Frames();
        Require(clicks == 1, "Leaving a pressed icon did not cancel the click.");
        icon.CanClick = false; Click(icon); Require(clicks == 1, "CanClick=false still emitted icon Click.");
        icon.IsEnabled = false; session.Frames();
        Require(ReferenceEquals(icon.Path.Fill, palette["ColorBrushGray5"]), "A disabled icon did not finish with its actual LF resource binding.");
        session.Healthy(); Console.WriteLine("PASS C027 icon: actual press/release frames, sender/args/handled/custom callbacks, leave cancellation, CanClick and disabled resource binding.");
    }
    private static void ListItems(Window window, Session session) {
        MyListItemView first = new() { Title = "First", Type = MyListItemView.CheckType.RadioBox }, second = new() { Title = "Second", Type = MyListItemView.CheckType.RadioBox };
        StackPanel radios = new(); radios.Children.Add(first); radios.Children.Add(second);
        MyListItemView check = new() { Title = "Check", Type = MyListItemView.CheckType.CheckBox }, menu = new() { Title = "Menu\r\n\u2028Title", Info = "one\f\u0085two" };
        StackPanel panel = new(); panel.Children.Add(radios); panel.Children.Add(check); panel.Children.Add(menu); Display(window, panel);
        first.SetChecked(true, false, false); bool veto = true; int checks = 0; ControlPreviewEventArgs changed = null;
        second.Changed += (_, args) => { Require(args.RaiseByMouse, "Radio user flag changed."); changed = args; args.Handled = veto; };
        second.Changed += (_, args) => Require(ReferenceEquals(args, changed) && args.Handled == veto, "Radio handlers did not share veto args.");
        second.Check += (_, args) => { Require(args.RaiseByMouse, "Radio Check lost its user flag."); checks++; };
        second.SetChecked(true, true, true); Require(first.Checked && !second.Checked && checks == 0, "Radio Changed veto did not restore the exact prior selection.");
        veto = false; second.SetChecked(true, true, true); session.Frames();
        Require(!first.Checked && second.Checked && checks == 1 && second.RectCheck.Height > 0 && second.RectCheck.Height < second.ActualHeight,
            "Actual list radio group/selection marker did not progress after accepting selection.");
        bool cancelChanged = true, cancelCheck = true; int checkEvents = 0;
        check.Changed += (_, args) => args.Handled = cancelChanged;
        check.Check += (_, args) => { checkEvents++; args.Handled = cancelCheck; };
        check.SetChecked(true, true, true); Require(!check.Checked && checkEvents == 0, "Checkbox Changed veto did not roll back before Check.");
        cancelChanged = false; check.SetChecked(true, true, true);
        Require(check.Checked && checkEvents == 1 && !session.Runtime.Engine.AniIsRun("MyListItem Checked " + check.Uuid) && check.RectCheck.Height == 0,
            "Check.Handled lost LF's intentional retained-value/no-style-update behavior.");
        check.SetChecked(false, false, false); cancelCheck = false; check.SetChecked(true, true, true); session.Frame(75);
        Require(check.RectCheck.Height > 0 && check.RectCheck.Height < check.ActualHeight, "Checkbox marker did not execute native Height frames.");
        session.Frames(); Require(check.Checked && checkEvents == 2 && Math.Abs(check.RectCheck.Height - (check.ActualHeight - 12)) < .001,
            "Checkbox native height marker did not reach its original LF size.");
        EventArgs expected = new(); int handlers = 0, replacementCalls = 0; Action<MyListItemView, EventArgs> handler = (sender, args) => {
            Require(ReferenceEquals(sender, menu) && ReferenceEquals(args, expected), "Menu callback lost actual view/args references."); handlers++; menu.ContentHandler = (_, _) => replacementCalls++;
        };
        menu.ContentHandler = handler; Require(ReferenceEquals(menu.ContentHandler, handler), "ContentHandler was wrapped or copied.");
        menu.RefreshColor(menu, expected); menu.RefreshColor(menu, EventArgs.Empty);
        MyIconButtonView button = new() { Logo = "M0,0 L8,8" }; MyIconButtonView[] buttons = { button }; menu.Buttons = buttons;
        Require(handlers == 1 && replacementCalls == 0 && menu.ContentHandler == null && ReferenceEquals(menu.Buttons, buttons) && ReferenceEquals(menu.ButtonStack, button) &&
            menu.Title == "MenuTitle" && menu.Info == "onetwo", "One-shot handler clear, native button identity or full LF newline normalization changed.");
        session.Healthy(); Console.WriteLine("PASS C027 list: real group exclusivity, Changed veto/Check.Handled distinction, original Height frames, delegate/button identities and Unicode text normalization.");
    }
    private static void Virtualization(Window window, Session session) {
        int factories = 0, unloads = 0;
        MyVirtualizingElementView<TextBlock> virtualized = new(() => new TextBlock { Text = "Realized " + ++factories, Height = 28 }) { Height = 28 };
        virtualized.Unloaded += (_, _) => unloads++;
        StackPanel stack = new(); stack.Children.Add(new FrameworkElement { Height = 180 }); stack.Children.Add(virtualized); stack.Children.Add(new FrameworkElement { Height = 100 });
        ScrollViewer scroll = new() { Width = 340, Height = 60, Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        StackPanel outer = new(); outer.Children.Add(scroll); Display(window, outer);
        Require(factories == 0 && ReferenceEquals(stack.Children[1], virtualized), "An off-viewport virtual element was eagerly created.");
        outer.Children.Remove(scroll); window.UpdateLayout(); PumpUntil(() => !virtualized.IsLoaded, "Virtual element did not unload with its scroll host.");
        outer.Children.Add(scroll); window.UpdateLayout(); PumpUntil(() => virtualized.IsLoaded, "Virtual element did not reload with its scroll host.");
        Require(factories == 0 && unloads >= 1, "Unloading/reloading an off-viewport element initialized or cached native content.");
        scroll.ScrollToVerticalOffset(170); window.UpdateLayout();
        PumpUntil(() => factories == 1 && stack.Children[1] is TextBlock text && text.IsLoaded, "Entering the real viewport did not replace the virtual element at its original index.");
        FrameworkElement realized = (FrameworkElement)stack.Children[1]; window.UpdateLayout();
        Require(virtualized.Parent == null && factories == 1 && realized is TextBlock, "Viewport realization retained the placeholder or repeated its factory.");
        TextBlock fresh = virtualized.Init(); Require(factories == 2 && !ReferenceEquals(fresh, realized) && ReferenceEquals(stack.Children[1], realized),
            "Explicit Init on a detached placeholder unexpectedly cached a value or replaced unrelated content.");
        Border invalidParent = new(); int attempted = 0; MyVirtualizingElementView plain = new(() => { attempted++; return new TextBlock(); }); invalidParent.Child = plain;
        bool rejected = false; try { plain.Init(); } catch (Exception error) { rejected = error.Message == "MyVirtualizingElement 的父级必须是一个 Panel"; }
        Require(rejected && attempted == 1 && ReferenceEquals(invalidParent.Child, plain) && ReferenceEquals(MyVirtualizingElementView.TryInit(realized), realized),
            "Virtualization changed factory-before-parent validation, parent safety or nonvirtual passthrough.");
        session.Healthy(); Console.WriteLine("PASS C027 virtualization: actual offscreen viewport/unload/reload, original-index realization, explicit Init without caching and parent guard.");
    }
    private static void Shadow(Window window, Session session) {
        MyDropShadowView shadow = new() { Width = 96, Height = 60, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, ShadowRadius = 6, CornerRadius = new CornerRadius(7, 9, 11, 13), Color = Color.FromArgb(150, 30, 60, 90) };
        Display(window, shadow);
        byte[] Render() { RenderTargetBitmap bitmap = new(96, 60, 96, 96, PixelFormats.Pbgra32); bitmap.Render(shadow); byte[] pixels = new byte[96 * 60 * 4]; bitmap.CopyPixels(pixels, 96 * 4, 0); return pixels; }
        byte[] first = Render(); int center = (30 * 96 + 48) * 4 + 3, edge = (30 * 96 + 1) * 4 + 3;
        Require(first[center] == 150 && first[edge] < first[center], "Actual nine-region shadow rendering lost solid center or fading edge alpha.");
        shadow.Color = Color.FromArgb(180, 90, 60, 30); window.UpdateLayout(); byte[] second = Render();
        Require(second[center] == 180 && !first.SequenceEqual(second), "Changing the shadow DP retained stale cached gradient brushes.");
        bool rejected = false; try { shadow.CornerRadius = new CornerRadius(double.NaN); } catch (ArgumentException) { rejected = true; }
        Require(rejected, "The shadow accepted an invalid corner radius.");
        session.Healthy(); Console.WriteLine("PASS C027 shadow: actual native render pixels, edge alpha, DP cache invalidation and invalid radius rejection.");
    }
}
