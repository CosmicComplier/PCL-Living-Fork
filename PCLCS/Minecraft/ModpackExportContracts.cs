using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using MeloongCore;
using Newtonsoft.Json.Linq;

namespace PCLCS;

// LF PageInstanceExport at f4aa0fae62c3ee8af24c81e61844c8fdd13c8983 and current LF
// fixes, LF LICENCE. PCL 5ef81e490ae46b1100d61e33d0183094c7a80ca8 supplies the
// original export page; LF mode/isolation, source filtering and current LF executable remain.
// CE 1a7653127d578efdb2a9388999a3fdc67d9e49d5 Pages/PageInstance/PageInstanceExport.xaml.cs
// reviewed under Plain Craft Launcher 2/LICENCE. Its DP option/globbing/current runtime
// policies are not substituted for LF CLR properties, VB Like or resource/thread semantics.
public class ModpackExportOptionData {
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Rules { get; set; }
    public string? ShowRules { get; set; }
    public bool DefaultChecked { get; set; }
    public bool RequireModLoader { get; set; }
    public bool RequireOptiFine { get; set; }
    public bool RequireModLoaderOrOptiFine { get; set; }
}
public sealed class ModpackExportRequest<TInstance> where TInstance : class {
    public string Name = null!, Version = null!, LoaderName = null!, PackPath = null!, CacheFolder = null!, OverridesFolder = null!, GameRoot = null!;
    public TInstance Instance = null!;
    public bool IncludeJava, CheckHostedAssets, ModrinthUploadMode, IncludeLauncher, IncludeCustom, IncludeModpackMode, VersionIsolation;
    public List<string> Rules = null!, ExtraFiles = null!;
    public string CopyStepName => "复制导出内容";
    public string OnlineStepName => "联网获取文件信息";
    public string ArchiveStepName => "生成压缩包";
    public double CopyWeight => 5;
    public bool ShowOnline => CheckHostedAssets;
    public double OnlineWeight => CheckHostedAssets ? 2 : 0.01;
    public double ArchiveWeight => 6;
}
public sealed class ModpackExportCopyTask<TLocal> {
    public Func<List<TLocal>> Output = null!;
    public Action<List<TLocal>> SetOutput = null!;
    public Func<double> Progress = null!;
    public Action<double> SetProgress = null!;
    public Func<CancellationToken> Cancellation = null!;
    public Func<double, double, ProgressProvider> ProgressProvider = null!;
}
public sealed class ModpackExportOnlineTask<TLocal> where TLocal : class {
    public Func<List<TLocal>> Input = null!;
    public Func<Dictionary<TLocal, List<string>>> Output = null!;
    public Action<Dictionary<TLocal, List<string>>> SetOutput = null!;
    public Func<bool> IsCanceled = null!;
    public Func<double> Progress = null!;
    public Action<double> SetProgress = null!;
}
public sealed class ModpackExportArchiveTask<TLocal> where TLocal : class {
    public Func<Dictionary<TLocal, List<string>>> Input = null!;
    public Action<double> SetProgress = null!;
}
public sealed class ModpackExportHost<TInstance, TLocal, TProject, TVersion>
    where TInstance : class where TLocal : LocalResourceFileData<TProject, TVersion>
    where TProject : ResourceProjectModelData where TVersion : ResourceVersionModelData {
    public Func<TInstance, string> VersionFolder = null!, Info = null!;
    public Func<TInstance, McVersion> Version = null!;
    public Func<string> SelectedMinecraftFolder = null!, Executable = null!, ExecutableName = null!, BaseFolder = null!;
    public Func<string, TLocal> CreateLocal = null!;
    public Func<TInstance, bool, CancellationToken, ProgressProvider, Java?> SelectJava = null!;
    public Action<string, string, bool> WriteBoolIni = null!;
    public Action<string, bool, List<string>> ConfigureIsolation = null!;
    public Action<string, bool, bool> ConfigureMode = null!;
    public Func<string, HttpMethod?, string?, string?, JToken> Request = null!;
    public Func<Action, string, Thread> StartThread = null!;
    public Func<string, string, string, string, int> Confirm = null!;
    public Action<string> InfoLog = null!, HintRed = null!, Explorer = null!;
    public Action<Exception, string> Warn = null!;
}
