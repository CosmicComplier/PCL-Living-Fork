// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class MySearchBox
    {
        private global::PCL.MyTextBox __LfPart_TextBox;
        internal virtual global::PCL.MyTextBox TextBox
        {
            get
            {
                return __LfPart_TextBox;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.Controls.TextChangedEventHandler hook0 = new global::System.Windows.Controls.TextChangedEventHandler(this.Text_TextChanged);
                global::PCL.MyTextBox previous = __LfPart_TextBox;
                if (previous != null)
                {
                    previous.TextChanged -= hook0;
                }

                __LfPart_TextBox = value;
                global::PCL.MyTextBox current = __LfPart_TextBox;
                if (current != null)
                {
                    current.TextChanged += hook0;
                }
            }
        }

        private global::PCL.MyIconButton __LfPart_BtnClear;
        internal virtual global::PCL.MyIconButton BtnClear
        {
            get
            {
                return __LfPart_BtnClear;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconButton.ClickEventHandler hook0 = new global::PCL.MyIconButton.ClickEventHandler(this.BtnClear_Click);
                global::PCL.MyIconButton previous = __LfPart_BtnClear;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnClear = value;
                global::PCL.MyIconButton current = __LfPart_BtnClear;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private void __LfAttachOwnHandles()
        {
            this.Loaded += new global::System.Windows.RoutedEventHandler(this.MySearchBox_Loaded);
            this.KeyUp += new global::System.Windows.Input.KeyEventHandler(this.MySearchBox_KeyUp);
        }
    }
}
