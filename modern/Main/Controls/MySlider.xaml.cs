using System;
using PCLLF.Presentation.Controls;

namespace PCL
{
    public partial class MySlider : MySliderView, ISettingControl
    {
        public MySlider()
        {
            __LfAttachOwnHandles();
        }

        static MySlider()
        {
            PresentationRuntime.EnsureConfigured();
        }

        public new event ChangeEventHandler Change;
        public new delegate void ChangeEventHandler(object sender, bool user);
        public new event PreviewChangeEventHandler PreviewChange;
        public new delegate void PreviewChangeEventHandler(object sender, ModBase.RouteEventArgs e);
        protected override void OnChange(bool User)
        {
            Change?.Invoke(this, User);
        }

        protected override bool OnPreviewChange()
        {
            var e = new ModBase.RouteEventArgs(false);
            PreviewChange?.Invoke(this, e);
            return e.Handled;
        }

        protected override object CurrentDragControl
        {
            get
            {
                return ModMain.DragControl;
            }
        }

        protected override void SetDragControl(object Value)
        {
            ModMain.DragControl = Value;
        }

        protected override void PerformMainDrag()
        {
            ModMain.FrmMain.DragDoing();
        }

        protected override double PopupDpi
        {
            get
            {
                return ModBase.DPI;
            }
        }

        private void RefreshSetting(string NewValue)
        {
            Value = (int)Math.Round(ModBase.Val(NewValue));
        }

        void ISettingControl.RefreshSetting(string NewValue) => RefreshSetting(NewValue);
        private string GetCurrentSetting()
        {
            return Value.ToString();
        }

        string ISettingControl.GetCurrentSetting() => GetCurrentSetting();
        private void SaveSetting()
        {
            SettingService.SaveSetting(this);
        }
    }
}
