using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PCL;
using PCLCS;
using static PCLLF.Native.Facade.Tests.ModpackUpdateChecks;

namespace PCLLF.Native.Facade.Tests;

// Test-ModpackUpdateUx.ps1 (15c778c2...) live Native Main controls and downloader.
// UX-* IDs preserve its release-channel/modal/preview and slow-loopback checks.
// Uses the host's genuine Application with LF Startup already detached. No Show,
// ResourceAssembly replacement, external API, game start or mouse-cache tricks.
// Observable assertion map, original Test-ModpackUpdateUx.ps1 line -> ID:
// 66 -> UX-P1; 67 -> UX-P2; 68 -> UX-P3; 73 -> UX-P4;
// 118 (Release/count/date order) -> UX-01; 120 -> UX-02;
// 122 (selected DTO + confirm enabled) -> UX-03; 124 -> UX-04;
// 126 (clear selection/disabled confirm/one Beta) -> UX-05; 128 -> UX-06;
// 145 -> UX-07; 148, all four categories -> UX-08:<kind>;
// 151, every row/count/border/tooltip in four categories -> UX-09:<kind>;
// 155 -> UX-10; 163 -> UX-11; 164 (count + conflicts unchanged) -> UX-12.
// Original 129/156/158/162 renders are retained and encoded as GUID-owned PNGs;
// UX-13 checks their creation, not visual fidelity. Original 108 C# compilation
// is now the real host project build; original STA/startup/cleanup contracts are
// supplied by Program/FixtureEnvironment, retaining actual host EntryAssembly.
internal static class ModpackUpdateUxChecks
{
    internal static void Run(string root)
    {
        ModpackUpdateCleanup.VerifyIsolatedChild();
        DownloadProgress(Path.Combine(root, "loopback"));
        VersionsAndPreview(Path.Combine(root, "preview"));
        Console.WriteLine("PASS native modpack-update UX: UX-P1..P4 and UX-01..UX-13 (actual loopback, selection/modal and read-only preview).");
    }

    private static void DownloadProgress(string folder)
    {
        Directory.CreateDirectory(folder);
        byte[] bytes = new byte[2 * 1024 * 1024]; new Random(731).NextBytes(bytes);
        string hash = Convert.ToHexString(SHA1.HashData(bytes)).ToLowerInvariant();
        using SlowLoopback server = new(bytes);
        List<ModNet.NetFile> files = new() { new(new[] { server.Url }, Path.Combine(folder, "update.jar"), new MeloongCore.FileChecker { Hash = hash }) };
        using CancellationTokenSource cancellation = new();
        using ManualResetEventSlim invocationExited = new();
        Thread? invocationThread = null;
        bool invoked = false;
        ModLoader.LoaderTask<string, string> step = new("actual pack download", task =>
        {
            Volatile.Write(ref invocationThread, Thread.CurrentThread);
            Volatile.Write(ref invoked, true);
            try
            {
                task.Progress = 0.15;
                Update("DownloadFilesWithProgress", files, cancellation.Token, task.CreateSyncProgressProvider(0.15, 0.65));
            }
            finally { invocationExited.Set(); }
        });
        bool advanced = false;
        double largest = 0.15;
        try
        {
            step.Start("fixture");
            DateTime deadline = DateTime.UtcNow.AddSeconds(25);
            while (step.State == ModBase.LoadState.Loading && DateTime.UtcNow < deadline)
            {
                double progress = step.Progress;
                if (progress > 0.15 && progress < 0.65) { advanced = true; largest = Math.Max(largest, progress); }
                Thread.Sleep(10);
            }
            Check(step.State == ModBase.LoadState.Finished, "UX-P1", "loopback download did not finish: " + step.Error);
            Check(File.Exists(files[0].LocalPath) && new FileInfo(files[0].LocalPath).Length == bytes.Length, "UX-P2", "downloaded content missing");
            Check(File.ReadAllBytes(files[0].LocalPath).SequenceEqual(bytes), "UX-P2-bytes", "download did not preserve actual bytes/hash");
            Check(advanced, "UX-P3", "reproduced 15% frozen progress despite " + server.Sent + " loopback bytes");
            using CancellationTokenSource canceled = new(); canceled.Cancel();
            bool rejected = false;
            try { Update("DownloadFilesWithProgress", files, canceled.Token, step.CreateSyncProgressProvider(0.15, 0.65)); }
            catch (OperationCanceledException) { rejected = true; }
            Check(rejected, "UX-P4", "pre-canceled real downloader restarted");
            Console.WriteLine("PASS native modpack-update parent download progressed from 15% through " + largest.ToString("P1", CultureInfo.InvariantCulture) + " before completion (loopback only).");
        }
        finally
        {
            try
            {
                // Cancel the real synchronous download before the outer task.
                // A task's terminal state alone does not mean its IO returned.
                cancellation.Cancel();
                Thread? taskThread = step.LastRunningThread;
                if (step.State == ModBase.LoadState.Loading) step.Cancel();
                DateTime deadline = DateTime.UtcNow.AddSeconds(10);
                while ((Volatile.Read(ref invoked) && !invocationExited.IsSet) || Volatile.Read(ref invocationThread)?.IsAlive == true ||
                    taskThread?.IsAlive == true || Volatile.Read(ref ModNet.NetTaskThreadCount) != 0 ||
                    files.Any(file => file.Threads is not null && file.Threads.Any(part => part.Thread?.IsAlive == true)))
                {
                    if (DateTime.UtcNow >= deadline)
                        ModpackUpdateCleanup.TerminateChild("UX downloader still active before loopback/roots disposal: state=" + step.State + ", invocationExited=" + invocationExited.IsSet + ", networkThreads=" + Volatile.Read(ref ModNet.NetTaskThreadCount));
                    Thread.Sleep(2);
                }
            }
            catch (Exception error)
            {
                ModpackUpdateCleanup.TerminateChild("UX downloader cancellation/completion barrier failed before loopback/roots disposal: " + error);
            }
        }
    }

