using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MeloongCore;
using PCLCS;
using PCLLF.Presentation.Foundation;

// Call only on an actual Windows STA Application dispatcher. This is not a Linux run.
// The host supplies live color fields/UI/bitmap primitives; the production theme,
// palette, bitmap loader and tooltip-card adapter contain every tested rule.
public static class ThemeScenarios {
    public static void Run(Application application, string assetDirectory) {
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA || !application.Dispatcher.CheckAccess()) throw new InvalidOperationException("Windows STA dispatcher required");
        ResourceDictionary oldResources = application.Resources;
        ShutdownMode oldShutdown = application.ShutdownMode;
        bool oldDark = LegacyThemePalette.IsDarkTheme;
        application.Resources = new ResourceDictionary();
        application.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        int owner = Environment.CurrentManagedThreadId, mode = 0, opacity = 960, backgroundMode = 0, suppression = 0, refreshes = 0;
        bool colorful = true, failRefresh = false;
        int hue = 225, saturation = 100, light = 20;
        OneOf<int,int[]> delta = 25;
        LegacyColor[] colors = new LegacyColor[19];
        for (int i=0;i<colors.Length;i++) colors[i]=new LegacyColor();
        LegacyThemeColorSlot<LegacyColor> Slot(int index) => new() { Read=()=>colors[index], Write=value=>colors[index]=value };
        Grid background=new(), main=new(); Border title=new();
        Border tooltip=new() { Child=new TextBlock { Text="actual card" } };
        LegacyTooltipThemeAdapter tooltipTheme=new(()=>tooltip);
        LegacyBitmapHost bitmapHost=new(typeof(ThemeScenarios).Assembly.GetName().Name!,_=>null,(error,_)=>throw error);
        Window window=new() { Width=320,Height=160,ShowActivated=false,ShowInTaskbar=false,Left=-10000,Top=-10000 };
        LegacyThemeHost<LegacyColor> host=new() {
            Color1=Slot(0),Color2=Slot(1),Color3=Slot(2),Color4=Slot(3),Color5=Slot(4),Color6=Slot(5),Color7=Slot(6),Color8=Slot(7),
            Bg0=Slot(8),Bg1=Slot(9),Gray1=Slot(10),Gray2=Slot(11),Gray3=Slot(12),Gray4=Slot(13),Gray5=Slot(14),Gray6=Slot(15),Gray7=Slot(16),Gray8=Slot(17),SemiTransparent=Slot(18),
            DefaultHue=225,DefaultSaturation=100,DefaultLight=40,DefaultDelta=115,
            Hue=()=>hue,Saturation=()=>saturation,Light=()=>light,SetHue=value=>hue=value,SetSaturation=value=>saturation=value,SetLight=value=>light=value,
            Delta=()=>delta,SetDelta=value=>delta=value,Hsl=(h,s,l)=>new LegacyColor().FromHSL2(h,s,l),Rgb=(r,g,b)=>new LegacyColor(r,g,b),
            Alpha=(a,color)=>new LegacyColor(a,color),Channels=color=>new LegacyColor(color.A,color.R,color.G,color.B),Resources=()=>application.Resources,
            RunInUi=work=>application.Dispatcher.Invoke(work),RunInUiWait=work=>application.Dispatcher.Invoke(work),
            MainLoaded=()=>window.IsLoaded,Colorful=()=>colorful,BackgroundMode=()=>backgroundMode,BackgroundOpacity=()=>748,ColorMode=()=>mode,PanelOpacity=()=>opacity,
            AddAnimationSuppression=value=>suppression=checked(suppression+value),InstallTooltipTheme=tooltipTheme.Install,
            LoadBackgroundResource=resource=>new LegacyBitmap(Path.Combine(assetDirectory,Path.GetFileName(resource)),bitmapHost).ToImageBrush(),
            SetTitleBackground=brush=>title.Background=brush,TitleBackground=()=>title.Background,MainPanel=()=>main,BackgroundImage=()=>background,
            ErrorToast=(error,_)=>throw new InvalidOperationException("Theme production failure",error)
        };
        LegacyThemeService<LegacyColor> theme=new(host);
        Grid watched=main;
        watched.Children.Add(background); watched.Children.Add(tooltip);
        Exception sentinel=new InvalidOperationException("synthetic palette callback");
        LegacyThemePalette.Watch(watched,()=>{
            Require(Environment.CurrentManagedThreadId==owner && application.Dispatcher.CheckAccess(),"Theme callback left the UI thread");
            refreshes++;
            if(failRefresh)throw sentinel;
        });
        try {
            theme.Refresh();
            Require(!LegacyThemePalette.IsDarkTheme && LegacyThemePalette.Brush("ColorBrushDialog").Color==(Color)ColorConverter.ConvertFromString("#FBFBFB"),"Light palette");
            SolidColorBrush sharedWhite=LegacyThemePalette.Brush("ColorBrushWhite");
            SolidColorBrush oldCard=LegacyThemePalette.Brush("ColorBrushCard");oldCard.Freeze();
            theme.UseDefaultBackground(background);
            ImageBrush day=(ImageBrush)background.Background;
            day.Stretch=Stretch.UniformToFill;day.AlignmentX=AlignmentX.Right;day.Freeze();
            window.Content=watched;window.Show();PumpUntil(()=>refreshes==1);
            int loadedRefreshes=refreshes;
            Task dispatched=Task.Run(()=>theme.Refresh());
            PumpUntil(()=>dispatched.IsCompleted);dispatched.GetAwaiter().GetResult();
            Require(refreshes==loadedRefreshes+1,"Real worker-to-UI refresh did not notify once");
            loadedRefreshes=refreshes;
            mode=1;theme.Refresh();
            Require(refreshes==loadedRefreshes+1 && suppression==0,"Loaded watcher/animation suppression");
            Require(LegacyThemePalette.IsDarkTheme && ReferenceEquals(sharedWhite,LegacyThemePalette.Brush("ColorBrushWhite")) && sharedWhite.Color==(Color)ColorConverter.ConvertFromString("#252932"),"Mutable role brush identity/dark color");
            Require(!ReferenceEquals(oldCard,LegacyThemePalette.Brush("ColorBrushCard")) && LegacyThemePalette.Brush("ColorBrushCard").Color.A==245,"Frozen card replacement/panel alpha");
            Require(colors[13].ToMediaColor()==(Color)ColorConverter.ConvertFromString("#7F8B9E") && colors[0].ToMediaColor()==(Color)ColorConverter.ConvertFromString("#DCE2EB"),"Live color slots");
            ImageBrush night=(ImageBrush)background.Background;
            Require(!ReferenceEquals(day,night) && !ReferenceEquals(day.ImageSource,night.ImageSource) && night.Stretch==Stretch.UniformToFill && night.AlignmentX==AlignmentX.Right,"Night switch preserves frozen ImageBrush settings");
            Require(LegacyThemePalette.DefaultBackgroundResource().EndsWith("DefaultBackgroundNight.png",StringComparison.Ordinal) && background.Opacity==0.748,"Built-in night image exposure");
            LegacyThemePalette.MarkCustomBackground(background);colorful=false;theme.ApplyBackground(main,background);
            Require(Math.Abs(background.Opacity-0.748*0.32)<1e-12 && ReferenceEquals(main.Background,LegacyThemePalette.Brush("ColorBrushBackground")),"Custom image dimming");
            backgroundMode=1;theme.ApplyBackground(main,background);
            Require(background.Visibility==Visibility.Collapsed && ReferenceEquals(main.Background,LegacyThemePalette.Brush("ColorBrushBackground")),"Plain dark background");
            Require(((LinearGradientBrush)title.Background).IsFrozen,"Title brush freeze");
            opacity=-1;theme.RefreshPanelOpacity();Require(LegacyThemePalette.Brush("ColorBrushCard").Color.A==0,"Panel lower clamp");
            opacity=1001;theme.RefreshPanelOpacity();Require(LegacyThemePalette.Brush("ColorBrushCard").Color.A==255,"Panel upper clamp");
            TextBlock replacement=new(){Text="replacement"};tooltip.Child=replacement;
            PumpUntil(()=>replacement.Foreground is SolidColorBrush b && b.Color==colors[0].ToMediaColor());
            Require(ReferenceEquals(tooltip.Background,LegacyThemePalette.Brush("ColorBrushPopup")),"Actual tooltip card role reference");
            failRefresh=true;
            try { theme.NotifyThemeChanged();throw new Exception("Palette callback failure swallowed"); }
            catch(Exception error) when(ReferenceEquals(error,sentinel)) { }
            finally { failRefresh=false; }
            Require(suppression==0,"Failed palette callback leaked animation suppression");
            window.Content=null;PumpUntil(()=>!watched.IsLoaded);
            int unloaded=refreshes;theme.NotifyThemeChanged();Require(refreshes==unloaded,"Unloaded page retained palette subscription");
            window.Content=watched;PumpUntil(()=>watched.IsLoaded && refreshes==unloaded+1);
            theme.NotifyThemeChanged();Require(refreshes==unloaded+2,"Reload duplicated palette subscription");
            window.Content=null;PumpUntil(()=>!watched.IsLoaded);
            Console.WriteLine("PASS theme-main50 Windows: actual palette/slots/resources/title/background/opacity, live loaded/unloaded watcher and failure finally, actual tooltip-card child replacement; host bitmap filesystem adapter used.");
        } finally {
            window.Close();theme.PrepareThemeTones(oldDark);
            application.Resources=oldResources;application.ShutdownMode=oldShutdown;
        }
    }
    private static void Require(bool value,string message){if(!value)throw new Exception(message);}
    private static void PumpUntil(Func<bool> done) {
        Stopwatch timer=Stopwatch.StartNew();
        while(!done()) {
            if(timer.ElapsedMilliseconds>4000)throw new TimeoutException("WPF theme operation timed out");
            DispatcherFrame frame=new();Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,new Action(()=>frame.Continue=false));
            Dispatcher.PushFrame(frame);
        }
    }
}
