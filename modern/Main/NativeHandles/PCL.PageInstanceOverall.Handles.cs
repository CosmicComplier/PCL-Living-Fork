// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class PageInstanceOverall
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

        private global::System.Windows.Controls.Grid __LfPart_PanDisplayItem;
        internal virtual global::System.Windows.Controls.Grid PanDisplayItem
        {
            get
            {
                return __LfPart_PanDisplayItem;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanDisplayItem = value;
            }
        }

        private global::System.Windows.Controls.StackPanel __LfPart_PanInstalledComponents;
        internal virtual global::System.Windows.Controls.StackPanel PanInstalledComponents
        {
            get
            {
                return __LfPart_PanInstalledComponents;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanInstalledComponents = value;
            }
        }

        private global::PCL.MyCard __LfPart_PanDisplay;
        internal virtual global::PCL.MyCard PanDisplay
        {
            get
            {
                return __LfPart_PanDisplay;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanDisplay = value;
            }
        }

        private global::System.Windows.Controls.Grid __LfPart_PanDisplayIcon;
        internal virtual global::System.Windows.Controls.Grid PanDisplayIcon
        {
            get
            {
                return __LfPart_PanDisplayIcon;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanDisplayIcon = value;
            }
        }

        private global::PCL.MyComboBox __LfPart_ComboDisplayLogo;
        internal virtual global::PCL.MyComboBox ComboDisplayLogo
        {
            get
            {
                return __LfPart_ComboDisplayLogo;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.Controls.SelectionChangedEventHandler hook0 = new global::System.Windows.Controls.SelectionChangedEventHandler(this.__LfHandle_4);
                global::PCL.MyComboBox previous = __LfPart_ComboDisplayLogo;
                if (previous != null)
                {
                    previous.SelectionChanged -= hook0;
                }

                __LfPart_ComboDisplayLogo = value;
                global::PCL.MyComboBox current = __LfPart_ComboDisplayLogo;
                if (current != null)
                {
                    current.SelectionChanged += hook0;
                }
            }
        }

        private global::PCL.MyComboBoxItem __LfPart_ItemDisplayLogoCustom;
        internal virtual global::PCL.MyComboBoxItem ItemDisplayLogoCustom
        {
            get
            {
                return __LfPart_ItemDisplayLogoCustom;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.Input.MouseButtonEventHandler hook0 = new global::System.Windows.Input.MouseButtonEventHandler(this.__LfHandle_3);
                global::PCL.MyComboBoxItem previous = __LfPart_ItemDisplayLogoCustom;
                if (previous != null)
                {
                    previous.PreviewMouseLeftButtonUp -= hook0;
                }

                __LfPart_ItemDisplayLogoCustom = value;
                global::PCL.MyComboBoxItem current = __LfPart_ItemDisplayLogoCustom;
                if (current != null)
                {
                    current.PreviewMouseLeftButtonUp += hook0;
                }
            }
        }

        private global::PCL.MyComboBox __LfPart_ComboDisplayType;
        internal virtual global::PCL.MyComboBox ComboDisplayType
        {
            get
            {
                return __LfPart_ComboDisplayType;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.Controls.SelectionChangedEventHandler hook0 = new global::System.Windows.Controls.SelectionChangedEventHandler(this.ComboDisplayType_SelectionChanged);
                global::PCL.MyComboBox previous = __LfPart_ComboDisplayType;
                if (previous != null)
                {
                    previous.SelectionChanged -= hook0;
                }

                __LfPart_ComboDisplayType = value;
                global::PCL.MyComboBox current = __LfPart_ComboDisplayType;
                if (current != null)
                {
                    current.SelectionChanged += hook0;
                }
            }
        }

        private global::PCL.MyButton __LfPart_BtnDisplayRename;
        internal virtual global::PCL.MyButton BtnDisplayRename
        {
            get
            {
                return __LfPart_BtnDisplayRename;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_2);
                global::PCL.MyButton previous = __LfPart_BtnDisplayRename;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnDisplayRename = value;
                global::PCL.MyButton current = __LfPart_BtnDisplayRename;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyButton __LfPart_BtnDisplayDesc;
        internal virtual global::PCL.MyButton BtnDisplayDesc
        {
            get
            {
                return __LfPart_BtnDisplayDesc;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_1);
                global::PCL.MyButton previous = __LfPart_BtnDisplayDesc;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnDisplayDesc = value;
                global::PCL.MyButton current = __LfPart_BtnDisplayDesc;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyButton __LfPart_BtnDisplayStar;
        internal virtual global::PCL.MyButton BtnDisplayStar
        {
            get
            {
                return __LfPart_BtnDisplayStar;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_5);
                global::PCL.MyButton previous = __LfPart_BtnDisplayStar;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnDisplayStar = value;
                global::PCL.MyButton current = __LfPart_BtnDisplayStar;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyCard __LfPart_PanFolder;
        internal virtual global::PCL.MyCard PanFolder
        {
            get
            {
                return __LfPart_PanFolder;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanFolder = value;
            }
        }

        private global::PCL.MyButton __LfPart_BtnFolderVersion;
        internal virtual global::PCL.MyButton BtnFolderVersion
        {
            get
            {
                return __LfPart_BtnFolderVersion;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_6);
                global::PCL.MyButton previous = __LfPart_BtnFolderVersion;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnFolderVersion = value;
                global::PCL.MyButton current = __LfPart_BtnFolderVersion;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyButton __LfPart_BtnFolderSaves;
        internal virtual global::PCL.MyButton BtnFolderSaves
        {
            get
            {
                return __LfPart_BtnFolderSaves;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_7);
                global::PCL.MyButton previous = __LfPart_BtnFolderSaves;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnFolderSaves = value;
                global::PCL.MyButton current = __LfPart_BtnFolderSaves;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyButton __LfPart_BtnFolderMods;
        internal virtual global::PCL.MyButton BtnFolderMods
        {
            get
            {
                return __LfPart_BtnFolderMods;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_8);
                global::PCL.MyButton previous = __LfPart_BtnFolderMods;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnFolderMods = value;
                global::PCL.MyButton current = __LfPart_BtnFolderMods;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyButton __LfPart_BtnFolderScreen;
        internal virtual global::PCL.MyButton BtnFolderScreen
        {
            get
            {
                return __LfPart_BtnFolderScreen;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.BtnFolderScreen_Click);
                global::PCL.MyButton previous = __LfPart_BtnFolderScreen;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnFolderScreen = value;
                global::PCL.MyButton current = __LfPart_BtnFolderScreen;
                if (current != null)
                {
                    current.Click += hook0;
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

        private global::PCL.MyButton __LfPart_BtnManageScript;
        internal virtual global::PCL.MyButton BtnManageScript
        {
            get
            {
                return __LfPart_BtnManageScript;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_9);
                global::PCL.MyButton previous = __LfPart_BtnManageScript;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnManageScript = value;
                global::PCL.MyButton current = __LfPart_BtnManageScript;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyButton __LfPart_BtnManageCheck;
        internal virtual global::PCL.MyButton BtnManageCheck
        {
            get
            {
                return __LfPart_BtnManageCheck;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_10);
                global::PCL.MyButton previous = __LfPart_BtnManageCheck;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnManageCheck = value;
                global::PCL.MyButton current = __LfPart_BtnManageCheck;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyButton __LfPart_BtnManageDelete;
        internal virtual global::PCL.MyButton BtnManageDelete
        {
            get
            {
                return __LfPart_BtnManageDelete;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_11);
                global::PCL.MyButton previous = __LfPart_BtnManageDelete;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnManageDelete = value;
                global::PCL.MyButton current = __LfPart_BtnManageDelete;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyCard __LfPart_PanComponentsLoad;
        internal virtual global::PCL.MyCard PanComponentsLoad
        {
            get
            {
                return __LfPart_PanComponentsLoad;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanComponentsLoad = value;
            }
        }

        private global::PCL.MyLoading __LfPart_LoadComponents;
        internal virtual global::PCL.MyLoading LoadComponents
        {
            get
            {
                return __LfPart_LoadComponents;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LoadComponents = value;
            }
        }

        private void __LfHandle_0(global::System.Object arg0, global::System.EventArgs arg1)
        {
            this.ComponentsLoaderInit();
        }

        private void __LfHandle_1(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.BtnDisplayDesc_Click((global::System.Object)arg0, (global::System.EventArgs)arg1);
        }

        private void __LfHandle_2(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.BtnDisplayRename_Click((global::System.Object)arg0, (global::System.EventArgs)arg1);
        }

        private void __LfHandle_3(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.ComboDisplayLogo_SelectionChanged();
        }

        private void __LfHandle_4(global::System.Object arg0, global::System.Windows.Controls.SelectionChangedEventArgs arg1)
        {
            this.ComboDisplayLogo_SelectionChanged();
        }

        private void __LfHandle_5(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.BtnDisplayStar_Click((global::System.Object)arg0, (global::System.EventArgs)arg1);
        }

        private void __LfHandle_6(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.BtnFolderVersion_Click();
        }

        private void __LfHandle_7(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.BtnFolderSaves_Click();
        }

        private void __LfHandle_8(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.BtnFolderMods_Click();
        }

        private void __LfHandle_9(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.BtnManageScript_Click();
        }

        private void __LfHandle_10(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.BtnManageCheck_Click((global::System.Object)arg0, (global::System.EventArgs)arg1);
        }

        private void __LfHandle_11(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.BtnManageDelete_Click((global::System.Object)arg0, (global::System.EventArgs)arg1);
        }

        private void __LfAttachOwnHandles()
        {
            this.Initialized += new global::System.EventHandler(this.__LfHandle_0);
            this.Loaded += new global::System.Windows.RoutedEventHandler(this.PageSetupLaunch_Loaded);
        }
    }
}
