using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;

namespace PCLCS;

// LF 50b028da5fcad2226d74d901a41d8c43a249bfb2,
// Plain Craft Launcher 2/Modules/Base/FileTaskProgress.vb; same real byte I/O policy.
public static class FileTaskProgress {
    public static Action<double>? Slice(Action<double>? report, double start, double finish) {
        if (report is null) return null;
        return value => report(start + (finish - start) * Math.Max(0, Math.Min(1, value)));
    }
    public static Action<long>? Bytes(long total, Action<double>? report) {
        if (report is null) return null;
        long completed = 0;
        return count => {
            completed = checked(completed + count);
            report(Math.Min(1, completed / Math.Max(1.0, total)));
        };
    }
    public static long Size(IEnumerable<string> paths) =>
        paths.Sum(address => File.Exists(address) ? Math.Max(1L, new FileInfo(address).Length) : 1L);
    public static string? Digest(string address, CancellationToken token, Action<double>? report) {
        if (!File.Exists(address)) return null;
        using FileStream input = File.OpenRead(address);
        using SHA1 algorithm = SHA1.Create();
        byte[] buffer = new byte[131072]; Action<long>? update = Bytes(input.Length, report);
        while (true) {
            token.ThrowIfCancellationRequested();
            int count = input.Read(buffer, 0, buffer.Length);
            if (count == 0) break;
            algorithm.TransformBlock(buffer, 0, count, buffer, 0);
            update?.Invoke(count);
        }
        algorithm.TransformFinalBlock(new byte[] { }, 0, 0);
        report?.Invoke(1);
        return "sha1:" + BitConverter.ToString(algorithm.Hash!).Replace("-", "").ToLowerInvariant();
    }
    public static void Copy(string source, string destination, CancellationToken token, Action<double>? report) {
        using (FileStream input = File.OpenRead(source))
        using (FileStream output = new(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None)) {
            byte[] buffer = new byte[131072]; Action<long>? update = Bytes(input.Length, report);
            while (true) {
                token.ThrowIfCancellationRequested();
                int count = input.Read(buffer, 0, buffer.Length);
                if (count == 0) break;
                output.Write(buffer, 0, count);
                update?.Invoke(count);
            }
            output.Flush();
        }
        report?.Invoke(1);
    }
    // Same LF task-local clamp, adapted to actual Loader getter/setter primitives.
    public static Action<double> ForTask(Func<double> read, Action<double> write) =>
        value => write(Math.Max(read(), Math.Min(0.99, value)));
}
