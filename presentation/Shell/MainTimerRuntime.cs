using System;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MeloongCore;

namespace PCLLF.Presentation.Shell;

public sealed class MainTimerHost {
    public Action HintTick = null!, MessageTick = null!, DragTick = null!, TaskbarRefresh = null!;
    public Action<Action> Ui = null!, UiWait = null!;
    public Action<Action, string> StartThread = null!;
    public Func<Window> Window = null!;
    public Func<bool> Hidden = null!, ShowAprilButton = null!;
    public Action AprilRibble = null!;
    public Func<int> AprilYear = null!;
    public Func<bool> AprilGiveUp = null!, HasLaunchPage = null!, IsLaunchPage = null!;
    public Func<int> AnimationControl = null!;
    public Func<MouseEventArgs?> LastMouse = null!;
    public Func<FrameworkElement> LaunchButton = null!, MainPanel = null!, FormPanel = null!;
    public Func<TranslateTransform?> AprilPosition = null!;
    public Func<ScaleTransform> AprilScale = null!;
    public Func<int, int, int> RandomInteger = null!;
    public Action<string> RedHint = null!;
}

public sealed class MainTimerRuntime {
    private readonly MainTimerHost host;
    private int timer150Count;
    private bool? aprilEnabled;
    private int difficultyDistance, idleCount;
    private Point lastMousePosition = new(0, 0);
    private Vector speed = new(0, 0);
    private double giveUpDistance = -1500;
    public MainTimerRuntime(MainTimerHost host) { this.host = host; }
    public bool IsAprilEnabled {
        get {
            if (aprilEnabled is null) aprilEnabled = DateTime.Now.Month == 4 && DateTime.Now.Day == 1 && host.AprilYear() != DateTime.Now.Year;
            return aprilEnabled.Value;
        }
    }
    public void Tick() {
        try {
            host.HintTick(); host.MessageTick(); host.DragTick(); host.TaskbarRefresh();
        } catch (Exception ex) { Logger.Error(ex, "短程主时钟执行异常", LogBehavior.AlertThenCrash); }
        timer150Count = checked(timer150Count + 1);
        if (timer150Count != 150) return;
        timer150Count = 0;
        try {
            if (host.ShowAprilButton() && difficultyDistance != 0) host.AprilRibble();
            host.Ui(() => {
                if (host.Hidden()) return;
                if (host.Window().Top < -9000) host.Window().Top = 100;
                if (host.Window().Left < -9000) host.Window().Left = 100;
            });
        } catch (Exception ex) { Logger.Error(ex, "长程主时钟执行异常", LogBehavior.AlertThenCrash); }
    }

