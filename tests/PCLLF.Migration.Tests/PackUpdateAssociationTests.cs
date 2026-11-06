using System.IO.Compression;
using System.Text;
using Newtonsoft.Json.Linq;
using PCLCS;

internal static class PackUpdateAssociationTests {
    internal static IEnumerable<(string Name, Action Run)> Cases() {
        yield return ("pack association keeps independent empty configuration paths independent", IndependentEmptyPaths);
        yield return ("pack association keeps independent identical nonempty templates independent", IndependentTemplatePaths);
        yield return ("pack association never consumes a same-hash neighbor of a missing author configuration", MissingAuthorNeighbor);
        yield return ("pack association installs an unrelated same-hash addition despite a deleted old path", DeletedOldUnrelatedNew);
        yield return ("pack association does not connect equal bytes across content categories", CrossCategoryHash);
        yield return ("pack association upgrades uniquely identified platform archives across author filenames", PlatformArchiveRename);
        yield return ("pack association does not connect a platform project across content categories", PlatformArchiveCrossCategory);
        yield return ("pack association does not choose among multiple platform project attachments", PlatformArchiveAmbiguity);
        yield return ("pack association protects a personal file occupying a platform archive destination", PlatformArchiveOccupiedTarget);
        yield return ("pack association updates and removes ordinary author configurations while preserving edits", OrdinaryConfigurations);
        yield return ("pack association does not interpret configuration backup suffixes as disabled mods", ConfigurationBackupSuffixes);
        yield return ("pack association protects per-world litematica data from author updates and removals", LitematicaExistingData);
        yield return ("pack association does not inject absent litematica runtime state", LitematicaAbsentData);
        yield return ("pack association still updates the litematica global configuration", LitematicaGlobalConfiguration);
        yield return ("pack association preserves personal same-byte target provenance across updates", PersonalTargetAcrossUpdates);
        yield return ("pack association repairs proven legacy nonmod hash ambiguity without persisting it", ProvenLegacyAmbiguity);
        yield return ("pack association retains legacy ambiguous content when migration evidence is incomplete", UnprovenLegacyAmbiguity);
        yield return ("pack association never grants a historical configuration observation cross-path ownership", UnknownRecordedPath);
        yield return ("pack association rejects a forged cross-path configuration source before committing", ForgedCrossPathPlan);
        yield return ("pack association rejects forged runtime-data writes and deletions with either path separator", ForgedRuntimeDataPlan);
        yield return ("pack association rejects post-preview target occupation without changing neighbors", OccupiedAfterPreview);
        yield return ("pack association restores configurations and their manifest after commit cancellation", ConfigurationCancellation);
    }

    private static void IndependentEmptyPaths() => IndependentPaths("");
    private static void IndependentTemplatePaths() => IndependentPaths("{\"enabled\":true,\"entries\":[]}");
    private static void IndependentPaths(string shared) {
        using Fixture f = new();
        Spec a = new("config/animatica.properties", shared), b = new("config/blurperfected.json", shared), c = new("config/waterdripsound.json", shared);
        Spec next = a with { Text = "author-new-value" };
        f.Install(a, b, c);
        Plan plan = f.Prepare([a, b, c], [next, b, c]);
        Require(!plan.Changes.Any(change => change.Ownership == PackUpdateOwnership.Ambiguous || change.Candidates.Count != 0),
            "Independent configuration paths became hash-rename candidates.");
        Change update = Incoming(plan, a.Relative);
        Require(update.LocalRelative == a.Relative && update.ApplyAuthor && !update.Conflict, "An ordinary author update lost its exact source path.");
        string backup = f.Commit(plan);
        f.Has(next); f.Has(b); f.Has(c);
        Require(Directory.EnumerateFiles(backup, "*.bak").Any(path => File.ReadAllBytes(path).SequenceEqual(Bytes(a))), "The actual changed configuration was not backed up.");
        JObject saved = f.ReadManifest();
        Plan repeated = f.PrepareWithBaseline(saved, [next with { Text = "author-third-value" }, b, c]);
        Require(!repeated.Changes.Any(change => change.Ownership == PackUpdateOwnership.Ambiguous), "An independent same-hash path acquired persistent ambiguity.");
        f.Commit(repeated); f.Has(b); f.Has(c);
    }

