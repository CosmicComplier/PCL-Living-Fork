using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PCLCS;

namespace PCL
{
    // Original LF XAML fields/Handles and public entry points remain typed; business lives in C#.
    public partial class PageSetupSystem
    {
        private LegacyPageSetupSystemController _controller;
        internal LegacyPageSetupSystemController Controller
        {
            get
            {
                if (_controller is null)
                {
                    _controller = new LegacyPageSetupSystemController(SetupPageRuntime.SystemHost, new LegacyPageSetupSystemView() { Page = new Func<DependencyObject>(() => this), PanBack = new Func<ScrollViewer>(() => this.PanBack), PanMain = new Func<StackPanel>(() => this.PanMain), ComboDownloadSource = new Func<PCLLF.Presentation.Controls.MyComboBoxView>(() => this.ComboDownloadSource), ComboDownloadVersion = new Func<PCLLF.Presentation.Controls.MyComboBoxView>(() => this.ComboDownloadVersion), SliderDownloadThread = new Func<PCLLF.Presentation.Controls.MySliderView>(() => this.SliderDownloadThread), SliderDownloadSpeed = new Func<PCLLF.Presentation.Controls.MySliderView>(() => this.SliderDownloadSpeed), ComboDownloadMod = new Func<PCLLF.Presentation.Controls.MyComboBoxView>(() => this.ComboDownloadMod), ComboDownloadTranslateV2 = new Func<PCLLF.Presentation.Controls.MyComboBoxView>(() => this.ComboDownloadTranslateV2), ComboModLocalNameStyle = new Func<PCLLF.Presentation.Controls.MyComboBoxView>(() => this.ComboModLocalNameStyle), CheckHelpChinese = new Func<PCLLF.Presentation.Controls.MyCheckBoxView>(() => this.CheckHelpChinese), LabLauncherVersion = new Func<TextBlock>(() => this.LabLauncherVersion), ComboSystemActivity = new Func<PCLLF.Presentation.Controls.MyComboBoxView>(() => this.ComboSystemActivity), TextSystemCache = new Func<PCLLF.Presentation.Controls.MyTextBoxView>(() => this.TextSystemCache), BtnSystemUpdate = new Func<PCLLF.Presentation.Controls.MyButtonView>(() => this.BtnSystemUpdate), BtnSystemSettingExp = new Func<PCLLF.Presentation.Controls.MyButtonView>(() => this.BtnSystemSettingExp), BtnSystemSettingImp = new Func<PCLLF.Presentation.Controls.MyButtonView>(() => this.BtnSystemSettingImp), LabModpackMode = new Func<TextBlock>(() => this.LabModpackMode), CheckModpackMode = new Func<PCLLF.Presentation.Controls.MyCheckBoxView>(() => this.CheckModpackMode), CardDebug = new Func<PCLLF.Presentation.Controls.MyCardView>(() => this.CardDebug), PanDebugAnim = new Func<Grid>(() => this.PanDebugAnim), SliderDebugAnim = new Func<PCLLF.Presentation.Controls.MySliderView>(() => this.SliderDebugAnim), CheckDebugSkipCopy = new Func<PCLLF.Presentation.Controls.MyCheckBoxView>(() => this.CheckDebugSkipCopy), CheckDebugMode = new Func<PCLLF.Presentation.Controls.MyCheckBoxView>(() => this.CheckDebugMode), CheckDebugDelay = new Func<PCLLF.Presentation.Controls.MyCheckBoxView>(() => this.CheckDebugDelay) });
                }

                return _controller;
            }
        }

        public PageSetupSystem()
        {
            __LfAttachOwnHandles();
            this.InitializeComponent();
        }

        private void PageSetupSystem_Loaded(object sender, RoutedEventArgs e)
        {
            Controller.Loaded();
        }

        public void Reload()
        {
            Controller.Reload();
        }

        internal void RefreshModpackSwitch()
        {
            Controller.RefreshModpackSwitch();
        }

        private void ModpackModeChanged(object sender, bool User)
        {
            Controller.ModpackModeChanged(sender, User);
        }

        public void Reset()
        {
            Controller.Reset();
        }

        private void SliderLoad()
        {
            Controller.SliderLoad();
        }

        private void SliderDownloadThread_PreviewChange(object sender, ModBase.RouteEventArgs e)
        {
            Controller.DownloadThreadPreview();
        }

        private void CheckDebugMode_Change()
        {
            Controller.DebugModeChanged();
        }

        private void BtnSystemUpdate_Click(object sender, EventArgs e)
        {
            Controller.CheckUpdate();
        }

        private void BtnSystemSettingExp_Click(object sender, MouseButtonEventArgs e)
        {
            Controller.ExportSettings();
        }

        private void BtnSystemSettingImp_Click(object sender, MouseButtonEventArgs e)
        {
            Controller.ImportSettings();
        }
    }
}
