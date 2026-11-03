using PCLCS;

internal static class InstanceResourcePathCompatibilityTests {
    internal static IEnumerable<(string Name, Action Run)> Cases() {
        yield return ("resource path compatibility resolves ordinary paths without creating them", OrdinaryPaths);
        yield return ("resource path compatibility retains null path API behavior", NullPaths);
        if (!OperatingSystem.IsWindows()) {
            yield return ("resource path compatibility leaves Unix path characters to native Path", NativeUnixPaths);
        } else {
            yield return ("resource Windows compatibility rejects raw NUL and Unicode whitespace", RawInput);
            yield return ("resource Windows compatibility rejects retained invalid characters and ADS", RetainedCharacters);
            yield return ("resource Windows compatibility permits bad segments removed by dot-dot", CollapsedCharacters);
            yield return ("resource Windows compatibility trims only the eight LF trailing characters", TrailingCharacters);
            yield return ("resource Windows compatibility limits components without adding MAX_PATH", ComponentLengths);
            yield return ("resource Windows compatibility keeps character length wildcard and colon error order", FailureOrder);
            yield return ("resource Windows compatibility returns exact extended paths before NUL checks", ExtendedPaths);
            yield return ("resource Windows compatibility skips leading spaces only before rooted input", LeadingSpaces);
        }
    }

    private static void OrdinaryPaths() {
        using Fixture fixture = new();
        string path = Path.Combine(fixture.Root, "missing", "..", "leaf");
        string expected = Path.Combine(fixture.Root, "leaf");
        Equal(expected, InstanceResourcePathCompatibility.GetFullPath(path));
        Equal(fixture.Root, InstanceResourcePathCompatibility.GetDirectoryName(expected));
        Equal(false, Directory.Exists(Path.Combine(fixture.Root, "missing")));
        Equal(false, File.Exists(expected));
        Equal(0, Directory.GetFileSystemEntries(fixture.Root).Length);
    }

    private static void NullPaths() {
        ArgumentNullException error = Throws<ArgumentNullException>(() => InstanceResourcePathCompatibility.GetFullPath(null!));
        Equal("path", error.ParamName);
        Equal<string?>(null, InstanceResourcePathCompatibility.GetDirectoryName(null));
    }

    private static void NativeUnixPaths() {
        using Fixture fixture = new();
        string path = Path.Combine(fixture.Root, "bad|?*:name\\tail\t");
        Equal(path, InstanceResourcePathCompatibility.GetFullPath(path));
        Equal(fixture.Root, InstanceResourcePathCompatibility.GetDirectoryName(path));
        Equal(Path.Combine(fixture.Root, "\u00a0"), InstanceResourcePathCompatibility.GetFullPath(Path.Combine(fixture.Root, "\u00a0")));
        Throws<ArgumentException>(() => InstanceResourcePathCompatibility.GetFullPath(path + '\0'));
        Equal(0, Directory.GetFileSystemEntries(fixture.Root).Length);
    }

    private static void RawInput() {
        using Fixture fixture = new();
        foreach (string path in new[] { "", " ", "\t", "\r\n", "\u00a0", "\u2000" })
            Throws<ArgumentException>(() => InstanceResourcePathCompatibility.GetFullPath(path));
        Throws<ArgumentException>(() => InstanceResourcePathCompatibility.GetFullPath(Path.Combine(fixture.Root, "bad\0name")));
        Throws<ArgumentException>(() => InstanceResourcePathCompatibility.GetFullPath("\0\u00a0"));
        Equal(0, Directory.GetFileSystemEntries(fixture.Root).Length);
    }

    private static void RetainedCharacters() {
        using Fixture fixture = new();
        foreach (char value in new[] { '|', '?', '*', '"', '<', '>', '\u0001', '\t' })
            Throws<ArgumentException>(() => InstanceResourcePathCompatibility.GetFullPath(Path.Combine(fixture.Root, "bad" + value + "name")));
        Throws<NotSupportedException>(() => InstanceResourcePathCompatibility.GetFullPath(Path.Combine(fixture.Root, "bad:name")));
        Equal(0, Directory.GetFileSystemEntries(fixture.Root).Length);
    }

    private static void CollapsedCharacters() {
        using Fixture fixture = new();
        foreach (char value in new[] { '|', '?', '*' }) {
            string path = Path.Combine(fixture.Root, "bad" + value + "name", "..", "leaf");
            Equal(Path.Combine(fixture.Root, "leaf"), InstanceResourcePathCompatibility.GetFullPath(path));
        }
        Equal(0, Directory.GetFileSystemEntries(fixture.Root).Length);
    }

