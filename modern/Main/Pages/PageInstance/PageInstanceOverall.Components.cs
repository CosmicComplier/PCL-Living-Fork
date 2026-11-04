using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using MeloongCore;
using MeloongCore.Extensions;
using MeloongCore.Wpf;
using MeloongCore.Wpf.Extensions;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PCLCS;
using ThrottleDebounce;

namespace PCL
{
    public partial class PageInstanceOverall
    {
        private global::System.Int32 ComponentRefreshId;
        private readonly global::PCL.ModLoader.LoaderTask<global::PCL.PageInstanceOverall.ComponentRequest, global::PCL.PageInstanceOverall.ComponentResult> ComponentsLoader;
        private class ComponentRequest : global::PCLCS.InstanceComponentRequestData<global::PCL.ModMinecraft.McInstance>
        {
        }

        private class ComponentResult : global::PCLCS.InstanceComponentResultData<global::PCL.PageInstanceOverall.ComponentRequest, global::PCL.InstalledComponentSummary.Item>
        {
        }

        private global::PCLCS.InstanceComponentsController<global::PCL.ModMinecraft.McInstance, global::PCL.PageInstanceOverall.ComponentRequest, global::PCL.PageInstanceOverall.ComponentResult, global::PCL.InstalledComponentSummary.Item, global::PCL.MyCard, global::PCL.MyImage> ComponentController;
        private global::PCLCS.InstanceComponentsController<global::PCL.ModMinecraft.McInstance, global::PCL.PageInstanceOverall.ComponentRequest, global::PCL.PageInstanceOverall.ComponentResult, global::PCL.InstalledComponentSummary.Item, global::PCL.MyCard, global::PCL.MyImage> ComponentView
        {
            get
            {
                if (this.ComponentController is null)
                {
                    global::PCLCS.InstanceComponentsHost<global::PCL.ModMinecraft.McInstance, global::PCL.PageInstanceOverall.ComponentRequest, global::PCL.PageInstanceOverall.ComponentResult, global::PCL.InstalledComponentSummary.Item, global::PCL.MyCard, global::PCL.MyImage> @init = new global::PCLCS.InstanceComponentsHost<global::PCL.ModMinecraft.McInstance, global::PCL.PageInstanceOverall.ComponentRequest, global::PCL.PageInstanceOverall.ComponentResult, global::PCL.InstalledComponentSummary.Item, global::PCL.MyCard, global::PCL.MyImage>();
                    this.ComponentController = new global::PCLCS.InstanceComponentsController<global::PCL.ModMinecraft.McInstance, global::PCL.PageInstanceOverall.ComponentRequest, global::PCL.PageInstanceOverall.ComponentResult, global::PCL.InstalledComponentSummary.Item, global::PCL.MyCard, global::PCL.MyImage>((@init.Instance = new Func<global::PCL.ModMinecraft.McInstance>(() => global::PCL.PageInstanceLeft.Instance), @init.Modpack = new Func<global::System.Boolean>(() => global::PCL.ModModpackMode.Enabled), @init.NextRevision = new Func<global::System.Int32>(() => global::PCLCS.InstanceComponentRevision.Next(ref this.ComponentRefreshId)), @init.Revision = new Func<global::System.Int32>(() => this.ComponentRefreshId), @init.CreateRequest = new Func<global::PCL.PageInstanceOverall.ComponentRequest>(() => new global::PCL.PageInstanceOverall.ComponentRequest()), @init.CreateResult = new Func<global::PCL.PageInstanceOverall.ComponentResult>(() => new global::PCL.PageInstanceOverall.ComponentResult()), @init.HasSavedIsolation = new Func<global::PCL.ModMinecraft.McInstance, global::System.Boolean>(Target => global::PCL.Settings.HasSaved("VersionArgumentIndieV2", Target)), @init.Isolation = new Func<global::PCL.ModMinecraft.McInstance, global::System.Boolean>(Target => global::PCL.Settings.Get<bool>("VersionArgumentIndieV2", Target)), @init.VersionFolder = new Func<global::PCL.ModMinecraft.McInstance, global::System.String>(Target => Target.PathVersion), @init.MinecraftFolder = new Func<global::System.String>(() => global::PCL.ModMinecraft.McFolderSelected), @init.ImageRoot = new Func<global::System.String>(() => global::PCL.ModBase.PathImage), @init.Version = new Func<global::PCL.ModMinecraft.McInstance, global::PCLCS.McVersion>(Target => Target.Version), @init.Components = new Func<global::PCL.ModMinecraft.McInstance, global::PCLCS.McComponents>(Target => global::PCLCS.Configs.McInstallManifest.Get(Target.Config)?.Components), @init.Start = new Action<global::PCL.PageInstanceOverall.ComponentRequest, global::System.Boolean>((Request, Force) => this.ComponentsLoader.Start(Request, IsForceRestart: Force)), @init.Cancel = new Action(() => this.ComponentsLoader.Cancel()), @init.Warn = new Action<global::System.Exception, global::System.String>((Err, Message) => global::MeloongCore.Logger.Warn(Err, Message)), @init.Cards = PanInstalledComponents, @init.CreateImage = new Func<global::System.String, global::PCL.MyImage>(Source => new global::PCL.MyImage() { Source = Source }), @init.SetStretch = new Action<global::PCL.MyImage, global::System.Windows.Media.Stretch>((Image, Stretch) => Image.Stretch = Stretch), @init.CreateCard = new Func<global::System.String, global::PCL.MyCard>(Title => new global::PCL.MyCard() { Title = Title }), @init.TouchTitle = new Action<global::PCL.MyCard>(Card =>
                    {
                        var Title = Card.MainTextBlock;
                    }), @init).@init, global::PCL.InstalledComponentSummary.Engine);
                }

                return this.ComponentController;
            }
        }

