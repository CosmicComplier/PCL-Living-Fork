using System.IO.Compression;
using System.Text;
using Newtonsoft.Json.Linq;
using PCLCS;

internal static class PackUpdateGraphTests {
    internal static IEnumerable<(string Name, Action Run)> Cases() {
        yield return ("pack update final graph reuses an author dependency for a personal mod", AuthorUpgradeSatisfiesPersonal);
        yield return ("pack update replaces an incompatible personal author override before judging its consumer", AuthorReplacementBeforeDisabling);
        yield return ("pack update pairs a unique personal mod with its new author version and preserves later upgrade choices", PersonalAuthorComponentUpdate);
        yield return ("pack update does not pair ambiguous local or author component candidates", AmbiguousPersonalComponents);
        yield return ("pack update binds a preserved component association to exact observed bytes and protects occupied targets", PersonalComponentAssociationGuards);
        yield return ("pack update paired component replacement rolls back the original personal bytes", PersonalComponentRollback);
        yield return ("pack update keeps unrelated author upgrades and removals out of the conflict category", OrdinaryAuthorActions);
        yield return ("pack update refuses an author version that breaks a retained personal dependency", AuthorUpgradeBreaksPersonal);
        yield return ("pack update retains a compatible personal override then adopts a compatible later author version", PersonalOverrideAcrossUpdates);
        yield return ("pack update unknown metadata requires explicit acknowledgement and keeps logical filenames", UnknownNeedsAcknowledgement);
        yield return ("pack update final graph excludes disabled files without enabling or deleting them", DisabledExcluded);
        yield return ("pack update can retain an author-removed dependency needed by a personal mod", RetainRemovedAuthorDependency);
        yield return ("pack update provider alias ambiguity needs acknowledgement without deleting the personal provider", AliasNeedsAcknowledgement);
        yield return ("pack update final graph detects missing dependencies and explicit conflicts among new author files", AuthorCombinationErrors);
        yield return ("pack update final graph includes legacy Forge version-folder constraints and snapshots", LegacyVersionFolderConstraints);
        yield return ("pack update final graph loads only the target legacy version folder and marks unsupported scopes unknown", LegacyIncomingScope);
    }

