using System.Collections;
using System.Text;
using Newtonsoft.Json.Linq;
using PCL;

namespace PCLLF.Native.Facade.Tests;
// Complete original -Check All: genuine Main loaders/transactions/recovery and
// redirect transport, plus clearly-labelled same-checkout source/CLI workflow
// checks. The CLR4 fixed-source oracle remains independent and is not run here.
internal static partial class ReviewFixesChecks
{
    private static readonly HashSet<int> Seen = new();
    private static int assertions;
    private static void C(int line, bool value, string? detail = null)
    {
        assertions++; Seen.Add(line);
        Native.Check(value, "Test-ReviewFixes.ps1:" + line + " " + ReviewFixesOriginalChecks.Assertions[line] + " " + detail);
    }
    internal static void Run(FixtureEnvironment environment)
    {
        Seen.Clear(); assertions = 0;
        C(5, OperatingSystem.IsWindows() && Thread.CurrentThread.GetApartmentState() == ApartmentState.STA);
        string root = Path.Combine(environment.Root, "PclReviewFixes-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        FileTaskScope tasks = new(environment);
        Action restoreCaches = PreserveSettingCaches();
        try
        {
            // Reuse the same real flat/nested/hidden/pre-canceled loader cases
            // already owned by lifecycle; this wrapper has its own full thread,
            // worker.Stopped, lease and bounded UI quiescence before returning.
            LauncherLifecycleChecks.ReviewLocks(environment);
            Seen.UnionWith(new[] { 75, 78, 80, 81, 82, 85, 86, 89, 94, 99, 104, 107, 108, 109 });
            TransactionCancellation(Path.Combine(root, "transaction"));
            Recovery(tasks, root);
            Redirects();
            new ReviewFixesWorkflowChecks(C).Run();
            environment.AssertNoForbiddenIo();
        }
        finally
        {
            // Never restore global settings/roots while an actual producer can
            // still observe them. FileTaskScope fail-closes the isolated child
            // on timeout, including real Stopped/Task/Combo/list/UI tails.
            tasks.Dispose();
            restoreCaches();
            string full = Native.Full(root), prefix = Native.Full(Path.GetTempPath()) + Path.DirectorySeparatorChar;
            C(291, full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(full), "\\APclReviewFixes-[a-f0-9]{32}\\z"));
            if (Directory.Exists(full)) Directory.Delete(full, true);
        }
        Native.Check(Seen.SetEquals(ReviewFixesOriginalChecks.Assertions.Keys), "Review-All omitted an original observable source ID: " + string.Join(",", ReviewFixesOriginalChecks.Assertions.Keys.Except(Seen)));
        Console.WriteLine("PASS native review-fixes All: real Main lock/late-cancel/transaction-cancel/recovery/redirect behavior and all original source IDs. Workflow section is source/CLI acceptance only, not Main runtime; own assertions=" + assertions + ", original IDs=" + Seen.Count);
    }
    private static Action PreserveSettingCaches()
    {
        List<(IDictionary Cache, List<DictionaryEntry> Values)> saved = new();
        foreach (Settings.Setting entry in Settings.Entries.Values)
        {
            IDictionary cache = entry.ValueCache;
            List<DictionaryEntry> values = new(); foreach (DictionaryEntry value in cache) values.Add(value);
            saved.Add((cache, values));
        }
        return () =>
        {
            foreach (var (cache, values) in saved) { cache.Clear(); foreach (var value in values) cache[value.Key] = value.Value; }
        };
    }
    private static void Put(string path, string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, text, new UTF8Encoding(false));
    }
    private static string Digest(string path) => (string)Native.Static("PCL.ModpackUpdate", "Digest", path, "sha1")!;
    private static void TransactionCancellation(string folder)
    {
        string game = Path.Combine(folder, "game"), version = Path.Combine(folder, "version");
        Directory.CreateDirectory(game); Directory.CreateDirectory(version);
        List<Tuple<string, string, string>> operations = new();
        foreach (string name in new[] { "a.txt", "b.txt" })
        {
            string target = Path.Combine(game, name), source = Path.Combine(folder, "new-" + name);
            Put(target, "old-" + name); Put(source, "new-" + name); operations.Add(Tuple.Create(target, source, Digest(target)));
        }
        using CancellationTokenSource canceled = new();
        bool rejected = false, secondCommit = false;
        Action<int> atCommit = index => { if (index == 1) { secondCommit = true; canceled.Cancel(); } };
        try { Native.Static("PCL.ModpackUpdate", "ApplyTransaction", game, version, operations, canceled.Token, atCommit, null); }
        catch (OperationCanceledException) { rejected = true; }
        C(127, rejected && secondCommit);
        foreach (string name in new[] { "a.txt", "b.txt" }) C(128, File.ReadAllText(Path.Combine(game, name)) == "old-" + name, name);
        // Additional handle proof preserves the original real-file boundary.
        foreach (string name in new[] { "a.txt", "b.txt" }) { using FileStream held = new(Path.Combine(game, name), FileMode.Open, FileAccess.ReadWrite, FileShare.None); }
        Console.WriteLine("PASS review Transaction: actual Main cancellation during second replacement rolls back both original files and releases handles.");
    }
    private static void Recovery(FileTaskScope tasks, string root)
    {
        string game = Path.Combine(root, "game"), version = Path.Combine(game, "versions", "example"); tasks.SelectMinecraft(game);
        string json = Path.Combine(version, "example.json");
        const string old = """{"id":"example","clientVersion":"1.20.1","type":"release","mainClass":"net.minecraft.client.main.Main","libraries":[]}""";
        string next = old.Replace("1.20.1", "1.21.1", StringComparison.Ordinal);
        Put(json, old); Put(Path.Combine(version, "PCL", "Setup.ini"), "VersionArgumentIndieV2:True");
        string before = Digest(json); Put(json, next); string after = Digest(json);
        ModMinecraft.McInstance instance = new(version); FileTaskScope.CacheInstance(instance);
        C(222, instance.Version.VanillaName == "1.21.1");
        string box = Path.Combine(version, "PCL", "PackUpdateBackups", "interrupted"), backup = Path.Combine(box, "0.bak");
        Put(backup, old);
        Put(Path.Combine(box, "transaction.json"), new JObject
        {
            ["complete"] = false, ["gameRoot"] = instance.PathIndie,
            ["files"] = new JArray(new JObject { ["target"] = json, ["backup"] = backup, ["before"] = before, ["after"] = after })
        }.ToString());
        // Require the actual migrated Main flow hook; no fallback copies the old
        // cached McInstance and falsely treats disk rollback as runtime refresh.
        var refreshed = (ModMinecraft.McInstance)Native.Static("PCL.ModpackUpdateFlow", "RecoverInstance", instance)!;
        C(227, refreshed.Version.VanillaName == "1.20.1");
        C(228, refreshed.Version.VanillaName != "1.21.1");
        Console.WriteLine("PASS review Recovery: actual interrupted Main runtime rollback returns refreshed instance metadata before retry.");
    }
}
