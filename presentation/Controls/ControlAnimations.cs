using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace PCLLF.Presentation.Controls;

public enum ControlEase { Linear, OutFluent, InFluent, OutBack, OutFluentWithInitial, InBack, OutElastic, InoutFluent }
public enum ControlEasePower { Weak, Middle, Strong, ExtraStrong }

public readonly struct ControlAnimationSpec {
    public readonly int Duration, Delay;
    public readonly ControlEase Ease;
    public readonly ControlEasePower Power;
    public readonly bool After;
    public readonly double InitialSpeed, TotalSeconds, TotalDistance;
    public readonly int? RawPower;
    public readonly double Middle;
    public ControlAnimationSpec(int duration, int delay = 0, ControlEase ease = ControlEase.Linear,
        ControlEasePower power = ControlEasePower.Middle, bool after = false) {
        Duration = duration; Delay = delay; Ease = ease; Power = power; After = after;
        InitialSpeed = 0; TotalSeconds = 0; TotalDistance = 0; RawPower = null; Middle = .5;
    }
    private ControlAnimationSpec(int duration, double initialSpeed, double totalSeconds, double totalDistance, bool after)
        : this(duration, 0, ControlEase.OutFluentWithInitial, ControlEasePower.Middle, after) {
        InitialSpeed = initialSpeed; TotalSeconds = totalSeconds; TotalDistance = totalDistance;
    }
    public static ControlAnimationSpec FluentWithInitial(int duration, double initialSpeed, double totalSeconds, double totalDistance, bool after = true) =>
        new(duration, initialSpeed, totalSeconds, totalDistance, after);
    private ControlAnimationSpec(int duration, int delay, ControlEase ease, int rawPower, bool after)
        : this(duration, delay, ease, ControlEasePower.Middle, after) { RawPower = rawPower; }
    private ControlAnimationSpec(int duration, ControlEasePower power, double middle)
        : this(duration, 0, ControlEase.InoutFluent, power) { Middle = middle; }
    public static ControlAnimationSpec WithRawPower(int duration, int rawPower, int delay = 0, ControlEase ease = ControlEase.OutFluent, bool after = false) =>
        new(duration, delay, ease, rawPower, after);
    public static ControlAnimationSpec InoutFluent(int duration, ControlEasePower power, double middle = .5) => new(duration, power, middle);
    // Original VB animation parameters round floating durations and delays to even.
    public ControlAnimationSpec(double duration, double delay = 0, ControlEase ease = ControlEase.Linear,
        ControlEasePower power = ControlEasePower.Middle, bool after = false)
        : this(Convert.ToInt32(Math.Round(duration)), Convert.ToInt32(Math.Round(delay)), ease, power, after) { }
}

public abstract class ControlAnimation { internal ControlAnimation() { } }

/// <summary>Real animation frames are wrapped without dynamic calls or a main-assembly reference.</summary>
public abstract class ControlAnimations {
    public abstract ControlAnimation ColorResource(FrameworkElement target, DependencyProperty property, string key, ControlAnimationSpec spec);
    public abstract ControlAnimation ColorTo(FrameworkElement target, DependencyProperty property, ControlColor value, ControlAnimationSpec spec);
    public abstract ControlAnimation Scale(FrameworkElement target, double value, ControlAnimationSpec spec, bool absolute = false);
    public abstract ControlAnimation ScaleTransform(FrameworkElement target, double value, ControlAnimationSpec spec);
    public abstract double Speed { get; }
    public abstract ControlAnimation Width(FrameworkElement target, double value, ControlAnimationSpec spec);
    public abstract ControlAnimation TranslateX(FrameworkElement target, double value, ControlAnimationSpec spec);
    public abstract ControlAnimation TranslateY(FrameworkElement target, double value, ControlAnimationSpec spec);
    public abstract ControlAnimation X(FrameworkElement target, double value, ControlAnimationSpec spec);
    public abstract ControlAnimation Y(FrameworkElement target, double value, ControlAnimationSpec spec);
    public abstract ControlAnimation Height(FrameworkElement target, double value, ControlAnimationSpec spec);
    public abstract ControlAnimation Opacity(FrameworkElement target, double value, ControlAnimationSpec spec);
    public abstract ControlAnimation RotateTransform(FrameworkElement target, double value, ControlAnimationSpec spec);
    public abstract ControlAnimation Double(Action<double> work, double value, ControlAnimationSpec spec);
    public abstract ControlAnimation Code(Action work, int delay = 0, bool after = false);
    public abstract void Start(IEnumerable<ControlAnimation> animations, string name, bool refreshTime = false);
    public void Start(ControlAnimation animation, string name = "", bool refreshTime = false) => Start(new[] { animation }, name, refreshTime);
    public abstract void Stop(string name);
}

