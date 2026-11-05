// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class MyCheckBox
    {
        private void __LfHandle_0(global::System.Object arg0, global::System.Boolean arg1)
        {
            this.SaveSetting();
        }

        private void __LfAttachOwnHandles()
        {
            this.Change += new global::PCL.MyCheckBox.ChangeEventHandler(this.__LfHandle_0);
        }
    }
}
