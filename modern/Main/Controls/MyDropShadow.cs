using System.Windows;
using PCLLF.Presentation.Controls;

namespace PCL
{

    public class MyDropShadow : MyDropShadowView
    {

        public static new readonly DependencyProperty ColorProperty = MyDropShadowView.ColorProperty;
        public static new readonly DependencyProperty ShadowRadiusProperty = MyDropShadowView.ShadowRadiusProperty;
        public static new readonly DependencyProperty CornerRadiusProperty = MyDropShadowView.CornerRadiusProperty;
    }
}