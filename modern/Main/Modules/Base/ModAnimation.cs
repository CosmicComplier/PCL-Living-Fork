using System;
// LF 动画模块的原调用面；调度、帧执行、工厂及缓动计算均由 C# 呈现程序集实现。
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using MeloongCore;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using PCLLF.Presentation.Animation;
using CoreAnimationEase = PCLLF.Presentation.Animation.AnimationEase;
using PCLLF.Presentation.Controls;

namespace PCL
{

    public static partial class ModAnimation
    {

        // 这些字段是原调用者与 C# 引擎共同使用的实际状态，不复制字典、动画或组列表。
        public static Dictionary<string, AniGroupEntry> AniGroups = new Dictionary<string, AniGroupEntry>();
        private static long AniLastTick;
        public static bool AniRunning = false;
        public static int AniFPS = 0;
        private readonly static AnimationEngine<AniData, AniGroupEntry> LegacySharedService = new AnimationEngine<AniData, AniGroupEntry>(CreateHost(), () => AniGroups, (Data, Tick) => new AniGroupEntry() { Data = Data, StartTick = Tick });
        private readonly static AnimationFactories<AniData> Factories = new AnimationFactories<AniData>(LegacySharedService.Host);

        public static double AniSpeed
        {
            get
            {
                return LegacySharedService.AniSpeed;
            }
        }
        public static int AniControlEnabled
        {
            get
            {
                return LegacySharedService.AniControlEnabled;
            }
            set
            {
                LegacySharedService.AniControlEnabled = value;
            }
        }

        public class AniGroupEntry : IAnimationGroup<AniData>
        {

            public List<AniData> Data;
            public long StartTick;
            public int Uuid = ModBase.GetUuid();

            private List<AniData> CoreFrames
            {
                get
                {
                    return Data;
                }
                set
                {
                    Data = value;
                }
            }

            List<AniData> IAnimationGroup<AniData>.Frames { get => CoreFrames; set => CoreFrames = value; }
            private long CoreTick
            {
                get
                {
                    return StartTick;
                }
                set
                {
                    StartTick = value;
                }
            }

            long IAnimationGroup<AniData>.Tick { get => CoreTick; set => CoreTick = value; }
            private int CoreId
            {
                get
                {
                    return Uuid;
                }
                set
                {
                    Uuid = value;
                }
            }

            int IAnimationGroup<AniData>.Id { get => CoreId; set => CoreId = value; }
        }

        public struct AniData : IAnimationData
        {

            public AniType TypeMain;
            public AniTypeSub TypeSub;
            public int TimeTotal;
            public double TimeFinished;
            public double TimePercent;
            public bool IsAfter;
            public AniEase Ease;
            public object Obj;
            public object Value;
            public object ValueLast;

            private AnimationKind CoreKind
            {
                get
                {
                    return (AnimationKind)(int)TypeMain;
                }
                set
                {
                    TypeMain = (AniType)(int)value;
                }
            }

            AnimationKind IAnimationData.Kind { get => CoreKind; set => CoreKind = value; }
            private AnimationSubKind CoreSubKind
            {
                get
                {
                    return (AnimationSubKind)(int)TypeSub;
                }
                set
                {
                    TypeSub = (AniTypeSub)(int)value;
                }
            }

            AnimationSubKind IAnimationData.SubKind { get => CoreSubKind; set => CoreSubKind = value; }
            private int CoreDuration
            {
                get
                {
                    return TimeTotal;
                }
                set
                {
                    TimeTotal = value;
                }
            }

            int IAnimationData.Duration { get => CoreDuration; set => CoreDuration = value; }
            private double CoreElapsed
            {
                get
                {
                    return TimeFinished;
                }
                set
                {
                    TimeFinished = value;
                }
            }

            double IAnimationData.Elapsed { get => CoreElapsed; set => CoreElapsed = value; }
            private double CorePercent
            {
                get
                {
                    return TimePercent;
                }
                set
                {
                    TimePercent = value;
                }
            }

            double IAnimationData.Percent { get => CorePercent; set => CorePercent = value; }
            private bool CoreAfter
            {
                get
                {
                    return IsAfter;
                }
                set
                {
                    IsAfter = value;
                }
            }

