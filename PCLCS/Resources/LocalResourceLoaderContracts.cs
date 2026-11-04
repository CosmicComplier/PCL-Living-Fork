using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace PCLCS;

public sealed class LocalResourceLoaderHost<TLocal, TProject, TVersion>
    where TLocal : LocalResourceFileData<TProject, TVersion>
    where TProject : ResourceProjectModelData where TVersion : ResourceVersionModelData {
    public Func<string, TLocal> CreateLocal = null!;
    public Func<JObject, TProject> CreateProject = null!;
    public Func<JObject, ResourceTypes, TVersion> ReadVersion = null!;
    public Func<Dictionary<string, TProject>> ProjectCache = null!;
    public Func<string, HttpMethod?, string?, string?, JToken> Request = null!;
    public Func<Action, string, Thread> StartThread = null!;
    public Func<string> TempPath = null!;
    public Func<bool> DebugMode = null!;
    public Func<int> CacheVersion = null!;
    public Func<McVersion?> CurrentVersion = null!;
    public Func<string, bool> IsUpdatingFolder = null!;
    public Action<bool> SetShowProgress = null!;
    public Action<string> SetLoadText = null!;
    public Action ReloadFolder = null!;
    public Action<List<TLocal>, JObject, McVersion> StartOnline = null!;
    public Func<bool> IsUpdateFilter = null!;
    public Action RefreshUi = null!, RefreshBars = null!;
    public Action<string> Info = null!, Warning = null!;
    public Action<string> WarnQuiet = null!;
    public Action<Func<string>> Trace = null!;
    public Action<Exception, string> Warn = null!;
}
public sealed class LocalResourceFileTask<TLocal> {
    public Func<string> Input = null!;
    public Func<bool> IsCanceled = null!;
    public Action<List<TLocal>> SetOutput = null!;
}
public sealed class LocalResourceOnlineTask<TLocal> {
    public Func<(List<TLocal> ModList, JObject Cache, McVersion Target)> Input = null!;
    public Func<bool> IsCanceled = null!;
    public Func<Thread, bool> IsCanceledWithThread = null!;
}
