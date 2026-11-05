using System;

namespace PCLLF.Presentation.Animation;

// LF Modules/Base/ModAnimation.vb easing functions; see root LICENCE.
// Same-source C# reference: CE 1a7653127d578efdb2a9388999a3fdc67d9e49d5,
// Plain Craft Launcher 2/Modules/Base/ModAnimation.cs:1200-1467; CE LICENSE and its PCL LICENCE.
// LF's CompareTo clamp and unmodified split formulas are retained, including NaN and zero splits.
public abstract class AnimationEase {
    public abstract double GetValue(double t);
    public virtual double GetDelta(double t1, double t0) => GetValue(t1) - GetValue(t0);
}

public class AnimationEaseInout : AnimationEase {
    private readonly AnimationEase easeIn, easeOut;
    private readonly double easeInPercent;
    public AnimationEaseInout(AnimationEase easeIn, AnimationEase easeOut, double easeInPercent = 0.5) {
        this.easeIn = easeIn; this.easeOut = easeOut; this.easeInPercent = easeInPercent;
    }
    public override double GetValue(double t) {
        if (t < easeInPercent) return easeInPercent * easeIn.GetValue(t / easeInPercent);
        return (1 - easeInPercent) * easeOut.GetValue((t - easeInPercent) / (1 - easeInPercent)) + easeInPercent;
    }
}

public class AnimationEaseLinear : AnimationEase {
    public override double GetValue(double t) => AnimationMath.Clamp(t, 0, 1);
    public override double GetDelta(double t1, double t0) => AnimationMath.Clamp(t1, 0, 1) - AnimationMath.Clamp(t0, 0, 1);
}

public class AnimationEaseInFluent : AnimationEase {
    private readonly AnimationEasePower power;
    public AnimationEaseInFluent(AnimationEasePower power = AnimationEasePower.Middle) { this.power = power; }
    public override double GetValue(double t) => Math.Pow(AnimationMath.Clamp(t, 0, 1), (int)power);
}

public class AnimationEaseOutFluent : AnimationEase {
    private readonly AnimationEasePower power;
    public AnimationEaseOutFluent(AnimationEasePower power = AnimationEasePower.Middle) { this.power = power; }
    public override double GetValue(double t) => 1 - Math.Pow(AnimationMath.Clamp(1 - t, 0, 1), (int)power);
}

public class AnimationEaseInoutFluent : AnimationEase {
    private readonly AnimationEaseInout ease;
    public AnimationEaseInoutFluent(AnimationEasePower power = AnimationEasePower.Middle, double middle = 0.5) {
        ease = new(new AnimationEaseInFluent(power), new AnimationEaseOutFluent(power), middle);
    }
    public override double GetValue(double t) => ease.GetValue(t);
}

public class AnimationEaseOutFluentWithInitial : AnimationEase {
    private readonly double alpha;
    public AnimationEaseOutFluentWithInitial(double initialPixelPerSecond, double totalSecond, double totalDistance) {
        double normalizedInitial = initialPixelPerSecond * totalSecond / totalDistance;
        alpha = normalizedInitial - 1.0;
        if (alpha < 0) alpha = 0;
    }
    public override double GetValue(double t) {
        double percent = AnimationMath.Clamp(t, 0, 1);
        if (alpha == 0) return percent;
        return (alpha + 1) * percent / (1 + alpha * percent);
    }
}

public class AnimationEaseInBack : AnimationEase {
    private readonly double power;
    public AnimationEaseInBack(AnimationEasePower power = AnimationEasePower.Middle) { this.power = 3 - (int)power * 0.5; }
    public override double GetValue(double t) {
        t = AnimationMath.Clamp(t, 0, 1);
        return Math.Pow(t, power) * Math.Cos(1.5 * Math.PI * (1 - t));
    }
}

public class AnimationEaseOutBack : AnimationEase {
    private readonly double power;
    public AnimationEaseOutBack(AnimationEasePower power = AnimationEasePower.Middle) { this.power = 3 - (int)power * 0.5; }
    public override double GetValue(double t) {
        t = AnimationMath.Clamp(t, 0, 1);
        return 1 - Math.Pow(1 - t, power) * Math.Cos(1.5 * Math.PI * t);
    }
}

public class AnimationEaseInCar : AnimationEase {
    private readonly AnimationEaseInout ease;
    public AnimationEaseInCar(double middle = 0.7, AnimationEasePower power = AnimationEasePower.Middle) {
        ease = new(new AnimationEaseInBack(power), new AnimationEaseOutFluent(power), middle);
    }
    public override double GetValue(double t) => ease.GetValue(t);
}

public class AnimationEaseOutCar : AnimationEase {
    private readonly AnimationEaseInout ease;
    public AnimationEaseOutCar(double middle = 0.3, AnimationEasePower power = AnimationEasePower.Middle) {
        ease = new(new AnimationEaseInFluent(power), new AnimationEaseOutBack(power), middle);
    }
    public override double GetValue(double t) => ease.GetValue(t);
}

public class AnimationEaseInElastic : AnimationEase {
    private readonly int power;
    public AnimationEaseInElastic(AnimationEasePower power = AnimationEasePower.Middle) { this.power = checked((int)power + 4); }
    public override double GetValue(double t) {
        t = AnimationMath.Clamp(t, 0, 1);
        return Math.Pow(t, (power - 1) * 0.25) * Math.Cos((power - 3.5) * Math.PI * Math.Pow(1 - t, 1.5));
    }
}

public class AnimationEaseOutElastic : AnimationEase {
    private readonly int power;
    public AnimationEaseOutElastic(AnimationEasePower power = AnimationEasePower.Middle) { this.power = checked((int)power + 4); }
    public override double GetValue(double t) {
        t = 1 - AnimationMath.Clamp(t, 0, 1);
        return 1 - Math.Pow(t, (power - 1) * 0.25) * Math.Cos((power - 3.5) * Math.PI * Math.Pow(1 - t, 1.5));
    }
}
