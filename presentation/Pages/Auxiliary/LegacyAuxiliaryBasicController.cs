// Behavior source: LF Pages/PageSetup and Pages/PageOther; repository LICENCE.
// LF lineage f4aa0fae62c3ee8af24c81e61844c8fdd13c8983; upstream PCL 5ef81e490ae46b1100d61e33d0183094c7a80ca8.
// Existing LF policies are preserved; no new native/performance/MMC behavior.
using System;
using MeloongCore;

namespace PCLCS;
public sealed class LegacyAuxiliaryBasicController
{
    private readonly LegacyAuxiliaryBasicHost host;
    private readonly LegacyAuxiliaryBasicView view;
    private bool loaded;
    public LegacyAuxiliaryBasicController(LegacyAuxiliaryBasicHost host, LegacyAuxiliaryBasicView view)
    {
        this.host = host;
        this.view = view;
    }

    public void AboutLoaded()
    {
        view.Scroll().ScrollToHome();
        if (loaded)
            return;
        loaded = true;
        view.About().Info = view.About().Info.Replace("%VERSION%", host.LauncherVersion());
    }

    public void CheckUpdate() => host.CheckUpdate();
    public void LinkLoaded()
    {
        view.Scroll().ScrollToHome();
        if (loaded)
            return;
        loaded = true;
        host.Common.AddAnimationSuppression(1);
        LinkReload();
        host.Common.AddAnimationSuppression(-1);
    }

    public void LinkReload() => host.Common.RefreshSettings(view.Page());
    public void LinkReset()
    {
        try
        {
            host.Common.ResetSettings(view.Page());
            Logger.Info("已初始化联机页设置");
            host.Common.Hint("已初始化联机页设置！", LegacySetupHint.Green, false);
        }
        catch (Exception error)
        {
            Logger.Error(error, "初始化联机页设置失败", LogBehavior.Alert);
        }

        LinkReload();
    }
}
