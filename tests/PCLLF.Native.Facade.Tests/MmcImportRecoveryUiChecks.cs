using System.Collections.Concurrent;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using MeloongCore;
using Newtonsoft.Json.Linq;
using PCL;
using PCLCS;
using PCLCS.Loaders;
using PCLLF.MMCC.Core;

namespace PCLLF.Native.Facade.Tests;

internal static class MmcImportRecoveryUiChecks {
    // Run each group in its own ordinary fresh 60-second Native child.
    internal static void RunRecovery(FixtureEnvironment environment) {
        using var scope = new Scope(environment, "initial-import-recovery-ui");
        scope.RecoverFromActualSelection(); scope.ReadOnlyMovedAndUnknown();
        scope.Finish(); environment.AssertNoForbiddenIo();
        Console.WriteLine("PASS mmc-import-recovery-ui: actual empty-list Main recovery button, bounded catalog/selection, released producer Use and real committed recovery retain original ID/G1, world and sibling bytes; moved-marker and unknown catalog reads are inert, unknown actions denied. Cache-hit production batch only; no network transfer or game launch.");
    }
    internal static void RunRetry(FixtureEnvironment environment) {
        using var scope = new Scope(environment, "initial-import-retry-ui");
        scope.RetryRefusals(); scope.CancelAndDrain(); scope.RetryFromActualSelection();
        scope.Finish(); environment.AssertNoForbiddenIo();
        Console.WriteLine("PASS mmc-import-retry-ui: actual Main catalog/picker/name/confirmation, real SHA/Java policy/backend and fresh same-SHA different-name publication preserve old parked bytes with a new ID/G1; wrong/forged source SHA, forged original paths, stale preview and destination collision denied; actual Prepared cancellation drains before busy release, late decided cancel preserves success. Cache-hit production batch only; no network transfer or game launch.");
    }

    private sealed class Scope : IDisposable {
        private readonly FixtureEnvironment environment;
        private readonly FileTaskScope files;
        private readonly string root, game, archive, sibling;
        private readonly HttpClient client = new(new NoNetwork());
        private readonly MmcMetadataStore metadata;
        private readonly MmcArtifactCacheRoots cache;
        private readonly MmcRuntimeEnvironment runtime;
        private readonly Type ui = Native.Type("PCL.MmcImportRecoveryUi");
        private readonly PropertyInfo factory;
        private readonly object? oldFactory;
        private readonly JsonConfigProvider originalProfile;
        private readonly LegacyLoaderHost<ModLoader.LoaderBase, ModBase.LoadState, MyLoading.MyLoadingState> scheduler;
        private readonly Action oldRefresh;
        private readonly List<ModLoader.LoaderCombo<string>> tasks = [];
        private readonly List<MmcImportRecoveryResult> recovered = [];
        private readonly List<MmcInstanceBackendResult> retried = [];
        private readonly List<Exception> alerts = [];
        private readonly MmcImportRecoveryUiServices services;
        private readonly string siblingBefore;
        private MmcImportRecoveryEntry? selected;
        private Func<MmcImportRecoveryCatalog, int?>? select;
        private Action<MmcTransactionCheckpoint>? transaction;
        private Action<ModLoader.LoaderCombo<string>>? registered;
        private ModLoader.LoaderCombo<string>? current;
        private string retryName = "", selectedArchive;
        private int action, catalogReads, confirmations, downloads;
        private bool lateCancel;
        private MmcImportRecoveryEntry? parked;
        private string? parkedTree;

