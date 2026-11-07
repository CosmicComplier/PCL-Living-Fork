using System;
using System.Threading.Tasks;
using System.Windows;
using PCLCS;

namespace PCL;

// Uses the very same PageResource BAML as the online download pages.
public partial class PageFavorites : MyPageRight, IRefreshable {
    private bool reading;
    public PageFavorites() {
        InitializeComponent();
        Content.UseFavorites(ResourceFavorites.Store, Refresh, entries => ModMain.FrmFavoritesLeft?.RefreshFavoriteCounts(entries));
        ResourceFavorites.Observe(this, (_, _) => Content.FavoritesChanged());
        Loaded += (_, _) => Refresh();
    }
    internal void ChooseCategory(ResourceTypes? value) => Content.ChooseFavoriteCategory(value);
    public async void Refresh() {
        if (!Dispatcher.CheckAccess()) { _ = Dispatcher.BeginInvoke(new Action(Refresh)); return; }
        if (reading) return;
        reading = true; Content.SetFavoritesLoading(true);
        await Task.Run(() => ResourceFavorites.Store.Reload()).ConfigureAwait(false);
        if (Dispatcher.HasShutdownStarted) { reading = false; return; }
        await Dispatcher.InvokeAsync(() => {
            reading = false;
            if (!IsLoaded) return;
            Content.SetFavoritesLoading(false);
            Content.FavoritesChanged(true);
        });
    }
}
