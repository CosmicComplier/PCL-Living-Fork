using System;
using System.Diagnostics;

namespace PCLCS;

// Original LF host implementation; repository LICENCE applies.
public sealed record MmcExecutableIdentity(string ExecutablePath, string Sha256);
public sealed record MmcSessionRuntimeBinding(string BoundPlanFingerprint);
public sealed record MmcSessionLaunchRequest(Guid InstanceId, long DefinitionGeneration, string RuntimeFingerprint,
    MmcExecutableIdentity JavaIdentity, MmcExecutableIdentity LauncherIdentity, ProcessStartInfo StartInfo, string GameRootLeaf = "minecraft") {
    /// <summary>Allocate once before building mutable runtime paths; null retains the legacy fixture allocation.</summary>
    public Guid? SessionId { get; init; }
    public MmcSessionRuntimeBinding? RuntimeBinding { get; init; }
}
public sealed record MmcSessionReceipt(int SchemaVersion, Guid SessionId, Guid InstanceId, string CanonicalRoot,
    long DefinitionGeneration, string RuntimeFingerprint, string Nonce, MmcExecutableIdentity JavaIdentity,
    MmcExecutableIdentity LauncherIdentity) {
    public MmcSessionRuntimeBinding? RuntimeBinding { get; init; }
}
public sealed record MmcSessionProcessRegistration(int SchemaVersion, Guid SessionId, string LaunchingSha256,
    int ProcessId, long CreationTimeUtcTicks, MmcExecutableIdentity ExecutableIdentity);
internal sealed record MmcSessionExitRecord(int SchemaVersion, Guid SessionId, string LaunchingSha256, string Proof);
public enum MmcSessionObservation { MatchingRunning, ProvenExited, Unknown }
public sealed record MmcSessionResult(Guid SessionId, MmcSessionObservation Observation, string? Diagnostic = null);

/// <summary>
/// Host Java-service contract. Acquisition is inside instance admission and before final Java validation.
/// Implementations coordinate with installers/cleaners and retain durable pins after host death/uncertainty.
/// Dispose alone MUST NOT unpin an unconfirmed process. No implementation is silently assumed here.
/// </summary>
public interface IMmcJavaSessionPin : IDisposable {
    MmcExecutableIdentity Identity { get; }
    void KeepForRecovery();
    void CompleteVerifiedExit();
}

/// <summary>Trusted host process observation. Unknown access, PID reuse or missing nonce evidence is Unknown.</summary>
public interface IMmcSessionProcessInspector {
    MmcSessionObservation Inspect(MmcSessionReceipt receipt, MmcSessionProcessRegistration? registration);
}

/// <summary>Conservative OS identity checks. Command contents are consumed only by the supplied in-memory nonce probe.</summary>
public sealed class MmcSessionProcessInspector(Func<int, string, bool?>? nonceProbe = null) : IMmcSessionProcessInspector {
    public MmcSessionObservation Inspect(MmcSessionReceipt receipt, MmcSessionProcessRegistration? registration) {
        if (registration is null || registration.ProcessId <= 0 || registration.CreationTimeUtcTicks <= 0 || registration.SessionId != receipt.SessionId)
            return MmcSessionObservation.Unknown;
        Process process;
        try { process = Process.GetProcessById(registration.ProcessId); }
        catch (ArgumentException) { return MmcSessionObservation.ProvenExited; }
        try {
            if (process.HasExited) return MmcSessionObservation.ProvenExited;
            if (process.StartTime.ToUniversalTime().Ticks != registration.CreationTimeUtcTicks) return MmcSessionObservation.Unknown;
            MmcExecutableIdentity observed = MmcSessions.ObserveExecutable(process.MainModule?.FileName ?? "");
            if (!MmcSessions.SameExecutable(observed, receipt.JavaIdentity) || !MmcSessions.SameExecutable(observed, registration.ExecutableIdentity))
                return MmcSessionObservation.Unknown;
            return nonceProbe?.Invoke(registration.ProcessId, receipt.Nonce) == true ? MmcSessionObservation.MatchingRunning : MmcSessionObservation.Unknown;
        } catch (Exception ex) when (ex is ArgumentException or System.ComponentModel.Win32Exception or InvalidOperationException or System.IO.IOException or UnauthorizedAccessException or NotSupportedException) {
            return MmcSessionObservation.Unknown;
        } finally { process.Dispose(); }
    }
}
