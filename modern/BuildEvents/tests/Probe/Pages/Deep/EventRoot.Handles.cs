namespace Probe { public partial class EventRoot {
    private LocalButton value;
    internal virtual LocalButton Fire {
        get { return value; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
        set { System.Windows.RoutedEventHandler hook = Click; if (this.value != null) this.value.Click -= hook; this.value = value; if (this.value != null) this.value.Click += hook; }
    }
} }