    private static void MissingAuthorNeighbor() {
        foreach (bool removed in new[] { false, true }) foreach (string shared in new[] { "", "{\"placements\":[]}" }) {
            using Fixture f = new();
            Spec author = new("config/author.properties", shared), neighbor = new("config/player.json", shared);
            f.Install(neighbor);
            Plan plan = f.Prepare([author], removed ? [] : [author with { Text = "new-author" }]);
            Require(!plan.Changes.Any(change => change.BaselineRelative == author.Relative && change.LocalRelative == neighbor.Relative),
                "A missing author path claimed an unrelated equal-byte configuration.");
            f.Commit(plan); f.Has(neighbor); f.Absent(author.Relative);
        }
    }

    private static void DeletedOldUnrelatedNew() {
        foreach (bool oldExists in new[] { false, true }) {
            using Fixture f = new();
            Spec old = new("config/deleted.properties", ""), added = new("config/unrelated-new.json", "");
            if (oldExists) f.Install(old);
            Plan plan = f.Prepare([old], [added]);
            Change addition = Incoming(plan, added.Relative);
            Require(addition.BaselineRelative is null && addition.LocalRelative is null && addition.ApplyAuthor,
                "An unrelated new declaration inherited a removed/deleted old file's association or deletion state.");
            f.Commit(plan); f.Has(added); f.Absent(old.Relative);
        }
    }

    private static void CrossCategoryHash() {
        foreach (string destination in new[] { "defaultconfigs/template.json", "resourcepacks/template.txt", "shaderpacks/template.txt", "datapacks/template.txt" }) {
            using Fixture f = new();
            Spec old = new("config/old-template.json", "identical-template"), next = new(destination, "identical-template");
            Plan plan = f.Prepare([old], [next]);
            Require(Incoming(plan, destination).BaselineRelative is null && Incoming(plan, destination).ApplyAuthor, "Equal bytes established a cross-category author association.");
            f.Commit(plan); f.Has(next);
        }
    }

    private static void PlatformArchiveRename() {
        foreach (string category in new[] { "resourcepacks", "shaderpacks" }) {
            using Fixture f = new();
            Spec old = new(category + "/author-v1.zip", "old-archive-bytes", "sameProject"), next = old with { Relative = category + "/author-v2.zip", Text = "new-archive-bytes" };
            f.Install(old);
            Plan plan = f.Prepare([old], [next]);
            Change update = Incoming(plan, next.Relative);
            Require(update.BaselineRelative == old.Relative && update.LocalRelative == old.Relative && update.Association == PackUpdateAssociation.PlatformProject &&
                update.ApplyAuthor && !update.Conflict, "A unique same-category project upgrade did not retain its canonical old source.");
            string backup = f.Commit(plan);
            f.Has(next); f.Absent(old.Relative);
            Require(Directory.EnumerateFiles(backup, "*.bak").Any(path => File.ReadAllBytes(path).SequenceEqual(Bytes(old))), "A renamed platform archive did not back up its actual old bytes.");
            JObject saved = f.ReadManifest();
            Require(saved["files"]?[old.Relative] is null && saved["files"]?[next.Relative]?["identity"]?.ToString() == "modrinth:sameProject",
                "A platform upgrade persisted the wrong author path or project identity.");
        }
    }

    private static void PlatformArchiveCrossCategory() {
        foreach (bool originalExists in new[] { false, true }) foreach (string destination in new[] { "shaderpacks/new.zip", "datapacks/new.zip" }) {
            using Fixture f = new();
            Spec old = new("resourcepacks/old.zip", "same-content", "sharedProject"), next = new(destination, "same-content", "sharedProject");
            if (originalExists) f.Install(old);
            Plan plan = f.Prepare([old], [next]);
            Change addition = Incoming(plan, next.Relative);
            Require(addition.BaselineRelative is null && addition.LocalRelative is null && addition.ApplyAuthor,
                "A shared project identity transferred an old declaration or deletion state across content categories.");
            f.Commit(plan); f.Has(next); f.Absent(old.Relative);
        }
    }

