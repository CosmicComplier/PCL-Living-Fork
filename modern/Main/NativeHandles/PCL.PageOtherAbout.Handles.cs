// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class PageOtherAbout
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

        private global::PCL.MyListItem __LfPart_ItemAboutPcl;
        internal virtual global::PCL.MyListItem ItemAboutPcl
        {
            get
            {
                return __LfPart_ItemAboutPcl;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_ItemAboutPcl = value;
            }
        }

        private global::PCL.MyButton __LfPart_BtnAboutUpdate;
        internal virtual global::PCL.MyButton BtnAboutUpdate
        {
            get
            {
                return __LfPart_BtnAboutUpdate;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyButton.ClickEventHandler hook0 = new global::PCL.MyButton.ClickEventHandler(this.BtnAboutUpdate_Click);
                global::PCL.MyButton previous = __LfPart_BtnAboutUpdate;
                if (previous != null)
                {
                    previous.Click -= hook0;
                }

                __LfPart_BtnAboutUpdate = value;
                global::PCL.MyButton current = __LfPart_BtnAboutUpdate;
                if (current != null)
                {
                    current.Click += hook0;
                }
            }
        }

        private void __LfAttachOwnHandles()
        {
            this.Loaded += new global::System.Windows.RoutedEventHandler(this.PageOtherAbout_Loaded);
        }
    }
}
