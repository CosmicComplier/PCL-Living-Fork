using System;
using System.Windows.Controls;
using MeloongCore;
using PCLLF.Presentation.Controls;

namespace PCL
{
    public partial class MyComboBox : MyComboBoxView, ISettingControl
    {
        public MyComboBox()
        {
            __LfAttachOwnHandles();
        }

        static MyComboBox()
        {
            PresentationRuntime.EnsureConfigured();
        }

        public new event TextChangedEventHandler TextChanged;
        public new delegate void TextChangedEventHandler(object sender, TextChangedEventArgs e);
        protected override void OnTextChanged(object sender, TextChangedEventArgs Args)
        {
            TextChanged?.Invoke(sender, Args);
        }

        protected override double MainWindowOpacity
        {
            get
            {
                return ModMain.FrmMain.Opacity;
            }
        }

        protected override void ReportError(Exception ErrorValue, string Message)
        {
            Logger.Error(ErrorValue, Message);
        }

        private void RefreshSetting(string NewValue)
        {
            RefreshSettingValue(NewValue, Item => SettingService.GetValue(Item), () => SettingService.GetKey(this), Message => Logger.Warn(Message));
        }

        void ISettingControl.RefreshSetting(string NewValue) => RefreshSetting(NewValue);
        private string GetCurrentSetting()
        {
            return CurrentSettingValue(Item => SettingService.GetValue(Item));
        }

        string ISettingControl.GetCurrentSetting() => GetCurrentSetting();
        private void SaveSetting()
        {
            if (SelectedItem is null)
                return;
            SettingService.SaveSetting(this);
        }
    }
}
