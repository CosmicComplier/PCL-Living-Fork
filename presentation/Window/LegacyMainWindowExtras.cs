using System;
using System.Windows;
using MeloongCore;
using PCLLF.Presentation.Controls;

using PCLCS.Startup;

namespace PCLLF.Presentation.Windowing;

public sealed partial class LegacyMainWindowController<TStack, TInstance, TAnimation>
    where TStack : class, ILegacyMainPageStack, new() where TInstance : class where TAnimation : struct {
    public void MusicClick() => host.MusicPause();
    public void MusicRightClick() => host.MusicNext();
    public void DownloadClick() => PageChange(Stack((int)LegacyMainPage.TaskManager));
    public bool DownloadShowCheck() => host.HasDownloadingTask() && !IsPage(PageCurrent, LegacyMainPage.TaskManager);
    public void AprilGiveup() {
        if (!host.AprilEnabled() || host.AprilGivenUp()) return;
        Hint("=D", LegacyMainHint.Green); host.WriteAprilGivenUp(true); host.AprilScale().ScaleX = 1; host.AprilScale().ScaleY = 1; view.ExtraApril().ShowRefresh();
    }
    public bool AprilShowCheck() => host.AprilEnabled() && !host.AprilGivenUp() && IsPage(PageCurrent, LegacyMainPage.Launch);
    public void ShutdownClick() {
        try { if (host.HasLaunchOperation()) host.CancelLaunchOperation(); foreach (Action kill in host.WatcherKills()) kill(); Hint("已关闭运行中的 Minecraft！", LegacyMainHint.Green); }
        catch (Exception ex) { Logger.Error(ex, "强制关闭所有 Minecraft 失败"); }
    }
    public bool ShutdownShowCheck() => host.HasRunningMinecraft();
    public void BackToTop() {
        MyScrollViewerView? scroll = RealScroll();
        if (scroll is not null) scroll.PerformVerticalOffsetDelta(-scroll.VerticalOffset);
        else Logger.Error("无法返回顶部，未找到合适的 RealScroll", LogBehavior.Toast);
    }
    public bool BackShowCheck() {
        MyScrollViewerView? scroll = RealScroll();
        return scroll is not null && scroll.Visibility == Visibility.Visible && scroll.VerticalOffset > Window.Height + (view.ExtraBack().Show() ? 0 : 700);
    }
    public MyScrollViewerView? RealScroll() {
        if (view.PanMainRight().Child is null || view.PanMainRight().Child is not MyPageRightView) return null;
        MyPageRightView page = (MyPageRightView)view.PanMainRight().Child;
        return (MyScrollViewerView?)page.FindName(page.PanScroll!);
    }
}
