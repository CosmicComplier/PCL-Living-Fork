using System.Collections;
using System.IO.Compression;
using System.Windows;
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
        byte[] alphaBytes = File.ReadAllBytes(alpha.File.FullName), betaBytes = File.ReadAllBytes(beta.File.FullName);

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

        var readKeys = (Func<ISet<string>>)Native.Get(host, "FavoriteKeys")!;
        MyLocalModItem betaRow = page.ModItems[beta.EnabledName];
        betaRow.Refresh();
        MyIconButton betaStar = (MyIconButton)betaRow.FindName("BtnFavorite");
        string betaFilled = betaStar.Logo;
        ISet<string> snapshot = readKeys();
        for (int i = 0; i < 5000; i++) Native.Check(page.IsModFavorite("BETA.JAR") && ReferenceEquals(snapshot, readKeys()), "Unchanged row lookups reparsed/replaced the favorites snapshot.");
        Native.Set(host, "ApplyMode", new Action(() => { }));
        Native.Set(host, "LastWasDetails", new Func<bool>(() => true));
        Native.Set(host, "BindMainKey", new Action(() => { }));
        Native.Set(host, "AdjustFilterMargins", new Action(() => { }));
        Native.Set(host, "RefreshLoaderWarning", new Action<MyLocalModItem, ModMinecraft.McInstance>((_, _) => { }));
        Native.Set(host, "LoaderRun", new Func<int, bool>(_ => false)); // shared mods folder: no rebuild
        ModMinecraft.McInstance other = new(Path.Combine(root, "second-instance"));
        PageInstanceLeft.Instance = other;
        page.PageOther_Loaded();
        Native.Check(!page.IsModFavorite(beta.EnabledName) && !ReferenceEquals(snapshot, readKeys()), "Changing instances retained the previous favorites snapshot.");
        Native.Check(betaStar.Logo != betaFilled && rows.Visibility == Visibility.Collapsed, "A reused page retained the other instance's filled star or favorite-filter rows.");
        PageInstanceLeft.Instance = owner;
        page.PageOther_Loaded();
        Native.Check(page.IsModFavorite(beta.EnabledName), "Returning to the first instance lost its favorites.");
        Native.Check(betaStar.Logo == betaFilled && rows.Visibility == Visibility.Visible && rows.Children.OfType<MyLocalModItem>().Count() == 1,
            "Returning to a reused page did not restore its stars and favorite-filter rows.");

        // Both adding and removing under a real Windows sharing failure retain the
        // prior typed cache, star, filtered rows, exact file bytes and other keys.
        betaRow.Refresh();
        byte[] beforeFailure = File.ReadAllBytes(instanceSetup);
        string cachedBeforeFailure = Settings.Get<string>(LegacyModFavorites.SettingKey, Instance: owner);
        snapshot = readKeys();
        using (FileStream held = new(instanceSetup, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            CallOnRow(row, "BtnFavorite_Click");
            Native.Check(!page.IsModFavorite(alpha.EnabledName) && star.Logo == likeLine, "A failed favorite add falsely filled the star/cache.");
            CallOnRow(betaRow, "BtnFavorite_Click");
            Native.Check(page.IsModFavorite(beta.EnabledName) && betaStar.Logo == betaFilled, "A failed favorite removal falsely cleared the star/cache.");
            Native.Check(rows.Children.OfType<MyLocalModItem>().Count() == 1 && ReferenceEquals(rows.Children.OfType<MyLocalModItem>().Single(), betaRow), "A failed save changed the favorites filter result.");
        }
        Native.Check(File.ReadAllBytes(instanceSetup).SequenceEqual(beforeFailure) &&
            Settings.Get<string>(LegacyModFavorites.SettingKey, Instance: owner) == cachedBeforeFailure && ReferenceEquals(snapshot, readKeys()), "A failed favorite save mutated the original bytes or cached snapshot.");

        WriteFavorites("ALPHA.jar|alpha.JAR|BETA.jar");
        Native.Check(readKeys().Count == 2 && page.IsModFavorite("aLpHa.JaR"), "Runtime snapshot did not deduplicate favorite casing.");
        string full = string.Join('|', Enumerable.Range(0, LegacyModFavorites.MaxCount - 1).Select(i => "fixture" + i.ToString("D4") + ".jar").Prepend(alpha.EnabledName));
        WriteFavorites(full);
        byte[] beforeOverflow = File.ReadAllBytes(instanceSetup);
        Native.Check(!page.ToggleModFavorite(beta.EnabledName) && File.ReadAllBytes(instanceSetup).SequenceEqual(beforeOverflow) &&
            Settings.Get<string>(LegacyModFavorites.SettingKey, Instance: owner) == full && readKeys().Count == LegacyModFavorites.MaxCount,
            "Count overflow changed the old favorites, Setup.ini or typed cache.");
        Native.Check(!page.ToggleModFavorite("ALPHA.JAR") && readKeys().Count == LegacyModFavorites.MaxCount - 1, "Un-favoriting failed at the count limit.");

        string large = new('x', LegacyModFavorites.MaxLength - 1);
        WriteFavorites(large);
        beforeOverflow = File.ReadAllBytes(instanceSetup);
        Native.Check(!page.ToggleModFavorite(beta.EnabledName) && File.ReadAllBytes(instanceSetup).SequenceEqual(beforeOverflow), "Byte-length overflow changed the original favorites.");
        WriteFavorites(new string('x', LegacyModFavorites.MaxLength + 10) + "|" + alpha.EnabledName);
        Native.Check(!page.ToggleModFavorite(alpha.EnabledName) && !page.IsModFavorite(alpha.EnabledName), "An oversized old value prevented un-favoriting.");

        WriteFavorites(alpha.EnabledName);
        Call("RefreshUI");
        Native.Check(rows.Children.OfType<MyLocalModItem>().Count() == 1, "Favorites filter did not restore the last favorite row.");
        CallOnRow(row, "BtnFavorite_Click");
        Native.Check(rows.Visibility == System.Windows.Visibility.Collapsed && Part<FrameworkElement>("PanEmpty").Visibility == System.Windows.Visibility.Visible &&
            Part<TextBlock>("LabEmptyTitle").Text == "暂无收藏的 Mod", "Removing the last favorite did not show the dedicated empty state.");
        Native.Check((int)Native.Property(controller.GetType(), "ListRebuildSerial").GetValue(controller)! == serial &&
            ModBase.ReadIni(instanceSetup, "Keep", "") == "原实例设置" &&
            File.ReadAllBytes(alpha.File.FullName).SequenceEqual(alphaBytes) && File.ReadAllBytes(beta.File.FullName).SequenceEqual(betaBytes),
            "Favorite operations rebuilt the list or changed another setting/mod file.");

        // The same native row class also serves resources. Rebinding an already
        // visible mod row must hide its star immediately and never mutate favorites.
        object resource = Activator.CreateInstance(Native.Type("PCL.InstanceResourceEntry"), true)!;
        ((InstanceResourceEntryData)resource).Name = "fixture-resource.zip";
        ((InstanceResourceEntryData)resource).FullPath = Path.Combine(root, "fixture-resource.zip");
        Native.Call(typeof(MyLocalModItem), "BindResource", row, resource, null);
        byte[] beforeResourceClick = File.ReadAllBytes(instanceSetup);
        Native.Check(star.Visibility == Visibility.Collapsed, "Rebinding a mod row as a resource retained its star.");
        CallOnRow(row, "BtnFavorite_Click");
        Native.Check(File.ReadAllBytes(instanceSetup).SequenceEqual(beforeResourceClick), "A reused resource row wrote mod favorites.");

        environment.AssertNoForbiddenIo();
        Console.WriteLine("PASS native mod favorites: actual row icons/filter/empty state, 5000 cached case-insensitive lookups, instance switching, locked add/remove rollback, count/byte overflows and unlimited removal; instance-only Setup.ini, unrelated settings/mod bytes and rebuild serial preserved.");

        void WriteFavorites(string value)
        {
            File.WriteAllText(instanceSetup, "Keep:原实例设置\r\n" + LegacyModFavorites.SettingKey + ":" + value + "\r\n");
            ModBase.IniClearCache(instanceSetup);
            Native.Call(Native.Entries[LegacyModFavorites.SettingKey]!.GetType(), "Set", Native.Entries[LegacyModFavorites.SettingKey], value, owner);
        }

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
