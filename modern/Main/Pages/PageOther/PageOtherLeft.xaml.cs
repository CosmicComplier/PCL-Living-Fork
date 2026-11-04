using System;
using System.Windows;
using PCLCS;
using PCLLF.Presentation.Controls;

namespace PCL
{
    // Only LF original XAML/type/Handles/model identity adapters; all workflows are C#.
    public partial class PageOtherLeft
    {
        public PCL.FormMain.PageSubType PageID;
        private LegacyAuxiliaryNavigationController _controller;
        internal LegacyAuxiliaryNavigationController Controller
        {
            get
            {
                if (_controller is null)
                {
                    _controller = new LegacyAuxiliaryNavigationController(AuxiliaryPageRuntime.NavigationHost(() => (int)PageID, Value => PageID = (PCL.FormMain.PageSubType)Value), new LegacyAuxiliaryNavigationView() { Help = new Func<MyListItemView>(() => this.ItemHelp), About = new Func<MyListItemView>(() => this.ItemAbout), Test = new Func<MyListItemView>(() => this.ItemTest) }, false);
                }

                return _controller;
            }
        }

        private void PageOtherLeft_Loaded(object sender, RoutedEventArgs e)
        {
            Controller.Loaded();
        }

        private void PageOtherLeft_Unloaded(object sender, RoutedEventArgs e)
        {
            Controller.Unloaded();
        }

        public PageOtherLeft()
        {
            __LfAttachOwnHandles();
            this.InitializeComponent();
            Controller.InitializePageId();
        }

        private void PageCheck(FrameworkElement sender, PCL.ModBase.RouteEventArgs e)
        {
            Controller.PageCheck(sender);
        }

        public object PageGet(PCL.FormMain.PageSubType ID = (PCL.FormMain.PageSubType)(-1))
        {
            return (PCL.MyPageRight)Controller.PageGet((int)ID);
        }

        public void PageChange(PCL.FormMain.PageSubType ID)
        {
            Controller.PageChange((int)ID);
        }

        private static void PageChangeRun(PCL.MyPageRight Target)
        {
            LegacyAuxiliaryNavigationController.PageChangeRun(AuxiliaryPageRuntime.NavigationHost(null, null), Target);
        }

        public void Refresh(object sender, EventArgs e) // 由边栏按钮匿名调用
        {
            Controller.Refresh(sender);
        }

        public static void RefreshHelp()
        {
            AuxiliaryPageRuntime.RefreshHelp();
        }
    }
}
