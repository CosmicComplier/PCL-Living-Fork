// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class MyComboBox
    {
        private void __LfHandle_0(global::System.Object arg0, global::System.Windows.Controls.SelectionChangedEventArgs arg1)
        {
            this.SaveSetting();
        }

        private void __LfAttachOwnHandles()
        {
            this.SelectionChanged += new global::System.Windows.Controls.SelectionChangedEventHandler(this.__LfHandle_0);
        }
    }
}
