using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using PCLCS;
using PCLCS.Startup;
using PCLLF.Presentation.Mods;
using PCLLF.Presentation.Windowing;

namespace PCLLF.Modern.Wpf.Tests;

// A-003 feature checks against the real presentation controllers with host fakes.
// No dialog window, list reload or real settings file is required here; the native
// facade/窗口级 evidence remains separate.
internal static class A003Checks {
    internal static void Run() {
        UpdateLogChecks();
        ModFavoriteChecks();
        Console.WriteLine("PASS A-003 update-log/favorites controller checks: ShouldAutoShowUpdateLog tri-state, dismiss write-failure rollback, Upgrade gate, single note flag, favorites filter/serial/empty state and search+sort combination.");
    }

    // ---------- A-003.1 update-log prompt ----------
    private sealed class FakeStack : ILegacyMainPageStack {
        public int PageCode { get; set; }
        public object Additional => null;
    }
    private struct FakeAnim { }
    private sealed class UpdateLogHost {
        public bool Prompt = true;
        public bool PersistResult = true;
        public bool ThrowOnRead;
        public int MessageResult = 1;
        public LegacyMainMessage? LastMessage;
        public int PersistCalls;
        public string? Website;
        public readonly List<Action> Threads = new();
        public readonly List<string> Writes = new();
        public int OpenWebsiteCount => Website is null ? 0 : 1;
        internal LegacyMainWindowHost<FakeStack, object, FakeAnim> Build() => new() {
            ReadBoolean = key => {
                if (ThrowOnRead) throw new InvalidOperationException("synthetic read failure");
                return key == "SystemUpdateLogPrompt" ? Prompt : false;
            },
            PersistUpdateLogPrompt = value => {
                PersistCalls++;
                if (PersistResult) Prompt = value;
                return PersistResult;
            },
            Message = message => { LastMessage = message; return MessageResult; },
            NewThread = (work, name, priority) => Threads.Add(work),
            OpenWebsite = url => Website = url,
            LauncherName = () => "LivingPCL",
            VersionBaseName = () => "synthetic",
            ReleasesUrl = () => "https://example.invalid/releases",
            ReadInteger = key => key == "LaunchArgumentWindowType" ? 1 : 0,
            ReadString = key => "0|1|2|3|4",
            HasSaved = key => false,
            DefaultValue = key => 0,
            WriteSetting = (key, value) => Writes.Add(key + "=" + value),
            ResetSetting = key => Writes.Add("reset:" + key),
            TempFolder = () => Path.Combine(Path.GetTempPath(), "pcl-a003-temp") + Path.DirectorySeparatorChar,
            VersionCode = 1000,
            BuildType = 0,
            VersionBranchMain = () => "OpenSource"
        };
    }

