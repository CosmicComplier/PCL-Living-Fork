// Independently authored fixtures. Host test suite retains its existing license.
using System.Collections.Immutable;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;
using PCLLF.MMCC.Core;

internal static class MmcProfileTests {
    internal static IEnumerable<(string Name, Action Run)> Cases() {
        yield return ("MMC source boundary reads optional cfg and ordered metadata requests", OptionalCfg);
        yield return ("MMC legacy cfg includes first line BOM equals escaped hash and General marker", LegacyCfg);
        yield return ("MMC Qt scalar cfg preserves quotes continuations and escaped Unicode", QtCfg);
        yield return ("MMC cfg command aliases and inactive dangerous values are explicit", CfgCapabilities);
        yield return ("MMC cfg malformed booleans duplicate keys variants and unknown behavior fail closed", CfgInvalid);
        yield return ("MMC archive root discovery does not depend on directory entries", Roots);
        yield return ("MMC ambiguous roots and missing source metadata fail closed", AmbiguousRoots);
        yield return ("MMC Windows traversal device names and file collisions are rejected", UnsafePaths);
        yield return ("MMC directory spelling aliases preserve both nonconflicting leaves", DirectoryAliases);
        yield return ("MMC implicit parent and explicit directory file role collisions are rejected", FileRoles);
        yield return ("MMC incoming LF ownership records cannot claim managed ownership", ReservedOwnership);
        yield return ("MMC strict JSON rejects duplicate keys including nested escaped spellings", DuplicateJson);
        yield return ("MMC malformed manifest shapes and unknown format return diagnostics", ManifestShapes);
        yield return ("MMC local component version and non-disableable state preserve original declarations", LocalComponents);
        yield return ("MMC patch uid fileId and duplicate identities are checked", PatchIdentity);
        yield return ("MMC dependency requests retain exact version and author order", Dependencies);
        yield return ("MMC conflicting disabled cyclic and exact-version dependency graphs fail", InvalidGraphs);
        yield return ("MMC enabled unknown launch capabilities fail without silently discarding fields", Capabilities);
        yield return ("MMC library roots remain distinct and JVM array tokens remain intact", LibrariesAndTokens);
        yield return ("MMC Java candidate declarations retain discrete positive integers", JavaCandidates);
        yield return ("MMC metadata limits apply per item cumulatively and to input structure", MetadataBudgets);
        yield return ("MMC archive sizes components edges and JSON depth are bounded", StructureBudgets);
        yield return ("MMC cumulative raw and logical prefix path budgets reject product-size allocation", PathBudgets);
        yield return ("MMC results survive source mutation and detached JSON document disposal", Immutability);
    }

