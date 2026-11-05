// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class WorldBackupSettings
    {
        private global::PCL.MyCard __LfPart_CardSettings;
        internal virtual global::PCL.MyCard CardSettings
        {
            get
            {
                return __LfPart_CardSettings;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CardSettings = value;
            }
        }

        private global::System.Windows.Controls.StackPanel __LfPart_PanSettings;
        internal virtual global::System.Windows.Controls.StackPanel PanSettings
        {
            get
            {
                return __LfPart_PanSettings;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanSettings = value;
            }
        }

        private global::PCL.MyCheckBox __LfPart_CheckAuto;
        internal virtual global::PCL.MyCheckBox CheckAuto
        {
            get
            {
                return __LfPart_CheckAuto;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyCheckBox.ChangeEventHandler hook0 = new global::PCL.MyCheckBox.ChangeEventHandler(this.AutoChanged);
                global::PCL.MyCheckBox previous = __LfPart_CheckAuto;
                if (previous != null)
                {
                    previous.Change -= hook0;
                }

                __LfPart_CheckAuto = value;
                global::PCL.MyCheckBox current = __LfPart_CheckAuto;
                if (current != null)
                {
                    current.Change += hook0;
                }
            }
        }

        private global::PCL.MyTextBox __LfPart_TextRetention;
        internal virtual global::PCL.MyTextBox TextRetention
        {
            get
            {
                return __LfPart_TextRetention;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.Controls.TextChangedEventHandler hook0 = new global::System.Windows.Controls.TextChangedEventHandler(this.__LfHandle_1);
                global::System.Windows.Input.KeyboardFocusChangedEventHandler hook1 = new global::System.Windows.Input.KeyboardFocusChangedEventHandler(this.__LfHandle_2);
                global::System.Windows.Input.KeyEventHandler hook2 = new global::System.Windows.Input.KeyEventHandler(this.RetentionKey);
                global::PCL.MyTextBox previous = __LfPart_TextRetention;
                if (previous != null)
                {
                    previous.TextChanged -= hook0;
                    previous.LostKeyboardFocus -= hook1;
                    previous.PreviewKeyDown -= hook2;
                }

                __LfPart_TextRetention = value;
                global::PCL.MyTextBox current = __LfPart_TextRetention;
                if (current != null)
                {
                    current.TextChanged += hook0;
                    current.LostKeyboardFocus += hook1;
                    current.PreviewKeyDown += hook2;
                }
            }
        }

        private void __LfHandle_0(global::System.Object arg0, global::System.EventArgs arg1)
        {
            this.Init();
        }

        private void __LfHandle_1(global::System.Object arg0, global::System.Windows.Controls.TextChangedEventArgs arg1)
        {
            this.RetentionEdited();
        }

        private void __LfHandle_2(global::System.Object arg0, global::System.Windows.Input.KeyboardFocusChangedEventArgs arg1)
        {
            this.RetentionLostFocus();
        }

        private void __LfAttachOwnHandles()
        {
            this.Initialized += new global::System.EventHandler(this.__LfHandle_0);
        }
    }
}
