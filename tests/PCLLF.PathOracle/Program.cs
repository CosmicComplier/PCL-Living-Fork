using System.Buffers.Binary;
using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using MeloongCore;
using Newtonsoft.Json.Linq;
using PCLCS;

// LF source/behaviour: f4aa0fae62c3ee8af24c81e61844c8fdd13c8983, LICENCE/NOTICE.md.
// Prefix implementation: real MeloongCore c774ae4c85fd94e0eaa171db21e78a65870df19b.
// Original path/size methods are compiled from git, not reimplemented in this driver.
internal static class Program {
    internal const string Protocol = "pcllf-resource-paths-v6";
    private const string Lf = "f4aa0fae62c3ee8af24c81e61844c8fdd13c8983";
    private const string Shared = "c774ae4c85fd94e0eaa171db21e78a65870df19b";
    private const string Outside = "资源路径超出了当前目录。";
    private const string Link = "请通过资源管理器管理链接文件或链接目录。";
    private static readonly UTF8Encoding Utf8 = new(false, true);

    private static int Main(string[] args) {
        try {
            if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("This oracle requires Windows and the LF CLR4 baseline executable.");
            if (args.Length == 2 && args[0] == "prepare") { Prepare(args[1]); return 0; }
            if (args.Length == 2 && args[0] == "cleanup") { Cleanup(args[1]); return 0; }
            if (args.Length == 5 && args[0] == "compare") return Compare(args[1], args[2], args[3], args[4]);
            throw new ArgumentException("Usage: prepare <fixture> | cleanup <fixture> | compare <cases.json> <baseline.json> <report.json> Diagnostic|RequireEquivalent");
        } catch (Exception error) {
            Console.Error.WriteLine(error);
            return 1;
        }
    }

    private static string FixtureRoot(string path) {
        string full = Path.GetFullPath(path).TrimEnd('\\');
        string temporary = Path.GetFullPath(Path.GetTempPath()).TrimEnd('\\') + "\\";
        if (!full.StartsWith(temporary, StringComparison.OrdinalIgnoreCase) ||
            !Regex.IsMatch(Path.GetFileName(full), "^PclResourcePathOracle-[a-f0-9]{32}$")) throw new IOException("Unsafe oracle fixture path.");
        return full;
    }

    private static void Save(string file, JToken value) => File.WriteAllText(file, value.ToString(), Utf8);
    private static string? Text(JToken? value) => value?.Type == JTokenType.Null ? null : value?.ToString();
    private static JObject Ok(object? value = null) => new(new JProperty("status", "ok"), new JProperty("value", value));
    private static JObject Error(string type, string? message = null) {
        JObject result = new(new JProperty("status", "exception"), new JProperty("type", type));
        if (message is not null) result["message"] = message;
        return result;
    }

    private static void Prepare(string requestedRoot) {
        string fixture = FixtureRoot(requestedRoot);
        string cwd = Path.Combine(fixture, "work");
        string root = Path.Combine(fixture, "resources");
        Directory.CreateDirectory(cwd);
        Directory.CreateDirectory(Path.Combine(root, "sub"));
        File.WriteAllText(Path.Combine(root, "file.txt"), "owned resource", Utf8);
        Directory.CreateDirectory(Path.Combine(fixture, "target", "nested"));
        File.WriteAllText(Path.Combine(fixture, "target", "file.txt"), "owned link target", Utf8);
        File.WriteAllText(Path.Combine(fixture, "target", "nested", "file.txt"), "owned ancestor target", Utf8);
        JArray links = new();
        Save(Path.Combine(fixture, "links.json"), links);
        void CreateLink(string relative, string target, bool directory) {
            string path = Path.Combine(fixture, relative);
            links.Add(new JObject(new JProperty("path", path), new JProperty("directory", directory)));
            Save(Path.Combine(fixture, "links.json"), links); // Also permits cleanup after a failed creation.
            if (directory) Directory.CreateSymbolicLink(path, target); else File.CreateSymbolicLink(path, target);
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0) throw new IOException("The fixture link was not created as a reparse point.");
        }
        CreateLink("trusted-root", Path.Combine(fixture, "target"), true);
        CreateLink("ancestor", Path.Combine(fixture, "target"), true);
        CreateLink("resources\\directory-link", Path.Combine(fixture, "target"), true);
        CreateLink("resources\\file-link", Path.Combine(fixture, "target", "file.txt"), false);
        CreateLink("resources\\dangling-link", Path.Combine(fixture, "absent-target"), false);
        // These are OpenRead/entry-decoding API probes, not a replacement baseline
        // for the LF ValidatePackage business function. All ZIP IO is fixture-owned.
        string ownedZip = Path.Combine(fixture, "owned.zip");
        string utf8Zip = Path.Combine(fixture, "utf8-long-s.zip");
        string noEfsZip = Path.Combine(fixture, "utf8-long-s-no-efs.zip");
        string badZip = Path.Combine(fixture, "bad.zip");
        CreateZipFixture(ownedZip, "shaders/entry");
        CreateZipFixture(utf8Zip, "ſhaders/entry");
        ClearFixtureEfs(utf8Zip, noEfsZip);
        File.WriteAllText(badZip, "not-a-zip", Utf8);

        JArray cases = new();
        void Add(string id, string operation, string? value, JObject expected, string? parent = null, bool allowRoot = false, string? other = null, bool informational = false) {
            JObject item = new(new JProperty("id", id), new JProperty("operation", operation), new JProperty("value", value),
                new JProperty("root", parent), new JProperty("allowRoot", allowRoot), new JProperty("other", other),
                new JProperty("expected", expected), new JProperty("informational", informational));
            if (operation == "ordinal") {
                // Numeric UTF16 preserves isolated surrogates as well as NUL, independent
                // of JSON string-reader Unicode replacement and UTF8 encoder policy.
                item["value"] = null;
                item["other"] = null;
                item["valueUtf16"] = new JArray(value!.Select(unit => (int)unit));
                item["otherUtf16"] = new JArray(other!.Select(unit => (int)unit));
            }
            cases.Add(item);
        }
        void Ensure(string id, string parent, string value, string? error = null, bool allow = false) =>
            Add(id, "ensure", value, error is null ? Ok() : Error("System.IO.IOException", error), parent, allow);
        JObject SystemOutcome(params string[] allowed) => new(new JProperty("allowed", new JArray(allowed)));