    private static void PersonalAuthorComponentUpdate() {
        foreach (bool preserve in new[] { false, true }) {
            using Fixture f = new();
            FileSpec local = f.Spec("mods/local-1.jar", "shared_component", "1.0");
            FileSpec incoming = f.Spec("mods/author-2.jar", "shared_component", "2.0", project: "component");
            f.Install(local);
            Plan plan = f.Plan([], [incoming]); f.Stage(plan);
            Change update = plan.Changes.Single();
            Require(update.PersonalComponentMatched && update.Before == local.Hash && update.LocalRelative == local.Relative && update.BaselineRelative is null &&
                update.Incoming?.Relative == incoming.Relative && update.CanChoose && update.ApplyAuthor && !update.Conflict && update.Operation == PackUpdateOperation.AuthorChange,
                "Unique local/author components were not one previewed upgrade with explicit choices and separate provenance.");
            Require(File.ReadAllBytes(f.Target(local.Relative)).SequenceEqual(local.Bytes) && !File.Exists(f.Target(incoming.Relative)) && !File.Exists(f.ManifestPath),
                "Preparing or cancelling the preview must not change local bytes or create an author record.");
            if (preserve) update.Choice = PackUpdateChoice.PreservePersonal;
            string backup = f.Engine.Commit(plan, f.Game, CancellationToken.None);
            JObject saved = f.Engine.ReadManifest(f.Game)!;
            Require(saved["files"]![incoming.Relative]!["hash"]!.ToString() == incoming.Hash, "Personal bytes replaced the declared new-author hash.");
            if (preserve) {
                JToken observed = saved["files"]![incoming.Relative]!["local"]!;
                Require(observed["ownership"]!.ToString() == "personal-added" && observed["personalComponentMatched"]!.Value<bool>() &&
                    observed["hash"]!.ToString() == local.Hash && File.ReadAllBytes(f.Target(local.Relative)).SequenceEqual(local.Bytes) && !File.Exists(f.Target(incoming.Relative)),
                    "Keeping the local component lost its personal provenance or recorded update association.");
                FileSpec later = f.Spec("mods/author-3.jar", "shared_component", "3.0", project: "component");
                Plan next = f.Engine.Plan(f.Game, saved, f.Package([later])); f.Stage(next);
                Change laterUpdate = next.Changes.Single(change => change.Incoming?.Relative == later.Relative);
                Require(laterUpdate.PersonalComponentMatched && laterUpdate.CanChoose, "Keeping a personal version permanently disabled future author updates.");
                laterUpdate.Choice = PackUpdateChoice.UseAuthor;
                f.Engine.Commit(next, f.Game, CancellationToken.None);
                saved = f.Engine.ReadManifest(f.Game)!;
                Require(File.ReadAllBytes(f.Target(later.Relative)).SequenceEqual(later.Bytes) && !File.Exists(f.Target(local.Relative)) &&
                    saved["files"]![later.Relative]!["local"]!["ownership"]!.ToString() == "author" &&
                    saved["files"]![later.Relative]!["local"]!["personalComponentMatched"] is null,
                    "Adopting the author version lost bytes or retained the personal-only update exception.");
            } else {
                Require(File.ReadAllBytes(f.Target(incoming.Relative)).SequenceEqual(incoming.Bytes) && !File.Exists(f.Target(local.Relative)) &&
                    !File.Exists(f.Target(local.Relative + ".disabled")) && Directory.EnumerateFiles(backup, "*.bak").Any(path => File.ReadAllBytes(path).SequenceEqual(local.Bytes)),
                    "The paired upgrade did not back up the original local file or left two active/disabled version rows.");
            }
        }
    }
    private static void AmbiguousPersonalComponents() {
        foreach (bool duplicateAuthor in new[] { false, true }) {
            using Fixture f = new();
            FileSpec local = f.Spec("mods/local-1.jar", "shared_component", "1.0");
            FileSpec author = f.Spec("mods/author-2.jar", "shared_component", "2.0", project: "component");
            f.Install(local);
            if (!duplicateAuthor) f.Install(local with { Relative = "mods/local-copy.jar" });
            Plan plan = f.Plan([], duplicateAuthor ? [author, author with { Relative = "mods/author-copy.jar" }] : [author]);
            f.Stage(plan);
            Require(!plan.Changes.Any(change => change.PersonalComponentMatched), "A many-to-one or one-to-many component association selected an arbitrary replacement.");
            Require(File.ReadAllBytes(f.Target(local.Relative)).SequenceEqual(local.Bytes) && !File.Exists(f.Target(author.Relative)) && !File.Exists(f.ManifestPath),
                "Unconfirmed component preparation changed the instance.");
        }
    }
    private static void PersonalComponentAssociationGuards() {
        foreach (string kind in new[] { "occupied", "copies", "external", "string-marker" }) {
            using Fixture f = new();
            FileSpec local = f.Spec("mods/local-1.jar", "shared_component", "1.0", new() { ["minecraft"] = "1.21.1" });
            FileSpec author = f.Spec("mods/author-2.jar", "shared_component", "2.0", project: "component");
            FileSpec nextAuthor = f.Spec("mods/author-3.jar", "shared_component", "3.0", project: "component");
            f.Install(local); Plan initial = f.Plan([], [author]); f.Stage(initial);
            initial.Changes.Single().Choice = PackUpdateChoice.PreservePersonal;
            f.Engine.Commit(initial, f.Game, CancellationToken.None);
            JObject saved = f.Engine.ReadManifest(f.Game)!;
            if (kind == "occupied") f.Install(f.Spec(nextAuthor.Relative, "unrelated", "1"));
            if (kind == "copies") f.Install(local with { Relative = "mods/copied-local.jar" });
            if (kind == "external") f.Install(f.Spec(local.Relative, "changed_component", "99", new() { ["minecraft"] = "1.21.1" }));
            if (kind == "string-marker") saved["files"]![author.Relative]!["local"]!["personalComponentMatched"] = "true";
            byte[] before = File.ReadAllBytes(f.Target(local.Relative));
            Plan plan = f.Engine.Plan(f.Game, saved, f.Package([nextAuthor], minecraft: "26.2", loaderVersion: "0.20.1")); f.Stage(plan);
            Change update = plan.Changes.Single(change => change.Incoming?.Relative == nextAuthor.Relative);
            Require(!update.AllowAuthor && !update.ApplyAuthor, "A stale, ambiguous or untyped personal association bypassed its protection: " + kind);
            update.Choice = PackUpdateChoice.UseAuthor;
            Throws<IOException>(() => f.Engine.ValidateChoices(plan));
            Require(File.ReadAllBytes(f.Target(local.Relative)).SequenceEqual(before), "Rejecting a stale association changed the original: " + kind);
        }
    }
    private static void PersonalComponentRollback() {
        using Fixture f = new();
        FileSpec local = f.Spec("mods/local-1.jar", "shared_component", "1.0");
        FileSpec author = f.Spec("mods/author-2.jar", "shared_component", "2.0", project: "component");
        f.Install(local); Plan plan = f.Plan([], [author]); f.Stage(plan);
        Throws<IOException>(() => f.Engine.Commit(plan, f.Game, CancellationToken.None, beforeApply: index => { if (index == 2) throw new IOException("Synthetic manifest-stage failure."); }));
        Require(File.ReadAllBytes(f.Target(local.Relative)).SequenceEqual(local.Bytes) && !File.Exists(f.Target(author.Relative)) && !File.Exists(f.ManifestPath),
            "Failed component replacement did not restore the actual personal bytes and original manifest state.");
    }
    private static void AuthorReplacementBeforeDisabling() {
        using Fixture f = new();
        FileSpec original = f.Spec("mods/library.jar", "author_library", "0.5", project: "library");
        FileSpec local = f.Spec(original.Relative, "author_library", "1.0", project: "library");
        FileSpec incoming = f.Spec("mods/library-2.jar", "author_library", "2.0", project: "library");
        FileSpec extra = f.Spec("mods/personal.jar", "personal_extra", "1.0", new() { ["author_library"] = ">=2" });
        f.Install(local); f.Install(extra);
        Plan plan = f.Plan([original], [incoming]); f.Stage(plan);
        Change replacement = plan.Changes.Single(change => change.Incoming?.Relative == incoming.Relative);
        Change personal = plan.Changes.Single(change => change.LocalRelative == extra.Relative);
        Require(replacement.ApplyAuthor && !replacement.Conflict && personal.PersonalCompatible && personal.Operation == PackUpdateOperation.AuthorChange && !personal.ApplyAuthor,
            "Replacing the provider should repair the personal consumer, not disable it based on an outdated graph.");
        f.Engine.Commit(plan, f.Game, CancellationToken.None);
        Require(File.ReadAllBytes(f.Target(incoming.Relative)).SequenceEqual(incoming.Bytes) && File.ReadAllBytes(f.Target(extra.Relative)).SequenceEqual(extra.Bytes),
            "Compatible consumer or author replacement was lost.");
    }
    private static void OrdinaryAuthorActions() {
        using Fixture f = new();
        FileSpec original = f.Spec("mods/old.jar", "normal", "1", project: "normal");
        FileSpec incoming = f.Spec("mods/new.jar", "normal", "2", project: "normal");
        FileSpec removed = f.Spec("mods/removed.jar", "removed", "1", project: "removed");
        FileSpec brokenAuthor = f.Spec("mods/broken-author.jar", "broken_author", "1", new() { ["missing"] = ">=1" });
        f.Install(original); f.Install(removed);
        Plan plan = f.Plan([original, removed], [incoming, brokenAuthor]); f.Stage(plan);
        Change update = plan.Changes.Single(change => change.Incoming?.Relative == incoming.Relative);
        Change removal = plan.Changes.Single(change => change.BaselineRelative == removed.Relative);
        Require(plan.DependencyCheck!.HasErrors && update.AllowPreserveAuthor && removal.AllowPreserveAuthor &&
            !update.Conflict && !removal.Conflict && update.ApplyAuthor && removal.ApplyAuthor,
            "An unrelated graph error turned ordinary author actions into conflicts or changed their default actions.");
        update.Choice = removal.Choice = PackUpdateChoice.PreservePersonal;
        Require(!update.ApplyAuthor && !removal.ApplyAuthor, "Independent preserve choices stopped affecting execution.");
        Throws<IOException>(() => f.Engine.Commit(plan, f.Game, CancellationToken.None));
    }
    private static void AuthorUpgradeSatisfiesPersonal() {
        using Fixture f = new();
        FileSpec original = f.Spec("mods/library-1.jar", "author_library", "1.0", project: "library");
        FileSpec incoming = f.Spec("mods/library-2.jar", "author_library", "2.0", project: "library");
        f.Install(original);
        f.Install(f.Spec("mods/extra.jar", "personal_extra", "1.0", new() { ["author_library"] = ">=2.0" }));
        Plan plan = f.Plan([original], [incoming]); f.Stage(plan);
        var graph = f.Engine.CheckDependencies(plan);
        Require(!graph.HasErrors && !graph.HasUnknown, "The complete chosen author+personal graph should be known and satisfied.");
        f.Engine.Commit(plan, f.Game, CancellationToken.None);
        Require(File.Exists(f.Target(incoming.Relative)) && !File.Exists(f.Target(original.Relative)) && File.Exists(f.Target("mods/extra.jar")), "The valid upgrade failed to retain the personal mod.");
    }

