// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class PageLaunchLeft
    {
        private global::PCL.PageLaunchLeft __LfPart_PanBack;
        internal virtual global::PCL.PageLaunchLeft PanBack
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

        private global::System.Windows.Controls.Grid __LfPart_PanInput;
        internal virtual global::System.Windows.Controls.Grid PanInput
        {
            get
            {
                return __LfPart_PanInput;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanInput = value;
            }
        }

        private global::PCL.MyButton __LfPart_BtnVersion;
        internal virtual global::PCL.MyButton BtnVersion
        {
            get
            {
                return __LfPart_BtnVersion;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_1);
                global::PCL.MyButton previous = __LfPart_BtnVersion;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnVersion = value;
                global::PCL.MyButton current = __LfPart_BtnVersion;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyButton __LfPart_BtnMore;
        internal virtual global::PCL.MyButton BtnMore
        {
            get
            {
                return __LfPart_BtnMore;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_5);
                global::PCL.MyButton previous = __LfPart_BtnMore;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnMore = value;
                global::PCL.MyButton current = __LfPart_BtnMore;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::System.Windows.Controls.Grid __LfPart_PanLogin;
        internal virtual global::System.Windows.Controls.Grid PanLogin
        {
            get
            {
                return __LfPart_PanLogin;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanLogin = value;
            }
        }

        private global::System.Windows.Controls.Grid __LfPart_PanTypeOne;
        internal virtual global::System.Windows.Controls.Grid PanTypeOne
        {
            get
            {
                return __LfPart_PanTypeOne;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanTypeOne = value;
            }
        }

        private global::System.Windows.Shapes.Path __LfPart_PathTypeOne;
        internal virtual global::System.Windows.Shapes.Path PathTypeOne
        {
            get
            {
                return __LfPart_PathTypeOne;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PathTypeOne = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabTypeOne;
        internal virtual global::System.Windows.Controls.TextBlock LabTypeOne
        {
            get
            {
                return __LfPart_LabTypeOne;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabTypeOne = value;
            }
        }

        private global::System.Windows.Controls.Grid __LfPart_PanType;
        internal virtual global::System.Windows.Controls.Grid PanType
        {
            get
            {
                return __LfPart_PanType;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanType = value;
            }
        }

        private global::PCL.MyRadioButton __LfPart_RadioLoginType5;
        internal virtual global::PCL.MyRadioButton RadioLoginType5
        {
            get
            {
                return __LfPart_RadioLoginType5;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyRadioButton.CheckEventHandler hook0 = new global::PCL.MyRadioButton.CheckEventHandler(this.RadioLoginType_Change);
                global::PCL.MyRadioButton previous = __LfPart_RadioLoginType5;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_RadioLoginType5 = value;
                global::PCL.MyRadioButton current = __LfPart_RadioLoginType5;
                if (current != null)
                {
                    current.Check += hook0;
                }
            }
        }

        private global::PCL.MyRadioButton __LfPart_RadioLoginType0;
        internal virtual global::PCL.MyRadioButton RadioLoginType0
        {
            get
            {
                return __LfPart_RadioLoginType0;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyRadioButton.CheckEventHandler hook0 = new global::PCL.MyRadioButton.CheckEventHandler(this.RadioLoginType_Change);
                global::PCL.MyRadioButton previous = __LfPart_RadioLoginType0;
                if (previous != null)
                {
                    previous.Check -= hook0;
                }

                __LfPart_RadioLoginType0 = value;
                global::PCL.MyRadioButton current = __LfPart_RadioLoginType0;
                if (current != null)
                {
                    current.Check += hook0;
                }
            }
        }

        private global::System.Windows.Media.ScaleTransform __LfPart_AprilScaleTrans;
        internal virtual global::System.Windows.Media.ScaleTransform AprilScaleTrans
        {
            get
            {
                return __LfPart_AprilScaleTrans;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_AprilScaleTrans = value;
            }
        }

        private global::System.Windows.Media.TranslateTransform __LfPart_AprilPosTrans;
        internal virtual global::System.Windows.Media.TranslateTransform AprilPosTrans
        {
            get
            {
                return __LfPart_AprilPosTrans;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_AprilPosTrans = value;
            }
        }

        private global::PCL.MyButton __LfPart_BtnLaunch;
        internal virtual global::PCL.MyButton BtnLaunch
        {
            get
            {
                return __LfPart_BtnLaunch;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_2);
                global::System.Windows.RoutedEventHandler hook1 = new global::System.Windows.RoutedEventHandler(this.__LfHandle_3);
                global::PCL.MyButton previous = __LfPart_BtnLaunch;
                if (previous != null)
                {
                    previous.Click -= hook0;
                    previous.Loaded -= hook1;
                }

                __LfPart_BtnLaunch = value;
                global::PCL.MyButton current = __LfPart_BtnLaunch;
                if (current != null)
                {
                    current.Click += hook0;
                    current.Loaded += hook1;
                }
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabVersion;
        internal virtual global::System.Windows.Controls.TextBlock LabVersion
        {
            get
            {
                return __LfPart_LabVersion;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabVersion = value;
            }
        }

        private global::System.Windows.Controls.Grid __LfPart_PanLaunching;
        internal virtual global::System.Windows.Controls.Grid PanLaunching
        {
            get
            {
                return __LfPart_PanLaunching;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanLaunching = value;
            }
        }

        private global::PCL.MyLoading __LfPart_LoadLaunching;
        internal virtual global::PCL.MyLoading LoadLaunching
        {
            get
            {
                return __LfPart_LoadLaunching;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LoadLaunching = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabLaunchingTitle;
        internal virtual global::System.Windows.Controls.TextBlock LabLaunchingTitle
        {
            get
            {
                return __LfPart_LabLaunchingTitle;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabLaunchingTitle = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabLaunchingName;
        internal virtual global::System.Windows.Controls.TextBlock LabLaunchingName
        {
            get
            {
                return __LfPart_LabLaunchingName;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabLaunchingName = value;
            }
        }

        private global::System.Windows.Controls.ColumnDefinition __LfPart_ProgressLaunchingFinished;
        internal virtual global::System.Windows.Controls.ColumnDefinition ProgressLaunchingFinished
        {
            get
            {
                return __LfPart_ProgressLaunchingFinished;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ProgressLaunchingFinished = value;
            }
        }

        private global::System.Windows.Controls.ColumnDefinition __LfPart_ProgressLaunchingUnfinished;
        internal virtual global::System.Windows.Controls.ColumnDefinition ProgressLaunchingUnfinished
        {
            get
            {
                return __LfPart_ProgressLaunchingUnfinished;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ProgressLaunchingUnfinished = value;
            }
        }

        private global::System.Windows.Controls.Grid __LfPart_PanLaunchingInfo;
        internal virtual global::System.Windows.Controls.Grid PanLaunchingInfo
        {
            get
            {
                return __LfPart_PanLaunchingInfo;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.SizeChangedEventHandler hook0 = new global::System.Windows.SizeChangedEventHandler(this.PanLaunchingInfo_SizeChangedW);
                global::System.Windows.SizeChangedEventHandler hook1 = new global::System.Windows.SizeChangedEventHandler(this.PanLaunchingInfo_SizeChangedH);
                global::System.Windows.Controls.Grid previous = __LfPart_PanLaunchingInfo;
                if (previous != null)
                {
                    previous.SizeChanged -= hook0;
                    previous.SizeChanged -= hook1;
                }

                __LfPart_PanLaunchingInfo = value;
                global::System.Windows.Controls.Grid current = __LfPart_PanLaunchingInfo;
                if (current != null)
                {
                    current.SizeChanged += hook0;
                    current.SizeChanged += hook1;
                }
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabLaunchingStage;
        internal virtual global::System.Windows.Controls.TextBlock LabLaunchingStage
        {
            get
            {
                return __LfPart_LabLaunchingStage;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabLaunchingStage = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabLaunchingMethod;
        internal virtual global::System.Windows.Controls.TextBlock LabLaunchingMethod
        {
            get
            {
                return __LfPart_LabLaunchingMethod;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabLaunchingMethod = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabLaunchingProgressLeft;
        internal virtual global::System.Windows.Controls.TextBlock LabLaunchingProgressLeft
        {
            get
            {
                return __LfPart_LabLaunchingProgressLeft;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabLaunchingProgressLeft = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabLaunchingProgress;
        internal virtual global::System.Windows.Controls.TextBlock LabLaunchingProgress
        {
            get
            {
                return __LfPart_LabLaunchingProgress;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabLaunchingProgress = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabLaunchingDownloadLeft;
        internal virtual global::System.Windows.Controls.TextBlock LabLaunchingDownloadLeft
        {
            get
            {
                return __LfPart_LabLaunchingDownloadLeft;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabLaunchingDownloadLeft = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabLaunchingDownload;
        internal virtual global::System.Windows.Controls.TextBlock LabLaunchingDownload
        {
            get
            {
                return __LfPart_LabLaunchingDownload;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabLaunchingDownload = value;
            }
        }

        private global::System.Windows.Controls.Grid __LfPart_PanLaunchingHint;
        internal virtual global::System.Windows.Controls.Grid PanLaunchingHint
        {
            get
            {
                return __LfPart_PanLaunchingHint;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanLaunchingHint = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabLaunchingHint;
        internal virtual global::System.Windows.Controls.TextBlock LabLaunchingHint
        {
            get
            {
                return __LfPart_LabLaunchingHint;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabLaunchingHint = value;
            }
        }

        private global::PCL.MyButton __LfPart_BtnCancel;
        internal virtual global::PCL.MyButton BtnCancel
        {
            get
            {
                return __LfPart_BtnCancel;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.__LfHandle_4);
                global::PCL.MyButton previous = __LfPart_BtnCancel;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnCancel = value;
                global::PCL.MyButton current = __LfPart_BtnCancel;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private void __LfHandle_0(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.PageLaunchLeft_Loaded();
        }

        private void __LfHandle_1(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.BtnVersion_Click();
        }

        private void __LfHandle_2(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.LaunchButtonClick();
        }

        private void __LfHandle_3(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.RefreshButtonsUI();
        }

        private void __LfHandle_4(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.BtnCancel_Click();
        }

        private void __LfHandle_5(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.BtnMore_Click();
        }

        private void __LfAttachOwnHandles()
        {
            this.Loaded += new global::System.Windows.RoutedEventHandler(this.__LfHandle_0);
        }
    }
}
