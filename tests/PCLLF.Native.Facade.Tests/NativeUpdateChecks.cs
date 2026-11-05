using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;

namespace PCLLF.Native.Facade.Tests;

// The genuine modern Main is the subject. Transport callbacks supply fixture
// bytes; no Core replacement, application entry point or external HTTP is run.
internal static partial class NativeUpdateChecks
{
    private const string Updater = "PCL.ModLauncherUpdate";
    private const string GitHub = "https://github.com/DDZmumo/PCL-Living-Fork/releases/download/";
    private const string Cnb = "https://cnb.cool/ddzmumo/PCL-Living-Fork/-/releases/download/";

    internal static void Run(FixtureEnvironment environment)
    {
        Native.Check(OperatingSystem.IsWindows(), "Updater acceptance requires real Windows filesystem/process/mutex semantics.");
        Native.Check(Native.Type(Updater).Assembly == Native.Main && Native.Main == typeof(PCL.Application).Assembly,
            "Updater acceptance must reference the actual Main, not a stand-in assembly.");
        string root = Path.Combine(environment.Data, "updater-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            VersionsUrlsAssetsAndManifest();
            string source = Native.Main.Location;
            Native.Check(File.Exists(source), "Actual managed Main assembly is unavailable in the complete staged output.");
            CandidateIdentity(root, source);
            Replacements(root, source);
            WorkflowAndCancellation(root, source);
            environment.AssertNoForbiddenIo();
            Console.WriteLine("PENDING native updater EXTERNAL-01: official PCL binary rejection requires a pinned external official artifact; the actual foreign test-host assembly is rejected here.");
            Console.WriteLine("PENDING native updater EXTERNAL-02: published legacy EXE -> native entry and single-file bundle are the separate Test-NativeUpdateBridge job, not this managed-Main facade run.");
        }
        finally
        {
            Native.Check(Path.GetDirectoryName(root) == environment.Data && Path.GetFileName(root).StartsWith("updater-", StringComparison.Ordinal),
                "Unsafe updater fixture cleanup path.");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static object? Call(string method, params object?[] args) => Native.Static(Updater, method, args);
    private static bool Bool(string method, params object?[] args) => (bool)Call(method, args)!;
    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
    private static void Pass(string id, string detail) => Console.WriteLine("PASS native updater " + id + ": " + detail);
    private static Exception Reject(Action action, string detail)
    {
        try { action(); }
        catch (Exception error) { return error; }
        throw new InvalidOperationException(detail);
    }
    private static JObject Asset(string name, string tag = "v1.2.0", bool cnb = false) => new()
    {
        ["name"] = name,
        ["browser_download_url"] = (cnb ? Cnb : GitHub) + Uri.EscapeDataString(tag) + "/" + Uri.EscapeDataString(name)
    };
    private static JObject Release(string tag, long bytes, bool cnb = true)
    {
        string name = "PCL-Living-Fork-" + tag.TrimStart('v') + ".exe";
        JObject executable = Asset(name, tag, cnb); executable["size"] = bytes;
        JObject sums = Asset("SHA256SUMS.txt", tag, cnb); sums["size"] = 200;
        return new JObject
        {
            ["tag_name"] = tag, ["draft"] = false, ["prerelease"] = tag.Contains('-'),
            ["assets"] = new JArray(executable, sums)
        };
    }

    private static void VersionsUrlsAssetsAndManifest()
    {
        foreach (var (tag, current, expected) in new[]
        {
            ("v1.0.1", "1.0.0", true), ("v1.0.1.0", "1.0.1", false),
            ("v0.9.9", "1.0.1", false), ("v1.0.2-beta", "1.0.1", false),
            ("v1.3.5", "1.3.5-pre-1", true), ("v1.3.6", "1.3.5-pre-1", true),
            ("v1.3.4", "1.3.5-pre-1", false), ("v1.3.5-pre-2", "1.3.5-pre-1", false),
            ("v1.3.5", "1.3.5-pre-", false)
        }) Native.Check(Bool("NewerRelease", tag, current) == expected, "Release version comparison changed: " + tag + "/" + current);
        object stable = Native.Enum(Updater + "+UpdateChannel", "Stable")!;
        object preview = Native.Enum(Updater + "+UpdateChannel", "StableAndPrerelease")!;
        Native.Check(!Bool("NewerReleaseForChannel", "v1.3.5-pre-3", "1.3.4", stable) &&
            Bool("NewerReleaseForChannel", "v1.3.5-pre-3", "1.3.4", preview) &&
            (int)Call("CompareReleaseVersions", "v1.3.5-pre-10", "v1.3.5-pre-3")! > 0 &&
            (int)Call("CompareReleaseVersions", "v1.3.5", "v1.3.5-pre-10")! > 0,
            "LF50 stable/prerelease ordering changed.");
        Pass("REL-01", "all nine LF50 Test-Release comparisons and stable/prerelease ordering");
        Native.Check(!Bool("NewerReleaseForChannel", "v" + PCL.ModBase.ReleaseBaselineVersion, "dev_edition", preview) &&
            Bool("NewerReleaseForChannel", "v1.3.5", "dev_edition", stable) &&
            !Bool("NewerReleaseForChannel", "dev_edition", "1.3.4", preview), "Development edition was treated as a public version or downgraded to its baseline.");
        object pageTransport = Activator.CreateInstance(Native.Type(Updater + "+UpdateTransport"), true)!;
        Native.Set(pageTransport, "ReadReleaseList", (Func<string, JArray>)(_ => new JArray(Release("v1.3.4", 10, false), Release("v1.3.5", 10, false), Release("v1.3.5-pre-3", 10, false), Release("dev_edition", 10, false))));
        object githubSource = Native.Enum(Updater + "+UpdateSource", "GitHub")!;
        var stableRows = (List<JObject>)Call("GetPageReleases", githubSource, stable, pageTransport)!;
        var previewRows = (List<JObject>)Call("GetPageReleases", githubSource, preview, pageTransport)!;
        Native.Check(stableRows.Count == 2 && previewRows.Count == 3 && previewRows.All(value => value["tag_name"]!.ToString() != "dev_edition"), "Update-page catalogue did not validate public tags and channel eligibility.");
        var firstPage = new JArray(Enumerable.Range(0, 100).Select(index => Release("v1.2." + index, 10, false)));
        JObject historical = Release("v1.1.0", 10, false); historical["assets"] = new JArray();
        JObject draft = Release("v1.4.0", 10, false); draft["draft"] = true;
        var secondPage = new JArray(Release("v1.2.0", 10, false), Release("v1.3.5-pre-10", 10, false), historical, draft, Release("dev_edition", 10, false));
        var pagesRead = new List<string>();
        Native.Set(pageTransport, "ReadReleaseList", (Func<string, JArray>)(url =>
        {
            pagesRead.Add(url);
            Native.Check(url.Contains("per_page=100", StringComparison.Ordinal), "Release catalogue uses a truncated page size.");
            return url.EndsWith("&page=1", StringComparison.Ordinal) ? firstPage : url.EndsWith("&page=2", StringComparison.Ordinal) ? secondPage : throw new InvalidOperationException("Unexpected release page.");
        }));
        var allPages = (List<JObject>)Call("GetPageReleases", githubSource, preview, pageTransport)!;
        Native.Check(allPages.Count == 102 && pagesRead.Count == 2 && allPages.Any(value => value["tag_name"]!.ToString() == "v1.1.0") &&
            allPages.Select(value => value["tag_name"]!.ToString()).Distinct().Count() == 102 && !Bool("CanInstallPageRelease", historical),
            "Catalogue omitted later pages/history-only releases or exposed an unusable update.");
        object cnbSource = Native.Enum(Updater + "+UpdateSource", "Cnb")!;
        Native.Set(pageTransport, "ReadReleaseList", (Func<string, JArray>)(url =>
        {
            Native.Check(url.StartsWith("https://cnb.cool/", StringComparison.Ordinal) && url.Contains("page_size=100&page=1", StringComparison.Ordinal), "CNB catalogue did not use its public paginated endpoint.");
            return new JArray(Release("v1.3.4", 10));
        }));
        Native.Check(((List<JObject>)Call("GetPageReleases", cnbSource, preview, pageTransport)!).Count == 1, "CNB only supports the latest manifest instead of the release catalogue.");
        pagesRead.Clear();
        Native.Set(pageTransport, "ReadReleaseList", (Func<string, JArray>)(url =>
        {
            pagesRead.Add(url);
            if (url.StartsWith("https://cnb.cool/", StringComparison.Ordinal)) return new JArray(Release("v1.3.4", 10));
            if (url.EndsWith("&page=1", StringComparison.Ordinal)) return firstPage;
            throw new IOException("Fixture second-page failure");
        }));
        var fallbackRows = (List<JObject>)Call("GetPageReleases", githubSource, preview, pageTransport)!;
        Native.Check(fallbackRows.Count == 1 && fallbackRows[0]["tag_name"]!.ToString() == "v1.3.4" && pagesRead.Count == 3, "Partial release catalogue was reported as complete instead of retrying the other source.");
        Native.Set(pageTransport, "ReadReleaseList", (Func<string, JArray>)(_ => throw new IOException("Fixture catalogue unavailable")));
        Reject(() => Call("GetPageReleases", githubSource, preview, pageTransport), "Incomplete catalogue was replaced by a latest-only list.");
        Pass("CAT-01", "paginated public CNB/GitHub catalogues, deduplication, draft/dev exclusion, history-only releases and complete-source fallback");

        foreach (string url in new[]
        {
            "https://github.com/DDZmumo/LivingPCL2-Re/releases/download/v1.0.1/LivingPCL2-Re-1.0.1.exe",
            GitHub + "v1.2.0/PCL-Living-Fork-1.2.0.exe", Cnb + "v1.2.0/PCL-Living-Fork-1.2.0.exe"
        }) Native.Check(Bool("ValidAssetUrl", url), "Own release URL rejected: " + url);
        foreach (string url in new[]
        {
            "http://github.com/DDZmumo/LivingPCL2-Re/releases/download/v1.0.1/a.exe",
            "https://github.com/Other/PCL/releases/download/v1.0.1/a.exe", "https://example.com/a.exe",
            "https://github.com/DDZmumo/LivingPCL2-Re-other/releases/download/a.exe",
            "https://github.com:444/DDZmumo/PCL-Living-Fork/releases/download/v1/a.exe",
            "https://user@github.com/DDZmumo/PCL-Living-Fork/releases/download/v1/a.exe",
            GitHub + "v1/a.exe#fragment"
        }) Native.Check(!Bool("ValidAssetUrl", url), "Untrusted release URL accepted: " + url);
        Pass("REL-02", "original own/renamed repository URL guards plus CNB, userinfo, port and fragment boundaries");

        JObject old = Asset("LivingPCL2-Re-1.2.0.exe");
        old["browser_download_url"] = "https://github.com/DDZmumo/LivingPCL2-Re/releases/download/v1.2.0/LivingPCL2-Re-1.2.0.exe";
        JObject currentAsset = Asset("PCL-Living-Fork-1.2.0.exe"), spaced = Asset("PCL Living Fork - 1.2.0.exe");
        string actualName = (string)Native.Field(Native.Type("PCL.ModBase"), "LauncherExecutableName").GetValue(null)!;
        foreach (var (assets, expected) in new[]
        {
            (new JArray(old.DeepClone()), (string)old["name"]!),
            (new JArray(old.DeepClone(), currentAsset.DeepClone()), (string)currentAsset["name"]!),
            (new JArray(old.DeepClone(), spaced.DeepClone(), Asset(actualName)), actualName)
        }) Native.Check((string)((JObject)Call("ReleaseExecutable", new JObject { ["assets"] = assets })!)["name"]! == expected,
            "Updater chose the wrong historical/renamed release asset.");
        Reject(() => Call("ReleaseExecutable", new JObject { ["assets"] = new JArray(currentAsset.DeepClone(), currentAsset.DeepClone()) }),
            "Ambiguous canonical release assets were accepted.");
        Reject(() => Call("ReleaseExecutable", new JObject { ["assets"] = new JArray(new JObject { ["name"] = "PCL-Living-Fork-1.0.exe", ["browser_download_url"] = "https://example.com/not-owned.exe" }) }),
            "Untrusted selected release asset was accepted.");
        Pass("REL-03", "all original asset precedence assertions and actual canonical filename; ambiguity/foreign candidate refused");

        string hash = Hash(Native.Main.Location);
        foreach (string name in new[] { "LivingPCL2-Re-1.0.1.exe", "PCL Living Fork - 1.2.0.exe" })
            Native.Check((string)Call("ManifestHash", hash + "  " + name, name)! == hash, "Original SHA256 manifest/space parsing changed.");
        Native.Check((string)Call("ManifestHash", hash.ToUpperInvariant() + " *" + actualName + "\r\n", actualName)! == hash,
            "Uppercase/star/CRLF SHA256 record changed.");
        Reject(() => Call("ManifestHash", hash + "  unrelated.exe", actualName), "Wrong manifest candidate name was accepted.");
        Reject(() => Call("ManifestHash", new string('z', 64) + "  " + actualName, actualName), "Nonhexadecimal manifest hash was accepted.");
        Pass("REL-04", "actual Main SHA256, historical/spaced names and exact manifest candidate identity");
    }

    private static void CandidateIdentity(string root, string source)
    {
        Native.Check(Bool("IsLivingExecutable", source), "Actual Main managed assembly was not recognized as LF.");
        Call("ValidateExecutable", source, Hash(source));
        Reject(() => Call("ValidateExecutable", source, new string('0', 64)), "Wrong SHA256 candidate was accepted.");
        string foreign = Path.Combine(root, "foreign-test-host.exe");
        File.Copy(typeof(NativeUpdateChecks).Assembly.Location, foreign);
        Native.Check(!Bool("IsLivingExecutable", foreign), "Foreign facade test-host assembly was mistaken for LF.");
        Reject(() => Call("ValidateExecutable", foreign, Hash(foreign)), "A foreign managed assembly with a valid hash was accepted.");
        string target = Path.Combine(root, "foreign-target.exe"); File.Copy(foreign, target);
        string before = Hash(target);
        Reject(() => Call("ReplaceExecutable", source, target, Hash(source), false), "A foreign executable target was overwritten.");
        Native.Check(Hash(target) == before, "Foreign target changed despite rejected identity.");
        Pass("REL-05", "real Main role+SHA accepted; foreign real test assembly/correct hash rejected as candidate and untouched target");
    }
}
