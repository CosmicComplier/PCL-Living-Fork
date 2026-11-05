using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using PCLCS;
using NavigationCore = PCLCS.DownloadNavigationController<PCL.MyPageRight, PCL.FormMain.PageSubType, PCL.ModAnimation.AniData>;

namespace PCL
{
    public partial class PageDownloadLeft : PCL.IRefreshable
    {
        public PCL.FormMain.PageSubType PageID;
        private PCL.PageDownloadMap MapPage;
        private NavigationCore Controller;
        private NavigationCore Service
        {
            get
            {
                if (Controller is null)
                    Controller = new NavigationCore(CreateHost(), CreateAnimations());
                return Controller;
            }
        }

        public PageDownloadLeft()
        {
            __LfAttachOwnHandles();
            this.PageID = PCL.FormMain.PageSubType.DownloadInstall;
            this.InitializeComponent();
        }

        private DownloadNavigationHost<PCL.MyPageRight, PCL.FormMain.PageSubType> CreateHost()
        {
            var Host = CreateFrameHost();
            Host.PageId = new Func<PCL.FormMain.PageSubType>(() => PageID);
            Host.SetPageId = new Action<PCL.FormMain.PageSubType>(Value => PageID = Value);
            Host.CurrentSub = new Func<PCL.FormMain.PageSubType>(() => ModMain.FrmMain.PageCurrentSub);
            Host.CachedPage = ReadCached;
            Host.StorePage = StoreCached;
            Host.NewPage = NewPage;
            Host.ClearProjectCache = new Action(() => PCL.ResourceProject.Cache.Clear());
            Host.ClearVersionCache = new Action(() => PCL.ResourceVersion.ProjectFilesCache.Clear());
            Host.ForceLoader = ForceLoader;
            Host.ResetStorage = new Action<PCL.MyPageRight>(Page => ResourceContent(Page).Storage = new PCL.ResourceSearcher.SearchResult());
            Host.SetResultPage = new Action<PCL.MyPageRight, int>((Page, Value) => ResourceContent(Page).Page = Value);
            Host.Restart = new Action<PCL.MyPageRight>(Page => Page.PageLoaderRestart());
            Host.CheckItem = new Action<int, bool>((Code, Value) => NativeItem(Code).Checked = Value);
            Host.InstallChecked = new Func<bool>(() => this.ItemInstall.Checked);
            Host.ExitInstallSelection = new Action(() => ModMain.FrmDownloadInstall.ExitSelectPage());
            Host.RefreshHint = new Action(() => ModMain.Hint("正在刷新……", Log: false));
            Host.SenderTag = new Func<object, object>(Sender => ((dynamic)Sender).Tag);
            Host.AddMapRefreshButton = AddMapRefreshButton;
            return Host;
        }

        private static DownloadNavigationHost<PCL.MyPageRight, PCL.FormMain.PageSubType> CreateFrameHost()
        {
            return new DownloadNavigationHost<PCL.MyPageRight, PCL.FormMain.PageSubType>()
            {
                PageRight = new Func<PCL.MyPageRight>(() => ModMain.FrmMain.PageRight),
                VisibleRight = new Func<PCL.MyPageRight>(() => (PCL.MyPageRight)ModMain.FrmMain.PanMainRight.Child),
                SetPageRight = new Action<PCL.MyPageRight>(Page => ModMain.FrmMain.PageRight = Page),
                SetVisibleRight = new Action<PCL.MyPageRight>(Page => ModMain.FrmMain.PanMainRight.Child = Page),
                OnExit = new Action<PCL.MyPageRight>(Page => Page.PageOnExit()),
                OnForceExit = new Action<PCL.MyPageRight>(Page => Page.PageOnForceExit()),
                OnEnter = new Action<PCL.MyPageRight>(Page => Page.PageOnEnter())
            };
        }

        private PCL.MyPageRight ReadCached(int Code)
        {
            switch (Code)
            {
                case 7:
                {
                    return MapPage;
                }

                case 0:
                {
                    return ModMain.FrmDownloadInstall;
                }

                case 2:
                {
                    return ModMain.FrmDownloadMod;
                }

                case 3:
                {
                    return ModMain.FrmDownloadPack;
                }

                case 5:
                {
                    return ModMain.FrmDownloadResourcePack;
                }

                case 6:
                {
                    return ModMain.FrmDownloadShader;
                }

                case 4:
                {
                    return ModMain.FrmDownloadDataPack;
                }

                default:
                {
                    throw new ArgumentOutOfRangeException(nameof(Code));
                }
            }
        }

