using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using JsonSerializer = System.Text.Json.JsonSerializer;
using PCLLF.MMCC.Core;

internal static class MmcLaunchAuthorityTests {
    internal static IEnumerable<(string Name, Action Run)> Cases() {
        yield return ("MMC binder publishes a new immutable definition and retains exact input digests", InitialBinding);
        yield return ("MMC binder requires all active facts source objects and declared checksum evidence", BindingRejections);
        yield return ("MMC binder prevents existing adopted keys from changing bytes during repair or adoption", ImmutableAdoption);
        yield return ("MMC binder requires explicit new generations for new variants", Generations);
        yield return ("MMC binder binds native output batches to locked parents exclusions and context", NativeBinding);
        yield return ("MMC binder validates dynamic asset membership against raw adopted index bytes", AssetBinding);
        yield return ("MMC binder rejects arbitrary extra facts unowned targets and prior-lock omissions", Ownership);
        yield return ("MMC aggregate launch proof accepts a sourced local final entry while retaining target-specific heuristics", AggregateAuthority);
        yield return ("MMC aggregate launch proof remains conservative for missing class locks candidates or bound agents", ConservativeAuthority);
    }
    private sealed record Fixture(MmcCompiledProfile Plan, MmcInstanceDefinition Definition,
        ImmutableArray<MmcMaterializedArtifactFact> Facts, MmcResolvedProfile Sources, MmcAssetIndexInput? AssetIndex = null);
    private static string Hash(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static string View => new('9', 64);
    private static Fixture Create(bool native = false, string? assetJson = null, bool mavenDuplicates = false, bool declaredJava = false, bool lateEntrypoint = false) {
        Dictionary<string, object?> patch = new() {
            ["uid"] = "net.minecraft", ["version"] = "1", ["mainClass"] = "fixture.Main",
            ["mainJar"] = new Dictionary<string, object?> { ["name"] = "fixture:main:1:client", ["MMC-hint"] = "local" },
            ["libraries"] = new object[] { new Dictionary<string, object?> { ["name"] = "fixture:library:1", ["MMC-hint"] = "local" } }
        };
        if (native) patch["+libraries"] = new[] { new { name = "fixture:native:1", natives = new { windows = "native-64" }, extract = new { exclude = new[] { "META-INF/" } } } };
        if (assetJson is not null) patch["assetIndex"] = new { id = "fixture", url = "https://example.invalid/fixture.json" };
        if (mavenDuplicates) patch["mavenFiles"] = new[] { new { name = "fixture:tool:1" }, new { name = "fixture:tool:1" } };
        if (declaredJava) patch["compatibleJavaMajors"] = new[] { 17, 21 };
        string manifest = lateEntrypoint ? "{\"formatVersion\":1,\"components\":[{\"uid\":\"net.minecraft\",\"version\":\"1\"},{\"uid\":\"bootstrap\",\"version\":\"1\"}]}" : "{\"formatVersion\":1,\"components\":[{\"uid\":\"net.minecraft\",\"version\":\"1\"}]}";
        string component = JsonSerializer.Serialize(patch);
        Dictionary<string, string> content = new() { ["mmc-pack.json"] = manifest, ["patches/net.minecraft.json"] = component };
        if (lateEntrypoint) content["patches/bootstrap.json"] = "{\"uid\":\"bootstrap\",\"version\":\"1\",\"mainClass\":\"fixture.Boot\",\"+jvmArgs\":[\"-Djava.system.class.loader=fixture.Loader\"],\"+tweakers\":[\"fixture.Tweaker\"]}";
        var parsed = MmcPackageReader.Parse(new(content.Select(c => new MmcArchiveEntry(c.Key)).ToArray(), content));
        var resolved = MmcComponentResolver.Resolve(parsed, []);
        Require(resolved.IsComplete, "Fixture resolution failed.");
        var compiled = MmcProfileCompiler.Compile(resolved.Profile!, new("windows", "amd64", 17));
        Require(!compiled.HasErrors && compiled.Profile is not null, "Fixture compilation failed.");
        var plan = compiled.Profile!;
        var draft = MmcDefinitionTests.Draft(plan.SourceFingerprint);
        string prefix = "PCL/MmcSource/" + draft.SourceBundle.BundleSha256 + "/";
        var metadataRecords = ImmutableArray.CreateBuilder<MmcSourceMetadataRecord>();
        metadataRecords.Add(new("mmc-pack.json", Hash(manifest), prefix + "mmc-pack.json", MmcSourceRecordKind.Manifest));
        foreach (var source in resolved.Profile!.Components) metadataRecords.Add(new("patches/" + source.Uid + ".json", source.Patch.ContentSha256, prefix + "patches/" + source.Uid + ".json",
            MmcSourceRecordKind.Component, source.Uid, source.Version, MmcComponentSourceKind.PackageLocal, "package:patches/" + source.Uid + ".json"));
        draft = draft with { SourceBundle = draft.SourceBundle with { MetadataRecords = metadataRecords.ToImmutable() } };
        var artifacts = plan.Classpath.Concat(plan.NativeArtifacts).Concat(plan.DownloadOnlyArtifacts).Concat(plan.AuxiliaryArtifacts).DistinctBy(a => a.Key).ToArray();
        var facts = artifacts.Select(a => new MmcMaterializedArtifactFact(a.Key, a.SourceRevision,
            a.Role == MmcArtifactRole.AssetIndex ? Hash(assetJson!) : Hash("verified fixture bytes:" + a.Key),
            a.Role == MmcArtifactRole.AssetIndex ? Encoding.UTF8.GetByteCount(assetJson!) : 10,
            "PCL/MmcArtifacts/sha256/" + (a.Role == MmcArtifactRole.AssetIndex ? Hash(assetJson!) : Hash("verified fixture bytes:" + a.Key)),
            a.Scope == MmcArtifactScope.InstanceLocal ? "libraries/" + a.RelativePath : "PCL/MmcRuntime/" + View + "/" + (a.Role == MmcArtifactRole.AssetIndex ? "assets/" : a.Role == MmcArtifactRole.LoggingConfiguration ? "" : "libraries/") + a.RelativePath,
            a.Sha1)).ToImmutableArray();
        return new(plan, draft, facts, resolved.Profile!, assetJson is null ? null : new(plan.Assets.Artifact!.Key, Encoding.UTF8.GetBytes(assetJson)));
    }
    private static MmcBindOptions Options(long expected = 1, long next = 1, MmcBindingIntent intent = MmcBindingIntent.InitialImport) => new(expected, next, intent, Hash("projection"), next);
    private static MmcBindingResult Bind(Fixture fixture, MmcInstanceDefinition? definition = null, IReadOnlyList<MmcMaterializedArtifactFact>? facts = null,
        MmcBindOptions? options = null, IReadOnlyList<MmcDerivedOutputRecord>? derived = null) =>
        MmcPlanBinder.Bind(definition ?? fixture.Definition, fixture.Plan, facts ?? fixture.Facts, options ?? Options(), derivedOutputs: derived,
            assetIndexes: fixture.AssetIndex is null ? [] : [fixture.AssetIndex]);
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private static MmcBindingResult Good(MmcBindingResult value) {
        Require(!value.HasErrors && value.Definition is not null && value.Plan is not null, string.Join(";", value.Diagnostics.Select(d => d.Code + " " + d.Message)));
        return value;
    }
    private static void Error(MmcBindingResult value, string code) => Require(value.HasErrors && value.Diagnostics.Any(d => d.Code == code), "Missing " + code + ": " + string.Join(",", value.Diagnostics.Select(d => d.Code)));
    private static void InitialBinding() {
        var fixture = Create(mavenDuplicates: true);
        var result = Good(Bind(fixture));
        Require(fixture.Definition.ArtifactLocks.IsEmpty && fixture.Definition.InstalledProjection is null && result.Definition!.ArtifactLocks.Length == fixture.Facts.Length,
            "Binding mutated input definition or lost duplicated download-only semantics.");
        Require(MmcDefinitionCodec.Read(MmcDefinitionCodec.Serialize(result.Definition!)).Status == MmcDefinitionReadStatus.Valid && result.Plan!.BoundFingerprint == result.Definition!.InstalledProjection!.BoundFingerprint, "Bound projection did not persist coherently.");
    }
    private static void BindingRejections() {
        var fixture = Create();
        Error(Bind(fixture, facts: fixture.Facts.Skip(1).ToArray()), "MMC_BIND_FACT_MISSING");
        Error(Bind(fixture, definition: fixture.Definition with { SourceBundle = fixture.Definition.SourceBundle with { MetadataRecords = [fixture.Definition.SourceBundle.MetadataRecords[0]] } }), "MMC_BIND_SOURCE");
        Error(Bind(fixture, facts: fixture.Facts.SetItem(0, fixture.Facts[0] with { SourceRevision = new string('0', 64) })), "MMC_BIND_DIGEST");
    }
    private static void ImmutableAdoption() {
        var fixture = Create();
        var original = Good(Bind(fixture)).Definition!;
        var changed = fixture.Facts[0] with { ObservedSha256 = Hash("different bytes"), RecoveryObjectRelativePath = "PCL/MmcArtifacts/sha256/" + Hash("different bytes") };
        Error(Bind(fixture, original, fixture.Facts.SetItem(0, changed), Options(intent: MmcBindingIntent.RepairExisting)), "MMC_BIND_REBIND");
        Error(Bind(fixture, original, fixture.Facts.SetItem(0, changed), Options(next: 2, intent: MmcBindingIntent.AdoptDeclaredVariant)), "MMC_BIND_REBIND");
        Good(Bind(fixture, original, options: Options(intent: MmcBindingIntent.RepairExisting)));
    }
    private static void Generations() {
        var fixture = Create();
        Error(Bind(fixture, options: Options(next: 2)), "MMC_BIND_GENERATION");
        Error(Bind(fixture, options: Options(intent: MmcBindingIntent.RepairExisting)), "MMC_BIND_ADOPTION_REQUIRED");
        var adopted = Good(Bind(fixture, options: Options(next: 2, intent: MmcBindingIntent.AdoptDeclaredVariant)));
        Require(adopted.Definition!.Generation == 2, "Explicit adoption did not advance generation.");
    }
    private static void NativeBinding() {
        var fixture = Create(native: true);
        Error(Bind(fixture), "MMC_BIND_NATIVE_INCOMPLETE");
        var artifact = fixture.Plan.NativeArtifacts[0]; var parent = fixture.Facts.Single(f => f.ArtifactKey == artifact.Key);
        string sources = MmcDerivedProofs.ComputeNativeSourceSetFingerprint(fixture.Plan.NativeArtifacts,
            fixture.Facts.Select(f => new MmcArtifactDigestFact(f.ArtifactKey, f.SourceRevision, f.ObservedSha256, f.SizeBytes)).ToArray());
        var completion = new MmcDerivationCompletion("bounded-native-v1", sources, fixture.Plan.RuntimePlanFingerprint, View, 1, true);
        var output = new MmcDerivedOutputRecord(MmcDerivedOutputKind.NativeFile, parent.ArtifactKey, parent.ObservedSha256, "bounded-native-v1", sources, fixture.Plan.RuntimePlanFingerprint,
            MmcDerivedProofs.ComputeDerivedContextFingerprint(fixture.Plan.RuntimePlanFingerprint, sources, "bounded-native-v1"), View,
            "fixture.dll", "PCL/MmcRuntime/" + View + "/natives/fixture.dll", Hash("dll bytes"), 8);
        var result = Good(Bind(fixture, options: Options() with { NativeDerivation = completion }, derived: [output]));
        Require(result.Definition!.DerivedOutputs.Length == 1, "Native output authority missing.");
        Error(Bind(fixture, options: Options() with { NativeDerivation = completion }, derived: [output with { ParentSha256 = Hash("wrong") }]), "MMC_STRUCTURE");
        Error(Bind(fixture, options: Options() with { NativeDerivation = completion with { SourceSetFingerprint = Hash("wrong") } }, derived: [output]), "MMC_BIND_DERIVED_CONTEXT");
    }
    private static void AssetBinding() {
        string objectSha1 = Convert.ToHexStringLower(SHA1.HashData([1, 2, 3]));
        string objectSha256 = Convert.ToHexStringLower(SHA256.HashData([1, 2, 3]));
        string json = "{\"objects\":{\"sound/test.ogg\":{\"hash\":\"" + objectSha1 + "\",\"size\":3}}}";
        var fixture = Create(assetJson: json);
        var parent = fixture.Facts.Single(f => f.ArtifactKey == fixture.Plan.Assets.Artifact!.Key);
        string key = MmcDerivedProofs.ComputeAssetObjectKey(parent.ArtifactKey, parent.ObservedSha256, objectSha1, 3);
        var objectFact = new MmcMaterializedArtifactFact(key, parent.ObservedSha256, objectSha256, 3,
            "PCL/MmcArtifacts/sha256/" + objectSha256, "PCL/MmcRuntime/" + View + "/assets/objects/" + objectSha1[..2] + "/" + objectSha1, objectSha1);
        var result = Good(Bind(fixture, facts: fixture.Facts.Add(objectFact)));
        Require(result.Definition!.DerivedOutputs is [{ Kind: MmcDerivedOutputKind.AssetObject }] && result.Definition.ArtifactLocks.Length == fixture.Facts.Length, "Dynamic asset object was not parent-bound independently of static inputs.");
        Error(Bind(fixture), "MMC_BIND_ASSET_OBJECT");
        Error(Bind(fixture, facts: fixture.Facts.Add(objectFact with { ArtifactKey = "PCLLF:asset-object:v1:" + Hash("unlisted") })), "MMC_BIND_ASSET_OBJECT");
        var changedInput = fixture with { AssetIndex = fixture.AssetIndex! with { Content = Encoding.UTF8.GetBytes("{\"objects\":{}}") } };
        Error(Bind(changedInput, facts: fixture.Facts.Add(objectFact)), "MMC_BIND_ASSET_INDEX");
        Good(Bind(fixture, result.Definition, fixture.Facts.Add(objectFact), Options(intent: MmcBindingIntent.RepairExisting)));
    }
    private static void Ownership() {
        var fixture = Create();
        Error(Bind(fixture, facts: fixture.Facts.Add(fixture.Facts[0] with { ArtifactKey = Hash("unlisted") })), "MMC_BIND_UNDECLARED");
        Error(Bind(fixture, facts: fixture.Facts.SetItem(0, fixture.Facts[0] with { TargetRelativePath = "minecraft/libraries/escape.jar" })), "MMC_STRUCTURE");
        var original = Good(Bind(fixture)).Definition!;
        var result = MmcPlanBinder.Bind(original, fixture.Plan, fixture.Facts, Options(intent: MmcBindingIntent.RepairExisting), priorLocks: []);
        Error(result, "MMC_BIND_PRIOR_LOCK");
    }
    private static MmcHostLaunchChain Host() => new(Hash("verified Java identity"), Hash("verified wrapper identity"), Hash("non-sensitive host arguments"));
    private static ImmutableArray<MmcJarClassObservation> ClassObservations(Fixture fixture) {
        string? systemLoader = fixture.Plan.JvmArguments.Select(a => a.Value).LastOrDefault(a => a.StartsWith("-Djava.system.class.loader=", StringComparison.Ordinal));
        string[] names = new[] { fixture.Plan.MainClass }.Concat(systemLoader is null ? [] : new[] { systemLoader["-Djava.system.class.loader=".Length..] }).Concat(fixture.Plan.Tweakers).Distinct().ToArray();
        return fixture.Plan.Classpath.DistinctBy(a => a.Key).Select((artifact, position) => new MmcJarClassObservation(artifact.Key,
            fixture.Facts.Single(f => f.ArtifactKey == artifact.Key).ObservedSha256,
            names.Select(name => new MmcJarClassPresence(name, position == 0, position == 0 ? name.Replace('.', '/') + ".class" : null)).ToImmutableArray())).ToImmutableArray();
    }
    private static void AggregateAuthority() {
        var fixture = Create(declaredJava: true, lateEntrypoint: true);
        var bound = Good(Bind(fixture));
        Require(!fixture.Plan.OverrideEffects.Single(e => e.ComponentUid == "net.minecraft").ReplacesBuiltinLaunchDefinition, "Fixture must exercise a final entry from a later local component.");
        var proof = MmcLaunchAuthorityEvaluator.Evaluate(bound.Definition!, bound.Plan!, fixture.Sources, Host(), ClassObservations(fixture));
        Require(proof.HasAuthority && proof.Retires("net.minecraft", MmcRetiredHeuristicKind.BuiltinMinecraftJavaInference) && !proof.Retires("other", MmcRetiredHeuristicKind.BuiltinMinecraftJavaInference) &&
            !proof.Retires("net.minecraftforge", MmcRetiredHeuristicKind.BuiltinForgeJavaInference) && proof.Proof!.ClassBindings.Length == 3,
            "Aggregate proof lost local final entry/classloader/tweaker structure or widened retired heuristics.");
        var differentJava = MmcLaunchAuthorityEvaluator.Evaluate(bound.Definition!, bound.Plan!, fixture.Sources, Host() with { JavaIdentitySha256 = Hash("other Java identity") }, ClassObservations(fixture));
        Require(differentJava.HasAuthority && differentJava.Proof!.ProofFingerprint != proof.Proof!.ProofFingerprint, "Authority fingerprint did not bind Java identity.");
    }
    private static void ConservativeAuthority() {
        var fixture = Create(declaredJava: true);
        var bound = Good(Bind(fixture));
        Require(!MmcLaunchAuthorityEvaluator.Evaluate(bound.Definition!, bound.Plan!, fixture.Sources, Host(), ClassObservations(fixture).RemoveAt(0)).HasAuthority, "Missing ordered class observations authorized heuristic retirement.");
        Require(!MmcLaunchAuthorityEvaluator.Evaluate(bound.Definition!, bound.Plan!, fixture.Sources, Host() with { AdditionalJvmArguments = ["-javaagent:unbound.jar"] }, ClassObservations(fixture)).HasAuthority, "Unbound agent chain authorized heuristic retirement.");
        var probes = ClassObservations(fixture);
        Require(!MmcLaunchAuthorityEvaluator.Evaluate(bound.Definition!, bound.Plan!, fixture.Sources, Host(), probes.SetItem(0, probes[0] with { ArtifactSha256 = Hash("different jar") })).HasAuthority, "Unmatched JAR bytes authorized retirement.");
        var unspecified = Create(); var unknown = Good(Bind(unspecified));
        Require(!MmcLaunchAuthorityEvaluator.Evaluate(unknown.Definition!, unknown.Plan!, unspecified.Sources, Host(), ClassObservations(unspecified)).HasAuthority, "No Java declaration authorized retirement.");
        Require(!MmcLaunchAuthorityEvaluator.Evaluate(bound.Definition!, bound.Plan! with { DefinitionGeneration = 2 }, fixture.Sources, Host(), probes).HasAuthority, "Stale generation authorized retirement.");
    }
}
