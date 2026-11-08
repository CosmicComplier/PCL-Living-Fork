using System.Collections.Immutable;
using System.Text;
using PCLLF.MMCC.Core;

internal static class MmcPackagePreservationTests {
    internal static IEnumerable<(string Name, Action Run)> Cases() {
        yield return ("MMC local custom patch can be unversioned without adopting cached or invented versions", UnversionedLocal);
        yield return ("MMC orphan patch files are opaque package metadata while referenced patches remain strict", OrphanMetadata);
        yield return ("MMC persisted local metadata preserves missing raw versions and remote metadata remains exact", MetadataIdentity);
        yield return ("MMC external author baseline pins complete file membership and preserves only root-local libraries", BaselineMembership);
        yield return ("MMC external author baseline rejects missing checksums wrong roles forged members and duplicate JSON", BaselineRejections);
    }
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private static MmcParseResult Package(string manifest, Dictionary<string, string> patches, bool omitOrphanContent = false) {
        Dictionary<string, string> content = new() { ["mmc-pack.json"] = manifest };
        foreach (var patch in patches) content["patches/" + patch.Key + ".json"] = patch.Value;
        var entries = content.Select(p => new MmcArchiveEntry(p.Key, Encoding.UTF8.GetByteCount(p.Value))).ToArray();
        if (omitOrphanContent) content.Remove("patches/backup.json");
        return MmcPackageReader.Parse(new(entries, content));
    }
    private static void UnversionedLocal() {
        const string manifest = "{\"formatVersion\":1,\"components\":[{\"uid\":\"custom\",\"cachedVersion\":\"hint-only\"}]}";
        var package = Package(manifest, new() { ["custom"] = "{\"uid\":\"custom\",\"+jvmArgs\":[\"-Dfixture=true\"]}" });
        var resolved = MmcComponentResolver.Resolve(package, []);
        Require(!package.HasErrors && resolved.IsComplete && resolved.Profile!.Components is [{ Version: null }] && resolved.MissingMetadata.IsEmpty,
            "Local UID+hash identity became a fabricated version or a remote metadata request.");
        var exact = Package("{\"formatVersion\":1,\"components\":[{\"uid\":\"consumer\",\"version\":\"1\"},{\"uid\":\"custom\"}]}", new() {
            ["consumer"] = "{\"uid\":\"consumer\",\"version\":\"1\",\"requires\":[{\"uid\":\"custom\",\"equals\":\"1\"}]}", ["custom"] = "{\"uid\":\"custom\"}"
        });
        Require(exact.HasErrors && exact.Diagnostics.Any(d => d.Code == "MMC_EXACT_VERSION"), "Unknown local version falsely satisfied a concrete exact version.");
    }
    private static void OrphanMetadata() {
        const string manifest = "{\"formatVersion\":1,\"components\":[{\"uid\":\"custom\"}]}";
        var package = Package(manifest, new() { ["custom"] = "{\"uid\":\"custom\"}", ["backup"] = "{\"uid\":\"custom\",\"formatVersion\":99,\"futureLaunch\":true,\"libraries\":{\"not\":\"active\"}}" });
        Require(!package.HasErrors && package.Package!.Components.Length == 1 && package.Package.PackageMetadata is [{ RelativePath: "patches/backup.json", ContentSha256: not null }], "Orphan backup JSON contributed or blocked active component semantics.");
        var invalidJson = Package(manifest, new() { ["custom"] = "{\"uid\":\"custom\"}", ["backup"] = "opaque malformed backup {" });
        Require(!invalidJson.HasErrors, "Unreferenced package backup was parsed as executable metadata.");
        var missing = Package(manifest, new() { ["custom"] = "{\"uid\":\"custom\"}", ["backup"] = "opaque" }, omitOrphanContent: true);
        Require(!missing.HasErrors && missing.Package!.PackageMetadata is [{ ContentSha256: null }], "Opaque file bytes must be preserved by the package baseline, not forced through semantic UTF-8 parsing.");
        var referenced = Package(manifest, new() { ["custom"] = "{\"uid\":\"custom\",\"futureLaunch\":true}" });
        Require(referenced.HasErrors, "Referenced patch capability validation was bypassed.");
    }
    private static void MetadataIdentity() {
        var draft = MmcDefinitionTests.Draft();
        string prefix = "PCL/MmcSource/" + draft.SourceBundle.BundleSha256 + "/";
        var local = new MmcSourceMetadataRecord("patches/custom.json", MmcDefinitionTests.Hash('4'), prefix + "patches/custom.json", MmcSourceRecordKind.Component,
            "custom", null, MmcComponentSourceKind.PackageLocal, "package:patches/custom.json");
        var orphan = new MmcSourceMetadataRecord("patches/backup.json", MmcDefinitionTests.Hash('5'), prefix + "patches/backup.json", MmcSourceRecordKind.PackageMetadata);
        draft = draft with { SourceBundle = draft.SourceBundle with { MetadataRecords = draft.SourceBundle.MetadataRecords.Add(local).Add(orphan) } };
        Require(MmcDefinitionCodec.Validate(draft, MmcDefinitionValidationMode.Draft).IsEmpty, "UID+raw patch hash local source identity was refused.");
        var remote = local with { RelativePath = "metadata/custom.json", ComponentSourceKind = MmcComponentSourceKind.RemoteMetadata, SourceIdentity = "https://example.invalid/custom.json" };
        Require(!MmcDefinitionCodec.Validate(draft with { SourceBundle = draft.SourceBundle with { MetadataRecords = [draft.SourceBundle.MetadataRecords[0], remote] } }, MmcDefinitionValidationMode.Draft).IsEmpty,
            "Remote source lost its mandatory exact version identity.");
    }
    internal static (MmcValidatedPackageBaseline Validated, MmcPackageBaselineInput Input) BaselineFixture() {
        string[] names = ["pack/mmc-pack.json", "pack/libraries/private.jar", "pack/minecraft/libraries/game.jar", "pack/patches/backup.json", "pack/PCL/Setup.ini"];
        var rows = names.Select((path, index) => {
            byte[] payload = Encoding.UTF8.GetBytes("self-authored-file-" + index);
            var role = MmcPackageFileBaselineCodec.Classify("pack/", path);
            return new MmcPackageFileRecord(path, role.Role, role.TargetRelativePath,
                Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(payload)), Convert.ToHexStringLower(System.Security.Cryptography.SHA1.HashData(payload)), payload.Length, Crc(payload));
        }).ToImmutableArray();
        var entries = ImmutableArray.Create(new MmcPackageArchiveDescriptor("pack/", true, 0, 0)).AddRange(rows.Select(r => new MmcPackageArchiveDescriptor(r.RelativePath, false, r.SizeBytes, r.Crc32)));
        var document = new MmcPackageFileBaseline(1, MmcDefinitionTests.Hash('a'), "pack/", entries, rows);
        byte[] bytes = MmcPackageFileBaselineCodec.Serialize(document);
        var reference = Reference(bytes, rows.Length);
        var input = new MmcPackageBaselineInput(bytes);
        var result = MmcPackageFileBaselineCodec.Read(input, reference, document.PackageSha256, "pack/");
        Require(!result.HasErrors && result.Baseline is not null, "Synthetic package baseline failed to read.");
        return (result.Baseline!, input);
    }
    private static MmcExternalBundleReference Reference(byte[] content, int count) => new(
        Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(content)), "PCL/MmcSource/" + MmcDefinitionTests.Hash('b') + "/author-files.json", count);
    private static void BaselineMembership() {
        var fixture = BaselineFixture();
        var members = MmcPackageFilePreservation.LibraryMembers(fixture.Validated);
        var artifacts = MmcPackageFilePreservation.CreateLibraryArtifacts(fixture.Validated);
        Require(members is [{ SourceRelativePath: "pack/libraries/private.jar", LibraryRelativePath: "private.jar" }] && artifacts is [{ ComponentUid: "", Role: MmcArtifactRole.DownloadOnly, Scope: MmcArtifactScope.InstanceLocal, PackageFileOrigin: not null }], "Package originals invented a component UID or treated game-root libraries/metadata as executable inputs.");
        Require(members[0].ArtifactKey.Length == 64 && members[0].SourceRevision.Length == 64 && members[0].ArtifactKey != members[0].SourceRevision && members[0].BaselineReference.EntryCount == 5, "Independent package-file origin protocol was not preserved.");
    }
    private static void BaselineRejections() {
        var fixture = BaselineFixture();
        var reference = fixture.Validated.Reference;
        var corrupted = fixture.Input.Content.ToArray(); corrupted[0] ^= 1;
        Require(MmcPackageFileBaselineCodec.Read(new(corrupted), reference, fixture.Validated.Baseline.PackageSha256, "pack/").HasErrors, "Changed baseline bytes kept their original membership authority.");
        Require(MmcPackageFileBaselineCodec.Read(fixture.Input, reference, MmcDefinitionTests.Hash('f'), "pack/").HasErrors, "Wrong original ZIP identity accepted.");
        foreach (Action<System.Text.Json.Nodes.JsonObject> change in new Action<System.Text.Json.Nodes.JsonObject>[] {
            root => root["files"]![1]!.AsObject().Remove("sha256"),
            root => { root["files"]![2]!["role"] = "LocalLibrary"; root["files"]![2]!["targetRelativePath"] = "libraries/evil.jar"; },
            root => root["files"]![1]!["relativePath"] = "pack/libraries/forged.jar",
            root => root["files"]![1]!["crc32"] = 1
        }) {
            var root = System.Text.Json.Nodes.JsonNode.Parse(fixture.Input.Content.Span)!.AsObject(); change(root);
            byte[] bytes = Encoding.UTF8.GetBytes(root.ToJsonString());
            Require(MmcPackageFileBaselineCodec.Read(new(bytes), Reference(bytes, 5), fixture.Validated.Baseline.PackageSha256, "pack/").HasErrors, "Malformed role/checksum/member baseline accepted.");
        }
        string duplicate = Encoding.UTF8.GetString(fixture.Input.Content.Span).Replace("\"formatVersion\":1", "\"formatVersion\":1,\"formatVersion\":1", StringComparison.Ordinal);
        byte[] duplicateBytes = Encoding.UTF8.GetBytes(duplicate);
        Require(MmcPackageFileBaselineCodec.Read(new(duplicateBytes), Reference(duplicateBytes, 5), fixture.Validated.Baseline.PackageSha256, "pack/").HasErrors, "Duplicate baseline root property accepted.");
        Require(MmcPackageFileBaselineCodec.Read(fixture.Input, reference, fixture.Validated.Baseline.PackageSha256, "pack/", new() { MaxDocumentBytes = 10 }).HasErrors, "Baseline document budget ignored.");
    }
    private static uint Crc(byte[] bytes) {
        uint crc = uint.MaxValue;
        foreach (byte value in bytes) { crc ^= value; for (int bit = 0; bit < 8; bit++) crc = crc >> 1 ^ ((crc & 1) == 0 ? 0 : 0xedb88320u); }
        return ~crc;
    }
}
