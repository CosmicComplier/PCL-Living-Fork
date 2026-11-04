using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PCLCS;
using PCLLF.Presentation.Controls;

namespace PCL
{
    // Only LF original XAML/type/Handles/model identity adapters; all workflows are C#.
    public partial class PageOtherAbout
    {
        private LegacyAuxiliaryBasicController _controller;
        internal LegacyAuxiliaryBasicController Controller
        {
            get
            {
                if (_controller is null)
                    _controller = new LegacyAuxiliaryBasicController(AuxiliaryPageRuntime.Basic, new LegacyAuxiliaryBasicView() { Page = new Func<DependencyObject>(() => this), Scroll = new Func<ScrollViewer>(() => this.PanBack), About = new Func<MyListItemView>(() => this.ItemAboutPcl) });
                return _controller;
            }
        }

        public PageOtherAbout()
        {
            __LfAttachOwnHandles();
            this.InitializeComponent();
        }

        private void PageOtherAbout_Loaded(object sender, RoutedEventArgs e)
        {
            Controller.AboutLoaded();
        }

        private void BtnAboutUpdate_Click(object sender, MouseButtonEventArgs e)
        {
            Controller.CheckUpdate();
        }
    }
}
