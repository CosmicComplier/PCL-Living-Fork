using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace PCLLF.Presentation.Foundation;
// LF main50 light/dark palette/background remains LF policy. CE ThemeManager at stable
// 1a7653127d578efdb2a9388999a3fdc67d9e49d5 uses different theme/configuration policy.
public sealed class LegacyThemeService<TColor> where TColor : LegacyColor {
    private readonly LegacyThemeHost<TColor> host;
    public LegacyThemeService(LegacyThemeHost<TColor> host){this.host=host;}
    private TColor Scale(TColor color,double scale)=>host.Channels(LegacyColor.Multiply(color,scale));
    private TColor Add(TColor first,TColor second)=>host.Channels(LegacyColor.Add(first,second));
    public void Refresh(int newTheme=-1) {
        if (Application.Current is null) return;
        if (!Application.Current.Dispatcher.CheckAccess()) { host.RunInUiWait(() => Refresh(newTheme)); return; }
        try {
            PrepareThemeTones(host.ColorMode() == 1);
            host.SetHue(host.DefaultHue);host.SetSaturation(host.DefaultSaturation);host.SetLight(checked(host.DefaultLight-20));host.SetDelta(checked(host.DefaultDelta-90));
            host.Color1.Write(host.Hsl(host.Hue(),host.Saturation()*0.2,25+host.Light()*0.3));
            host.Color2.Write(host.Hsl(host.Hue(),host.Saturation(),checked(45+host.Light())));
            host.Color3.Write(host.Hsl(host.Hue(),host.Saturation(),checked(55+host.Light())));
            host.Color4.Write(host.Hsl(host.Hue(),host.Saturation(),checked(65+host.Light())));
            host.Color5.Write(host.Hsl(host.Hue(),host.Saturation(),80+host.Light()*0.4));
            host.Color6.Write(host.Hsl(host.Hue(),host.Saturation(),91+host.Light()*0.1));
            host.Color7.Write(host.Hsl(host.Hue(),host.Saturation(),95));host.Color8.Write(host.Hsl(host.Hue(),host.Saturation(),97));
            host.Bg0.Write(Add(Add(Scale(host.Color4.Read(),0.4),Scale(host.Color5.Read(),0.4)),Scale(host.Gray4.Read(),0.2)));
            host.Bg1.Write(host.Alpha(190,host.Color7.Read()));ApplyThemePalette();
            RefreshMain();
            NotifyThemeChanged();
        }catch(Exception ex){host.ErrorToast(ex,"刷新主题颜色失败");}
    }
    public Brush CreateTitleBackground() {
        LinearGradientBrush brush=new(){EndPoint=new Point(1,0),StartPoint=new Point(0,0)};
        int[] deltas=host.Delta().Switch(d=>new[]{checked(-d),0,d},d=>d);
        double saturation=LegacyThemePalette.IsDarkTheme?72:host.Saturation(), hueScale=LegacyThemePalette.IsDarkTheme?0.5:1.0;
        brush.GradientStops.Add(new GradientStop{Offset=0,Color=host.Hsl(host.Hue()+deltas[0]*hueScale,saturation,LegacyThemePalette.IsDarkTheme?20:checked(48+host.Light())).ToMediaColor()});
        brush.GradientStops.Add(new GradientStop{Offset=0.5,Color=host.Hsl(host.Hue()+deltas[1]*hueScale,saturation,LegacyThemePalette.IsDarkTheme?24:checked(54+host.Light())).ToMediaColor()});
        brush.GradientStops.Add(new GradientStop{Offset=1,Color=host.Hsl(host.Hue()+deltas[2]*hueScale,saturation,LegacyThemePalette.IsDarkTheme?20:checked(48+host.Light())).ToMediaColor()});
        brush.Freeze();
        return brush;
    }
    public void RefreshMain()=>host.RunInUi(()=>{
        if(!host.MainLoaded())return;
        host.SetTitleBackground(CreateTitleBackground());
        ApplyBackground(host.MainPanel(),host.BackgroundImage());
    });
    public void ApplyBackground(Panel panel,UIElement image) {
        bool plain=host.BackgroundMode()==1;
        if(image is Panel imagePanel)RefreshDefaultBackground(imagePanel);
        image.Visibility=plain?Visibility.Collapsed:Visibility.Visible;
        image.Opacity=host.BackgroundOpacity()/1000.0*LegacyThemePalette.BackgroundThemeOpacityFactor(image);
        if(plain)panel.Background=LegacyThemePalette.Brush(LegacyThemePalette.IsDarkTheme?"ColorBrushBackground":"ColorBrushWhite");
        else if(host.Colorful()) {
            LinearGradientBrush brush=new(){EndPoint=new Point(0.1,1),StartPoint=new Point(0.9,0)};
            double saturation=LegacyThemePalette.IsDarkTheme?15:host.Saturation()*0.8;
            brush.GradientStops.Add(new GradientStop{Offset=-0.1,Color=host.Hsl(checked(host.Hue()-15),saturation,LegacyThemePalette.IsDarkTheme?11:91).ToMediaColor()});
            brush.GradientStops.Add(new GradientStop{Offset=0.4,Color=host.Hsl(host.Hue(),saturation,LegacyThemePalette.IsDarkTheme?14:91).ToMediaColor()});
            brush.GradientStops.Add(new GradientStop{Offset=1.1,Color=host.Hsl(checked(host.Hue()+15),saturation,LegacyThemePalette.IsDarkTheme?11:91).ToMediaColor()});
            panel.Background=brush;
        }else panel.Background=LegacyThemePalette.Brush("ColorBrushBackground");
    }
    public void UseDefaultBackground(Panel target) {
        target.SetValue(LegacyThemePalette.BuiltInBackgroundProperty,true);
        target.SetValue(LegacyThemePalette.BackgroundAssetProperty,"");
        RefreshDefaultBackground(target);
    }
    public void RefreshDefaultBackground(Panel target) {
        if(!(bool)target.GetValue(LegacyThemePalette.BuiltInBackgroundProperty))return;
        string resource=LegacyThemePalette.DefaultBackgroundResource();
        if((string)target.GetValue(LegacyThemePalette.BackgroundAssetProperty)==resource)return;
        ImageBrush picture=host.LoadBackgroundResource(resource);
        if(target.Background is ImageBrush current) {
            if(current.IsFrozen)current=current.CloneCurrentValue();
            current.ImageSource=picture.ImageSource;
            target.Background=current;
        }else target.Background=picture;
        target.SetValue(LegacyThemePalette.BackgroundAssetProperty,resource);
    }
    private TColor Tone(string value) {
        Color color=(Color)ColorConverter.ConvertFromString(value)!;
        return host.Channels(new Controls.ControlColor(color.A,color.R,color.G,color.B));
    }
    public void RefreshPanelOpacity() {
        if(Application.Current is null)return;
        if(!Application.Current.Dispatcher.CheckAccess()){host.RunInUiWait(RefreshPanelOpacity);return;}
        int value=Math.Max(0,Math.Min(1000,host.PanelOpacity()));
        byte alpha=Convert.ToByte(Math.Round(value*255.0/1000));
        foreach(string role in new[]{"Card","BackgroundTransparentSidebar"}) {
            Color current=LegacyThemePalette.Brush("ColorBrush"+role).Color;
            LegacyThemePalette.PutColor(role,Color.FromArgb(alpha,current.R,current.G,current.B));
        }
    }
    public void PrepareThemeTones(bool dark) {
        LegacyThemePalette.Prepare(dark);
        string[] grays=dark
            ?new[]{"#DCE2EB","#B4BECD","#A1ACBD","#7F8B9E","#586275","#3C4656","#303846","#272F3B"}
            :new[]{"#404040","#737373","#8C8C8C","#A6A6A6","#CCCCCC","#EBEBEB","#F0F0F0","#F5F5F5"};
        LegacyThemeColorSlot<TColor>[] slots={host.Gray1,host.Gray2,host.Gray3,host.Gray4,host.Gray5,host.Gray6,host.Gray7,host.Gray8};
        for(int index=0;index<slots.Length;index++)slots[index].Write(Tone(grays[index]));
    }
    public void ApplyThemePalette() {
        if(LegacyThemePalette.IsDarkTheme) {
            LegacyThemeColorSlot<TColor>[] slots={host.Color1,host.Color2,host.Color3,host.Color4,host.Color5,host.Color6,host.Color7,host.Color8,host.Bg0,host.Bg1};
            string[] tones={"#DCE2EB","#AABFFF","#91AAFF","#ADC2FF","#6583B7","#394A69","#2B3851","#252F44","#7894C5","#BE252F44"};
            for(int index=0;index<slots.Length;index++)slots[index].Write(Tone(tones[index]));
        }
        host.SemiTransparent.Write(host.Alpha(1,host.Color8.Read()));
        LegacyThemeColorSlot<TColor>[] main={host.Color1,host.Color2,host.Color3,host.Color4,host.Color5,host.Color6,host.Color7,host.Color8};
        LegacyThemeColorSlot<TColor>[] gray={host.Gray1,host.Gray2,host.Gray3,host.Gray4,host.Gray5,host.Gray6,host.Gray7,host.Gray8};
        for(int index=0;index<8;index++) {
            LegacyThemePalette.PutColor((index+1).ToString(),main[index].Read().ToMediaColor());
            LegacyThemePalette.PutColor("Gray"+(index+1),gray[index].Read().ToMediaColor());
        }
        LegacyThemePalette.PutColor("Bg0",host.Bg0.Read().ToMediaColor());
        LegacyThemePalette.PutColor("Bg1",host.Bg1.Read().ToMediaColor());
        LegacyThemePalette.PutColor("SemiTransparent",host.SemiTransparent.Read().ToMediaColor());
        foreach(var role in Roles)LegacyThemePalette.PutColor(role.Key,Tone(role.Value[LegacyThemePalette.IsDarkTheme?1:0]).ToMediaColor());
        RefreshPanelOpacity();
        host.InstallTooltipTheme();
        LegacyThemePalette.PutBrush(SystemColors.WindowBrushKey,LegacyThemePalette.Brush("ColorBrushWhite").Color);
        LegacyThemePalette.PutBrush(SystemColors.WindowTextBrushKey,host.Color1.Read().ToMediaColor());
        LegacyThemePalette.PutBrush(SystemColors.ControlBrushKey,LegacyThemePalette.Brush("ColorBrushWhite").Color);
        LegacyThemePalette.PutBrush(SystemColors.ControlTextBrushKey,host.Color1.Read().ToMediaColor());
        LegacyThemePalette.PutBrush(SystemColors.GrayTextBrushKey,host.Gray4.Read().ToMediaColor());
        LegacyThemePalette.PutBrush(SystemColors.HighlightBrushKey,host.Color3.Read().ToMediaColor());
        LegacyThemePalette.PutBrush(SystemColors.HighlightTextBrushKey,LegacyThemePalette.Brush("ColorBrushOnAccent").Color);
        LegacyThemePalette.PutBrush(SystemColors.MenuBrushKey,LegacyThemePalette.Brush("ColorBrushPopup").Color);
        LegacyThemePalette.PutBrush(SystemColors.MenuTextBrushKey,host.Color1.Read().ToMediaColor());
        LegacyThemePalette.PutBrush(SystemColors.InfoBrushKey,LegacyThemePalette.Brush("ColorBrushPopup").Color);
        LegacyThemePalette.PutBrush(SystemColors.InfoTextBrushKey,host.Color1.Read().ToMediaColor());
        LegacyThemePalette.PutBrush(SystemColors.ControlLightBrushKey,host.Gray6.Read().ToMediaColor());
        LegacyThemePalette.PutBrush(SystemColors.ControlLightLightBrushKey,LegacyThemePalette.Brush("ColorBrushWhite").Color);
        LegacyThemePalette.PutBrush(SystemColors.ControlDarkBrushKey,host.Gray5.Read().ToMediaColor());
        LegacyThemePalette.PutBrush(SystemColors.ControlDarkDarkBrushKey,host.Gray7.Read().ToMediaColor());
        LegacyThemePalette.ApplyWindowChrome();
    }
    public void NotifyThemeChanged()=>LegacyThemePalette.Notify(host.AddAnimationSuppression);
    private static readonly System.Collections.Generic.Dictionary<string,string[]> Roles=new() {
        {"White",new[]{"#FFFFFF","#252932"}},
        {"HalfWhite",new[]{"#55FFFFFF","#55252932"}},
        {"SemiWhite",new[]{"#BBFFFFFF","#BB252932"}},
        {"Card",new[]{"#F5FFFFFF","#F5252932"}},
        {"Dialog",new[]{"#FBFBFB","#252A34"}},
        {"Popup",new[]{"#FFFFFF","#2B303B"}},
        {"Background",new[]{"#F5F5F5","#171B22"}},
        {"BackgroundTransparentSidebar",new[]{"#F1FFFFFF","#F11C2028"}},
        {"TextSecondary",new[]{"#5C5C5C","#B4BECD"}},
        {"OnAccent",new[]{"#FFFFFF","#141B2B"}},
        {"TitleForeground",new[]{"#FFFFFF","#DCE2EB"}},
        {"TitleSelected",new[]{"#FFFFFF","#303B52"}},
        {"RedBack",new[]{"#80FBDDDD","#804E252C"}},
        {"RedLight",new[]{"#FF4C4C","#FF9A9F"}},
        {"RedDark",new[]{"#CE2111","#FF8B91"}},
        {"InfoBack",new[]{"#D9ECFF","#24364A"}},
        {"InfoBorder",new[]{"#1172D4","#6AAFF3"}},
        {"InfoText",new[]{"#0F64B8","#A9D5FF"}},
        {"WarningBack",new[]{"#FFEBD7","#413421"}},
        {"WarningBorder",new[]{"#F57A00","#DAAE58"}},
        {"WarningText",new[]{"#D86C00","#F4CE8A"}},
        {"DangerBack",new[]{"#FFDDDF","#43272D"}},
        {"DangerBorder",new[]{"#D82929","#EA818B"}},
        {"DangerText",new[]{"#BF0B0B","#FFB3BA"}},
        {"SuccessText",new[]{"#218A43","#8ED8AF"}},
        {"Shadow",new[]{"#343D4A","#000000"}},
    };
}
