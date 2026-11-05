using System.IO.Compression;
using System.Text;
using Newtonsoft.Json.Linq;
using PCLCS;

internal static class PackUpdateGraphTests {
    internal static IEnumerable<(string Name, Action Run)> Cases() {
        yield return ("pack update final graph reuses an author dependency for a personal mod", AuthorUpgradeSatisfiesPersonal);
        yield return ("pack update refuses an author version that breaks a retained personal dependency", AuthorUpgradeBreaksPersonal);
        yield return ("pack update retains a compatible personal override then adopts a compatible later author version", PersonalOverrideAcrossUpdates);
        yield return ("pack update unknown metadata requires explicit acknowledgement and keeps logical filenames", UnknownNeedsAcknowledgement);
        yield return ("pack update final graph excludes disabled files without enabling or deleting them", DisabledExcluded);
        yield return ("pack update can retain an author-removed dependency needed by a personal mod", RetainRemovedAuthorDependency);
        yield return ("pack update provider alias ambiguity needs acknowledgement without deleting the personal provider", AliasNeedsAcknowledgement);
        yield return ("pack update final graph detects missing dependencies and explicit conflicts among new author files", AuthorCombinationErrors);
        yield return ("pack update final graph includes legacy Forge version-folder constraints and snapshots", LegacyVersionFolderConstraints);
        yield return ("pack update final graph loads only the target legacy version folder and marks unsupported scopes unknown", LegacyIncomingScope);
    }

    private static void AuthorUpgradeSatisfiesPersonal() {
        using Fixture f = new();
        FileSpec original = f.Spec("mods/library-1.jar", "author_library", "1.0", project: "library");
        FileSpec incoming = f.Spec("mods/library-2.jar", "author_library", "2.0", project: "library");
        f.Install(original);
        f.Install(f.Spec("mods/extra.jar", "personal_extra", "1.0", new() { ["author_library"] = ">=2.0" }));
        Plan plan = f.Plan([original], [incoming]); f.Stage(plan);
        var graph = f.Engine.CheckDependencies(plan);
        Require(!graph.HasErrors && !graph.HasUnknown, "The complete chosen author+personal graph should be known and satisfied.");
        f.Engine.Commit(plan, f.Game, CancellationToken.None);
        Require(File.Exists(f.Target(incoming.Relative)) && !File.Exists(f.Target(original.Relative)) && File.Exists(f.Target("mods/extra.jar")), "The valid upgrade failed to retain the personal mod.");
    }

    private static void AuthorUpgradeBreaksPersonal() {
        using Fixture f = new();
        FileSpec original = f.Spec("mods/library-1.jar", "author_library", "1.0", project: "library");
        FileSpec incoming = f.Spec("mods/library-2.jar", "author_library", "2.0", project: "library");
        f.Install(original);
        f.Install(f.Spec("mods/extra.jar", "personal_extra", "1.0", new() { ["author_library"] = ">=1.0 <2.0" }));
        Plan plan = f.Plan([original], [incoming]); f.Stage(plan);
        Require(plan.DependencyCheck?.HasErrors == true, "The preview did not see the complete final graph error.");
        Change choice = plan.Changes.Single(item => item.Incoming?.Relative == incoming.Relative);
        Require(choice.Conflict && choice.Choice == PackUpdateChoice.UseAuthor && choice.ApplyAuthor, "A problematic author upgrade must keep its default action but expose the current-version choice.");
        IOException failure = Throws<IOException>(() => f.Engine.Commit(plan, f.Game, CancellationToken.None));
        Require(failure.Message.Contains("依赖") && failure.Message.Contains("author_library"), "The rejected plan lost the conflicting dependency evidence.");
        Require(File.ReadAllBytes(f.Target(original.Relative)).SequenceEqual(original.Bytes) && !File.Exists(f.Target(incoming.Relative)) && !File.Exists(f.ManifestPath), "A rejected complete graph altered the instance or author baseline.");
        choice.Choice = PackUpdateChoice.PreservePersonal;
        Require(!f.Engine.CheckDependencies(plan).HasErrors && !f.Engine.CheckDependencies(plan).HasUnknown, "Keeping the compatible current author version did not resolve the actual personal dependency.");
        f.Engine.Commit(plan, f.Game, CancellationToken.None);
        JObject saved = f.Engine.ReadManifest(f.Game)!;
        Require(saved["files"]![incoming.Relative]!["hash"]!.ToString() == incoming.Hash && saved["files"]![incoming.Relative]!["local"]!["relative"]!.ToString() == original.Relative,
            "Keeping the current version overwrote the new author baseline or lost its local override mapping.");
        FileSpec later = f.Spec("mods/library-1.5.jar", "author_library", "1.5", project: "library");
        Plan next = f.Engine.Plan(f.Game, saved, f.Package([later])); f.Stage(next);
        next.Changes.Single(item => item.Incoming?.Relative == later.Relative).Choice = PackUpdateChoice.UseAuthor;
        f.Engine.Commit(next, f.Game, CancellationToken.None);
        Require(!File.Exists(f.Target(original.Relative)) && File.ReadAllBytes(f.Target(later.Relative)).SequenceEqual(later.Bytes), "A later compatible author version could not replace the tracked kept version.");
    }

