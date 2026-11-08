using System;
using System.Collections.Generic;
using System.Threading;

namespace PCL;

internal static class MmcOperationLoaders {
    internal static int Finishing;
    internal static bool HasBlockingCommit => Volatile.Read(ref Finishing) != 0;
}
// Original LF host integration; repository LICENCE applies.
internal sealed class MmcOperationCancellationFence {
    private readonly object gate = new();
    private readonly CancellationTokenSource source = new();
    private bool finishing, completed;
    public CancellationToken Token => source.Token;
    public void Cancel(Action standardCancel) {
        lock (gate) { if (finishing || completed) return; source.Cancel(); standardCancel(); }
    }
    public void EnterDecision(CancellationToken token) {
        lock (gate) {
            token.ThrowIfCancellationRequested();
            if (!finishing) { finishing = true; Interlocked.Increment(ref MmcOperationLoaders.Finishing); }
        }
    }
    public void Complete() {
        lock (gate) {
            if (completed) return;
            completed = true; if (finishing) Interlocked.Decrement(ref MmcOperationLoaders.Finishing); source.Dispose();
        }
    }
}

// The original scheduler reads concrete generic input types. Preserve that contract.
internal sealed class MmcOperationWorker<TIn, TOut>(MmcOperationCancellationFence fence, string name,
    Action<ModLoader.LoaderTask<TIn, TOut>> work) : ModLoader.LoaderTask<TIn, TOut>(name, work) {
    public override void Cancel() => fence.Cancel(() => base.Cancel());
}
internal sealed class MmcOperationCombo<TIn>(MmcOperationCancellationFence fence, string name,
    IEnumerable<ModLoader.LoaderBase> children) : ModLoader.LoaderCombo<TIn>(name, children) {
    public override void Cancel() => fence.Cancel(() => base.Cancel());
}
