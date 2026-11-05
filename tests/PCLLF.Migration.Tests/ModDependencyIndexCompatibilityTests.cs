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

// Actual LF 50b028da5fcad2226d74d901a41d8c43a249bfb2 fixtures from
// scripts/Test-ModDependencies.ps1 and Test-ModDependencyOnline.ps1, LF LICENCE.
// Uses the real C# backend and real archives/index files; no game or JAR code executes.
internal static class ModDependencyIndexCompatibilityTests {
    internal static IEnumerable<(string Name, Action Run)> Cases() => LocalCases().Concat(OnlineCases());
    internal static IEnumerable<(string Name, Action Run)> LocalCases() {
        yield return ("dependency index persists graph identity and reuses locked archives while reevaluating Java", PersistAndReevaluate);
        yield return ("dependency index invalidates inventory and recovers corrupt or unavailable storage without publishing cancellation", InvalidationAndFailures);
        yield return ("dependency compatibility reads genuine legacy annotations and forces class-cache rebuilds without filename guesses", LegacyAndLoaderRules);
    }
    internal static IEnumerable<(string Name, Action Run)> OnlineCases() {
        yield return ("dependency online refresh covers disabled files and persists project identity separately from pinned versions", RefreshAndIdentity);
    }
    private static void Check(bool value, string text) { if (!value) throw new InvalidOperationException(text); }
    private static Context EnvironmentFor(string loader = "fabricloader", string version = "0.16.0", string minecraft = "1.20.1", string java = "17") {
        Context value = new() { MinecraftVersion = minecraft, JavaVersion = java, LoaderKnown = true }; value.LoaderVersions[loader] = version; return value;
    }
    private static JObject Fabric(string id, JObject? extras = null) {
        JObject value = new() { ["schemaVersion"] = 1, ["id"] = id, ["name"] = id, ["version"] = "1.0" };
        if (extras is not null) foreach (JProperty pair in extras.Properties()) value[pair.Name] = pair.Value.DeepClone(); return value;
    }
    private static byte[] Json(JToken value) => Encoding.UTF8.GetBytes(value.ToString(Formatting.None));
    private static string Jar(string folder, string name, params (string Path, byte[] Bytes)[] entries) {
        Directory.CreateDirectory(folder); string file = Path.Combine(folder, name); using ZipArchive zip = ZipFile.Open(file, ZipArchiveMode.Create);
        foreach (var entry in entries) { using Stream payload = zip.CreateEntry(entry.Path, CompressionLevel.NoCompression).Open(); payload.Write(entry.Bytes); } return file;
    }
    private static string Mod(string folder, string name, string id, JObject? extras = null) => Jar(folder, name, ("fabric.mod.json", Json(Fabric(id, extras))));
    private sealed class Fixture : IDisposable {
        internal readonly string Root = Path.Combine(Path.GetTempPath(), "pcllf-new-dependencies-" + Guid.NewGuid().ToString("N"));
        private readonly string previous = Local.Index.CacheDirectory;
        internal Fixture() { Directory.CreateDirectory(Root); Local.Index.CacheDirectory = Path.Combine(Root, "cache"); Local.Index.ClearMemory(); }
        internal string Folder(string name) { string path = Path.Combine(Root, name); Directory.CreateDirectory(path); return path; }
        public void Dispose() { Local.Index.ClearMemory(); Local.Index.CacheDirectory = previous; Directory.Delete(Root, true); }
    }
    private static void PersistAndReevaluate() {
        using Fixture fixture = new(); string folder = fixture.Folder("mods"); Context context = EnvironmentFor();
        Mod(folder, "core.jar", "core"); string user = Mod(folder, "user.jar", "user", new JObject { ["depends"] = new JObject { ["core"] = ">=1", ["java"] = ">=21" } });
        Report first = Local.Inspect(folder, context); Check(!first.IndexHit && first.IndexPersisted && first.HasErrors, "Initial tree was not persisted with its Java mismatch.");
        using (FileStream lease = new(user, FileMode.Open, FileAccess.Read, FileShare.None)) {
            Local.Index.ClearMemory(); Report second = Local.Inspect(folder, context); Check(second.IndexHit && second.Mods.Count == 2, "Disk-tree reuse reopened an exclusively locked archive.");
            var edge = second.Edges.Single(value => value.Requirement.Id == "core"); var owner = second.Mods.Single(value => value.Id == "user");
            Check(ReferenceEquals(edge.Owner, owner) && ReferenceEquals(edge.Requirement, owner.Dependencies.Single(value => value.Id == "core")), "JSON restore copied graph nodes instead of preserving edge identity.");
            Check(second.ReverseDependencies.Values.SelectMany(value => value).Any(value => ReferenceEquals(value, edge)), "Reverse dependencies lost edge identity.");
            Report changed = Local.Inspect(folder, EnvironmentFor(java: "21.0.2")); Check(changed.IndexHit && changed.IndexReevaluated && !changed.HasErrors, "Java-only change reread archives or kept old errors.");
        }
        first.Mods[0].Name = "caller mutation"; Report fresh = Local.Inspect(folder, EnvironmentFor(java: "21.0.2")); Check(fresh.Mods.All(value => value.Name != "caller mutation"), "Mutable caller report leaked into cached JSON.");
    }
    private static void InvalidationAndFailures() {
        using Fixture fixture = new(); string folder = fixture.Folder("mods"); Context context = EnvironmentFor();
        string core = Mod(folder, "core.jar", "core"); Mod(folder, "user.jar", "user", new JObject { ["depends"] = new JObject { ["core"] = "*" } });
        Report initial = Local.Inspect(folder, context); File.Move(core, core + ".disabled"); Report disabled = Local.Inspect(folder, context);
        Check(!disabled.IndexHit && disabled.HasErrors, "Disabling an actual provider reused a stale tree."); File.Move(core + ".disabled", core);
        Report enabled = Local.Inspect(folder, context); Check(!enabled.IndexHit && !enabled.HasErrors, "Re-enable did not invalidate the tree.");
        string extra = Mod(folder, "extra.jar", "extra"); Check(Local.Inspect(folder, context).Mods.Count == 3, "Addition was not observed."); File.Delete(extra); Check(Local.Inspect(folder, context).Mods.Count == 2, "Deletion was not observed.");
        string index = Local.Index.IndexPath(folder, context); File.WriteAllText(index, "{broken index"); Local.Index.ClearMemory(); Check(!Local.Inspect(folder, context).IndexHit, "Corrupt JSON index was trusted.");
        JObject broken = JObject.Parse(File.ReadAllText(index)); broken["Tree"]!["Mods"] = JValue.CreateNull(); File.WriteAllText(index, broken.ToString()); Local.Index.ClearMemory();
        Report repaired = Local.Inspect(folder, context); Check(!repaired.IndexHit && repaired.Mods.Count == 2, "Structurally invalid graph was trusted.");
        byte[] committed = File.ReadAllBytes(index); using (CancellationTokenSource canceled = new()) { canceled.Cancel(); bool observed = false; try { Local.Inspect(folder, context, canceled.Token); } catch (OperationCanceledException) { observed = true; } Check(observed && File.ReadAllBytes(index).SequenceEqual(committed), "Canceled inspection published a snapshot."); }
        Check(!Directory.EnumerateFiles(Local.Index.CacheDirectory, "*.tmp").Any(), "Index transaction left temporary files.");
        string firstLocked = fixture.Folder("first-locked"); string lockedJar = Mod(firstLocked, "locked.jar", "locked");
        using (FileStream locked = new(lockedJar, FileMode.Open, FileAccess.Read, FileShare.None)) {
            Report partial = Local.Inspect(firstLocked, context); Check(partial.IndexReadFailed && !partial.IndexPersisted, "A transient archive read failure was published as a complete index.");
        }
        Report recovered = Local.Inspect(firstLocked, context); Check(!recovered.IndexHit && recovered.IndexPersisted && recovered.Mods.Count == 1, "Releasing a previously locked archive did not recover the failed index.");
        string blocked = Path.Combine(fixture.Root, "cache-file"); File.WriteAllText(blocked, "fixture"); Local.Index.CacheDirectory = blocked;
        Report memoryOnly = Local.Inspect(folder, context); Check(!memoryOnly.IndexPersisted && !memoryOnly.HasErrors, "Unavailable disk cache blocked local checking or claimed persistence.");
        using FileStream lease = new(core, FileMode.Open, FileAccess.Read, FileShare.None); Report memoryHit = Local.Inspect(folder, context); Check(memoryHit.IndexHit && !memoryHit.IndexPersisted, "Memory fallback reopened archives or falsely claimed persistence.");
    }
    // Same genuine RuntimeVisibleAnnotations fixture as pinned LF's Windows regression.
    private static byte[] LegacyClass(params (string Name, string Value)[] values) {
        List<string> pool = ["Lnet/minecraftforge/fml/common/Mod;", "RuntimeVisibleAnnotations", "fixture/Mod", "java/lang/Object"];
        foreach (var value in values) { pool.Add(value.Name); pool.Add(value.Value); }
        using MemoryStream stream = new(); using BinaryWriter writer = new(stream); void U2(int value) { writer.Write((byte)((value >> 8) & 255)); writer.Write((byte)(value & 255)); } void U4(long value) { U2((int)((value >> 16) & 65535)); U2((int)(value & 65535)); }
        U4(0xcafebabe); U2(0); U2(52); U2(pool.Count + 3); foreach (string text in pool) { byte[] bytes = Encoding.UTF8.GetBytes(text); writer.Write((byte)1); U2(bytes.Length); writer.Write(bytes); }
        writer.Write((byte)7); U2(3); writer.Write((byte)7); U2(4); U2(33); U2(pool.Count + 1); U2(pool.Count + 2); U2(0); U2(0); U2(0); U2(1); U2(2); U4(6 + 5 * values.Length); U2(1); U2(1); U2(values.Length);
        for (int i = 0; i < values.Length; i++) { U2(5 + 2 * i); writer.Write((byte)'s'); U2(6 + 2 * i); } writer.Flush(); return stream.ToArray();
    }
    private static void LegacyAndLoaderRules() {
        using Fixture fixture = new(); string folder = fixture.Folder("legacy"); Context context = EnvironmentFor("forge", "14.23.5.2855", "1.12.2");
        byte[] annotation = LegacyClass(("modid", "real_mod"), ("name", "Real Mod"), ("version", "1"), ("dependencies", "required-after:forge@[14.21.1.2443,)"));
        string jar = Jar(folder, "actual.jar", ("fixture/Mod.class", annotation), ("mcmod.info", Encoding.UTF8.GetBytes("[{\"modid\":\"stale_template\",\"version\":\"${version}\"}]")));
        Report first = Local.Inspect(folder, context); Check(first.Mods.Count == 1 && first.Mods[0].Id == "real_mod" && !first.HasErrors, "Legacy loader did not use the actual class annotation.");
        Report foreign = Local.Inspect(folder, EnvironmentFor()); Check(foreign.Issues.Any(value => value.KindValue == ModDependencyIssueKind.LoaderMismatch && value.IsError), "Enabled legacy archive was silently accepted by Fabric.");
        Check(Local.LoaderWarningFor(folder, jar, EnvironmentFor()) != "", "Inspected mismatch did not produce a current file marker.");
        DateTime modified = File.GetLastWriteTimeUtc(jar); File.SetLastWriteTimeUtc(jar, modified.AddSeconds(1)); Check(Local.LoaderWarningFor(folder, jar, EnvironmentFor()) == "", "A stale content marker remained after file state changed.");
        string swap = Jar(fixture.Folder("replacement"), "actual.jar", ("fixture/Mod.class", LegacyClass(("modid", "real_mod"), ("name", "Real Mod"), ("version", "2"), ("dependencies", "required-after:forge@[14.21.1.2443,)"))), ("mcmod.info", Encoding.UTF8.GetBytes("[{\"modid\":\"stale_template\",\"version\":\"${version}\"}]")));
        FileInfo info = new(jar); DateTime created = info.CreationTimeUtc, written = info.LastWriteTimeUtc; Check(info.Length == new FileInfo(swap).Length, "Force-refresh fixture must keep the same inventory size."); File.WriteAllBytes(jar, File.ReadAllBytes(swap)); File.SetCreationTimeUtc(jar, created); File.SetLastWriteTimeUtc(jar, written);
        Report forced = Local.RebuildIndex(folder, context); Check(forced.Mods.Single().Version == "2", "Manual rebuild retained the old legacy class cache.");
        string schema = fixture.Folder("schema0"); JObject oldSchema = new() { ["id"] = "schema_zero", ["version"] = "1", ["requires"] = new JObject { ["minecraft"] = "1.20.1" }, ["recommends"] = new JObject { ["absent_recommendation"] = "*" } }; Jar(schema, "schema0.jar", ("fabric.mod.json", Json(oldSchema)));
        Report old = Local.Inspect(schema, EnvironmentFor()); Check(!old.MetadataIncomplete && !old.HasErrors && old.Mods.Single().Dependencies.Count > 0, "Fabric schema 0 declarations were not parsed reliably.");
        Check(Local.MatchVersion("0.9.0", "^0.2.1") == ModDependencyMatchState.Satisfied && Local.MatchVersion("1.1.0", "~1") == ModDependencyMatchState.Unsatisfied, "Pinned LF Fabric caret/tilde semantics changed.");
    }
    private static void RefreshAndIdentity() {
        using Fixture fixture = new(); string folder = fixture.Folder("online"); Context context = EnvironmentFor(); string api = Mod(folder, "api.jar", "api");
        string user = Mod(folder, "user.jar", "user", new JObject { ["depends"] = new JObject { ["api"] = ">=1" } }); string disabled = Mod(folder, "disabled.jar.disabled", "disabled");
        Dictionary<string, string> projects = new() { [api] = "apiProject", [user] = "userProject", [disabled] = "disabledProject" }; int requests = 0; List<(int Current, int Total)> progress = new();
        JToken Request(string url, HttpMethod method, string? body, CancellationToken token) {
            requests++; token.ThrowIfCancellationRequested();
            if (url.EndsWith("/version_files", StringComparison.Ordinal)) { JObject response = new(); foreach (var pair in projects) { string sha1 = Convert.ToHexString(SHA1.HashData(File.ReadAllBytes(pair.Key))).ToLowerInvariant(); JArray deps = pair.Key == user ? new JArray(new JObject { ["project_id"] = "apiProject", ["version_id"] = "apiOther", ["dependency_type"] = "required" }) : new(); response[sha1] = new JObject { ["id"] = pair.Value + "Installed", ["project_id"] = pair.Value, ["version_number"] = "1.0", ["files"] = new JArray(new JObject { ["hashes"] = new JObject { ["sha1"] = sha1 }, ["size"] = new FileInfo(pair.Key).Length }), ["dependencies"] = deps }; } return response; }
            if (url.Contains("/fingerprints/", StringComparison.Ordinal)) return new JObject { ["data"] = new JObject { ["exactMatches"] = new JArray() } };
            if (url.Contains("/versions?ids=", StringComparison.Ordinal)) return new JArray(new JObject { ["id"] = "apiOther", ["project_id"] = "apiProject", ["version_number"] = "2.0" });
            if (url.Contains("/projects?ids=", StringComparison.Ordinal)) return new JArray(projects.Values.Select(id => new JObject { ["id"] = id, ["project_type"] = "mod", ["title"] = id + " name" }));
            throw new InvalidOperationException("Unexpected controlled request: " + url);
        }
        Online engine = new(new ModDependencyOnlinePipelineTests.Cache(), Request); ModDependencyOnlinePipelineTests.Options options = new() { Progress = (current, total) => progress.Add((current, total)) };
        Report report = engine.RebuildWithOnline(folder, context, default, options); Check(!report.OnlineIncomplete && report.OnlineFiles.Count == 3 && report.OnlineFiles.All(value => value.IsSupplementTarget), "Manual refresh failed to cover enabled and disabled archives.");
        Check(progress.Count == 3 && progress[^1] == (3, 3) && report.OnlineIndexScope == "all-files", "Manual refresh did not publish actual file progress and its full-file scope.");
        var requirement = report.OnlineFiles.Single(value => value.FullPath == user).Matches.Single().Dependencies.Single(); Check(requirement.ProjectPaths.SequenceEqual([api]) && requirement.MatchedPaths.Count == 0 && requirement.VersionName == "2.0", "Project identity was conflated with a different pinned platform version.");
        Check(Local.RuntimeProvidersForPlatformDependency(report, user, requirement).Single().Id == "api" && !report.HasErrors, "Reliable local provider was replaced by a guessed online version.");
        int before = requests; engine.ResetIndexLookups(); Local.Index.ClearMemory(); Report cached = Local.Inspect(folder, context); engine.Enrich(cached, folder, default, options);
        Check(cached.IndexHit && cached.OnlineRequests == 0 && requests == before && cached.OnlineFiles.Count == 3, "Persisted full-file online tree made unnecessary requests after cache reset.");
        var savedRequirement = cached.OnlineFiles.Single(value => value.FullPath == user).Matches.Single().Dependencies.Single();
        cached.OnlineFiles.Single(value => value.FullPath == api).Matches.Single().ProjectId = "UnrelatedProjectWithTheSameTitle";
        Online.MapRelations(cached); Check(savedRequirement.ProjectPaths.Count == 0 && savedRequirement.MatchedPaths.Count == 0, "The original remapping hook inferred project identity from a shared title.");
    }
}
