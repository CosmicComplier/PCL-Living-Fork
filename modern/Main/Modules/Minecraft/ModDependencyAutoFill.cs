using System;
using System.Collections;
using System.Collections.Concurrent;
using global::System.Collections.Generic;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
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
using global::Newtonsoft.Json.Linq;
using Newtonsoft.Json.Linq;
using Inspector = global::PCL.ModDependencyInspector;
using PCLCS;
using AutoFillCore = global::PCLCS.ModDependencyAutoFillEngine<global::PCL.ModDependencyInspector.InspectionContext, global::PCL.ModDependencyInspector.ModInfo, global::PCL.ModDependencyInspector.Dependency, global::PCL.ModDependencyInspector.DependencyEdge, global::PCL.ModDependencyInspector.InspectionIssue, global::PCL.ModDependencyInspector.RemovalImpact, global::PCL.ModDependencyInspector.InspectionReport, global::PCL.ModDependencyInspector.OnlineProjectInfo, global::PCL.ModDependencyInspector.OnlineDependency, global::PCL.ModDependencyInspector.OnlineFileInfo, global::PCL.ResourceVersion, global::PCL.ModNet.NetFile, global::PCL.ModDependencyAutoFill.Item, global::PCL.ModDependencyAutoFill.Plan, global::PCL.ModDependencyActivation.Item, global::PCL.ModDependencyOnline.LookupOptions, global::PCL.ModDependencyOnline.LookupCache>;
using ThrottleDebounce;

namespace PCL
{

    // 原 LF 对话框、版本模型、NetFile 与 Loader 的适配；规划、复查和事务在 C# 中执行。
    internal sealed class ModDependencyAutoFill
    {
        internal class Item : global::PCLCS.ModDependencyAutoFillItemData<global::PCL.ResourceVersion, global::PCL.ModDependencyAutoFill.Item>
        {
        }
        internal class Plan : global::PCLCS.ModDependencyAutoFillPlanData<global::PCL.ModDependencyInspector.InspectionReport, global::PCL.ModDependencyAutoFill.Item, global::PCL.ModDependencyActivation.Item>
        {
        }
        internal static readonly global::PCLCS.ModDependencyAutoFillEngine<global::PCL.ModDependencyInspector.InspectionContext, global::PCL.ModDependencyInspector.ModInfo, global::PCL.ModDependencyInspector.Dependency, global::PCL.ModDependencyInspector.DependencyEdge, global::PCL.ModDependencyInspector.InspectionIssue, global::PCL.ModDependencyInspector.RemovalImpact, global::PCL.ModDependencyInspector.InspectionReport, global::PCL.ModDependencyInspector.OnlineProjectInfo, global::PCL.ModDependencyInspector.OnlineDependency, global::PCL.ModDependencyInspector.OnlineFileInfo, global::PCL.ResourceVersion, global::PCL.ModNet.NetFile, global::PCL.ModDependencyAutoFill.Item, global::PCL.ModDependencyAutoFill.Plan, global::PCL.ModDependencyActivation.Item, global::PCL.ModDependencyOnline.LookupOptions, global::PCL.ModDependencyOnline.LookupCache> Service = new global::PCLCS.ModDependencyAutoFillEngine<global::PCL.ModDependencyInspector.InspectionContext, global::PCL.ModDependencyInspector.ModInfo, global::PCL.ModDependencyInspector.Dependency, global::PCL.ModDependencyInspector.DependencyEdge, global::PCL.ModDependencyInspector.InspectionIssue, global::PCL.ModDependencyInspector.RemovalImpact, global::PCL.ModDependencyInspector.InspectionReport, global::PCL.ModDependencyInspector.OnlineProjectInfo, global::PCL.ModDependencyInspector.OnlineDependency, global::PCL.ModDependencyInspector.OnlineFileInfo, global::PCL.ResourceVersion, global::PCL.ModNet.NetFile, global::PCL.ModDependencyAutoFill.Item, global::PCL.ModDependencyAutoFill.Plan, global::PCL.ModDependencyActivation.Item, global::PCL.ModDependencyOnline.LookupOptions, global::PCL.ModDependencyOnline.LookupCache>(global::PCL.ModDependencyAutoFill.CreateHost());

