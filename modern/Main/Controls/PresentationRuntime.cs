using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using MeloongCore;
using MeloongCore.Extensions;
using MeloongCore.Wpf;
using MeloongCore.Wpf.Extensions;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PCLCS;
using global::PCLLF.Presentation.Controls;
using ThrottleDebounce;

namespace PCL
{

    // 仅适配 LF 平台服务；控件状态、鼠标流程和动画计划均在 C# 呈现程序集。
    internal sealed class PresentationRuntime
    {
        private static readonly global::System.Object Gate = new object();
        private static global::System.Boolean Configured;

        internal static void EnsureConfigured()
        {
            lock (global::PCL.PresentationRuntime.Gate)
            {
                if (global::PCL.PresentationRuntime.Configured)
                    return;
                var Animations = new global::PCLLF.Presentation.Controls.ControlAnimationHost<global::PCL.ModAnimation.AniData>()
                {
                    CreateColorResource = new Func<global::System.Windows.FrameworkElement, global::System.Windows.DependencyProperty, global::System.String, global::PCLLF.Presentation.Controls.ControlAnimationSpec, global::PCL.ModAnimation.AniData>((Target, Prop, Key, Spec) => global::PCL.ModAnimation.AaColor(Target, Prop, Key, Spec.Duration, Spec.Delay, global::PCL.PresentationRuntime.ToEase(Spec), Spec.After)),
                    CreateColorTo = new Func<global::System.Windows.FrameworkElement, global::System.Windows.DependencyProperty, global::PCLLF.Presentation.Controls.ControlColor, global::PCLLF.Presentation.Controls.ControlAnimationSpec, global::PCL.ModAnimation.AniData>((Target, Prop, Value, Spec) => global::PCL.ModAnimation.AaColor(Target, Prop, (new global::PCL.ModBase.MyColor(Value.A, Value.R, Value.G, Value.B)) - (((global::System.Windows.Media.Brush)(Target.GetValue(Prop)))), Spec.Duration, Spec.Delay, global::PCL.PresentationRuntime.ToEase(Spec), Spec.After)),
                    CreateScale = new Func<global::System.Windows.FrameworkElement, global::System.Double, global::PCLLF.Presentation.Controls.ControlAnimationSpec, global::System.Boolean, global::PCL.ModAnimation.AniData>((Target, Value, Spec, Absolute) => global::PCL.ModAnimation.AaScale(Target, Value, Spec.Duration, Spec.Delay, global::PCL.PresentationRuntime.ToEase(Spec), Spec.After, Absolute)),
                    CreateScaleTransform = new Func<global::System.Windows.FrameworkElement, global::System.Double, global::PCLLF.Presentation.Controls.ControlAnimationSpec, global::PCL.ModAnimation.AniData>((Target, Value, Spec) => global::PCL.ModAnimation.AaScaleTransform(Target, Value, Spec.Duration, Spec.Delay, global::PCL.PresentationRuntime.ToEase(Spec), Spec.After)),
                    ReadSpeed = new Func<global::System.Double>(() => global::PCL.ModAnimation.AniSpeed),
                    CreateWidth = new Func<global::System.Windows.FrameworkElement, global::System.Double, global::PCLLF.Presentation.Controls.ControlAnimationSpec, global::PCL.ModAnimation.AniData>((Target, Value, Spec) => global::PCL.ModAnimation.AaWidth(Target, Value, Spec.Duration, Spec.Delay, global::PCL.PresentationRuntime.ToEase(Spec), Spec.After)),
                    CreateTranslateX = new Func<global::System.Windows.FrameworkElement, global::System.Double, global::PCLLF.Presentation.Controls.ControlAnimationSpec, global::PCL.ModAnimation.AniData>((Target, Value, Spec) => global::PCL.ModAnimation.AaTranslateX(Target, Value, Spec.Duration, Spec.Delay, global::PCL.PresentationRuntime.ToEase(Spec), Spec.After)),
                    CreateTranslateY = new Func<global::System.Windows.FrameworkElement, global::System.Double, global::PCLLF.Presentation.Controls.ControlAnimationSpec, global::PCL.ModAnimation.AniData>((Target, Value, Spec) => global::PCL.ModAnimation.AaTranslateY(Target, Value, Spec.Duration, Spec.Delay, global::PCL.PresentationRuntime.ToEase(Spec), Spec.After)),
                    CreateX = new Func<global::System.Windows.FrameworkElement, global::System.Double, global::PCLLF.Presentation.Controls.ControlAnimationSpec, global::PCL.ModAnimation.AniData>((Target, Value, Spec) => global::PCL.ModAnimation.AaX(Target, Value, Spec.Duration, Spec.Delay, global::PCL.PresentationRuntime.ToEase(Spec), Spec.After)),
                    CreateY = new Func<global::System.Windows.FrameworkElement, global::System.Double, global::PCLLF.Presentation.Controls.ControlAnimationSpec, global::PCL.ModAnimation.AniData>((Target, Value, Spec) => global::PCL.ModAnimation.AaY(Target, Value, Spec.Duration, Spec.Delay, global::PCL.PresentationRuntime.ToEase(Spec), Spec.After)),
                    CreateHeight = new Func<global::System.Windows.FrameworkElement, global::System.Double, global::PCLLF.Presentation.Controls.ControlAnimationSpec, global::PCL.ModAnimation.AniData>((Target, Value, Spec) => global::PCL.ModAnimation.AaHeight(Target, Value, Spec.Duration, Spec.Delay, global::PCL.PresentationRuntime.ToEase(Spec), Spec.After)),
                    CreateOpacity = new Func<global::System.Windows.FrameworkElement, global::System.Double, global::PCLLF.Presentation.Controls.ControlAnimationSpec, global::PCL.ModAnimation.AniData>((Target, Value, Spec) => global::PCL.ModAnimation.AaOpacity(Target, Value, Spec.Duration, Spec.Delay, global::PCL.PresentationRuntime.ToEase(Spec), Spec.After)),
                    CreateRotateTransform = new Func<global::System.Windows.FrameworkElement, global::System.Double, global::PCLLF.Presentation.Controls.ControlAnimationSpec, global::PCL.ModAnimation.AniData>((Target, Value, Spec) => global::PCL.ModAnimation.AaRotateTransform(Target, Value, Spec.Duration, Spec.Delay, global::PCL.PresentationRuntime.ToEase(Spec), Spec.After)),
                    CreateDouble = new Func<global::System.Action<global::System.Double>, global::System.Double, global::PCLLF.Presentation.Controls.ControlAnimationSpec, global::PCL.ModAnimation.AniData>((Work, Value, Spec) => global::PCL.ModAnimation.AaDouble(Delta => Work(Conversions.ToDouble(Delta)), Value, Spec.Duration, Spec.Delay, global::PCL.PresentationRuntime.ToEase(Spec), Spec.After)),
                    CreateCode = new Func<global::System.Action, global::System.Int32, global::System.Boolean, global::PCL.ModAnimation.AniData>((Work, Delay, After) => global::PCL.ModAnimation.AaCode(() => Work(), Delay, After)),
                    StartFrames = new Action<global::PCL.ModAnimation.AniData[], global::System.String, global::System.Boolean>((Frames, Name, RefreshTime) => global::PCL.ModAnimation.AniStart(Frames, Name, RefreshTime)),
                    StopFrames = new Action<global::System.String>(Name => global::PCL.ModAnimation.AniStop(Name))
                };
                global::PCLLF.Presentation.Controls.ControlServices.Configure(new global::PCLLF.Presentation.Controls.ControlServiceHost()
                {
                    NewUuid = global::PCL.ModBase.GetUuid,
                    AnimationEnabled = new Func<global::System.Boolean>(() => ((global::PCL.ModAnimation.AniControlEnabled) == (0))),
                    ThemeColor = global::PCL.PresentationRuntime.ReadThemeColor,
                    Info = new Action<global::System.String>(Message => global::MeloongCore.Logger.Info(Message)),
                    Warn = new Action<global::System.Exception, global::System.String>((ErrorValue, Message) => global::MeloongCore.Logger.Warn(ErrorValue, Message)),
                    Error = new Action<global::System.Exception, global::System.String>((ErrorValue, Message) => global::MeloongCore.Logger.Error(ErrorValue, Message, global::MeloongCore.LogBehavior.Toast)),
                    RaiseCustomEvent = new Action<global::System.Windows.DependencyObject>(Control => Control.RaiseCustomEvent()),
                    Animations = Animations
                });
                global::PCL.PresentationRuntime.Configured = true;
            }
        }

