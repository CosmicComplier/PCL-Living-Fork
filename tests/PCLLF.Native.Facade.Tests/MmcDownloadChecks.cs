using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using PCL;
using PCLCS;
using PCLCS.Network;
using PCLLF.MMCC.Core;

namespace PCLLF.Native.Facade.Tests;

internal static class MmcDownloadChecks {
    internal const string Group = "mmc-download";
    private const string ChildSwitch = "--mmc-download-child", ReceiptName = "mmc-download-complete.json";
    private static readonly string[] Managers = ["NetManager ThreadStarter 0", "NetManager ThreadStarter 1", "NetManager StatRefresher"];
    private static Scope? retained;

    internal static void Run(FixtureEnvironment environment) {
        string[] args = Environment.GetCommandLineArgs().Skip(1).ToArray();
        int child = Array.IndexOf(args, ChildSwitch);
        if (child < 0) { RunOwnedChild(); return; }
        try {
            Native.Check(args.Length == 7 && child == 2 && args[0] == "--groups" && args[1] == Group && args[4] == "--isolated" &&
                args[5] == "--fixture" && Native.Full(args[6]) == Native.Full(environment.Root) && Guid.TryParseExact(args[3], "N", out _), "MMC download child arguments are not bound.");
            ModpackUpdateCleanup.VerifyIsolatedChild();
            retained = new(environment);
            Success(retained); Console.WriteLine("DIAG MMC HTTP success complete");
            TransferCancel(retained); Console.WriteLine("DIAG MMC transfer cancellation complete");
            CopyCancel(retained); Console.WriteLine("DIAG MMC existing-file copy cancellation complete");
            MergeCancel(retained); Console.WriteLine("DIAG MMC merge cancellation complete");
            FailureAndLimit(retained); Console.WriteLine("DIAG MMC digest recovery/size limit complete");
            retained.Finish();
            environment.AssertNoForbiddenIo();
            Native.Check(System.Windows.Application.Current.Windows.Count == 0, "MMC downloader opened a window.");
            string receipt = string.Join("\n", args[3], Environment.ProcessId, Native.Full(environment.Root), MainHash(), "all-http-copy-merge-drained");
            string temp = Path.Combine(environment.Root, ReceiptName + ".pending");
            using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { file.Write(Encoding.UTF8.GetBytes(receipt)); file.Flush(true); }
            Console.WriteLine("PASS mmc-download: genuine Main NetFile/Loader batch HTTP, checksums/size/limits, copy/transfer/merge cancellation drain and retry; retained original manager lifetime, no game.");
            Console.Out.Flush(); Console.Error.Flush(); File.Move(temp, Path.Combine(environment.Root, ReceiptName));
            using ManualResetEventSlim lifetime = new(false); lifetime.Wait();
        } catch (Exception error) {
            System.IO.File.WriteAllText(Path.Combine(environment.Root, ReceiptName + ".failed"), error.ToString());
            ModpackUpdateCleanup.TerminateChild("MMC download child failed with live guards retained: " + error);
        }
    }

    private static void Success(Scope scope) {
        using ManualResetEventSlim first = new(), second = new(), release = new();
        byte[] a = Encoding.UTF8.GetBytes("first controlled Main transfer"), b = Encoding.UTF8.GetBytes("second controlled Main transfer");
        scope.Server.Add("/first", a, first, release); scope.Server.Add("/second", b, second, release);
        var files = new[] { scope.File("/first", "first.bin", a), scope.File("/second", "second.bin", b) }.ToImmutableArray();
        int loaders = 0; ModNet.LoaderDownload? actual = null;
        Task run = MmcDownloads.RunBatchAsync(files, onLoaderCreated: loader => { Interlocked.Increment(ref loaders); actual = loader; });
        scope.Until(() => first.IsSet && second.IsSet, "both HTTP requests in the one native batch");
        Native.Check(loaders == 1 && actual!.Files.Count == 2 && actual.GetType().Assembly == Native.Main, "MMC batch was split into serial/fake loaders.");
        release.Set(); scope.Complete(run);
        Native.Check(File.ReadAllBytes(files[0].LocalPath).SequenceEqual(a) && File.ReadAllBytes(files[1].LocalPath).SequenceEqual(b), "Actual Main HTTP bytes differ.");
        scope.NoWriters(actual!);
    }

    private static void TransferCancel(Scope scope) {
        using ManualResetEventSlim entered = new(), release = new(); using CancellationTokenSource token = new();
        byte[] bytes = new byte[2048]; scope.Server.Add("/transfer-cancel", bytes, entered, release);
        var file = scope.File("/transfer-cancel", "transfer-cancel.bin", bytes); ModNet.LoaderDownload? loader = null;
        Task run = MmcDownloads.RunBatchAsync([file], token.Token, onLoaderCreated: current => loader = current);
        scope.Until(() => entered.IsSet && file.ActiveIoProducerCount > 0, "owned transfer starts"); scope.Capture(file);
        token.Cancel();
        Native.Check(!run.IsCompleted && file.ActiveIoProducerCount > 0, "Canceled state was mistaken for transfer completion.");
        release.Set(); scope.Canceled(run); scope.NoWriters(loader!);
    }

