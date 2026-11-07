using PCLCS;

internal static class ResourceFavoriteTests {
    internal static IEnumerable<(string Name, Action Run)> Cases() {
        yield return ("resource favorites use platform project identities and preserve Modrinth case", Identity);
        yield return ("resource favorites retain every supported resource category and offline metadata", Metadata);
        yield return ("resource favorites combine source tag exact cached version and loader filters without guessing Drops", Filters);
        yield return ("resource favorites persist one project across versions and never rewrite legacy settings", Persistence);
        yield return ("resource favorites reject damaged stores and preserve the last good snapshot", Corruption);
        yield return ("resource favorites publish only after successful atomic persistence", SaveFailure);
        yield return ("resource favorites serialize concurrent stores without losing projects", ConcurrentStores);
        yield return ("resource favorites refuse overflow and malformed project identities", Limits);
    }
    private static ResourceFavorite Project(string id = "AbCd1234", ResourceTypes type = ResourceTypes.Mod, ResourcePlatforms platform = ResourcePlatforms.Modrinth, string name = "Fixture project") =>
        ResourceFavorite.FromMetadata(new JObject {
            ["Platform"] = (int)platform, ["Types"] = (int)type, ["Id"] = id, ["Slug"] = "fixture-slug", ["RawName"] = name,
            ["Description"] = "Offline fixture description", ["LastUpdate"] = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), ["DownloadCount"] = 123,
            ["Tags"] = new JArray("fixture"), ["Drops"] = new JArray(21), ["Website"] = "https://example.invalid/project"
        }, new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc));
    private static void Identity() {
        Check(ResourceFavoriteIdentity.TryCreate(ResourcePlatforms.Modrinth, "AbCd1234", out var mixed), "Valid project rejected");
        Check(ResourceFavoriteIdentity.TryCreate(ResourcePlatforms.Modrinth, "abcd1234", out var lower) && mixed != lower, "Modrinth IDs lost case");
        Check(ResourceFavoriteIdentity.TryCreate(ResourcePlatforms.CurseForge, "000123", out var cf) && cf.ProjectId == "123", "Numeric provider identity not canonical");
        Check(ResourceFavoriteIdentity.TryCreate(ResourcePlatforms.Modrinth, "123", out var mr) && cf != mr, "Providers were merged");
        foreach (string invalid in new[] { "", " ", "a.jar", "file/path", " project", "https://modrinth.com/x" })
            Check(!ResourceFavoriteIdentity.TryCreate(ResourcePlatforms.Modrinth, invalid, out _), "Filename/path became a project ID");
        Check(!ResourceFavoriteIdentity.TryCreate(ResourcePlatforms.Any, "123", out _) && !ResourceFavoriteIdentity.TryCreate(ResourcePlatforms.CurseForge, "version", out _), "Unknown provider accepted");
    }
    private static void Metadata() {
        ResourceTypes[] kinds = { ResourceTypes.Mod, ResourceTypes.ModPack, ResourceTypes.Map, ResourceTypes.DataPack, ResourceTypes.ResourcePack, ResourceTypes.Shader };
        foreach (var kind in kinds) {
            ResourceFavorite entry = Project(type: kind);
            Check(entry.Matches(kind, "OFFLINE") && entry.Matches(null, "modrinth") && !entry.Matches(kind, "missing"), "Category/search metadata lost");
            JObject json = entry.ProjectJson; json["RawName"] = "changed";
            Check(entry.Name == "Fixture project", "Caller mutated stored metadata");
        }
        Check(Project(type: ResourceTypes.ModOrDataPack).Matches(ResourceTypes.DataPack, null), "Multi-category project missing");
        Check(Project(type: ResourceTypes.ModOrDataPack).Identity == Project(type: ResourceTypes.Mod).Identity, "Category split project identity");
        JObject translated = Project().ProjectJson; translated["FavoriteDisplayName"] = "离线中文名称";
        var bilingual = ResourceFavorite.FromMetadata(translated, DateTime.UtcNow);
        Check(bilingual.Matches(null, "中文名称") && bilingual.Matches(null, "Fixture project"), "Displayed and platform names must both remain searchable offline");
    }
    private static void Persistence() {
        using Scope scope = new();
        string legacy = Path.Combine(scope.Root, "Setup.ini"); File.WriteAllText(legacy, "UiModFavorite:Alpha.jar|Beta.jar\r\nKeep:保留\r\n");
        byte[] previous = File.ReadAllBytes(legacy);
        ResourceFavoriteStore store = new(scope.File);
        ResourceFavorite project = Project();
        Check(store.TryToggle(project, out bool saved, out _) && saved, "Add failed");
        var snapshot = store.Snapshot;
        for (int i = 0; i < 5000; i++) Check(store.Contains(project.Identity) && ReferenceEquals(snapshot, store.Snapshot), "Read reparsed/replaced snapshot");
        ResourceFavoriteStore otherInstance = new(scope.File);
        Check(otherInstance.Snapshot.Count == 1 && otherInstance.Contains(project.Identity) && otherInstance.Snapshot[0].Description == project.Description, "Global/offline restore failed");
        var anotherVersion = Project(name: "Same project newer version");
        Check(otherInstance.TryToggle(anotherVersion, out saved, out _) && !saved && otherInstance.Snapshot.Count == 0, "Another file/version created a second favorite");
        Check(store.Reload() && store.Snapshot.Count == 0, "Reload missed external changes");
        Check(File.ReadAllBytes(legacy).SequenceEqual(previous), "Legacy filename favorites/settings were rewritten");
        Check(!Directory.EnumerateFiles(scope.Root, "*.tmp").Any(), "Atomic temporary file leaked");
    }
    private static void Filters() {
        JObject json = Project().ProjectJson;
        json["Drops"] = new JArray(210); json["KnownGameVersions"] = new JArray("1.21.1", "25w10a"); json["ModLoaders"] = (int)(ModLoaders.Fabric | ModLoaders.Forge);
        ResourceFavorite entry = ResourceFavorite.FromMetadata(json, DateTime.UtcNow);
        Check(new ResourceFavoriteFilter { Category = ResourceTypes.Mod, Name = "fixture", Source = ResourcePlatforms.Modrinth, Tag = "fixture", GameVersion = "1.21.1", Loader = ModLoaders.Fabric }.Matches(entry), "Combined cached metadata filter missed a project");
        Check(!new ResourceFavoriteFilter { GameVersion = "1.21.11" }.Matches(entry) && !new ResourceFavoriteFilter { GameVersion = "1.21" }.Matches(entry), "A coarse Drop was treated as exact patch compatibility");
        Check(!new ResourceFavoriteFilter { Source = ResourcePlatforms.CurseForge }.Matches(entry) && !new ResourceFavoriteFilter { Tag = "other" }.Matches(entry), "Source/tag filters were ignored");
        Check(new ResourceFavoriteFilter { GameVersion = "25w10a" }.Matches(entry), "Known snapshots were discarded");
        json.Remove("KnownGameVersions"); json.Remove("ModLoaders");
        var unknown = ResourceFavorite.FromMetadata(json, DateTime.UtcNow);
        Check(new ResourceFavoriteFilter().Matches(unknown) && !new ResourceFavoriteFilter { GameVersion = "1.21.1" }.Matches(unknown) && !new ResourceFavoriteFilter { Loader = ModLoaders.Fabric }.Matches(unknown), "Unknown compatibility was guessed or excluded from unfiltered browsing");
        Check(!ResourceFavoriteFilter.IsGameVersionLabel("AbCd1234") && !ResourceFavoriteFilter.IsGameVersionLabel("12345678") && !ResourceFavoriteFilter.IsGameVersionLabel("1.21 预览版") && ResourceFavoriteFilter.IsGameVersionLabel("26.1"), "Version IDs or a coarse preview label were accepted as exact game versions");
    }
    private static void Corruption() {
        using Scope scope = new(); ResourceFavoriteStore store = new(scope.File); var project = Project();
        Check(store.TryToggle(project, out _, out _), "Seed failed"); var before = store.Snapshot;
        byte[] valid = File.ReadAllBytes(scope.File);
        foreach (string corrupted in new[] { "broken", "{\"Schema\":2,\"Projects\":[]}", "{\"Schema\":1,\"Projects\":[{}]}", "{\"Schema\":1,\"Schema\":1,\"Projects\":[]}" }) {
            File.WriteAllText(scope.File, corrupted); byte[] bytes = File.ReadAllBytes(scope.File);
            Check(!store.Reload() && store.LoadError is not null && ReferenceEquals(before, store.Snapshot), "Corruption cleared the last good cache");
            Check(!store.TryToggle(project, out bool saved, out var error) && saved && error is not null && File.ReadAllBytes(scope.File).SequenceEqual(bytes), "Corruption was silently overwritten");
            var cold = new ResourceFavoriteStore(scope.File);
            Check(cold.Snapshot.Count == 0 && cold.LoadError is not null && !cold.TryToggle(Project("Other123"), out _, out _), "Cold corrupt store became writable");
        }
        File.WriteAllBytes(scope.File, valid);
        Check(store.Reload() && store.LoadError is null && store.Contains(project.Identity), "Repaired store could not reload");
    }
    private static void SaveFailure() {
        using Scope scope = new(); var seed = new ResourceFavoriteStore(scope.File);
        Check(seed.TryToggle(Project(), out _, out _), "Seed failed"); byte[] previous = File.ReadAllBytes(scope.File);
        var store = new ResourceFavoriteStore(scope.File, (_, _) => throw new IOException("synthetic atomic save failure"));
        var snapshot = store.Snapshot; int changes = 0; store.Changed += (_, _) => changes++;
        Check(!store.TryToggle(Project("Other123"), out bool added, out var error) && !added && error is IOException, "Failed add reported success");
        Check(!store.TryToggle(Project(), out bool removed, out _) && removed, "Failed removal cleared state");
        Check(changes == 0 && ReferenceEquals(snapshot, store.Snapshot) && File.ReadAllBytes(scope.File).SequenceEqual(previous), "Failed write published changes");
        // A real filesystem replacement failure must also leave the cache untouched.
        string directory = Path.Combine(scope.Root, "directory.json"); Directory.CreateDirectory(directory);
        var realFailure = new ResourceFavoriteStore(directory);
        Check(!realFailure.TryToggle(Project(), out _, out _) && realFailure.Snapshot.Count == 0, "Real atomic write failure reported success");
    }
    private static void ConcurrentStores() {
        using Scope scope = new();
        var first = new ResourceFavoriteStore(scope.File); var second = new ResourceFavoriteStore(scope.File);
        Parallel.For(0, 24, i => {
            var store = i % 2 == 0 ? first : second;
            Check(store.TryToggle(Project("P" + i), out bool saved, out _) && saved, "Concurrent save failed");
        });
        Check(first.Reload() && first.Snapshot.Count == 24 && new ResourceFavoriteStore(scope.File).Snapshot.Count == 24, "Concurrent projects were lost");
        Parallel.For(0, 24, i => Check(new ResourceFavoriteStore(scope.File).TryToggle(Project("Same1234"), out _, out _), "Concurrent same-project toggle failed"));
        Check(first.Reload() && first.Snapshot.Count == 24, "Same project toggles produced duplicates");
    }
    private static void Limits() {
        using Scope scope = new();
        JArray projects = new(Enumerable.Range(0, ResourceFavoriteStore.MaxCount).Select(i => new JObject { ["SavedAt"] = DateTime.UtcNow, ["Project"] = Project("P" + i).ProjectJson }));
        File.WriteAllText(scope.File, new JObject { ["Schema"] = 1, ["Projects"] = projects }.ToString()); byte[] original = File.ReadAllBytes(scope.File);
        var store = new ResourceFavoriteStore(scope.File);
        Check(store.Snapshot.Count == ResourceFavoriteStore.MaxCount && !store.TryToggle(Project("TooMany1"), out _, out _) && File.ReadAllBytes(scope.File).SequenceEqual(original), "Overflow altered the file");
        Check(store.TryToggle(Project("P1"), out bool saved, out _) && !saved, "Count limit prevented removal");
    }
    private sealed class Scope : IDisposable {
        internal readonly string Root = Path.Combine(Path.GetTempPath(), "PclResourceFavorites-" + Guid.NewGuid().ToString("N"));
        internal string File => Path.Combine(Root, "favorites.json");
        internal Scope() => Directory.CreateDirectory(Root);
        public void Dispose() { if (Path.GetDirectoryName(Root) != Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar) || !System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(Root), "^PclResourceFavorites-[a-f0-9]{32}$")) throw new InvalidOperationException("Unexpected fixture root"); Directory.Delete(Root, true); }
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