            bool IAnimationData.After { get => CoreAfter; set => CoreAfter = value; }
            private CoreAnimationEase CoreEasing
            {
                get
                {
                    return Ease;
                }
                set
                {
                    Ease = (AniEase)value;
                }
            }

            CoreAnimationEase IAnimationData.Easing { get => CoreEasing; set => CoreEasing = value; }
            private object CoreTarget
            {
                get
                {
                    return Obj;
                }
                set
                {
                    Obj = value;
                }
            }

            object IAnimationData.Target { get => CoreTarget; set => CoreTarget = value; }
            private object CoreValue
            {
                get
                {
                    return Value;
                }
                set
                {
                    Value = value;
                }
            }

            object IAnimationData.Value { get => CoreValue; set => CoreValue = value; }
            private object CoreRemainder
            {
                get
                {
                    return ValueLast;
                }
                set
                {
                    ValueLast = value;
                }
            }

            object IAnimationData.Remainder { get => CoreRemainder; set => CoreRemainder = value; }

            public override string ToString()
            {
                return TypeMain.ToString() + " | " + TimeFinished + "/" + TimeTotal + "(" + Math.Round(TimePercent * 100d) + "%)" + (Obj is null ? "" : " | " + Obj.ToString() + "(" + Obj.GetType().Name + ")");
            }
        }

        public enum AniType
        {
            Number,
            Color,
            Scale,
            TextAppear,
            Code,
            ScaleTransform,
            RotateTransform
        }
        public enum AniTypeSub
        {
            X,
            Y,
            Width,
            Height,
            Opacity,
            Value,
            Radius,
            BorderThickness,
            StrokeThickness,
            TranslateX,
            TranslateY,
            Double,
            DoubleParam,
            GridLengthWidth
        }

        public static AniData AaX(object Obj, double Value, int Time = 400, int Delay = 0, AniEase Ease = null, bool After = false)
        {
            return Factories.AaX(Obj, Value, Time, Delay, Ease, After);
        }

        public static AniData AaY(object Obj, double Value, int Time = 400, int Delay = 0, AniEase Ease = null, bool After = false)
        {
            return Factories.AaY(Obj, Value, Time, Delay, Ease, After);
        }

        public static AniData AaWidth(object Obj, double Value, int Time = 400, int Delay = 0, AniEase Ease = null, bool After = false)
        {
            return Factories.AaWidth(Obj, Value, Time, Delay, Ease, After);
        }

        public static AniData AaHeight(object Obj, double Value, int Time = 400, int Delay = 0, AniEase Ease = null, bool After = false)
        {
            return Factories.AaHeight(Obj, Value, Time, Delay, Ease, After);
        }

        public static AniData AaOpacity(object Obj, double Value, int Time = 400, int Delay = 0, AniEase Ease = null, bool After = false)
        {
            return Factories.AaOpacity(Obj, Value, Time, Delay, Ease, After);
        }

        public static AniData AaValue(object Obj, double Value, int Time = 400, int Delay = 0, AniEase Ease = null, bool After = false)
        {
            return Factories.AaValue(Obj, Value, Time, Delay, Ease, After);
        }

        public static AniData AaRadius(object Obj, double Value, int Time = 400, int Delay = 0, AniEase Ease = null, bool After = false)
        {
            return Factories.AaRadius(Obj, Value, Time, Delay, Ease, After);
        }

        public static AniData AaBorderThickness(object Obj, double Value, int Time = 400, int Delay = 0, AniEase Ease = null, bool After = false)
        {
            return Factories.AaBorderThickness(Obj, Value, Time, Delay, Ease, After);
        }

        public static AniData AaStrokeThickness(object Obj, double Value, int Time = 400, int Delay = 0, AniEase Ease = null, bool After = false)
        {
            return Factories.AaStrokeThickness(Obj, Value, Time, Delay, Ease, After);
        }

        public static AniData AaGridLengthWidth(object Obj, double Value, int Time = 400, int Delay = 0, AniEase Ease = null, bool After = false)
        {
            return Factories.AaGridLengthWidth(Obj, Value, Time, Delay, Ease, After);
        }

        public static AniData AaDouble(object Obj, DependencyProperty Prop, double Value, int Time = 400, int Delay = 0, AniEase Ease = null, bool After = false)
        {
            return Factories.AaDouble(Obj, Prop, Value, Time, Delay, Ease, After);
        }

