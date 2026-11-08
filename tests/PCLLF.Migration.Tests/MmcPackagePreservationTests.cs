using System.Collections.Immutable;
using System.Text;
using PCLLF.MMCC.Core;

internal static class MmcPackagePreservationTests {
    internal static IEnumerable<(string Name, Action Run)> Cases() {
        yield return ("MMC local custom patch can be unversioned without adopting cached or invented versions", UnversionedLocal);
        yield return ("MMC orphan patch files are opaque package metadata while referenced patches remain strict", OrphanMetadata);
        yield return ("MMC persisted local metadata preserves missing raw versions and remote metadata remains exact", MetadataIdentity);
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
}
