using System;
using System.Windows;
using PCLLF.Presentation.Controls;

namespace PCL
{

    public class MyTextButton : MyTextButtonView
    {
        static MyTextButton()
        {
            PresentationRuntime.EnsureConfigured();
        }
        public static new readonly DependencyProperty TextProperty = MyTextButtonView.TextProperty;
        public new event ClickEventHandler Click;

        public new delegate void ClickEventHandler(object sender, EventArgs e);
        protected override void OnClick()
        {
            Click?.Invoke(this, null);
        }
    }
}