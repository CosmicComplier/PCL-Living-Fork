using System.Windows;
using PCLLF.Presentation.Controls;

namespace PCL
{

    public class MyPageLeft : MyPageLeftView
    {
        static MyPageLeft()
        {
            PresentationRuntime.EnsureConfigured();
        }
        public static new readonly DependencyProperty AnimatedControlProperty = MyPageLeftView.AnimatedControlProperty;
    }
}