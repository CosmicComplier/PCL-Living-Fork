using System;
using System.Windows;
using System.Windows.Controls;
using PCLCS;
using PCLLF.Presentation.Controls;

namespace PCL
{
    // Only LF original XAML/type/Handles/model identity adapters; all workflows are C#.
    public partial class PageOtherHelpDetail : PCL.IRefreshable
    {
        public ModMain.HelpEntry Entry;
        private LegacyAuxiliaryHelpDetailController<ModMain.HelpEntry, CustomEvent.EventType> _controller;
        internal LegacyAuxiliaryHelpDetailController<ModMain.HelpEntry, CustomEvent.EventType> Controller
        {
            get
            {
                if (_controller is null)
                    _controller = new LegacyAuxiliaryHelpDetailController<ModMain.HelpEntry, CustomEvent.EventType>(AuxiliaryPageRuntime.Help.Host, new LegacyAuxiliaryHelpDetailView<ModMain.HelpEntry>() { Page = new Func<MyPageRightView>(() => this), Entry = new Func<ModMain.HelpEntry>(() => Entry), SetEntry = new Action<ModMain.HelpEntry>(Value => Entry = Value), Scroll = new Func<ScrollViewer>(() => this.PanBack), Content = new Func<StackPanel>(() => this.PanCustom) });
                return _controller;
            }
        }

        public PageOtherHelpDetail()
        {
            __LfAttachOwnHandles();
            this.InitializeComponent();
        }

        public void Refresh()
        {
            Controller.Refresh();
        }

        private void PageOtherHelpDetail_Loaded(object sender, RoutedEventArgs e)
        {
            Controller.Loaded();
        }

        public bool Init(ModMain.HelpEntry Entry)
        {
            return Controller.Init(Entry);
        }
    }
}
