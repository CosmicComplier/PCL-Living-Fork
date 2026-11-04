using System.Collections;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using PCL;
using PCLCS;

namespace PCLLF.Native.Facade.Tests;
// Original LF scripts/Test-InstanceClone.ps1. Source line numbers identify its
// observable assertions; real Main DTOs/engines/transactions perform all work.
internal static class CloneNativeChecks
{
    internal static void Run(FixtureEnvironment environment)
    {
        using CloneFixture f = new(environment);
        Clone(f);
        Type node = Native.Type("PCL.InstanceCloneGroupNode"), set = Native.Type("PCL.InstanceCloneGroupSet");
        Native.Check(node.Assembly == Native.Main && set.Assembly == Native.Main, "Clone group DTOs must be the actual Main types.");
        Native.Call(typeof(CloneGroupChecks<, >).MakeGenericType(node, set), "Run", null, f);
        f.RequireCoverage("Test-InstanceClone.ps1", new[] { 62, 63, 65, 66, 68, 70, 71, 72, 74, 76, 77, 78, 79, 80, 82, 84, 86, 88, 92, 94, 100, 106, 107, 108, 111, 113, 115, 118, 119, 120, 121, 123, 124, 125, 128, 130, 131, 136, 139, 141, 144 });
        f.RequireCoverage("Test-InstanceCloneGroups.ps1", new[] { 38, 39, 40, 41, 43, 49, 50, 52, 53, 54, 56, 58, 59, 72, 74, 75, 76, 79, 81, 84, 86, 89, 91, 93, 95, 99, 101, 104, 111, 113, 115, 117, 119, 121, 123, 125, 129, 132, 135, 138, 146, 147, 150, 151, 152, 153, 154, 155 });
        environment.AssertNoForbiddenIo();
        Console.WriteLine("PASS native instance-clone: actual Main clone/migration/rollback/history and native clone-page/group-selection contract; " + f.Assertions + " assertions with original script IDs.");
    }

