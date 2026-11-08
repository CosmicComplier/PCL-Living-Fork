using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using MeloongCore;
using Newtonsoft.Json.Linq;
using PCLCS;

internal static class MmcConfigurationTests {
    internal static IEnumerable<(string Name, Action Run)> Cases() {
        yield return ("MMC configuration separate processes rebase own preference keys onto latest disk", CrossProcessRebase);
        yield return ("MMC configuration old cross-process manifest queue cannot overwrite a published generation", StaleManifest);
        yield return ("MMC configuration Set remains nonblocking while a structural scope owns admission", NonblockingSet);
        yield return ("MMC configuration Shared PreviewGet Reset uses the same queued key writer", PreviewReset);
        yield return ("MMC configuration rollback invalidates snapshots without consuming pending preferences", RollbackEpoch);
    }
    internal static bool TryRunChild(string[] args, out int exitCode) {
        exitCode = 0;
        if (args.Length == 0 || args[0] != "--mmc-configuration-child") return false;
        try {
            if (args.Length != 5) throw new Exception("Invalid controlled config child.");
            using var provider = new InstanceConfigProvider(args[2]);
            if (args[1] == "rebase") { provider.Set("childPreference", "B", false); provider.Save(); return true; }
            provider.Set("MinecraftInstallManifest", new JObject { ["stale"] = true }, false);
            provider.Set("childPreference", "B-after-conversion", false);
            using (MmcConfigurationCoordinator.Quiesce(args[2])) {
                File.WriteAllText(args[3], "ready"); WaitFile(args[4], 10000);
            }
            try { provider.Save(); } catch (IOException) { }
            JObject disk = JObject.Parse(File.ReadAllText(Path.Combine(args[2], "PCL", "config.json")));
            if (disk["MinecraftInstallManifest"]?["pcllfManagement"]?["generation"]?.Value<int>() != 1) exitCode = 32;
        } catch (Exception ex) { Console.Error.WriteLine(ex); exitCode = 31; }
        return true;
    }
    private static void CrossProcessRebase() {
        using Fixture fixture = new(); using var provider = new InstanceConfigProvider(fixture.Root);
        Equal("kept", provider.Read("unknown", "", false));
        using (Process child = StartChild("rebase", fixture)) WaitChild(child);
        provider.Set("parentPreference", "A", false); provider.Save();
        JObject disk = fixture.Config(); Equal("B", disk.Value<string>("childPreference")); Equal("A", disk.Value<string>("parentPreference")); Equal("kept", disk.Value<string>("unknown"));
    }
    private static void StaleManifest() {
        using Fixture fixture = new();
        using Process child = StartChild("stale", fixture); WaitFile(fixture.Ready, 10000);
        using (var quiet = MmcConfigurationCoordinator.Quiesce(fixture.Root))
        using (var lease = fixture.Lease())
        using (var scope = quiet.OpenStructuralWrite(lease, 0)) {
            scope.SetConfig("MinecraftInstallManifest", new JObject { ["pcllfManagement"] = fixture.Hint(1) });
            Equal(MmcTransactionOutcome.Committed, scope.Execute(fixture.Publish(0)).Outcome);
        }
        File.WriteAllText(fixture.Go, "go"); WaitChild(child);
        Equal(1, fixture.Config()["MinecraftInstallManifest"]!["pcllfManagement"]!.Value<int>("generation"));
        Equal("B-after-conversion", fixture.Config().Value<string>("childPreference"));
    }
    private static void NonblockingSet() {
        using Fixture fixture = new(); using var first = new InstanceConfigProvider(fixture.Root); using var second = new InstanceConfigProvider(fixture.Root);
        first.Set("before", "one", false); Equal("one", second.Read("before", "", false));
        using (var quiet = MmcConfigurationCoordinator.Quiesce(fixture.Root))
        using (var lease = fixture.Lease())
        using (var scope = quiet.OpenStructuralWrite(lease, 0)) {
            Task setter = Task.Run(() => second.Set("during", "two", false));
            Equal(true, setter.Wait(300));
            scope.SetConfig("MinecraftInstallManifest", new JObject { ["pcllfManagement"] = fixture.Hint(1) });
            Equal(MmcTransactionOutcome.Committed, scope.Execute(fixture.Publish(0)).Outcome);
        }
        first.Save(); Equal("one", fixture.Config().Value<string>("before")); Equal("two", fixture.Config().Value<string>("during"));
    }
    private static void PreviewReset() {
        using Fixture fixture = new(); using var provider = new InstanceConfigProvider(fixture.Root);
        // Actual fixed Shared DynamicConfigEntry callback/Reset contract, with a self-owned synthetic
        // value. Full Java deserialization is left to the existing isolated Java/product fixture.
        DynamicConfigEntry<string> forced = new("InstanceForcedJava", null);
        forced.PreviewGet += (value, owner) => { if (value.Value == "not-installed") { value.Value = null; forced.Reset(owner); } };
        forced.Set("not-installed", provider); provider.Save();
        Equal<string?>(null, forced.Get(provider)); provider.Save();
        Equal(false, fixture.Config().ContainsKey("InstanceForcedJava")); Equal("kept", fixture.Config().Value<string>("unknown"));
    }
    private static void RollbackEpoch() {
        using Fixture fixture = new(); using var provider = new InstanceConfigProvider(fixture.Root);
        using (var initialQuiet = MmcConfigurationCoordinator.Quiesce(fixture.Root))
        using (var initialLease = fixture.Lease())
        using (var initialScope = initialQuiet.OpenStructuralWrite(initialLease, 0)) {
            initialScope.SetConfig("MinecraftInstallManifest", new JObject { ["pcllfManagement"] = fixture.Hint(1) });
            Equal(MmcTransactionOutcome.Committed, initialScope.Execute(fixture.Publish(0)).Outcome);
        }
        provider.Set("queued", "retained", false); long before = provider.Epoch;
        using (var quiet = MmcConfigurationCoordinator.Quiesce(fixture.Root))
        using (var lease = fixture.Lease())
        using (var scope = quiet.OpenStructuralWrite(lease, 1)) {
            scope.SetConfig("MinecraftInstallManifest", new JObject { ["pcllfManagement"] = fixture.Hint(2) });
            Equal(MmcTransactionOutcome.Aborted, scope.Execute(fixture.Publish(1), checkpoint: (phase, _) => {
                if (phase == MmcTransactionCheckpoint.BeforeDecision) throw new IOException("controlled rollback");
            }).Outcome);
        }
        Equal(true, provider.Epoch > before); Equal("retained", provider.Read("queued", "", false)); provider.Save();
        Equal("retained", fixture.Config().Value<string>("queued")); Equal(1, JObject.Parse(File.ReadAllText(fixture.DefinitionFile)).Value<int>("generation"));
    }
    private static Process StartChild(string mode, Fixture fixture) {
        string executable = Environment.ProcessPath!; ProcessStartInfo start = new(executable) { UseShellExecute = false, CreateNoWindow = true };
        if (Path.GetFileNameWithoutExtension(executable).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        foreach (string value in new[] { "--mmc-configuration-child", mode, fixture.Root, fixture.Ready, fixture.Go }) start.ArgumentList.Add(value);
        return Process.Start(start) ?? throw new Exception("Could not start controlled config writer.");
    }
    private static void WaitFile(string path, int budget) { var timer = Stopwatch.StartNew(); while (!File.Exists(path) && timer.ElapsedMilliseconds < budget) Thread.Sleep(10); if (!File.Exists(path)) throw new Exception("Controlled config writer timed out."); }
    private static void WaitChild(Process child) { if (!child.WaitForExit(12000)) { child.Kill(true); throw new Exception("Controlled config writer timed out."); } Equal(0, child.ExitCode); }
    private static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}, got {actual}."); }
    private sealed class Fixture : IDisposable {
        public string Scope { get; } = Path.Combine(Path.GetTempPath(), "pcllf-mmc-config-" + Guid.NewGuid().ToString("N"));
        public string Root => Path.Combine(Scope, "versions", "sample");
        public string DefinitionFile => Path.Combine(Root, "PCL", "InstanceDefinition.json");
        public string Ready => Path.Combine(Scope, "ready"); public string Go => Path.Combine(Scope, "go");
        private readonly Guid id = Guid.NewGuid();
        public Fixture() { Directory.CreateDirectory(Path.Combine(Root, "PCL")); File.WriteAllText(Path.Combine(Root, "PCL", "config.json"), "{\"unknown\":\"kept\"}"); }
        public JObject Config() => JObject.Parse(File.ReadAllText(Path.Combine(Root, "PCL", "config.json")));
        public JObject Hint(long generation) => new() { ["schemaVersion"] = 1, ["managementKind"] = "Mmc", ["instanceId"] = id.ToString("D"), ["generation"] = generation };
        public MmcAdmissionLease Lease() => MmcAdmissionGate.Acquire(Root, id, TimeSpan.Zero);
        public MmcFileTransactionRequest Publish(long previous) {
            JObject definition = Hint(previous + 1); definition["formatProfile"] = "prism-v1"; definition["layout"] = new JObject { ["gameRootLeaf"] = "minecraft" };
            byte[] bytes = Encoding.UTF8.GetBytes(definition.ToString()); byte[] version = Encoding.UTF8.GetBytes(new JObject { ["pcllfManagement"] = Hint(previous + 1) }.ToString());
            return new(id, previous, previous + 1, [new("PCL/InstanceDefinition.json", previous == 0 ? null : Hash(File.ReadAllBytes(DefinitionFile)), bytes, Hash(bytes)),
                new("sample.json", previous == 0 ? null : Hash(File.ReadAllBytes(Path.Combine(Root, "sample.json"))), version, Hash(version))]);
        }
        private static string Hash(byte[] content) => Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        public void Dispose() { InstanceConfigProviders.FlushAndDisposeWithin(Scope); if (Directory.Exists(Scope)) Directory.Delete(Scope, true); }
    }
}
