using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using PCLCS;

internal static class MmcSessionTests {
    internal static IEnumerable<(string Name, Action Run)> Cases() {
        yield return ("MMC session final validation and creation stay inside admission with one protected argv nonce", LaunchAndExit);
        yield return ("MMC session active receipt blocks structural transactions and another launch", StructuralGate);
        yield return ("MMC session active Dispose keeps durable uncertainty and Java pin", DisposeKeepsProtection);
        yield return ("MMC session PID reuse and absent nonce evidence remain unknown", IdentityAndNonce);
        yield return ("MMC session interrupted Launching without PID cannot be guessed exited", LaunchingInterruption);
        yield return ("MMC session rejects stale generation layout and caller nonce overrides", InvalidLaunch);
        yield return ("MMC session serialized receipts contain identities but no process arguments", ReceiptPrivacy);
        yield return ("MMC session host termination retains running receipt and blocks structural writes", KilledHost);
    }

    internal static bool TryRunChild(string[] args, out int exitCode) {
        exitCode = 0;
        string? nonce = args.FirstOrDefault(a => a.StartsWith(MmcSessions.NonceArgumentPrefix, StringComparison.Ordinal))?[MmcSessions.NonceArgumentPrefix.Length..];
        string[] data = args.Where(a => !a.StartsWith(MmcSessions.NonceArgumentPrefix, StringComparison.Ordinal)).ToArray();
        if (data.Length == 0 || data[0] is not ("--mmc-session-child" or "--mmc-session-host-child")) return false;
        try {
            if (data[0] == "--mmc-session-child") {
                if (data.Length < 4 || nonce is null) throw new Exception("Controlled child has no protected nonce.");
                File.WriteAllText(data[1], Environment.ProcessId + "\n" + nonce);
                var timer = Stopwatch.StartNew();
                while (!File.Exists(data[2]) && timer.ElapsedMilliseconds < 30000) Thread.Sleep(20);
                exitCode = 0; return true;
            }
            if (data.Length != 3 || !Guid.TryParse(data[2], out Guid id)) throw new Exception("Invalid controlled host child.");
            using Fixture fixture = new(data[1], id, initialize: false);
            using var lease = fixture.Lease();
            using var session = MmcSessions.Launch(lease, fixture.Request(), () => { }, (_, image) => new FixturePin(image, fixture.PinFile));
            File.WriteAllText(fixture.HostReady, "ready");
            // Leave admission after registration exactly as a real monitor does.
            lease.Dispose(); Thread.Sleep(Timeout.Infinite);
        } catch (Exception ex) { Console.Error.WriteLine(ex); exitCode = 20; }
        return true;
    }

