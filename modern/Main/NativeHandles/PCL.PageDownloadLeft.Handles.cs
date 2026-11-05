// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class PageDownloadLeft
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

        private global::PCL.MyListItem __LfPart_ItemInstall;
        internal virtual global::PCL.MyListItem ItemInstall
        {
            get
            {
                return __LfPart_ItemInstall;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.IMyRadio.CheckEventHandler hook0 = new global::PCL.IMyRadio.CheckEventHandler(this.__LfHandle_0);
                global::PCL.MyListItem.ClickEventHandler hook1 = new global::PCL.MyListItem.ClickEventHandler(this.ItemInstall_Click);
                global::PCL.MyListItem previous = __LfPart_ItemInstall;
                if (previous != null)
                {
                    previous.Check -= hook0;
                    previous.Click -= hook1;
                }

                __LfPart_ItemInstall = value;
                global::PCL.MyListItem current = __LfPart_ItemInstall;
                if (current != null)
                {
                    current.Check += hook0;
                    current.Click += hook1;
                }
            }
        }

        private global::PCL.MyListItem __LfPart_ItemMod;
        internal virtual global::PCL.MyListItem ItemMod
        {
            get
            {
                return __LfPart_ItemMod;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.IMyRadio.CheckEventHandler hook0 = new global::PCL.IMyRadio.CheckEventHandler(this.__LfHandle_1);
                global::PCL.MyListItem previous = __LfPart_ItemMod;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_ItemMod = value;
                global::PCL.MyListItem current = __LfPart_ItemMod;
                if (current != null)
                {
                    current.Check += hook0;
                }
            }
        }

        private global::PCL.MyListItem __LfPart_ItemPack;
        internal virtual global::PCL.MyListItem ItemPack
        {
            get
            {
                return __LfPart_ItemPack;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.IMyRadio.CheckEventHandler hook0 = new global::PCL.IMyRadio.CheckEventHandler(this.__LfHandle_2);
                global::PCL.MyListItem previous = __LfPart_ItemPack;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_ItemPack = value;
                global::PCL.MyListItem current = __LfPart_ItemPack;
                if (current != null)
                {
                    current.Check += hook0;
                }
            }
        }

        private global::PCL.MyListItem __LfPart_ItemMap;
        internal virtual global::PCL.MyListItem ItemMap
        {
            get
            {
                return __LfPart_ItemMap;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.IMyRadio.CheckEventHandler hook0 = new global::PCL.IMyRadio.CheckEventHandler(this.__LfHandle_6);
                global::PCL.MyListItem previous = __LfPart_ItemMap;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_ItemMap = value;
                global::PCL.MyListItem current = __LfPart_ItemMap;
                if (current != null)
                {
                    current.Check += hook0;
                }
            }
        }

        private global::PCL.MyListItem __LfPart_ItemDataPack;
        internal virtual global::PCL.MyListItem ItemDataPack
        {
            get
            {
                return __LfPart_ItemDataPack;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.IMyRadio.CheckEventHandler hook0 = new global::PCL.IMyRadio.CheckEventHandler(this.__LfHandle_5);
                global::PCL.MyListItem previous = __LfPart_ItemDataPack;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_ItemDataPack = value;
                global::PCL.MyListItem current = __LfPart_ItemDataPack;
                if (current != null)
                {
                    current.Check += hook0;
                }
            }
        }

        private global::PCL.MyListItem __LfPart_ItemResourcePack;
        internal virtual global::PCL.MyListItem ItemResourcePack
        {
            get
            {
                return __LfPart_ItemResourcePack;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.IMyRadio.CheckEventHandler hook0 = new global::PCL.IMyRadio.CheckEventHandler(this.__LfHandle_3);
                global::PCL.MyListItem previous = __LfPart_ItemResourcePack;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_ItemResourcePack = value;
                global::PCL.MyListItem current = __LfPart_ItemResourcePack;
                if (current != null)
                {
                    current.Check += hook0;
                }
            }
        }

        private global::PCL.MyListItem __LfPart_ItemShader;
        internal virtual global::PCL.MyListItem ItemShader
        {
            get
            {
                return __LfPart_ItemShader;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.IMyRadio.CheckEventHandler hook0 = new global::PCL.IMyRadio.CheckEventHandler(this.__LfHandle_4);
                global::PCL.MyListItem previous = __LfPart_ItemShader;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_ItemShader = value;
                global::PCL.MyListItem current = __LfPart_ItemShader;
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

        private void __LfHandle_3(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.PageCheck((global::System.Windows.FrameworkElement)arg0, (global::PCL.ModBase.RouteEventArgs)arg1);
        }

        private void __LfHandle_4(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.PageCheck((global::System.Windows.FrameworkElement)arg0, (global::PCL.ModBase.RouteEventArgs)arg1);
        }

        private void __LfHandle_5(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.PageCheck((global::System.Windows.FrameworkElement)arg0, (global::PCL.ModBase.RouteEventArgs)arg1);
        }

        private void __LfHandle_6(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.PageCheck((global::System.Windows.FrameworkElement)arg0, (global::PCL.ModBase.RouteEventArgs)arg1);
        }

        private void __LfHandle_7(global::System.Object arg0, global::System.EventArgs arg1)
        {
            this.MapButtonInit();
        }

        private void __LfAttachOwnHandles()
        {
            this.Initialized += new global::System.EventHandler(this.__LfHandle_7);
        }
    }
}