        public static AniData AaDouble(ParameterizedThreadStart Lambda, double Value, int Time = 400, int Delay = 0, AniEase Ease = null, bool After = false)
        {
            return Factories.AaDouble(Lambda, Value, Time, Delay, Ease, After);
        }

        public static AniData AaColor(FrameworkElement Obj, DependencyProperty Prop, ModBase.MyColor Value, int Time = 400, int Delay = 0, AniEase Ease = null, bool After = false)
        {
            return Factories.AaColor(Obj, Prop, Value, Time, Delay, Ease, After);
        }

        public static AniData AaColor(FrameworkElement Obj, DependencyProperty Prop, string Res, int Time = 400, int Delay = 0, AniEase Ease = null, bool After = false)
        {
            return Factories.AaColor(Obj, Prop, Res, Time, Delay, Ease, After);
        }

        public static AniData AaScale(object Obj, double Value, int Time = 400, int Delay = 0, AniEase Ease = null, bool After = false, bool Absolute = false)
        {
            return Factories.AaScale(Obj, Value, Time, Delay, Ease, After, Absolute);
        }

        public static AniData AaTextAppear(object Obj, bool Hide = false, bool TimePerText = true, int Time = 70, int Delay = 0, AniEase Ease = null, bool After = false)
        {
            return Factories.AaTextAppear(Obj, Hide, TimePerText, Time, Delay, Ease, After);
        }

        public static AniData AaCode(ThreadStart Code, int Delay = 0, bool After = false)
        {
            return Factories.AaCode(Code, Delay, After);
        }

        public static AniData AaScaleTransform(object Obj, double Value, int Time = 400, int Delay = 0, AniEase Ease = null, bool After = false)
        {
            return Factories.AaScaleTransform(Obj, Value, Time, Delay, Ease, After);
        }

        public static AniData AaRotateTransform(object Obj, double Value, int Time = 400, int Delay = 0, AniEase Ease = null, bool After = false)
        {
            return Factories.AaRotateTransform(Obj, Value, Time, Delay, Ease, After);
        }

        public static AniData AaTranslateX(object Obj, double Value, int Time = 400, int Delay = 0, AniEase Ease = null, bool After = false)
        {
            return Factories.AaTranslateX(Obj, Value, Time, Delay, Ease, After);
        }

        public static AniData AaTranslateY(object Obj, double Value, int Time = 400, int Delay = 0, AniEase Ease = null, bool After = false)
        {
            return Factories.AaTranslateY(Obj, Value, Time, Delay, Ease, After);
        }

        public static List<AniData> AaStack(StackPanel Stack, int Time = 100, int Delay = 25)
        {
            return Factories.AaStack(Stack, Time, Delay);
        }

        public enum AniEasePower : int
        {
            Weak = 2,
            Middle = 3,
            Strong = 4,
            ExtraStrong = 5
        }
        public abstract class AniEase : CoreAnimationEase
        {

