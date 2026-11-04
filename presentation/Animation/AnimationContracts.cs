using System;
using System.Collections.Generic;
using System.Threading;
using PCLLF.Presentation.Controls;

namespace PCLLF.Presentation.Animation;

public enum AnimationKind { Number, Color, Scale, TextAppear, Code, ScaleTransform, RotateTransform }
public enum AnimationSubKind { X, Y, Width, Height, Opacity, Value, Radius, BorderThickness, StrokeThickness, TranslateX, TranslateY, Double, DoubleParam, GridLengthWidth }
public enum AnimationEasePower { Weak = 2, Middle = 3, Strong = 4, ExtraStrong = 5 }

/// <summary>Constrained struct access maps the original LF fields; writes are returned to the original list.</summary>
public interface IAnimationData {
    AnimationKind Kind { get; set; }
    AnimationSubKind SubKind { get; set; }
    int Duration { get; set; }
    double Elapsed { get; set; }
    double Percent { get; set; }
    bool After { get; set; }
    AnimationEase? Easing { get; set; }
    object? Target { get; set; }
    object? Value { get; set; }
    object? Remainder { get; set; }
}

public interface IAnimationGroup<TData> where TData : struct, IAnimationData {
    List<TData> Frames { get; set; }
    long Tick { get; set; }
    int Id { get; set; }
}

public readonly struct AnimationRect {
    public readonly double Left, Top, Width, Height;
    public AnimationRect(double left, double top, double width, double height) {
        Left = left; Top = top; Width = width; Height = height;
    }
}

/// <summary>LF platform primitives and field aliases; no frame or group state is mirrored here.</summary>
public sealed class AnimationHost {
    public Func<long> Clock = null!, ReadLastTick = null!;
    public Action<long> WriteLastTick = null!;
    public Func<bool> ReadRunning = null!, DebugMode = null!;
    public Action<bool> WriteRunning = null!;
    public Func<int> ReadFps = null!, AnimationSetting = null!, DownloadRemaining = null!, NewUuid = null!;
    public Action<int> WriteFps = null!, Sleep = null!;
    public Action<Action> UiWait = null!;
    public Action<Action, string, ThreadPriority> StartThread = null!;
    public Action<string> Info = null!;
    public Action<Exception, string> Error = null!, Fatal = null!;
    public Action<int, int> ReportDiagnostics = null!;
    public Func<int, int, int> RandomInteger = null!;
    public Func<byte[], string> DecodeText = null!;
    public Func<int, string> Radix86 = null!;
    public Func<AnimationEase> NewLinearEasing = null!;
    public Func<string, object?> FindResource = null!;
    public Func<object?, double> Number = null!;
    public Func<object, string, object?> GetProperty = null!;
    public Action<object, string, object?> SetProperty = null!;
    public Func<object?, ControlColor> ReadColor = null!;
    public Func<ControlColor, object> BoxColor = null!;
    public Func<object?, AnimationRect> ReadRect = null!;
    public Func<AnimationRect, object> BoxRect = null!;
}

public static class AnimationMath {
    // LF's pinned MeloongCore c774ae4 MathUtils uses CompareTo (including NaN)
    // and rounds numeric Lerp to seven decimals. Math.Clamp is not equivalent.
    public static double Clamp(double value, double minimum, double maximum) {
        if (value.CompareTo(minimum) < 0) return minimum;
        if (value.CompareTo(maximum) > 0) return maximum;
        return value;
    }
    public static long Clamp(long value, long minimum, long maximum) {
        if (value.CompareTo(minimum) < 0) return minimum;
        if (value.CompareTo(maximum) > 0) return maximum;
        return value;
    }
    public static double Lerp(double start, double end, double percentage) => Math.Round(start + (end - start) * percentage, 7);
}

public static class AnimationColors {
    public static ControlColor Add(ControlColor left, ControlColor right) => new(left.A + right.A, left.R + right.R, left.G + right.G, left.B + right.B);
    public static ControlColor Subtract(ControlColor left, ControlColor right) => new(left.A - right.A, left.R - right.R, left.G - right.G, left.B - right.B);
    // MyColor.Lerp rounds each signed channel to six places before adding ValueLast.
    public static ControlColor Lerp(ControlColor start, ControlColor end, double percentage) => new(
        Math.Round(start.A * (1 - percentage) + end.A * percentage, 6),
        Math.Round(start.R * (1 - percentage) + end.R * percentage, 6),
        Math.Round(start.G * (1 - percentage) + end.G * percentage, 6),
        Math.Round(start.B * (1 - percentage) + end.B * percentage, 6));
}