    private static void PlatformArchiveAmbiguity() {
        foreach (string category in new[] { "resourcepacks", "shaderpacks" }) foreach (bool multipleOld in new[] { false, true }) {
            using Fixture f = new();
            Spec old = new(category + "/old-main.zip", "old-main", "attachments"), oldExtra = old with { Relative = category + "/old-extra.zip", Text = "old-extra" };
            Spec next = old with { Relative = category + "/new-main.zip", Text = "new-main" }, nextExtra = next with { Relative = category + "/new-extra.zip", Text = "new-extra" };
            Spec[] originals = multipleOld ? [old, oldExtra] : [old];
            f.Install(originals);
            Plan plan = f.Prepare(originals, multipleOld ? [next] : [next, nextExtra]);
            Require(plan.Changes.Any(change => change.Ownership == PackUpdateOwnership.Ambiguous) && !plan.Changes.Any(change => change.ApplyAuthor),
                "A many-to-one or one-to-many project attachment mapping selected an arbitrary archive.");
            f.Commit(plan); foreach (Spec original in originals) f.Has(original);
            f.Absent(next.Relative); f.Absent(nextExtra.Relative);
        }
    }

    private static void PlatformArchiveOccupiedTarget() {
        foreach (string category in new[] { "resourcepacks", "shaderpacks" }) {
            using Fixture f = new();
            Spec old = new(category + "/old.zip", "old-author", "occupiedProject"), next = old with { Relative = category + "/new.zip", Text = "new-author" }, player = next with { Text = "player-owned-target", Project = null };
            f.Install(old, player);
            Plan plan = f.Prepare([old], [next]);
            Change update = Incoming(plan, next.Relative);
            Require(!update.AllowAuthor && !update.ApplyAuthor, "Platform identity bypassed an occupied personal destination.");
            f.Commit(plan); f.Has(old); f.Has(player);
        }
    }

    private static void OrdinaryConfigurations() {
        foreach (bool modified in new[] { false, true }) foreach (bool removed in new[] { false, true }) {
            using Fixture f = new();
            Spec old = new("config/ordinary.cfg", "author-before"), local = old with { Text = modified ? "player-edit" : old.Text }, next = old with { Text = "author-after" };
            f.Install(local);
            Plan plan = f.Prepare([old], removed ? [] : [next]);
            f.Commit(plan);
            if (modified) f.Has(local); else if (removed) f.Absent(old.Relative); else f.Has(next);
        }
    }

    private static void ConfigurationBackupSuffixes() {
        foreach (string suffix in new[] { ".old", ".disabled" }) foreach (bool removed in new[] { false, true }) {
            using Fixture f = new();
            Spec author = new("config/settings.json", "author-before"), backup = author with { Relative = author.Relative + suffix };
            f.Install(backup);
            Plan plan = f.Prepare([author], removed ? [] : [author with { Text = "author-after" }]);
            Require(!plan.Changes.Any(change => change.BaselineRelative == author.Relative &&
                (change.LocalRelative == backup.Relative || change.Ownership == PackUpdateOwnership.Disabled)), "A configuration backup became a disabled author mod.");
            f.Commit(plan); f.Has(backup); f.Absent(author.Relative);
        }
    }

    private static readonly string[] RuntimePaths = [
        "config/litematica/litematica_test-server.json",
        "config/litematica/litematica_New World.json",
        "config/litematica/litematica_test-server_dim_minecraft_overworld.json",
        "config/litematica/litematica_test-server_dim_minecraft_the_end.json",
        "config/litematica/litematica_New World_dim_minecraft_overworld.json"
    ];

    private static void LitematicaExistingData() {
        foreach (string path in RuntimePaths) foreach (string action in new[] { "update", "remove", "new-declaration" }) {
            using Fixture f = new();
            Spec runtime = new(path, "{\"placements\":[]}"), updated = runtime with { Text = "{\"placements\":[\"author-template\"]}" };
            f.Install(runtime);
            Plan plan = f.Prepare(action == "new-declaration" ? [] : [runtime], action == "remove" ? [] : [updated]);
            Require(!plan.Changes.Any(change => change.LocalRelative == runtime.Relative && change.ApplyAuthor), "An author action automatically modifies player runtime state.");
            f.Commit(plan); f.Has(runtime);
        }
    }

