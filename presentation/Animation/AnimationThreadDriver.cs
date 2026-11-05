using System;
using System.Collections.Generic;
using System.Threading;

namespace PCLLF.Presentation.Animation;

// LF Modules/Base/ModAnimation.vb: same clock, foreground worker and synchronous UI cadence.
public sealed partial class AnimationEngine<TData, TGroup>
    where TData : struct, IAnimationData where TGroup : class, IAnimationGroup<TData> {
    private int fpsCounter;
    private long fpsTimer;
    private int controlEnabled;
    private readonly object controlEnabledLock = new();

    public double AniSpeed {
        get {
            int setting = host.AnimationSetting();
            return setting >= 30 ? 200 : AnimationMath.Clamp(setting * 0.1 + 0.1, 0.1, 3);
        }
    }
    public int AniControlEnabled {
        get => controlEnabled;
        set { lock (controlEnabledLock) controlEnabled = value; }
    }

    public void AniStart() {
        host.WriteLastTick(host.Clock());
        fpsTimer = host.ReadLastTick();
        host.WriteRunning(true);
        host.StartThread(() => {
            try {
                host.Info("动画线程开始");
                while (true) {
                    long deltaTime = AnimationMath.Clamp(checked(host.Clock() - host.ReadLastTick()), 0L, 100000L);
                    if (deltaTime >= 3) {
                        host.WriteLastTick(host.Clock());
                        if (host.DebugMode()) {
                            if (AnimationMath.Clamp(checked(host.ReadLastTick() - fpsTimer), 0L, 100000L) >= 500) {
                                host.WriteFps(fpsCounter);
                                fpsCounter = 0;
                                fpsTimer = host.ReadLastTick();
                            }
                            fpsCounter = checked(fpsCounter + 2);
                        }
                        host.UiWait(() => {
                            frameCount = 0;
                            AniTimer(deltaTime * AniSpeed);
                            if (host.RandomInteger(0, 64 * (host.DebugMode() ? 5 : 30)) == 0 &&
                                ((host.ReadFps() < 62 && host.ReadFps() > 0) || frameCount > 4 || host.DownloadRemaining() != 0))
                                host.ReportDiagnostics(host.ReadFps(), frameCount);
                        });
                    }
                    host.Sleep(1);
                }
            } catch (Exception error) { host.Fatal(error, "动画帧执行失败"); }
        }, "Animation", ThreadPriority.AboveNormal);
    }
}
