// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class MyRadioBox
    {
        private void __LfHandle_0(global::System.Object arg0, global::PCL.ModBase.RouteEventArgs arg1)
        {
            this.SaveSetting();
        }

        private void __LfAttachOwnHandles()
        {
            this.Check += new global::PCL.IMyRadio.CheckEventHandler(this.__LfHandle_0);
        }
    }
}