        private class CandidateMismatchException : global::PCLCS.DependencyCandidateMismatchException
        {
            internal CandidateMismatchException(global::System.Int32 Index, global::System.String Message) : base(Index, Message)
            {
            }
        }

        private static global::PCLCS.ModDependencyAutoFillHost<global::PCL.ModDependencyInspector.InspectionReport, global::PCL.ResourceVersion, global::PCL.ModNet.NetFile, global::PCL.ModDependencyAutoFill.Plan, global::PCL.ModDependencyOnline.LookupOptions> CreateHost()
        {
            global::PCLCS.ModDependencyAutoFillHost<global::PCL.ModDependencyInspector.InspectionReport, global::PCL.ResourceVersion, global::PCL.ModNet.NetFile, global::PCL.ModDependencyAutoFill.Plan, global::PCL.ModDependencyOnline.LookupOptions> @init = new global::PCLCS.ModDependencyAutoFillHost<global::PCL.ModDependencyInspector.InspectionReport, global::PCL.ResourceVersion, global::PCL.ModNet.NetFile, global::PCL.ModDependencyAutoFill.Plan, global::PCL.ModDependencyOnline.LookupOptions>();
            return (@init.ParseVersion = new Func<global::Newtonsoft.Json.Linq.JObject, global::PCL.ResourceVersion>(Raw => global::PCL.ResourceVersion.FromPlatformJson(Raw, global::PCLCS.ResourceTypes.Mod)), @init.ResourceType = new Func<global::PCL.ResourceVersion, global::PCLCS.ResourceTypes>(Value => Value.ResourceType), @init.DownloadAvailable = new Func<global::PCL.ResourceVersion, global::System.Boolean>(Value => Value.DownloadAvailable), @init.Size = new Func<global::PCL.ResourceVersion, global::System.Int64>(Value => Value.Size), @init.GameVersions = new Func<global::PCL.ResourceVersion, global::System.Collections.Generic.IEnumerable<global::System.String>>(Value => Value.GameVersions), @init.ModLoaders = new Func<global::PCL.ResourceVersion, global::PCLCS.ModLoaders>(Value => Value.ModLoaders), @init.SetModLoaders = new Action<global::PCL.ResourceVersion, global::PCLCS.ModLoaders>((Value, Loaders) => Value.ModLoaders = Loaders), @init.Id = new Func<global::PCL.ResourceVersion, global::System.String>(Value => Value.Id), @init.FileName = new Func<global::PCL.ResourceVersion, global::System.String>(Value => Value.FileName), @init.Hash = new Func<global::PCL.ResourceVersion, global::System.String>(Value => Value.Hash), @init.Display = new Func<global::PCL.ResourceVersion, global::System.String>(Value => Value.Display), @init.DownloadUrls = new Func<global::PCL.ResourceVersion, global::System.Collections.Generic.List<global::System.String>>(Value => Value.DownloadUrls), @init.SetDownloadUrls = new Action<global::PCL.ResourceVersion, global::System.Collections.Generic.List<global::System.String>>((Value, Urls) => Value.DownloadUrls = Urls), @init.ReleaseType = new Func<global::PCL.ResourceVersion, global::System.Int32>(Value => (global::System.Int32)Value.ReleaseType), @init.ReleaseDate = new Func<global::PCL.ResourceVersion, global::System.DateTime>(Value => Value.ReleaseDate), @init.ToNetFile = new Func<global::PCL.ResourceVersion, global::System.String, global::System.String, global::PCLCS.ModLoaders, global::PCL.ModNet.NetFile>((Value, FileName, GameVersion, Loaders) => Value.ToNetFile(FileName, global::PCL.ResourceVersion.DownloadReason.Dependency, GameVersion, Loaders)), @init.Enrich = new Action<global::PCL.ModDependencyInspector.InspectionReport, global::System.String, global::System.Threading.CancellationToken, global::PCL.ModDependencyOnline.LookupOptions>((Report, Root, Token, Options) => global::PCL.ModDependencyOnline.Enrich(Report, Root, Token, Options)), @init.DefaultRequest = global::PCL.ModDependencyOnline.DefaultRequest, @init.SafeName = new Func<global::System.String, global::System.String>(Name => global::PCL.InstanceResourceStore.SafeName(Name)), @init.EnsureWithin = new Action<global::System.String, global::System.String>((Root, Name) => global::PCL.InstanceResourceStore.EnsureWithin(Root, Name)), @init.DeleteStage = new Action<global::System.String, global::System.String>((Root, Stage) => global::PCL.InstanceResourceStore.DeleteStage(Root, Stage)), @init.FileHash = new Func<global::System.String, global::System.Boolean, global::System.String>((Name, Md5) => global::PCL.InstanceResourceUpdates.FileHash(Name, Md5)), @init.Download = global::PCL.ModDependencyAutoFill.DownloadFiles, @init.Info = new Action<global::System.String>(Message => global::MeloongCore.Logger.Info(Message)), @init.Warn = new Action<global::System.Exception, global::System.String>((ErrorValue, Message) => global::MeloongCore.Logger.Warn(ErrorValue, Message)), @init.RefreshOptions = new Action(() => global::PCL.ModBase.RunInUi(() => global::PCL.ModMain.FrmLaunchRight?.RefreshDependencyOptions())), @init.CandidateMismatch = new Func<global::System.Int32, global::System.String, global::PCLCS.DependencyCandidateMismatchException>((Index, Message) => new global::PCL.ModDependencyAutoFill.CandidateMismatchException(Index, Message)), @init).@init;
        }

