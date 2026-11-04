// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class WorldBackupGroup
    {
        private global::PCL.MyCard __LfPart_CardBackups;
        internal virtual global::PCL.MyCard CardBackups
        {
            get
            {
                return __LfPart_CardBackups;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyCard.SwapEventHandler hook0 = new global::PCL.MyCard.SwapEventHandler(this.__LfHandle_1);
                global::PCL.MyCard previous = __LfPart_CardBackups;
                if (previous != null)
                {
                    previous.Swap -= hook0;
                }

                __LfPart_CardBackups = value;
                global::PCL.MyCard current = __LfPart_CardBackups;
                if (current != null)
                {
                    current.Swap += hook0;
                }
            }
        }

        private global::System.Windows.Controls.StackPanel __LfPart_PanContent;
        internal virtual global::System.Windows.Controls.StackPanel PanContent
        {
            get
            {
                return __LfPart_PanContent;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanContent = value;
            }
        }

        private global::PCL.MyIconTextButton __LfPart_BtnCreate;
        internal virtual global::PCL.MyIconTextButton BtnCreate
        {
            get
            {
                return __LfPart_BtnCreate;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconTextButton.ClickEventHandler hook0 = new global::PCL.MyIconTextButton.ClickEventHandler(this.__LfHandle_2);
                global::PCL.MyIconTextButton previous = __LfPart_BtnCreate;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnCreate = value;
                global::PCL.MyIconTextButton current = __LfPart_BtnCreate;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabState;
        internal virtual global::System.Windows.Controls.TextBlock LabState
        {
            get
            {
                return __LfPart_LabState;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabState = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabEmpty;
        internal virtual global::System.Windows.Controls.TextBlock LabEmpty
        {
            get
            {
                return __LfPart_LabEmpty;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabEmpty = value;
            }
        }

        private global::System.Windows.Controls.StackPanel __LfPart_PanEntries;
        internal virtual global::System.Windows.Controls.StackPanel PanEntries
        {
            get
            {
                return __LfPart_PanEntries;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanEntries = value;
            }
        }

        private void __LfHandle_0(global::System.Object arg0, global::System.EventArgs arg1)
        {
            this.Init();
        }

        private void __LfHandle_1(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.RememberExpansion();
        }

        private void __LfHandle_2(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.CreateBackup();
        }

        private void __LfAttachOwnHandles()
        {
            this.Initialized += new global::System.EventHandler(this.__LfHandle_0);
        }
    }
}