public sealed class ControlAnimationHost<TAnimation> : ControlAnimations where TAnimation : struct {
    private sealed class Frame : ControlAnimation {
        internal readonly TAnimation Value;
        internal Frame(TAnimation value) { Value = value; }
    }

    public Func<FrameworkElement, DependencyProperty, string, ControlAnimationSpec, TAnimation> CreateColorResource = null!;
    public Func<FrameworkElement, DependencyProperty, ControlColor, ControlAnimationSpec, TAnimation> CreateColorTo = null!;
    public Func<FrameworkElement, double, ControlAnimationSpec, bool, TAnimation> CreateScale = null!;
    public Func<FrameworkElement, double, ControlAnimationSpec, TAnimation> CreateScaleTransform = null!;
    public Func<double> ReadSpeed = null!;
    public Func<FrameworkElement, double, ControlAnimationSpec, TAnimation> CreateWidth = null!;
    public Func<FrameworkElement, double, ControlAnimationSpec, TAnimation> CreateTranslateX = null!;
    public Func<FrameworkElement, double, ControlAnimationSpec, TAnimation> CreateTranslateY = null!;
    public Func<FrameworkElement, double, ControlAnimationSpec, TAnimation> CreateX = null!;
    public Func<FrameworkElement, double, ControlAnimationSpec, TAnimation> CreateY = null!;
    public Func<FrameworkElement, double, ControlAnimationSpec, TAnimation> CreateHeight = null!;
    public Func<FrameworkElement, double, ControlAnimationSpec, TAnimation> CreateOpacity = null!;
    public Func<FrameworkElement, double, ControlAnimationSpec, TAnimation> CreateRotateTransform = null!;
    public Func<Action<double>, double, ControlAnimationSpec, TAnimation> CreateDouble = null!;
    public Func<Action, int, bool, TAnimation> CreateCode = null!;
    public Action<TAnimation[], string, bool> StartFrames = null!;
    public Action<string> StopFrames = null!;

    public override ControlAnimation ColorResource(FrameworkElement target, DependencyProperty property, string key, ControlAnimationSpec spec) => new Frame(CreateColorResource(target, property, key, spec));
    public override ControlAnimation ColorTo(FrameworkElement target, DependencyProperty property, ControlColor value, ControlAnimationSpec spec) => new Frame(CreateColorTo(target, property, value, spec));
    public override ControlAnimation Scale(FrameworkElement target, double value, ControlAnimationSpec spec, bool absolute = false) => new Frame(CreateScale(target, value, spec, absolute));
    public override ControlAnimation ScaleTransform(FrameworkElement target, double value, ControlAnimationSpec spec) => new Frame(CreateScaleTransform(target, value, spec));
    public override double Speed => ReadSpeed();
    public override ControlAnimation Width(FrameworkElement target, double value, ControlAnimationSpec spec) => new Frame(CreateWidth(target, value, spec));
    public override ControlAnimation TranslateX(FrameworkElement target, double value, ControlAnimationSpec spec) => new Frame(CreateTranslateX(target, value, spec));
    public override ControlAnimation TranslateY(FrameworkElement target, double value, ControlAnimationSpec spec) => new Frame(CreateTranslateY(target, value, spec));
    public override ControlAnimation X(FrameworkElement target, double value, ControlAnimationSpec spec) => new Frame(CreateX(target, value, spec));
    public override ControlAnimation Y(FrameworkElement target, double value, ControlAnimationSpec spec) => new Frame(CreateY(target, value, spec));
    public override ControlAnimation Height(FrameworkElement target, double value, ControlAnimationSpec spec) => new Frame(CreateHeight(target, value, spec));
    public override ControlAnimation Opacity(FrameworkElement target, double value, ControlAnimationSpec spec) => new Frame(CreateOpacity(target, value, spec));
    public override ControlAnimation RotateTransform(FrameworkElement target, double value, ControlAnimationSpec spec) => new Frame(CreateRotateTransform(target, value, spec));
    public override ControlAnimation Double(Action<double> work, double value, ControlAnimationSpec spec) => new Frame(CreateDouble(work, value, spec));
    public override ControlAnimation Code(Action work, int delay = 0, bool after = false) => new Frame(CreateCode(work, delay, after));
    public override void Start(IEnumerable<ControlAnimation> animations, string name, bool refreshTime = false) =>
        StartFrames(animations.Select(animation => ((Frame)animation).Value).ToArray(), name, refreshTime);
    public override void Stop(string name) => StopFrames(name);
}
