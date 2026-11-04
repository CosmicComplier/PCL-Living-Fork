// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class PageSelectLeft
    {
        private global::System.Windows.Controls.StackPanel __LfPart_PanList;
        internal virtual global::System.Windows.Controls.StackPanel PanList
        {
            get
            {
                return __LfPart_PanList;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_PanList = value;
            }
        }

        private void __LfAttachOwnHandles()
        {
            this.Initialized += new global::System.EventHandler(this.PageSelectLeft_Initialized);
            this.Loaded += new global::System.Windows.RoutedEventHandler(this.PageSelectLeft_Loaded);
        }
    }
}
