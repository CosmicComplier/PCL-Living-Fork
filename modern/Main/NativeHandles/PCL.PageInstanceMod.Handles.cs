// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class PageInstanceMod
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
                global::System.Windows.Input.KeyEventHandler hook0 = new global::System.Windows.Input.KeyEventHandler(this.SearchBox_PreviewKeyDown);
                global::PCL.MySearchBox.TextChangedEventHandler hook1 = new global::PCL.MySearchBox.TextChangedEventHandler(this.__LfHandle_14);
                global::PCL.MySearchBox previous = __LfPart_SearchBox;
                if (previous != null)
                {
                    previous.PreviewKeyDown -= hook0;
                    previous.TextChanged -= hook1;
                }

                __LfPart_SearchBox = value;
                global::PCL.MySearchBox current = __LfPart_SearchBox;
                if (current != null)
                {
                    current.PreviewKeyDown += hook0;
                    current.TextChanged += hook1;
                }
            }
        }

        private global::PCL.MyCard __LfPart_PanManage;
        internal virtual global::PCL.MyCard PanManage
        {
            get
            {
                return __LfPart_PanManage;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanManage = value;
            }
        }

        private global::PCL.MyButton __LfPart_BtnManageOpen;
        internal virtual global::PCL.MyButton BtnManageOpen
        {
            get
            {
                return __LfPart_BtnManageOpen;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_2);
                global::PCL.MyButton previous = __LfPart_BtnManageOpen;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnManageOpen = value;
                global::PCL.MyButton current = __LfPart_BtnManageOpen;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyButton __LfPart_BtnManageInstall;
        internal virtual global::PCL.MyButton BtnManageInstall
        {
            get
            {
                return __LfPart_BtnManageInstall;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.BtnManageInstall_Click);
                global::PCL.MyButton previous = __LfPart_BtnManageInstall;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnManageInstall = value;
                global::PCL.MyButton current = __LfPart_BtnManageInstall;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyButton __LfPart_BtnManageDownload;
        internal virtual global::PCL.MyButton BtnManageDownload
        {
            get
            {
                return __LfPart_BtnManageDownload;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.BtnManageDownload_Click);
                global::PCL.MyButton previous = __LfPart_BtnManageDownload;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnManageDownload = value;
                global::PCL.MyButton current = __LfPart_BtnManageDownload;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyButton __LfPart_BtnManageSelectAll;
        internal virtual global::PCL.MyButton BtnManageSelectAll
        {
            get
            {
                return __LfPart_BtnManageSelectAll;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.BtnManageSelectAll_Click);
                global::PCL.MyButton previous = __LfPart_BtnManageSelectAll;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnManageSelectAll = value;
                global::PCL.MyButton current = __LfPart_BtnManageSelectAll;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyCard __LfPart_PanListBack;
        internal virtual global::PCL.MyCard PanListBack
        {
            get
            {
                return __LfPart_PanListBack;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanListBack = value;
            }
        }

        private global::System.Windows.Controls.StackPanel __LfPart_PanFilter;
        internal virtual global::System.Windows.Controls.StackPanel PanFilter
        {
            get
            {
                return __LfPart_PanFilter;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanFilter = value;
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
                global::PCL.MyRadioButton.CheckEventHandler hook0 = new global::PCL.MyRadioButton.CheckEventHandler(this.__LfHandle_5);
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

        private global::PCL.MyRadioButton __LfPart_BtnFilterEnabled;
        internal virtual global::PCL.MyRadioButton BtnFilterEnabled
        {
            get
            {
                return __LfPart_BtnFilterEnabled;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyRadioButton.CheckEventHandler hook0 = new global::PCL.MyRadioButton.CheckEventHandler(this.__LfHandle_8);
                global::PCL.MyRadioButton previous = __LfPart_BtnFilterEnabled;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_BtnFilterEnabled = value;
                global::PCL.MyRadioButton current = __LfPart_BtnFilterEnabled;
                if (current != null)
                {
                    current.Check += hook0;
                }
            }
        }

        private global::PCL.MyRadioButton __LfPart_BtnFilterDisabled;
        internal virtual global::PCL.MyRadioButton BtnFilterDisabled
        {
            get
            {
                return __LfPart_BtnFilterDisabled;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyRadioButton.CheckEventHandler hook0 = new global::PCL.MyRadioButton.CheckEventHandler(this.__LfHandle_7);
                global::PCL.MyRadioButton previous = __LfPart_BtnFilterDisabled;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_BtnFilterDisabled = value;
                global::PCL.MyRadioButton current = __LfPart_BtnFilterDisabled;
                if (current != null)
                {
                    current.Check += hook0;
                }
            }
        }

        private global::PCL.MyRadioButton __LfPart_BtnFilterCanUpdate;
        internal virtual global::PCL.MyRadioButton BtnFilterCanUpdate
        {
            get
            {
                return __LfPart_BtnFilterCanUpdate;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyRadioButton.CheckEventHandler hook0 = new global::PCL.MyRadioButton.CheckEventHandler(this.__LfHandle_6);
                global::PCL.MyRadioButton previous = __LfPart_BtnFilterCanUpdate;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_BtnFilterCanUpdate = value;
                global::PCL.MyRadioButton current = __LfPart_BtnFilterCanUpdate;
                if (current != null)
                {
                    current.Check += hook0;
                }
            }
        }

        private global::System.Windows.Controls.StackPanel __LfPart_PanList;
        internal virtual global::System.Windows.Controls.StackPanel PanList
        {
            get
            {
                return __LfPart_PanList;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanList = value;
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

        private global::System.Windows.Controls.TextBlock __LfPart_LabEmptyHint;
        internal virtual global::System.Windows.Controls.TextBlock LabEmptyHint
        {
            get
            {
                return __LfPart_LabEmptyHint;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabEmptyHint = value;
            }
        }

        private global::PCL.MyButton __LfPart_BtnHintDownload;
        internal virtual global::PCL.MyButton BtnHintDownload
        {
            get
            {
                return __LfPart_BtnHintDownload;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.BtnManageDownload_Click);
                global::PCL.MyButton previous = __LfPart_BtnHintDownload;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnHintDownload = value;
                global::PCL.MyButton current = __LfPart_BtnHintDownload;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyButton __LfPart_BtnHintInstall;
        internal virtual global::PCL.MyButton BtnHintInstall
        {
            get
            {
                return __LfPart_BtnHintInstall;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.BtnManageInstall_Click);
                global::PCL.MyButton previous = __LfPart_BtnHintInstall;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnHintInstall = value;
                global::PCL.MyButton current = __LfPart_BtnHintInstall;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyButton __LfPart_BtnHintOpen;
        internal virtual global::PCL.MyButton BtnHintOpen
        {
            get
            {
                return __LfPart_BtnHintOpen;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_3);
                global::PCL.MyButton previous = __LfPart_BtnHintOpen;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnHintOpen = value;
                global::PCL.MyButton current = __LfPart_BtnHintOpen;
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
                global::PCL.MyLoading.ClickEventHandler hook0 = new global::PCL.MyLoading.ClickEventHandler(this.Load_Click);
                global::PCL.MyLoading.StateChangedEventHandler hook1 = new global::PCL.MyLoading.StateChangedEventHandler(this.__LfHandle_4);
                global::PCL.MyLoading previous = __LfPart_Load;
                if (previous != null)
                {
                    previous.Click -= hook0;
                    previous.StateChanged -= hook1;
                }

                __LfPart_Load = value;
                global::PCL.MyLoading current = __LfPart_Load;
                if (current != null)
                {
                    current.Click += hook0;
                    current.StateChanged += hook1;
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

        private global::PCL.MyIconTextButton __LfPart_BtnSelectUpdate;
        internal virtual global::PCL.MyIconTextButton BtnSelectUpdate
        {
            get
            {
                return __LfPart_BtnSelectUpdate;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconTextButton.ClickEventHandler hook0 = new global::PCL.MyIconTextButton.ClickEventHandler(this.__LfHandle_11);
                global::PCL.MyIconTextButton previous = __LfPart_BtnSelectUpdate;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnSelectUpdate = value;
                global::PCL.MyIconTextButton current = __LfPart_BtnSelectUpdate;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyIconTextButton __LfPart_BtnSelectEnable;
        internal virtual global::PCL.MyIconTextButton BtnSelectEnable
        {
            get
            {
                return __LfPart_BtnSelectEnable;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconTextButton.ClickEventHandler hook0 = new global::PCL.MyIconTextButton.ClickEventHandler(this.__LfHandle_9);
                global::PCL.MyIconTextButton previous = __LfPart_BtnSelectEnable;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnSelectEnable = value;
                global::PCL.MyIconTextButton current = __LfPart_BtnSelectEnable;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyIconTextButton __LfPart_BtnSelectDisable;
        internal virtual global::PCL.MyIconTextButton BtnSelectDisable
        {
            get
            {
                return __LfPart_BtnSelectDisable;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconTextButton.ClickEventHandler hook0 = new global::PCL.MyIconTextButton.ClickEventHandler(this.__LfHandle_10);
                global::PCL.MyIconTextButton previous = __LfPart_BtnSelectDisable;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnSelectDisable = value;
                global::PCL.MyIconTextButton current = __LfPart_BtnSelectDisable;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyIconTextButton __LfPart_BtnSelectDelete;
        internal virtual global::PCL.MyIconTextButton BtnSelectDelete
        {
            get
            {
                return __LfPart_BtnSelectDelete;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconTextButton.ClickEventHandler hook0 = new global::PCL.MyIconTextButton.ClickEventHandler(this.__LfHandle_12);
                global::PCL.MyIconTextButton previous = __LfPart_BtnSelectDelete;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnSelectDelete = value;
                global::PCL.MyIconTextButton current = __LfPart_BtnSelectDelete;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyIconTextButton __LfPart_BtnSelectCancel;
        internal virtual global::PCL.MyIconTextButton BtnSelectCancel
        {
            get
            {
                return __LfPart_BtnSelectCancel;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconTextButton.ClickEventHandler hook0 = new global::PCL.MyIconTextButton.ClickEventHandler(this.__LfHandle_13);
                global::PCL.MyIconTextButton previous = __LfPart_BtnSelectCancel;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnSelectCancel = value;
                global::PCL.MyIconTextButton current = __LfPart_BtnSelectCancel;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private void __LfHandle_0(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.PageOther_Loaded();
        }

        private void __LfHandle_1(global::System.Object arg0, global::System.EventArgs arg1)
        {
            this.LoaderInit();
        }

        private void __LfHandle_2(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.BtnManageOpen_Click((global::System.Object)arg0, (global::System.EventArgs)arg1);
        }

        private void __LfHandle_3(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.BtnManageOpen_Click((global::System.Object)arg0, (global::System.EventArgs)arg1);
        }

        private void __LfHandle_4(global::System.Object arg0, global::PCL.MyLoading.MyLoadingState arg1, global::PCL.MyLoading.MyLoadingState arg2)
        {
            this.UnselectedAllWithAnimation();
        }

        private void __LfHandle_5(global::System.Object arg0, global::System.Boolean arg1)
        {
            this.ChangeFilter((global::PCL.MyRadioButton)arg0, (global::System.Boolean)arg1);
        }

        private void __LfHandle_6(global::System.Object arg0, global::System.Boolean arg1)
        {
            this.ChangeFilter((global::PCL.MyRadioButton)arg0, (global::System.Boolean)arg1);
        }

        private void __LfHandle_7(global::System.Object arg0, global::System.Boolean arg1)
        {
            this.ChangeFilter((global::PCL.MyRadioButton)arg0, (global::System.Boolean)arg1);
        }

        private void __LfHandle_8(global::System.Object arg0, global::System.Boolean arg1)
        {
            this.ChangeFilter((global::PCL.MyRadioButton)arg0, (global::System.Boolean)arg1);
        }

        private void __LfHandle_9(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.BtnSelectED_Click((global::PCL.MyIconTextButton)arg0, (global::PCL.ModBase.RouteEventArgs)arg1);
        }

        private void __LfHandle_10(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.BtnSelectED_Click((global::PCL.MyIconTextButton)arg0, (global::PCL.ModBase.RouteEventArgs)arg1);
        }

        private void __LfHandle_11(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.BtnSelectUpdate_Click();
        }

        private void __LfHandle_12(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.BtnSelectDelete_Click();
        }

        private void __LfHandle_13(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.BtnSelectCancel_Click();
        }

        private void __LfHandle_14(global::System.Object arg0, global::System.EventArgs arg1)
        {
            this.SearchRun();
        }

        private void __LfAttachOwnHandles()
        {
            this.Loaded += new global::System.Windows.RoutedEventHandler(this.__LfHandle_0);
            this.Initialized += new global::System.EventHandler(this.__LfHandle_1);
            this.PageExit += new global::PCL.MyPageRight.PageExitEventHandler(this.UnselectedAllWithAnimation);
        }
    }
}