    private static void LitematicaAbsentData() {
        foreach (string path in RuntimePaths) foreach (bool previouslyDeclared in new[] { false, true }) {
            using Fixture f = new();
            Spec runtime = new(path, "{\"placements\":[]}");
            Plan plan = f.Prepare(previouslyDeclared ? [runtime] : [], [runtime with { Text = "author-state" }]);
            Require(!Incoming(plan, path).ApplyAuthor, "Missing player runtime state was scheduled for implicit installation.");
            f.Commit(plan); f.Absent(path);
        }
    }

    private static void LitematicaGlobalConfiguration() {
        using Fixture f = new();
        Spec old = new("config/litematica.json", "{\"enabled\":false}"), next = old with { Text = "{\"enabled\":true}" };
        f.Install(old); Plan plan = f.Prepare([old], [next]);
        Require(Incoming(plan, next.Relative).ApplyAuthor && !Incoming(plan, next.Relative).Conflict, "Runtime-state protection swallowed the ordinary global configuration.");
        f.Commit(plan); f.Has(next);
    }

    private static void PersonalTargetAcrossUpdates() {
        foreach (bool removed in new[] { false, true }) {
            using Fixture f = new();
            Spec personal = new("config/local.json", "same-bytes-as-author");
            f.Install(personal);
            Plan first = f.Prepare([], [personal]); f.Commit(first); f.Has(personal);
            JObject saved = f.ReadManifest();
            Require(saved["files"]![personal.Relative]!["local"]!["ownership"]!.ToString() == "personal-added", "Matching bytes promoted a personal target into author ownership.");
            Plan second = f.PrepareWithBaseline(saved, removed ? [] : [personal with { Text = "new-author-content" }]);
            Require(!second.Changes.Any(change => change.LocalRelative == personal.Relative && change.ApplyAuthor), "A later update adopted a previously protected personal target.");
            f.Commit(second); f.Has(personal);
        }
    }

    private static void ProvenLegacyAmbiguity() {
        foreach (bool policyOne in new[] { false, true }) {
            using Fixture f = new();
            Spec original = new("config/legacy.json", "original-author"), current = original with { Text = "uninstalled-author-v2" }, next = original with { Text = "author-v3" };
            f.Install(original);
            JObject baseline = f.LegacyAmbiguous(current, original);
            if (policyOne) baseline["associationPolicyVersion"] = 1;
            Plan plan = f.PrepareWithBaseline(baseline, [next]);
            Change update = Incoming(plan, next.Relative);
            Require(update.Ownership != PackUpdateOwnership.Ambiguous && update.LocalRelative == original.Relative && update.ApplyAuthor,
                "Fully proven legacy hash-only configuration ambiguity was not re-evaluated.");
            f.Commit(plan); f.Has(next);
            JObject saved = f.ReadManifest();
            Require(saved["associationPolicyVersion"]?.Value<int>() == 2 && saved["files"]![next.Relative]!["local"]!["ownership"]!.ToString() != "ambiguous",
                "Migrated ambiguity persisted without a versioned association policy.");
        }
    }

    private static void UnprovenLegacyAmbiguity() {
        foreach (string missing in new[] { "baselineRelative", "originalHash", "bytes", "modern-policy", "future-policy", "string-policy" }) {
            using Fixture f = new();
            Spec original = new("config/legacy.json", "original-author"), current = original with { Text = "uninstalled-author-v2" };
            Spec local = original with { Text = missing == "bytes" ? "later-player-edit" : original.Text };
            f.Install(local);
            JObject baseline = f.LegacyAmbiguous(current, original);
            JObject entry = (JObject)baseline["files"]![original.Relative]!;
            if (missing is "baselineRelative" or "originalHash") entry.Remove(missing);
            if (missing == "modern-policy") baseline["associationPolicyVersion"] = 2;
            if (missing == "future-policy") baseline["associationPolicyVersion"] = 3;
            if (missing == "string-policy") baseline["associationPolicyVersion"] = "2";
            Plan plan = f.PrepareWithBaseline(baseline, [current with { Text = "author-v3" }]);
            Require(!Incoming(plan, original.Relative).ApplyAuthor, "Incomplete or inapplicable migration evidence authorized a replacement: " + missing);
            f.Commit(plan); f.Has(local);
            JObject saved = f.ReadManifest();
            Plan repeated = f.PrepareWithBaseline(saved, [current with { Text = "author-v4" }]);
            Require(!Incoming(repeated, original.Relative).ApplyAuthor, "Unknown local ownership was rejected or adopted on the next update: " + missing);
            f.Commit(repeated); f.Has(local); f.ReadManifest();
        }
    }