        internal Scope(FixtureEnvironment environment, string name) {
            this.environment = environment;
            originalProfile = ConfigProviders.Default;
            var profile = new JsonConfigProvider(Path.Combine(environment.Data, name + ".json")); ConfigProviders.Default = profile;
            Native.Check(ReferenceEquals(Configs.JavaList.Provider, profile), "Initial recovery Java catalog captured a profile outside its fresh fixture.");
            files = new(environment); root = Path.Combine(files.Root, name); game = Path.Combine(root, "minecraft"); files.SelectMinecraft(game);
            var javaScope = typeof(MmcManagedImportChecks).GetNestedType("Scope", Native.Flags)!;
            var java = (Java)Native.Call(javaScope, "FindJava", null)!;
            Configs.JavaList.Set(new ConcurrentList<Java>([java])); Configs.JavaConfigVersion.Set(Versions.JavaConfigVersion);
            runtime = new("windows", "x86_64", java.Version!.Major);
            byte[] index = Encoding.UTF8.GetBytes("{\"objects\":{}}");
            string indexPath = Path.Combine(game, "assets", "indexes", "initial-recovery.json"); Directory.CreateDirectory(Path.GetDirectoryName(indexPath)!); File.WriteAllBytes(indexPath, index);
            archive = Path.Combine(root, "original.zip"); selectedArchive = archive; WriteZip(archive, "original author world", index);
            sibling = Path.Combine(game, "versions", "ordinary-sibling"); Put(sibling, "ordinary-sibling.json", "{\"id\":\"ordinary-sibling\"}"); Put(sibling, "saves/world/level.dat", "unrelated player world"); siblingBefore = Tree(sibling);
            metadata = new(Path.Combine(root, "metadata"), new Uri("https://fixture.invalid/meta/"), client);
            cache = new(Path.Combine(game, "libraries"), Path.Combine(game, "assets"), Path.Combine(root, "source-cache"), Path.Combine(game, "versions"));
            factory = Native.Property(ui, "ServicesFactory"); oldFactory = factory.GetValue(null);
            scheduler = (LegacyLoaderHost<ModLoader.LoaderBase, ModBase.LoadState, MyLoading.MyLoadingState>)Native.Field(typeof(ModLoader), "NativeLoaderHost").GetValue(null)!;
            oldRefresh = scheduler.RefreshExtraDownload; scheduler.RefreshExtraDownload = () => { };
            services = new() {
                Metadata = metadata, CacheRoots = cache,
                SelectEntry = catalog => {
                    catalogReads++;
                    if (select is not null) return select(catalog);
                    int index = Array.FindIndex(catalog.Entries.ToArray(), entry => entry.OperationDirectory == selected?.OperationDirectory && entry.TemporaryRoot == selected!.TemporaryRoot);
                    Native.Check(index >= 0 && catalog.Entries[index].PreviewFingerprint == selected!.PreviewFingerprint, "Actual Main catalog selection changed the fixed backend entry."); return index;
                },
                SelectAction = _ => action, SelectArchive = _ => selectedArchive, SelectName = _ => retryName,
                ConfirmRetry = (_, _, _) => { confirmations++; return true; },
                RegisterTask = task => { current = task; tasks.Add(task); ModLoader.LoaderTaskbar.Add(task); registered?.Invoke(task); },
                Recovered = result => { recovered.Add(result); if (lateCancel) current!.Cancel(); },
                Retried = result => retried.Add(result), DownloadCreated = _ => { downloads++; throw new IOException("Cache-hit initial recovery fixture unexpectedly started a network manager."); },
                TransactionObserved = (phase, _) => { transaction?.Invoke(phase); if (lateCancel && phase == MmcTransactionCheckpoint.CommitDecided) current!.Cancel(); },
                ErrorObserved = (error, _) => alerts.Add(error)
            };
            factory.SetValue(null, new Func<string, MmcImportRecoveryUiServices>(value => { Native.Check(PathUtils.IsSame(value, game), "Main recovery selected another game root."); return services; }));
        }

