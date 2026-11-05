using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using PCLCS;
using Models = PCLCS.PackUpdateModInspection;
using Inspector = PCLCS.ModDependencyInspection<PCLCS.PackUpdateModInspection.InspectionContext, PCLCS.PackUpdateModInspection.Mod, PCLCS.PackUpdateModInspection.Dependency, PCLCS.PackUpdateModInspection.Edge, PCLCS.PackUpdateModInspection.Issue, PCLCS.PackUpdateModInspection.Impact, PCLCS.PackUpdateModInspection.Report, PCLCS.PackUpdateModInspection.Project, PCLCS.PackUpdateModInspection.OnlineDependency, PCLCS.PackUpdateModInspection.OnlineFile>;
using Policy = PCLCS.ModDependencyAutoFillEngine<PCLCS.PackUpdateModInspection.InspectionContext, PCLCS.PackUpdateModInspection.Mod, PCLCS.PackUpdateModInspection.Dependency, PCLCS.PackUpdateModInspection.Edge, PCLCS.PackUpdateModInspection.Issue, PCLCS.PackUpdateModInspection.Impact, PCLCS.PackUpdateModInspection.Report, PCLCS.PackUpdateModInspection.Project, PCLCS.PackUpdateModInspection.OnlineDependency, PCLCS.PackUpdateModInspection.OnlineFile, PackDependencyPolicyTests.Version, PackDependencyPolicyTests.Download, PackDependencyPolicyTests.Item, PackDependencyPolicyTests.Plan, PackDependencyPolicyTests.Activation, PackDependencyPolicyTests.Options, PackDependencyPolicyTests.Cache>;
using Transaction = PCLCS.PackUpdateEngine<PackDependencyPolicyTests.PackFile, PackDependencyPolicyTests.Pack, PackDependencyPolicyTests.PackChange, PackDependencyPolicyTests.PackPlan, object>;

// Production declaration scanning, candidate planning, combined-graph validation and
// backup transaction run unchanged; only platform metadata/transport are fixtures.
internal static class PackDependencyPolicyTests {
    internal static IEnumerable<(string Name, Action Run)> Cases() {
        yield return ("pack launch preserves author combinations without consulting automatic platform dependencies", AuthorOnly);
        yield return ("personal mods reuse an existing author provider without installing duplicate dependencies", ReuseAuthor);
        yield return ("personal mods reuse author-bundled nested providers without platform supplementation", ReuseAuthorNested);
        yield return ("personal-only missing dependencies can add a compatible new provider", PersonalAddition);
        yield return ("author replacement requires confirmation and preserves expected author hashes separately", ConfirmedReplacement);
        yield return ("a personal replacement that breaks another author mod is rejected without changing bytes", ReplacementBreaksPack);
        yield return ("author disabled files are protected while personal disabled providers can be explicitly enabled", DisabledProviders);
        yield return ("confirmed replacements validate the final graph including explicitly enabled personal providers", ReplacementAndActivation);
        yield return ("unknown existing author metadata prevents claiming a replacement is fully validated", UnknownAuthorBlocksReplacement);
        yield return ("untrusted pack records remain read-only and ordinary instances retain dependency planning", UntrustedAndOrdinary);
        yield return ("content changes with unchanged size and mtime invalidate accepted pack repair plans", SameMetadataChange);
        yield return ("metadata failures identify the downloaded file and transaction failures roll back overrides", DiagnosticsAndRollback);
    }