    public void FoolTick() {
        try {
            if (!host.HasLaunchPage() || host.AprilPosition() is null || host.LastMouse() is null) return;
            if (host.AprilGiveUp() || !host.IsLaunchPage() || host.AnimationControl() != 0 || !host.LaunchButton().IsLoaded) return;
            Point mouse = host.LastMouse()!.GetPosition(host.Window());
            if (mouse == lastMousePosition) idleCount = checked(idleCount + 1);
            else { lastMousePosition = mouse; idleCount = 0; }
            double buttonWidth = host.LaunchButton().ActualWidth / 2, buttonHeight = host.LaunchButton().ActualHeight / 2;
            Vector vector = (Vector)host.LastMouse()!.GetPosition(host.LaunchButton()) - new Vector(buttonWidth, buttonHeight);
            Vector direction = new(vector.X, vector.Y);
            direction.Normalize(); direction = -direction;
            double distance = new Vector(Math.Max(0, Math.Abs(vector.X) - buttonWidth), Math.Max(0, Math.Abs(vector.Y) - buttonHeight)).Length;
            // The original unused breath calculation is kept (its evaluation has no host callbacks).
            _ = Math.Sin(timer150Count / 37.5 * Math.PI);
            double difficulty = 1 / (1 + Math.Pow(difficultyDistance / 6000.0, 2)) + .25;
            Vector acceleration = Math.Max(0, -.65 - Math.Log((distance / difficulty + .4) / 200)) * direction * difficulty;
            if (idleCount >= 64 * 5) {
                Vector safeDistance = (Vector)host.LastMouse()!.GetPosition(host.MainPanel()) - new Vector(buttonWidth, host.MainPanel().ActualHeight - buttonHeight * 3);
                Vector back = new(host.AprilPosition()!.X, host.AprilPosition()!.Y);
                if (safeDistance.Length > 250 && back.Length > .4) {
                    acceleration -= back * .0005; back.Normalize(); acceleration -= back * .15;
                }
            }
            Point relative = host.LaunchButton().TranslatePoint(new Point(0, 0), host.FormPanel());
            if (relative.X < -buttonWidth * 2) {
                host.AprilPosition()!.X += host.FormPanel().ActualWidth + buttonWidth * 2; speed.X -= 80;
                if (relative.Y < 0) host.AprilPosition()!.Y += buttonHeight * 2.5;
                else if (relative.Y > host.FormPanel().ActualHeight - buttonHeight * 2) host.AprilPosition()!.Y -= buttonHeight * 2.5;
            } else if (relative.X > host.FormPanel().ActualWidth) {
                host.AprilPosition()!.X -= host.FormPanel().ActualWidth + buttonWidth * 2; speed.X += 80;
                if (relative.Y < 0) host.AprilPosition()!.Y += buttonHeight * 2.5;
                else if (relative.Y > host.FormPanel().ActualHeight - buttonHeight * 2) host.AprilPosition()!.Y -= buttonHeight * 2.5;
            } else if (relative.Y < -buttonHeight * 2) {
                host.AprilPosition()!.Y += host.FormPanel().ActualHeight + buttonHeight * 2; speed.Y -= 25;
                if (relative.X < 0) host.AprilPosition()!.X += buttonWidth * 2;
                else if (relative.X > host.FormPanel().ActualWidth - buttonWidth * 2) host.AprilPosition()!.X -= buttonWidth * 2;
            } else if (relative.Y > host.FormPanel().ActualHeight) {
                host.AprilPosition()!.Y -= host.FormPanel().ActualHeight + buttonHeight * 2; speed.Y += 25;
                if (relative.X < 0) host.AprilPosition()!.X += buttonWidth * 2;
                else if (relative.X > host.FormPanel().ActualWidth - buttonWidth * 2) host.AprilPosition()!.X -= buttonWidth * 2;
            }
            speed = speed * .8 + acceleration;
            double magnitude = Math.Min(60, speed.Length);
            if (magnitude < .01) return;
            speed.Normalize(); speed *= magnitude;
            difficultyDistance = Convert.ToInt32(difficultyDistance + magnitude);
            host.AprilPosition()!.X += speed.X; host.AprilPosition()!.Y += speed.Y;
            host.AprilScale().ScaleX = MathUtils.Clamp(1 - (Math.Abs(direction.X) - Math.Abs(direction.Y)) * (magnitude / 160), .2, 1.8);
            host.AprilScale().ScaleY = MathUtils.Clamp(1 - (Math.Abs(direction.Y) - Math.Abs(direction.X)) * (magnitude / 100), .2, 1.8);
            giveUpDistance += magnitude;
            if (giveUpDistance > 2500) {
                giveUpDistance = 0;
                switch (host.RandomInteger(0, 3)) {
                    case 0: host.RedHint("放弃吧！只需要点一下右下角的小白旗……"); break;
                    case 1: host.RedHint("看到右下角的那面小白旗了吗？"); break;
                    case 2: host.RedHint("这里建议点一下右下角的小白旗投降呢.jpg"); break;
                    case 3: host.RedHint("右下角的小白旗永远等着你……"); break;
                }
            }
        } catch (Exception ex) { Logger.Error(ex, "愚人节移动出错"); }
    }

    public void Start() {
        host.StartThread(() => {
            try {
                while (true) { host.UiWait(Tick); Thread.Sleep(49); }
            } catch (Exception ex) { Logger.Error(ex, "程序主时钟出错"); }
        }, "Timer Main");
        if (!IsAprilEnabled) return;
        host.StartThread(() => {
            try {
                int lastTime = Environment.TickCount;
                while (true) {
                    if (lastTime != Environment.TickCount) { lastTime = Environment.TickCount; host.UiWait(FoolTick); }
                    Thread.Sleep(1);
                }
            } catch (Exception ex) { Logger.Error(ex, "愚人节主时钟出错"); }
        }, "Timer Main Fool");
    }
}