        private bool Busy => (bool)Native.Property(ui, "HasActiveOperation").GetValue(null)!;
        private MmcInstanceBackend<ModNet.NetFile> Backend() => new(metadata, cache, MmcDownloads.BuildFile,
            (batch, token) => MmcDownloads.RunBatchAsync(batch, token, onLoaderCreated: services.DownloadCreated));
        private (MmcInstanceBackendResult Result, MmcImportRecoveryEntry Entry) Interrupt(string name, MmcTransactionCheckpoint phase) {
            var result = Backend().ImportAsync(new(archive, Path.Combine(game, "versions", name), name, runtime) {
                TransactionCheckpoint = (currentPhase, _) => { if (currentPhase == phase) throw new MmcTransactionInterruptionException("owned Main recovery fixture interruption"); }
            }).GetAwaiter().GetResult();
            Native.Check(result.Status == MmcInstanceBackendStatus.NeedsRecovery && result.WorkingRoot is not null, "Real initial import did not retain its interrupted transaction: " + result.Failure);
            var entry = MmcImportRecovery.Read(game).Entries.Single(value => value.FinalRoot == Path.Combine(game, "versions", name));
            Native.Check(entry.State == MmcImportRecoveryState.Recoverable && entry.CanRecover && entry.CanRetry, "Producer Use did not release or the committed entry is not recoverable: " + entry.Description);
            return (result, entry);
        }
        private void OpenCatalog(bool button = false) {
            if (!button) { Native.Static("PCL.MmcImportRecoveryUi", "PromptCatalog", game); return; }
            ModMinecraft.McInstanceList = [];
            var page = new PageSelectRight(); var controller = Native.Property(page.GetType(), "Service").GetValue(page)!; Native.Call(controller.GetType(), "Render", controller);
            var entry = (FrameworkElement?)page.FindName("BtnMmcImportRecovery");
            Native.Check(entry is { Visibility: Visibility.Visible } && ((FrameworkElement)page.FindName("PanEmpty")).Visibility == Visibility.Visible, "Actual empty-list Main hid initial import recovery.");
            Native.Call(page.GetType(), "BtnMmcImportRecovery_Click", page, entry, null);
        }
        private void Until(Func<bool> ready, string message) {
            var deadline = DateTime.UtcNow.AddSeconds(12);
            while (!ready()) { Native.Call(typeof(FileTaskScope), "DrainPostedUi", null, deadline); if (DateTime.UtcNow >= deadline) throw new TimeoutException(message); Thread.Sleep(5); }
            Native.Call(typeof(FileTaskScope), "DrainPostedUi", null, deadline);
        }
        private void Wait(int before, ModBase.LoadState expected) {
            Until(() => tasks.Count > before && !Busy && current!.State != ModBase.LoadState.Loading, "Actual initial recovery task did not return and release its fence.");
            Native.Check(current!.State == expected, "Actual initial recovery task state differs: " + current.State + " / " + current.Error);
            Native.Check(alerts.Count == 0 && downloads == 0 && Tree(sibling) == siblingBefore, "Initial recovery escaped its cache/ordinary sibling boundary or raised a UI error: " + string.Join(";", alerts));
        }
        private void Reset() { current = null; selected = null; select = null; transaction = null; registered = null; action = 0; lateCancel = false; alerts.Clear(); }
        private static Guid Id(MmcImportRecoveryEntry entry) => Guid.Parse(JObject.Parse(File.ReadAllText(Path.Combine(entry.TemporaryRoot!, "PCL", "MmcImportPending.json"))).Value<string>("InstanceId")!);

        internal void RecoverFromActualSelection() {
            var original = Interrupt("committed", MmcTransactionCheckpoint.CommitDecided); selected = original.Entry; Guid id = Id(selected);
            Native.Check(File.ReadAllText(Path.Combine(selected.TemporaryRoot!, "minecraft", "saves", "author-world", "level.dat")) == "original author world", "Interrupted initial import lost authored world bytes.");
            action = 1; lateCancel = true; int before = tasks.Count; OpenCatalog(button: true); Wait(before, ModBase.LoadState.Finished);
            var final = MmcInstanceInspector.Read(original.Entry.FinalRoot!);
            Native.Check(recovered.Count == 1 && final.IsConsistent && final.Definition!.InstanceId == id && final.Definition.Generation == 1 && !Directory.Exists(original.Entry.TemporaryRoot) &&
                File.ReadAllText(Path.Combine(final.Paths!.GameRoot, "saves", "author-world", "level.dat")) == "original author world", "Real Main committed recovery changed ID/G1/world bytes or failed publication.");
            Native.Check(MmcImportRecovery.Read(game).Entries.Single(entry => entry.OperationDirectory == original.Entry.OperationDirectory) is { State: MmcImportRecoveryState.Completed, CanRecover: false, CanRetry: false, PreviewFingerprint: null }, "Completed import remained actionable.");
            Reset();
        }