    private static void AuthorUpgradeBreaksPersonal() {
        using Fixture f = new();
        FileSpec original = f.Spec("mods/library-1.jar", "author_library", "1.0", project: "library");
        FileSpec incoming = f.Spec("mods/library-2.jar", "author_library", "2.0", project: "library");
        f.Install(original);
        f.Install(f.Spec("mods/extra.jar", "personal_extra", "1.0", new() { ["author_library"] = ">=1.0 <2.0" }));
        Plan plan = f.Plan([original], [incoming]); f.Stage(plan);
        Require(plan.DependencyCheck?.HasErrors == false, "The revised author combination was not resolved after handling the incompatible personal addition.");
        Change choice = plan.Changes.Single(item => item.Incoming?.Relative == incoming.Relative);
        Require(!choice.Conflict && choice.ApplyAuthor, "The normal author upgrade was misclassified or rolled back to satisfy a personal mod.");
        Change extra = plan.Changes.Single(item => item.LocalRelative == "mods/extra.jar");
        Require(extra.Operation == PackUpdateOperation.DisablePersonal && extra.ApplyAuthor, "The incompatible personal addition was not prepared for reversible disabling.");
        Require(extra.Reason!.Contains("author_library", StringComparison.Ordinal) && extra.Reason.Contains(">=1.0 <2.0", StringComparison.Ordinal),
            "The personal mod card lost the exact failing dependency and version requirement.");
        f.Engine.Commit(plan, f.Game, CancellationToken.None);
        JObject saved = f.Engine.ReadManifest(f.Game)!;
        Require(saved["files"]![incoming.Relative]!["hash"]!.ToString() == incoming.Hash && File.ReadAllBytes(f.Target(incoming.Relative)).SequenceEqual(incoming.Bytes) &&
            !File.Exists(f.Target("mods/extra.jar")) && File.Exists(f.Target(extra.DestinationRelative)), "Curated author upgrade or reversible personal disabling was not committed correctly.");
        FileSpec later = f.Spec("mods/library-1.5.jar", "author_library", "1.5", project: "library");
        Plan next = f.Engine.Plan(f.Game, saved, f.Package([later])); f.Stage(next);
        next.Changes.Single(item => item.Incoming?.Relative == later.Relative).Choice = PackUpdateChoice.UseAuthor;
        f.Engine.Commit(next, f.Game, CancellationToken.None);
        Require(!File.Exists(f.Target(original.Relative)) && File.ReadAllBytes(f.Target(later.Relative)).SequenceEqual(later.Bytes), "A later author version could not replace the tracked author version.");
    }