    private static void Clone(CloneFixture f)
    {
        const string script = "Test-InstanceClone.ps1";
        void C(int line, bool value, string message) => f.Check(script, line, value, message);
        void Reject(int line, Action work, string message) => f.Reject(script, line, work, message);
        object? E(string name, params object? [] args) => Native.Static("PCL.InstanceCloner", name, args);
        object? H(string name, params object? [] args) => Native.Static("PCL.EnvironmentHistory", name, args);
        object Options(bool saves = false, bool screenshots = false, bool servers = false)
        {
            object result = Activator.CreateInstance(Native.Type("PCL.InstanceCloneOptions"), true)!;
            InstanceCloneOptionsData data = (InstanceCloneOptionsData)result;
            data.CopySaves = saves;
            data.CopyScreenshots = screenshots;
            data.CopyServers = servers;
            return result;
        }

        string mc = f.Folder("engine/minecraft"), source = Path.Combine(mc, "versions", "original");
        f.SelectMinecraft(mc);
        CloneFixture.Put(Path.Combine(source, "original.json"), """{"id":"original","inheritsFrom":"1.20.1","jar":"original","type":"release","mainClass":"net.minecraft.client.main.Main","libraries":[],"arguments":{"game":[],"jvm":[]}}""");
        foreach (string version in new[]
        {
            "1.20.1",
            "1.21.1"
        }

        )
            CloneFixture.Put(Path.Combine(mc, "versions", version, version + ".json"), new JObject { ["id"] = version, ["type"] = "release", ["mainClass"] = "net.minecraft.client.main.Main", ["libraries"] = new JArray() }.ToString());
        var files = new Dictionary<string, string>
        {
            ["original.jar"] = "fixture-client",
            ["PCL/Setup.ini"] = "VersionArgumentIndieV2:True\r\nVersionRamCustom:23\r\nCacheMsAccess:fixture-only-private\r\nVersionAdvanceRun:fixture-command",
            ["PCL/config.json"] = """{"MinecraftInstallManifest":{"fixture":"old"},"AccountToken":"fixture-only-private"}""",
            ["mods/example-1.0.jar"] = "old-mod",
            ["mods/off.jar.disabled"] = "off-mod",
            ["config/example.toml"] = "fixture-setting=old",
            ["options.txt"] = "key_key.jump:key.keyboard.space",
            ["resourcepacks/example.zip"] = "resource-pack",
            ["shaderpacks/example.zip"] = "shader-pack",
            ["saves/world/level.dat"] = "original-world",
            ["screenshots/fixture.png"] = "original-screenshot",
            ["servers.dat"] = "original-servers",
            ["servers.dat_old"] = "original-servers-old",
            ["logs/latest.log"] = "skip-log",
            ["original-natives/native.dll"] = "skip-native",
            ["PCL/PackUpdateBackups/old.bak"] = "skip-backup",
            ["launcher_accounts.json"] = """{"fixture":"never-copy"}""",
            ["config/accounts.json"] = """{"fixture":"never-copy"}""",
            ["mods/.connector/cache.jar"] = "skip-cache",
            ["config/xaero/waypoints.txt"] = "original-waypoint"
        };
        foreach (var item in files)
            CloneFixture.Put(Path.Combine(source, item.Key), item.Value);
        string[] extra = ["coremods", "configureddefaults", "global_packs", "openloader"];
        foreach (string directory in extra)
            CloneFixture.Put(Path.Combine(source, directory, "review.txt"), "original-" + directory);
        Dictionary<string, string> before = CloneFixture.Snapshot(source);
        InstanceCloneResultData clone = (InstanceCloneResultData)E("Clone", source, source, "test-minimal", Options(), CancellationToken.None, null)!;
        string dest = clone.VersionFolder!.TrimEnd('\\');
        object emitted = Native.Static("PCL.InstanceCloneGroups", "Build", new[] { source, dest }, CancellationToken.None)!;
        IDictionary emittedPaths = (IDictionary)Native.Get(emitted, "ByPath")!;
        f.Check("LF actual clone/group boundary", 1, emittedPaths.Count == 2 && CloneFixture.SamePath((string)Native.Get(emittedPaths[dest]!, "ParentFolder")!, source), "Actual Main grouping rejected the actual cloner-emitted origin record.");
        string session = Path.Combine(f.Profile, "world-backup-sessions", "review-active.json");
        using (Process current = Process.GetCurrentProcess())
        {
            try
            {
                CloneFixture.Put(session, new JObject { ["gameFolder"] = source, ["pid"] = current.Id, ["started"] = current.StartTime.ToUniversalTime().Ticks }.ToString());
                Reject(62, () => E("Clone", source, source, "active-session", Options(), CancellationToken.None, null), "Persisted live game session was ignored during cloning.");
                Reject(63, () => E("PreviewMigration", dest, source, CancellationToken.None), "Persisted live source session was ignored during migration.");
            }
            finally
            {
                File.Delete(session);
            }
        }

        foreach (string name in new[]
        {
            "test-minimal.json",
            "test-minimal.jar",
            "mods/example-1.0.jar",
            "mods/off.jar.disabled",
            "config/example.toml",
            "options.txt",
            "resourcepacks/example.zip",
            "shaderpacks/example.zip",
            "PCL/CloneOrigin.json"
        }

        )
            C(65, File.Exists(Path.Combine(dest, name)), "Missing clone file: " + name);
        foreach (string name in new[]
        {
            "saves",
            "screenshots",
            "servers.dat",
            "servers.dat_old",
            "logs",
            "original-natives",
            "launcher_accounts.json",
            "config/accounts.json",
            "mods/.connector",
            "PCL/PackUpdateBackups"
        }

        )
            C(66, !Path.Exists(Path.Combine(dest, name)), "Excluded data was copied: " + name);
        JObject json = JObject.Parse(File.ReadAllText(Path.Combine(dest, "test-minimal.json")));
        C(68, (string? )json["id"] == "test-minimal" && (string? )json["jar"] == "test-minimal" && (string? )json["inheritsFrom"] == "1.20.1", "Clone JSON id/jar/inheritance mismatch.");
        string setup = File.ReadAllText(Path.Combine(dest, "PCL/Setup.ini"));
        C(70, setup.Contains("VersionArgumentIndieV2:True", StringComparison.Ordinal) && setup.Contains("VersionRamCustom:23", StringComparison.Ordinal) && !setup.Contains("fixture-only-private", StringComparison.Ordinal) && !setup.Contains("fixture-command", StringComparison.Ordinal), "Isolation or setup sanitization failed.");
        C(71, !File.ReadAllText(Path.Combine(dest, "PCL/config.json")).Contains("AccountToken", StringComparison.Ordinal) && !File.ReadAllText(Path.Combine(dest, "PCL/config.json")).Contains("fixture-only-private", StringComparison.Ordinal), "Private JSON setting was copied.");
        C(72, CloneFixture.Same(before, CloneFixture.Snapshot(source)), "Cloning modified the original version.");
        CloneFixture.Put(Path.Combine(dest, "mods/example-1.0.jar"), "clone-only-mod");
        C(74, File.ReadAllText(Path.Combine(source, "mods/example-1.0.jar")) == "old-mod", "Editable clone file shares storage with original.");
        InstanceCloneResultData full = (InstanceCloneResultData)E("Clone", source, source, "test-full", Options(true, true, true), CancellationToken.None, null)!;
        foreach (string name in new[]
        {
            "saves/world/level.dat",
            "screenshots/fixture.png",
            "servers.dat",
            "servers.dat_old"
        }

        )
            C(76, File.Exists(Path.Combine(full.VersionFolder!, name)), "Selected data missing: " + name);
        C(77, CloneFixture.Same(before, CloneFixture.Snapshot(source)), "Full cloning modified the original version.");
        Reject(78, () => E("Clone", source, source, "original", Options(), CancellationToken.None, null), "Clone overwrote original.");
        Reject(79, () => E("Clone", source, source, "test-full", Options(), CancellationToken.None, null), "Clone overwrote existing target.");
        foreach (string bad in new[]
        {
            "../escape",
            "CON",
            "COM0",
            "bad;name",
            ".pcl-clone-user"
        }

        )
            Reject(80, () => E("Clone", source, source, bad, Options(), CancellationToken.None, null), "Invalid clone name accepted: " + bad);
        using (CancellationTokenSource cancel = new())
        {
            cancel.Cancel();
            f.Reject<OperationCanceledException>(script, 82, () => E("Clone", source, source, "canceled", Options(), cancel.Token, null), "Canceled clone did not preserve real cancellation.");
        }

        using (CancellationTokenSource cancel = new())
        {
            bool callbackReached = false;
            Action<int> abort = index =>
            {
                if (index == 1)
                {
                    callbackReached = true;
                    cancel.Cancel();
                }
            };
            f.Reject<OperationCanceledException>(script, 84, () => E("Clone", source, source, "mid-canceled", Options(), cancel.Token, abort), "Mid-copy cancellation did not preserve real cancellation/callback.", _ => callbackReached && cancel.IsCancellationRequested);
        }

        Action<int> mutate = index =>
        {
            if (index == 1)
                CloneFixture.Put(Path.Combine(source, "config/new-during-copy.toml"), "external-edit");
        };
        Reject(86, () => E("Clone", source, source, "changed-source", Options(), CancellationToken.None, mutate), "Inconsistent source was cloned.");
        File.Delete(Path.Combine(source, "config/new-during-copy.toml"));
        C(88, !Directory.EnumerateDirectories(Path.Combine(mc, "versions")).Any(path => Path.GetFileName(path).StartsWith(".pcl-clone-", StringComparison.Ordinal) || new[] { "canceled", "mid-canceled", "changed-source" }.Contains(Path.GetFileName(path))), "Canceled/failed clone left a stage or visible version.");
        string outside = f.Folder("engine/outside");
        CloneFixture.Put(Path.Combine(outside, "keep.txt"), "keep-outside");
        string junction = Path.Combine(source, "config/linked");
        f.Junction(junction, outside);
        try
        {
            Reject(92, () => E("Clone", source, source, "linked-source", Options(), CancellationToken.None, null), "Linked source directory was cloned.");
        }
        finally
        {
            f.Unlink(junction);
        }

        C(94, File.ReadAllText(Path.Combine(outside, "keep.txt")) == "keep-outside", "Link rejection modified external data.");
        string sharedSource = Path.Combine(mc, "versions/shared");
        CloneFixture.Put(Path.Combine(sharedSource, "shared.json"), """{"id":"shared","type":"release","mainClass":"net.minecraft.client.main.Main","libraries":[]}""");
        CloneFixture.Put(Path.Combine(mc, "mods/shared.jar"), "shared-game-mod");
        InstanceCloneResultData shared = (InstanceCloneResultData)E("Clone", sharedSource, mc, "shared-test", Options(), CancellationToken.None, null)!;
        C(100, File.ReadAllText(Path.Combine(shared.VersionFolder!, "mods/shared.jar")) == "shared-game-mod", "Shared source game environment was not isolated.");
        foreach (string directory in extra)
            CloneFixture.Put(Path.Combine(dest, directory, "review.txt"), "clone-" + directory);
        CloneFixture.Put(Path.Combine(dest, "config/xaero/waypoints.txt"), "clone-waypoint");
        CloneFixture.Put(Path.Combine(dest, "test-minimal.json"), """{"id":"test-minimal","inheritsFrom":"1.21.1","jar":"test-minimal","type":"release","mainClass":"net.minecraft.client.main.Main","libraries":[],"arguments":{"game":[],"jvm":[]}}""");
        object plan = E("PreviewMigration", dest, source, CancellationToken.None)!;
        C(106, (string? )Native.Get(plan, "OldMinecraft") == "1.20.1" && (string? )Native.Get(plan, "NewMinecraft") == "1.21.1" && ((ICollection)Native.Get(plan, "Changes")!).Count >= 2, "Migration preview omitted MC or mod changes.");
        C(107, CloneFixture.Same(before, CloneFixture.Snapshot(source)), "Migration preview modified source.");
        Reject(108, () => E("ApplyMigration", plan, CancellationToken.None, null, null, null), "MC-changing migration ran without a world backup hook.");
        bool backupCalled = false;
        Action<string, string> backup = (_, _) => backupCalled = true;
        Func<string> capture = () =>
        {
            C(111, backupCalled, "Environment capture ran before world backup.");
            return (string)H("Capture", source, source, "migration-before", CancellationToken.None, null, "fixture migration")!;
        };
        IOException injected = new("Injected actual migration transaction failure.");
        bool failureCallbackReached = false;
        Action<int> fail = index =>
        {
            if (index == 1)
            {
                failureCallbackReached = true;
                throw injected;
            }
        };
        f.Reject<IOException>(script, 113, () => E("ApplyMigration", plan, CancellationToken.None, capture, backup, fail), "Migration did not preserve the actual injected transaction exception.", error => failureCallbackReached && ReferenceEquals(error, injected));
        foreach (var pair in before)
            C(115, CloneFixture.Hash(source + pair.Key) == pair.Value, "Failed migration did not roll back: " + pair.Key);
        backupCalled = false;
        plan = E("PreviewMigration", dest, source, CancellationToken.None)!;
        string undo = (string)E("ApplyMigration", plan, CancellationToken.None, capture, backup, null)!;
        C(118, !string.IsNullOrEmpty(undo) && backupCalled, "Successful migration lacks restore point or world backup callback.");
        C(119, File.ReadAllText(Path.Combine(source, "mods/example-1.0.jar")) == "clone-only-mod", "Migration did not apply tested mod.");
        C(120, File.ReadAllText(Path.Combine(source, "config/xaero/waypoints.txt")) == "original-waypoint", "Migration replaced personal map markers.");
        foreach (string directory in extra)
            C(121, File.ReadAllText(Path.Combine(source, directory, "review.txt")) == "clone-" + directory, "Additional environment directory was not migrated: " + directory);
        JObject migrated = JObject.Parse(File.ReadAllText(Path.Combine(source, "original.json")));
        C(123, (string? )migrated["id"] == "original" && (string? )migrated["jar"] == "original" && (string? )migrated["inheritsFrom"] == "1.21.1", "Migrated version JSON references wrong instance.");
        C(124, File.ReadAllText(Path.Combine(source, "PCL/config.json")).Contains("AccountToken", StringComparison.Ordinal), "Migration deleted unrelated local account configuration.");
        foreach (var pair in new[]
        {
            ("saves/world/level.dat", "original-world"),
            ("screenshots/fixture.png", "original-screenshot"),
            ("servers.dat", "original-servers")
        }

        )
            C(125, File.ReadAllText(Path.Combine(source, pair.Item1)) == pair.Item2, "Migration changed personal data: " + pair.Item1);
        object stale = E("PreviewMigration", dest, source, CancellationToken.None)!;
        CloneFixture.Put(Path.Combine(source, "config/example.toml"), "external-change-after-preview");
        Reject(128, () => E("ApplyMigration", stale, CancellationToken.None, capture, backup, null), "Stale migration plan was accepted.");
        H("Restore", source, source, mc, undo, CancellationToken.None, null, null);
        foreach (string directory in extra)
            C(130, File.ReadAllText(Path.Combine(source, directory, "review.txt")) == "original-" + directory, "Migration undo failed to restore the complete environment.");
        C(131, File.ReadAllText(Path.Combine(source, "config/xaero/waypoints.txt")) == "original-waypoint", "Migration undo modified personal map markers.");
        string probe = Path.Combine(mc, "versions/isolation-probe");
        Directory.CreateDirectory(Path.Combine(probe, "mods/1.12.2"));
        ModMinecraft.McInstance instance = new(probe)
        {
            Version = new McVersion("1.20.1", true)
        };
        Settings.Entries["LaunchArgumentIndieV2"].ValueCache[""] = 0;
        C(136, CloneFixture.SamePath((string)E("GameRootForInstance", instance)!, mc), "Nested mods folder incorrectly enabled isolation.");
        CloneFixture.Put(Path.Combine(probe, "saves/not-a-world.txt"), "file-only");
        C(139, CloneFixture.SamePath((string)E("GameRootForInstance", instance)!, mc), "File in saves incorrectly enabled isolation.");
        CloneFixture.Put(Path.Combine(probe, "mods/direct.jar"), "direct-file");
        C(141, CloneFixture.SamePath((string)E("GameRootForInstance", instance)!, probe), "Direct mod did not enable isolation.");
        File.Delete(Path.Combine(probe, "mods/direct.jar"));
        Directory.CreateDirectory(Path.Combine(probe, "saves/world-directory"));
        C(144, CloneFixture.SamePath((string)E("GameRootForInstance", instance)!, probe), "Direct save directory did not enable isolation.");
    }
}
