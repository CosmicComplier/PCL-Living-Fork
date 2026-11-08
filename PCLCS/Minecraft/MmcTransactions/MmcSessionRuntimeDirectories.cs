using System.Text;

namespace PCLCS;

// Original LF host implementation; governed by the repository LICENCE.
/// <summary>Only a proven exit or a proven pre-launch failure may retire mutable runtime files.</summary>
public static class MmcSessionRuntimeDirectories {
    private const string MarkerName = "owner.txt";
    private const int MaximumEntries = 65_536;

    internal static void ValidateLaunch(MmcInstancePaths paths, Guid sessionId, MmcSessionRuntimeBinding binding) {
        ValidateBinding(sessionId, binding);
        string live = Live(paths, sessionId), retired = Retired(paths, sessionId);
        if (MmcInstancePaths.Attributes(retired) is not null) throw new IOException("Mutable session ID has already been retired.");
        ValidateOwner(live, sessionId, binding);
    }

    /// <summary>The caller has not written Launching intent or created a process for this ID.</summary>
    public static void AbandonUnlaunched(MmcAdmissionLease lease, Guid sessionId, MmcSessionRuntimeBinding binding) {
        lease.AssertHeld(); ValidateBinding(sessionId, binding);
        MmcFileTransactions.AssertNoPending(lease); MmcSessions.AssertNoActive(lease);
        var paths = MmcInstancePaths.Resolve(lease.VersionRoot);
        if (MmcInstancePaths.Attributes(paths.ResolveRelative(MmcPathRole.Instance, "PCL/MmcSessionHistory/" + sessionId.ToString("N"))) is not null)
            throw new IOException("A previously launched session cannot be abandoned as unlaunched.");
        Retire(paths, sessionId, binding);
    }

    internal static void CompleteVerifiedExit(MmcInstancePaths paths, MmcSessionReceipt receipt) {
        if (receipt.RuntimeBinding is { } binding) Retire(paths, receipt.SessionId, binding);
    }

    private static void Retire(MmcInstancePaths paths, Guid sessionId, MmcSessionRuntimeBinding binding) {
        ValidateBinding(sessionId, binding);
        string live = Live(paths, sessionId), retired = Retired(paths, sessionId);
        bool hasLive = MmcInstancePaths.Attributes(live) is not null, hasRetired = MmcInstancePaths.Attributes(retired) is not null;
        if (hasLive && hasRetired) throw new IOException("Mutable session has conflicting live and retired directories.");
        if (!hasLive && !hasRetired) return;
        string current = hasLive ? live : retired;
        ValidateOwner(current, sessionId, binding);
        // Preflight every descendant before the first move/delete. In particular, never pass
        // an unchecked junction to the legacy recursive directory helper.
        InspectTree(current);
        if (hasLive) {
            string parent = Path.GetDirectoryName(retired)!;
            MmcInstancePaths.EnsureNoLinks(parent); Directory.CreateDirectory(parent); MmcInstancePaths.EnsureNoLinks(parent);
            Directory.Move(live, retired);
        }
        ValidateOwner(retired, sessionId, binding);
        var tree = InspectTree(retired);
        foreach (string file in tree.Files) {
            if (string.Equals(file, Path.Combine(retired, MarkerName), StringComparison.OrdinalIgnoreCase)) continue;
            MmcInstancePaths.EnsureNoLinks(file); File.Delete(file);
        }
        foreach (string directory in tree.Directories.AsEnumerable().Reverse()) {
            MmcInstancePaths.EnsureNoLinks(directory); Directory.Delete(directory, false);
        }
        // Retain this tiny non-sensitive ownership receipt for idempotent recovery. No temporary
        // native files survive successful cleanup, and no active plan refers to this retired path.
        ValidateOwner(retired, sessionId, binding);
        if (Directory.EnumerateFileSystemEntries(retired).Any(path => Path.GetFileName(path) != MarkerName))
            throw new IOException("Mutable cleanup encountered an external change; ownership evidence was preserved.");
    }

    private static (List<string> Files, List<string> Directories) InspectTree(string root) {
        List<string> files = [], directories = [];
        Stack<string> pending = new(); pending.Push(root); int count = 0;
        while (pending.TryPop(out string? directory)) {
            MmcInstancePaths.CheckDirectoryRole(directory);
            foreach (string entry in Directory.EnumerateFileSystemEntries(directory)) {
                if (++count > MaximumEntries) throw new IOException("Mutable session exceeds its cleanup entry budget.");
                MmcInstancePaths.EnsureNoLinks(entry);
                if ((File.GetAttributes(entry) & FileAttributes.Directory) != 0) { directories.Add(entry); pending.Push(entry); }
                else files.Add(entry);
            }
        }
        return (files, directories);
    }
    private static void ValidateOwner(string root, Guid id, MmcSessionRuntimeBinding binding) {
        MmcInstancePaths.CheckDirectoryRole(root);
        string marker = Path.Combine(root, MarkerName); MmcInstancePaths.EnsureNoLinks(marker);
        string expected = "PCLLF.MmcSessionMutable/v1\n" + id.ToString("N") + "\n" + binding.BoundPlanFingerprint;
        using FileStream input = new(marker, FileMode.Open, FileAccess.Read, FileShare.Read);
        byte[] bytes = Encoding.UTF8.GetBytes(expected);
        if (input.Length != bytes.Length) throw new IOException("Mutable runtime ownership marker differs from the session receipt.");
        byte[] actual = new byte[bytes.Length]; input.ReadExactly(actual);
        if (!actual.AsSpan().SequenceEqual(bytes)) throw new IOException("Mutable runtime ownership marker differs from the session receipt.");
    }
    private static void ValidateBinding(Guid sessionId, MmcSessionRuntimeBinding binding) {
        if (sessionId == Guid.Empty || binding is null || !MmcDurableFiles.IsHash(binding.BoundPlanFingerprint))
            throw new IOException("Mutable runtime session binding is invalid.");
    }
    private static string Live(MmcInstancePaths paths, Guid id) => paths.ResolveRelative(MmcPathRole.Runtime, "sessions/" + id.ToString("N"));
    private static string Retired(MmcInstancePaths paths, Guid id) => paths.ResolveRelative(MmcPathRole.Runtime, ".session-history/" + id.ToString("N"));
}
