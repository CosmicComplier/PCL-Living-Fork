using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using MeloongCore;
using MeloongCore.Extensions;
using Newtonsoft.Json.Linq;
using PCLCS;
using FileEngine = PCLCS.LocalResourceLoaderEngine<PCL.LocalResourceFile, PCL.ResourceProject, PCL.ResourceVersion>;

namespace PCL
{
    public static class LocalResourceLoaders
    {
        public static PCL.ModLoader.LoaderTask<string, List<PCL.LocalResourceFile>> LocalResourceLoader = new PCL.ModLoader.LoaderTask<string, List<PCL.LocalResourceFile>>("LocalResourceLoader", LocalResourceLoaders.LocalResourceFileLoad);
        public static PCL.ModLoader.LoaderTask<(List<PCL.LocalResourceFile> ModList, JObject Cache, McVersion Target), int> LocalResourceOnlineLoader = new PCL.ModLoader.LoaderTask<(List<PCL.LocalResourceFile> ModList, JObject Cache, McVersion Target), int>("LocalResourceOnlineLoader", LocalResourceLoaders.LocalResourceOnlineLoad);
        private readonly static FileEngine Engine = CreateResourceEngine();
        private static FileEngine CreateResourceEngine()
        {
            return new FileEngine(new LocalResourceLoaderHost<PCL.LocalResourceFile, PCL.ResourceProject, PCL.ResourceVersion>() { CreateLocal = new Func<string, PCL.LocalResourceFile>(Path => new PCL.LocalResourceFile(Path)), CreateProject = new Func<JObject, PCL.ResourceProject>(Data => new PCL.ResourceProject(Data)), ReadVersion = PCL.ResourceVersion.FromPlatformJson, ProjectCache = new Func<Dictionary<string, PCL.ResourceProject>>(() => PCL.ResourceProject.Cache), Request = new Func<string, HttpMethod, string, string, JToken>((Url, Method, Body, ContentType) => PCL.ModDownload.DlModRequest(Url, Method, Body, ContentType)), StartThread = new Func<Action, string, Thread>((Work, Name) => PCL.ModBase.RunInNewThread(() => Work(), Name)), TempPath = new Func<string>(() => PCL.ModBase.PathTemp), DebugMode = new Func<bool>(() => PCL.ModBase.ModeDebug), CacheVersion = new Func<int>(() => Versions.LocalModCacheVersion), CurrentVersion = new Func<McVersion>(() => PCL.PageInstanceLeft.Instance?.Version), IsUpdatingFolder = new Func<string, bool>(Path => PCL.PageInstanceMod.UpdatingInstanceModFolders.Contains(Path)), SetShowProgress = SetShowProgress, SetLoadText = SetLoadText, ReloadFolder = new Action(() => PCL.ModMain.FrmInstanceMod.LoaderRun(PCL.ModLoader.LoaderFolderRunType.UpdateOnly)), StartOnline = new Action<List<PCL.LocalResourceFile>, JObject, McVersion>((Mods, Cache, Target) => LocalResourceOnlineLoader.Start((Mods, Cache, Target), IsForceRestart: true)), IsUpdateFilter = new Func<bool>(() => ((PCL.ModMain.FrmInstanceMod?.Filter)is { } arg1 ? arg1 == PCL.PageInstanceMod.FilterType.CanUpdate : (bool? )null) ?? false), RefreshUi = new Action(() => PCL.ModBase.RunInUi(() => PCL.ModMain.FrmInstanceMod?.RefreshUI())), RefreshBars = new Action(() => PCL.ModBase.RunInUi(() => PCL.ModMain.FrmInstanceMod?.RefreshBars())), Info = new Action<string>(Message => Logger.Info(Message)), Warning = new Action<string>(Message => Logger.Warn(Message)), WarnQuiet = new Action<string>(Message => Logger.Warn(Message, LogBehavior.None)), Trace = new Action<Func<string>>(Message => Logger.Trace(Message)), Warn = new Action<Exception, string>((ErrorValue, Message) => Logger.Warn(ErrorValue, Message)) });
        }

        private static void SetShowProgress(bool Value)
        {
            PCL.ModBase.RunInUiWait(() =>
            {
                if (PCL.ModMain.FrmInstanceMod is not null)
                    PCL.ModMain.FrmInstanceMod.Load.ShowProgress = Value;
            });
        }

        private static void SetLoadText(string Value)
        {
            PCL.ModBase.RunInUiWait(() =>
            {
                if (PCL.ModMain.FrmInstanceMod is not null)
                    PCL.ModMain.FrmInstanceMod.Load.Text = Value;
            });
        }

        private static void LocalResourceFileLoad(PCL.ModLoader.LoaderTask<string, List<PCL.LocalResourceFile>> Loader)
        {
            Engine.LoadFiles(new LocalResourceFileTask<PCL.LocalResourceFile>() { Input = new Func<string>(() => Loader.Input), IsCanceled = new Func<bool>(() => Loader.IsCanceled), SetOutput = new Action<List<PCL.LocalResourceFile>>(Value => Loader.Output = Value) });
        }

        private static void LocalResourceOnlineLoad(PCL.ModLoader.LoaderTask<(List<PCL.LocalResourceFile> ModList, JObject Cache, McVersion Target), int> Loader)
        {
            Engine.LoadOnline(new LocalResourceOnlineTask<PCL.LocalResourceFile>() { Input = new Func<(List<PCL.LocalResourceFile> ModList, JObject Cache, McVersion Target)>(() => Loader.Input), IsCanceled = new Func<bool>(() => Loader.IsCanceled), IsCanceledWithThread = new Func<Thread, bool>(Thread => Loader.IsCanceledWithThread(Thread)) });
        }
    }
}