    private static void PersonalOverrideAcrossUpdates() {
        using Fixture f = new();
        FileSpec original = f.Spec("mods/library-1.jar", "author_library", "1.0", project: "library");
        FileSpec local = f.Spec(original.Relative, "author_library", "1.5", project: "library");
        FileSpec incoming = f.Spec("mods/library-2.jar", "author_library", "2.0", project: "library");
        f.Install(local);
        f.Install(f.Spec("mods/extra.jar", "personal_extra", "1.0", new() { ["author_library"] = ">=1.4 <2.0" }));
        Plan plan = f.Plan([original], [incoming]); f.Stage(plan);
        Change change = plan.Changes.Single(item => item.Incoming?.Relative == incoming.Relative);
        Require(change.PersonalCompatible && !change.Conflict && !change.ApplyAuthor && !f.Engine.CheckDependencies(plan).HasErrors, "A compatible personal override was not retained by default.");
        change.Choice = PackUpdateChoice.UseAuthor;
        Throws<IOException>(() => f.Engine.ValidateChoices(plan));
        change.Choice = PackUpdateChoice.PreservePersonal;
        f.Engine.Commit(plan, f.Game, CancellationToken.None);
        JObject saved = f.Engine.ReadManifest(f.Game)!;
        Require(saved["files"]![incoming.Relative]!["hash"]!.ToString() == incoming.Hash && saved["files"]![incoming.Relative]!["local"]!["relative"]!.ToString() == original.Relative,
            "Preserving the override rewrote the author's expected bytes or lost the actual local association.");
        FileSpec later = f.Spec("mods/library-1.8.jar", "author_library", "1.8", project: "library");
        Plan next = f.Engine.Plan(f.Game, saved, f.Package([later])); f.Stage(next);
        next.Changes.Single(item => item.Incoming?.Relative == later.Relative).Choice = PackUpdateChoice.UseAuthor;
        Require(!f.Engine.CheckDependencies(next).HasErrors && !f.Engine.CheckDependencies(next).HasUnknown, "A later author version satisfying the personal mod was blocked.");
        f.Engine.Commit(next, f.Game, CancellationToken.None);
        JObject latest = f.Engine.ReadManifest(f.Game)!;
        Require(latest["files"]![later.Relative]!["hash"]!.ToString() == later.Hash && latest["files"]![later.Relative]!["state"]!.ToString() == "installed" &&
            !File.Exists(f.Target(original.Relative)) && File.ReadAllBytes(f.Target(later.Relative)).SequenceEqual(later.Bytes), "The later author upgrade did not clear the old personal override through the normal transaction.");
    }

