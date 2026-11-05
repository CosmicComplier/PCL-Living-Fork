// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class PageInstanceModDisabled
    {
        private global::PCL.MyCard __LfPart_PanMain;
        internal virtual global::PCL.MyCard PanMain
        {
            get
            {
                return __LfPart_PanMain;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanMain = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabModHint;
        internal virtual global::System.Windows.Controls.TextBlock LabModHint
        {
            get
            {
                return __LfPart_LabModHint;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabModHint = value;
            }
        }

        private global::PCL.MyButton __LfPart_BtnDownload;
        internal virtual global::PCL.MyButton BtnDownload
        {
            get
            {
                return __LfPart_BtnDownload;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_0);
                global::System.Windows.RoutedEventHandler hook1 = new global::System.Windows.RoutedEventHandler(this.__LfHandle_2);
                global::PCL.MyButton previous = __LfPart_BtnDownload;
                if (previous != null)
                {
                    previous.Click -= hook0;
                    previous.Loaded -= hook1;
                }

                __LfPart_BtnDownload = value;
                global::PCL.MyButton current = __LfPart_BtnDownload;
                if (current != null)
                {
                    current.Click += hook0;
                    current.Loaded += hook1;
                }
            }
        }

        private global::PCL.MyButton __LfPart_BtnVersion;
        internal virtual global::PCL.MyButton BtnVersion
        {
            get
            {
                return __LfPart_BtnVersion;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_1);
                global::PCL.MyButton previous = __LfPart_BtnVersion;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnVersion = value;
                global::PCL.MyButton current = __LfPart_BtnVersion;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private void __LfHandle_0(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.BtnDownload_Click((global::System.Object)arg0, (global::System.EventArgs)arg1);
        }

        private void __LfHandle_1(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.BtnVersion_Click((global::System.Object)arg0, (global::System.EventArgs)arg1);
        }

        private void __LfHandle_2(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.BtnDownload_Loaded();
        }
    }
}
