using System;
using System.Windows;
using System.Windows.Input;
using MeloongCore.Extensions;
using Microsoft.VisualBasic.CompilerServices;
using static PCL.MyLoading;
using PCLLF.Presentation.Controls;

namespace PCL
{

    public class MyLoading : MyLoadingView
    {

        static MyLoading()
        {
            PresentationRuntime.EnsureConfigured();
        }
        public static new readonly DependencyProperty ForegroundProperty = MyLoadingView.ForegroundProperty;
        public new enum MyLoadingState
        {
            Unloaded = -1,
            Run = 0,
            Stop = 1,
            Error = 2
        }
        public new event IsErrorChangedEventHandler IsErrorChanged;

        public new delegate void IsErrorChangedEventHandler(object sender, bool isError);
        public new event StateChangedEventHandler StateChanged;

        public new delegate void StateChangedEventHandler(object sender, MyLoadingState newState, MyLoadingState oldState);
        public new event ClickEventHandler Click;

        public new delegate void ClickEventHandler(object sender, MouseButtonEventArgs e);
        public new ILoadingTrigger State
        {
            get
            {
                return ((LegacyTriggerAdapter)base.State).Original;
            }
            set
            {
                base.State = value is null ? null : new LegacyTriggerAdapter(value);
            }
        }
        protected override ILoadingViewTrigger CreateDefaultState()
        {
            return new LegacyTriggerAdapter(new MyLoadingStateSimulator());
        }
        protected override void UiPost(Action Work)
        {
            ModBase.RunInUi(Work);
        }
        protected override string DisplayError(Exception ErrorValue)
        {
            return ErrorValue.GetDisplay(false);
        }
        protected override void OnStateChanged(MyLoadingView.MyLoadingState NextState, MyLoadingView.MyLoadingState OldState)
        {
            StateChanged?.Invoke(this, (MyLoadingState)(int)NextState, (MyLoadingState)(int)OldState);
        }
        protected override void OnIsErrorChanged(bool IsError)
        {
            IsErrorChanged?.Invoke(this, IsError);
        }
        protected override void OnClick(object sender, MouseButtonEventArgs Args)
        {
            Click?.Invoke(sender, Args);
        }

        private sealed class LegacyTriggerAdapter : ILoadingViewTrigger
        {
            public readonly ILoadingTrigger Original;
            public LegacyTriggerAdapter(ILoadingTrigger Value)
            {
                Original = Value;
            }
            public bool IsLoader
            {
                get
                {
                    return Original.IsLoader;
                }
            }
            public MyLoadingView.MyLoadingState LoadingState
            {
                get
                {
                    return (MyLoadingView.MyLoadingState)(int)Original.LoadingState;
                }
                set
                {
                    Original.LoadingState = (MyLoadingState)(int)value;
                }
            }
            public Exception Error
            {
                get
                {
                    return (Exception)NewLateBinding.LateGet(Original, null, "Error", Array.Empty<object>(), null, null, null);
                }
            }
            public double Progress
            {
                get
                {
                    return Conversions.ToDouble(NewLateBinding.LateGet(Original, null, "Progress", Array.Empty<object>(), null, null, null));
                }
            }
            public IDisposable Subscribe(Action<MyLoadingView.MyLoadingState, MyLoadingView.MyLoadingState> StateCallback, Action<double, double> ProgressCallback)
            {
                ILoadingTrigger.LoadingStateChangedEventHandler StateHandler = (NextState, OldState) => StateCallback((MyLoadingView.MyLoadingState)(int)NextState, (MyLoadingView.MyLoadingState)(int)OldState);
                ILoadingTrigger.ProgressChangedEventHandler ProgressHandler = (NextValue, OldValue) => ProgressCallback(NextValue, OldValue);
                Original.LoadingStateChanged += StateHandler;
                Original.ProgressChanged += ProgressHandler;
                return new LoadingTriggerSubscription(() =>
                    {
                        Original.LoadingStateChanged -= StateHandler;
                        Original.ProgressChanged -= ProgressHandler;
                    });
            }
        }
    }

    public interface ILoadingTrigger
    {
        bool IsLoader { get; }
        MyLoadingState LoadingState { get; set; }
        event LoadingStateChangedEventHandler LoadingStateChanged;

        delegate void LoadingStateChangedEventHandler(MyLoadingState NewState, MyLoadingState OldState);
        event ProgressChangedEventHandler ProgressChanged;

        delegate void ProgressChangedEventHandler(double NewProgress, double OldProgress);
    }

    public class MyLoadingStateSimulator : LoadingStateSimulatorView, ILoadingTrigger
    {
        public new MyLoadingState LoadingState
        {
            get
            {
                return (MyLoadingState)(int)base.LoadingState;
            }
            set
            {
                base.LoadingState = (MyLoadingView.MyLoadingState)(int)value;
            }
        }
        public new bool IsLoader
        {
            get
            {
                return base.IsLoader;
            }
        }
        public event ILoadingTrigger.LoadingStateChangedEventHandler LoadingStateChanged;
        public event ILoadingTrigger.ProgressChangedEventHandler ProgressChanged;
        protected override void OnStateChanged(MyLoadingView.MyLoadingState NextState, MyLoadingView.MyLoadingState OldState)
        {
            LoadingStateChanged?.Invoke((MyLoadingState)(int)NextState, (MyLoadingState)(int)OldState);
        }
    }
}