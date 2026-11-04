// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class PageOtherHelp
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

        private global::PCL.MySearchBox __LfPart_SearchBox;
        internal virtual global::PCL.MySearchBox SearchBox
        {
            get
            {
                return __LfPart_SearchBox;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MySearchBox.TextChangedEventHandler hook0 = new global::PCL.MySearchBox.TextChangedEventHandler(this.__LfHandle_0);
                global::PCL.MySearchBox previous = __LfPart_SearchBox;
                if (previous != null)
                {
                    previous.TextChanged -= hook0;
                }

                __LfPart_SearchBox = value;
                global::PCL.MySearchBox current = __LfPart_SearchBox;
                if (current != null)
                {
                    current.TextChanged += hook0;
                }
            }
        }

        private global::PCL.MyCard __LfPart_PanSearch;
        internal virtual global::PCL.MyCard PanSearch
        {
            get
            {
                return __LfPart_PanSearch;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanSearch = value;
            }
        }

        private global::System.Windows.Controls.StackPanel __LfPart_PanSearchList;
        internal virtual global::System.Windows.Controls.StackPanel PanSearchList
        {
            get
            {
                return __LfPart_PanSearchList;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanSearchList = value;
            }
        }

        private global::System.Windows.Controls.StackPanel __LfPart_PanList;
        internal virtual global::System.Windows.Controls.StackPanel PanList
        {
            get
            {
                return __LfPart_PanList;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanList = value;
            }
        }

        private global::PCL.MyCard __LfPart_PanLoad;
        internal virtual global::PCL.MyCard PanLoad
        {
            get
            {
                return __LfPart_PanLoad;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanLoad = value;
            }
        }

        private global::PCL.MyLoading __LfPart_Load;
        internal virtual global::PCL.MyLoading Load
        {
            get
            {
                return __LfPart_Load;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_Load = value;
            }
        }

        private void __LfHandle_0(global::System.Object arg0, global::System.EventArgs arg1)
        {
            this.SearchRun();
        }

        private void __LfAttachOwnHandles()
        {
            this.Loaded += new global::System.Windows.RoutedEventHandler(this.PageOther_Loaded);
            this.Initialized += new global::System.EventHandler(this.PageOther_Inited);
        }
    }
}
