using System.Windows;
using System.Windows.Input;
using PCLCS;

namespace PCL;

// Original LF host integration; repository LICENCE applies.
public partial class PageInstanceOverall {
    private void RefreshMmcActions() {
        // ContextStamp is already attached by the normal instance reader. Avoid
        // re-reading settings with side effects merely to decide button visibility.
        var visibility = PageInstanceLeft.Instance?.Config.ContextStamp.Kind == MmcConfigurationContextKind.Managed ? Visibility.Visible : Visibility.Collapsed;
        BtnMmcExport.Visibility = visibility; BtnMmcHistory.Visibility = visibility; BtnMmcMove.Visibility = visibility; BtnMmcPackageUpdate.Visibility = visibility;
        BtnMmcLegacyAdoption.Visibility = PageInstanceLeft.Instance?.Config.ContextStamp.Kind == MmcConfigurationContextKind.Unmanaged ? Visibility.Visible : Visibility.Collapsed;
    }
    private void BtnMmcLegacyAdoption_Click(object sender, MouseButtonEventArgs e) {
        try { MmcLegacyAdoptionUi.PromptCreateCopy(PageInstanceLeft.Instance); }
        catch (System.Exception error) { MeloongCore.Logger.Error(error, "无法开始创建 MMC 管理副本", MeloongCore.LogBehavior.Alert); }
    }
    private void BtnMmcExport_Click(object sender, MouseButtonEventArgs e) => MmcExportUi.Prompt(PageInstanceLeft.Instance);
    private void BtnMmcHistory_Click(object sender, MouseButtonEventArgs e) => MmcHistoryUi.Prompt(PageInstanceLeft.Instance);
    private void BtnMmcPackageUpdate_Click(object sender, MouseButtonEventArgs e) => MmcPackageUpdateUi.Prompt(PageInstanceLeft.Instance);
    private void BtnMmcMove_Click(object sender, MouseButtonEventArgs e) {
        var original = PageInstanceLeft.Instance;
        MmcLifecycleUi.PromptMove(original, _ => {
            if (ReferenceEquals(PageInstanceLeft.Instance, original) && ReferenceEquals(ModMain.FrmMain?.PageRight, this)) ModMain.FrmMain.PageBack();
        });
    }
}
