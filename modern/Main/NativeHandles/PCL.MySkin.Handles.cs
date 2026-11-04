// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class MySkin
    {
        private global::System.Windows.Media.Effects.DropShadowEffect __LfPart_ShadowSkin;
        internal virtual global::System.Windows.Media.Effects.DropShadowEffect ShadowSkin
        {
            get
            {
                return __LfPart_ShadowSkin;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ShadowSkin = value;
            }
        }

        private global::PCL.MyMenuItem __LfPart_BtnSkinSave;
        internal virtual global::PCL.MyMenuItem BtnSkinSave
        {
            get
            {
                return __LfPart_BtnSkinSave;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.RoutedEventHandler hook0 = new global::System.Windows.RoutedEventHandler(this.__LfHandle_0);
                global::System.Windows.RoutedEventHandler hook1 = new global::System.Windows.RoutedEventHandler(this.__LfHandle_1);
                global::PCL.MyMenuItem previous = __LfPart_BtnSkinSave;
                if (previous != null)
                {
                    previous.Click -= hook0;
                    previous.Checked -= hook1;
                }

                __LfPart_BtnSkinSave = value;
                global::PCL.MyMenuItem current = __LfPart_BtnSkinSave;
                if (current != null)
                {
                    current.Click += hook0;
                    current.Checked += hook1;
                }
            }
        }

        private global::PCL.MyMenuItem __LfPart_BtnSkinRefresh;
        internal virtual global::PCL.MyMenuItem BtnSkinRefresh
        {
            get
            {
                return __LfPart_BtnSkinRefresh;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.RoutedEventHandler hook0 = new global::System.Windows.RoutedEventHandler(this.__LfHandle_2);
                global::PCL.MyMenuItem previous = __LfPart_BtnSkinRefresh;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnSkinRefresh = value;
                global::PCL.MyMenuItem current = __LfPart_BtnSkinRefresh;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyMenuItem __LfPart_BtnSkinCape;
        internal virtual global::PCL.MyMenuItem BtnSkinCape
        {
            get
            {
                return __LfPart_BtnSkinCape;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.RoutedEventHandler hook0 = new global::System.Windows.RoutedEventHandler(this.__LfHandle_3);
                global::PCL.MyMenuItem previous = __LfPart_BtnSkinCape;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnSkinCape = value;
                global::PCL.MyMenuItem current = __LfPart_BtnSkinCape;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::System.Windows.Controls.Image __LfPart_ImgBack;
        internal virtual global::System.Windows.Controls.Image ImgBack
        {
            get
            {
                return __LfPart_ImgBack;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ImgBack = value;
            }
        }

        private global::System.Windows.Controls.Image __LfPart_ImgFore;
        internal virtual global::System.Windows.Controls.Image ImgFore
        {
            get
            {
                return __LfPart_ImgFore;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ImgFore = value;
            }
        }

        private void __LfHandle_0(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.BtnSkinSave_Click();
        }

        private void __LfHandle_1(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.BtnSkinSave_Checked((global::PCL.MyMenuItem)arg0, (global::System.Windows.RoutedEventArgs)arg1);
        }

        private void __LfHandle_2(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.RefreshClick();
        }

        private void __LfHandle_3(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.BtnSkinCape_Click();
        }

        private void __LfAttachOwnHandles()
        {
            this.MouseEnter += new global::System.Windows.Input.MouseEventHandler(this.PanSkin_MouseEnter);
            this.MouseLeave += new global::System.Windows.Input.MouseEventHandler(this.PanSkin_MouseLeave);
            this.MouseLeftButtonDown += new global::System.Windows.Input.MouseButtonEventHandler(this.PanSkin_MouseLeftButtonDown);
            this.MouseLeftButtonUp += new global::System.Windows.Input.MouseButtonEventHandler(this.PanSkin_MouseLeftButtonUp);
        }
    }
}