        internal void ReadOnlyMovedAndUnknown() {
            string final = Path.Combine(game, "versions", "moved"); FileStream? held = null;
            MmcInstanceBackendResult result;
            try {
                result = Backend().ImportAsync(new(archive, final, "moved", runtime), progress: new InlineProgress(value => {
                    if (value.Phase == "private-import-marker-cleanup") held = new FileStream(Path.Combine(final, "PCL", "MmcImportPending.json"), FileMode.Open, FileAccess.Read, FileShare.Read);
                })).GetAwaiter().GetResult();
            } finally { held?.Dispose(); }
            Native.Check(result.Status == MmcInstanceBackendStatus.NeedsRecovery && Directory.Exists(final), "Real moved-marker failure was not retained.");
            string unknown = Path.Combine(game, "PCL", "MmcImportOperations", "unknown-owned-fixture"); Put(unknown, "keep.txt", "unknown bytes stay");
            string before = Tree(game); int tasksBefore = tasks.Count, reads = catalogReads;
            select = catalog => {
                Native.Check(catalog.Entries.Any(entry => entry.FinalRoot == final && entry.CanRecover) && catalog.Entries.Any(entry => entry.State == MmcImportRecoveryState.Unknown), "Actual Main omitted moved-marker or unknown diagnostic entries."); return null;
            };
            OpenCatalog(); Until(() => catalogReads > reads, "Read-only initial recovery catalog was not displayed.");
            Native.Check(alerts.Count == 0 && tasks.Count == tasksBefore && Tree(game) == before, "Reading/canceling the catalog failed or changed moved/unknown data.");
            var unproven = MmcImportRecovery.Read(game).Entries.First(entry => entry.State == MmcImportRecoveryState.Unknown);
            Denied(() => Native.Static("PCL.MmcImportRecoveryUi", "StartRecovery", unproven with { CanRecover = true }, services));
            Denied(() => Native.Static("PCL.MmcImportRecoveryUi", "StartRetry", unproven with { CanRetry = true }, archive, "unknown-retry", services));
            Native.Check(tasks.Count == tasksBefore && Tree(game) == before && !Busy, "Unknown diagnostic entry reached a worker or mutated bytes."); Reset();
        }

