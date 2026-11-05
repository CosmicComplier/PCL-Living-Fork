using System.IO.Compression;
using System.Text;
using PCLCS;

internal static class PackUpdateMetadataTests {
    internal static IEnumerable<(string Name, Action Run)> Cases() {
        yield return ("pack metadata separates Forge/NeoForge self IDs from dependencies and TOML text", ForgeDeclarations);
        yield return ("pack TOML identities require array tables and reject malformed or unsupported syntax", TomlTableShapes);
        yield return ("pack metadata keeps Fabric provides and nested candidates separate from ownership", FabricDeclarations);
        yield return ("pack metadata selects loader-specific metadata in multi-loader archives", MultipleFormats);
        yield return ("pack metadata accepts Quilt string/object aliases without treating dependencies as IDs", QuiltDeclarations);
        yield return ("pack legacy metadata confirms annotations and treats descriptive mcmod.info as unknown", LegacyDeclarations);
        yield return ("pack metadata reports unsupported, damaged and missing identities conservatively", UnknownDeclarations);
        yield return ("pack metadata reports declared target-runtime incompatibility and unknown constraints", RuntimeCompatibility);
        yield return ("pack single-archive reading preserves disabled state, cancellation and file inventory", ReadBoundaries);
    }

    private static Dictionary<ModLoaders, string> Loader(ModLoaders kind = ModLoaders.NeoForge, string version = "21.1.264") => new() { [kind] = version };
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static string Toml(string id, string dependency = "minecraft") =>
        "modLoader=\"javafml\"\nloaderVersion=\"[4,)\"\nlicense=\"MIT\"\n[[mods]]\nmodId=\"" + id + "\"\nversion=\"1.0\"\n" +
        "[[dependencies." + id + "]]\nmodId=\"" + dependency + "\"\ntype=\"required\"\nmandatory=true\nversionRange=\"[1.21.1,1.22)\"\nordering=\"NONE\"\nside=\"BOTH\"\n";
    private static byte[] Utf8(string value) => Encoding.UTF8.GetBytes(value);

    private static void ForgeDeclarations() {
        using Fixture f = new();
        foreach (var (kind, entry, mc, loader) in new[] {
            (ModLoaders.NeoForge, "META-INF/neoforge.mods.toml", "1.21.1", "21.1.264"),
            (ModLoaders.Forge, "META-INF/mods.toml", "1.20.1", "47.2.0")
        }) {
            string content = Toml("appliedenergistics2") +
                "# modId='comment_is_not_a_mod'\n[[mods]]\nmodId = 'appliedflux' # own second module\nversion='2'\n" +
                "description = '''A multiline description\nmodId='text_is_not_a_mod'\n[[mods]]\n'''\n" +
                "[[dependencies.appliedflux]]\nmodId='" + (kind == ModLoaders.NeoForge ? "neoforge" : "forge") + "'\ntype='required'\nversionRange='[21,)'\n";
            PackModInspection result = PackUpdateModInspection.Read(f.Jar(kind + ".jar", (entry, Utf8(content))), mc, Loader(kind, loader));
            Check(result.Confirmed && result.OwnIds.SetEquals(new[] { "appliedenergistics2", "appliedflux" }), "TOML dependencies/comment/string leaked into own identities.");
            Check(result.ProvidedIds.Count == 0, "TOML dependency became a provided alias.");
        }
        var decay = PackUpdateModInspection.Read(f.Jar("accelerated-decay.jar", ("META-INF/neoforge.mods.toml", Utf8(Toml("accelerated_decay")))), "1.21.1", Loader());
        var ae2 = PackUpdateModInspection.Read(f.Jar("ae2.jar", ("META-INF/neoforge.mods.toml", Utf8(Toml("appliedenergistics2")))), "1.21.1", Loader());
        Check(decay.Confirmed && ae2.Confirmed && !decay.OwnIds.Overlaps(ae2.OwnIds), "Unrelated mods sharing Minecraft were treated as identical.");
    }

