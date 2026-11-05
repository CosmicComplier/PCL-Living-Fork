using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using PCLLF.Presentation.Controls;

namespace PCL
{

    [ContentProperty("Inlines")]
    public class MyButton : MyButtonView
    {

        static MyButton()
        {
            PresentationRuntime.EnsureConfigured();
        }

        public static new readonly DependencyProperty TextProperty = MyButtonView.TextProperty;
        public static new readonly DependencyProperty PaddingProperty = MyButtonView.PaddingProperty;

        // 保留原嵌套类型名；实际颜色状态只存放在 C# 基类。
        public new enum ColorState
        {
            Normal = 0,
            Highlight = 1,
            Red = 2
        }
        public new ColorState ColorType
        {
            get
            {
                return (ColorState)(int)base.ColorType;
            }
            set
            {
                base.ColorType = (MyButtonView.ColorState)(int)value;
            }
        }

        public new event ClickEventHandler Click;

        public new delegate void ClickEventHandler(object sender, MouseButtonEventArgs e);
        protected override void OnClick(object sender, MouseButtonEventArgs args)
        {
            Click?.Invoke(sender, args);
        }
    }
}