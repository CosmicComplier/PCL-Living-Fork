// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class PageResource
    {
        private global::PCL.MyCard __LfPart_PanAlways;
        internal virtual global::PCL.MyCard PanAlways
        {
            get
            {
                return __LfPart_PanAlways;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanAlways = value;
            }
        }

        private global::PCL.MyTextBox __LfPart_TextSearchName;
        internal virtual global::PCL.MyTextBox TextSearchName
        {
            get
            {
                return __LfPart_TextSearchName;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.Input.KeyEventHandler hook0 = new global::System.Windows.Input.KeyEventHandler(this.EnterTrigger);
                global::PCL.MyTextBox previous = __LfPart_TextSearchName;
                if (previous != null)
                {
                    previous.KeyDown -= hook0;
                }

                __LfPart_TextSearchName = value;
                global::PCL.MyTextBox current = __LfPart_TextSearchName;
                if (current != null)
                {
                    current.KeyDown += hook0;
                }
            }
        }

        private global::PCL.MyComboBox __LfPart_ComboSearchSource;
        internal virtual global::PCL.MyComboBox ComboSearchSource
        {
            get
            {
                return __LfPart_ComboSearchSource;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ComboSearchSource = value;
            }
        }

        private global::PCL.MyComboBox __LfPart_TextSearchVersion;
        internal virtual global::PCL.MyComboBox TextSearchVersion
        {
            get
            {
                return __LfPart_TextSearchVersion;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.Input.KeyEventHandler hook0 = new global::System.Windows.Input.KeyEventHandler(this.EnterTrigger);
                global::PCL.MyComboBox.TextChangedEventHandler hook1 = new global::PCL.MyComboBox.TextChangedEventHandler(this.__LfHandle_6);
                global::System.EventHandler hook2 = new global::System.EventHandler(this.__LfHandle_7);
                global::PCL.MyComboBox previous = __LfPart_TextSearchVersion;
                if (previous != null)
                {
                    previous.KeyDown -= hook0;
                    previous.TextChanged -= hook1;
                    previous.DropDownClosed -= hook2;
                }

                __LfPart_TextSearchVersion = value;
                global::PCL.MyComboBox current = __LfPart_TextSearchVersion;
                if (current != null)
                {
                    current.KeyDown += hook0;
                    current.TextChanged += hook1;
                    current.DropDownClosed += hook2;
                }
            }
        }

        private global::PCL.MyComboBox __LfPart_ComboSearchLoader;
        internal virtual global::PCL.MyComboBox ComboSearchLoader
        {
            get
            {
                return __LfPart_ComboSearchLoader;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ComboSearchLoader = value;
            }
        }

        private global::PCL.MyComboBox __LfPart_ComboSearchTag;
        internal virtual global::PCL.MyComboBox ComboSearchTag
        {
            get
            {
                return __LfPart_ComboSearchTag;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ComboSearchTag = value;
            }
        }

        private global::PCL.MyButton __LfPart_BtnSearchRun;
        internal virtual global::PCL.MyButton BtnSearchRun
        {
            get
            {
                return __LfPart_BtnSearchRun;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_4);
                global::PCL.MyButton previous = __LfPart_BtnSearchRun;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnSearchRun = value;
                global::PCL.MyButton current = __LfPart_BtnSearchRun;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyButton __LfPart_BtnSearchReset;
        internal virtual global::PCL.MyButton BtnSearchReset
        {
            get
            {
                return __LfPart_BtnSearchReset;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_5);
                global::PCL.MyButton previous = __LfPart_BtnSearchReset;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnSearchReset = value;
                global::PCL.MyButton current = __LfPart_BtnSearchReset;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyButton __LfPart_BtnSearchInstallModPack;
        internal virtual global::PCL.MyButton BtnSearchInstallModPack
        {
            get
            {
                return __LfPart_BtnSearchInstallModPack;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_8);
                global::PCL.MyButton previous = __LfPart_BtnSearchInstallModPack;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnSearchInstallModPack = value;
                global::PCL.MyButton current = __LfPart_BtnSearchInstallModPack;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::System.Windows.Controls.StackPanel __LfPart_PanContent;
        internal virtual global::System.Windows.Controls.StackPanel PanContent
        {
            get
            {
                return __LfPart_PanContent;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanContent = value;
            }
        }

        private global::PCL.MyHint __LfPart_HintError;
        internal virtual global::PCL.MyHint HintError
        {
            get
            {
                return __LfPart_HintError;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_HintError = value;
            }
        }

        private global::PCL.MyCard __LfPart_CardProjects;
        internal virtual global::PCL.MyCard CardProjects
        {
            get
            {
                return __LfPart_CardProjects;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CardProjects = value;
            }
        }

        private global::System.Windows.Controls.StackPanel __LfPart_PanProjects;
        internal virtual global::System.Windows.Controls.StackPanel PanProjects
        {
            get
            {
                return __LfPart_PanProjects;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanProjects = value;
            }
        }

        private global::PCL.MyCard __LfPart_CardPages;
        internal virtual global::PCL.MyCard CardPages
        {
            get
            {
                return __LfPart_CardPages;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CardPages = value;
            }
        }

        private global::PCL.MyIconButton __LfPart_BtnPageFirst;
        internal virtual global::PCL.MyIconButton BtnPageFirst
        {
            get
            {
                return __LfPart_BtnPageFirst;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconButton.ClickEventHandler hook0 = new global::PCL.MyIconButton.ClickEventHandler(this.__LfHandle_1);
                global::PCL.MyIconButton previous = __LfPart_BtnPageFirst;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnPageFirst = value;
                global::PCL.MyIconButton current = __LfPart_BtnPageFirst;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyIconButton __LfPart_BtnPageLeft;
        internal virtual global::PCL.MyIconButton BtnPageLeft
        {
            get
            {
                return __LfPart_BtnPageLeft;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconButton.ClickEventHandler hook0 = new global::PCL.MyIconButton.ClickEventHandler(this.__LfHandle_2);
                global::PCL.MyIconButton previous = __LfPart_BtnPageLeft;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnPageLeft = value;
                global::PCL.MyIconButton current = __LfPart_BtnPageLeft;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabPage;
        internal virtual global::System.Windows.Controls.TextBlock LabPage
        {
            get
            {
                return __LfPart_LabPage;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabPage = value;
            }
        }

        private global::PCL.MyIconButton __LfPart_BtnPageRight;
        internal virtual global::PCL.MyIconButton BtnPageRight
        {
            get
            {
                return __LfPart_BtnPageRight;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconButton.ClickEventHandler hook0 = new global::PCL.MyIconButton.ClickEventHandler(this.__LfHandle_3);
                global::PCL.MyIconButton previous = __LfPart_BtnPageRight;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnPageRight = value;
                global::PCL.MyIconButton current = __LfPart_BtnPageRight;
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
                global::PCL.MyLoading.StateChangedEventHandler hook0 = new global::PCL.MyLoading.StateChangedEventHandler(this.Load_State);
                global::PCL.MyLoading previous = __LfPart_Load;
                if (previous != null)
                {
                    previous.StateChanged -= hook0;
                }

                __LfPart_Load = value;
                global::PCL.MyLoading current = __LfPart_Load;
                if (current != null)
                {
                    current.StateChanged += hook0;
                }
            }
        }

        private void __LfHandle_0(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.PageResource_Loaded((global::System.Object)arg0, (global::System.EventArgs)arg1);
        }

        private void __LfHandle_1(global::System.Object arg0, global::System.EventArgs arg1)
        {
            this.BtnPageFirst_Click((global::System.Object)arg0, (global::System.Windows.RoutedEventArgs)arg1);
        }

        private void __LfHandle_2(global::System.Object arg0, global::System.EventArgs arg1)
        {
            this.BtnPageLeft_Click((global::System.Object)arg0, (global::System.Windows.RoutedEventArgs)arg1);
        }

        private void __LfHandle_3(global::System.Object arg0, global::System.EventArgs arg1)
        {
            this.BtnPageRight_Click((global::System.Object)arg0, (global::System.Windows.RoutedEventArgs)arg1);
        }

        private void __LfHandle_4(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.StartNewSearch();
        }

        private void __LfHandle_5(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.ResetFilter();
        }

        private void __LfHandle_6(global::System.Object arg0, global::System.Windows.Controls.TextChangedEventArgs arg1)
        {
            this.TextSearchVersion_TextChanged();
        }

        private void __LfHandle_7(global::System.Object arg0, global::System.EventArgs arg1)
        {
            this.UpdateSearchLoaderVisibility();
        }

        private void __LfHandle_8(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.BtnSearchInstallModPack_Click((global::System.Object)arg0, (global::System.EventArgs)arg1);
        }

        private void __LfAttachOwnHandles()
        {
            this.Loaded += new global::System.Windows.RoutedEventHandler(this.__LfHandle_0);
        }
    }
}
