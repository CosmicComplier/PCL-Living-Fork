using System.Diagnostics;
using System.Globalization;
using Newtonsoft.Json.Linq;
using PCLCS;
using Transport = ModDependencyOnlineFixtureNetwork;

namespace PCLLF.Native.Facade.Tests;

// LF LICENCE: complete fixed-50b028d Test-ModDependencyOnline.ps1 fixture,
// source-line IDs remain the original assertions including every batch/API loop.
// Every scan/enrichment/index/relationship operation uses the genuine Main facade.
internal static partial class DependencyCases<TContext, TMod, TDependency, TEdge, TIssue, TImpact, TReport, TOnlineProject, TOnlineDependency, TOnlineFile, TOptions, TCache>
{
    internal static void Online(FixtureEnvironment environment)
    {
        using Scope f = new(environment, "dependency-online");
        onlineCount = 0;
        OnlineSeen.Clear();
        TReport S(string folder)
        {
            TContext context = new() { MinecraftVersion = "1.20.1", JavaVersion = "17", LoaderKnown = true };
            context.LoaderVersions.Add("fabricloader", "0.16");
            return Scan(folder, context);
        }

        string folder = f.Folder("exact");
        string owner = OnlineJar(folder, "owner.jar"), peer = OnlineJar(folder, "peer.jar", "local_peer");
        string ownerHash = Hash(owner), peerHash = Hash(peer);
        Transport network = new();
        network.Versions = new JObject
        {
            [ownerHash] = MrVersion(owner, "OwnerP", "OwnerV", new JArray(
                Obj("""{"project_id":"PeerP","dependency_type":"required"}"""),
                Obj("""{"project_id":"OptP","dependency_type":"optional"}"""),
                Obj("""{"version_id":"OnlyVer","dependency_type":"required"}"""),
                Obj("""{"file_name":"peer.jar","dependency_type":"required"}"""))),
            [peerHash] = MrVersion(peer, "PeerP", "PeerV", new JArray(Obj("""{"project_id":"OwnerP","dependency_type":"required"}""")))
        };
        network.VersionProjects = new JArray(Obj("""{"id":"OnlyVer","project_id":"OnlyP"}"""));
        network.Projects = new JArray(
            Obj("""{"id":"OwnerP","title":"Owner project","project_type":"mod"}"""),
            Obj("""{"id":"PeerP","title":"Peer project","project_type":"mod"}"""),
            Obj("""{"id":"OptP","title":"Optional project","project_type":"mod"}"""),
            Obj("""{"id":"OnlyP","title":"Version-only project","project_type":"mod"}"""));
        TOptions options = Options(network);
        TReport report = S(folder);
        int localMods = report.Mods.Count, localIssues = report.Issues.Count;
        bool localErrors = report.HasErrors;
        report = Enrich(report, folder, options);
        TOnlineFile info = Info(report, owner);
        TOnlineProject match = info.Matches.First(item => item.Platform == "Modrinth");
        O(161, report.OnlineAttempted && info.IsSupplementTarget && match.IsExact && match.ProjectName == "Owner project");
        O(162, match.Dependencies.Count == 4 && match.Dependencies.Count(item => item.Kind == "optional") == 1);
        TOnlineDependency required = match.Dependencies.First(item => item.ProjectId == "PeerP");
        O(164, required.EnabledPaths.Count == 1 && required.EnabledPaths[0] == peer);
        O(165, match.Dependencies.Count(item => item.ProjectId == "OnlyP") == 1);
        TOnlineDependency unmapped = match.Dependencies.First(item => item.ProjectId == "");
        O(167, unmapped.MatchedPaths.Count == 0);
        O(168, report.Mods.Count == localMods && report.Issues.Count == localIssues && report.HasErrors == localErrors);
        O(169, report.Mods.Count(item => item.Id == "OwnerP") == 0);
        O(170, report.DescribeFile(owner).Contains("Modrinth", StringComparison.Ordinal) && report.DescribeFile(owner).Contains("Owner project", StringComparison.Ordinal));
        O(171, report.DescribeFile(peer).Contains("owner.jar", StringComparison.Ordinal));
        O(172, report.DescribeOnlineImpacts(new[] { peer }).Contains("owner.jar", StringComparison.Ordinal));
        O(173, report.DescribeFile(Extended(owner)) == report.DescribeFile(owner));
        O(174, report.DescribeOnlineImpacts(new[] { Extended(peer) }).Contains("owner.jar", StringComparison.Ordinal));
        O(175, DescribeImpacts(report, peer).Contains("下载源", StringComparison.Ordinal));
        O(176, report.DescribeOnlineImpacts(new[] { owner, peer }) == "");
        O(177, report.DescribeOnlineImpacts(new[] { owner }) == "");
        O(178, network.Unexpected.Count == 0);
        int requestCount = network.Calls.Count;
        report = Enrich(S(folder), folder, options);
        O(180, network.Calls.Count == requestCount && report.OnlineRequests == 0);
        File.Move(peer, peer + ".disabled");
        TReport disabledReport = Enrich(S(folder), folder, options);
        TOnlineDependency disabledRequirement = Info(disabledReport, owner).Matches.First(item => item.Platform == "Modrinth")
            .Dependencies.First(item => item.ProjectId == "PeerP");
        O(184, disabledRequirement.MatchedPaths.Count == 1 && disabledRequirement.EnabledPaths.Count == 0 && disabledRequirement.Status.Contains("禁用", StringComparison.Ordinal));
        O(185, disabledReport.DescribeOnlineImpacts(new[] { peer + ".disabled" }) == "");
        File.Move(peer + ".disabled", peer);
        network.UtcNow = network.UtcNow.AddHours(7);
        report = Enrich(S(folder), folder, options);
        O(188, network.Calls.Count > requestCount);
        string oldHash = ownerHash;
        File.AppendAllText(owner, "fixture changed bytes");
        requestCount = network.Calls.Count;
        report = Enrich(S(folder), folder, options);
        O(191, Info(report, owner).Sha1 != oldHash && network.Calls.Count > requestCount);
        O(192, Info(report, owner).Matches.Count == 0 && !report.HasErrors);

        folder = f.Folder("search");
        string unknown = OnlineJar(folder, "Example-Mod-fabric-1.20.1.jar");
        network = new();
        network.Search = Obj("""{"hits":[{"project_id":"Candidate1","title":"First candidate","project_type":"mod"},{"project_id":"Pack","title":"Not a mod","project_type":"modpack"}]}""");
        network.CfSearch = Obj("""{"data":[{"id":75,"name":"CF candidate","classId":6,"links":{"websiteUrl":"https://evil.invalid/key-trap"}}]}""");
        options = Options(network);
        options.MaxSearchFiles = 1;
        report = Enrich(S(folder), folder, options);
        info = Info(report, unknown);
        O(199, info.Matches.Count == 0 && info.Candidates.Count == 2 && !info.Candidates.Any(item => item.IsExact));
        O(200, !report.HasErrors && report.Issues.Count == 0);
        O(201, info.Candidates[1].Website.StartsWith("https://www.curseforge.com/", StringComparison.Ordinal));
        O(202, report.DescribeFile(unknown).Contains("未选定", StringComparison.Ordinal) && report.DescribeOnlineImpacts(new[] { unknown }) == "");
        string query = (string)Native.Call(OnlineApi, "SearchQuery", null, "Example-Mod-fabric-1.20.1.jar.disabled")!;
        O(204, query == "Example Mod");
        O(205, network.Calls.Count <= 12 && network.Unexpected.Count == 0);
        network = new();
        string candidateHash = Hash(unknown);
        JObject forged = MrVersion(unknown, "ForgedP", "ForgedV");
        forged["files"]![0]!["hashes"]!["sha1"] = new string('a', 40);
        network.Versions = new JObject { [candidateHash] = forged };
        report = Enrich(S(folder), folder, Options(network));
        O(209, Info(report, unknown).Matches.Count == 0);
        forged = MrVersion(unknown, "PackP", "PackV");
        network.Versions = new JObject { [candidateHash] = forged };
        network.Projects = new JArray(Obj("""{"id":"PackP","title":"Pack","project_type":"modpack"}"""));
        report = Enrich(S(folder), folder, Options(network));
        O(212, Info(report, unknown).Matches.Count == 0);

        folder = f.Folder("curseforge");
        owner = OnlineJar(folder, "cf-owner.jar");
        peer = OnlineJar(folder, "cf-peer.jar", "local_cf_peer");
        network = new();
        JObject cfOwner = CfFile(owner, 42, 101, new JArray(Obj("""{"modId":43,"relationType":3}"""), Obj("""{"modId":44,"relationType":2}""")));
        JObject cfPeer = CfFile(peer, 43, 102);
        network.Fingerprints = new JObject { ["data"] = new JObject { ["exactMatches"] = new JArray(new JObject { ["id"] = 42, ["file"] = cfOwner }, new JObject { ["id"] = 43, ["file"] = cfPeer }) } };
        network.CfProjects = Obj("""{"data":[{"id":42,"name":"CF owner","classId":6,"links":{"websiteUrl":"https://www.curseforge.com/minecraft/mc-mods/fixture-owner"}},{"id":43,"name":"CF peer","classId":6},{"id":44,"name":"CF optional","classId":6}]}""");
        options = Options(network);
        report = Enrich(S(folder), folder, options);
        info = Info(report, owner);
        match = info.Matches.First(item => item.Platform == "CurseForge");
        O(220, match.IsExact && match.ProjectName == "CF owner" && match.MatchBasis.Contains("内容哈希", StringComparison.Ordinal));
        O(221, match.Dependencies.Count(item => item.Kind == "required" && item.EnabledPaths.Count == 1) == 1 && match.Dependencies.Count(item => item.Kind == "optional") == 1);
        O(222, report.DescribeFile(peer).Contains("cf-owner.jar", StringComparison.Ordinal) && !report.HasErrors);
        string fingerprintBody = network.Bodies[network.Calls.IndexOf("https://api.curseforge.com/v1/fingerprints/432")];
        O(224, fingerprintBody.Contains(Transport.Fingerprint(owner).ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal));
        cfOwner["hashes"] = new JArray(new JObject { ["algo"] = 1, ["value"] = new string('a', 40) });
        network.Fingerprints = new JObject { ["data"] = new JObject { ["exactMatches"] = new JArray(new JObject { ["id"] = 42, ["file"] = cfOwner }) } };
        report = Enrich(S(folder), folder, Options(network));
        O(227, Info(report, owner).Matches.Count == 0);
        cfOwner["hashes"] = new JArray(new JObject { ["algo"] = 2, ["value"] = Hash(owner, "MD5") });
        network.Fingerprints = new JObject { ["data"] = new JObject { ["exactMatches"] = new JArray(new JObject { ["id"] = 42, ["file"] = cfOwner }) } };
        report = Enrich(S(folder), folder, Options(network));
        O(230, Info(report, owner).Matches.Count == 1);
        cfOwner["gameId"] = 999;
        network.Fingerprints = new JObject { ["data"] = new JObject { ["exactMatches"] = new JArray(new JObject { ["id"] = 42, ["file"] = cfOwner }) } };
        O(232, Info(Enrich(S(folder), folder, Options(network)), owner).Matches.Count == 0);
        cfOwner["gameId"] = 432;
        network.Fingerprints = new JObject { ["data"] = new JObject { ["exactMatches"] = new JArray(new JObject { ["id"] = 42, ["file"] = cfOwner }) } };
        network.CfProjects = Obj("""{"data":[{"id":42,"name":"Not a mod","classId":12,"links":{"websiteUrl":"https://www.curseforge.com/minecraft/mc-mods/misleading-path"}}]}""");
        O(234, Info(Enrich(S(folder), folder, Options(network)), owner).Matches.Count == 0);

        folder = f.Folder("failure");
        unknown = OnlineJar(folder, "offline.jar");
        OnlineJar(folder, "duplicate-one.jar", "duplicate");
        OnlineJar(folder, "duplicate-two.jar", "duplicate");
        network = new() { Fail = true };
        options = Options(network);
        TReport local = S(folder);
        int beforeErrors = local.Issues.Count;
        report = Enrich(local, folder, options);
        O(239, report.HasErrors && report.Issues.Count == beforeErrors && report.OnlineNotes.Count > 0 && report.OnlineIncomplete);
        int calls = network.Calls.Count;
        report = Enrich(S(folder), folder, options);
        O(241, network.Calls.Count == calls);
        network = new() { Failure = (Exception)Activator.CreateInstance(Native.Type("PCL.ModDependencyOnline+HttpFailure"), Native.Flags, null, new object[] { 429, 120 }, null)! };
        options = Options(network);
        report = Enrich(S(folder), folder, options);
        O(245, string.Join(' ', report.OnlineNotes).Contains("过于频繁", StringComparison.Ordinal));
        calls = network.Calls.Count;
        network.UtcNow = network.UtcNow.AddSeconds(60);
        report = Enrich(S(folder), folder, options);
        O(247, network.Calls.Count == calls);
        network = new();
        using (CancellationTokenSource cancel = new())
        {
            cancel.Cancel();
            report = Enrich(S(folder), folder, Options(network), cancel.Token);
        }
        O(250, report.OnlineCancelled && network.Calls.Count == 0 && report.Issues.Count == beforeErrors);
        network = new();
        using (CancellationTokenSource cancel = new())
        {
            network.Cancel = cancel;
            report = Enrich(S(folder), folder, Options(network), cancel.Token);
        }
        O(253, report.OnlineCancelled && network.Calls.Count == 1 && report.HasErrors);
        network = new() { Delay = true };
        options = Options(network);
        options.TimeBudgetMs = 150;
        options.RequestTimeoutMs = 1000;
        Stopwatch watch = Stopwatch.StartNew();
        report = Enrich(S(folder), folder, options);
        watch.Stop();
        O(256, report.OnlineTimedOut && watch.ElapsedMilliseconds < 1500 && report.HasErrors);
        network = new();
        options = Options(network);
        options.MaxRequests = 1;
        report = Enrich(S(folder), folder, options);
        O(258, report.OnlineRequests == 1 && string.Join(' ', report.OnlineNotes).Contains("数量上限", StringComparison.Ordinal));

        folder = f.Folder("complete");
        string path = OnlineJar(folder, "complete.jar", "complete");
        network = new();
        local = S(folder);
        report = Enrich(local, folder, Options(network));
        O(261, !report.OnlineAttempted && network.Calls.Count == 0);
        string disabled = OnlineJar(folder, "unknown.jar.disabled");
        local = S(folder);
        O(263, !local.MetadataIncomplete);
        network = new();
        report = Enrich(local, folder, Options(network));
        O(265, !report.OnlineAttempted && network.Calls.Count == 0);
        string disabledHash = Hash(disabled), knownHash = Hash(path);
        network.Versions = new JObject
        {
            [disabledHash] = MrVersion(disabled, "DisabledP", "DisabledV", new JArray(Obj("""{"project_id":"CompleteP","dependency_type":"required"}"""))),
            [knownHash] = MrVersion(path, "CompleteP", "CompleteV")
        };
        network.Projects = new JArray(Obj("""{"id":"DisabledP","title":"Disabled project","project_type":"mod"}"""), Obj("""{"id":"CompleteP","title":"Complete project","project_type":"mod"}"""));
        options = Options(network);
        options.TargetFile = disabled;
        report = Enrich(S(folder), folder, options);
        info = Info(report, disabled);
        O(270, report.OnlineAttempted && network.Calls.Count > 0 && info.IsSupplementTarget && !info.Enabled && info.Matches.Count == 1);
        O(271, info.Matches[0].Dependencies[0].EnabledPaths.Count == 1 && report.DescribeFile(disabled).Contains("Disabled project", StringComparison.Ordinal));
        O(272, !report.MetadataIncomplete && !report.HasErrors && report.Issues.Count == 0);
        TReport combined = (TReport)Native.Call(OnlineApi, "InspectWithOnline", null, folder, report.Context, CancellationToken.None, options)!;
        O(274, combined.OnlineAttempted && Info(combined, disabled).Matches.Count == 1);
        options.TargetFile = Extended(disabled);
        TReport extended = Enrich(S(folder), folder, options);
        O(277, extended.OnlineAttempted && Info(extended, disabled).Matches.Count == 1);
        O(278, extended.DescribeFile(options.TargetFile).Contains("Disabled project", StringComparison.Ordinal));
        extended = Enrich(S(Extended(folder)), Extended(folder), options);
        O(280, extended.OnlineFiles.Count(item => item.IsSupplementTarget && item.Matches.Count == 1) == 1);
        network = new();
        options = Options(network);
        options.TargetFile = path;
        report = Enrich(S(folder), folder, options);
        O(282, !report.OnlineAttempted && network.Calls.Count == 0);
        TOptions compareOptions = Options(network);
        compareOptions.TargetFile = path;
        compareOptions.ComparePlatformDependencies = true;
        compareOptions.MaxSearchFiles = 0;
        TReport compare = Enrich(S(folder), folder, compareOptions);
        O(285, compare.OnlineAttempted && network.Calls.Count > 0 && Info(compare, path).IsSupplementTarget);
        O(286, !compare.HasErrors && !compare.MetadataIncomplete && !Info(compare, disabled).IsSupplementTarget);
        Transport batchNetwork = new();
        TOptions batchOptions = Options(batchNetwork);
        batchOptions.ComparePlatformDependencies = true;
        batchOptions.MaxSearchFiles = 0;
        TReport batch = Enrich(S(folder), folder, batchOptions);
        O(289, batch.OnlineAttempted && Info(batch, path).IsSupplementTarget && !Info(batch, disabled).IsSupplementTarget);
        network = new();
        string outside = OnlineJar(f.Folder("outside-target"), "outside.jar.disabled");
        options = Options(network);
        options.TargetFile = outside;
        report = Enrich(S(folder), folder, options);
        O(292, !report.OnlineAttempted && network.Calls.Count == 0);
        options.ComparePlatformDependencies = true;
        report = Enrich(S(folder), folder, options);
        O(294, !report.OnlineAttempted && network.Calls.Count == 0);
        network = new() { Search = Obj("""{"hits":[{"project_id":"DisabledCandidate","title":"Disabled search candidate","project_type":"mod"}]}""") };
        options = Options(network);
        options.TargetFile = disabled;
        options.MaxSearchFiles = 1;
        report = Enrich(S(folder), folder, options);
        O(297, Info(report, disabled).Matches.Count == 0 && Info(report, disabled).Candidates.Count == 1 && !report.HasErrors);
        string mixed = f.Folder("mixed-disabled");
        OnlineJar(mixed, "active-unknown.jar");
        string mixedDisabled = OnlineJar(mixed, "disabled-unknown.jar.disabled");
        network = new();
        report = Enrich(S(mixed), mixed, Options(network));
        O(300, report.OnlineAttempted && !Info(report, mixedDisabled).IsSupplementTarget);

        string indexedFolder = f.Folder("persistent-lookup");
        string indexedFile = OnlineJar(indexedFolder, "indexed.jar", "indexed_mod");
        TReport indexed = S(indexedFolder);
        info = new TOnlineFile { FullPath = indexedFile, Enabled = true, IsSupplementTarget = true };
        TOnlineProject project = new() { Platform = "Modrinth", ProjectId = "indexed_project", ProjectName = "Cached project", IsExact = true };
        info.Matches.Add(project);
        indexed.OnlineFiles.Add(info);
        indexed.OnlineAttempted = true;
        indexed.OnlineIndexScope = "all";
        indexed.OnlineCheckedAtUtc = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Native.Call(Index, "RememberOnline", null, indexedFolder, indexed, CancellationToken.None);
        ClearIndex();
        TReport saved = S(indexedFolder);
        using (FileStream lease = File.Open(indexedFile, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            TOptions cachedOptions = new() { ComparePlatformDependencies = true };
            TReport reused = Enrich(saved, indexedFolder, cachedOptions);
            O(310, reused.OnlineFiles.Count == 1 && reused.OnlineFiles[0].Matches[0].ProjectName == "Cached project" && reused.OnlineRequests == 0 && reused.OnlineCheckedAtUtc.Year == 2020 && reused.OnlineNotes.Count == 0);
        }
        TReport rebuilt = Rebuild(indexedFolder, saved.Context);
        O(313, !rebuilt.OnlineAttempted && rebuilt.OnlineFiles.Count == 0);

        // More than two API pages preserves the original 64-file and 100-ID cap regressions.
        string refreshFolder = f.Folder("refresh-all");
        network = new();
        JObject values = new();
        JArray projects = new(), cfFiles = new(), cfProjects = new(), versionProjects = new();
        for (int i = 0; i < 205; i++)
        {
            string suffix = i % 31 == 0 ? ".jar.disabled" : ".jar";
            path = OnlineJar(refreshFolder, "batch-" + i + suffix, "batch_" + i);
            values[Hash(path)] = MrVersion(path, "BatchP" + i, "BatchV" + i, new JArray(new JObject { ["version_id"] = "ExternalV" + i, ["dependency_type"] = "optional" }));
            projects.Add(new JObject { ["id"] = "BatchP" + i, ["title"] = "Batch project " + i, ["project_type"] = "mod" });
            projects.Add(new JObject { ["id"] = "ExternalP" + i, ["title"] = "External project " + i, ["project_type"] = "mod" });
            versionProjects.Add(new JObject { ["id"] = "ExternalV" + i, ["project_id"] = "ExternalP" + i, ["version_number"] = "2.0." + i });
            cfFiles.Add(new JObject { ["id"] = 1000 + i, ["file"] = CfFile(path, 1000 + i, 2000 + i) });
            cfProjects.Add(new JObject { ["id"] = 1000 + i, ["name"] = "CF batch project " + i, ["classId"] = 6 });
        }
        network.Versions = values;
        network.Projects = projects;
        network.VersionProjects = versionProjects;
        network.Fingerprints = new JObject { ["data"] = new JObject { ["exactMatches"] = cfFiles } };
        network.CfProjects = new JObject { ["data"] = cfProjects };
        options = Options(network);
        options.Progress = network.Progress;
        TReport before = S(refreshFolder);
        batch = (TReport)Native.Call(OnlineApi, "RebuildWithOnline", null, refreshFolder, before.Context, CancellationToken.None, options)!;
        O(328, batch.OnlineFiles.Count == 205 && batch.OnlineFiles.Count(item => item.IsSupplementTarget && item.Matches.Count == 2) == 205);
        O(329, batch.OnlineFiles.Count(item => !item.Enabled && item.IsSupplementTarget) == 7);
        O(330, batch.OnlineFiles.SelectMany(item => item.Matches).Count(item => item.ProjectName == "Batch project 204") == 1 && batch.OnlineFiles.SelectMany(item => item.Matches).Count(item => item.ProjectName == "CF batch project 204") == 1);
        O(331, batch.OnlineFiles.SelectMany(item => item.Matches).SelectMany(item => item.Dependencies).Count(item => item.ProjectId == "ExternalP204") == 1);
        O(332, batch.OnlineFiles.SelectMany(item => item.Matches).SelectMany(item => item.Dependencies).Count(item => item.VersionName == "2.0.204") == 1);
        O(333, batch.Mods.Count == before.Mods.Count && batch.Issues.Count == before.Issues.Count && !batch.OnlineIncomplete);
        O(334, network.ProgressCounts.Count == 205 && network.ProgressCounts[204] == 205);
        O(335, network.Calls.Count(address => address == "https://api.modrinth.com/v2/version_files") == 3 && network.Calls.Count(address => address.StartsWith("https://api.modrinth.com/v2/projects?", StringComparison.Ordinal)) == 5 && network.Calls.Count(address => address == "https://api.curseforge.com/v1/mods") == 3);
        for (int i = 0; i < network.Calls.Count; i++)
            if (network.Calls[i] == "https://api.modrinth.com/v2/version_files")
                O(337, JObject.Parse(network.Bodies[i])["hashes"]!.Count() <= 100);
        // Default transport reuse is guarded; an exclusive handle prohibits accidental rehashing.
        batch.OnlineIndexScope = "all-files";
        Native.Call(Index, "RememberOnline", null, refreshFolder, batch, CancellationToken.None);
        ClearIndex();
        TReport cachedBatch = S(refreshFolder);
        TOptions batchCachedOptions = new() { TargetFile = batch.OnlineFiles[204].FullPath, ComparePlatformDependencies = true };
        using (FileStream lease = File.Open(batchCachedOptions.TargetFile, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            TReport reused = Enrich(cachedBatch, refreshFolder, batchCachedOptions);
            O(343, reused.OnlineRequests == 0 && reused.OnlineFiles.Count == 205 && reused.OnlineIndexScope == "all-files");
        }
        TOnlineProject pinned = batch.OnlineFiles[0].Matches[0];
        TOnlineFile provider = batch.OnlineFiles[1];
        required = new TOnlineDependency
        {
            Platform = "Modrinth", Kind = "required", ProjectId = provider.Matches[0].ProjectId,
            VersionId = "DifferentPinnedVersion", ProjectName = provider.Matches[0].ProjectName
        };
        pinned.Dependencies.Add(required);
        Native.Call(OnlineApi, "MapRelations", null, batch);
        O(347, required.ProjectPaths.Count == 1 && required.MatchedPaths.Count == 0 && required.EnabledPaths.Count == 0 && required.Status.Contains("DifferentPinnedVersion", StringComparison.Ordinal));
        provider.Matches[0].ProjectId = "UnrelatedProjectWithTheSameTitle";
        Native.Call(OnlineApi, "MapRelations", null, batch);
        O(349, required.ProjectPaths.Count == 0);
        foreach (string address in new[] { "https://api.modrinth.com/v2/version_files", "https://api.curseforge.com/v1/fingerprints/432" })
            O(350, (bool)Native.Call(OnlineApi, "IsAllowedApi", null, address)!);
        foreach (string address in new[] { "http://api.curseforge.com/v1/mods", "https://evil.invalid/v1/mods", "https://api.curseforge.com.evil.invalid/v1/mods", "https://api.curseforge.com:444/v1/mods", "https://user@api.curseforge.com/v1/mods", "https://cdn.modrinth.com/v2/version_files" })
            O(351, !(bool)Native.Call(OnlineApi, "IsAllowedApi", null, address)!);

        int[] missing = NativeDependencyOriginalAssertions.Online.Keys.Except(OnlineSeen).Order().ToArray();
        Native.Check(missing.Length == 0 && OnlineSeen.Count == NativeDependencyOriginalAssertions.Online.Count,
            "Online fixture skipped original assertion source lines: " + string.Join(",", missing));
        environment.AssertNoForbiddenIo();
        Console.WriteLine("PASS native dependency-online: " + onlineCount + " executed assertions from all " + OnlineSeen.Count + " LF50 source statements, actual Main DTO/facade/cache/index/cancellation/partial-failure and 205-file batches; injected transport only, no live HTTP.");
    }
}
