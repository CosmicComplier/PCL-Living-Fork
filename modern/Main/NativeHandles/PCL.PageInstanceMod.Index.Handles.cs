// New fixed-main 50b BtnManageIndex original WithEvents identity and zero-argument Handles relaxation.
namespace PCL { partial class PageInstanceMod {
private MyButton __lfIndexButton;
internal virtual MyButton BtnManageIndex {get {return __lfIndexButton;} [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)] set {MyButton.ClickEventHandler hook=__LfIndexClick;var previous=__lfIndexButton;if(previous!=null)previous.Click-=hook;__lfIndexButton=value;var current=__lfIndexButton;if(current!=null)current.Click+=hook;}}
private void __LfIndexClick(object sender,System.Windows.Input.MouseButtonEventArgs args){RebuildDependencyIndex();}
} }
