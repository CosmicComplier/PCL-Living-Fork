using System;

namespace PCLLF.Presentation.Controls;

// A subscription owns only its bridge delegates, never the actual loader or trigger.
public sealed class LoadingTriggerSubscription : IDisposable {
    private Action? disconnect;
    public LoadingTriggerSubscription(Action disconnect) => this.disconnect = disconnect;
    public void Dispose() { if (disconnect is null) return; disconnect(); disconnect = null; }
}
public interface ILoadingViewTrigger {
    bool IsLoader { get; }
    MyLoadingView.MyLoadingState LoadingState { get; set; }
    Exception? Error { get; }
    double Progress { get; }
    IDisposable Subscribe(Action<MyLoadingView.MyLoadingState, MyLoadingView.MyLoadingState> state, Action<double, double> progress);
}
public class LoadingStateSimulatorView : ILoadingViewTrigger {
    private MyLoadingView.MyLoadingState state = MyLoadingView.MyLoadingState.Unloaded;
    public MyLoadingView.MyLoadingState LoadingState {
        get => state;
        set { if (state == value) return; MyLoadingView.MyLoadingState old = state; state = value; OnStateChanged(value, old); }
    }
    public bool IsLoader => false;
    public Exception? Error => null;
    public double Progress => 0;
    public event Action<MyLoadingView.MyLoadingState, MyLoadingView.MyLoadingState>? StateChanged;
    protected virtual void OnStateChanged(MyLoadingView.MyLoadingState next, MyLoadingView.MyLoadingState old) => StateChanged?.Invoke(next, old);
    public IDisposable Subscribe(Action<MyLoadingView.MyLoadingState, MyLoadingView.MyLoadingState> state, Action<double, double> progress) {
        StateChanged += state; return new LoadingTriggerSubscription(() => StateChanged -= state);
    }
}
