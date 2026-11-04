// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class MyLocalModItem
    {
        private global::PCL.MyLocalModItem __LfPart_PanBack;
        internal virtual global::PCL.MyLocalModItem PanBack
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

        private global::System.Windows.Controls.ColumnDefinition __LfPart_ColumnPaddingRight;
        internal virtual global::System.Windows.Controls.ColumnDefinition ColumnPaddingRight
        {
            get
            {
                return __LfPart_ColumnPaddingRight;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ColumnPaddingRight = value;
            }
        }

        private global::PCL.MyImage __LfPart_PathLogo;
        internal virtual global::PCL.MyImage PathLogo
        {
            get
            {
                return __LfPart_PathLogo;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PathLogo = value;
            }
        }

        private global::System.Windows.Controls.Image __LfPart_ResourceLogo;
        internal virtual global::System.Windows.Controls.Image ResourceLogo
        {
            get
            {
                return __LfPart_ResourceLogo;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ResourceLogo = value;
            }
        }

        private global::System.Windows.Controls.Grid __LfPart_PanTitle;
        internal virtual global::System.Windows.Controls.Grid PanTitle
        {
            get
            {
                return __LfPart_PanTitle;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.SizeChangedEventHandler hook0 = new global::System.Windows.SizeChangedEventHandler(this.__LfHandle_12);
                global::System.Windows.Controls.Grid previous = __LfPart_PanTitle;
                if (previous != null)
                {
                    previous.SizeChanged -= hook0;
                }

                __LfPart_PanTitle = value;
                global::System.Windows.Controls.Grid current = __LfPart_PanTitle;
                if (current != null)
                {
                    current.SizeChanged += hook0;
                }
            }
        }

        private global::System.Windows.Controls.ColumnDefinition __LfPart_ColumnTitle;
        internal virtual global::System.Windows.Controls.ColumnDefinition ColumnTitle
        {
            get
            {
                return __LfPart_ColumnTitle;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ColumnTitle = value;
            }
        }

        private global::System.Windows.Controls.ColumnDefinition __LfPart_ColumnSubtitle;
        internal virtual global::System.Windows.Controls.ColumnDefinition ColumnSubtitle
        {
            get
            {
                return __LfPart_ColumnSubtitle;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ColumnSubtitle = value;
            }
        }

        private global::System.Windows.Controls.ColumnDefinition __LfPart_ColumnExtend;
        internal virtual global::System.Windows.Controls.ColumnDefinition ColumnExtend
        {
            get
            {
                return __LfPart_ColumnExtend;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ColumnExtend = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabTitle;
        internal virtual global::System.Windows.Controls.TextBlock LabTitle
        {
            get
            {
                return __LfPart_LabTitle;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabTitle = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabSubtitle;
        internal virtual global::System.Windows.Controls.TextBlock LabSubtitle
        {
            get
            {
                return __LfPart_LabSubtitle;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabSubtitle = value;
            }
        }

        private global::PCL.MyIconButton __LfPart_BtnUpdate;
        internal virtual global::PCL.MyIconButton BtnUpdate
        {
            get
            {
                return __LfPart_BtnUpdate;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.Input.MouseButtonEventHandler hook0 = new global::System.Windows.Input.MouseButtonEventHandler(this.BtnUpdate_PreviewMouseRightButtonUp);
                global::PCL.MyIconButton.ClickEventHandler hook1 = new global::PCL.MyIconButton.ClickEventHandler(this.BtnUpdate_Click);
                global::PCL.MyIconButton previous = __LfPart_BtnUpdate;
                if (previous != null)
                {
                    previous.PreviewMouseRightButtonUp -= hook0;
                    previous.Click -= hook1;
                }

                __LfPart_BtnUpdate = value;
                global::PCL.MyIconButton current = __LfPart_BtnUpdate;
                if (current != null)
                {
                    current.PreviewMouseRightButtonUp += hook0;
                    current.Click += hook1;
                }
            }
        }

        private global::System.Windows.Controls.StackPanel __LfPart_PanTags;
        internal virtual global::System.Windows.Controls.StackPanel PanTags
        {
            get
            {
                return __LfPart_PanTags;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanTags = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabInfo;
        internal virtual global::System.Windows.Controls.TextBlock LabInfo
        {
            get
            {
                return __LfPart_LabInfo;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabInfo = value;
            }
        }

        private void __LfHandle_0(global::System.Object arg0, global::System.Windows.Input.MouseEventArgs arg1)
        {
            this.Button_MouseLeave((global::System.Object)arg0, (global::System.Object)arg1);
        }

        private void __LfHandle_1(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.Button_MouseLeave((global::System.Object)arg0, (global::System.Object)arg1);
        }

        private void __LfHandle_2(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.Button_MouseSwipeStart((global::System.Object)arg0, (global::System.Object)arg1);
        }

        private void __LfHandle_3(global::System.Object arg0, global::System.Windows.Input.MouseEventArgs arg1)
        {
            this.Button_MouseSwipe((global::System.Object)arg0, (global::System.Object)arg1);
        }

        private void __LfHandle_4(global::System.Object arg0, global::System.Windows.Input.MouseEventArgs arg1)
        {
            this.Button_MouseSwipe((global::System.Object)arg0, (global::System.Object)arg1);
        }

        private void __LfHandle_5(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.Button_MouseSwipe((global::System.Object)arg0, (global::System.Object)arg1);
        }

        private void __LfHandle_6(global::System.Object arg0, global::System.Windows.Input.MouseEventArgs arg1)
        {
            this.RefreshColor((global::System.Object)arg0, (global::System.EventArgs)arg1);
        }

        private void __LfHandle_7(global::System.Object arg0, global::System.Windows.Input.MouseEventArgs arg1)
        {
            this.RefreshColor((global::System.Object)arg0, (global::System.EventArgs)arg1);
        }

        private void __LfHandle_8(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.RefreshColor((global::System.Object)arg0, (global::System.EventArgs)arg1);
        }

        private void __LfHandle_9(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.RefreshColor((global::System.Object)arg0, (global::System.EventArgs)arg1);
        }

        private void __LfHandle_10(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.RefreshColor((global::System.Object)arg0, (global::System.EventArgs)arg1);
        }

        private void __LfHandle_11(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.ReleaseResourceInteraction();
        }

        private void __LfHandle_12(global::System.Object arg0, global::System.Windows.SizeChangedEventArgs arg1)
        {
            this.PanTitle_SizeChanged();
        }

        private void __LfAttachOwnHandles()
        {
            this.PreviewMouseLeftButtonUp += new global::System.Windows.Input.MouseButtonEventHandler(this.Button_MouseUp);
            this.PreviewMouseLeftButtonDown += new global::System.Windows.Input.MouseButtonEventHandler(this.Button_MouseDown);
            this.MouseLeave += new global::System.Windows.Input.MouseEventHandler(this.__LfHandle_0);
            this.PreviewMouseLeftButtonUp += new global::System.Windows.Input.MouseButtonEventHandler(this.__LfHandle_1);
            this.MouseLeftButtonDown += new global::System.Windows.Input.MouseButtonEventHandler(this.__LfHandle_2);
            this.MouseEnter += new global::System.Windows.Input.MouseEventHandler(this.__LfHandle_3);
            this.MouseLeave += new global::System.Windows.Input.MouseEventHandler(this.__LfHandle_4);
            this.MouseLeftButtonUp += new global::System.Windows.Input.MouseButtonEventHandler(this.__LfHandle_5);
            this.MouseEnter += new global::System.Windows.Input.MouseEventHandler(this.__LfHandle_6);
            this.MouseLeave += new global::System.Windows.Input.MouseEventHandler(this.__LfHandle_7);
            this.MouseLeftButtonDown += new global::System.Windows.Input.MouseButtonEventHandler(this.__LfHandle_8);
            this.MouseLeftButtonUp += new global::System.Windows.Input.MouseButtonEventHandler(this.__LfHandle_9);
            this.Changed += new global::PCL.MyLocalModItem.ChangedEventHandler(this.__LfHandle_10);
            this.Unloaded += new global::System.Windows.RoutedEventHandler(this.__LfHandle_11);
        }
    }
}
