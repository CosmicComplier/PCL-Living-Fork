using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using PCLLF.Presentation.Controls;

namespace PCLLF.Presentation.Animation;

/// <summary>LF's original frame/group algorithm, running against the host's actual data and dictionary.</summary>
public sealed partial class AnimationEngine<TData, TGroup>
    where TData : struct, IAnimationData where TGroup : class, IAnimationGroup<TData> {
    // LF Modules/Base/ModAnimation.vb: AniStart/Stop/IsRun, AniTimer and AniRun.
    // This is an LF behavior port under the repository LICENCE; no CE implementation is copied.
    private readonly AnimationHost host;
    private readonly Func<Dictionary<string, TGroup>> groups;
    private readonly Func<List<TData>, long, TGroup> createGroup;
    private int frameCount;

    public AnimationEngine(AnimationHost host, Func<Dictionary<string, TGroup>> groups,
        Func<List<TData>, long, TGroup> createGroup) {
        this.host = host;
        this.groups = groups;
        this.createGroup = createGroup;
    }

    public AnimationHost Host => host;
    public int FrameCount => frameCount;

    public void AniStart(IList animations, string name = "", bool refreshTime = false) {
        if (refreshTime) host.WriteLastTick(host.Clock());
        List<TData> all = new();
        foreach (object? element in animations) {
            if (element is ICollection) all.AddRange((IEnumerable<TData>)element);
            else all.Add((TData)element!);
        }
        TGroup entry = createGroup(all, host.Clock());
        if (string.IsNullOrEmpty(name)) name = entry.Id.ToString();
        else AniStop(name);
        groups().Add(name, entry);
    }

    public void AniStart(TData animation, string name = "", bool refreshTime = false) =>
        AniStart(new List<TData> { animation }, name, refreshTime);

    public void AniStop(string name) => groups().Remove(name);
    public bool AniIsRun(string name) => groups().ContainsKey(name);

    public void AniTimer(double deltaTick) {
        try {
            if (deltaTick / AniSpeed > 100) host.Info($"两个动画帧间隔 {deltaTick} ms");
            int index = -1;
            while (index + 1 < groups().Count) {
                index = checked(index + 1);
                TGroup entry = groups().Values.ElementAt(index);
                if (entry.Tick > host.ReadLastTick()) continue;
                bool canRemoveAfter = true;
                int frameIndex = 0;
                while (frameIndex < entry.Frames.Count) {
                    TData frame = entry.Frames[frameIndex];
                    if (!frame.After) {
                        canRemoveAfter = false;
                        frame.Elapsed += deltaTick;
                        if (frame.Elapsed > 0) {
                            frame = AniRun(frame);
                            frameCount = checked(frameCount + 1);
                        }
                        if (frame.Elapsed >= frame.Duration) {
                            if (frame.Kind == AnimationKind.Color) {
                                object?[] target = (object?[])frame.Target!;
                                string? resource = (string?)target[2];
                                if (!string.IsNullOrEmpty(resource))
                                    ((FrameworkElement)target[0]!).SetResourceReference((DependencyProperty)target[1]!, resource);
                            }
                            entry.Frames.RemoveAt(frameIndex);
                            continue;
                        }
                        entry.Frames[frameIndex] = frame;
                    } else {
                        if (canRemoveAfter) {
                            canRemoveAfter = false;
                            frame.After = false;
                            entry.Frames[frameIndex] = frame;
                            continue;
                        }
                        break;
                    }
                    frameIndex = checked(frameIndex + 1);
                }
                if (!entry.Frames.Any()) {
                    // Code callbacks can replace/add a same-name group. Match the old identity,
                    // and read the real dictionary again on every access, just as LF does.
                    for (int current = 0; current < groups().Count; current++) {
                        if (groups().ElementAt(current).Value.Id == entry.Id) {
                            groups().Remove(groups().ElementAt(current).Key);
                            break;
                        }
                    }
                    index = checked(index - 1);
                }
            }
        } catch (Exception error) {
            host.Error(error, "动画刻执行失败");
        }
    }

    private TData AniRun(TData frame) {
        try {
            switch (frame.Kind) {
                case AnimationKind.Number: {
                    double delta = AnimationMath.Lerp(0, host.Number(frame.Value),
                        frame.Easing!.GetDelta(frame.Elapsed / frame.Duration, frame.Percent));
                    if (delta != 0) {
                        object target = frame.Target!;
                        switch (frame.SubKind) {
                            case AnimationSubKind.X:
                                if (target is Window windowX) windowX.Left += delta;
                                else if (target is FrameworkElement elementX) {
                                    switch (elementX.HorizontalAlignment) {
                                        case HorizontalAlignment.Left:
                                        case HorizontalAlignment.Stretch: {
                                            Thickness margin = elementX.Margin;
                                            elementX.Margin = new Thickness(margin.Left + delta, margin.Top, margin.Right, margin.Bottom);
                                            break;
                                        }
                                        case HorizontalAlignment.Right: {
                                            Thickness margin = elementX.Margin;
                                            elementX.Margin = new Thickness(margin.Left, margin.Top, margin.Right - delta, margin.Bottom);
                                            break;
                                        }
                                    }
                                } else {
                                    double alignment = host.Number(host.GetProperty(target, "HorizontalAlignment"));
                                    if (alignment == (int)HorizontalAlignment.Left || alignment == (int)HorizontalAlignment.Stretch) {
                                        Thickness margin = (Thickness)host.GetProperty(target, "Margin")!;
                                        host.SetProperty(target, "Margin", new Thickness(margin.Left + delta, margin.Top, margin.Right, margin.Bottom));
                                    } else if (alignment == (int)HorizontalAlignment.Right) {
                                        Thickness margin = (Thickness)host.GetProperty(target, "Margin")!;
                                        host.SetProperty(target, "Margin", new Thickness(margin.Left, margin.Top, margin.Right - delta, margin.Bottom));
                                    }
                                }
                                break;
                            case AnimationSubKind.Y:
                                if (target is Window windowY) windowY.Top += delta;
                                else if (target is FrameworkElement elementY) {
                                    switch (elementY.VerticalAlignment) {
                                        case VerticalAlignment.Top: {
                                            Thickness margin = elementY.Margin;
                                            elementY.Margin = new Thickness(margin.Left, margin.Top + delta, margin.Right, margin.Bottom);
                                            break;
                                        }
                                        case VerticalAlignment.Bottom: {
                                            Thickness margin = elementY.Margin;
                                            elementY.Margin = new Thickness(margin.Left, margin.Top, margin.Right, margin.Bottom - delta);
                                            break;
                                        }
                                    }
                                } else {
                                    double alignment = host.Number(host.GetProperty(target, "VerticalAlignment"));
                                    if (alignment == (int)VerticalAlignment.Top) {
                                        Thickness margin = (Thickness)host.GetProperty(target, "Margin")!;
                                        host.SetProperty(target, "Margin", new Thickness(margin.Left, margin.Top + delta, margin.Right, margin.Bottom));
                                    } else if (alignment == (int)VerticalAlignment.Bottom) {
                                        Thickness margin = (Thickness)host.GetProperty(target, "Margin")!;
                                        host.SetProperty(target, "Margin", new Thickness(margin.Left, margin.Top, margin.Right, margin.Bottom - delta));
                                    }
                                }
                                break;
                            case AnimationSubKind.Opacity:
                                if (target is UIElement opacityElement)
                                    opacityElement.Opacity = AnimationMath.Clamp(opacityElement.Opacity + delta, 0, 1);
                                else if (target is DropShadowEffect shadow)
                                    shadow.Opacity = AnimationMath.Clamp(shadow.Opacity + delta, 0, 1);
                                break;
                            case AnimationSubKind.Width: {
                                FrameworkElement element = (FrameworkElement)target;
                                element.Width = Math.Max((double.IsNaN(element.Width) ? element.ActualWidth : element.Width) + delta, 0);
                                break;
                            }
                            case AnimationSubKind.Height: {
                                FrameworkElement element = (FrameworkElement)target;
                                element.Height = Math.Max((double.IsNaN(element.Height) ? element.ActualHeight : element.Height) + delta, 0);
                                break;
                            }
                            case AnimationSubKind.Value:
                                if (target is RangeBase range) range.Value += delta;
                                else host.SetProperty(target, "Value", host.Number(host.GetProperty(target, "Value")) + delta);
                                break;
                            case AnimationSubKind.Radius:
                                if (target is BlurEffect blur) blur.Radius += delta;
                                else host.SetProperty(target, "Radius", host.Number(host.GetProperty(target, "Radius")) + delta);
                                break;
                            case AnimationSubKind.StrokeThickness:
                                if (target is Shape shape) shape.StrokeThickness = Math.Max(shape.StrokeThickness + delta, 0);
                                else host.SetProperty(target, "StrokeThickness", Math.Max(host.Number(host.GetProperty(target, "StrokeThickness")) + delta, 0));
                                break;
                            case AnimationSubKind.BorderThickness:
                                if (target is Control control) control.BorderThickness = new Thickness(control.BorderThickness.Bottom + delta);
                                else if (target is Border border) border.BorderThickness = new Thickness(border.BorderThickness.Bottom + delta);
                                else host.SetProperty(target, "BorderThickness", new Thickness(((Thickness)host.GetProperty(target, "BorderThickness")!).Bottom + delta));
                                break;
                            case AnimationSubKind.TranslateX:
                                if (target is UIElement translateElementX) {
                                    if (translateElementX.RenderTransform is not TranslateTransform) translateElementX.RenderTransform = new TranslateTransform(0, 0);
                                    ((TranslateTransform)translateElementX.RenderTransform).X += delta;
                                } else {
                                    object? transform = host.GetProperty(target, "RenderTransform");
                                    if (transform is null || host.GetProperty(target, "RenderTransform") is not TranslateTransform)
                                        host.SetProperty(target, "RenderTransform", new TranslateTransform(0, 0));
                                    ((TranslateTransform)host.GetProperty(target, "RenderTransform")!).X += delta;
                                }
                                break;
                            case AnimationSubKind.TranslateY:
                                if (target is UIElement translateElementY) {
                                    if (translateElementY.RenderTransform is not TranslateTransform) translateElementY.RenderTransform = new TranslateTransform(0, 0);
                                    ((TranslateTransform)translateElementY.RenderTransform).Y += delta;
                                } else {
                                    object? transform = host.GetProperty(target, "RenderTransform");
                                    if (transform is null || host.GetProperty(target, "RenderTransform") is not TranslateTransform)
                                        host.SetProperty(target, "RenderTransform", new TranslateTransform(0, 0));
                                    ((TranslateTransform)host.GetProperty(target, "RenderTransform")!).Y += delta;
                                }
                                break;
                            case AnimationSubKind.Double: {
                                object[] data = (object[])target;
                                DependencyObject element = (DependencyObject)data[0];
                                DependencyProperty property = (DependencyProperty)data[1];
                                element.SetValue(property, host.Number(element.GetValue(property)) + delta);
                                break;
                            }
                            case AnimationSubKind.DoubleParam:
                                ((ParameterizedThreadStart)target)(delta);
                                break;
                            case AnimationSubKind.GridLengthWidth:
                                if (target is ColumnDefinition column) column.Width = new GridLength(Math.Max(column.Width.Value + delta, 0), GridUnitType.Star);
                                else {
                                    object width = host.GetProperty(target, "Width")!;
                                    double value = width is GridLength length ? length.Value : host.Number(host.GetProperty(width, "Value"));
                                    host.SetProperty(target, "Width", new GridLength(Math.Max(value + delta, 0), GridUnitType.Star));
                                }
                                break;
                        }
                    }
                    break;
                }
                case AnimationKind.Color: {
                    ControlColor delta = AnimationColors.Add(AnimationColors.Lerp(new ControlColor(0, 0, 0, 0), host.ReadColor(frame.Value),
                        frame.Easing!.GetDelta(frame.Elapsed / frame.Duration, frame.Percent)), host.ReadColor(frame.Remainder));
                    object[] target = (object[])frame.Target!;
                    FrameworkElement element = (FrameworkElement)target[0];
                    DependencyProperty property = (DependencyProperty)target[1];
                    ControlColor color = AnimationColors.Add(host.ReadColor(element.GetValue(property)), delta);
                    if (property.PropertyType.Name == "Color") element.SetValue(property, color.ToBrush().Color);
                    else element.SetValue(property, color.ToBrush());
                    frame.Remainder = host.BoxColor(AnimationColors.Subtract(color, host.ReadColor(element.GetValue(property))));
                    break;
                }
                case AnimationKind.Scale: {
                    FrameworkElement element = (FrameworkElement)frame.Target!;
                    double delta = frame.Easing!.GetDelta(frame.Elapsed / frame.Duration, frame.Percent);
                    AnimationRect value = host.ReadRect(frame.Value);
                    Thickness margin = element.Margin;
                    element.Margin = new Thickness(margin.Left + AnimationMath.Lerp(0, value.Left, delta), margin.Top + AnimationMath.Lerp(0, value.Top, delta),
                        margin.Right + AnimationMath.Lerp(0, value.Left, delta), margin.Bottom + AnimationMath.Lerp(0, value.Top, delta));
                    element.Width = Math.Max(element.Width + AnimationMath.Lerp(0, host.ReadRect(frame.Value).Width, delta), 0);
                    element.Height = Math.Max(element.Height + AnimationMath.Lerp(0, host.ReadRect(frame.Value).Height, delta), 0);
                    break;
                }
                case AnimationKind.TextAppear: {
                    object[] value = (object[])frame.Value!;
                    string text = (string)value[0];
                    bool hide = (bool)value[1];
                    int count = checked((hide ? text.Length : 0) + Convert.ToInt32(Math.Round(text.Length * (hide ? -1 : 1) * frame.Easing!.GetDelta(frame.Elapsed / frame.Duration, 0))));
                    string result = text.Substring(0, count);
                    if (count < text.Length) {
                        if (text[count] >= 128)
                            result += host.DecodeText(new[] { checked((byte)host.RandomInteger(176, 247)), checked((byte)host.RandomInteger(161, 249)) });
                        else result += host.Radix86(host.RandomInteger(0, 85));
                    }
                    if (frame.Target is TextBlock block) block.Text = result;
                    else host.SetProperty(frame.Target!, "Context", result);
                    break;
                }
                case AnimationKind.Code:
                    frame.Elapsed = frame.Duration;
                    ((ThreadStart)frame.Value!)();
                    break;
                case AnimationKind.ScaleTransform: {
                    FrameworkElement element = (FrameworkElement)frame.Target!;
                    if (element.RenderTransform is not ScaleTransform) {
                        if (element.RenderTransformOrigin == new Point(0, 0)) element.RenderTransformOrigin = new Point(0.5, 0.5);
                        element.RenderTransform = new ScaleTransform(1, 1);
                    }
                    double delta = AnimationMath.Lerp(0, host.Number(frame.Value), frame.Easing!.GetDelta(frame.Elapsed / frame.Duration, frame.Percent));
                    ScaleTransform transform = (ScaleTransform)element.RenderTransform;
                    transform.ScaleX = Math.Max(transform.ScaleX + delta, 0);
                    transform.ScaleY = Math.Max(transform.ScaleY + delta, 0);
                    break;
                }
                case AnimationKind.RotateTransform: {
                    FrameworkElement element = (FrameworkElement)frame.Target!;
                    if (element.RenderTransform is not RotateTransform) {
                        if (element.RenderTransformOrigin == new Point(0, 0)) element.RenderTransformOrigin = new Point(0.5, 0.5);
                        element.RenderTransform = new RotateTransform(0);
                    }
                    double delta = AnimationMath.Lerp(0, host.Number(frame.Value), frame.Easing!.GetDelta(frame.Elapsed / frame.Duration, frame.Percent));
                    RotateTransform transform = (RotateTransform)element.RenderTransform;
                    transform.Angle += delta;
                    break;
                }
            }
            frame.Percent = frame.Elapsed / frame.Duration;
        } catch (Exception error) {
            host.Error(error, $"执行动画失败：{frame}");
        }
        return frame;
    }
}
