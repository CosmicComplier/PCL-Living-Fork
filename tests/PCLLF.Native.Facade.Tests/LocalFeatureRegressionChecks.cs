using System.Collections;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PCL;
using static PCLLF.Native.Facade.Tests.LocalFeatureChecks;

namespace PCLLF.Native.Facade.Tests;
internal static class LocalFeatureRegressionChecks
{
    private static void R(int line, bool value, string? detail = null) => Check("regression", line, value, detail);
    internal static void Run(Scope scope)
    {
        ModMinecraft.McInstance instance = Instance(scope, "test", "1.20.1");
        string version = instance.PathVersion, world = Path.Combine(version, "saves/Test world");
        Put(Path.Combine(world, "level.dat"), "world-fixture"); Put(Path.Combine(version, "options.txt"), "lang:zh_cn\n");
        IList rows = (IList)Bindings("ReadBindings", version, CancellationToken.None)!;
        R(22, rows.Count == 31); bool blank = rows.Count == 0;
        foreach (object row in rows) {
            R(24, row.GetType().FullName == "PCL.InstanceKeybinding");
            foreach (string name in new[] { "Key", "Name", "Owners", "Value", "Normalized", "SharedCount", "Saved" }) R(25, row.GetType().GetField(name, Native.Flags) is not null, name);
        }
        object session = Quick("Prepare", instance, null, new List<string>(), false)!;
        R(32, session is not null && session.GetType().FullName == "PCL.QuickPlaySession");
        foreach (string name in new[] { "Id", "VersionPath", "GameFolder", "LogFile" }) R(33, session!.GetType().GetField(name, Native.Flags) is not null, name);
        R(34, Regex.IsMatch((string)Native.Get(session!, "Id")!, "\\A[a-f0-9]{32}\\z"));
        Put((string)Native.Get(session!, "LogFile")!, "[{\"type\":\"singleplayer\",\"id\":\"Test world\",\"name\":\"Test world\",\"lastPlayedTime\":\"2026-10-24T00:00:00Z\",\"gamemode\":\"survival\"}]");
        Quick("Finish", session);
        IList recent = (IList)Quick("Recent")!; bool missing = recent.Count == 0;
        R(39, recent.Count == 1 && recent[0]!.GetType().FullName == "PCL.RecentWorld");
        foreach (string name in new[] { "VersionPath", "GameFolder", "Identifier", "Name", "PlayedUtc" }) R(40, recent[0]!.GetType().GetField(name, Native.Flags) is not null, name);
        DateTime played = (DateTime)Native.Get(recent[0]!, "PlayedUtc")!;
        R(42, played.Kind == DateTimeKind.Utc && played == new DateTime(2026, 10, 24, 0, 0, 0, DateTimeKind.Utc));
        object live = Quick("Prepare", instance, null, new List<string>(), false)!;
        string liveLog = (string)Native.Get(live, "LogFile")!, liveId = (string)Native.Get(live, "Id")!;
        string journalName = "quickplay-sessions/" + liveId + ".json", journalPath = (string)Data("Address", journalName)!;
        using (Process self = Process.GetCurrentProcess()) Quick("Track", live, self);
        Put(liveLog, "[{\"type\":\"singleplayer\",\"id\":\"Test world\",\"name\":\"Live recovered world\",\"lastPlayedTime\":\"2026-10-25T00:00:00Z\"}]"); Quick("RecoverExitedSessions");
        recent = (IList)Quick("Recent")!;
        R(52, recent.Count == 1 && (string)Native.Get(recent[0]!, "Name")! == "Live recovered world");
        R(53, File.Exists(liveLog) && File.Exists(journalPath) && (bool)Quick("HasRunningSessions")!);
        object enabled = Native.Get(Native.Entries["LaunchRecentWorlds"]!, "ValueCache")!;
        IDictionary cache = (IDictionary)enabled; object? beforeEnabled = cache[""];
        try { cache[""] = false; R(55, (bool)Quick("HasRunningSessions")!); }
        finally { cache[""] = beforeEnabled; }
        JObject journal = (JObject)Data("ReadJson", journalName)!; journal["pid"] = 2147483646; journal["started"] = 1L; Data("WriteJson", journalName, journal); Quick("RecoverExitedSessions");
        R(59, !File.Exists(liveLog) && !File.Exists(journalPath) && !(bool)Quick("HasRunningSessions")!);
        object bad = Quick("Prepare", instance, null, new List<string>(), false)!;
        using (Process self = Process.GetCurrentProcess()) Quick("Track", bad, self);
        string badLog = (string)Native.Get(bad, "LogFile")!, badJournal = (string)Data("Address", "quickplay-sessions/" + (string)Native.Get(bad, "Id")! + ".json")!;
        Put(badLog, "{"); Exception failed = Rejected(() => Quick("Finish", bad));
        R(66, failed is JsonReaderException && !File.Exists(badLog) && !File.Exists(badJournal), failed.ToString());
        R(68, !blank && !missing);
        R(70, rows.Cast<object>().Count(row => !(bool)Native.Get(row, "Saved")!) >= 20);
        Bindings("Save", version, "key_key.forward", null, "key.keyboard.r");
        R(72, File.ReadAllText(Path.Combine(version, "options.txt")).Contains("key_key.forward:key.keyboard.r", StringComparison.Ordinal));
        R(74, ((IList)Bindings("ReadBindings", version, CancellationToken.None)!).Count >= 20);
        File.Delete((string)Data("Address", "recent-worlds.json")!);
        WriteLevel(Path.Combine(world, "level.dat")); Quick("DiscoverExistingWorlds", instance); recent = (IList)Quick("Recent")!;
        R(85, recent.Count == 1 && (string)Native.Get(recent[0]!, "Name")! == "Saved display name");
        string corrupt = Path.Combine(version, "saves/Corrupt gzip/level.dat"); Put(corrupt, "not-a-gzip-fixture"); byte[] bytes = File.ReadAllBytes(corrupt);
        Exception rejected = Rejected(() => Quick("DiscoverExistingWorlds", instance));
        R(89, rejected is InvalidDataException && File.ReadAllBytes(corrupt).SequenceEqual(bytes), rejected.ToString());
        Console.WriteLine("PASS local-features regressions: original 31 defaults, actual Main inherited fields, Mojang id/UTC, live/exited/fault session cleanup, real NBT discovery and exact corrupt-gzip boundary.");
    }
    private static void WriteLevel(string file)
    {
        using FileStream stream = new(file, FileMode.Create, FileAccess.Write);
        using GZipStream gzip = new(stream, CompressionMode.Compress);
        static void Name(Stream output, string text) { byte[] bytes = Encoding.UTF8.GetBytes(text); output.WriteByte((byte)(bytes.Length >> 8)); output.WriteByte((byte)(bytes.Length & 255)); output.Write(bytes); }
        gzip.WriteByte(10); Name(gzip, ""); gzip.WriteByte(10); Name(gzip, "Data"); gzip.WriteByte(8); Name(gzip, "LevelName"); Name(gzip, "Saved display name");
        gzip.WriteByte(4); Name(gzip, "LastPlayed"); byte[] time = BitConverter.GetBytes(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()); if (BitConverter.IsLittleEndian) Array.Reverse(time); gzip.Write(time); gzip.WriteByte(0); gzip.WriteByte(0);
    }
}
