using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using PCLCS;

internal static class PackDependencyOriginTests {
    internal static IEnumerable<(string Name, Action Run)> Cases() {
        yield return ("pack startup provenance separates author declarations from actual personal files", Classifications);
        yield return ("pack startup provenance proves pure renames using each recorded hash algorithm", RenamesAndAlgorithms);
        yield return ("pack startup provenance preserves disabled and optional states without review spam", PreservedStates);
        yield return ("pack startup provenance refuses ambiguous hashes and reverse author associations", Ambiguities);
        yield return ("pack startup provenance keeps personal and retained ownership across updates", RecordedProvenance);
        yield return ("pack startup origin fingerprints change for same-length same-mtime local bytes and manifest bytes", Fingerprints);
        yield return ("pack startup old missing broken and hinted origins never authorize personal additions", UntrustedOrigins);
        yield return ("pack startup provenance rejects escaped or linked paths and propagates cancellation", SafetyAndCancellation);
        yield return ("pack startup provenance reads only mods without modifying files or timestamps", ReadOnly);
        yield return ("pack author override observations preserve immutable declarations across two accepted replacements", OverrideRounds);
        yield return ("pack author override JSON rejects stale ambiguous duplicate or escaped associations", OverrideGuards);
        yield return ("pack startup versioned Forge mod folders keep provenance conservative instead of adopting root aliases", LegacyVersionFolders);
    }
    private static void Require(bool value, string text) { if (!value) throw new Exception(text); }
    private static T Throws<T>(Action action) where T : Exception {
        try { action(); } catch (T error) { return error; }
        throw new Exception("Expected " + typeof(T).Name);
    }
    private static PackDependencyFile Entry(PackDependencyOrigin value, string relative) =>
        value.Files.Single(file => file.Relative == relative);
    private static void Classifications() {
        using Fixture f = new();
        string a = f.Jar("mods/author.jar", "author"), b = f.Jar("source/replacement.jar", "replacement");
        f.Declare("mods/author.jar", a, "modrinth:author", "modrinth:v1");
        f.Declare("mods/changed.jar", b, "curseforge:42", "curseforge:55");
        f.Jar("mods/changed.jar", "player_override");
        f.Declare("mods/missing.jar", f.Jar("source/missing.jar", "missing"), "modrinth:missing");
        string extra = f.Jar("mods/extra.jar", "extra");
        string disabled = f.Jar("mods/personal.jar.disabled", "personal");
        f.Save();
        PackDependencyOrigin value = f.Read();
        Require(value.IsPack && value.Complete && value.NeedsAttention, "Complete author declarations must remain separate from missing author bytes.");
        Require(Entry(value, "mods/author.jar").Role == PackDependencyFileRole.AuthorUnchanged, "Unchanged author file was not proven.");
        Require(Entry(value, "mods/changed.jar").Role == PackDependencyFileRole.AuthorModified, "Personal replacement was not kept separate.");
        Require(Entry(value, "mods/missing.jar").Role == PackDependencyFileRole.AuthorMissing && Entry(value, "mods/missing.jar").LocalRelative is null, "Missing declaration gained a fictional local path.");
        Require(value.IsPersonal(extra) && value.IsPersonal(disabled), "Genuine personal additions including disabled addons need their own role.");
        Require(Entry(value, "mods/personal.jar.disabled").Role == PackDependencyFileRole.PersonalAdded && Entry(value, "mods/personal.jar.disabled").Disabled, "Disabled personal addon became an author file.");
        Require(!value.IsPersonal(a) && !value.IsPersonal(f.Path("mods/changed.jar")), "Declared author counterparts cannot be classified personal additions.");
        Require(value.ProtectsProject("Modrinth", "author") && value.ProtectsProject("CURSEFORGE", "42") && value.ProtectsProject("Modrinth", "missing"), "Author project protection lost installed or missing declarations.");
        Require(!value.ProtectsProject("Modrinth", "v1") && !value.ProtectsProject("Modrinth", "extra"), "File versions or mod names became project identities.");
        Require(value.Files.All(file => file.LocalRelative is null || file.LocalRelative.StartsWith("mods/")), "Provenance used the version folder instead of the isolated game root.");
    }
    private static void RenamesAndAlgorithms() {
        foreach (string algorithm in new[] { "md5", "sha1", "sha256", "sha512" }) {
            using Fixture f = new();
            string actual = f.Jar("mods/[锂] original.jar", "lithium");
            f.Declare("mods/original.jar", actual, "modrinth:lithium", algorithm: algorithm);
            f.Save();
            PackDependencyOrigin origin = f.Read();
            PackDependencyFile entry = Entry(origin, "mods/original.jar");
            Require(origin.Complete && !origin.NeedsAttention && entry.Role == PackDependencyFileRole.AuthorRenamed && entry.LocalRelative == "mods/[锂] original.jar", "Pure rename failed with " + algorithm);
            Require(entry.ExpectedHash!.StartsWith(algorithm + ":") && entry.ActualHash == Fixture.Hash(actual, "sha1", default), "Expected and observed hash algorithms were confused.");
            Require(!origin.IsPersonal(actual) && origin.Files.Count == 1, "Renamed author file was duplicated as a personal addition.");
        }
    }
    private static void PreservedStates() {
        using Fixture f = new();
        string disabled = f.Jar("mods/author.jar.disabled", "disabled");
        f.Declare("mods/author.jar", disabled, "modrinth:disabled");
        JObject optional = f.Declare("mods/optional.jar", f.Jar("source/optional.jar", "optional"), "modrinth:optional");
        optional["optional"] = true; optional["state"] = "optional-absent";
        string modified = f.Jar("source/modified.jar", "original");
        f.Declare("mods/modified.jar", modified);
        f.Jar("mods/modified.jar", "personal");
        f.Save();
        PackDependencyOrigin origin = f.Read();
        Require(!origin.NeedsAttention, "Known disabled/optional/personal-modified states should be annotations, not automatic repair prompts.");
        Require(Entry(origin, "mods/author.jar").Role == PackDependencyFileRole.AuthorDisabled && Entry(origin, "mods/author.jar").Disabled, "Disabled author file was treated as an active rename.");
        Require(Entry(origin, "mods/optional.jar").Role == PackDependencyFileRole.OptionalAbsent, "Unselected optional file was considered missing mandatory content.");
        Require(!origin.IsPersonal(disabled), "Disabled author version acquired personal-addition permission.");
    }
    private static void Ambiguities() {
        using (Fixture f = new()) {
            string original = f.Jar("mods/original.jar", "duplicate");
            File.Copy(original, f.Path("mods/copy.jar"));
            f.Declare("mods/original.jar", original); f.Save();
            PackDependencyOrigin origin = f.Read();
            Require(origin.NeedsAttention && Entry(origin, "mods/original.jar").Role == PackDependencyFileRole.Ambiguous && Entry(origin, "mods/original.jar").Candidates.Count == 2, "A duplicate byte-identical candidate was chosen arbitrarily.");
            Require(!origin.IsPersonal(f.Path("mods/copy.jar")), "An ambiguous copy was authorized as an unrelated personal addon.");
        }
        using (Fixture f = new()) {
            string actual = f.Jar("mods/remaining.jar", "shared");
            f.Declare("mods/old-a.jar", actual); f.Declare("mods/old-b.jar", actual); f.Save();
            PackDependencyOrigin origin = f.Read();
            Require(origin.Files.Count == 2 && origin.Files.All(file => file.Role == PackDependencyFileRole.Ambiguous), "One physical file discharged two author declarations.");
            Require(!origin.IsPersonal(actual), "Reverse association ambiguity became personal.");
        }
        using (Fixture f = new()) {
            string expected = f.Jar("source/old.jar", "old");
            f.Declare("mods/old.jar", expected);
            f.Jar("mods/old.jar.disabled", "modified_one"); f.Jar("mods/old.jar.old", "modified_two"); f.Save();
            PackDependencyOrigin origin = f.Read();
            Require(Entry(origin, "mods/old.jar").Role == PackDependencyFileRole.Ambiguous && origin.NeedsAttention, "Two changed disabled variants were picked by suffix order.");
        }
    }
    private static void RecordedProvenance() {
        using Fixture f = new();
        string player = f.Jar("mods/new-target.jar", "personal");
        JObject declared = f.Declare("mods/new-target.jar", player, "modrinth:expected");
        declared["local"] = new JObject { ["relative"] = "mods/new-target.jar", ["hash"] = Fixture.Hash(player, "sha1", default), ["ownership"] = "personal-added" };
        string old = f.Jar("mods/retained-old.jar", "retained");
        f.Manifest["retained"] = new JObject { ["mods/retained-old.jar"] = new JObject {
            ["authorRelative"] = "mods/old.jar", ["hash"] = Fixture.Hash(old, "sha1", default), ["identity"] = "modrinth:old",
            ["observedHash"] = Fixture.Hash(old, "sha1", default), ["ownership"] = "personal-retained", ["state"] = "retained"
        }};
        f.Save();
        PackDependencyOrigin first = f.Read();
        Require(first.IsPersonal(player) && Entry(first, "mods/new-target.jar").Role == PackDependencyFileRole.PersonalAdded, "Coincidentally identical bytes erased a recorded personal-addition provenance.");
        Require(!first.IsPersonal(old) && Entry(first, "mods/retained-old.jar").Role == PackDependencyFileRole.AuthorModified, "Personal retained old author file became a freely mutable addon.");
        declared["hash"] = Fixture.Hash(f.Jar("source/next.jar", "next"), "sha1", default);
        declared["fileIdentity"] = "modrinth:v2"; f.Save();
        PackDependencyOrigin second = first.Refresh();
        Require(second.IsPersonal(player) && second.ProtectsProject("modrinth", "expected"), "A later author declaration adopted the player's file or removed project protection.");
        declared["local"]!["ownership"] = "ambiguous"; f.Save();
        Require(!second.Refresh().IsPersonal(player), "Ambiguous recorded provenance granted personal-addition permission.");
    }
    private static void Fingerprints() {
        using Fixture f = new();
        string author = f.Jar("mods/author.jar", "author"), player = f.Jar("mods/player.jar", "player");
        f.Declare("mods/author.jar", author, "modrinth:author", "modrinth:v1"); f.Save();
        PackDependencyOrigin first = f.Read();
        Require(first.Fingerprint == first.Refresh().Fingerprint, "Unchanged source state generated unstable acceptance fingerprints.");
        byte[] bytes = File.ReadAllBytes(player); DateTime write = File.GetLastWriteTimeUtc(player);
        bytes[bytes.Length / 2] ^= 1; File.WriteAllBytes(player, bytes); File.SetLastWriteTimeUtc(player, write);
        PackDependencyOrigin changed = first.Refresh();
        Require(changed.Fingerprint != first.Fingerprint && changed.IsPersonal(player), "Same-length same-mtime content replacement did not invalidate the accepted state.");
        string manifest = f.ManifestPath; DateTime stamp = File.GetLastWriteTimeUtc(manifest);
        f.Manifest["files"]!["mods/author.jar"]!["fileIdentity"] = "modrinth:v2"; f.Save(); File.SetLastWriteTimeUtc(manifest, stamp);
        Require(changed.Refresh().Fingerprint != changed.Fingerprint, "Manifest bytes did not participate in the fingerprint.");
    }
    private static void UntrustedOrigins() {
        using (Fixture f = new()) {
            string local = f.Jar("mods/personal.jar", "personal");
            PackDependencyOrigin normal = f.Read();
            Require(!normal.IsPack && !normal.NeedsAttention && !normal.IsPersonal(local), "A normal instance was converted into a pack.");
            File.WriteAllText(Path.Combine(f.Game, "modrinth.index.json"), "{}");
            PackDependencyOrigin hinted = f.Read();
            Require(hinted.IsPack && !hinted.Complete && hinted.NeedsAttention && !hinted.IsPersonal(local), "Source-manifest hint allowed unrecorded addons.");
            Require(hinted.Files.All(file => file.Role == PackDependencyFileRole.Unknown), "A missing author baseline labelled all files personal additions.");
        }
        using (Fixture f = new()) {
            string local = f.Jar("mods/extra.jar", "extra"); f.Manifest["schema"] = 1; f.Manifest.Remove("complete"); f.Save();
            Require(f.Read().IsPack && !f.Read().Complete && !f.Read().IsPersonal(local), "Historical pruned schema one became authoritative.");
        }
        using (Fixture f = new()) {
            string local = f.Jar("mods/extra.jar", "extra");
            File.WriteAllText(f.ManifestPath, "{ damaged json");
            PackDependencyOrigin broken = f.Read();
            Require(broken.IsPack && !broken.Complete && broken.NeedsAttention && !broken.IsPersonal(local), "Broken author record enabled modifications.");
        }
        using (Fixture f = new()) {
            f.Jar("mods/extra.jar", "extra");
            PackDependencyOrigin mode = f.Read(packMode: true);
            Require(mode.IsPack && !mode.Complete && mode.NeedsAttention, "Managed pack mode without a manifest fell back to an ordinary instance.");
        }
    }
    private static void SafetyAndCancellation() {
        using Fixture f = new();
        string actual = f.Jar("mods/author.jar", "author");
        f.Declare("../escaped.jar", actual); f.Save();
        List<string> reads = new();
        PackDependencyOrigin unsafeOrigin = PackDependencyOrigin.Read(f.Version, f.Game, false, _ => JObject.Parse(File.ReadAllText(f.ManifestPath)),
            (path, algorithm, token) => { reads.Add(path); return Fixture.Hash(path, algorithm, token); });
        Require(!unsafeOrigin.Complete && unsafeOrigin.NeedsAttention && reads.All(path => path.StartsWith(f.Game + Path.DirectorySeparatorChar) || path == f.ManifestPath), "Escaped author path reached file hashing.");
        using CancellationTokenSource canceled = new(); canceled.Cancel();
        Require(Throws<OperationCanceledException>(() => f.Read(token: canceled.Token)).CancellationToken == canceled.Token, "Provenance swallowed or replaced cancellation.");
        if (!OperatingSystem.IsWindows()) {
            using Fixture links = new();
            string outside = links.Jar("source/outside.jar", "external");
            string link = links.Path("mods/link.jar");
            File.CreateSymbolicLink(link, outside);
            links.Save();
            List<string> opened = new();
            var origin = PackDependencyOrigin.Read(links.Version, links.Game, false, _ => links.Manifest.DeepClone() as JObject,
                (path, algorithm, token) => { opened.Add(path); return Fixture.Hash(path, algorithm, token); });
            Require(!origin.Complete && !origin.IsPersonal(link) && opened.All(path => path != link), "A linked local addon was read or authorized.");
        }
    }
    private static void ReadOnly() {
        using Fixture f = new();
        string author = f.Jar("mods/author.jar", "author"), extra = f.Jar("mods/extra.jar", "extra");
        f.Declare("mods/author.jar", author); f.Save();
        string config = f.Path("config/generated.cfg"); Directory.CreateDirectory(System.IO.Path.GetDirectoryName(config)!); File.WriteAllText(config, "generated");
        Dictionary<string, (byte[] Bytes, DateTime Write)> before = Directory.GetFiles(f.Root, "*", SearchOption.AllDirectories)
            .ToDictionary(path => path, path => (File.ReadAllBytes(path), File.GetLastWriteTimeUtc(path)));
        PackDependencyOrigin origin = f.Read();
        Require(!origin.Files.Any(file => file.Relative.StartsWith("config/")), "The provenance scan classified runtime-generated configs as author mods.");
        Require(origin.Files.Count == 2 && origin.IsPersonal(extra), "Read-only scan lost expected or personal local mods.");
        Require(before.Keys.Order().SequenceEqual(Directory.GetFiles(f.Root, "*", SearchOption.AllDirectories).Order()), "The scan created an index, stage or repaired author record.");
        foreach (var item in before) Require(File.ReadAllBytes(item.Key).SequenceEqual(item.Value.Bytes) && File.GetLastWriteTimeUtc(item.Key) == item.Value.Write, "Provenance reading changed existing bytes or timestamps.");
    }
    private static void OverrideRounds() {
        using Fixture f = new();
        string original = f.Jar("mods/author.jar", "original");
        f.Declare("mods/author.jar", original, "modrinth:project", "modrinth:original");
        f.Declare("mods/unrelated.jar", f.Jar("mods/unrelated.jar", "unrelated"), "curseforge:11", "curseforge:12");
        string personal = f.Jar("mods/personal.jar", "personal"); f.Save();
        JObject authorExpected = (JObject)f.Manifest["files"]!["mods/author.jar"]!.DeepClone();
        JObject unrelatedExpected = (JObject)f.Manifest["files"]!["mods/unrelated.jar"]!.DeepClone();
        PackDependencyOrigin origin = f.Read();
        Require(origin.GameRoot == f.Game && origin.VersionFolder == f.Version && origin.FindLocal(original)?.Role == PackDependencyFileRole.AuthorUnchanged, "Origin did not expose its real isolated roots or unique local counterpart.");
        for (int round = 1; round <= 2; round++) {
            string stage = f.Jar("source/replacement-" + round + ".jar", "replacement_" + round);
            string relative = "mods/replacement-" + round + ".jar", destination = f.Path(relative);
            string hash = Fixture.Hash(stage, round == 1 ? "sha1" : "sha512", default)!;
            byte[] manifestBefore = File.ReadAllBytes(f.ManifestPath);
            JObject next = origin.BuildOverrideManifest(new[] { new PackDependencyOverride {
                AuthorRelative = "mods/author.jar", LocalRelative = relative, ActualHash = hash,
                FileIdentity = "modrinth:replacement" + round, Reason = "accepted full-graph personal dependency override"
            }});
            Require(File.ReadAllBytes(f.ManifestPath).SequenceEqual(manifestBefore) && !File.Exists(destination), "Building override JSON wrote a manifest or installed a file.");
            JObject record = (JObject)next["files"]!["mods/author.jar"]!;
            foreach (string key in new[] { "hash", "identity", "fileIdentity", "optional" }) Require(JToken.DeepEquals(record[key], authorExpected[key]), "An accepted override changed author expectation " + key);
            Require(JToken.DeepEquals(next["files"]!["mods/unrelated.jar"], unrelatedExpected), "An override changed unrelated author declarations.");
            Require(((JObject)next["files"]!).Count == 2 && next["files"]!["mods/personal.jar"] is null && next["files"]![relative] is null, "Personal files or replacement paths became new author declarations.");
            Require(record["state"]!.ToString() == "modified" && record["local"]!["relative"]!.ToString() == relative && record["local"]!["hash"]!.ToString() == hash && record["local"]!["ownership"]!.ToString() == "author", "Override observation lost its author link or actual bytes.");
            Require(record["baselineRelative"]!.ToString() == "mods/author.jar" && JToken.DeepEquals(record["originalHash"], authorExpected["hash"]), "Repeated overrides rewrote original author evidence.");
            PackDependencyFile before = origin.Files.Single(file => file.Relative == "mods/author.jar");
            File.Move(stage, destination);
            File.Delete(f.Path(before.LocalRelative!));
            File.WriteAllText(f.ManifestPath, next.ToString());
            origin = origin.Refresh();
            PackDependencyFile observed = origin.FindLocal(destination)!;
            Require(observed.Role == PackDependencyFileRole.AuthorModified && observed.Relative == "mods/author.jar" && observed.LocalRelative == relative, "A later read did not preserve the modified author's new local path.");
            Require(observed.ExpectedHash == authorExpected["hash"]!.ToString() && origin.ProtectsProject("modrinth", "project") && !origin.IsPersonal(destination) && origin.IsPersonal(personal), "A replacement lost author protection or changed personal provenance.");
        }
    }
    private static void OverrideGuards() {
        using Fixture f = new();
        string a = f.Jar("mods/a.jar", "a"), b = f.Jar("mods/b.jar", "b"), personal = f.Jar("mods/personal.jar", "p");
        f.Declare("mods/a.jar", a); f.Declare("mods/b.jar", b); f.Save();
        PackDependencyOrigin origin = f.Read();
        string hash = Fixture.Hash(f.Jar("source/new.jar", "new"), "sha1", default)!;
        PackDependencyOverride Change(string author = "mods/a.jar", string local = "mods/new.jar", string? actual = null) => new() { AuthorRelative = author, LocalRelative = local, ActualHash = actual ?? hash };
        byte[] unchanged = File.ReadAllBytes(f.ManifestPath);
        Throws<IOException>(() => origin.BuildOverrideManifest(new[] { Change(), Change() }));
        Throws<IOException>(() => origin.BuildOverrideManifest(new[] { Change(), Change("mods/b.jar") }));
        Throws<IOException>(() => origin.BuildOverrideManifest(new[] { Change("mods/unknown.jar") }));
        Throws<IOException>(() => origin.BuildOverrideManifest(new[] { Change(local: "../outside.jar") }));
        Throws<IOException>(() => origin.BuildOverrideManifest(new[] { Change(local: "config/generated.json") }));
        Throws<IOException>(() => origin.BuildOverrideManifest(new[] { Change(actual: "sha999:abcd") }));
        Throws<IOException>(() => origin.BuildOverrideManifest(new[] { Change(local: "mods/b.jar") }));
        Throws<IOException>(() => origin.BuildOverrideManifest(new[] { Change(local: "mods/personal.jar") }));
        Require(File.ReadAllBytes(f.ManifestPath).SequenceEqual(unchanged), "Refused author overrides changed the manifest.");
        byte[] bytes = File.ReadAllBytes(personal); DateTime write = File.GetLastWriteTimeUtc(personal);
        bytes[bytes.Length / 2] ^= 1; File.WriteAllBytes(personal, bytes); File.SetLastWriteTimeUtc(personal, write);
        Throws<IOException>(() => origin.BuildOverrideManifest(new[] { Change() }));
        using Fixture ambiguous = new();
        string source = ambiguous.Jar("mods/a.jar", "a"); File.Copy(source, ambiguous.Path("mods/copy.jar"));
        ambiguous.Declare("mods/a.jar", source); ambiguous.Save();
        Throws<IOException>(() => ambiguous.Read().BuildOverrideManifest(new[] { Change() }));
        using Fixture disabled = new();
        string inactive = disabled.Jar("mods/a.jar.disabled", "a"); disabled.Declare("mods/a.jar", inactive); disabled.Save();
        Throws<IOException>(() => disabled.Read().BuildOverrideManifest(new[] { Change() }));
        disabled.Manifest["schema"] = 1; disabled.Manifest.Remove("complete"); disabled.Save();
        Throws<IOException>(() => disabled.Read().BuildOverrideManifest(new[] { Change(local: "mods/new.jar.disabled") }));
    }
    private static void LegacyVersionFolders() {
        using (Fixture f = new()) {
            string versioned = f.Jar("mods/1.12.2/author.jar", "versioned_author");
            string alias = f.Path("mods/same-bytes.jar"); File.Copy(versioned, alias);
            f.Declare("mods/1.12.2/author.jar", versioned, "curseforge:42", "curseforge:99"); f.Save();
            PackDependencyOrigin origin = f.Read();
            Require(origin.IsPack && !origin.Complete && origin.NeedsAttention && origin.Notes.Any(note => note.Contains("版本子目录", StringComparison.Ordinal)), "Version-scoped author declarations were silently omitted from a complete provenance inventory.");
            Require(origin.ProtectsProject("curseforge", "42") && Entry(origin, "mods/1.12.2/author.jar").Role == PackDependencyFileRole.Unknown, "A legitimate versioned author file lost its project protection or was called an invalid path.");
            Require(!origin.IsPersonal(alias) && Entry(origin, "mods/same-bytes.jar").Role == PackDependencyFileRole.Unknown, "A root-level alias of a versioned author file became a personal addition.");
            Throws<IOException>(() => origin.BuildOverrideManifest(new[] { new PackDependencyOverride { AuthorRelative = "mods/1.12.2/author.jar", LocalRelative = "mods/replaced.jar", ActualHash = Fixture.Hash(versioned, "sha1", default)! } }));
        }
        using (Fixture f = new()) {
            string root = f.Jar("mods/personal.jar", "personal"), versioned = f.Jar("mods/1.7.10/core.jar", "core"); f.Save();
            PackDependencyOrigin first = f.Read();
            Require(!first.Complete && first.NeedsAttention && !first.IsPersonal(root) && !first.IsPersonal(versioned), "An actual active version-folder JAR was ignored when granting personal provenance.");
            byte[] bytes = File.ReadAllBytes(versioned); DateTime write = File.GetLastWriteTimeUtc(versioned);
            bytes[bytes.Length / 2] ^= 1; File.WriteAllBytes(versioned, bytes); File.SetLastWriteTimeUtc(versioned, write);
            Require(first.Refresh().Fingerprint != first.Fingerprint, "Content changes in an unsupported version folder did not invalidate its provenance fingerprint.");
        }
        using (Fixture f = new()) {
            string personal = f.Jar("mods/personal.jar", "personal");
            f.Jar("mods/1.12.2/not-active.jar.disabled", "inactive");
            f.Jar("mods/.runtime-cache/not-loaded.jar", "cache"); f.Save();
            Require(f.Read().Complete && f.Read().IsPersonal(personal), "Disabled version-folder files or unrelated cache directories were treated as active legacy Forge providers.");
        }
    }

