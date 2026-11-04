using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MeloongCore;
using Newtonsoft.Json.Linq;
using PCL;

namespace PCLLF.Native.Facade.Tests;
// The production Native facades, loaders and workers perform all work. These are
// the pinned LF progress fixtures plus controlled platform/UI setup, not new engines.
internal static class FileTaskScenarios
{
    internal static void Run(FixtureEnvironment environment)
    {
        using FileTaskScope scope = new(environment);
        string backup = Path.Combine(scope.Root, "backup");
        scope.SelectMinecraft(Path.Combine(backup, "minecraft"));
        Console.WriteLine("PASS native file-tasks backup: " + FileTaskBackupScenarios.Run(Native.Main, backup));
        string clone = Path.Combine(scope.Root, "clone");
        scope.SelectMinecraft(Path.Combine(clone, "minecraft"));
        FileTaskCloneScenarios.Run(Native.Main, clone);
        FileTaskNavigationScenarios.Run(scope);
        TransactionRollback(scope.Root);
        FileTaskUpdateCancellationScenarios.Run(scope.Root);
        scope.FinishActualTasks();
        environment.AssertNoForbiddenIo();
    }

    private static void TransactionRollback(string root)
    {
        string game = Path.Combine(root, "transaction", "versions", "fixture");
        Directory.CreateDirectory(game);
        string first = Path.Combine(game, "first.txt"), second = Path.Combine(game, "second.txt");
        string firstPayload = Path.Combine(root, "first-payload.txt"), secondPayload = Path.Combine(root, "second-payload.txt");
        File.WriteAllText(first, "first original");
        File.WriteAllText(second, "second original");
        File.WriteAllText(firstPayload, "first replacement");
        File.WriteAllText(secondPayload, "second replacement");
        string Digest(string path) => (string)Native.Static("PCL.ModpackUpdate", "Digest", path)!;
        var operations = new List<Tuple<string, string, string>>
        {
            Tuple.Create(first, firstPayload, Digest(first)),
            Tuple.Create(second, secondPayload, Digest(second))
        };
        var reports = new List<double>();
        var sentinel = new IOException("Synthetic second-operation transaction failure.");
        Action<int> failSecond = index =>
        {
            if (index == 1)
                throw sentinel;
        };
        bool failed = false;
        try
        {
            Native.Static("PCL.ModpackUpdate", "ApplyTransactionWithProgress", game, game, operations, CancellationToken.None, new Action<double>(reports.Add), failSecond, null);
        }
        catch (IOException error)when (ReferenceEquals(error, sentinel))
        {
            failed = true;
        }

        Native.Check(failed && reports.Any(x => x > 0 && x < 1) && !reports.Contains(1), "Actual Main transaction did not stop during its second operation.");
        Native.Check(File.ReadAllText(first) == "first original" && File.ReadAllText(second) == "second original", "Actual Main transaction did not roll back the first applied replacement.");
        Native.Check(!Directory.EnumerateFiles(game, "*.pcl-update-*", SearchOption.AllDirectories).Any(), "Actual transaction left a staging file after rollback.");
        Native.Check(!((IEnumerable<string>)Native.Static("PCL.ModpackUpdate", "Pending", game)!).Any(), "Rolled-back transaction remained pending.");
        foreach (string path in new[]
        {
            first,
            second
        }

        )
        {
            using FileStream exclusive = new(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }

        Console.WriteLine("PASS native file-tasks transaction: real Main two-file write, injected second-step failure, reverse rollback, completed recovery log and released handles.");
    }
}

internal static class FileTaskShell
{
    // The original LF fixture deliberately does not run a launcher window. The
    // migrated facade requires the REAL production controller, built with its
    // actual private view/host factories; copying PageCurrent into a fake host
    // would miss precisely the navigation forwarding this fixture must verify.
    internal static FormMain Install()
    {
        FormMain shell = (FormMain)RuntimeHelpers.GetUninitializedObject(typeof(FormMain));
        ModMain.FrmMain = shell;
        Set(shell, "PanTitleMain", new Grid());
        Set(shell, "PanTitleInner", new Grid());
        Set(shell, "LabTitleInner", new TextBlock());
        Set(shell, "BtnExtraDownload", new MyExtraButton());
        Set(shell, "BtnExtraApril", new MyExtraButton());
        Set(shell, "BtnExtraBack", new MyExtraButton());
        object view = Native.Call(typeof(FormMain), "CreateWindowView", shell)!;
        object host = Native.Call(typeof(FormMain), "CreateWindowHost", shell)!;
        FieldInfo controller = Native.Field(typeof(FormMain), "Controller");
        object actual = Activator.CreateInstance(controller.FieldType, view, host)!;
        Native.Check(actual.GetType().Assembly.GetName().Name == "PCLLF.Presentation", "Navigation fixture did not construct the actual Main window controller.");
        controller.SetValue(shell, actual);
        shell.PageCurrent = FormMain.PageType.Launch;
        shell.PageLast = FormMain.PageType.Launch;
        shell.PageStack = new List<FormMain.PageStackData>();
        return shell;
    }

