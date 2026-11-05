using System.IO.Compression;
using System.Text;
using Newtonsoft.Json.Linq;
using PCLCS;

internal static class PackDependencyGraphTests {
    internal static IEnumerable<(string Name, Action Run)> Cases() {
        yield return ("pack final graph reuses installed author providers for personal dependencies", ExistingProvider);
        yield return ("pack final graph allows a replacement only when the whole declared combination remains valid", Replacement);
        yield return ("pack final graph detects broken author constraints after a personal replacement", BrokenAuthor);
        yield return ("pack final graph detects duplicate IDs and actual incompatible declarations", Conflicts);
        yield return ("pack final graph treats alias provider ambiguity separately from project ownership", AliasProviders);
        yield return ("pack final graph keeps malformed metadata and unknown required versions unresolved", Unknown);
        yield return ("pack final graph selects multi-loader metadata and separates shared NeoForge dependencies", Loaders);
        yield return ("pack final graph preserves nested candidate semantics without flat duplicate IDs", NestedCandidates);
        yield return ("pack final graph ignores disabled files and preserves cancellation and file bytes", ReadBoundaries);
    }

    private static Dictionary<ModLoaders, string> FabricLoader() => new() { [ModLoaders.Fabric] = "0.16.14" };
    private static PackDependencyGraphResult Check(params string[] files) => PackDependencyGraph.Check(files, "1.21.1", FabricLoader());
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Clean(PackDependencyGraphResult value) => Require(!value.HasErrors && !value.HasUnknown,
        "The final valid combination was rejected: " + string.Join(";", value.Errors.Concat(value.Warnings)));
    private static JObject Fabric(string id, string version, JObject? depends = null, JObject? breaks = null) => new() {
        ["schemaVersion"] = 1, ["id"] = id, ["name"] = id, ["version"] = version,
        ["depends"] = depends ?? new JObject { ["minecraft"] = ">=1.21", ["fabricloader"] = ">=0.16" },
        ["breaks"] = breaks ?? new JObject()
    };
    private static void ExistingProvider() {
        using Fixture f = new();
        string provider = f.Fabric("author-library.jar", Fabric("author_library", "1.0.0"));
        string addon = f.Fabric("personal.jar", Fabric("personal", "1", new JObject { ["author_library"] = ">=1" }));
        Require(Check(addon).HasErrors, "A real missing library was not detected in isolation.");
        Clean(Check(provider, addon));
        Clean(Check(addon, provider, provider)); // Repeated path is one physical input, not a duplicate mod.
    }
    private static void Replacement() {
        using Fixture f = new();
        string old = f.Fabric("old.jar", Fabric("author_library", "1.0.0"));
        string next = f.Fabric("next.jar", Fabric("author_library", "2.0.0"));
        string author = f.Fabric("author-client.jar", Fabric("author_client", "1", new JObject { ["author_library"] = ">=1 <3" }));
        string addon = f.Fabric("personal.jar", Fabric("personal", "1", new JObject { ["author_library"] = ">=2 <3" }));
        Require(Check(old, author, addon).HasErrors, "The old version must not satisfy the personal >=2 requirement.");
        Clean(Check(next, author, addon));
        Require(File.Exists(old), "A read-only proposed replacement removed the old author file.");
    }
    private static void BrokenAuthor() {
        using Fixture f = new();
        string old = f.Fabric("old.jar", Fabric("author_library", "1.0.0"));
        string next = f.Fabric("next.jar", Fabric("author_library", "2.0.0"));
        string author = f.Fabric("author-client.jar", Fabric("author_client", "1", new JObject { ["author_library"] = "<2" }));
        string addon = f.Fabric("personal.jar", Fabric("personal", "1", new JObject { ["author_library"] = ">=2" }));
        Clean(Check(old, author));
        var result = Check(next, author, addon);
        Require(result.HasErrors && result.Errors.Any(text => text.Contains("author_client") && text.Contains("author_library")), "Checking only the personal requirement hid damage to the author graph.");
    }
    private static void Conflicts() {
        using Fixture f = new();
        string first = f.Fabric("author.jar", Fabric("author_mod", "1"));
        string copy = f.Fabric("copy.jar", Fabric("author_mod", "1"));
        Require(Check(first, copy).HasErrors, "Two active top-level own IDs were accepted.");
        string incompatible = f.Fabric("personal.jar", Fabric("personal", "1", breaks: new JObject { ["author_mod"] = "*" }));
        Require(Check(first, incompatible).HasErrors, "An explicit incompatible declaration was ignored.");
    }
    private static void AliasProviders() {
        using Fixture f = new();
        JObject withAlias = Fabric("first_owner", "1"); withAlias["provides"] = new JArray("shared_alias");
        string first = f.Fabric("first.jar", withAlias);
        string consumer = f.Fabric("consumer.jar", Fabric("consumer", "1", new JObject { ["shared_alias"] = "*" }));
        Clean(Check(first, consumer));
        JObject otherAlias = Fabric("second_owner", "1"); otherAlias["provides"] = new JArray("shared_alias");
        string second = f.Fabric("second.jar", otherAlias);
        var result = Check(first, second, consumer);
        Require(result.HasUnknown && result.Warnings.Any(text => text.Contains("shared_alias") && text.Contains("提供")), "Ambiguous alias providers were advertised as a confirmed safe combination.");
        Require(!result.Errors.Any(text => text.Contains("重复模组 ID")), "Alias overlap was incorrectly promoted to duplicate own identity.");
    }
    private static void Unknown() {
        using Fixture f = new();
        string damaged = f.Jar("damaged.jar", ("fabric.mod.json", Encoding.UTF8.GetBytes("{broken")));
        string unknown = f.Fabric("unknown.jar", Fabric("uncertain", "1", new JObject { ["java"] = ">=17" }));
        Require(Check(damaged).HasUnknown, "Malformed metadata was considered verified.");
        var missingJava = Check(unknown);
        Require(missingJava.HasUnknown && missingJava.Warnings.Any(text => text.Contains("unknown.jar") && text.Contains("java")), "Unknown Java runtime requirement was not identified with its actual owner file.");
        Clean(PackDependencyGraph.Check([unknown], "1.21.1", FabricLoader(), javaVersion: "21.0.1"));
        Require(Check(Path.Combine(f.Root, "missing.jar")).HasUnknown, "A missing proposed payload was considered verified.");
        var noContext = PackDependencyGraph.Check([unknown], null, new());
        Require(noContext.HasUnknown, "Missing target runtime context was considered verified.");
    }
    private static void Loaders() {
        using Fixture f = new();
        string ae2 = f.Jar("ae2.jar", ("META-INF/neoforge.mods.toml", Encoding.UTF8.GetBytes(Neo("ae2"))));
        string decay = f.Jar("decay.jar", ("META-INF/neoforge.mods.toml", Encoding.UTF8.GetBytes(Neo("accelerated_decay"))));
        var result = PackDependencyGraph.Check([ae2, decay], "1.21.1", new() { [ModLoaders.NeoForge] = "21.1.264" });
        Require(!result.HasErrors, "Independent NeoForge mods sharing runtime dependencies were called duplicates: " + string.Join(";", result.Errors));
        JObject fabric = Fabric("fabric_owner", "1");
        string multi = f.Jar("multi.jar", ("fabric.mod.json", Encoding.UTF8.GetBytes(fabric.ToString())),
            ("META-INF/neoforge.mods.toml", Encoding.UTF8.GetBytes(Neo("neo_owner"))));
        string fabricPeer = f.Fabric("fabric-peer.jar", Fabric("fabric_consumer", "1", new JObject { ["fabric_owner"] = "*" }));
        Clean(Check(multi, fabricPeer));
    }
    private static void NestedCandidates() {
        using Fixture f = new();
        byte[] nested = Archive(("fabric.mod.json", Encoding.UTF8.GetBytes(Fabric("embedded", "1").ToString())));
        JObject owner = Fabric("owner", "1", new JObject { ["embedded"] = "*" });
        owner["jars"] = new JArray(new JObject { ["file"] = "META-INF/jars/library.jar" });
        string jar = f.Jar("owner.jar", ("fabric.mod.json", Encoding.UTF8.GetBytes(owner.ToString())), ("META-INF/jars/library.jar", nested));
        Clean(Check(jar));
        string explicitLibrary = f.Fabric("explicit.jar", Fabric("embedded", "1"));
        var result = Check(jar, explicitLibrary);
        Require(!result.Errors.Any(text => text.Contains("重复模组 ID")), "An embedded selectable provider was flattened into an outer duplicate file.");
    }
    private static void ReadBoundaries() {
        using Fixture f = new();
        string active = f.Fabric("active.jar", Fabric("active_mod", "1"));
        string disabled = f.Fabric("disabled.jar.disabled", Fabric("active_mod", "1"));
        byte[] original = File.ReadAllBytes(active);
        string[] before = Directory.GetFiles(f.Root).OrderBy(path => path).ToArray();
        Clean(Check(active, disabled));
        Require(File.ReadAllBytes(active).SequenceEqual(original) && Directory.GetFiles(f.Root).OrderBy(path => path).SequenceEqual(before), "A graph check changed content or created a cache.");
        using (FileStream exclusive = new(active, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        using CancellationTokenSource cancel = new(); cancel.Cancel();
        try { PackDependencyGraph.Check([active], "1.21.1", FabricLoader(), cancel.Token); }
        catch (OperationCanceledException error) { Require(error.CancellationToken == cancel.Token, "Cancellation token changed."); return; }
        throw new Exception("A canceled graph check ran instead of propagating cancellation.");
    }
    private static string Neo(string id) => "modLoader='javafml'\nloaderVersion='[4,)'\nlicense='MIT'\n[[mods]]\nmodId='" + id + "'\nversion='1.0'\n" +
        "[[dependencies." + id + "]]\nmodId='minecraft'\ntype='required'\nversionRange='[1.21.1,1.22)'\nside='BOTH'\n" +
        "[[dependencies." + id + "]]\nmodId='neoforge'\ntype='required'\nversionRange='[21,)'\nside='BOTH'\n";
    private static byte[] Archive(params (string Name, byte[] Bytes)[] entries) {
        using MemoryStream data = new();
        using (ZipArchive zip = new(data, ZipArchiveMode.Create, true)) foreach (var entry in entries) {
            using Stream stream = zip.CreateEntry(entry.Name).Open(); stream.Write(entry.Bytes);
        }
        return data.ToArray();
    }
    private sealed class Fixture : IDisposable {
        internal readonly string Root = Path.Combine(Path.GetTempPath(), "lf-pack-graph-" + Guid.NewGuid().ToString("N"));
        internal Fixture() => Directory.CreateDirectory(Root);
        internal string Jar(string name, params (string Name, byte[] Bytes)[] entries) { string path = Path.Combine(Root, name); File.WriteAllBytes(path, Archive(entries)); return path; }
        internal string Fabric(string name, JObject data) => Jar(name, ("fabric.mod.json", Encoding.UTF8.GetBytes(data.ToString())));
        public void Dispose() => Directory.Delete(Root, true);
    }
}