    private static void UpdateLogChecks() {
        var host = new UpdateLogHost();
        var controller = new LegacyMainWindowController<FakeStack, object, FakeAnim>(new LegacyMainWindowView(), host.Build());

        // Tri-state: true, false, and a throwing reader falls back to the default (show).
        host.Prompt = true; Check(controller.ShouldAutoShowUpdateLog(), "true must auto-show.");
        host.Prompt = false; Check(!controller.ShouldAutoShowUpdateLog(), "false must suppress.");
        host.ThrowOnRead = true; Check(controller.ShouldAutoShowUpdateLog(), "read failure must fall back to true.");
        host.ThrowOnRead = false;

        // Upgrade() gate: false suppresses ShowUpdateLog entirely; true schedules it once.
        controller.Upgrade(999);
        Check(host.Threads.Count == 0, "Suppressed prompt must not schedule the update log.");
        host.Prompt = true;
        controller.Upgrade(999);
        Check(host.Threads.Count == 1, "Enabled prompt must schedule exactly one update log thread.");

        // Session flag: a scheduled update log marks the automatic note as issued.
        object? issued = typeof(LegacyMainWindowController<FakeStack, object, FakeAnim>)
            .GetField("updateLogPromptIssued", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(controller);
        Check(issued is true, "The session flag must record the automatic update note.");

        // Fake threads are captured, never run during Upgrade; run the captured one.
        Action logThread = host.Threads[0];
        host.Threads.Clear();
        host.MessageResult = 1;
        logThread();
        Check(host.LastMessage is not null && host.LastMessage.Button3 == "不再弹出"
            && host.LastMessage.Button1 == "确定" && host.LastMessage.Button2 == "查看更新日志",
            "Update log dialog must expose 确定/查看更新日志/不再弹出.");
        Check(host.PersistCalls == 0 && host.OpenWebsiteCount == 0, "确定 must neither write nor open a website.");

        // Button 3: writes false through the single persisted path and opens no website.
        host.Prompt = true; host.PersistResult = true; host.MessageResult = 3;
        logThread();
        Check(host.PersistCalls == 1 && host.OpenWebsiteCount == 0 && !host.Prompt, "不再弹出 must persist false without opening a website.");

        // Write failure: silent, session keeps true (no 置位), still closes.
        host.Prompt = true; host.PersistResult = false; host.PersistCalls = 0; host.MessageResult = 3;
        logThread();
        Check(host.PersistCalls == 1 && host.Prompt && controller.ShouldAutoShowUpdateLog(),
            "Rejected write must leave the session preference unchanged (still shows).");

        // Button 2 keeps opening the releases page and never writes.
        host.Prompt = true; host.PersistCalls = 0; host.MessageResult = 2;
        logThread();
        Check(host.OpenWebsiteCount == 1 && host.Website == "https://example.invalid/releases" && host.PersistCalls == 0,
            "查看更新日志 must open releases and never write the preference.");

        // Read failure during Upgrade still shows (default true).
        host.ThrowOnRead = true; host.Threads.Clear(); controller.Upgrade(999);
        Check(host.Threads.Count == 1, "Read failure must fall back to showing the update log.");
        host.ThrowOnRead = false;
    }

    // ---------- A-003.2 mod favorites ----------
    private sealed class FakeMod : LocalResourceFileData<ResourceProjectModelData, ResourceVersionModelData> {
        public FakeMod(string path) : base(new FileInfo(path), new LocalResourceFileServices<ResourceProjectModelData, ResourceVersionModelData> {
            ReadProject = _ => throw new InvalidOperationException("no project"),
            ReadVersion = _ => throw new InvalidOperationException("no version"),
            HideUpdates = () => false, DisableInstanceUpdates = () => false,
            ReadHashCache = _ => "", WriteHashCache = (_, _) => { },
            ReadBytes = _ => Array.Empty<byte>(), Sha1 = _ => ""
        }) { }
    }

    private static void ModFavoriteChecks() {
        string root = Path.Combine(Path.GetTempPath(), "pcl-a003-mods-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try {
            List<FakeMod> mods = new();
            string[] names = { "Alpha.jar", "Beta.jar.disabled", "Gamma.jar" };
            DateTime created = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            for (int index = 0; index < names.Length; index++) {
                string file = Path.Combine(root, names[index]);
                File.WriteAllBytes(file, new byte[] { 1 });
                File.SetCreationTimeUtc(file, created.AddDays(index));
                FakeMod mod = new(file) { Display = "needle " + (index + 1) };
                mods.Add(mod);
            }

            List<string> favorites = new() { "alpha.jar" };
            int loaderCalls = 0;
            ConcurrentDictionary<string, Border> items = new();
            List<string> selected = new();
            bool[] checkedState = new bool[5];
            string[] texts = new string[5];
            string? emptyTitle = null, emptyHint = null;
            string search = "";
            Panel list = new StackPanel();
            Border empty = new();
            Border[] filterElements = Enumerable.Range(0, 5).Select(index => new Border { Tag = index }).ToArray();
            ResourcePageFilter[] filters = filterElements.Select((element, index) => new ResourcePageFilter(element, index,
                () => checkedState[index], value => checkedState[index] = value, value => texts[index] = value)).ToArray();

            LegacyModPageView view = new() {
                Root = () => new Border(), Back = () => new Border(), Empty = () => empty, ListBack = () => new Border(),
                List = () => list, Search = () => search, SetSearch = value => search = value,
                SetSelectionText = _ => { }, ScrollHome = () => { }, FocusBack = () => { }, EnableIndex = _ => { }, IndexText = _ => { },
                Filters = () => filters, EnableDisable = _ => { }, EnableEnable = _ => { }, EnableUpdate = _ => { },
                SelectionBar = (int count, ref int previous, bool animate) => { }, SetEmptyTitle = value => emptyTitle = value, SetEmptyHint = value => emptyHint = value
            };
            LegacyModPageHost<FakeMod, ResourceProjectModelData, ResourceVersionModelData, Border, object, object, Border, ModDependencyImpactItemData> host = new() {
                Output = () => mods, Items = () => items, Selected = () => selected, SetSelected = value => selected = value,
                Instance = () => null, AnimationControl = () => 0, ChangeAnimationControl = _ => { },
                LoaderRun = type => { loaderCalls++; return true; },
                NewItem = (entry, isSelected) => new Border { Tag = entry },
                BindUpdate = (_, _) => { }, SetChecked = (_, _) => { },
                FavoriteKeys = () => favorites,
                ToggleFavorite = key => {
                    // Same normalization the product runtime applies before Settings.Set.
                    string normalized = LegacyModFavorites.Normalize(key);
                    if (favorites.RemoveAll(item => string.Equals(item, normalized, StringComparison.OrdinalIgnoreCase)) > 0) return false;
                    favorites.Add(normalized); return true;
                }
            };
            LegacyModPage<FakeMod, ResourceProjectModelData, ResourceVersionModelData, Border, object, object, Border, ModDependencyImpactItemData> page = new(view, host);

            page.LoadUI();
            Check(page.ListRebuildSerial == 1, "LoadUI must increment ListRebuildSerial once.");
            Check(VisibleRows(list).Count == 3 && items.Count == 3, "Initial render must show all three mods.");
            int serial = page.ListRebuildSerial;
            Check(loaderCalls == 0, "LoadUI must not run the loader.");

            // Toggling a favorite must not rebuild the list or reload it.
            bool nowFavorite = page.ToggleFavorite("beta.jar");
            Check(nowFavorite && favorites.Contains("beta.jar"), "Star must add the EnabledName key.");
            Check(page.ListRebuildSerial == serial && loaderCalls == 0, "Favorite toggle must not rebuild/reload the list.");
            Check(VisibleRows(list).Count == 3 && items.Count == 3, "Favorite toggle must keep every row in place.");
            Check(page.IsFavorite("BETA.jar"), "Favorite lookup is OrdinalIgnoreCase on EnabledName.");

            Check(!page.ToggleFavorite("ALPHA.jar") && !favorites.Contains("alpha.jar"), "Second click must remove the same normalized key.");
            Check(page.ListRebuildSerial == serial, "Un-favorite must not rebuild the list either.");

            // Favorites filter + counts, including the always-visible 0 case.
            favorites.Clear(); favorites.Add("alpha.jar"); favorites.Add("gamma.jar");
            page.FilterChanged(4);
            Check(page.Filter == LegacyModFilter.Favorites, "FilterChanged(4) must select LegacyModFilter.Favorites.");
            Check(VisibleRows(list).Count == 2, "Favorites filter must show only favorited rows.");
            Check(texts[4] == "收藏 (2)", "Favorites filter must show 收藏 (n) with the current search scope.");
            Check(page.ListRebuildSerial == serial, "Filtering must not rebuild the list.");

            // Search + favorites + sort combine without touching the rebuild counter.
            search = "gamma";
            page.SearchRun();
            Check(VisibleRows(list).Count == 1 && VisibleRows(list)[0].Tag == mods[2], "Search must intersect with favorites.");
            page.SortOrder = LocalResourceSortOrder.AddedOldest;
            Check(VisibleRows(list).Count == 1 && VisibleRows(list)[0].Tag == mods[2], "Sorting must keep the favorites+search result correct.");
            search = "";
            page.SearchRun();
            Check(VisibleRows(list).Count == 2, "Clearing search must restore both favorites.");
            Check(page.ListRebuildSerial == serial, "Search/sort must not rebuild the list.");

            // Enabled/Disabled/CanUpdate combine with favorites.
            favorites.Clear(); favorites.Add("alpha.jar"); favorites.Add("beta.jar"); favorites.Add("gamma.jar");
            page.FilterChanged(1);
            Check(VisibleRows(list).Count == 2 && VisibleRows(list).All(item => ((FakeMod)item.Tag).IsEnabled),
                "Favorites+Enabled must intersect correctly.");
            page.FilterChanged(2);
            Check(VisibleRows(list).Count == 1 && !((FakeMod)VisibleRows(list)[0].Tag).IsEnabled, "Favorites+Disabled must intersect correctly.");
            page.FilterChanged(3);
            Check(VisibleRows(list).Count == 0, "Favorites+CanUpdate keeps only updated mods (none here).");

            // 0 favorites gives an empty list plus the dedicated empty state.
            favorites.Clear();
            page.FilterChanged(4);
            Check(VisibleRows(list).Count == 0, "Empty favorites filter must show no rows.");
            Check(emptyTitle == "暂无收藏的 Mod" && emptyHint is not null && empty.Visibility == System.Windows.Visibility.Visible,
                "Empty favorites filter must show its own empty state.");
            Check(page.ListRebuildSerial == serial, "Empty favorites must not rebuild the list.");

            // ReloadModList is the only other serial writer and keeps favorites.
            page.ReloadModList();
            Check(page.ListRebuildSerial == serial + 1 && loaderCalls == 1, "ReloadModList success must increment the serial once.");
            Check(favorites.Count == 0, "ReloadModList must not clear favorites.");
        } finally {
            try { Directory.Delete(root, true); } catch { }
        }

        static List<Border> VisibleRows(Panel panel) => panel.Visibility == System.Windows.Visibility.Visible ? panel.Children.Cast<Border>().ToList() : new List<Border>();
    }

    private static void Check(bool condition, string message) {
        if (!condition) throw new InvalidOperationException(message);
    }
}
