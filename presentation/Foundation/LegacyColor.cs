using System;
using System.Globalization;
using Microsoft.VisualBasic.CompilerServices;
using System.Windows.Media;
using PCLLF.Presentation.Animation;
using PCLLF.Presentation.Controls;

namespace PCLLF.Presentation.Foundation;

// LF ModBase.MyColor at f4aa0fae62c3ee8af24c81e61844c8fdd13c8983 is the
// behavior source (LF LICENCE); the same region in PCL 5ef81e49 is unchanged.
// CE 1a7653127d578efdb2a9388999a3fdc67d9e49d5 ModBase.cs is a C# syntax
// reference (CE Plain Craft Launcher 2/LICENCE; LF LICENCE). LF constructor mutation order, Equals
// interop, signed channels and six-digit Lerp remain LF, not CE replacements.
public class LegacyColor {
    public double A = 255, R, G, B;

    public LegacyColor() { }
    public LegacyColor(Color color) { A = color.A; R = color.R; G = color.G; B = color.B; }
    public LegacyColor(string hexString) : this((Color)ColorConverter.ConvertFromString(hexString)!) { }
    public LegacyColor(double alpha, LegacyColor color) { A = alpha; R = color.R; G = color.G; B = color.B; }
    public LegacyColor(double red, double green, double blue) { A = 255; R = red; G = green; B = blue; }
    public LegacyColor(double alpha, double red, double green, double blue) { A = alpha; R = red; G = green; B = blue; }
    public LegacyColor(Brush brush) : this(((SolidColorBrush)brush).Color) { }
    public LegacyColor(SolidColorBrush brush) : this(brush.Color) { }
    public LegacyColor(object? value) {
        if (value is null) {
            A = 255; R = 255; G = 255; B = 255;
        } else if (value is SolidColorBrush brush) {
            Color color = brush.Color;
            A = color.A; R = color.R; G = color.G; B = color.B;
        } else {
            // Preserve the original VB object boundary, including boxed Color,
            // public ARGB members, conversions and missing-member exceptions.
            A = Channel(value, "A"); R = Channel(value, "R");
            G = Channel(value, "G"); B = Channel(value, "B");
        }
    }

    private static double Channel(object value, string member) =>
        Conversions.ToDouble(NewLateBinding.LateGet(value, null, member, Array.Empty<object>(), null, null, null));
    public ControlColor ToControlColor() => new(A, R, G, B);
    public static ControlColor Add(LegacyColor first, LegacyColor second) => AnimationColors.Add(first.ToControlColor(), second.ToControlColor());
    public static ControlColor Subtract(LegacyColor first, LegacyColor second) => AnimationColors.Subtract(first.ToControlColor(), second.ToControlColor());
    public static ControlColor Multiply(LegacyColor value, double scale) => new(value.A * scale, value.R * scale, value.G * scale, value.B * scale);
    public static ControlColor Divide(LegacyColor value, double divisor) => new(value.A / divisor, value.R / divisor, value.G / divisor, value.B / divisor);
    public static bool AreEqual(LegacyColor? first, LegacyColor? second) {
        if (first is null && second is null) return true;
        if (first is null || second is null) return false;
        // Do not replace with ReferenceEquals: the same NaN-containing color
        // remains unequal to itself in LF's original double comparisons.
        return first.A == second.A && first.R == second.R && first.G == second.G && first.B == second.B;
    }
    public static ControlColor Lerp(LegacyColor first, LegacyColor second, double percentage) =>
        AnimationColors.Lerp(first.ToControlColor(), second.ToControlColor(), percentage);
    public static ControlColor Round(LegacyColor value, int digits = 0) => new(
        Math.Round(value.A, digits), Math.Round(value.R, digits), Math.Round(value.G, digits), Math.Round(value.B, digits));

    // The shared helper already has LF's comparison/clamp/banker-round/NaN contract.
    public static byte ClampToByte(double value) => ControlColor.Byte(value);
    public Color ToMediaColor() => Color.FromArgb(ClampToByte(A), ClampToByte(R), ClampToByte(G), ClampToByte(B));
    public System.Drawing.Color ToDrawingColor() => System.Drawing.Color.FromArgb(ClampToByte(A), ClampToByte(R), ClampToByte(G), ClampToByte(B));
    public SolidColorBrush ToBrush() => ToControlColor().ToBrush();

    public double Hue(double first, double second, double hue) {
        if (hue < 0) hue += 1;
        if (hue > 1) hue -= 1;
        if (hue < 0.16667) return first + (second - first) * 6 * hue;
        if (hue < 0.5) return second;
        if (hue < 0.66667) return first + (second - first) * (4 - hue * 6);
        return first;
    }
    public LegacyColor FromHSL(double hue, double saturation, double lightness) {
        A = 255;
        if (saturation == 0) {
            R = lightness * 2.55; G = R; B = R;
            return this;
        }
        double h = hue / 360, s = saturation / 100, l = lightness / 100;
        s = l < 0.5 ? s * l + l : s * (1.0 - l) + l;
        l = 2 * l - s;
        R = 255 * Hue(l, s, h + 1.0 / 3.0);
        G = 255 * Hue(l, s, h);
        B = 255 * Hue(l, s, h - 1.0 / 3.0);
        return this;
    }
    public LegacyColor FromHSL2(double hue, double saturation, double lightness) {
        A = 255;
        if (saturation == 0) {
            R = lightness * 2.55; G = R; B = R;
            return this;
        }
        hue = (hue + 3600000) % 360;
        double[] centers = { +0.1, -0.06, -0.3, -0.19, -0.15, -0.24, -0.32, -0.09, +0.18, +0.05, -0.12, -0.02, +0.1, -0.06 };
        double center = hue / 30.0;
        int index = Convert.ToInt32(Math.Floor(center));
        center = 50 - ((1 - center + index) * centers[index] + (center - index) * centers[checked(index + 1)]) * saturation;
        lightness = (lightness < center ? lightness / center : 1 + (lightness - center) / (100 - center)) * 50;
        FromHSL(hue, saturation, lightness);
        return this;
    }

    private static string ChannelText(double value) {
#if NETFRAMEWORK
        return Conversions.ToString(value);
#else
        // Compile the single existing compatibility source into presentation;
        // finite LF CLR4 G15 formatting is not modern runtime default G17.
        if (double.IsNaN(value) || double.IsInfinity(value)) return Conversions.ToString(value);
        return PCLCS.LegacyPackMetadataNumberFormatting.Format(value, 15, CultureInfo.CurrentCulture);
#endif
    }
    public override string ToString() => "(" + ChannelText(A) + "," + ChannelText(R) + "," + ChannelText(G) + "," + ChannelText(B) + ")";
    public override bool Equals(object? value) => AreEqual(this, (LegacyColor?)value);
    // LF overrides Equals without changing identity hashing. Keep that behavior.
    public override int GetHashCode() => base.GetHashCode();
}