    internal static void Set(object target, string name, object value)
    {
        // Actual Main50 metadata (939061c...) retains these four generated
        // controls as fields; the remaining controls are real WithEvents
        // properties. Do not manufacture a property or mask a state adapter.
        if (name is "PanTitleMain" or "PanTitleInner" or "LabTitleInner" or "PanMainRight")
            Native.Field(target.GetType(), name).SetValue(target, value);
        else
            Native.Property(target.GetType(), name).SetValue(target, value);
    }
}

internal sealed class FileTaskScope : IDisposable
{
    private readonly FixtureEnvironment environment;
    private readonly Stack<Action> restore = new();
    private readonly HashSet<ModLoader.LoaderBase> originalTasks;
    private bool finished;
    internal string Root { get; }

    internal FileTaskScope(FixtureEnvironment environment)
    {
        this.environment = environment;
        Root = Path.Combine(environment.Data, "file-tasks");
        Directory.CreateDirectory(Root);
        originalTasks = new(ModLoader.LoaderTaskbar);
        foreach (FieldInfo field in typeof(ModMain).GetFields(Native.Flags).Where(field => field.IsStatic && field.Name.StartsWith("Frm", StringComparison.Ordinal)))
        {
            object? previous = field.GetValue(null);
            restore.Push(() => field.SetValue(null, previous));
        }

        PreserveField(typeof(PageInstanceLeft), "Instance");
        foreach (string name in new[]
        {
            "_McFolderSelected",
            "_McInstanceSelected",
            "McInstanceList",
            "McInstanceListForceRefresh",
            "IsFirstMcInstanceListLoad"
        }

        )
            PreserveField(typeof(ModMinecraft), name);
        foreach (string name in new[]
        {
            "PCL.LocalGameData",
            "PCL.WorldBackups",
            "PCL.EnvironmentHistory"
        }

        )
        {
            PropertyInfo field = Native.Property(Native.Type(name), "TestRoot");
            object? previous = field.GetValue(null);
            restore.Push(() => field.SetValue(null, previous));
            field.SetValue(null, Path.Combine(Root, name.Split('.').Last()));
        }

        Native.Check(Native.Full((string)Native.Property(Native.Type("PCL.LocalGameData"), "Root").GetValue(null)!).StartsWith(Native.Full(Root) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), "Actual personal-data root escaped the controlled fixture.");
        ModBase.RunInUi(); // Initialize the genuine Native UI-thread identity on this STA.
    }

    private void PreserveField(Type owner, string name)
    {
        FieldInfo field = Native.Field(owner, name);
        object? previous = field.GetValue(null);
        restore.Push(() => field.SetValue(null, previous));
    }

    internal void SelectMinecraft(string root)
    {
        Directory.CreateDirectory(root);
        Native.Field(typeof(ModMinecraft), "_McFolderSelected").SetValue(null, root + Path.DirectorySeparatorChar);
    }

    internal static void CacheInstance(ModMinecraft.McInstance instance)
    {
        foreach (Settings.Setting setting in Settings.Entries.Values)
            if (setting.Source == Settings.Sources.Instance)
                setting.ValueCache[instance.PathVersion] = setting.DefaultValue!;
        Settings.Entries["VersionArgumentIndieV2"].ValueCache[instance.PathVersion] = true;
    }

    // The real stopped handlers post UI notifications and an instance-list
    // refresh. Drain them while their original Main/profile/folder context is
    // still isolated, rather than leaking them into a later theme dispatcher.
    internal void FinishActualTasks()
    {
        if (finished)
            return;
        DateTime deadline = DateTime.UtcNow.AddSeconds(10);
        foreach (ModLoader.LoaderBase task in ModLoader.LoaderTaskbar.Where(task => !originalTasks.Contains(task)).ToArray())
            if (task.State == ModBase.LoadState.Loading || task.State == ModBase.LoadState.Waiting)
                task.Cancel();
        while (true)
        {
            DrainPostedUi();
            bool busy = new[]
            {
                "PCL.WorldBackupsUi",
                "PCL.EnvironmentHistoryUi",
                "PCL.InstanceClonerUi"
            }.Any(type => (bool)Native.Property(Native.Type(type), "IsBusy").GetValue(null)!);
            bool locked = (bool)Native.Property(typeof(ModMinecraft.McInstanceLock), "IsAnyLocked").GetValue(null)!;
            bool listing = ModMinecraft.McInstanceListLoader.State == ModBase.LoadState.Loading;
            if (!busy && !locked && !listing)
                break;
            Native.Check(DateTime.UtcNow < deadline, "Actual Native file-task workload/lock/posted list refresh did not stop.");
            Thread.Sleep(2);
        }

        DrainPostedUi();
        foreach (ModLoader.LoaderBase task in ModLoader.LoaderTaskbar.Where(task => !originalTasks.Contains(task)).ToArray())
            ModLoader.LoaderTaskbar.Remove(task);
        environment.AssertNoForbiddenIo();
        finished = true;
    }

    private static void DrainPostedUi()
    {
        DispatcherFrame frame = new();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }

    public void Dispose()
    {
        try
        {
            FinishActualTasks();
        }
        finally
        {
            while (restore.Count != 0)
                restore.Pop()();
        }
    }
}
