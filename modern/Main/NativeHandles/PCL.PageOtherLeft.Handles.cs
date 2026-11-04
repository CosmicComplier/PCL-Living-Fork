// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class PageOtherLeft
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

        private global::PCL.MyListItem __LfPart_ItemHelp;
        internal virtual global::PCL.MyListItem ItemHelp
        {
            get
            {
                return __LfPart_ItemHelp;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.IMyRadio.CheckEventHandler hook0 = new global::PCL.IMyRadio.CheckEventHandler(this.__LfHandle_1);
                global::PCL.MyListItem previous = __LfPart_ItemHelp;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_ItemHelp = value;
                global::PCL.MyListItem current = __LfPart_ItemHelp;
                if (current != null)
                {
                    current.Check += hook0;
                }
            }
        }

        private global::PCL.MyListItem __LfPart_ItemAbout;
        internal virtual global::PCL.MyListItem ItemAbout
        {
            get
            {
                return __LfPart_ItemAbout;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.IMyRadio.CheckEventHandler hook0 = new global::PCL.IMyRadio.CheckEventHandler(this.__LfHandle_0);
                global::PCL.MyListItem previous = __LfPart_ItemAbout;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_ItemAbout = value;
                global::PCL.MyListItem current = __LfPart_ItemAbout;
                if (current != null)
                {
                    current.Check += hook0;
                }
            }
        }

        private global::PCL.MyListItem __LfPart_ItemTest;
        internal virtual global::PCL.MyListItem ItemTest
        {
            get
            {
                return __LfPart_ItemTest;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.IMyRadio.CheckEventHandler hook0 = new global::PCL.IMyRadio.CheckEventHandler(this.__LfHandle_2);
                global::PCL.MyListItem previous = __LfPart_ItemTest;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_ItemTest = value;
                global::PCL.MyListItem current = __LfPart_ItemTest;
                if (current != null)
                {
                    current.Check += hook0;
                }
            }
        }

        private void __LfHandle_0(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.PageCheck((global::System.Windows.FrameworkElement)arg0, (global::PCL.ModBase.RouteEventArgs)arg1);
        }

        private void __LfHandle_1(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.PageCheck((global::System.Windows.FrameworkElement)arg0, (global::PCL.ModBase.RouteEventArgs)arg1);
        }

        private void __LfHandle_2(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.PageCheck((global::System.Windows.FrameworkElement)arg0, (global::PCL.ModBase.RouteEventArgs)arg1);
        }

        private void __LfAttachOwnHandles()
        {
            this.Loaded += new global::System.Windows.RoutedEventHandler(this.PageOtherLeft_Loaded);
            this.Unloaded += new global::System.Windows.RoutedEventHandler(this.PageOtherLeft_Unloaded);
        }
    }
}
