using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using PCLLF.Presentation.Accounts;

namespace PCL
{
    public partial class MySkin
    {
        public event ClickEventHandler Click;
        public delegate void ClickEventHandler(object sender, MouseButtonEventArgs e);
        public ModLoader.LoaderTask<(string, string), string> Loader;
        private readonly LegacySkinControl<ModLaunch.McLoginLegacy, ModLaunch.McLoginMs, ModLaunch.McLoginServer, ModLoader.LoaderTask<(string, string), string>> Native;
        public MySkin()
        {
            __LfAttachOwnHandles();
            Native = new LegacySkinControl<ModLaunch.McLoginLegacy, ModLaunch.McLoginMs, ModLaunch.McLoginServer, ModLoader.LoaderTask<(string, string), string>>(LoginPageRuntime.Skins, new LegacySkinView<ModLoader.LoaderTask<(string, string), string>>() { Loader = new Func<ModLoader.LoaderTask<(string, string), string>>(() => Loader), Control = new Func<FrameworkElement>(() => this), Scale = new Func<double>(() => ((ScaleTransform)this.RenderTransform).ScaleX), ShadowOpacity = new Func<double>(() => this.ShadowSkin.Opacity), AnimateShadow = new Action<double, int, int>((Value, Time, Delay) => ModAnimation.AniStart(ModAnimation.AaOpacity(this.ShadowSkin, Value, Time, Delay), "Skin Shadow")), SetToolTip = new Action<string>(Value => this.ToolTip = Value), SetBackTag = new Action<object>(Value => this.ImgBack.Tag = Value), SetFore = new Action<ImageSource>(Value => this.ImgFore.Source = Value), SetBack = new Action<ImageSource>(Value => this.ImgBack.Source = Value), CapeVisibility = new Func<Visibility>(() => this.BtnSkinCape.Visibility), SetCapeVisibility = new Action<Visibility>(Value => this.BtnSkinCape.Visibility = Value), SetSaveEnabled = new Action<bool>(Value => this.BtnSkinSave.IsEnabled = Value), Click = RaiseSkinClick });
            this.InitializeComponent();
        }

        private void RaiseSkinClick(object sender, MouseButtonEventArgs e)
        {
            Click?.Invoke(sender, e);
        }

        public string Address
        {
            get
            {
                return Native.Address;
            }

            set
            {
                Native.Address = value;
            }
        }

        public bool HasCape
        {
            get
            {
                return Native.HasCape;
            }

            set
            {
                Native.HasCape = value;
            }
        }

        private void PanSkin_MouseEnter(object sender, MouseEventArgs e)
        {
            Native.MouseEnter();
        }

        private void PanSkin_MouseLeave(object sender, MouseEventArgs e)
        {
            Native.MouseLeave();
        }

        private void PanSkin_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Native.MouseDown();
        }

        private void PanSkin_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Native.MouseUp(sender, e);
        }

        public void BtnSkinSave_Click()
        {
            Native.Save();
        }

        public static void Save(ModLoader.LoaderTask<(string, string), string> Loader)
        {
            LoginPageRuntime.Skins.Save(Loader);
        }

        private void BtnSkinSave_Checked(MyMenuItem sender, RoutedEventArgs e)
        {
            Native.SaveChecked();
        }

        public void Load()
        {
            Native.Load();
        }

        public void Clear()
        {
            Native.Clear();
        }

        public void RefreshClick()
        {
            Native.RefreshClick();
        }

        public static void RefreshCache(ModLoader.LoaderTask<(string, string), string> sender = null)
        {
            LoginPageRuntime.Skins.RefreshCache(sender);
        }

        public static void ReloadCache(string SkinAddress)
        {
            LoginPageRuntime.Skins.ReloadCache(SkinAddress);
        }

        public void BtnSkinCape_Click()
        {
            Native.ChangeCape();
        }
    }
}
