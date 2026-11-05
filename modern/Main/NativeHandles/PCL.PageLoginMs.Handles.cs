// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class PageLoginMs
    {
        private global::System.Windows.Controls.Grid __LfPart_PanEmpty;
        internal virtual global::System.Windows.Controls.Grid PanEmpty
        {
            get
            {
                return __LfPart_PanEmpty;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanEmpty = value;
            }
        }

        private global::PCL.MyComboBox __LfPart_ComboAccounts;
        internal virtual global::PCL.MyComboBox ComboAccounts
        {
            get
            {
                return __LfPart_ComboAccounts;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.Controls.SelectionChangedEventHandler hook0 = new global::System.Windows.Controls.SelectionChangedEventHandler(this.ComboAccounts_SelectionChanged);
                global::PCL.MyComboBox previous = __LfPart_ComboAccounts;
                if (previous != null)
                {
                    previous.SelectionChanged -= hook0;
                }

                __LfPart_ComboAccounts = value;
                global::PCL.MyComboBox current = __LfPart_ComboAccounts;
                if (current != null)
                {
                    current.SelectionChanged += hook0;
                }
            }
        }

        private global::PCL.MyButton __LfPart_BtnLogin;
        internal virtual global::PCL.MyButton BtnLogin
        {
            get
            {
                return __LfPart_BtnLogin;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_0);
                global::PCL.MyButton previous = __LfPart_BtnLogin;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnLogin = value;
                global::PCL.MyButton current = __LfPart_BtnLogin;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private void __LfHandle_0(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.BtnLogin_Click((global::System.Object)arg0, (global::System.EventArgs)arg1);
        }
    }
}
