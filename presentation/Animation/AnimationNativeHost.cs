using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using Microsoft.VisualBasic.CompilerServices;
using PCLLF.Presentation.Controls;

namespace PCLLF.Presentation.Animation;

/// <summary>Real WPF, clock and CLR compatibility primitives for the native C# engine.</summary>
public static class AnimationNativeHost {
    private static int uuid;
    private static readonly Random random = new();
    private static readonly object randomGate = new();

    public static AnimationHost Create(Func<int> animationSetting, Func<bool> debugMode, Func<int> downloadRemaining,
        Action<int, int> reportDiagnostics, Action<string> info, Action<Exception, string> error, Action<Exception, string> fatal) => new() {
        Clock = () => Stopwatch.GetTimestamp() / (Stopwatch.Frequency / 1000L),
        NewUuid = () => Interlocked.Increment(ref uuid),
        NewLinearEasing = () => new AnimationEaseLinear(),
        AnimationSetting = animationSetting, DebugMode = debugMode, DownloadRemaining = downloadRemaining,
        ReportDiagnostics = reportDiagnostics, Info = info, Error = error, Fatal = fatal,
        UiWait = work => {
            if (Application.Current.Dispatcher.CheckAccess()) work();
            else Application.Current.Dispatcher.Invoke(work);
        },
        StartThread = (work, name, priority) => {
            // LF's worker is a foreground thread. Running remains a status field, not a new stop token.
            Thread worker = new(() => work()) { Name = name, Priority = priority };
            worker.Start();
        },
        Sleep = Thread.Sleep,
        RandomInteger = (minimum, maximum) => {
            lock (randomGate) return checked((int)Math.Floor((maximum - minimum + 1) * random.NextDouble()) + minimum);
        },
        DecodeText = DecodeText,
        Radix86 = value => "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz/+=!?@#$%^&*()[]{}<>;:',"[value].ToString(),
        FindResource = name => Application.Current.FindResource(name),
        Number = Conversions.ToDouble,
        // Only the original object/Context fallback crosses the VB runtime compatibility accessor.
        // Normal WPF targets are handled by typed code in AnimationEngine.
        GetProperty = GetProperty,
        SetProperty = (target, member, value) => NewLateBinding.LateSet(target, null, member, new[] { value }, null, null),
        ReadColor = ReadColor, BoxColor = value => value,
        ReadRect = value => value is AnimationRect rectangle ? rectangle : new AnimationRect(
            Conversions.ToDouble(GetProperty(value!, "Left")), Conversions.ToDouble(GetProperty(value!, "Top")),
            Conversions.ToDouble(GetProperty(value!, "Width")), Conversions.ToDouble(GetProperty(value!, "Height"))),
        BoxRect = value => value
    };

    private static object? GetProperty(object target, string member) =>
        NewLateBinding.LateGet(target, null, member, Array.Empty<object>(), null, null, null);

    private static ControlColor ReadColor(object? value) {
        if (value is null) return new ControlColor(255, 255, 255);
        if (value is ControlColor channels) return channels;
        if (value is Color native) return new ControlColor(native.A, native.R, native.G, native.B);
        if (value is SolidColorBrush brush) return new ControlColor(brush.Color.A, brush.Color.R, brush.Color.G, brush.Color.B);
        // LF's MyColor(Object) reads signed channels individually for other objects.
        return new ControlColor(Conversions.ToDouble(GetProperty(value, "A")), Conversions.ToDouble(GetProperty(value, "R")),
            Conversions.ToDouble(GetProperty(value, "G")), Conversions.ToDouble(GetProperty(value, "B")));
    }

    private static string DecodeText(byte[] bytes) {
#if NETFRAMEWORK
        return Encoding.GetEncoding("GB18030").GetString(bytes);
#else
        // Ask this provider locally; do not register or alter process-wide encoding behavior.
        Encoding encoding = CodePagesEncodingProvider.Instance.GetEncoding("GB18030") ?? throw new NotSupportedException("GB18030 is unavailable.");
        return encoding.GetString(bytes);
#endif
    }
}