    private static void PersonalOverrideAcrossUpdates() {
        using Fixture f = new();
        FileSpec original = f.Spec("mods/library-1.jar", "author_library", "1.0", project: "library");
        FileSpec local = f.Spec(original.Relative, "author_library", "1.5", project: "library");
        FileSpec incoming = f.Spec("mods/library-2.jar", "author_library", "2.0", project: "library");
        f.Install(local);
        f.Install(f.Spec("mods/extra.jar", "personal_extra", "1.0", new() { ["author_library"] = ">=1.4 <2.0" }));
        Plan plan = f.Plan([original], [incoming]); f.Stage(plan);
        Change change = plan.Changes.Single(item => item.Incoming?.Relative == incoming.Relative);
        Require(change.Conflict && !change.ApplyAuthor && !f.Engine.CheckDependencies(plan).HasErrors, "A compatible personal override was not retained by default.");
        change.Choice = PackUpdateChoice.UseAuthor;
        Throws<IOException>(() => f.Engine.ValidateChoices(plan));
        change.Choice = PackUpdateChoice.PreservePersonal;
        f.Engine.Commit(plan, f.Game, CancellationToken.None);
        JObject saved = f.Engine.ReadManifest(f.Game)!;
        Require(saved["files"]![incoming.Relative]!["hash"]!.ToString() == incoming.Hash && saved["files"]![incoming.Relative]!["local"]!["relative"]!.ToString() == original.Relative,
            "Preserving the override rewrote the author's expected bytes or lost the actual local association.");
        FileSpec later = f.Spec("mods/library-1.8.jar", "author_library", "1.8", project: "library");
        Plan next = f.Engine.Plan(f.Game, saved, f.Package([later])); f.Stage(next);
        next.Changes.Single(item => item.Incoming?.Relative == later.Relative).Choice = PackUpdateChoice.UseAuthor;
        Require(!f.Engine.CheckDependencies(next).HasErrors && !f.Engine.CheckDependencies(next).HasUnknown, "A later author version satisfying the personal mod was blocked.");
        f.Engine.Commit(next, f.Game, CancellationToken.None);
        JObject latest = f.Engine.ReadManifest(f.Game)!;
        Require(latest["files"]![later.Relative]!["hash"]!.ToString() == later.Hash && latest["files"]![later.Relative]!["state"]!.ToString() == "installed" &&
            !File.Exists(f.Target(original.Relative)) && File.ReadAllBytes(f.Target(later.Relative)).SequenceEqual(later.Bytes), "The later author upgrade did not clear the old personal override through the normal transaction.");
    }