        internal static global::PCL.ModDependencyAutoFill.Plan BuildPlan(global::System.String Root, global::PCL.ModDependencyInspector.InspectionReport Report, global::System.Threading.CancellationToken Token, global::PCL.ModDependencyOnline.LookupOptions Options = null)
        {
            return global::PCL.ModDependencyAutoFill.Service.BuildPlan(Root, Report, Token, Options);
        }
        internal static global::System.Int32 Install(global::PCL.ModDependencyAutoFill.Plan Plan, global::System.Threading.CancellationToken Token, global::System.Action<global::System.Collections.Generic.List<global::PCL.ModNet.NetFile>, global::System.Threading.CancellationToken> Download = null)
        {
            return global::PCL.ModDependencyAutoFill.Service.Install(Plan, Token, Download);
        }
        internal static global::PCL.ModLoader.LoaderCombo<global::System.Int32> CreateDownloadTask(global::PCL.ModDependencyAutoFill.Plan Plan, global::System.Collections.Generic.List<global::PCL.ModNet.NetFile> Files)
        {
            var Batch = global::PCL.ModDependencyAutoFill.Service.CreateDownloadTask(Plan, Files);
            var Steps = new global::System.Collections.Generic.List<global::PCL.ModLoader.LoaderBase>();
            foreach (var Entry in Batch.Steps)
                Steps.Add(new global::PCL.ModNet.LoaderDownload(Entry.Name, new[] { Entry.File }) { Block = Entry.Block, ProgressWeight = Entry.ProgressWeight });
            return new global::PCL.ModLoader.LoaderCombo<global::System.Int32>(Batch.Name, (global::System.Collections.Generic.IEnumerable<global::PCL.ModLoader.LoaderBase>)Steps) { OnStateChanged = global::PCL.ModDownloadLib.LoaderStateChangedHintOnly };
        }
        private static void DownloadFiles(global::PCL.ModDependencyAutoFill.Plan Plan, global::System.Collections.Generic.List<global::PCL.ModNet.NetFile> Files, global::System.Threading.CancellationToken Token)
        {
            if (global::PCL.ModMain.FrmMain is null)
            {
                var Work = new global::PCL.ModNet.LoaderDownload("下载必需前置", (global::System.Collections.Generic.IEnumerable<global::PCL.ModNet.NetFile>)Files);
                using (var Registration = Token.Register(Work.Cancel))
                {
                    Work.WaitForExit(c: Token);
                }
            }
            else
            {
                var Work = global::PCL.ModDependencyAutoFill.CreateDownloadTask(Plan, Files);
                global::PCL.ModBase.RunInUiWait(() =>
                    {
                        Work.Start(1);
                        global::PCL.ModLoader.LoaderTaskbarAdd(Work);
                        global::PCL.ModMain.FrmMain.BtnExtraDownload.ShowRefresh();
                        global::PCL.ModMain.FrmMain.BtnExtraDownload.Ribble();
                    });
                using (var Registration = Token.Register(Work.Cancel))
                {
                    Work.WaitForExit(c: Token, StartIfNeeded: false);
                }
            }
        }
    }
}