    private static void TrailingCharacters() {
        using Fixture fixture = new();
        string path = Path.Combine(fixture.Root, "tail");
        // The original Windows report directly confirms tab. The full set and dot/tab ordering
        // come from the pinned source contracts and remain subject to the expanded CLR4 oracle.
        foreach (char value in new[] { '\t', '\n', '\v', '\f', '\r', ' ', '\u0085', '\u00a0' })
            Equal(path, InstanceResourcePathCompatibility.GetFullPath(path + value));
        Equal(path + ".", InstanceResourcePathCompatibility.GetFullPath(path + ".\t"));
        Equal(path + "\u2000", InstanceResourcePathCompatibility.GetFullPath(path + "\u2000"));
        Equal(path, InstanceResourcePathCompatibility.GetFullPath(path + "\t\u00a0\r"));
        Equal(0, Directory.GetFileSystemEntries(fixture.Root).Length);
    }

    private static void ComponentLengths() {
        using Fixture fixture = new();
        foreach (int length in new[] { 254, 255 }) {
            string path = Path.Combine(fixture.Root, new string('a', length));
            Equal(path, InstanceResourcePathCompatibility.GetFullPath(path));
        }
        foreach (int length in new[] { 256, 270 })
            Throws<PathTooLongException>(() => InstanceResourcePathCompatibility.GetFullPath(Path.Combine(fixture.Root, new string('a', length))));
        string longPath = Path.Combine(fixture.Root, string.Join("\\", Enumerable.Repeat(new string('a', 70), 5)));
        Equal(true, longPath.Length > 260);
        Equal(longPath, InstanceResourcePathCompatibility.GetFullPath(longPath));
        Equal(0, Directory.GetFileSystemEntries(fixture.Root).Length);
    }

    private static void FailureOrder() {
        using Fixture fixture = new();
        // These combined errors pin the source-defined order, rather than claiming that
        // the first 75-case report already executed every combination.
        string longComponent = new('a', 256);
        Throws<PathTooLongException>(() => InstanceResourcePathCompatibility.GetFullPath(Path.Combine(fixture.Root, longComponent, "bad|name")));
        Throws<ArgumentException>(() => InstanceResourcePathCompatibility.GetFullPath(Path.Combine(fixture.Root, "bad|name", longComponent)));
        Throws<ArgumentException>(() => InstanceResourcePathCompatibility.GetFullPath(Path.Combine(fixture.Root, longComponent + "|")));
        Throws<PathTooLongException>(() => InstanceResourcePathCompatibility.GetFullPath(Path.Combine(fixture.Root, longComponent + "?")));
        Throws<PathTooLongException>(() => InstanceResourcePathCompatibility.GetFullPath(Path.Combine(fixture.Root, longComponent + ":stream")));
        Throws<ArgumentException>(() => InstanceResourcePathCompatibility.GetFullPath(Path.Combine(fixture.Root, "bad:name?")));
        Equal(0, Directory.GetFileSystemEntries(fixture.Root).Length);
    }

    private static void ExtendedPaths() {
        using Fixture fixture = new();
        string nt = @"\??\" + Path.Combine(fixture.Root, "bad\0name");
        Equal(nt, InstanceResourcePathCompatibility.GetFullPath(nt));
        string canonical = @"\\?\" + Path.Combine(fixture.Root, "bad\0name");
        Equal(canonical, InstanceResourcePathCompatibility.GetFullPath(canonical));
        string longNt = @"\??\" + Path.Combine(fixture.Root, new string('a', 270)) + "\t";
        Equal(longNt, InstanceResourcePathCompatibility.GetFullPath(longNt));
        Throws<ArgumentException>(() => InstanceResourcePathCompatibility.GetFullPath(@"\\.\" + Path.Combine(fixture.Root, "bad\0name")));
        Equal(0, Directory.GetFileSystemEntries(fixture.Root).Length);
    }

    private static void LeadingSpaces() {
        using Fixture fixture = new();
        string absolute = Path.Combine(fixture.Root, "leaf");
        Equal(absolute, InstanceResourcePathCompatibility.GetFullPath("   " + absolute));
        string rooted = "\\PCLLF-ResourcePath-" + Guid.NewGuid().ToString("N");
        Equal(Path.GetPathRoot(Environment.CurrentDirectory) + rooted[1..], InstanceResourcePathCompatibility.GetFullPath("   " + rooted));
        const string relative = " leading relative name";
        Equal(Path.Combine(Environment.CurrentDirectory, relative), InstanceResourcePathCompatibility.GetFullPath(relative));
        Equal(0, Directory.GetFileSystemEntries(fixture.Root).Length);
    }

    private static T Throws<T>(Action run) where T : Exception {
        try { run(); }
        catch (Exception error) {
            Equal(typeof(T), error.GetType());
            return (T) error;
        }
        throw new Exception("Expected " + typeof(T).Name);
    }

    private static void Equal<T>(T expected, T actual) {
        if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected [{expected}], got [{actual}].");
    }

    private sealed class Fixture : IDisposable {
        internal string Root { get; } = Path.Combine(Path.GetTempPath(), "PCLLF-PathCompatibility-" + Guid.NewGuid().ToString("N"));
        internal Fixture() => Directory.CreateDirectory(Root);
        public void Dispose() => Directory.Delete(Root, true);
    }
}
