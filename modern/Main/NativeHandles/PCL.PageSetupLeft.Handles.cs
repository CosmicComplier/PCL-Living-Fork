// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class PageSetupLeft
    {
        private global::System.Windows.Controls.StackPanel __LfPart_PanItem;
        internal virtual global::System.Windows.Controls.StackPanel PanItem
        {
            get
            {
                return __LfPart_PanItem;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanItem = value;
            }
        }

        private global::PCL.MyListItem __LfPart_ItemLaunch;
        internal virtual global::PCL.MyListItem ItemLaunch
        {
            get
            {
                return __LfPart_ItemLaunch;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.IMyRadio.CheckEventHandler hook0 = new global::PCL.IMyRadio.CheckEventHandler(this.__LfHandle_0);
                global::PCL.MyListItem previous = __LfPart_ItemLaunch;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_ItemLaunch = value;
                global::PCL.MyListItem current = __LfPart_ItemLaunch;
                if (current != null)
                {
                    current.Check += hook0;
                }
            }
        }

        private global::PCL.MyListItem __LfPart_ItemLink;
        internal virtual global::PCL.MyListItem ItemLink
        {
            get
            {
                return __LfPart_ItemLink;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.IMyRadio.CheckEventHandler hook0 = new global::PCL.IMyRadio.CheckEventHandler(this.__LfHandle_3);
                global::PCL.MyListItem previous = __LfPart_ItemLink;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_ItemLink = value;
                global::PCL.MyListItem current = __LfPart_ItemLink;
                if (current != null)
                {
                    current.Check += hook0;
                }
            }
        }

        private global::PCL.MyListItem __LfPart_ItemUI;
        internal virtual global::PCL.MyListItem ItemUI
        {
            get
            {
                return __LfPart_ItemUI;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.IMyRadio.CheckEventHandler hook0 = new global::PCL.IMyRadio.CheckEventHandler(this.__LfHandle_2);
                global::PCL.MyListItem previous = __LfPart_ItemUI;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_ItemUI = value;
                global::PCL.MyListItem current = __LfPart_ItemUI;
                if (current != null)
                {
                    current.Check += hook0;
                }
            }
        }

        private global::PCL.MyListItem __LfPart_ItemSystem;
        internal virtual global::PCL.MyListItem ItemSystem
        {
            get
            {
                return __LfPart_ItemSystem;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.IMyRadio.CheckEventHandler hook0 = new global::PCL.IMyRadio.CheckEventHandler(this.__LfHandle_1);
                global::PCL.MyListItem previous = __LfPart_ItemSystem;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_ItemSystem = value;
                global::PCL.MyListItem current = __LfPart_ItemSystem;
                if (current != null)
                {
                    current.Check += hook0;
                }
            }
        }

        private void __LfHandle_0(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.PageCheck((global::PCL.MyListItem)arg0, (global::System.EventArgs)arg1);
        }

        private void __LfHandle_1(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.PageCheck((global::PCL.MyListItem)arg0, (global::System.EventArgs)arg1);
        }

        private void __LfHandle_2(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.PageCheck((global::PCL.MyListItem)arg0, (global::System.EventArgs)arg1);
        }

        private void __LfHandle_3(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.PageCheck((global::PCL.MyListItem)arg0, (global::System.EventArgs)arg1);
        }

        private void __LfAttachOwnHandles()
        {
            this.Loaded += new global::System.Windows.RoutedEventHandler(this.PageSetupLeft_Loaded);
            this.Unloaded += new global::System.Windows.RoutedEventHandler(this.PageOtherLeft_Unloaded);
        }
    }
}
