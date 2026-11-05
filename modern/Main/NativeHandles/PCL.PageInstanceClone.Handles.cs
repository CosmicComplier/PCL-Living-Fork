// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class PageInstanceClone
    {
        private global::PCL.MyScrollViewer __LfPart_PanBack;
        internal virtual global::PCL.MyScrollViewer PanBack
        {
            get
            {
                return __LfPart_PanBack;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanBack = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabSource;
        internal virtual global::System.Windows.Controls.TextBlock LabSource
        {
            get
            {
                return __LfPart_LabSource;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabSource = value;
            }
        }

        private global::PCL.MyTextBox __LfPart_TextName;
        internal virtual global::PCL.MyTextBox TextName
        {
            get
            {
                return __LfPart_TextName;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.Controls.TextChangedEventHandler hook0 = new global::System.Windows.Controls.TextChangedEventHandler(this.__LfHandle_2);
                global::PCL.MyTextBox previous = __LfPart_TextName;
                if (previous != null)
                {
                    previous.TextChanged -= hook0;
                }

                __LfPart_TextName = value;
                global::PCL.MyTextBox current = __LfPart_TextName;
                if (current != null)
                {
                    current.TextChanged += hook0;
                }
            }
        }

        private global::PCL.MyCheckBox __LfPart_CheckSaves;
        internal virtual global::PCL.MyCheckBox CheckSaves
        {
            get
            {
                return __LfPart_CheckSaves;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CheckSaves = value;
            }
        }

        private global::PCL.MyCheckBox __LfPart_CheckScreenshots;
        internal virtual global::PCL.MyCheckBox CheckScreenshots
        {
            get
            {
                return __LfPart_CheckScreenshots;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CheckScreenshots = value;
            }
        }

        private global::PCL.MyCheckBox __LfPart_CheckServers;
        internal virtual global::PCL.MyCheckBox CheckServers
        {
            get
            {
                return __LfPart_CheckServers;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CheckServers = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabValidation;
        internal virtual global::System.Windows.Controls.TextBlock LabValidation
        {
            get
            {
                return __LfPart_LabValidation;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabValidation = value;
            }
        }

        private global::PCL.MyButton __LfPart_BtnClone;
        internal virtual global::PCL.MyButton BtnClone
        {
            get
            {
                return __LfPart_BtnClone;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_3);
                global::PCL.MyButton previous = __LfPart_BtnClone;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnClone = value;
                global::PCL.MyButton current = __LfPart_BtnClone;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyCard __LfPart_CardOrigin;
        internal virtual global::PCL.MyCard CardOrigin
        {
            get
            {
                return __LfPart_CardOrigin;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CardOrigin = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabOrigin;
        internal virtual global::System.Windows.Controls.TextBlock LabOrigin
        {
            get
            {
                return __LfPart_LabOrigin;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabOrigin = value;
            }
        }

        private global::PCL.MyButton __LfPart_BtnMigrate;
        internal virtual global::PCL.MyButton BtnMigrate
        {
            get
            {
                return __LfPart_BtnMigrate;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_4);
                global::PCL.MyButton previous = __LfPart_BtnMigrate;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnMigrate = value;
                global::PCL.MyButton current = __LfPart_BtnMigrate;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private void __LfHandle_0(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.PageLoaded();
        }

        private void __LfHandle_1(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.PageUnloaded();
        }

        private void __LfHandle_2(global::System.Object arg0, global::System.Windows.Controls.TextChangedEventArgs arg1)
        {
            this.NameChanged();
        }

        private void __LfHandle_3(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.CloneVersion();
        }

        private void __LfHandle_4(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.MigrateVersion();
        }

        private void __LfAttachOwnHandles()
        {
            this.Loaded += new global::System.Windows.RoutedEventHandler(this.__LfHandle_0);
            this.Unloaded += new global::System.Windows.RoutedEventHandler(this.__LfHandle_1);
        }
    }
}
