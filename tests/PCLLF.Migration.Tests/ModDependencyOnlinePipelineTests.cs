using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PCLCS;
using Context = ModDependencyInspectorPipelineTests.Context;
using Report = ModDependencyInspectorPipelineTests.Report;
using Local = PCLCS.ModDependencyInspection<ModDependencyInspectorPipelineTests.Context, ModDependencyInspectorPipelineTests.Mod, ModDependencyInspectorPipelineTests.Dependency, ModDependencyInspectorPipelineTests.Edge, ModDependencyInspectorPipelineTests.Issue, ModDependencyInspectorPipelineTests.Impact, ModDependencyInspectorPipelineTests.Report, ModDependencyInspectorPipelineTests.OnlineProject, ModDependencyInspectorPipelineTests.OnlineDependency, ModDependencyInspectorPipelineTests.OnlineFile>;
using Online = PCLCS.ModDependencyOnlinePipeline<ModDependencyInspectorPipelineTests.Context, ModDependencyInspectorPipelineTests.Mod, ModDependencyInspectorPipelineTests.Dependency, ModDependencyInspectorPipelineTests.Edge, ModDependencyInspectorPipelineTests.Issue, ModDependencyInspectorPipelineTests.Impact, ModDependencyInspectorPipelineTests.Report, ModDependencyInspectorPipelineTests.OnlineProject, ModDependencyInspectorPipelineTests.OnlineDependency, ModDependencyInspectorPipelineTests.OnlineFile, ModDependencyOnlinePipelineTests.Options, ModDependencyOnlinePipelineTests.Cache>;

