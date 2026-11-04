// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class PageSelectRight
    {
        private global::System.Windows.Controls.Grid __LfPart_PanAllBack;
        internal virtual global::System.Windows.Controls.Grid PanAllBack
        {
            get
            {
                return __LfPart_PanAllBack;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanAllBack = value;
            }
        }

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

        private global::System.Windows.Controls.StackPanel __LfPart_PanMain;
        internal virtual global::System.Windows.Controls.StackPanel PanMain
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

        private global::PCL.MyCard __LfPart_PanEmpty;
        internal virtual global::PCL.MyCard PanEmpty
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

        private global::System.Windows.Controls.TextBlock __LfPart_LabEmptyTitle;
        internal virtual global::System.Windows.Controls.TextBlock LabEmptyTitle
        {
            get
            {
                return __LfPart_LabEmptyTitle;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabEmptyTitle = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabEmptyContent;
        internal virtual global::System.Windows.Controls.TextBlock LabEmptyContent
        {
            get
            {
                return __LfPart_LabEmptyContent;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabEmptyContent = value;
            }
        }

        private global::PCL.MyButton __LfPart_BtnEmptyDownload;
        internal virtual global::PCL.MyButton BtnEmptyDownload
        {
            get
            {
                return __LfPart_BtnEmptyDownload;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_1);
                global::System.Windows.RoutedEventHandler hook1 = new global::System.Windows.RoutedEventHandler(this.__LfHandle_2);
                global::PCL.MyButton previous = __LfPart_BtnEmptyDownload;
                if (previous != null)
                {
                    previous.Click -= hook0;
                    previous.Loaded -= hook1;
                }

                __LfPart_BtnEmptyDownload = value;
                global::PCL.MyButton current = __LfPart_BtnEmptyDownload;
                if (current != null)
                {
                    current.Click += hook0;
                    current.Loaded += hook1;
                }
            }
        }

        private global::PCL.MyCard __LfPart_PanLoad;
        internal virtual global::PCL.MyCard PanLoad
        {
            get
            {
                return __LfPart_PanLoad;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanLoad = value;
            }
        }

        private global::PCL.MyLoading __LfPart_Load;
        internal virtual global::PCL.MyLoading Load
        {
            get
            {
                return __LfPart_Load;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyLoading.ClickEventHandler hook0 = new global::PCL.MyLoading.ClickEventHandler(this.Load_Click);
                global::PCL.MyLoading previous = __LfPart_Load;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_Load = value;
                global::PCL.MyLoading current = __LfPart_Load;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private void __LfHandle_0(global::System.Object arg0, global::System.EventArgs arg1)
        {
            this.LoaderInit();
        }

        private void __LfHandle_1(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.BtnDownload_Click((global::System.Object)arg0, (global::System.EventArgs)arg1);
        }

        private void __LfHandle_2(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.BtnEmptyDownload_Loaded();
        }

        private void __LfAttachOwnHandles()
        {
            this.Loaded += new global::System.Windows.RoutedEventHandler(this.PageSelectRight_Loaded);
            this.Initialized += new global::System.EventHandler(this.__LfHandle_0);
        }
    }
}
