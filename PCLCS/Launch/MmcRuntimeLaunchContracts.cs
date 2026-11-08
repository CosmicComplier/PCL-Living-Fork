using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using PCLLF.MMCC.Core;

namespace PCLCS;

// Original LF host implementation; governed by the repository LICENCE.
public enum MmcRuntimeWrapperMode { JavaWrapper, DirectMain }
public sealed record MmcRuntimeArtifactTarget(string ArtifactKey, string TargetRelativePath, string ActualPath);
public sealed record MmcRuntimeSupportArtifact(string ActualPath, string Sha256, long SizeBytes);
public sealed record MmcRuntimeJavaSnapshot(string ExecutablePath, string ExecutableSha256, string IdentitySha256,
    int Major, string Architecture, int Bitness);
public sealed record MmcRuntimeDirectorySnapshot(string ViewKey, string NativeDirectory, string AssetsRoot, string GameAssetsRoot);
public sealed record MmcRuntimeSettingsSnapshot(string DisplayName, string LauncherName, string LauncherVersion,
    int MinimumMemoryMiB, int MaximumMemoryMiB, int ResolutionWidth, int ResolutionHeight, Guid SessionId) {
    public MmcRuntimeWrapperMode WrapperMode { get; init; } = MmcRuntimeWrapperMode.JavaWrapper;
    public ImmutableArray<string> UserJvmTokens { get; init; } = [];
    public ImmutableArray<string> ExtraGameTokens { get; init; } = [];
    public ImmutableArray<MmcJavaConstraint> PlatformJavaConstraints { get; init; } = [];
    public string StreamEncodingName { get; init; } = "UTF-8";
    public bool AppendResolutionArguments { get; init; } = true;
    public bool Fullscreen { get; init; }
}

/// <summary>Only live launch values; intentionally redacted ToString and no serializer/persistence API.</summary>
public sealed class MmcRuntimeAccountSnapshot {
    public string Name { get; }
    public string Uuid { get; }
    public string AccessToken { get; }
    public string Session { get; }
    public string UserType { get; }
    public string UserPropertiesJson { get; }
    public string? ClientToken { get; }
    public string? ClientId { get; }
    public string? Xuid { get; }
    public ImmutableArray<MmcJavaConstraint> JavaConstraints { get; }
    public MmcRuntimeAccountSnapshot(string name, string uuid, string accessToken, string session, string userType,
        string userPropertiesJson, string? clientToken = null, string? clientId = null, string? xuid = null,
        ImmutableArray<MmcJavaConstraint> javaConstraints = default) {
        Name = name; Uuid = uuid; AccessToken = accessToken; Session = session; UserType = userType;
        UserPropertiesJson = userPropertiesJson; ClientToken = clientToken; ClientId = clientId; Xuid = xuid;
        JavaConstraints = javaConstraints.IsDefault ? [] : javaConstraints;
    }
    public override string ToString() => "MMC runtime account snapshot [redacted]";
}
public sealed record MmcRuntimeLaunchRequest(MmcInstanceDefinition Definition, MmcBoundLaunchPlan BoundPlan,
    MmcInstancePaths Paths, ImmutableArray<MmcRuntimeArtifactTarget> Targets, MmcRuntimeJavaSnapshot Java,
    MmcRuntimeSettingsSnapshot Settings, MmcRuntimeAccountSnapshot Account, MmcRuntimeDirectorySnapshot Directories,
    MmcRuntimeSupportArtifact? Wrapper = null, MmcRuntimeSupportArtifact? Bridge = null) {
    [System.Text.Json.Serialization.JsonIgnore]
    public MmcMutableRuntimeAuthority? RuntimeAuthority { get; init; }
}
public sealed record MmcRuntimeInputObservation(string Identity, string ActualPath, string Sha256, long SizeBytes);
public sealed record MmcRuntimeMutableDirectories(string SessionRoot, string TemporaryDirectory, string ExtractDirectory) {
    public MmcSessionRuntimeBinding RuntimeBinding { get; init; } = null!;
}

