// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class PageDownloadInstall
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

        private global::System.Windows.Controls.Grid __LfPart_PanInner;
        internal virtual global::System.Windows.Controls.Grid PanInner
        {
            get
            {
                return __LfPart_PanInner;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanInner = value;
            }
        }

        private global::System.Windows.Controls.StackPanel __LfPart_PanMinecraft;
        internal virtual global::System.Windows.Controls.StackPanel PanMinecraft
        {
            get
            {
                return __LfPart_PanMinecraft;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanMinecraft = value;
            }
        }

        private global::System.Windows.Controls.StackPanel __LfPart_PanSelect;
        internal virtual global::System.Windows.Controls.StackPanel PanSelect
        {
            get
            {
                return __LfPart_PanSelect;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanSelect = value;
            }
        }

        private global::PCL.MyCard __LfPart_CardSelectName;
        internal virtual global::PCL.MyCard CardSelectName
        {
            get
            {
                return __LfPart_CardSelectName;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CardSelectName = value;
            }
        }

        private global::PCL.MyIconButton __LfPart_BtnBack;
        internal virtual global::PCL.MyIconButton BtnBack
        {
            get
            {
                return __LfPart_BtnBack;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconButton.ClickEventHandler hook0 = new global::PCL.MyIconButton.ClickEventHandler(this.__LfHandle_2);
                global::PCL.MyIconButton previous = __LfPart_BtnBack;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnBack = value;
                global::PCL.MyIconButton current = __LfPart_BtnBack;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyImage __LfPart_ImgLogo;
        internal virtual global::PCL.MyImage ImgLogo
        {
            get
            {
                return __LfPart_ImgLogo;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ImgLogo = value;
            }
        }

        private global::PCL.MyTextBox __LfPart_TextSelectName;
        internal virtual global::PCL.MyTextBox TextSelectName
        {
            get
            {
                return __LfPart_TextSelectName;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::System.Windows.Controls.TextChangedEventHandler hook0 = new global::System.Windows.Controls.TextChangedEventHandler(this.TextSelectName_TextChanged);
                global::PCL.MyTextBox.ValidateChangedEventHandler hook1 = new global::PCL.MyTextBox.ValidateChangedEventHandler(this.TextSelectName_ValidateChanged);
                global::System.Windows.Input.KeyEventHandler hook2 = new global::System.Windows.Input.KeyEventHandler(this.TextSelectName_KeyDown);
                global::PCL.MyTextBox previous = __LfPart_TextSelectName;
                if (previous != null)
                {
                    previous.TextChanged -= hook0;
                    global::PCL.MyTextBox.ValidateChanged -= hook1;
                    previous.KeyDown -= hook2;
                }

                __LfPart_TextSelectName = value;
                global::PCL.MyTextBox current = __LfPart_TextSelectName;
                if (current != null)
                {
                    current.TextChanged += hook0;
                    global::PCL.MyTextBox.ValidateChanged += hook1;
                    current.KeyDown += hook2;
                }
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabSelectName;
        internal virtual global::System.Windows.Controls.TextBlock LabSelectName
        {
            get
            {
                return __LfPart_LabSelectName;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabSelectName = value;
            }
        }

        private global::PCL.MyIconButton __LfPart_BtnOriginalNameRestore;
        internal virtual global::PCL.MyIconButton BtnOriginalNameRestore
        {
            get
            {
                return __LfPart_BtnOriginalNameRestore;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_BtnOriginalNameRestore = value;
            }
        }

        private global::PCL.MyHint __LfPart_HintSelectName;
        internal virtual global::PCL.MyHint HintSelectName
        {
            get
            {
                return __LfPart_HintSelectName;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_HintSelectName = value;
            }
        }

        private global::PCL.MyHint __LfPart_HintFabricAPI;
        internal virtual global::PCL.MyHint HintFabricAPI
        {
            get
            {
                return __LfPart_HintFabricAPI;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_HintFabricAPI = value;
            }
        }

        private global::PCL.MyHint __LfPart_HintOptiFabric;
        internal virtual global::PCL.MyHint HintOptiFabric
        {
            get
            {
                return __LfPart_HintOptiFabric;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_HintOptiFabric = value;
            }
        }

        private global::PCL.MyHint __LfPart_HintOptiFabricOld;
        internal virtual global::PCL.MyHint HintOptiFabricOld
        {
            get
            {
                return __LfPart_HintOptiFabricOld;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_HintOptiFabricOld = value;
            }
        }

        private global::PCL.MyHint __LfPart_HintModOptiFine;
        internal virtual global::PCL.MyHint HintModOptiFine
        {
            get
            {
                return __LfPart_HintModOptiFine;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_HintModOptiFine = value;
            }
        }

        private global::PCL.MyCard __LfPart_CardMinecraft;
        internal virtual global::PCL.MyCard CardMinecraft
        {
            get
            {
                return __LfPart_CardMinecraft;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_CardMinecraft = value;
            }
        }

        private global::System.Windows.Controls.Grid __LfPart_PanMinecraftInfo;
        internal virtual global::System.Windows.Controls.Grid PanMinecraftInfo
        {
            get
            {
                return __LfPart_PanMinecraftInfo;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanMinecraftInfo = value;
            }
        }

        private global::PCL.MyImage __LfPart_ImgMinecraft;
        internal virtual global::PCL.MyImage ImgMinecraft
        {
            get
            {
                return __LfPart_ImgMinecraft;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ImgMinecraft = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabMinecraftVersion;
        internal virtual global::System.Windows.Controls.TextBlock LabMinecraftVersion
        {
            get
            {
                return __LfPart_LabMinecraftVersion;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabMinecraftVersion = value;
            }
        }

        private global::System.Windows.Controls.Grid __LfPart_PanMinecraftCompare;
        internal virtual global::System.Windows.Controls.Grid PanMinecraftCompare
        {
            get
            {
                return __LfPart_PanMinecraftCompare;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanMinecraftCompare = value;
            }
        }

        private global::PCL.MyImage __LfPart_ImgMinecraftOld;
        internal virtual global::PCL.MyImage ImgMinecraftOld
        {
            get
            {
                return __LfPart_ImgMinecraftOld;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ImgMinecraftOld = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabMinecraftOld;
        internal virtual global::System.Windows.Controls.TextBlock LabMinecraftOld
        {
            get
            {
                return __LfPart_LabMinecraftOld;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabMinecraftOld = value;
            }
        }

        private global::PCL.MyImage __LfPart_ImgMinecraftNew;
        internal virtual global::PCL.MyImage ImgMinecraftNew
        {
            get
            {
                return __LfPart_ImgMinecraftNew;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ImgMinecraftNew = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabMinecraftNew;
        internal virtual global::System.Windows.Controls.TextBlock LabMinecraftNew
        {
            get
            {
                return __LfPart_LabMinecraftNew;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabMinecraftNew = value;
            }
        }

        private global::System.Windows.Controls.Grid __LfPart_PanMinecraftChange;
        internal virtual global::System.Windows.Controls.Grid PanMinecraftChange
        {
            get
            {
                return __LfPart_PanMinecraftChange;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanMinecraftChange = value;
            }
        }

        private global::PCL.MyIconButton __LfPart_BtnMinecraftRestore;
        internal virtual global::PCL.MyIconButton BtnMinecraftRestore
        {
            get
            {
                return __LfPart_BtnMinecraftRestore;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_BtnMinecraftRestore = value;
            }
        }

        private global::PCL.MyCard __LfPart_CardForge;
        internal virtual global::PCL.MyCard CardForge
        {
            get
            {
                return __LfPart_CardForge;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyCard.SwapEventHandler hook0 = new global::PCL.MyCard.SwapEventHandler(this.__LfHandle_5);
                global::PCL.MyCard.PreviewSwapEventHandler hook1 = new global::PCL.MyCard.PreviewSwapEventHandler(this.CardForge_PreviewSwap);
                global::PCL.MyCard previous = __LfPart_CardForge;
                if (previous != null)
                {
                    previous.Swap -= hook0;
                    previous.PreviewSwap -= hook1;
                }

                __LfPart_CardForge = value;
                global::PCL.MyCard current = __LfPart_CardForge;
                if (current != null)
                {
                    current.Swap += hook0;
                    current.PreviewSwap += hook1;
                }
            }
        }

        private global::System.Windows.Controls.StackPanel __LfPart_PanForge;
        internal virtual global::System.Windows.Controls.StackPanel PanForge
        {
            get
            {
                return __LfPart_PanForge;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanForge = value;
            }
        }

        private global::System.Windows.Controls.Grid __LfPart_PanForgeInfo;
        internal virtual global::System.Windows.Controls.Grid PanForgeInfo
        {
            get
            {
                return __LfPart_PanForgeInfo;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanForgeInfo = value;
            }
        }

        private global::PCL.MyImage __LfPart_ImgForge;
        internal virtual global::PCL.MyImage ImgForge
        {
            get
            {
                return __LfPart_ImgForge;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ImgForge = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabForge;
        internal virtual global::System.Windows.Controls.TextBlock LabForge
        {
            get
            {
                return __LfPart_LabForge;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabForge = value;
            }
        }

        private global::System.Windows.Controls.Grid __LfPart_PanForgeCompare;
        internal virtual global::System.Windows.Controls.Grid PanForgeCompare
        {
            get
            {
                return __LfPart_PanForgeCompare;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanForgeCompare = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabForgeOld;
        internal virtual global::System.Windows.Controls.TextBlock LabForgeOld
        {
            get
            {
                return __LfPart_LabForgeOld;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabForgeOld = value;
            }
        }

        private global::PCL.MyIconButton __LfPart_BtnForgeClear;
        internal virtual global::PCL.MyIconButton BtnForgeClear
        {
            get
            {
                return __LfPart_BtnForgeClear;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconButton.ClickEventHandler hook0 = new global::PCL.MyIconButton.ClickEventHandler(this.Forge_Clear);
                global::PCL.MyIconButton previous = __LfPart_BtnForgeClear;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnForgeClear = value;
                global::PCL.MyIconButton current = __LfPart_BtnForgeClear;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyIconButton __LfPart_BtnForgeRestore;
        internal virtual global::PCL.MyIconButton BtnForgeRestore
        {
            get
            {
                return __LfPart_BtnForgeRestore;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_BtnForgeRestore = value;
            }
        }

        private global::PCL.MyCard __LfPart_CardNeoForge;
        internal virtual global::PCL.MyCard CardNeoForge
        {
            get
            {
                return __LfPart_CardNeoForge;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyCard.SwapEventHandler hook0 = new global::PCL.MyCard.SwapEventHandler(this.__LfHandle_7);
                global::PCL.MyCard.PreviewSwapEventHandler hook1 = new global::PCL.MyCard.PreviewSwapEventHandler(this.CardNeoForge_PreviewSwap);
                global::PCL.MyCard previous = __LfPart_CardNeoForge;
                if (previous != null)
                {
                    previous.Swap -= hook0;
                    previous.PreviewSwap -= hook1;
                }

                __LfPart_CardNeoForge = value;
                global::PCL.MyCard current = __LfPart_CardNeoForge;
                if (current != null)
                {
                    current.Swap += hook0;
                    current.PreviewSwap += hook1;
                }
            }
        }

        private global::System.Windows.Controls.StackPanel __LfPart_PanNeoForge;
        internal virtual global::System.Windows.Controls.StackPanel PanNeoForge
        {
            get
            {
                return __LfPart_PanNeoForge;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanNeoForge = value;
            }
        }

        private global::System.Windows.Controls.Grid __LfPart_PanNeoForgeInfo;
        internal virtual global::System.Windows.Controls.Grid PanNeoForgeInfo
        {
            get
            {
                return __LfPart_PanNeoForgeInfo;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanNeoForgeInfo = value;
            }
        }

        private global::PCL.MyImage __LfPart_ImgNeoForge;
        internal virtual global::PCL.MyImage ImgNeoForge
        {
            get
            {
                return __LfPart_ImgNeoForge;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ImgNeoForge = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabNeoForge;
        internal virtual global::System.Windows.Controls.TextBlock LabNeoForge
        {
            get
            {
                return __LfPart_LabNeoForge;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabNeoForge = value;
            }
        }

        private global::System.Windows.Controls.Grid __LfPart_PanNeoForgeCompare;
        internal virtual global::System.Windows.Controls.Grid PanNeoForgeCompare
        {
            get
            {
                return __LfPart_PanNeoForgeCompare;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanNeoForgeCompare = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabNeoForgeOld;
        internal virtual global::System.Windows.Controls.TextBlock LabNeoForgeOld
        {
            get
            {
                return __LfPart_LabNeoForgeOld;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabNeoForgeOld = value;
            }
        }

        private global::PCL.MyIconButton __LfPart_BtnNeoForgeClear;
        internal virtual global::PCL.MyIconButton BtnNeoForgeClear
        {
            get
            {
                return __LfPart_BtnNeoForgeClear;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconButton.ClickEventHandler hook0 = new global::PCL.MyIconButton.ClickEventHandler(this.NeoForge_Clear);
                global::PCL.MyIconButton previous = __LfPart_BtnNeoForgeClear;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnNeoForgeClear = value;
                global::PCL.MyIconButton current = __LfPart_BtnNeoForgeClear;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyIconButton __LfPart_BtnNeoForgeRestore;
        internal virtual global::PCL.MyIconButton BtnNeoForgeRestore
        {
            get
            {
                return __LfPart_BtnNeoForgeRestore;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_BtnNeoForgeRestore = value;
            }
        }

        private global::PCL.MyCard __LfPart_CardFabric;
        internal virtual global::PCL.MyCard CardFabric
        {
            get
            {
                return __LfPart_CardFabric;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyCard.SwapEventHandler hook0 = new global::PCL.MyCard.SwapEventHandler(this.__LfHandle_9);
                global::PCL.MyCard.PreviewSwapEventHandler hook1 = new global::PCL.MyCard.PreviewSwapEventHandler(this.CardFabric_PreviewSwap);
                global::PCL.MyCard previous = __LfPart_CardFabric;
                if (previous != null)
                {
                    previous.Swap -= hook0;
                    previous.PreviewSwap -= hook1;
                }

                __LfPart_CardFabric = value;
                global::PCL.MyCard current = __LfPart_CardFabric;
                if (current != null)
                {
                    current.Swap += hook0;
                    current.PreviewSwap += hook1;
                }
            }
        }

        private global::System.Windows.Controls.StackPanel __LfPart_PanFabric;
        internal virtual global::System.Windows.Controls.StackPanel PanFabric
        {
            get
            {
                return __LfPart_PanFabric;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanFabric = value;
            }
        }

        private global::System.Windows.Controls.Grid __LfPart_PanFabricInfo;
        internal virtual global::System.Windows.Controls.Grid PanFabricInfo
        {
            get
            {
                return __LfPart_PanFabricInfo;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanFabricInfo = value;
            }
        }

        private global::PCL.MyImage __LfPart_ImgFabric;
        internal virtual global::PCL.MyImage ImgFabric
        {
            get
            {
                return __LfPart_ImgFabric;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ImgFabric = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabFabric;
        internal virtual global::System.Windows.Controls.TextBlock LabFabric
        {
            get
            {
                return __LfPart_LabFabric;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabFabric = value;
            }
        }

        private global::System.Windows.Controls.Grid __LfPart_PanFabricCompare;
        internal virtual global::System.Windows.Controls.Grid PanFabricCompare
        {
            get
            {
                return __LfPart_PanFabricCompare;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanFabricCompare = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabFabricOld;
        internal virtual global::System.Windows.Controls.TextBlock LabFabricOld
        {
            get
            {
                return __LfPart_LabFabricOld;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabFabricOld = value;
            }
        }

        private global::PCL.MyIconButton __LfPart_BtnFabricClear;
        internal virtual global::PCL.MyIconButton BtnFabricClear
        {
            get
            {
                return __LfPart_BtnFabricClear;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconButton.ClickEventHandler hook0 = new global::PCL.MyIconButton.ClickEventHandler(this.Fabric_Clear);
                global::PCL.MyIconButton previous = __LfPart_BtnFabricClear;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnFabricClear = value;
                global::PCL.MyIconButton current = __LfPart_BtnFabricClear;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyIconButton __LfPart_BtnFabricRestore;
        internal virtual global::PCL.MyIconButton BtnFabricRestore
        {
            get
            {
                return __LfPart_BtnFabricRestore;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_BtnFabricRestore = value;
            }
        }

        private global::PCL.MyCard __LfPart_CardFabricApi;
        internal virtual global::PCL.MyCard CardFabricApi
        {
            get
            {
                return __LfPart_CardFabricApi;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyCard.SwapEventHandler hook0 = new global::PCL.MyCard.SwapEventHandler(this.__LfHandle_11);
                global::PCL.MyCard.PreviewSwapEventHandler hook1 = new global::PCL.MyCard.PreviewSwapEventHandler(this.CardFabricApi_PreviewSwap);
                global::PCL.MyCard previous = __LfPart_CardFabricApi;
                if (previous != null)
                {
                    previous.Swap -= hook0;
                    previous.PreviewSwap -= hook1;
                }

                __LfPart_CardFabricApi = value;
                global::PCL.MyCard current = __LfPart_CardFabricApi;
                if (current != null)
                {
                    current.Swap += hook0;
                    current.PreviewSwap += hook1;
                }
            }
        }

        private global::System.Windows.Controls.StackPanel __LfPart_PanFabricApi;
        internal virtual global::System.Windows.Controls.StackPanel PanFabricApi
        {
            get
            {
                return __LfPart_PanFabricApi;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanFabricApi = value;
            }
        }

        private global::System.Windows.Controls.Grid __LfPart_PanFabricApiInfo;
        internal virtual global::System.Windows.Controls.Grid PanFabricApiInfo
        {
            get
            {
                return __LfPart_PanFabricApiInfo;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanFabricApiInfo = value;
            }
        }

        private global::PCL.MyImage __LfPart_ImgFabricApi;
        internal virtual global::PCL.MyImage ImgFabricApi
        {
            get
            {
                return __LfPart_ImgFabricApi;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ImgFabricApi = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabFabricApi;
        internal virtual global::System.Windows.Controls.TextBlock LabFabricApi
        {
            get
            {
                return __LfPart_LabFabricApi;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabFabricApi = value;
            }
        }

        private global::System.Windows.Controls.Grid __LfPart_PanFabricApiCompare;
        internal virtual global::System.Windows.Controls.Grid PanFabricApiCompare
        {
            get
            {
                return __LfPart_PanFabricApiCompare;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanFabricApiCompare = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabFabricApiOld;
        internal virtual global::System.Windows.Controls.TextBlock LabFabricApiOld
        {
            get
            {
                return __LfPart_LabFabricApiOld;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabFabricApiOld = value;
            }
        }

        private global::PCL.MyIconButton __LfPart_BtnFabricApiClear;
        internal virtual global::PCL.MyIconButton BtnFabricApiClear
        {
            get
            {
                return __LfPart_BtnFabricApiClear;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconButton.ClickEventHandler hook0 = new global::PCL.MyIconButton.ClickEventHandler(this.FabricApi_Clear);
                global::PCL.MyIconButton previous = __LfPart_BtnFabricApiClear;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnFabricApiClear = value;
                global::PCL.MyIconButton current = __LfPart_BtnFabricApiClear;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyIconButton __LfPart_BtnFabricApiRestore;
        internal virtual global::PCL.MyIconButton BtnFabricApiRestore
        {
            get
            {
                return __LfPart_BtnFabricApiRestore;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_BtnFabricApiRestore = value;
            }
        }

        private global::PCL.MyCard __LfPart_CardOptiFine;
        internal virtual global::PCL.MyCard CardOptiFine
        {
            get
            {
                return __LfPart_CardOptiFine;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyCard.SwapEventHandler hook0 = new global::PCL.MyCard.SwapEventHandler(this.__LfHandle_3);
                global::PCL.MyCard.PreviewSwapEventHandler hook1 = new global::PCL.MyCard.PreviewSwapEventHandler(this.CardOptiFine_PreviewSwap);
                global::PCL.MyCard previous = __LfPart_CardOptiFine;
                if (previous != null)
                {
                    previous.Swap -= hook0;
                    previous.PreviewSwap -= hook1;
                }

                __LfPart_CardOptiFine = value;
                global::PCL.MyCard current = __LfPart_CardOptiFine;
                if (current != null)
                {
                    current.Swap += hook0;
                    current.PreviewSwap += hook1;
                }
            }
        }

        private global::System.Windows.Controls.StackPanel __LfPart_PanOptiFine;
        internal virtual global::System.Windows.Controls.StackPanel PanOptiFine
        {
            get
            {
                return __LfPart_PanOptiFine;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanOptiFine = value;
            }
        }

        private global::System.Windows.Controls.Grid __LfPart_PanOptiFineInfo;
        internal virtual global::System.Windows.Controls.Grid PanOptiFineInfo
        {
            get
            {
                return __LfPart_PanOptiFineInfo;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanOptiFineInfo = value;
            }
        }

        private global::PCL.MyImage __LfPart_ImgOptiFine;
        internal virtual global::PCL.MyImage ImgOptiFine
        {
            get
            {
                return __LfPart_ImgOptiFine;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ImgOptiFine = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabOptiFine;
        internal virtual global::System.Windows.Controls.TextBlock LabOptiFine
        {
            get
            {
                return __LfPart_LabOptiFine;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabOptiFine = value;
            }
        }

        private global::System.Windows.Controls.Grid __LfPart_PanOptiFineCompare;
        internal virtual global::System.Windows.Controls.Grid PanOptiFineCompare
        {
            get
            {
                return __LfPart_PanOptiFineCompare;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanOptiFineCompare = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabOptiFineOld;
        internal virtual global::System.Windows.Controls.TextBlock LabOptiFineOld
        {
            get
            {
                return __LfPart_LabOptiFineOld;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabOptiFineOld = value;
            }
        }

        private global::PCL.MyIconButton __LfPart_BtnOptiFineClear;
        internal virtual global::PCL.MyIconButton BtnOptiFineClear
        {
            get
            {
                return __LfPart_BtnOptiFineClear;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconButton.ClickEventHandler hook0 = new global::PCL.MyIconButton.ClickEventHandler(this.OptiFine_Clear);
                global::PCL.MyIconButton previous = __LfPart_BtnOptiFineClear;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnOptiFineClear = value;
                global::PCL.MyIconButton current = __LfPart_BtnOptiFineClear;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyIconButton __LfPart_BtnOptiFineRestore;
        internal virtual global::PCL.MyIconButton BtnOptiFineRestore
        {
            get
            {
                return __LfPart_BtnOptiFineRestore;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_BtnOptiFineRestore = value;
            }
        }

        private global::PCL.MyCard __LfPart_CardOptiFabric;
        internal virtual global::PCL.MyCard CardOptiFabric
        {
            get
            {
                return __LfPart_CardOptiFabric;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyCard.SwapEventHandler hook0 = new global::PCL.MyCard.SwapEventHandler(this.__LfHandle_13);
                global::PCL.MyCard.PreviewSwapEventHandler hook1 = new global::PCL.MyCard.PreviewSwapEventHandler(this.CardOptiFabric_PreviewSwap);
                global::PCL.MyCard previous = __LfPart_CardOptiFabric;
                if (previous != null)
                {
                    previous.Swap -= hook0;
                    previous.PreviewSwap -= hook1;
                }

                __LfPart_CardOptiFabric = value;
                global::PCL.MyCard current = __LfPart_CardOptiFabric;
                if (current != null)
                {
                    current.Swap += hook0;
                    current.PreviewSwap += hook1;
                }
            }
        }

        private global::System.Windows.Controls.StackPanel __LfPart_PanOptiFabric;
        internal virtual global::System.Windows.Controls.StackPanel PanOptiFabric
        {
            get
            {
                return __LfPart_PanOptiFabric;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanOptiFabric = value;
            }
        }

        private global::System.Windows.Controls.Grid __LfPart_PanOptiFabricInfo;
        internal virtual global::System.Windows.Controls.Grid PanOptiFabricInfo
        {
            get
            {
                return __LfPart_PanOptiFabricInfo;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanOptiFabricInfo = value;
            }
        }

        private global::PCL.MyImage __LfPart_ImgOptiFabric;
        internal virtual global::PCL.MyImage ImgOptiFabric
        {
            get
            {
                return __LfPart_ImgOptiFabric;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ImgOptiFabric = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabOptiFabric;
        internal virtual global::System.Windows.Controls.TextBlock LabOptiFabric
        {
            get
            {
                return __LfPart_LabOptiFabric;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabOptiFabric = value;
            }
        }

        private global::System.Windows.Controls.Grid __LfPart_PanOptiFabricCompare;
        internal virtual global::System.Windows.Controls.Grid PanOptiFabricCompare
        {
            get
            {
                return __LfPart_PanOptiFabricCompare;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanOptiFabricCompare = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabOptiFabricOld;
        internal virtual global::System.Windows.Controls.TextBlock LabOptiFabricOld
        {
            get
            {
                return __LfPart_LabOptiFabricOld;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabOptiFabricOld = value;
            }
        }

        private global::PCL.MyIconButton __LfPart_BtnOptiFabricClear;
        internal virtual global::PCL.MyIconButton BtnOptiFabricClear
        {
            get
            {
                return __LfPart_BtnOptiFabricClear;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconButton.ClickEventHandler hook0 = new global::PCL.MyIconButton.ClickEventHandler(this.OptiFabric_Clear);
                global::PCL.MyIconButton previous = __LfPart_BtnOptiFabricClear;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnOptiFabricClear = value;
                global::PCL.MyIconButton current = __LfPart_BtnOptiFabricClear;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyIconButton __LfPart_BtnOptiFabricRestore;
        internal virtual global::PCL.MyIconButton BtnOptiFabricRestore
        {
            get
            {
                return __LfPart_BtnOptiFabricRestore;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_BtnOptiFabricRestore = value;
            }
        }

        private global::PCL.MyCard __LfPart_CardLiteLoader;
        internal virtual global::PCL.MyCard CardLiteLoader
        {
            get
            {
                return __LfPart_CardLiteLoader;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyCard.SwapEventHandler hook0 = new global::PCL.MyCard.SwapEventHandler(this.__LfHandle_15);
                global::PCL.MyCard.PreviewSwapEventHandler hook1 = new global::PCL.MyCard.PreviewSwapEventHandler(this.CardLiteLoader_PreviewSwap);
                global::PCL.MyCard previous = __LfPart_CardLiteLoader;
                if (previous != null)
                {
                    previous.Swap -= hook0;
                    previous.PreviewSwap -= hook1;
                }

                __LfPart_CardLiteLoader = value;
                global::PCL.MyCard current = __LfPart_CardLiteLoader;
                if (current != null)
                {
                    current.Swap += hook0;
                    current.PreviewSwap += hook1;
                }
            }
        }

        private global::System.Windows.Controls.StackPanel __LfPart_PanLiteLoader;
        internal virtual global::System.Windows.Controls.StackPanel PanLiteLoader
        {
            get
            {
                return __LfPart_PanLiteLoader;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanLiteLoader = value;
            }
        }

        private global::System.Windows.Controls.Grid __LfPart_PanLiteLoaderInfo;
        internal virtual global::System.Windows.Controls.Grid PanLiteLoaderInfo
        {
            get
            {
                return __LfPart_PanLiteLoaderInfo;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanLiteLoaderInfo = value;
            }
        }

        private global::PCL.MyImage __LfPart_ImgLiteLoader;
        internal virtual global::PCL.MyImage ImgLiteLoader
        {
            get
            {
                return __LfPart_ImgLiteLoader;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ImgLiteLoader = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabLiteLoader;
        internal virtual global::System.Windows.Controls.TextBlock LabLiteLoader
        {
            get
            {
                return __LfPart_LabLiteLoader;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabLiteLoader = value;
            }
        }

        private global::System.Windows.Controls.Grid __LfPart_PanLiteLoaderCompare;
        internal virtual global::System.Windows.Controls.Grid PanLiteLoaderCompare
        {
            get
            {
                return __LfPart_PanLiteLoaderCompare;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanLiteLoaderCompare = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabLiteLoaderOld;
        internal virtual global::System.Windows.Controls.TextBlock LabLiteLoaderOld
        {
            get
            {
                return __LfPart_LabLiteLoaderOld;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabLiteLoaderOld = value;
            }
        }

        private global::PCL.MyIconButton __LfPart_BtnLiteLoaderClear;
        internal virtual global::PCL.MyIconButton BtnLiteLoaderClear
        {
            get
            {
                return __LfPart_BtnLiteLoaderClear;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyIconButton.ClickEventHandler hook0 = new global::PCL.MyIconButton.ClickEventHandler(this.LiteLoader_Clear);
                global::PCL.MyIconButton previous = __LfPart_BtnLiteLoaderClear;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnLiteLoaderClear = value;
                global::PCL.MyIconButton current = __LfPart_BtnLiteLoaderClear;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyIconButton __LfPart_BtnLiteLoaderRestore;
        internal virtual global::PCL.MyIconButton BtnLiteLoaderRestore
        {
            get
            {
                return __LfPart_BtnLiteLoaderRestore;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_BtnLiteLoaderRestore = value;
            }
        }

        private global::PCL.MyExtraTextButton __LfPart_BtnStart;
        internal virtual global::PCL.MyExtraTextButton BtnStart
        {
            get
            {
                return __LfPart_BtnStart;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyExtraTextButton.ClickEventHandler hook0 = new global::PCL.MyExtraTextButton.ClickEventHandler(this.__LfHandle_24);
                global::PCL.MyExtraTextButton previous = __LfPart_BtnStart;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnStart = value;
                global::PCL.MyExtraTextButton current = __LfPart_BtnStart;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private global::PCL.MyCard __LfPart_PanLoad;
        internal virtual global::PCL.MyCard PanLoad
        {
            get
            {
                return __LfPart_PanLoad;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanLoad = value;
            }
        }

        private global::PCL.MyLoading __LfPart_LoadMinecraft;
        internal virtual global::PCL.MyLoading LoadMinecraft
        {
            get
            {
                return __LfPart_LoadMinecraft;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LoadMinecraft = value;
            }
        }

        private global::PCL.MyLoading __LfPart_LoadOptiFine;
        internal virtual global::PCL.MyLoading LoadOptiFine
        {
            get
            {
                return __LfPart_LoadOptiFine;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyLoading.StateChangedEventHandler hook0 = new global::PCL.MyLoading.StateChangedEventHandler(this.__LfHandle_4);
                global::PCL.MyLoading.StateChangedEventHandler hook1 = new global::PCL.MyLoading.StateChangedEventHandler(this.__LfHandle_17);
                global::PCL.MyLoading previous = __LfPart_LoadOptiFine;
                if (previous != null)
                {
                    previous.StateChanged -= hook0;
                    previous.StateChanged -= hook1;
                }

                __LfPart_LoadOptiFine = value;
                global::PCL.MyLoading current = __LfPart_LoadOptiFine;
                if (current != null)
                {
                    current.StateChanged += hook0;
                    current.StateChanged += hook1;
                }
            }
        }

        private global::PCL.MyLoading __LfPart_LoadForge;
        internal virtual global::PCL.MyLoading LoadForge
        {
            get
            {
                return __LfPart_LoadForge;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyLoading.StateChangedEventHandler hook0 = new global::PCL.MyLoading.StateChangedEventHandler(this.__LfHandle_6);
                global::PCL.MyLoading.StateChangedEventHandler hook1 = new global::PCL.MyLoading.StateChangedEventHandler(this.__LfHandle_19);
                global::PCL.MyLoading previous = __LfPart_LoadForge;
                if (previous != null)
                {
                    previous.StateChanged -= hook0;
                    previous.StateChanged -= hook1;
                }

                __LfPart_LoadForge = value;
                global::PCL.MyLoading current = __LfPart_LoadForge;
                if (current != null)
                {
                    current.StateChanged += hook0;
                    current.StateChanged += hook1;
                }
            }
        }

        private global::PCL.MyLoading __LfPart_LoadNeoForge;
        internal virtual global::PCL.MyLoading LoadNeoForge
        {
            get
            {
                return __LfPart_LoadNeoForge;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyLoading.StateChangedEventHandler hook0 = new global::PCL.MyLoading.StateChangedEventHandler(this.__LfHandle_8);
                global::PCL.MyLoading.StateChangedEventHandler hook1 = new global::PCL.MyLoading.StateChangedEventHandler(this.__LfHandle_20);
                global::PCL.MyLoading previous = __LfPart_LoadNeoForge;
                if (previous != null)
                {
                    previous.StateChanged -= hook0;
                    previous.StateChanged -= hook1;
                }

                __LfPart_LoadNeoForge = value;
                global::PCL.MyLoading current = __LfPart_LoadNeoForge;
                if (current != null)
                {
                    current.StateChanged += hook0;
                    current.StateChanged += hook1;
                }
            }
        }

        private global::PCL.MyLoading __LfPart_LoadLiteLoader;
        internal virtual global::PCL.MyLoading LoadLiteLoader
        {
            get
            {
                return __LfPart_LoadLiteLoader;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyLoading.StateChangedEventHandler hook0 = new global::PCL.MyLoading.StateChangedEventHandler(this.__LfHandle_16);
                global::PCL.MyLoading.StateChangedEventHandler hook1 = new global::PCL.MyLoading.StateChangedEventHandler(this.__LfHandle_18);
                global::PCL.MyLoading previous = __LfPart_LoadLiteLoader;
                if (previous != null)
                {
                    previous.StateChanged -= hook0;
                    previous.StateChanged -= hook1;
                }

                __LfPart_LoadLiteLoader = value;
                global::PCL.MyLoading current = __LfPart_LoadLiteLoader;
                if (current != null)
                {
                    current.StateChanged += hook0;
                    current.StateChanged += hook1;
                }
            }
        }

        private global::PCL.MyLoading __LfPart_LoadFabric;
        internal virtual global::PCL.MyLoading LoadFabric
        {
            get
            {
                return __LfPart_LoadFabric;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyLoading.StateChangedEventHandler hook0 = new global::PCL.MyLoading.StateChangedEventHandler(this.__LfHandle_10);
                global::PCL.MyLoading.StateChangedEventHandler hook1 = new global::PCL.MyLoading.StateChangedEventHandler(this.__LfHandle_21);
                global::PCL.MyLoading previous = __LfPart_LoadFabric;
                if (previous != null)
                {
                    previous.StateChanged -= hook0;
                    previous.StateChanged -= hook1;
                }

                __LfPart_LoadFabric = value;
                global::PCL.MyLoading current = __LfPart_LoadFabric;
                if (current != null)
                {
                    current.StateChanged += hook0;
                    current.StateChanged += hook1;
                }
            }
        }

        private global::PCL.MyLoading __LfPart_LoadFabricApi;
        internal virtual global::PCL.MyLoading LoadFabricApi
        {
            get
            {
                return __LfPart_LoadFabricApi;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyLoading.StateChangedEventHandler hook0 = new global::PCL.MyLoading.StateChangedEventHandler(this.__LfHandle_12);
                global::PCL.MyLoading.StateChangedEventHandler hook1 = new global::PCL.MyLoading.StateChangedEventHandler(this.__LfHandle_22);
                global::PCL.MyLoading previous = __LfPart_LoadFabricApi;
                if (previous != null)
                {
                    previous.StateChanged -= hook0;
                    previous.StateChanged -= hook1;
                }

                __LfPart_LoadFabricApi = value;
                global::PCL.MyLoading current = __LfPart_LoadFabricApi;
                if (current != null)
                {
                    current.StateChanged += hook0;
                    current.StateChanged += hook1;
                }
            }
        }

        private global::PCL.MyLoading __LfPart_LoadOptiFabric;
        internal virtual global::PCL.MyLoading LoadOptiFabric
        {
            get
            {
                return __LfPart_LoadOptiFabric;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyLoading.StateChangedEventHandler hook0 = new global::PCL.MyLoading.StateChangedEventHandler(this.__LfHandle_14);
                global::PCL.MyLoading.StateChangedEventHandler hook1 = new global::PCL.MyLoading.StateChangedEventHandler(this.__LfHandle_23);
                global::PCL.MyLoading previous = __LfPart_LoadOptiFabric;
                if (previous != null)
                {
                    previous.StateChanged -= hook0;
                    previous.StateChanged -= hook1;
                }

                __LfPart_LoadOptiFabric = value;
                global::PCL.MyLoading current = __LfPart_LoadOptiFabric;
                if (current != null)
                {
                    current.StateChanged += hook0;
                    current.StateChanged += hook1;
                }
            }
        }

        private void __LfHandle_0(global::System.Object arg0, global::System.EventArgs arg1)
        {
            this.LoaderInit();
        }

        private void __LfHandle_1(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.Init();
        }

        private void __LfHandle_2(global::System.Object arg0, global::System.EventArgs arg1)
        {
            this.ExitSelectPage();
        }

        private void __LfHandle_3(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.ReloadSelected();
        }

        private void __LfHandle_4(global::System.Object arg0, global::PCL.MyLoading.MyLoadingState arg1, global::PCL.MyLoading.MyLoadingState arg2)
        {
            this.ReloadSelected();
        }

        private void __LfHandle_5(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.ReloadSelected();
        }

        private void __LfHandle_6(global::System.Object arg0, global::PCL.MyLoading.MyLoadingState arg1, global::PCL.MyLoading.MyLoadingState arg2)
        {
            this.ReloadSelected();
        }

        private void __LfHandle_7(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.ReloadSelected();
        }

        private void __LfHandle_8(global::System.Object arg0, global::PCL.MyLoading.MyLoadingState arg1, global::PCL.MyLoading.MyLoadingState arg2)
        {
            this.ReloadSelected();
        }

        private void __LfHandle_9(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.ReloadSelected();
        }

        private void __LfHandle_10(global::System.Object arg0, global::PCL.MyLoading.MyLoadingState arg1, global::PCL.MyLoading.MyLoadingState arg2)
        {
            this.ReloadSelected();
        }

        private void __LfHandle_11(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.ReloadSelected();
        }

        private void __LfHandle_12(global::System.Object arg0, global::PCL.MyLoading.MyLoadingState arg1, global::PCL.MyLoading.MyLoadingState arg2)
        {
            this.ReloadSelected();
        }

        private void __LfHandle_13(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.ReloadSelected();
        }

        private void __LfHandle_14(global::System.Object arg0, global::PCL.MyLoading.MyLoadingState arg1, global::PCL.MyLoading.MyLoadingState arg2)
        {
            this.ReloadSelected();
        }

        private void __LfHandle_15(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.ReloadSelected();
        }

        private void __LfHandle_16(global::System.Object arg0, global::PCL.MyLoading.MyLoadingState arg1, global::PCL.MyLoading.MyLoadingState arg2)
        {
            this.ReloadSelected();
        }

        private void __LfHandle_17(global::System.Object arg0, global::PCL.MyLoading.MyLoadingState arg1, global::PCL.MyLoading.MyLoadingState arg2)
        {
            this.OptiFine_Loaded();
        }

        private void __LfHandle_18(global::System.Object arg0, global::PCL.MyLoading.MyLoadingState arg1, global::PCL.MyLoading.MyLoadingState arg2)
        {
            this.LiteLoader_Loaded();
        }

        private void __LfHandle_19(global::System.Object arg0, global::PCL.MyLoading.MyLoadingState arg1, global::PCL.MyLoading.MyLoadingState arg2)
        {
            this.Forge_Loaded();
        }

        private void __LfHandle_20(global::System.Object arg0, global::PCL.MyLoading.MyLoadingState arg1, global::PCL.MyLoading.MyLoadingState arg2)
        {
            this.NeoForge_Loaded();
        }

        private void __LfHandle_21(global::System.Object arg0, global::PCL.MyLoading.MyLoadingState arg1, global::PCL.MyLoading.MyLoadingState arg2)
        {
            this.Fabric_Loaded();
        }

        private void __LfHandle_22(global::System.Object arg0, global::PCL.MyLoading.MyLoadingState arg1, global::PCL.MyLoading.MyLoadingState arg2)
        {
            this.FabricApi_Loaded();
        }

        private void __LfHandle_23(global::System.Object arg0, global::PCL.MyLoading.MyLoadingState arg1, global::PCL.MyLoading.MyLoadingState arg2)
        {
            this.OptiFabric_Loaded();
        }

        private void __LfHandle_24(global::System.Object arg0, global::System.Windows.Input.MouseButtonEventArgs arg1)
        {
            this.BtnStart_Click();
        }

        private void __LfAttachOwnHandles()
        {
            this.Initialized += new global::System.EventHandler(this.__LfHandle_0);
            this.Loaded += new global::System.Windows.RoutedEventHandler(this.__LfHandle_1);
        }
    }
}
