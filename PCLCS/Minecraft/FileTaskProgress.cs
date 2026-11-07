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
    private static long totalRead, totalWritten;
    public static long ReadBytes => Interlocked.Read(ref totalRead);
    public static long WrittenBytes => Interlocked.Read(ref totalWritten);
    public static void RecordRead(int bytes) { if (bytes > 0) Interlocked.Add(ref totalRead, bytes); }
    public static void RecordWrite(int bytes) { if (bytes > 0) Interlocked.Add(ref totalWritten, bytes); }
    public sealed class IoRateSampler {
        private long stamp = System.Diagnostics.Stopwatch.GetTimestamp(), read = ReadBytes, written = WrittenBytes;
        public double ReadPerSecond { get; private set; }
        public double WritePerSecond { get; private set; }
        public void Reset() { stamp = System.Diagnostics.Stopwatch.GetTimestamp(); read = ReadBytes; written = WrittenBytes; ReadPerSecond = WritePerSecond = 0; }
        public void Sample() {
            long now = System.Diagnostics.Stopwatch.GetTimestamp();
            double seconds = (now - stamp) / (double)System.Diagnostics.Stopwatch.Frequency;
            if (seconds < 0.25) return;
            long nextRead = ReadBytes, nextWritten = WrittenBytes;
            ReadPerSecond = Math.Max(0, nextRead - read) / seconds; WritePerSecond = Math.Max(0, nextWritten - written) / seconds;
            stamp = now; read = nextRead; written = nextWritten;
        }
    }
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
            RecordRead(count);
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
                RecordRead(count);
                if (count == 0) break;
                output.Write(buffer, 0, count);
                RecordWrite(count);
                update?.Invoke(count);
            }
            output.Flush();
        }
        report?.Invoke(1);
    }
    public static void CopyStream(Stream input, Stream output, CancellationToken token) {
        byte[] buffer = new byte[131072];
        while (true) {
            token.ThrowIfCancellationRequested();
            int count = input.Read(buffer, 0, buffer.Length); RecordRead(count);
            if (count == 0) break;
            output.Write(buffer, 0, count); RecordWrite(count);
        }
    }
    // Same LF task-local clamp, adapted to actual Loader getter/setter primitives.
    public static Action<double> ForTask(Func<double> read, Action<double> write) =>
        value => write(Math.Max(read(), Math.Min(0.99, value)));
}
