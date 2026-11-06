using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using PCLCS;

internal static class PackUpdateSupplementPolicyTests {
    internal static IEnumerable<(string Name, Action Run)> Cases() {
        yield return ("pack update supplements personal required mods without adopting them into author files", SupplementAndCommit);
        yield return ("pack update tries an older dependency candidate satisfying the actual personal range", AlternativeAndCommit);
        yield return ("pack update resolves the selected alternative dependency closure and prunes the unused first choice closure", AlternativeDependencyClosure);
        yield return ("pack update supplement excludes protected author projects", ProtectedAuthorProject);
        yield return ("pack update supplement does not search names without exact platform evidence", NoPlatformMatch);
        yield return ("pack update supplement honors caller cancellation during fake metadata lookup", Cancellation);
    }
    private static void SupplementAndCommit() => RunUpdate(false);
    private static void AlternativeAndCommit() => RunUpdate(true);
    private static void AlternativeDependencyClosure() {
        using Fixture f = new(true, true);
        var package = f.Package();
        var plan = f.Engine.Plan(f.Game, PackUpdateEngine<F, P, C, L, D>.Manifest(package), package);
        f.Engine.DownloadChanges(plan, f.Stage, CancellationToken.None);
        var additions = plan.Changes.Where(change => change.Operation == PackUpdateOperation.PersonalDependency).ToArray();
        Require(additions.Length == 2 && additions.Any(change => change.Incoming!.FileIdentity == "modrinth:library15") &&
            additions.Any(change => change.Incoming!.FileIdentity == "modrinth:depb1"),
            "The selected alternative's actual B dependency was not resolved, or unused first-choice A remained in the transaction.");
        Require(!plan.Changes.Any(change => change.Operation == PackUpdateOperation.DisablePersonal), "A satisfiable transitive personal dependency caused the personal owner to be disabled.");
        var graph = f.Engine.CheckDependencies(plan);
        Require(!graph.HasErrors && !graph.HasUnknown, "The selected alternative plus its own dependency did not satisfy the complete graph.");
        f.Engine.Commit(plan, f.Game, CancellationToken.None);
        Require(File.ReadAllBytes(f.ExtraPath).SequenceEqual(f.Extra), "The personal extra was replaced or disabled.");
        Require(File.Exists(Path.Combine(f.Game, "mods", "library15.jar")) && File.Exists(Path.Combine(f.Game, "mods", "depb1.jar")), "Alternative and its B dependency were not installed.");
        Require(!File.Exists(Path.Combine(f.Game, "mods", "library2.jar")) && !File.Exists(Path.Combine(f.Game, "mods", "depa1.jar")), "Unselected first-choice version or its A dependency was installed.");
        Require(((JObject)f.Engine.ReadManifest(f.Game)!["files"]!).Count == 0, "Supplemented closure polluted author ownership.");
    }
    private static void RunUpdate(bool alternative) {
        using Fixture f = new(alternative);
        var package = f.Package();
        var plan = f.Engine.Plan(f.Game, PackUpdateEngine<F, P, C, L, D>.Manifest(package), package);
        f.Engine.DownloadChanges(plan, f.Stage, CancellationToken.None);
        Require(f.Downloaded.Contains(alternative ? "library15" : "library2"), "The required compatible dependency was not staged.");
        if (alternative) Require(f.Downloaded.Contains("library2"), "The first incompatible candidate was not exercised.");
        Require(!plan.Changes.Any(change => change.Operation == PackUpdateOperation.DisablePersonal), "A personal mod with a satisfiable dependency was disabled.");
        var dependency = plan.Changes.Single(change => change.Operation == PackUpdateOperation.PersonalDependency);
        Require(dependency.Incoming!.FileIdentity == "modrinth:" + (alternative ? "library15" : "library2"), "The final plan selected the wrong dependency version.");
        var graph = f.Engine.CheckDependencies(plan);
        Require(!graph.HasErrors && !graph.HasUnknown, "The final selected personal combination did not pass declaration checks.");
        f.Engine.Commit(plan, f.Game, CancellationToken.None);
        Require(File.ReadAllBytes(f.ExtraPath).SequenceEqual(f.Extra), "Personal content was replaced while supplementing dependencies.");
        Require(File.Exists(Path.Combine(f.Game, dependency.Relative!.Replace('/', Path.DirectorySeparatorChar))), "Selected dependency was not committed.");
        JObject saved = f.Engine.ReadManifest(f.Game)!;
        Require(((JObject)saved["files"]!).Count == 0, "Personal dependency or personal owner was adopted into the author manifest.");
    }
    private static void ProtectedAuthorProject() {
        using Fixture f = new(false);
        var result = f.Prepare(new HashSet<string> { "modrinth:library" });
        Require(result.Candidates.Count == 0, "Supplementation proposed another version of a protected author project.");
        Require(result.Notes.Any(note => note.Contains("作者")), "Protected author filtering lost its explanation.");
        Require(f.Downloaded.Count == 0, "Candidate preparation downloaded or installed content.");
    }
    private static void NoPlatformMatch() {
        using Fixture f = new(false) { MatchExtra = false };
        var result = f.Prepare(new HashSet<string>());
        Require(result.Candidates.Count == 0, "Unmatched local bytes were converted into a guessed dependency project.");
        Require(!f.Requests.Any(url => url.Contains("search", StringComparison.OrdinalIgnoreCase)), "Missing exact evidence triggered a name search.");
        Require(f.Downloaded.Count == 0 && Directory.GetFiles(Path.GetDirectoryName(f.ExtraPath)!).Length == 1, "Preparation changed installed files.");
    }
    private static void Cancellation() {
        using Fixture f = new(false);
        using CancellationTokenSource source = new();
        f.OnRequest = () => source.Cancel();
        bool canceled = false;
        try { f.Prepare(new HashSet<string>(), source.Token); }
        catch (OperationCanceledException) { canceled = true; }
        Require(canceled && f.Requests.Count > 0, "Cancellation during an actual metadata callback was swallowed.");
        Require(f.Downloaded.Count == 0 && File.ReadAllBytes(f.ExtraPath).SequenceEqual(f.Extra), "Canceled preparation modified personal files.");
    }
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private sealed class F : PackUpdateFileData { }
    private sealed class P : PackUpdatePackageData<F> { }
    private sealed class C : PackUpdateChangeData<F> { }
    private sealed class L : PackUpdatePlanData<P, C> { }
    private sealed record D(List<string> Urls, string Path);
    private sealed class Fixture : IDisposable {
        internal readonly string Root = Path.Combine(Path.GetTempPath(), "PclSupplementPolicy-" + Guid.NewGuid().ToString("N"));
        internal string Game => Path.Combine(Root, "game");
        internal string Stage => Path.Combine(Root, "stage");
        internal string ExtraPath => Path.Combine(Game, "mods", "extra.jar");
        internal readonly byte[] Extra;
        internal readonly List<string> Requests = new(), Downloaded = new();
        internal bool MatchExtra = true;
        internal Action? OnRequest;
        private readonly bool alternative;
        private readonly bool transitive;
        private readonly Dictionary<string, byte[]> payloads = new();
        internal readonly PackUpdateEngine<F, P, C, L, D> Engine;
        internal Fixture(bool alternative, bool transitive = false) {
            this.alternative = alternative;
            this.transitive = transitive;
            Extra = Jar("extra", "1.0", alternative ? ">=1 <2" : ">=2");
            payloads["library2"] = Jar("library", "2.0", transitive ? ">=1" : null, "depa");
            payloads["library15"] = Jar("library", "1.5", transitive ? ">=1" : null, "depb");
            if (transitive) { payloads["depa1"] = Jar("depa", "1.0", null); payloads["depb1"] = Jar("depb", "1.0", null); }
            Directory.CreateDirectory(Path.GetDirectoryName(ExtraPath)!); Directory.CreateDirectory(Stage);
            File.WriteAllBytes(ExtraPath, Extra);
            Engine = new(new PackUpdateHost<F, P, C, L, D> {
                CreateFile = () => new(), CreatePackage = () => new(), CreateChange = () => new(), CreatePlan = () => new(),
                EnsureWithin = EnsureWithin, AtomicWrite = (path, bytes) => File.WriteAllBytes(path, bytes),
                Request = Request, ParseVersion = Parse, CreateDownload = (urls, path, _) => new(urls, path),
                Download = (files, token) => { foreach (D file in files) {
                    token.ThrowIfCancellationRequested(); string id = Path.GetFileNameWithoutExtension(new Uri(file.Urls.Single()).AbsolutePath);
                    Require(payloads.ContainsKey(id), "Unexpected fake download: " + id); Downloaded.Add(id);
                    Directory.CreateDirectory(Path.GetDirectoryName(file.Path)!); File.WriteAllBytes(file.Path, payloads[id]);
                } },
                TempFolder = () => Stage, InstanceGameRoot = _ => Game,
                Warn = (error, message) => throw new Exception(message, error)
            });
        }
        internal P Package() => new() { Name = "Author", Version = "2", Minecraft = "1.21.11", Loaders = new() { [ModLoaders.Fabric] = "0.19.3" } };
        internal PackUpdateDependencyPreparation Prepare(ISet<string> protectedProjects, CancellationToken token = default) {
            var report = PackDependencyGraph.Check(new[] { ExtraPath }, "1.21.11", Package().Loaders, token).Report!;
            return PackUpdateDependencySupplement.Prepare(Path.GetDirectoryName(ExtraPath)!, report,
                new HashSet<string> { ExtraPath }, protectedProjects, Parse, Request, token);
        }
        private static JObject Project(string id) => new() { ["id"] = id, ["project_type"] = "mod", ["title"] = id, ["client_side"] = "required" };
        private JObject Version(string id) {
            bool extra = id == "extra1";
            byte[] bytes = extra ? Extra : payloads[id];
            string project = extra ? "extra" : id == "depa1" ? "depa" : id == "depb1" ? "depb" : "library";
            string? required = extra ? "library" : transitive && id == "library2" ? "depa" : transitive && id == "library15" ? "depb" : null;
            return new JObject { ["id"] = id, ["project_id"] = project, ["version_number"] = id,
                ["game_versions"] = new JArray("1.21.11"), ["loaders"] = new JArray("fabric"),
                ["dependencies"] = required is not null ? new JArray(new JObject { ["dependency_type"] = "required", ["project_id"] = required }) : new JArray(),
                ["files"] = new JArray(new JObject { ["primary"] = true, ["filename"] = id + ".jar", ["size"] = bytes.Length,
                    ["url"] = "https://cdn.modrinth.com/data/library/versions/fixture/" + id + ".jar",
                    ["hashes"] = new JObject { ["sha1"] = Convert.ToHexString(SHA1.HashData(bytes)).ToLowerInvariant() } }) };
        }
        private JToken Request(string url, HttpMethod? method, string? body) {
            Requests.Add(url); OnRequest?.Invoke();
            if (url.EndsWith("/version_files", StringComparison.Ordinal)) {
                JObject found = new();
                var hashes = JObject.Parse(body!)["hashes"]!.Select(value => value.ToString()).ToHashSet(StringComparer.Ordinal);
                foreach (string id in new[] { "extra1" }.Concat(payloads.Keys)) {
                    string hash = Convert.ToHexString(SHA1.HashData(id == "extra1" ? Extra : payloads[id])).ToLowerInvariant();
                    if (hashes.Contains(hash) && (id != "extra1" || MatchExtra)) found[hash] = Version(id);
                }
                return found;
            }
            if (url.Contains("/fingerprints/", StringComparison.Ordinal)) return new JObject { ["data"] = new JObject { ["exactMatches"] = new JArray() } };
            if (url.Contains("/projects?", StringComparison.Ordinal)) return new JArray(Project("extra"), Project("library"), Project("depa"), Project("depb"));
            if (url.Contains("/project/library/version?", StringComparison.Ordinal)) return alternative ? new JArray(Version("library2"), Version("library15")) : new JArray(Version("library2"));
            if (url.EndsWith("/project/library", StringComparison.Ordinal)) return Project("library");
            foreach (string project in new[] { "depa", "depb" }) {
                if (url.EndsWith("/project/" + project, StringComparison.Ordinal)) return Project(project);
                if (url.Contains("/project/" + project + "/version?", StringComparison.Ordinal)) return new JArray(Version(project + "1"));
            }
            throw new IOException("Unexpected fake metadata request: " + url);
        }
        private static ResourceVersionModelData Parse(JObject raw, ResourceTypes type) {
            var file = raw["files"]!.Single();
            return new ResourceVersionModel(new ResourceVersionServices { DebugMode = () => false, MirrorUrl = value => value, ImageRoot = () => "" }) {
                Id = raw["id"]!.ToString(), ResourceType = type, Display = raw["id"]!.ToString(),
                FileName = file["filename"]!.ToString(), Size = file["size"]!.Value<int>(), Hash = file["hashes"]!["sha1"]!.ToString(),
                DownloadUrls = new() { file["url"]!.ToString() }, GameVersions = new() { "1.21.11" }, ModLoaders = ModLoaders.Fabric,
                ReleaseTypeCode = 1, ReleaseDate = raw["id"]!.ToString() == "library2" ? new DateTime(2026, 2, 1) : new DateTime(2026, 1, 1)
            };
        }
        private static void EnsureWithin(string root, string path) {
            if (!Path.GetFullPath(path).StartsWith(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new IOException("Fixture path escaped its root.");
        }
        public void Dispose() {
            string resolved = Path.GetFullPath(Root), temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!resolved.StartsWith(temp, StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(resolved).StartsWith("PclSupplementPolicy-", StringComparison.Ordinal)) throw new IOException("Invalid fixture cleanup path.");
            if (Directory.Exists(resolved)) Directory.Delete(resolved, true);
        }
    }
    private static byte[] Jar(string id, string version, string? dependency, string dependencyId = "library") {
        JObject json = new() { ["schemaVersion"] = 1, ["id"] = id, ["version"] = version,
            ["depends"] = new JObject { ["minecraft"] = "1.21.11", ["fabricloader"] = ">=0.19" } };
        if (dependency is not null) json["depends"]![dependencyId] = dependency;
        using MemoryStream memory = new();
        using (ZipArchive zip = new(memory, ZipArchiveMode.Create, true))
        using (StreamWriter writer = new(zip.CreateEntry("fabric.mod.json").Open(), new UTF8Encoding(false))) writer.Write(json.ToString());
        return memory.ToArray();
    }
}
