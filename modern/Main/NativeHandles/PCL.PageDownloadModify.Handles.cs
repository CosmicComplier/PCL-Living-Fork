// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class PageDownloadModify
    {
        internal override global::PCL.MyTextBox TextSelectName
        {
            get
            {
                return base.TextSelectName;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.Controls.TextChangedEventHandler hook0 = new global::System.Windows.Controls.TextChangedEventHandler(this.__LfHandle_1);
                global::PCL.MyTextBox.ValidateChangedEventHandler hook1 = new global::PCL.MyTextBox.ValidateChangedEventHandler(this.__LfHandle_2);
                global::System.Windows.Controls.TextChangedEventHandler hook2 = new global::System.Windows.Controls.TextChangedEventHandler(this.__LfHandle_3);
                global::PCL.MyTextBox previous = base.TextSelectName;
                if (previous != null)
                {
                    previous.TextChanged -= hook0;
                    global::PCL.MyTextBox.ValidateChanged -= hook1;
                    previous.TextChanged -= hook2;
                }

                base.TextSelectName = value;
                global::PCL.MyTextBox current = base.TextSelectName;
                if (current != null)
                {
                    current.TextChanged += hook0;
                    global::PCL.MyTextBox.ValidateChanged += hook1;
                    current.TextChanged += hook2;
                }
            }
        }

        internal override global::PCL.MyIconButton BtnOriginalNameRestore
        {
            get
            {
                return base.BtnOriginalNameRestore;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconButton.ClickEventHandler hook0 = new global::PCL.MyIconButton.ClickEventHandler(this.BtnOriginalNameRestore_Click);
                global::PCL.MyIconButton previous = base.BtnOriginalNameRestore;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                base.BtnOriginalNameRestore = value;
                global::PCL.MyIconButton current = base.BtnOriginalNameRestore;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        internal override global::PCL.MyCard CardMinecraft
        {
            get
            {
                return base.CardMinecraft;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.Input.MouseButtonEventHandler hook0 = new global::System.Windows.Input.MouseButtonEventHandler(this.CardMinecraft_MouseLeftButtonUp);
                global::PCL.MyCard previous = base.CardMinecraft;
                if (previous != null)
                {
                    previous.MouseLeftButtonUp -= hook0;
                }

                base.CardMinecraft = value;
                global::PCL.MyCard current = base.CardMinecraft;
                if (current != null)
                {
                    current.MouseLeftButtonUp += hook0;
                }
            }
        }

        internal override global::PCL.MyIconButton BtnMinecraftRestore
        {
            get
            {
                return base.BtnMinecraftRestore;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconButton.ClickEventHandler hook0 = new global::PCL.MyIconButton.ClickEventHandler(this.RestoreComponent);
                global::PCL.MyIconButton previous = base.BtnMinecraftRestore;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                base.BtnMinecraftRestore = value;
                global::PCL.MyIconButton current = base.BtnMinecraftRestore;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        internal override global::PCL.MyIconButton BtnForgeRestore
        {
            get
            {
                return base.BtnForgeRestore;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconButton.ClickEventHandler hook0 = new global::PCL.MyIconButton.ClickEventHandler(this.RestoreComponent);
                global::PCL.MyIconButton previous = base.BtnForgeRestore;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                base.BtnForgeRestore = value;
                global::PCL.MyIconButton current = base.BtnForgeRestore;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        internal override global::PCL.MyIconButton BtnNeoForgeRestore
        {
            get
            {
                return base.BtnNeoForgeRestore;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconButton.ClickEventHandler hook0 = new global::PCL.MyIconButton.ClickEventHandler(this.RestoreComponent);
                global::PCL.MyIconButton previous = base.BtnNeoForgeRestore;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                base.BtnNeoForgeRestore = value;
                global::PCL.MyIconButton current = base.BtnNeoForgeRestore;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        internal override global::PCL.MyIconButton BtnFabricRestore
        {
            get
            {
                return base.BtnFabricRestore;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconButton.ClickEventHandler hook0 = new global::PCL.MyIconButton.ClickEventHandler(this.RestoreComponent);
                global::PCL.MyIconButton previous = base.BtnFabricRestore;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                base.BtnFabricRestore = value;
                global::PCL.MyIconButton current = base.BtnFabricRestore;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        internal override global::PCL.MyIconButton BtnFabricApiRestore
        {
            get
            {
                return base.BtnFabricApiRestore;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconButton.ClickEventHandler hook0 = new global::PCL.MyIconButton.ClickEventHandler(this.RestoreComponent);
                global::PCL.MyIconButton previous = base.BtnFabricApiRestore;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                base.BtnFabricApiRestore = value;
                global::PCL.MyIconButton current = base.BtnFabricApiRestore;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        internal override global::PCL.MyIconButton BtnOptiFineRestore
        {
            get
            {
                return base.BtnOptiFineRestore;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconButton.ClickEventHandler hook0 = new global::PCL.MyIconButton.ClickEventHandler(this.RestoreComponent);
                global::PCL.MyIconButton previous = base.BtnOptiFineRestore;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                base.BtnOptiFineRestore = value;
                global::PCL.MyIconButton current = base.BtnOptiFineRestore;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        internal override global::PCL.MyIconButton BtnOptiFabricRestore
        {
            get
            {
                return base.BtnOptiFabricRestore;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconButton.ClickEventHandler hook0 = new global::PCL.MyIconButton.ClickEventHandler(this.RestoreComponent);
                global::PCL.MyIconButton previous = base.BtnOptiFabricRestore;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                base.BtnOptiFabricRestore = value;
                global::PCL.MyIconButton current = base.BtnOptiFabricRestore;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        internal override global::PCL.MyIconButton BtnLiteLoaderRestore
        {
            get
            {
                return base.BtnLiteLoaderRestore;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconButton.ClickEventHandler hook0 = new global::PCL.MyIconButton.ClickEventHandler(this.RestoreComponent);
                global::PCL.MyIconButton previous = base.BtnLiteLoaderRestore;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                base.BtnLiteLoaderRestore = value;
                global::PCL.MyIconButton current = base.BtnLiteLoaderRestore;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private void __LfHandle_0(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.PageUnloaded();
        }

        private void __LfHandle_1(global::System.Object arg0, global::System.Windows.Controls.TextChangedEventArgs arg1)
        {
            this.UpdateStartButton();
        }

        private void __LfHandle_2(global::System.Object arg0, global::System.EventArgs arg1)
        {
            this.UpdateStartButton();
        }

        private void __LfHandle_3(global::System.Object arg0, global::System.Windows.Controls.TextChangedEventArgs arg1)
        {
            this.UpdateOriginalNameVisibility();
        }

        private void __LfAttachOwnHandles()
        {
            this.Unloaded += new global::System.Windows.RoutedEventHandler(this.__LfHandle_0);
        }
    }
}
