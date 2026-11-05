using PCLLF.Presentation.Controls;

namespace PCL
{

    public class MyComboBoxItem : MyComboBoxItemView
    {
        static MyComboBoxItem()
        {
            PresentationRuntime.EnsureConfigured();
        }
        public override string ToString()
        {
            return base.ToString();
        }
        public static implicit operator string(MyComboBoxItem Value)
        {
            return Value.Content.ToString();
        }
    }
}