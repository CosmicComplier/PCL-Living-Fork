// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class PageSetupModpack
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

        private global::PCL.MyCard __LfPart_CardLauncher;
        internal virtual global::PCL.MyCard CardLauncher
        {
            get
            {
                return __LfPart_CardLauncher;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CardLauncher = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabVersion;
        internal virtual global::System.Windows.Controls.TextBlock LabVersion
        {
            get
            {
                return __LfPart_LabVersion;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabVersion = value;
            }
        }

        private global::PCL.MyTextBox __LfPart_TextSystemCache;
        internal virtual global::PCL.MyTextBox TextSystemCache
        {
            get
            {
                return __LfPart_TextSystemCache;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.RoutedEventHandler hook0 = new global::System.Windows.RoutedEventHandler(this.__LfHandle_1);
                global::PCL.MyTextBox previous = __LfPart_TextSystemCache;
                if (previous != null)
                {
                    previous.ValidatedTextChanged -= hook0;
                }

                __LfPart_TextSystemCache = value;
                global::PCL.MyTextBox current = __LfPart_TextSystemCache;
                if (current != null)
                {
                    current.ValidatedTextChanged += hook0;
                }
            }
        }

        private global::PCL.MyCheckBox __LfPart_CheckModpackMode;
        internal virtual global::PCL.MyCheckBox CheckModpackMode
        {
            get
            {
                return __LfPart_CheckModpackMode;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyCheckBox.ChangeEventHandler hook0 = new global::PCL.MyCheckBox.ChangeEventHandler(this.ModeChanged);
                global::PCL.MyCheckBox previous = __LfPart_CheckModpackMode;
                if (previous != null)
                {
                    previous.Change -= hook0;
                }

                __LfPart_CheckModpackMode = value;
                global::PCL.MyCheckBox current = __LfPart_CheckModpackMode;
                if (current != null)
                {
                    current.Change += hook0;
                }
            }
        }

        private void __LfHandle_0(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.PageLoaded();
        }

        private void __LfHandle_1(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.CacheChanged();
        }

        private void __LfAttachOwnHandles()
        {
            this.Loaded += new global::System.Windows.RoutedEventHandler(this.__LfHandle_0);
        }
    }
}
