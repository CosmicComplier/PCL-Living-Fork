// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class PageOtherTest
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

        private global::PCL.MyButton __LfPart_BtnMemory;
        internal virtual global::PCL.MyButton BtnMemory
        {
            get
            {
                return __LfPart_BtnMemory;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_10);
                global::PCL.MyButton previous = __LfPart_BtnMemory;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnMemory = value;
                global::PCL.MyButton current = __LfPart_BtnMemory;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyButton __LfPart_BtnClear;
        internal virtual global::PCL.MyButton BtnClear
        {
            get
            {
                return __LfPart_BtnClear;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_9);
                global::PCL.MyButton previous = __LfPart_BtnClear;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnClear = value;
                global::PCL.MyButton current = __LfPart_BtnClear;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyTextBox __LfPart_TextDownloadUrl;
        internal virtual global::PCL.MyTextBox TextDownloadUrl
        {
            get
            {
                return __LfPart_TextDownloadUrl;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.Controls.TextChangedEventHandler hook0 = new global::System.Windows.Controls.TextChangedEventHandler(this.__LfHandle_1);
                global::System.Windows.Controls.TextChangedEventHandler hook1 = new global::System.Windows.Controls.TextChangedEventHandler(this.__LfHandle_4);
                global::System.Windows.Input.KeyEventHandler hook2 = new global::System.Windows.Input.KeyEventHandler(this.TextDownloadUrl_KeyDown);
                global::PCL.MyTextBox previous = __LfPart_TextDownloadUrl;
                if (previous != null)
                {
                    previous.TextChanged -= hook0;
                    previous.TextChanged -= hook1;
                    previous.KeyDown -= hook2;
                }

                __LfPart_TextDownloadUrl = value;
                global::PCL.MyTextBox current = __LfPart_TextDownloadUrl;
                if (current != null)
                {
                    current.TextChanged += hook0;
                    current.TextChanged += hook1;
                    current.KeyDown += hook2;
                }
            }
        }

        private global::PCL.MyTextBox __LfPart_TextDownloadFolder;
        internal virtual global::PCL.MyTextBox TextDownloadFolder
        {
            get
            {
                return __LfPart_TextDownloadFolder;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.Controls.TextChangedEventHandler hook0 = new global::System.Windows.Controls.TextChangedEventHandler(this.__LfHandle_2);
                global::System.Windows.RoutedEventHandler hook1 = new global::System.Windows.RoutedEventHandler(this.__LfHandle_5);
                global::PCL.MyTextBox previous = __LfPart_TextDownloadFolder;
                if (previous != null)
                {
                    previous.TextChanged -= hook0;
                    previous.ValidatedTextChanged -= hook1;
                }

                __LfPart_TextDownloadFolder = value;
                global::PCL.MyTextBox current = __LfPart_TextDownloadFolder;
                if (current != null)
                {
                    current.TextChanged += hook0;
                    current.ValidatedTextChanged += hook1;
                }
            }
        }

        private global::PCL.MyButton __LfPart_BtnDownloadSelect;
        internal virtual global::PCL.MyButton BtnDownloadSelect
        {
            get
            {
                return __LfPart_BtnDownloadSelect;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_7);
                global::PCL.MyButton previous = __LfPart_BtnDownloadSelect;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnDownloadSelect = value;
                global::PCL.MyButton current = __LfPart_BtnDownloadSelect;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyTextBox __LfPart_TextDownloadName;
        internal virtual global::PCL.MyTextBox TextDownloadName
        {
            get
            {
                return __LfPart_TextDownloadName;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.Controls.TextChangedEventHandler hook0 = new global::System.Windows.Controls.TextChangedEventHandler(this.__LfHandle_3);
                global::System.Windows.Input.KeyEventHandler hook1 = new global::System.Windows.Input.KeyEventHandler(this.TextDownloadUrl_KeyDown);
                global::PCL.MyTextBox previous = __LfPart_TextDownloadName;
                if (previous != null)
                {
                    previous.TextChanged -= hook0;
                    previous.KeyDown -= hook1;
                }

                __LfPart_TextDownloadName = value;
                global::PCL.MyTextBox current = __LfPart_TextDownloadName;
                if (current != null)
                {
                    current.TextChanged += hook0;
                    current.KeyDown += hook1;
                }
            }
        }

        private global::PCL.MyButton __LfPart_BtnDownloadStart;
        internal virtual global::PCL.MyButton BtnDownloadStart
        {
            get
            {
                return __LfPart_BtnDownloadStart;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_6);
                global::PCL.MyButton previous = __LfPart_BtnDownloadStart;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnDownloadStart = value;
                global::PCL.MyButton current = __LfPart_BtnDownloadStart;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyButton __LfPart_BtnDownloadOpen;
        internal virtual global::PCL.MyButton BtnDownloadOpen
        {
            get
            {
                return __LfPart_BtnDownloadOpen;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_8);
                global::PCL.MyButton previous = __LfPart_BtnDownloadOpen;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnDownloadOpen = value;
                global::PCL.MyButton current = __LfPart_BtnDownloadOpen;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private void __LfHandle_0(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.MeLoaded();
        }

        private void __LfHandle_1(global::System.Object arg0, global::System.Windows.Controls.TextChangedEventArgs arg1)
        {
            this.StartButtonRefresh();
        }

        private void __LfHandle_2(global::System.Object arg0, global::System.Windows.Controls.TextChangedEventArgs arg1)
        {
            this.StartButtonRefresh();
        }

        private void __LfHandle_3(global::System.Object arg0, global::System.Windows.Controls.TextChangedEventArgs arg1)
        {
            this.StartButtonRefresh();
        }

        private void __LfHandle_4(global::System.Object arg0, global::System.Windows.Controls.TextChangedEventArgs arg1)
        {
            this.TextDownloadUrl_TextChanged();
        }

        private void __LfHandle_5(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.SaveCacheDownloadFolder();
        }

        private void __LfHandle_6(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.BtnDownloadStart_Click();
        }

        private void __LfHandle_7(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.BtnDownloadSelect_Click();
        }

        private void __LfHandle_8(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.BtnDownloadOpen_Click();
        }

        private void __LfHandle_9(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.BtnClear_Click();
        }

        private void __LfHandle_10(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.BtnMemory_Click();
        }

        private void __LfAttachOwnHandles()
        {
            this.Loaded += new global::System.Windows.RoutedEventHandler(this.__LfHandle_0);
        }
    }
}