            public abstract override double GetValue(double t);
            public override double GetDelta(double t1, double t0)
            {
                return base.GetDelta(t1, t0);
            }
        }
        public class AniEaseInout : AniEase
        {
            private readonly AnimationEaseInout Core;
            public AniEaseInout(AniEase EaseIn, AniEase EaseOut, double EaseInPercent = 0.5d)
            {
                Core = new AnimationEaseInout(EaseIn, EaseOut, EaseInPercent);
            }
            public override double GetValue(double t)
            {
                return Core.GetValue(t);
            }
        }
        public class AniEaseLinear : AniEase
        {
            private readonly AnimationEaseLinear Core = new AnimationEaseLinear();
            public override double GetValue(double t)
            {
                return Core.GetValue(t);
            }
            public override double GetDelta(double t1, double t0)
            {
                return Core.GetDelta(t1, t0);
            }
        }
        public class AniEaseInFluent : AniEase
        {
            private readonly AnimationEaseInFluent Core;
            public AniEaseInFluent(AniEasePower Power = AniEasePower.Middle)
            {
                Core = new AnimationEaseInFluent((AnimationEasePower)(int)Power);
            }
            public override double GetValue(double t)
            {
                return Core.GetValue(t);
            }
        }
        public class AniEaseOutFluent : AniEase
        {
            private readonly AnimationEaseOutFluent Core;
            public AniEaseOutFluent(AniEasePower Power = AniEasePower.Middle)
            {
                Core = new AnimationEaseOutFluent((AnimationEasePower)(int)Power);
            }
            public override double GetValue(double t)
            {
                return Core.GetValue(t);
            }
        }
        public class AniEaseInoutFluent : AniEase
        {
            private readonly AnimationEaseInoutFluent Core;
            public AniEaseInoutFluent(AniEasePower Power = AniEasePower.Middle, double Middle = 0.5d)
            {
                Core = new AnimationEaseInoutFluent((AnimationEasePower)(int)Power, Middle);
            }
            public override double GetValue(double t)
            {
                return Core.GetValue(t);
            }
        }
        public class AniEaseOutFluentWithInitial : AniEase
        {
            private readonly AnimationEaseOutFluentWithInitial Core;
            public AniEaseOutFluentWithInitial(double InitialPixelPerSecond, double TotalSecond, double TotalDistance)
            {
                Core = new AnimationEaseOutFluentWithInitial(InitialPixelPerSecond, TotalSecond, TotalDistance);
            }
            public override double GetValue(double percent)
            {
                return Core.GetValue(percent);
            }
        }
        public class AniEaseInBack : AniEase
        {
            private readonly AnimationEaseInBack Core;
            public AniEaseInBack(AniEasePower Power = AniEasePower.Middle)
            {
                Core = new AnimationEaseInBack((AnimationEasePower)(int)Power);
            }
            public override double GetValue(double t)
            {
                return Core.GetValue(t);
            }
        }
        public class AniEaseOutBack : AniEase
        {
            private readonly AnimationEaseOutBack Core;
            public AniEaseOutBack(AniEasePower Power = AniEasePower.Middle)
            {
                Core = new AnimationEaseOutBack((AnimationEasePower)(int)Power);
            }
            public override double GetValue(double t)
            {
                return Core.GetValue(t);
            }
        }
        public class AniEaseInCar : AniEase
        {
            private readonly AnimationEaseInCar Core;
            public AniEaseInCar(double Middle = 0.7d, AniEasePower Power = AniEasePower.Middle)
            {
                Core = new AnimationEaseInCar(Middle, (AnimationEasePower)(int)Power);
            }
            public override double GetValue(double t)
            {
                return Core.GetValue(t);
            }
        }
        public class AniEaseOutCar : AniEase
        {
            private readonly AnimationEaseOutCar Core;
            public AniEaseOutCar(double Middle = 0.3d, AniEasePower Power = AniEasePower.Middle)
            {
                Core = new AnimationEaseOutCar(Middle, (AnimationEasePower)(int)Power);
            }
            public override double GetValue(double t)
            {
                return Core.GetValue(t);
            }
        }
        public class AniEaseInElastic : AniEase
        {
            private readonly AnimationEaseInElastic Core;
            public AniEaseInElastic(AniEasePower Power = AniEasePower.Middle)
            {
                Core = new AnimationEaseInElastic((AnimationEasePower)(int)Power);
            }
            public override double GetValue(double t)
            {
                return Core.GetValue(t);
            }
        }
        public class AniEaseOutElastic : AniEase
        {
            private readonly AnimationEaseOutElastic Core;
            public AniEaseOutElastic(AniEasePower Power = AniEasePower.Middle)
            {
                Core = new AnimationEaseOutElastic((AnimationEasePower)(int)Power);
            }
            public override double GetValue(double t)
            {
                return Core.GetValue(t);
            }
        }

        public static void AniStart(IList AniGroup, string Name = "", bool RefreshTime = false)
        {
            LegacySharedService.AniStart(AniGroup, Name, RefreshTime);
        }
        public static void AniStart(AniData AniGroup, string Name = "", bool RefreshTime = false)
        {
            LegacySharedService.AniStart(AniGroup, Name, RefreshTime);
        }
        public static void AniStop(string Name)
        {
            LegacySharedService.AniStop(Name);
        }
        public static bool AniIsRun(string Name)
        {
            return LegacySharedService.AniIsRun(Name);
        }
        public static void AniStart()
        {
            LegacySharedService.AniStart();
        }
        public static void AniTimer(double DeltaTick)
        {
            LegacySharedService.AniTimer(DeltaTick);
        }