    private static void UnknownNeedsAcknowledgement() {
        using Fixture f = new();
        FileSpec unknown = f.Raw("mods/unconfirmed.jar", Jar("readme.txt", "metadata intentionally unavailable"));
        Plan plan = f.Plan([], [unknown]); f.Stage(plan);
        Require(plan.DependencyCheck?.HasUnknown == true && !plan.DependencyCheck.HasErrors, "Unknown metadata was called compatible or a definite dependency conflict.");
        Require(plan.DependencyCheck!.Warnings.Any(message => message.Contains(unknown.Relative)), "Temporary payload names hid the logical author filename.");
        Throws<IOException>(() => f.Engine.Commit(plan, f.Game, CancellationToken.None));
        Require(!File.Exists(f.Target(unknown.Relative)), "Unconfirmed dependencies committed without acknowledgement.");
        plan.AllowUnverifiedDependencies = true;
        f.Engine.Commit(plan, f.Game, CancellationToken.None);
        Require(File.ReadAllBytes(f.Target(unknown.Relative)).SequenceEqual(unknown.Bytes), "Explicitly acknowledged unknown content was not preserved byte-for-byte.");
    }

    private static void DisabledExcluded() {
        using Fixture f = new();
        FileSpec original = f.Spec("mods/library-1.jar", "author_library", "1.0", project: "library");
        FileSpec incoming = f.Spec("mods/library-2.jar", "author_library", "2.0", project: "library");
        FileSpec disabled = f.Spec("mods/disabled.jar.disabled", "disabled_extra", "1.0", new() { ["nonexistent_dependency"] = ">=1" });
        f.Install(original); f.Install(disabled);
        Plan plan = f.Plan([original], [incoming]); f.Stage(plan);
        Require(!f.Engine.CheckDependencies(plan).HasErrors && !f.Engine.CheckDependencies(plan).HasUnknown, "Disabled personal content contaminated the enabled final dependency graph.");
        f.Engine.Commit(plan, f.Game, CancellationToken.None);
        Require(File.ReadAllBytes(f.Target(disabled.Relative)).SequenceEqual(disabled.Bytes) && !File.Exists(f.Target("mods/disabled.jar")), "Dependency validation changed a disabled personal file.");
    }

    private static void RetainRemovedAuthorDependency() {
        using Fixture f = new();
        FileSpec original = f.Spec("mods/library.jar", "author_library", "1.0", project: "library");
        FileSpec extra = f.Spec("mods/extra.jar", "personal_extra", "1.0", new() { ["author_library"] = ">=1" });
        f.Install(original); f.Install(extra);
        Plan plan = f.Plan([original], []); f.Stage(plan);
        Change removal = plan.Changes.Single(item => item.BaselineRelative == original.Relative);
        Require(!removal.Conflict && removal.ApplyAuthor && plan.DependencyCheck?.HasErrors == false, "Author removal should stay the default, with its incompatible personal dependent handled separately.");
        Change dependent = plan.Changes.Single(item => item.LocalRelative == extra.Relative);
        Require(dependent.Operation == PackUpdateOperation.DisablePersonal, "Personal dependent was not reversibly disabled.");
        f.Engine.Commit(plan, f.Game, CancellationToken.None);
        JObject saved = f.Engine.ReadManifest(f.Game)!;
        Require(saved["files"]![original.Relative] is null && !File.Exists(f.Target(original.Relative)) && !File.Exists(f.Target(extra.Relative)) && File.Exists(f.Target(dependent.DestinationRelative)),
            "Author removal did not preserve the disabled personal dependency outside author ownership.");
    }

