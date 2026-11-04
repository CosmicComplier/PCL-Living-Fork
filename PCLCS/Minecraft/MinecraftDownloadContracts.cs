using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;

namespace PCLCS;

public sealed class MinecraftDownloadRequest {
    public string Url = null!;
    public HttpMethod? Method;
    public string? Content, ContentType;
    public string Accept = "*/*";
    public int Timeout = 25000;
    public bool RequireJson, SimulateBrowserHeaders;
    public Encoding? Encoding;
}
public readonly struct MinecraftDownloadResult<TValue> {
    public readonly string SourceName;
    public readonly bool IsOfficial;
    public readonly TValue Value;
    public MinecraftDownloadResult(string sourceName, bool isOfficial, TValue value) { SourceName = sourceName; IsOfficial = isOfficial; Value = value; }
}
/// <summary>All reads and writes act on the existing real LoaderTask. No task state is mirrored.</summary>
public sealed class MinecraftDownloadSourceTask<TOutput> {
    public Func<object?> Input = null!;
    public Action ClearInput = null!;
    public Func<TOutput> Output = null!;
    public Action<TOutput> SetOutput = null!;
    public Func<bool> IsFailed = null!, IsFinished = null!, IsLoading = null!, IsCanceled = null!, IsCanceledState = null!, IsWaiting = null!, IsForceRestarting = null!;
    public Func<Exception?> Error = null!;
    public Action<object?, bool> Start = null!, WaitForExit = null!;
    public Action SetWaiting = null!, Cancel = null!;
}
public sealed class MinecraftDownloadSourceHost {
    public Func<string, int> SettingInteger = null!;
    public Func<string, string?> SettingString = null!;
    public Action<string, string> SetSetting = null!;
    public Func<MinecraftDownloadRequest, string> Request = null!, RetryRequest = null!;
    public Func<string, bool, string> LoaderRequest = null!;
    public Func<long> Clock = null!;
    public Action<int> Sleep = null!;
    public Func<string, int> VersionToDrop = null!;
    public Func<string> TempPath = null!;
    public Func<string, bool> FileExists = null!;
    public Func<string, JToken?> ReadJson = null!;
    public Action<string> Info = null!, Warn = null!;
    public Action<Exception, string> WarnException = null!;
    public Func<Exception, bool, string> ErrorDisplay = null!;
    public Func<string, string?> ForgeRecommended = null!;
    public Func<JToken, DateTime> GetDate = null!;
    public Func<DateTime, DateTime> LocalTime = null!;
}
public sealed class MinecraftDownloadCatalogHost<TOptiFine, TForge, TNeoForge, TLiteLoader> {
    public Func<string, string?, string, TForge> Forge = null!;
    public Func<string, TNeoForge> NeoForge = null!;
    public Func<TOptiFine> OptiFine = null!;
    public Func<TLiteLoader> LiteLoader = null!;
}
public sealed class MinecraftDownloadCheck {
    public long MinSize = -1, ActualSize = -1;
    public string? Hash;
    public bool IsJson;
}
public sealed class MinecraftDownloadLoaderSettings {
    public double? ProgressWeight;
    public bool? Block, Show;
}
public sealed class MinecraftDownloadFileTask<TNetFile, TLoader> {
    public TLoader Native = default!;
    public Action<List<TNetFile>> SetOutput = null!;
    public Action Cancel = null!;
}
/// <summary>Existing instance, FileChecker, NetFile, library/assets scanners and native loader constructors.</summary>
public sealed class MinecraftDownloadClientHost<TInstance, TNetFile, TLoader, TChecker> {
    public Func<TInstance, string?> InheritName = null!;
    public Func<string, TInstance> CreateInstance = null!;
    public Func<TInstance, JObject> JsonObject = null!;
    public Func<TInstance, string> Name = null!, VersionPath = null!;
    public Func<TInstance, JToken> AssetIndex = null!;
    public Func<string> GameFolder = null!, TempPath = null!;
    public Func<MinecraftDownloadCheck, TChecker> Checker = null!;
    public Func<TChecker, string, string?> Check = null!;
    public Func<IEnumerable<string>, string, TChecker, TNetFile> NetFile = null!;
    public Func<TNetFile, string> LocalPath = null!, LocalName = null!;
    public Action<TNetFile, string> SetLocalPath = null!;
    public Func<TNetFile, string, string?> CheckFile = null!;
    public Func<TInstance, List<TNetFile>> LibraryFiles = null!;
    public Func<TInstance, bool> IgnoreFileCheck = null!;
    public Func<TInstance, bool, TLoader, List<TNetFile>> AssetFiles = null!;
    public Func<string, bool> FileExists = null!;
    public Func<string, DateTime> LastWriteTime = null!;
    public Action<string, string> MoveFile = null!;
    public Func<DateTime> Now = null!;
    public Func<string, Action<MinecraftDownloadFileTask<TNetFile, TLoader>>, MinecraftDownloadLoaderSettings, TLoader> Task = null!;
    public Func<string, List<TNetFile>, MinecraftDownloadLoaderSettings, TLoader> Download = null!;
    public Func<string, List<TLoader>, MinecraftDownloadLoaderSettings, TLoader> Combo = null!;
    public Action<TLoader> Start = null!;
    public Action<string> Info = null!, Warn = null!, WarnNone = null!;
    public Action<Exception, string> WarnException = null!;
}
