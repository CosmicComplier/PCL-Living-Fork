using System;
using System.Windows;
using System.Windows.Input;
using PCLLF.Presentation.Accounts;

namespace PCL
{
    public partial class PageLoginNideSkin
    {
        private readonly LegacyLoggedInPage<ModLaunch.McLoginLegacy, ModLaunch.McLoginMs, ModLaunch.McLoginServer> Native;
        public PageLoginNideSkin()
        {
            __LfAttachOwnHandles();
            Native = CreateController(this);
            this.InitializeComponent();
            this.Skin.Loader = PageLaunchLeft.SkinNide;
        }

        private static LegacyLoggedInPage<ModLaunch.McLoginLegacy, ModLaunch.McLoginMs, ModLaunch.McLoginServer> CreateController(PageLoginNideSkin Page)
        {
            return new LegacyLoggedInPage<ModLaunch.McLoginLegacy, ModLaunch.McLoginMs, ModLaunch.McLoginServer>(LoginPageRuntime.Accounts, new LegacyLoggedInView() { SetName = new Action<string>(Value => Page.TextName.Text = Value), StartSkin = new Action(() => Page.Skin.Loader.Start()), EditButton = new Func<FrameworkElement>(() => Page.BtnEdit), ExitButton = new Func<FrameworkElement>(() => Page.BtnExit), SetEmail = new Action<string>(Value => Page.TextEmail.Text = Value), ShowEmail = new Action<bool>(Value => Page.TextEmail.Visibility = Value ? Visibility.Visible : Visibility.Collapsed), SaveSkin = new Action(() => Page.Skin.BtnSkinSave_Click()), RefreshSkin = new Action(() => Page.Skin.RefreshClick()), ChangeCape = new Action(() => Page.Skin.BtnSkinCape_Click()) }, "Nide");
        }

        private void PageLoginLegacy_Loaded(object sender, RoutedEventArgs e)
        {
            Native.Loaded();
        }

        public void Reload(bool KeepInput)
        {
            Native.Reload(KeepInput);
        }

        public static ModLaunch.McLoginServer GetLoginData()
        {
            return CreateController(ModMain.FrmLoginNideSkin).GetServerLoginData();
        }

        private void PageLoginNideSkin_MouseEnter(object sender, MouseEventArgs e)
        {
            Native.ShowPanel();
        }

        private void PageLoginNideSkin_MouseLeave(object sender, MouseEventArgs e)
        {
            Native.HidePanel();
        }

        public static void ExitLogin()
        {
            CreateController(ModMain.FrmLoginNideSkin).ExitLogin();
        }

        private void Skin_Click(object sender, MouseButtonEventArgs e)
        {
            Native.SkinClick();
        }
    }
}