        private void StoreCached(int Code, PCL.MyPageRight Page)
        {
            switch (Code)
            {
                case 7:
                {
                    MapPage = (PCL.PageDownloadMap)Page;
                    break;
                }

                case 0:
                {
                    ModMain.FrmDownloadInstall = (PageDownloadInstall)Page;
                    break;
                }

                case 2:
                {
                    ModMain.FrmDownloadMod = (PCL.PageDownloadMod)Page;
                    break;
                }

                case 3:
                {
                    ModMain.FrmDownloadPack = (PCL.PageDownloadPack)Page;
                    break;
                }

                case 5:
                {
                    ModMain.FrmDownloadResourcePack = (PCL.PageDownloadResourcePack)Page;
                    break;
                }

                case 6:
                {
                    ModMain.FrmDownloadShader = (PCL.PageDownloadShader)Page;
                    break;
                }

                case 4:
                {
                    ModMain.FrmDownloadDataPack = (PCL.PageDownloadDataPack)Page;
                    break;
                }

                default:
                {
                    throw new ArgumentOutOfRangeException(nameof(Code));
                }
            }
        }

        private static PCL.MyPageRight NewPage(int Code)
        {
            switch (Code)
            {
                case 7:
                {
                    return new PCL.PageDownloadMap();
                }

                case 0:
                {
                    return new PageDownloadInstall();
                }

                case 2:
                {
                    return new PCL.PageDownloadMod();
                }

                case 3:
                {
                    return new PCL.PageDownloadPack();
                }

                case 5:
                {
                    return new PCL.PageDownloadResourcePack();
                }

                case 6:
                {
                    return new PCL.PageDownloadShader();
                }

                case 4:
                {
                    return new PCL.PageDownloadDataPack();
                }

                default:
                {
                    throw new ArgumentOutOfRangeException(nameof(Code));
                }
            }
        }

        private PCL.MyListItem NativeItem(int Code)
        {
            switch (Code)
            {
                case 7:
                {
                    return this.ItemMap;
                }

                case 0:
                {
                    return this.ItemInstall;
                }

                case 2:
                {
                    return this.ItemMod;
                }

                case 3:
                {
                    return this.ItemPack;
                }

                case 5:
                {
                    return this.ItemResourcePack;
                }

                case 6:
                {
                    return this.ItemShader;
                }

                case 4:
                {
                    return this.ItemDataPack;
                }

                default:
                {
                    throw new ArgumentOutOfRangeException(nameof(Code));
                }
            }
        }

        private static PCL.PageResource ResourceContent(PCL.MyPageRight Page)
        {
            switch (true)
            {
                case object _ when Page is PCL.PageDownloadMap:
                {
                    return ((PCL.PageDownloadMap)Page).Content;
                }

                case object _ when Page is PCL.PageDownloadMod:
                {
                    return ((PCL.PageDownloadMod)Page).Content;
                }

                case object _ when Page is PCL.PageDownloadPack:
                {
                    return ((PCL.PageDownloadPack)Page).Content;
                }

                case object _ when Page is PCL.PageDownloadResourcePack:
                {
                    return ((PCL.PageDownloadResourcePack)Page).Content;
                }

                case object _ when Page is PCL.PageDownloadShader:
                {
                    return ((PCL.PageDownloadShader)Page).Content;
                }

                case object _ when Page is PCL.PageDownloadDataPack:
                {
                    return ((PCL.PageDownloadDataPack)Page).Content;
                }

                default:
                {
                    throw new InvalidCastException("下载页面不包含资源搜索视图。");
                }
            }
        }

        private static void ForceLoader(int Code)
        {
            switch (Code)
            {
                case 0:
                {
                    PCL.ModDownload.DlClientListLoader.Start(IsForceRestart: true);
                    break;
                }

                case 1:
                {
                    PCL.ModDownload.DlOptiFineListLoader.Start(IsForceRestart: true);
                    break;
                }

                case 2:
                {
                    PCL.ModDownload.DlForgeListLoader.Start(IsForceRestart: true);
                    break;
                }

                case 3:
                {
                    PCL.ModDownload.DlNeoForgeListLoader.Start(IsForceRestart: true);
                    break;
                }

                case 4:
                {
                    PCL.ModDownload.DlLiteLoaderListLoader.Start(IsForceRestart: true);
                    break;
                }

                case 5:
                {
                    PCL.ModDownload.DlFabricListLoader.Start(IsForceRestart: true);
                    break;
                }

                case 6:
                {
                    PCL.ModDownload.DlFabricApiLoader.Start(IsForceRestart: true);
                    break;
                }

                case 7:
                {
                    PCL.ModDownload.DlOptiFabricLoader.Start(IsForceRestart: true);
                    break;
                }

                default:
                {
                    throw new ArgumentOutOfRangeException(nameof(Code));
                }
            }
        }