    private static void FabricDeclarations() {
        using Fixture f = new();
        byte[] nested = Archive(("fabric.mod.json", Utf8("""{"schemaVersion":1,"id":"embedded_library","version":"1"}""")));
        string jar = f.Jar("fabric.jar", ("fabric.mod.json", Utf8("""{"schemaVersion":1,"id":"owner","version":"1","provides":["alias"],"depends":{"minecraft":">=1.21","external":"*"},"jars":[{"file":"META-INF/jars/library.jar"}]}""")), ("META-INF/jars/library.jar", nested));
        PackModInspection result = PackUpdateModInspection.Read(jar, "1.21.1", Loader(ModLoaders.Fabric, "0.16.14"));
        Check(result.Confirmed && result.OwnIds.SetEquals(new[] { "owner" }), "Fabric dependencies/aliases/nested jar mixed into root identity.");
        Check(result.ProvidedIds.SetEquals(new[] { "alias" }) && result.NestedIds.SetEquals(new[] { "embedded_library" }), "Fabric aliases and nested candidates were lost.");
    }

    private static void TomlTableShapes() {
        using Fixture f = new();
        foreach (string metadata in new[] {
            "[mods]\nmodId='fake'\nversion='1'\n",                  // A table is not a mods array.
            "[[mods]\nmodId='fake'\nversion='1'\n",                 // Unequal opening/closing delimiters.
            "[mods]]\nmodId='fake'\nversion='1'\n",
            "[[mods]]\nmodId='real'\nversion='1'\n[mods]\nmodId='fake'\nversion='1'\n",
            "[[mods]]\nmodId='real'\nversion='1'\n[dependencies.real]\nmodId='minecraft'\nmandatory=true\n",
            "[[\"mods\"]]\nmodId='quoted_table'\nversion='1'\n", // Legal TOML outside this bounded subset: unknown.
            "[[mods]]\n\"modId\"='quoted_key'\nversion='1'\n",
            "[[mods]]\nmodId='real'\nversion='1'\ndescription='unterminated\ntext'\n",
            "[[mods]]\nmodId='real'\nmodId='second'\nversion='1'\n"
        }) {
            var result = PackUpdateModInspection.Read(f.Jar(Guid.NewGuid() + ".jar", ("META-INF/neoforge.mods.toml", Utf8(metadata))), "1.21.1", Loader());
            Check(!result.Confirmed && result.Warnings.Count > 0, "A scalar/malformed/unsupported TOML table established confirmed mod identity: " + metadata);
        }
        string valid = Toml("real") + "[[mods]] # array-table comment\nmodId='second'\nversion='1'\n" +
            "description=\"A # character and [[mods]] are text\" # actual comment\n";
        var supported = PackUpdateModInspection.Read(f.Jar("supported.jar", ("META-INF/neoforge.mods.toml", Utf8(valid))), "1.21.1", Loader());
        Check(supported.Confirmed && supported.OwnIds.SetEquals(new[] { "real", "second" }), "Supported valid TOML/comment/string syntax was weakened into a non-identity.");
    }

    private static void MultipleFormats() {
        using Fixture f = new();
        string jar = f.Jar("multi.jar", ("fabric.mod.json", Utf8("""{"schemaVersion":1,"id":"fabric_owner","version":"1"}""")),
            ("META-INF/neoforge.mods.toml", Utf8(Toml("neo_owner"))));
        var fabric = PackUpdateModInspection.Read(jar, "1.21.1", Loader(ModLoaders.Fabric, "0.16.14"));
        var neo = PackUpdateModInspection.Read(jar, "1.21.1", Loader());
        Check(fabric.Confirmed && fabric.OwnIds.SetEquals(new[] { "fabric_owner" }), "Fabric did not select its own metadata.");
        Check(neo.Confirmed && neo.OwnIds.SetEquals(new[] { "neo_owner" }), "NeoForge did not select its own metadata.");
    }