    internal sealed class Version {
        public string Id = "", Name = "", Hash = "";
        public int Size;
        public List<string> Urls = new();
        public ModLoaders Loaders = ModLoaders.Fabric;
    }
    internal sealed class Download { public string Path = ""; public byte[] Bytes = []; }
    internal sealed class Item : ModDependencyAutoFillItemData<Version, Item> { }
    internal sealed class Activation : DependencyActivationItemData<Models.Mod> { }
    internal sealed class Plan : ModDependencyAutoFillPlanData<Models.Report, Item, Activation> { }
    internal sealed class Cache : ModDependencyLookupCache { }
    internal sealed class Options : ModDependencyLookupOptions<Cache> { }
    internal sealed class PackFile : PackUpdateFileData { }
    internal sealed class Pack : PackUpdatePackageData<PackFile> { }
    internal sealed class PackChange : PackUpdateChangeData<PackFile> { }
    internal sealed class PackPlan : PackUpdatePlanData<Pack, PackChange> { }

    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private static T Throws<T>(Action action) where T : Exception {
        try { action(); } catch (T error) { return error; }
        throw new Exception("Expected " + typeof(T).Name);
    }
    private static JObject Depends(string id, string range = "*") => new() { [id] = range };
    private static byte[] Jar(string id, string version = "1.0.0", JObject? depends = null, JObject? breaks = null, byte[]? nested = null) {
        JObject metadata = new() { ["schemaVersion"] = 1, ["id"] = id, ["name"] = id, ["version"] = version };
        if (depends is not null) metadata["depends"] = depends;
        if (breaks is not null) metadata["breaks"] = breaks;
        if (nested is not null) metadata["jars"] = new JArray(new JObject { ["file"] = "META-INF/jars/embedded.jar" });
        using MemoryStream bytes = new();
        using (ZipArchive archive = new(bytes, ZipArchiveMode.Create, true)) {
            using (Stream stream = archive.CreateEntry("fabric.mod.json").Open()) stream.Write(Encoding.UTF8.GetBytes(metadata.ToString()));
            using (Stream asset = archive.CreateEntry("fixture-asset.bin", CompressionLevel.NoCompression).Open()) asset.Write(Encoding.ASCII.GetBytes("fixture-assets-0000"));
            if (nested is not null) using (Stream embedded = archive.CreateEntry("META-INF/jars/embedded.jar").Open()) embedded.Write(nested);
        }
        return bytes.ToArray();
    }
    private static void AuthorOnly() {
        using Fixture f = new();
        f.Author("author.jar", "author", Jar("author", depends: Depends("unlisted")));
        f.Requirement("author.jar", "author", "unlisted");
        Models.Report report = f.Report();
        Require(report.HasErrors, "The fixture must retain the real missing dependency error.");
        Plan plan = f.Build(report);
        Require(plan.Items.Count == 0 && plan.Enables.Count == 0 && f.EnrichCalls == 0 && f.RequestCalls == 0,
            "The author combination triggered automatic platform repair.");
        Require(report.HasErrors, "Author provenance was used to erase a real declaration error.");
    }
    private static void ReuseAuthor() {
        using Fixture f = new();
        f.Author("api.jar", "pack_api", Jar("api", "1.2.0"));
        f.Personal("addon.jar", Jar("addon", depends: Depends("api", ">=1.0.0")));
        f.Requirement("addon.jar", "addon_project", "pack_api");
        Models.Report report = f.Report();
        Require(report.Edges.Any(edge => edge.Owner.Id == "addon" && edge.Requirement.Id == "api" &&
            edge.StateValue == ModDependencyMatchState.Satisfied && edge.Providers.Any(provider => provider.Id == "api" && !provider.IsNested && provider.RootPath == f.Path("api.jar"))),
            "The fixture must prove that the actual personal dependency was parsed and satisfied by the author file.");
        Plan plan = f.Build(report);
        Require(!plan.Report.HasErrors && plan.Items.Count == 0 && plan.Enables.Count == 0 && f.RequestCalls == 0,
            "The already satisfied author provider was replaced or downloaded again.");
        f.HasBytes("api.jar", f.AuthorBytes["api.jar"]);
    }
    private static void ReuseAuthorNested() {
        using Fixture f = new();
        byte[] embedded = Jar("embedded_api", "1.2.0");
        f.Author("bundle.jar", "author_bundle", Jar("author_bundle", nested: embedded));
        f.Personal("addon.jar", Jar("addon", depends: Depends("embedded_api", ">=1.0.0")));
        f.Requirement("addon.jar", "addon_project", "embedded_project");
        f.Candidate("embedded_project", "embedded_v1", "external-copy.jar", embedded);
        Models.Report report = f.Report();
        Require(!report.HasErrors && report.Edges.Any(edge => edge.Owner.Id == "addon" && edge.Requirement.Id == "embedded_api" &&
            edge.StateValue == ModDependencyMatchState.Satisfied && edge.Providers.Any(provider => provider.Id == "embedded_api" && provider.IsNested && provider.RootPath == f.Path("bundle.jar"))),
            "The actual loader graph must select the author's nested provider before exercising supplementation policy.");
        byte[] manifest = File.ReadAllBytes(f.ManifestPath);
        Plan plan = f.Build(report);
        Require(plan.Items.Count == 0 && plan.Enables.Count == 0 && f.EnrichCalls == 0 && f.RequestCalls == 0,
            "An already satisfied embedded author provider triggered platform lookup or a duplicate external installation.");
        Require(!File.Exists(f.Path("external-copy.jar")) && File.ReadAllBytes(f.ManifestPath).SequenceEqual(manifest),
            "Nested dependency reuse changed the author's combination or provenance.");
        f.HasBytes("bundle.jar", f.AuthorBytes["bundle.jar"]);
    }
    private static void PersonalAddition() {
        using Fixture f = new();
        f.Author("base.jar", "base", Jar("base"));
        f.Personal("addon.jar", Jar("addon", depends: Depends("new_api", ">=1.0.0")));
        f.Requirement("addon.jar", "addon_project", "new_api_project");
        byte[] added = Jar("new_api", depends: Depends("base"));
        f.Candidate("new_api_project", "api_v1", "new-api.jar", added);
        byte[] origin = File.ReadAllBytes(f.ManifestPath);
        Plan plan = f.Build();
        Require(plan.Items.Count == 1 && plan.Items[0].ProjectId == "new_api_project" && plan.Items[0].Replacement is null,
            "A compatible missing personal dependency was not planned as an addition.");
        Require(f.Engine.Install(plan, CancellationToken.None) == 1, "The real dependency install did not add its provider.");
        f.HasBytes("new-api.jar", added); f.HasBytes("base.jar", f.AuthorBytes["base.jar"]);
        Require(File.ReadAllBytes(f.ManifestPath).SequenceEqual(origin), "A personal addition rewrote the author declaration.");
        Require(!f.Report().HasErrors, "The installed personal provider did not satisfy the complete real graph.");
    }
    private static Plan Replacement(Fixture f, bool breakAuthor = false) {
        f.Author("api.jar", "pack_api", Jar("api", "1.0.0"));
        if (breakAuthor) f.Author("consumer.jar", "consumer", Jar("consumer", depends: Depends("api", "<2.0.0")));
        f.Personal("addon.jar", Jar("addon", depends: Depends("api", ">=2.0.0")));
        f.Requirement("addon.jar", "addon_project", "pack_api");
        f.Candidate("pack_api", "api_v2", "api-v2.jar", Jar("api", "2.0.0"));
        Plan plan = f.Build();
        Require(plan.Items.Count == 1 && plan.Items[0].Replacement?.Source == f.Path("api.jar"),
            "A unique author provider's personal version mismatch was not planned for explicit validated replacement.");
        return plan;
    }
    private static void ConfirmedReplacement() {
        using Fixture f = new(); Plan plan = Replacement(f);
        byte[] manifest = File.ReadAllBytes(f.ManifestPath);
        Throws<IOException>(() => f.Engine.Install(plan, CancellationToken.None));
        f.HasBytes("api.jar", f.AuthorBytes["api.jar"]);
        Require(!File.Exists(f.Path("api-v2.jar")) && File.ReadAllBytes(f.ManifestPath).SequenceEqual(manifest),
            "An unconfirmed replacement changed author bytes or the baseline.");
        plan.AuthorReplacementConfirmed = true;
        Require(f.Engine.Install(plan, CancellationToken.None) > 0 && f.TransactionCalls == 1, "Confirmed replacement bypassed the real backup transaction.");
        Require(!File.Exists(f.Path("api.jar")), "Replacement left the old active author copy beside the new provider.");
        f.HasBytes("api-v2.jar", f.Candidates["api_v2"].Bytes);
        JObject saved = f.ReadManifest();
        Require(saved["files"]!["mods/api.jar"]!["hash"]?.ToString() == "sha1:" + Hex(f.AuthorBytes["api.jar"]),
            "The personal override overwrote the author's expected hash.");
        Require(saved["files"]!["mods/api.jar"]!["local"]?["relative"]?.ToString() == "mods/api-v2.jar",
            "The override did not preserve its actual local path for the next pack update.");
        Require(!f.Report().HasErrors, "Replacement was published despite unresolved complete-graph errors.");
        PackPlan update = f.NextAuthorVersion(saved, Jar("api", "3.0.0"));
        PackChange next = update.Changes.Single(change => change.Incoming?.Relative == "mods/api-v3.jar");
        Require(next.Conflict && !next.ApplyAuthor && next.LocalRelative == "mods/api-v2.jar" && next.Ownership == PackUpdateOwnership.PersonalModified,
            "The real next pack-update plan lost the personal override association or silently replaced it.");
    }
    private static void ReplacementBreaksPack() {
        using Fixture f = new(); Plan plan = Replacement(f, true); plan.AuthorReplacementConfirmed = true;
        byte[] manifest = File.ReadAllBytes(f.ManifestPath);
        Throws<IOException>(() => f.Engine.Install(plan, CancellationToken.None));
        Require(f.TransactionCalls == 0 && !File.Exists(f.Path("api-v2.jar")), "A candidate breaking an author consumer reached the transaction.");
        f.HasBytes("api.jar", f.AuthorBytes["api.jar"]); f.HasBytes("consumer.jar", f.AuthorBytes["consumer.jar"]);
        Require(File.ReadAllBytes(f.ManifestPath).SequenceEqual(manifest), "A rejected replacement changed author provenance.");
    }
    private static void DisabledProviders() {
        using (Fixture f = new()) {
            f.Author("api.jar", "pack_api", Jar("api")); File.Move(f.Path("api.jar"), f.Path("api.jar.disabled"));
            f.Personal("addon.jar", Jar("addon", depends: Depends("api")));
            Plan plan = f.Engine.Prepare(f.Mods, f.Report(), CancellationToken.None);
            Require(plan.Enables.Count == 0 && File.Exists(f.Path("api.jar.disabled")), "Author-disabled state was automatically changed.");
        }
        using (Fixture f = new()) {
            f.Author("base.jar", "base", Jar("base"));
            f.Personal("api.jar.disabled", Jar("api")); f.Personal("addon.jar", Jar("addon", depends: Depends("api")));
            Plan plan = f.Engine.Prepare(f.Mods, f.Report(), CancellationToken.None);
            Require(plan.Enables.Count == 1 && plan.Enables[0].Source == f.Path("api.jar.disabled"), "A compatible disabled personal provider was not offered for explicit activation.");
            Require(f.Engine.Install(plan, CancellationToken.None) > 0 && File.Exists(f.Path("api.jar")) && !File.Exists(f.Path("api.jar.disabled")), "The confirmed personal activation did not use the real install flow.");
            f.HasBytes("base.jar", f.AuthorBytes["base.jar"]);
        }
    }
    private static void ReplacementAndActivation() {
        using Fixture f = new(); Replacement(f);
        byte[] extra = Jar("extra"); f.Personal("extra.jar.disabled", extra);
        f.Personal("addon.jar", Jar("addon", depends: new JObject { ["api"] = ">=2.0.0", ["extra"] = ">=1.0.0" }));
        Plan plan = f.Build(); plan.AuthorReplacementConfirmed = true;
        Require(plan.Items.Count == 1 && plan.Items[0].Replacement is not null && plan.Enables.Count == 1,
            "The mixed fixture must contain a real author replacement and personal activation.");
        Require(f.Engine.Install(plan, CancellationToken.None) == 2 && f.TransactionCalls == 1,
            "The complete combined graph omitted the explicitly enabled personal provider.");
        f.HasBytes("api-v2.jar", f.Candidates["api_v2"].Bytes); f.HasBytes("extra.jar", extra);
        Require(!File.Exists(f.Path("api.jar")) && !File.Exists(f.Path("extra.jar.disabled")) && !f.Report().HasErrors,
            "The mixed transaction left stale sources or unresolved declarations.");
    }
    private static void UnknownAuthorBlocksReplacement() {
        using Fixture f = new(); Replacement(f);
        f.Author("unknown-author.jar", "unknown_author", UnknownArchive());
        Plan plan = f.Build(); plan.AuthorReplacementConfirmed = true;
        Require(plan.Report.MetadataIncomplete && plan.Items.Any(item => item.Replacement is not null),
            "The fixture must retain incomplete author metadata alongside the proposed replacement.");
        byte[] manifest = File.ReadAllBytes(f.ManifestPath);
        Throws<IOException>(() => f.Engine.Install(plan, CancellationToken.None));
        Require(f.TransactionCalls == 0 && !File.Exists(f.Path("api-v2.jar")) && File.ReadAllBytes(f.ManifestPath).SequenceEqual(manifest),
            "An unknown author declaration was treated as evidence that the entire replacement graph passed.");
        f.HasBytes("api.jar", f.AuthorBytes["api.jar"]);
    }
    private static byte[] UnknownArchive() {
        using MemoryStream bytes = new();
        using (ZipArchive archive = new(bytes, ZipArchiveMode.Create, true)) {
            using StreamWriter writer = new(archive.CreateEntry("readme.txt").Open()); writer.Write("A valid archive without loader declarations.");
        }
        return bytes.ToArray();
    }
    private static void UntrustedAndOrdinary() {
        using (Fixture f = new()) {
            f.Author("base.jar", "base", Jar("base"));
            f.Personal("addon.jar", Jar("addon", depends: Depends("new_api"))); f.Requirement("addon.jar", "addon", "new_api_project");
            JObject old = f.ReadManifest(); old["schema"] = 1; old.Remove("complete"); f.WriteManifest(old);
            Plan plan = f.Build();
            Require(plan.Items.Count == 0 && plan.Enables.Count == 0 && f.EnrichCalls == 0, "An incomplete legacy author record authorized repair.");
        }
        using (Fixture f = new()) {
            f.Personal("addon.jar", Jar("addon", depends: Depends("new_api"))); f.Requirement("addon.jar", "addon", "new_api_project");
            f.Candidate("new_api_project", "api_v1", "new-api.jar", Jar("new_api"));
            Models.Report report = f.Report(pack: false); Plan plan = f.Build(report);
            Require(plan.Items.Count == 1 && f.EnrichCalls == 1 && f.RequestCalls > 0, "Ordinary instances lost their existing auto-fill behavior.");
        }
    }
    private static void SameMetadataChange() {
        using Fixture f = new(); Plan plan = Replacement(f); plan.AuthorReplacementConfirmed = true;
        Models.Report original = plan.Report;
        f.Engine.Accept(f.Mods, original);
        DateTime stamp = File.GetLastWriteTimeUtc(f.Path("api.jar"));
        long length = new FileInfo(f.Path("api.jar")).Length;
        using (ZipArchive archive = ZipFile.Open(f.Path("api.jar"), ZipArchiveMode.Update)) {
            using Stream asset = archive.GetEntry("fixture-asset.bin")!.Open(); asset.WriteByte(123);
        }
        File.SetLastWriteTimeUtc(f.Path("api.jar"), stamp);
        byte[] bytes = File.ReadAllBytes(f.Path("api.jar"));
        Require(bytes.LongLength == length, "The stale-plan fixture must preserve file length.");
        Models.Report changed = f.Report();
        Require(!changed.MetadataIncomplete && original.Mods.Select(item => item.Id + ":" + item.Version).SequenceEqual(changed.Mods.Select(item => item.Id + ":" + item.Version)), "The stale-plan fixture must preserve parsed declarations.");
        Require(!f.Engine.WasAccepted(f.Mods, changed), "An old user acceptance survived a same-metadata file content change.");
        Throws<IOException>(() => f.Engine.Install(plan, CancellationToken.None));
        Require(File.ReadAllBytes(f.Path("api.jar")).SequenceEqual(bytes) && !File.Exists(f.Path("api-v2.jar")) && f.TransactionCalls == 0,
            "A same-size/same-mtime edit escaped the content/provenance stale-plan guard.");
    }
    private static void DiagnosticsAndRollback() {
        using (Fixture f = new()) {
            f.Author("base.jar", "base", Jar("base"));
            f.Personal("addon.jar", Jar("addon", depends: Depends("missing"))); f.Requirement("addon.jar", "addon", "unknown_project");
            f.Candidate("unknown_project", "unknown", "unrecognized.jar", UnknownArchive());
            IOException error = Throws<IOException>(() => f.Engine.Install(f.Build(), CancellationToken.None));
            Require(error.Message.Contains("unrecognized.jar", StringComparison.OrdinalIgnoreCase) && error.Message.Contains("声明", StringComparison.Ordinal),
                "The metadata failure discarded its staged file name or scanner reason.");
            Require(!File.Exists(f.Path("unrecognized.jar")) && !Directory.GetDirectories(f.Mods, ".pcl-import-*").Any(), "Metadata failure published or leaked its stage.");
        }
        using (Fixture f = new()) {
            Plan plan = Replacement(f); plan.AuthorReplacementConfirmed = true; f.FailTransaction = true;
            byte[] original = File.ReadAllBytes(f.ManifestPath);
            Throws<IOException>(() => f.Engine.Install(plan, CancellationToken.None));
            f.HasBytes("api.jar", f.AuthorBytes["api.jar"]);
            Require(!File.Exists(f.Path("api-v2.jar")) && File.ReadAllBytes(f.ManifestPath).SequenceEqual(original), "A failed real transaction did not roll back author override bytes and provenance.");
        }
    }
    private static string Hex(byte[] bytes) => Convert.ToHexString(SHA1.HashData(bytes)).ToLowerInvariant();