    private static void VersionsAndPreview(string folder)
    {
        Directory.CreateDirectory(folder);
        List<ResourceVersion> versions = new();
        foreach ((string display, ResourceVersion.ReleaseTypes type, string date, string game) in new[]
        {
            ("15.0.0-beta.3 for 26.3", ResourceVersion.ReleaseTypes.Beta, "2026-11-01", "26.3"),
            ("14.1.2 for 26.2", ResourceVersion.ReleaseTypes.Release, "2026-11-01", "26.2"),
            ("14.1.1 for 26.2", ResourceVersion.ReleaseTypes.Release, "2026-10-11", "26.2"),
            ("15.0.0-alpha.5 for 26.3", ResourceVersion.ReleaseTypes.Alpha, "2026-10-05", "26.3")
        })
            versions.Add(new ResourceVersion { Id = display, Display = display, ReleaseType = type, ReleaseDate = DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture), GameVersions = new() { game }, ModLoaders = ModLoaders.Fabric });
        object selector = Construct("ModpackVersionSelectView", versions, "Fabulously Optimized", "13.0.0 for 1.21.11");
        IList selections = (IList)Get(selector, "Selections")!;
        Check(Property(selector, "ActiveChannel")!.ToString() == "Release" && selections.Count == 2 && ((MyListItem)selections[0]!).Title == "14.1.2 for 26.2", "UX-01", "channels mixed or releases not sorted by date");
        MyMsgText modal = Dialog((FrameworkElement)selector, "选择整合包版本", "下载并预览");
        MyButton confirm = (MyButton)modal.FindName("Btn1");
        Check(!confirm.IsEnabled, "UX-02", "download enabled before choosing a version");
        MyListItem second = (MyListItem)selections[1]!;
        second.Checked = true;
        Check(ReferenceEquals(Property(selector, "Selected"), second.Tag) && confirm.IsEnabled, "UX-03", "grouped selection returned wrong version or left confirm disabled");
        MyListItem first = (MyListItem)selections[0]!;
        first.Checked = true;
        Check(selections.Cast<MyListItem>().Count(item => item.Checked) == 1 && ReferenceEquals(Property(selector, "Selected"), first.Tag), "UX-04", "native radio selection not exclusive");
        Native.Call(selector.GetType(), "ShowChannel", selector, ResourceVersion.ReleaseTypes.Beta);
        selections = (IList)Get(selector, "Selections")!;
        Check(Property(selector, "Selected") is null && !confirm.IsEnabled && selections.Count == 1, "UX-05", "channel switch retained invisible selection");
        ((MyListItem)selections[0]!).Checked = true;
        Check(((ResourceVersion)Property(selector, "Selected")!).Id == "15.0.0-beta.3 for 26.3", "UX-06", "beta selection used stale ungrouped index");
        Render(modal, Path.Combine(folder, "modpack-version-selector.png"));

