using System;
using System.Windows.Input;
using PCLLF.Presentation.Controls;

namespace PCL
{

    public class MyExtraButton : MyExtraButtonView
    {
        static MyExtraButton()
        {
            PresentationRuntime.EnsureConfigured();
        }
        public new event ClickEventHandler Click;

        public new delegate void ClickEventHandler(object sender, MouseButtonEventArgs e);
        public new event RightClickEventHandler RightClick;

        public new delegate void RightClickEventHandler(object sender, MouseButtonEventArgs e);
        protected override void OnClick(object sender, MouseButtonEventArgs Args)
        {
            Click?.Invoke(sender, Args);
        }
        protected override void OnRightClick(object sender, MouseButtonEventArgs Args)
        {
            RightClick?.Invoke(sender, Args);
        }
        public new delegate bool ShowCheckDelegate();
        public new ShowCheckDelegate ShowCheck;
        protected override bool HasShowCheck
        {
            get
            {
                return ShowCheck is not null;
            }
        }
        protected override bool CheckShow()
        {
            return ShowCheck();
        }
        protected override void UiPost(Action Work)
        {
            ModBase.RunInUi(Work);
        }
    }
}