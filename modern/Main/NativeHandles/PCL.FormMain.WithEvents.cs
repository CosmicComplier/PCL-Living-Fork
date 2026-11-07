// LF FormMain original typed WithEvents reset semantics. Body and Me lifecycle remain separately owned.
namespace PCL { partial class FormMain {
private global::PCL.MyIconButton __lfMain_BtnTitleClose;
internal virtual global::PCL.MyIconButton BtnTitleClose { get {return __lfMain_BtnTitleClose;} [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)] set {
global::PCL.MyIconButton.ClickEventHandler hook0 = new global::PCL.MyIconButton.ClickEventHandler(this.__LfMainClose);
global::PCL.MyIconButton previous=__lfMain_BtnTitleClose; if(previous!=null) {
previous.Click -= hook0;
}
__lfMain_BtnTitleClose=value;
global::PCL.MyIconButton current=__lfMain_BtnTitleClose; if(current!=null) {
current.Click += hook0;
}
} }
private global::System.Windows.Controls.Grid __lfMain_PanTitle;
internal virtual global::System.Windows.Controls.Grid PanTitle { get {return __lfMain_PanTitle;} [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)] set {
global::System.Windows.Input.MouseButtonEventHandler hook0 = new global::System.Windows.Input.MouseButtonEventHandler(this.FormDragMove);
global::System.Windows.Controls.Grid previous=__lfMain_PanTitle; if(previous!=null) {
previous.MouseLeftButtonDown -= hook0;
}
__lfMain_PanTitle=value;
global::System.Windows.Controls.Grid current=__lfMain_PanTitle; if(current!=null) {
current.MouseLeftButtonDown += hook0;
}
} }
private global::System.Windows.Controls.Grid __lfMain_PanMsg;
internal virtual global::System.Windows.Controls.Grid PanMsg { get {return __lfMain_PanMsg;} [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)] set {
global::System.Windows.Input.MouseButtonEventHandler hook0 = new global::System.Windows.Input.MouseButtonEventHandler(this.FormDragMove);
global::System.Windows.Controls.Grid previous=__lfMain_PanMsg; if(previous!=null) {
previous.MouseLeftButtonDown -= hook0;
}
__lfMain_PanMsg=value;
global::System.Windows.Controls.Grid current=__lfMain_PanMsg; if(current!=null) {
current.MouseLeftButtonDown += hook0;
}
} }
private global::PCL.MyIconButton __lfMain_BtnTitleMin;
internal virtual global::PCL.MyIconButton BtnTitleMin { get {return __lfMain_BtnTitleMin;} [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)] set {
global::PCL.MyIconButton.ClickEventHandler hook0 = new global::PCL.MyIconButton.ClickEventHandler(this.__LfMainMinimize);
global::PCL.MyIconButton previous=__lfMain_BtnTitleMin; if(previous!=null) {
previous.Click -= hook0;
}
__lfMain_BtnTitleMin=value;
global::PCL.MyIconButton current=__lfMain_BtnTitleMin; if(current!=null) {
current.Click += hook0;
}
} }
private global::PCL.MyRadioButton __lfMain_BtnTitleSelect5;
internal virtual global::PCL.MyRadioButton BtnTitleSelect5 { get {return __lfMain_BtnTitleSelect5;} [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)] set {
global::PCL.MyRadioButton.CheckEventHandler hook0 = new global::PCL.MyRadioButton.CheckEventHandler(this.BtnTitleSelect_CheckEvent);
global::PCL.MyRadioButton previous=__lfMain_BtnTitleSelect5; if(previous!=null) previous.Check -= hook0;
__lfMain_BtnTitleSelect5=value;
global::PCL.MyRadioButton current=__lfMain_BtnTitleSelect5; if(current!=null) current.Check += hook0;
} }
private global::PCL.MyRadioButton __lfMain_BtnTitleSelect0;
internal virtual global::PCL.MyRadioButton BtnTitleSelect0 { get {return __lfMain_BtnTitleSelect0;} [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)] set {
global::PCL.MyRadioButton.CheckEventHandler hook0 = new global::PCL.MyRadioButton.CheckEventHandler(this.BtnTitleSelect_CheckEvent);
global::PCL.MyRadioButton previous=__lfMain_BtnTitleSelect0; if(previous!=null) {
previous.Check -= hook0;
}
__lfMain_BtnTitleSelect0=value;
global::PCL.MyRadioButton current=__lfMain_BtnTitleSelect0; if(current!=null) {
current.Check += hook0;
}
} }
private global::PCL.MyRadioButton __lfMain_BtnTitleSelect1;
internal virtual global::PCL.MyRadioButton BtnTitleSelect1 { get {return __lfMain_BtnTitleSelect1;} [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)] set {
global::PCL.MyRadioButton.CheckEventHandler hook0 = new global::PCL.MyRadioButton.CheckEventHandler(this.BtnTitleSelect_CheckEvent);
global::PCL.MyRadioButton previous=__lfMain_BtnTitleSelect1; if(previous!=null) {
previous.Check -= hook0;
}
__lfMain_BtnTitleSelect1=value;
global::PCL.MyRadioButton current=__lfMain_BtnTitleSelect1; if(current!=null) {
current.Check += hook0;
}
} }
private global::PCL.MyRadioButton __lfMain_BtnTitleSelect2;
internal virtual global::PCL.MyRadioButton BtnTitleSelect2 { get {return __lfMain_BtnTitleSelect2;} [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)] set {
global::PCL.MyRadioButton.CheckEventHandler hook0 = new global::PCL.MyRadioButton.CheckEventHandler(this.BtnTitleSelect_CheckEvent);
global::PCL.MyRadioButton previous=__lfMain_BtnTitleSelect2; if(previous!=null) {
previous.Check -= hook0;
}
__lfMain_BtnTitleSelect2=value;
global::PCL.MyRadioButton current=__lfMain_BtnTitleSelect2; if(current!=null) {
current.Check += hook0;
}
} }
private global::PCL.MyRadioButton __lfMain_BtnTitleSelect3;
internal virtual global::PCL.MyRadioButton BtnTitleSelect3 { get {return __lfMain_BtnTitleSelect3;} [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)] set {
global::PCL.MyRadioButton.CheckEventHandler hook0 = new global::PCL.MyRadioButton.CheckEventHandler(this.BtnTitleSelect_CheckEvent);
global::PCL.MyRadioButton previous=__lfMain_BtnTitleSelect3; if(previous!=null) {
previous.Check -= hook0;
}
__lfMain_BtnTitleSelect3=value;
global::PCL.MyRadioButton current=__lfMain_BtnTitleSelect3; if(current!=null) {
current.Check += hook0;
}
} }
private global::PCL.MyRadioButton __lfMain_BtnTitleSelect4;
internal virtual global::PCL.MyRadioButton BtnTitleSelect4 { get {return __lfMain_BtnTitleSelect4;} [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)] set {
global::PCL.MyRadioButton.CheckEventHandler hook0 = new global::PCL.MyRadioButton.CheckEventHandler(this.BtnTitleSelect_CheckEvent);
global::PCL.MyRadioButton previous=__lfMain_BtnTitleSelect4; if(previous!=null) {
previous.Check -= hook0;
}
__lfMain_BtnTitleSelect4=value;
global::PCL.MyRadioButton current=__lfMain_BtnTitleSelect4; if(current!=null) {
current.Check += hook0;
}
} }
private global::PCL.MyIconButton __lfMain_BtnTitleInner;
internal virtual global::PCL.MyIconButton BtnTitleInner { get {return __lfMain_BtnTitleInner;} [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)] set {
global::PCL.MyIconButton.ClickEventHandler hook0 = new global::PCL.MyIconButton.ClickEventHandler(this.__LfMainBack);
global::PCL.MyIconButton previous=__lfMain_BtnTitleInner; if(previous!=null) {
previous.Click -= hook0;
}
__lfMain_BtnTitleInner=value;
global::PCL.MyIconButton current=__lfMain_BtnTitleInner; if(current!=null) {
current.Click += hook0;
}
} }
private global::System.Windows.Controls.Border __lfMain_PanMainLeft;
internal virtual global::System.Windows.Controls.Border PanMainLeft { get {return __lfMain_PanMainLeft;} [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)] set {
global::System.Windows.SizeChangedEventHandler hook0 = new global::System.Windows.SizeChangedEventHandler(this.PanMainLeft_SizeChanged);
global::System.Windows.Controls.Border previous=__lfMain_PanMainLeft; if(previous!=null) {
previous.SizeChanged -= hook0;
}
__lfMain_PanMainLeft=value;
global::System.Windows.Controls.Border current=__lfMain_PanMainLeft; if(current!=null) {
current.SizeChanged += hook0;
}
} }
private global::System.Windows.Controls.Grid __lfMain_PanBack;
internal virtual global::System.Windows.Controls.Grid PanBack { get {return __lfMain_PanBack;} [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)] set {
global::System.Windows.Input.MouseEventHandler hook0 = new global::System.Windows.Input.MouseEventHandler(this.PanBack_MouseMoveEvent);
global::System.Windows.Controls.Grid previous=__lfMain_PanBack; if(previous!=null) {
previous.MouseMove -= hook0;
}
__lfMain_PanBack=value;
global::System.Windows.Controls.Grid current=__lfMain_PanBack; if(current!=null) {
current.MouseMove += hook0;
}
} }
private global::PCL.MyExtraButton __lfMain_BtnExtraMusic;
internal virtual global::PCL.MyExtraButton BtnExtraMusic { get {return __lfMain_BtnExtraMusic;} [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)] set {
global::PCL.MyExtraButton.ClickEventHandler hook0 = new global::PCL.MyExtraButton.ClickEventHandler(this.BtnExtraMusic_Click);
global::PCL.MyExtraButton.RightClickEventHandler hook1 = new global::PCL.MyExtraButton.RightClickEventHandler(this.BtnExtraMusic_RightClick);
global::PCL.MyExtraButton previous=__lfMain_BtnExtraMusic; if(previous!=null) {
previous.Click -= hook0;
previous.RightClick -= hook1;
}
__lfMain_BtnExtraMusic=value;
global::PCL.MyExtraButton current=__lfMain_BtnExtraMusic; if(current!=null) {
current.Click += hook0;
current.RightClick += hook1;
}
} }
private global::PCL.MyExtraButton __lfMain_BtnExtraDownload;
internal virtual global::PCL.MyExtraButton BtnExtraDownload { get {return __lfMain_BtnExtraDownload;} [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)] set {
global::PCL.MyExtraButton.ClickEventHandler hook0 = new global::PCL.MyExtraButton.ClickEventHandler(this.BtnExtraDownload_Click);
global::PCL.MyExtraButton previous=__lfMain_BtnExtraDownload; if(previous!=null) {
previous.Click -= hook0;
}
__lfMain_BtnExtraDownload=value;
global::PCL.MyExtraButton current=__lfMain_BtnExtraDownload; if(current!=null) {
current.Click += hook0;
}
} }
private global::PCL.MyExtraButton __lfMain_BtnExtraApril;
internal virtual global::PCL.MyExtraButton BtnExtraApril { get {return __lfMain_BtnExtraApril;} [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)] set {
global::PCL.MyExtraButton.ClickEventHandler hook0 = new global::PCL.MyExtraButton.ClickEventHandler(this.AprilGiveup_ClickEvent);
global::PCL.MyExtraButton previous=__lfMain_BtnExtraApril; if(previous!=null) {
previous.Click -= hook0;
}
__lfMain_BtnExtraApril=value;
global::PCL.MyExtraButton current=__lfMain_BtnExtraApril; if(current!=null) {
current.Click += hook0;
}
} }
private global::PCL.MyExtraButton __lfMain_BtnExtraShutdown;
internal virtual global::PCL.MyExtraButton BtnExtraShutdown { get {return __lfMain_BtnExtraShutdown;} [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)] set {
global::PCL.MyExtraButton.ClickEventHandler hook0 = new global::PCL.MyExtraButton.ClickEventHandler(this.BtnExtraShutdown_ClickEvent);
global::PCL.MyExtraButton previous=__lfMain_BtnExtraShutdown; if(previous!=null) {
previous.Click -= hook0;
}
__lfMain_BtnExtraShutdown=value;
global::PCL.MyExtraButton current=__lfMain_BtnExtraShutdown; if(current!=null) {
current.Click += hook0;
}
} }
private global::PCL.MyExtraButton __lfMain_BtnExtraBack;
internal virtual global::PCL.MyExtraButton BtnExtraBack { get {return __lfMain_BtnExtraBack;} [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.Synchronized)] set {
global::PCL.MyExtraButton.ClickEventHandler hook0 = new global::PCL.MyExtraButton.ClickEventHandler(this.BackToTop_ClickEvent);
global::PCL.MyExtraButton previous=__lfMain_BtnExtraBack; if(previous!=null) {
previous.Click -= hook0;
}
__lfMain_BtnExtraBack=value;
global::PCL.MyExtraButton current=__lfMain_BtnExtraBack; if(current!=null) {
current.Click += hook0;
}
} }
private void __LfMainClose(object sender, global::System.EventArgs args) { this.BtnTitleClose_Click(sender,(global::System.Windows.RoutedEventArgs)args); }
private void __LfMainMinimize(object sender, global::System.EventArgs args) { this.BtnTitleMin_Click(); }
private void __LfMainBack(object sender, global::System.EventArgs args) { this.PageBack(); }
} }
