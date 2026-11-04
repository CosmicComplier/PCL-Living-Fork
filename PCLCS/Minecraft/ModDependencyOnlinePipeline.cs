using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace PCLCS;

public class ModDependencyLookupOptions<TCache> where TCache : ModDependencyLookupCache {
    public Func<string, HttpMethod, string?, CancellationToken, JToken?>? Request;
    public Func<string, uint>? Fingerprint;
    public Func<DateTime>? Clock = () => DateTime.UtcNow;
    public TCache? Cache;
    public string? TargetFile;
    public bool ComparePlatformDependencies;
    public int TimeBudgetMs = 6000, RequestTimeoutMs = 2000, MaxRequests = 12, MaxFiles = 64, MaxSearchFiles = 3, CandidatesPerPlatform = 3;
    public long MaxFileBytes = 128L * 1024 * 1024, MaxTotalBytes = 256L * 1024 * 1024;
    public TimeSpan CacheLifetime = TimeSpan.FromHours(6), NegativeCacheLifetime = TimeSpan.FromMinutes(10);
}

public class ModDependencyLookupCache {
    private sealed class CacheEntry {
        internal JToken Data = null!;
        internal DateTime Expires, Created;
    }
    private readonly Dictionary<string, CacheEntry> Entries = new(StringComparer.Ordinal);
    private readonly object Gate = new();
    public JToken? Read(string key, DateTime now) {
        lock (Gate) {
            if (!Entries.TryGetValue(key, out CacheEntry? value)) return null;
            if (value.Expires <= now) { Entries.Remove(key); return null; }
            return value.Data.DeepClone();
        }
    }
    public void Write(string key, JToken data, DateTime now, TimeSpan lifetime) {
        lock (Gate) {
            foreach (string expired in Entries.Where(pair => pair.Value.Expires <= now).Select(pair => pair.Key).ToArray()) Entries.Remove(expired);
            if (Entries.Count >= 512 && !Entries.ContainsKey(key)) Entries.Remove(Entries.OrderBy(pair => pair.Value.Created).First().Key);
            Entries[key] = new CacheEntry { Data = data.DeepClone(), Created = now, Expires = now.Add(lifetime) };
        }
    }
    public void Clear() { lock (Gate) Entries.Clear(); }
}

public class ModDependencyHttpFailure : Exception {
    public int Code, RetrySeconds;
    public ModDependencyHttpFailure(int code, int retrySeconds) : base("HTTP " + code) { Code = code; RetrySeconds = retrySeconds; }
}