// LF ModDependencyOnline.vb is the behavior source, pinned at
// f4aa0fae62c3ee8af24c81e61844c8fdd13c8983 (LF LICENCE). Existing script fixtures:
// Test-ModDependencyOnline.ps1 at b84e6d2b1a34ef3e571036a5ae18ce662b8f0583.
// PCL 5ef81e490ae46b1100d61e33d0183094c7a80ca8 has no corresponding supplement.
// CE 1a7653127d578efdb2a9388999a3fdc67d9e49d5 was reviewed by the production port;
// its project/version selection policies are not imported into these LF assertions.
internal static class ModDependencyOnlinePipelineTests {
    internal static IEnumerable<(string Name, Action Run)> Cases() {
        yield return ("platform review uses exact active files and keeps local errors authoritative", PlatformReview);
        yield return ("mod online pipeline maps real SHA1 matches without promoting project IDs into the local graph", ExactModrinth);
        yield return ("mod online pipeline streams real fingerprints and requires content size game and project identity", ContentGuards);
        yield return ("mod online pipeline clones cached JSON and expires content results while remapping current paths", CacheAndFreshPaths);
        yield return ("mod online pipeline retains local errors and honors offline HTTP429 and HTTP503 cooldowns", CooldownsAndLocalErrors);
        yield return ("mod online pipeline bounds actual files bytes requests and caller or elapsed cancellation", BudgetsAndCancellation);
        yield return ("mod online pipeline keeps candidates and explicit disabled targets separate from reliable startup metadata", CandidatesAndTargets);
    }
    private static void PlatformReview() {
        using Fixture fixture = new();
        string folder = fixture.Folder("platform-review"), owner = fixture.Jar(folder, "owner.jar"), peer = fixture.Jar(folder, "peer.jar", "local_peer");
        Network network = new(); ExactResponses(network, owner, peer);
        Report report = Engine(network).Enrich(Scan(folder), folder, options: network.CreateOptions());
        var file = OnlineFileInfo(report, owner); var project = file.Matches.Single(match => match.Platform == "Modrinth");
        var requirement = project.Dependencies.Single(dependency => dependency.ProjectId == "PeerP");
        project.Dependencies.Clear(); project.Dependencies.Add(requirement);
        foreach (var other in report.OnlineFiles.Where(item => item != file)) other.IsSupplementTarget = false;
        Equal(0, report.PlatformReviewNotes().Count);
        var mismatch = new ModDependencyInspectorPipelineTests.Issue {
            Owner = new ModDependencyInspectorPipelineTests.Mod { RootPath = owner }, IsError = true, KindValue = ModDependencyIssueKind.VersionMismatch
        };
        mismatch.RelatedMods.Add(new ModDependencyInspectorPipelineTests.Mod { IsBuiltin = true });
        report.Issues.Add(mismatch);
        Equal(0, report.PlatformReviewNotes().Count);
        mismatch.RelatedMods.Clear(); mismatch.RelatedMods.Add(new ModDependencyInspectorPipelineTests.Mod { RootPath = peer });
        True(report.PlatformReviewNotes().Single().Contains("内嵌声明"), "A platform match concealed a local version conflict.");
        report.Issues.Remove(mismatch);
        requirement.Kind = "incompatible";
        True(report.PlatformReviewNotes().Single().Contains("平台声明不兼容"), "An exact enabled platform conflict was hidden.");
        bool before = report.HasErrors;
        project.IsExact = false; Equal(0, report.PlatformReviewNotes().Count);
        project.IsExact = true; file.Enabled = false; Equal(0, report.PlatformReviewNotes().Count);
        file.Enabled = true; requirement.Kind = "required"; requirement.EnabledPaths.Clear();
        True(report.PlatformReviewNotes().Single().Contains("平台声明必需"), "Disabled or unconfirmed platform requirements were hidden.");
        Equal(before, report.HasErrors);
    }
    private static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException($"Expected {expected}, got {actual}."); }
    private static void True(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static Report Scan(string folder) {
        Context context = new() { MinecraftVersion = "1.20.1", JavaVersion = "17", LoaderKnown = true }; context.LoaderVersions["fabricloader"] = "0.16";
        return Local.Inspect(folder, context);
    }
    private static Online Engine(Network network) => new(network.Cache, (_, _, _, _) => { network.DefaultCalls++; throw new InvalidOperationException("Unexpected default transport; no live HTTP is allowed."); });
    private static ModDependencyInspectorPipelineTests.OnlineFile OnlineFileInfo(Report report, string path) => report.OnlineFiles.Single(item => item.FullPath == path);
    private static string Sha1(string path) => Convert.ToHexString(SHA1.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
    private static string Md5(string path) => Convert.ToHexString(MD5.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
    private static JObject MrVersion(string path, string project, string version, JArray? dependencies = null) => new() {
        ["id"] = version, ["project_id"] = project, ["version_number"] = "1.0", ["dependencies"] = dependencies ?? new JArray(),
        ["files"] = new JArray(new JObject { ["hashes"] = new JObject { ["sha1"] = Sha1(path) }, ["size"] = new FileInfo(path).Length })
    };
    private static JObject Project(string id, string type = "mod") => new() { ["id"] = id, ["title"] = id + " project", ["project_type"] = type };
    private static void ExactResponses(Network network, string owner, string peer) {
        network.Versions = new JObject {
            [Sha1(owner)] = MrVersion(owner, "OwnerP", "OwnerV", new JArray(
                new JObject { ["project_id"] = "PeerP", ["dependency_type"] = "required" }, new JObject { ["project_id"] = "OptionalP", ["dependency_type"] = "optional" },
                new JObject { ["version_id"] = "OnlyVersion", ["dependency_type"] = "required" }, new JObject { ["file_name"] = "peer.jar", ["dependency_type"] = "required" })),
            [Sha1(peer)] = MrVersion(peer, "PeerP", "PeerV", new JArray(new JObject { ["project_id"] = "OwnerP", ["dependency_type"] = "required" }))
        };
        network.Projects = new JArray(Project("OwnerP"), Project("PeerP"), Project("OptionalP"), Project("OnlyP"));
        network.VersionProjects = new JArray(new JObject { ["id"] = "OnlyVersion", ["project_id"] = "OnlyP" });
    }
    private static void ExactModrinth() {
        using Fixture fixture = new(); string folder = fixture.Folder("exact"), owner = fixture.Jar(folder, "owner.jar"), peer = fixture.Jar(folder, "peer.jar", "local_peer");
        Network network = new(); ExactResponses(network, owner, peer); Online engine = Engine(network);
        Report local = Scan(folder); var mods = local.Mods.ToArray(); var edges = local.Edges.ToArray(); var issues = local.Issues.ToArray(); bool errors = local.HasErrors;
        Dictionary<string, string> before = Snapshot(folder);
        Report report = engine.Enrich(local, folder, options: network.CreateOptions());
        True(ReferenceEquals(local, report), "Enrichment replaced the real local report.");
        var info = OnlineFileInfo(report, owner); var match = info.Matches.Single(item => item.Platform == "Modrinth");
        True(report.OnlineAttempted && info.IsSupplementTarget && match.IsExact, "Real content lookup did not produce an exact target match."); Equal("OwnerP project", match.ProjectName);
        Equal(4, match.Dependencies.Count); Equal(1, match.Dependencies.Count(item => item.Kind == "optional"));
        Equal(peer, match.Dependencies.Single(item => item.ProjectId == "PeerP").EnabledPaths.Single());
        Equal("OnlyVersion", match.Dependencies.Single(item => item.ProjectId == "OnlyP").VersionId);
        Equal(0, match.Dependencies.Single(item => item.FileName == "peer.jar").MatchedPaths.Count);
        True(report.Mods.SequenceEqual(mods) && report.Edges.SequenceEqual(edges) && report.Issues.SequenceEqual(issues) && report.HasErrors == errors && report.Mods.All(item => item.Id != "OwnerP"), "Platform project IDs or requirements changed the local loader graph.");
        True(report.DescribeFile(owner).Contains("OwnerP project", StringComparison.Ordinal) && report.DescribeFile(peer).Contains("owner.jar", StringComparison.Ordinal), "Exact forward/reverse platform details were lost.");
        True(report.DescribeOnlineImpacts(new[] { peer }).Contains("owner.jar", StringComparison.Ordinal), "Actual exact-file removal supplement was lost.");
        Equal("", report.DescribeOnlineImpacts(new[] { owner, peer })); Equal("", report.DescribeOnlineImpacts(new[] { owner }));
        JObject request = JObject.Parse(network.Calls.Single(item => item.Url == Network.MrFiles).Body!); Equal("sha1", (string?)request["algorithm"]);
        True(request["hashes"]!.Values<string>().Order().SequenceEqual(new[] { Sha1(owner), Sha1(peer) }.Order()), "The request did not hash the actual selected file set.");
        True(report.OnlineRequests <= 12, "Default lookup exceeded LF's request policy."); network.Healthy(); SameSnapshot(before, folder);
    }

    private static void ContentGuards() {
        using Fixture fixture = new(); string folder = fixture.Folder("content"), owner = fixture.Jar(folder, "cf-owner.jar"); uint observed = 0; int fingerprintRequests = 0;
        Report Lookup(Action<JObject>? mutate = null, int projectClass = 6) {
            Network network = new(); network.Respond = (url, _, body, _) => {
                if (url != Network.CfFingerprints) return network.Response(url);
                fingerprintRequests++; observed = JObject.Parse(body!)["fingerprints"]![0]!.Value<uint>();
                JObject file = new() { ["id"] = 101, ["modId"] = 42, ["gameId"] = 432, ["fileLength"] = new FileInfo(owner).Length, ["fileFingerprint"] = observed,
                    ["displayName"] = "CF version", ["hashes"] = new JArray(new JObject { ["algo"] = 1, ["value"] = Sha1(owner) }),
                    ["dependencies"] = new JArray(new JObject { ["modId"] = 43, ["relationType"] = 3 }, new JObject { ["modId"] = 44, ["relationType"] = 2 }) };
                mutate?.Invoke(file); return new JObject { ["data"] = new JObject { ["exactMatches"] = new JArray(new JObject { ["id"] = 42, ["file"] = file }) } };
            };
            network.CfProjects = new JObject { ["data"] = new JArray(new JObject { ["id"] = 42, ["name"] = "CF owner", ["classId"] = projectClass, ["links"] = new JObject { ["websiteUrl"] = "https://www.curseforge.com/minecraft/mc-mods/fixture" } }) };
            Options options = network.CreateOptions(); options.Fingerprint = null; // Exercise the actual streaming LF fingerprint, not a reimplementation.
            Report report = Engine(network).Enrich(Scan(folder), folder, options: options); network.Healthy(); return report;
        }
        var match = OnlineFileInfo(Lookup(), owner).Matches.Single();
        True(fingerprintRequests == 1 && match.IsExact && match.Platform == "CurseForge" && match.MatchBasis.Contains("内容哈希", StringComparison.Ordinal), "The streamed native fingerprint/content match was lost.");
        Equal(1, match.Dependencies.Count(item => item.Kind == "required")); Equal(1, match.Dependencies.Count(item => item.Kind == "optional"));
        Equal(0, OnlineFileInfo(Lookup(file => file["hashes"] = new JArray(new JObject { ["algo"] = 1, ["value"] = new string('a', 40) })), owner).Matches.Count);
        Equal(1, OnlineFileInfo(Lookup(file => file["hashes"] = new JArray(new JObject { ["algo"] = 2, ["value"] = Md5(owner) })), owner).Matches.Count);
        Equal(0, OnlineFileInfo(Lookup(file => file["fileFingerprint"] = observed ^ 1U), owner).Matches.Count);
        Equal(0, OnlineFileInfo(Lookup(file => file["fileLength"] = new FileInfo(owner).Length + 1), owner).Matches.Count);
        Equal(0, OnlineFileInfo(Lookup(file => file["gameId"] = 999), owner).Matches.Count); Equal(0, OnlineFileInfo(Lookup(projectClass: 12), owner).Matches.Count);
        Network forged = new() { Versions = new JObject { [Sha1(owner)] = MrVersion(owner, "ForgedP", "ForgedV") } };
        forged.Versions[Sha1(owner)]!["files"]![0]!["hashes"]!["sha1"] = new string('b', 40);
        Equal(0, OnlineFileInfo(Engine(forged).Enrich(Scan(folder), folder, options: forged.CreateOptions()), owner).Matches.Count); forged.Healthy();
        Network pack = new() { Versions = new JObject { [Sha1(owner)] = MrVersion(owner, "PackP", "PackV") }, Projects = new JArray(Project("PackP", "modpack")) };
        Equal(0, OnlineFileInfo(Engine(pack).Enrich(Scan(folder), folder, options: pack.CreateOptions()), owner).Matches.Count); pack.Healthy();
        int signed = 0;
        foreach (string invalid in new[] { "http://api.modrinth.com/v2/version_files", "https://api.curseforge.com.evil.invalid/v1/mods", "https://api.curseforge.com:444/v1/mods", "https://user@api.curseforge.com/v1/mods" }) {
            Equal(false, Online.IsAllowedApi(invalid)); bool rejected = false;
            try { ModDependencyTransport.DefaultRequest(invalid, HttpMethod.Get, null, CancellationToken.None, (_, _) => signed++, (code, retry) => new ModDependencyHttpFailure(code, retry)); } catch (ArgumentException) { rejected = true; }
            True(rejected && signed == 0, "A foreign transport address reached the real signing/HTTP boundary.");
        }
    }

    private static void CacheAndFreshPaths() {
        using Fixture fixture = new(); string folder = fixture.Folder("cache"), owner = fixture.Jar(folder, "owner.jar"), peer = fixture.Jar(folder, "peer.jar", "local_peer");
        Network network = new(); ExactResponses(network, owner, peer); Online engine = Engine(network); Options options = network.CreateOptions(); DateTime initial = network.Now;
        Report first = engine.Enrich(Scan(folder), folder, options: options); int calls = network.Calls.Count;
        OnlineFileInfo(first, owner).Matches.Single(item => item.Platform == "Modrinth").ProjectName = "caller mutated";
        OnlineFileInfo(first, owner).Matches.Single(item => item.Platform == "Modrinth").Dependencies.Single(item => item.ProjectId == "PeerP").MatchedPaths.Add("caller path");
        Report second = engine.Enrich(Scan(folder), folder, options: options); Equal(calls, network.Calls.Count); Equal(0, second.OnlineRequests);
        Equal("OwnerP project", OnlineFileInfo(second, owner).Matches.Single(item => item.Platform == "Modrinth").ProjectName);
        Equal(peer, OnlineFileInfo(second, owner).Matches.Single(item => item.Platform == "Modrinth").Dependencies.Single(item => item.ProjectId == "PeerP").MatchedPaths.Single());
        File.Move(peer, peer + ".disabled");
        Report disabled = engine.Enrich(Scan(folder), folder, options: options); var requirement = OnlineFileInfo(disabled, owner).Matches.Single(item => item.Platform == "Modrinth").Dependencies.Single(item => item.ProjectId == "PeerP");
        Equal(peer + ".disabled", requirement.MatchedPaths.Single()); Equal(0, requirement.EnabledPaths.Count); True(requirement.Status.Contains("禁用", StringComparison.Ordinal), "Cached platform JSON retained a stale enabled path.");
        File.Move(peer + ".disabled", peer);
        int mrCalls = network.Calls.Count(item => item.Url == Network.MrFiles); network.Now = initial.AddMinutes(11);
        engine.Enrich(Scan(folder), folder, options: options); Equal(mrCalls, network.Calls.Count(item => item.Url == Network.MrFiles)); True(network.Calls.Count > calls, "Negative ten-minute cache did not expire independently of positive results.");
        network.Now = initial.AddHours(7); calls = network.Calls.Count; engine.Enrich(Scan(folder), folder, options: options); True(network.Calls.Count > calls, "Positive six-hour content/project cache did not expire.");
        string oldHash = Sha1(owner); File.AppendAllText(owner, "new actual trailing bytes"); calls = network.Calls.Count;
        Report changed = engine.Enrich(Scan(folder), folder, options: options); True(OnlineFileInfo(changed, owner).Sha1 != oldHash && network.Calls.Count > calls && OnlineFileInfo(changed, owner).Matches.Count == 0, "Changed file bytes retained a cached exact platform match.");
        Cache cache = new(); JObject payload = new() { ["nested"] = new JObject { ["value"] = "original" } };
        cache.Write("clone", payload, initial, TimeSpan.FromSeconds(5)); payload["nested"]!["value"] = "writer mutation";
        JToken read = cache.Read("clone", initial)!; Equal("original", (string?)read["nested"]!["value"]); read["nested"]!["value"] = "reader mutation";
        Equal("original", (string?)cache.Read("clone", initial)!["nested"]!["value"]); Equal<JToken?>(null, cache.Read("clone", initial.AddSeconds(5)));
        cache.Write("clear", new JValue(true), initial, TimeSpan.FromMinutes(1)); cache.Clear(); Equal<JToken?>(null, cache.Read("clear", initial)); network.Healthy();
    }

    private static void CooldownsAndLocalErrors() {
        using Fixture fixture = new(); string folder = fixture.Folder("errors"); fixture.Jar(folder, "unknown.jar"); fixture.Jar(folder, "duplicate-one.jar", "duplicate"); fixture.Jar(folder, "duplicate-two.jar", "duplicate");
        foreach (Exception failure in new Exception[] { new IOException("synthetic offline"), new ModDependencyHttpFailure(429, 120), new ModDependencyHttpFailure(503, 0) }) {
            Network network = new() { Respond = (_, _, _, _) => throw failure }; Online engine = Engine(network); Options options = network.CreateOptions();
            Report local = Scan(folder); var issues = local.Issues.ToArray(); engine.Enrich(local, folder, options: options);
            True(local.HasErrors && local.Issues.SequenceEqual(issues) && local.OnlineNotes.Count > 0, "Online failure changed definite local errors."); int calls = network.Calls.Count;
            network.Now = network.Now.AddSeconds(failure is ModDependencyHttpFailure http && http.Code == 429 ? 60 : 29);
            engine.Enrich(Scan(folder), folder, options: options); Equal(calls, network.Calls.Count);
            network.Now = network.Now.AddSeconds(failure is ModDependencyHttpFailure retry && retry.Code == 429 ? 61 : 2);
            Report expired = engine.Enrich(Scan(folder), folder, options: options); True(network.Calls.Count > calls && expired.HasErrors, "Expired platform cooldown did not release requests while retaining local errors.");
            if (failure is ModDependencyHttpFailure limited && limited.Code == 429) True(expired.OnlineNotes.Any(item => item.Contains("过于频繁", StringComparison.Ordinal)), "429 lost its bounded Retry-After explanation.");
            network.Healthy();
        }
    }

    private static void BudgetsAndCancellation() {
        Options defaults = new(); Equal(6000, defaults.TimeBudgetMs); Equal(12, defaults.MaxRequests); Equal(64, defaults.MaxFiles); Equal(128L * 1024 * 1024, defaults.MaxFileBytes); Equal(256L * 1024 * 1024, defaults.MaxTotalBytes);
        using Fixture fixture = new(); string folder = fixture.Folder("budgets"); string first = fixture.Jar(folder, "a.jar"); fixture.Jar(folder, "b.jar"); fixture.Jar(folder, "c.jar");
        Network files = new(); Options oneFile = files.CreateOptions(); oneFile.MaxFiles = 1; Report limited = Engine(files).Enrich(Scan(folder), folder, options: oneFile);
        Equal(1, limited.OnlineFiles.Count); True(limited.OnlineNotes.Any(item => item.Contains("数量上限", StringComparison.Ordinal)), "MaxFiles did not bound actual hashed files."); files.Healthy();
        Network bytes = new(); Options tiny = bytes.CreateOptions(); tiny.MaxFileBytes = 1; Report skipped = Engine(bytes).Enrich(Scan(folder), folder, options: tiny);
        Equal(0, skipped.OnlineFiles.Count); Equal(0, bytes.Calls.Count); True(skipped.OnlineNotes.Any(item => item.Contains("哈希读取上限", StringComparison.Ordinal)), "Oversized file bytes reached online lookup.");
        Network total = new(); Options totalLimit = total.CreateOptions(); totalLimit.MaxTotalBytes = new FileInfo(first).Length; Equal(1, Engine(total).Enrich(Scan(folder), folder, options: totalLimit).OnlineFiles.Count); total.Healthy();
        Network requests = new(); Options requestLimit = requests.CreateOptions(); requestLimit.MaxRequests = 1; Report capped = Engine(requests).Enrich(Scan(folder), folder, options: requestLimit);
        Equal(1, capped.OnlineRequests); Equal(1, requests.Calls.Count); True(capped.OnlineNotes.Any(item => item.Contains("请求数量上限", StringComparison.Ordinal)), "The configured request cap was ignored."); requests.Healthy();
        using CancellationTokenSource canceled = new(); canceled.Cancel(); Network pre = new(); Report local = Scan(folder); var issues = local.Issues.ToArray();
        Report before = Engine(pre).Enrich(local, folder, canceled.Token, pre.CreateOptions()); True(before.OnlineCancelled && !before.OnlineTimedOut && before.Issues.SequenceEqual(issues) && pre.Calls.Count == 0, "Pre-cancelled enrichment requested data or changed local results.");
        using CancellationTokenSource midCancel = new(); Network mid = new() { Respond = (_, _, _, token) => { midCancel.Cancel(); token.ThrowIfCancellationRequested(); return null; } };
        Report during = Engine(mid).Enrich(Scan(folder), folder, midCancel.Token, mid.CreateOptions()); True(during.OnlineCancelled && mid.Calls.Count == 1, "Mid-request cancellation did not stop the actual request pipeline.");
        Network stalled = new() { Respond = (_, _, _, token) => { token.WaitHandle.WaitOne(2000); token.ThrowIfCancellationRequested(); return new JObject(); } };
        Options deadline = stalled.CreateOptions(); deadline.TimeBudgetMs = 150; deadline.RequestTimeoutMs = 1000;
        // Clock controls cache time only; the production total budget uses its real linked CTS/Stopwatch.
        Stopwatch watch = Stopwatch.StartNew(); Report timed = Engine(stalled).Enrich(Scan(folder), folder, options: deadline); watch.Stop();
        True(timed.OnlineTimedOut && !timed.OnlineCancelled && stalled.Calls.Count == 1 && watch.ElapsedMilliseconds < 1500, "The actual total deadline failed to bound a stalled injected request.");
        pre.Healthy(); mid.Healthy(); stalled.Healthy();
    }

    private static void CandidatesAndTargets() {
        using Fixture fixture = new(); string folder = fixture.Folder("targets"), known = fixture.Jar(folder, "complete.jar", "complete"), disabled = fixture.Jar(folder, "Example-Mod-fabric-1.20.1.jar.disabled");
        Network network = new(); Options options = network.CreateOptions(); Online engine = Engine(network);
        Report startup = engine.Enrich(Scan(folder), folder, options: options); True(!startup.OnlineAttempted && network.Calls.Count == 0 && !startup.MetadataIncomplete, "Reliable local or disabled unread metadata triggered startup lookup.");
        options.TargetFile = known; Equal(false, engine.Enrich(Scan(folder), folder, options: options).OnlineAttempted);
        network.Search = JObject.Parse("""{"hits":[{"project_id":"Candidate1","title":"Hint only","project_type":"mod"},{"project_id":"Pack","title":"Pack","project_type":"modpack"}]}""");
        network.CfSearch = JObject.Parse("""{"data":[{"id":75,"name":"CF hint","classId":6,"links":{"websiteUrl":"https://evil.invalid/not-used"}}]}""");
        options.TargetFile = disabled; options.MaxSearchFiles = 1; Report report = engine.Enrich(Scan(folder), folder, options: options);
        var info = OnlineFileInfo(report, disabled); Equal(0, info.Matches.Count); Equal(2, info.Candidates.Count);
        True(!info.Enabled && info.IsSupplementTarget && info.Candidates.All(item => !item.IsExact && item.Dependencies.Count == 0) && report.Mods.Count == 1 && report.Edges.Count == 0 && !report.HasErrors && !report.MetadataIncomplete,
            "A disabled search hint became a required local/project dependency.");
        Equal("https://www.curseforge.com/projects/75", info.Candidates.Single(item => item.Platform == "CurseForge").Website); Equal("", report.DescribeOnlineImpacts(new[] { known }));
        Equal("Example Mod", Online.SearchQuery(disabled));
        Network compare = new(); Options compareOptions = compare.CreateOptions(); compareOptions.TargetFile = known; compareOptions.ComparePlatformDependencies = true;
        Report manual = Engine(compare).InspectWithOnline(folder, Scan(folder).Context, options: compareOptions);
        True(manual.OnlineAttempted && OnlineFileInfo(manual, known).IsSupplementTarget && !OnlineFileInfo(manual, disabled).IsSupplementTarget && !manual.MetadataIncomplete && !manual.HasErrors, "Explicit platform comparison changed reliable startup policy.");
        compareOptions.TargetFile = fixture.Jar(fixture.Folder("outside"), "outside.jar.disabled"); int calls = compare.Calls.Count;
        Equal(false, Engine(compare).Enrich(Scan(folder), folder, options: compareOptions).OnlineAttempted); Equal(calls, compare.Calls.Count);
        network.Healthy(); compare.Healthy();
    }

    private sealed class Network {
        internal const string MrFiles = "https://api.modrinth.com/v2/version_files", CfFingerprints = "https://api.curseforge.com/v1/fingerprints/432";
        internal readonly Cache Cache = new(); internal DateTime Now = new(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);
        internal readonly List<(string Url, string? Body)> Calls = new(); internal readonly List<string> Unexpected = new(); internal int DefaultCalls;
        internal JObject Versions = new(); internal JArray Projects = new(), VersionProjects = new();
        internal JObject CfProjects = JObject.Parse("""{"data":[]}"""), Search = JObject.Parse("""{"hits":[]}"""), CfSearch = JObject.Parse("""{"data":[]}""");
        internal Func<string, HttpMethod, string?, CancellationToken, JToken?>? Respond;
        internal Options CreateOptions() => new() { Request = Request, Fingerprint = _ => 17U, Clock = () => Now, Cache = Cache, MaxSearchFiles = 0 };
        private JToken? Request(string url, HttpMethod method, string? body, CancellationToken token) {
            token.ThrowIfCancellationRequested(); Calls.Add((url, body)); return (Respond is null ? Response(url) : Respond(url, method, body, token))?.DeepClone();
        }
        internal JToken Response(string url) {
            if (url == MrFiles) return Versions;
            if (url.StartsWith("https://api.modrinth.com/v2/projects?", StringComparison.Ordinal)) return Projects;
            if (url.StartsWith("https://api.modrinth.com/v2/versions?", StringComparison.Ordinal)) return VersionProjects;
            if (url == CfFingerprints) return JObject.Parse("""{"data":{"exactMatches":[]}}""");
            if (url == "https://api.curseforge.com/v1/mods") return CfProjects;
            if (url.StartsWith("https://api.modrinth.com/v2/search?", StringComparison.Ordinal)) return Search;
            if (url.StartsWith("https://api.curseforge.com/v1/mods/search?", StringComparison.Ordinal)) return CfSearch;
            Unexpected.Add(url); throw new InvalidOperationException("Unexpected injected endpoint.");
        }
        internal void Healthy() => True(DefaultCalls == 0 && Unexpected.Count == 0, "The test used a default/live transport or an unexpected endpoint.");
    }
    private static Dictionary<string, string> Snapshot(string folder) => Directory.GetFiles(folder).ToDictionary(path => path, Sha1);
    private static void SameSnapshot(Dictionary<string, string> expected, string folder) {
        Equal(expected.Count, Directory.GetFiles(folder).Length);
        foreach (KeyValuePair<string, string> pair in expected) { Equal(pair.Value, Sha1(pair.Key)); using FileStream exclusive = new(pair.Key, FileMode.Open, FileAccess.ReadWrite, FileShare.None); }
    }
    private sealed class Fixture : IDisposable {
        private readonly string root = Path.Combine(Path.GetTempPath(), "PclModOnline-" + Guid.NewGuid().ToString("N"));
        private readonly string previousIndexCache = Local.Index.CacheDirectory;
        internal Fixture() {
            Directory.CreateDirectory(root);
            Local.Index.CacheDirectory = Path.Combine(root, "dependency-index");
            Local.Index.ClearMemory();
        }
        internal string Folder(string name) { string path = Path.Combine(root, name); Directory.CreateDirectory(path); return path; }
        internal string Jar(string folder, string name, string? id = null) {
            string path = Path.Combine(folder, name); using ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Create);
            using StreamWriter writer = new(archive.CreateEntry(id is null ? "fixture.txt" : "fabric.mod.json").Open(), new UTF8Encoding(false));
            writer.Write(id is null ? "unrecognized fixture " + name : new JObject { ["schemaVersion"] = 1, ["id"] = id, ["version"] = "1.0" }.ToString(Formatting.None)); return path;
        }
        public void Dispose() {
            Local.Index.ClearMemory();
            Local.Index.CacheDirectory = previousIndexCache;
            Directory.Delete(root, true);
        }
    }
    internal sealed class Cache : ModDependencyLookupCache { public Cache() { } }
    internal sealed class Options : ModDependencyLookupOptions<Cache> { public Options() { } }
}
