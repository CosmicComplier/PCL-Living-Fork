using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using PCLCS;

namespace PCL;

// Original LF view; repository LICENCE applies.
internal sealed class MmcLifecycleCatalogView : ModpackDialogView {
    internal readonly List<MyListItem> Selections = [];
    internal MmcLifecycleCatalogEntry Selected { get; private set; }
    internal MmcLifecycleCatalogPage CurrentPage { get; private set; }
    internal MmcLifecycleCatalogPageRequest CurrentRequest { get; private set; }
    internal bool IsReading { get; private set; }
    private readonly CancellationTokenSource cancellation = new();
    private Task reading = Task.CompletedTask;
    private int closed;

    internal MmcLifecycleCatalogView(MmcLifecycleCatalogPageRequest request, MmcLifecycleCatalogPage page) : base(
        "MMC 隔离区与恢复", "按操作记录标识排序；请选择一条记录查看恢复或清理操作。", 3) {
        CurrentRequest = request;
        Header.Children.Add(Label("游戏文件夹：" + request.MinecraftRoot, 12, margin: new Thickness(0, 8, 0, 0)));
        AddFilter("上一页", 0, false, _ => Read(CurrentPage.Previous));
        AddFilter("下一页", 1, false, _ => Read(CurrentPage.Next));
        AddFilter("刷新列表", 2, true, _ => Read(null));
        Unloaded += (_, _) => CancelReading();
        Show(page);
    }

    private void Show(MmcLifecycleCatalogPage page) {
        CurrentPage = page; Selected = null; Selections.Clear(); SetCanConfirm(false); BeginFilter(-1);
        EnableNavigation();
        if (page.NeedsRefresh) { Rows.Children.Add(StatusHint(page.Diagnostic, (int)MyHint.Themes.Yellow)); return; }
        if (page.Entries.IsEmpty) { Rows.Children.Add(Label("当前游戏文件夹没有 MMC 隔离区或未完成的改名、移动、删除记录。", 13)); return; }
        Rows.Children.Add(Label("本页 " + page.Entries.Length + " 条；翻页或刷新后需要重新选择。", 12, margin: new Thickness(3, 4, 7, 10)));
        foreach (var entry in page.Entries) {
            var item = new MyListItem { Title = MmcDeleteUi.Label(entry), Info = entry.Description + " · 记录 " + Path.GetFileName(entry.OperationDirectory), Type = MyListItem.CheckType.RadioBox,
                Tag = entry, ToolTip = entry.Description + "\n\n操作记录：" + Path.GetFileName(entry.OperationDirectory),
                Height = 66, PaddingLeft = 14, MinPaddingRight = 12, Margin = new Thickness(3, 0, 7, 8) };
            item.Check += (_, _) => {
                if (IsReading || Volatile.Read(ref closed) != 0 || !ReferenceEquals(CurrentPage, page) || !Selections.Contains(item)) return;
                Selected = entry; SetCanConfirm(true);
            };
            Selections.Add(item); Rows.Children.Add(item);
        }
    }
    private void EnableNavigation() {
        ((MyButton)Filters.Children[0]).IsEnabled = !IsReading && CurrentPage?.Previous is not null;
        ((MyButton)Filters.Children[1]).IsEnabled = !IsReading && CurrentPage?.Next is not null;
        ((MyButton)Filters.Children[2]).IsEnabled = !IsReading;
    }
    private void Read(MmcLifecycleCatalogCursor cursor) {
        if (IsReading || Volatile.Read(ref closed) != 0) return;
        CurrentRequest = CurrentRequest with { Cursor = cursor };
        Selected = null; SetCanConfirm(false); Selections.Clear(); IsReading = true; EnableNavigation();
        BeginFilter(-1); Rows.Children.Add(Label("正在读取恢复记录…可取消并关闭列表。", 13));
        reading = ReadAsync(CurrentRequest);
    }
    private async Task ReadAsync(MmcLifecycleCatalogPageRequest request) {
        try {
            var page = await Task.Run(() => MmcInstanceLifecycleCatalog.ReadPage(request, cancellation.Token)).ConfigureAwait(false);
            if (Volatile.Read(ref closed) != 0) return;
            await Dispatcher.InvokeAsync(() => {
                if (Volatile.Read(ref closed) != 0) return;
                IsReading = false; Show(page);
            });
        } catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception error) {
            if (Volatile.Read(ref closed) != 0) return;
            await Dispatcher.InvokeAsync(() => {
                if (Volatile.Read(ref closed) != 0) return;
                IsReading = false; Selected = null; SetCanConfirm(false); EnableNavigation();
                Rows.Children.Clear(); Rows.Children.Add(StatusHint("读取失败，请刷新列表：" + error.Message, (int)MyHint.Themes.Yellow));
            });
        }
    }
    internal void CancelReading() {
        if (Interlocked.Exchange(ref closed, 1) == 0) cancellation.Cancel();
    }
    internal async Task CloseAndDrainAsync() {
        CancelReading();
        await reading.ConfigureAwait(false);
        cancellation.Dispose();
    }
}
