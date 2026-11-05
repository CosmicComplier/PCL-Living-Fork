// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class PageLoginAuthSkin
    {
        private global::System.Windows.Controls.Grid __LfPart_PanData;
        internal virtual global::System.Windows.Controls.Grid PanData
        {
            get
            {
                return __LfPart_PanData;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.Input.MouseEventHandler hook0 = new global::System.Windows.Input.MouseEventHandler(this.PageLoginAuthSkin_MouseEnter);
                global::System.Windows.Input.MouseEventHandler hook1 = new global::System.Windows.Input.MouseEventHandler(this.PageLoginAuthSkin_MouseLeave);
                global::System.Windows.Controls.Grid previous = __LfPart_PanData;
                if (previous != null)
                {
                    previous.MouseEnter -= hook0;
                    previous.MouseLeave -= hook1;
                }

                __LfPart_PanData = value;
                global::System.Windows.Controls.Grid current = __LfPart_PanData;
                if (current != null)
                {
                    current.MouseEnter += hook0;
                    current.MouseLeave += hook1;
                }
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_TextName;
        internal virtual global::System.Windows.Controls.TextBlock TextName
        {
            get
            {
                return __LfPart_TextName;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_TextName = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_TextEmail;
        internal virtual global::System.Windows.Controls.TextBlock TextEmail
        {
            get
            {
                return __LfPart_TextEmail;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_TextEmail = value;
            }
        }

        private global::PCL.MyIconButton __LfPart_BtnEdit;
        internal virtual global::PCL.MyIconButton BtnEdit
        {
            get
            {
                return __LfPart_BtnEdit;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconButton.ClickEventHandler hook0 = new global::PCL.MyIconButton.ClickEventHandler(this.BtnEdit_Click);
                global::PCL.MyIconButton previous = __LfPart_BtnEdit;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnEdit = value;
                global::PCL.MyIconButton current = __LfPart_BtnEdit;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyIconButton __LfPart_BtnExit;
        internal virtual global::PCL.MyIconButton BtnExit
        {
            get
            {
                return __LfPart_BtnExit;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconButton.ClickEventHandler hook0 = new global::PCL.MyIconButton.ClickEventHandler(this.__LfHandle_1);
                global::PCL.MyIconButton previous = __LfPart_BtnExit;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnExit = value;
                global::PCL.MyIconButton current = __LfPart_BtnExit;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MySkin __LfPart_Skin;
        internal virtual global::PCL.MySkin Skin
        {
            get
            {
                return __LfPart_Skin;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MySkin.ClickEventHandler hook0 = new global::PCL.MySkin.ClickEventHandler(this.Skin_Click);
                global::PCL.MySkin previous = __LfPart_Skin;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_Skin = value;
                global::PCL.MySkin current = __LfPart_Skin;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private void __LfHandle_0(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.PageLoginLegacy_Loaded();
        }

        private void __LfHandle_1(global::System.Object arg0, global::System.EventArgs arg1)
        {
            global::PCL.PageLoginAuthSkin.ExitLogin();
        }

        private void __LfAttachOwnHandles()
        {
            this.Loaded += new global::System.Windows.RoutedEventHandler(this.__LfHandle_0);
        }
    }
}
