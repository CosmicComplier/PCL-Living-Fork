using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using PCLLF.Presentation.Controls;

namespace PCLLF.Presentation.Animation;

/// <summary>Native fields and real control frames use the same complete LF engine as the legacy facade.</summary>
public sealed class AnimationNativeRuntime {
    public Dictionary<string, AnimationGroup> Groups = new();
    public long LastTick;
    public bool Running;
    public int Fps;
    public AnimationHost Host { get; }
    public AnimationEngine<AnimationData, AnimationGroup> Engine { get; }
    public AnimationFactories<AnimationData> Factories { get; }

    /// <param name="host">A dedicated host; its state callbacks bind these actual native fields.</param>
    public AnimationNativeRuntime(AnimationHost host) {
        Host = host;
        host.ReadLastTick = () => LastTick; host.WriteLastTick = value => LastTick = value;
        host.ReadRunning = () => Running; host.WriteRunning = value => Running = value;
        host.ReadFps = () => Fps; host.WriteFps = value => Fps = value;
        Engine = new AnimationEngine<AnimationData, AnimationGroup>(host, () => Groups,
            (frames, tick) => new AnimationGroup { Data = frames, StartTick = tick, Uuid = host.NewUuid() });
        Factories = new AnimationFactories<AnimationData>(host);
    }

    public ControlServiceHost CreateControlServices(Func<ControlThemeColor, ControlColor> themeColor,
        Action<string> info, Action<Exception, string> warn, Action<Exception, string> error,
        Action<DependencyObject> raiseCustomEvent) => new() {
        NewUuid = Host.NewUuid,
        AnimationEnabled = () => Engine.AniControlEnabled == 0,
        ThemeColor = themeColor, Info = info, Warn = warn, Error = error, RaiseCustomEvent = raiseCustomEvent,
        Animations = new ControlAnimationHost<AnimationData> {
            CreateColorResource = (target, property, key, spec) => Factories.AaColor(target, property, key,
                spec.Duration, spec.Delay, ToEase(spec), spec.After),
            CreateColorTo = (target, property, value, spec) => Factories.AaColor(target, property,
                AnimationColors.Subtract(value, Host.ReadColor(target.GetValue(property))), spec.Duration, spec.Delay, ToEase(spec), spec.After),
            CreateScale = (target, value, spec, absolute) => Factories.AaScale(target, value,
                spec.Duration, spec.Delay, ToEase(spec), spec.After, absolute),
            CreateScaleTransform = (target, value, spec) => Factories.AaScaleTransform(target, value,
                spec.Duration, spec.Delay, ToEase(spec), spec.After),
            ReadSpeed = () => Engine.AniSpeed,
            CreateWidth = (target, value, spec) => Factories.AaWidth(target, value, spec.Duration, spec.Delay, ToEase(spec), spec.After),
            CreateTranslateX = (target, value, spec) => Factories.AaTranslateX(target, value, spec.Duration, spec.Delay, ToEase(spec), spec.After),
            CreateTranslateY = (target, value, spec) => Factories.AaTranslateY(target, value, spec.Duration, spec.Delay, ToEase(spec), spec.After),
            CreateX = (target, value, spec) => Factories.AaX(target, value, spec.Duration, spec.Delay, ToEase(spec), spec.After),
            CreateY = (target, value, spec) => Factories.AaY(target, value, spec.Duration, spec.Delay, ToEase(spec), spec.After),
            CreateHeight = (target, value, spec) => Factories.AaHeight(target, value,
                spec.Duration, spec.Delay, ToEase(spec), spec.After),
            CreateOpacity = (target, value, spec) => Factories.AaOpacity(target, value,
                spec.Duration, spec.Delay, ToEase(spec), spec.After),
            CreateRotateTransform = (target, value, spec) => Factories.AaRotateTransform(target, value,
                spec.Duration, spec.Delay, ToEase(spec), spec.After),
            CreateDouble = (work, value, spec) => Factories.AaDouble(new ParameterizedThreadStart(delta => work(Convert.ToDouble(delta))), value,
                spec.Duration, spec.Delay, ToEase(spec), spec.After),
            CreateCode = (work, delay, after) => Factories.AaCode(new ThreadStart(() => work()), delay, after),
            StartFrames = (frames, name, refreshTime) => Engine.AniStart(frames, name, refreshTime),
            StopFrames = Engine.AniStop
        }
    };

    private static AnimationEase ToEase(ControlAnimationSpec spec) {
        AnimationEasePower power = spec.RawPower.HasValue ? (AnimationEasePower)spec.RawPower.Value : spec.Power switch {
            ControlEasePower.Weak => AnimationEasePower.Weak,
            ControlEasePower.Middle => AnimationEasePower.Middle,
            ControlEasePower.Strong => AnimationEasePower.Strong,
            ControlEasePower.ExtraStrong => AnimationEasePower.ExtraStrong,
            _ => throw new ArgumentOutOfRangeException(nameof(spec))
        };
        return spec.Ease switch {
            ControlEase.Linear => new AnimationEaseLinear(),
            ControlEase.OutFluent => new AnimationEaseOutFluent(power),
            ControlEase.InFluent => new AnimationEaseInFluent(power),
            ControlEase.OutBack => new AnimationEaseOutBack(power),
            ControlEase.InoutFluent => new AnimationEaseInoutFluent(power, spec.Middle),
            ControlEase.InBack => new AnimationEaseInBack(power),
            ControlEase.OutElastic => new AnimationEaseOutElastic(power),
            ControlEase.OutFluentWithInitial => new AnimationEaseOutFluentWithInitial(spec.InitialSpeed, spec.TotalSeconds, spec.TotalDistance),
            _ => throw new ArgumentOutOfRangeException(nameof(spec))
        };
    }
}