        public PageInstanceOverall()
        {
            __LfAttachOwnHandles();
            ComponentsLoader = new global::PCL.ModLoader.LoaderTask<global::PCL.PageInstanceOverall.ComponentRequest, global::PCL.PageInstanceOverall.ComponentResult>("读取已安装组件", this.ReadInstalledComponents);
            this.IsLoad = false;
            this.OriginalHeaderInfo = "";
            this.OverallView = new global::PCLCS.InstanceOverallPageView<global::PCL.MyListItem>();
            this.InitializeComponent();
        }

        private static global::PCLCS.InstanceComponentsTask<global::PCL.PageInstanceOverall.ComponentRequest, global::PCL.PageInstanceOverall.ComponentResult> ComponentTask(global::PCL.ModLoader.LoaderTask<global::PCL.PageInstanceOverall.ComponentRequest, global::PCL.PageInstanceOverall.ComponentResult> Loader)
        {
            return new global::PCLCS.InstanceComponentsTask<global::PCL.PageInstanceOverall.ComponentRequest, global::PCL.PageInstanceOverall.ComponentResult>()
            {
                Input = new Func<global::PCL.PageInstanceOverall.ComponentRequest>(() => Loader.Input),
                Output = new Func<global::PCL.PageInstanceOverall.ComponentResult>(() => Loader.Output),
                SetOutput = new Action<global::PCL.PageInstanceOverall.ComponentResult>(Value => Loader.Output = Value),
                IsCanceled = new Func<global::System.Boolean>(() => Loader.IsCanceled)
            };
        }

        private void ComponentsLoaderInit()
        {
            this.PageLoaderInit(this.LoadComponents, this.PanComponentsLoad, this.PanBack, (global::System.Windows.FrameworkElement)null, this.ComponentsLoader, this.InstalledComponentsLoaded, AutoRun: false);
        }

        private void RefreshInstalledComponents()
        {
            this.ComponentView.Refresh();
        }

        private void ReadInstalledComponents(global::PCL.ModLoader.LoaderTask<global::PCL.PageInstanceOverall.ComponentRequest, global::PCL.PageInstanceOverall.ComponentResult> Loader)
        {
            this.ComponentView.Read(global::PCL.PageInstanceOverall.ComponentTask(Loader));
        }

        private void InstalledComponentsLoaded(global::PCL.ModLoader.LoaderTask<global::PCL.PageInstanceOverall.ComponentRequest, global::PCL.PageInstanceOverall.ComponentResult> Loader)
        {
            this.ComponentView.Loaded(global::PCL.PageInstanceOverall.ComponentTask(Loader));
        }

        internal void ShowInstalledComponents(global::System.Collections.Generic.IEnumerable<global::PCL.InstalledComponentSummary.Item> Items)
        {
            this.ComponentView.Show(Items);
        }
    }
}
