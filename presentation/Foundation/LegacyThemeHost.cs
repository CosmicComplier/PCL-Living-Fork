using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MeloongCore;
using PCLLF.Presentation.Controls;
namespace PCLLF.Presentation.Foundation;
public sealed class LegacyThemeColorSlot<TColor> where TColor : LegacyColor {
    public Func<TColor> Read=null!;
    public Action<TColor> Write=null!;
}
public sealed class LegacyThemeHost<TColor> where TColor : LegacyColor {
    public LegacyThemeColorSlot<TColor> Color1=null!,Color2=null!,Color3=null!,Color4=null!,Color5=null!,Color6=null!,Color7=null!,Color8=null!,Bg0=null!,Bg1=null!,Gray1=null!,Gray2=null!,Gray3=null!,Gray4=null!,Gray5=null!,Gray6=null!,Gray7=null!,Gray8=null!,SemiTransparent=null!;
    public int DefaultHue,DefaultSaturation,DefaultLight,DefaultDelta;
    public Func<int> Hue=null!,Saturation=null!,Light=null!;
    public Action<int> SetHue=null!,SetSaturation=null!,SetLight=null!;
    public Func<OneOf<int,int[]>> Delta=null!;
    public Action<OneOf<int,int[]>> SetDelta=null!;
    public Func<double,double,double,TColor> Hsl=null!,Rgb=null!;
    public Func<double,TColor,TColor> Alpha=null!;
    public Func<ControlColor,TColor> Channels=null!;
    public Func<ResourceDictionary> Resources=null!;
    public Action<Action> RunInUi=null!;
    public Func<bool> MainLoaded=null!,Colorful=null!;
    public Func<int> BackgroundMode=null!,BackgroundOpacity=null!,ColorMode=null!,PanelOpacity=null!;
    public Action<Action> RunInUiWait=null!;
    public Action<int> AddAnimationSuppression=null!;
    public Func<string,ImageBrush> LoadBackgroundResource=null!;
    public Action InstallTooltipTheme=null!;
    public Action<Brush> SetTitleBackground=null!;
    public Func<Brush> TitleBackground=null!;
    public Func<Panel> MainPanel=null!;
    public Func<UIElement> BackgroundImage=null!;
    public Action<Exception,string> ErrorToast=null!;
}