        private static global::PCLLF.Presentation.Controls.ControlColor ReadThemeColor(global::PCLLF.Presentation.Controls.ControlThemeColor Kind)
        {
            global::PCL.ModBase.MyColor Value;
            switch (Kind)
            {
                case global::PCLLF.Presentation.Controls.ControlThemeColor.Gray4:
                    {
                        Value = global::PCL.ModSecret.ColorGray4;
                        break;
                    }
                case global::PCLLF.Presentation.Controls.ControlThemeColor.Gray5:
                    {
                        Value = global::PCL.ModSecret.ColorGray5;
                        break;
                    }
                case global::PCLLF.Presentation.Controls.ControlThemeColor.Accent8:
                    {
                        Value = global::PCL.ModSecret.Color8;
                        break;
                    }
                case global::PCLLF.Presentation.Controls.ControlThemeColor.SemiTransparent:
                    {
                        Value = global::PCL.ModSecret.ColorSemiTransparent;
                        break;
                    }

                default:
                    {
                        throw new global::System.ArgumentOutOfRangeException(nameof(Kind));
                    }
            }
            return new global::PCLLF.Presentation.Controls.ControlColor(Value.A, Value.R, Value.G, Value.B);
        }

        private static global::PCL.ModAnimation.AniEase ToEase(global::PCLLF.Presentation.Controls.ControlAnimationSpec Spec)
        {
            var Power = (Spec.RawPower.HasValue) ? (((global::PCL.ModAnimation.AniEasePower)(Spec.RawPower.GetValueOrDefault()))) : (global::PCL.PresentationRuntime.ToPower(Spec.Power));
            switch (Spec.Ease)
            {
                case global::PCLLF.Presentation.Controls.ControlEase.Linear:
                    {
                        return new global::PCL.ModAnimation.AniEaseLinear();
                    }
                case global::PCLLF.Presentation.Controls.ControlEase.OutFluent:
                    {
                        return new global::PCL.ModAnimation.AniEaseOutFluent(Power);
                    }
                case global::PCLLF.Presentation.Controls.ControlEase.InFluent:
                    {
                        return new global::PCL.ModAnimation.AniEaseInFluent(Power);
                    }
                case global::PCLLF.Presentation.Controls.ControlEase.OutBack:
                    {
                        return new global::PCL.ModAnimation.AniEaseOutBack(Power);
                    }
                case global::PCLLF.Presentation.Controls.ControlEase.InoutFluent:
                    {
                        return new global::PCL.ModAnimation.AniEaseInoutFluent(Power, Spec.Middle);
                    }
                case global::PCLLF.Presentation.Controls.ControlEase.InBack:
                    {
                        return new global::PCL.ModAnimation.AniEaseInBack(Power);
                    }
                case global::PCLLF.Presentation.Controls.ControlEase.OutElastic:
                    {
                        return new global::PCL.ModAnimation.AniEaseOutElastic(Power);
                    }
                case global::PCLLF.Presentation.Controls.ControlEase.OutFluentWithInitial:
                    {
                        return new global::PCL.ModAnimation.AniEaseOutFluentWithInitial(Spec.InitialSpeed, Spec.TotalSeconds, Spec.TotalDistance);
                    }

                default:
                    {
                        throw new global::System.ArgumentOutOfRangeException(nameof(Spec));
                    }
            }
        }
        private static global::PCL.ModAnimation.AniEasePower ToPower(global::PCLLF.Presentation.Controls.ControlEasePower Power)
        {
            switch (Power)
            {
                case global::PCLLF.Presentation.Controls.ControlEasePower.Weak:
                    {
                        return global::PCL.ModAnimation.AniEasePower.Weak;
                    }
                case global::PCLLF.Presentation.Controls.ControlEasePower.Middle:
                    {
                        return global::PCL.ModAnimation.AniEasePower.Middle;
                    }
                case global::PCLLF.Presentation.Controls.ControlEasePower.Strong:
                    {
                        return global::PCL.ModAnimation.AniEasePower.Strong;
                    }
                case global::PCLLF.Presentation.Controls.ControlEasePower.ExtraStrong:
                    {
                        return global::PCL.ModAnimation.AniEasePower.ExtraStrong;
                    }

                default:
                    {
                        throw new global::System.ArgumentOutOfRangeException(nameof(Power));
                    }
            }
        }
    }
}