/// <summary>No Start method: the existing instance/session admission service must revalidate and create the process.</summary>
public sealed class MmcRuntimeLaunchPlan {
    private readonly ImmutableArray<string> arguments;
    private readonly ImmutableArray<string> secrets;
    private readonly string encodingName;
    public MmcRuntimeLaunchRequest Request { get; }
    public ImmutableArray<string> RedactedArguments { get; }
    public ImmutableArray<string> RedactedLogicalGameArguments { get; }
    public ImmutableArray<string> LogicalJvmArguments { get; }
    // Live values may contain credentials. Inspection uses them internally;
    // they are never public/log-safe fields or persistence/fingerprint inputs.
    internal ImmutableArray<string> EffectiveJvmArguments { get; }
    public ImmutableArray<string> NonSensitiveAdditionalJvmArguments { get; }
    public ImmutableArray<string> RequiredClassNames { get; }
    public ImmutableArray<string> OrderedGameClasspath { get; }
    public ImmutableArray<MmcRuntimeInputObservation> Inputs { get; }
    public MmcRuntimeMutableDirectories MutableDirectories { get; }
    public MmcSessionRuntimeBinding RuntimeBinding=>MutableDirectories.RuntimeBinding;
    public string NonSensitiveArgumentFingerprint { get; }
    public string RuntimeInputFingerprint { get; }
    internal MmcRuntimeLaunchPlan(MmcRuntimeLaunchRequest request, ImmutableArray<string> arguments,
        ImmutableArray<string> redactedArguments, ImmutableArray<string> redactedGameArguments, ImmutableArray<string> logicalJvmArguments,
        ImmutableArray<string> additionalJvm, ImmutableArray<string> requiredClasses, ImmutableArray<string> classpath, ImmutableArray<MmcRuntimeInputObservation> inputs, MmcRuntimeMutableDirectories mutable,
        string argumentFingerprint, string inputFingerprint, ImmutableArray<string> secrets, ImmutableArray<string> effectiveJvmArguments) {
        Request = request; this.arguments = arguments; RedactedArguments = redactedArguments;
        RedactedLogicalGameArguments = redactedGameArguments; LogicalJvmArguments = logicalJvmArguments;
        NonSensitiveAdditionalJvmArguments = additionalJvm; RequiredClassNames = requiredClasses;
        OrderedGameClasspath = classpath; Inputs = inputs; MutableDirectories = mutable;
        NonSensitiveArgumentFingerprint = argumentFingerprint; RuntimeInputFingerprint = inputFingerprint;
        this.secrets = secrets; encodingName = request.Settings.StreamEncodingName;
        EffectiveJvmArguments = effectiveJvmArguments;
    }
    public ProcessStartInfo CreateStartInfo() {
        if (OperatingSystem.IsWindows()) MmcRuntimeWindowsCommandLine.Validate(Request.Java.ExecutablePath, arguments);
        var start = new ProcessStartInfo(Request.Java.ExecutablePath) {
            UseShellExecute = false, WorkingDirectory = Request.Paths.GameRoot, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true,
            StandardOutputEncoding = Encoding.GetEncoding(encodingName), StandardErrorEncoding = Encoding.GetEncoding(encodingName)
        };
        foreach (string argument in arguments) start.ArgumentList.Add(argument);
        // These environment channels would inject unobserved JVM/agent options.
        foreach (string key in new[] { "JAVA_TOOL_OPTIONS", "JDK_JAVA_OPTIONS", "_JAVA_OPTIONS", "CLASSPATH" }) start.Environment.Remove(key);
        start.Environment["TEMP"] = MutableDirectories.TemporaryDirectory; start.Environment["TMP"] = MutableDirectories.TemporaryDirectory;
        return start;
    }
    public string Redact(string text) {
        foreach (string secret in secrets.Where(s => s.Length >= 4).OrderByDescending(s => s.Length)) text = text.Replace(secret, "[redacted]", StringComparison.Ordinal);
        return text;
    }
    public override string ToString() => "MMC runtime launch plan [arguments redacted], input " + RuntimeInputFingerprint;
}
public sealed class MmcRuntimeLaunchException(string code, string message) : InvalidOperationException(message) {
    public string Code { get; } = code;
}
