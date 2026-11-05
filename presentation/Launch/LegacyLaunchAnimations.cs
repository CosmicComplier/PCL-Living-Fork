using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace PCLLF.Presentation.Launch;

public enum LegacyLaunchEase { Linear, OutFluent, InFluent, OutBackWeak, OutFluentWeak, InFluentWeak }
// The actual LF AniData and existing animation engine stay closed by the VB host.
// Page state/animation plans are C#; these delegates are only Aa/Ani primitives.
public sealed class LegacyLaunchAnimations<TAnimation> where TAnimation : struct {
    public Func<object, double, int, int, LegacyLaunchEase, bool, TAnimation> Opacity = null!, ScaleTransform = null!, Width = null!, Height = null!, GridWidth = null!, TranslateX = null!;
    public Func<Action, int, bool, TAnimation> Code = null!;
    public Action<IEnumerable<TAnimation>, string, bool> Start = null!;
    public Action<string> Stop = null!;
    public Func<string, bool> IsRunning = null!;
}
