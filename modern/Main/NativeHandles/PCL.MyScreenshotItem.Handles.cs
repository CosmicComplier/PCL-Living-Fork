// LF Handles/WithEvents, original Roslyn symbols at 6a8bfc6; LF LICENCE. No runtime lookup or generated WPF output mutation.
namespace PCL
{
    partial class MyScreenshotItem
    {
        private global::System.Windows.Controls.Image __LfPart_Thumbnail;
        internal virtual global::System.Windows.Controls.Image Thumbnail
        {
            get
            {
                return __LfPart_Thumbnail;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_Thumbnail = value;
            }
        }

        private global::System.Windows.Controls.TextBlock __LfPart_LabName;
        internal virtual global::System.Windows.Controls.TextBlock LabName
        {
            get
            {
                return __LfPart_LabName;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_LabName = value;
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

        private global::System.Windows.Controls.Border __LfPart_SelectionBorder;
        internal virtual global::System.Windows.Controls.Border SelectionBorder
        {
            get
            {
                return __LfPart_SelectionBorder;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                __LfPart_SelectionBorder = value;
            }
        }

        private global::PCL.MyCheckBox __LfPart_Check;
        internal virtual global::PCL.MyCheckBox Check
        {
            get
            {
                return __LfPart_Check;
            }

            [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            set
            {
                global::PCL.MyCheckBox.ChangeEventHandler hook0 = new global::PCL.MyCheckBox.ChangeEventHandler(this.__LfHandle_0);
                global::PCL.MyCheckBox previous = __LfPart_Check;
                if (previous != null)
                {
                    previous.Change -= hook0;
                }

                __LfPart_Check = value;
                global::PCL.MyCheckBox current = __LfPart_Check;
                if (current != null)
                {
                    current.Change += hook0;
                }
            }
        }

        private void __LfHandle_0(global::System.Object arg0, global::System.Boolean arg1)
        {
            this.CheckChanged();
        }
    }
}
