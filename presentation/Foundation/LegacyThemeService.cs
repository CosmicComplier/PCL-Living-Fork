using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace PCLLF.Presentation.Foundation;
// LF's fixed default palette/background remains LF policy. CE ThemeManager at stable
// 1a7653127d578efdb2a9388999a3fdc67d9e49d5 uses different theme/configuration policy.
public sealed class LegacyThemeService<TColor> where TColor : LegacyColor {
    private readonly LegacyThemeHost<TColor> host;
    public LegacyThemeService(LegacyThemeHost<TColor> host){this.host=host;}
    private TColor Scale(TColor color,double scale)=>host.Channels(LegacyColor.Multiply(color,scale));
    private TColor Add(TColor first,TColor second)=>host.Channels(LegacyColor.Add(first,second));
    public void Refresh(int newTheme=-1) {
        try {
            host.SetHue(host.DefaultHue);host.SetSaturation(host.DefaultSaturation);host.SetLight(checked(host.DefaultLight-20));host.SetDelta(checked(host.DefaultDelta-90));
            host.Color1.Write(host.Hsl(host.Hue(),host.Saturation()*0.2,25+host.Light()*0.3));
            host.Color2.Write(host.Hsl(host.Hue(),host.Saturation(),checked(45+host.Light())));
            host.Color3.Write(host.Hsl(host.Hue(),host.Saturation(),checked(55+host.Light())));
            host.Color4.Write(host.Hsl(host.Hue(),host.Saturation(),checked(65+host.Light())));
            host.Color5.Write(host.Hsl(host.Hue(),host.Saturation(),80+host.Light()*0.4));
            host.Color6.Write(host.Hsl(host.Hue(),host.Saturation(),91+host.Light()*0.1));
            host.Color7.Write(host.Hsl(host.Hue(),host.Saturation(),95));host.Color8.Write(host.Hsl(host.Hue(),host.Saturation(),97));
            host.Bg0.Write(Add(Add(Scale(host.Color4.Read(),0.4),Scale(host.Color5.Read(),0.4)),Scale(host.Gray4.Read(),0.2)));
            host.Bg1.Write(host.Alpha(190,host.Color7.Read()));host.SemiTransparent.Write(host.Alpha(1,host.Color8.Read()));
            LegacyThemeColorSlot<TColor>[] slots={host.Color1,host.Color2,host.Color3,host.Color4,host.Color5,host.Color6,host.Color7,host.Color8,host.Bg0,host.Bg1};
            string[] names={"1","2","3","4","5","6","7","8","Bg0","Bg1"};
            for(int i=0;i<slots.Length;i++)host.Resources()["ColorBrush"+names[i]]=new SolidColorBrush(slots[i].Read().ToMediaColor());
            for(int i=0;i<slots.Length;i++)host.Resources()["ColorObject"+names[i]]=slots[i].Read().ToMediaColor();
            RefreshMain();
        }catch(Exception ex){host.ErrorToast(ex,"刷新主题颜色失败");}
    }
    public void RefreshMain()=>host.RunInUi(()=>{
        if(!host.MainLoaded())return;
        LinearGradientBrush brush=new(){EndPoint=new Point(1,0),StartPoint=new Point(0,0)};
        int[] deltas=host.Delta().Switch(d=>new[]{checked(-d),0,d},d=>d);
        brush.GradientStops.Add(new GradientStop{Offset=0,Color=host.Hsl(checked(host.Hue()+deltas[0]),host.Saturation(),checked(48+host.Light())).ToMediaColor()});
        brush.GradientStops.Add(new GradientStop{Offset=0.5,Color=host.Hsl(checked(host.Hue()+deltas[1]),host.Saturation(),checked(54+host.Light())).ToMediaColor()});
        brush.GradientStops.Add(new GradientStop{Offset=1,Color=host.Hsl(checked(host.Hue()+deltas[2]),host.Saturation(),checked(48+host.Light())).ToMediaColor()});
        host.SetTitleBackground(brush);host.TitleBackground().Freeze();ApplyBackground(host.MainPanel(),host.BackgroundImage());
    });
    public void ApplyBackground(Panel panel,UIElement image) {
        bool plain=host.BackgroundMode()==1;image.Visibility=plain?Visibility.Collapsed:Visibility.Visible;
        if(plain)panel.Background=Brushes.White;
        else if(host.Colorful()) {
            LinearGradientBrush brush=new(){EndPoint=new Point(0.1,1),StartPoint=new Point(0.9,0)};
            brush.GradientStops.Add(new GradientStop{Offset=-0.1,Color=host.Hsl(checked(host.Hue()-15),host.Saturation()*0.8,91).ToMediaColor()});
            brush.GradientStops.Add(new GradientStop{Offset=0.4,Color=host.Hsl(host.Hue(),host.Saturation()*0.8,91).ToMediaColor()});
            brush.GradientStops.Add(new GradientStop{Offset=1.1,Color=host.Hsl(checked(host.Hue()+15),host.Saturation()*0.8,91).ToMediaColor()});panel.Background=brush;
        }else panel.Background=host.Rgb(245,245,245).ToBrush();
        panel.Background.Freeze();
    }
}
