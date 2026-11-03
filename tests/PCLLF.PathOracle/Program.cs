using System.Globalization;
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
// The old guard is compiled from git, not reimplemented in this driver.
internal static class Program {
    internal const string Protocol = "pcllf-resource-paths-v2";
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
            if ((string?)item["operation"] is not ("normalize" or "ensure" or "prefix" or "ordinal" or "get-directory-name") || item["allowRoot"]?.Type != JTokenType.Boolean)
                throw new InvalidDataException("Unknown operation or malformed request.");
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
            } else contract = SameOutcome(expected, old);
            if (!contract) failures.Add(new JObject(new JProperty("id", id), new JProperty("expected", expected.DeepClone()), new JProperty("actual", old.DeepClone())));
            JObject current = Execute(item);
            modern.Add(current);
            if (!SameOutcome(old, current)) {
                JObject difference = new(new JProperty("id", id), new JProperty("baseline", old.DeepClone()), new JProperty("modern", current.DeepClone()));
                ((bool?)item["informational"] == true ? informational : differences).Add(difference);
            }
        }
        JObject report = new(new JProperty("protocol", Protocol), new JProperty("mode", mode), new JProperty("profile", manifest["profile"]!.DeepClone()),
            new JProperty("lf", Lf), new JProperty("shared", Shared), new JProperty("caseCount", cases.Count), new JProperty("linkCount", JArray.Parse(File.ReadAllText(Path.Combine(fixture, "links.json"), Utf8)).Count),
            new JProperty("nativeComparisonCount", cases.OfType<JObject>().Count(item => (string?)item["operation"] == "ordinal") * 4),
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
        if ((string?)result["status"] == "ok") {
            if (result.Property("value") is null || result.Property("type") is not null) throw new InvalidDataException("Malformed successful baseline result.");
            JTokenType valueType = result["value"]!.Type;
            if (operation == "ensure" ? valueType != JTokenType.Null : operation == "ordinal" ? valueType != JTokenType.Boolean : valueType is not (JTokenType.String or JTokenType.Null))
                throw new InvalidDataException("Baseline returned a value incompatible with the requested operation.");
            if (operation == "ordinal" && new[] { "reverseValue", "startsWith", "reverseStartsWith" }.Any(field => result[field]?.Type != JTokenType.Boolean))
                throw new InvalidDataException("Missing real String reverse/prefix baseline comparisons.");
        } else if ((string?)result["status"] == "exception") {
            if (result["type"]?.Type != JTokenType.String || string.IsNullOrEmpty((string?)result["type"]) || result["message"]?.Type != JTokenType.String)
                throw new InvalidDataException("Malformed exceptional baseline result.");
        } else throw new InvalidDataException("Unknown baseline status.");
    }

    private static bool SameOutcome(JObject expected, JObject actual) {
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
