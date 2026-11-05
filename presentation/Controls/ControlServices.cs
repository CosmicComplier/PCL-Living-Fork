using System;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace PCLLF.Presentation.Controls;

public enum ControlThemeColor { Gray4, Gray5, Accent8, SemiTransparent }

/// <summary>LF MyColor channel values, retained as doubles until the original brush conversion.</summary>
public readonly struct ControlColor {
    public readonly double A, R, G, B;
    public ControlColor(double red, double green, double blue) : this(255, red, green, blue) { }
    public ControlColor(double alpha, double red, double green, double blue) { A = alpha; R = red; G = green; B = blue; }
    public ControlColor WithAlpha(double alpha) => new(alpha, R, G, B);
    public SolidColorBrush ToBrush() => new(Color.FromArgb(Byte(A), Byte(R), Byte(G), Byte(B)));
    internal static byte Byte(double value) {
        if (value < 0) value = 0;
        if (value > 255) value = 255;
        return Convert.ToByte(Math.Round(value));
    }
}

public sealed class ControlPreviewEventArgs : EventArgs {
    public bool RaiseByMouse;
    public bool Handled;
    public ControlPreviewEventArgs(bool raiseByMouse = false) { RaiseByMouse = raiseByMouse; }
}

/// <summary>Platform primitives only; controls retain their LF state transitions and animation plans.</summary>
public sealed class ControlServiceHost {
    public Func<int> NewUuid = null!;
    public Func<bool> AnimationEnabled = null!;
    public Func<ControlThemeColor, ControlColor> ThemeColor = null!;
    public Action<string> Info = null!;
    public Action<Exception, string> Warn = null!, Error = null!;
    public Action<DependencyObject> RaiseCustomEvent = null!;
    public ControlAnimations Animations = null!;
}

public static class ControlServices {
    private static ControlServiceHost? current;
    public static ControlServiceHost Current => Volatile.Read(ref current) ??
        throw new InvalidOperationException("Configure LF control services before constructing presentation controls.");

    public static void Configure(ControlServiceHost host) {
        if (host is null) throw new ArgumentNullException(nameof(host));
        if (host.Animations is null) throw new ArgumentException("A real control animation host is required.", nameof(host));
        Volatile.Write(ref current, host);
    }
}
