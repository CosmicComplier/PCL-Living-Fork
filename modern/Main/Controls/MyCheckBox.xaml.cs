using System.Windows;
using System.Windows.Markup;
using PCLLF.Presentation.Controls;

namespace PCL
{
    [ContentProperty("Inlines")]
    public partial class MyCheckBox : MyCheckBoxView, ISettingControl
    {
        public MyCheckBox()
        {
            __LfAttachOwnHandles();
        }

        static MyCheckBox()
        {
            PresentationRuntime.EnsureConfigured();
        }

        public static new readonly DependencyProperty TextProperty = MyCheckBoxView.TextProperty;
        public static new readonly DependencyProperty CheckedProperty = MyCheckBoxView.CheckedProperty;
        public new event ChangeEventHandler Change;
        public new delegate void ChangeEventHandler(object sender, bool user);
        public new event PreviewChangeEventHandler PreviewChange;
        public new delegate void PreviewChangeEventHandler(object sender, ModBase.RouteEventArgs e);
        protected override void OnChange(bool user)
        {
            Change?.Invoke(this, user);
        }

        protected override bool RaisePreviewChange(bool user)
        {
            var e = new ModBase.RouteEventArgs(user);
            PreviewChange?.Invoke(this, e);
            return e.Handled;
        }

        private new void RefreshSetting(string NewValue)
        {
            base.RefreshSetting(NewValue);
        }

        void ISettingControl.RefreshSetting(string NewValue) => RefreshSetting(NewValue);
        private new string GetCurrentSetting()
        {
            return base.GetCurrentSetting();
        }

        string ISettingControl.GetCurrentSetting() => GetCurrentSetting();
        private void SaveSetting()
        {
            SettingService.SaveSetting(this);
        }
    }
}