    private static void AliasNeedsAcknowledgement() {
        using Fixture f = new();
        FileSpec personal = f.Spec("mods/personal-alias.jar", "personal_extra", "1.0", provides: new JArray("author_library"));
        FileSpec author = f.Spec("mods/author-library.jar", "author_library", "2.0");
        f.Install(personal);
        Plan plan = f.Plan([], [author]); f.Stage(plan);
        Require(!plan.Changes.Single(change => change.Incoming?.Relative == author.Relative).Conflict,
            "A provider alias was treated as a definite self-ID file collision.");
        var graph = f.Engine.CheckDependencies(plan);
        Require(!graph.HasErrors && graph.HasUnknown, "Loader provider ambiguity lost its distinct unknown classification.");
        Throws<IOException>(() => f.Engine.Commit(plan, f.Game, CancellationToken.None));
        plan.AllowUnverifiedDependencies = true;
        f.Engine.Commit(plan, f.Game, CancellationToken.None);
        Require(File.ReadAllBytes(f.Target(personal.Relative)).SequenceEqual(personal.Bytes) && File.Exists(f.Target(author.Relative)),
            "Acknowledged alias ambiguity deleted or replaced the personal provider.");
    }

    private static void AuthorCombinationErrors() {
        foreach (bool explicitConflict in new[] { false, true }) {
            using Fixture f = new();
            FileSpec library = f.Spec("mods/library.jar", "author_library", "2.0");
            FileSpec broken = f.Spec("mods/broken.jar", "author_client", "1.0", explicitConflict ? null : new() { ["missing_author_dependency"] = ">=1" },
                breaks: explicitConflict ? new() { ["author_library"] = ">=2" } : null);
            Plan plan = f.Plan([], [library, broken]); f.Stage(plan);
            Require(f.Engine.CheckDependencies(plan).HasErrors, "New author entries were assumed compatible without checking their actual declarations.");
            plan.AllowUnverifiedDependencies = true;
            Throws<IOException>(() => f.Engine.Commit(plan, f.Game, CancellationToken.None));
            Require(!File.Exists(f.Target(library.Relative)) && !File.Exists(f.Target(broken.Relative)), "Acknowledging uncertainty bypassed a known dependency/conflict error.");
        }
    }

    private static void LegacyVersionFolderConstraints() {
        using Fixture f = new();
        FileSpec original = f.Legacy("mods/library-1.jar", "author_library", "1", project: "library");
        FileSpec next = f.Legacy("mods/library-2.jar", "author_library", "2", project: "library");
        FileSpec client = f.Legacy("mods/1.12.2/client.jar", "legacy_client", "1", "required-after:author_library@[1,2)");
        f.Install(original); f.Install(client);
        Package before = f.Package([original], "1.12.2", ModLoaders.Forge, "14.23.5.2855");
        Plan plan = f.Engine.Plan(f.Game, PackUpdateEngine<PackFile, Package, Change, Plan, object>.Manifest(before),
            f.Package([next], "1.12.2", ModLoaders.Forge, "14.23.5.2855"));
        f.Stage(plan);
        Require(plan.ObservedModFiles.Contains(client.Relative) && plan.Snapshot.ContainsKey(client.Relative), "The active legacy version-folder mod was omitted from the preview snapshot.");
        Require(plan.DependencyCheck?.HasErrors == false && plan.Changes.Single(change => change.LocalRelative == client.Relative).Operation == PackUpdateOperation.DisablePersonal,
            "Active legacy personal constraints were not resolved by preserving the curated library and reversibly disabling the incompatible client.");
        Require(plan.Changes.Single(change => change.Incoming?.Relative == next.Relative).ApplyAuthor &&
            !f.Engine.CheckDependencies(plan).HasErrors && !f.Engine.CheckDependencies(plan).HasUnknown, "Curated legacy library was unexpectedly rolled back.");
        File.WriteAllBytes(f.Target(client.Relative), f.Legacy(client.Relative, "legacy_client", "2", "required-after:author_library@[1,3)").Bytes);
        Throws<IOException>(() => f.Engine.Commit(plan, f.Game, CancellationToken.None));
        Require(!File.Exists(f.ManifestPath), "A modified legacy version-folder file did not invalidate the old preview.");
    }

