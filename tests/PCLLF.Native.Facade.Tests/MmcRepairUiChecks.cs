using System.Collections.Concurrent;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;
using PCL;
using PCLCS;
using PCLCS.Loaders;
using PCLLF.MMCC.Core;
using Overall = PCLCS.InstanceOverallPageController<PCL.ModMinecraft.McInstance, PCL.ModMinecraft.McInstance.McInstanceState, PCL.ModMinecraft.McInstanceCardType, PCL.MyListItem, PCL.ModLoader.LoaderCombo<string>>;

namespace PCLLF.Native.Facade.Tests;

internal static class MmcRepairUiChecks {
    internal static void Run(FixtureEnvironment environment) {
        using FileTaskScope files = new(environment); using var scope = new Scope(files, "repair-ui");
        scope.UnstartedCancellation(); scope.RepairMissingProjection();
        scope.Finish(); environment.AssertNoForbiddenIo();
        Console.WriteLine("PASS mmc-repair-ui: genuine overall host/controller routes before legacy IgnoreFileCheck/CreateFixLoader; actual worker restores a poisoned adopted local main JAR plus missing projection from owned originals, preserves launch policy/player data, and prepares without automatic repair. Real pre-Update cancel/late Start remain inert; no JVM/game.");
    }
    internal static void RunAdmission(FixtureEnvironment environment) {
        using FileTaskScope files = new(environment); using var scope = new Scope(files, "repair-admission");
        scope.ForeignConfiguration(); scope.LatePreference(); scope.PendingAndUnknownSession(); scope.UnresolvedRequiresReload();
        scope.Finish(); environment.AssertNoForbiddenIo();
        Console.WriteLine("PASS mmc-repair-admission: actual repair rejects foreign config ID/generation at publication without overwriting it, rebases ordinary preferences, refuses pending/unknown session, and refuses unresolved old model after metadata becomes consistent; no legacy fallback/JVM/game.");
    }
    private sealed class Scope : IDisposable {
        private readonly FileTaskScope files;
        private readonly string root, game, version, projection, config, localJar;
        private readonly HttpClient client = new(new NoNetwork());
        private readonly Type ui = Native.Type("PCL.MmcRepairUi");
        private readonly PropertyInfo factory;
        private readonly object? oldFactory;
        private readonly LegacyLoaderHost<ModLoader.LoaderBase, ModBase.LoadState, MyLoading.MyLoadingState> scheduler;
        private readonly Action oldRefresh;
        private readonly ConcurrentQueue<MmcInstanceBackendResult> published = new();
        private readonly List<ModLoader.LoaderCombo<string>> allTasks = [];
        private readonly List<Exception> alerts = [];
        private ModLoader.LoaderCombo<string>? current;
        private Action<MmcInstanceBackendProgress>? progress;
        private int progressCalls, legacyCalls;
        private readonly ModMinecraft.McInstance owner;
        private readonly byte[] world, personal;
        internal Scope(FileTaskScope files, string name) {
            this.files = files; root = Path.Combine(files.Root, name); game = Path.Combine(root, "minecraft"); Directory.CreateDirectory(game); files.SelectMinecraft(game);
            version = (string)Native.Call(typeof(MmcLifecycleChecks), "ImportOwned", null, root, game)!;
            owner = new ModMinecraft.McInstance(version).Load();
            projection = Path.Combine(version, Path.GetFileName(version) + ".json"); config = Path.Combine(version, "PCL", "config.json"); localJar = Path.Combine(version, "libraries", "owned.jar");
            world = Encoding.UTF8.GetBytes("player world: preserve exactly"); personal = Encoding.UTF8.GetBytes("player override of author config");
            Put("minecraft/saves/world/level.dat", world); Put("minecraft/config/owned.cfg", personal);
            owner.Config.Set("UnknownRepairPreference", "keep", false); owner.Config.Save();
            factory = Native.Property(ui, "ServicesFactory"); oldFactory = factory.GetValue(null);
            scheduler = (LegacyLoaderHost<ModLoader.LoaderBase, ModBase.LoadState, MyLoading.MyLoadingState>)Native.Field(typeof(ModLoader), "NativeLoaderHost").GetValue(null)!;
            oldRefresh = scheduler.RefreshExtraDownload; scheduler.RefreshExtraDownload = () => { }; // Only absent offscreen button presentation.
            factory.SetValue(null, new Func<ModMinecraft.McInstance, MmcRepairServices>(_ => new() {
                Metadata = new MmcMetadataStore(Path.Combine(root, "repair-metadata"), new Uri("https://fixture.invalid/meta/"), client),
                CacheRoots = new(Path.Combine(root, "cache", "libraries"), Path.Combine(root, "cache", "assets"), Path.Combine(root, "cache", "sources")),
                RegisterTask = task => { current = task; allTasks.Add(task); ModLoader.LoaderTaskbar.Add(task); }, Published = result => published.Enqueue(result),
                ProgressObserved = value => { Interlocked.Increment(ref progressCalls); progress?.Invoke(value); },
                DownloadCreated = _ => throw new IOException("Owned-original repair must remain offline.")
            }));
        }
        private bool Busy => (bool)Native.Property(ui, "HasActiveOperation").GetValue(null)!;
        private InstanceOverallPageHost<ModMinecraft.McInstance, MyListItem, ModLoader.LoaderCombo<string>> Host(ModMinecraft.McInstance instance) {
            var host = (InstanceOverallPageHost<ModMinecraft.McInstance, MyListItem, ModLoader.LoaderCombo<string>>)Native.Call(typeof(PageInstanceOverall), "CreateHost", RuntimeHelpers.GetUninitializedObject(typeof(PageInstanceOverall)))!;
            Native.Check(host.BeginManagedRepair?.Method.DeclaringType == ui && host.BeginManagedRepair.Method.Name == "Prompt", "Genuine overall host did not bind managed repair.");
            host.Instance = () => instance;
            host.IgnoreFileCheck = _ => { legacyCalls++; throw new IOException("MMC reached legacy IgnoreFileCheck."); };
            host.CreateFixLoader = (_, _) => { legacyCalls++; throw new IOException("MMC reached ordinary loader repair."); };
            host.ErrorAlert = (error, _) => alerts.Add(error); return host;
        }
        private void Repair(ModMinecraft.McInstance? instance = null) => new Overall(new(), Host(instance ?? owner), _ => { }, () => 0).Repair();
        private void Reset() { current = null; alerts.Clear(); progress = null; while (published.TryDequeue(out _)) { } }
        private void Wait() {
            var deadline = DateTime.UtcNow.AddSeconds(30);
            while (current is null || Busy || current.State == ModBase.LoadState.Loading) {
                Native.Call(typeof(FileTaskScope), "DrainPostedUi", null, deadline); if (DateTime.UtcNow >= deadline) throw new TimeoutException("Real MMC repair did not return."); Thread.Sleep(5);
            }
            Native.Call(typeof(FileTaskScope), "DrainPostedUi", null, deadline);
            Native.Check(alerts.Count == 0 && legacyCalls == 0, "Repair controller reached an alert/legacy path: " + string.Join(";", alerts));
        }
        internal void UnstartedCancellation() {
            string before = Tree(version); int calls = progressCalls;
            using (var pause = new MmcRootUpdatePause(() => current)) {
                Repair(); Native.Check(current is not null && pause.Captured, "Repair did not reach the actual root Update scheduling boundary.");
                current!.Cancel(); pause.Release(); pause.AwaitReturn();
                Native.Check(!Busy && Native.Get(current.Loaders.Single(), "LastRunningThread") is null && published.IsEmpty && progressCalls == calls,
                    "Canceled unstarted repair retained busy or ran its delegate.");
                current.Loaders.Single().Start(version);
                Native.Check(Native.Get(current.Loaders.Single(), "LastRunningThread") is null && Tree(version) == before, "Late repair Start escaped one-shot fence or changed bytes.");
            }
            Reset();
        }
        internal void RepairMissingProjection() {
            var before = MmcInstanceInspector.Read(version); string oldSignature = Signature(before.Definition!); byte[] adopted = File.ReadAllBytes(localJar);
            var locked = before.Definition!.ArtifactLocks.Single(value => value.TargetRelativePath == "libraries/owned.jar");
            Native.Check(Hash(adopted) == locked.AdoptedSha256 && owner.Config.ContextStamp.Kind == MmcConfigurationContextKind.Managed, "Fixture did not capture the actual adopted local main JAR.");
            File.WriteAllText(localJar, "poisoned current main JAR", new UTF8Encoding(false)); File.Delete(projection); File.Delete(Path.Combine(root, "owned.zip"));
            Repair(); Wait();
            Native.Check(current!.State == ModBase.LoadState.Finished && published.TryPeek(out var result) && result.Status == MmcInstanceBackendStatus.Succeeded,
                "Actual managed repair failed: " + current.Error);
            var after = MmcInstanceInspector.Read(version);
            Native.Check(after.IsConsistent && after.Definition!.InstanceId == before.Definition.InstanceId && after.Definition.Generation == before.Definition.Generation + 1 &&
                Signature(after.Definition) == oldSignature && File.Exists(projection) && Hash(File.ReadAllBytes(localJar)) == locked.AdoptedSha256 && File.ReadAllBytes(localJar).SequenceEqual(adopted),
                "Repair changed adopted entry/JVM/Java policy or failed to restore exact original bytes and projection.");
            CheckPlayerData();
            var definition = after.Definition ?? throw new IOException("Successful repair did not return its primary definition.");
            var saved = MmcSavedSourceStore.LoadAsync(after.Paths!, definition).GetAwaiter().GetResult();
            var compile = MmcProfileCompiler.Compile(saved.Resolved.Resolution.Profile!, definition.InstalledProjection!.RuntimeEnvironment!);
            Native.Check(!compile.HasErrors && compile.Profile!.MainClass == "fixture.Main" && compile.Profile.JvmArguments.IsEmpty, "Repair replaced fixture launch settings with ordinary Forge settings.");
            var backend = Backend();
            var prepared = backend.PrepareLaunchAsync(new(version, definition.InstalledProjection.RuntimeEnvironment!, definition.InstanceId, definition.Generation, RepairIncomplete: false)).GetAwaiter().GetResult();
            Native.Check(prepared.Status == MmcInstanceBackendStatus.Succeeded && prepared.Snapshot is not null && !prepared.Repaired, "Post-repair immutable preparation still needed automatic repair: " + prepared.Failure);
        }
        internal void ForeignConfiguration() {
            foreach (bool wrongGeneration in new[] { false, true }) {
                Reset(); byte[] original = File.ReadAllBytes(config); string primary = Hash(File.ReadAllBytes(Path.Combine(version, "PCL", "InstanceDefinition.json"))), json = Hash(File.ReadAllBytes(projection)); byte[]? foreign = null;
                progress = value => {
                    if (value.Phase != "publishing-repair" || foreign is not null) return;
                    var parsed = JObject.Parse(File.ReadAllText(config)); var hint = (JObject)parsed[MmcManagementReader.ConfigManifestPropertyName]![MmcManagementReader.HintPropertyName]!;
                    if (wrongGeneration) hint["generation"] = hint.Value<long>("generation") + 100; else hint["instanceId"] = Guid.NewGuid().ToString("D");
                    foreign = Encoding.UTF8.GetBytes(parsed.ToString()); File.WriteAllBytes(config, foreign);
                };
                Repair(); Wait();
                Native.Check(foreign is not null && current!.State == ModBase.LoadState.Failed && published.IsEmpty && File.ReadAllBytes(config).SequenceEqual(foreign) &&
                    Hash(File.ReadAllBytes(Path.Combine(version, "PCL", "InstanceDefinition.json"))) == primary && Hash(File.ReadAllBytes(projection)) == json,
                    "Final repair admission accepted or overwrote foreign config identity/generation.");
                CheckPlayerData(readPreference: false); File.WriteAllBytes(config, original);
            }
        }
        internal void LatePreference() {
            Reset(); bool changed = false; string before = Signature(MmcInstanceInspector.Read(version).Definition!);
            progress = value => { if (value.Phase == "publishing-repair" && !changed) { var parsed = JObject.Parse(File.ReadAllText(config)); parsed["LateRepairPreference"] = "keep"; File.WriteAllText(config, parsed.ToString(), new UTF8Encoding(false)); changed = true; } };
            Repair(); Wait();
            Native.Check(changed && current!.State == ModBase.LoadState.Finished && published.Count == 1 && JObject.Parse(File.ReadAllText(config)).Value<string>("LateRepairPreference") == "keep" &&
                Signature(MmcInstanceInspector.Read(version).Definition!) == before, "Repair did not preserve ordinary preference rebase or changed launch chain.");
            CheckPlayerData();
        }
        internal void PendingAndUnknownSession() {
            Reset(); string pending = Path.Combine(version, "PCL", "MmcImportPending.json"); File.WriteAllText(pending, "{unknown}"); string before = Tree(version); int count = allTasks.Count;
            Repair(); Native.Check(current is null && alerts.Count == 1 && allTasks.Count == count && !Busy && Tree(version) == before, "Pending management was queued or reached legacy repair."); File.Delete(pending);
            Reset(); string directory = Path.Combine(version, "PCL", "MmcSessions", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory); string receipt = Path.Combine(directory, "Launching.record"); File.WriteAllText(receipt, "{unknown}"); before = Tree(version);
            Repair(); Wait(); Native.Check(current!.State == ModBase.LoadState.Failed && published.IsEmpty && Tree(version) == before && !Busy, "Unknown session did not block repair publication.");
            File.Delete(receipt); Directory.Delete(directory, false);
        }
        internal void UnresolvedRequiresReload() {
            Reset(); byte[] json = File.ReadAllBytes(projection); File.Delete(projection);
            var unresolved = new ModMinecraft.McInstance(version).Load(); Native.Check(unresolved.Config.ContextStamp.Kind == MmcConfigurationContextKind.Unresolved, "Missing projection did not create a genuinely unresolved model.");
            File.WriteAllBytes(projection, json); Native.Check(MmcInstanceInspector.Read(version).IsConsistent, "Restored metadata did not become consistent.");
            string before = Tree(version); int count = allTasks.Count;
            Repair(unresolved); Native.Check(current is null && alerts.Count == 1 && allTasks.Count == count && !Busy && Tree(version) == before, "Unresolved old object silently adopted a now-consistent instance instead of requiring reload.");
        }
        private MmcInstanceBackend<MmcArtifactDownloadSpec> Backend() => new(new MmcMetadataStore(Path.Combine(root, "prepare-meta"), new Uri("https://fixture.invalid/meta/"), client),
            new(Path.Combine(root, "cache", "libraries"), Path.Combine(root, "cache", "assets"), Path.Combine(root, "cache", "sources")), value => value,
            (files, _) => files.IsEmpty ? Task.CompletedTask : throw new IOException("Repaired instance must prepare entirely offline."));
        private void CheckPlayerData(bool readPreference = true) {
            Native.Check(File.ReadAllBytes(Path.Combine(version, "minecraft", "saves", "world", "level.dat")).SequenceEqual(world) && File.ReadAllBytes(Path.Combine(version, "minecraft", "config", "owned.cfg")).SequenceEqual(personal), "Repair overwrote world or personal game config.");
            if (readPreference) Native.Check(JObject.Parse(File.ReadAllText(config)).Value<string>("UnknownRepairPreference") == "keep", "Repair dropped an unknown user preference.");
        }
        private void Put(string relative, byte[] bytes) { string path = Path.Combine(version, relative.Replace('/', Path.DirectorySeparatorChar)); Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllBytes(path, bytes); }
        internal void Finish() { Native.Check(!Busy && legacyCalls == 0, "Repair retained actual work or invoked legacy hooks."); Native.Call(typeof(FileTaskScope), "FinishActualTasks", files); InstanceConfigProviders.FlushAndDisposeWithin(root); }
        public void Dispose() {
            Native.Check(!Busy && allTasks.All(task => task.State != ModBase.LoadState.Loading), "Refusing to restore fixture hooks while actual repair I/O remains.");
            Native.Call(typeof(FileTaskScope), "FinishActualTasks", files); factory.SetValue(null, oldFactory); scheduler.RefreshExtraDownload = oldRefresh; client.Dispose();
        }
    }
    private static string Signature(MmcInstanceDefinition definition) => definition.InstalledProjection!.ProfileFingerprint + ":" + definition.InstalledProjection.RuntimePlanFingerprint;
    private static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
    private static string Tree(string root) => string.Join("|", Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal)
        .Select(path => Path.GetRelativePath(root, path) + ":" + Hash(File.ReadAllBytes(path))));
    private sealed class NoNetwork : HttpMessageHandler { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => throw new IOException("Repair fixture cannot access external metadata."); }
}