    private sealed class CandidateData { internal string Project = "", Name = ""; internal byte[] Bytes = []; }
    private sealed class Fixture : IDisposable {
        internal readonly string Root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "PclLfPackDependency-" + Guid.NewGuid().ToString("N"));
        internal string Game => System.IO.Path.Combine(Root, "isolated-game");
        internal string VersionFolder => System.IO.Path.Combine(Root, "minecraft", "versions", "pack");
        internal string Mods => System.IO.Path.Combine(Game, "mods");
        internal string ManifestPath => System.IO.Path.Combine(VersionFolder, "PCL", "ModpackOrigin.json");
        internal readonly Dictionary<string, byte[]> AuthorBytes = new(StringComparer.Ordinal);
        internal readonly Dictionary<string, CandidateData> Candidates = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Models.Project> online = new(StringComparer.OrdinalIgnoreCase);
        private readonly JObject manifest = new() { ["schema"] = 2, ["complete"] = true, ["name"] = "fixture pack", ["version"] = "1", ["minecraft"] = "1.21.1", ["loaders"] = new JObject { ["Fabric"] = "0.16.14" }, ["files"] = new JObject() };
        internal int EnrichCalls, RequestCalls, TransactionCalls;
        internal bool FailTransaction;
        internal readonly Policy Engine;
        private readonly Transaction transaction;
        internal Fixture() {
            Directory.CreateDirectory(Mods); Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ManifestPath)!);
            transaction = new(new() { CreateFile = () => new(), CreatePackage = () => new(), CreateChange = () => new(), CreatePlan = () => new(), EnsureWithin = Safe, AtomicWrite = (path, bytes) => File.WriteAllBytes(path, bytes) });
            Engine = new(new() {
                ParseVersion = Parse, ResourceType = _ => ResourceTypes.Mod, DownloadAvailable = _ => true,
                Size = value => value.Size, GameVersions = _ => new[] { "1.21.1" }, ModLoaders = value => value.Loaders, SetModLoaders = (value, loaders) => value.Loaders = loaders,
                Id = value => value.Id, FileName = value => value.Name, Hash = value => value.Hash, Display = value => value.Id,
                DownloadUrls = value => value.Urls, SetDownloadUrls = (value, urls) => value.Urls = urls, ReleaseType = _ => 1, ReleaseDate = _ => new DateTime(2025, 1, 1),
                ToNetFile = (value, path, _, _) => new() { Path = path, Bytes = Candidates[value.Id].Bytes },
                Enrich = (report, _, token, _) => { token.ThrowIfCancellationRequested(); EnrichCalls++; report.OnlineFiles = online.Where(pair => File.Exists(pair.Key)).Select(pair => new Models.OnlineFile {
                    FullPath = pair.Key, Sha1 = Hex(File.ReadAllBytes(pair.Key)), Enabled = !pair.Key.EndsWith(".disabled", StringComparison.Ordinal), Matches = new() { pair.Value }
                }).ToList(); },
                DefaultRequest = (url, _, _, token) => Reply(url, token), SafeName = name => name,
                EnsureWithin = Safe, DeleteStage = (root, path) => { Safe(root, path); if (Directory.Exists(path)) Directory.Delete(path, true); },
                FileHash = (path, md5) => Digest(path, md5 ? "md5" : "sha1", CancellationToken.None)!.Split(':')[1],
                Download = (_, files, token) => { foreach (Download file in files) { token.ThrowIfCancellationRequested(); File.WriteAllBytes(file.Path, file.Bytes); } },
                Info = _ => { }, Warn = (_, _) => { }, RefreshOptions = () => { }, CandidateMismatch = (index, text) => new(index, text),
                ApplyTransaction = (game, version, operations, token) => { TransactionCalls++; return transaction.ApplyTransaction(game, version, operations, token, FailTransaction ? index => { if (index == 2) throw new IOException("Injected failure after the author source removal."); } : null); }
            });
        }
        internal string Path(string name) => System.IO.Path.Combine(Mods, name);
        internal void Author(string name, string project, byte[] bytes) {
            Personal(name, bytes); AuthorBytes[name] = bytes;
            manifest["files"]!["mods/" + name] = new JObject { ["hash"] = "sha1:" + Hex(bytes), ["identity"] = "modrinth:" + project, ["fileIdentity"] = "modrinth:original_" + project, ["state"] = "installed", ["optional"] = false };
            WriteManifest(manifest);
            online[Path(name)] = new() { Platform = "Modrinth", ProjectId = project, VersionId = "original_" + project, ProjectName = project, IsExact = true };
        }
        internal void Personal(string name, byte[] bytes) => File.WriteAllBytes(Path(name), bytes);
        internal void Requirement(string name, string project, string dependency) {
            online[Path(name)] = new() { Platform = "Modrinth", ProjectId = project, VersionId = "owner_v1", ProjectName = project, IsExact = true,
                Dependencies = new() { new() { Platform = "Modrinth", ProjectId = dependency, VersionId = "", Kind = "required" } } };
        }
        internal void Candidate(string project, string id, string name, byte[] bytes) => Candidates[id] = new() { Project = project, Name = name, Bytes = bytes };
        internal JObject ReadManifest() => JObject.Parse(File.ReadAllText(ManifestPath));
        internal void WriteManifest(JObject value) => File.WriteAllText(ManifestPath, value.ToString());
        internal PackPlan NextAuthorVersion(JObject origin, byte[] payload) {
            string source = System.IO.Path.Combine(Root, "next-author.jar"); File.WriteAllBytes(source, payload);
            Pack package = new() { Name = "fixture pack", Version = "2", Minecraft = "1.21.1", Loaders = new() { [ModLoaders.Fabric] = "0.16.14" } };
            package.Files.Add("mods/api-v3.jar", new() { Relative = "mods/api-v3.jar", Hash = "sha1:" + Hex(payload), Identity = "modrinth:pack_api", FileIdentity = "modrinth:api_v3", Payload = source });
            return transaction.Plan(Game, origin, package);
        }
        internal Models.Report Report(bool pack = true) {
            Models.InspectionContext context = new() { MinecraftVersion = "1.21.1", LoaderKnown = true };
            context.LoaderVersions["fabricloader"] = "0.16.14";
            Models.Report report = Inspector.ScanRaw(Mods, context, CancellationToken.None, true);
            if (pack) report.PackOrigin = PackDependencyOrigin.Read(VersionFolder, Game, false, _ => File.Exists(ManifestPath) ? ReadManifest() : null, Digest);
            return report;
        }
        internal Plan Build(Models.Report? report = null) => Engine.BuildPlan(Mods, report ?? Report(), CancellationToken.None,
            new Options { Request = (url, _, _, token) => Reply(url, token) });
        internal void HasBytes(string name, byte[] expected) => Require(File.ReadAllBytes(Path(name)).SequenceEqual(expected), "Wrong file bytes: " + name);
        private Version Parse(JObject raw) {
            JToken file = ((JArray)raw["files"]!)[0]!;
            return new() { Id = raw["id"]!.ToString(), Name = file["filename"]!.ToString(), Hash = file["hashes"]!["sha1"]!.ToString(), Size = (int)file["size"]!, Urls = new() { file["url"]!.ToString() } };
        }
        private JToken Reply(string url, CancellationToken token) {
            token.ThrowIfCancellationRequested(); RequestCalls++;
            string prefix = "https://api.modrinth.com/v2/project/";
            if (url.StartsWith(prefix, StringComparison.Ordinal)) {
                string project = url.Substring(prefix.Length).Split('/')[0];
                if (url.Contains("/version?", StringComparison.Ordinal)) return new JArray(Candidates.Where(pair => pair.Value.Project == project).Select(pair => Raw(pair.Key, pair.Value)));
                return new JObject { ["id"] = project, ["project_type"] = "mod", ["client_side"] = "required", ["title"] = project };
            }
            string id = url.Substring(url.LastIndexOf('/') + 1);
            if (Candidates.TryGetValue(id, out CandidateData? candidate)) return Raw(id, candidate);
            throw new IOException("Unexpected fixture network request " + url);
        }
        private static JObject Raw(string id, CandidateData candidate) => new() {
            ["id"] = id, ["project_id"] = candidate.Project, ["loaders"] = new JArray("fabric"), ["game_versions"] = new JArray("1.21.1"), ["dependencies"] = new JArray(),
            ["files"] = new JArray(new JObject { ["primary"] = true, ["filename"] = candidate.Name, ["size"] = candidate.Bytes.Length,
                ["url"] = "https://cdn.modrinth.com/data/" + candidate.Project + "/versions/" + id + "/" + candidate.Name, ["hashes"] = new JObject { ["sha1"] = Hex(candidate.Bytes) } })
        };
        private static void Safe(string root, string file) {
            string parent = System.IO.Path.GetFullPath(root).TrimEnd(System.IO.Path.DirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar;
            string full = System.IO.Path.GetFullPath(file);
            if (!full.StartsWith(parent, StringComparison.OrdinalIgnoreCase)) throw new IOException("Fixture path escaped its owned root.");
        }
        private static string? Digest(string path, string algorithm, CancellationToken token) {
            token.ThrowIfCancellationRequested(); if (!File.Exists(path)) return null;
            using var input = File.OpenRead(path);
            using HashAlgorithm hash = algorithm.ToLowerInvariant() switch { "sha1" => SHA1.Create(), "sha256" => SHA256.Create(), "sha512" => SHA512.Create(), "md5" => MD5.Create(), _ => throw new IOException("Unknown digest") };
            return algorithm.ToLowerInvariant() + ":" + Convert.ToHexString(hash.ComputeHash(input)).ToLowerInvariant();
        }
        public void Dispose() => Directory.Delete(Root, true);
    }
}
