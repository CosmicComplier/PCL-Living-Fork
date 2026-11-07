using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace PCL;

// Sparse decoration only; no input, work progress or layout animation.
internal sealed class ThemeStarfield : Canvas {
    private readonly List<AnimationClock> clocks = new();
    private readonly DispatcherTimer watcher;
    private bool moving;
    internal ThemeStarfield(int count) {
        IsHitTestVisible = false; ClipToBounds = true;
        for (int i = 0; i < count; i++) {
            double size = i % 4 == 0 ? 4 : i % 3 == 0 ? 2.2 : 1.4;
            Shape star = i % 4 == 0 ? new System.Windows.Shapes.Path { Data = Geometry.Parse("M5,0 L6.1,3.9 10,5 6.1,6.1 5,10 3.9,6.1 0,5 3.9,3.9 Z"), Stretch = Stretch.Fill } : new Ellipse();
            star.Width = star.Height = size;
            star.Fill = new SolidColorBrush(i % 3 == 0 ? Color.FromRgb(190, 210, 255) : Color.FromRgb(215, 246, 255));
            star.Opacity = .18;
            var animation = new DoubleAnimation(.16, .66, TimeSpan.FromSeconds(3.5 + i % 5)) {
                BeginTime = TimeSpan.FromSeconds(i * .23), AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            var clock = (AnimationClock)animation.CreateClock(true); clock.Controller.Pause(); star.ApplyAnimationClock(OpacityProperty, clock); clocks.Add(clock);
            Children.Add(star);
        }
        watcher = new DispatcherTimer(DispatcherPriority.Background, Dispatcher) { Interval = TimeSpan.FromMilliseconds(300) };
        watcher.Tick += (_, _) => RefreshActivity();
        SizeChanged += (_, _) => { for (int i = 0; i < Children.Count; i++) {
            SetLeft(Children[i], Math.Max(0, ActualWidth - 6) * ((.09 + i * .6180339887) % 1));
            SetTop(Children[i], Math.Max(0, ActualHeight - 6) * ((.14 + i * .3819660113) % 1));
        } };
        Loaded += (_, _) => RefreshActivity(); Unloaded += (_, _) => RefreshActivity(); IsVisibleChanged += (_, _) => RefreshActivity();
        ModTheme.WatchTheme(this, () => { Visibility = ModTheme.IsDarkTheme ? Visibility.Visible : Visibility.Collapsed; RefreshActivity(); });
        Visibility = ModTheme.IsDarkTheme ? Visibility.Visible : Visibility.Collapsed;
    }
    private void RefreshActivity() {
        bool visible = IsLoaded && IsVisible && ModTheme.IsDarkTheme;
        if (visible) watcher.Start(); else watcher.Stop();
        double speed = ModAnimation.AniSpeed;
        bool active = visible && ModAnimation.AniControlEnabled == 0 && speed > 0 && speed < 200 && SystemParameters.ClientAreaAnimation;
        foreach (var clock in clocks) {
            if (active) { clock.Controller.SpeedRatio = speed; if (!moving) clock.Controller.Resume(); }
            else clock.Controller.Pause();
        }
        moving = active;
    }
}
