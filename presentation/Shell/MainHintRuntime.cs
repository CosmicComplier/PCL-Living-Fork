using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MeloongCore;
using MeloongCore.Extensions;
using Microsoft.VisualBasic.CompilerServices;
using PCLLF.Presentation.Controls;
using PCLLF.Presentation.Foundation;

namespace PCLLF.Presentation.Shell;

public struct MainHintData<TKind> {
    public string Text;
    public TKind Type;
    public bool Log;
}

public sealed class MainHintHost<TKind> {
    public Func<ConcurrentQueue<MainHintData<TKind>>> Queue = null!;
    public Func<Panel> Panel = null!;
    public Func<int> NewUuid = null!;
    public Func<ControlAnimations> Animations = null!;
    public Func<string, bool> IsAnimationRunning = null!;
    public TKind Blue = default!, Green = default!;
}

/// <summary>The original queue, duplicate bounce and enter/hide animation plans, on the actual hint panel.</summary>
public sealed class MainHintRuntime<TKind> {
    private readonly MainHintHost<TKind> host;
    public MainHintRuntime(MainHintHost<TKind> host) { this.host = host; }
    public void Enqueue(string? text, TKind type, bool log = true) =>
        host.Queue().Enqueue(new MainHintData<TKind> { Text = text ?? "", Type = type, Log = log });

    public void Tick() {
        try {
            while (host.Queue().TryDequeue(out MainHintData<TKind> current)) {
                current.Text = StringExtensions.ReplaceLineEndings(current.Text, " ", mergeMultiple: true);
                if (host.Panel().Children.Count < 20) Show(current);
                if (current.Log) Logger.Info($"弹出提示：{current.Text}");
            }
        } catch (Exception ex) { Logger.Info(ex, "显示弹出提示失败"); }
    }

    private void Show(MainHintData<TKind> current) {
        Border? duplicate = null;
        foreach (Border stack in host.Panel().Children) {
            if (Conversions.ToBoolean(((object[])stack.Tag)[0]) && ((TextBlock)stack.Child).Text == current.Text) duplicate = stack;
        }
        ControlColor target0, target1;
        double percent = .3;
        if (EqualityComparer<TKind>.Default.Equals(current.Type, host.Blue)) {
            target0 = new ControlColor(215, 37, 155, 252); target1 = new ControlColor(215, 10, 142, 252);
        } else if (EqualityComparer<TKind>.Default.Equals(current.Type, host.Green)) {
            target0 = new ControlColor(215, 33, 177, 33); target1 = new ControlColor(215, 29, 160, 29);
        } else {
            target0 = new ControlColor(215, 255, 53, 11); target1 = new ControlColor(215, 255, 43, 0);
        }
        ControlAnimations animation = host.Animations();
        if (duplicate is not null) {
            Border control = duplicate;
            if (!host.IsAnimationRunning("Hint Show " + Id(control))) {
                animation.Stop("Hint Hide " + Id(control));
                double delay = (800 + Math.Min(23, Math.Max(5, current.Text.Length)) * 180) * animation.Speed;
                animation.Start(new[] {
                    animation.X(control, -12 - control.Margin.Left, new(50, ease: ControlEase.OutFluent)),
                    animation.X(control, -8, new(50, 50, ControlEase.InFluent)),
                    animation.X(control, 8, new(50, 100, ControlEase.OutFluent)),
                    animation.X(control, -8, new(50, 150, ControlEase.InFluent)),
                    animation.Double(delta => { percent += delta; Recolor(control, target0, target1, percent); }, .7, new(250)),
                    animation.X(control, -50, new(200, delay, ControlEase.InFluent)),
                    animation.Opacity(control, -1, new(150, delay)),
                    animation.Code(() => ((object[])control.Tag)[0] = false, Convert.ToInt32(delay)),
                    animation.Height(control, -26, new(100, ease: ControlEase.OutFluent, after: true)),
                    animation.Code(() => host.Panel().Children.Remove(control), after: true)
                }, "Hint Hide " + Id(control));
            }
            return;
        }
        Border next = new() {
            Tag = new object[] { true, host.NewUuid() }, Margin = new Thickness(-70, 0, 20, 0), Opacity = 0, Height = 0,
            HorizontalAlignment = HorizontalAlignment.Left, CornerRadius = new CornerRadius(0, 6, 6, 0),
            Background = new LinearGradientBrush(new GradientStopCollection(new List<GradientStop> {
                new(Blend(target0, percent), 0), new(Blend(target1, percent), 1)
            }), 90),
            Child = new TextBlock {
                TextTrimming = TextTrimming.CharacterEllipsis, FontSize = 13, Text = current.Text,
                Foreground = new SolidColorBrush(Colors.White), Margin = new Thickness(33, 5, 8, 5)
            }
        };
        host.Panel().Children.Add(next);
        List<ControlAnimation> animations = new();
        if (host.Panel().Children.Count > 1) animations.Add(animation.Height(next, 26, new(150, ease: ControlEase.OutFluent)));
        else next.Height = 26;
        animations.AddRange(new[] {
            animation.X(next, 30, new(400, ease: ControlEase.OutElastic, power: ControlEasePower.Weak)),
            animation.X(next, 20, new(200, ease: ControlEase.OutFluent)),
            animation.Opacity(next, 1, new(100)),
            animation.Double(delta => { percent += delta; Recolor(next, target0, target1, percent); }, .7, new(250, 100))
        });
        animation.Start(animations, "Hint Show " + Id(next));
        double endDelay = (800 + Math.Min(23, Math.Max(5, current.Text.Length)) * 180) * animation.Speed;
        animation.Start(new[] {
            animation.X(next, -50, new(200, endDelay, ControlEase.InFluent)),
            animation.Opacity(next, -1, new(150, endDelay)),
            animation.Code(() => ((object[])next.Tag)[0] = false, Convert.ToInt32(endDelay)),
            animation.Height(next, -26, new(100, ease: ControlEase.OutFluent, after: true)),
            animation.Code(() => host.Panel().Children.Remove(next), after: true)
        }, "Hint Hide " + Id(next));
    }
    private static string Id(Border control) => Conversions.ToString(((object[])control.Tag)[1])!;
    private static Color Blend(ControlColor color, double percent) {
        ControlColor background = LegacyThemePalette.Color("ColorBrushWhite");
        return Color.FromArgb(ControlColor.Byte(color.A * percent + background.A * (1 - percent)),
            ControlColor.Byte(color.R * percent + background.R * (1 - percent)),
            ControlColor.Byte(color.G * percent + background.G * (1 - percent)),
            ControlColor.Byte(color.B * percent + background.B * (1 - percent)));
    }
    private static void Recolor(Border control, ControlColor first, ControlColor second, double percent) {
        LinearGradientBrush brush = (LinearGradientBrush)control.Background;
        brush.GradientStops[0].Color = Blend(first, percent);
        brush.GradientStops[1].Color = Blend(second, percent);
    }
    public void HideAll() {
        foreach (Border control in host.Panel().Children) {
            control.IsHitTestVisible = false;
            ControlAnimations animation = host.Animations();
            animation.Start(new[] {
                animation.X(control, -50, new(200, ease: ControlEase.InFluent)),
                animation.Opacity(control, -1, new(150, ease: ControlEase.InFluent)),
                animation.Code(() => ((object[])control.Tag)[0] = false),
                animation.Height(control, -26, new(100, ease: ControlEase.OutFluent, after: true)),
                animation.Code(() => host.Panel().Children.Remove(control), after: true)
            }, "Hint Hide " + Id(control));
        }
    }
}