// LF Modules/Minecraft/ModDependencyOnline.vb, unchanged from f4aa0fae62c3ee8af24c81e61844c8fdd13c8983,
// is the behavior source (LF LICENCE). PCL 5ef81e490ae46b1100d61e33d0183094c7a80ca8 has no such module.
// CE 1a7653127d578efdb2a9388999a3fdc67d9e49d5 Plain Craft Launcher 2/Modules/Minecraft/ModCompDependency.cs
// and PCL.Core/Minecraft/ResourceProject/ModDependencyResolver.cs were reviewed (CE LICENSE/PCL LICENCE).
// Their platform project/version selection differs from LF's bounded exact-content supplement; no policy is copied.
public sealed partial class ModDependencyOnlinePipeline<TContext, TMod, TDependency, TEdge, TIssue, TImpact, TReport, TOnlineProject, TOnlineDependency, TOnlineFile, TOptions, TCache>
    where TContext : ModDependencyInspection<TContext, TMod, TDependency, TEdge, TIssue, TImpact, TReport, TOnlineProject, TOnlineDependency, TOnlineFile>.ContextData, new()
    where TMod : ModDependencyInspection<TContext, TMod, TDependency, TEdge, TIssue, TImpact, TReport, TOnlineProject, TOnlineDependency, TOnlineFile>.ModData, new()
    where TDependency : ModDependencyInspection<TContext, TMod, TDependency, TEdge, TIssue, TImpact, TReport, TOnlineProject, TOnlineDependency, TOnlineFile>.DependencyData, new()
    where TEdge : ModDependencyInspection<TContext, TMod, TDependency, TEdge, TIssue, TImpact, TReport, TOnlineProject, TOnlineDependency, TOnlineFile>.EdgeData, new()
    where TIssue : ModDependencyInspection<TContext, TMod, TDependency, TEdge, TIssue, TImpact, TReport, TOnlineProject, TOnlineDependency, TOnlineFile>.IssueData, new()
    where TImpact : ModDependencyInspection<TContext, TMod, TDependency, TEdge, TIssue, TImpact, TReport, TOnlineProject, TOnlineDependency, TOnlineFile>.ImpactData, new()
    where TReport : ModDependencyInspection<TContext, TMod, TDependency, TEdge, TIssue, TImpact, TReport, TOnlineProject, TOnlineDependency, TOnlineFile>.ReportData, new()
    where TOnlineProject : ModDependencyInspection<TContext, TMod, TDependency, TEdge, TIssue, TImpact, TReport, TOnlineProject, TOnlineDependency, TOnlineFile>.OnlineProjectData, new()
    where TOnlineDependency : ModDependencyInspection<TContext, TMod, TDependency, TEdge, TIssue, TImpact, TReport, TOnlineProject, TOnlineDependency, TOnlineFile>.OnlineDependencyData, new()
    where TOnlineFile : ModDependencyInspection<TContext, TMod, TDependency, TEdge, TIssue, TImpact, TReport, TOnlineProject, TOnlineDependency, TOnlineFile>.OnlineFileData, new()
    where TOptions : ModDependencyLookupOptions<TCache>, new()
    where TCache : ModDependencyLookupCache, new() {
    private readonly TCache sharedCache;
    private readonly Func<string, HttpMethod, string?, CancellationToken, JToken?> defaultRequest;
    public ModDependencyOnlinePipeline(TCache sharedCache, Func<string, HttpMethod, string?, CancellationToken, JToken?> defaultRequest) {
        this.sharedCache = sharedCache; this.defaultRequest = defaultRequest;
    }
    private sealed class FileData {
        internal TOnlineFile Info = null!;
        internal string Md5 = null!;
        internal long Length, FilteredLength;
        internal uint Fingerprint;
        internal bool HasFingerprint;
    }
    private sealed class LookupBudgetException : Exception { }
    private sealed class Session : IDisposable {
        internal readonly TOptions Options;
        internal readonly TReport Report;
        internal readonly TCache Cache;
        internal readonly CancellationToken CallerToken;
        internal readonly CancellationTokenSource Deadline;
        internal readonly string? TargetFile;
        internal readonly Stopwatch Timer = Stopwatch.StartNew();
        private readonly HashSet<string> Blocked = new(StringComparer.Ordinal);
        private readonly Func<string, HttpMethod, string?, CancellationToken, JToken?> defaultRequest;
        internal Session(TReport report, CancellationToken token, TOptions options, string? targetFile, TCache sharedCache,
            Func<string, HttpMethod, string?, CancellationToken, JToken?> defaultRequest) {
            Report = report; Options = options; TargetFile = targetFile; CallerToken = token;
            Cache = options.Cache ?? sharedCache;
            this.defaultRequest = defaultRequest;
            Deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            Deadline.CancelAfter(Math.Max(100, Math.Min(10000, options.TimeBudgetMs)));
        }
        internal DateTime Now() => (Options.Clock ?? (() => DateTime.UtcNow))().ToUniversalTime();
        internal void Check() {
            Deadline.Token.ThrowIfCancellationRequested();
            if (Timer.ElapsedMilliseconds >= Math.Max(100, Math.Min(10000, Options.TimeBudgetMs))) throw new OperationCanceledException(Deadline.Token);
        }
        internal JToken? Ask(string platform, string url, HttpMethod method, string? body) {
            Check();
            if (!IsAllowedApi(url)) throw new ArgumentException("Unsupported dependency API address.");
            if (Blocked.Contains(platform)) return null;
            if (Cache.Read("cooldown|" + platform, Now()) is not null) {
                Note(platform + " 暂缓请求；已保留缓存和本地结果。"); Blocked.Add(platform); return null;
            }
            if (Report.OnlineRequests >= Math.Max(1, Math.Min(20, Options.MaxRequests))) throw new LookupBudgetException();
            Report.OnlineRequests = checked(Report.OnlineRequests + 1);
            using CancellationTokenSource requestDeadline = CancellationTokenSource.CreateLinkedTokenSource(Deadline.Token);
            requestDeadline.CancelAfter(Math.Max(50, Math.Min(3000, Options.RequestTimeoutMs)));
            try {
                Func<string, HttpMethod, string?, CancellationToken, JToken?> request = Options.Request ?? defaultRequest;
                JToken? result = request(url, method, body, requestDeadline.Token);
                Check(); return result;
            } catch (OperationCanceledException) {
                if (Deadline.IsCancellationRequested || CallerToken.IsCancellationRequested) throw;
                Blocked.Add(platform);
                Cache.Write("cooldown|" + platform, new JValue(true), Now(), TimeSpan.FromSeconds(30));
                Note(platform + " 查询超时；保留本地检查结果。"); return null;
            } catch (ModDependencyHttpFailure error) {
                Blocked.Add(platform);
                int seconds = error.Code == 429 ? Math.Max(60, Math.Min(900, error.RetrySeconds)) : 30;
                Cache.Write("cooldown|" + platform, new JValue(true), Now(), TimeSpan.FromSeconds(seconds));
                Note(platform + (error.Code == 429 ? " 请求过于频繁，已停止本轮查询。" : " 查询失败（HTTP " + error.Code + "）；保留本地结果。")); return null;
            } catch (Exception) {
                Blocked.Add(platform);
                Cache.Write("cooldown|" + platform, new JValue(true), Now(), TimeSpan.FromSeconds(30));
                Note(platform + " 暂不可用；保留本地检查结果。"); return null;
            }
        }
        internal void Note(string value) { if (!Report.OnlineNotes.Contains(value)) Report.OnlineNotes.Add(value); }
        internal TimeSpan Lifetime(bool positive) {
            TimeSpan value = positive ? Options.CacheLifetime : Options.NegativeCacheLifetime;
            return TimeSpan.FromSeconds(Math.Max(1, Math.Min(positive ? 86400 : 3600, value.TotalSeconds)));
        }
        public void Dispose() => Deadline.Dispose();
    }
    public TReport InspectWithOnline(string modsFolder, TContext? context = null, CancellationToken token = default, TOptions? options = null) =>
        Enrich(ModDependencyInspection<TContext, TMod, TDependency, TEdge, TIssue, TImpact, TReport, TOnlineProject, TOnlineDependency, TOnlineFile>.Inspect(modsFolder, context, token), modsFolder, token, options);
    public TReport Enrich(TReport report, string modsFolder, CancellationToken token = default, TOptions? options = null) {
        if (report is null) throw new ArgumentNullException("Report");
        TOptions settings = options ?? new TOptions();
        string? targetFile = ExplicitTarget(settings.TargetFile, modsFolder, report, settings.ComparePlatformDependencies);
        if (!string.IsNullOrWhiteSpace(settings.TargetFile) && targetFile is null) return report;
        if (!report.MetadataIncomplete && targetFile is null && !settings.ComparePlatformDependencies) return report;
        report.OnlineAttempted = true; report.OnlineCancelled = false; report.OnlineTimedOut = false; report.OnlineRequests = 0;
        report.OnlineFiles.Clear(); report.OnlineNotes.Clear();
        using Session work = new(report, token, settings, targetFile, sharedCache, defaultRequest);
        report.OnlineCheckedAtUtc = work.Now();
        try {
            work.Check();
            List<FileData> files = ReadFiles(modsFolder, work);
            if (!files.Any(item => item.Info.IsSupplementTarget)) { work.Note("没有找到适合联网补充的本地文件。"); return report; }
            report.OnlineFiles.AddRange(files.Select(item => item.Info));
            MatchModrinth(files, work); MatchCurseForge(files, work); ResolveVersionProjects(files, work);
            ResolveProjects(files, "Modrinth", work); ResolveProjects(files, "CurseForge", work); SearchCandidates(files, work);
        } catch (OperationCanceledException) {
            report.OnlineCancelled = token.IsCancellationRequested; report.OnlineTimedOut = !report.OnlineCancelled;
            work.Note(report.OnlineCancelled ? "联网补充已取消，本地检查结果已保留。" : "联网补充达到总时间预算，已停止后续请求。");
        } catch (LookupBudgetException) { work.Note("联网补充达到请求数量上限，已停止后续请求。"); }
        catch (Exception) { work.Note("联网补充未完成，本地检查结果已保留。"); }
        finally { MapRelations(report); }
        return report;
    }
    private static bool SameFilePath(string first, string second) =>
        ModDependencyInspection<TContext, TMod, TDependency, TEdge, TIssue, TImpact, TReport, TOnlineProject, TOnlineDependency, TOnlineFile>.SameFilePath(first, second);
    public static string SearchQuery(string fullPath) {
        string value = Path.GetFileName(fullPath);
        if (value.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase) || value.EndsWith(".old", StringComparison.OrdinalIgnoreCase)) value = Path.GetFileNameWithoutExtension(value);
        value = Path.GetFileNameWithoutExtension(value);
        value = Regex.Replace(value, @"(?i)(?:^|[-_+ \[\]])(?:fabric|forge|neoforge|quilt)(?=$|[-_+ \[\]])", " ");
        value = Regex.Replace(value, @"(?i)[-_+ ]+(?:mc[-_ ]*)?v?\d+(?:\.\d+)+(?:[-_+][A-Za-z0-9.-]+)?$", "");
        value = Regex.Replace(value, @"[^\p{L}0-9 ]", " ");
        value = Regex.Replace(value, @"\s+", " ").Trim();
        if (value.Length > 64) value = value.Substring(0, 64).Trim();
        return Regex.IsMatch(value, @"^\d+$") ? "" : value;
    }
    private static string S(JToken? value) => value is null || value.Type == JTokenType.Null ? "" : value.ToString();
    private static string Caption(string? value) {
        value = Regex.Replace(value ?? "", "[\r\n\t]+", " ").Trim();
        return value.Length > 160 ? value.Substring(0, 160) + "…" : value;
    }
    private static IEnumerable<JToken> A(JToken? value) => value is JArray array ? array.AsEnumerable() : Enumerable.Empty<JToken>();
    private static bool ValidModrinthId(string? value) => Regex.IsMatch(value ?? "", "^[A-Za-z0-9]{1,64}$");
    private static bool ValidCurseForgeId(string? value) => int.TryParse(value, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int number) && number > 0;
    private static string JsonIds(IEnumerable<string> ids) => Uri.EscapeDataString(new JArray(ids).ToString(Newtonsoft.Json.Formatting.None));
    public static bool IsAllowedApi(string? url) => ModDependencyTransport.IsAllowedApi(url);
    private static bool IsCurseForgeWebsite(string? url) {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? parsed)) return false;
        Uri address = parsed!;
        return address.Scheme == "https" && address.IsDefaultPort && address.UserInfo == "" &&
            new[] { "www.curseforge.com", "curseforge.com" }.Contains(address.Host, StringComparer.OrdinalIgnoreCase) && address.AbsolutePath.StartsWith("/minecraft/mc-mods/", StringComparison.OrdinalIgnoreCase);
    }
    private static bool IsCurseForgeModProject(JToken project) {
        if (project["classId"] is not null && project["classId"]!.Type != JTokenType.Null) return S(project["classId"]) == "6";
        return IsCurseForgeWebsite(S(project["links"]?["websiteUrl"]));
    }
}
