using System.Collections;
using System.IO.Compression;
using System.Windows.Controls;
using MeloongCore;
using PCL;
using PCLCS;

namespace PCLLF.Native.Facade.Tests;

// Real MyLocalModItem star button -> real PageInstanceMod controller -> Settings
// instance store. Only the loader output is isolated; no second favorites writer.
internal static class ModFavoriteChecks
{
    internal static void Run(FixtureEnvironment environment)
    {
        string root = Path.Combine(environment.Root, "mod-favorites");
        Directory.CreateDirectory(root);
        ModMinecraft.McInstance? previous = PageInstanceLeft.Instance;
        PageInstanceMod? previousPage = ModMain.FrmInstanceMod;
        ModMinecraft.McInstance owner = new(Path.Combine(root, "instance"));
        try
        {
            PageInstanceLeft.Instance = owner;
            RunPage(environment, root, owner);
        }
        finally
        {
            ModMain.FrmInstanceMod = previousPage;
            PageInstanceLeft.Instance = previous;
        }
    }

    private static void RunPage(FixtureEnvironment environment, string root, ModMinecraft.McInstance owner)
    {
        LocalResourceFile alpha = Mod("Alpha.jar", "alpha needle", 2001);
        LocalResourceFile beta = Mod("Beta.jar.disabled", "beta needle", 2002);
        List<LocalResourceFile> output = [alpha, beta];
        PageInstanceMod page = new();
        ModMain.FrmInstanceMod = page;
        object controller = Native.Get(page, "Native")!;
        object host = Native.Get(controller, "host")!;
        int animations = 1;
        Native.Set(host, "Output", new Func<List<LocalResourceFile>?>(() => output));
        Native.Set(host, "Instance", new Func<ModMinecraft.McInstance?>(() => owner));
        Native.Set(host, "AnimationControl", new Func<int>(() => animations));
        Native.Set(host, "ChangeAnimationControl", new Action<int>(value => animations += value));
        Native.Set(host, "LoaderRun", new Func<int, bool>(_ => true));
        Native.Set(host, "NewItem", new Func<LocalResourceFile, bool, MyLocalModItem>((entry, selected) => new() { Entry = entry, Title = entry.Display, Description = entry.File.Name, Checked = selected }));
        Native.Set(host, "BindUpdate", new Action<LocalResourceFile, MyLocalModItem>((_, _) => { }));
        void Call(string method, params object?[] args) => Native.Call(controller.GetType(), method, controller, args);
        T Part<T>(string name) where T : class => (page.FindName(name) ?? Native.Get(page, name)) as T ?? throw new InvalidOperationException("Missing real mod page part " + name);

        // Real page controller over the real row controls; only the loader output is
        // isolated, so no second favorites writer or copied controller is involved.
        foreach (LocalResourceFile entry in output)
            page.ModItems[entry.EnabledName] = new MyLocalModItem { Entry = entry, Title = entry.Display, Description = entry.File.Name };
        Call("RefreshUI");
        Panel rows = Part<Panel>("PanList");
        Native.Check(rows.Children.OfType<MyLocalModItem>().Count() == 2, "The real mod page did not render both fixture mods.");
        MyLocalModItem row = rows.Children.OfType<MyLocalModItem>().Single(item => ReferenceEquals(item.Entry, alpha));
        row.Refresh();
        MyIconButton star = row.FindName("BtnFavorite") as MyIconButton ?? throw new InvalidOperationException("The real row has no favorite button.");
        Native.Check(star.Visibility == System.Windows.Visibility.Visible, "Favorite star is not visible for a real mod row.");
        string likeLine = star.Logo;
        Native.Check(!string.IsNullOrEmpty(likeLine), "The un-favorited star has no like-line icon data.");

        int serial = (int)Native.Property(controller.GetType(), "ListRebuildSerial").GetValue(controller)!;
        int itemCount = page.ModItems.Count;
        int rowCount = rows.Children.Count;
        Call("ReloadModList", false);
        serial = (int)Native.Property(controller.GetType(), "ListRebuildSerial").GetValue(controller)!;
        Native.Check(serial == 1, "ReloadModList success must increment ListRebuildSerial exactly once.");
        CallOnRow(row, "BtnFavorite_Click");

        Native.Check((int)Native.Property(controller.GetType(), "ListRebuildSerial").GetValue(controller)! == serial,
            "Clicking the star rebuilt the list (ListRebuildSerial changed).");
        Native.Check(page.ModItems.Count == itemCount && rows.Children.Count == rowCount,
            "Clicking the star reloaded or removed rows instead of re-binding in place.");
        Native.Check(page.IsModFavorite(alpha.EnabledName), "The real page did not record the favorite after the click.");
        Native.Check(star.Logo != likeLine, "Favorited star must switch its icon in place.");

        string instanceSetup = owner.PathVersion + @"PCL\Setup.ini";
        string instanceValue = ModBase.ReadIni(instanceSetup, LegacyModFavorites.SettingKey, "");
        Native.Check(Native.Full(instanceSetup).StartsWith(Native.Full(root) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase),
            "The instance Setup.ini path escaped the fixture.");
        Native.Check(instanceValue.Length > 0 && instanceValue.Split('|').Contains(alpha.EnabledName.ToLowerInvariant()),
            "The favorite key was not persisted to the instance Setup.ini: " + instanceValue);
        Native.Check(ModBase.ReadIni("Setup", LegacyModFavorites.SettingKey, "") == "" && !ModBase.HasIniKey("Setup", LegacyModFavorites.SettingKey),
            "The favorite key leaked into the launcher Setup.ini.");
        string launcherSetup = Paths.Base + @"PCL\Setup.ini";
        if (File.Exists(launcherSetup))
            Native.Check(!File.ReadAllText(launcherSetup).Contains(LegacyModFavorites.SettingKey, StringComparison.Ordinal),
                "The launcher Setup.ini text contains the instance-scoped favorite key.");

        // Second click removes the same normalized key, still without a rebuild.
        CallOnRow(row, "BtnFavorite_Click");
        Native.Check(!page.IsModFavorite(alpha.EnabledName), "The second star click did not remove the favorite.");
        Native.Check((int)Native.Property(controller.GetType(), "ListRebuildSerial").GetValue(controller)! == serial,
            "Un-favoriting rebuilt the list.");
        string removedValue = ModBase.ReadIni(instanceSetup, LegacyModFavorites.SettingKey, "");
        Native.Check(!removedValue.Split('|').Contains(alpha.EnabledName.ToLowerInvariant()),
            "The instance Setup.ini favorite key was not removed by the toggle: " + removedValue);
        Native.Check(star.Logo == likeLine, "Un-favorited star must return to the original like-line icon.");

        // Favorites filter shows only favorited rows and keeps the serial stable.
        Call("ToggleFavorite", beta.EnabledName);
        Call("FilterChanged", 4);
        Native.Check(rows.Children.OfType<MyLocalModItem>().Count() == 1 && ReferenceEquals(rows.Children.OfType<MyLocalModItem>().Single().Entry, beta),
            "Favorites filter did not show only the favorited row.");
        Native.Check((int)Native.Property(controller.GetType(), "ListRebuildSerial").GetValue(controller)! == serial,
            "Applying the favorites filter rebuilt the list.");
        Native.Check(!File.Exists(launcherSetup) || !File.ReadAllText(launcherSetup).Contains(LegacyModFavorites.SettingKey, StringComparison.Ordinal),
            "Filtering wrote the favorite key into the launcher Setup.ini.");

        environment.AssertNoForbiddenIo();
        Console.WriteLine("PASS native mod favorites: real row star toggles like-line/like-fill in place, instance PCL\\Setup.ini owns UiModFavorite (launcher Setup.ini 0 hits), favorites filter and ListRebuildSerial stay unchanged.");

        void CallOnRow(MyLocalModItem item, string method) => Native.Call(typeof(MyLocalModItem), method, item, new object?[] { item, EventArgs.Empty });

        LocalResourceFile Mod(string name, string title, int year)
        {
            string path = Path.Combine(root, name);
            using (ZipArchive zip = ZipFile.Open(path, ZipArchiveMode.Create))
            using (StreamWriter writer = new(zip.CreateEntry("fabric.mod.json").Open()))
                writer.Write("{\"schemaVersion\":1,\"id\":\"favorite_fixture\",\"version\":\"1\"}");
            DateTime created = new(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            File.SetCreationTimeUtc(path, created);
            return new LocalResourceFile(path) { Display = title };
        }
    }
}