        private void EnsureParked() {
            if (parked is not null) return;
            var original = Interrupt("parked", MmcTransactionCheckpoint.Prepared);
            var rollback = MmcImportRecovery.RecoverAsync(original.Entry).GetAwaiter().GetResult();
            Native.Check(rollback.Status == MmcInstanceBackendStatus.NeedsRecovery && rollback.FreshImportRequired, "Undecided original transaction was not rolled back for fresh retry.");
            parked = MmcImportRecovery.Read(game).Entries.Single(entry => entry.OperationDirectory == original.Entry.OperationDirectory);
            Native.Check(parked.CanRetry, "Rolled-back initial import lost its fresh retry option."); parkedTree = Tree(parked.TemporaryRoot!);
        }
        private void StartRetry(MmcImportRecoveryEntry entry, string source, string name) => Native.Static("PCL.MmcImportRecoveryUi", "StartRetry", entry, source, name, services);
        private void ExpectRetryFailure(MmcImportRecoveryEntry entry, string source, string name) {
            int before = tasks.Count; StartRetry(entry, source, name); Wait(before, ModBase.LoadState.Failed);
            Native.Check(!Directory.Exists(Path.Combine(game, "versions", name)) && Tree(parked!.TemporaryRoot!) == parkedTree, "Rejected retry published a destination or changed old parked data."); Reset();
        }
        internal void RetryRefusals() {
            EnsureParked(); string wrong = Path.Combine(root, "different.zip"); WriteZip(wrong, "different original bytes", Encoding.UTF8.GetBytes("{\"objects\":{}}"));
            ExpectRetryFailure(parked!, wrong, "wrong-sha");
            ExpectRetryFailure(parked! with { PackageSha256 = Hash(File.ReadAllBytes(wrong)) }, wrong, "forged-source-sha");
            ExpectRetryFailure(parked! with { Name = "forged-original-name", TemporaryRoot = sibling, FinalRoot = sibling }, archive, "parked");
            ExpectRetryFailure(parked! with { PreviewFingerprint = new string('a', 64) }, archive, "stale-preview");
            int before = tasks.Count; string target = Path.Combine(game, "versions", "collision");
            registered = _ => Put(target, "keep.txt", "foreign target bytes"); StartRetry(parked!, archive, "collision"); Wait(before, ModBase.LoadState.Failed);
            Native.Check(File.ReadAllText(Path.Combine(target, "keep.txt")) == "foreign target bytes" && Tree(parked!.TemporaryRoot!) == parkedTree, "Late destination collision overwrote foreign or parked bytes."); Reset();
        }
        internal void CancelAndDrain() {
            EnsureParked(); using ManualResetEventSlim entered = new(), release = new();
            transaction = phase => { if (phase == MmcTransactionCheckpoint.Prepared) { entered.Set(); if (!release.Wait(10000)) throw new TimeoutException("Owned Prepared cancellation barrier was not released."); } };
            int before = tasks.Count; StartRetry(parked!, archive, "early-cancel");
            try {
                Until(() => entered.IsSet, "Actual retry did not enter its Prepared transaction."); current!.Cancel();
                var worker = current.Loaders.Single();
                Native.Check(Busy && !(bool)Native.Property(worker.GetType(), "OperationCompleted").GetValue(worker)! && Native.Get(worker, "LastRunningThread") is Thread { IsAlive: true }, "Cancel released busy/fence before the genuine retry producer drained.");
            } finally { release.Set(); }
            Wait(before, ModBase.LoadState.Canceled);
            Native.Check(!Directory.Exists(Path.Combine(game, "versions", "early-cancel")) && Tree(parked!.TemporaryRoot!) == parkedTree, "Canceled retry published or rewrote the old parked import."); Reset();
        }
        internal void RetryFromActualSelection() {
            EnsureParked(); selected = parked; action = 3; selectedArchive = archive; retryName = "successful-retry"; lateCancel = true;
            Guid oldId = Id(parked!); int before = tasks.Count; OpenCatalog(); Wait(before, ModBase.LoadState.Finished);
            var result = retried.Single(); var final = MmcInstanceInspector.Read(Path.Combine(game, "versions", retryName));
            Native.Check(confirmations == 1 && result.Status == MmcInstanceBackendStatus.Succeeded && final.IsConsistent && final.Definition!.InstanceId != oldId && final.Definition.Generation == 1 &&
                final.Definition.InstalledProjection!.RuntimeEnvironment!.JavaMajor == runtime.JavaMajor && Tree(parked!.TemporaryRoot!) == parkedTree &&
                File.ReadAllText(Path.Combine(final.Paths!.GameRoot, "saves", "author-world", "level.dat")) == "original author world", "Real same-SHA retry lost fresh identity/Java/world or touched its old parked source."); Reset();
        }
        internal void Finish() {
            Native.Check(!Busy && tasks.All(task => task.State != ModBase.LoadState.Loading) && Tree(sibling) == siblingBefore && downloads == 0, "Initial recovery retained a producer or escaped its cache/sibling boundary.");
            Native.Call(typeof(FileTaskScope), "FinishActualTasks", files); InstanceConfigProviders.FlushAndDisposeWithin(root); environment.AssertNoForbiddenIo();
        }
        public void Dispose() {
            Native.Call(typeof(FileTaskScope), "FinishActualTasks", files);
            Native.Check(!Busy && tasks.All(task => task.State != ModBase.LoadState.Loading), "Refusing to restore hooks with live initial recovery IO.");
            factory.SetValue(null, oldFactory); scheduler.RefreshExtraDownload = oldRefresh; client.Dispose(); ConfigProviders.Default = originalProfile; files.Dispose();
        }
        private void WriteZip(string path, string world, byte[] index) {
            using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
            Add("mmc-pack.json", "{\"formatVersion\":1,\"components\":[{\"uid\":\"net.minecraft\",\"version\":\"1.7.10\"}]}");
            Add("patches/net.minecraft.json", new JObject { ["uid"] = "net.minecraft", ["version"] = "1.7.10", ["mainClass"] = "fixture.Main", ["compatibleJavaMajors"] = new JArray(runtime.JavaMajor),
                ["mainJar"] = new JObject { ["name"] = "fixture:game:1", ["MMC-hint"] = "local", ["MMC-filename"] = "main.jar" },
                ["assetIndex"] = new JObject { ["id"] = "initial-recovery", ["url"] = "https://fixture.invalid/index.json", ["sha1"] = Convert.ToHexStringLower(SHA1.HashData(index)), ["size"] = index.Length } }.ToString());
            Add("libraries/main.jar", "inert owned fixture; never executed"); Add("minecraft/saves/author-world/level.dat", world);
            void Add(string name, string contents) { using var writer = new StreamWriter(zip.CreateEntry(name, CompressionLevel.NoCompression).Open(), new UTF8Encoding(false)); writer.Write(contents); }
        }
    }
    private static void Denied(Action work) {
        try { work(); } catch (Exception error) when (error is IOException or InvalidOperationException || error is TargetInvocationException { InnerException: IOException or InvalidOperationException }) { return; }
        throw new Exception("An unavailable initial recovery action was not refused.");
    }
    private static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
    private static string Tree(string root) => string.Join("|", Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal).Select(file => Path.GetRelativePath(root, file) + ":" + Hash(File.ReadAllBytes(file))));
    private static void Put(string root, string relative, string contents) { string path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)); Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, contents, new UTF8Encoding(false)); }
    private sealed class InlineProgress(Action<MmcInstanceBackendProgress> observed) : IProgress<MmcInstanceBackendProgress> { public void Report(MmcInstanceBackendProgress value) => observed(value); }
    private sealed class NoNetwork : HttpMessageHandler { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => throw new IOException("Initial recovery fixture cannot access external metadata."); }
}