        Ensure("guard.file", root, Path.Combine(root, "file.txt"));
        Ensure("guard.directory", root, Path.Combine(root, "sub"));
        Ensure("guard.absent-child", root, Path.Combine(root, "absent"));
        Ensure("guard.absent-chain", root, Path.Combine(root, "missing", "deep", "leaf"));
        Ensure("guard.normalized-inside", root, Path.Combine(root, "sub", "..", "file.txt"));
        Ensure("guard.normalized-outside", root, Path.Combine(root, "..", "escape"), Outside);
        Ensure("guard.sibling-prefix", root, root + "-sibling\\file.txt", Outside);
        Ensure("guard.same-denied", root, root, Outside);
        Ensure("guard.same-allowed", root, root + "\\", allow: true);
        Ensure("guard.absent-root-same", Path.Combine(fixture, "absent-root"), Path.Combine(fixture, "absent-root"), allow: true);
        Ensure("guard.mixed-separators", root.Replace('\\', '/') + "///", Path.Combine(root, "sub").Replace('\\', '/') + "//");
        Ensure("guard.ascii-case", root.ToUpperInvariant(), Path.Combine(root, "file.txt").ToLowerInvariant());
        Ensure("guard.file-link", root, Path.Combine(root, "file-link"), Link);
        Ensure("guard.directory-link", root, Path.Combine(root, "directory-link"), Link);
        Ensure("guard.ancestor-link", root, Path.Combine(root, "directory-link", "nested", "file.txt"), Link);
        Ensure("guard.missing-below-link", root, Path.Combine(root, "directory-link", "missing", "leaf"), Link);
        Ensure("guard.trusted-root-link", Path.Combine(fixture, "trusted-root"), Path.Combine(fixture, "trusted-root", "file.txt"));
        Ensure("guard.trusted-root-link-same", Path.Combine(fixture, "trusted-root"), Path.Combine(fixture, "trusted-root"), allow: true);
        Ensure("guard.trusted-root-denied", Path.Combine(fixture, "trusted-root"), Path.Combine(fixture, "trusted-root"), Outside);
        Ensure("guard.link-above-root", Path.Combine(fixture, "ancestor", "nested"), Path.Combine(fixture, "ancestor", "nested", "file.txt"));
        Ensure("guard.collapsed-link-segment", root, Path.Combine(root, "directory-link", "..", "file.txt"));
        Add("guard.dangling-link", "ensure", Path.Combine(root, "dangling-link"), SystemOutcome("ok", "System.IO.IOException"), root);
        Add("guard.null-root", "ensure", Path.Combine(root, "file.txt"), Error("System.ArgumentNullException"));
        Add("guard.null-target", "ensure", null, Error("System.ArgumentNullException"), root);
        Add("guard.root-error-first", "ensure", "also\0bad", Error("System.ArgumentNullException"));
        string drive = Path.GetPathRoot(fixture)!;
        Ensure("guard.drive-root-equal-allow", drive, drive, allow: true);
        Ensure("guard.drive-root-equal-deny", drive, drive, Outside);
        // Read-only ancestor attributes are permitted here. The baseline result is observed,
        // without assuming that a runner's temp-directory ancestors lack links or restrictions.
        Add("guard.drive-root-descendant", "ensure", Path.Combine(fixture, "missing-drive-root-leaf"),
            SystemOutcome("ok", "System.NullReferenceException", "System.ArgumentNullException", "System.IO.IOException", "System.UnauthorizedAccessException", "System.Security.SecurityException"), drive);
        foreach ((string id, string first, string second) in new[] { ("sigma", "Σ", "ς"), ("deseret", "\U00010400", "\U00010428") }) {
            string unicodeRoot = Path.Combine(fixture, "unicode-" + first), variant = Path.Combine(fixture, "unicode-" + second);
            Ensure("guard.unicode-" + id + "-equal", unicodeRoot, variant, Outside, true);
            Ensure("guard.unicode-" + id + "-prefix", unicodeRoot, Path.Combine(variant, "missing", "leaf"), Outside);
            Ensure("guard.unicode-" + id + "-exact", unicodeRoot, Path.Combine(unicodeRoot, "missing", "leaf"));
        }
        foreach ((string id, string prefix) in new[] { ("nt", @"\??\"), ("dot", @"\\.\") }) {
            Add("guard.device-" + id + "-descendant", "ensure", prefix + Path.Combine(fixture, "missing-device-leaf"),
                SystemOutcome("ok", "System.NullReferenceException", "System.ArgumentNullException", "System.ArgumentException", "System.IO.IOException", "System.UnauthorizedAccessException", "System.Security.SecurityException"), prefix + drive);
        }

        Add("normalize.absolute", "normalize", root, Ok(root));
        Add("normalize.trailing", "normalize", root + "\\\\//", Ok(root));
        Add("normalize.relative", "normalize", "relative\\..\\leaf", Ok(Path.Combine(cwd, "leaf")));
        Add("normalize.drive-root", "normalize", drive, Ok(drive.TrimEnd('\\', '/')));
        Add("normalize.drive-relative", "normalize", drive[..2] + "leaf", SystemOutcome("ok", "System.ArgumentException", "System.NotSupportedException"));
        Add("normalize.null", "normalize", null, Error("System.ArgumentNullException"));
        Add("normalize.empty", "normalize", "", Error("System.ArgumentException"));
        Add("normalize.spaces", "normalize", "   ", Error("System.ArgumentException"));
        Add("normalize.unicode-space", "normalize", "\u00a0", SystemOutcome("ok", "System.ArgumentException"));
        Add("normalize.nul", "normalize", root + "\\bad\0name", Error("System.ArgumentException"));
        Add("normalize.illegal-kept", "normalize", root + "\\bad|name", SystemOutcome("ok", "System.ArgumentException", "System.NotSupportedException"));
        Add("normalize.illegal-collapsed", "normalize", root + "\\bad|name\\..\\leaf", SystemOutcome("ok", "System.ArgumentException", "System.NotSupportedException"));
        Add("normalize.question-kept", "normalize", root + "\\bad?name", SystemOutcome("ok", "System.ArgumentException", "System.NotSupportedException"));
        Add("normalize.question-collapsed", "normalize", root + "\\bad?name\\..\\leaf", SystemOutcome("ok", "System.ArgumentException", "System.NotSupportedException"));
        Add("normalize.star-kept", "normalize", root + "\\bad*name", SystemOutcome("ok", "System.ArgumentException", "System.NotSupportedException"));
        Add("normalize.star-collapsed", "normalize", root + "\\bad*name\\..\\leaf", SystemOutcome("ok", "System.ArgumentException", "System.NotSupportedException"));
        Add("normalize.colon", "normalize", root + "\\bad:name", SystemOutcome("ok", "System.ArgumentException", "System.NotSupportedException"));
        Add("normalize.trailing-dot-space", "normalize", root + "\\tail. ", SystemOutcome("ok", "System.ArgumentException"));
        Add("normalize.trailing-tab", "normalize", root + "\\tail\t", SystemOutcome("ok", "System.ArgumentException"));
        Add("normalize.long", "normalize", root + "\\" + string.Join("\\", Enumerable.Repeat(new string('a', 70), 5)), SystemOutcome("ok", "System.IO.PathTooLongException"));
        Add("normalize.long-component", "normalize", root + "\\" + new string('a', 270), SystemOutcome("ok", "System.IO.PathTooLongException"));
        Add("normalize.extended-drive", "normalize", @"\\?\" + root, Ok(root));
        Add("normalize.device-dot", "normalize", @"\\.\" + root, SystemOutcome("ok", "System.ArgumentException", "System.NotSupportedException"));
        Add("normalize.device-nt", "normalize", @"\??\" + root, SystemOutcome("ok", "System.ArgumentException", "System.NotSupportedException"));
        Add("normalize.device-dot-nul", "normalize", @"\\.\" + root + "\\bad\0name", SystemOutcome("ok", "System.ArgumentException", "System.NotSupportedException"));
        Add("normalize.device-nt-nul", "normalize", @"\??\" + root + "\\bad\0name", SystemOutcome("ok", "System.ArgumentException", "System.NotSupportedException"));
        Add("normalize.unc-exact", "normalize", @"\\?\UNC\pcllf-path-oracle.invalid\share\leaf", Ok(@"\\pcllf-path-oracle.invalid\share\leaf"));
        Add("normalize.unc-lower", "normalize", @"\\?\unc\server\share", Ok(Path.Combine(cwd, "unc", "server", "share")));
        // Reference Source ec9 Path.TrimEndChars / LongPathHelper.s_trimEndChars.
        // The real CLR4 driver also records the runtime's private arrays as evidence.
        foreach (char trim in new[] { '\t', '\n', '\v', '\f', '\r', ' ', '\u0085', '\u00a0' })
            Add("normalize.trim-" + ((int)trim).ToString("x4", CultureInfo.InvariantCulture), "normalize", root + "\\tail" + trim, Ok(root + "\\tail"));
        Add("normalize.nontrim-2000", "normalize", root + "\\tail\u2000", SystemOutcome("ok", "System.ArgumentException"));
        Add("normalize.dot-tab", "normalize", root + "\\tail.\t", SystemOutcome("ok", "System.ArgumentException"));
        foreach (int size in new[] { 254, 255, 256 }) {
            string component = new('a', size);
            JObject expected = size <= 255 ? Ok(root + "\\" + component) : Error("System.IO.PathTooLongException");
            Add("normalize.component-final-" + size, "normalize", root + "\\" + component, expected);
            Add("normalize.component-middle-" + size, "normalize", root + "\\" + component + "\\leaf", size <= 255 ? Ok(root + "\\" + component + "\\leaf") : Error("System.IO.PathTooLongException"));
        }
        string TotalLengthPath(int length) {
            StringBuilder path = new(root);
            while (path.Length < length) {
                if (length - path.Length == 1) path.Append('a');
                else { path.Append('\\'); path.Append('a', Math.Min(200, length - path.Length)); }
            }
            return path.ToString(); // Every component stays <=201; no target IO.
        }
        string maximum = TotalLengthPath(32766), overMaximum = TotalLengthPath(32767);
        Add("normalize.total-32766", "normalize", maximum, Ok(maximum));
        Add("normalize.total-32767", "normalize", overMaximum, Error("System.IO.PathTooLongException"));
        Add("normalize.total-before-illegal", "normalize", overMaximum[..^1] + "|", Error("System.IO.PathTooLongException"));
        Add("normalize.leading-space-drive", "normalize", "  " + root, SystemOutcome("ok", "System.ArgumentException", "System.NotSupportedException"));
        Add("normalize.leading-space-rooted", "normalize", "  " + root[2..], SystemOutcome("ok", "System.ArgumentException", "System.NotSupportedException"));
        Add("normalize.leading-space-relative", "normalize", "  relative\\leaf", SystemOutcome("ok", "System.ArgumentException", "System.NotSupportedException"));
        // Normalize only: no Exists/GetAttributes call is permitted for these fake UNC paths.
        Add("normalize.unc-no-share", "normalize", @"\\pcllf-path-oracle.invalid", SystemOutcome("ok", "System.ArgumentException", "System.NotSupportedException"));
        Add("normalize.unc-no-share-slash", "normalize", @"\\pcllf-path-oracle.invalid\", SystemOutcome("ok", "System.ArgumentException", "System.NotSupportedException"));
        // Frame tests IsExtended before stripping leading root spaces. Observe that order.
        Add("normalize.leading-space-nt", "normalize", "  " + @"\??\" + root + "\\sub\\..\\leaf", SystemOutcome("ok", "System.ArgumentException", "System.NotSupportedException"));
        Add("normalize.leading-space-extended", "normalize", "  " + @"\\?\" + root + "\\sub\\..\\leaf", SystemOutcome("ok", "System.ArgumentException", "System.NotSupportedException"));
        string oversized = new('a', 256);
        Add("normalize.pipe-before-oversize", "normalize", root + "\\bad|name\\" + oversized, SystemOutcome("System.ArgumentException", "System.IO.PathTooLongException"));
        Add("normalize.pipe-after-oversize", "normalize", root + "\\" + oversized + "\\bad|name", SystemOutcome("System.ArgumentException", "System.IO.PathTooLongException"));
        Add("normalize.wildcard-before-oversize", "normalize", root + "\\bad?name\\" + oversized, SystemOutcome("System.ArgumentException", "System.IO.PathTooLongException"));
        Add("normalize.colon-after-oversize", "normalize", root + "\\" + oversized + "\\bad:name", SystemOutcome("System.NotSupportedException", "System.IO.PathTooLongException"));
        Add("dirname.nt-repeated-separators", "get-directory-name", @"\??\" + root + @"\\sub\\leaf", SystemOutcome("ok", "System.ArgumentException", "System.NotSupportedException"));
        Add("dirname.nt-nul", "get-directory-name", @"\??\" + root + "\\bad\0name", SystemOutcome("ok", "System.ArgumentException", "System.NotSupportedException"));
        Add("dirname.nt-root", "get-directory-name", @"\??\" + drive, SystemOutcome("ok", "System.ArgumentException", "System.NotSupportedException"));
        Add("dirname.dot-root", "get-directory-name", @"\\.\" + drive, SystemOutcome("ok", "System.ArgumentException", "System.NotSupportedException"));
        // GetDirectoryName uses Framework's limited normalization, whose component
        // check counts a preceding separator for middle components. FullPath's
        // successful 255-character component cannot serve as its expected result.
        foreach (int size in new[] { 254, 255 }) {
            string parent = root + "\\" + new string('a', size);
            Add("dirname.component-middle-" + size, "get-directory-name", parent + "\\leaf",
                size == 254 ? Ok(parent) : Error("System.IO.PathTooLongException"));
        }
        Add("dirname.component-final-255", "get-directory-name", root + "\\" + new string('a', 255), Ok(root));
        Add("dirname.total-32766", "get-directory-name", maximum, Ok(maximum[..maximum.LastIndexOf('\\')]));
        Add("dirname.total-32767", "get-directory-name", overMaximum, Error("System.IO.PathTooLongException"));
        Add("guard.limited-middle-255", "ensure", root + "\\" + new string('a', 255) + "\\missing-leaf", Error("System.IO.PathTooLongException"), root);
        Add("dirname.canonical-root", "get-directory-name", @"\\?\" + drive, Ok());
        Add("dirname.nt-nul-parent", "get-directory-name", @"\??\" + root + "\\dir\0name\\leaf", Ok(@"\??\" + root + "\\dir\0name"));
        Add("dirname.empty", "get-directory-name", "", Error("System.ArgumentException"));
        Add("dirname.unicode-space", "get-directory-name", "\u00a0", Error("System.ArgumentException"));
        Add("dirname.illegal-before-length", "get-directory-name", root + "\\bad|" + new string('a', 256) + "\\leaf", Error("System.ArgumentException"));
        Add("dirname.collapsed-illegal", "get-directory-name", root + @"\bad|name\..\leaf", Error("System.ArgumentException"));
        // Canonical extended roots have a different Framework root boundary from
        // dot-device roots. Observe it independently rather than applying their fix.
        Add("guard.leading-space-canonical-root", "ensure", "  " + @"\\?\" + Path.Combine(fixture, "missing-canonical-leaf"),
            SystemOutcome("ok", "System.NullReferenceException", "System.ArgumentNullException", "System.IO.IOException", "System.UnauthorizedAccessException", "System.Security.SecurityException"), "  " + @"\\?\" + drive);
        Add("prefix.drive", "prefix", @"\\?\C:\a", Ok(@"C:\a"));
        Add("prefix.unc", "prefix", @"\\?\UNC\server\share\a", Ok(@"\\server\share\a"));
        Add("prefix.unc-lower", "prefix", @"\\?\unc\server\share", Ok(@"unc\server\share"));
        Add("prefix.unc-mixed", "prefix", @"\\?\UnC\server\share", Ok(@"UnC\server\share"));
        Add("prefix.repeated-drive", "prefix", @"\\?\C:\one\\?\D:\two", Ok(@"D:\two"));
        Add("prefix.repeated-unc", "prefix", @"\\?\UNC\one\share\\?\UNC\two\share", Ok(@"\\two\share"));
        Add("prefix.ordinary-unc-word", "prefix", @"\\?\UNC\server\share\UNC\leaf", Ok(@"\\server\share\UNC\leaf"));
        Add("prefix.nt-unchanged", "prefix", @"\??\C:\leaf", Ok(@"\??\C:\leaf"));
        Add("prefix.dot-unchanged", "prefix", @"\\.\C:\leaf", Ok(@"\\.\C:\leaf"));
        Add("prefix.forward-unchanged", "prefix", "//?/C:/leaf", Ok("//?/C:/leaf"));
        Add("prefix.marker", "prefix", @"\\?\", Ok(""));
        Add("prefix.unc-marker", "prefix", @"\\?\UNC\", Ok(@"\\"));
        Add("prefix.null", "prefix", null, Ok());
        foreach ((string id, string first, string second) in new[] { ("ascii", "I", "i"), ("turkish", "\u0130", "i"), ("sigma", "Σ", "ς"), ("kelvin", "K", "K"), ("sharp-s", "ß", "SS"), ("deseret", "\U00010400", "\U00010428") })
            Add("ordinal." + id, "ordinal", first, new JObject(new JProperty("allowed", new JArray("ok"))), other: second, informational: true);
        foreach ((string id, string first, string second) in new[] {
            ("sigma-capital-small", "Σ", "σ"), ("sigma-small-final", "σ", "ς"),
            ("ascii-prefix", new string('a', 64) + "Σ", new string('A', 64) + "ς"),
            ("nul-difference", "A\0B", "a\0C"), ("nul-prefix", "A\0Btail", "a\0b"),
            ("high-surrogate", "\ud800A", "\ud800a"), ("low-surrogate", "\udc00A", "\udc00a") })
            Add("ordinal." + id, "ordinal", first, SystemOutcome("ok"), other: second, informational: true);
        Add("ordinal.long-s-shaders", "ordinal", "ſhaders/entry", SystemOutcome("ok"), other: "shaders/", informational: true);
        JObject ownedZipValue = new(new JProperty("entries", new JArray(new JObject(new JProperty("fullNameUtf16", new JArray("shaders/entry".Select(unit => (int)unit))), new JProperty("shaderPrefix", true)))));
        Add("zip.owned", "zip-open-read", ownedZip, Ok(ownedZipValue.DeepClone()));
        Add("zip.utf8-long-s", "zip-open-read", utf8Zip, SystemOutcome("ok"));
        Add("zip.utf8-no-efs-long-s", "zip-open-read", noEfsZip, SystemOutcome("ok"));
        Add("zip.pipe-collapsed", "zip-open-read", Path.Combine(fixture, "bad|segment", "..", "owned.zip"), Error("System.ArgumentException"));
        Add("zip.question-collapsed", "zip-open-read", Path.Combine(fixture, "bad?segment", "..", "owned.zip"), Ok(ownedZipValue.DeepClone()));
        Add("zip.pipe-kept", "zip-open-read", Path.Combine(fixture, "bad|name.zip"), Error("System.ArgumentException"));
        Add("zip.nbsp", "zip-open-read", "\u00a0", Error("System.ArgumentException"));
        Add("zip.unicode-space-2000", "zip-open-read", "\u2000", Error("System.ArgumentException"));
        Add("zip.empty", "zip-open-read", "", Error("System.ArgumentException"));
        Add("zip.null", "zip-open-read", null, Error("System.ArgumentNullException"));
        Add("zip.bad", "zip-open-read", badZip, Error("System.IO.InvalidDataException"));
        Add("zip.missing", "zip-open-read", Path.Combine(fixture, "missing.zip"), Error("System.IO.FileNotFoundException"));

        // Primitive Int64 text travels invariantly; only formatting runs under
        // the per-case culture. Unknown rounding/extrema use real CLR4 output,
        // never modern formatting as a generated baseline expectation.
        long[] sizeSamples = {
            long.MinValue, -1, 0, 1023, 1024, 1025, 1075, 1076, 1280, 1536,
            1048575, 1048576, 1048577, 1073741823, 1073741824, 1073741825,
            1099511627776, 1099511627777, 1610612736, long.MaxValue
        };
        foreach (string culture in new[] { "en-US", "fr-FR" }) {
            foreach (long size in sizeSamples) {
                string? expected = size switch {
                    long.MinValue => "-9223372036854775808 B",
                    -1 => "-1 B",
                    0 => "0 B",
                    1023 => "1023 B",
                    1024 or 1025 => "1 KB",
                    1536 => culture == "fr-FR" ? "1,5 KB" : "1.5 KB",
                    1610612736 => culture == "fr-FR" ? "1,5 GB" : "1.5 GB",
                    _ => null
                };
                string text = size.ToString(CultureInfo.InvariantCulture);
                Add("format-size." + culture + "." + text, "format-size", text, expected is null ? SystemOutcome("ok") : Ok(expected));
                ((JObject)cases[cases.Count - 1]!)["culture"] = culture;
            }
        }

        // These are the original ReadPackInfo JSON block only. The post-state
        // is numeric UTF16, so a truncated high surrogate cannot be replaced by
        // JSON/UTF8 serialization. The old host's IO/image work and catch stay out.
        string Quote(string value) => new JValue(value).ToString(Newtonsoft.Json.Formatting.None);
        string Description(string value) => "{\"pack\":{\"description\":" + Quote(value) + "}}";
        JObject MetadataOk(string? detail) { JObject result = Ok(); result["detailAfter"] = DetailUtf16(detail); return result; }
        JObject MetadataError(string type, string? detail) { JObject result = Error(type); result["detailAfter"] = DetailUtf16(detail); return result; }
        void Metadata(string id, string? value, JObject expected, string? initial = "before", string? culture = null) {
            Add("pack-metadata." + id, "pack-metadata", value, expected);
            JObject request = (JObject)cases[cases.Count - 1]!;
            request["initialDetail"] = initial;
            if (culture is not null) request["culture"] = culture;
        }
        Metadata("null", null, MetadataOk("before"));
        Metadata("null-initial-null", null, MetadataOk(null), initial: null);
        Metadata("null-initial-empty", null, MetadataOk(""), initial: "");
        Metadata("empty", "", MetadataError("Newtonsoft.Json.JsonReaderException", "before"));
        Metadata("whitespace", "  ", MetadataError("Newtonsoft.Json.JsonReaderException", "before"));
        Metadata("malformed", "{", MetadataError("Newtonsoft.Json.JsonReaderException", "before"));
        Metadata("root-array", "[]", MetadataError("Newtonsoft.Json.JsonReaderException", "before"));
        Metadata("root-null", "null", MetadataError("Newtonsoft.Json.JsonReaderException", "before"));
        Metadata("missing-pack", "{}", MetadataOk("before"));
        Metadata("pack-null", "{\"pack\":null}", MetadataError("System.InvalidOperationException", "before"));
        Metadata("pack-scalar", "{\"pack\":7}", MetadataError("System.InvalidOperationException", "before"));
        Metadata("pack-array", "{\"pack\":[]}", MetadataError("System.ArgumentException", "before"));
        Metadata("missing-description", "{\"pack\":{}}", MetadataOk("before"));
        Metadata("description-null", "{\"pack\":{\"description\":null}}", MetadataError("System.InvalidOperationException", "before"));
        Metadata("description-number", "{\"pack\":{\"description\":7}}", MetadataError("System.InvalidOperationException", "before"));
        Metadata("description-array", "{\"pack\":{\"description\":[]}}", MetadataError("System.ArgumentException", "before"));
        Metadata("description-no-text", "{\"pack\":{\"description\":{}}}", MetadataOk("before"));
        Metadata("description-text-null", "{\"pack\":{\"description\":{\"text\":null}}}", MetadataOk("before"));
        Metadata("description-string", Description("label"), MetadataOk("label"));
        Metadata("description-empty", Description(""), MetadataOk("before"));
        Metadata("description-text-bool", "{\"pack\":{\"description\":{\"text\":true}}}", MetadataOk("True"));
        Metadata("description-text-array", "{\"pack\":{\"description\":{\"text\":[1,2]}}}", SystemOutcome("ok"));
        Metadata("description-text-object", "{\"pack\":{\"description\":{\"text\":{\"a\":1}}}}", SystemOutcome("ok"));
        Metadata("format-null", "{\"pack\":{\"pack_format\":null}}", MetadataOk("before · 资源格式 "));
        Metadata("format-object", "{\"pack\":{\"description\":\"updated\",\"pack_format\":{\"x\":1}}}", SystemOutcome("ok"));
        Metadata("format-array", "{\"pack\":{\"description\":\"updated\",\"pack_format\":[1,2]}}", SystemOutcome("ok"));
        Metadata("format-initial-null", "{\"pack\":{\"pack_format\":47}}", MetadataOk(" · 资源格式 47"), initial: null);
        Metadata("blank-description-format", "{\"pack\":{\"description\":\" \\t\",\"pack_format\":47}}", MetadataOk("before · 资源格式 47"));
        foreach (int count in new[] { 179, 180, 181 })
            Metadata("length-" + count, Description(new string('a', count)), MetadataOk(new string('a', Math.Min(count, 180))));
        string pairBoundary = new string('a', 179) + "\U0001f642";
        Metadata("length-half-surrogate", Description(pairBoundary), MetadataOk(new string('a', 179) + '\ud83d'));
        foreach ((string id, string ending) in new[] {
            ("crlf", "\r\n"), ("lf", "\n"), ("cr", "\r"), ("ff", "\f"),
            ("nel", "\u0085"), ("ls", "\u2028"), ("ps", "\u2029"), ("vt", "\v") })
            Metadata("line-" + id, Description("left" + ending + "right"), MetadataOk(id == "vt" ? "left\vright" : "left right"));

        // Raw numeric tokens must never be parsed/re-emitted by modern preparation.
        // Scalar text/format output uses CurrentCulture; container rendering has
        // its own JSON rules, and ignored fields still pass through JObject.Parse.
        (string Id, string Token)[] numericTokens = {
            ("precision", "1.2345678901234567"), ("negative-zero", "-0.0"), ("negative-underflow", "-1e-999"),
            ("large", "1e308"), ("overflow", "1e400"), ("negative-overflow", "-1e400"),
            ("nan", "NaN"), ("infinity", "Infinity"), ("negative-infinity", "-Infinity"),
            ("int64-max", "9223372036854775807"), ("int64-min", "-9223372036854775808"),
            ("big-integer", "9223372036854775808"), ("negative-big-integer", "-9223372036854775809")
        };
        foreach (string culture in new[] { "en-US", "fr-FR" }) {
            foreach ((string id, string token) in numericTokens) {
                string metadata = "{\"pack\":{\"description\":{\"text\":" + token + "},\"pack_format\":" + token + "},\"ignored\":{\"nested\":" + token + "}}";
                Metadata("numeric-" + culture + "." + id, metadata, SystemOutcome("ok", "Newtonsoft.Json.JsonReaderException"), culture: culture);
            }
            foreach ((string id, string token) in new[] {
                ("precision", "1.2345678901234567"), ("negative-zero", "-0.0"), ("overflow", "1e400"), ("big-integer", "9223372036854775808") }) {
                string metadata = "{\"pack\":{\"description\":{\"text\":{\"outer\":[{\"n\":" + token + "}]}},\"pack_format\":[{\"n\":" + token + "}]},\"ignored\":" + token + "}";
                Metadata("container-" + culture + "." + id, metadata, SystemOutcome("ok", "Newtonsoft.Json.JsonReaderException"), culture: culture);
            }
            foreach ((string id, string token) in new[] {
                ("overflow", "1e400"), ("negative-overflow", "-1e400"), ("negative-underflow", "-1e-999"), ("nan", "NaN") }) {
                string metadata = "{\"pack\":{\"description\":\"stable\"},\"ignored\":{\"nested\":" + token + "}}";
                Metadata("ignored-" + culture + "." + id, metadata, SystemOutcome("ok", "Newtonsoft.Json.JsonReaderException"), culture: culture);
            }
            Metadata("date-text-" + culture, "{\"pack\":{\"description\":{\"text\":\"2024-01-02T03:04:05Z\"}}}", SystemOutcome("ok"), culture: culture);
            Metadata("date-format-" + culture, "{\"pack\":{\"description\":\"updated\",\"pack_format\":\"2024-01-02T03:04:05Z\"}}", SystemOutcome("ok"), culture: culture);
        }
        Metadata("date-ar-text", "{\"pack\":{\"description\":{\"text\":\"1700-01-01T00:00:00Z\"}}}", SystemOutcome("ok", "System.ArgumentOutOfRangeException"), culture: "ar-SA");
        Metadata("date-ar-partial-format", "{\"pack\":{\"description\":\"updated\",\"pack_format\":\"1700-01-01T00:00:00Z\"}}", SystemOutcome("ok", "System.ArgumentOutOfRangeException"), culture: "ar-SA");
        Metadata("date-ar-container", "{\"pack\":{\"description\":\"updated\",\"pack_format\":{\"date\":\"1700-01-01T00:00:00Z\"}}}", SystemOutcome("ok"), culture: "ar-SA");
        Metadata("date-ignored", "{\"pack\":{\"description\":\"stable\"},\"ignored\":\"1700-01-01T00:00:00Z\"}", MetadataOk("stable"), culture: "en-US");

        // Reader compatibility must survive line/buffer boundaries and reject
        // overflow even when a later property overwrites it. Probe finite/parser
        // and container round-trip edges separately; do not force container G15.
        Metadata("reader-crlf-overflow", "{\r\n/* Infinity is a comment */\r\n\"pack\":{\"description\":\"updated\"},\r\n\"ignored\":1e400\r\n}", MetadataError("Newtonsoft.Json.JsonReaderException", "before"));
        Metadata("reader-crlf-explicit-infinity", "{\r\n/* 1e400 is a comment */\r\n\"pack\":{\"description\":\"updated\",\"pack_format\":Infinity},\r\n\"ignored\":[-Infinity,NaN]\r\n}", SystemOutcome("ok"));
        Metadata("reader-buffer-overflow", "{\"padding\":\"" + new string('p', 5000) + "\",\"pack\":{\"description\":\"updated\"},\"ignored\":1e400}", MetadataError("Newtonsoft.Json.JsonReaderException", "before"));
        Metadata("reader-overwritten-overflow", "{\"pack\":{\"description\":\"updated\"},\"ignored\":1e400,\"ignored\":0}", MetadataError("Newtonsoft.Json.JsonReaderException", "before"));
        foreach ((string id, string token) in new[] {
            ("max-finite", "1.7976931348623157e308"), ("max-rounding", "1.7976931348623158e308"),
            ("max-overflow", "1.7976931348623159e308"), ("epsilon", "4.9406564584124654e-324"),
            ("normal-boundary", "2.2250738585072012e-308"), ("round-trip", "0.84551240822557006"),
            ("g15-midpoint", "282589933980712.5") }) {
            string metadata = "{\"pack\":{\"description\":{\"text\":" + token + "},\"pack_format\":{\"n\":" + token + "}}}";
            Metadata("reader-" + id, metadata, SystemOutcome("ok", "Newtonsoft.Json.JsonReaderException"), culture: "en-US");
        }

        // Separate primitive API probes expose parser bits and formatter output
        // independently. The old CLR4 APIs provide the observations; no modern
        // parser/formatter generates a baseline expectation or reformats input.
        string[] numberLexemes = {
            "0.0", "-0.0", "0e0", "-0e0", "1.0", "-1.0", "0.1",
            "4.9406564584124654e-324", "-4.9406564584124654e-324", "2.4703282292062327e-324",
            "2.2250738585072009e-308", "2.2250738585072012e-308", "2.2250738585072014e-308",
            "1.7976931348623157e308", "1.7976931348623158e308", "1.7976931348623159e308", "-1.7976931348623159e308",
            "1e400", "-1e400", "1e-999", "-1e-999",
            "0.84551240822557006", "282589933980712.5", "1.2345678901234567", "1e50",
            "1." + new string('2', 48) + "e50", "1." + new string('2', 49) + "e50", "1." + new string('2', 50) + "e50",
            "1.23" + new string('0', 60) + "e50", "1e9999", "1e10000", "1e-9999", "1e-10000", "-0.000e10000"
        };
        for (int index = 0; index < numberLexemes.Length; index++)
            Add("number-parse." + index.ToString("D2", CultureInfo.InvariantCulture), "number-parse", numberLexemes[index], SystemOutcome("ok"));

        // IEEE bits are supplied directly, so a parser difference cannot mask a
        // formatter difference. Cover signed zero, subnormal/normal neighbors,
        // finite extrema, 1.0 neighbors and decimal/exponent rounding boundaries.
        List<string> numberBits = new() {
            "0000000000000000", "8000000000000000", "0000000000000001", "8000000000000001",
            "0000000000000002", "000FFFFFFFFFFFFF", "0010000000000000", "0010000000000001", "8010000000000000",
            "7FEFFFFFFFFFFFFF", "7FEFFFFFFFFFFFFE", "FFEFFFFFFFFFFFFF",
            "3FF0000000000000", "3FEFFFFFFFFFFFFF", "3FF0000000000001", "BFF0000000000000",
            "3FB999999999999A", "3FEB0E7009B61CE0", "42F010398A1CC288",
            "3F1A36E2EB1C432D", "3EE4F8B588E368F1", "42D6BCC41E900000", "430C6BF526340000", "4341C37937E08000"
        };
        HashSet<string> seenNumberBits = new(numberBits, StringComparer.Ordinal);
        ulong numberState = 0x4C465F5041434B31UL;
        for (int added = 0; added < 12;) {
            numberState = unchecked(numberState * 6364136223846793005UL + 1442695040888963407UL);
            if ((numberState & 0x7FF0000000000000UL) == 0x7FF0000000000000UL) continue;
            string bits = numberState.ToString("X16", CultureInfo.InvariantCulture);
            if (!seenNumberBits.Add(bits)) continue;
            numberBits.Add(bits);
            added++;
        }
        foreach (string culture in new[] { "en-US", "fr-FR" }) {
            foreach (string bits in numberBits) {
                Add("number-format." + culture + "." + bits, "number-format", bits, SystemOutcome("ok"));
                ((JObject)cases[cases.Count - 1]!)["culture"] = culture;
            }
        }

        Save(Path.Combine(fixture, "cases.json"), new JObject(new JProperty("protocol", Protocol), new JProperty("fixture", fixture),
            new JProperty("cwd", cwd), new JProperty("profile", "LF-default"), new JProperty("lf", Lf), new JProperty("shared", Shared), new JProperty("cases", cases)));
        Console.WriteLine($"Prepared {cases.Count} path cases and {links.Count} real owned symbolic links.");
    }

    private static JObject Execute(JObject item) {
        JObject result;
        try {
            string? value = Text(item["value"]);
            switch ((string?)item["operation"]) {
                case "normalize": result = Ok(InstanceResourcePaths.Normalize(value!, PathUtils.RemoveExtendedPrefix)); break;
                case "prefix": result = Ok(PathUtils.RemoveExtendedPrefix(value!)); break;
                case "ensure": InstanceResourcePaths.EnsureWithin(Text(item["root"])!, value!, (bool)item["allowRoot"]!, PathUtils.RemoveExtendedPrefix); result = Ok(); break;
                case "get-directory-name": result = Ok(InstanceResourcePathCompatibility.GetDirectoryName(value)); break;
                case "format-size": result = Ok(FormatSize(item)); break;
                case "pack-metadata": result = ApplyPackMetadata(item); break;
                case "number-parse": result = Ok(ParseNumber(item)); break;
                case "number-format": result = Ok(FormatNumber(item)); break;
                case "zip-open-read":
                    using (ZipArchive archive = InstanceResourcePackageRules.OpenRead(value!)) result = Ok(ZipSnapshot(archive));
                    break;
                case "ordinal":
                    value = OrdinalText(item, "value");
                    string other = OrdinalText(item, "other");
                    result = Ok(InstanceResourcePathText.SamePathText(value, other));
                    result["reverseValue"] = InstanceResourcePathText.SamePathText(other, value);
                    result["startsWith"] = InstanceResourcePathText.StartsWithPathText(value, other);
                    result["reverseStartsWith"] = InstanceResourcePathText.StartsWithPathText(other, value);
                    break;
                default: throw new InvalidOperationException("Unknown operation in path protocol.");
            }
        } catch (Exception error) {
            result = Error(error.GetType().FullName!, error.Message);
            if (error.InnerException is System.ComponentModel.Win32Exception native) result["win32Error"] = native.NativeErrorCode;
        }
        result["id"] = item["id"]!.DeepClone();
        return result;
    }

    private static void CreateZipFixture(string path, string entryName) {
        using FileStream output = new(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using ZipArchive archive = new(output, ZipArchiveMode.Create, leaveOpen: false, entryNameEncoding: Encoding.UTF8);
        using Stream entry = archive.CreateEntry(entryName, CompressionLevel.NoCompression).Open();
    }

    private static string FormatSize(JObject item) {
        long size = long.Parse(Text(item["value"])!, NumberStyles.Integer, CultureInfo.InvariantCulture);
        CultureInfo previous = CultureInfo.CurrentCulture;
        try {
            string? culture = Text(item["culture"]);
            if (culture is not null) CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            return InstanceResourceDetailsRules.FormatSize(size);
        } finally { CultureInfo.CurrentCulture = previous; }
    }

    private static JObject ApplyPackMetadata(JObject item) {
        string detail = Text(item["initialDetail"])!;
        CultureInfo previous = CultureInfo.CurrentCulture;
        JObject result;
        try {
            string? culture = Text(item["culture"]);
            if (culture is not null) CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            InstanceResourceDetailsRules.ApplyPackMetadata(Text(item["value"]), ref detail,
                value => MeloongCore.Extensions.StringExtensions.ReplaceLineEndings(value, " "));
            result = Ok();
        } catch (Exception error) {
            result = Error(error.GetType().FullName!, error.Message);
        } finally { CultureInfo.CurrentCulture = previous; }
        result["detailAfter"] = DetailUtf16(detail);
        return result;
    }

    private static JToken DetailUtf16(string? value) => value is null ? JValue.CreateNull() : new JArray(value.Select(unit => (int)unit));

    private static JObject ParseNumber(JObject item) {
        bool parsed = LegacyPackMetadataNumberParser.TryParseDouble(Text(item["value"]), out double number);
        return new JObject(new JProperty("parsed", parsed),
            new JProperty("bits", BitConverter.DoubleToInt64Bits(number).ToString("X16", CultureInfo.InvariantCulture)));
    }

    private static JObject FormatNumber(JObject item) {
        if (!TryFiniteNumber(Text(item["value"]), out double number)) throw new InvalidDataException("Malformed finite IEEE number bits.");
        CultureInfo previous = CultureInfo.CurrentCulture;
        try {
            CultureInfo culture = CultureInfo.GetCultureInfo(Text(item["culture"])!);
            CultureInfo.CurrentCulture = culture;
            return new JObject(new JProperty("general", LegacyPackMetadataNumberFormatting.Format(number, 15, culture)),
                new JProperty("g15", LegacyPackMetadataNumberFormatting.Format(number, 15, culture)),
                new JProperty("g17", LegacyPackMetadataNumberFormatting.Format(number, 17, culture)),
                new JProperty("roundTrip", InstanceResourcePackMetadataCompatibility.FormatRoundTrip(number, culture)));
        } finally { CultureInfo.CurrentCulture = previous; }
    }

    private static bool HexNumberBits(string? value) => value is not null && Regex.IsMatch(value, @"\A[0-9A-F]{16}\z", RegexOptions.CultureInvariant);

    private static bool TryFiniteNumber(string? value, out double number) {
        number = 0;
        if (!HexNumberBits(value) || !ulong.TryParse(value, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ulong bits) ||
            (bits & 0x7FF0000000000000UL) == 0x7FF0000000000000UL) return false;
        number = BitConverter.Int64BitsToDouble(unchecked((long)bits));
        return true;
    }

    private static void ClearFixtureEfs(string source, string destination) {
        // This is a bounded edit of our one-entry, uncommented fixture. The stored
        // UTF8 name bytes and all ZIP data remain unchanged in both headers.
        byte[] data = File.ReadAllBytes(source);
        int end = data.Length - 22;
        if (end < 30 || BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan()) != 0x04034b50 ||
            BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(end)) != 0x06054b50 ||
            BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(end + 10)) != 1 ||
            BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(end + 20)) != 0)
            throw new InvalidDataException("Unexpected single-entry ZIP fixture layout.");
        int central = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(end + 16)));
        if (central < 30 || central + 46 > end || BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(central)) != 0x02014b50 ||
            BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(central + 42)) != 0)
            throw new InvalidDataException("Unexpected ZIP fixture central directory.");
        ushort localFlags = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(6));
        ushort centralFlags = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(central + 8));
        if ((localFlags & 0x0800) == 0 || (centralFlags & 0x0800) == 0) throw new InvalidDataException("UTF8 fixture was not written with EFS set in both headers.");
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(6), (ushort)(localFlags & ~0x0800));
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(central + 8), (ushort)(centralFlags & ~0x0800));
        File.WriteAllBytes(destination, data);
    }

    private static JObject ZipSnapshot(ZipArchive archive) {
        JArray entries = new();
        foreach (ZipArchiveEntry entry in archive.Entries) entries.Add(new JObject(new JProperty("fullNameUtf16", new JArray(entry.FullName.Select(unit => (int)unit))),
            new JProperty("shaderPrefix", InstanceResourcePathText.StartsWithPathText(entry.FullName, "shaders/"))));
        return new JObject(new JProperty("entries", entries));
    }

    private static string OrdinalText(JObject item, string name) {
        JArray units = item[name + "Utf16"] as JArray ?? throw new InvalidDataException("Missing UTF16 comparison request.");
        char[] text = new char[units.Count];
        for (int index = 0; index < text.Length; index++) {
            if (units[index].Type != JTokenType.Integer) throw new InvalidDataException("Non-integer UTF16 request unit.");
            text[index] = checked((char)(int)units[index]);
        }
        return new string(text);
    }

    private static JObject Switch(string name) {
        bool present = AppContext.TryGetSwitch(name, out bool value);
        return new JObject(new JProperty("present", present), new JProperty("value", value));
    }

    private static JObject Context() => new(new JProperty("runtime", RuntimeInformation.FrameworkDescription), new JProperty("clr", Environment.Version.ToString()),
        new JProperty("domainFramework", AppContext.TargetFrameworkName), new JProperty("assemblyFramework", Assembly.GetExecutingAssembly().GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName),
        new JProperty("cwd", Environment.CurrentDirectory), new JProperty("culture", CultureInfo.CurrentCulture.Name), new JProperty("is64Bit", Environment.Is64BitProcess), new JProperty("os", Environment.OSVersion.ToString()),
        new JProperty("defaultEncodingCodePage", Encoding.Default.CodePage), new JProperty("zipEntryCodePage", InstanceResourcePackageRules.EntryNameEncoding().CodePage),
        new JProperty("zipArchiveAssembly", typeof(ZipArchive).Assembly.FullName), new JProperty("zipFileAssembly", typeof(ZipFile).Assembly.FullName),
        new JProperty("useLegacyPathHandling", Switch("Switch.System.IO.UseLegacyPathHandling")), new JProperty("blockLongPaths", Switch("Switch.System.IO.BlockLongPaths")));

    private static int Compare(string casesPath, string baselinePath, string reportPath, string mode) {
        if (mode is not "Diagnostic" and not "RequireEquivalent") throw new ArgumentException("Unknown comparison mode.");
        JObject manifest = JObject.Parse(File.ReadAllText(casesPath, Utf8));
        JObject baseline = JObject.Parse(File.ReadAllText(baselinePath, Utf8));
        string fixture = FixtureRoot((string?)manifest["fixture"] ?? "");
        if ((string?)manifest["protocol"] != Protocol || (string?)baseline["protocol"] != Protocol || (string?)manifest["lf"] != Lf || (string?)manifest["shared"] != Shared ||
            (string?)baseline["lf"] != Lf || (string?)baseline["shared"] != Shared || (string?)manifest["profile"] != "LF-default")
            throw new InvalidDataException("Oracle protocol or fixed source mismatch.");
        string cwd = (string?)manifest["cwd"] ?? throw new InvalidDataException("Missing process cwd.");
        FrameworkName baselineFramework = new((string?)baseline["context"]?["domainFramework"] ?? throw new InvalidDataException("Missing CLR4 domain target."));
        if (!string.Equals(Environment.CurrentDirectory, cwd, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals((string?)baseline["context"]?["cwd"], cwd, StringComparison.OrdinalIgnoreCase) ||
            baselineFramework.Identifier != ".NETFramework" || baselineFramework.Version < new Version(4, 6, 2) ||
            (string?)baseline["context"]?["assemblyFramework"] != ".NETFramework,Version=v4.8" ||
            !((string?)baseline["context"]?["clr"] ?? "").StartsWith("4.", StringComparison.Ordinal) ||
            baseline["context"]?["is64Bit"]?.Type != JTokenType.Boolean || (bool?)baseline["context"]?["is64Bit"] != Environment.Is64BitProcess ||
            baseline["context"]?["useLegacyPathHandling"]?["present"]?.Type != JTokenType.Boolean || baseline["context"]?["useLegacyPathHandling"]?["value"]?.Type != JTokenType.Boolean || (bool?)baseline["context"]?["useLegacyPathHandling"]?["value"] != false ||
            baseline["context"]?["blockLongPaths"]?["present"]?.Type != JTokenType.Boolean || baseline["context"]?["blockLongPaths"]?["value"]?.Type != JTokenType.Boolean || (bool?)baseline["context"]?["blockLongPaths"]?["value"] != false)
            throw new InvalidDataException("The baseline was not executed as an independent LF-profile CLR4 process with matching cwd.");
        JArray cases = manifest["cases"] as JArray ?? throw new InvalidDataException("Missing cases.");
        JArray oldResults = baseline["results"] as JArray ?? throw new InvalidDataException("Missing baseline results.");
        if (cases.Count != oldResults.Count) throw new InvalidDataException("Baseline result count differs from request.");
        HashSet<string> ids = new(StringComparer.Ordinal);
        JArray modern = new(), differences = new(), informational = new(), failures = new(), nativeFailures = new();
        for (int index = 0; index < cases.Count; index++) {
            JObject item = cases[index] as JObject ?? throw new InvalidDataException("Invalid case object.");
            JObject old = oldResults[index] as JObject ?? throw new InvalidDataException("Invalid result object.");
            string id = (string?)item["id"] ?? throw new InvalidDataException("Missing case id.");
            if (!ids.Add(id) || (string?)old["id"] != id) throw new InvalidDataException("Duplicate or reordered oracle case id.");
            if ((string?)item["operation"] is not ("normalize" or "ensure" or "prefix" or "ordinal" or "get-directory-name" or "zip-open-read" or "format-size" or "pack-metadata" or "number-parse" or "number-format") || item["allowRoot"]?.Type != JTokenType.Boolean)
                throw new InvalidDataException("Unknown operation or malformed request.");
            if ((string?)item["operation"] == "format-size") {
                if (item["value"]?.Type != JTokenType.String || !long.TryParse(Text(item["value"]), NumberStyles.Integer, CultureInfo.InvariantCulture, out _) ||
                    item["culture"] is JToken culture && culture.Type is not (JTokenType.String or JTokenType.Null))
                    throw new InvalidDataException("Malformed invariant Int64 or culture in size request.");
                string? cultureName = Text(item["culture"]);
                if (cultureName is not null) CultureInfo.GetCultureInfo(cultureName);
            }
            bool metadataOperation = (string?)item["operation"] == "pack-metadata";
            if (metadataOperation) {
                if (item["value"]?.Type is not (JTokenType.String or JTokenType.Null) ||
                    item["initialDetail"]?.Type is not (JTokenType.String or JTokenType.Null) ||
                    item["culture"] is JToken culture && culture.Type is not (JTokenType.String or JTokenType.Null))
                    throw new InvalidDataException("Malformed metadata, initial detail or culture request.");
                string? cultureName = Text(item["culture"]);
                if (cultureName is not null) CultureInfo.GetCultureInfo(cultureName);
            }
            if ((string?)item["operation"] == "number-parse" &&
                (item["value"]?.Type != JTokenType.String || !Regex.IsMatch(Text(item["value"])!, @"\A-?(?:0|[1-9][0-9]*)(?:\.[0-9]+)?(?:[eE][+-]?[0-9]+)?\z", RegexOptions.CultureInvariant)))
                throw new InvalidDataException("Malformed raw JSON decimal number request.");
            if ((string?)item["operation"] == "number-format" &&
                (item["value"]?.Type != JTokenType.String || !TryFiniteNumber(Text(item["value"]), out _) ||
                    item["culture"]?.Type != JTokenType.String || Text(item["culture"]) is not ("en-US" or "fr-FR")))
                throw new InvalidDataException("Malformed finite IEEE bits or number-format culture request.");
            ValidateOutcome(old, (string)item["operation"]!);
            if ((string?)item["operation"] == "ordinal") {
                // Decode before executing either side, so malformed protocol cannot be
                // mistaken for a meaningful candidate/runtime difference in Diagnostic mode.
                OrdinalText(item, "value");
                OrdinalText(item, "other");
                if ((string?)old["status"] == "ok") {
                    JObject native = old["native"] as JObject ?? throw new InvalidDataException("Missing independent CLR4 Win32 probe.");
                    if ((string?)native["status"] == "exception") {
                        ValidateOutcome(native, "ordinal");
                        if (native["win32Error"] is JToken error && error.Type != JTokenType.Integer)
                            throw new InvalidDataException("Malformed native Win32 error code.");
                        nativeFailures.Add(new JObject(new JProperty("id", id), new JProperty("framework", old["status"]!.DeepClone()), new JProperty("win32", native.DeepClone())));
                    } else {
                        foreach (string field in new[] { "value", "reverseValue", "startsWith", "reverseStartsWith" }) {
                            if (native[field]?.Type != JTokenType.Boolean) throw new InvalidDataException("Malformed CLR4 native probe result.");
                            if (!JToken.DeepEquals(old[field], native[field])) nativeFailures.Add(new JObject(new JProperty("id", id), new JProperty("api", field),
                                new JProperty("framework", old[field]!.DeepClone()), new JProperty("win32", native[field]!.DeepClone())));
                        }
                    }
                }
            }
            JObject expected = (JObject?)item["expected"] ?? throw new InvalidDataException("Missing independent baseline contract.");
            bool contract;
            if (expected["allowed"] is JArray allowed) {
                if (allowed.Count == 0 || allowed.Any(value => value.Type != JTokenType.String)) throw new InvalidDataException("Malformed observation contract.");
                string outcome = (string?)old["status"] == "ok" ? "ok" : (string?)old["type"] ?? "invalid";
                contract = allowed.Any(value => (string?)value == outcome);
            } else contract = SameOutcome(expected, old, metadataOperation);
            if (!contract) failures.Add(new JObject(new JProperty("id", id), new JProperty("expected", expected.DeepClone()), new JProperty("actual", old.DeepClone())));
            JObject current = Execute(item);
            modern.Add(current);
            if (!SameOutcome(old, current, metadataOperation)) {
                JObject difference = new(new JProperty("id", id), new JProperty("baseline", old.DeepClone()), new JProperty("modern", current.DeepClone()));
                ((bool?)item["informational"] == true ? informational : differences).Add(difference);
            }
        }
        JObject report = new(new JProperty("protocol", Protocol), new JProperty("mode", mode), new JProperty("profile", manifest["profile"]!.DeepClone()),
            new JProperty("lf", Lf), new JProperty("shared", Shared), new JProperty("caseCount", cases.Count), new JProperty("linkCount", JArray.Parse(File.ReadAllText(Path.Combine(fixture, "links.json"), Utf8)).Count),
            new JProperty("nativeComparisonCount", cases.OfType<JObject>().Count(item => (string?)item["operation"] == "ordinal") * 4),
            new JProperty("zipApiProbeCount", cases.OfType<JObject>().Count(item => (string?)item["operation"] == "zip-open-read")),
            new JProperty("zipApiProbeScope", "Original CLR4 ZipFile.OpenRead plus entry snapshot; modern production InstanceResourcePackageRules.OpenRead plus entry snapshot. This is not the old ValidatePackage business function."),
            new JProperty("sizeFormattingProbeCount", cases.OfType<JObject>().Count(item => (string?)item["operation"] == "format-size")),
            new JProperty("sizeFormattingProbeScope", "Original fixed LF FormatSize method, injected verbatim into CLR4; modern production InstanceResourceDetailsRules.FormatSize. CurrentCulture is scoped and restored per request."),
            new JProperty("packMetadataProbeCount", cases.OfType<JObject>().Count(item => (string?)item["operation"] == "pack-metadata")),
            new JProperty("packMetadataProbeScope", "Only the original fixed LF ReadPackInfo JSON block, injected verbatim. The old filesystem/image work and outer catch are excluded; actual top-level exception and partial detail state are compared."),
            new JProperty("packMetadataLineEndingsHelper", "MeloongCore.Extensions.StringExtensions.ReplaceLineEndings(string,string,bool=false), real pinned Shared; explicit static binding in modern host."),
            new JProperty("numberParseProbeCount", cases.OfType<JObject>().Count(item => (string?)item["operation"] == "number-parse")),
            new JProperty("numberParseProbeScope", "Primitive CLR4 Double.TryParse(NumberStyles.Float, InvariantCulture) versus the scoped production parser; raw JSON decimal lexemes and exact IEEE result bits. These are API probes, not the original ReadPackInfo block."),
            new JProperty("numberFormatProbeCount", cases.OfType<JObject>().Count(item => (string?)item["operation"] == "number-format")),
            new JProperty("numberFormatProbeScope", "Primitive CLR4 Double.ToString()/G15/G17/R versus the scoped production formatter and round-trip selector; finite raw IEEE bits under en-US/fr-FR. These are API probes, not the original ReadPackInfo block."),
            new JProperty("requests", cases.DeepClone()),
            new JProperty("baseline", baseline), new JProperty("modern", new JObject(new JProperty("context", Context()), new JProperty("results", modern))),
            new JProperty("contractFailures", failures), new JProperty("nativeCandidateFailures", nativeFailures), new JProperty("differences", differences), new JProperty("informationalDifferences", informational),
            new JProperty("equivalent", failures.Count == 0 && nativeFailures.Count == 0 && differences.Count == 0 && informational.Count == 0));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(reportPath))!);
        Save(reportPath, report);
        Console.WriteLine($"Path oracle: {cases.Count} cases; {failures.Count} baseline contract failures; {nativeFailures.Count} rejected native comparisons; {differences.Count} path differences; {informational.Count} text differences (also strict failures). Mode={mode}.");
        if (failures.Count != 0) return 3;
        if (nativeFailures.Count != 0) return 4; // Diagnostic must not endorse a candidate contradicted by real CLR4.
        return mode == "RequireEquivalent" && (differences.Count != 0 || informational.Count != 0) ? 2 : 0;
    }

    private static void ValidateOutcome(JObject result, string operation) {
        if (operation == "pack-metadata") {
            JToken detail = result["detailAfter"] ?? throw new InvalidDataException("Missing metadata post-state.");
            if (detail.Type != JTokenType.Null && (detail is not JArray units || units.Any(unit => unit.Type != JTokenType.Integer || (long)unit < 0 || (long)unit > char.MaxValue)))
                throw new InvalidDataException("Malformed raw UTF16 metadata post-state.");
        }
        if ((string?)result["status"] == "ok") {
            if (result.Property("value") is null || result.Property("type") is not null) throw new InvalidDataException("Malformed successful baseline result.");
            JTokenType valueType = result["value"]!.Type;
            if (operation is "ensure" or "pack-metadata" ? valueType != JTokenType.Null : operation == "ordinal" ? valueType != JTokenType.Boolean : operation is "zip-open-read" or "number-parse" or "number-format" ? valueType != JTokenType.Object : operation == "format-size" ? valueType != JTokenType.String : valueType is not (JTokenType.String or JTokenType.Null))
                throw new InvalidDataException("Baseline returned a value incompatible with the requested operation.");
            if (operation == "number-parse") {
                JObject number = (JObject)result["value"]!;
                if (number.Properties().Count() != 2 || number["parsed"]?.Type != JTokenType.Boolean || number["bits"]?.Type != JTokenType.String || !HexNumberBits(Text(number["bits"])))
                    throw new InvalidDataException("Malformed primitive number parse result.");
            }
            if (operation == "number-format") {
                JObject number = (JObject)result["value"]!;
                if (number.Properties().Count() != 4 || new[] { "general", "g15", "g17", "roundTrip" }.Any(field => number[field]?.Type != JTokenType.String))
                    throw new InvalidDataException("Malformed primitive number format result.");
            }
            if (operation == "zip-open-read") {
                JArray entries = result["value"]?["entries"] as JArray ?? throw new InvalidDataException("Missing structured ZIP baseline entries.");
                foreach (JToken token in entries) {
                    if (token is not JObject entry || entry["shaderPrefix"]?.Type != JTokenType.Boolean || entry["fullNameUtf16"] is not JArray units ||
                        units.Any(unit => unit.Type != JTokenType.Integer || (long)unit < 0 || (long)unit > char.MaxValue))
                        throw new InvalidDataException("Malformed ZIP baseline entry snapshot.");
                }
            }
            if (operation == "ordinal" && new[] { "reverseValue", "startsWith", "reverseStartsWith" }.Any(field => result[field]?.Type != JTokenType.Boolean))
                throw new InvalidDataException("Missing real String reverse/prefix baseline comparisons.");
        } else if ((string?)result["status"] == "exception") {
            if (result["type"]?.Type != JTokenType.String || string.IsNullOrEmpty((string?)result["type"]) || result["message"]?.Type != JTokenType.String)
                throw new InvalidDataException("Malformed exceptional baseline result.");
        } else throw new InvalidDataException("Unknown baseline status.");
    }

    private static bool SameOutcome(JObject expected, JObject actual, bool compareDetail = false) {
        if (compareDetail && (expected.Property("detailAfter") is null || actual.Property("detailAfter") is null || !JToken.DeepEquals(expected["detailAfter"], actual["detailAfter"]))) return false;
        if ((string?)expected["status"] != (string?)actual["status"]) return false;
        if ((string?)expected["status"] == "ok") {
            if (!JToken.DeepEquals(expected["value"], actual["value"])) return false;
            foreach (string field in new[] { "reverseValue", "startsWith", "reverseStartsWith" })
                if (expected.Property(field) is not null && !JToken.DeepEquals(expected[field], actual[field])) return false;
            return true;
        }
        if ((string?)expected["type"] != (string?)actual["type"]) return false;
        string? message = (string?)expected["message"];
        return message is not Outside and not Link || message == (string?)actual["message"];
    }

    private static void Cleanup(string requestedRoot) {
        string fixture = FixtureRoot(requestedRoot);
        string manifest = Path.Combine(fixture, "links.json");
        if (File.Exists(manifest)) {
            foreach (JObject link in JArray.Parse(File.ReadAllText(manifest, Utf8)).OfType<JObject>().Reverse()) {
                string path = Path.GetFullPath((string)link["path"]!);
                if (!path.StartsWith(fixture + "\\", StringComparison.OrdinalIgnoreCase)) throw new IOException("Unsafe link cleanup path.");
                if ((bool)link["directory"]!) { try { Directory.Delete(path, false); } catch (DirectoryNotFoundException) { } }
                else File.Delete(path);
            }
        }
        // The shell removes the tree after this process exits: the running host's
        // own exe/dlls live below fixture and are locked by Windows while loaded.
    }
}
