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
using FlowEngine = global::PCLCS.PackUpdateFlowEngine<global::PCL.PackUpdateFile, global::PCL.PackUpdatePackage, global::PCL.PackUpdateChange, global::PCL.PackUpdatePlan, global::PCL.ModNet.NetFile, global::PCL.ResourceVersion, global::PCL.ModMinecraft.McInstance, global::PCL.ModLoader.LoaderTask<global::System.String, global::System.String>, global::PCL.ModLoader.LoaderCombo<global::System.String>, global::MeloongCore.RedoableWorker, global::MeloongCore.ProgressProvider>;
using ThrottleDebounce;

namespace PCL
{

    internal sealed class ModpackUpdateFlow
    {
        private static global::System.Int32 Active;
        private static readonly global::PCLCS.PackUpdateFlowEngine<global::PCL.PackUpdateFile, global::PCL.PackUpdatePackage, global::PCL.PackUpdateChange, global::PCL.PackUpdatePlan, global::PCL.ModNet.NetFile, global::PCL.ResourceVersion, global::PCL.ModMinecraft.McInstance, global::PCL.ModLoader.LoaderTask<global::System.String, global::System.String>, global::PCL.ModLoader.LoaderCombo<global::System.String>, global::MeloongCore.RedoableWorker, global::MeloongCore.ProgressProvider> Engine = new global::PCLCS.PackUpdateFlowEngine<global::PCL.PackUpdateFile, global::PCL.PackUpdatePackage, global::PCL.PackUpdateChange, global::PCL.PackUpdatePlan, global::PCL.ModNet.NetFile, global::PCL.ResourceVersion, global::PCL.ModMinecraft.McInstance, global::PCL.ModLoader.LoaderTask<global::System.String, global::System.String>, global::PCL.ModLoader.LoaderCombo<global::System.String>, global::MeloongCore.RedoableWorker, global::MeloongCore.ProgressProvider>(global::PCL.ModpackUpdate.Engine, global::PCL.ModpackUpdateFlow.CreateHost());
        private static global::PCLCS.PackUpdateFlowHost<global::PCL.PackUpdatePackage, global::PCL.PackUpdatePlan, global::PCL.ModNet.NetFile, global::PCL.ResourceVersion, global::PCL.ModMinecraft.McInstance, global::PCL.ModLoader.LoaderTask<global::System.String, global::System.String>, global::PCL.ModLoader.LoaderCombo<global::System.String>, global::MeloongCore.RedoableWorker, global::MeloongCore.ProgressProvider> CreateHost()
        {
            return new global::PCLCS.PackUpdateFlowHost<global::PCL.PackUpdatePackage, global::PCL.PackUpdatePlan, global::PCL.ModNet.NetFile, global::PCL.ResourceVersion, global::PCL.ModMinecraft.McInstance, global::PCL.ModLoader.LoaderTask<global::System.String, global::System.String>, global::PCL.ModLoader.LoaderCombo<global::System.String>, global::MeloongCore.RedoableWorker, global::MeloongCore.ProgressProvider>()
            {
                ReadActive = new Func<global::System.Int32>(() => global::PCLCS.PackUpdateFlowState.Read(ref global::PCL.ModpackUpdateFlow.Active)),
                CompareExchangeActive = new Func<global::System.Int32, global::System.Int32, global::System.Int32>((Value, Comparand) => global::PCLCS.PackUpdateFlowState.CompareExchange(ref global::PCL.ModpackUpdateFlow.Active, Value, Comparand)),
                ExchangeActive = new Func<global::System.Int32, global::System.Int32>(Value => global::PCLCS.PackUpdateFlowState.Exchange(ref global::PCL.ModpackUpdateFlow.Active, Value)),
                EnvironmentBusy = new Func<global::System.Boolean>(() => global::PCL.EnvironmentHistoryUi.IsBusy),
                HasRunningGame = new Func<global::System.Boolean>(() => global::PCL.ModWatcher.HasRunningMinecraft),
                HasRunningSessions = global::PCL.QuickPlayWorlds.HasRunningSessions,
                LaunchLoading = new Func<global::System.Boolean>(() => (global::PCL.ModLaunch.McLaunchLoader.State == global::PCL.ModBase.LoadState.Loading)),
                IsLocked = new Func<global::PCL.ModMinecraft.McInstance, global::System.Boolean>(Instance => global::PCL.ModMinecraft.McInstanceLock.IsLocked(Instance)),
                Hint = new Action<global::System.String>(Message => global::PCL.ModMain.Hint(Message)),
                HintRed = new Action<global::System.String>(Message => global::PCL.ModMain.Hint(Message, global::PCL.ModMain.HintType.Red)),
                SelectFile = new Func<global::System.String, global::System.String>(Title => global::MeloongCore.Wpf.Dialogs.SelectFile(Title, false, filter: new[] { (new[] { "zip", "mrpack" }, "整合包文件") }).FirstOrDefault()),
                CreateStep = new Func<global::System.String, global::System.Action<global::PCL.ModLoader.LoaderTask<global::System.String, global::System.String>>, global::PCL.ModLoader.LoaderTask<global::System.String, global::System.String>>((Name, Work) => new global::PCL.ModLoader.LoaderTask<global::System.String, global::System.String>(Name, Work)),
                CreateCombo = new Func<global::System.String, global::PCL.ModLoader.LoaderTask<global::System.String, global::System.String>, global::PCL.ModLoader.LoaderCombo<global::System.String>>((Name, StepLoader) => new global::PCL.ModLoader.LoaderCombo<global::System.String>(Name, new[] { StepLoader }) { OnStateChanged = global::PCL.ModDownloadLib.LoaderStateChangedHintOnly }),
                CreateWorker = new Func<global::PCL.ModLoader.LoaderCombo<global::System.String>, global::System.String, global::MeloongCore.RedoableWorker>((Loader, Folder) => new global::MeloongCore.RedoableWorker(global::PCL.LoaderFileOperation.CreateWorkload(Loader, Folder))),
                TryAcquire = new Func<global::MeloongCore.RedoableWorker, global::System.String, global::System.Boolean>((Worker, Folder) => global::PCL.ModMinecraft.McInstanceLock.TryAcquire(Worker, Folder)),
                OnStopped = new Action<global::MeloongCore.RedoableWorker, global::System.Action>((Worker, Work) => Worker.Stopped += () => Work()),
                StartWorker = new Action<global::MeloongCore.RedoableWorker>(Worker => Worker.Start()),
                AddTaskbar = new Action<global::PCL.ModLoader.LoaderCombo<global::System.String>>(Loader => global::PCL.ModLoader.LoaderTaskbarAdd(Loader)),
                RefreshButton = new Action(() => global::PCL.ModMain.FrmMain.BtnExtraDownload.ShowRefresh()),
                Name = new Func<global::PCL.ModMinecraft.McInstance, global::System.String>(Instance => Instance.Name),
                VersionFolder = new Func<global::PCL.ModMinecraft.McInstance, global::System.String>(Instance => Instance.PathVersion),
                GameRoot = new Func<global::PCL.ModMinecraft.McInstance, global::System.String>(Instance => Instance.PathIndie),
                Version = new Func<global::PCL.ModMinecraft.McInstance, global::PCLCS.McVersion>(Instance => Instance.Version),
                UiWait = new Action<global::System.Action>(Work => global::PCL.ModBase.RunInUiWait(() => Work())),
                InputProjectAddress = new Func<global::System.String>(() => global::PCL.ModMain.MyMsgBoxInput("整合包项目地址", "CurseForge 或 Modrinth 整合包项目地址", "", Button2: "取消")),
                Request = new Func<global::System.String, global::Newtonsoft.Json.Linq.JToken>(Url => global::PCL.ModDownload.DlModRequest(Url)),
                EnsureWithin = new Action<global::System.String, global::System.String>((Root, FileName) => global::PCL.InstanceResourceStore.EnsureWithin(Root, FileName)),
                Message = new Func<global::System.String, global::System.String, global::System.String, global::System.String, global::System.Boolean, global::System.Int32>((Caption, Title, Button1, Button2, Warn) => global::PCL.ModMain.MyMsgBox(Caption, Title, Button1, Button2, IsWarn: Warn)),
                Preview = global::PCL.ModpackUpdateFlow.Preview,
                SelectVersion = global::PCL.ModpackUpdateFlow.SelectOnlineVersion,
                Project = new Func<global::System.String, global::PCLCS.ResourcePlatforms, global::PCLCS.ResourceProjectModelData>((Id, Platform) => global::PCL.ResourceProject.FromProjectId(Id, Platform)),
                RemoveVersionCache = new Action<global::System.String>(Id => global::PCL.ResourceVersion.ProjectFilesCache.Remove(Id)),
                Versions = global::PCL.ResourceVersion.FromProjectId,
                ToUpdateFile = new Func<global::PCL.ResourceVersion, global::System.String, global::System.String, global::PCLCS.ModLoaders, global::PCL.ModNet.NetFile>((Version, Destination, Game, Loaders) => Version.ToNetFile(Destination, global::PCL.ResourceVersion.DownloadReason.Update, Game, Loaders)),
                Download = global::PCL.ModpackUpdate.DownloadFilesWithProgress,
                TempFolder = new Func<global::System.String>(() => global::PCL.ModMain.RequestTaskTempFolder()),
                Cancellation = new Func<global::PCL.ModLoader.LoaderTask<global::System.String, global::System.String>, global::System.Threading.CancellationToken>(Task => Task.CreateCancellationToken()),
                SetProgress = new Action<global::PCL.ModLoader.LoaderTask<global::System.String, global::System.String>, global::System.Double>((Task, Value) => Task.Progress = Value),
                Progress = new Func<global::PCL.ModLoader.LoaderTask<global::System.String, global::System.String>, global::System.Double, global::System.Double, global::MeloongCore.ProgressProvider>((Task, Start, Finish) => Task.CreateSyncProgressProvider(Start, Finish)),
                SetOutput = new Action<global::PCL.ModLoader.LoaderTask<global::System.String, global::System.String>, global::System.String>((Task, Value) => Task.Output = Value),
                CreateRuntime = global::PCL.ModpackUpdateFlow.CreateRuntime,
                RuntimeWorkload = new Func<global::PCL.ModLoader.LoaderCombo<global::System.String>, global::System.String, global::MeloongCore.ProgressProvider, global::System.Action>((Build, Folder, Progress) => global::PCL.LoaderFileOperation.CreateWorkloadWithProgress(Build, Folder, Progress)),
                CancelCombo = new Action<global::PCL.ModLoader.LoaderCombo<global::System.String>>(Build => Build.Cancel()),
                BeforeMinecraftChange = new Action<global::PCL.ModMinecraft.McInstance, global::System.String, global::System.String, global::System.Threading.CancellationToken>((Instance, OldMinecraft, NewMinecraft, Token) => global::PCL.WorldBackups.BeforeMinecraftChange(Instance, OldMinecraft, NewMinecraft, Token)),
                CaptureHistory = new Action<global::PCL.ModMinecraft.McInstance, global::System.String, global::System.Threading.CancellationToken>((Instance, Caption, Token) => global::PCL.EnvironmentHistory.CaptureInstance(Instance, Caption, Token)),
                ResetSettings = new Action<global::PCL.ModMinecraft.McInstance>(Instance => Instance.ResetSettingsCache()),
                LoadInstance = new Func<global::System.String, global::PCL.ModMinecraft.McInstance>(Folder => new global::PCL.ModMinecraft.McInstance(Folder).Load()),
                PageInstance = new Func<global::PCL.ModMinecraft.McInstance>(() => global::PCL.PageInstanceLeft.Instance),
                SetPageInstance = new Action<global::PCL.ModMinecraft.McInstance>(Value => global::PCL.PageInstanceLeft.Instance = Value),
                SelectedInstance = new Func<global::PCL.ModMinecraft.McInstance>(() => global::PCL.ModMinecraft.McInstanceSelected),
                SetSelectedInstance = new Action<global::PCL.ModMinecraft.McInstance>(Value => global::PCL.ModMinecraft.McInstanceSelected = Value),
                Explorer = global::PCL.ModBase.OpenExplorer,
                Info = new Action<global::System.String>(Message => global::MeloongCore.Logger.Info(Message))
            };
        }
        internal static bool IsUpdating
        {
            get
            {
                return global::PCL.ModpackUpdateFlow.Engine.IsUpdating;
            }
        }
        internal static void Start(global::PCL.ModMinecraft.McInstance Instance, global::System.Boolean Online)
        {
            global::PCL.ModpackUpdateFlow.Engine.Start(Instance, Online);
        }
        internal static void OpenBackups(global::PCL.ModMinecraft.McInstance Instance)
        {
            global::PCL.ModpackUpdateFlow.Engine.OpenBackups(Instance);
        }
        private static global::System.String SelectArchive(global::System.String Title)
        {
            return global::PCL.ModpackUpdateFlow.Engine.SelectArchive(Title);
        }
        private static global::System.String ChooseOnline(global::Newtonsoft.Json.Linq.JObject Baseline, global::System.String Stage, global::System.Threading.CancellationToken Token, ref global::Newtonsoft.Json.Linq.JObject Source, global::MeloongCore.ProgressProvider Progress)
        {
            return global::PCL.ModpackUpdateFlow.Engine.ChooseOnline(Baseline, Stage, Token, ref Source, Progress);
        }
        private static void Execute(global::PCL.ModMinecraft.McInstance Instance, global::System.Boolean Online, global::System.String ArchivePath, global::PCL.ModLoader.LoaderTask<global::System.String, global::System.String> Task)
        {
            global::PCL.ModpackUpdateFlow.Engine.Execute(Instance, Online, ArchivePath, Task);
        }
        internal static global::PCL.ModMinecraft.McInstance RecoverInstance(global::PCL.ModMinecraft.McInstance Instance)
        {
            return global::PCL.ModpackUpdateFlow.Engine.RecoverInstance(Instance);
        }
        private static global::PCL.ModMinecraft.McInstance ReloadInstance(global::PCL.ModMinecraft.McInstance Instance)
        {
            return global::PCL.ModpackUpdateFlow.Engine.ReloadInstance(Instance);
        }
        // 实际模态控件及运行时 Loader 构造，流程决策与取消/等待编排由 C# 持有。
        private static global::System.Int32 Preview(global::System.String Summary, global::PCL.PackUpdatePlan Plan, global::System.Func<global::PCLCS.McVersion> Version)
        {
            return global::PCL.ModMain.MyMsgBoxConfirmContent(Summary, "整合包更新预览", "按以上选择更新", () => new global::PCL.ModpackUpdatePreviewView(Plan, Version().VanillaName, Version().ModLoaderVersions), IsWarn: false);
        }
        private static global::PCL.ResourceVersion SelectOnlineVersion(global::System.Collections.Generic.List<global::PCL.ResourceVersion> Versions, global::System.String Name, global::System.String CurrentVersion)
        {
            var View = new global::PCL.ModpackVersionSelectView((global::System.Collections.Generic.IEnumerable<global::PCL.ResourceVersion>)Versions, Name, CurrentVersion);
            return ((global::PCL.ModMain.MyMsgBoxConfirmContent("请选择要下载并预览的整合包版本。", "选择整合包版本", "下载并预览", () => View, IsWarn: false)) == (1)) ? (View.Selected) : (global::PCL.ResourceVersion)(null);
        }
        private static global::PCL.ModLoader.LoaderCombo<global::System.String> CreateRuntime(global::PCLCS.PackUpdateRuntimeRequest Request)
        {
            return new global::PCL.ModLoader.LoaderCombo<global::System.String>("准备整合包游戏组件", (global::System.Collections.Generic.IEnumerable<global::PCL.ModLoader.LoaderBase>)global::PCL.ModDownloadLib.McInstallLoadersGet(new global::PCL.ModDownloadLib.McInstallRequest()
            {
                NewInstanceName = Request.Name,
                InstanceFolder = Request.BuildFolder,
                Components = new global::PCLCS.McComponents(Request.Minecraft, Request.Loaders),
                IsIntermediateOutput = true,
                MinecraftFolder = Request.MinecraftFolder
            }));
        }
    }
}