    private static void QuiltDeclarations() {
        using Fixture f = new();
        string jar = f.Jar("quilt.jar", ("quilt.mod.json", Utf8("""{"schema_version":1,"quilt_loader":{"group":"fixture","id":"quilt_owner","version":"1","provides":["short_alias",{"id":"object_alias","version":"2"}],"depends":[{"id":"minecraft","versions":">=1.21"}],"metadata":{"name":"Quilt fixture"}}}""")));
        var result = PackUpdateModInspection.ReadForLoaderContext(jar, "1.21.1", new Dictionary<string, string?> { ["quilt_loader"] = "0.29.2" });
        Check(result.Confirmed && result.OwnIds.SetEquals(new[] { "quilt_owner" }), "Quilt dependency/alias mixed into own ID.");
        Check(result.ProvidedIds.Contains("short_alias") && result.ProvidedIds.Contains("object_alias") && !result.ProvidedIds.Contains("minecraft"), "Quilt string/object provides were not kept separate.");
    }

    private static void LegacyDeclarations() {
        using Fixture f = new();
        string jar = f.Jar("legacy.jar", ("fixture/Mod.class", LegacyClass(("modid", "actual_owner"), ("name", "Actual"), ("version", "1"))),
            ("mcmod.info", Utf8("""[{"modid":"stale_template","name":"Template","version":"1"}]""")));
        var result = PackUpdateModInspection.Read(jar, "1.12.2", Loader(ModLoaders.Forge, "14.23.5.2855"));
        Check(result.Confirmed && result.OwnIds.SetEquals(new[] { "actual_owner" }), "Legacy loading identity was taken from descriptive mcmod.info.");
        foreach (string info in new[] { """[{"modid":"description_only","version":"1"}]""", """{"modList":[{"modid":"description_only","version":"1"}]}""" }) {
            var description = PackUpdateModInspection.Read(f.Jar(Guid.NewGuid() + ".jar", ("mcmod.info", Utf8(info))), "1.12.2", Loader(ModLoaders.Forge, "14.23.5.2855"));
            Check(!description.Confirmed && description.OwnIds.Count == 0, "mcmod.info alone was promoted to confirmed runtime identity.");
        }
    }

    private static void UnknownDeclarations() {
        using Fixture f = new();
        foreach (string jar in new[] {
            f.Jar("empty.jar", ("notes.txt", Utf8("no mod metadata"))),
            f.Jar("unsupported.jar", ("fabric.mod.json", Utf8("""{"schemaVersion":42,"id":"uncertain","version":"1"}"""))),
            f.Jar("damaged.jar", ("fabric.mod.json", Utf8("{broken"))),
            f.Jar("unsupported-toml.jar", ("META-INF/neoforge.mods.toml", Utf8("[[\"mods\"]]\nmodId='uncertain'\nversion='1'\n"))),
            Path.Combine(f.Root, "missing.jar")
        }) {
            var result = PackUpdateModInspection.Read(jar, "1.21.1", Loader());
            Check(!result.Confirmed && result.Warnings.Count > 0, "Unsupported/missing identity was trusted.");
        }
    }

    private static void RuntimeCompatibility() {
        using Fixture f = new();
        string jar = f.Jar("old-lithium.jar", ("fabric.mod.json", Utf8("""{"schemaVersion":1,"id":"lithium","version":"0.21.4","depends":{"minecraft":"=1.21.11","fabricloader":">=0.16.0"}}""")));
        var mismatch = PackUpdateModInspection.Read(jar, "26.1.2", Loader(ModLoaders.Fabric, "0.18.0"));
        Check(mismatch.CompatibilityWarnings.Any(text => text.Contains("minecraft", StringComparison.OrdinalIgnoreCase) && text.Contains("1.21.11", StringComparison.Ordinal)), "Known Minecraft mismatch was hidden.");
        var wrongLoader = PackUpdateModInspection.Read(jar, "1.21.11", Loader());
        Check(!wrongLoader.Confirmed && wrongLoader.CompatibilityWarnings.Any(text => text.Contains("Fabric", StringComparison.Ordinal)), "Wrong loader metadata was accepted silently.");
        var unknown = PackUpdateModInspection.Read(f.Jar("no-constraints.jar", ("fabric.mod.json", Utf8("""{"schemaVersion":1,"id":"no_constraints","version":"1"}"""))), "1.21.1", Loader(ModLoaders.Fabric, "0.16.14"));
        Check(unknown.CompatibilityWarnings.Any(text => text.Contains("未能完整确认", StringComparison.Ordinal)), "Missing constraints were advertised as compatible.");
    }

