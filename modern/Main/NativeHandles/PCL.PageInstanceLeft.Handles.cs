// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class PageInstanceLeft
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

        private global::PCL.MyListItem __LfPart_ItemOverall;
        internal virtual global::PCL.MyListItem ItemOverall
        {
            get
            {
                return __LfPart_ItemOverall;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.IMyRadio.CheckEventHandler hook0 = new global::PCL.IMyRadio.CheckEventHandler(this.__LfHandle_2);
                global::PCL.MyListItem previous = __LfPart_ItemOverall;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_ItemOverall = value;
                global::PCL.MyListItem current = __LfPart_ItemOverall;
                if (current != null)
                {
                    current.Check += hook0;
                }
            }
        }

        private global::PCL.MyListItem __LfPart_ItemSetup;
        internal virtual global::PCL.MyListItem ItemSetup
        {
            get
            {
                return __LfPart_ItemSetup;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.IMyRadio.CheckEventHandler hook0 = new global::PCL.IMyRadio.CheckEventHandler(this.__LfHandle_5);
                global::PCL.MyListItem previous = __LfPart_ItemSetup;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_ItemSetup = value;
                global::PCL.MyListItem current = __LfPart_ItemSetup;
                if (current != null)
                {
                    current.Check += hook0;
                }
            }
        }

        private global::PCL.MyListItem __LfPart_ItemModify;
        internal virtual global::PCL.MyListItem ItemModify
        {
            get
            {
                return __LfPart_ItemModify;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.IMyRadio.CheckEventHandler hook0 = new global::PCL.IMyRadio.CheckEventHandler(this.__LfHandle_6);
                global::PCL.MyListItem previous = __LfPart_ItemModify;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_ItemModify = value;
                global::PCL.MyListItem current = __LfPart_ItemModify;
                if (current != null)
                {
                    current.Check += hook0;
                }
            }
        }

        private global::PCL.MyListItem __LfPart_ItemExport;
        internal virtual global::PCL.MyListItem ItemExport
        {
            get
            {
                return __LfPart_ItemExport;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.IMyRadio.CheckEventHandler hook0 = new global::PCL.IMyRadio.CheckEventHandler(this.__LfHandle_7);
                global::PCL.MyListItem previous = __LfPart_ItemExport;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_ItemExport = value;
                global::PCL.MyListItem current = __LfPart_ItemExport;
                if (current != null)
                {
                    current.Check += hook0;
                }
            }
        }

        private global::PCL.MyListItem __LfPart_ItemKeybindings;
        internal virtual global::PCL.MyListItem ItemKeybindings
        {
            get
            {
                return __LfPart_ItemKeybindings;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.IMyRadio.CheckEventHandler hook0 = new global::PCL.IMyRadio.CheckEventHandler(this.__LfHandle_13);
                global::PCL.MyListItem previous = __LfPart_ItemKeybindings;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_ItemKeybindings = value;
                global::PCL.MyListItem current = __LfPart_ItemKeybindings;
                if (current != null)
                {
                    current.Check += hook0;
                }
            }
        }

        private global::PCL.MyListItem __LfPart_ItemRestorePoints;
        internal virtual global::PCL.MyListItem ItemRestorePoints
        {
            get
            {
                return __LfPart_ItemRestorePoints;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.IMyRadio.CheckEventHandler hook0 = new global::PCL.IMyRadio.CheckEventHandler(this.__LfHandle_14);
                global::PCL.MyListItem previous = __LfPart_ItemRestorePoints;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_ItemRestorePoints = value;
                global::PCL.MyListItem current = __LfPart_ItemRestorePoints;
                if (current != null)
                {
                    current.Check += hook0;
                }
            }
        }

        private global::PCL.MyListItem __LfPart_ItemClone;
        internal virtual global::PCL.MyListItem ItemClone
        {
            get
            {
                return __LfPart_ItemClone;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.IMyRadio.CheckEventHandler hook0 = new global::PCL.IMyRadio.CheckEventHandler(this.__LfHandle_15);
                global::PCL.MyListItem previous = __LfPart_ItemClone;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_ItemClone = value;
                global::PCL.MyListItem current = __LfPart_ItemClone;
                if (current != null)
                {
                    current.Check += hook0;
                }
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabGameResources;
        internal virtual global::System.Windows.Controls.TextBlock LabGameResources
        {
            get
            {
                return __LfPart_LabGameResources;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabGameResources = value;
            }
        }

        private global::PCL.MyListItem __LfPart_ItemSaves;
        internal virtual global::PCL.MyListItem ItemSaves
        {
            get
            {
                return __LfPart_ItemSaves;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.IMyRadio.CheckEventHandler hook0 = new global::PCL.IMyRadio.CheckEventHandler(this.__LfHandle_8);
                global::PCL.MyListItem previous = __LfPart_ItemSaves;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_ItemSaves = value;
                global::PCL.MyListItem current = __LfPart_ItemSaves;
                if (current != null)
                {
                    current.Check += hook0;
                }
            }
        }

        private global::PCL.MyListItem __LfPart_ItemScreenshots;
        internal virtual global::PCL.MyListItem ItemScreenshots
        {
            get
            {
                return __LfPart_ItemScreenshots;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.IMyRadio.CheckEventHandler hook0 = new global::PCL.IMyRadio.CheckEventHandler(this.__LfHandle_9);
                global::PCL.MyListItem previous = __LfPart_ItemScreenshots;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_ItemScreenshots = value;
                global::PCL.MyListItem current = __LfPart_ItemScreenshots;
                if (current != null)
                {
                    current.Check += hook0;
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
                global::PCL.IMyRadio.CheckEventHandler hook0 = new global::PCL.IMyRadio.CheckEventHandler(this.__LfHandle_3);
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

        private global::PCL.MyListItem __LfPart_ItemModDisabled;
        internal virtual global::PCL.MyListItem ItemModDisabled
        {
            get
            {
                return __LfPart_ItemModDisabled;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.IMyRadio.CheckEventHandler hook0 = new global::PCL.IMyRadio.CheckEventHandler(this.__LfHandle_4);
                global::PCL.MyListItem previous = __LfPart_ItemModDisabled;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_ItemModDisabled = value;
                global::PCL.MyListItem current = __LfPart_ItemModDisabled;
                if (current != null)
                {
                    current.Check += hook0;
                }
            }
        }

        private global::PCL.MyListItem __LfPart_ItemResourcePacks;
        internal virtual global::PCL.MyListItem ItemResourcePacks
        {
            get
            {
                return __LfPart_ItemResourcePacks;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.IMyRadio.CheckEventHandler hook0 = new global::PCL.IMyRadio.CheckEventHandler(this.__LfHandle_10);
                global::PCL.MyListItem previous = __LfPart_ItemResourcePacks;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_ItemResourcePacks = value;
                global::PCL.MyListItem current = __LfPart_ItemResourcePacks;
                if (current != null)
                {
                    current.Check += hook0;
                }
            }
        }

        private global::PCL.MyListItem __LfPart_ItemShaderPacks;
        internal virtual global::PCL.MyListItem ItemShaderPacks
        {
            get
            {
                return __LfPart_ItemShaderPacks;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.IMyRadio.CheckEventHandler hook0 = new global::PCL.IMyRadio.CheckEventHandler(this.__LfHandle_11);
                global::PCL.MyListItem previous = __LfPart_ItemShaderPacks;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_ItemShaderPacks = value;
                global::PCL.MyListItem current = __LfPart_ItemShaderPacks;
                if (current != null)
                {
                    current.Check += hook0;
                }
            }
        }

        private global::PCL.MyListItem __LfPart_ItemSchematics;
        internal virtual global::PCL.MyListItem ItemSchematics
        {
            get
            {
                return __LfPart_ItemSchematics;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.IMyRadio.CheckEventHandler hook0 = new global::PCL.IMyRadio.CheckEventHandler(this.__LfHandle_12);
                global::PCL.MyListItem previous = __LfPart_ItemSchematics;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_ItemSchematics = value;
                global::PCL.MyListItem current = __LfPart_ItemSchematics;
                if (current != null)
                {
                    current.Check += hook0;
                }
            }
        }

        private void __LfHandle_0(global::System.Object arg0, global::System.EventArgs arg1)
        {
            this.ResourceButtonsInit();
        }

        private void __LfHandle_1(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.RefreshModDisabled();
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

        private void __LfHandle_7(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.PageCheck((global::System.Windows.FrameworkElement)arg0, (global::PCL.ModBase.RouteEventArgs)arg1);
        }

        private void __LfHandle_8(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.PageCheck((global::System.Windows.FrameworkElement)arg0, (global::PCL.ModBase.RouteEventArgs)arg1);
        }

        private void __LfHandle_9(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.PageCheck((global::System.Windows.FrameworkElement)arg0, (global::PCL.ModBase.RouteEventArgs)arg1);
        }

        private void __LfHandle_10(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.PageCheck((global::System.Windows.FrameworkElement)arg0, (global::PCL.ModBase.RouteEventArgs)arg1);
        }

        private void __LfHandle_11(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.PageCheck((global::System.Windows.FrameworkElement)arg0, (global::PCL.ModBase.RouteEventArgs)arg1);
        }

        private void __LfHandle_12(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.PageCheck((global::System.Windows.FrameworkElement)arg0, (global::PCL.ModBase.RouteEventArgs)arg1);
        }

        private void __LfHandle_13(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.PageCheck((global::System.Windows.FrameworkElement)arg0, (global::PCL.ModBase.RouteEventArgs)arg1);
        }

        private void __LfHandle_14(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.PageCheck((global::System.Windows.FrameworkElement)arg0, (global::PCL.ModBase.RouteEventArgs)arg1);
        }

        private void __LfHandle_15(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.PageCheck((global::System.Windows.FrameworkElement)arg0, (global::PCL.ModBase.RouteEventArgs)arg1);
        }

        private void __LfHandle_16(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            global::PCL.PageInstanceLeft.ReloadCurrentJava();
        }

        private void __LfAttachOwnHandles()
        {
            this.Loaded += new global::System.Windows.RoutedEventHandler(this.PageInstanceLeft_Loaded);
            this.Initialized += new global::System.EventHandler(this.__LfHandle_0);
            this.Loaded += new global::System.Windows.RoutedEventHandler(this.__LfHandle_1);
            this.Loaded += new global::System.Windows.RoutedEventHandler(this.__LfHandle_16);
        }
    }
}
