using System;
using System.Windows;
using System.Windows.Input;
using PCLLF.Presentation.Accounts;

namespace PCL
{
    public partial class PageLoginMsSkin
    {
        private readonly LegacyLoggedInPage<ModLaunch.McLoginLegacy, ModLaunch.McLoginMs, ModLaunch.McLoginServer> Native;
        public PageLoginMsSkin()
        {
            __LfAttachOwnHandles();
            Native = CreateController(this);
            this.InitializeComponent();
            this.Skin.Loader = PageLaunchLeft.SkinMs;
        }

        private static LegacyLoggedInPage<ModLaunch.McLoginLegacy, ModLaunch.McLoginMs, ModLaunch.McLoginServer> CreateController(PageLoginMsSkin Page)
        {
            return new LegacyLoggedInPage<ModLaunch.McLoginLegacy, ModLaunch.McLoginMs, ModLaunch.McLoginServer>(LoginPageRuntime.Accounts, new LegacyLoggedInView() { SetName = new Action<string>(Value => Page.TextName.Text = Value), StartSkin = new Action(() => Page.Skin.Loader.Start()), EditButton = new Func<FrameworkElement>(() => Page.BtnEdit), ExitButton = new Func<FrameworkElement>(() => Page.BtnExit), ButtonsPanel = new Func<FrameworkElement>(() => Page.PanButtons), EditMenuOpen = new Func<bool>(() => Page.BtnEdit.ContextMenu.IsOpen), SkinMenuOpen = new Func<bool>(() => Page.BtnSkin.ContextMenu.IsOpen), DataMouseOver = new Func<bool>(() => Page.PanData.IsMouseOver), OpenEditMenu = new Action(() => Page.BtnEdit.ContextMenu.IsOpen = true), OpenSkinMenu = new Action(() => Page.BtnSkin.ContextMenu.IsOpen = true), SelectSkin = LoginPageRuntime.SelectSkin, ReloadSkinCache = new Action<string>(Address => MySkin.ReloadCache(Address)), SaveSkin = new Action(() => Page.Skin.BtnSkinSave_Click()), RefreshSkin = new Action(() => Page.Skin.RefreshClick()), ChangeCape = new Action(() => Page.Skin.BtnSkinCape_Click()) }, "MsV2");
        }

        private void PageLoginLegacy_Loaded(object sender, RoutedEventArgs e)
        {
            Native.Loaded();
        }

        public void Reload(bool KeepInput)
        {
            Native.Reload(KeepInput);
        }

        public static ModLaunch.McLoginMs GetLoginData()
        {
            return CreateController(ModMain.FrmLoginMsSkin).GetMsLoginData();
        }

        private void ShowPanel(object sender, MouseEventArgs e)
        {
            Native.ShowPanel();
        }

        public void HidePanel(object sender, EventArgs e)
        {
            Native.HidePanel();
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            Native.OpenEditMenu();
        }

        private void BtnExit_Click()
        {
            Native.ExitLogin();
        }

        private void BtnSkin_Click(object sender, RoutedEventArgs e)
        {
            Native.SkinClick();
        }

        public void BtnSkinEdit_Click(object sender, RoutedEventArgs e)
        {
            Native.SkinEditClick(GetLoginData);
        }

        public void BtnSkinSave_Click(object sender, RoutedEventArgs e)
        {
            Native.SaveSkin();
        }

        public void BtnSkinRefresh_Click(object sender, RoutedEventArgs e)
        {
            Native.RefreshSkin();
        }

        public void BtnSkinCape_Click(object sender, RoutedEventArgs e)
        {
            Native.ChangeCape();
        }

        public void BtnSkinCapeRefresh_Click(object sender, RoutedEventArgs e)
        {
            Native.RefreshCape(GetLoginData);
        }
    }
}