    private static void UnknownRecordedPath() {
        foreach (bool canonicalExists in new[] { false, true }) foreach (bool removed in new[] { false, true }) {
            using Fixture f = new();
            Spec author = new("config/author.json", "author-before"), neighbor = new("config/other-player.json", "author-before");
            f.Install(neighbor); if (canonicalExists) f.Install(author);
            JObject baseline = Manifest(f.Package([author])); baseline.Remove("associationPolicyVersion");
            baseline["files"]![author.Relative]!["local"] = new JObject { ["relative"] = neighbor.Relative, ["hash"] = f.Hash(neighbor), ["ownership"] = "author" };
            Plan plan = f.PrepareWithBaseline(baseline, removed ? [] : [author with { Text = "author-after" }]);
            Require(!plan.Changes.Any(change => change.BaselineRelative == author.Relative && change.LocalRelative == neighbor.Relative), "An unverified observation path was accepted as configuration ownership.");
            f.Commit(plan); f.Has(neighbor);
            if (!canonicalExists) f.Absent(author.Relative);
        }
    }

    private static void ForgedCrossPathPlan() {
        using Fixture f = new();
        Spec old = new("config/author.json", "old"), neighbor = new("config/player.json", "old"), next = old with { Text = "new" };
        f.Install(old, neighbor);
        Plan plan = f.Prepare([old], [next]);
        Change change = Incoming(plan, next.Relative);
        change.LocalRelative = neighbor.Relative; change.Before = f.Hash(neighbor);
        change.Ownership = PackUpdateOwnership.AuthorRenamed; change.AllowAuthor = true; change.Conflict = false; change.Choice = PackUpdateChoice.UseAuthor;
        Throws<IOException>(() => f.Commit(plan));
        f.Has(old); f.Has(neighbor); f.Absent(PackUpdateEngine<PackFile, Package, Change, Plan, Download>.ManifestName);
    }

    private static void ForgedRuntimeDataPlan() {
        foreach (string path in RuntimePaths) foreach (bool remove in new[] { false, true }) foreach (bool backslash in new[] { false, true }) {
            using Fixture f = new();
            Spec original = new(path, "player-runtime-before"), incoming = original with { Text = "author-runtime-after" };
            f.Install(original);
            Plan plan = f.Prepare([original], remove ? [] : [incoming]);
            Change change = plan.Changes.Single(item => item.BaselineRelative == original.Relative);
            change.DisplayOnly = false; change.AllowAuthor = true; change.Conflict = false;
            change.AllowPreserveAuthor = false; change.Choice = PackUpdateChoice.UseAuthor;
            change.Ownership = PackUpdateOwnership.AuthorUnchanged; change.Association = PackUpdateAssociation.DeclaredPath;
            if (backslash) {
                change.Relative = change.Relative!.Replace('/', '\\');
                change.LocalRelative = change.LocalRelative!.Replace('/', '\\');
                change.BaselineRelative = change.BaselineRelative!.Replace('/', '\\');
            }
            bool refusedProtectedData = false;
            try { f.Commit(plan); }
            catch (IOException error) { refusedProtectedData = error.Message.Contains("玩家运行数据", StringComparison.Ordinal); }
            Require(refusedProtectedData, "A forced runtime-state operation was not rejected by the engine's protected-data guard.");
            f.Has(original); f.Absent(PackUpdateEngine<PackFile, Package, Change, Plan, Download>.ManifestName);
        }
    }

