param([Parameter(Mandatory=$true)][string]$ExecutablePath,[switch]$Isolated,[string]$FixtureRoot,[string]$ExpectedPresentationHash)
# A fresh child receives only the EXE; all LF dependencies must resolve from its embedded resources.
$ErrorActionPreference='Stop'
if($PSVersionTable.PSEdition -ne 'Desktop' -or [Threading.Thread]::CurrentThread.ApartmentState -ne 'STA'){throw 'Use powershell -NoProfile -STA.'}
$root=Split-Path $PSScriptRoot -Parent
function Hash-Bytes([byte[]]$bytes){$hash=[Security.Cryptography.SHA256]::Create();try{return ([BitConverter]::ToString($hash.ComputeHash($bytes))).Replace('-','').ToLowerInvariant()}finally{$hash.Dispose()}}
if(-not $Isolated){
 $fixture=Join-Path ([IO.Path]::GetTempPath()) ('PclMigratedControls-'+[Guid]::NewGuid().ToString('N'))
 [IO.Directory]::CreateDirectory($fixture)|Out-Null
 $process=$null;$started=$false
 try{
  $source=[IO.Path]::GetFullPath($ExecutablePath);$copy=Join-Path $fixture ([IO.Path]::GetFileName($source));[IO.File]::Copy($source,$copy)
  $expected=Hash-Bytes ([IO.File]::ReadAllBytes((Join-Path $root 'presentation/bin/PCLLF.Presentation.dll')))
  # Windows file paths cannot contain quotes; -File receives literal arguments, never executable command text.
  foreach($value in @($PSCommandPath,$copy,$fixture)){if($value.Contains('"')){throw 'Invalid quote in a Windows fixture path'}}
  $info=[Diagnostics.ProcessStartInfo]::new((Join-Path $PSHOME 'powershell.exe'))
  $info.UseShellExecute=$false;$info.CreateNoWindow=$true;$info.RedirectStandardOutput=$true;$info.RedirectStandardError=$true;$info.WorkingDirectory=$fixture
  $info.Arguments='-NoProfile -STA -File "'+$PSCommandPath+'" -ExecutablePath "'+$copy+'" -Isolated -FixtureRoot "'+$fixture+'" -ExpectedPresentationHash '+$expected
  $process=[Diagnostics.Process]::new();$process.StartInfo=$info
  $started=$process.Start();if(-not $started){throw 'Could not start the standalone-EXE control fixture'}
  $output=$process.StandardOutput.ReadToEndAsync();$errors=$process.StandardError.ReadToEndAsync()
  if(-not $process.WaitForExit(60000)){$process.Kill();if(-not $process.WaitForExit(5000)){throw 'The timed-out standalone child could not be stopped'};throw 'Standalone-EXE native control validation exceeded 60 seconds'}
  $stdout=$output.GetAwaiter().GetResult();$stderr=$errors.GetAwaiter().GetResult()
  if($stdout){Write-Output $stdout.TrimEnd()};if($stderr){[Console]::Error.WriteLine($stderr.TrimEnd())}
  if($process.ExitCode -ne 0){throw ('Standalone-EXE native control validation failed with exit '+$process.ExitCode)}
 }finally{
  if($process){try{if($started -and -not $process.HasExited){$process.Kill();$null=$process.WaitForExit(5000)}}finally{$process.Dispose()}}
  $full=[IO.Path]::GetFullPath($fixture);$prefix=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')+'\'
  if($full.StartsWith($prefix,[StringComparison]::OrdinalIgnoreCase) -and [IO.Path]::GetFileName($full) -cmatch '^PclMigratedControls-[a-f0-9]{32}$' -and [IO.Directory]::Exists($full)){Remove-Item -LiteralPath $full -Recurse -Force}
 }
 exit 0
}
$fixture=[IO.Path]::GetFullPath($FixtureRoot);$executable=[IO.Path]::GetFullPath($ExecutablePath)
$tempPrefix=[IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')+'\'
if(-not $fixture.StartsWith($tempPrefix,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetFileName($fixture) -cnotmatch '^PclMigratedControls-[a-f0-9]{32}$' -or [IO.Path]::GetDirectoryName($executable) -ine $fixture -or $ExpectedPresentationHash -cnotmatch '^[a-f0-9]{64}$'){throw 'Invalid standalone-EXE fixture parameters'}
if(@([IO.Directory]::GetFiles($fixture,'*.dll')).Count -ne 0 -or @([IO.Directory]::GetFiles($fixture,'*.exe')).Count -ne 1){throw 'The isolated child must begin with exactly the copied EXE and no dependency sidecars'}
Add-Type -AssemblyName PresentationFramework,PresentationCore,WindowsBase,System.Xaml
$asm=[Reflection.Assembly]::LoadFrom($executable);$flags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance'
$app=[Activator]::CreateInstance($asm.GetType('PCL.Application',$true),$true)
[Windows.Application]::ResourceAssembly=$asm
$testRoot=$null;$previousRoot=$null
$checks = @'
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using PCLLF.Presentation.Controls;

public static class PclMigratedControlChecks {
    private static readonly FieldInfo LastTick = typeof(PCL.ModAnimation).GetField("AniLastTick", BindingFlags.NonPublic | BindingFlags.Static);
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Step(int frames) {
        Require(LastTick != null, "The exact LF AniLastTick hook is missing.");
        for (int index = 0; index < frames; index++) { LastTick.SetValue(null, long.MaxValue); PCL.ModAnimation.AniTimer(16.0); }
    }
    private static void PumpUntil(Func<bool> ready) {
        if (ready()) return;
        DispatcherFrame frame = new DispatcherFrame(); Stopwatch elapsed = Stopwatch.StartNew();
        DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Background); timer.Interval = TimeSpan.FromMilliseconds(10);
        timer.Tick += delegate { if (ready() || elapsed.ElapsedMilliseconds > 4000) frame.Continue = false; };
        timer.Start(); try { Dispatcher.PushFrame(frame); } finally { timer.Stop(); }
        Require(ready(), "The offscreen native controls did not load in their real WPF presentation source.");
    }
    private static void InlineForwarding(System.Windows.Documents.InlineCollection forwarded, TextBlock visual, string label) {
        // TextBlock.Inlines creates collection wrappers. Verify their shared visual content,
        // as the original LF getter returns LabText.Inlines without caching its wrapper.
        int originalCount = visual.Inlines.Count;
        System.Windows.Documents.Run throughControl = new System.Windows.Documents.Run(label + " control");
        System.Windows.Documents.Run throughVisual = new System.Windows.Documents.Run(label + " visual");
        try {
            forwarded.Add(throughControl);
            Require(visual.Inlines.Count == originalCount + 1 && visual.Inlines.Contains(throughControl), label + " inline mutation did not reach the original visual.");
            visual.Inlines.Add(throughVisual);
            Require(forwarded.Count == originalCount + 2 && forwarded.Contains(throughVisual), label + " original visual mutation did not reach the forwarded collection.");
        } finally {
            forwarded.Remove(throughControl);
            visual.Inlines.Remove(throughVisual);
        }
        Require(visual.Inlines.Count == originalCount && forwarded.Count == originalCount, label + " inline cleanup changed original text content.");
    }
    private static MouseButtonEventArgs RaiseMouse(UIElement control, RoutedEvent routedEvent) {
        Require(PresentationSource.FromVisual(control) != null, "Synthetic mouse input requires the live offscreen presentation source.");
        MouseButtonEventArgs args = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left);
        args.RoutedEvent = routedEvent; control.RaiseEvent(args); return args;
    }
    private static void Click(UIElement control) { RaiseMouse(control, UIElement.MouseLeftButtonDownEvent); RaiseMouse(control, UIElement.MouseLeftButtonUpEvent); }
    private static void Leave(UIElement control) {
        MouseEventArgs args = new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount); args.RoutedEvent = UIElement.MouseLeaveEvent; control.RaiseEvent(args);
    }
    private static int AnimationId(string name) {
        PCL.ModAnimation.AniGroupEntry entry;
        return PCL.ModAnimation.AniGroups.TryGetValue(name, out entry) ? entry.Uuid : -1;
    }
    private static void Alias(DependencyProperty legacy, DependencyProperty migrated, Type target, string name, Type propertyType) {
        Require(object.ReferenceEquals(legacy, migrated) && legacy.PropertyType == propertyType, "Dependency property alias/type changed: " + target.Name + "." + name);
        DependencyPropertyDescriptor descriptor = DependencyPropertyDescriptor.FromName(name, target, target);
        Require(descriptor != null && object.ReferenceEquals(descriptor.DependencyProperty, legacy) && legacy.GetMetadata(target) != null,
            "Inherited dependency property discovery failed: " + target.Name + "." + name);
    }
    private static void Parts(FrameworkElement control, params string[] names) {
        foreach (string name in names) Require(control.FindName(name) != null, "Missing original BAML part " + control.GetType().Name + "." + name);
        Require(object.ReferenceEquals(control.FindName("PanBack"), control), "The original root part no longer identifies its native wrapper.");
    }
    private static void StaticApi() {
        Require(typeof(PCL.MyButton).BaseType == typeof(MyButtonView) && typeof(PCL.MyRadioButton).BaseType == typeof(MyRadioButtonView) &&
            typeof(PCL.MyCheckBox).BaseType == typeof(MyCheckBoxView), "Native PCL control identities do not inherit the migrated C# views.");
        Alias(PCL.MyButton.TextProperty, MyButtonView.TextProperty, typeof(PCL.MyButton), "Text", typeof(string));
        Alias(PCL.MyButton.PaddingProperty, MyButtonView.PaddingProperty, typeof(PCL.MyButton), "Padding", typeof(Thickness));
        Alias(PCL.MyRadioButton.TextProperty, MyRadioButtonView.TextProperty, typeof(PCL.MyRadioButton), "Text", typeof(string));
        Alias(PCL.MyCheckBox.TextProperty, MyCheckBoxView.TextProperty, typeof(PCL.MyCheckBox), "Text", typeof(string));
        Alias(PCL.MyCheckBox.CheckedProperty, MyCheckBoxView.CheckedProperty, typeof(PCL.MyCheckBox), "Checked", typeof(bool));
        Require(typeof(PCL.MyCheckBox).GetProperty("Checked").PropertyType == typeof(bool) &&
            object.Equals(PCL.MyCheckBox.CheckedProperty.GetMetadata(typeof(PCL.MyCheckBox)).DefaultValue, false) &&
            typeof(PCL.MyCheckBox).GetProperty("IsThreeState") == null, "LF's nonnullable two-state checkbox contract changed.");
        string ns = " xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:p='clr-namespace:PCL;assembly=" + typeof(PCL.MyButton).Assembly.GetName().Name + "' ";
        PCL.MyButton button = (PCL.MyButton)XamlReader.Parse("<p:MyButton" + ns + "Text='XAML button' Padding='3,5,7,9'/>");
        PCL.MyRadioButton radio = (PCL.MyRadioButton)XamlReader.Parse("<p:MyRadioButton" + ns + "Text='XAML radio' Logo='M0,0 L8,8'/>");
        PCL.MyCheckBox check = (PCL.MyCheckBox)XamlReader.Parse("<p:MyCheckBox" + ns + "Text='XAML checkbox' Checked='True'/>");
        Parts(button, "PanBack", "PanFore", "LabText"); Parts(radio, "PanBack", "ShapeLogo", "LabText"); Parts(check, "PanBack", "ShapeBorder", "ShapeCheck", "LabText");
        Require(button.LabText.Text == "XAML button" && button.PanFore.Padding == new Thickness(3, 5, 7, 9) && radio.LabText.Text == "XAML radio" &&
            check.LabText.Text == "XAML checkbox" && check.Checked && ((ScaleTransform)check.ShapeCheck.RenderTransform).ScaleX == 1,
            "Inherited XAML property callbacks did not update their original visual parts.");
        InlineForwarding(button.Inlines, button.LabText, "button");
        InlineForwarding(radio.Inlines, radio.LabText, "radio");
        InlineForwarding(check.Inlines, check.LabText, "checkbox");
        button.TextPadding = new Thickness(2); button.Padding = new Thickness(4); ScaleTransform transform = new ScaleTransform(.9, .9); button.RealRenderTransform = transform;
        radio.LogoScale = .8; button.ColorType = PCL.MyButton.ColorState.Red; radio.ColorType = PCL.MyRadioButton.ColorState.Highlight;
        Require(button.LabText.Padding == new Thickness(2) && button.PanFore.Padding == new Thickness(4) && object.ReferenceEquals(button.PanFore.RenderTransform, transform) &&
            ((ScaleTransform)radio.ShapeLogo.RenderTransform).ScaleX == .8 && ((MyButtonView)button).ColorType == MyButtonView.ColorState.Red &&
            ((MyRadioButtonView)radio).ColorType == MyRadioButtonView.ColorState.Highlight, "Original layout, logo or nested color enum forwarding changed.");
        Console.WriteLine("PASS controls 1/6: native identities, DP aliases/discovery, actual XAML callbacks, original parts and property forwarding.");
    }
    private static void InitialSetting() {
        PCL.MyCheckBox check = new PCL.MyCheckBox(); int changes = 0, previews = 0;
        check.Change += delegate { changes++; }; check.PreviewChange += delegate { previews++; };
        PCL.ISettingControl setting = check; setting.RefreshSetting("True");
        Require(!check.IsLoaded && check.Checked && setting.GetCurrentSetting() == "True" && ((ScaleTransform)check.ShapeCheck.RenderTransform).ScaleX == 1,
            "Unloaded setting refresh did not immediately synchronize the real checkbox visual.");
        setting.RefreshSetting("false");
        Require(!check.Checked && setting.GetCurrentSetting() == "False" && ((ScaleTransform)check.ShapeCheck.RenderTransform).ScaleX == 0 && changes == 0 && previews == 0,
            "Unloaded bool refresh raised user/change events or retained the old visual.");
        bool rejected = false; try { setting.RefreshSetting("indeterminate"); } catch (FormatException) { rejected = true; }
        Require(rejected && !check.Checked && changes == 0 && previews == 0, "The original bool.Parse setting boundary was replaced by a third state or silent fallback.");
        Console.WriteLine("PASS controls 2/6: unloaded bool setting refresh, immediate visuals, exact GetCurrentSetting and invalid-value rejection.");
    }
    private static void ButtonBehavior(PCL.MyButton button) {
        string scaleName = "MyButton Scale " + button.Uuid; int clicks = 0, custom = 0, pressedId = -1; bool valid = true;
        MouseButtonEventArgs expected = null; List<string> events = new List<string>();
        DependencyPropertyDescriptor descriptor = DependencyPropertyDescriptor.FromProperty(PCL.CustomEventService.EventsProperty, button.GetType());
        Require(descriptor != null && button.ReadLocalValue(PCL.CustomEventService.EventsProperty) == DependencyProperty.UnsetValue, "The button custom-event fixture was already initialized.");
        EventHandler customChanged = delegate {
            custom++; events.Add("custom"); valid &= AnimationId(scaleName) == pressedId;
        };
        button.Click += delegate(object sender, MouseButtonEventArgs args) {
            clicks++; events.Add("click"); valid &= object.ReferenceEquals(sender, button) && object.ReferenceEquals(args, expected) &&
                button.ReadLocalValue(PCL.CustomEventService.EventsProperty) == DependencyProperty.UnsetValue && AnimationId(scaleName) == pressedId;
        };
        descriptor.AddValueChanged(button, customChanged);
        try {
            RaiseMouse(button, UIElement.MouseLeftButtonUpEvent); Require(clicks == 0, "A release without a press raised Click.");
            RaiseMouse(button, UIElement.MouseLeftButtonDownEvent); pressedId = AnimationId(scaleName); Step(5);
            Require(pressedId != -1 && ((ScaleTransform)button.RealRenderTransform).ScaleX < .99 && ((ScaleTransform)button.RealRenderTransform).ScaleX > .8,
                "The real LF animation timer did not progress the button press scale.");
            expected = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left); expected.RoutedEvent = UIElement.MouseLeftButtonUpEvent; button.RaiseEvent(expected);
            Require(clicks == 1 && custom == 1 && valid && string.Join("|", events) == "click|custom" && !expected.Handled &&
                AnimationId(scaleName) != pressedId && PCL.CustomEventService.GetEvents(button).Events.Count == 0,
                "Button sender/args/handled state or Click -> CustomEvent -> release-animation order changed.");
            Step(35); Require(Math.Abs(((ScaleTransform)button.RealRenderTransform).ScaleX - 1) < .001, "Button release animation did not restore scale.");
            RaiseMouse(button, UIElement.MouseLeftButtonDownEvent); Step(3); Leave(button); RaiseMouse(button, UIElement.MouseLeftButtonUpEvent); Step(60);
            Require(clicks == 1 && Math.Abs(((ScaleTransform)button.RealRenderTransform).ScaleX - 1) < .001, "Leaving a pressed button did not cancel its click and restore its scale.");
        } finally { descriptor.RemoveValueChanged(button, customChanged); }
        Console.WriteLine("PASS controls 3/6: original button input, sender/args, event order, leave cancellation and real press/release animation ticks.");
    }
    private static void RadioBehavior(PCL.MyRadioButton first, PCL.MyRadioButton second, PCL.MyRadioButton third) {
        List<string> events = new List<string>(); bool valid = true, cancel = true; PCL.ModBase.RouteEventArgs preview = null;
        first.Check += delegate(object sender, bool user) { valid &= object.ReferenceEquals(sender, first); events.Add("first:" + user); };
        second.Check += delegate(object sender, bool user) { valid &= object.ReferenceEquals(sender, second); events.Add("second:" + user); };
        second.PreviewClick += delegate(object sender, PCL.ModBase.RouteEventArgs args) {
            valid &= object.ReferenceEquals(sender, second) && args.RaiseByMouse && args.GetType() == typeof(PCL.ModBase.RouteEventArgs) && !second.Checked;
            preview = args; args.Handled = cancel; events.Add("preview1");
        };
        second.PreviewClick += delegate(object sender, PCL.ModBase.RouteEventArgs args) {
            valid &= object.ReferenceEquals(sender, second) && object.ReferenceEquals(args, preview) && args.Handled == cancel; events.Add("preview2");
        };
        first.SetChecked(false, false, false);
        Require(first.Checked && !second.Checked && !third.Checked && string.Join("|", events) == "first:False", "An empty radio group did not select its original first item.");
        events.Clear(); second.SetChecked(true, false, false);
        Require(!first.Checked && second.Checked && string.Join("|", events) == "second:False", "Programmatic radio selection did not keep one checked item without preview.");
        events.Clear(); second.SetChecked(false, false, false);
        Require(first.Checked && !second.Checked && string.Join("|", events) == "first:False", "Clearing the only radio selection lost the first-item fallback.");
        events.Clear(); Click(second);
        Require(valid && first.Checked && !second.Checked && string.Join("|", events) == "preview1|preview2", "Original RouteEventArgs cancellation did not preserve the radio selection.");
        events.Clear(); cancel = false; Click(second); Step(25);
        Require(valid && !first.Checked && second.Checked && !third.Checked && string.Join("|", events) == "preview1|preview2|second:True",
            "Radio preview/check ordering, same-args propagation or user flag changed.");
        events.Clear(); RaiseMouse(first, UIElement.MouseLeftButtonDownEvent); Leave(first); RaiseMouse(first, UIElement.MouseLeftButtonUpEvent); Step(25);
        Require(!first.Checked && second.Checked && events.Count == 0, "Radio MouseLeave failed to cancel a pending press.");
        Console.WriteLine("PASS controls 4/6: native radio grouping, first-item fallback, same original preview args, cancellation, event order and leave handling.");
    }
    private static void CheckboxBehavior(PCL.MyCheckBox check) {
        Require(PCL.SettingService.GetKey(check) == null, "Synthetic checkbox must not write a real setting.");
        PCL.ISettingControl setting = check; List<string> events = new List<string>(); bool valid = true, cancel = true;
        PCL.ModBase.RouteEventArgs preview = null; double scaleAtChange = double.NaN;
        check.PreviewChange += delegate(object sender, PCL.ModBase.RouteEventArgs args) {
            valid &= object.ReferenceEquals(sender, check) && args.RaiseByMouse && args.GetType() == typeof(PCL.ModBase.RouteEventArgs) && !check.Checked;
            preview = args; args.Handled = cancel; events.Add("preview1");
        };
        check.PreviewChange += delegate(object sender, PCL.ModBase.RouteEventArgs args) {
            valid &= object.ReferenceEquals(sender, check) && object.ReferenceEquals(args, preview) && args.Handled == cancel; events.Add("preview2");
        };
        check.Change += delegate(object sender, bool user) {
            valid &= object.ReferenceEquals(sender, check); events.Add("change:" + user + ":" + setting.GetCurrentSetting());
            if (user && check.Checked) scaleAtChange = ((ScaleTransform)check.ShapeCheck.RenderTransform).ScaleX;
        };
        check.SetChecked(true, true); Step(50);
        Require(valid && !check.Checked && setting.GetCurrentSetting() == "False" && string.Join("|", events) == "preview1|preview2" &&
            ((ScaleTransform)check.ShapeCheck.RenderTransform).ScaleX == 0, "Checkbox cancellation changed state, emitted Change or lost the original preview args.");
        events.Clear(); cancel = false; check.SetChecked(true, true);
        Require(valid && check.Checked && string.Join("|", events) == "preview1|preview2|change:True:True" && scaleAtChange == 0 &&
            PCL.ModAnimation.AniIsRun("MyCheckBox AllowMouseDown " + check.Uuid), "Checkbox preview/change/current-setting/UI ordering or delayed mouse guard changed.");
        Click(check);
        Require(check.Checked && events.Count == 3, "Checkbox accepted another mouse toggle before its original animation guard completed.");
        Step(12); double partial = ((ScaleTransform)check.ShapeCheck.RenderTransform).ScaleX;
        Require(partial > 0 && partial < 1.5, "Real checkbox animation ticks did not progress the delayed check mark.");
        Step(45);
        Require(Math.Abs(((ScaleTransform)check.ShapeCheck.RenderTransform).ScaleX - 1) < .001 && Math.Abs(check.ShapeBorder.Width - 18) < .001,
            "Checkbox check animation did not finish at the original scale and border size.");
        events.Clear(); Click(check); Step(50);
        Require(!check.Checked && string.Join("|", events) == "change:True:False", "The real delayed callback did not restore mouse editing, or unchecking incorrectly raised PreviewChange.");
        events.Clear(); check.SetChecked(true, false); Step(50);
        Require(check.Checked && string.Join("|", events) == "change:False:True", "Non-user changes incorrectly raised PreviewChange or lost their false user flag.");
        events.Clear(); check.SetChecked(true, true); Require(events.Count == 0, "Setting the same checkbox value raised events.");
        setting.RefreshSetting("False"); Step(50);
        Require(!check.Checked && setting.GetCurrentSetting() == "False" && string.Join("|", events) == "change:False:False", "ISettingControl refresh lost its original non-user boolean event contract.");
        events.Clear(); check.SetChecked(false, true); Require(events.Count == 0, "Unchanged false state raised a user preview.");
        bool rejected = false; try { check.SetValue(PCL.MyCheckBox.CheckedProperty, null); } catch (ArgumentException) { rejected = true; }
        Require(valid && rejected && !check.Checked && events.Count == 0, "Checkbox DP admitted a third state or mutated its event contract.");
        Console.WriteLine("PASS controls 5/6: checkbox preview conditions/cancellation, original args/sender/order, setting refresh and real scale/delayed mouse-guard ticks.");
    }
    private static void SameColor(Brush actual, string expectedKey) {
        SolidColorBrush first = actual as SolidColorBrush, second = Application.Current.FindResource(expectedKey) as SolidColorBrush;
        Require(first != null && second != null && Math.Abs(first.Color.A - second.Color.A) <= 1 && Math.Abs(first.Color.R - second.Color.R) <= 1 &&
            Math.Abs(first.Color.G - second.Color.G) <= 1 && Math.Abs(first.Color.B - second.Color.B) <= 1, "Original theme color was not reached: " + expectedKey);
    }
    private static void ThemeBehavior(PCL.MyButton button, PCL.MyRadioButton radio, PCL.MyCheckBox check) {
        button.ColorType = PCL.MyButton.ColorState.Red; Step(25); SameColor(button.PanFore.BorderBrush, "ColorBrushRedDark");
        button.IsEnabled = false; Step(25); SameColor(button.PanFore.BorderBrush, "ColorBrushGray4");
        button.IsEnabled = true; button.ColorType = PCL.MyButton.ColorState.Highlight; Step(25); SameColor(button.PanFore.BorderBrush, "ColorBrush2");
        radio.ColorType = PCL.MyRadioButton.ColorState.Highlight; Step(25); SameColor(radio.Background, "ColorBrush3");
        check.IsEnabled = false; Step(25); Leave(check); Step(25);
        SameColor(check.ShapeBorder.BorderBrush, "ColorBrushGray5"); SameColor(check.LabText.Foreground, "ColorBrushGray5");
        check.IsEnabled = true; Step(25); SameColor(check.ShapeBorder.BorderBrush, "ColorBrush1");
        Console.WriteLine("PASS controls 6/6: real LF theme color animations, original nested color states and disabled-checkbox MouseLeave guard.");
    }
    public static void Run() {
        int enabled = PCL.ModAnimation.AniControlEnabled; object last = LastTick == null ? null : LastTick.GetValue(null);
        Application app = Application.Current; Window previousMain = app.MainWindow; ShutdownMode shutdown = app.ShutdownMode;
        PCL.MyButton button = null; PCL.MyCheckBox check = null; PCL.MyRadioButton first = null, second = null, third = null; Window window = null;
        try {
            PCL.ModAnimation.AniControlEnabled = 0; app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            StaticApi(); InitialSetting();
            Require(ControlServices.Current.Animations is ControlAnimationHost<PCL.ModAnimation.AniData> && ControlServices.Current.AnimationEnabled(),
                "Native controls did not use the real LF AniData host; a stub animation adapter was configured.");
            StackPanel panel = new StackPanel(); StackPanel radios = new StackPanel { Orientation = Orientation.Horizontal };
            button = new PCL.MyButton { Text = "Native button", Height = 36 }; check = new PCL.MyCheckBox { Text = "Native checkbox", Height = 30 };
            first = new PCL.MyRadioButton { Text = "First", Logo = "M0,0 L8,8" }; second = new PCL.MyRadioButton { Text = "Second", Logo = "M0,0 L8,8" }; third = new PCL.MyRadioButton { Text = "Third", Logo = "M0,0 L8,8" };
            radios.Children.Add(first); radios.Children.Add(second); radios.Children.Add(third); panel.Children.Add(button); panel.Children.Add(radios); panel.Children.Add(check);
            window = new Window { Content = panel, Width = 500, Height = 300, Left = -20000, Top = -20000, ShowActivated = false, ShowInTaskbar = false, WindowStyle = WindowStyle.None };
            window.Show(); window.UpdateLayout();
            PumpUntil(delegate { return button.IsLoaded && first.IsLoaded && second.IsLoaded && third.IsLoaded && check.IsLoaded; }); Step(30);
            ButtonBehavior(button); RadioBehavior(first, second, third); CheckboxBehavior(check); ThemeBehavior(button, second, check);
        } finally {
            try { if (window != null) window.Close(); }
            finally {
                if (button != null) foreach (string name in new[] { "Color", "Scale", "Background" }) PCL.ModAnimation.AniStop("MyButton " + name + " " + button.Uuid);
                foreach (PCL.MyRadioButton radio in new[] { first, second, third }) if (radio != null) foreach (string name in new[] { "Checked", "Color" }) PCL.ModAnimation.AniStop("MyRadioButton " + name + " " + radio.Uuid);
                if (check != null) foreach (string name in new[] { "Scale", "BorderColor", "AllowMouseDown", "TextColor", "Background" }) PCL.ModAnimation.AniStop("MyCheckBox " + name + " " + check.Uuid);
                PCL.ModAnimation.AniControlEnabled = enabled; if (LastTick != null && last != null) LastTick.SetValue(null, last);
                app.MainWindow = previousMain; app.ShutdownMode = shutdown;
            }
        }
    }
}
'@
try{
 $app.InitializeComponent()
 # InitializeComponent wires Startup/exception handlers; remove them afterward, before any dispatcher pump.
 [Windows.Application].GetField('_startupUri',[Reflection.BindingFlags]'NonPublic,Instance').SetValue($app,$null)
 $app.remove_Startup([Delegate]::CreateDelegate([Windows.StartupEventHandler],$app,$asm.GetType('PCL.Application').GetMethod('Application_Startup',$flags)))
 $app.remove_DispatcherUnhandledException([Delegate]::CreateDelegate([Windows.Threading.DispatcherUnhandledExceptionEventHandler],$app,$asm.GetType('PCL.Application').GetMethod('Application_DispatcherUnhandledException',$flags)))
 $resources=$asm.GetType('PCL.My.Resources.Resources',$true).GetProperty('ResourceManager',$flags).GetValue($null,$null)
 $presentationBytes=$resources.GetObject('PCLLF.Presentation')
 if($presentationBytes -isnot [byte[]] -or $presentationBytes.Length -eq 0 -or (Hash-Bytes $presentationBytes) -cne $ExpectedPresentationHash){throw 'The EXE embedded PCLLF.Presentation bytes differ from the actual LF presentation build'}
 $buttonType=$asm.GetType('PCL.MyButton',$true);$presentation=$buttonType.BaseType.Assembly
 if($presentation.GetName().Name -cne 'PCLLF.Presentation' -or $presentation.Location -ne '' -or $buttonType.BaseType.FullName -cne 'PCLLF.Presentation.Controls.MyButtonView'){throw 'The standalone EXE did not resolve its real C# presentation assembly from embedded bytes'}
 $metadata=[Reflection.Assembly]::ReflectionOnlyLoad($presentationBytes)
 if($metadata.FullName -cne $presentation.FullName -or $metadata.ManifestModule.ModuleVersionId -ne $presentation.ManifestModule.ModuleVersionId){throw 'The loaded presentation identity/module does not match the embedded assembly bytes'}
 Write-Output ('PASS standalone EXE: original embedded PCLLF.Presentation key, SHA256 '+$ExpectedPresentationHash+', matching runtime identity/module and no sidecar.')
 $settings=$asm.GetType('PCL.Settings',$true).GetField('Entries').GetValue($null)
 foreach($entry in $settings.Values){$entry.ValueCache['']=$entry.DefaultValue};$settings['Identify'].ValueCache['']='migrated-controls-fixture'
 $local=$asm.GetType('PCL.LocalGameData',$true);$testRoot=$local.GetProperty('TestRoot',$flags);$previousRoot=$testRoot.GetValue($null,$null)
 $testRoot.SetValue($null,(Join-Path $fixture 'profile'),$null)
 $asm.GetType('PCL.ModSecret',$true).GetMethod('ThemeRefresh',$flags).Invoke($null,@(0))|Out-Null
 # Supply DLL file references so Windows PowerShell Add-Type does not parse the copied EXE path as an assembly name.
 # These exact bytes are compiler inputs under a non-probing subdirectory; never LoadFrom either file.
 $compiler=Join-Path $fixture 'compiler';[IO.Directory]::CreateDirectory($compiler)|Out-Null
 $reference=Join-Path $compiler 'PCLLF.Presentation.dll';[IO.File]::WriteAllBytes($reference,$presentationBytes)
 $mainReference=Join-Path $compiler ($asm.GetName().Name+'.dll');$mainBytes=[IO.File]::ReadAllBytes($asm.Location)
 [IO.File]::WriteAllBytes($mainReference,$mainBytes)
 $mainMetadata=[Reflection.Assembly]::ReflectionOnlyLoad([IO.File]::ReadAllBytes($mainReference))
 if($mainMetadata.FullName -cne $asm.FullName -or $mainMetadata.ManifestModule.ModuleVersionId -ne $asm.ManifestModule.ModuleVersionId){throw 'The compiler main-assembly reference differs from the loaded standalone EXE'}
 Add-Type -TypeDefinition $checks -ReferencedAssemblies @('System.dll','System.Core.dll','System.Xaml.dll',
  [Windows.FrameworkElement].Assembly.Location,[Windows.UIElement].Assembly.Location,[Windows.Point].Assembly.Location,
  $mainReference,$reference)
 $boundMain=[PclMigratedControlChecks].GetField('LastTick',[Reflection.BindingFlags]'NonPublic,Static').GetValue($null).DeclaringType.Assembly
 if(-not [object]::ReferenceEquals($boundMain,$asm) -or $boundMain.Location -ine $executable){throw 'The compiled verifier rebound its main types away from the original standalone EXE'}
 try{[PclMigratedControlChecks]::Run()}catch{throw ($_.Exception.GetBaseException().ToString())}
 Write-Output 'Migrated native controls passed six targeted groups in a fresh standalone-EXE process using the real LF Application/theme/AniTimer and original setting/preview contracts. Pointer hover, focus navigation and visual feel still require manual interaction; no private WPF mouse cache was changed.'
}finally{
 try{if($testRoot){$testRoot.SetValue($null,$previousRoot,$null)}}finally{$app.Shutdown()}
}
