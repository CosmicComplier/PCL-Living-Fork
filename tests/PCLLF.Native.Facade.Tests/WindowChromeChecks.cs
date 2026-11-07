using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml.Linq;
using PCL;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace PCLLF.Native.Facade.Tests;

// Load the exact product window markup without FormMain's startup/account lifecycle.
// Native controls and frame geometry are real; content cards use fixture text.
internal static class WindowChromeChecks {
    internal static void Run(FixtureEnvironment environment) {
        var app = System.Windows.Application.Current;
        var existing = app.Windows.Cast<Window>().ToHashSet();
        Window? main = app.MainWindow; ShutdownMode mode = app.ShutdownMode;
        app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        try { RunCore(environment); }
        finally {
            foreach (Window window in app.Windows.Cast<Window>().Where(window => !existing.Contains(window)).ToArray()) { window.Content = null; window.Close(); }
            app.MainWindow = main; app.ShutdownMode = mode;
        }
    }
    private static void RunCore(FixtureEnvironment environment) {
        var app = System.Windows.Application.Current;
        Window? oldMain = app.MainWindow; ShutdownMode oldMode = app.ShutdownMode;
        int oldTheme = Settings.Get<int>("UiLauncherColorMode"), suppression = ModAnimation.AniControlEnabled;
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        XElement markup = XElement.Load(Path.Combine(AppContext.BaseDirectory, "assets", "FormMain.xaml"));
        markup.Attribute(x + "Class")!.Remove();
        markup.SetAttributeValue(XNamespace.Xmlns + "local", "clr-namespace:PCL;assembly=LivingPCL");
        foreach (var element in markup.DescendantsAndSelf().Where(element => element.Name.NamespaceName == "clr-namespace:PCL"))
            element.Name = XNamespace.Get("clr-namespace:PCL;assembly=LivingPCL") + element.Name.LocalName;
        var window = (Window)XamlReader.Parse(markup.ToString());
        window.ShowActivated = false; window.ShowInTaskbar = false; window.Topmost = false;
        window.Left = -30000; window.Top = -30000; window.Width = 1005; window.Height = 634;
        T Find<T>(string name) where T : class => (T)window.FindName(name);
        var form = Find<Grid>("PanForm"); var title = Find<Grid>("PanTitle");
        var sidebar = Find<Border>("SidebarCard"); var left = Find<Border>("PanMainLeft"); var right = Find<Border>("PanMainRight");
        var main = Find<Grid>("PanMain"); var background = Find<Rectangle>("RectLeftBackground");
        Find<Grid>("PanBack").RenderTransform = null;
        Find<TextBlock>("LabTitleLogo").Text = "PCL-LF"; Find<TextBlock>("LabTitleLogo").Visibility = Visibility.Visible;
        Find<System.Windows.Shapes.Path>("ShapeTitleLogo").Visibility = Visibility.Collapsed;
        Native.Call(typeof(FormMain), "ConfigureThemeToggle", null, Find<MyIconButton>("BtnTitleTheme"));
        Native.Call(typeof(FormMain), "ConfigureTitleClip", null, title);
        left.SizeChanged += (_, args) => Native.Call(typeof(FormMain), "ReflowSidebarCard", null, sidebar, args);
        var cards = new StackPanel { Margin = new Thickness(25) };
        var content = new MyCard { Title = "内容卡片", Margin = new Thickness(0, 0, 0, 15) };
        content.Children.Add(new TextBlock { Text = "原生窗口框架与侧栏布局验证", Margin = new Thickness(20, 45, 20, 25) });
        cards.Children.Add(content); right.Child = cards;
        void Layout() {
            window.UpdateLayout();
            var border = Find<Border>("BorderForm");
            form.Width = border.ActualWidth + .001; form.Height = border.ActualHeight + .001;
            main.Width = form.Width; main.Height = Math.Max(0, form.Height - title.ActualHeight);
            window.UpdateLayout(); VisualAssetChecks.Drain();
            background.Width = left.ActualWidth; window.UpdateLayout();
        }
        Rect Bounds(FrameworkElement element) => new(element.TranslatePoint(new Point(), form), element.RenderSize);
        try {
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown; ModAnimation.AniControlEnabled = 1;
            left.Child = new PageDownloadLeft();
            window.Show(); Layout();
            foreach (bool launch in new[] { false, true }) {
                if (launch) {
                    var panel = new StackPanel { Width = 300, Margin = new Thickness(0, 24, 0, 20) };
                    panel.Children.Add(new TextBlock { Text = "正版  /  离线", HorizontalAlignment = HorizontalAlignment.Center });
                    panel.Children.Add(new TextBlock { Text = "Player", HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 95, 0, 95), FontSize = 20 });
                    panel.Children.Add(new MyButton { Text = "启动游戏", Margin = new Thickness(20, 0, 20, 10), Height = 50 });
                    panel.Children.Add(new MyButton { Text = "版本选择", Margin = new Thickness(20, 0, 20, 0), Height = 35 });
                    left.Child = panel;
                }
                foreach (bool dark in new[] { false, true }) {
                    Settings.Set("UiLauncherColorMode", dark ? 1 : 0);
                    title.Background = (Brush)Native.Static("PCL.ModSecret", "CreateTitleBackground")!;
                    Native.Static("PCL.ModTheme", "UseDefaultBackground", Find<Canvas>("ImgBack"));
                    foreach (int width in new[] { 1005, 810 }) {
                        window.Width = width; Layout();
                        Rect side = Bounds(sidebar), card = Bounds(content);
                        ModpackStageLayoutChecks.Save(form, Path.Combine(environment.Data, $"frame-{(launch ? "launch" : "download")}-{(dark ? "dark" : "light")}-{width}.png"), (int)form.ActualWidth, (int)form.ActualHeight);
                        Native.Check(Math.Abs(side.Left - 8) < 1 && Math.Abs(side.Top - title.ActualHeight - 8) < 1 &&
                            Math.Abs(form.ActualHeight - side.Bottom - 8) < 1 && Math.Abs(card.Top - title.ActualHeight - 25) < 1 &&
                            Math.Abs(card.Left - side.Right - 25) < 1 && Math.Abs(form.ActualWidth - card.Right - 25) < 1,
                            "Sidebar did not use its tighter 8-DIP inset while content retained 25-DIP spacing.");
                        Native.Check(Math.Abs(Bounds(background).Left - side.Left) < 1 && Math.Abs(background.ActualWidth - sidebar.ActualWidth) < 1 && Math.Abs(background.ActualHeight - sidebar.ActualHeight) < 1,
                            $"Animated sidebar background and actual content card diverged: background={Bounds(background)}, sidebar={side}, desired={sidebar.DesiredSize}, width={sidebar.Width}/{sidebar.MinWidth}/{sidebar.MaxWidth}, measure={sidebar.IsMeasureValid}, sameChild={ReferenceEquals(sidebar.Child,left)}, left={left.RenderSize}, desiredLeft={left.DesiredSize}, column={main.ColumnDefinitions[0].Width}/{main.ColumnDefinitions[0].ActualWidth}.");
                        foreach (var element in new FrameworkElement[] { title }) {
                            Native.Check(VisualTreeHelper.GetClip(element) is RectangleGeometry clip && clip.RadiusX == 6 && clip.RadiusY == 6 &&
                                Math.Abs(clip.Rect.Width - element.ActualWidth) < 1 && Math.Abs(clip.Rect.Height - element.ActualHeight) < 1 &&
                                !clip.FillContains(new Point(1, element.ActualHeight - 1)),
                                "Title rounding did not follow resized bounds or clip its lower corners.");
                        }
                        Native.Check(sidebar.CornerRadius.TopLeft == 6 && background.RadiusX == 6 && background.RadiusY == 6 && right.ActualWidth > 300,
                            "Minimum-width content overflowed after adding sidebar spacing.");
                    }
                }
            }
            Native.Check(!window.IsActive, "Frame fixture took desktop focus.");
            environment.AssertNoForbiddenIo();
            Console.WriteLine("PASS actual window XAML chrome: native download sidebar and launch-width fixture, 8-DIP sidebar/25-DIP content insets, native rounded card background/title clip, page-width reflow, light/dark palettes and minimum/default resizing; no launcher startup or accounts.");
        } finally {
            window.Content = null; window.Close(); app.MainWindow = oldMain; app.ShutdownMode = oldMode;
            ModAnimation.AniControlEnabled = suppression; Settings.Set("UiLauncherColorMode", oldTheme);
        }
    }
}
