using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace PCLLF.Presentation.Controls;

public enum ControlEase { Linear, OutFluent, InFluent, OutBack }
public enum ControlEasePower { Weak, Middle, Strong, ExtraStrong }

public readonly struct ControlAnimationSpec {
    public readonly int Duration, Delay;
    public readonly ControlEase Ease;
    public readonly ControlEasePower Power;
    public readonly bool After;
    public ControlAnimationSpec(int duration, int delay = 0, ControlEase ease = ControlEase.Linear,
        ControlEasePower power = ControlEasePower.Middle, bool after = false) {
        Duration = duration; Delay = delay; Ease = ease; Power = power; After = after;
    }
}

public abstract class ControlAnimation { internal ControlAnimation() { } }

/// <summary>Real animation frames are wrapped without dynamic calls or a main-assembly reference.</summary>
public abstract class ControlAnimations {
    public abstract ControlAnimation ColorResource(FrameworkElement target, DependencyProperty property, string key, ControlAnimationSpec spec);
    public abstract ControlAnimation ColorTo(FrameworkElement target, DependencyProperty property, ControlColor value, ControlAnimationSpec spec);
    public abstract ControlAnimation Scale(FrameworkElement target, double value, ControlAnimationSpec spec, bool absolute = false);
    public abstract ControlAnimation ScaleTransform(FrameworkElement target, double value, ControlAnimationSpec spec);
    public abstract ControlAnimation Code(Action work, int delay = 0, bool after = false);
    public abstract void Start(IEnumerable<ControlAnimation> animations, string name, bool refreshTime = false);
    public void Start(ControlAnimation animation, string name, bool refreshTime = false) => Start(new[] { animation }, name, refreshTime);
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
    public Func<Action, int, bool, TAnimation> CreateCode = null!;
    public Action<TAnimation[], string, bool> StartFrames = null!;
    public Action<string> StopFrames = null!;

    public override ControlAnimation ColorResource(FrameworkElement target, DependencyProperty property, string key, ControlAnimationSpec spec) => new Frame(CreateColorResource(target, property, key, spec));
    public override ControlAnimation ColorTo(FrameworkElement target, DependencyProperty property, ControlColor value, ControlAnimationSpec spec) => new Frame(CreateColorTo(target, property, value, spec));
    public override ControlAnimation Scale(FrameworkElement target, double value, ControlAnimationSpec spec, bool absolute = false) => new Frame(CreateScale(target, value, spec, absolute));
    public override ControlAnimation ScaleTransform(FrameworkElement target, double value, ControlAnimationSpec spec) => new Frame(CreateScaleTransform(target, value, spec));
    public override ControlAnimation Code(Action work, int delay = 0, bool after = false) => new Frame(CreateCode(work, delay, after));
    public override void Start(IEnumerable<ControlAnimation> animations, string name, bool refreshTime = false) =>
        StartFrames(animations.Select(animation => ((Frame)animation).Value).ToArray(), name, refreshTime);
    public override void Stop(string name) => StopFrames(name);
}