    private static void LaunchAndExit() {
        using Fixture fixture = new(); using var lease = fixture.Lease(); FixturePin? pin = null;
        Guid plannedSession = Guid.NewGuid();
        var request = fixture.Request() with { SessionId = plannedSession };
        using var session = MmcSessions.Launch(lease, request, () => Equal(true, pin is not null), (_, image) => pin = new(image, fixture.PinFile));
        Equal(plannedSession, session.Receipt.SessionId);
        fixture.WaitChild();
        Equal(session.Receipt.Nonce, fixture.ChildNonce());
        Throws<IOException>(() => session.CompleteVerifiedExit(lease));
        fixture.Stop(); session.Process.WaitForExit(10000); session.CompleteVerifiedExit(lease);
        Equal(true, pin!.Completed); Equal(false, File.Exists(fixture.PinFile)); MmcSessions.AssertNoActive(lease);
        Throws<IOException>(() => MmcSessions.Launch(lease, request, () => { }, (_, _) => throw new Exception("Reused session reached pin acquisition.")));
    }
    private static void StructuralGate() {
        using Fixture fixture = new(); using var lease = fixture.Lease();
        using var session = MmcSessions.Launch(lease, fixture.Request(), () => { }, (_, image) => new FixturePin(image, fixture.PinFile));
        fixture.WaitChild();
        Throws<IOException>(() => MmcFileTransactions.Execute(lease, fixture.TransactionRequest()));
        Throws<IOException>(() => MmcSessions.Launch(lease, fixture.Request(), () => { }, (_, image) => new FixturePin(image, fixture.PinFile)));
        fixture.Stop(); session.Process.WaitForExit(10000); session.CompleteVerifiedExit(lease);
    }
    private static void DisposeKeepsProtection() {
        using Fixture fixture = new(); using var lease = fixture.Lease(); FixturePin? pin = null;
        var session = MmcSessions.Launch(lease, fixture.Request(), () => { }, (_, image) => pin = new(image, fixture.PinFile));
        fixture.WaitChild(); var id = session.Receipt.SessionId; session.Dispose();
        Equal(true, pin!.Retained); Equal(true, File.Exists(fixture.PinFile));
        Throws<IOException>(() => MmcSessions.AssertNoActive(lease));
        Equal(MmcSessionObservation.Unknown, MmcSessions.Reconcile(lease, id, new MmcSessionProcessInspector(), _ => pin.CompleteVerifiedExit()).Observation);
        fixture.StopAndWait();
        Equal(MmcSessionObservation.ProvenExited, MmcSessions.Reconcile(lease, id, new MmcSessionProcessInspector(), _ => pin.CompleteVerifiedExit()).Observation);
        MmcSessions.AssertNoActive(lease);
    }
    private static void IdentityAndNonce() {
        using Fixture fixture = new(); using var lease = fixture.Lease();
        using var session = MmcSessions.Launch(lease, fixture.Request(), () => { }, (_, image) => new FixturePin(image, fixture.PinFile)); fixture.WaitChild();
        IMmcSessionProcessInspector inspector = new MmcSessionProcessInspector((pid, nonce) => pid == fixture.ChildPid() && nonce == fixture.ChildNonce());
        Equal(MmcSessionObservation.MatchingRunning, inspector.Inspect(session.Receipt, session.Registration));
        Equal(MmcSessionObservation.Unknown, inspector.Inspect(session.Receipt, session.Registration with { CreationTimeUtcTicks = session.Registration.CreationTimeUtcTicks + 1 }));
        Equal(MmcSessionObservation.Unknown, inspector.Inspect(session.Receipt, null));
        Equal(MmcSessionObservation.Unknown, new MmcSessionProcessInspector().Inspect(session.Receipt, session.Registration));
        fixture.Stop(); session.Process.WaitForExit(10000); session.CompleteVerifiedExit(lease);
    }
    private static void LaunchingInterruption() {
        using Fixture fixture = new(); using var lease = fixture.Lease();
        Throws<MmcTransactionInterruptionException>(() => MmcSessions.Launch(lease, fixture.Request(), () => { }, (_, image) => new FixturePin(image, fixture.PinFile), stage => {
            if (stage == MmcSessionCheckpoint.LaunchingPersisted) throw new MmcTransactionInterruptionException("before actual create");
        }));
        Guid id = Guid.ParseExact(Path.GetFileName(Directory.GetDirectories(fixture.Sessions).Single()), "N");
        Equal(MmcSessionObservation.Unknown, MmcSessions.Reconcile(lease, id, new MmcSessionProcessInspector(), _ => throw new Exception("must not release")).Observation);
        Equal(true, File.Exists(fixture.PinFile)); Throws<IOException>(() => MmcSessions.AssertNoActive(lease));
        // This specific fixture knows Process.Start was never called. The generic OS inspector does not
        // make this claim; a real host must supply equally strong evidence before resolving Launching.
        Equal(MmcSessionObservation.ProvenExited, MmcSessions.Reconcile(lease, id, new KnownNotStarted(), _ => File.Delete(fixture.PinFile)).Observation);
    }
    private static void InvalidLaunch() {
        using Fixture fixture = new(); using var lease = fixture.Lease();
        Throws<IOException>(() => MmcSessions.Launch(lease, fixture.Request() with { SessionId = Guid.Empty }, () => { }, (_, _) => throw new Exception("Empty session reached pin acquisition.")));
        Throws<IOException>(() => MmcSessions.Launch(lease, fixture.Request() with { DefinitionGeneration = 2 }, () => { }, (_, image) => new FixturePin(image, fixture.PinFile)));
        Throws<IOException>(() => MmcSessions.Launch(lease, fixture.Request() with { GameRootLeaf = ".minecraft" }, () => { }, (_, image) => new FixturePin(image, fixture.PinFile)));
        var request = fixture.Request(); request.StartInfo.ArgumentList.Add(MmcSessions.NonceArgumentPrefix + "forged");
        Throws<IOException>(() => MmcSessions.Launch(lease, request, () => { }, (_, image) => new FixturePin(image, fixture.PinFile)));
        MmcSessions.AssertNoActive(lease);
    }
    private static void ReceiptPrivacy() {
        using Fixture fixture = new(); using var lease = fixture.Lease(); var request = fixture.Request();
        request.StartInfo.RedirectStandardOutput = true; request.StartInfo.StandardOutputEncoding = Encoding.UTF8;
        request.StartInfo.RedirectStandardError = true; request.StartInfo.StandardErrorEncoding = Encoding.Unicode;
        request.StartInfo.ArgumentList.Add("synthetic-access-token-should-not-persist");
        using var session = MmcSessions.Launch(lease, request, () => { }, (_, image) => new FixturePin(image, fixture.PinFile));
        Equal(Encoding.UTF8.CodePage, session.Process.StartInfo.StandardOutputEncoding?.CodePage ?? -1);
        Equal(Encoding.Unicode.CodePage, session.Process.StartInfo.StandardErrorEncoding?.CodePage ?? -1);
        fixture.WaitChild();
        foreach (string record in Directory.GetFiles(fixture.Sessions, "*.record", SearchOption.AllDirectories))
            Equal(false, Encoding.UTF8.GetString(File.ReadAllBytes(record)).Contains("synthetic-access-token-should-not-persist", StringComparison.Ordinal));
        fixture.Stop(); session.Process.WaitForExit(10000); session.CompleteVerifiedExit(lease);
    }
    private static void KilledHost() {
        using Fixture fixture = new();
        ProcessStartInfo start = new(AppHost()) { UseShellExecute = false, CreateNoWindow = true };
        foreach (string arg in new[] { "--mmc-session-host-child", fixture.Scope, fixture.Id.ToString("D") }) start.ArgumentList.Add(arg);
        using (Process host = Process.Start(start) ?? throw new Exception("Could not start controlled monitor.")) {
            try {
                WaitFile(fixture.HostReady, 12000); fixture.WaitChild();
            } finally { if (!host.HasExited) host.Kill(); if (!host.WaitForExit(5000)) throw new Exception("Controlled monitor did not exit."); }
        }
        // Kill only the explicitly-created monitor, not its process tree: the owned game remains alive.
        using var lease = fixture.Lease();
        Guid id = Guid.ParseExact(Path.GetFileName(Directory.GetDirectories(fixture.Sessions).Single()), "N");
        Equal(true, File.Exists(fixture.PinFile)); Throws<IOException>(() => MmcSessions.AssertNoActive(lease));
        var inspector = new MmcSessionProcessInspector((pid, nonce) => pid == fixture.ChildPid() && nonce == fixture.ChildNonce());
        MmcSessionResult result = MmcSessions.Reconcile(lease, id, inspector, _ => throw new Exception("running pin must remain"));
        Equal(MmcSessionObservation.MatchingRunning, result.Observation);
        fixture.StopAndWait();
        Equal(MmcSessionObservation.ProvenExited, MmcSessions.Reconcile(lease, id, new MmcSessionProcessInspector(), _ => File.Delete(fixture.PinFile)).Observation);
    }

