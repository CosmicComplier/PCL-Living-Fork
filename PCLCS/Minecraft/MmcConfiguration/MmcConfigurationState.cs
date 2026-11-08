using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace PCLCS;

// Original LF host implementation; repository LICENCE applies.
internal sealed record MmcConfigPatch(long Sequence, JToken? Value, bool Remove);
internal sealed class MmcConfigurationState {
    internal readonly string Root;
    internal readonly object Sync = new();
    internal readonly SemaphoreSlim Writer = new(1, 1);
    internal readonly ManualResetEventSlim Resumed = new(true);
    internal readonly Timer Timer;
    internal readonly Dictionary<string, MmcConfigPatch> Patches = new(StringComparer.Ordinal);
    internal readonly List<string> RejectedKeys = [];
    internal int PauseCount, ProviderCount;
    internal long Epoch;
    internal Exception? LastSaveFailure;
    private long sequence;
    private JObject snapshot;
    private bool readsInvalid;

    internal MmcConfigurationState(string root) {
        Root = root;
        using (var lease = MmcConfigurationCoordinator.PreferenceLease(root, CancellationToken.None)) snapshot = MmcConfigurationCoordinator.ReadConfig(root);
        Timer = new(static value => {
            if (value is WeakReference<MmcConfigurationState> weak && weak.TryGetTarget(out var state)) state.AutoFlush();
        }, new WeakReference<MmcConfigurationState>(this), Timeout.Infinite, Timeout.Infinite);
    }
    internal void Enqueue(string key, JToken? value, bool remove) {
        if (string.IsNullOrEmpty(key)) throw new ArgumentException("配置键不能为空。", nameof(key));
        lock (Sync) {
            Patches[key] = new(++sequence, value?.DeepClone(), remove);
            if (PauseCount == 0) Timer.Change(500, Timeout.Infinite);
        }
    }
    internal JToken? Read(string key, out bool present) {
        bool refresh;
        lock (Sync) refresh = readsInvalid && PauseCount == 0;
        if (refresh) Reload();
        lock (Sync) {
            if (Patches.TryGetValue(key, out var patch)) { present = !patch.Remove; return patch.Value?.DeepClone(); }
            present = snapshot.TryGetValue(key, out var token); return token?.DeepClone();
        }
    }
    internal void InvalidateReads() { lock (Sync) { readsInvalid = true; Epoch++; } }
    internal Dictionary<string, MmcConfigPatch> CapturePatches() { lock (Sync) return new(Patches, StringComparer.Ordinal); }
    internal void Publish(JObject disk, Dictionary<string, MmcConfigPatch>? applied = null) {
        lock (Sync) {
            snapshot = (JObject)disk.DeepClone(); readsInvalid = false; Epoch++;
            if (applied is not null) foreach (var pair in applied)
                if (Patches.TryGetValue(pair.Key, out var current) && current.Sequence == pair.Value.Sequence) Patches.Remove(pair.Key);
        }
    }
    internal void Reject(Dictionary<string, MmcConfigPatch> rejected) {
        lock (Sync) foreach (var pair in rejected) {
            if (Patches.TryGetValue(pair.Key, out var current) && current.Sequence == pair.Value.Sequence) Patches.Remove(pair.Key);
            RejectedKeys.Add(pair.Key);
        }
    }
    internal void Flush(CancellationToken token) {
        while (true) {
            Resumed.Wait(token); Writer.Wait(token);
            bool paused;
            lock (Sync) paused = PauseCount != 0;
            if (paused) { Writer.Release(); continue; }
            try { FlushUnderWriter(token); return; }
            finally { Writer.Release(); }
        }
    }
    private void FlushUnderWriter(CancellationToken token) {
        var pending = CapturePatches(); if (pending.Count == 0) return;
        try {
            using var lease = MmcConfigurationCoordinator.PreferenceLease(Root, token);
            var management = MmcManagementProbe.Read(Root);
            MmcConfigurationCoordinator.AssertPreferenceWritable(management);
            JObject current = MmcConfigurationCoordinator.ReadConfig(Root);
            var rejected = pending.Where(p => MmcConfigurationCoordinator.IsAlwaysReserved(p.Key) ||
                management.State != MmcManagementState.Legacy && MmcConfigurationCoordinator.IsStructuralKey(p.Key)).ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
            foreach (string key in rejected.Keys) pending.Remove(key);
            Reject(rejected);
            MmcConfigurationCoordinator.Apply(current, pending);
            if (pending.Count != 0) MmcConfigurationCoordinator.WritePreferenceConfig(Root, current);
            Publish(current, pending); LastSaveFailure = null;
            if (rejected.Count != 0) throw new IOException("拒绝过期或未授权的实例结构配置修改。请通过实例维护流程修改。");
        } catch (Exception error) { LastSaveFailure = error; throw; }
    }
    private void Reload() {
        Writer.Wait();
        try {
            lock (Sync) { if (PauseCount != 0) return; }
            using var lease = MmcConfigurationCoordinator.PreferenceLease(Root, CancellationToken.None);
            Publish(MmcConfigurationCoordinator.ReadConfig(Root));
        } finally { Writer.Release(); }
    }
    private void AutoFlush() {
        lock (Sync) { if (PauseCount != 0) return; }
        try { Flush(CancellationToken.None); }
        catch (Exception error) { LastSaveFailure = error; } // Exposed to explicit flush/shutdown; never invoke UI under a lease.
    }
}