        object package = New("PackUpdatePackage");
        Set(package, "Name", "Fabulously Optimized"); Set(package, "Version", "15.0.0-beta.3"); Set(package, "Minecraft", "26.3");
        ((IDictionary)Get(package, "Loaders")!).Add(ModLoaders.Fabric, "0.19.3");
        object plan = New("PackUpdatePlan"); Set(plan, "Package", package);
        Dictionary<string, int> expected = new() { ["Added"] = 35, ["Updated"] = 46, ["Removed"] = 50, ["Preserved"] = 12 };
        foreach ((string kind, int count) in expected)
            for (int index = 0; index < count; index++)
            {
                object change = New("PackUpdateChange");
                string relative = "mods/" + new[] { "Iris", "Sodium", "BetterGrassify", "ConfigManager", "Continuity", "DynamicLights" }[index % 6] + "-fabric-1.21.11-" + index + ".jar";
                string reason = "作者文件发生变化";
                if (kind == "Preserved")
                {
                    Set(change, "Conflict", true); reason = "保留玩家删除或禁用";
                    if (index % 2 == 0) { relative = "config/MySettings-" + index + ".toml"; reason = "保留个人文件或修改"; }
                }
                Set(change, "Relative", relative); Set(change, "Reason", reason);
                if (kind != "Added") Set(change, "Before", "sha1:fixture-before");
                if (kind != "Removed") { object incoming = New("PackUpdateFile"); Set(incoming, "Relative", relative); Set(change, "Incoming", incoming); }
                ((IList)Get(plan, "Changes")!).Add(change);
            }
        object preview = Construct("ModpackUpdatePreviewView", plan, "1.21.11", new Dictionary<ModLoaders, string> { [ModLoaders.Fabric] = "0.19.3" });
        IDictionary counts = (IDictionary)Get(preview, "Counts")!;
        Type kinds = Native.Type("PCL.ModpackUpdatePreviewView+ChangeKind");
        Check(Property(preview, "ActiveKind")!.ToString() == "Preserved", "UX-07", "personal changes not visible by default");
        foreach ((string kind, int count) in expected)
        {
            object value = Enum.Parse(kinds, kind);
            Check((int)counts[value]! == count, "UX-08:" + kind, "incorrect category count");
            Native.Call(preview.GetType(), "ShowKind", preview, value);
            Border[] boxes = ((StackPanel)Get(preview, "Rows")!).Children.OfType<Border>().ToArray();
            Check(boxes.Length == count && boxes.All(box => box.BorderThickness.Left == 1 && !string.IsNullOrWhiteSpace(box.ToolTip?.ToString())), "UX-09:" + kind, "file border or full path tooltip missing");
        }
        Native.Call(preview.GetType(), "ShowKind", preview, Enum.Parse(kinds, "Preserved"));
        MyMsgText previewModal = Dialog((FrameworkElement)preview, "整合包更新预览", "保留个人修改并更新");
        Check(((MyButton)previewModal.FindName("Btn1")).ColorType != MyButton.ColorState.Red, "UX-10", "safe preserve action styled as destructive");
        Render(previewModal, Path.Combine(folder, "modpack-update-preview.png"));
        Native.Call(preview.GetType(), "ShowKind", preview, Enum.Parse(kinds, "Removed"));
        Render(previewModal, Path.Combine(folder, "modpack-removals-preview.png"));
        int before = Changes(plan).Count, conflicts = Changes(plan).Count(Conflict);
        ((ContentControl)previewModal.FindName("PanCaption")).Content = null;
        FrameworkElement compact = (FrameworkElement)preview; compact.MaxHeight = 240; compact.Width = 460;
        Render(compact, Path.Combine(folder, "modpack-preview-compact.png"), 470, 250);
        Check(((UniformGrid)Get(preview, "Filters")!).Columns == 2, "UX-11", "compact categories did not wrap to two columns");
        Check(Changes(plan).Count == before && Changes(plan).Count(Conflict) == conflicts, "UX-12", "preview mutated original plan");
        Check(new[] { "modpack-version-selector.png", "modpack-update-preview.png", "modpack-removals-preview.png", "modpack-preview-compact.png" }.All(name => new FileInfo(Path.Combine(folder, name)).Length > 8), "UX-13", "actual offscreen WPF renders missing");
    }

    private static object? Property(object target, string name) => Native.Property(target.GetType(), name).GetValue(target);
    private static object Construct(string name, params object?[] values)
    {
        var constructor = Native.Type("PCL." + name).GetConstructors(Native.Flags).Single(info => info.GetParameters().Length == values.Length);
        return constructor.Invoke(values);
    }
    private static MyMsgText Dialog(FrameworkElement content, string title, string button)
    {
        ModMain.MyMsgBoxConverter converter = new() { Title = title, Text = "整合包预览", Button1 = button, Button2 = "取消", IsWarn = false, ContentFactory = () => content };
        MyMsgText modal = new(converter);
        ((RotateTransform)modal.FindName("TransformRotate")).Angle = 0;
        ((TranslateTransform)modal.FindName("TransformPos")).Y = 0;
        return modal;
    }
    private static void Render(FrameworkElement element, string path, int width = 820, int height = 640)
    {
        Grid host = new() { Background = new SolidColorBrush(Color.FromRgb(231, 239, 251)) };
        host.Children.Add(element);
        try
        {
            host.Measure(new Size(width, height)); host.Arrange(new Rect(0, 0, width, height)); host.UpdateLayout();
            RenderTargetBitmap bitmap = new(width, height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(host);
            PngBitmapEncoder encoder = new(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using FileStream stream = File.Create(path); encoder.Save(stream);
        }
        finally { host.Children.Clear(); }
    }

    private sealed class SlowLoopback : IDisposable
    {
        private readonly byte[] bytes;
        private readonly TcpListener listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource stop = new();
        private readonly ConcurrentBag<TcpClient> clients = new();
        private readonly ConcurrentBag<Task> responses = new();
        private readonly Task accept;
        private int sent;
        internal string Url { get; }
        internal int Sent => Volatile.Read(ref sent);
        internal SlowLoopback(byte[] bytes)
        {
            this.bytes = bytes; listener.Start();
            Url = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port + "/update.jar";
            accept = Task.Run(Accept);
        }
        private async Task Accept()
        {
            try
            {
                while (true)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync(stop.Token).ConfigureAwait(false);
                    clients.Add(client); responses.Add(Task.Run(() => Respond(client)));
                }
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
            catch (SocketException) when (stop.IsCancellationRequested) { }
            catch (ObjectDisposedException) when (stop.IsCancellationRequested) { }
        }
        private void Respond(TcpClient client)
        {
            using (client)
                try
                {
                    client.ReceiveTimeout = 5000; client.SendTimeout = 5000;
                    NetworkStream stream = client.GetStream(); List<byte> request = new(); int current;
                    while ((current = stream.ReadByte()) >= 0)
                    {
                        request.Add((byte)current); int count = request.Count;
                        if (count > 16384) throw new IOException("Loopback request exceeded fixture limit.");
                        if (count > 3 && request[count - 4] == 13 && request[count - 3] == 10 && request[count - 2] == 13 && request[count - 1] == 10) break;
                    }
                    Match range = Regex.Match(Encoding.ASCII.GetString(request.ToArray()), @"(?im)^Range:\s*bytes=(\d+)-");
                    long start = range.Success ? long.Parse(range.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
                    if (start < 0 || start >= bytes.Length) throw new IOException("Unexpected loopback range.");
                    string header = "HTTP/1.1 " + (range.Success ? "206 Partial Content" : "200 OK") + "\r\nContent-Length: " + (bytes.Length - start) + "\r\nAccept-Ranges: bytes\r\n" + (range.Success ? "Content-Range: bytes " + start + "-" + (bytes.Length - 1) + "/" + bytes.Length + "\r\n" : "") + "Connection: close\r\n\r\n";
                    byte[] encoded = Encoding.ASCII.GetBytes(header); stream.Write(encoded);
                    for (long offset = start; offset < bytes.Length; offset += 16384)
                    {
                        if (stop.IsCancellationRequested) break;
                        int size = (int)Math.Min(16384, bytes.Length - offset); stream.Write(bytes, (int)offset, size);
                        Interlocked.Add(ref sent, size); Thread.Sleep(25);
                    }
                }
                catch (IOException) { /* Actual downloader can close resumed/racing loopback segments. */ }
                catch (SocketException) { }
                catch (ObjectDisposedException) when (stop.IsCancellationRequested) { }
        }
        public void Dispose()
        {
            stop.Cancel(); listener.Stop();
            foreach (TcpClient client in clients) client.Dispose();
            Check(accept.Wait(2000), "UX-cleanup-accept", "loopback listener did not stop");
            foreach (TcpClient client in clients) client.Dispose();
            Check(Task.WaitAll(responses.ToArray(), 2000), "UX-cleanup-responses", "loopback handlers did not stop");
            stop.Dispose();
        }
    }
}
