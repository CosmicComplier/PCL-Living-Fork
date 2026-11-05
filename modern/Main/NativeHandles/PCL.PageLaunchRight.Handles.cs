// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class PageLaunchRight
    {
        private global::PCL.MyScrollViewer __LfPart_PanBack;
        internal virtual global::PCL.MyScrollViewer PanBack
        {
            get
            {
                return __LfPart_PanBack;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanBack = value;
            }
        }

        private global::System.Windows.Controls.StackPanel __LfPart_PanMain;
        internal virtual global::System.Windows.Controls.StackPanel PanMain
        {
            get
            {
                return __LfPart_PanMain;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanMain = value;
            }
        }

        private global::System.Windows.Controls.StackPanel __LfPart_PanCustom;
        internal virtual global::System.Windows.Controls.StackPanel PanCustom
        {
            get
            {
                return __LfPart_PanCustom;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanCustom = value;
            }
        }

        private global::PCL.MyCard __LfPart_PanLog;
        internal virtual global::PCL.MyCard PanLog
        {
            get
            {
                return __LfPart_PanLog;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanLog = value;
            }
        }

        private global::PCL.MyTextBox __LfPart_LabLog;
        internal virtual global::PCL.MyTextBox LabLog
        {
            get
            {
                return __LfPart_LabLog;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabLog = value;
            }
        }

        private global::System.Windows.Controls.Grid __LfPart_PanDependencyOptionsAnchor;
        internal virtual global::System.Windows.Controls.Grid PanDependencyOptionsAnchor
        {
            get
            {
                return __LfPart_PanDependencyOptionsAnchor;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanDependencyOptionsAnchor = value;
            }
        }

        private global::PCL.MyCard __LfPart_CardDependencyChecks;
        internal virtual global::PCL.MyCard CardDependencyChecks
        {
            get
            {
                return __LfPart_CardDependencyChecks;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CardDependencyChecks = value;
            }
        }

        private global::System.Windows.Media.TranslateTransform __LfPart_TransDependencyChecks;
        internal virtual global::System.Windows.Media.TranslateTransform TransDependencyChecks
        {
            get
            {
                return __LfPart_TransDependencyChecks;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_TransDependencyChecks = value;
            }
        }

        private global::System.Windows.Controls.StackPanel __LfPart_PanDependencyChecks;
        internal virtual global::System.Windows.Controls.StackPanel PanDependencyChecks
        {
            get
            {
                return __LfPart_PanDependencyChecks;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanDependencyChecks = value;
            }
        }

        private global::PCL.MyCheckBox __LfPart_CheckModDependencies;
        internal virtual global::PCL.MyCheckBox CheckModDependencies
        {
            get
            {
                return __LfPart_CheckModDependencies;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyCheckBox.ChangeEventHandler hook0 = new global::PCL.MyCheckBox.ChangeEventHandler(this.DependencyCheckChanged);
                global::PCL.MyCheckBox previous = __LfPart_CheckModDependencies;
                if (previous != null)
                {
                    previous.Change -= hook0;
                }

                __LfPart_CheckModDependencies = value;
                global::PCL.MyCheckBox current = __LfPart_CheckModDependencies;
                if (current != null)
                {
                    current.Change += hook0;
                }
            }
        }

        private global::PCL.MyCheckBox __LfPart_CheckAutoFillDependencies;
        internal virtual global::PCL.MyCheckBox CheckAutoFillDependencies
        {
            get
            {
                return __LfPart_CheckAutoFillDependencies;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CheckAutoFillDependencies = value;
            }
        }

        private global::PCL.MyCard __LfPart_CardRecentWorlds;
        internal virtual global::PCL.MyCard CardRecentWorlds
        {
            get
            {
                return __LfPart_CardRecentWorlds;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CardRecentWorlds = value;
            }
        }

        private global::System.Windows.Media.TranslateTransform __LfPart_TransRecentWorld;
        internal virtual global::System.Windows.Media.TranslateTransform TransRecentWorld
        {
            get
            {
                return __LfPart_TransRecentWorld;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_TransRecentWorld = value;
            }
        }

        private global::System.Windows.Controls.StackPanel __LfPart_PanRecentWorlds;
        internal virtual global::System.Windows.Controls.StackPanel PanRecentWorlds
        {
            get
            {
                return __LfPart_PanRecentWorlds;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanRecentWorlds = value;
            }
        }

        private global::PCL.MyListItem __LfPart_ItemRecentWorld;
        internal virtual global::PCL.MyListItem ItemRecentWorld
        {
            get
            {
                return __LfPart_ItemRecentWorld;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyListItem.ClickEventHandler hook0 = new global::PCL.MyListItem.ClickEventHandler(this.RecentWorldClick);
                global::PCL.MyListItem previous = __LfPart_ItemRecentWorld;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_ItemRecentWorld = value;
                global::PCL.MyListItem current = __LfPart_ItemRecentWorld;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private void __LfHandle_0(global::System.Object arg0, global::System.EventArgs arg1)
        {
            this.InitRecentWorldCard();
        }

        private void __LfHandle_1(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.Init();
        }

        private void __LfHandle_2(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.ReleaseRecentWorlds();
        }

        private void __LfHandle_3(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.Refresh();
        }

        private void __LfAttachOwnHandles()
        {
            this.Initialized += new global::System.EventHandler(this.__LfHandle_0);
            this.PageEnter += new global::PCL.MyPageRight.PageEnterEventHandler(this.RecentPageEntered);
            this.Loaded += new global::System.Windows.RoutedEventHandler(this.__LfHandle_1);
            this.Unloaded += new global::System.Windows.RoutedEventHandler(this.__LfHandle_2);
            this.Loaded += new global::System.Windows.RoutedEventHandler(this.__LfHandle_3);
        }
    }
}
