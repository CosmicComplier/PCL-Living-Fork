// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class PageLoginLegacy
    {
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

        private global::PCL.MyHint __LfPart_HintChinese;
        internal virtual global::PCL.MyHint HintChinese
        {
            get
            {
                return __LfPart_HintChinese;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_HintChinese = value;
            }
        }

        private global::PCL.MySkin __LfPart_Skin;
        internal virtual global::PCL.MySkin Skin
        {
            get
            {
                return __LfPart_Skin;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MySkin.ClickEventHandler hook0 = new global::PCL.MySkin.ClickEventHandler(this.__LfHandle_0);
                global::PCL.MySkin previous = __LfPart_Skin;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_Skin = value;
                global::PCL.MySkin current = __LfPart_Skin;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyComboBox __LfPart_ComboName;
        internal virtual global::PCL.MyComboBox ComboName
        {
            get
            {
                return __LfPart_ComboName;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.Input.KeyEventHandler hook0 = new global::System.Windows.Input.KeyEventHandler(this.ComboName_PreviewKeyDown);
                global::PCL.MyComboBox.TextChangedEventHandler hook1 = new global::PCL.MyComboBox.TextChangedEventHandler(this.ComboLegacy_TextChanged);
                global::PCL.MyComboBox previous = __LfPart_ComboName;
                if (previous != null)
                {
                    previous.PreviewKeyDown -= hook0;
                    previous.TextChanged -= hook1;
                }

                __LfPart_ComboName = value;
                global::PCL.MyComboBox current = __LfPart_ComboName;
                if (current != null)
                {
                    current.PreviewKeyDown += hook0;
                    current.TextChanged += hook1;
                }
            }
        }

        private void __LfHandle_0(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.Skin_Click();
        }

        private void __LfAttachOwnHandles()
        {
            this.Loaded += new global::System.Windows.RoutedEventHandler(this.PageLoginLegacy_Loaded);
        }
    }
}
