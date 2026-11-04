// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class MyMsgLogin
    {
        private global::System.Windows.Media.RotateTransform __LfPart_TransformRotate;
        internal virtual global::System.Windows.Media.RotateTransform TransformRotate
        {
            get
            {
                return __LfPart_TransformRotate;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_TransformRotate = value;
            }
        }

        private global::System.Windows.Media.TranslateTransform __LfPart_TransformPos;
        internal virtual global::System.Windows.Media.TranslateTransform TransformPos
        {
            get
            {
                return __LfPart_TransformPos;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_TransformPos = value;
            }
        }

        private global::System.Windows.Controls.Border __LfPart_PanBorder;
        internal virtual global::System.Windows.Controls.Border PanBorder
        {
            get
            {
                return __LfPart_PanBorder;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.Input.MouseButtonEventHandler hook0 = new global::System.Windows.Input.MouseButtonEventHandler(this.Drag);
                global::System.Windows.Controls.Border previous = __LfPart_PanBorder;
                if (previous != null)
                {
                    previous.MouseLeftButtonDown -= hook0;
                }

                __LfPart_PanBorder = value;
                global::System.Windows.Controls.Border current = __LfPart_PanBorder;
                if (current != null)
                {
                    current.MouseLeftButtonDown += hook0;
                }
            }
        }

        private global::System.Windows.Media.Effects.DropShadowEffect __LfPart_EffectShadow;
        internal virtual global::System.Windows.Media.Effects.DropShadowEffect EffectShadow
        {
            get
            {
                return __LfPart_EffectShadow;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_EffectShadow = value;
            }
        }

        private global::System.Windows.Controls.Grid __LfPart_PanMain;
        internal virtual global::System.Windows.Controls.Grid PanMain
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
                global::System.Windows.Input.MouseButtonEventHandler hook0 = new global::System.Windows.Input.MouseButtonEventHandler(this.Drag);
                global::System.Windows.Controls.TextBlock previous = __LfPart_LabTitle;
                if (previous != null)
                {
                    previous.MouseLeftButtonDown -= hook0;
                }

                __LfPart_LabTitle = value;
                global::System.Windows.Controls.TextBlock current = __LfPart_LabTitle;
                if (current != null)
                {
                    current.MouseLeftButtonDown += hook0;
                }
            }
        }

        private global::System.Windows.Shapes.Rectangle __LfPart_ShapeLine;
        internal virtual global::System.Windows.Shapes.Rectangle ShapeLine
        {
            get
            {
                return __LfPart_ShapeLine;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ShapeLine = value;
            }
        }

        private global::PCL.MyScrollViewer __LfPart_PanCaption;
        internal virtual global::PCL.MyScrollViewer PanCaption
        {
            get
            {
                return __LfPart_PanCaption;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanCaption = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabCaption;
        internal virtual global::System.Windows.Controls.TextBlock LabCaption
        {
            get
            {
                return __LfPart_LabCaption;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabCaption = value;
            }
        }

        private global::System.Windows.Controls.StackPanel __LfPart_PanBtn;
        internal virtual global::System.Windows.Controls.StackPanel PanBtn
        {
            get
            {
                return __LfPart_PanBtn;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanBtn = value;
            }
        }

        private global::PCL.MyButton __LfPart_Btn1;
        public virtual global::PCL.MyButton Btn1
        {
            get
            {
                return __LfPart_Btn1;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_1);
                global::PCL.MyButton previous = __LfPart_Btn1;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_Btn1 = value;
                global::PCL.MyButton current = __LfPart_Btn1;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyButton __LfPart_Btn2;
        public virtual global::PCL.MyButton Btn2
        {
            get
            {
                return __LfPart_Btn2;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_Btn2 = value;
            }
        }

        private global::PCL.MyButton __LfPart_Btn3;
        public virtual global::PCL.MyButton Btn3
        {
            get
            {
                return __LfPart_Btn3;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_2);
                global::PCL.MyButton previous = __LfPart_Btn3;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_Btn3 = value;
                global::PCL.MyButton current = __LfPart_Btn3;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private void __LfHandle_0(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.Load((global::System.Object)arg0, (global::System.EventArgs)arg1);
        }

        private void __LfHandle_1(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.Btn1_Click();
        }

        private void __LfHandle_2(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.Btn3_Click();
        }

        private void __LfAttachOwnHandles()
        {
            base.Loaded += new global::System.Windows.RoutedEventHandler(this.__LfHandle_0);
        }
    }
}
