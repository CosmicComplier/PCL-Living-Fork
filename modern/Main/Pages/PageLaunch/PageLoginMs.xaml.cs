using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualBasic.CompilerServices;
using PCLLF.Presentation.Accounts;

namespace PCL
{
    public partial class PageLoginMs
    {
        private readonly LegacyMicrosoftLoginPage<ModLaunch.McLoginLegacy, ModLaunch.McLoginMs, ModLaunch.McLoginServer> Native;
        public PageLoginMs()
        {
            Native = CreateController(this);
            this.InitializeComponent();
        }

        private static LegacyMicrosoftLoginPage<ModLaunch.McLoginLegacy, ModLaunch.McLoginMs, ModLaunch.McLoginServer> CreateController(PageLoginMs Page)
        {
            return new LegacyMicrosoftLoginPage<ModLaunch.McLoginLegacy, ModLaunch.McLoginMs, ModLaunch.McLoginServer>(LoginPageRuntime.Accounts, new LegacyMicrosoftLoginView() { SelectedIndex = new Func<int>(() => Page.ComboAccounts.SelectedIndex), SelectIndex = new Action<int>(Value => Page.ComboAccounts.SelectedIndex = Value), ClearItems = new Action(() => Page.ComboAccounts.Items.Clear()), AddNewAccount = new Action(() => Page.ComboAccounts.Items.Add(new MyComboBoxItem() { Content = "添加新账号" })), AddAccount = new Action<string, Func<string>>((Name, Token) => Page.AddAccount(Name, Token)), SelectedItem = new Func<object>(() => Page.ComboAccounts.SelectedItem), HasPresenter = new Func<bool>(() => Page.ComboAccounts.ContentPresenter is not null), IsAccountItem = new Func<object, bool>(Item => Item is MyListItem), IsNewAccountItem = new Func<object, bool>(Item => Item is MyComboBoxItem), AccountName = new Func<object, string>(Item => ((MyListItem)Item).Title), AccountToken = new Func<object, string>(Item => Conversions.ToString(((MyListItem)Item).Tag)), NewAccountContent = new Func<object, object>(Item => ((MyComboBoxItem)Item).Content), SetPresenterContent = new Action<object>(Value => Page.ComboAccounts.ContentPresenter.Content = Value), EnableAccounts = new Action<bool>(Value => Page.ComboAccounts.IsEnabled = Value), EnableLogin = new Action<bool>(Value => Page.BtnLogin.IsEnabled = Value), SetLoginText = new Action<string>(Value => Page.BtnLogin.Text = Value) });
        }

        private void AddAccount(string Name, Func<string> Token)
        {
            MyListItem Item = (MyListItem)((DataTemplate)this.FindResource("ComboBoxItemTemplateWithDelete")).LoadContent();
            Item.Tag = Token();
            Item.Title = Name;
            Item.Buttons.ElementAtOrDefault(0).Tag = Name;
            this.ComboAccounts.Items.Add(Item);
        }

        public void Reload(bool KeepInput)
        {
            Native.Reload(KeepInput);
        }

        private void ComboAccounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Native.SelectionChanged();
        }

        public static ModLaunch.McLoginMs GetLoginData()
        {
            return CreateController(ModMain.FrmLoginMs).GetLoginData(ModMain.FrmLoginMs is not null);
        }

        public static string IsVaild(ModLaunch.McLoginMs LoginData)
        {
            return CreateController(ModMain.FrmLoginMs).IsVaild(LoginData);
        }

        public string IsVaild()
        {
            return IsVaild(GetLoginData());
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            Native.LoginClick(GetLoginData);
        }
    }
}
