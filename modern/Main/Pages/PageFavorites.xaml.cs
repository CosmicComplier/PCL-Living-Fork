using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using PCLCS;

namespace PCL;

public partial class PageFavorites : MyPageRight, IRefreshable {
    private ResourceTypes? category;
    private bool reading;
    private int pageIndex;
    private const int PageSize = 40;
    public PageFavorites() {
        InitializeComponent();
        BtnRetry.Click += Retry_Click;
        BtnPageFirst.Click += PageFirst_Click;
        BtnPageLeft.Click += PageLeft_Click;
        BtnPageRight.Click += PageRight_Click;
        SearchBox.TextChanged += (_, _) => { pageIndex = 0; Render(); };
        ResourceFavorites.Observe(this, (_, _) => Render());
        Loaded += (_, _) => Refresh();
    }
    internal void ChooseCategory(ResourceTypes? value) {
        if (category == value) return;
        category = value; pageIndex = 0; Render(); PanBack.ScrollToHome();
    }
    public async void Refresh() {
        if (!Dispatcher.CheckAccess()) { _ = Dispatcher.BeginInvoke(new Action(Refresh)); return; }
        if (reading) return;
        reading = true; LabStatus.Text = "正在读取收藏……";
        BtnRetry.IsEnabled = false;
        await Task.Run(() => ResourceFavorites.Store.Reload()).ConfigureAwait(false);
        if (Dispatcher.HasShutdownStarted) { reading = false; return; }
        await Dispatcher.InvokeAsync(() => {
            reading = false;
            if (!IsLoaded) return;
            BtnRetry.IsEnabled = true;
            Render();
        });
    }
    private void Retry_Click(object sender, EventArgs e) => Refresh();
    private void PageFirst_Click(object sender, EventArgs e) { pageIndex = 0; Render(); PanBack.ScrollToHome(); }
    private void PageLeft_Click(object sender, EventArgs e) { pageIndex--; Render(); PanBack.ScrollToHome(); }
    private void PageRight_Click(object sender, EventArgs e) { pageIndex++; Render(); PanBack.ScrollToHome(); }
    private void Render() {
        if (reading) return;
        ResourceFavoriteStore store = ResourceFavorites.Store;
        var all = store.Snapshot;
        var searched = all.Where(entry => entry.Matches(null, SearchBox.Text)).ToArray();
        ModMain.FrmFavoritesLeft?.RefreshFavoriteCounts(searched);
        PanResults.Children.Clear();
        var shown = searched.Where(entry => entry.Matches(category, null)).ToArray();
        int pageCount = Math.Max(1, (shown.Length + PageSize - 1) / PageSize);
        pageIndex = Math.Clamp(pageIndex, 0, pageCount - 1);
        CardPages.Visibility = pageCount > 1 ? Visibility.Visible : Visibility.Collapsed;
        BtnPageFirst.IsEnabled = BtnPageLeft.IsEnabled = pageIndex > 0;
        BtnPageFirst.Opacity = BtnPageLeft.Opacity = pageIndex > 0 ? 1 : 0.2;
        BtnPageRight.IsEnabled = pageIndex + 1 < pageCount;
        BtnPageRight.Opacity = BtnPageRight.IsEnabled ? 1 : 0.2;
        LabPage.Text = (pageIndex + 1) + " / " + pageCount;
        foreach (ResourceFavorite entry in shown.Skip(pageIndex * PageSize).Take(PageSize)) {
            var project = new ResourceProject(entry.ProjectJson);
            PanResults.Children.Add(project.ToResourceItem(true, true, category));
        }
        Exception error = store.LoadError ?? store.LastOperationError;
        HintError.Visibility = error is null ? Visibility.Collapsed : Visibility.Visible;
        BtnRetry.Visibility = error is null ? Visibility.Collapsed : Visibility.Visible;
        HintError.Text = "无法读取或保存收藏，原文件已保留。" + (error is null ? "" : "\n" + error.Message);
        LabStatus.Visibility = shown.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        LabStatus.Text = error is not null && all.Count == 0 ? "收藏暂时不可用，可修复收藏文件后重新读取。" :
            all.Count == 0 ? "还没有收藏。点击在线资源或已识别本地模组旁的星标，即可收藏整个项目。" :
            "没有符合当前分类和搜索条件的收藏。";
    }
}