    private static void UnknownNeedsAcknowledgement() {
        using Fixture f = new();
        FileSpec unknown = f.Raw("mods/unconfirmed.jar", Jar("readme.txt", "metadata intentionally unavailable"));
        Plan plan = f.Plan([], [unknown]); f.Stage(plan);
        Require(plan.DependencyCheck?.HasUnknown == true && !plan.DependencyCheck.HasErrors, "Unknown metadata was called compatible or a definite dependency conflict.");
        Require(plan.DependencyCheck.Warnings.Any(message => message.Contains(unknown.Relative)), "Temporary payload names hid the logical author filename.");
        Throws<IOException>(() => f.Engine.Commit(plan, f.Game, CancellationToken.None));
        Require(!File.Exists(f.Target(unknown.Relative)), "Unconfirmed dependencies committed without acknowledgement.");
        plan.AllowUnverifiedDependencies = true;
        f.Engine.Commit(plan, f.Game, CancellationToken.None);
        Require(File.ReadAllBytes(f.Target(unknown.Relative)).SequenceEqual(unknown.Bytes), "Explicitly acknowledged unknown content was not preserved byte-for-byte.");
    }

    private static void DisabledExcluded() {
        using Fixture f = new();
        FileSpec original = f.Spec("mods/library-1.jar", "author_library", "1.0", project: "library");
        FileSpec incoming = f.Spec("mods/library-2.jar", "author_library", "2.0", project: "library");
        FileSpec disabled = f.Spec("mods/disabled.jar.disabled", "disabled_extra", "1.0", new() { ["nonexistent_dependency"] = ">=1" });
        f.Install(original); f.Install(disabled);
        Plan plan = f.Plan([original], [incoming]); f.Stage(plan);
        Require(!f.Engine.CheckDependencies(plan).HasErrors && !f.Engine.CheckDependencies(plan).HasUnknown, "Disabled personal content contaminated the enabled final dependency graph.");
        f.Engine.Commit(plan, f.Game, CancellationToken.None);
        Require(File.ReadAllBytes(f.Target(disabled.Relative)).SequenceEqual(disabled.Bytes) && !File.Exists(f.Target("mods/disabled.jar")), "Dependency validation changed a disabled personal file.");
    }

    private static void RetainRemovedAuthorDependency() {
        using Fixture f = new();
        FileSpec original = f.Spec("mods/library.jar", "author_library", "1.0", project: "library");
        FileSpec extra = f.Spec("mods/extra.jar", "personal_extra", "1.0", new() { ["author_library"] = ">=1" });
        f.Install(original); f.Install(extra);
        Plan plan = f.Plan([original], []); f.Stage(plan);
        Change removal = plan.Changes.Single(item => item.BaselineRelative == original.Relative);
        Require(removal.Conflict && removal.ApplyAuthor && plan.DependencyCheck?.HasErrors == true, "Author removal should remain the default action and expose its final graph problem.");
        removal.Choice = PackUpdateChoice.PreservePersonal;
        f.Engine.Commit(plan, f.Game, CancellationToken.None);
        JObject saved = f.Engine.ReadManifest(f.Game)!;
        Require(saved["files"]![original.Relative] is null && saved["retained"]![original.Relative] is not null && File.Exists(f.Target(original.Relative)) && File.Exists(f.Target(extra.Relative)),
            "Keeping a removed component either deleted the personal dependency or falsely restored author ownership.");
    }

    private static void AliasNeedsAcknowledgement() {
        using Fixture f = new();
        FileSpec personal = f.Spec("mods/personal-alias.jar", "personal_extra", "1.0", provides: new JArray("author_library"));
        FileSpec author = f.Spec("mods/author-library.jar", "author_library", "2.0");
        f.Install(personal);
        Plan plan = f.Plan([], [author]); f.Stage(plan);
        Require(!plan.Changes.Single(change => change.Incoming?.Relative == author.Relative).Conflict,
            "A provider alias was treated as a definite self-ID file collision.");
        var graph = f.Engine.CheckDependencies(plan);
        Require(!graph.HasErrors && graph.HasUnknown, "Loader provider ambiguity lost its distinct unknown classification.");
        Throws<IOException>(() => f.Engine.Commit(plan, f.Game, CancellationToken.None));
        plan.AllowUnverifiedDependencies = true;
        f.Engine.Commit(plan, f.Game, CancellationToken.None);
        Require(File.ReadAllBytes(f.Target(personal.Relative)).SequenceEqual(personal.Bytes) && File.Exists(f.Target(author.Relative)),
            "Acknowledged alias ambiguity deleted or replaced the personal provider.");
    }

