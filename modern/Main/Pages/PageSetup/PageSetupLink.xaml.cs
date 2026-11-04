// LF optional LegacyLink from f4aa0fae62c3ee8af24c81e61844c8fdd13c8983 / main50b028d (unchanged); LF LICENCE.
using System;
using System.Windows;
using System.Windows.Controls;
using PCLCS;

namespace PCL
{

    // Only LF original XAML/type/Handles/model identity adapters; all workflows are C#.
    public partial class PageSetupLink
    {
        private LegacyAuxiliaryBasicController _controller;
        internal LegacyAuxiliaryBasicController Controller
        {
            get
            {
                if (_controller is null)
                    _controller = new LegacyAuxiliaryBasicController(AuxiliaryPageRuntime.Basic, new LegacyAuxiliaryBasicView() { Page = new Func<DependencyObject>(() => this), Scroll = new Func<ScrollViewer>(() => PanBack) });
                return _controller;
            }
        }

        public PageSetupLink()
        {
            this.Loaded += PageSetupLink_Loaded;
            InitializeComponent();
        }

        private void PageSetupLink_Loaded(object sender, RoutedEventArgs e)
        {
            Controller.LinkLoaded();
        }

        public void Reload()
        {
            Controller.LinkReload();
        }

        public void Reset()
        {
            Controller.LinkReset();
        }

    }
}