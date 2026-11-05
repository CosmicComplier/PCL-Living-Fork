using System;
using System.Collections.Generic;
using System.Windows;
using MeloongCore;
using MeloongCore.Extensions;

namespace PCLLF.Presentation.Launch;

public sealed partial class LegacyLaunchLeft<TInstance, TAnimation> where TInstance : class where TAnimation : struct {
    private double showProgress;
    private bool widthAnimating, heightAnimating;
    private double actualWidth, actualHeight;
    public void LaunchingRefresh() {
        try {
            if (host.RealLaunch().State() == LegacyLaunchPanelState.Canceled) return;
            bool launched = false;
            try {
                bool found = false;
                foreach (LegacyLaunchItemPort item in host.RealLaunch().Items()) {
                    if (item.State() == LegacyLaunchPanelState.Loading || item.State() == LegacyLaunchPanelState.Waiting) {
                        view.Stage().Text = item.Name(); launched = item.Name() == "等待游戏窗口出现" || item.Name() == "结束处理"; found = true; break;
                    }
                }
                if (!found) view.Stage().Text = "已完成";
            } catch (Exception error) { Logger.Warn(error, "获取是否启动完成失败，可能是由于启动状态改变导致集合已修改"); return; }
            view.Title().Text = launched || host.RealLaunch().State() == LegacyLaunchPanelState.Finished ? "已启动游戏" : host.ExportScript() ? "正在导出启动脚本" : "正在启动游戏";
            if (animation.IsRunning("Launch State Page")) launched = false;
            double actual = host.RealLaunch().Progress();
            if (actual >= showProgress) showProgress += (actual - showProgress) * 0.1 + 0.0025;
            if (actual <= showProgress) showProgress = actual;
            if (launched) showProgress = 1;
            view.Progress().Text = (showProgress * 100).ToString("0.00") + " %";
            bool downloader = false;
            try {
                if (host.NetSpeed() == 0 && view.Download().Visibility == Visibility.Collapsed) downloader = false;
                else foreach (LegacyLaunchItemPort item in host.NetTasks()) if (item.Parent() is not null && item.Parent()!.Name() == "Minecraft 启动" && item.State() == LegacyLaunchPanelState.Loading) downloader = true;
            } catch (Exception error) { Logger.Warn(error, "获取 Minecraft 启动下载器失败，可能是因为启动被取消"); downloader = false; }
            view.Download().Text = host.FormatByteSize(host.NetSpeed()) + "/s";
            List<TAnimation> frames = new() {
                animation.GridWidth(view.FinishedColumn(), showProgress - view.FinishedColumn().Width.Value, 130, 0, LegacyLaunchEase.OutFluent, false),
                animation.GridWidth(view.UnfinishedColumn(), 1 - showProgress - view.UnfinishedColumn().Width.Value, 130, 0, LegacyLaunchEase.OutFluent, false)
            };
            if (downloader == (view.Download().Visibility == Visibility.Collapsed)) {
                view.Download().Visibility = Visibility.Visible; view.DownloadLeft().Visibility = Visibility.Visible;
                frames.AddRange(new[] {
                    animation.Opacity(view.Download(), (downloader ? 1 : 0) - view.Download().Opacity, 90, 0, LegacyLaunchEase.Linear, false),
                    animation.Opacity(view.DownloadLeft(), (downloader ? 0.5 : 0) - view.DownloadLeft().Opacity, 90, 0, LegacyLaunchEase.Linear, false),
                    animation.Code(() => { if (!downloader) { view.Download().Visibility = Visibility.Collapsed; view.DownloadLeft().Visibility = Visibility.Collapsed; } }, 110, false)
                });
            }
            if (!launched == (view.Progress().Visibility == Visibility.Collapsed)) {
                view.Progress().Visibility = Visibility.Visible; view.ProgressLeft().Visibility = Visibility.Visible;
                if (launched) view.LaunchingHint().Visibility = Visibility.Visible;
                frames.AddRange(new[] {
                    animation.Opacity(view.Progress(), (!launched ? 1 : 0) - view.Progress().Opacity, 90, 0, LegacyLaunchEase.Linear, false),
                    animation.Opacity(view.ProgressLeft(), (!launched ? 0.5 : 0) - view.ProgressLeft().Opacity, 90, 0, LegacyLaunchEase.Linear, false),
                    animation.Opacity(view.LaunchingHint(), (launched ? 1 : 0) - view.LaunchingHint().Opacity, 90, 0, LegacyLaunchEase.Linear, false)
                });
            }
            animation.Start(frames, "Launching Progress", false);
        } catch (Exception error) { Logger.Error(error, "刷新启动信息失败"); }
    }
    public void WidthChanged(SizeChangedEventArgs e) {
        double delta = e.NewSize.Width - e.PreviousSize.Width;
        if (e.PreviousSize.Width == 0 || widthAnimating || Math.Abs(delta) < 1 || view.LaunchingInfo().ActualWidth == 0) return;
        animation.Start(new[] {
            animation.Width(view.LaunchingInfo(), delta, 180, 0, LegacyLaunchEase.OutFluent, false),
            animation.Code(() => { widthAnimating = false; view.LaunchingInfo().Width = actualWidth; }, 0, true)
        }, "Launching Info Width", false);
        widthAnimating = true; actualWidth = view.LaunchingInfo().Width; view.LaunchingInfo().Width = e.PreviousSize.Width;
    }
    public void HeightChanged(SizeChangedEventArgs e) {
        double delta = e.NewSize.Height - e.PreviousSize.Height;
        if (e.PreviousSize.Height == 0 || heightAnimating || Math.Abs(delta) < 1 || view.LaunchingInfo().ActualHeight == 0) return;
        animation.Start(new[] {
            animation.Height(view.LaunchingInfo(), delta, 180, 0, LegacyLaunchEase.OutFluent, false),
            animation.Code(() => { heightAnimating = false; view.LaunchingInfo().Height = actualHeight; }, 0, true)
        }, "Launching Info Height", false);
        heightAnimating = true; actualHeight = view.LaunchingInfo().Height; view.LaunchingInfo().Height = e.PreviousSize.Height;
    }
}