    private static void AuthorCombinationErrors() {
        foreach (bool explicitConflict in new[] { false, true }) {
            using Fixture f = new();
            FileSpec library = f.Spec("mods/library.jar", "author_library", "2.0");
            FileSpec broken = f.Spec("mods/broken.jar", "author_client", "1.0", explicitConflict ? null : new() { ["missing_author_dependency"] = ">=1" },
                breaks: explicitConflict ? new() { ["author_library"] = ">=2" } : null);
            Plan plan = f.Plan([], [library, broken]); f.Stage(plan);
            Require(f.Engine.CheckDependencies(plan).HasErrors, "New author entries were assumed compatible without checking their actual declarations.");
            plan.AllowUnverifiedDependencies = true;
            Throws<IOException>(() => f.Engine.Commit(plan, f.Game, CancellationToken.None));
            Require(!File.Exists(f.Target(library.Relative)) && !File.Exists(f.Target(broken.Relative)), "Acknowledging uncertainty bypassed a known dependency/conflict error.");
        }
    }

    private static void LegacyVersionFolderConstraints() {
        using Fixture f = new();
        FileSpec original = f.Legacy("mods/library-1.jar", "author_library", "1", project: "library");
        FileSpec next = f.Legacy("mods/library-2.jar", "author_library", "2", project: "library");
        FileSpec client = f.Legacy("mods/1.12.2/client.jar", "legacy_client", "1", "required-after:author_library@[1,2)");
        f.Install(original); f.Install(client);
        Package before = f.Package([original], "1.12.2", ModLoaders.Forge, "14.23.5.2855");
        Plan plan = f.Engine.Plan(f.Game, PackUpdateEngine<PackFile, Package, Change, Plan, object>.Manifest(before),
            f.Package([next], "1.12.2", ModLoaders.Forge, "14.23.5.2855"));
        f.Stage(plan);
        Require(plan.ObservedModFiles.Contains(client.Relative) && plan.Snapshot.ContainsKey(client.Relative), "The active legacy version-folder mod was omitted from the preview snapshot.");
        Require(plan.DependencyCheck?.HasErrors == true && plan.DependencyCheck.Errors.Any(message => message.Contains("legacy_client") && message.Contains("author_library")),
            "A root library replacement escaped an active mods/1.12.2 client's dependency constraint.");
        plan.AllowUnverifiedDependencies = true;
        Throws<IOException>(() => f.Engine.Commit(plan, f.Game, CancellationToken.None));
        Require(File.ReadAllBytes(f.Target(original.Relative)).SequenceEqual(original.Bytes) && !File.Exists(f.Target(next.Relative)), "A known legacy version-folder conflict changed the author library.");
        plan.Changes.Single(change => change.Incoming?.Relative == next.Relative).Choice = PackUpdateChoice.PreservePersonal;
        Require(!f.Engine.CheckDependencies(plan).HasErrors && !f.Engine.CheckDependencies(plan).HasUnknown, "Retaining the compatible library did not satisfy the real legacy annotation dependency.");
        File.WriteAllBytes(f.Target(client.Relative), f.Legacy(client.Relative, "legacy_client", "2", "required-after:author_library@[1,3)").Bytes);
        Throws<IOException>(() => f.Engine.Commit(plan, f.Game, CancellationToken.None));
        Require(!File.Exists(f.ManifestPath), "A modified legacy version-folder file did not invalidate the old preview.");
    }

