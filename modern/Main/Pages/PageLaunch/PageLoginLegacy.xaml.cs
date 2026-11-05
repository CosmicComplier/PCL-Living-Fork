using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PCLLF.Presentation.Accounts;

namespace PCL
{
    public partial class PageLoginLegacy
    {
        private readonly LegacyOfflineLoginPage<ModLaunch.McLoginLegacy, ModLaunch.McLoginMs, ModLaunch.McLoginServer> Native;
        public bool IsReloaded;
        public PageLoginLegacy()
        {
            __LfAttachOwnHandles();
            this.IsReloaded = false;
            Native = CreateController(this);
            this.InitializeComponent();
            this.Skin.Loader = PageLaunchLeft.SkinLegacy;
        }

        private static LegacyOfflineLoginPage<ModLaunch.McLoginLegacy, ModLaunch.McLoginMs, ModLaunch.McLoginServer> CreateController(PageLoginLegacy Page)
        {
            return new LegacyOfflineLoginPage<ModLaunch.McLoginLegacy, ModLaunch.McLoginMs, ModLaunch.McLoginServer>(LoginPageRuntime.Accounts, new LegacyOfflineLoginView() { IsReloaded = new Func<bool>(() => Page.IsReloaded), SetReloaded = new Action<bool>(Value => Page.IsReloaded = Value), Name = new Func<string>(() => Page.ComboName.Text), SetName = new Action<string>(Value => Page.ComboName.Text = Value), SetNames = new Action<IEnumerable<string>>(Value => Page.ComboName.ItemsSource = Value), StartSkin = new Action(() => Page.Skin.Loader.Start()), RestartSkin = new Action(() => PageLaunchLeft.SkinLegacy.Start(IsForceRestart: true)), ShowChineseHint = new Action<bool>(Value => Page.HintChinese.Visibility = Value ? Visibility.Visible : Visibility.Collapsed), InvalidateMeasureWhenLoaded = new Action(() => ModBase.RunInUi(() => Page.PanMain.InvalidateMeasure(), true)) });
        }

        private void PageLoginLegacy_Loaded(object sender, RoutedEventArgs e)
        {
            Native.Loaded();
        }

        public void Reload(bool KeepInput)
        {
            Native.Reload(KeepInput);
        }

        public static ModLaunch.McLoginData GetLoginData()
        {
            return CreateController(ModMain.FrmLoginLegacy).GetLoginData(ModMain.FrmLoginLegacy is not null, () => ModMain.FrmLoginLegacy.ComboName.Text);
        }

        public static string IsVaild(ModLaunch.McLoginLegacy LoginData)
        {
            return CreateController(ModMain.FrmLoginLegacy).IsVaild(LoginData);
        }

        public string IsVaild()
        {
            return IsVaild((ModLaunch.McLoginLegacy)GetLoginData());
        }

        private void ComboName_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                int Index = ((MyTextBox)this.ComboName.Template.FindName("PART_EditableTextBox", this.ComboName)).CaretIndex;
                if (LegacyOfflineLoginPage<ModLaunch.McLoginLegacy, ModLaunch.McLoginMs, ModLaunch.McLoginServer>.SuppressSpace(Index, this.ComboName.Text.Length))
                    e.Handled = true;
            }
        }

        private void ComboLegacy_TextChanged(object sender, TextChangedEventArgs e)
        {
            Native.TextChanged();
        }

        private void Skin_Click()
        {
            Native.SkinClick();
        }
    }
}
