using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using PCLLF.Presentation.Controls;

namespace PCL
{

    [ContentProperty("Inlines")]
    public class MyExtraTextButton : MyExtraTextButtonView
    {
        static MyExtraTextButton()
        {
            PresentationRuntime.EnsureConfigured();
        }
        public static new readonly DependencyProperty TextProperty = MyExtraTextButtonView.TextProperty;
        public new event ClickEventHandler Click;

        public new delegate void ClickEventHandler(object sender, MouseButtonEventArgs e);
        protected override void OnClick(object sender, MouseButtonEventArgs Args)
        {
            Click?.Invoke(sender, Args);
        }
        protected override void UiPost(Action Work)
        {
            ModBase.RunInUi(Work);
        }
    }
}