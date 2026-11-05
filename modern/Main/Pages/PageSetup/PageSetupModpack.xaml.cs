using System;
using PCLCS;

namespace PCL
{
    public partial class PageSetupModpack
    {
        private bool Refreshing;
        private ModpackSetupPageController<MyCheckBox> PageController;
        private ModpackSetupPageController<MyCheckBox> Controller
        {
            get
            {
                if (PageController is null)
                    PageController = new ModpackSetupPageController<MyCheckBox>(new ModpackSetupPageHost<MyCheckBox>() { Refreshing = new Func<bool>(() => Refreshing), SetRefreshing = new Action<bool>(Value => Refreshing = Value), AdjustAnimation = new Action<int>(Value => ModAnimation.AniControlEnabled += Value), Animation = new Func<int>(() => ModAnimation.AniControlEnabled), TouchTitle = TouchTitle, VersionText = LabVersion, Brand = new Func<string>(() => ModBase.LauncherDisplayName), Version = new Func<string>(() => ModBase.VersionBaseName), RefreshSettings = new Action(() => SettingService.RefreshSettings(this)), Enabled = new Func<bool>(() => ModModpackMode.Enabled), SetChecked = new Action<bool>(Value => this.CheckModpackMode.Checked = Value), ScrollHome = new Action(() => this.PanBack.ScrollToHome()), Toggle = new Action<MyCheckBox, bool>((Sender, User) => ModModpackMode.Toggle(Sender, User)), HintWithoutLog = new Action<string>(Message => ModMain.Hint(Message, Log: false)) });
                return PageController;
            }
        }

        public PageSetupModpack()
        {
            __LfAttachOwnHandles();
            this.InitializeComponent();
        }

        private void TouchTitle()
        {
            var Title = this.CardLauncher.MainTextBlock;
        }

        private void PageLoaded()
        {
            Controller.Loaded();
        }

        private void ModeChanged(object sender, bool User)
        {
            Controller.ModeChanged(this.CheckModpackMode, User);
        }

        private void CacheChanged()
        {
            Controller.CacheChanged();
        }
    }
}
