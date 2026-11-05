using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using MeloongCore;
using MeloongCore.Extensions;
using Microsoft.VisualBasic.CompilerServices;
using PCLLF.Presentation.Controls;

namespace PCLCS;

public sealed class LegacyPageSetupSystemController {
    private readonly LegacySetupSystemHost host;
    public readonly LegacyPageSetupSystemView View;
    private bool loaded;
    private bool refreshingUpdateChannel = true;
    private LegacySetupPageHost Common => host.Common;
    public LegacyPageSetupSystemController(LegacySetupSystemHost host, LegacyPageSetupSystemView view) { this.host = host; View = view; }
    // The retained LF f4 comparison tree has neither update-selection view
    // delegates nor update-channel host delegates. Its original Reload remains
    // valid. Native LF50 supplies the complete contract; any partial wiring fails.
    private bool HasUpdateSelection() {
        if (View.UpdatesOnSeparatePage) return false;
        bool noView = View.ComboLauncherUpdateSource is null && View.ComboLauncherUpdateChannel is null && View.ConfirmPrereleaseUpdates is null;
        bool noHost = host.PreferredUpdateChannel is null && host.ChangeUpdateChannel is null;
        if (noView && noHost) {
            // Missing delegates alone cannot identify the retained old UI: a
            // Native page could accidentally omit every new callback as well.
            // Require the actual old page's name scope to lack both LF50 parts.
            if (View.Page is null || View.Page() is not FrameworkElement page)
                throw new InvalidOperationException("Legacy launcher update selection requires a real page name scope.");
            if (page.FindName("ComboLauncherUpdateSource") is not null || page.FindName("ComboLauncherUpdateChannel") is not null)
                throw new InvalidOperationException("Launcher update selection controls exist but their view/host delegates are missing.");
            return false;
        }
        if (View.ComboLauncherUpdateSource is null || View.ComboLauncherUpdateChannel is null || View.ConfirmPrereleaseUpdates is null || host.PreferredUpdateChannel is null || host.ChangeUpdateChannel is null)
            throw new InvalidOperationException("Launcher update selection view/host delegates are only partially connected.");
        return true;
    }
    public void InitializeLauncherUpdates() {
        if (!HasUpdateSelection()) return;
        Common.AddAnimationSuppression(1);
        try {
            Common.RefreshSettings(View.ComboLauncherUpdateSource());
            RefreshUpdateChannel();
        } finally { Common.AddAnimationSuppression(-1); }
    }
    public void RefreshUpdateChannel() {
        if (!HasUpdateSelection()) return;
        refreshingUpdateChannel = true;
        try { View.ComboLauncherUpdateChannel().SelectedIndex = host.PreferredUpdateChannel(); }
        finally { refreshingUpdateChannel = false; }
    }
    public void UpdateChannelChanged() {
        if (!HasUpdateSelection()) return;
        if (refreshingUpdateChannel || Common.AnimationSuppression() != 0 || View.ComboLauncherUpdateChannel().SelectedIndex < 0) return;
        try {
            if (!host.ChangeUpdateChannel(View.ComboLauncherUpdateChannel().SelectedIndex, View.ConfirmPrereleaseUpdates())) RefreshUpdateChannel();
        } catch (Exception error) {
            RefreshUpdateChannel();
            Logger.Error(error, "切换启动器更新通道失败", LogBehavior.Toast);
        }
    }
    public void Loaded() {
        View.PanBack().ScrollToHome();
        RefreshModpackSwitch();
        if (!View.UpdatesOnSeparatePage) View.LabLauncherVersion().Text = host.LauncherDisplayName() + " " + host.LauncherVersion();
        if (loaded) return;
        loaded = true;
        Common.AddAnimationSuppression(1);
        Reload(); SliderLoad();
        Common.AddAnimationSuppression(-1);
    }
    public void Reload() {
        Common.RefreshSettings(View.Page()); RefreshUpdateChannel(); RefreshModpackSwitch();
        if (!View.UpdatesOnSeparatePage) View.LabLauncherVersion().Text = host.LauncherDisplayName() + " " + host.LauncherVersion();
    }
    public void RefreshModpackSwitch() {
        View.CheckModpackMode().Checked = host.ModpackEnabled();
        View.CheckModpackMode().Visibility = host.CanEnableModpack() ? Visibility.Visible : Visibility.Collapsed;
        View.LabModpackMode().Visibility = View.CheckModpackMode().Visibility;
    }
    public void ModpackModeChanged(object sender, bool user) => host.ToggleModpack(View.CheckModpackMode(), user);
    public void Reset() {
        try {
            Common.ResetSettings(View.Page());
            if (HasUpdateSelection()) Common.Reset("ToolLauncherUpdateChannel");
            Logger.Info("已初始化启动器页设置"); Common.Hint("已初始化启动器页设置！", LegacySetupHint.Green, false);
        } catch (Exception error) { Logger.Error(error, "初始化启动器页设置失败", LogBehavior.Alert); }
        Reload();
    }
    public void SliderLoad() {
        View.SliderDownloadThread().GetHintText = new Func<object, object>(value => Operators.AddObject(value, 1)!);
        View.SliderDownloadSpeed().GetHintText = new Func<object, object>(value => {
            if (Operators.ConditionalCompareObjectLessEqual(value, 14, false)) return LegacySetupText.String(Operators.MultiplyObject(Operators.AddObject(value, 1), 0.1)) + " M/s";
            if (Operators.ConditionalCompareObjectLessEqual(value, 31, false)) return LegacySetupText.String(Operators.MultiplyObject(Operators.SubtractObject(value, 11), 0.5)) + " M/s";
            if (Operators.ConditionalCompareObjectLessEqual(value, 41, false)) return LegacySetupText.String(Operators.SubtractObject(value, 21)) + " M/s";
            return "无限制";
        });
        View.SliderDebugAnim().GetHintText = new Func<object, object>(value => Operators.ConditionalCompareObjectGreater(value, 29, false) ? "关闭" : LegacySetupText.String(Operators.AddObject(Operators.DivideObject(value, 10), 0.1)) + "x");
    }
    public void DownloadThreadPreview() {
        if (View.SliderDownloadThread().Value < 100) return;
        if (!Conversions.ToBoolean(Common.Read("HintDownloadThread"))) {
            Common.Write("HintDownloadThread", true);
            Common.Message("如果设置过多的下载线程，可能会导致下载时出现非常严重的卡顿。\r\n一般设置 64 线程即可满足大多数下载需求，除非你知道你在干什么，否则不建议设置更多的线程数！", "警告", "我知道了", warning: true);
        }
    }
    public void DebugModeChanged() { if (Common.AnimationSuppression() == 0) Common.Hint("部分调试信息将在刷新或启动器重启后切换显示！", LegacySetupHint.Blue, false); }
    public void CheckUpdate() => host.CheckLauncherUpdate();
    public void ExportSettings() => Common.Hint("该功能尚在开发中！", LegacySetupHint.Blue, true);
    public void ImportSettings() => Common.Hint("该功能尚在开发中！", LegacySetupHint.Blue, true);
}