    private static string AppHost() {
        string candidate = Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, OperatingSystem.IsWindows() ? ".exe" : null);
        return File.Exists(candidate) ? candidate : throw new Exception("Session fixture requires the actual generated apphost.");
    }
    private static byte[] Definition(Guid id, long generation) => Encoding.UTF8.GetBytes($$$"""{"schemaVersion":1,"managementKind":"Mmc","formatProfile":"prism-v1","instanceId":"{{{id:D}}}","generation":{{{generation}}},"layout":{"gameRootLeaf":"minecraft"}}""");
    private static string Hash(byte[] content) => Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
    private static void WaitFile(string path, int timeout) { var timer = Stopwatch.StartNew(); while (!File.Exists(path) && timer.ElapsedMilliseconds < timeout) Thread.Sleep(20); if (!File.Exists(path)) throw new Exception("Controlled child did not report ready."); }
    private static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}, got {actual}."); }
    private static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception("Expected " + typeof(T).Name); }
    private sealed class KnownNotStarted : IMmcSessionProcessInspector { public MmcSessionObservation Inspect(MmcSessionReceipt receipt, MmcSessionProcessRegistration? registration) => MmcSessionObservation.ProvenExited; }
    private sealed class FixturePin(MmcExecutableIdentity identity, string marker) : IMmcJavaSessionPin {
        public MmcExecutableIdentity Identity { get; } = Save(identity, marker);
        public bool Retained { get; private set; }
        public bool Completed { get; private set; }
        private static MmcExecutableIdentity Save(MmcExecutableIdentity identity, string marker) { File.WriteAllText(marker, identity.Sha256); return identity; }
        public void KeepForRecovery() => Retained = true;
        public void CompleteVerifiedExit() { Completed = true; File.Delete(marker); }
        public void Dispose() { }
    }
    private sealed class Fixture : IDisposable {
        public string Scope { get; }
        public string Root => Path.Combine(Scope, "versions", "sample");
        public Guid Id { get; }
        public string Ready => Path.Combine(Scope, "game-ready");
        public string HostReady => Path.Combine(Scope, "host-ready");
        public string StopFile => Path.Combine(Scope, "game-stop");
        public string PinFile => Path.Combine(Scope, "java-pin");
        public string Sessions => Path.Combine(Root, "PCL", "MmcSessions");
        private readonly bool cleanup;
        public Fixture() : this(Path.Combine(Path.GetTempPath(), "pcllf-mmc-session-" + Guid.NewGuid().ToString("N")), Guid.NewGuid(), true) { }
        public Fixture(string scope, Guid id, bool initialize) {
            Scope = scope; Id = id; cleanup = initialize;
            if (initialize) {
                Directory.CreateDirectory(Path.Combine(Root, "PCL")); Directory.CreateDirectory(Path.Combine(Root, "minecraft"));
                File.WriteAllBytes(Path.Combine(Root, "PCL", "InstanceDefinition.json"), Definition(id, 1));
            }
        }
        public MmcAdmissionLease Lease() => MmcAdmissionGate.Acquire(Root, Id, TimeSpan.Zero);
        public MmcSessionLaunchRequest Request() {
            var image = MmcSessions.ObserveExecutable(AppHost()); var launcher = MmcSessions.ObserveExecutable(Environment.ProcessPath!);
            ProcessStartInfo start = new(AppHost()) { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Path.Combine(Root, "minecraft") };
            foreach (string arg in new[] { "--mmc-session-child", Ready, StopFile, "bounded-fixture" }) start.ArgumentList.Add(arg);
            return new(Id, 1, new string('a', 64), image, launcher, start);
        }
        public MmcFileTransactionRequest TransactionRequest() {
            byte[] newer = Definition(Id, 2);
            return new(Id, 1, 2, [new("PCL/InstanceDefinition.json", Hash(Definition(Id, 1)), newer, Hash(newer))]);
        }
        public void WaitChild() => WaitFile(Ready, 10000);
        public int ChildPid() => int.Parse(File.ReadAllLines(Ready)[0], System.Globalization.CultureInfo.InvariantCulture);
        public string ChildNonce() => File.ReadAllLines(Ready)[1];
        public void Stop() => File.WriteAllText(StopFile, "stop");
        public void StopAndWait() {
            Stop(); if (!File.Exists(Ready)) return;
            try { using Process process = Process.GetProcessById(ChildPid()); if (!process.WaitForExit(10000)) throw new Exception("Controlled game did not exit."); }
            catch (ArgumentException) { }
        }
        public void Dispose() { if (!cleanup) return; StopAndWait(); if (Directory.Exists(Scope)) Directory.Delete(Scope, true); }
    }
}
