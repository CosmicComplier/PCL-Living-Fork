// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class PageInstanceExport
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

        private global::PCL.MyHint __LfPart_HintOptiFine;
        internal virtual global::PCL.MyHint HintOptiFine
        {
            get
            {
                return __LfPart_HintOptiFine;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_HintOptiFine = value;
            }
        }

        private global::PCL.MyHint __LfPart_HintJava;
        internal virtual global::PCL.MyHint HintJava
        {
            get
            {
                return __LfPart_HintJava;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_HintJava = value;
            }
        }

        private global::PCL.MyTextBox __LfPart_TextExportName;
        internal virtual global::PCL.MyTextBox TextExportName
        {
            get
            {
                return __LfPart_TextExportName;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.RoutedEventHandler hook0 = new global::System.Windows.RoutedEventHandler(this.__LfHandle_6);
                global::PCL.MyTextBox previous = __LfPart_TextExportName;
                if (previous != null)
                {
                    previous.GotFocus -= hook0;
                }

                __LfPart_TextExportName = value;
                global::PCL.MyTextBox current = __LfPart_TextExportName;
                if (current != null)
                {
                    current.GotFocus += hook0;
                }
            }
        }

        private global::PCL.MyTextBox __LfPart_TextExportVersion;
        internal virtual global::PCL.MyTextBox TextExportVersion
        {
            get
            {
                return __LfPart_TextExportVersion;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_TextExportVersion = value;
            }
        }

        private global::PCL.MyCard __LfPart_CardOptions;
        internal virtual global::PCL.MyCard CardOptions
        {
            get
            {
                return __LfPart_CardOptions;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.Input.MouseButtonEventHandler hook0 = new global::System.Windows.Input.MouseButtonEventHandler(this.__LfHandle_1);
                global::PCL.MyCard previous = __LfPart_CardOptions;
                if (previous != null)
                {
                    previous.MouseLeftButtonDown -= hook0;
                }

                __LfPart_CardOptions = value;
                global::PCL.MyCard current = __LfPart_CardOptions;
                if (current != null)
                {
                    current.MouseLeftButtonDown += hook0;
                }
            }
        }

        private global::PCL.MyIconTextButton __LfPart_BtnOverrideCancel;
        internal virtual global::PCL.MyIconTextButton BtnOverrideCancel
        {
            get
            {
                return __LfPart_BtnOverrideCancel;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_BtnOverrideCancel = value;
            }
        }

        private global::System.Windows.Controls.StackPanel __LfPart_PanOptions;
        internal virtual global::System.Windows.Controls.StackPanel PanOptions
        {
            get
            {
                return __LfPart_PanOptions;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanOptions = value;
            }
        }

        private global::PCL.MyCheckBox __LfPart_CheckOptionsBasic;
        internal virtual global::PCL.MyCheckBox CheckOptionsBasic
        {
            get
            {
                return __LfPart_CheckOptionsBasic;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CheckOptionsBasic = value;
            }
        }

        private global::PCL.MyCheckBox __LfPart_CheckOptionsOptions;
        internal virtual global::PCL.MyCheckBox CheckOptionsOptions
        {
            get
            {
                return __LfPart_CheckOptionsOptions;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CheckOptionsOptions = value;
            }
        }

        private global::PCL.MyCheckBox __LfPart_CheckOptionsMod;
        internal virtual global::PCL.MyCheckBox CheckOptionsMod
        {
            get
            {
                return __LfPart_CheckOptionsMod;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CheckOptionsMod = value;
            }
        }

        private global::PCL.MyCheckBox __LfPart_CheckOptionsResourcePacks;
        internal virtual global::PCL.MyCheckBox CheckOptionsResourcePacks
        {
            get
            {
                return __LfPart_CheckOptionsResourcePacks;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CheckOptionsResourcePacks = value;
            }
        }

        private global::System.Windows.Controls.StackPanel __LfPart_PanOptionsResourcePacks;
        internal virtual global::System.Windows.Controls.StackPanel PanOptionsResourcePacks
        {
            get
            {
                return __LfPart_PanOptionsResourcePacks;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanOptionsResourcePacks = value;
            }
        }

        private global::PCL.MyCheckBox __LfPart_CheckOptionsShaderPacks;
        internal virtual global::PCL.MyCheckBox CheckOptionsShaderPacks
        {
            get
            {
                return __LfPart_CheckOptionsShaderPacks;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CheckOptionsShaderPacks = value;
            }
        }

        private global::PCL.MyCheckBox __LfPart_CheckOptionsShaderPackSettings;
        internal virtual global::PCL.MyCheckBox CheckOptionsShaderPackSettings
        {
            get
            {
                return __LfPart_CheckOptionsShaderPackSettings;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CheckOptionsShaderPackSettings = value;
            }
        }

        private global::System.Windows.Controls.StackPanel __LfPart_PanOptionsShaderPacks;
        internal virtual global::System.Windows.Controls.StackPanel PanOptionsShaderPacks
        {
            get
            {
                return __LfPart_PanOptionsShaderPacks;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanOptionsShaderPacks = value;
            }
        }

        private global::PCL.MyCheckBox __LfPart_CheckOptionsSaves;
        internal virtual global::PCL.MyCheckBox CheckOptionsSaves
        {
            get
            {
                return __LfPart_CheckOptionsSaves;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CheckOptionsSaves = value;
            }
        }

        private global::System.Windows.Controls.StackPanel __LfPart_PanOptionsSaves;
        internal virtual global::System.Windows.Controls.StackPanel PanOptionsSaves
        {
            get
            {
                return __LfPart_PanOptionsSaves;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanOptionsSaves = value;
            }
        }

        private global::PCL.MyCheckBox __LfPart_CheckOptionsJava;
        internal virtual global::PCL.MyCheckBox CheckOptionsJava
        {
            get
            {
                return __LfPart_CheckOptionsJava;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CheckOptionsJava = value;
            }
        }

        private global::PCL.MyCheckBox __LfPart_CheckOptionsPcl;
        internal virtual global::PCL.MyCheckBox CheckOptionsPcl
        {
            get
            {
                return __LfPart_CheckOptionsPcl;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyCheckBox.ChangeEventHandler hook0 = new global::PCL.MyCheckBox.ChangeEventHandler(this.CheckAdvancedModrinth_Change);
                global::PCL.MyCheckBox previous = __LfPart_CheckOptionsPcl;
                if (previous != null)
                {
                    previous.Change -= hook0;
                }

                __LfPart_CheckOptionsPcl = value;
                global::PCL.MyCheckBox current = __LfPart_CheckOptionsPcl;
                if (current != null)
                {
                    current.Change += hook0;
                }
            }
        }

        private global::PCL.MyCheckBox __LfPart_CheckOptionsPclCustom;
        internal virtual global::PCL.MyCheckBox CheckOptionsPclCustom
        {
            get
            {
                return __LfPart_CheckOptionsPclCustom;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CheckOptionsPclCustom = value;
            }
        }

        private global::PCL.MyCheckBox __LfPart_CheckOptionsModpackMode;
        internal virtual global::PCL.MyCheckBox CheckOptionsModpackMode
        {
            get
            {
                return __LfPart_CheckOptionsModpackMode;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CheckOptionsModpackMode = value;
            }
        }

        private global::PCL.MyCheckBox __LfPart_CheckAdvancedVersionIsolation;
        internal virtual global::PCL.MyCheckBox CheckAdvancedVersionIsolation
        {
            get
            {
                return __LfPart_CheckAdvancedVersionIsolation;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CheckAdvancedVersionIsolation = value;
            }
        }

        private global::PCL.MyCheckBox __LfPart_CheckAdvancedInclude;
        internal virtual global::PCL.MyCheckBox CheckAdvancedInclude
        {
            get
            {
                return __LfPart_CheckAdvancedInclude;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyCheckBox.ChangeEventHandler hook0 = new global::PCL.MyCheckBox.ChangeEventHandler(this.CheckAdvancedInclude_Change);
                global::PCL.MyCheckBox previous = __LfPart_CheckAdvancedInclude;
                if (previous != null)
                {
                    previous.Change -= hook0;
                }

                __LfPart_CheckAdvancedInclude = value;
                global::PCL.MyCheckBox current = __LfPart_CheckAdvancedInclude;
                if (current != null)
                {
                    current.Change += hook0;
                }
            }
        }

        private global::PCL.MyCheckBox __LfPart_CheckAdvancedModrinth;
        internal virtual global::PCL.MyCheckBox CheckAdvancedModrinth
        {
            get
            {
                return __LfPart_CheckAdvancedModrinth;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyCheckBox.ChangeEventHandler hook0 = new global::PCL.MyCheckBox.ChangeEventHandler(this.CheckAdvancedModrinth_Change);
                global::PCL.MyCheckBox previous = __LfPart_CheckAdvancedModrinth;
                if (previous != null)
                {
                    previous.Change -= hook0;
                }

                __LfPart_CheckAdvancedModrinth = value;
                global::PCL.MyCheckBox current = __LfPart_CheckAdvancedModrinth;
                if (current != null)
                {
                    current.Change += hook0;
                }
            }
        }

        private global::PCL.MyHint __LfPart_HintModrinthFormat;
        internal virtual global::PCL.MyHint HintModrinthFormat
        {
            get
            {
                return __LfPart_HintModrinthFormat;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_HintModrinthFormat = value;
            }
        }

        private global::PCL.MyButton __LfPart_BtnAdvancedImport;
        internal virtual global::PCL.MyButton BtnAdvancedImport
        {
            get
            {
                return __LfPart_BtnAdvancedImport;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_3);
                global::PCL.MyButton previous = __LfPart_BtnAdvancedImport;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnAdvancedImport = value;
                global::PCL.MyButton current = __LfPart_BtnAdvancedImport;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyButton __LfPart_BtnAdvancedExport;
        internal virtual global::PCL.MyButton BtnAdvancedExport
        {
            get
            {
                return __LfPart_BtnAdvancedExport;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_2);
                global::PCL.MyButton previous = __LfPart_BtnAdvancedExport;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnAdvancedExport = value;
                global::PCL.MyButton current = __LfPart_BtnAdvancedExport;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyButton __LfPart_BtnAdvancedHelp;
        internal virtual global::PCL.MyButton BtnAdvancedHelp
        {
            get
            {
                return __LfPart_BtnAdvancedHelp;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_BtnAdvancedHelp = value;
            }
        }

        private global::PCL.MyExtraTextButton __LfPart_BtnExport;
        internal virtual global::PCL.MyExtraTextButton BtnExport
        {
            get
            {
                return __LfPart_BtnExport;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyExtraTextButton.ClickEventHandler hook0 = new global::PCL.MyExtraTextButton.ClickEventHandler(this.__LfHandle_4);
                global::PCL.MyExtraTextButton previous = __LfPart_BtnExport;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnExport = value;
                global::PCL.MyExtraTextButton current = __LfPart_BtnExport;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private void __LfHandle_0(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.PageInstanceExport_Loaded();
        }

        private void __LfHandle_1(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.CardOptions_MouseLeftButtonDown();
        }

        private void __LfHandle_2(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.ExportConfig();
        }

        private void __LfHandle_3(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.ImportConfig();
        }

        private void __LfHandle_4(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.StartExport();
        }

        private void __LfHandle_5(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.RefreshJavaInfo();
        }

        private void __LfHandle_6(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.TextExportName_GotFocus();
        }

        private void __LfAttachOwnHandles()
        {
            this.Initialized += new global::System.EventHandler(this.PageInstanceExport_Initialized);
            this.Loaded += new global::System.Windows.RoutedEventHandler(this.__LfHandle_0);
            this.Loaded += new global::System.Windows.RoutedEventHandler(this.__LfHandle_5);
        }
    }
}