    private static void OccupiedAfterPreview() {
        using Fixture f = new();
        Spec next = new("config/new.json", "author"), player = next with { Text = "player-created-after-preview" }, neighbor = new("config/neighbor.json", "");
        f.Install(neighbor); Plan plan = f.Prepare([], [next]); f.Install(player);
        Throws<IOException>(() => f.Commit(plan)); f.Has(player); f.Has(neighbor);
    }

    private static void ConfigurationCancellation() {
        using Fixture f = new();
        Spec old = new("config/author.json", "before"), next = old with { Text = "after" }, neighbor = new("config/player.json", "before");
        f.Install(old, neighbor); JObject baseline = Manifest(f.Package([old])); f.WriteManifest(baseline);
        byte[] manifestBefore = File.ReadAllBytes(f.ManifestPath);
        Plan plan = f.PrepareWithBaseline(baseline, [next]);
        using CancellationTokenSource cancel = new();
        Throws<OperationCanceledException>(() => f.Engine.Commit(plan, f.Game, cancel.Token, beforeApply: index => { if (index == 1) cancel.Cancel(); }));
        f.Has(old); f.Has(neighbor);
        Require(File.ReadAllBytes(f.ManifestPath).SequenceEqual(manifestBefore), "Cancellation did not restore the original author manifest.");
    }

