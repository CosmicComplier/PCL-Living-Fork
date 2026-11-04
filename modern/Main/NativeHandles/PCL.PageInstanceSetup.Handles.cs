// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class PageInstanceSetup
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
                global::System.Windows.Controls.SelectionChangedEventHandler hook0 = new global::System.Windows.Controls.SelectionChangedEventHandler(this.ComboArgumentIndieV2_SelectionChanged);
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

        private global::PCL.MyTextBox __LfPart_TextArgumentTitle;
        internal virtual global::PCL.MyTextBox TextArgumentTitle
        {
            get
            {
                return __LfPart_TextArgumentTitle;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_TextArgumentTitle = value;
            }
        }

        private global::PCL.MyTextBox __LfPart_TextArgumentInfo;
        internal virtual global::PCL.MyTextBox TextArgumentInfo
        {
            get
            {
                return __LfPart_TextArgumentInfo;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_TextArgumentInfo = value;
            }
        }

        private global::PCL.MyComboBox __LfPart_ComboArgumentJava;
        internal virtual global::PCL.MyComboBox ComboArgumentJava
        {
            get
            {
                return __LfPart_ComboArgumentJava;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.Controls.SelectionChangedEventHandler hook0 = new global::System.Windows.Controls.SelectionChangedEventHandler(this.__LfHandle_13);
                global::System.Windows.Controls.SelectionChangedEventHandler hook1 = new global::System.Windows.Controls.SelectionChangedEventHandler(this.__LfHandle_15);
                global::PCL.MyComboBox previous = __LfPart_ComboArgumentJava;
                if (previous != null)
                {
                    previous.SelectionChanged -= hook0;
                    previous.SelectionChanged -= hook1;
                }

                __LfPart_ComboArgumentJava = value;
                global::PCL.MyComboBox current = __LfPart_ComboArgumentJava;
                if (current != null)
                {
                    current.SelectionChanged += hook0;
                    current.SelectionChanged += hook1;
                }
            }
        }

        private global::PCL.MyTextBox __LfPart_TextArgumentJavaRange;
        internal virtual global::PCL.MyTextBox TextArgumentJavaRange
        {
            get
            {
                return __LfPart_TextArgumentJavaRange;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.RoutedEventHandler hook0 = new global::System.Windows.RoutedEventHandler(this.__LfHandle_14);
                global::System.Windows.Controls.TextChangedEventHandler hook1 = new global::System.Windows.Controls.TextChangedEventHandler(this.__LfHandle_16);
                global::PCL.MyTextBox previous = __LfPart_TextArgumentJavaRange;
                if (previous != null)
                {
                    previous.ValidatedTextChanged -= hook0;
                    previous.TextChanged -= hook1;
                }

                __LfPart_TextArgumentJavaRange = value;
                global::PCL.MyTextBox current = __LfPart_TextArgumentJavaRange;
                if (current != null)
                {
                    current.ValidatedTextChanged += hook0;
                    current.TextChanged += hook1;
                }
            }
        }

        private global::PCL.MyHint __LfPart_HintArgumentJava;
        internal virtual global::PCL.MyHint HintArgumentJava
        {
            get
            {
                return __LfPart_HintArgumentJava;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyHint.ClickEventHandler hook0 = new global::PCL.MyHint.ClickEventHandler(this.HintArgumentJava_Click);
                global::PCL.MyHint previous = __LfPart_HintArgumentJava;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_HintArgumentJava = value;
                global::PCL.MyHint current = __LfPart_HintArgumentJava;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::System.Windows.Controls.Grid __LfPart_PanArgumentJavaSelect;
        internal virtual global::System.Windows.Controls.Grid PanArgumentJavaSelect
        {
            get
            {
                return __LfPart_PanArgumentJavaSelect;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanArgumentJavaSelect = value;
            }
        }

        private global::PCL.MyComboBox __LfPart_ComboArgumentJavaSelect;
        internal virtual global::PCL.MyComboBox ComboArgumentJavaSelect
        {
            get
            {
                return __LfPart_ComboArgumentJavaSelect;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ComboArgumentJavaSelect = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabArgumentJava;
        internal virtual global::System.Windows.Controls.TextBlock LabArgumentJava
        {
            get
            {
                return __LfPart_LabArgumentJava;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabArgumentJava = value;
            }
        }

        private global::PCL.MyIconButton __LfPart_BtnArgumentJavaSearch;
        internal virtual global::PCL.MyIconButton BtnArgumentJavaSearch
        {
            get
            {
                return __LfPart_BtnArgumentJavaSearch;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconButton.ClickEventHandler hook0 = new global::PCL.MyIconButton.ClickEventHandler(this.__LfHandle_17);
                global::PCL.MyIconButton previous = __LfPart_BtnArgumentJavaSearch;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnArgumentJavaSearch = value;
                global::PCL.MyIconButton current = __LfPart_BtnArgumentJavaSearch;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyRadioBox __LfPart_RadioRamType2;
        internal virtual global::PCL.MyRadioBox RadioRamType2
        {
            get
            {
                return __LfPart_RadioRamType2;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.IMyRadio.CheckEventHandler hook0 = new global::PCL.IMyRadio.CheckEventHandler(this.__LfHandle_3);
                global::PCL.MyRadioBox previous = __LfPart_RadioRamType2;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_RadioRamType2 = value;
                global::PCL.MyRadioBox current = __LfPart_RadioRamType2;
                if (current != null)
                {
                    current.Check += hook0;
                }
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
                global::PCL.IMyRadio.CheckEventHandler hook0 = new global::PCL.IMyRadio.CheckEventHandler(this.__LfHandle_1);
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
                global::PCL.IMyRadio.CheckEventHandler hook0 = new global::PCL.IMyRadio.CheckEventHandler(this.__LfHandle_2);
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
                global::PCL.MySlider.ChangeEventHandler hook0 = new global::PCL.MySlider.ChangeEventHandler(this.__LfHandle_0);
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

        private global::PCL.MyComboBox __LfPart_ComboRamOptimize;
        internal virtual global::PCL.MyComboBox ComboRamOptimize
        {
            get
            {
                return __LfPart_ComboRamOptimize;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ComboRamOptimize = value;
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
                global::System.Windows.SizeChangedEventHandler hook0 = new global::System.Windows.SizeChangedEventHandler(this.__LfHandle_4);
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
                global::System.Windows.SizeChangedEventHandler hook0 = new global::System.Windows.SizeChangedEventHandler(this.__LfHandle_5);
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
                global::System.Windows.SizeChangedEventHandler hook0 = new global::System.Windows.SizeChangedEventHandler(this.__LfHandle_6);
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

        private global::PCL.MyCard __LfPart_CardServer;
        internal virtual global::PCL.MyCard CardServer
        {
            get
            {
                return __LfPart_CardServer;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CardServer = value;
            }
        }

        private global::PCL.MyComboBox __LfPart_ComboServerLogin;
        internal virtual global::PCL.MyComboBox ComboServerLogin
        {
            get
            {
                return __LfPart_ComboServerLogin;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.Controls.SelectionChangedEventHandler hook0 = new global::System.Windows.Controls.SelectionChangedEventHandler(this.__LfHandle_8);
                global::PCL.MyComboBox previous = __LfPart_ComboServerLogin;
                if (previous != null)
                {
                    previous.SelectionChanged -= hook0;
                }

                __LfPart_ComboServerLogin = value;
                global::PCL.MyComboBox current = __LfPart_ComboServerLogin;
                if (current != null)
                {
                    current.SelectionChanged += hook0;
                }
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabServerNide;
        internal virtual global::System.Windows.Controls.TextBlock LabServerNide
        {
            get
            {
                return __LfPart_LabServerNide;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabServerNide = value;
            }
        }

        private global::PCL.MyTextBox __LfPart_TextServerNide;
        internal virtual global::PCL.MyTextBox TextServerNide
        {
            get
            {
                return __LfPart_TextServerNide;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.RoutedEventHandler hook0 = new global::System.Windows.RoutedEventHandler(this.__LfHandle_9);
                global::PCL.MyTextBox previous = __LfPart_TextServerNide;
                if (previous != null)
                {
                    previous.ValidatedTextChanged -= hook0;
                }

                __LfPart_TextServerNide = value;
                global::PCL.MyTextBox current = __LfPart_TextServerNide;
                if (current != null)
                {
                    current.ValidatedTextChanged += hook0;
                }
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabServerAuthServer;
        internal virtual global::System.Windows.Controls.TextBlock LabServerAuthServer
        {
            get
            {
                return __LfPart_LabServerAuthServer;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabServerAuthServer = value;
            }
        }

        private global::PCL.MyTextBox __LfPart_TextServerAuthServer;
        internal virtual global::PCL.MyTextBox TextServerAuthServer
        {
            get
            {
                return __LfPart_TextServerAuthServer;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.RoutedEventHandler hook0 = new global::System.Windows.RoutedEventHandler(this.__LfHandle_10);
                global::PCL.MyTextBox previous = __LfPart_TextServerAuthServer;
                if (previous != null)
                {
                    previous.ValidatedTextChanged -= hook0;
                }

                __LfPart_TextServerAuthServer = value;
                global::PCL.MyTextBox current = __LfPart_TextServerAuthServer;
                if (current != null)
                {
                    current.ValidatedTextChanged += hook0;
                }
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabServerAuthRegister;
        internal virtual global::System.Windows.Controls.TextBlock LabServerAuthRegister
        {
            get
            {
                return __LfPart_LabServerAuthRegister;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabServerAuthRegister = value;
            }
        }

        private global::PCL.MyTextBox __LfPart_TextServerAuthRegister;
        internal virtual global::PCL.MyTextBox TextServerAuthRegister
        {
            get
            {
                return __LfPart_TextServerAuthRegister;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.RoutedEventHandler hook0 = new global::System.Windows.RoutedEventHandler(this.__LfHandle_11);
                global::PCL.MyTextBox previous = __LfPart_TextServerAuthRegister;
                if (previous != null)
                {
                    previous.ValidatedTextChanged -= hook0;
                }

                __LfPart_TextServerAuthRegister = value;
                global::PCL.MyTextBox current = __LfPart_TextServerAuthRegister;
                if (current != null)
                {
                    current.ValidatedTextChanged += hook0;
                }
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabServerAuthName;
        internal virtual global::System.Windows.Controls.TextBlock LabServerAuthName
        {
            get
            {
                return __LfPart_LabServerAuthName;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabServerAuthName = value;
            }
        }

        private global::PCL.MyTextBox __LfPart_TextServerAuthName;
        internal virtual global::PCL.MyTextBox TextServerAuthName
        {
            get
            {
                return __LfPart_TextServerAuthName;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_TextServerAuthName = value;
            }
        }

        private global::PCL.MyTextBox __LfPart_TextServerEnter;
        internal virtual global::PCL.MyTextBox TextServerEnter
        {
            get
            {
                return __LfPart_TextServerEnter;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.Controls.TextChangedEventHandler hook0 = new global::System.Windows.Controls.TextChangedEventHandler(this.__LfHandle_7);
                global::PCL.MyTextBox previous = __LfPart_TextServerEnter;
                if (previous != null)
                {
                    previous.TextChanged -= hook0;
                }

                __LfPart_TextServerEnter = value;
                global::PCL.MyTextBox current = __LfPart_TextServerEnter;
                if (current != null)
                {
                    current.TextChanged += hook0;
                }
            }
        }

        private global::System.Windows.Controls.Grid __LfPart_PanServerNide;
        internal virtual global::System.Windows.Controls.Grid PanServerNide
        {
            get
            {
                return __LfPart_PanServerNide;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanServerNide = value;
            }
        }

        private global::PCL.MyButton __LfPart_BtnServerAuthLittle;
        internal virtual global::PCL.MyButton BtnServerAuthLittle
        {
            get
            {
                return __LfPart_BtnServerAuthLittle;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_12);
                global::PCL.MyButton previous = __LfPart_BtnServerAuthLittle;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnServerAuthLittle = value;
                global::PCL.MyButton current = __LfPart_BtnServerAuthLittle;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyCard __LfPart_CardAdvance;
        internal virtual global::PCL.MyCard CardAdvance
        {
            get
            {
                return __LfPart_CardAdvance;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CardAdvance = value;
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
                global::System.Windows.Controls.TextChangedEventHandler hook0 = new global::System.Windows.Controls.TextChangedEventHandler(this.__LfHandle_18);
                global::PCL.MyTextBox previous = __LfPart_TextAdvanceJvm;
                if (previous != null)
                {
                    previous.TextChanged -= hook0;
                }

                __LfPart_TextAdvanceJvm = value;
                global::PCL.MyTextBox current = __LfPart_TextAdvanceJvm;
                if (current != null)
                {
                    current.TextChanged += hook0;
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
                global::System.Windows.Controls.TextChangedEventHandler hook0 = new global::System.Windows.Controls.TextChangedEventHandler(this.__LfHandle_19);
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

        private global::PCL.MyCheckBox __LfPart_CheckAdvanceDisableModUpdate;
        internal virtual global::PCL.MyCheckBox CheckAdvanceDisableModUpdate
        {
            get
            {
                return __LfPart_CheckAdvanceDisableModUpdate;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CheckAdvanceDisableModUpdate = value;
            }
        }

        private global::PCL.MyCheckBox __LfPart_CheckAdvanceAssetsV2;
        internal virtual global::PCL.MyCheckBox CheckAdvanceAssetsV2
        {
            get
            {
                return __LfPart_CheckAdvanceAssetsV2;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CheckAdvanceAssetsV2 = value;
            }
        }

        private global::PCL.MyCheckBox __LfPart_CheckAdvanceDisableJLW;
        internal virtual global::PCL.MyCheckBox CheckAdvanceDisableJLW
        {
            get
            {
                return __LfPart_CheckAdvanceDisableJLW;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CheckAdvanceDisableJLW = value;
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

        private void __LfHandle_0(global::System.Object arg0, global::System.Boolean arg1)
        {
            this.RefreshRam();
        }

        private void __LfHandle_1(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.RefreshRam();
        }

        private void __LfHandle_2(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.RefreshRam();
        }

        private void __LfHandle_3(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.RefreshRam();
        }

        private void __LfHandle_4(global::System.Object arg0, global::System.Windows.SizeChangedEventArgs arg1)
        {
            this.RefreshRamText();
        }

        private void __LfHandle_5(global::System.Object arg0, global::System.Windows.SizeChangedEventArgs arg1)
        {
            this.RefreshRamText();
        }

        private void __LfHandle_6(global::System.Object arg0, global::System.Windows.SizeChangedEventArgs arg1)
        {
            this.RefreshRamText();
        }

        private void __LfHandle_7(global::System.Object arg0, global::System.Windows.Controls.TextChangedEventArgs arg1)
        {
            this.TextServerEnter_Change();
        }

        private void __LfHandle_8(global::System.Object arg0, global::System.Windows.Controls.SelectionChangedEventArgs arg1)
        {
            this.ComboServerLogin_Changed();
        }

        private void __LfHandle_9(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.ComboServerLogin_Changed();
        }

        private void __LfHandle_10(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.ComboServerLogin_Changed();
        }

        private void __LfHandle_11(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.ComboServerLogin_Changed();
        }

        private void __LfHandle_12(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.BtnServerAuthLittle_Click((global::System.Object)arg0, (global::System.EventArgs)arg1);
        }

        private void __LfHandle_13(global::System.Object arg0, global::System.Windows.Controls.SelectionChangedEventArgs arg1)
        {
            this.ReloadCurrentJava();
        }

        private void __LfHandle_14(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.ReloadCurrentJava();
        }

        private void __LfHandle_15(global::System.Object arg0, global::System.Windows.Controls.SelectionChangedEventArgs arg1)
        {
            this.UpdateJavaRelatedUi();
        }

        private void __LfHandle_16(global::System.Object arg0, global::System.Windows.Controls.TextChangedEventArgs arg1)
        {
            this.UpdateJavaRelatedUi();
        }

        private void __LfHandle_17(global::System.Object arg0, global::System.EventArgs arg1)
        {
            this.BtnAdvanceJavaSearch_Click();
        }

        private void __LfHandle_18(global::System.Object arg0, global::System.Windows.Controls.TextChangedEventArgs arg1)
        {
            this.ReplaceEnter((global::PCL.MyTextBox)arg0, (global::System.Windows.Controls.TextChangedEventArgs)arg1);
        }

        private void __LfHandle_19(global::System.Object arg0, global::System.Windows.Controls.TextChangedEventArgs arg1)
        {
            this.ReplaceEnter((global::PCL.MyTextBox)arg0, (global::System.Windows.Controls.TextChangedEventArgs)arg1);
        }

        private void __LfAttachOwnHandles()
        {
            this.Loaded += new global::System.Windows.RoutedEventHandler(this.PageSetupSystem_Loaded);
        }
    }
}
