// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class MyTextBox
    {
        private void __LfHandle_0(global::System.Object arg0, global::System.Windows.RoutedEventArgs arg1)
        {
            this.SaveSetting();
        }

        private void __LfAttachOwnHandles()
        {
            this.ValidatedTextChanged += new global::System.Windows.RoutedEventHandler(this.__LfHandle_0);
        }
    }
}
