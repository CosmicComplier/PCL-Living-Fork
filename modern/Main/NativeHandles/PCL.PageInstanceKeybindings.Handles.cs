// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class PageInstanceKeybindings
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

        private global::PCL.MySearchBox __LfPart_SearchBox;
        internal virtual global::PCL.MySearchBox SearchBox
        {
            get
            {
                return __LfPart_SearchBox;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MySearchBox.TextChangedEventHandler hook0 = new global::PCL.MySearchBox.TextChangedEventHandler(this.__LfHandle_2);
                global::PCL.MySearchBox previous = __LfPart_SearchBox;
                if (previous != null)
                {
                    previous.TextChanged -= hook0;
                }

                __LfPart_SearchBox = value;
                global::PCL.MySearchBox current = __LfPart_SearchBox;
                if (current != null)
                {
                    current.TextChanged += hook0;
                }
            }
        }

        private global::PCL.MyCard __LfPart_CardBindings;
        internal virtual global::PCL.MyCard CardBindings
        {
            get
            {
                return __LfPart_CardBindings;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CardBindings = value;
            }
        }

        private global::PCL.MyRadioButton __LfPart_BtnFilterAll;
        internal virtual global::PCL.MyRadioButton BtnFilterAll
        {
            get
            {
                return __LfPart_BtnFilterAll;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyRadioButton.CheckEventHandler hook0 = new global::PCL.MyRadioButton.CheckEventHandler(this.__LfHandle_3);
                global::PCL.MyRadioButton previous = __LfPart_BtnFilterAll;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_BtnFilterAll = value;
                global::PCL.MyRadioButton current = __LfPart_BtnFilterAll;
                if (current != null)
                {
                    current.Check += hook0;
                }
            }
        }

        private global::PCL.MyRadioButton __LfPart_BtnFilterConflicts;
        internal virtual global::PCL.MyRadioButton BtnFilterConflicts
        {
            get
            {
                return __LfPart_BtnFilterConflicts;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyRadioButton.CheckEventHandler hook0 = new global::PCL.MyRadioButton.CheckEventHandler(this.__LfHandle_4);
                global::PCL.MyRadioButton previous = __LfPart_BtnFilterConflicts;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_BtnFilterConflicts = value;
                global::PCL.MyRadioButton current = __LfPart_BtnFilterConflicts;
                if (current != null)
                {
                    current.Check += hook0;
                }
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabEmpty;
        internal virtual global::System.Windows.Controls.TextBlock LabEmpty
        {
            get
            {
                return __LfPart_LabEmpty;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabEmpty = value;
            }
        }

        private global::System.Windows.Controls.StackPanel __LfPart_PanBindings;
        internal virtual global::System.Windows.Controls.StackPanel PanBindings
        {
            get
            {
                return __LfPart_PanBindings;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanBindings = value;
            }
        }

        private global::PCL.MyCard __LfPart_CardCapture;
        internal virtual global::PCL.MyCard CardCapture
        {
            get
            {
                return __LfPart_CardCapture;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CardCapture = value;
            }
        }

        private global::System.Windows.Media.TranslateTransform __LfPart_TransCapture;
        internal virtual global::System.Windows.Media.TranslateTransform TransCapture
        {
            get
            {
                return __LfPart_TransCapture;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_TransCapture = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabCapture;
        internal virtual global::System.Windows.Controls.TextBlock LabCapture
        {
            get
            {
                return __LfPart_LabCapture;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabCapture = value;
            }
        }

        private global::PCL.MyIconTextButton __LfPart_BtnClear;
        internal virtual global::PCL.MyIconTextButton BtnClear
        {
            get
            {
                return __LfPart_BtnClear;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconTextButton.ClickEventHandler hook0 = new global::PCL.MyIconTextButton.ClickEventHandler(this.__LfHandle_6);
                global::PCL.MyIconTextButton previous = __LfPart_BtnClear;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnClear = value;
                global::PCL.MyIconTextButton current = __LfPart_BtnClear;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyIconTextButton __LfPart_BtnCancel;
        internal virtual global::PCL.MyIconTextButton BtnCancel
        {
            get
            {
                return __LfPart_BtnCancel;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconTextButton.ClickEventHandler hook0 = new global::PCL.MyIconTextButton.ClickEventHandler(this.__LfHandle_5);
                global::PCL.MyIconTextButton previous = __LfPart_BtnCancel;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnCancel = value;
                global::PCL.MyIconTextButton current = __LfPart_BtnCancel;
                if (current != null)
                {
                    current.Click += hook0;
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
                global::PCL.MyLoading.ClickEventHandler hook0 = new global::PCL.MyLoading.ClickEventHandler(this.__LfHandle_7);
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

        private void __LfHandle_0(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.PageLoaded();
        }

        private void __LfHandle_1(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.PageUnloaded();
        }

        private void __LfHandle_2(global::System.Object arg0, global::System.EventArgs arg1)
        {
            this.FilterChanged();
        }

        private void __LfHandle_3(global::System.Object arg0, global::System.Boolean arg1)
        {
            this.FilterChecked((global::PCL.MyRadioButton)arg0, (global::System.Boolean)arg1);
        }

        private void __LfHandle_4(global::System.Object arg0, global::System.Boolean arg1)
        {
            this.FilterChecked((global::PCL.MyRadioButton)arg0, (global::System.Boolean)arg1);
        }

        private void __LfHandle_5(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.CancelCapture();
        }

        private void __LfHandle_6(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.ClearBinding();
        }

        private void __LfHandle_7(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.RetryLoad();
        }

        private void __LfAttachOwnHandles()
        {
            this.Loaded += new global::System.Windows.RoutedEventHandler(this.__LfHandle_0);
            this.Unloaded += new global::System.Windows.RoutedEventHandler(this.__LfHandle_1);
        }
    }
}