    private const string Manifest = "{\"formatVersion\":1,\"components\":[{\"uid\":\"net.minecraft\",\"version\":\"1.7.10\",\"important\":true}]}";
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private static MmcPackageInput Input(string manifest = Manifest, string? cfg = null,
        Dictionary<string, string>? patches = null, string root = "", params MmcArchiveEntry[] extra) {
        Dictionary<string, string> content = new() { [root + "mmc-pack.json"] = manifest };
        if (cfg is not null) content[root + "instance.cfg"] = cfg;
        if (patches is not null) foreach ((string file, string text) in patches) content[root + "patches/" + file] = text;
        return new(content.Select(x => new MmcArchiveEntry(x.Key, System.Text.Encoding.UTF8.GetByteCount(x.Value))).Concat(extra).ToArray(), content);
    }
    private static string Pack(params object[] components) => JsonSerializer.Serialize(new { formatVersion = 1, components });
    private static string Patch(string uid, string? version = "1", object[]? requires = null, object[]? conflicts = null) =>
        JsonSerializer.Serialize(new { formatVersion = 1, uid, version, requires = requires ?? [], conflicts = conflicts ?? [] });
    private static MmcPackage Good(MmcPackageInput input, MmcReadLimits? limits = null) {
        MmcParseResult result = MmcPackageReader.Parse(input, limits);
        Require(!result.HasErrors && result.Package is not null, "Unexpected rejection: " + string.Join(" | ", result.Diagnostics.Select(d => d.Code + " " + d.Message)));
        return result.Package!;
    }
    private static void Error(MmcPackageInput input, string code, MmcReadLimits? limits = null) {
        MmcParseResult result = MmcPackageReader.Parse(input, limits);
        Require(result.HasErrors && result.Diagnostics.Any(d => d.Code == code && d.Severity == MmcDiagnosticSeverity.Error), "Missing " + code + ": " + string.Join(" | ", result.Diagnostics.Select(d => d.Code)));
    }
    private static void OptionalCfg() {
        MmcPackage result = Good(Input());
        Require(result.Configuration.Format == MmcConfigurationFormat.Absent && result.Configuration.Name is null, "Absent cfg was fabricated.");
        Require(result.MissingMetadata is [{ Uid: "net.minecraft", Version: "1.7.10" }], "Explicit metadata request lost.");
        Require(result.ManifestSha256.Length == 64, "Source hash missing.");
    }
    private static void LegacyCfg() {
        MmcPackage result = Good(Input(cfg: "\uFEFFname=First=Name\r\n[General]\r\nnotes=one\\nline\\t\\#literal # comment\r\nOverrideGameTime=false\nOverrideNativeWorkarounds=false"));
        Require(result.Configuration.Name == "First=Name" && result.Configuration.Values["notes"] == "one\nline\t#literal", "Legacy first line/equal/escape changed.");
        Require(Good(Input(cfg: "name=\"a,b\"" )).Configuration.Name == "a,b", "Legacy conditional quote removal changed.");
        Require(Good(Input(cfg: "name=\"plain\"" )).Configuration.Name == "\"plain\"", "Legacy plain quotes were rewritten.");
    }
    private static void QtCfg() {
        string cfg = "[General]\nConfigVersion\t=1.3\nname=\"Hello; world,a=b\"\nnotes=\\x4e2d\\x6587\\nline\nJvmArgs=\"-Dpath=C:\\\\game files\"\\\n -Dnext=true\niconKey=@@fixture";
        var values = Good(Input(cfg: cfg)).Configuration.Values;
        Require(values["name"] == "Hello; world,a=b" && values["notes"] == "中文\nline", "Qt scalar escapes changed.");
        Require(values["JvmArgs"] == "-Dpath=C:\\game files-Dnext=true", "Continuation/quoted spaces changed.");
        Require(values["iconKey"] == "@fixture", "Escaped at sign changed.");
    }
    private static void CfgCapabilities() {
        Good(Input(cfg: "OverrideCommands=false\nPreLaunchCommand=ignored\nOverrideEnv=false\nEnv=@Variant(ignored)"));
        Error(Input(cfg: "OverrideCommands=true\nPreLaunchCommand=active"), "MMC_UNSUPPORTED_REQUIRED");
        Error(Input(cfg: "OverrideLaunchCmd=true\nWrapperCommand=active"), "MMC_UNSUPPORTED_REQUIRED");
        var result = MmcPackageReader.Parse(Input(cfg: "OverrideCommands=false\nOverrideLaunchCmd=true\nPostExitCommand=inactive"));
        Require(!result.HasErrors && result.Diagnostics.Any(d => d.Code == "MMC_CFG_ALIAS"), "New alias precedence changed.");
        Error(Input(cfg: "OverrideEnv=true\nEnv=active"), "MMC_UNSUPPORTED_REQUIRED");
        Error(Input(cfg: "OverrideNativeWorkarounds=true"), "MMC_UNSUPPORTED_REQUIRED");
    }
    private static void CfgInvalid() {
        Error(Input(cfg: "OverrideMemory=garbage"), "MMC_CFG_BOOLEAN");
        Error(Input(cfg: "name=a\nname=b"), "MMC_CFG");
        Error(Input(cfg: "ConfigVersion=1.3\nname=\"unterminated"), "MMC_CFG");
        Error(Input(cfg: "ConfigVersion=1.3\nname=a,b"), "MMC_CFG_LIST");
        Error(Input(cfg: "ConfigVersion=1.3\nJvmArgs=@Variant(data)"), "MMC_CFG_VARIANT");
        Error(Input(cfg: "ConfigVersion=9"), "MMC_UNSUPPORTED_FORMAT");
        Error(Input(cfg: "FutureLaunchHook=run"), "MMC_UNSUPPORTED_REQUIRED");
        Error(Input(cfg: "MaxMemAlloc=-10"), "MMC_CFG");
    }
    private static void Roots() {
        Require(Good(Input(root: "nested/", extra: [new("nested/.minecraft/config/file")])).GameRootLeaf == ".minecraft", "Implicit game directory lost.");
        Require(Good(Input(root: "nested/", extra: [new("nested/.minecraft/config/file"), new("nested/minecraft/config/file")])).GameRootLeaf == "minecraft", "Dual-tree precedence changed.");
        Require(Good(Input(root: "nested/")).RelativeRoot == "nested/", "Relative instance root changed.");
    }
    private static void AmbiguousRoots() {
        Error(Input(extra: [new("second/mmc-pack.json")]), "MMC_ROOT");
        Error(new([], new Dictionary<string, string>()), "MMC_ROOT");
        Error(new([new("mmc-pack.json")], new Dictionary<string, string>()), "MMC_METADATA_MISSING");
        Error(new([new("mmc-pack.json")], new Dictionary<string, string> { ["orphan.json"] = "{}" }), "MMC_METADATA_SOURCE");
    }
    private static void UnsafePaths() {
        foreach (string path in new[] { "../evil", "a/../../evil", "/root", "C:/file", "a./file", "AUX.txt", "x/COM1/file", "x//file", "a:stream", "bad?name", "COM¹.jar", "LPT².txt", "CONIN$.bin", "CONOUT$" })
            Error(Input(extra: [new(path)]), "MMC_PATH");
        Error(Input(extra: [new("mods/A.jar"), new("mods/a.jar")]), "MMC_PATH_COLLISION");
    }
    private static void DirectoryAliases() {
        MmcPackageInput input = Input(extra: [new("config/Foo/", 0, true), new("config/foo/", 0, true), new("config/Foo/a"), new("config/foo/b")]);
        MmcParseResult result = MmcPackageReader.Parse(input);
        Require(!result.HasErrors && result.Diagnostics.Any(d => d.Code == "MMC_DIRECTORY_ALIAS") && result.Package!.ArchiveEntries.Length == input.Entries.Count, "Pure directory aliases were lost or rejected.");
        Good(Input(extra: [new("Config/Thing/a"), new("config/thing/b")]));
        Error(Input(extra: [new("config/Foo/a"), new("config/foo/A")]), "MMC_PATH_COLLISION");
    }
    private static void FileRoles() {
        Error(Input(extra: [new("mods"), new("mods/file.jar")]), "MMC_PATH_COLLISION");
        Error(Input(extra: [new("MODS/file.jar"), new("mods")]), "MMC_PATH_COLLISION");
        Error(Input(extra: [new("data", 0, true), new("DATA")]), "MMC_PATH_COLLISION");
        Error(Input(extra: [new("minecraft")]), "MMC_DIRECTORY_ROLE");
        Error(Input(extra: [new("libraries")]), "MMC_DIRECTORY_ROLE");
    }
    private static void ReservedOwnership() {
        Error(Input(extra: [new("PCL/InstanceDefinition.json")]), "MMC_RESERVED_PATH");
        Error(Input(extra: [new("PCL/MmcTransactions/fake/log")]), "MMC_RESERVED_PATH");
    }
    private static void DuplicateJson() {
        Error(Input("{\"formatVersion\":1,\"formatVersion\":1,\"components\":[]}"), "MMC_DUPLICATE_JSON_KEY");
        Error(Input(patches: new() { ["net.minecraft.json"] = "{\"uid\":\"net.minecraft\",\"libraries\":[{\"name\":\"a:b:1\",\"downloads\":{\"a\":1,\"\\u0061\":2}}]}" }), "MMC_DUPLICATE_JSON_KEY");
    }
    private static void ManifestShapes() {
        foreach (string manifest in new[] { "{\"formatVersion\":\"1\",\"components\":[]}", "{\"formatVersion\":2,\"components\":[]}" }) Error(Input(manifest), "MMC_UNSUPPORTED_FORMAT");
        foreach (string manifest in new[] { "[]", "{\"formatVersion\":1,\"components\":{}}", "{\"formatVersion\":1,\"components\":[null]}" }) Error(Input(manifest), "MMC_STRUCTURE");
        Error(Input(Pack(new { uid = "a", version = "1" }, new { uid = "a", version = "2" })), "MMC_DUPLICATE_UID");
        Error(Input(Pack(new { uid = "../bad", version = "1" })), "MMC_STRUCTURE");
    }
    private static void LocalComponents() {
        var input = Input(Pack(new { uid = "a", version = "request", cachedVersion = "hint", disabled = true, important = true },
            new { uid = "b", disabled = true, dependencyOnly = true }), patches: new() { ["a.json"] = Patch("a", "local"), ["b.json"] = Patch("b", "effective") });
        var result = Good(input);
        Require(result.Components.Select(c => c.Uid).SequenceEqual(["a", "b"]), "Author order changed.");
        Require(result.Components[0] is { RequestedVersion: "request", CachedVersion: "hint", Disabled: true, EffectiveEnabled: true, EffectiveVersion: "local" }, "Declaration or local override lost.");
        Require(result.Components[1].EffectiveEnabled && result.MissingMetadata.IsEmpty, "dependencyOnly incorrectly disabled or local overwritten by remote.");
    }
    private static void PatchIdentity() {
        Good(Input(patches: new() { ["net.minecraft.json"] = "{\"fileId\":\"net.minecraft\",\"version\":\"1.7.10\"}" }));
        Error(Input(patches: new() { ["net.minecraft.json"] = "{\"uid\":\"net.minecraft\",\"fileId\":\"other\"}" }), "MMC_STRUCTURE");
        Error(Input(patches: new() { ["net.minecraft.json"] = Patch("different") }), "MMC_PATCH_UID");
    }
    private static void Dependencies() {
        var result = Good(Input(Pack(new { uid = "consumer", version = "1" }, new { uid = "a", version = "1" }), patches: new() {
            ["consumer.json"] = Patch("consumer", requires: [new { uid = "missing", equals = "2", suggests = "3" }])
        }));
        Require(result.Components[0].Uid == "consumer" && result.MissingMetadata.Any(r => r.Uid == "missing" && r.Version == "2"), "Exact dependency or original component order lost.");
        Error(Input(Pack(new { uid = "unknown" })), "MMC_VERSION_REQUIRED");
    }
    private static void InvalidGraphs() {
        string pack = Pack(new { uid = "a", version = "1" }, new { uid = "b", version = "1" });
        Error(Input(pack, patches: new() { ["a.json"] = Patch("a", requires: [new { uid = "b", equals = "2" }]), ["b.json"] = Patch("b") }), "MMC_EXACT_VERSION");
        Error(Input(Pack(new { uid = "a", version = "1" }, new { uid = "b", version = "1", disabled = true }), patches: new() {
            ["a.json"] = Patch("a", requires: [new { uid = "b" }]), ["b.json"] = Patch("b") }), "MMC_DISABLED_DEPENDENCY");
        Error(Input(pack, patches: new() { ["a.json"] = Patch("a", requires: [new { uid = "b" }]), ["b.json"] = Patch("b", requires: [new { uid = "a" }]) }), "MMC_DEPENDENCY_CYCLE");
        Error(Input(pack, patches: new() { ["a.json"] = Patch("a", conflicts: [new { uid = "b" }]), ["b.json"] = Patch("b") }), "MMC_COMPONENT_CONFLICT");
    }
    private static void Capabilities() {
        Error(Input(patches: new() { ["net.minecraft.json"] = "{\"uid\":\"net.minecraft\",\"futureLaunch\":true}" }), "MMC_UNSUPPORTED_REQUIRED");
        Error(Input(patches: new() { ["net.minecraft.json"] = "{\"uid\":\"net.minecraft\",\"+traits\":[\"legacyFML\"]}" }), "MMC_UNSUPPORTED_REQUIRED");
        Error(Input(patches: new() { ["net.minecraft.json"] = "{\"uid\":\"net.minecraft\",\"libraries\":[{\"name\":\"a:b:1\",\"rules\":[{\"action\":\"allow\",\"futureCondition\":true}]}]}" }), "MMC_UNSUPPORTED_REQUIRED");
        Error(Input(patches: new() { ["net.minecraft.json"] = "{\"uid\":\"net.minecraft\",\"libraries\":[{\"name\":\"a:b:1\",\"downloads\":{\"artifact\":{\"size\":-1}}}]}" }), "MMC_STRUCTURE");
        Good(Input(Pack(new { uid = "disabled", version = "1", disabled = true }), patches: new() { ["disabled.json"] = "{\"uid\":\"disabled\",\"futureLaunch\":true}" }));
        Good(Input(patches: new() { ["net.minecraft.json"] = "{\"uid\":\"net.minecraft\",\"name\":\"fixture\",\"description\":\"descriptive\",\"volatile\":true}" }));
    }
    private static void LibrariesAndTokens() {
        string patch = "{\"uid\":\"net.minecraft\",\"mainClass\":\"fixture.Main\",\"+jvmArgs\":[\"-Dpath=中文 space\",\"\",\"-Dsame=1\",\"-Dsame=1\"],\"libraries\":[{\"name\":\"fixture:lib:1\",\"MMC-hint\":\"local\"}]}";
        var result = Good(Input(patches: new() { ["net.minecraft.json"] = patch }, extra: [new("libraries/lib-1.jar", 2), new("minecraft/libraries/lib-1.jar", 3)]));
        JsonElement args = result.Components[0].LocalPatch!.Fields["+jvmArgs"];
        Require(args.GetArrayLength() == 4 && args[0].GetString() == "-Dpath=中文 space" && args[1].GetString() == "", "JVM array values were split/deduplicated.");
    }
    private static void JavaCandidates() {
        var result = Good(Input(patches: new() { ["net.minecraft.json"] = "{\"uid\":\"net.minecraft\",\"compatibleJavaMajors\":[17,21,25,26],\"compatibleJavaName\":\"fixture-runtime\"}" }));
        Require(result.Components[0].LocalPatch!.Fields["compatibleJavaMajors"].EnumerateArray().Select(e => e.GetInt32()).SequenceEqual([17, 21, 25, 26]), "Discrete candidate source changed.");
        foreach (string value in new[] { "[0]", "[\"17\"]", "[17.5]", "{}" })
            Error(Input(patches: new() { ["net.minecraft.json"] = "{\"uid\":\"net.minecraft\",\"compatibleJavaMajors\":" + value + "}" }), "MMC_STRUCTURE");
    }
    private static void MetadataBudgets() {
        Error(Input(cfg: "name=中文"), "MMC_METADATA_BUDGET", new() { MaxMetadataBytes = 10 });
        Error(Input(cfg: "name=fixture"), "MMC_METADATA_BUDGET", new() { MaxTotalMetadataBytes = Manifest.Length + 5 });
        Error(Input(cfg: "name=\uD800"), "MMC_METADATA_ENCODING");
        Error(Input(extra: [new("file")]), "MMC_ARCHIVE_BUDGET", new() { MaxArchiveEntries = 1 });
    }
    private static void StructureBudgets() {
        Error(Input(extra: [new("huge", 500)]), "MMC_ARCHIVE_BUDGET", new() { MaxEntryBytes = 300 });
        Error(Input(extra: [new("a", 100), new("b", 100)]), "MMC_ARCHIVE_BUDGET", new() { MaxArchiveBytes = Manifest.Length + 150 });
        Error(Input(Pack(new { uid = "a", version = "1" }, new { uid = "b", version = "1" })), "MMC_COMPONENT_BUDGET", new() { MaxComponents = 1 });
        Error(Input(patches: new() { ["net.minecraft.json"] = "{\"uid\":\"net.minecraft\",\"description\":{\"nested\":{\"tooDeep\":true}}}" }), "MMC_JSON", new() { MaxDepth = 2 });
        Error(Input(patches: new() { ["net.minecraft.json"] = Patch("net.minecraft", requires: [new { uid = "a", equals = "1" }, new { uid = "b", equals = "1" }]) }), "MMC_DEPENDENCY_BUDGET", new() { MaxDependencyEdges = 1 });
    }
    private static void PathBudgets() {
        Error(Input(), "MMC_PATH_BUDGET", new() { MaxTotalPathCharacters = 20 }); // metadata path is counted too
        Error(Input(extra: [new("a/b/c/file")]), "MMC_PATH_BUDGET", new() { MaxLogicalPathCharacters = 25 });
        // Repeated prefix construction is charged before allocating substrings,
        // even when its logical directory node was already present.
        Error(Input(extra: [new("repeat/deep/a"), new("repeat/deep/b")]), "MMC_PATH_BUDGET", new() { MaxLogicalPathCharacters = 60 });
    }
    private static void Immutability() {
        MmcPackageInput input = Input(patches: new() { ["net.minecraft.json"] = "{\"uid\":\"net.minecraft\",\"+jvmArgs\":[\"first\"]}" });
        var result = Good(input);
        ((Dictionary<string, string>)input.Metadata)["patches/net.minecraft.json"] = "corrupt";
        ((MmcArchiveEntry[])input.Entries)[0] = new("evil");
        Require(result.Components[0].LocalPatch!.Fields["+jvmArgs"][0].GetString() == "first" && result.ArchiveEntries[0].Path == "mmc-pack.json", "Result retains mutable source alias.");
        var replay = Good(Input());
        Require(replay.ManifestSha256 == Good(Input()).ManifestSha256, "Same source produced different digest.");
        Require(typeof(MmcPackageReader).Assembly.GetReferencedAssemblies().All(a => a.Name is not "PCLCS" and not "Main" && !a.Name!.StartsWith("Presentation", StringComparison.Ordinal)), "Core acquired a host reference.");
    }
}
