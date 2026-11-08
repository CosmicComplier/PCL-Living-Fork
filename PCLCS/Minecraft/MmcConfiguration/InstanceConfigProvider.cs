using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using MeloongCore;
using Newtonsoft.Json.Linq;

namespace PCLCS;

// Original LF host implementation; repository LICENCE applies. No Shared implementation is copied.
public interface IInstanceConfigProvider : IConfigProvider, IDisposable {
    string VersionRoot { get; }
    void Save();
    Task FlushAsync(CancellationToken cancellationToken = default);
}

/// <summary>Instance-only key-patch provider. Global/account providers retain their existing implementation.</summary>
public sealed class InstanceConfigProvider : IInstanceConfigProvider {
    private readonly MmcConfigurationState state;
    private bool disposed;
    public string VersionRoot => state.Root;
    public long Epoch => state.Epoch;
    public Exception? LastSaveFailure => state.LastSaveFailure;
    public InstanceConfigProvider(string versionRoot) {
        state = MmcConfigurationCoordinator.StateFor(versionRoot);
        lock (state.Sync) state.ProviderCount++;
        InstanceConfigProviders.Register(this);
    }
    public void Set<T>(string key, T? value, bool encrypted) {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (encrypted && value is not null) throw new NotImplementedException("加密需要改为使用识别码，现在尚未实现。");
        JToken token = value is null ? JValue.CreateNull() : JToken.FromObject(value);
        lock (state.Sync) { ObjectDisposedException.ThrowIf(disposed, this); state.Enqueue(key, token, remove: false); }
    }
    public void Remove(string key) { lock (state.Sync) { ObjectDisposedException.ThrowIf(disposed, this); state.Enqueue(key, null, remove: true); } }
    public T? Read<T>(string key, T? defaultValue, bool encrypted) {
        ObjectDisposedException.ThrowIf(disposed, this);
        JToken? token = state.Read(key, out bool present);
        if (!present) return defaultValue;
        if (token is null || token.Type is JTokenType.Null or JTokenType.Undefined) return default;
        if (!encrypted) return token.ToObject<T>();
        try {
            string plaintext = CryptographyUtils.AesDecrypt(token.Value<string>())!;
            return typeof(T) == typeof(string) ? (T)(object)plaintext : Newtonsoft.Json.JsonConvert.DeserializeObject<T>(plaintext);
        } catch (CryptographicException) { Remove(key); return defaultValue; }
    }
    public bool HasValue(string key) { ObjectDisposedException.ThrowIf(disposed, this); state.Read(key, out bool present); return present; }
    public void DiscardCache() { ObjectDisposedException.ThrowIf(disposed, this); state.InvalidateReads(); }
    public void Save() { ObjectDisposedException.ThrowIf(disposed, this); state.Flush(CancellationToken.None); }
    public Task FlushAsync(CancellationToken cancellationToken = default) {
        ObjectDisposedException.ThrowIf(disposed, this);
        return Task.Run(() => state.Flush(cancellationToken), cancellationToken);
    }
    public void Dispose() {
        lock (state.Sync) { if (disposed) return; disposed = true; }
        try { state.Flush(CancellationToken.None); }
        catch { lock (state.Sync) disposed = false; throw; }
        lock (state.Sync) {
            state.ProviderCount--;
            if (state.ProviderCount == 0) state.Timer.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }
}

public static class InstanceConfigProviders {
    private static readonly ConcurrentBag<WeakReference<InstanceConfigProvider>> providers = [];
    internal static void Register(InstanceConfigProvider provider) => providers.Add(new(provider));
    public static void SaveAll() {
        List<Exception> errors = [];
        foreach (var weak in providers)
            if (weak.TryGetTarget(out var provider)) {
                try { provider.Save(); } catch (ObjectDisposedException) { } catch (Exception error) { errors.Add(error); }
            }
        if (errors.Count != 0) throw new AggregateException("实例配置尚未全部保存。", errors);
    }
    /// <summary>Fixture/owned-root shutdown hook; no provider under an unrelated root is flushed or disposed.</summary>
    public static void FlushAndDisposeWithin(string ownedRoot) {
        string prefix = Path.TrimEndingDirectorySeparator(Path.GetFullPath(ownedRoot)) + Path.DirectorySeparatorChar;
        List<Exception> errors = [];
        foreach (var weak in providers) {
            if (!weak.TryGetTarget(out var provider) || !provider.VersionRoot.StartsWith(prefix, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)) continue;
            try { provider.Dispose(); } catch (Exception error) { errors.Add(error); }
        }
        if (errors.Count != 0) throw new AggregateException("独立目录的实例配置收尾失败。", errors);
    }
}