    private static void LegacyIncomingScope() {
        using Fixture f = new();
        FileSpec library = f.Legacy("mods/library.jar", "author_library", "1", project: "library");
        FileSpec active = f.Legacy("mods/1.12.2/client.jar", "legacy_client", "1", "required-after:author_library@[1,2)");
        FileSpec inactive = f.Legacy("mods/1.11.2/duplicate.jar", "author_library", "9");
        f.Install(library);
        Package before = f.Package([library], "1.12.2", ModLoaders.Forge, "14.23.5.2855");
        Plan plan = f.Engine.Plan(f.Game, PackUpdateEngine<PackFile, Package, Change, Plan, object>.Manifest(before),
            f.Package([library, active, inactive], "1.12.2", ModLoaders.Forge, "14.23.5.2855"));
        f.Stage(plan);
        Require(!plan.DependencyCheck!.HasErrors && !plan.DependencyCheck.HasUnknown,
            "An inactive MC version-folder file was treated as an enabled duplicate or the active staged client was omitted.");
        f.Engine.Commit(plan, f.Game, CancellationToken.None);
        Require(File.Exists(f.Target(active.Relative)) && File.Exists(f.Target(inactive.Relative)), "Author package content outside the active loader scope was unexpectedly removed.");

        FileSpec custom = f.Legacy("mods/custom/client.jar", "custom_client", "1", "required-after:unavailable@[1,2)");
        JObject saved = f.Engine.ReadManifest(f.Game)!;
        Plan unknown = f.Engine.Plan(f.Game, saved, f.Package([library, active, inactive, custom], "1.12.2", ModLoaders.Forge, "14.23.5.2855"));
        f.Stage(unknown);
        Require(unknown.DependencyCheck!.HasUnknown && unknown.DependencyCheck.Warnings.Any(message => message.Contains(custom.Relative) && message.Contains("加载范围")),
            "An unsupported changed mod subdirectory was silently advertised as a complete checked graph.");
        Throws<IOException>(() => f.Engine.Commit(unknown, f.Game, CancellationToken.None));
    }

    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    private static T Throws<T>(Action action) where T : Exception { try { action(); } catch (T error) { return error; } throw new Exception("Expected " + typeof(T).Name); }
    private static byte[] Jar(string name, string text) {
        using MemoryStream memory = new();
        using (ZipArchive archive = new(memory, ZipArchiveMode.Create, true))
            using (StreamWriter writer = new(archive.CreateEntry(name).Open(), new UTF8Encoding(false))) writer.Write(text);
        return memory.ToArray();
    }
    // A real RuntimeVisibleAnnotations attribute, read by the shared legacy
    // bytecode parser rather than a descriptive mcmod.info shortcut.
    private static byte[] LegacyJar(string id, string version, string? dependencies) {
        var values = new List<(string Name, string Value)> { ("modid", id), ("name", id), ("version", version) };
        if (dependencies is not null) values.Add(("dependencies", dependencies));
        List<string> pool = ["Lnet/minecraftforge/fml/common/Mod;", "RuntimeVisibleAnnotations", "fixture/Mod", "java/lang/Object"];
        foreach (var value in values) { pool.Add(value.Name); pool.Add(value.Value); }
        using MemoryStream stream = new();
        using (BinaryWriter writer = new(stream, Encoding.UTF8, true)) {
            void U2(int value) { writer.Write((byte)((value >> 8) & 255)); writer.Write((byte)(value & 255)); }
            void U4(long value) { U2((int)((value >> 16) & 65535)); U2((int)(value & 65535)); }
            U4(0xcafebabe); U2(0); U2(52); U2(pool.Count + 3);
            foreach (string text in pool) { byte[] bytes = Encoding.UTF8.GetBytes(text); writer.Write((byte)1); U2(bytes.Length); writer.Write(bytes); }
            writer.Write((byte)7); U2(3); writer.Write((byte)7); U2(4); U2(33); U2(pool.Count + 1); U2(pool.Count + 2); U2(0); U2(0); U2(0); U2(1); U2(2); U4(6 + 5 * values.Count); U2(1); U2(1); U2(values.Count);
            for (int i = 0; i < values.Count; i++) { U2(5 + 2 * i); writer.Write((byte)'s'); U2(6 + 2 * i); }
        }
        using MemoryStream zip = new();
        using (ZipArchive archive = new(zip, ZipArchiveMode.Create, true))
            using (Stream output = archive.CreateEntry("fixture/Mod.class").Open()) output.Write(stream.ToArray());
        return zip.ToArray();
    }
    private sealed record FileSpec(string Relative, byte[] Bytes, string Hash, string? Project);
    private sealed class PackFile : PackUpdateFileData { }
    private sealed class Package : PackUpdatePackageData<PackFile> { }
    private sealed class Change : PackUpdateChangeData<PackFile> { }
    private sealed class Plan : PackUpdatePlanData<Package, Change> { }
    private sealed class Fixture : IDisposable {
        private readonly string root = Path.Combine(Path.GetTempPath(), "PclPackGraph-" + Guid.NewGuid().ToString("N"));
        private int index;
        internal readonly string Game;
        internal readonly PackUpdateEngine<PackFile, Package, Change, Plan, object> Engine;
        internal string ManifestPath => Target("PCL/ModpackOrigin.json");
        internal Fixture() {
            Game = Path.Combine(root, "game"); Directory.CreateDirectory(Game);
            Engine = new(new() {
                CreateFile = () => new(), CreatePackage = () => new(), CreateChange = () => new(), CreatePlan = () => new(),
                EnsureWithin = (parent, path) => { if (!Path.GetFullPath(path).StartsWith(Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new IOException("Escaped fixture root."); },
                AtomicWrite = (path, bytes) => { Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllBytes(path, bytes); },
                Download = (_, _) => throw new Exception("The graph fixture must never use network transport."),
                TempFolder = Stage, InstanceGameRoot = _ => Game
            });
        }
        internal string Target(string relative) => Path.Combine(Game, relative.Replace('/', Path.DirectorySeparatorChar));
        private string Stage() { string path = Path.Combine(root, "stage-" + (++index)); Directory.CreateDirectory(path); return path; }
        internal FileSpec Spec(string relative, string id, string version, JObject? depends = null, string? project = null, JObject? breaks = null, JArray? provides = null) {
            JObject requirements = new() { ["minecraft"] = "*", ["fabricloader"] = ">=0.16.0" };
            if (depends is not null) foreach (JProperty property in depends.Properties()) requirements[property.Name] = property.Value.DeepClone();
            JObject metadata = new() { ["schemaVersion"] = 1, ["id"] = id, ["name"] = id, ["version"] = version, ["depends"] = requirements };
            if (breaks is not null) metadata["breaks"] = breaks;
            if (provides is not null) metadata["provides"] = provides;
            return Raw(relative, Jar("fabric.mod.json", metadata.ToString()), project);
        }
        internal FileSpec Raw(string relative, byte[] bytes, string? project = null) {
            string payload = Path.Combine(Stage(), "input.jar"); File.WriteAllBytes(payload, bytes);
            return new(relative, bytes, PackUpdateEngine<PackFile, Package, Change, Plan, object>.Digest(payload)!, project);
        }
        internal FileSpec Legacy(string relative, string id, string version, string? dependencies = null, string? project = null) =>
            Raw(relative, LegacyJar(id, version, dependencies), project);
        internal void Install(FileSpec spec) { string path = Target(spec.Relative); Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllBytes(path, spec.Bytes); }
        internal Package Package(IEnumerable<FileSpec> specs, string minecraft = "1.21.1", ModLoaders loader = ModLoaders.Fabric, string loaderVersion = "0.16.14") {
            Package result = new() { Name = "Full graph fixture", Version = "version-" + (++index), Minecraft = minecraft };
            result.Loaders[loader] = loaderVersion;
            foreach (FileSpec spec in specs) {
                string payload = Path.Combine(Stage(), "payload.jar"); File.WriteAllBytes(payload, spec.Bytes);
                result.Files[spec.Relative] = new() { Relative = spec.Relative, Hash = spec.Hash, Payload = payload, Identity = spec.Project is null ? null : "modrinth:" + spec.Project };
            }
            return result;
        }
        internal Plan Plan(FileSpec[] old, FileSpec[] next) => Engine.Plan(Game, PackUpdateEngine<PackFile, Package, Change, Plan, object>.Manifest(Package(old)), Package(next));
        internal void Stage(Plan plan) => Engine.DownloadChanges(plan, Stage(), CancellationToken.None);
        public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