    private sealed class Fixture : IDisposable {
        internal string Root { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "PclLfPackOrigin-" + Guid.NewGuid().ToString("N"));
        internal string Version => System.IO.Path.Combine(Root, "version");
        internal string Game => System.IO.Path.Combine(Root, "isolated-game");
        internal string ManifestPath => System.IO.Path.Combine(Version, "PCL", "ModpackOrigin.json");
        internal JObject Manifest { get; } = new() {
            ["schema"] = 2, ["complete"] = true, ["name"] = "Pack", ["version"] = "one", ["files"] = new JObject(),
            ["minecraft"] = "1.21.1", ["loaders"] = new JObject { ["Fabric"] = "0.16.14" }
        };
        internal Fixture() { Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ManifestPath)!); Directory.CreateDirectory(Game); }
        internal string Path(string relative) {
            string value = System.IO.Path.Combine(Game, relative.Replace('/', System.IO.Path.DirectorySeparatorChar));
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(value)!); return value;
        }
        internal string Jar(string relative, string id) {
            string path = Path(relative);
            using ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Create);
            using StreamWriter writer = new(archive.CreateEntry("fabric.mod.json").Open(), new UTF8Encoding(false));
            writer.Write(new JObject { ["schemaVersion"] = 1, ["id"] = id, ["version"] = "1.0" }.ToString());
            return path;
        }
        internal JObject Declare(string relative, string source, string? identity = null, string? fileIdentity = null, string algorithm = "sha1") {
            JObject record = new() { ["hash"] = Hash(source, algorithm, default), ["identity"] = identity, ["fileIdentity"] = fileIdentity, ["optional"] = false };
            Manifest["files"]![relative] = record; return record;
        }
        internal void Save() => File.WriteAllText(ManifestPath, Manifest.ToString(), new UTF8Encoding(false));
        internal PackDependencyOrigin Read(bool packMode = false, CancellationToken token = default) =>
            PackDependencyOrigin.Read(Version, Game, packMode, _ => File.Exists(ManifestPath) ? JObject.Parse(File.ReadAllText(ManifestPath)) : null, Hash, token);
        internal static string? Hash(string path, string algorithm, CancellationToken token) {
            token.ThrowIfCancellationRequested();
            if (!File.Exists(path)) return null;
            using HashAlgorithm hash = algorithm switch { "md5" => MD5.Create(), "sha1" => SHA1.Create(), "sha256" => SHA256.Create(), "sha512" => SHA512.Create(), _ => throw new IOException("Unknown hash") };
            using FileStream stream = File.OpenRead(path);
            string result = algorithm + ":" + Convert.ToHexString(hash.ComputeHash(stream)).ToLowerInvariant();
            token.ThrowIfCancellationRequested(); return result;
        }
        public void Dispose() => Directory.Delete(Root, true);
    }
}
