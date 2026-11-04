// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class PageLoginMsSkin
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
                global::System.Windows.Input.MouseEventHandler hook0 = new global::System.Windows.Input.MouseEventHandler(this.ShowPanel);
                global::System.Windows.Input.MouseEventHandler hook1 = new global::System.Windows.Input.MouseEventHandler(this.__LfHandle_0);
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

        private global::System.Windows.Controls.Border __LfPart_PanButtons;
        internal virtual global::System.Windows.Controls.Border PanButtons
        {
            get
            {
                return __LfPart_PanButtons;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanButtons = value;
            }
        }

        private global::PCL.MyIconButton __LfPart_BtnSkin;
        internal virtual global::PCL.MyIconButton BtnSkin
        {
            get
            {
                return __LfPart_BtnSkin;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconButton.ClickEventHandler hook0 = new global::PCL.MyIconButton.ClickEventHandler(this.__LfHandle_2);
                global::PCL.MyIconButton previous = __LfPart_BtnSkin;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnSkin = value;
                global::PCL.MyIconButton current = __LfPart_BtnSkin;
                if (current != null)
                {
                    current.Click += hook0;
                }
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
                __LfPart_Skin = value;
            }
        }

        private void __LfHandle_0(global::System.Object arg0, global::System.Windows.Input.MouseEventArgs arg1)
        {
            this.HidePanel((global::System.Object)arg0, (global::System.EventArgs)arg1);
        }

        private void __LfHandle_1(global::System.Object arg0, global::System.EventArgs arg1)
        {
            this.BtnExit_Click();
        }

        private void __LfHandle_2(global::System.Object arg0, global::System.EventArgs arg1)
        {
            this.BtnSkin_Click((global::System.Object)arg0, (global::System.Windows.RoutedEventArgs)arg1);
        }

        private void __LfAttachOwnHandles()
        {
            this.Loaded += new global::System.Windows.RoutedEventHandler(this.PageLoginLegacy_Loaded);
        }
    }
}
