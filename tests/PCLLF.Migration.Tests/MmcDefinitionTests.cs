using System.Collections.Immutable;
using System.Text.Json.Nodes;
using PCLLF.MMCC.Core;

internal static class MmcDefinitionTests {
    internal static IEnumerable<(string Name, Action Run)> Cases() {
        yield return ("MMC definition uses strict camelCase immutable source references and roundtrips", Roundtrip);
        yield return ("MMC definition unknown managed schema stays unsupported and damaged JSON stays invalid", UnsupportedAndDamaged);
        yield return ("MMC definition rejects self parent invalid IDs generation and duplicate keys", Identity);
        yield return ("MMC definition separates original sources from user patches and preferences", SourceOwnership);
        yield return ("MMC definition validates artifact recovery targets states and declared digests", ArtifactLocks);
        yield return ("MMC definition pins fingerprints and projection generation", Projection);
        yield return ("MMC definition bounds arrays text and external author file baselines", Budgets);
    }
    internal static string Hash(char digit) => new(digit, 64);
    internal static MmcInstanceDefinition Draft(string? resolutionFingerprint = null) {
        string bundle = Hash('b');
        return new(1, "Mmc", "prism-v1", Guid.Parse("8a0dacb4-38be-4cf7-8c07-99f9c535c07b"), 1,
            new(Hash('a'), "Fixture.zip"), new("fixture/", bundle, resolutionFingerprint ?? Hash('c'),
                [new("mmc-pack.json", Hash('d'), "PCL/MmcSource/" + bundle + "/mmc-pack.json", MmcSourceRecordKind.Manifest)]),
            new("minecraft"), [], [], []);
    }
    internal static MmcInstanceDefinition Published(MmcInstanceDefinition? draft = null) {
        var value = (draft ?? Draft()) with { InstalledProjection = new(Hash('1'), Hash('2'), (draft ?? Draft()).Generation, Hash('3'), Hash('0')) };
        return value with { InstalledProjection = value.InstalledProjection! with { BoundFingerprint = MmcDefinitionCodec.ComputeBoundFingerprint(value) } };
    }
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Invalid(MmcInstanceDefinition value) => Require(!MmcDefinitionCodec.Validate(value).IsEmpty, "Invalid definition passed structural validation.");
    private static void Roundtrip() {
        var original = Published();
        string json = MmcDefinitionCodec.Serialize(original);
        var result = MmcDefinitionCodec.Read(json);
        Require(result.Status == MmcDefinitionReadStatus.Valid && result.Definition!.InstanceId == original.InstanceId && result.Definition.SourceBundle.MetadataRecords[0].ObjectRelativePath == original.SourceBundle.MetadataRecords[0].ObjectRelativePath, "Definition source identity did not roundtrip.");
        Require(json.Contains("\"schemaVersion\":1", StringComparison.Ordinal) && !json.Contains("SchemaVersion", StringComparison.Ordinal), "Persistence is not strict camelCase.");
        Require(MmcDefinitionCodec.Validate(Draft(), MmcDefinitionValidationMode.Draft).IsEmpty && !MmcDefinitionCodec.Validate(Draft()).IsEmpty, "Unbound draft was mistaken for a published definition.");
    }
    private static void UnsupportedAndDamaged() {
        string json = MmcDefinitionCodec.Serialize(Published());
        Require(MmcDefinitionCodec.Read(json.Replace("\"schemaVersion\":1", "\"schemaVersion\":2", StringComparison.Ordinal)).Status == MmcDefinitionReadStatus.Unsupported, "Unknown schema fell back to a usable definition.");
        Require(MmcDefinitionCodec.Read("{bad").Status == MmcDefinitionReadStatus.Invalid, "Malformed JSON did not remain invalid managed data.");
        var withUnknown = JsonNode.Parse(json)!.AsObject(); withUnknown["accessToken"] = "not-a-real-account";
        Require(MmcDefinitionCodec.Read(withUnknown.ToJsonString()).Status == MmcDefinitionReadStatus.Invalid, "Unknown authentication/preference field was silently retained.");
    }
    private static void Identity() {
        var definition = Published();
        Invalid(definition with { InstanceId = Guid.Empty }); Invalid(definition with { ParentInstanceId = definition.InstanceId });
        Invalid(definition with { ParentInstanceId = Guid.Empty }); Invalid(definition with { Generation = 0 });
        string json = MmcDefinitionCodec.Serialize(definition);
        Require(MmcDefinitionCodec.Read(json.Replace("\"schemaVersion\":1", "\"schemaVersion\":1,\"schemaVersion\":1", StringComparison.Ordinal)).Status == MmcDefinitionReadStatus.Invalid, "Duplicate key accepted.");
        Require(MmcDefinitionCodec.Read(json.Replace(definition.InstanceId.ToString("D"), definition.InstanceId.ToString("N"), StringComparison.Ordinal)).Status == MmcDefinitionReadStatus.Invalid, "Non-D GUID spelling accepted.");
    }
    private static void SourceOwnership() {
        var definition = Published();
        Invalid(definition with { Origin = definition.Origin with { OriginalName = "C:/player.zip" } });
        var original = definition.SourceBundle.MetadataRecords[0];
        Invalid(definition with { SourceBundle = definition.SourceBundle with { MetadataRecords = [original with { ObjectRelativePath = "saves/world/source.json" }] } });
        Invalid(definition with { ComponentOverrides = [new("fixture", PatchReference: new(Hash('4'), original.ObjectRelativePath))] });
        Invalid(definition with { SourceBundle = definition.SourceBundle with { MetadataRecords = [original, new("metadata/fixture.json", Hash('4'), "PCL/MmcSource/" + definition.SourceBundle.BundleSha256 + "/remote.json", MmcSourceRecordKind.Component, "fixture", "1", MmcComponentSourceKind.RemoteMetadata, "https://user:secret@example.invalid/file")] } });
    }
    internal static MmcInstanceDefinition WithArtifact() {
        string key = Hash('4'), content = Hash('5'), revision = Hash('6');
        return Draft() with {
            ArtifactInventory = [new(key, "fixture", MmcArtifactRole.Classpath, MmcArtifactScope.InstanceLocal, "windows-x86_64", revision, "fixture.jar", MmcArtifactAdoptionState.Adopted, DeclaredSha256: content)],
            ArtifactLocks = [new(key, revision, content, 10, "PCL/MmcArtifacts/sha256/" + content, "libraries/fixture.jar")]
        };
    }
    private static void ArtifactLocks() {
        var definition = Published(WithArtifact());
        Require(MmcDefinitionCodec.Validate(definition).IsEmpty, "Valid local artifact lock was rejected.");
        var locked = definition.ArtifactLocks[0];
        Invalid(definition with { ArtifactLocks = [locked, locked] });
        Invalid(definition with { ArtifactLocks = [locked with { RecoveryObjectRelativePath = "libraries/original.jar" }] });
        Invalid(definition with { ArtifactLocks = [locked with { TargetRelativePath = "minecraft/libraries/fixture.jar" }] });
        Invalid(definition with { ArtifactInventory = [definition.ArtifactInventory[0] with { State = MmcArtifactAdoptionState.Unadopted }] });
        Invalid(definition with { ArtifactLocks = [locked with { AdoptedSha256 = Hash('7'), RecoveryObjectRelativePath = "PCL/MmcArtifacts/sha256/" + Hash('7') }] });
        Invalid(definition with { ArtifactInventory = [definition.ArtifactInventory[0] with { DeclaredSha1 = new string('a', 40) }] });
    }
    private static void Projection() {
        var definition = Published();
        Invalid(definition with { InstalledProjection = definition.InstalledProjection! with { ManifestGeneration = 2 } });
        Invalid(definition with { InstalledProjection = definition.InstalledProjection! with { BoundFingerprint = Hash('9') } });
        Invalid(definition with { Origin = definition.Origin with { OriginalName = "Changed.zip" } });
    }
    private static void Budgets() {
        string json = MmcDefinitionCodec.Serialize(Published());
        Require(MmcDefinitionCodec.Read(json, new() { MaxMetadataBytes = 10 }).Status == MmcDefinitionReadStatus.Invalid, "Definition byte budget ignored.");
        var draft = Draft() with { ComponentOverrides = [new("one"), new("two")] };
        Require(!MmcDefinitionCodec.Validate(draft, MmcDefinitionValidationMode.Draft, new() { MaxComponents = 1 }).IsEmpty, "Array count budget ignored.");
        var bundle = Draft();
        var external = bundle with { AuthorFileBaseline = new(Hash('7'), "PCL/MmcSource/" + bundle.SourceBundle.BundleSha256 + "/author-files.json", 100000) };
        Require(MmcDefinitionCodec.Validate(external, MmcDefinitionValidationMode.Draft).IsEmpty, "Large author-file list envelope must remain external.");
        Require(!MmcDefinitionCodec.Validate(external with { AuthorFileBaseline = external.AuthorFileBaseline! with { EntryCount = 100001 } }, MmcDefinitionValidationMode.Draft).IsEmpty, "External list count budget ignored.");
    }
}
