// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class PageInstanceRestorePoints
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
                global::PCL.MySearchBox.TextChangedEventHandler hook0 = new global::PCL.MySearchBox.TextChangedEventHandler(this.__LfHandle_4);
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

        private global::PCL.MyButton __LfPart_BtnFolder;
        internal virtual global::PCL.MyButton BtnFolder
        {
            get
            {
                return __LfPart_BtnFolder;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_12);
                global::PCL.MyButton previous = __LfPart_BtnFolder;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnFolder = value;
                global::PCL.MyButton current = __LfPart_BtnFolder;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyButton __LfPart_BtnCreate;
        internal virtual global::PCL.MyButton BtnCreate
        {
            get
            {
                return __LfPart_BtnCreate;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_10);
                global::PCL.MyButton previous = __LfPart_BtnCreate;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnCreate = value;
                global::PCL.MyButton current = __LfPart_BtnCreate;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyButton __LfPart_BtnSelectAll;
        internal virtual global::PCL.MyButton BtnSelectAll
        {
            get
            {
                return __LfPart_BtnSelectAll;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_7);
                global::PCL.MyButton previous = __LfPart_BtnSelectAll;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnSelectAll = value;
                global::PCL.MyButton current = __LfPart_BtnSelectAll;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyCard __LfPart_CardRetention;
        internal virtual global::PCL.MyCard CardRetention
        {
            get
            {
                return __LfPart_CardRetention;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CardRetention = value;
            }
        }

        private global::PCL.MyCheckBox __LfPart_CheckAutoCleanup;
        internal virtual global::PCL.MyCheckBox CheckAutoCleanup
        {
            get
            {
                return __LfPart_CheckAutoCleanup;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyCheckBox.ChangeEventHandler hook0 = new global::PCL.MyCheckBox.ChangeEventHandler(this.AutoCleanupChanged);
                global::PCL.MyCheckBox previous = __LfPart_CheckAutoCleanup;
                if (previous != null)
                {
                    previous.Change -= hook0;
                }

                __LfPart_CheckAutoCleanup = value;
                global::PCL.MyCheckBox current = __LfPart_CheckAutoCleanup;
                if (current != null)
                {
                    current.Change += hook0;
                }
            }
        }

        private global::System.Windows.Controls.StackPanel __LfPart_PanRetention;
        internal virtual global::System.Windows.Controls.StackPanel PanRetention
        {
            get
            {
                return __LfPart_PanRetention;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanRetention = value;
            }
        }

        private global::PCL.MyTextBox __LfPart_TextRetention;
        internal virtual global::PCL.MyTextBox TextRetention
        {
            get
            {
                return __LfPart_TextRetention;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.Controls.TextChangedEventHandler hook0 = new global::System.Windows.Controls.TextChangedEventHandler(this.__LfHandle_2);
                global::System.Windows.Input.KeyboardFocusChangedEventHandler hook1 = new global::System.Windows.Input.KeyboardFocusChangedEventHandler(this.__LfHandle_3);
                global::System.Windows.Input.KeyEventHandler hook2 = new global::System.Windows.Input.KeyEventHandler(this.RetentionKey);
                global::PCL.MyTextBox previous = __LfPart_TextRetention;
                if (previous != null)
                {
                    previous.TextChanged -= hook0;
                    previous.LostKeyboardFocus -= hook1;
                    previous.PreviewKeyDown -= hook2;
                }

                __LfPart_TextRetention = value;
                global::PCL.MyTextBox current = __LfPart_TextRetention;
                if (current != null)
                {
                    current.TextChanged += hook0;
                    current.LostKeyboardFocus += hook1;
                    current.PreviewKeyDown += hook2;
                }
            }
        }

        private global::PCL.MyCard __LfPart_CardList;
        internal virtual global::PCL.MyCard CardList
        {
            get
            {
                return __LfPart_CardList;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CardList = value;
            }
        }

        private global::PCL.MyRadioButton __LfPart_FilterAll;
        internal virtual global::PCL.MyRadioButton FilterAll
        {
            get
            {
                return __LfPart_FilterAll;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyRadioButton.CheckEventHandler hook0 = new global::PCL.MyRadioButton.CheckEventHandler(this.__LfHandle_5);
                global::PCL.MyRadioButton previous = __LfPart_FilterAll;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_FilterAll = value;
                global::PCL.MyRadioButton current = __LfPart_FilterAll;
                if (current != null)
                {
                    current.Check += hook0;
                }
            }
        }

        private global::PCL.MyRadioButton __LfPart_FilterHealthy;
        internal virtual global::PCL.MyRadioButton FilterHealthy
        {
            get
            {
                return __LfPart_FilterHealthy;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyRadioButton.CheckEventHandler hook0 = new global::PCL.MyRadioButton.CheckEventHandler(this.__LfHandle_6);
                global::PCL.MyRadioButton previous = __LfPart_FilterHealthy;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_FilterHealthy = value;
                global::PCL.MyRadioButton current = __LfPart_FilterHealthy;
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

        private global::System.Windows.Controls.StackPanel __LfPart_PanEntries;
        internal virtual global::System.Windows.Controls.StackPanel PanEntries
        {
            get
            {
                return __LfPart_PanEntries;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanEntries = value;
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
                global::PCL.MyLoading.ClickEventHandler hook0 = new global::PCL.MyLoading.ClickEventHandler(this.__LfHandle_13);
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

        private global::PCL.MyCard __LfPart_CardSelect;
        internal virtual global::PCL.MyCard CardSelect
        {
            get
            {
                return __LfPart_CardSelect;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CardSelect = value;
            }
        }

        private global::System.Windows.Media.TranslateTransform __LfPart_TransSelect;
        internal virtual global::System.Windows.Media.TranslateTransform TransSelect
        {
            get
            {
                return __LfPart_TransSelect;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_TransSelect = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabSelect;
        internal virtual global::System.Windows.Controls.TextBlock LabSelect
        {
            get
            {
                return __LfPart_LabSelect;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabSelect = value;
            }
        }

        private global::PCL.MyIconTextButton __LfPart_BtnRestore;
        internal virtual global::PCL.MyIconTextButton BtnRestore
        {
            get
            {
                return __LfPart_BtnRestore;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconTextButton.ClickEventHandler hook0 = new global::PCL.MyIconTextButton.ClickEventHandler(this.__LfHandle_11);
                global::PCL.MyIconTextButton previous = __LfPart_BtnRestore;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnRestore = value;
                global::PCL.MyIconTextButton current = __LfPart_BtnRestore;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyIconTextButton __LfPart_BtnDelete;
        internal virtual global::PCL.MyIconTextButton BtnDelete
        {
            get
            {
                return __LfPart_BtnDelete;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconTextButton.ClickEventHandler hook0 = new global::PCL.MyIconTextButton.ClickEventHandler(this.__LfHandle_9);
                global::PCL.MyIconTextButton previous = __LfPart_BtnDelete;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnDelete = value;
                global::PCL.MyIconTextButton current = __LfPart_BtnDelete;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyIconTextButton __LfPart_BtnDeselect;
        internal virtual global::PCL.MyIconTextButton BtnDeselect
        {
            get
            {
                return __LfPart_BtnDeselect;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconTextButton.ClickEventHandler hook0 = new global::PCL.MyIconTextButton.ClickEventHandler(this.__LfHandle_8);
                global::PCL.MyIconTextButton previous = __LfPart_BtnDeselect;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnDeselect = value;
                global::PCL.MyIconTextButton current = __LfPart_BtnDeselect;
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
            this.RetentionEdited();
        }

        private void __LfHandle_3(global::System.Object arg0, global::System.Windows.Input.KeyboardFocusChangedEventArgs arg1)
        {
            this.RetentionLostFocus();
        }

        private void __LfHandle_4(global::System.Object arg0, global::System.EventArgs arg1)
        {
            this.SearchChanged();
        }

        private void __LfHandle_5(global::System.Object arg0, global::System.Boolean arg1)
        {
            this.FilterChanged((global::PCL.MyRadioButton)arg0, (global::System.Boolean)arg1);
        }

        private void __LfHandle_6(global::System.Object arg0, global::System.Boolean arg1)
        {
            this.FilterChanged((global::PCL.MyRadioButton)arg0, (global::System.Boolean)arg1);
        }

        private void __LfHandle_7(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.SelectAll();
        }

        private void __LfHandle_8(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.Deselect();
        }

        private void __LfHandle_9(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.DeleteSelected();
        }

        private void __LfHandle_10(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.CreatePoint();
        }

        private void __LfHandle_11(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.RestorePoint();
        }

        private void __LfHandle_12(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.OpenFolder();
        }

        private void __LfHandle_13(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.Retry();
        }

        private void __LfAttachOwnHandles()
        {
            this.Loaded += new global::System.Windows.RoutedEventHandler(this.__LfHandle_0);
            this.Unloaded += new global::System.Windows.RoutedEventHandler(this.__LfHandle_1);
            this.PreviewKeyDown += new global::System.Windows.Input.KeyEventHandler(this.SelectionKeys);
        }
    }
}
