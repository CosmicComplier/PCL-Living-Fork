namespace Probe {
    public class LocalButton : System.Windows.Controls.Button { }
    public partial class EventRoot : System.Windows.Window {
        public EventRoot() { InitializeComponent(); }
        public int Count;
        private void Click(object sender, System.Windows.RoutedEventArgs args) { Count++; }
    }
}
