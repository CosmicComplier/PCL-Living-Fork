using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace PCL;

// This background belongs only to the launcher identity card. Native clocks
// animate brush transforms; no layout, file work or update state is animated.
internal sealed class LauncherUpdateCardBackground : Grid {
    private readonly Rectangle first = new(), second = new();
    private readonly TranslateTransform firstFlow = new(), secondFlow = new();
    private readonly List<AnimationClock> clocks = new();
    private readonly DispatcherTimer settingsWatcher;
    private bool moving;
    private double speed = -1;

    internal LauncherUpdateCardBackground() {
        IsHitTestVisible = false;
        Children.Add(first); Children.Add(second);
        Animate(firstFlow, TranslateTransform.XProperty, -0.12, 0.12, 24);
        Animate(firstFlow, TranslateTransform.YProperty, -0.08, 0.08, 32);
        Animate(secondFlow, TranslateTransform.XProperty, 0.1, -0.1, 32);
        Animate(secondFlow, TranslateTransform.YProperty, 0.08, -0.08, 24);
        settingsWatcher = new DispatcherTimer(DispatcherPriority.Background, Dispatcher) { Interval = TimeSpan.FromMilliseconds(300) };
        settingsWatcher.Tick += (_, _) => RefreshMotion();
        SizeChanged += (_, args) => Clip = new RectangleGeometry(new Rect(new Point(), args.NewSize), 5, 5);
        ModTheme.WatchTheme(this, RefreshPalette);
        Loaded += (_, _) => RefreshActivity();
        Unloaded += (_, _) => RefreshActivity();
        IsVisibleChanged += (_, _) => RefreshActivity();
        RefreshPalette();
    }
    private void Animate(Animatable target, DependencyProperty property, double from, double to, double seconds) {
        var animation = new DoubleAnimationUsingKeyFrames {
            Duration = TimeSpan.FromSeconds(seconds), AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever
        };
        animation.KeyFrames.Add(new LinearDoubleKeyFrame(from, KeyTime.FromPercent(0)));
        animation.KeyFrames.Add(new SplineDoubleKeyFrame(to, KeyTime.FromPercent(1), new KeySpline(0.77, 0, 0.175, 1)));
        var clock = (AnimationClock)animation.CreateClock(true);
        clock.Controller.Pause(); target.ApplyAnimationClock(property, clock); clocks.Add(clock);
    }
    private void RefreshActivity() {
        if (IsLoaded && IsVisible) { settingsWatcher.Start(); RefreshMotion(); }
        else { settingsWatcher.Stop(); PauseMotion(); }
    }
    private void RefreshMotion() {
        double currentSpeed = ModAnimation.AniSpeed;
        bool allowed = IsLoaded && IsVisible && ModAnimation.AniControlEnabled == 0 && currentSpeed < 200 && SystemParameters.ClientAreaAnimation;
        if (!allowed) { PauseMotion(); return; }
        if (speed != currentSpeed) {
            speed = currentSpeed;
            foreach (var clock in clocks) clock.Controller.SpeedRatio = currentSpeed;
        }
        if (moving) return;
        foreach (var clock in clocks) clock.Controller.Resume();
        moving = true;
    }
    private void PauseMotion() {
        foreach (var clock in clocks) clock.Controller.Pause();
        moving = false;
    }
    private void RefreshPalette() {
        bool dark = ModTheme.IsDarkTheme;
        Background = new LinearGradientBrush(dark ? Color.FromRgb(20, 32, 49) : Color.FromRgb(247, 252, 255),
            dark ? Color.FromRgb(28, 48, 74) : Color.FromRgb(226, 240, 255), 20);
        first.Fill = Glow(dark ? Color.FromArgb(100, 47, 94, 153) : Color.FromArgb(120, 142, 194, 250), new Point(0.18, 0.25), firstFlow);
        second.Fill = Glow(dark ? Color.FromArgb(80, 40, 77, 124) : Color.FromArgb(180, 255, 255, 255), new Point(0.78, 0.75), secondFlow);
    }
    private static Brush Glow(Color color, Point center, Transform flow) {
        var brush = new RadialGradientBrush { Center = center, GradientOrigin = center, RadiusX = 0.7, RadiusY = 1.0, RelativeTransform = flow };
        brush.GradientStops.Add(new GradientStop(color, 0));
        brush.GradientStops.Add(new GradientStop(Color.FromArgb(0, color.R, color.G, color.B), 1));
        return brush;
    }
}
