using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PCLCS;

// Original LF host implementation; repository LICENCE applies.
public static class MmcConfigurationCoordinator {
    // This is only a path-gate admission identity before reading disk, never an instance identity.
    private static readonly Guid PreferenceAdmissionIdentity = new("649a06b5-2b88-4f56-9abd-139c4d39cdf9");
    private static readonly ConcurrentDictionary<string, WeakReference<MmcConfigurationState>> states = new(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    internal static Action<string, string>? OperationObserver;
    private static readonly HashSet<string> structural = new(StringComparer.Ordinal) {
        "MinecraftInstallManifest", "pcllfManagement", "instanceId", "generation", "managementKind", "schemaVersion", "formatProfile", "layout",
        "sourceBundle", "artifactInventory", "artifactLocks", "installedProjection", "componentOverrides"
    };
    internal static MmcConfigurationState StateFor(string versionRoot) {
        string root = MmcCanonicalPaths.ForAdmission(MmcInstancePaths.ValidateVersionRoot(versionRoot));
        while (true) {
            if (states.TryGetValue(root, out var weak) && weak.TryGetTarget(out var found)) return found;
            var created = new MmcConfigurationState(root); var candidate = new WeakReference<MmcConfigurationState>(created);
            if (weak is null ? states.TryAdd(root, candidate) : states.TryUpdate(root, candidate, weak)) return created;
            created.Timer.Dispose();
        }
    }
    /// <summary>Pause/drain before acquiring AdmissionGate; do not call while holding UI/Worker/data locks.</summary>
    public static MmcConfigurationQuiescence Quiesce(string versionRoot, CancellationToken cancellationToken = default) {
        MmcConfigurationState state = StateFor(versionRoot);
        lock (state.Sync) { state.PauseCount++; state.Resumed.Reset(); state.Timer.Change(Timeout.Infinite, Timeout.Infinite); }
        try { OperationObserver?.Invoke(state.Root, "QuiescePaused"); state.Drained.Wait(cancellationToken); return new(state); }
        catch {
            lock (state.Sync) { if (--state.PauseCount == 0) state.Resumed.Set(); }
            throw;
        }
    }
    public static bool IsStructuralKey(string key) => structural.Contains(key);
    internal static bool IsAlwaysReserved(string key) => key != "MinecraftInstallManifest" && structural.Contains(key);
    internal static MmcAdmissionLease PreferenceLease(string root, CancellationToken token) => MmcAdmissionGate.Acquire(root, PreferenceAdmissionIdentity, Timeout.InfiniteTimeSpan, token);
    internal static MmcAdmissionLease? TryPreferenceLease(string root) {
        try { return MmcAdmissionGate.Acquire(root, PreferenceAdmissionIdentity, TimeSpan.Zero); }
        catch (IOException) { return null; }
    }
    internal static void AssertPreferenceWritable(MmcManagementResult management) {
        if (management.State == MmcManagementState.Legacy && management.ObservationComplete || management.State == MmcManagementState.ManagedMmc) return;
        throw new IOException("实例管理状态尚未确认，配置修改已排队并等待恢复。");
    }
    internal static JObject ReadConfig(string root) {
        string path = MmcInstancePaths.Resolve(root).ResolveRelative(MmcPathRole.Instance, "PCL/config.json");
        if (MmcInstancePaths.Attributes(path) is null) return [];
        using FileStream file = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (file.Length > MmcManagementReader.MaximumDocumentBytes) throw new IOException("实例配置超过读取预算。");
        using StreamReader reader = new(file, new UTF8Encoding(false, true), true);
        string text = reader.ReadToEnd();
        if (Encoding.UTF8.GetByteCount(text) > MmcManagementReader.MaximumDocumentBytes) throw new IOException("实例配置读取期间超过预算。");
        try { return JObject.Parse(text, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error }); }
        catch (Newtonsoft.Json.JsonException) { throw new IOException("实例配置 JSON 无效，已保留原文件。"); }
    }
    internal static void Apply(JObject target, Dictionary<string, MmcConfigPatch> patches) {
        foreach (var pair in patches) {
            if (pair.Value.Remove) target.Remove(pair.Key);
            else target[pair.Key] = pair.Value.Value?.DeepClone() ?? JValue.CreateNull();
        }
    }
    internal static byte[] ConfigBytes(JObject value) => new UTF8Encoding(false).GetBytes(value.ToString(Formatting.Indented));
    internal static void WritePreferenceConfig(string root, JObject value) {
        string path = MmcInstancePaths.Resolve(root).ResolveRelative(MmcPathRole.Instance, "PCL/config.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporary = path + ".preferences-" + Guid.NewGuid().ToString("N");
        byte[] bytes = ConfigBytes(value); MmcDurableFiles.WriteNew(temporary, bytes);
        if (MmcInstancePaths.Attributes(path) is null) File.Move(temporary, path, false);
        else File.Replace(temporary, path, null, false);
        if (MmcDurableFiles.Observe(path) != MmcDurableFiles.Hash(bytes)) throw new IOException("实例配置写入后校验失败。");
    }
}

public sealed class MmcConfigurationQuiescence : IDisposable {
    internal readonly MmcConfigurationState State;
    private bool disposed;
    internal MmcConfigurationQuiescence(MmcConfigurationState state) => State = state;
    public MmcConfigurationWriteScope OpenStructuralWrite(MmcAdmissionLease lease, long expectedGeneration) {
        ObjectDisposedException.ThrowIf(disposed, this); lease.AssertHeld();
        if (!string.Equals(lease.VersionRoot, State.Root, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)) throw new IOException("设置协调目录与准入租约不一致。");
        return new(State, lease, expectedGeneration);
    }
    public void Dispose() {
        if (disposed) return; disposed = true;
        lock (State.Sync) {
            if (--State.PauseCount == 0) { State.Resumed.Set(); if (State.Patches.Count != 0) State.Timer.Change(1, Timeout.Infinite); }
        }
    }
}

/// <summary>Scoped key merge feeding the file transaction; never recursively calls public Save.</summary>
public sealed class MmcConfigurationWriteScope : IDisposable {
    private readonly MmcConfigurationState state;
    private readonly MmcAdmissionLease lease;
    private readonly long generation;
    private readonly string? oldConfigHash;
    private readonly JObject config;
    private readonly Dictionary<string, MmcConfigPatch> accepted;
    private bool disposed;
    public JObject ConfigSnapshot => (JObject)config.DeepClone();
    internal MmcConfigurationWriteScope(MmcConfigurationState state, MmcAdmissionLease lease, long generation) {
        this.state = state; this.lease = lease; this.generation = generation;
        if (generation < 0) throw new ArgumentOutOfRangeException(nameof(generation));
        MmcFileTransactions.AssertNoPending(lease); MmcSessions.AssertNoActive(lease);
        var paths = MmcInstancePaths.Resolve(state.Root);
        string definitionPath = paths.ResolveRelative(MmcPathRole.Instance, "PCL/InstanceDefinition.json");
        if (generation == 0) {
            if (MmcInstancePaths.Attributes(definitionPath) is not null) throw new IOException("初始发布目标已有定义。");
        } else {
            var definition = MmcManagementProbe.Read(state.Root).Envelope;
            if (definition is null || definition.InstanceId != lease.InstanceId || definition.Generation != generation) throw new IOException("结构设置的 ExpectedGeneration 已过期或定义尚不可验证。");
        }
        oldConfigHash = MmcDurableFiles.Observe(paths.ResolveRelative(MmcPathRole.Instance, "PCL/config.json"));
        config = MmcConfigurationCoordinator.ReadConfig(state.Root);
        accepted = state.CapturePatches();
        var rejected = accepted.Where(p => MmcConfigurationCoordinator.IsStructuralKey(p.Key)).ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
        foreach (string key in rejected.Keys) accepted.Remove(key);
        state.Reject(rejected); MmcConfigurationCoordinator.Apply(config, accepted);
    }
    public void SetConfig<T>(string key, T? value) {
        ObjectDisposedException.ThrowIf(disposed, this); lease.AssertHeld(); config[key] = value is null ? JValue.CreateNull() : JToken.FromObject(value);
    }
    public void RemoveConfig(string key) { ObjectDisposedException.ThrowIf(disposed, this); lease.AssertHeld(); config.Remove(key); }
    public MmcTransactionResult Execute(MmcFileTransactionRequest request, CancellationToken cancellationToken = default,
        Action<MmcTransactionCheckpoint, int>? checkpoint = null) {
        ObjectDisposedException.ThrowIf(disposed, this); lease.AssertHeld();
        if (request.ExpectedGeneration != generation || request.InstanceId != lease.InstanceId) throw new IOException("结构设置事务与 ExpectedGeneration 不一致。");
        if (request.Mutations.Any(m => string.Equals(m.RelativePath, "PCL/config.json", StringComparison.OrdinalIgnoreCase))) throw new IOException("配置内容只能由当前设置协调作用域合成。");
        byte[] bytes = MmcConfigurationCoordinator.ConfigBytes(config);
        var mutations = request.Mutations.Concat([new MmcFileMutation("PCL/config.json", oldConfigHash, bytes, MmcDurableFiles.Hash(bytes))]).ToArray();
        MmcTransactionResult result = MmcFileTransactions.Execute(lease, request with { Mutations = mutations }, cancellationToken, checkpoint);
        if (result.Outcome == MmcTransactionOutcome.Committed) state.Publish(MmcConfigurationCoordinator.ReadConfig(state.Root), accepted);
        else if (result.Outcome == MmcTransactionOutcome.Aborted) state.Publish(MmcConfigurationCoordinator.ReadConfig(state.Root));
        else state.InvalidateReads();
        return result;
    }
    public void Dispose() => disposed = true;
}
