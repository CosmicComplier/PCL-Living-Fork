using PCLLF.Presentation.Controls;

namespace PCL
{

    public class MyScrollBar : MyScrollBarView
    {
        static MyScrollBar()
        {
            PresentationRuntime.EnsureConfigured();
        }
    }
}