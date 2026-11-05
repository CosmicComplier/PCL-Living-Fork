using System;
using System.Collections.Generic;

namespace PCLLF.Presentation.Animation;

/// <summary>Native C# fields for the same constrained engine used by LF's original struct.</summary>
public struct AnimationData : IAnimationData {
    public AnimationKind TypeMain;
    public AnimationSubKind TypeSub;
    public int TimeTotal;
    public double TimeFinished, TimePercent;
    public bool IsAfter;
    public AnimationEase? Ease;
    public object? Obj, Value, ValueLast;

    AnimationKind IAnimationData.Kind { get => TypeMain; set => TypeMain = value; }
    AnimationSubKind IAnimationData.SubKind { get => TypeSub; set => TypeSub = value; }
    int IAnimationData.Duration { get => TimeTotal; set => TimeTotal = value; }
    double IAnimationData.Elapsed { get => TimeFinished; set => TimeFinished = value; }
    double IAnimationData.Percent { get => TimePercent; set => TimePercent = value; }
    bool IAnimationData.After { get => IsAfter; set => IsAfter = value; }
    AnimationEase? IAnimationData.Easing { get => Ease; set => Ease = value; }
    object? IAnimationData.Target { get => Obj; set => Obj = value; }
    object? IAnimationData.Value { get => Value; set => Value = value; }
    object? IAnimationData.Remainder { get => ValueLast; set => ValueLast = value; }
    public override string ToString() => TypeMain + " | " + TimeFinished + "/" + TimeTotal + "(" + Math.Round(TimePercent * 100) + "%)" +
        (Obj is null ? "" : " | " + Obj + "(" + Obj.GetType().Name + ")");
}

public sealed class AnimationGroup : IAnimationGroup<AnimationData> {
    public List<AnimationData> Data = null!;
    public long StartTick;
    public int Uuid;
    List<AnimationData> IAnimationGroup<AnimationData>.Frames { get => Data; set => Data = value; }
    long IAnimationGroup<AnimationData>.Tick { get => StartTick; set => StartTick = value; }
    int IAnimationGroup<AnimationData>.Id { get => Uuid; set => Uuid = value; }
}