    private static Change Incoming(Plan plan, string relative) => plan.Changes.Single(change => change.Incoming?.Relative == relative);
    private static byte[] Bytes(Spec spec) => Encoding.UTF8.GetBytes(spec.Text);
    private static JObject Manifest(Package package) => PackUpdateEngine<PackFile, Package, Change, Plan, Download>.Manifest(package);
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception("Expected " + typeof(T).Name + "."); }
    private sealed record Spec(string Relative, string Text, string? Project = null);
    private sealed class PackFile : PackUpdateFileData { }
    private sealed class Package : PackUpdatePackageData<PackFile> { }
    private sealed class Change : PackUpdateChangeData<PackFile> { }
    private sealed class Plan : PackUpdatePlanData<Package, Change> { }
    private sealed record Download(List<string> Urls, string Path);

    private sealed class Fixture : IDisposable {
        private readonly string root = Path.Combine(Path.GetTempPath(), "PclPackAssociation-" + Guid.NewGuid().ToString("N"));
        private int counter;
        private readonly Dictionary<string, byte[]> downloads = new(StringComparer.Ordinal);
        internal string Game => Path.Combine(root, "game");
        internal string ManifestPath => Target(PackUpdateEngine<PackFile, Package, Change, Plan, Download>.ManifestName);
        internal readonly PackUpdateEngine<PackFile, Package, Change, Plan, Download> Engine;
        internal Fixture() {
            Directory.CreateDirectory(Game);
            Engine = new(new PackUpdateHost<PackFile, Package, Change, Plan, Download> {
                CreateFile = () => new(), CreatePackage = () => new(), CreateChange = () => new(), CreatePlan = () => new(),
                EnsureWithin = EnsureWithin, AtomicWrite = (path, bytes) => LocalGameDataStore.AtomicWrite(path, bytes, null, false),
                CreateDownload = (urls, path, _) => new(urls, path),
                Download = DownloadLocally,
                Request = (_, _, _) => throw new Exception("Association fixture attempted a metadata request."),
                TempFolder = StagePath, InstanceGameRoot = _ => Game, Warn = (error, message) => throw new Exception(message, error)
            });
        }
        internal string Target(string relative) => Engine.Target(Game, relative);
        private string StagePath() { string directory = Path.Combine(root, "stage-" + ++counter); Directory.CreateDirectory(directory); return directory; }
        internal void Install(params Spec[] specs) {
            foreach (Spec spec in specs) { string target = Target(spec.Relative); Directory.CreateDirectory(Path.GetDirectoryName(target)!); File.WriteAllBytes(target, Bytes(spec)); }
        }
        internal string Hash(Spec spec) {
            string path = Path.Combine(StagePath(), "hash-payload"); File.WriteAllBytes(path, Bytes(spec));
            return PackUpdateEngine<PackFile, Package, Change, Plan, Download>.Digest(path)!;
        }
        internal Package Package(Spec[] specs) {
            string directory = StagePath(), archivePath = Path.Combine(directory, "fixture.mrpack");
            JArray declared = new();
            foreach (Spec spec in specs.Where(spec => spec.Project is not null)) {
                string hash = Hash(spec), url = "https://cdn.modrinth.com/data/" + spec.Project + "/versions/fixture" + counter + "/" + declared.Count + ".zip";
                downloads.Add(url, Bytes(spec));
                declared.Add(new JObject { ["path"] = spec.Relative, ["hashes"] = new JObject { ["sha1"] = hash.Split(':')[1] },
                    ["downloads"] = new JArray(url), ["env"] = new JObject { ["client"] = "required" } });
            }
            JObject index = new() { ["formatVersion"] = 1, ["game"] = "minecraft", ["name"] = "Association fixture", ["versionId"] = "fixture-" + counter,
                ["dependencies"] = new JObject { ["minecraft"] = "1.21.11", ["fabric-loader"] = "0.18.4" }, ["files"] = declared };
            using (ZipArchive archive = ZipFile.Open(archivePath, ZipArchiveMode.Create)) {
                using (StreamWriter writer = new(archive.CreateEntry("modrinth.index.json").Open(), new UTF8Encoding(false))) writer.Write(index.ToString());
                foreach (Spec spec in specs.Where(spec => spec.Project is null)) { using Stream stream = archive.CreateEntry("overrides/" + spec.Relative).Open(); stream.Write(Bytes(spec)); }
            }
            return Engine.ReadPackage(archivePath, StagePath(), CancellationToken.None);
        }
        private void DownloadLocally(List<Download> files, CancellationToken token) {
            foreach (Download file in files) {
                token.ThrowIfCancellationRequested();
                if (file.Urls.Count != 1 || !downloads.TryGetValue(file.Urls[0], out byte[]? bytes)) throw new Exception("Fixture attempted an undeclared download.");
                Directory.CreateDirectory(Path.GetDirectoryName(file.Path)!); File.WriteAllBytes(file.Path, bytes);
            }
        }
        internal Plan Prepare(Spec[] original, Spec[] incoming) => PrepareWithBaseline(Manifest(Package(original)), incoming);
        internal Plan PrepareWithBaseline(JObject baseline, Spec[] incoming) {
            Plan plan = Engine.Plan(Game, baseline, Package(incoming)); Engine.DownloadChanges(plan, StagePath(), CancellationToken.None); return plan;
        }
        internal JObject LegacyAmbiguous(Spec declaration, Spec original) {
            JObject baseline = Manifest(Package([declaration])); baseline.Remove("associationPolicyVersion");
            JToken entry = baseline["files"]![declaration.Relative]!;
            entry["baselineRelative"] = declaration.Relative; entry["originalHash"] = Hash(original); entry["state"] = "retained";
            entry["local"] = new JObject { ["relative"] = declaration.Relative, ["hash"] = Hash(original), ["ownership"] = "ambiguous" };
            return baseline;
        }
        internal string Commit(Plan plan) => Engine.Commit(plan, Game, CancellationToken.None);
        internal JObject ReadManifest() => Engine.ReadManifest(Game)!;
        internal void WriteManifest(JObject manifest) => LocalGameDataStore.AtomicWrite(ManifestPath, Encoding.UTF8.GetBytes(manifest.ToString()), null, false);
        internal void Has(Spec spec) => Require(File.Exists(Target(spec.Relative)) && File.ReadAllBytes(Target(spec.Relative)).SequenceEqual(Bytes(spec)), "File missing or changed: " + spec.Relative);
        internal void Absent(string relative) => Require(!File.Exists(Target(relative)), "Unexpected file: " + relative);
        private static void EnsureWithin(string parent, string target) {
            string prefix = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!Path.GetFullPath(target).StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) throw new IOException("Association fixture path escaped its root.");
        }
        public void Dispose() {
            // Only this fixture's freshly generated GUID directory can be removed.
            string resolved = Path.GetFullPath(root), prefix = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!resolved.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(resolved).StartsWith("PclPackAssociation-", StringComparison.Ordinal))
                throw new IOException("Refusing to remove an unexpected fixture path.");
            if (Directory.Exists(resolved)) Directory.Delete(resolved, true);
        }
    }
}