    private static void ReadBoundaries() {
        using Fixture f = new();
        string jar = f.Jar("lithium.jar.disabled", ("fabric.mod.json", Utf8("""{"schemaVersion":1,"id":"lithium","version":"1"}""")));
        string[] before = Directory.GetFileSystemEntries(f.Root, "*", SearchOption.AllDirectories);
        var result = PackUpdateModInspection.Read(jar, "1.21.1", Loader(ModLoaders.Fabric, "0.16.14"));
        Check(result.Confirmed && !result.Enabled && File.Exists(jar), "Metadata reading lost disabled state.");
        Check(before.SequenceEqual(Directory.GetFileSystemEntries(f.Root, "*", SearchOption.AllDirectories)), "Single-archive inspection created cache/index files.");
        using FileStream reopened = new(jar, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        using CancellationTokenSource cancel = new(); cancel.Cancel();
        bool observed = false;
        try { PackUpdateModInspection.Read(jar, "1.21.1", Loader(), cancel.Token); } catch (OperationCanceledException) { observed = true; }
        Check(observed, "Cancellation was swallowed.");
    }

    // Same class-level RuntimeVisibleAnnotations bytecode shape used by LF's existing
    // ModDependencyIndexCompatibilityTests, not a text imitation of an @Mod annotation.
    private static byte[] LegacyClass(params (string Name, string Value)[] values) {
        List<string> pool = ["Lnet/minecraftforge/fml/common/Mod;", "RuntimeVisibleAnnotations", "fixture/Mod", "java/lang/Object"];
        foreach (var value in values) { pool.Add(value.Name); pool.Add(value.Value); }
        using MemoryStream stream = new(); using BinaryWriter writer = new(stream);
        void U2(int value) { writer.Write((byte)((value >> 8) & 255)); writer.Write((byte)(value & 255)); }
        void U4(long value) { U2((int)((value >> 16) & 65535)); U2((int)(value & 65535)); }
        U4(0xcafebabe); U2(0); U2(52); U2(pool.Count + 3);
        foreach (string text in pool) { byte[] bytes = Utf8(text); writer.Write((byte)1); U2(bytes.Length); writer.Write(bytes); }
        writer.Write((byte)7); U2(3); writer.Write((byte)7); U2(4); U2(33); U2(pool.Count + 1); U2(pool.Count + 2); U2(0); U2(0); U2(0); U2(1); U2(2); U4(6 + 5 * values.Length); U2(1); U2(1); U2(values.Length);
        for (int i = 0; i < values.Length; i++) { U2(5 + 2 * i); writer.Write((byte)'s'); U2(6 + 2 * i); }
        writer.Flush(); return stream.ToArray();
    }

    private static byte[] Archive(params (string Name, byte[] Bytes)[] entries) {
        using MemoryStream bytes = new();
        using (ZipArchive zip = new(bytes, ZipArchiveMode.Create, true)) {
            foreach (var entry in entries) { using Stream output = zip.CreateEntry(entry.Name).Open(); output.Write(entry.Bytes); }
        }
        return bytes.ToArray();
    }
    private sealed class Fixture : IDisposable {
        internal readonly string Root = Path.Combine(Path.GetTempPath(), "lf-pack-metadata-" + Guid.NewGuid().ToString("N"));
        internal Fixture() { Directory.CreateDirectory(Root); }
        internal string Jar(string name, params (string Name, byte[] Bytes)[] entries) { string file = Path.Combine(Root, name); File.WriteAllBytes(file, Archive(entries)); return file; }
        public void Dispose() { Directory.Delete(Root, true); }
    }
}
