using System.Windows.Controls;
using MeloongCore;
using PCL;

namespace PCLLF.Native.Facade.Tests;
// Assertions are the pinned Test-FileTaskNavigation.ps1 PageBack reproduction,
// through genuine Native controls/ports and its real PF controller.
internal static class FileTaskNavigationScenarios
{
    internal static void Run(FileTaskScope scope)
    {
        string minecraft = Path.Combine(scope.Root, "navigation", "minecraft");
        string game = Path.Combine(minecraft, "versions", "fixture");
        scope.SelectMinecraft(minecraft);
        Directory.CreateDirectory(Path.Combine(game, "PCL"));
        File.WriteAllText(Path.Combine(game, "fixture.json"), "{\"id\":\"fixture\",\"type\":\"release\",\"mainClass\":\"fixture.Main\",\"libraries\":[]}");
        File.WriteAllText(Path.Combine(game, "PCL", "Setup.ini"), "VersionArgumentIndieV2:True");
        ModMinecraft.McInstance instance = new(game);
        PageInstanceLeft.Instance = instance;
        FileTaskScope.CacheInstance(instance);
        FormMain main = FileTaskShell.Install();
        PageInstanceLeft left = new();
        ModMain.FrmInstanceLeft = left;
        RedoableWorker? worker = new((Action)(() =>
        {
        }));
        Native.Check((bool)Native.Call(typeof(ModMinecraft.McInstanceLock), "TryAcquireSnapshot", null, worker, instance.PathVersion)!, "Fixture snapshot reservation unavailable.");
        try
        {
            foreach (string mode in new[]
            {
                "snapshot",
                "completed"
            }

            )
            {
                if (mode == "completed")
                {
                    worker!.Start();
                    Native.Check(worker.WaitIfRunning(5000), "Navigation snapshot fixture did not stop.");
                    DateTime deadline = DateTime.UtcNow.AddSeconds(5);
                    while (ModMinecraft.McInstanceLock.IsLocked(instance))
                    {
                        Native.Check(DateTime.UtcNow < deadline, "Stopped navigation worker retained its reservation.");
                        Thread.Sleep(1);
                    }

                    worker = null;
                }

                foreach (FormMain.PageSubType id in new[]
                {
                    FormMain.PageSubType.InstanceRestorePoints,
                    FormMain.PageSubType.InstanceClone,
                    FormMain.PageSubType.InstanceSaves
                }

                )
                {
                    StopAnimations();
                    left.PageID = id;
                    MyListItem item = Item(left, id);
                    item.SetChecked(true, false, false);
                    object expected = left.PageGet(id);
                    ResetTaskManager(main);
                    main.PageBack();
                    Native.Check(ReferenceEquals(expected, main.PageRight) && left.PageID == id && item.Checked, mode + " task return did not preserve its originating sidebar/content: " + id);
                    Console.WriteLine("PASS native file-tasks " + mode + " task return: " + id + "; real sidebar and content agree.");
                }

                if (mode == "snapshot")
                {
                    MyPageRight previous = main.PageRight;
                    FormMain.PageSubType previousId = left.PageID;
                    MyListItem oldItem = Item(left, previousId);
                    FormMain.PageSubType editId = FormMain.PageSubType.InstanceSetup;
                    MyListItem edit = Item(left, editId);
                    edit.SetChecked(true, true, false);
                    Native.Check(left.PageID == previousId && oldItem.Checked && !edit.Checked && ReferenceEquals(previous, main.PageRight), "Rejected editor click left sidebar and content inconsistent.");
                    left.PageID = editId;
                    edit.SetChecked(true, false, false);
                    main.PageCurrent = FormMain.PageType.TaskManager;
                    main.PageStack.Clear();
                    main.PageStack.Add(FormMain.PageType.InstanceSetup);
                    main.PageBack();
                    FormMain.PageSubType overview = FormMain.PageSubType.InstanceOverall;
                    MyListItem overviewItem = Item(left, overview);
                    Native.Check(left.PageID == overview && overviewItem.Checked && !edit.Checked && ReferenceEquals(left.PageGet(overview), main.PageRight), "Restricted-editor fallback selected a different page from its content.");
                    Console.WriteLine("PASS native file-tasks restricted-editor rejection and fallback keep navigation consistent.");
                }
            }
        }
        finally
        {
            StopAnimations();
            if (worker is not null)
            {
                worker.Start();
                Native.Check(worker.WaitIfRunning(5000), "Navigation reservation cleanup did not finish.");
            }
        }
    }

    private static MyListItem Item(PageInstanceLeft left, FormMain.PageSubType id) => (MyListItem)Native.Call(typeof(PageInstanceLeft), "NavigationItem", left, id)!;
    private static void ResetTaskManager(FormMain main)
    {
        Border left = new()
        {
            Child = new MyPageLeft()
        };
        Border right = new()
        {
            Child = new MyPageRight()
        };
        FileTaskShell.Set(main, "PanMainLeft", left);
        FileTaskShell.Set(main, "PanMainRight", right);
        main.PageLeft = (MyPageLeft)left.Child;
        main.PageRight = (MyPageRight)right.Child;
        main.PageCurrent = FormMain.PageType.TaskManager;
        main.PageStack = new List<FormMain.PageStackData>
        {
            FormMain.PageType.InstanceSetup
        };
    }

    private static void StopAnimations()
    {
        foreach (string name in new[]
        {
            "FrmMain Titlebar SubLayer",
            "FrmMain PageChangeLeft",
            "FrmMain PageChangeRight",
            "PageLeft PageChange",
            "FrmMain LeftChange"
        }

        )
            Native.Static("PCL.ModAnimation", "AniStop", name);
    }
}
