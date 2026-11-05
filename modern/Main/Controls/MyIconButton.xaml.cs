using System;
using System.Threading;
using PCLLF.Presentation.Controls;

namespace PCL
{

    public class MyIconButton : MyIconButtonView
    {

        static MyIconButton()
        {
            PresentationRuntime.EnsureConfigured();
        }
        public new enum Themes
        {
            Color,
            White,
            Black,
            Red,
            Custom
        }
        public new Themes Theme
        {
            get
            {
                return (Themes)(int)base.Theme;
            }
            set
            {
                base.Theme = (MyIconButtonView.Themes)(int)value;
            }
        }
        protected override void WithAnimationSuppressed(Action Work)
        {
            ModAnimation.AniControlEnabled += 1;
            Work();
            ModAnimation.AniControlEnabled -= 1;
        }
        public new event ClickEventHandler Click;

        public new delegate void ClickEventHandler(object sender, EventArgs e);
        protected override void OnClick(object sender, EventArgs e)
        {
            Click?.Invoke(sender, e);
        }
    }

    public static partial class ModAnimation
    {
        public static void AniDispose(MyIconButton Control, bool RemoveFromChildren, ParameterizedThreadStart CallBack = null)
        {
            Control.DisposeAnimated(RemoveFromChildren, CallBack is null ? null : new Action<object>(Value => CallBack(Value)));
        }
    }
}