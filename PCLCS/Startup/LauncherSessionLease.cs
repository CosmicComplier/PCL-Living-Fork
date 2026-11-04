using System;
using System.Collections.Generic;
using System.Threading;

namespace PCLCS.Startup;

// LF-only LauncherSessionLock.vb at 419909eb9c5a1404924d26f374eb8864f64f7521.
// BCL only: acquiring the shared official/LF mutexes must not initialize settings,
// logging, Shared paths, WPF or any account service. LF LICENCE remains applicable.
public sealed class LauncherSessionLease : IDisposable {
    private readonly List<Mutex> held = new();
    private LauncherSessionLease() { }
    public static LauncherSessionLease? TryAcquire(IEnumerable<string> names, int timeout) {
        LauncherSessionLease lease = new();
        try {
            foreach (string name in names) {
                Mutex gate = new(false, name);
                bool acquired;
                try {
                    try { acquired = gate.WaitOne(timeout); }
                    catch (AbandonedMutexException) { acquired = true; }
                } catch { gate.Dispose(); throw; }
                if (!acquired) { gate.Dispose(); lease.Dispose(); return null; }
                lease.held.Add(gate);
            }
            return lease;
        } catch { lease.Dispose(); throw; }
    }
    public void Dispose() {
        for (int i = held.Count - 1; i >= 0; i--) {
            try { held[i].ReleaseMutex(); }
            finally { held[i].Dispose(); }
        }
        held.Clear();
    }
}
