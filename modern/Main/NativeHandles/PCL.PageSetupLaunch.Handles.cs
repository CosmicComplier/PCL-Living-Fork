// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class PageSetupLaunch
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

        private global::PCL.MyCheckBox __LfPart_CheckPersonalOptionsSync;
        internal virtual global::PCL.MyCheckBox CheckPersonalOptionsSync
        {
            get
            {
                return __LfPart_CheckPersonalOptionsSync;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyCheckBox.ChangeEventHandler hook0 = new global::PCL.MyCheckBox.ChangeEventHandler(this.PersonalSyncChanged);
                global::PCL.MyCheckBox previous = __LfPart_CheckPersonalOptionsSync;
                if (previous != null)
                {
                    previous.Change -= hook0;
                }

                __LfPart_CheckPersonalOptionsSync = value;
                global::PCL.MyCheckBox current = __LfPart_CheckPersonalOptionsSync;
                if (current != null)
                {
                    current.Change += hook0;
                }
            }
        }

        private global::PCL.MyButton __LfPart_BtnImportPersonalOptions;
        internal virtual global::PCL.MyButton BtnImportPersonalOptions
        {
            get
            {
                return __LfPart_BtnImportPersonalOptions;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_0);
                global::PCL.MyButton previous = __LfPart_BtnImportPersonalOptions;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnImportPersonalOptions = value;
                global::PCL.MyButton current = __LfPart_BtnImportPersonalOptions;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyCard __LfPart_CardArgument;
        internal virtual global::PCL.MyCard CardArgument
        {
            get
            {
                return __LfPart_CardArgument;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CardArgument = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabArgumentIndie;
        internal virtual global::System.Windows.Controls.TextBlock LabArgumentIndie
        {
            get
            {
                return __LfPart_LabArgumentIndie;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabArgumentIndie = value;
            }
        }

        private global::PCL.MyComboBox __LfPart_ComboArgumentIndieV2;
        internal virtual global::PCL.MyComboBox ComboArgumentIndieV2
        {
            get
            {
                return __LfPart_ComboArgumentIndieV2;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.Controls.SelectionChangedEventHandler hook0 = new global::System.Windows.Controls.SelectionChangedEventHandler(this.ComboArgumentIndie_SelectionChanged);
                global::PCL.MyComboBox previous = __LfPart_ComboArgumentIndieV2;
                if (previous != null)
                {
                    previous.SelectionChanged -= hook0;
                }

                __LfPart_ComboArgumentIndieV2 = value;
                global::PCL.MyComboBox current = __LfPart_ComboArgumentIndieV2;
                if (current != null)
                {
                    current.SelectionChanged += hook0;
                }
            }
        }

        private global::PCL.MyComboBox __LfPart_ComboArgumentVisibie;
        internal virtual global::PCL.MyComboBox ComboArgumentVisibie
        {
            get
            {
                return __LfPart_ComboArgumentVisibie;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.Controls.SelectionChangedEventHandler hook0 = new global::System.Windows.Controls.SelectionChangedEventHandler(this.ComboArgumentVisibie_SizeChanged);
                global::PCL.MyComboBox previous = __LfPart_ComboArgumentVisibie;
                if (previous != null)
                {
                    previous.SelectionChanged -= hook0;
                }

                __LfPart_ComboArgumentVisibie = value;
                global::PCL.MyComboBox current = __LfPart_ComboArgumentVisibie;
                if (current != null)
                {
                    current.SelectionChanged += hook0;
                }
            }
        }

        private global::System.Windows.Controls.Grid __LfPart_PanArgumentWindow;
        internal virtual global::System.Windows.Controls.Grid PanArgumentWindow
        {
            get
            {
                return __LfPart_PanArgumentWindow;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanArgumentWindow = value;
            }
        }

        private global::PCL.MyComboBox __LfPart_ComboArgumentWindowType;
        internal virtual global::PCL.MyComboBox ComboArgumentWindowType
        {
            get
            {
                return __LfPart_ComboArgumentWindowType;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.Controls.SelectionChangedEventHandler hook0 = new global::System.Windows.Controls.SelectionChangedEventHandler(this.__LfHandle_12);
                global::PCL.MyComboBox previous = __LfPart_ComboArgumentWindowType;
                if (previous != null)
                {
                    previous.SelectionChanged -= hook0;
                }

                __LfPart_ComboArgumentWindowType = value;
                global::PCL.MyComboBox current = __LfPart_ComboArgumentWindowType;
                if (current != null)
                {
                    current.SelectionChanged += hook0;
                }
            }
        }

        private global::PCL.MyTextBox __LfPart_TextArgumentWindowWidth;
        internal virtual global::PCL.MyTextBox TextArgumentWindowWidth
        {
            get
            {
                return __LfPart_TextArgumentWindowWidth;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_TextArgumentWindowWidth = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabArgumentWindowMiddle;
        internal virtual global::System.Windows.Controls.TextBlock LabArgumentWindowMiddle
        {
            get
            {
                return __LfPart_LabArgumentWindowMiddle;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabArgumentWindowMiddle = value;
            }
        }

        private global::PCL.MyTextBox __LfPart_TextArgumentWindowHeight;
        internal virtual global::PCL.MyTextBox TextArgumentWindowHeight
        {
            get
            {
                return __LfPart_TextArgumentWindowHeight;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_TextArgumentWindowHeight = value;
            }
        }

        private global::PCL.MyRadioBox __LfPart_RadioRamType0;
        internal virtual global::PCL.MyRadioBox RadioRamType0
        {
            get
            {
                return __LfPart_RadioRamType0;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.IMyRadio.CheckEventHandler hook0 = new global::PCL.IMyRadio.CheckEventHandler(this.__LfHandle_6);
                global::PCL.MyRadioBox previous = __LfPart_RadioRamType0;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_RadioRamType0 = value;
                global::PCL.MyRadioBox current = __LfPart_RadioRamType0;
                if (current != null)
                {
                    current.Check += hook0;
                }
            }
        }

        private global::PCL.MyRadioBox __LfPart_RadioRamType1;
        internal virtual global::PCL.MyRadioBox RadioRamType1
        {
            get
            {
                return __LfPart_RadioRamType1;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.IMyRadio.CheckEventHandler hook0 = new global::PCL.IMyRadio.CheckEventHandler(this.__LfHandle_7);
                global::PCL.MyRadioBox previous = __LfPart_RadioRamType1;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_RadioRamType1 = value;
                global::PCL.MyRadioBox current = __LfPart_RadioRamType1;
                if (current != null)
                {
                    current.Check += hook0;
                }
            }
        }

        private global::PCL.MySlider __LfPart_SliderRamCustom;
        internal virtual global::PCL.MySlider SliderRamCustom
        {
            get
            {
                return __LfPart_SliderRamCustom;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MySlider.ChangeEventHandler hook0 = new global::PCL.MySlider.ChangeEventHandler(this.__LfHandle_5);
                global::PCL.MySlider previous = __LfPart_SliderRamCustom;
                if (previous != null)
                {
                    previous.Change -= hook0;
                }

                __LfPart_SliderRamCustom = value;
                global::PCL.MySlider current = __LfPart_SliderRamCustom;
                if (current != null)
                {
                    current.Change += hook0;
                }
            }
        }

        private global::PCL.MyCheckBox __LfPart_CheckArgumentRam;
        internal virtual global::PCL.MyCheckBox CheckArgumentRam
        {
            get
            {
                return __LfPart_CheckArgumentRam;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyCheckBox.ChangeEventHandler hook0 = new global::PCL.MyCheckBox.ChangeEventHandler(this.__LfHandle_13);
                global::PCL.MyCheckBox previous = __LfPart_CheckArgumentRam;
                if (previous != null)
                {
                    previous.Change -= hook0;
                }

                __LfPart_CheckArgumentRam = value;
                global::PCL.MyCheckBox current = __LfPart_CheckArgumentRam;
                if (current != null)
                {
                    current.Change += hook0;
                }
            }
        }

        private global::System.Windows.Controls.Grid __LfPart_PanRamDisplay;
        internal virtual global::System.Windows.Controls.Grid PanRamDisplay
        {
            get
            {
                return __LfPart_PanRamDisplay;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanRamDisplay = value;
            }
        }

        private global::System.Windows.Controls.ColumnDefinition __LfPart_ColumnRamUsed;
        internal virtual global::System.Windows.Controls.ColumnDefinition ColumnRamUsed
        {
            get
            {
                return __LfPart_ColumnRamUsed;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ColumnRamUsed = value;
            }
        }

        private global::System.Windows.Controls.ColumnDefinition __LfPart_ColumnRamGame;
        internal virtual global::System.Windows.Controls.ColumnDefinition ColumnRamGame
        {
            get
            {
                return __LfPart_ColumnRamGame;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ColumnRamGame = value;
            }
        }

        private global::System.Windows.Controls.ColumnDefinition __LfPart_ColumnRamEmpty;
        internal virtual global::System.Windows.Controls.ColumnDefinition ColumnRamEmpty
        {
            get
            {
                return __LfPart_ColumnRamEmpty;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ColumnRamEmpty = value;
            }
        }

        private global::System.Windows.Shapes.Rectangle __LfPart_RectRamUsed;
        internal virtual global::System.Windows.Shapes.Rectangle RectRamUsed
        {
            get
            {
                return __LfPart_RectRamUsed;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_RectRamUsed = value;
            }
        }

        private global::System.Windows.Shapes.Rectangle __LfPart_RectRamGame;
        internal virtual global::System.Windows.Shapes.Rectangle RectRamGame
        {
            get
            {
                return __LfPart_RectRamGame;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.SizeChangedEventHandler hook0 = new global::System.Windows.SizeChangedEventHandler(this.__LfHandle_8);
                global::System.Windows.Shapes.Rectangle previous = __LfPart_RectRamGame;
                if (previous != null)
                {
                    previous.SizeChanged -= hook0;
                }

                __LfPart_RectRamGame = value;
                global::System.Windows.Shapes.Rectangle current = __LfPart_RectRamGame;
                if (current != null)
                {
                    current.SizeChanged += hook0;
                }
            }
        }

        private global::System.Windows.Shapes.Rectangle __LfPart_RectRamEmpty;
        internal virtual global::System.Windows.Shapes.Rectangle RectRamEmpty
        {
            get
            {
                return __LfPart_RectRamEmpty;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.SizeChangedEventHandler hook0 = new global::System.Windows.SizeChangedEventHandler(this.__LfHandle_9);
                global::System.Windows.Shapes.Rectangle previous = __LfPart_RectRamEmpty;
                if (previous != null)
                {
                    previous.SizeChanged -= hook0;
                }

                __LfPart_RectRamEmpty = value;
                global::System.Windows.Shapes.Rectangle current = __LfPart_RectRamEmpty;
                if (current != null)
                {
                    current.SizeChanged += hook0;
                }
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabRamUsedTitle;
        internal virtual global::System.Windows.Controls.TextBlock LabRamUsedTitle
        {
            get
            {
                return __LfPart_LabRamUsedTitle;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabRamUsedTitle = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabRamGameTitle;
        internal virtual global::System.Windows.Controls.TextBlock LabRamGameTitle
        {
            get
            {
                return __LfPart_LabRamGameTitle;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabRamGameTitle = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabRamUsed;
        internal virtual global::System.Windows.Controls.TextBlock LabRamUsed
        {
            get
            {
                return __LfPart_LabRamUsed;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabRamUsed = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabRamTotal;
        internal virtual global::System.Windows.Controls.TextBlock LabRamTotal
        {
            get
            {
                return __LfPart_LabRamTotal;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabRamTotal = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabRamGame;
        internal virtual global::System.Windows.Controls.TextBlock LabRamGame
        {
            get
            {
                return __LfPart_LabRamGame;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.SizeChangedEventHandler hook0 = new global::System.Windows.SizeChangedEventHandler(this.__LfHandle_10);
                global::System.Windows.Controls.TextBlock previous = __LfPart_LabRamGame;
                if (previous != null)
                {
                    previous.SizeChanged -= hook0;
                }

                __LfPart_LabRamGame = value;
                global::System.Windows.Controls.TextBlock current = __LfPart_LabRamGame;
                if (current != null)
                {
                    current.SizeChanged += hook0;
                }
            }
        }

        private global::PCL.MyCard __LfPart_CardSkin;
        internal virtual global::PCL.MyCard CardSkin
        {
            get
            {
                return __LfPart_CardSkin;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CardSkin = value;
            }
        }

        private global::PCL.MyRadioBox __LfPart_RadioSkinType0;
        internal virtual global::PCL.MyRadioBox RadioSkinType0
        {
            get
            {
                return __LfPart_RadioSkinType0;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_RadioSkinType0 = value;
            }
        }

        private global::PCL.MyRadioBox __LfPart_RadioSkinType1;
        internal virtual global::PCL.MyRadioBox RadioSkinType1
        {
            get
            {
                return __LfPart_RadioSkinType1;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_RadioSkinType1 = value;
            }
        }

        private global::PCL.MyRadioBox __LfPart_RadioSkinType2;
        internal virtual global::PCL.MyRadioBox RadioSkinType2
        {
            get
            {
                return __LfPart_RadioSkinType2;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_RadioSkinType2 = value;
            }
        }

        private global::PCL.MyRadioBox __LfPart_RadioSkinType3;
        internal virtual global::PCL.MyRadioBox RadioSkinType3
        {
            get
            {
                return __LfPart_RadioSkinType3;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_RadioSkinType3 = value;
            }
        }

        private global::PCL.MyRadioBox __LfPart_RadioSkinType4;
        internal virtual global::PCL.MyRadioBox RadioSkinType4
        {
            get
            {
                return __LfPart_RadioSkinType4;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyRadioBox.PreviewCheckEventHandler hook0 = new global::PCL.MyRadioBox.PreviewCheckEventHandler(this.RadioSkinType3_Check);
                global::PCL.MyRadioBox previous = __LfPart_RadioSkinType4;
                if (previous != null)
                {
                    previous.PreviewCheck -= hook0;
                }

                __LfPart_RadioSkinType4 = value;
                global::PCL.MyRadioBox current = __LfPart_RadioSkinType4;
                if (current != null)
                {
                    current.PreviewCheck += hook0;
                }
            }
        }

        private global::System.Windows.Controls.Grid __LfPart_PanSkinID;
        internal virtual global::System.Windows.Controls.Grid PanSkinID
        {
            get
            {
                return __LfPart_PanSkinID;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanSkinID = value;
            }
        }

        private global::PCL.MyButton __LfPart_BtnSkinSave;
        internal virtual global::PCL.MyButton BtnSkinSave
        {
            get
            {
                return __LfPart_BtnSkinSave;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_3);
                global::PCL.MyButton previous = __LfPart_BtnSkinSave;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnSkinSave = value;
                global::PCL.MyButton current = __LfPart_BtnSkinSave;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyButton __LfPart_BtnSkinCache;
        internal virtual global::PCL.MyButton BtnSkinCache
        {
            get
            {
                return __LfPart_BtnSkinCache;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_4);
                global::PCL.MyButton previous = __LfPart_BtnSkinCache;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnSkinCache = value;
                global::PCL.MyButton current = __LfPart_BtnSkinCache;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::System.Windows.Controls.Grid __LfPart_PanSkinChange;
        internal virtual global::System.Windows.Controls.Grid PanSkinChange
        {
            get
            {
                return __LfPart_PanSkinChange;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanSkinChange = value;
            }
        }

        private global::PCL.MyButton __LfPart_BtnSkinChange;
        internal virtual global::PCL.MyButton BtnSkinChange
        {
            get
            {
                return __LfPart_BtnSkinChange;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_1);
                global::PCL.MyButton previous = __LfPart_BtnSkinChange;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnSkinChange = value;
                global::PCL.MyButton current = __LfPart_BtnSkinChange;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyButton __LfPart_BtnSkinDelete;
        internal virtual global::PCL.MyButton BtnSkinDelete
        {
            get
            {
                return __LfPart_BtnSkinDelete;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_2);
                global::PCL.MyButton previous = __LfPart_BtnSkinDelete;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnSkinDelete = value;
                global::PCL.MyButton current = __LfPart_BtnSkinDelete;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyIconButton __LfPart_BtnAdvanceJvmReset;
        internal virtual global::PCL.MyIconButton BtnAdvanceJvmReset
        {
            get
            {
                return __LfPart_BtnAdvanceJvmReset;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconButton.ClickEventHandler hook0 = new global::PCL.MyIconButton.ClickEventHandler(this.BtnAdvanceJvmReset_Click);
                global::PCL.MyIconButton previous = __LfPart_BtnAdvanceJvmReset;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnAdvanceJvmReset = value;
                global::PCL.MyIconButton current = __LfPart_BtnAdvanceJvmReset;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyTextBox __LfPart_TextAdvanceJvm;
        internal virtual global::PCL.MyTextBox TextAdvanceJvm
        {
            get
            {
                return __LfPart_TextAdvanceJvm;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.RoutedEventHandler hook0 = new global::System.Windows.RoutedEventHandler(this.__LfHandle_14);
                global::System.Windows.Controls.TextChangedEventHandler hook1 = new global::System.Windows.Controls.TextChangedEventHandler(this.__LfHandle_15);
                global::PCL.MyTextBox previous = __LfPart_TextAdvanceJvm;
                if (previous != null)
                {
                    previous.ValidatedTextChanged -= hook0;
                    previous.TextChanged -= hook1;
                }

                __LfPart_TextAdvanceJvm = value;
                global::PCL.MyTextBox current = __LfPart_TextAdvanceJvm;
                if (current != null)
                {
                    current.ValidatedTextChanged += hook0;
                    current.TextChanged += hook1;
                }
            }
        }

        private global::PCL.MyTextBox __LfPart_TextAdvanceGame;
        internal virtual global::PCL.MyTextBox TextAdvanceGame
        {
            get
            {
                return __LfPart_TextAdvanceGame;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.Controls.TextChangedEventHandler hook0 = new global::System.Windows.Controls.TextChangedEventHandler(this.__LfHandle_16);
                global::PCL.MyTextBox previous = __LfPart_TextAdvanceGame;
                if (previous != null)
                {
                    previous.TextChanged -= hook0;
                }

                __LfPart_TextAdvanceGame = value;
                global::PCL.MyTextBox current = __LfPart_TextAdvanceGame;
                if (current != null)
                {
                    current.TextChanged += hook0;
                }
            }
        }

        private global::PCL.MyTextBox __LfPart_TextAdvanceRun;
        internal virtual global::PCL.MyTextBox TextAdvanceRun
        {
            get
            {
                return __LfPart_TextAdvanceRun;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.Controls.TextChangedEventHandler hook0 = new global::System.Windows.Controls.TextChangedEventHandler(this.TextAdvanceRun_TextChanged);
                global::PCL.MyTextBox previous = __LfPart_TextAdvanceRun;
                if (previous != null)
                {
                    previous.TextChanged -= hook0;
                }

                __LfPart_TextAdvanceRun = value;
                global::PCL.MyTextBox current = __LfPart_TextAdvanceRun;
                if (current != null)
                {
                    current.TextChanged += hook0;
                }
            }
        }

        private global::PCL.MyCheckBox __LfPart_CheckAdvanceRunWait;
        internal virtual global::PCL.MyCheckBox CheckAdvanceRunWait
        {
            get
            {
                return __LfPart_CheckAdvanceRunWait;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CheckAdvanceRunWait = value;
            }
        }

        private global::System.Windows.Controls.Grid __LfPart_PanAdvanceJava;
        internal virtual global::System.Windows.Controls.Grid PanAdvanceJava
        {
            get
            {
                return __LfPart_PanAdvanceJava;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanAdvanceJava = value;
            }
        }

        private global::PCL.MyComboBox __LfPart_ComboAdvanceJava;
        internal virtual global::PCL.MyComboBox ComboAdvanceJava
        {
            get
            {
                return __LfPart_ComboAdvanceJava;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ComboAdvanceJava = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabAdvanceJava;
        internal virtual global::System.Windows.Controls.TextBlock LabAdvanceJava
        {
            get
            {
                return __LfPart_LabAdvanceJava;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabAdvanceJava = value;
            }
        }

        private global::PCL.MyIconButton __LfPart_BtnAdvanceJavaSearch;
        internal virtual global::PCL.MyIconButton BtnAdvanceJavaSearch
        {
            get
            {
                return __LfPart_BtnAdvanceJavaSearch;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconButton.ClickEventHandler hook0 = new global::PCL.MyIconButton.ClickEventHandler(this.__LfHandle_11);
                global::PCL.MyIconButton previous = __LfPart_BtnAdvanceJavaSearch;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnAdvanceJavaSearch = value;
                global::PCL.MyIconButton current = __LfPart_BtnAdvanceJavaSearch;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyExtraTextButton __LfPart_BtnSwitch;
        internal virtual global::PCL.MyExtraTextButton BtnSwitch
        {
            get
            {
                return __LfPart_BtnSwitch;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyExtraTextButton.ClickEventHandler hook0 = new global::PCL.MyExtraTextButton.ClickEventHandler(this.BtnSwitch_Click);
                global::PCL.MyExtraTextButton previous = __LfPart_BtnSwitch;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnSwitch = value;
                global::PCL.MyExtraTextButton current = __LfPart_BtnSwitch;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private void __LfHandle_0(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.ImportPersonalOptionsClick();
        }

        private void __LfHandle_1(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.BtnSkinChange_Click((global::System.Object)arg0, (global::System.EventArgs)arg1);
        }

        private void __LfHandle_2(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.BtnSkinDelete_Click((global::System.Object)arg0, (global::System.EventArgs)arg1);
        }

        private void __LfHandle_3(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.BtnSkinSave_Click((global::System.Object)arg0, (global::System.EventArgs)arg1);
        }

        private void __LfHandle_4(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.BtnSkinCache_Click((global::System.Object)arg0, (global::System.EventArgs)arg1);
        }

        private void __LfHandle_5(global::System.Object arg0, global::System.Boolean arg1)
        {
            this.RefreshRam();
        }

        private void __LfHandle_6(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.RefreshRam();
        }

        private void __LfHandle_7(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.RefreshRam();
        }

        private void __LfHandle_8(global::System.Object arg0, global::System.Windows.SizeChangedEventArgs arg1)
        {
            this.RefreshRamText();
        }

        private void __LfHandle_9(global::System.Object arg0, global::System.Windows.SizeChangedEventArgs arg1)
        {
            this.RefreshRamText();
        }

        private void __LfHandle_10(global::System.Object arg0, global::System.Windows.SizeChangedEventArgs arg1)
        {
            this.RefreshRamText();
        }

        private void __LfHandle_11(global::System.Object arg0, global::System.EventArgs arg1)
        {
            this.BtnAdvanceJavaSearch_Click();
        }

        private void __LfHandle_12(global::System.Object arg0, global::System.Windows.Controls.SelectionChangedEventArgs arg1)
        {
            this.WindowTypeUIRefresh();
        }

        private void __LfHandle_13(global::System.Object arg0, global::System.Boolean arg1)
        {
            this.CheckArgumentRam_Change();
        }

        private void __LfHandle_14(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.TextAdvanceJvm_TextChanged();
        }

        private void __LfHandle_15(global::System.Object arg0, global::System.Windows.Controls.TextChangedEventArgs arg1)
        {
            this.ReplaceEnter((global::PCL.MyTextBox)arg0, (global::System.Windows.Controls.TextChangedEventArgs)arg1);
        }

        private void __LfHandle_16(global::System.Object arg0, global::System.Windows.Controls.TextChangedEventArgs arg1)
        {
            this.ReplaceEnter((global::PCL.MyTextBox)arg0, (global::System.Windows.Controls.TextChangedEventArgs)arg1);
        }

        private void __LfAttachOwnHandles()
        {
            this.Loaded += new global::System.Windows.RoutedEventHandler(this.PageSetupLaunch_Loaded);
        }
    }
}