    private static void CopyCancel(Scope scope) {
        using ManualResetEventSlim entered = new(), release = new(); using CancellationTokenSource token = new();
        byte[] bytes = Enumerable.Range(0, 2048).Select(x => (byte)x).ToArray(); scope.Server.Add("/copy-never", bytes);
        scope.CopyBarrier = (entered, release);
        ImmutableArray<ModNet.NetFile> Files(string prefix) => Enumerable.Range(0, 80).Select(index => {
            string relative = "fixture/copy-" + index + ".bin";
            string source = Path.Combine(scope.SourceGame, "libraries", relative); Directory.CreateDirectory(Path.GetDirectoryName(source)!); File.WriteAllBytes(source, bytes);
            return scope.File("/copy-never", prefix + "/libraries/" + relative, bytes);
        }).ToImmutableArray();
        var files = Files("copy-cancel"); ModNet.LoaderDownload? loader = null;
        Task run = MmcDownloads.RunBatchAsync(files, token.Token, onLoaderCreated: current => loader = current);
        scope.Until(() => entered.IsSet && ((ILegacyNetworkDownload<ModNet.NetFile>)loader!).CoreRecord.ActiveIoProducerCount > 0, "copy producer enters");
        token.Cancel(); Native.Check(!run.IsCompleted, "Canceled copy worker was not drained."); release.Set(); scope.Canceled(run); scope.NoWriters(loader!);
        scope.CopyBarrier = null;
        var copied = Files("copy-success");
        Task successful = MmcDownloads.RunBatchAsync(copied, onLoaderCreated: current => loader = current); scope.Complete(successful);
        Native.Check(copied.All(file => File.ReadAllBytes(file.LocalPath).SequenceEqual(bytes)) && !scope.Server.Requested("/copy-never"), "Real existing-file copy path was bypassed.");
        scope.NoWriters(loader!);
    }

    private static void MergeCancel(Scope scope) {
        using ManualResetEventSlim entered = new(), body = new(), mergeStarts = new(), continueMerge = new(), merge = new(), finish = new(); using CancellationTokenSource token = new();
        byte[] bytes = new byte[4096]; scope.Server.Add("/merge-cancel", bytes, entered, body);
        var file = scope.File("/merge-cancel", "merge-cancel.bin", bytes);
        file.MergeStarting += () => { mergeStarts.Set(); if (!continueMerge.Wait(10000)) throw new TimeoutException("Owned merge phase was not released."); };
        Native.Field(file.GetType(), "CanceledMergeFinished").SetValue(file, new Action(() => { merge.Set(); if (!finish.Wait(10000)) throw new TimeoutException("Owned merge release was not signaled."); }));
        ModNet.LoaderDownload? loader = null; Task run = MmcDownloads.RunBatchAsync([file], token.Token, onLoaderCreated: current => loader = current);
        scope.Until(() => entered.IsSet, "merge HTTP enters"); scope.Capture(file);
        body.Set(); scope.Until(() => mergeStarts.IsSet, "merge state before file publication");
        token.Cancel(); Native.Check(!run.IsCompleted && file.ActiveIoProducerCount > 0, "Canceled merge phase was not retained.");
        continueMerge.Set();
        scope.Until(() => merge.IsSet, "merge cleanup callback");
        Native.Check(!run.IsCompleted && file.ActiveIoProducerCount > 0, "Merge cleanup escaped the I/O barrier.");
        finish.Set();
        scope.Canceled(run); scope.NoWriters(loader!);
    }

    private static void FailureAndLimit(Scope scope) {
        byte[] bytes = Encoding.UTF8.GetBytes("checksum failure and recovery"); scope.Server.Add("/wrong-hash", bytes);
        var wrong = scope.File("/wrong-hash", "retry.bin", bytes, wrongHash: true); ModNet.LoaderDownload? loader = null;
        Task run = MmcDownloads.RunBatchAsync([wrong], onLoaderCreated: current => loader = current); scope.Failed(run); scope.NoWriters(loader!);
        var good = scope.File("/wrong-hash", "retry.bin", bytes); scope.Complete(MmcDownloads.RunBatchAsync([good], onLoaderCreated: current => loader = current));
        Native.Check(File.ReadAllBytes(good.LocalPath).SequenceEqual(bytes), "Failure recovery did not download exact bytes."); scope.NoWriters(loader!);
        scope.Server.Add("/oversized", new byte[1024]);
        var maximum = scope.File("/oversized", "oversized.bin", new byte[1024], maximum: 4, omitSizeAndHash: true);
        scope.Failed(MmcDownloads.RunBatchAsync([maximum], onLoaderCreated: current => loader = current)); scope.NoWriters(loader!);
    }

