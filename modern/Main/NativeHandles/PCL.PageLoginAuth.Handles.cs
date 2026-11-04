// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class PageLoginAuth
    {
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
                global::PCL.MyComboBox.TextChangedEventHandler hook0 = new global::PCL.MyComboBox.TextChangedEventHandler(this.ComboName_TextChanged);
                global::System.Windows.Controls.SelectionChangedEventHandler hook1 = new global::System.Windows.Controls.SelectionChangedEventHandler(this.__LfHandle_0);
                global::PCL.MyComboBox.TextChangedEventHandler hook2 = new global::PCL.MyComboBox.TextChangedEventHandler(this.__LfHandle_2);
                global::PCL.MyComboBox previous = __LfPart_ComboName;
                if (previous != null)
                {
                    previous.TextChanged -= hook0;
                    previous.SelectionChanged -= hook1;
                    previous.TextChanged -= hook2;
                }

                __LfPart_ComboName = value;
                global::PCL.MyComboBox current = __LfPart_ComboName;
                if (current != null)
                {
                    current.TextChanged += hook0;
                    current.SelectionChanged += hook1;
                    current.TextChanged += hook2;
                }
            }
        }

        private global::System.Windows.Controls.PasswordBox __LfPart_TextPass;
        internal virtual global::System.Windows.Controls.PasswordBox TextPass
        {
            get
            {
                return __LfPart_TextPass;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.RoutedEventHandler hook0 = new global::System.Windows.RoutedEventHandler(this.TextPass_PasswordChanged);
                global::System.Windows.Controls.PasswordBox previous = __LfPart_TextPass;
                if (previous != null)
                {
                    previous.PasswordChanged -= hook0;
                }

                __LfPart_TextPass = value;
                global::System.Windows.Controls.PasswordBox current = __LfPart_TextPass;
                if (current != null)
                {
                    current.PasswordChanged += hook0;
                }
            }
        }

        private global::PCL.MyCheckBox __LfPart_CheckRemember;
        internal virtual global::PCL.MyCheckBox CheckRemember
        {
            get
            {
                return __LfPart_CheckRemember;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyCheckBox.ChangeEventHandler hook0 = new global::PCL.MyCheckBox.ChangeEventHandler(this.__LfHandle_1);
                global::PCL.MyCheckBox previous = __LfPart_CheckRemember;
                if (previous != null)
                {
                    previous.Change -= hook0;
                }

                __LfPart_CheckRemember = value;
                global::PCL.MyCheckBox current = __LfPart_CheckRemember;
                if (current != null)
                {
                    current.Change += hook0;
                }
            }
        }

        private global::PCL.MyTextButton __LfPart_BtnLink;
        internal virtual global::PCL.MyTextButton BtnLink
        {
            get
            {
                return __LfPart_BtnLink;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyTextButton.ClickEventHandler hook0 = new global::PCL.MyTextButton.ClickEventHandler(this.Btn_Click);
                global::PCL.MyTextButton previous = __LfPart_BtnLink;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnLink = value;
                global::PCL.MyTextButton current = __LfPart_BtnLink;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private void __LfHandle_0(global::System.Object arg0, global::System.Windows.Controls.SelectionChangedEventArgs arg1)
        {
            this.ComboName_SelectionChanged((global::PCL.MyComboBox)arg0, (global::System.Windows.Controls.SelectionChangedEventArgs)arg1);
        }

        private void __LfHandle_1(global::System.Object arg0, global::System.Boolean arg1)
        {
            this.CheckBoxChange((global::PCL.MyCheckBox)arg0, (global::System.Object)arg1);
        }

        private void __LfHandle_2(global::System.Object arg0, global::System.Windows.Controls.TextChangedEventArgs arg1)
        {
            this.ComboName_TextChanged();
        }

        private void __LfHandle_3(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.ReloadRegisterButton();
        }

        private void __LfAttachOwnHandles()
        {
            this.Loaded += new global::System.Windows.RoutedEventHandler(this.__LfHandle_3);
        }
    }
}
