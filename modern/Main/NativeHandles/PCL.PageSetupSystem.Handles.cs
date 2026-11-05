// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class PageSetupSystem
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

        private global::PCL.MyComboBox __LfPart_ComboDownloadSource;
        internal virtual global::PCL.MyComboBox ComboDownloadSource
        {
            get
            {
                return __LfPart_ComboDownloadSource;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ComboDownloadSource = value;
            }
        }

        private global::PCL.MyComboBox __LfPart_ComboDownloadVersion;
        internal virtual global::PCL.MyComboBox ComboDownloadVersion
        {
            get
            {
                return __LfPart_ComboDownloadVersion;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ComboDownloadVersion = value;
            }
        }

        private global::PCL.MySlider __LfPart_SliderDownloadThread;
        internal virtual global::PCL.MySlider SliderDownloadThread
        {
            get
            {
                return __LfPart_SliderDownloadThread;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MySlider.PreviewChangeEventHandler hook0 = new global::PCL.MySlider.PreviewChangeEventHandler(this.SliderDownloadThread_PreviewChange);
                global::PCL.MySlider previous = __LfPart_SliderDownloadThread;
                if (previous != null)
                {
                    previous.PreviewChange -= hook0;
                }

                __LfPart_SliderDownloadThread = value;
                global::PCL.MySlider current = __LfPart_SliderDownloadThread;
                if (current != null)
                {
                    current.PreviewChange += hook0;
                }
            }
        }

        private global::PCL.MySlider __LfPart_SliderDownloadSpeed;
        internal virtual global::PCL.MySlider SliderDownloadSpeed
        {
            get
            {
                return __LfPart_SliderDownloadSpeed;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_SliderDownloadSpeed = value;
            }
        }

        private global::PCL.MyComboBox __LfPart_ComboDownloadMod;
        internal virtual global::PCL.MyComboBox ComboDownloadMod
        {
            get
            {
                return __LfPart_ComboDownloadMod;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ComboDownloadMod = value;
            }
        }

        private global::PCL.MyComboBox __LfPart_ComboDownloadTranslateV2;
        internal virtual global::PCL.MyComboBox ComboDownloadTranslateV2
        {
            get
            {
                return __LfPart_ComboDownloadTranslateV2;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ComboDownloadTranslateV2 = value;
            }
        }

        private global::PCL.MyComboBox __LfPart_ComboModLocalNameStyle;
        internal virtual global::PCL.MyComboBox ComboModLocalNameStyle
        {
            get
            {
                return __LfPart_ComboModLocalNameStyle;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ComboModLocalNameStyle = value;
            }
        }

        private global::PCL.MyCheckBox __LfPart_CheckHelpChinese;
        internal virtual global::PCL.MyCheckBox CheckHelpChinese
        {
            get
            {
                return __LfPart_CheckHelpChinese;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CheckHelpChinese = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabLauncherVersion;
        internal virtual global::System.Windows.Controls.TextBlock LabLauncherVersion
        {
            get
            {
                return __LfPart_LabLauncherVersion;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabLauncherVersion = value;
            }
        }

        private global::PCL.MyComboBox __LfPart_ComboSystemActivity;
        internal virtual global::PCL.MyComboBox ComboSystemActivity
        {
            get
            {
                return __LfPart_ComboSystemActivity;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ComboSystemActivity = value;
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
                __LfPart_TextSystemCache = value;
            }
        }

        private global::PCL.MyButton __LfPart_BtnSystemUpdate;
        internal virtual global::PCL.MyButton BtnSystemUpdate
        {
            get
            {
                return __LfPart_BtnSystemUpdate;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_1);
                global::PCL.MyButton previous = __LfPart_BtnSystemUpdate;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnSystemUpdate = value;
                global::PCL.MyButton current = __LfPart_BtnSystemUpdate;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyButton __LfPart_BtnSystemSettingExp;
        internal virtual global::PCL.MyButton BtnSystemSettingExp
        {
            get
            {
                return __LfPart_BtnSystemSettingExp;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.BtnSystemSettingExp_Click);
                global::PCL.MyButton previous = __LfPart_BtnSystemSettingExp;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnSystemSettingExp = value;
                global::PCL.MyButton current = __LfPart_BtnSystemSettingExp;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyButton __LfPart_BtnSystemSettingImp;
        internal virtual global::PCL.MyButton BtnSystemSettingImp
        {
            get
            {
                return __LfPart_BtnSystemSettingImp;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.BtnSystemSettingImp_Click);
                global::PCL.MyButton previous = __LfPart_BtnSystemSettingImp;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnSystemSettingImp = value;
                global::PCL.MyButton current = __LfPart_BtnSystemSettingImp;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabModpackMode;
        internal virtual global::System.Windows.Controls.TextBlock LabModpackMode
        {
            get
            {
                return __LfPart_LabModpackMode;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabModpackMode = value;
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
                global::PCL.MyCheckBox.ChangeEventHandler hook0 = new global::PCL.MyCheckBox.ChangeEventHandler(this.ModpackModeChanged);
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

        private global::PCL.MyCard __LfPart_CardDebug;
        internal virtual global::PCL.MyCard CardDebug
        {
            get
            {
                return __LfPart_CardDebug;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CardDebug = value;
            }
        }

        private global::System.Windows.Controls.Grid __LfPart_PanDebugAnim;
        internal virtual global::System.Windows.Controls.Grid PanDebugAnim
        {
            get
            {
                return __LfPart_PanDebugAnim;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanDebugAnim = value;
            }
        }

        private global::PCL.MySlider __LfPart_SliderDebugAnim;
        internal virtual global::PCL.MySlider SliderDebugAnim
        {
            get
            {
                return __LfPart_SliderDebugAnim;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_SliderDebugAnim = value;
            }
        }

        private global::PCL.MyCheckBox __LfPart_CheckDebugSkipCopy;
        internal virtual global::PCL.MyCheckBox CheckDebugSkipCopy
        {
            get
            {
                return __LfPart_CheckDebugSkipCopy;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CheckDebugSkipCopy = value;
            }
        }

        private global::PCL.MyCheckBox __LfPart_CheckDebugMode;
        internal virtual global::PCL.MyCheckBox CheckDebugMode
        {
            get
            {
                return __LfPart_CheckDebugMode;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyCheckBox.ChangeEventHandler hook0 = new global::PCL.MyCheckBox.ChangeEventHandler(this.__LfHandle_0);
                global::PCL.MyCheckBox previous = __LfPart_CheckDebugMode;
                if (previous != null)
                {
                    previous.Change -= hook0;
                }

                __LfPart_CheckDebugMode = value;
                global::PCL.MyCheckBox current = __LfPart_CheckDebugMode;
                if (current != null)
                {
                    current.Change += hook0;
                }
            }
        }

        private global::PCL.MyCheckBox __LfPart_CheckDebugDelay;
        internal virtual global::PCL.MyCheckBox CheckDebugDelay
        {
            get
            {
                return __LfPart_CheckDebugDelay;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CheckDebugDelay = value;
            }
        }

        private void __LfHandle_0(global::System.Object arg0, global::System.Boolean arg1)
        {
            this.CheckDebugMode_Change();
        }

        private void __LfHandle_1(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.BtnSystemUpdate_Click((global::System.Object)arg0, (global::System.EventArgs)arg1);
        }

        private void __LfAttachOwnHandles()
        {
            this.Loaded += new global::System.Windows.RoutedEventHandler(this.PageSetupSystem_Loaded);
        }
    }
}
