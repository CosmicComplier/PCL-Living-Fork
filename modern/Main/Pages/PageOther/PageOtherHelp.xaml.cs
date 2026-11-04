using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using PCLCS;
using PCLLF.Presentation.Controls;

namespace PCL
{
    // Only LF original XAML/type/Handles/model identity adapters; all workflows are C#.
    public partial class PageOtherHelp : PCL.IRefreshable
    {
        private LegacyAuxiliaryHelpController<ModMain.HelpEntry, CustomEvent.EventType> _controller;
        internal LegacyAuxiliaryHelpController<ModMain.HelpEntry, CustomEvent.EventType> Controller
        {
            get
            {
                if (_controller is null)
                    _controller = new LegacyAuxiliaryHelpController<ModMain.HelpEntry, CustomEvent.EventType>(AuxiliaryPageRuntime.Help, new LegacyAuxiliaryHelpView() { Page = new Func<MyPageRightView>(() => this), Scroll = new Func<ScrollViewer>(() => this.PanBack), Loading = new Func<MyLoadingView>(() => this.Load), LoadingPanel = new Func<FrameworkElement>(() => this.PanLoad), List = new Func<StackPanel>(() => this.PanList), SearchList = new Func<StackPanel>(() => this.PanSearchList), SearchCard = new Func<MyCardView>(() => this.PanSearch), Query = new Func<string>(() => this.SearchBox.Text) });
                return _controller;
            }
        }

        public PageOtherHelp()
        {
            __LfAttachOwnHandles();
            this.InitializeComponent();
        }

        private void PageOther_Loaded(object sender, RoutedEventArgs e)
        {
            Controller.Loaded();
        }

        private void PageOther_Inited(object sender, EventArgs e)
        {
            Controller.Initialized();
        }

        private void HelpListLoad(PCL.ModLoader.LoaderTask<int, List<ModMain.HelpEntry>> Loader)
        {
            Controller.HelpListLoad(Loader.Output);
        }

        public static void OnItemClick(ModMain.HelpEntry Entry)
        {
            AuxiliaryPageRuntime.Help.OnItemClick(Entry);
        }

        public static void EnterHelpPage(string Location)
        {
            AuxiliaryPageRuntime.Help.EnterHelpPage(Location);
        }

        public static void EnterHelpPage(ModMain.HelpEntry Entry)
        {
            AuxiliaryPageRuntime.Help.EnterHelpPage(Entry);
        }

        public void SearchRun()
        {
            Controller.SearchRun();
        }

        public void Refresh()
        {
            Controller.Refresh();
        }
    }
}
