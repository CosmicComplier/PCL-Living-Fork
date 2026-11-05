using System;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using PCLLF.Presentation.Controls;

namespace PCL
{

    [ContentProperty("Inlines")]
    public class MyHint : MyHintView
    {

        static MyHint()
        {
            PresentationRuntime.EnsureConfigured();
        }
        public static new readonly DependencyProperty IsWarnProperty = MyHintView.IsWarnProperty;
        public static new readonly DependencyProperty TextProperty = MyHintView.TextProperty;
        public new enum Themes
        {
            Blue = 0,
            Red = 1,
            Yellow = 2
        }
        public new Themes Theme
        {
            get
            {
                return (Themes)(int)base.Theme;
            }
            set
            {
                base.Theme = (MyHintView.Themes)(int)value;
            }
        }
        protected override double WpfSize(double Pixels)
        {
            return ModBase.GetWPFSize(Pixels);
        }
        protected override bool ReadDismissed(string Key)
        {
            return Settings.Get<bool>(Key);
        }
        protected override void WriteDismissed(string Key)
        {
            Settings.SetSafe(Key, true);
        }
        public new event ClickEventHandler Click;

        public new delegate void ClickEventHandler(object sender, MouseButtonEventArgs e);
        protected override void OnClick(object sender, MouseButtonEventArgs Args)
        {
            Click?.Invoke(sender, Args);
        }
    }

    public static partial class ModAnimation
    {
        public static void AniDispose(MyHint Control, bool RemoveFromChildren, ParameterizedThreadStart CallBack = null)
        {
            Control.DisposeAnimated(RemoveFromChildren, CallBack is null ? null : new Action<object>(Value => CallBack(Value)));
        }
    }
}