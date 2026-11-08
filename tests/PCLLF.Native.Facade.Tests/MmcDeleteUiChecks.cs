using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using PCL;
using PCLCS;
using Selection = PCLCS.InstanceSelectionPageController<PCL.ModMinecraft.McInstance, PCL.ModMinecraft.McInstance.McInstanceState, PCL.ModMinecraft.McInstanceCardType, PCL.InstanceCloneGroupNode, PCL.InstanceCloneGroupSet>;
using Overall = PCLCS.InstanceOverallPageController<PCL.ModMinecraft.McInstance, PCL.ModMinecraft.McInstance.McInstanceState, PCL.ModMinecraft.McInstanceCardType, System.Windows.Controls.Grid, object>;

namespace PCLLF.Native.Facade.Tests;

internal static class MmcDeleteUiChecks {
    internal static void Run(FixtureEnvironment environment) {
        using FileTaskScope files = new(environment);
        using var scope = new Scope(files);
        scope.Bindings(); scope.SelectionDeleteAndEmptyRecovery(); scope.OverallAndNavigation();
        scope.Cancellation(); scope.StaleConfirmation(); scope.BlockedSessionsAndPending(); scope.IncompleteRecovery();
        InstanceConfigProviders.FlushAndDisposeWithin(files.Root); environment.AssertNoForbiddenIo();
        Console.WriteLine("PASS mmc-delete-ui: both real Main deletion bindings, Shift capability, empty-list recovery, preview identity, before/after-decision cancel, active/unknown/pending refusal, exact catalog conflicts and real quarantine/recovery/restore; no game or user data.");
    }
    private sealed class Scope : IDisposable {
        private readonly FileTaskScope files;
        private readonly Type ui = Native.Type("PCL.MmcDeleteUi");
        private readonly Dictionary<PropertyInfo, object?> previous = [];
        private readonly ConcurrentQueue<ModLoader.LoaderCombo<string>> tasks = new();
        private readonly ConcurrentQueue<MmcInstanceLifecycleResult> results = new();
        private readonly ConcurrentQueue<Exception> errors = new();
        private int sequence;
        internal Func<string, int> Confirmation = _ => 1;
        internal Action<MmcLifecycleCheckpoint>? Checkpoint;
        internal Action<ModLoader.LoaderCombo<string>>? Registered;
        internal Action? Completed;
        internal Scope(FileTaskScope files) {
            this.files = files;
            Set("RegisterTask", new Action<ModLoader.LoaderCombo<string>>(task => { tasks.Enqueue(task); ModLoader.LoaderTaskbar.Add(task); Registered?.Invoke(task); }));
            Set("Published", new Action<MmcInstanceLifecycleResult>(result => { results.Enqueue(result); if (Completed is { } complete) ModBase.RunInUi(complete); }));
            Set("Confirm", new Func<string, string, bool, int>((text, _, _) => Confirmation(text)));
            Set("ErrorObserved", new Action<Exception, string>((error, _) => errors.Enqueue(error)));
            Set("CheckpointObserved", new Action<MmcLifecycleCheckpoint, string>((phase, _) => Checkpoint?.Invoke(phase)));
        }
        private void Set(string name, object? value) { var property = Native.Property(ui, name); if (!previous.ContainsKey(property)) previous.Add(property, property.GetValue(null)); property.SetValue(null, value); }
        private bool Busy => (bool)Native.Property(ui, "HasActiveOperation").GetValue(null)!;
        private ModLoader.LoaderCombo<string> Current => tasks.Last();
        private void Reset() { Confirmation = _ => 1; Checkpoint = null; Registered = null; Completed = null; while (results.TryDequeue(out _)) { } while (errors.TryDequeue(out _)) { } }
        private ModMinecraft.McInstance Import() {
            Reset(); string root = Path.Combine(files.Root, "delete-ui-" + ++sequence), game = Path.Combine(root, "minecraft"); Directory.CreateDirectory(game); files.SelectMinecraft(game);
            string path = (string)Native.Call(typeof(MmcLifecycleChecks), "ImportOwned", null, root, game)!;
            string world = Path.Combine(path, "minecraft", "saves", "player-world", "level.dat"); Directory.CreateDirectory(Path.GetDirectoryName(world)!); File.WriteAllText(world, "current player world", new UTF8Encoding(false));
            return new ModMinecraft.McInstance(path).Load();
        }
        private static void Until(Func<bool> done, string message) {
            var deadline = DateTime.UtcNow.AddSeconds(30);
            while (!done()) { Native.Call(typeof(FileTaskScope), "DrainPostedUi", null, deadline); if (DateTime.UtcNow >= deadline) throw new TimeoutException(message); Thread.Sleep(5); }
            Native.Call(typeof(FileTaskScope), "DrainPostedUi", null, deadline);
        }
        private void Wait(int before) => Until(() => tasks.Count > before && !Busy && Current.State != ModBase.LoadState.Loading, "MMC deletion/recovery task did not really return.");
        internal void Bindings() {
            object selection = Native.Call(typeof(PageSelectRight), "CreateHost", null, new object?[] { null })!;
            object overall = Native.Call(typeof(PageInstanceOverall), "CreateHost", RuntimeHelpers.GetUninitializedObject(typeof(PageInstanceOverall)))!;
            foreach (object host in new[] { selection, overall }) {
                var managed = (Delegate)Native.Get(host, "BeginManagedDelete")!;
                Native.Check(managed.Method.DeclaringType == ui && managed.Method.Name == "PromptDelete", "A Main deletion UI bypassed the new managed dispatcher.");
                var legacy = (Delegate)Native.Get(host, "DeleteConfirmed")!;
                Native.Check(legacy.Method.DeclaringType == typeof(PageSelectRight) && legacy.Method.Name == "DeleteInstanceInBackground", "Ordinary deletion lost its original binding.");
            }
        }
        internal void SelectionDeleteAndEmptyRecovery() {
            var instance = Import(); int before = tasks.Count; Action? callback = null; bool shifted = false;
            var host = (InstanceSelectionPageHost<ModMinecraft.McInstance, ModMinecraft.McInstanceCardType>)Native.Call(typeof(PageSelectRight), "CreateHost", null, new object?[] { null })!;
            var actual = host.BeginManagedDelete!;
            var cards = new Dictionary<ModMinecraft.McInstanceCardType, List<ModMinecraft.McInstance>> { [ModMinecraft.McInstanceCardType.Star] = [instance] };
            ModMinecraft.McInstance? selected = instance, pageInstance = instance;
            host.ModpackEnabled = () => false; host.Shift = () => true;
            host.Instances = () => cards; host.SetInstances = value => cards = value;
            host.SelectedInstance = () => selected; host.SetSelectedInstance = value => selected = value;
            host.InstancePage = () => pageInstance; host.SetInstancePage = value => pageInstance = value;
            host.ForceRefresh = () => { }; host.RefreshInstances = _ => { };
            host.BeginManagedDelete = (target, shift, complete) => { shifted = shift; callback = complete; actual(target, shift, complete); };
            Confirmation = text => { Native.Check(text.Contains("仅支持可恢复删除", StringComparison.Ordinal) && text.Contains("不会释放", StringComparison.Ordinal), "Shift silently entered permanent deletion or claimed reclaimed space."); return 1; };
            Completed = () => callback!();
            Selection.Delete(new Grid { Tag = instance }, instance, host); Wait(before);
            Native.Check(shifted && Current.State == ModBase.LoadState.Finished && cards.Count == 0 && selected is null && pageInstance is null, "Managed list deletion did not update its last-instance state.");
            var quarantined = results.Single(); Native.Check(quarantined.Quarantined && !Directory.Exists(instance.PathVersion), "Managed list deletion did not quarantine the source.");
            Native.Check(File.ReadAllText(Path.Combine(quarantined.CurrentRoot!, "minecraft", "saves", "player-world", "level.dat")) == "current player world", "Quarantine changed the player's world.");
            ModMinecraft.McInstanceList = [];
            var view = new PageSelectRight(); var controller = Native.Property(view.GetType(), "Service").GetValue(view)!;
            Native.Call(controller.GetType(), "Render", controller);
            var button = (FrameworkElement)view.FindName("BtnMmcRecovery");
            Native.Check(button.Visibility == Visibility.Visible && ((FrameworkElement)view.FindName("PanEmpty")).Visibility == Visibility.Visible && Grid.GetRow(button) == 0,
                "The last-instance deletion hid the recovery entry with the empty list.");
            bool catalogSeen = false;
            Set("SelectEntry", new Func<ImmutableArray<MmcLifecycleCatalogEntry>, int?>(entries => { catalogSeen = entries.Any(entry => entry.CanRestore && entry.InstanceId == quarantined.Definition!.InstanceId); return null; }));
            Native.Call(view.GetType(), "BtnMmcRecovery_Click", view, button, EventArgs.Empty); Until(() => catalogSeen, "The empty-list recovery button did not read the current folder's real catalog.");
            var preview = MmcInstanceLifecycleCatalog.Read(ModMinecraft.McFolderSelected).Entries.Single(entry => entry.CanRestore);
            string foreign = Path.Combine(instance.PathVersion, "keep.txt"); Directory.CreateDirectory(instance.PathVersion); File.WriteAllText(foreign, "foreign replacement");
            try { MmcInstanceLifecycleCatalog.Execute(preview, true); throw new Exception("Occupied restore path was overwritten."); } catch (IOException) { }
            Native.Check(File.ReadAllText(foreign) == "foreign replacement" && Directory.Exists(quarantined.CurrentRoot), "Restore conflict changed a foreign directory or quarantine.");
            File.Delete(foreign); Directory.Delete(instance.PathVersion, false);
            Reset(); before = tasks.Count;
            var fresh = MmcInstanceLifecycleCatalog.Read(ModMinecraft.McFolderSelected).Entries.Single(entry => entry.CanRestore);
            Native.Static("PCL.MmcDeleteUi", "StartCatalogAction", fresh, true); Wait(before);
            Native.Check(Current.State == ModBase.LoadState.Finished && Directory.Exists(instance.PathVersion) && results.Single().Definition!.InstanceId == quarantined.Definition!.InstanceId,
                "Real UI restore did not preserve the deleted instance identity.");
        }
        internal void OverallAndNavigation() {
            var instance = Import(); int before = tasks.Count; Action? callback = null; int backed = 0, refreshed = 0; bool currentPage = true;
            var host = (InstanceOverallPageHost<ModMinecraft.McInstance, Grid, object>)Native.Call(typeof(PageInstanceOverall), "CreateHost", RuntimeHelpers.GetUninitializedObject(typeof(PageInstanceOverall)))!;
            var actual = host.BeginManagedDelete!;
            host.Instance = () => instance; host.ModpackEnabled = () => false; host.Shift = () => false; host.IsCurrentPage = () => currentPage;
            host.RefreshInstanceList = () => refreshed++; host.PageBack = () => backed++;
            host.BeginManagedDelete = (target, shift, complete) => { callback = complete; actual(target, shift, complete); };
            Confirmation = _ => { currentPage = false; return 1; }; Completed = () => callback!();
            new Overall(new(), host, _ => { }, () => 0).Delete(); Wait(before);
            Native.Check(Current.State == ModBase.LoadState.Finished && results.Single().Quarantined && refreshed == 1 && backed == 0,
                "Details deletion failed or navigated a newer page away.");
        }
        internal void Cancellation() {
            var instance = Import(); string original = HashTree(instance.PathVersion); int before = tasks.Count;
            Registered = task => task.Cancel(); Native.Static("PCL.MmcDeleteUi", "PromptDelete", instance, false, null); Wait(before);
            Native.Check(Current.State == ModBase.LoadState.Canceled && !Busy && HashTree(instance.PathVersion) == original, "Unstarted cancellation retained busy or changed source files.");
            Registered = null; before = tasks.Count;
            Checkpoint = phase => { if (phase == MmcLifecycleCheckpoint.BeforeDecision) Current.Cancel(); };
            Native.Static("PCL.MmcDeleteUi", "PromptDelete", instance, false, null); Wait(before);
            Native.Check(Current.State == ModBase.LoadState.Canceled && Directory.Exists(instance.PathVersion) && results.IsEmpty, "Before-decision cancellation published a deletion.");
            before = tasks.Count; Checkpoint = phase => { if (phase == MmcLifecycleCheckpoint.CommitDecided) Current.Cancel(); };
            Native.Static("PCL.MmcDeleteUi", "PromptDelete", instance, false, null); Wait(before);
            Native.Check(Current.State == ModBase.LoadState.Finished && results.Single().Quarantined && !Busy, "Post-decision cancellation interrupted quarantine or retained busy.");
        }
        internal void StaleConfirmation() {
            var instance = Import(); string source = instance.PathVersion, game = ModMinecraft.McFolderSelected; var original = MmcInstanceInspector.Read(source);
            int before = tasks.Count; Guid replacementId = Guid.Empty; string replacementHash = "";
            Confirmation = text => {
                string retained = Path.Combine(Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(source))!, "original-retained");
                var moved = new MmcInstanceLifecycleBackend().RelocateAsync(new(source, retained, original.Definition!.InstanceId, original.Definition.Generation, original.PrimarySha256!, MmcInstanceRelocationKind.Rename)).GetAwaiter().GetResult();
                Native.Check(moved.Status == MmcInstanceBackendStatus.Succeeded, "Concurrent owned relocation failed.");
                string input = Path.Combine(files.Root, "stale-replacement-input"); Directory.CreateDirectory(input);
                _ = Native.Call(typeof(MmcLifecycleChecks), "ImportOwned", null, input, game);
                replacementId = MmcInstanceInspector.Read(source).Definition!.InstanceId; replacementHash = HashTree(source); return 1;
            };
            Native.Static("PCL.MmcDeleteUi", "PromptDelete", instance, false, null); Wait(before);
            Native.Check(Current.State == ModBase.LoadState.Failed && results.IsEmpty && replacementId != original.Definition!.InstanceId &&
                MmcInstanceInspector.Read(source).Definition!.InstanceId == replacementId && HashTree(source) == replacementHash, "A confirmed old identity deleted its same-name replacement.");
        }
        internal void BlockedSessionsAndPending() {
            foreach (string mode in new[] { "active", "unknown", "pending", "primary" }) {
                var instance = Import(); int before = tasks.Count;
                using var idle = mode == "active" ? new BackupResourceChecks.IdleProcess() : null;
                if (idle is not null) {
                    var source = MmcInstanceInspector.Read(instance.PathVersion); Guid sessionId = Guid.NewGuid(); string directory = Path.Combine(instance.PathVersion, "PCL", "MmcSessions", sessionId.ToString("N")); Directory.CreateDirectory(directory);
                    var identity = MmcSessions.ObserveExecutable(idle.Value.MainModule!.FileName!);
                    var receipt = new MmcSessionReceipt(1, sessionId, source.Definition!.InstanceId, source.Paths!.InstanceRoot, source.Definition.Generation, new string('a', 64), new string('b', 32), identity, MmcSessions.ObserveExecutable(Environment.ProcessPath!));
                    string launching = Record(Path.Combine(directory, "Launching.record"), receipt);
                    _ = Record(Path.Combine(directory, "Running.record"), new MmcSessionProcessRegistration(1, sessionId, launching, idle.Value.Id, idle.Value.StartTime.ToUniversalTime().Ticks, identity));
                    Native.Check(!idle.Value.HasExited, "Active session prerequisite already exited.");
                } else {
                    string relative = mode == "unknown" ? "PCL/MmcSessions/" + Guid.NewGuid().ToString("N") + "/Launching.record" : mode == "pending" ? "PCL/MmcImportPending.json" : "PCL/InstanceDefinition.json";
                    string path = Path.Combine(instance.PathVersion, relative.Replace('/', Path.DirectorySeparatorChar)); Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, "{broken}");
                }
                string bytes = HashTree(instance.PathVersion);
                Native.Static("PCL.MmcDeleteUi", "PromptDelete", instance, false, null);
                if (mode is "active" or "unknown") { Wait(before); Native.Check(Current.State == ModBase.LoadState.Failed, "Active/unknown session admitted deletion."); }
                else Until(() => !errors.IsEmpty, "Pending/damaged instance did not show its refusal.");
                Native.Check(results.IsEmpty && HashTree(instance.PathVersion) == bytes && !Busy, "A refused deletion changed source bytes or retained busy.");
                idle?.Finish();
            }
        }
        internal void IncompleteRecovery() {
            var instance = Import(); var source = MmcInstanceInspector.Read(instance.PathVersion);
            var interrupted = new MmcInstanceLifecycleBackend().DeleteAsync(new(source.Paths!.InstanceRoot, source.Definition!.InstanceId, source.Definition.Generation, source.PrimarySha256!) {
                Checkpoint = (phase, _) => { if (phase == MmcLifecycleCheckpoint.CommitDecided) throw new MmcLifecycleInterruptionException("controlled post-decision interruption"); }
            }).GetAwaiter().GetResult();
            Native.Check(interrupted.Status == MmcInstanceBackendStatus.NeedsRecovery, "Recovery fixture did not retain a decided operation.");
            var preview = MmcInstanceLifecycleCatalog.Read(ModMinecraft.McFolderSelected).Entries.Single(entry => entry.CanRecover);
            int before = tasks.Count; Native.Static("PCL.MmcDeleteUi", "StartCatalogAction", preview, false); Wait(before);
            Native.Check(Current.State == ModBase.LoadState.Finished && results.Single().Quarantined, "Real UI did not recover the existing decided deletion.");
            var restore = MmcInstanceLifecycleCatalog.Read(ModMinecraft.McFolderSelected).Entries.Single(entry => entry.CanRestore);
            string intent = Path.Combine(restore.OperationDirectory, "intent.record"); byte[] bytes = File.ReadAllBytes(intent); File.AppendAllText(intent, "changed");
            try { MmcInstanceLifecycleCatalog.Execute(restore, true); throw new Exception("Changed preview was accepted."); } catch (IOException) { }
            Native.Check(MmcInstanceLifecycleCatalog.Read(ModMinecraft.McFolderSelected).Entries.Any(entry => entry.State == MmcLifecycleCatalogState.Unknown && !entry.CanRecover && !entry.CanRestore), "Damaged record disappeared or acquired an action.");
            File.WriteAllBytes(intent, bytes); Reset(); before = tasks.Count;
            var fresh = MmcInstanceLifecycleCatalog.Read(ModMinecraft.McFolderSelected).Entries.Single(entry => entry.CanRestore);
            Native.Static("PCL.MmcDeleteUi", "StartCatalogAction", fresh, true); Wait(before);
            Native.Check(Current.State == ModBase.LoadState.Finished && Directory.Exists(instance.PathVersion) && !results.Single().Quarantined, "Decided recovery did not remain restorable.");
        }
        private static string Record<T>(string path, T value) => (string)typeof(MmcSessions).Assembly.GetType("PCLCS.MmcDurableFiles")!.GetMethod("WriteRecord", BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(typeof(T)).Invoke(null, [path, value])!;
        private static string HashTree(string root) => string.Join("|", Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal).Select(file => Path.GetRelativePath(root, file) + ":" + Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)))));
        public void Dispose() { Native.Check(!Busy && tasks.All(task => task.State != ModBase.LoadState.Loading), "Refusing to release test hooks with a real MMC worker still active."); foreach (var value in previous) value.Key.SetValue(null, value.Value); }
    }
}
