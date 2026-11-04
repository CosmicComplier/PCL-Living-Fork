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
using System.Net.Http;
using System.Runtime.CompilerServices;
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

    // 保留 LF Java 公共入口及原 WithEvents 工作器；规则、设置迁移、选择和下载计划由 C# 单实现维护。
    public static class ModJava
    {
        /// <summary>搜索、检查并缓存 Java，完成后由原事件刷新设置页。</summary>
        private static global::MeloongCore.RedoableWorker _JavaListRefreshWorker;

        public static global::MeloongCore.RedoableWorker JavaListRefreshWorker
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                return _JavaListRefreshWorker;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                if (_JavaListRefreshWorker != null)
                {
                    _JavaListRefreshWorker.Started -= UpdateJavaLists;
                    _JavaListRefreshWorker.Stopped -= UpdateJavaLists;
                }

                _JavaListRefreshWorker = value;
                if (_JavaListRefreshWorker != null)
                {
                    _JavaListRefreshWorker.Started += UpdateJavaLists;
                    _JavaListRefreshWorker.Stopped += UpdateJavaLists;
                }
            }
        }
        private readonly static global::PCLCS.LegacyJavaService<global::PCL.ModMinecraft.McInstance> Service = new global::PCLCS.LegacyJavaService<global::PCL.ModMinecraft.McInstance>(global::PCL.ModJava.CreateJavaHost());

        static ModJava()
        {
            JavaListRefreshWorker = new global::MeloongCore.RedoableWorker((c, p) => global::PCLCS.JavaUtils.RefreshListAsync((global::System.Threading.CancellationToken)((c)), p).Run());
        }

        private static global::PCLCS.LegacyJavaHost<global::PCL.ModMinecraft.McInstance> CreateJavaHost()
        {
            return new global::PCLCS.LegacyJavaHost<global::PCL.ModMinecraft.McInstance>()
            {
                ReadString = new Func<global::System.String, global::PCL.ModMinecraft.McInstance, global::System.String>((Key, Instance) => global::PCL.Settings.Get<string>(Key, Instance: Instance)),
                ReadInteger = new Func<global::System.String, global::PCL.ModMinecraft.McInstance, global::System.Int32>((Key, Instance) => global::PCL.Settings.Get<int>(Key, Instance: Instance)),
                WriteInteger = new Action<global::System.String, global::System.Int32, global::PCL.ModMinecraft.McInstance>((Key, Value, Instance) => global::PCL.Settings.Set(Key, Value, Instance: Instance)),
                Config = new Func<global::PCL.ModMinecraft.McInstance, global::MeloongCore.JsonConfigProvider>(Instance => Instance.Config),
                Version = new Func<global::PCL.ModMinecraft.McInstance, global::PCLCS.McVersion>(Instance => Instance.Version),
                VersionPath = new Func<global::PCL.ModMinecraft.McInstance, global::System.String>(Instance => Instance.PathVersion),
                InstanceName = new Func<global::PCL.ModMinecraft.McInstance, global::System.String>(Instance => Instance.Name),
                VersionDisplayName = new Func<global::PCL.ModMinecraft.McInstance, global::System.String>(Instance => Instance.VersionDisplayName()),
                ReleaseTime = new Func<global::PCL.ModMinecraft.McInstance, global::System.DateTime>(Instance => Instance.ReleaseTime),
                JsonObject = new Func<global::PCL.ModMinecraft.McInstance, global::Newtonsoft.Json.Linq.JObject>(Instance => Instance.JsonObject),
                JsonVersion = new Func<global::PCL.ModMinecraft.McInstance, global::Newtonsoft.Json.Linq.JObject>(Instance => Instance.JsonVersion),
                IsNideLogin = new Func<global::System.Boolean>(() => (global::PCL.Settings.Get<global::PCL.ModLaunch.McLoginType>("LoginType") == global::PCL.ModLaunch.McLoginType.Nide)),
                RefreshWorker = new Func<global::MeloongCore.RedoableWorker>(() => global::PCL.ModJava.JavaListRefreshWorker),
                UpdateJavaLists = global::PCL.ModJava.UpdateJavaLists,
                SelectJavaExecutable = new Func<global::System.String>(() => global::MeloongCore.Wpf.Dialogs.SelectFile("选择 java.exe（可能在 bin 文件夹中）", false, filter: "java.exe|java.exe").FirstOrDefault()),
                Hint = global::PCL.ModJava.PublishJavaHint,
                ReloadCurrentJava = new Action(() => global::PCL.PageInstanceLeft.ReloadCurrentJava()),
                RunInThread = new Action<global::System.Action>(Work => global::PCL.ModBase.RunInThread(Work)),
                SwitchToInstanceSetup = global::PCL.ModJava.SwitchToInstanceSetup,
                Message = new Action<global::System.String, global::System.String, global::System.Boolean, global::System.Boolean>((Caption, Title, IsWarn, ForceWait) => global::PCL.ModMain.MyMsgBox(Caption, Title, IsWarn: IsWarn, ForceWait: ForceWait)),
                DisableJavaSettings = global::PCL.ModJava.DisableJavaSettings,
                RestoreJavaSettings = global::PCL.ModJava.RestoreJavaSettings,
                VersionListOrder = new Func<global::System.Collections.Generic.IEnumerable<global::System.String>, global::System.Collections.Generic.IEnumerable<global::System.String>, global::System.Collections.Generic.IEnumerable<global::System.String>>((Official, Mirror) => global::PCL.ModDownload.DlVersionListOrder(Official, Mirror)),
                SourceOrder = new Func<global::System.Collections.Generic.IEnumerable<global::System.String>, global::System.Collections.Generic.IEnumerable<global::System.String>, global::System.Collections.Generic.IEnumerable<global::System.String>>((Official, Mirror) => global::PCL.ModDownload.DlSourceOrder(Official, Mirror)),
                RequestJson = new Func<global::System.Collections.Generic.IEnumerable<global::System.String>, global::System.String>(Urls => global::PCL.ModNet.NetRequestByLoader(Urls, IsJson: true)),
                CreateDownload = global::PCL.ModJava.CreateJavaDownload
            };
        }

        /// <summary>初始化原 Java 缓存并迁移旧版设置；无可用项目时触发搜索。</summary>
        public static void JavaInit()
        {
            global::PCL.ModJava.Service.JavaInit();
        }

        public static void UpdateJavaLists()
        {
            global::PCL.ModBase.RunInUi(() =>
        {
            global::PCL.ModMain.FrmSetupLaunch?.UpdateJavaList();
            global::PCL.ModMain.FrmInstanceSetup?.UpdateJavaList();
        });
        }

        public static void ManuallyImportJava()
        {
            global::PCL.ModJava.Service.ManuallyImportJava();
        }
        public static void ManuallyRemoveJava(global::PCLCS.Java JavaEntry)
        {
            global::PCL.ModJava.Service.ManuallyRemoveJava(JavaEntry);
        }
        public static void ManuallySearchJava(global::System.Threading.CancellationToken CancellationToken = default(global::System.Threading.CancellationToken))
        {
            global::PCL.ModJava.Service.ManuallySearchJava(CancellationToken);
        }

        public static (global::MeloongCore.ValueRange<global::System.Version> Range, global::System.String RecommendedComponent) GetJavaRequirement(global::PCL.ModMinecraft.McInstance Instance)
        {
            return global::PCL.ModJava.Service.GetJavaRequirement(Instance);
        }

        public static global::PCLCS.Java SelectOrDownloadJava(global::MeloongCore.OneOf<global::MeloongCore.ValueRange<global::System.Version>, global::PCL.ModMinecraft.McInstance> Target, global::System.Boolean TryFixOrHint, global::System.Threading.CancellationToken c = default(global::System.Threading.CancellationToken), global::MeloongCore.ProgressProvider p = null)
        {
            return global::PCL.ModJava.Service.SelectOrDownloadJava(Target, TryFixOrHint, c, p);
        }

        private static void PublishJavaHint(global::System.String Text, global::PCLCS.LegacyJavaHintKind Kind)
        {
            global::PCL.ModMain.Hint(Text, (Kind == global::PCLCS.LegacyJavaHintKind.Red) ? (global::PCL.ModMain.HintType.Red) : (global::PCL.ModMain.HintType.Green));
        }
        private static void SwitchToInstanceSetup(global::PCL.ModMinecraft.McInstance Instance)
        {
            global::PCL.ModBase.RunInUi(() =>
        {
            global::PCL.PageInstanceLeft.Instance = Instance;
            global::PCL.ModMain.FrmMain.PageChange((global::PCL.FormMain.PageStackData)global::PCL.FormMain.PageType.InstanceSetup, global::PCL.FormMain.PageSubType.InstanceSetup);
        });
        }
        private static void DisableJavaSettings()
        {
            if (global::PCL.ModMain.FrmInstanceSetup is not null)
                global::PCL.ModBase.RunInUiWait(() => global::PCL.ModMain.FrmInstanceSetup.ComboArgumentJava.IsEnabled = false);
        }
        private static void RestoreJavaSettings()
        {
            if (global::PCL.ModMain.FrmInstanceSetup is not null)
            {
                global::PCL.ModBase.RunInUiWait(() =>
                        {
                            global::PCL.SettingService.RefreshSettings(global::PCL.ModMain.FrmInstanceSetup.ComboArgumentJava);
                            global::PCL.SettingService.RefreshSettings(global::PCL.ModMain.FrmInstanceSetup.ComboArgumentJavaSelect);
                            global::PCL.ModMain.FrmInstanceSetup.ComboArgumentJava.IsEnabled = true;
                        });
            }
        }

        // 仅适配原 Loader/NetFile 生命周期；manifest 解释、文件选择、校验参数和清理由 C# 计划提供。
        private static global::PCLCS.LegacyJavaDownloadOperation CreateJavaDownload(global::PCLCS.LegacyJavaDownloadPlan Plan)
        {
            var FilesToDownload = Plan.Files.Select(File => new global::PCL.ModNet.NetFile(File.Sources, File.Destination, new FileChecker() { ActualSize = File.ActualSize, Hash = File.Hash })).ToList();
            var Loader = new global::PCL.ModLoader.LoaderCombo<global::System.Int32>("下载 Java", new global::PCL.ModLoader.LoaderBase[] { new global::PCL.ModNet.LoaderDownload("下载 Java 文件", (global::System.Collections.Generic.IEnumerable<global::PCL.ModNet.NetFile>)FilesToDownload) { ProgressWeight = 10d }, new global::PCL.ModLoader.LoaderWorker("刷新 Java 列表", global::PCL.ModJava.JavaListRefreshWorker) }) { OnStateChanged = new Action<global::PCL.ModLoader.LoaderBase>(RawValue => { var RawLoader = (global::PCL.ModLoader.LoaderCombo<global::System.Int32>)RawValue; if (((RawLoader.State == global::PCL.ModBase.LoadState.Failed) || (RawLoader.State == global::PCL.ModBase.LoadState.Canceled))) Plan.Incomplete(); }) };
            return new global::PCLCS.LegacyJavaDownloadOperation()
            {
                Wait = new Action<global::System.Threading.CancellationToken, global::MeloongCore.ProgressProvider>((c, p) => Loader.WaitForExit(IsForceRestart: true, c: c, p: p)),
                Cancel = new Action(() => Loader.Cancel())
            };
        }
    }
}