        // 平台原语及兼容数据装箱；C# 不引用启动器主程序集。
        private static AnimationHost CreateHost()
        {
            return new AnimationHost()
            {
                Clock = ModBase.GetTimeMs,
                ReadLastTick = new Func<long>(() => AniLastTick),
                WriteLastTick = new Action<long>(Value => AniLastTick = Value),
                ReadRunning = new Func<bool>(() => AniRunning),
                WriteRunning = new Action<bool>(Value => AniRunning = Value),
                ReadFps = new Func<int>(() => AniFPS),
                WriteFps = new Action<int>(Value => AniFPS = Value),
                AnimationSetting = new Func<int>(() => Settings.Get<int>("SystemDebugAnim")),
                DebugMode = new Func<bool>(() => ModBase.ModeDebug),
                DownloadRemaining = new Func<int>(() => ModNet.NetManager.FileRemain),
                UiWait = new Action<Action>(Work => ModBase.RunInUiWait(Work)),
                StartThread = new Action<Action, string, ThreadPriority>((Work, Name, Priority) => ModBase.RunInNewThread(Work, Name, Priority)),
                Sleep = new Action<int>(Time => Thread.Sleep(Time)),
                Info = new Action<string>(Message => Logger.Info(Message)),
                Error = new Action<Exception, string>((ErrorValue, Message) => Logger.Error(ErrorValue, Message, LogBehavior.Toast)),
                Fatal = new Action<Exception, string>((ErrorValue, Message) => Logger.Error(ErrorValue, Message, LogBehavior.AlertThenCrash)),
                ReportDiagnostics = ReportDiagnostics,
                NewUuid = ModBase.GetUuid,
                RandomInteger = new Func<int, int, int>((Minimum, Maximum) => ModBase.RandomInteger(Minimum, Maximum)),
                DecodeText = new Func<byte[], string>(Bytes => Encoding.GetEncoding("GB18030").GetString(Bytes)),
                Radix86 = new Func<int, string>(Value => Value.ConvertRadix(86)),
                NewLinearEasing = new Func<CoreAnimationEase>(() => new AniEaseLinear()),
                FindResource = new Func<string, object>(Key => System.Windows.Application.Current.FindResource(Key)),
                Number = new Func<object, double>(Value => Conversions.ToDouble(Value)),
                GetProperty = new Func<object, string, object>((Target, Name) => Interaction.CallByName(Target, Name, CallType.Get)),
                SetProperty = new Action<object, string, object>((Target, Name, Value) => Interaction.CallByName(Target, Name, CallType.Let, Value)),
                ReadColor = ReadAnimationColor,
                BoxColor = new Func<ControlColor, object>(Value => new ModBase.MyColor(Value.A, Value.R, Value.G, Value.B)),
                ReadRect = ReadAnimationRect,
                BoxRect = new Func<AnimationRect, object>(Value => new ModBase.MyRect(Value.Left, Value.Top, Value.Width, Value.Height))
            };
        }

        private static void ReportDiagnostics(int Fps, int Count)
        {
            Logger.Info($"FPS {Fps}, 动画 {Count}, 下载中 {ModNet.NetManager.FileRemain}（{StringUtils.FormatByteSize(ModNet.NetManager.Speed)}/s）");
            if (ModNet.NetManager.FileRemain < 5)
            {
                foreach (var File in ModNet.NetManager.AllFiles.Values)
                {
                    if (File.State < ModNet.NetState.Finished)
                        Logger.Trace($"- {File.State} {StringUtils.FormatByteSize(File.Speed)}/s {File.LocalPath}");
                }
            }
        }
        private static ControlColor ReadAnimationColor(object Value)
        {
            var Color = new ModBase.MyColor(Value);
            return new ControlColor(Color.A, Color.R, Color.G, Color.B);
        }
        private static AnimationRect ReadAnimationRect(object Value)
        {
            if (Value is ModBase.MyRect)
            {
                ModBase.MyRect Rect = (ModBase.MyRect)Value;
                return new AnimationRect(Rect.Left, Rect.Top, Rect.Width, Rect.Height);
            }
            return new AnimationRect(Conversions.ToDouble(Interaction.CallByName(Value, "Left", CallType.Get)), Conversions.ToDouble(Interaction.CallByName(Value, "Top", CallType.Get)), Conversions.ToDouble(Interaction.CallByName(Value, "Width", CallType.Get)), Conversions.ToDouble(Interaction.CallByName(Value, "Height", CallType.Get)));
        }
    }
}