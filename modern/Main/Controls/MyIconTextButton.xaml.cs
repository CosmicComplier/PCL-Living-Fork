using System.Windows;
using System.Windows.Markup;
using Microsoft.VisualBasic.CompilerServices;
using PCLLF.Presentation.Controls;

namespace PCL
{

    [ContentProperty("Inlines")]
    public class MyIconTextButton : MyIconTextButtonView
    {
        static MyIconTextButton()
        {
            PresentationRuntime.EnsureConfigured();
        }
        public static new readonly DependencyProperty TextProperty = MyIconTextButtonView.TextProperty;
        public new enum ColorState
        {
            Black,
            Highlight
        }
        public static new readonly DependencyProperty ColorTypeProperty = DependencyProperty.Register("ColorType", typeof(ColorState), typeof(MyIconTextButton), new PropertyMetadata(ColorState.Black));
        public new ColorState ColorType
        {
            get
            {
                return (ColorState)(int)base.ColorType;
            }
            set
            {
                base.ColorType = (MyIconTextButtonView.ColorState)(int)value;
            }
        }
        protected override MyIconTextButtonView.ColorState ReadColorType()
        {
            return (MyIconTextButtonView.ColorState)Conversions.ToInteger(GetValue(ColorTypeProperty));
        }
        protected override void WriteColorType(MyIconTextButtonView.ColorState Value)
        {
            SetValue(ColorTypeProperty, (ColorState)(int)Value);
        }
        public new event CheckEventHandler Check;

        public new delegate void CheckEventHandler(object sender, bool raiseByMouse);
        public new event ChangeEventHandler Change;

        public new delegate void ChangeEventHandler(object sender, bool raiseByMouse);
        public new event ClickEventHandler Click;

        public new delegate void ClickEventHandler(object sender, ModBase.RouteEventArgs e);
        protected override void OnClick()
        {
            Click?.Invoke(this, new ModBase.RouteEventArgs(true));
        }
    }
}