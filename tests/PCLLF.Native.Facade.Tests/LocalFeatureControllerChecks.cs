// Exact existing LF controller fixture, with original Require line IDs and explicit nullable defaults.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using PCLCS;

namespace PCLLF.Native.Facade.Tests;

internal static class LocalFeatureControllerChecks {
    private const string Original = "lang:zh_cn\r\nkey_key.forward:key.keyboard.w\r\nkey_key.other:key.keyboard.c\r\n";
    private static void Require(int line, bool value, string message) => LocalFeatureChecks.Check("controller", line, value, message);
    private sealed class Fixture {
        internal readonly object First = new object(), Second = new object(), Gate = new object();
        internal readonly string RootA, RootB;
        internal readonly KeybindingPageView<InstanceKeybindingData> View;
        internal readonly KeybindingPageController<object, InstanceKeybindingData> Controller;
        internal readonly InstanceKeybindingData Entry = new InstanceKeybindingData {
            Key = "key_key.forward", Name = "Forward", Owners = "Minecraft", Value = "key.keyboard.w", Normalized = "key.keyboard.w", SharedCount = 1
        };
        internal readonly IOException ProfileError = new IOException("synthetic profile capture failure");
        internal object Current, ProfileOwner = null!;
        internal string SavedFolder = null!, LoadedFolder = null!;
        internal int SaveCalls, Invalidations, ProfileCalls, Loads, Messages, Successes, Errors, Warnings, CaptureCount;
        internal Exception? LastError, LastWarning;
        internal bool FailProfile;
        internal Func<int>? Confirm;
        internal Fixture(string root) {
            RootA = Path.Combine(root, "first"); RootB = Path.Combine(root, "second"); Current = First;
            Directory.CreateDirectory(RootA); Directory.CreateDirectory(RootB);
            File.WriteAllText(Path.Combine(RootA, "options.txt"), Original, new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(RootB, "options.txt"), Original, new UTF8Encoding(false));
            Grid page = new Grid(), card = new Grid(); StackPanel rows = new StackPanel();
            Button cancel = new Button(), clear = new Button();
            card.Children.Add(rows); page.Children.Add(card); page.Children.Add(cancel); page.Children.Add(clear);
            View = new KeybindingPageView<InstanceKeybindingData> {
                Root = page, CardBindings = card, Bindings = rows, CancelButton = cancel, ClearButton = clear,
                Empty = new TextBlock(), Capture = new TextBlock(), Search = delegate { return ""; },
                SetAllText = delegate(string text) { }, SetConflictsText = delegate(string text) { }, ScrollHome = delegate { },
                CreateButton = CreateButton, CreateGroup = CreateGroup,
                RefreshCaptureBar = delegate(int count, int previous, bool animate) { CaptureCount = count; return count; }
            };
            KeybindingPageHost<object> host = new KeybindingPageHost<object> {
                CurrentInstance = delegate { return Current; }, GameFolder = Folder, VersionPath = Folder,
                AllowsInstance = delegate(object instance) { return true; }, IsLocked = delegate(object instance) { return false; },
                VersionValid = delegate(object instance) { return true; }, VersionMajor = delegate(object instance) { return 20; },
                HasRunningGame = delegate { return false; }, IsCurrentPage = delegate { return true; },
                AnimationEnabled = delegate { return false; }, LoaderFailed = delegate { return false; },
                StartLoader = delegate(string folder) { Loads++; LoadedFolder = folder; }, CancelLoader = delegate { },
                BindExternalEvents = delegate(bool attach, KeyEventHandler key, MouseButtonEventHandler mouse) { },
                BindingForVersion = PersonalOptionsRules.BindingForVersion,
                Save = Save,
                CaptureEditedBinding = delegate(object instance, string key, string encoded) {
                    ProfileCalls++; ProfileOwner = instance; if (FailProfile) throw ProfileError;
                },
                Message = delegate(string text, string title, string first, string second, bool warning) {
                    Messages++;
                    Require(60, Controller!.Captured == null && CaptureCount == 0, "Capture was still active when the modal confirmation opened.");
                    return Confirm == null ? 1 : Confirm();
                },
                Hint = delegate(string text, bool success) { if (success) Successes++; },
                Warn = delegate(Exception error, string message) { Warnings++; LastWarning = error; },
                Error = delegate(Exception error, string message) { Errors++; LastError = error; }
            };
            Controller = new KeybindingPageController<object, InstanceKeybindingData>(View, host);
            cancel.Click += delegate { Controller.CancelCapture(); };
            clear.Click += delegate { Controller.ClearBinding(); };
            Controller.RefreshBindings();
            Controller.BindingsLoaded(new List<InstanceKeybindingData> {
                Entry, new InstanceKeybindingData { Key = "key_key.other", Name = "Other", Owners = "Minecraft", Value = "key.keyboard.c", Normalized = "key.keyboard.c", SharedCount = 1 }
            });
        }
        private string Folder(object owner) {
            if (object.ReferenceEquals(owner, First)) return RootA;
            if (object.ReferenceEquals(owner, Second)) return RootB;
            throw new InvalidOperationException("Unknown synthetic owner.");
        }
        private void Save(string folder, string key, string? previous, string replacement) {
            Require(81, folder == RootA || folder == RootB, "Controller attempted to save outside its two synthetic roots.");
            SaveCalls++; SavedFolder = folder;
            InstanceKeybindingFiles.Save(folder, key, previous, replacement, Gate, delegate { return false; },
                delegate(string root, string path) {
                    Require(85, Path.GetFullPath(path) == Path.Combine(Path.GetFullPath(root), "options.txt"), "Unexpected options path.");
                }, delegate(string path) { Invalidations++; });
        }
        private static KeybindingPageButton CreateButton(InstanceKeybindingData entry, string text, bool conflict, string tooltip, Action clicked) {
            Button button = new Button { Tag = entry, Content = text, ToolTip = tooltip };
            button.Click += delegate { clicked(); };
            return new KeybindingPageButton(button, delegate(string value) { button.Content = value; },
                delegate(KeybindingPageButtonState state) { button.Background = state == KeybindingPageButtonState.Capturing ? Brushes.Gold : Brushes.Transparent; });
        }
        private static KeybindingPageGroup CreateGroup(string title, string? normalized) {
            GroupBox group = new GroupBox { Header = title, Tag = normalized };
            return new KeybindingPageGroup(group, delegate(Panel rows) { group.Content = rows; });
        }
    }
    private static void Saved(Fixture fixture, string folder, string value, int loads) {
        string file = Path.Combine(folder, "options.txt");
        Require(101, File.ReadAllText(file) == Original.Replace("key_key.forward:key.keyboard.w", "key_key.forward:" + value), "The real options transaction did not write the expected binding.");
        Require(102, File.ReadAllText(file + ".pcl-backup") == Original, "The original options backup was not preserved.");
        Require(103, fixture.SaveCalls == 1 && fixture.Invalidations == 1 && fixture.ProfileCalls == 1 && fixture.Successes == 1 && fixture.Errors == 0,
            "Successful save did not complete cache/profile/success callbacks exactly once.");
        Require(105, fixture.Controller.Captured == null && fixture.CaptureCount == 0 && fixture.Loads == loads && fixture.LoadedFolder == folder,
            "Successful save left capture active or failed to refresh the active root.");
    }
    private static bool PreviewMouse(Fixture fixture, UIElement source) {
        bool called = false, handled = false;
        MouseButtonEventHandler preview = delegate(object sender, MouseButtonEventArgs args) {
            called = true;
            Require(112, args.OriginalSource is DependencyObject, "The routed mouse event has no real original source.");
            fixture.Controller.HandleCapturedMouse(sender, args); handled = args.Handled;
        };
        fixture.View.Root.AddHandler(UIElement.PreviewMouseDownEvent, preview);
        try {
            MouseButtonEventArgs args = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left);
            args.RoutedEvent = UIElement.PreviewMouseDownEvent; source.RaiseEvent(args);
        } finally { fixture.View.Root.RemoveHandler(UIElement.PreviewMouseDownEvent, preview); }
        Require(120, called, "The preview mouse event did not reach the real controller.");
        return handled;
    }
    internal static void Run(string root) {
        Fixture escape = new Fixture(Path.Combine(root, "escape"));
        escape.Controller.BeginCapture(escape.Entry);
        Require(126, object.ReferenceEquals(escape.Controller.Captured, escape.Entry) && escape.CaptureCount == 1, "BeginCapture did not update capture state.");
        // KeyEventArgs requires a real PresentationSource, even for an offscreen synthetic key.
        using (HwndSource inputSource = new HwndSource(new HwndSourceParameters("PclKeybindingCapture-" + Guid.NewGuid().ToString("N")) {
            WindowStyle = 0, Width = 1, Height = 1, PositionX = -20000, PositionY = -20000
        })) {
            KeyEventArgs key = new KeyEventArgs(Keyboard.PrimaryDevice, inputSource, Environment.TickCount, Key.Escape);
            key.RoutedEvent = Keyboard.PreviewKeyDownEvent;
            escape.Controller.CaptureKey(escape.View.Root, key);
            Require(134, key.Handled && escape.Controller.Captured == null && escape.CaptureCount == 0 && escape.SaveCalls == 0 && escape.Successes == 0 && escape.Errors == 0,
                "Escape did not cancel capture without saving.");
        }
        Require(137, File.ReadAllText(Path.Combine(escape.RootA, "options.txt")) == Original, "Escape changed options.");

        Fixture cancel = new Fixture(Path.Combine(root, "cancel"));
        cancel.Controller.BeginCapture(cancel.Entry); cancel.View.CancelButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(141, cancel.Controller.Captured == null && cancel.CaptureCount == 0 && cancel.SaveCalls == 0, "The standard cancel click did not cancel capture.");

        Fixture clear = new Fixture(Path.Combine(root, "clear"));
        clear.Controller.BeginCapture(clear.Entry); clear.View.ClearButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Saved(clear, clear.RootA, "key.keyboard.unknown", 2);
        Require(146, clear.Messages == 0 && clear.Warnings == 0, "Clearing a binding unexpectedly opened a conflict dialog or warned.");

        Fixture mouse = new Fixture(Path.Combine(root, "mouse"));
        mouse.Controller.BeginCapture(mouse.Entry);
        Require(150, PreviewMouse(mouse, mouse.View.Root), "A regular captured mouse press was not handled.");
        Saved(mouse, mouse.RootA, "key.mouse.left", 2);

        Fixture reject = new Fixture(Path.Combine(root, "reject"));
        reject.Confirm = delegate { return 0; };
        reject.Controller.BeginCapture(reject.Entry); reject.Controller.SaveBinding("key.keyboard.c");
        Require(156, reject.Messages == 1 && reject.Controller.Captured == null && reject.CaptureCount == 0 && reject.SaveCalls == 0 && reject.ProfileCalls == 0 &&
            reject.Successes == 0 && reject.Errors == 0 && reject.Loads == 1, "Rejecting a conflict saved, refreshed or left capture active.");
        Require(158, File.ReadAllText(Path.Combine(reject.RootA, "options.txt")) == Original && !File.Exists(Path.Combine(reject.RootA, "options.txt.pcl-backup")),
            "Rejecting a conflict touched the options transaction.");

        Fixture change = new Fixture(Path.Combine(root, "owner-change"));
        change.Confirm = delegate {
            change.Current = change.Second; change.Controller.RefreshBindings();
            Require(164, object.ReferenceEquals(change.Controller.Owner, change.Second) && change.Controller.GameFolder == change.RootB,
                "The modal callback did not change the real owner/folder state.");
            return 1;
        };
        change.Controller.BeginCapture(change.Entry); change.Controller.SaveBinding("key.keyboard.c");
        Saved(change, change.RootB, "key.keyboard.c", 3);
        Require(170, change.Messages == 1 && change.SavedFolder == change.RootB && object.ReferenceEquals(change.ProfileOwner, change.Second) && change.Warnings == 0,
            "Saving after a modal callback reused the old owner or root.");
        Require(172, File.ReadAllText(Path.Combine(change.RootA, "options.txt")) == Original && !File.Exists(Path.Combine(change.RootA, "options.txt.pcl-backup")),
            "The previous owner's options were changed after the modal callback.");

        Fixture profile = new Fixture(Path.Combine(root, "profile-failure"));
        profile.FailProfile = true; profile.Controller.BeginCapture(profile.Entry); profile.Controller.SaveBinding("key.keyboard.v");
        Saved(profile, profile.RootA, "key.keyboard.v", 2);
        Require(178, profile.Warnings == 1 && object.ReferenceEquals(profile.LastWarning, profile.ProfileError), "A profile capture failure did not warn while retaining save success and refresh.");

        Fixture failure = new Fixture(Path.Combine(root, "save-failure"));
        string changed = Original.Replace("key_key.forward:key.keyboard.w", "key_key.forward:key.keyboard.z");
        File.WriteAllText(Path.Combine(failure.RootA, "options.txt"), changed, new UTF8Encoding(false));
        failure.Controller.BeginCapture(failure.Entry); failure.Controller.SaveBinding("key.keyboard.v");
        Require(184, failure.SaveCalls == 1 && failure.Errors == 1 && failure.LastError is IOException && failure.Invalidations == 0 && failure.ProfileCalls == 0 &&
            failure.Successes == 0 && failure.Warnings == 0 && failure.Loads == 1 && failure.Controller.Captured == null && failure.CaptureCount == 0,
            "A real stale-file save failure was not reported, falsely succeeded or refreshed.");
        Require(187, File.ReadAllText(Path.Combine(failure.RootA, "options.txt")) == changed && !File.Exists(Path.Combine(failure.RootA, "options.txt.pcl-backup")),
            "A failed save changed the current options or created a backup.");
    }
}