    private static void LegacyIncomingScope() {
        using Fixture f = new();
        FileSpec library = f.Legacy("mods/library.jar", "author_library", "1", project: "library");
        FileSpec active = f.Legacy("mods/1.12.2/client.jar", "legacy_client", "1", "required-after:author_library@[1,2)");
        FileSpec inactive = f.Legacy("mods/1.11.2/duplicate.jar", "author_library", "9");
        f.Install(library);
        Package before = f.Package([library], "1.12.2", ModLoaders.Forge, "14.23.5.2855");
        Plan plan = f.Engine.Plan(f.Game, PackUpdateEngine<PackFile, Package, Change, Plan, object>.Manifest(before),
            f.Package([library, active, inactive], "1.12.2", ModLoaders.Forge, "14.23.5.2855"));
        f.Stage(plan);
        Require(!plan.DependencyCheck!.HasErrors && !plan.DependencyCheck.HasUnknown,
            "An inactive MC version-folder file was treated as an enabled duplicate or the active staged client was omitted.");
        f.Engine.Commit(plan, f.Game, CancellationToken.None);
        Require(File.Exists(f.Target(active.Relative)) && File.Exists(f.Target(inactive.Relative)), "Author package content outside the active loader scope was unexpectedly removed.");

        FileSpec custom = f.Legacy("mods/custom/client.jar", "custom_client", "1", "required-after:unavailable@[1,2)");
        JObject saved = f.Engine.ReadManifest(f.Game)!;
        Plan unknown = f.Engine.Plan(f.Game, saved, f.Package([library, active, inactive, custom], "1.12.2", ModLoaders.Forge, "14.23.5.2855"));
        f.Stage(unknown);
        Require(unknown.DependencyCheck!.HasUnknown && unknown.DependencyCheck.Warnings.Any(message => message.Contains(custom.Relative) && message.Contains("加载范围")),
            "An unsupported changed mod subdirectory was silently advertised as a complete checked graph.");
        Throws<IOException>(() => f.Engine.Commit(unknown, f.Game, CancellationToken.None));
    }

    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private static T Throws<T>(Action action) where T : Exception { try { action(); } catch (T error) { return error; } throw new Exception("Expected " + typeof(T).Name); }
    private static byte[] Jar(string name, string text) {
        using MemoryStream memory = new();
        using (ZipArchive archive = new(memory, ZipArchiveMode.Create, true))
            using (StreamWriter writer = new(archive.CreateEntry(name).Open(), new UTF8Encoding(false))) writer.Write(text);
        return memory.ToArray();
    }
    // A real RuntimeVisibleAnnotations attribute, read by the shared legacy
    // bytecode parser rather than a descriptive mcmod.info shortcut.
    private static byte[] LegacyJar(string id, string version, string? dependencies) {
        var values = new List<(string Name, string Value)> { ("modid", id), ("name", id), ("version", version) };
        if (dependencies is not null) values.Add(("dependencies", dependencies));
        List<string> pool = ["Lnet/minecraftforge/fml/common/Mod;", "RuntimeVisibleAnnotations", "fixture/Mod", "java/lang/Object"];
        foreach (var value in values) { pool.Add(value.Name); pool.Add(value.Value); }
        using MemoryStream stream = new();
        using (BinaryWriter writer = new(stream, Encoding.UTF8, true)) {
            void U2(int value) { writer.Write((byte)((value >> 8) & 255)); writer.Write((byte)(value & 255)); }
            void U4(long value) { U2((int)((value >> 16) & 65535)); U2((int)(value & 65535)); }
            U4(0xcafebabe); U2(0); U2(52); U2(pool.Count + 3);
            foreach (string text in pool) { byte[] bytes = Encoding.UTF8.GetBytes(text); writer.Write((byte)1); U2(bytes.Length); writer.Write(bytes); }
            writer.Write((byte)7); U2(3); writer.Write((byte)7); U2(4); U2(33); U2(pool.Count + 1); U2(pool.Count + 2); U2(0); U2(0); U2(0); U2(1); U2(2); U4(6 + 5 * values.Count); U2(1); U2(1); U2(values.Count);
            for (int i = 0; i < values.Count; i++) { U2(5 + 2 * i); writer.Write((byte)'s'); U2(6 + 2 * i); }
        }
        using MemoryStream zip = new();
        using (ZipArchive archive = new(zip, ZipArchiveMode.Create, true))
            using (Stream output = archive.CreateEntry("fixture/Mod.class").Open()) output.Write(stream.ToArray());
        return zip.ToArray();
    }
    private sealed record FileSpec(string Relative, byte[] Bytes, string Hash, string? Project);
    private sealed class PackFile : PackUpdateFileData { }
    private sealed class Package : PackUpdatePackageData<PackFile> { }
    private sealed class Change : PackUpdateChangeData<PackFile> { }
    private sealed class Plan : PackUpdatePlanData<Package, Change> { }
    private sealed class Fixture : IDisposable {
        private readonly string root = Path.Combine(Path.GetTempPath(), "PclPackGraph-" + Guid.NewGuid().ToString("N"));
        private int index;
        internal readonly string Game;
        internal readonly PackUpdateEngine<PackFile, Package, Change, Plan, object> Engine;
        internal string ManifestPath => Target("PCL/ModpackOrigin.json");
        internal Fixture() {
            Game = Path.Combine(root, "game"); Directory.CreateDirectory(Game);
            Engine = new(new() {
                CreateFile = () => new(), CreatePackage = () => new(), CreateChange = () => new(), CreatePlan = () => new(),
                EnsureWithin = (parent, path) => { if (!Path.GetFullPath(path).StartsWith(Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new IOException("Escaped fixture root."); },
                AtomicWrite = (path, bytes) => { Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllBytes(path, bytes); },
                Download = (_, _) => throw new Exception("The graph fixture must never use network transport."),
                TempFolder = Stage, InstanceGameRoot = _ => Game
            });
        }
        internal string Target(string relative) => Path.Combine(Game, relative.Replace('/', Path.DirectorySeparatorChar));
        private string Stage() { string path = Path.Combine(root, "stage-" + (++index)); Directory.CreateDirectory(path); return path; }
        internal FileSpec Spec(string relative, string id, string version, JObject? depends = null, string? project = null, JObject? breaks = null, JArray? provides = null) {
            JObject requirements = new() { ["minecraft"] = "*", ["fabricloader"] = ">=0.16.0" };
            if (depends is not null) foreach (JProperty property in depends.Properties()) requirements[property.Name] = property.Value.DeepClone();
            JObject metadata = new() { ["schemaVersion"] = 1, ["id"] = id, ["name"] = id, ["version"] = version, ["depends"] = requirements };
            if (breaks is not null) metadata["breaks"] = breaks;
            if (provides is not null) metadata["provides"] = provides;
            return Raw(relative, Jar("fabric.mod.json", metadata.ToString()), project);
        }
        internal FileSpec Raw(string relative, byte[] bytes, string? project = null) {
            string payload = Path.Combine(Stage(), "input.jar"); File.WriteAllBytes(payload, bytes);
            return new(relative, bytes, PackUpdateEngine<PackFile, Package, Change, Plan, object>.Digest(payload)!, project);
        }
        internal FileSpec Legacy(string relative, string id, string version, string? dependencies = null, string? project = null) =>
            Raw(relative, LegacyJar(id, version, dependencies), project);
        internal void Install(FileSpec spec) { string path = Target(spec.Relative); Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllBytes(path, spec.Bytes); }
        internal Package Package(IEnumerable<FileSpec> specs, string minecraft = "1.21.1", ModLoaders loader = ModLoaders.Fabric, string loaderVersion = "0.16.14") {
            Package result = new() { Name = "Full graph fixture", Version = "version-" + (++index), Minecraft = minecraft };
            result.Loaders[loader] = loaderVersion;
            foreach (FileSpec spec in specs) {
                string payload = Path.Combine(Stage(), "payload.jar"); File.WriteAllBytes(payload, spec.Bytes);
                result.Files[spec.Relative] = new() { Relative = spec.Relative, Hash = spec.Hash, Payload = payload, Identity = spec.Project is null ? null : "modrinth:" + spec.Project };
            }
            return result;
        }
        internal Plan Plan(FileSpec[] old, FileSpec[] next) => Engine.Plan(Game, PackUpdateEngine<PackFile, Package, Change, Plan, object>.Manifest(Package(old)), Package(next));
        internal void Stage(Plan plan) => Engine.DownloadChanges(plan, Stage(), CancellationToken.None);
        public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
