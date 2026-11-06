using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using PCLCS;

internal static class WorldLastPlayedSortTests {
    internal static IEnumerable<(string Name, Action Run)> Cases() {
        yield return ("world sorting uses level.dat LastPlayed instead of directory modification time", LastPlayedSource);
        yield return ("world sorting keeps damaged missing and unsupported metadata as unknown", UnknownMetadata);
        yield return ("world sorting rereads changed metadata without retaining file handles or modifying bytes", RefreshAndOwnership);
        yield return ("world sorting keeps unknown timestamps last in either direction and ties stable", UnknownAndTies);
        yield return ("world timestamp scanning is limited to worlds and propagates cancellation", ScopeAndCancellation);
        if (!OperatingSystem.IsWindows())
            yield return ("world timestamp scanning does not follow metadata file links", MetadataLink);
    }

    private static void LastPlayedSource() {
        using Fixture f = new();
        string recent = f.World("A-recent", 1_701_000_000_000), old = f.World("Z-old", 1_700_000_000_000);
        Directory.SetLastWriteTimeUtc(recent, new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        Directory.SetLastWriteTimeUtc(old, new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var entries = f.Scan();
        Require(entries[0].Name == "Z-old", "The fixture must put directory mtime in the opposite order to LastPlayed.");
        Require(entries.Single(item => item.Name == "A-recent").LastPlayedUtc == DateTimeOffset.FromUnixTimeMilliseconds(1_701_000_000_000).UtcDateTime, "The production scanner did not preserve the exact UTC LastPlayed value.");
        Require(entries.Single(item => item.Name == "A-recent").Description!.EndsWith("最后游玩：" + DateTimeOffset.FromUnixTimeMilliseconds(1_701_000_000_000).LocalDateTime.ToString("yyyy-MM-dd HH:mm"), StringComparison.Ordinal), "The displayed world date does not match the actual LastPlayed sorting key.");
        Sequence(["A-recent", "Z-old"], Sort(entries, true));
        Sequence(["Z-old", "A-recent"], Sort(entries, false));
    }

    private static void UnknownMetadata() {
        using Fixture f = new();
        f.World("valid", 1_700_000_000_000);
        f.World("no-tag", null);
        f.World("zero", 0);
        f.World("negative", -1);
        f.World("out-of-range", long.MaxValue);
        string broken = f.Directory("broken"); File.WriteAllBytes(Path.Combine(broken, "level.dat"), [1, 2, 3]);
        string truncated = f.Directory("truncated"); f.Compressed(truncated, [10, 0, 0, 10, 0, 4, 68, 97, 116, 97, 4]);
        f.Directory("missing");
        string tooLarge = f.Directory("too-large");
        using (FileStream oversized = File.Create(Path.Combine(tooLarge, "level.dat"))) oversized.SetLength(8L * 1024 * 1024 + 1);
        var entries = f.Scan();
        Require(entries.Count == 9, "Unknown metadata hid one or more local worlds.");
        Require(entries.Where(item => item.Name != "valid").All(item => item.LastPlayedUtc is null), "A filesystem timestamp or invalid LastPlayed was treated as a playing time.");
        Require(entries.Where(item => item.Name != "valid").All(item => item.Description!.EndsWith("最后游玩：未知", StringComparison.Ordinal)), "Unknown playing times must be labelled rather than replaced with directory dates.");
        Require(entries.Single(item => item.Name == "missing").Description!.Contains("未找到 level.dat"), "Missing metadata lost its existing explanation.");
        Require(Sort(entries, true)[0] == "valid" && Sort(entries, false)[0] == "valid", "Known playing times must precede unknown values in both directions.");
    }

    private static void RefreshAndOwnership() {
        using Fixture f = new();
        string world = f.World("changing", 1_700_000_000_000);
        string file = Path.Combine(world, "level.dat"); byte[] before = File.ReadAllBytes(file);
        var first = f.Scan().Single();
        Require(File.ReadAllBytes(file).SequenceEqual(before), "Reading the playing time modified level.dat.");
        using (FileStream exclusive = new(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        f.WriteWorld(world, 1_701_000_000_000);
        var second = f.Scan().Single();
        Require(second.LastPlayedUtc > first.LastPlayedUtc, "Refreshing a world list kept stale LastPlayed metadata.");
        Require(first.LastPlayedUtc == DateTimeOffset.FromUnixTimeMilliseconds(1_700_000_000_000).UtcDateTime, "Rescanning mutated the previous entry snapshot.");
        using (FileStream exclusive = new(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
    }

    private static void UnknownAndTies() {
        using Fixture f = new();
        f.World("B", 1_700_000_000_000); f.World("A", 1_700_000_000_000); f.Directory("unknown");
        var entries = f.Scan();
        Sequence(["A", "B", "unknown"], Sort(entries, true));
        Sequence(["A", "B", "unknown"], Sort(entries, false));
        string[] original = entries.Select(item => item.Name!).ToArray();
        Sort(entries, true);
        Sequence(original, entries.Select(item => item.Name!).ToArray());
    }

    private static void ScopeAndCancellation() {
        using Fixture f = new();
        string world = f.World("world", 1_700_000_000_000);
        Require(f.Scan(kind: 3).Single().LastPlayedUtc is null, "A shader/resource folder was interpreted as a Minecraft world.");
        using CancellationTokenSource token = new();
        try {
            f.Scan(token: token.Token, factory: () => { token.Cancel(); return new InstanceResourceEntryData(); });
        } catch (OperationCanceledException error) {
            Require(error.CancellationToken == token.Token, "Metadata reading replaced the caller's cancellation token.");
            using FileStream exclusive = new(Path.Combine(world, "level.dat"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            return;
        }
        throw new Exception("Metadata scanning swallowed cancellation.");
    }

    private static void MetadataLink() {
        using Fixture f = new();
        string target = f.World(".pcl-external", 1_701_000_000_000), world = f.Directory("linked-metadata");
        File.CreateSymbolicLink(Path.Combine(world, "level.dat"), Path.Combine(target, "level.dat"));
        var entry = f.Scan().Single();
        Require(entry.Name == "linked-metadata" && entry.LastPlayedUtc is null, "A metadata link supplied a playing time from outside the world.");
        Require(File.Exists(Path.Combine(target, "level.dat")), "Reading a metadata link changed its target.");
    }

    private static string[] Sort(IEnumerable<InstanceResourceEntryData> source, bool newestFirst) =>
        LocalResourceSort.ByTime(source, newestFirst, item => item.LastPlayedUtc, item => item.Name!, item => item.FullPath!).Select(item => item.Name!).ToArray();
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Sequence(IEnumerable<string> expected, IEnumerable<string> actual) => Require(expected.SequenceEqual(actual), "Unexpected world order: " + string.Join(", ", actual));

    private sealed class Fixture : IDisposable {
        private readonly string root = Path.Combine(Path.GetTempPath(), "PclLfWorldSort-" + Guid.NewGuid().ToString("N"));
        internal Fixture() => System.IO.Directory.CreateDirectory(root);
        internal string Directory(string name) { string path = Path.Combine(root, name); System.IO.Directory.CreateDirectory(path); return path; }
        internal string World(string name, long? time) { string path = Directory(name); WriteWorld(path, time); return path; }
        internal void WriteWorld(string path, long? time) {
            using MemoryStream nbt = new();
            nbt.WriteByte(10); Name(""); nbt.WriteByte(10); Name("Data");
            nbt.WriteByte(8); Name("LevelName"); Name(Path.GetFileName(path));
            if (time.HasValue) {
                nbt.WriteByte(4); Name("LastPlayed");
                Span<byte> number = stackalloc byte[8]; BinaryPrimitives.WriteInt64BigEndian(number, time.Value); nbt.Write(number);
            }
            nbt.WriteByte(0); nbt.WriteByte(0); Compressed(path, nbt.ToArray());
            void Name(string value) {
                byte[] bytes = Encoding.UTF8.GetBytes(value);
                Span<byte> length = stackalloc byte[2]; BinaryPrimitives.WriteInt16BigEndian(length, checked((short)bytes.Length));
                nbt.Write(length); nbt.Write(bytes);
            }
        }
        internal void Compressed(string path, byte[] value) {
            using FileStream output = File.Create(Path.Combine(path, "level.dat"));
            using GZipStream gzip = new(output, CompressionMode.Compress); gzip.Write(value);
        }
        internal List<InstanceResourceEntryData> Scan(int kind = 0, CancellationToken token = default, Func<InstanceResourceEntryData>? factory = null) =>
            InstanceResourceCatalog.Scan(root, root, kind, token, "downloads", factory ?? (() => new()), (_, _, _) => { }, (_, _, _) => { }, text => text, path => path);
        public void Dispose() => System.IO.Directory.Delete(root, true);
    }
}
