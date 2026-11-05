using System;
using System.Collections;
using System.Collections.Concurrent;
using global::System.Collections.Generic;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using global::System.Linq;
using System.Linq;
using System.Net;
using global::System.Net.Http;
using System.Net.Http;
using System.Runtime.CompilerServices;
using global::System.Text;
using System.Text;
using global::System.Threading;
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

    // LF 原类型、公开入口与 Loader/实例/平台服务适配；完整下载决策及解析在 C# 中。
    public static class ModDownload
    {
        private readonly static global::PCLCS.MinecraftDownloadSources Sources = new global::PCLCS.MinecraftDownloadSources(global::PCL.ModDownload.CreateSourceHost());
        private readonly static global::PCLCS.MinecraftDownloadCatalog<global::PCL.ModDownload.DlOptiFineListEntry, global::PCL.ModDownload.DlForgeVersionEntry, global::PCL.ModDownload.DlNeoForgeListEntry, global::PCL.ModDownload.DlLiteLoaderListEntry> Catalog = new global::PCLCS.MinecraftDownloadCatalog<global::PCL.ModDownload.DlOptiFineListEntry, global::PCL.ModDownload.DlForgeVersionEntry, global::PCL.ModDownload.DlNeoForgeListEntry, global::PCL.ModDownload.DlLiteLoaderListEntry>(global::PCL.ModDownload.Sources, new global::PCLCS.MinecraftDownloadCatalogHost<global::PCL.ModDownload.DlOptiFineListEntry, global::PCL.ModDownload.DlForgeVersionEntry, global::PCL.ModDownload.DlNeoForgeListEntry, global::PCL.ModDownload.DlLiteLoaderListEntry>()
        {
            Forge = new Func<global::System.String, global::System.String, global::System.String, global::PCL.ModDownload.DlForgeVersionEntry>((Version, Branch, Inherit) => new global::PCL.ModDownload.DlForgeVersionEntry(Version, Branch, Inherit)),
            NeoForge = new Func<global::System.String, global::PCL.ModDownload.DlNeoForgeListEntry>(ApiName => new global::PCL.ModDownload.DlNeoForgeListEntry(ApiName)),
            OptiFine = new Func<global::PCL.ModDownload.DlOptiFineListEntry>(() => new global::PCL.ModDownload.DlOptiFineListEntry()),
            LiteLoader = new Func<global::PCL.ModDownload.DlLiteLoaderListEntry>(() => new global::PCL.ModDownload.DlLiteLoaderListEntry())
        });
        private readonly static global::PCLCS.MinecraftDownloadClient<global::PCL.ModMinecraft.McInstance, global::PCL.ModNet.NetFile, global::PCL.ModLoader.LoaderBase, global::MeloongCore.FileChecker> Client = new global::PCLCS.MinecraftDownloadClient<global::PCL.ModMinecraft.McInstance, global::PCL.ModNet.NetFile, global::PCL.ModLoader.LoaderBase, global::MeloongCore.FileChecker>(global::PCL.ModDownload.Sources, global::PCL.ModDownload.CreateClientHost());

        private static global::PCLCS.MinecraftDownloadSourceHost CreateSourceHost()
        {
            return new global::PCLCS.MinecraftDownloadSourceHost()
            {
                SettingInteger = new Func<global::System.String, global::System.Int32>(Name => global::PCL.Settings.Get<int>(Name)),
                SettingString = new Func<global::System.String, global::System.String>(Name => global::PCL.Settings.Get<string>(Name)),
                SetSetting = new Action<global::System.String, global::System.String>((Name, Value) => global::PCL.Settings.Set(Name, Value)),
                Request = new Func<global::PCLCS.MinecraftDownloadRequest, global::System.String>(Request => global::PCL.ModNet.NetRequestByClient(Request.Url, Request.Method, Request.Content, Request.ContentType, Accept: Request.Accept, Timeout: Request.Timeout, RequireJson: Request.RequireJson, Encoding: Request.Encoding, SimulateBrowserHeaders: Request.SimulateBrowserHeaders)),
                RetryRequest = new Func<global::PCLCS.MinecraftDownloadRequest, global::System.String>(Request => global::PCL.ModNet.NetRequestByClientRetry(Request.Url, Request.Method, Content: Request.Content, ContentType: Request.ContentType, Accept: Request.Accept, RequireJson: Request.RequireJson, Encoding: Request.Encoding, SimulateBrowserHeaders: Request.SimulateBrowserHeaders)),
                LoaderRequest = new Func<global::System.String, global::System.Boolean, global::System.String>((Url, Browser) => global::PCL.ModNet.NetRequestByLoader(Url, SimulateBrowserHeaders: Browser)),
                Clock = global::PCL.ModBase.GetTimeMs,
                Sleep = global::System.Threading.Thread.Sleep,
                VersionToDrop = global::PCLCS.McVersion.VersionToDrop,
                TempPath = new Func<global::System.String>(() => global::PCL.ModBase.PathTemp),
                FileExists = new Func<global::System.String, global::System.Boolean>(Name => global::MeloongCore.FileUtils.Exists(Name)),
                ReadJson = new Func<global::System.String, global::Newtonsoft.Json.Linq.JToken>(Name => global::MeloongCore.FileUtils.ReadAsJson(Name)),
                Info = new Action<global::System.String>(Message => global::MeloongCore.Logger.Info(Message)),
                Warn = new Action<global::System.String>(Message => global::MeloongCore.Logger.Warn(Message)),
                WarnException = new Action<global::System.Exception, global::System.String>((ErrorValue, Message) => global::MeloongCore.Logger.Warn(ErrorValue, Message)),
                ErrorDisplay = new Func<global::System.Exception, global::System.Boolean, global::System.String>((ErrorValue, Stack) => ErrorValue.GetDisplay(Stack)),
                ForgeRecommended = global::PCL.ModDownloadLib.McDownloadForgeRecommendedGet,
                GetDate = new Func<global::Newtonsoft.Json.Linq.JToken, global::System.DateTime>(Value => global::PCL.ModBase.GetDate((global::System.Int32)Value)),
                LocalTime = global::PCL.ModBase.GetLocalTime
            };
        }
        private static global::PCLCS.MinecraftDownloadClientHost<global::PCL.ModMinecraft.McInstance, global::PCL.ModNet.NetFile, global::PCL.ModLoader.LoaderBase, global::MeloongCore.FileChecker> CreateClientHost()
        {
            return new global::PCLCS.MinecraftDownloadClientHost<global::PCL.ModMinecraft.McInstance, global::PCL.ModNet.NetFile, global::PCL.ModLoader.LoaderBase, global::MeloongCore.FileChecker>()
            {
                InheritName = new Func<global::PCL.ModMinecraft.McInstance, global::System.String>(Instance => Instance.InheritName),
                CreateInstance = new Func<global::System.String, global::PCL.ModMinecraft.McInstance>(Name => new global::PCL.ModMinecraft.McInstance(Name)),
                JsonObject = new Func<global::PCL.ModMinecraft.McInstance, global::Newtonsoft.Json.Linq.JObject>(Instance => Instance.JsonObject),
                Name = new Func<global::PCL.ModMinecraft.McInstance, global::System.String>(Instance => Instance.Name),
                VersionPath = new Func<global::PCL.ModMinecraft.McInstance, global::System.String>(Instance => Instance.PathVersion),
                AssetIndex = new Func<global::PCL.ModMinecraft.McInstance, global::Newtonsoft.Json.Linq.JToken>(Instance => global::PCL.ModMinecraft.McAssetsGetIndex(Instance, true, true)),
                GameFolder = new Func<global::System.String>(() => global::PCL.ModMinecraft.McFolderSelected),
                TempPath = new Func<global::System.String>(() => global::PCL.ModBase.PathTemp),
                Checker = new Func<global::PCLCS.MinecraftDownloadCheck, global::MeloongCore.FileChecker>(Value => new global::MeloongCore.FileChecker() { MinSize = Value.MinSize, ActualSize = Value.ActualSize, Hash = Value.Hash, IsJson = Value.IsJson }),
                Check = new Func<global::MeloongCore.FileChecker, global::System.String, global::System.String>((Checker, Name) => Checker.Check(Name)),
                NetFile = new Func<global::System.Collections.Generic.IEnumerable<global::System.String>, global::System.String, global::MeloongCore.FileChecker, global::PCL.ModNet.NetFile>((Urls, Name, Checker) => new global::PCL.ModNet.NetFile(Urls, Name, Checker)),
                LocalPath = new Func<global::PCL.ModNet.NetFile, global::System.String>(File => File.LocalPath),
                LocalName = new Func<global::PCL.ModNet.NetFile, global::System.String>(File => File.LocalName),
                SetLocalPath = new Action<global::PCL.ModNet.NetFile, global::System.String>((File, Name) => File.LocalPath = Name),
                CheckFile = new Func<global::PCL.ModNet.NetFile, global::System.String, global::System.String>((File, Name) => File.Check.Check(Name)),
                LibraryFiles = global::PCL.ModMinecraft.McLibNetFilesFromInstance,
                IgnoreFileCheck = new Func<global::PCL.ModMinecraft.McInstance, global::System.Boolean>(Instance => global::Microsoft.VisualBasic.CompilerServices.Conversions.ToBoolean(global::PCL.ModMinecraft.ShouldIgnoreFileCheck(Instance))),
                AssetFiles = global::PCL.ModDownload.ReadAssetFiles,
                FileExists = new Func<global::System.String, global::System.Boolean>(Name => global::MeloongCore.FileUtils.Exists(Name)),
                LastWriteTime = new Func<global::System.String, global::System.DateTime>(Name => global::MeloongCore.FileUtils.GetInfo(Name).LastWriteTime),
                MoveFile = new Action<global::System.String, global::System.String>((SourceName, Target) => global::MeloongCore.FileUtils.Move(SourceName, Target)),
                Now = new Func<global::System.DateTime>(() => global::System.DateTime.Now),
                Task = global::PCL.ModDownload.NewFileTask,
                Download = global::PCL.ModDownload.NewFileDownload,
                Combo = global::PCL.ModDownload.NewFileCombo,
                Start = new Action<global::PCL.ModLoader.LoaderBase>(Loader => Loader.Start()),
                Info = new Action<global::System.String>(Message => global::MeloongCore.Logger.Info(Message)),
                Warn = new Action<global::System.String>(Message => global::MeloongCore.Logger.Warn(Message)),
                WarnNone = new Action<global::System.String>(Message => global::MeloongCore.Logger.Warn(Message, global::MeloongCore.LogBehavior.None)),
                WarnException = new Action<global::System.Exception, global::System.String>((ErrorValue, Message) => global::MeloongCore.Logger.Warn(ErrorValue, Message))
            };
        }
        private static global::System.Collections.Generic.List<global::PCL.ModNet.NetFile> ReadAssetFiles(global::PCL.ModMinecraft.McInstance Instance, global::System.Boolean CheckHash, global::PCL.ModLoader.LoaderBase Loader)
        {
            return global::PCL.ModMinecraft.McAssetsFixList(Instance, CheckHash, ref Loader);
        }
        private static global::PCL.ModLoader.LoaderBase NewFileTask(global::System.String Name, global::System.Action<global::PCLCS.MinecraftDownloadFileTask<global::PCL.ModNet.NetFile, global::PCL.ModLoader.LoaderBase>> Work, global::PCLCS.MinecraftDownloadLoaderSettings Options)
        {
            var Result = new global::PCL.ModLoader.LoaderTask<global::System.String, global::System.Collections.Generic.List<global::PCL.ModNet.NetFile>>(Name, Task => Work(new global::PCLCS.MinecraftDownloadFileTask<global::PCL.ModNet.NetFile, global::PCL.ModLoader.LoaderBase>() { Native = Task, SetOutput = new Action<global::System.Collections.Generic.List<global::PCL.ModNet.NetFile>>(Files => Task.Output = Files), Cancel = Task.Cancel }));
            if (Options.ProgressWeight.HasValue)
                Result.ProgressWeight = Options.ProgressWeight.Value;
            if (Options.Show.HasValue)
                Result.Show = Options.Show.Value;
            if (Options.Block.HasValue)
                Result.Block = Options.Block.Value;
            return Result;
        }
        private static global::PCL.ModLoader.LoaderBase NewFileDownload(global::System.String Name, global::System.Collections.Generic.List<global::PCL.ModNet.NetFile> Files, global::PCLCS.MinecraftDownloadLoaderSettings Options)
        {
            var Result = new global::PCL.ModNet.LoaderDownload(Name, (global::System.Collections.Generic.IEnumerable<global::PCL.ModNet.NetFile>)Files);
            if (Options.ProgressWeight.HasValue)
                Result.ProgressWeight = Options.ProgressWeight.Value;
            if (Options.Show.HasValue)
                Result.Show = Options.Show.Value;
            if (Options.Block.HasValue)
                Result.Block = Options.Block.Value;
            return Result;
        }
        private static global::PCL.ModLoader.LoaderBase NewFileCombo(global::System.String Name, global::System.Collections.Generic.List<global::PCL.ModLoader.LoaderBase> Steps, global::PCLCS.MinecraftDownloadLoaderSettings Options)
        {
            var Result = new global::PCL.ModLoader.LoaderCombo<global::System.String>(Name, (global::System.Collections.Generic.IEnumerable<global::PCL.ModLoader.LoaderBase>)Steps);
            if (Options.Block.HasValue)
                Result.Block = Options.Block.Value;
            if (Options.Show.HasValue)
                Result.Show = Options.Show.Value;
            if (Options.ProgressWeight.HasValue)
                Result.ProgressWeight = Options.ProgressWeight.Value;
            return Result;
        }
        private static global::PCLCS.MinecraftDownloadSourceTask<TOut> Wrap<TIn, TOut>(global::PCL.ModLoader.LoaderTask<TIn, TOut> Loader)
        {
            return new global::PCLCS.MinecraftDownloadSourceTask<TOut>()
            {
                Input = new Func<global::System.Object>(() => Loader.Input),
                ClearInput = new Action(() => Loader.Input = (TIn)(default(TIn))),
                Output = new Func<TOut>(() => Loader.Output),
                SetOutput = new Action<TOut>(Value => Loader.Output = Value),
                IsFailed = new Func<global::System.Boolean>(() => (Loader.State == global::PCL.ModBase.LoadState.Failed)),
                IsFinished = new Func<global::System.Boolean>(() => (Loader.State == global::PCL.ModBase.LoadState.Finished)),
                IsLoading = new Func<global::System.Boolean>(() => (Loader.State == global::PCL.ModBase.LoadState.Loading)),
                IsCanceled = new Func<global::System.Boolean>(() => Loader.IsCanceled),
                IsCanceledState = new Func<global::System.Boolean>(() => (Loader.State == global::PCL.ModBase.LoadState.Canceled)),
                IsWaiting = new Func<global::System.Boolean>(() => (Loader.State == global::PCL.ModBase.LoadState.Waiting)),
                IsForceRestarting = new Func<global::System.Boolean>(() => Loader.IsForceRestarting),
                Error = new Func<global::System.Exception>(() => Loader.Error),
                Start = new Action<global::System.Object, global::System.Boolean>((Input, Force) => Loader.Start(Input, Force)),
                WaitForExit = new Action<global::System.Object, global::System.Boolean>((Input, Force) => Loader.WaitForExit(Input, IsForceRestart: Force)),
                SetWaiting = new Action(() => Loader.State = global::PCL.ModBase.LoadState.Waiting),
                Cancel = Loader.Cancel
            };
        }

        public static global::PCL.ModNet.NetFile DlClientJarGet(global::PCL.ModMinecraft.McInstance Instance, global::System.Boolean ReturnNothingOnFileUseable)
        {
            return global::PCL.ModDownload.Client.DlClientJarGet(Instance, ReturnNothingOnFileUseable);
        }
        public static global::PCL.ModNet.NetFile DlClientAssetIndexGet(global::PCL.ModMinecraft.McInstance Instance)
        {
            return global::PCL.ModDownload.Client.DlClientAssetIndexGet(Instance);
        }
        public static global::System.Collections.Generic.List<global::PCL.ModLoader.LoaderBase> DlClientFix(global::PCL.ModMinecraft.McInstance Instance, global::System.Boolean CheckAssetsHash, global::System.Boolean DownloadAssetIndexInBackground)
        {
            return global::PCL.ModDownload.Client.DlClientFix(Instance, CheckAssetsHash, DownloadAssetIndexInBackground);
        }
        public static global::System.Collections.Generic.List<global::System.Int32> AllDrops
        {
            get
            {
                return global::PCL.ModDownload.Sources.AllDrops;
            }
            set
            {
                global::PCL.ModDownload.Sources.AllDrops = value;
            }
        }

        public struct DlClientListResult
        {
            /// <summary>
        /// 数据来源名称，如“Mojang”，“BMCLAPI”。
        /// </summary>
            public global::System.String SourceName;
            /// <summary>
        /// 是否为官方的实时数据。
        /// </summary>
            public global::System.Boolean IsOfficial;
            /// <summary>
        /// 获取到的 Json 数据。
        /// </summary>
            public global::Newtonsoft.Json.Linq.JObject Value;
        }

        public struct DlOptiFineListResult
        {
            /// <summary>
        /// 数据来源名称，如“Official”，“BMCLAPI”。
        /// </summary>
            public global::System.String SourceName;
            /// <summary>
        /// 是否为官方的实时数据。
        /// </summary>
            public global::System.Boolean IsOfficial;
            /// <summary>
        /// 获取到的数据。
        /// </summary>
            public global::System.Collections.Generic.List<global::PCL.ModDownload.DlOptiFineListEntry> Value;
        }

        public struct DlForgeListResult
        {
            /// <summary>
        /// 数据来源名称，如“Official”，“BMCLAPI”。
        /// </summary>
            public global::System.String SourceName;
            /// <summary>
        /// 是否为官方的实时数据。
        /// </summary>
            public global::System.Boolean IsOfficial;
            /// <summary>
        /// 获取到的数据。
        /// </summary>
            public global::System.Collections.Generic.List<global::System.String> Value;
        }

        public struct DlNeoForgeListResult
        {
            /// <summary>
        /// 数据来源名称，如“Official”，“BMCLAPI”。
        /// </summary>
            public global::System.String SourceName;
            /// <summary>
        /// 是否为官方的实时数据。
        /// </summary>
            public global::System.Boolean IsOfficial;
            /// <summary>
        /// 所有版本的列表。已经按从新到老排序。
        /// </summary>
            public global::System.Collections.Generic.List<global::PCL.ModDownload.DlNeoForgeListEntry> Value;
        }

        public struct DlLiteLoaderListResult
        {
            /// <summary>
        /// 数据来源名称，如“Official”，“BMCLAPI”。
        /// </summary>
            public global::System.String SourceName;
            /// <summary>
        /// 是否为官方的实时数据。
        /// </summary>
            public global::System.Boolean IsOfficial;
            /// <summary>
        /// 获取到的数据。
        /// </summary>
            public global::System.Collections.Generic.List<global::PCL.ModDownload.DlLiteLoaderListEntry> Value;
            /// <summary>
        /// 官方源的失败原因。若没有则为 Nothing。
        /// </summary>
            public global::System.Exception OfficialError;
        }

        public struct DlFabricListResult
        {
            /// <summary>
        /// 数据来源名称，如“Official”，“BMCLAPI”。
        /// </summary>
            public global::System.String SourceName;
            /// <summary>
        /// 是否为官方的实时数据。
        /// </summary>
            public global::System.Boolean IsOfficial;
            /// <summary>
        /// 获取到的数据。
        /// </summary>
            public global::Newtonsoft.Json.Linq.JObject Value;
        }

        public class DlOptiFineListEntry : global::PCLCS.MinecraftOptiFineEntryData
        {
        }
        public abstract class DlForgelikeEntry : global::PCLCS.MinecraftForgelikeEntryData, global::System.IComparable<global::PCL.ModDownload.DlForgelikeEntry>
        {
            public abstract override string LoaderName { get; }
            public abstract override string FileExtension { get; }
            public global::System.Int32 CompareTo(global::PCL.ModDownload.DlForgelikeEntry other)
            {
                return base.CompareTo(other);
            }
        }
        public class DlForgeVersionEntry : global::PCL.ModDownload.DlForgelikeEntry, global::PCLCS.IMinecraftForgeVersionFields
        {
            public global::System.String ReleaseTime;
            public global::System.String Hash = (global::System.String)null;
            public global::System.Boolean IsRecommended;
            public global::System.String Category;
            public global::System.String FileVersion;
            private string CoreReleaseTime
            {
                get
                {
                    return this.ReleaseTime;
                }
                set
                {
                    this.ReleaseTime = value;
                }
            }

            string global::PCLCS.IMinecraftForgeVersionFields.ReleaseTime { get => this.CoreReleaseTime; set => this.CoreReleaseTime = value; }
            private string CoreHash
            {
                get
                {
                    return this.Hash;
                }
                set
                {
                    this.Hash = value;
                }
            }

            string global::PCLCS.IMinecraftForgeVersionFields.Hash { get => this.CoreHash; set => this.CoreHash = value; }
            private bool CoreIsRecommended
            {
                get
                {
                    return this.IsRecommended;
                }
                set
                {
                    this.IsRecommended = value;
                }
            }

            bool global::PCLCS.IMinecraftForgeVersionFields.IsRecommended { get => this.CoreIsRecommended; set => this.CoreIsRecommended = value; }
            private string CoreCategory
            {
                get
                {
                    return this.Category;
                }
                set
                {
                    this.Category = value;
                }
            }

            string global::PCLCS.IMinecraftForgeVersionFields.Category { get => this.CoreCategory; set => this.CoreCategory = value; }
            private string CoreFileVersion
            {
                get
                {
                    return this.FileVersion;
                }
                set
                {
                    this.FileVersion = value;
                }
            }

            string global::PCLCS.IMinecraftForgeVersionFields.FileVersion { get => this.CoreFileVersion; set => this.CoreFileVersion = value; }
            public override string LoaderName
            {
                get
                {
                    return global::PCLCS.MinecraftDownloadEntries.ForgeLoaderName;
                }
            }
            public override string FileExtension
            {
                get
                {
                    return global::PCLCS.MinecraftDownloadEntries.ForgeFileExtension(this.Category);
                }
            }
            public DlForgeVersionEntry(global::System.String Version, global::System.String Branch, global::System.String Inherit)
            {
                global::PCLCS.MinecraftDownloadEntries.InitializeForge(this, Version, Branch, Inherit);
            }
        }
        public class DlNeoForgeListEntry : global::PCL.ModDownload.DlForgelikeEntry, global::PCLCS.IMinecraftNeoForgeFields
        {
            public global::System.Boolean IsBeta;
            public global::System.String ApiName;
            private bool CoreIsBeta
            {
                get
                {
                    return this.IsBeta;
                }
                set
                {
                    this.IsBeta = value;
                }
            }

            bool global::PCLCS.IMinecraftNeoForgeFields.IsBeta { get => this.CoreIsBeta; set => this.CoreIsBeta = value; }
            private string CoreApiName
            {
                get
                {
                    return this.ApiName;
                }
                set
                {
                    this.ApiName = value;
                }
            }

            string global::PCLCS.IMinecraftNeoForgeFields.ApiName { get => this.CoreApiName; set => this.CoreApiName = value; }
            public string UrlBase
            {
                get
                {
                    return global::PCLCS.MinecraftDownloadEntries.NeoForgeUrlBase(this, this.ApiName);
                }
            }
            public override string LoaderName
            {
                get
                {
                    return global::PCLCS.MinecraftDownloadEntries.NeoForgeLoaderName;
                }
            }
            public override string FileExtension
            {
                get
                {
                    return global::PCLCS.MinecraftDownloadEntries.JarExtension;
                }
            }
            public DlNeoForgeListEntry(global::System.String ApiName)
            {
                global::PCLCS.MinecraftDownloadEntries.InitializeNeoForge(this, ApiName);
            }
        }
        public class DlLiteLoaderListEntry : global::PCLCS.MinecraftLiteLoaderEntryData
        {
        }

        public static global::PCL.ModLoader.LoaderTask<global::System.String, global::PCL.ModDownload.DlClientListResult> DlClientListLoader = new global::PCL.ModLoader.LoaderTask<global::System.String, global::PCL.ModDownload.DlClientListResult>("DlClientList Main", global::PCL.ModDownload.DlClientListMain);
        private static void DlClientListMain(global::PCL.ModLoader.LoaderTask<global::System.String, global::PCL.ModDownload.DlClientListResult> Loader)
        {
            global::PCL.ModDownload.Sources.RunClientVersionList(Wrap(Loader), Wrap(global::PCL.ModDownload.DlClientListMojangLoader), Wrap(global::PCL.ModDownload.DlClientListBmclapiLoader), Value => Value.Value);
        }

        public static global::PCL.ModLoader.LoaderTask<global::System.String, global::PCL.ModDownload.DlClientListResult> DlClientListMojangLoader = new global::PCL.ModLoader.LoaderTask<global::System.String, global::PCL.ModDownload.DlClientListResult>("DlClientList Mojang", global::PCL.ModDownload.DlClientListMojangMain);
        private static void DlClientListMojangMain(global::PCL.ModLoader.LoaderTask<global::System.String, global::PCL.ModDownload.DlClientListResult> Loader)
        {
            var Result = global::PCL.ModDownload.Sources.DlClientListMojangMain();
            Loader.Output = new global::PCL.ModDownload.DlClientListResult() { SourceName = Result.SourceName, IsOfficial = Result.IsOfficial, Value = Result.Value };
        }

        public static global::PCL.ModLoader.LoaderTask<global::System.String, global::PCL.ModDownload.DlClientListResult> DlClientListBmclapiLoader = new global::PCL.ModLoader.LoaderTask<global::System.String, global::PCL.ModDownload.DlClientListResult>("DlClientList Bmclapi", global::PCL.ModDownload.DlClientListBmclapiMain);
        private static void DlClientListBmclapiMain(global::PCL.ModLoader.LoaderTask<global::System.String, global::PCL.ModDownload.DlClientListResult> Loader)
        {
            var Result = global::PCL.ModDownload.Sources.DlClientListBmclapiMain(Loader.Input, () => global::PCL.ModDownload.DlClientListLoader.Output.Value);
            Loader.Output = new global::PCL.ModDownload.DlClientListResult() { SourceName = Result.SourceName, IsOfficial = Result.IsOfficial, Value = Result.Value };
        }

        public static global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlOptiFineListResult> DlOptiFineListLoader = new global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlOptiFineListResult>("DlOptiFineList Main", global::PCL.ModDownload.DlOptiFineListMain);
        private static void DlOptiFineListMain(global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlOptiFineListResult> Loader)
        {
            global::PCL.ModDownload.Sources.RunVersionList(Wrap(Loader), Wrap(global::PCL.ModDownload.DlOptiFineListOfficialLoader), Wrap(global::PCL.ModDownload.DlOptiFineListBmclapiLoader));
        }

        public static global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlOptiFineListResult> DlOptiFineListOfficialLoader = new global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlOptiFineListResult>("DlOptiFineList Official", global::PCL.ModDownload.DlOptiFineListOfficialMain);
        private static void DlOptiFineListOfficialMain(global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlOptiFineListResult> Loader)
        {
            var Result = global::PCL.ModDownload.Catalog.DlOptiFineListOfficialMain();
            Loader.Output = new global::PCL.ModDownload.DlOptiFineListResult() { SourceName = Result.SourceName, IsOfficial = Result.IsOfficial, Value = Result.Value };
        }

        public static global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlOptiFineListResult> DlOptiFineListBmclapiLoader = new global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlOptiFineListResult>("DlOptiFineList Bmclapi", global::PCL.ModDownload.DlOptiFineListBmclapiMain);
        private static void DlOptiFineListBmclapiMain(global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlOptiFineListResult> Loader)
        {
            var Result = global::PCL.ModDownload.Catalog.DlOptiFineListBmclapiMain();
            Loader.Output = new global::PCL.ModDownload.DlOptiFineListResult() { SourceName = Result.SourceName, IsOfficial = Result.IsOfficial, Value = Result.Value };
        }

        public static global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlForgeListResult> DlForgeListLoader = new global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlForgeListResult>("DlForgeList Main", global::PCL.ModDownload.DlForgeListMain);
        private static void DlForgeListMain(global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlForgeListResult> Loader)
        {
            global::PCL.ModDownload.Sources.RunVersionList(Wrap(Loader), Wrap(global::PCL.ModDownload.DlForgeListOfficialLoader), Wrap(global::PCL.ModDownload.DlForgeListBmclapiLoader));
        }

        public static global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlForgeListResult> DlForgeListOfficialLoader = new global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlForgeListResult>("DlForgeList Official", global::PCL.ModDownload.DlForgeListOfficialMain);
        private static void DlForgeListOfficialMain(global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlForgeListResult> Loader)
        {
            var Result = global::PCL.ModDownload.Catalog.DlForgeListOfficialMain();
            Loader.Output = new global::PCL.ModDownload.DlForgeListResult() { SourceName = Result.SourceName, IsOfficial = Result.IsOfficial, Value = Result.Value };
        }

        public static global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlForgeListResult> DlForgeListBmclapiLoader = new global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlForgeListResult>("DlForgeList Bmclapi", global::PCL.ModDownload.DlForgeListBmclapiMain);
        private static void DlForgeListBmclapiMain(global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlForgeListResult> Loader)
        {
            var Result = global::PCL.ModDownload.Catalog.DlForgeListBmclapiMain();
            Loader.Output = new global::PCL.ModDownload.DlForgeListResult() { SourceName = Result.SourceName, IsOfficial = Result.IsOfficial, Value = Result.Value };
        }

        public static global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlNeoForgeListResult> DlNeoForgeListLoader = new global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlNeoForgeListResult>("DlNeoForgeList Main", global::PCL.ModDownload.DlNeoForgeListMain);
        private static void DlNeoForgeListMain(global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlNeoForgeListResult> Loader)
        {
            global::PCL.ModDownload.Sources.RunVersionList(Wrap(Loader), Wrap(global::PCL.ModDownload.DlNeoForgeListOfficialLoader), Wrap(global::PCL.ModDownload.DlNeoForgeListBmclapiLoader));
        }

        public static global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlNeoForgeListResult> DlNeoForgeListOfficialLoader = new global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlNeoForgeListResult>("DlNeoForgeList Official", global::PCL.ModDownload.DlNeoForgeListOfficialMain);
        private static void DlNeoForgeListOfficialMain(global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlNeoForgeListResult> Loader)
        {
            var Result = global::PCL.ModDownload.Catalog.DlNeoForgeListMain(false);
            Loader.Output = new global::PCL.ModDownload.DlNeoForgeListResult() { SourceName = Result.SourceName, IsOfficial = Result.IsOfficial, Value = Result.Value };
        }

        public static global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlNeoForgeListResult> DlNeoForgeListBmclapiLoader = new global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlNeoForgeListResult>("DlNeoForgeList Bmclapi", global::PCL.ModDownload.DlNeoForgeListBmclapiMain);
        public static void DlNeoForgeListBmclapiMain(global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlNeoForgeListResult> Loader)
        {
            var Result = global::PCL.ModDownload.Catalog.DlNeoForgeListMain(true);
            Loader.Output = new global::PCL.ModDownload.DlNeoForgeListResult() { SourceName = Result.SourceName, IsOfficial = Result.IsOfficial, Value = Result.Value };
        }

        public static global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlLiteLoaderListResult> DlLiteLoaderListLoader = new global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlLiteLoaderListResult>("DlLiteLoaderList Main", global::PCL.ModDownload.DlLiteLoaderListMain);
        private static void DlLiteLoaderListMain(global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlLiteLoaderListResult> Loader)
        {
            global::PCL.ModDownload.Sources.RunVersionList(Wrap(Loader), Wrap(global::PCL.ModDownload.DlLiteLoaderListOfficialLoader), Wrap(global::PCL.ModDownload.DlLiteLoaderListBmclapiLoader));
        }

        public static global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlLiteLoaderListResult> DlLiteLoaderListOfficialLoader = new global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlLiteLoaderListResult>("DlLiteLoaderList Official", global::PCL.ModDownload.DlLiteLoaderListOfficialMain);
        private static void DlLiteLoaderListOfficialMain(global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlLiteLoaderListResult> Loader)
        {
            var Result = global::PCL.ModDownload.Catalog.DlLiteLoaderListMain(false);
            Loader.Output = new global::PCL.ModDownload.DlLiteLoaderListResult() { SourceName = Result.SourceName, IsOfficial = Result.IsOfficial, Value = Result.Value };
        }

        public static global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlLiteLoaderListResult> DlLiteLoaderListBmclapiLoader = new global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlLiteLoaderListResult>("DlLiteLoaderList Bmclapi", global::PCL.ModDownload.DlLiteLoaderListBmclapiMain);
        private static void DlLiteLoaderListBmclapiMain(global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlLiteLoaderListResult> Loader)
        {
            var Result = global::PCL.ModDownload.Catalog.DlLiteLoaderListMain(true);
            Loader.Output = new global::PCL.ModDownload.DlLiteLoaderListResult() { SourceName = Result.SourceName, IsOfficial = Result.IsOfficial, Value = Result.Value };
        }

        public static global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlFabricListResult> DlFabricListLoader = new global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlFabricListResult>("DlFabricList Main", global::PCL.ModDownload.DlFabricListMain);
        private static void DlFabricListMain(global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlFabricListResult> Loader)
        {
            global::PCL.ModDownload.Sources.RunVersionList(Wrap(Loader), Wrap(global::PCL.ModDownload.DlFabricListOfficialLoader), Wrap(global::PCL.ModDownload.DlFabricListBmclapiLoader));
        }

        public static global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlFabricListResult> DlFabricListOfficialLoader = new global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlFabricListResult>("DlFabricList Official", global::PCL.ModDownload.DlFabricListOfficialMain);
        private static void DlFabricListOfficialMain(global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlFabricListResult> Loader)
        {
            var Result = global::PCL.ModDownload.Catalog.DlFabricListMain(false);
            Loader.Output = new global::PCL.ModDownload.DlFabricListResult() { SourceName = Result.SourceName, IsOfficial = Result.IsOfficial, Value = Result.Value };
        }

        public static global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlFabricListResult> DlFabricListBmclapiLoader = new global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlFabricListResult>("DlFabricList Bmclapi", global::PCL.ModDownload.DlFabricListBmclapiMain);
        private static void DlFabricListBmclapiMain(global::PCL.ModLoader.LoaderTask<global::System.Int32, global::PCL.ModDownload.DlFabricListResult> Loader)
        {
            var Result = global::PCL.ModDownload.Catalog.DlFabricListMain(true);
            Loader.Output = new global::PCL.ModDownload.DlFabricListResult() { SourceName = Result.SourceName, IsOfficial = Result.IsOfficial, Value = Result.Value };
        }

        public static global::System.Object DlClientListGet(global::System.String Id)
        {
            return global::PCL.ModDownload.Sources.DlClientListGet(Id, Wrap(global::PCL.ModDownload.DlClientListLoader), Value => Value.Value);
        }
        public static void DlForgeVersionMain(global::PCL.ModLoader.LoaderTask<global::System.String, global::System.Collections.Generic.List<global::PCL.ModDownload.DlForgeVersionEntry>> Loader)
        {
            var Official = new global::PCL.ModLoader.LoaderTask<global::System.String, global::System.Collections.Generic.List<global::PCL.ModDownload.DlForgeVersionEntry>>("DlForgeVersion Official", global::PCL.ModDownload.DlForgeVersionOfficialMain);
            var Mirror = new global::PCL.ModLoader.LoaderTask<global::System.String, global::System.Collections.Generic.List<global::PCL.ModDownload.DlForgeVersionEntry>>("DlForgeVersion Bmclapi", global::PCL.ModDownload.DlForgeVersionBmclapiMain);
            global::PCL.ModDownload.Sources.RunVersionList(Wrap(Loader), Wrap(Official), Wrap(Mirror));
        }
        public static void DlForgeVersionOfficialMain(global::PCL.ModLoader.LoaderTask<global::System.String, global::System.Collections.Generic.List<global::PCL.ModDownload.DlForgeVersionEntry>> Loader)
        {
            Loader.Output = global::PCL.ModDownload.Catalog.DlForgeVersionOfficialMain(Loader.Input);
        }
        public static void DlForgeVersionBmclapiMain(global::PCL.ModLoader.LoaderTask<global::System.String, global::System.Collections.Generic.List<global::PCL.ModDownload.DlForgeVersionEntry>> Loader)
        {
            Loader.Output = global::PCL.ModDownload.Catalog.DlForgeVersionBmclapiMain(Loader.Input);
        }
        private static global::System.Collections.Generic.List<global::PCL.ModDownload.DlNeoForgeListEntry> GetNeoForgeEntries(global::System.String LatestJson, global::System.String LatestLegacyJson)
        {
            return global::PCL.ModDownload.Catalog.GetNeoForgeEntries(LatestJson, LatestLegacyJson);
        }
        public static global::PCL.ModLoader.LoaderTask<global::System.Int32, global::System.Collections.Generic.List<global::PCL.ResourceVersion>> DlFabricApiLoader = new global::PCL.ModLoader.LoaderTask<global::System.Int32, global::System.Collections.Generic.List<global::PCL.ResourceVersion>>("Fabric API List Loader", Task => Task.Output = global::PCL.ModDownload.Catalog.DlFabricApiList((Id, Platform, Dependencies) => global::PCL.ResourceVersion.FromProjectId(Id, Platform, Dependencies)));
        public static global::PCL.ModLoader.LoaderTask<global::System.Int32, global::System.Collections.Generic.List<global::PCL.ResourceVersion>> DlOptiFabricLoader = new global::PCL.ModLoader.LoaderTask<global::System.Int32, global::System.Collections.Generic.List<global::PCL.ResourceVersion>>("OptiFabric List Loader", Task => Task.Output = global::PCL.ModDownload.Catalog.DlOptiFabricList((Id, Platform, Dependencies) => global::PCL.ResourceVersion.FromProjectId(Id, Platform, Dependencies)));

        public static global::Newtonsoft.Json.Linq.JToken DlModRequest(global::System.String Url, global::System.Net.Http.HttpMethod Method = null, global::System.String Content = null, global::System.String ContentType = null)
        {
            return global::PCL.ModDownload.Sources.DlModRequest(Url, Method, Content, ContentType);
        }
        public static bool DlSourcePreferMojang
        {
            get
            {
                return global::PCL.ModDownload.Sources.DlSourcePreferMojang;
            }
        }
        public static global::System.Collections.Generic.IEnumerable<global::System.String> DlSourceOrder(global::System.Collections.Generic.IEnumerable<global::System.String> OfficialUrls, global::System.Collections.Generic.IEnumerable<global::System.String> MirrorUrls)
        {
            return global::PCL.ModDownload.Sources.DlSourceOrder(OfficialUrls, MirrorUrls);
        }
        public static bool DlVersionListPreferMojang
        {
            get
            {
                return global::PCL.ModDownload.Sources.DlVersionListPreferMojang;
            }
        }
        public static global::System.Collections.Generic.IEnumerable<global::System.String> DlVersionListOrder(global::System.Collections.Generic.IEnumerable<global::System.String> OfficialUrls, global::System.Collections.Generic.IEnumerable<global::System.String> MirrorUrls)
        {
            return global::PCL.ModDownload.Sources.DlVersionListOrder(OfficialUrls, MirrorUrls);
        }
        public static global::System.Collections.Generic.IEnumerable<global::System.String> DlSourceAssetsGet(global::System.String Original)
        {
            return global::PCL.ModDownload.Sources.DlSourceAssetsGet(Original);
        }
        public static global::System.Collections.Generic.IEnumerable<global::System.String> DlSourceLibraryGet(global::System.String Original)
        {
            return global::PCL.ModDownload.Sources.DlSourceLibraryGet(Original);
        }
        public static global::System.Collections.Generic.IEnumerable<global::System.String> DlSourceLauncherOrMetaGet(global::System.String Original)
        {
            return global::PCL.ModDownload.Sources.DlSourceLauncherOrMetaGet(Original);
        }
        public static global::System.String DlSourceModGet(global::System.String Original)
        {
            return global::PCL.ModDownload.Sources.DlSourceModGet(Original);
        }
        private static void DlSourceLoader<InputType, OutputType>(global::PCL.ModLoader.LoaderTask<InputType, OutputType> MainLoader, global::System.Collections.Generic.List<global::System.Collections.Generic.KeyValuePair<global::PCL.ModLoader.LoaderTask<InputType, OutputType>, global::System.Int32>> LoaderList, global::System.Boolean IsForceRestart = false)
        {
            global::PCL.ModDownload.Sources.DlSourceLoader(Wrap(MainLoader), LoaderList.Select(Item => new KeyValuePair<global::PCLCS.MinecraftDownloadSourceTask<OutputType>, int>(Wrap(Item.Key), Item.Value)).ToList(), IsForceRestart);
        }
        private static void DlSourceLoaderCancel<InputType, OutputType>(global::System.Collections.Generic.List<global::System.Collections.Generic.KeyValuePair<global::PCL.ModLoader.LoaderTask<InputType, OutputType>, global::System.Int32>> LoaderList)
        {
            global::PCL.ModDownload.Sources.DlSourceLoaderCancel(LoaderList.Select(Item => new KeyValuePair<global::PCLCS.MinecraftDownloadSourceTask<OutputType>, int>(Wrap(Item.Key), Item.Value)).ToList());
        }
    }
}