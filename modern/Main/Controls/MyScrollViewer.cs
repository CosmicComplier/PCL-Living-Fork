using PCLLF.Presentation.Controls;

namespace PCL
{

    public class MyScrollViewer : MyScrollViewerView
    {
        static MyScrollViewer()
        {
            PresentationRuntime.EnsureConfigured();
        }
        public new MyScrollBar ScrollBar;
        public new void PerformVerticalOffsetDelta(double Delta)
        {
            base.PerformVerticalOffsetDelta(Delta);
        }
        protected override MyScrollBarView VerticalBar
        {
            get
            {
                return ScrollBar;
            }
        }
        protected override void AssignBar(MyScrollBarView Value)
        {
            ScrollBar = (MyScrollBar)Value;
        }
        protected override bool HasMainWindow
        {
            get
            {
                return ModMain.FrmMain is not null;
            }
        }
        protected override void RefreshBack()
        {
            ModMain.FrmMain?.BtnExtraBack?.ShowRefresh();
        }
    }
}