    private sealed class Scope {
        internal readonly FixtureEnvironment Environment;
        internal readonly PackRootInstallChecksLoopback Server = new();
        internal readonly ConcurrentBag<Thread> Workers = new(), Transfers = new();
        internal readonly string Root, SourceGame;
        internal (ManualResetEventSlim Entered, ManualResetEventSlim Release)? CopyBarrier;
        internal Scope(FixtureEnvironment environment) {
            Environment = environment; Root = Path.Combine(environment.Data, "mmc-download"); SourceGame = Path.Combine(Root, "existing-game");
            Directory.CreateDirectory(Path.Combine(SourceGame, "versions"));
            var host = (LegacyNetworkHost<ModNet.NetFile, ModNet.LoaderDownload, ModNet.NetManagerClass, ModLoader.LoaderBase>)Native.Field(typeof(ModNet), "NativeHost").GetValue(null)!;
            Native.Check(ModNet.NetManager.FileRemain == 0 && ModNet.NetManager.Tasks.IsEmpty, "MMC requires a fresh native download manager.");
            host.SignCdn = value => Server.Owns(value) ? value : throw new InvalidOperationException("MMC fixture transport escaped owned HTTP.");
            var sign = host.SignHeaders;
            host.SignHeaders = (string url, ref HttpRequestMessage request, bool browser, bool credentials) => {
                Native.Check(Server.Owns(url) && !credentials, "MMC sent credential-enabled or external request."); sign(url, ref request, browser, credentials);
                Native.Check(!request.Headers.Contains("Authorization") && !request.Headers.Contains("x-api-key"), "MMC loopback carried credentials.");
            };
            host.TempFolder = () => Path.Combine(Root, "network-temp") + Path.DirectorySeparatorChar;
            host.RequestTempFolder = () => Path.Combine(Root, "network-work-" + Guid.NewGuid().ToString("N")) + Path.DirectorySeparatorChar;
            host.AppData = () => Path.Combine(Root, "app-data") + Path.DirectorySeparatorChar; host.MinecraftFolders = () => [SourceGame + Path.DirectorySeparatorChar];
            var start = host.RunNewThread;
            host.RunNewThread = (work, name, priority) => {
                Thread thread = start(() => {
                    var barrier = CopyBarrier;
                    if (name.StartsWith("下载 文件复制", StringComparison.Ordinal) && barrier is { } held) {
                        held.Entered.Set(); if (!held.Release.Wait(10000)) throw new TimeoutException("Owned copy producer was not released.");
                    }
                    work();
                }, name, priority); Workers.Add(thread); return thread;
            };
            var integer = host.IntegerSetting; host.IntegerSetting = name => name == "ToolDownloadThread" ? 4 : integer(name);
        }
        internal ModNet.NetFile File(string route, string relative, byte[] bytes, bool wrongHash = false, long? maximum = null, bool omitSizeAndHash = false) {
            string hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
            var source = new MmcFieldSource("fixture", "1", "owned-http", new string('1', 64), "libraries", 0);
            var artifact = new MmcArtifactReference(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(relative))), "fixture", new string('1', 64),
                MmcArtifactRole.Classpath, MmcArtifactScope.Shared, null, relative.Replace('\\', '/'), [Server.Url(route)], null, hash, bytes.Length, null, [], source);
            return MmcDownloads.BuildFile(new(artifact, [artifact.Key], Path.Combine(Root, relative.Replace('/', Path.DirectorySeparatorChar)), [Server.Url(route)],
                omitSizeAndHash ? null : wrongHash ? new string('0', 64) : hash, omitSizeAndHash ? null : Convert.ToHexStringLower(SHA1.HashData(bytes)),
                omitSizeAndHash ? null : bytes.Length, maximum ?? 1024 * 1024));
        }
        internal void Until(Func<bool> condition, string phase) {
            DateTime deadline = DateTime.UtcNow.AddSeconds(15);
            while (!condition()) { Native.Call(typeof(FileTaskScope), "DrainPostedUi", null, deadline); if (DateTime.UtcNow >= deadline) throw new TimeoutException("MMC download phase: " + phase); Thread.Sleep(2); }
        }
        internal void Complete(Task work) { Until(() => work.IsCompleted, "batch completion"); work.GetAwaiter().GetResult(); }
        internal void Canceled(Task work) { Until(() => work.IsCompleted, "canceled batch drain"); try { work.GetAwaiter().GetResult(); } catch (OperationCanceledException) { return; } throw new Exception("Expected canceled MMC batch."); }
        internal void Failed(Task work) { Until(() => work.IsCompleted, "failed batch drain"); try { work.GetAwaiter().GetResult(); } catch (Exception error) when (error is not OperationCanceledException) { return; } throw new Exception("Expected failed MMC batch."); }
        internal void Capture(ModNet.NetFile file) { lock (file.LockChain) if (file.Threads is not null) foreach (var part in file.Threads) if (part.Thread is not null) Transfers.Add(part.Thread); }
        internal void NoWriters(ModNet.LoaderDownload loader) {
            foreach (var file in loader.Files) { Capture(file); Native.Check(file.ActiveIoProducerCount == 0, "MMC file retained an I/O producer after barrier."); }
            Native.Check(((ILegacyNetworkDownload<ModNet.NetFile>)loader).CoreRecord.ActiveIoProducerCount == 0, "MMC loader retained copy/start producers.");
            Until(() => Transfers.All(thread => !thread.IsAlive) && Workers.Where(thread => !Managers.Contains(thread.Name, StringComparer.Ordinal)).All(thread => !thread.IsAlive), "native producers exit");
        }
        internal void Finish() {
            Server.Dispose();
            Native.Check(ModNet.NetManager.FileRemain == 0 && ModNet.NetManager.Tasks.All(x => x.State is ModBase.LoadState.Finished or ModBase.LoadState.Canceled or ModBase.LoadState.Failed), "MMC actual manager retains unfinished downloads.");
            var managers = Workers.Where(thread => Managers.Contains(thread.Name, StringComparer.Ordinal)).ToArray();
            Native.Check(managers.Length == 3 && managers.All(thread => thread.IsAlive && !thread.IsBackground), "MMC changed original manager lifetime.");
        }
    }

    private static string MainHash() => Convert.ToHexStringLower(SHA256.HashData(System.IO.File.ReadAllBytes(Native.Main.Location)));
    private static void RunOwnedChild() {
        ModpackUpdateCleanup.VerifyIsolatedChild();
        string root = Path.Combine(Path.GetTempPath(), "PclNativeFacade-" + Guid.NewGuid().ToString("N")), nonce = Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(root); Program.ValidateRoot(root); Process? child = null;
        Task<string>? stdout = null, stderr = null; bool stopped = false;
        try {
            string output = Path.Combine(root, "output"); Native.Call(typeof(Program), "CopyOutput", null, AppContext.BaseDirectory, output);
            var start = new ProcessStartInfo(Path.Combine(output, Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location) + ".exe")) {
                UseShellExecute = false, WorkingDirectory = root, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
            };
            foreach (string arg in new[] { "--groups", Group, ChildSwitch, nonce, "--isolated", "--fixture", root }) start.ArgumentList.Add(arg);
            child = Process.Start(start) ?? throw new IOException("MMC owned child did not start."); stdout = child.StandardOutput.ReadToEndAsync(); stderr = child.StandardError.ReadToEndAsync();
            string receipt = Path.Combine(root, ReceiptName); var watch = Stopwatch.StartNew();
            while (!System.IO.File.Exists(receipt)) {
                if (System.IO.File.Exists(receipt + ".failed")) throw new Exception("MMC child assertions failed: " + System.IO.File.ReadAllText(receipt + ".failed"));
                if (watch.Elapsed > TimeSpan.FromSeconds(55)) throw new TimeoutException("MMC owned downloader did not prove completion.");
                if (child.WaitForExit(10)) throw new Exception("MMC child exited before its receipt: " + child.ExitCode);
            }
            Native.Check(System.IO.File.ReadAllText(receipt) == string.Join("\n", nonce, child.Id, Native.Full(root), MainHash(), "all-http-copy-merge-drained"), "MMC receipt does not bind nonce/PID/root/Main.");
            child.Kill(true); Native.Check(child.WaitForExit(5000), "MMC owned manager child did not terminate."); stopped = true;
            Native.Check(Task.WaitAll([stdout, stderr], 5000), "MMC child output did not drain.");
            Native.Check(stderr.GetAwaiter().GetResult().Length == 0, "MMC child reported failure: " + stderr.GetAwaiter().GetResult()); Console.Write(stdout.GetAwaiter().GetResult());
        } finally {
            if (child is not null) { if (!child.HasExited) { child.Kill(true); stopped = child.WaitForExit(5000); } child.Dispose(); }
            if (stdout is not null && stderr is not null && Task.WaitAll([stdout, stderr], 5000)) {
                if (stderr.Result.Length != 0) { Console.Write(stdout.Result); Console.Error.Write(stderr.Result); }
            }
            if (stopped) { Program.ValidateRoot(root); Directory.Delete(root, true); }
        }
    }
}