        private void AddMapRefreshButton(Action<object> Click)
        {
            var Button = new PCL.MyIconButton()
            {
                Tag = 7,
                ToolTip = "刷新",
                LogoScale = 0.85d,
                Logo = PCL.ModBase.Logo.IconButtonRefresh
            };
            Button.Click += (Sender, Args) => Click(Sender);
            this.ItemMap.Buttons = new[]
            {
                Button
            };
        }

        private void PageCheck(FrameworkElement sender, PCL.ModBase.RouteEventArgs e)
        {
            Service.Check(sender);
        }

        private void MapButtonInit()
        {
            Service.InitializeMapButton();
        }

        public object PageGet(PCL.FormMain.PageSubType ID = (PCL.FormMain.PageSubType)(-1))
        {
            return Service.Get(ID);
        }

        public void PageChange(PCL.FormMain.PageSubType ID)
        {
            Service.Change(ID);
        }

        private static void PageChangeRun(PCL.MyPageRight Target)
        {
            NavigationCore.Run(Target, CreateFrameHost(), CreateAnimations());
        }

        public void Refresh_Click(object sender, EventArgs e)
        {
            Service.RefreshClick(sender);
        }

        public void Refresh()
        {
            Service.Refresh();
        }

        public void Refresh(PCL.FormMain.PageSubType SubType)
        {
            Service.Refresh(SubType);
        }

        private void ItemInstall_Click(object sender, MouseButtonEventArgs e)
        {
            Service.InstallClick();
        }

        private static DownloadPageAnimationHost<PCL.ModAnimation.AniData> CreateAnimations()
        {
            return new DownloadPageAnimationHost<PCL.ModAnimation.AniData>()
            {
                Disabled = new Func<int>(() => PCL.ModAnimation.AniControlEnabled),
                SetDisabled = new Action<int>(Value => PCL.ModAnimation.AniControlEnabled = Value),
                Opacity = new Func<FrameworkElement, double, double, int, DownloadPageEase, PCL.ModAnimation.AniData>((Target, Delta, Duration, Delay, Ease) => PCL.ModAnimation.AaOpacity(Target, Delta, (int)Math.Round(Duration), Delay, ReadEase(Ease))),
                TranslateX = new Func<FrameworkElement, double, double, int, DownloadPageEase, PCL.ModAnimation.AniData>((Target, Delta, Duration, Delay, Ease) => PCL.ModAnimation.AaTranslateX(Target, Delta, (int)Math.Round(Duration), Delay, ReadEase(Ease))),
                TranslateY = new Func<FrameworkElement, double, double, int, DownloadPageEase, PCL.ModAnimation.AniData>((Target, Delta, Duration, Delay, Ease) => PCL.ModAnimation.AaTranslateY(Target, Delta, (int)Math.Round(Duration), Delay, ReadEase(Ease))),
                Scale = new Func<FrameworkElement, double, double, int, DownloadPageEase, bool, PCL.ModAnimation.AniData>((Target, Delta, Duration, Delay, Ease, After) => PCL.ModAnimation.AaScaleTransform(Target, Delta, (int)Math.Round(Duration), Delay, ReadEase(Ease), After)),
                Color = new Func<FrameworkElement, DependencyProperty, string, int, int, DownloadPageEase, PCL.ModAnimation.AniData>((Target, Property, Color, Duration, Delay, Ease) => PCL.ModAnimation.AaColor(Target, Property, Color, Duration, Delay, ReadEase(Ease))),
                Code = new Func<Action, int, bool, PCL.ModAnimation.AniData>((Work, Delay, After) => PCL.ModAnimation.AaCode(() => Work(), Delay, After)),
                Start = new Action<IEnumerable<PCL.ModAnimation.AniData>, string, bool>((Plan, Name, Refresh) => PCL.ModAnimation.AniStart(Plan.ToList(), Name, Refresh)),
                Stop = PCL.ModAnimation.AniStop
            };
        }

        private static PCL.ModAnimation.AniEase ReadEase(DownloadPageEase Value)
        {
            switch (Value)
            {
                case DownloadPageEase.OutFluent:
                {
                    return new PCL.ModAnimation.AniEaseOutFluent();
                }

                case DownloadPageEase.OutFluentExtraStrong:
                {
                    return new PCL.ModAnimation.AniEaseOutFluent(PCL.ModAnimation.AniEasePower.ExtraStrong);
                }

                default:
                {
                    return (PCL.ModAnimation.AniEase)null;
                }
            }
        }
    }
}
