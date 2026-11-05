using System.Windows;
using System.Windows.Markup;
using PCLLF.Presentation.Controls;

namespace PCL
{
    [ContentProperty("Inlines")]
    public partial class MyRadioBox : MyRadioBoxView, IMyRadio, ISettingControl
    {
        public MyRadioBox()
        {
            __LfAttachOwnHandles();
        }

        static MyRadioBox()
        {
            PresentationRuntime.EnsureConfigured();
        }

        public static new readonly DependencyProperty CheckedProperty = MyRadioBoxView.CheckedProperty;
        public static new readonly DependencyProperty TextProperty = MyRadioBoxView.TextProperty;
        public new event PreviewCheckEventHandler PreviewCheck;
        public new delegate void PreviewCheckEventHandler(object sender, ModBase.RouteEventArgs e);
        public new event PreviewChangeEventHandler PreviewChange;
        public new delegate void PreviewChangeEventHandler(object sender, ModBase.RouteEventArgs e);
        public new event IMyRadio.CheckEventHandler Check;
        public new event IMyRadio.ChangedEventHandler Changed;
        protected override bool OnPreviewCheck(bool User)
        {
            var e = new ModBase.RouteEventArgs(User);
            PreviewCheck?.Invoke(this, e);
            return e.Handled;
        }

        protected override void OnPreviewChange(bool User)
        {
            PreviewChange?.Invoke(this, new ModBase.RouteEventArgs(User));
        }

        protected override void OnCheck(bool User)
        {
            Check?.Invoke(this, new ModBase.RouteEventArgs(User));
        }

        protected override void OnChanged(bool User)
        {
            Changed?.Invoke(this, new ModBase.RouteEventArgs(User));
        }

        private void RefreshSetting(string NewValue)
        {
            Checked = (NewValue ?? "") == (SettingService.GetValue(this) ?? "");
        }

        void ISettingControl.RefreshSetting(string NewValue) => RefreshSetting(NewValue);
        private string GetCurrentSetting()
        {
            return Checked ? SettingService.GetValue(this) : null;
        }

        string ISettingControl.GetCurrentSetting() => GetCurrentSetting();
        private void SaveSetting()
        {
            SettingService.SaveSetting(this);
        }
    }
}
