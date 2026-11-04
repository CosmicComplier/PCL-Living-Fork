using PCLLF.Presentation.Controls;

namespace PCL
{

    public class MyMenuItem : MyMenuItemView
    {
        static MyMenuItem()
        {
            PresentationRuntime.EnsureConfigured();
        }
        protected override int LauncherTransparency
        {
            get
            {
                return Settings.Get<int>("UiLauncherTransparent");
            }
        }
    }
}