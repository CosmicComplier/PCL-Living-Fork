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
        yield return ("pack preview exposes unchanged incompatible author mods and commits explicit disabling", PreviewUnchangedIncompatible);
        yield return ("pack preview asks to enable updated and unchanged disabled required providers", PreviewDisabledRequired);
        yield return ("pack preview snapshots isolate choices and occupied disable targets remain protected", PreviewSnapshotsAndCollisions);
        yield return ("pack preview rechecks a personal mod explicitly kept enabled", PreviewPersonalChoice);
        yield return ("pack preview rejects disabled providers with incompatible required versions", PreviewWrongProvider);
        yield return ("pack preview state switches cannot authorize unknown or forbidden changes", PreviewSwitchAuthorization);
        yield return ("pack preview author disable and enable choices survive repeated updates", PreviewStateRoundTrip);
        yield return ("pack preview leaves multiple local state candidates outside switch authorization", PreviewMultipleStateCandidates);
        yield return ("pack preview preserves explicit author selection for unknown ordinary configuration ownership", PreviewUnknownConfigurationChoice);
        yield return ("pack preview exposes alternative valid providers despite unrelated errors and rejects duplicate selection", PreviewAlternativeProviders);
        yield return ("pack preview explicitly enables personal disabled providers for author and personal consumers", PreviewPersonalRequiredProvider);
        yield return ("pack preview rejects unsafe personal disabled provider candidates", PreviewUnsafePersonalProviders);
        yield return ("pack preview personal enabling rolls back original disabled bytes after cancellation", PreviewPersonalEnableRollback);
        yield return ("pack preview rejects duplicate state-switch operation targets before confirmation", PreviewDuplicateOperationTargets);
        yield return ("pack preview resolves author and personal declared conflicts against the author baseline", PreviewPackFirstConflicts);
        yield return ("pack preview restores author providers hidden by personal overrides without blaming author consumers", PreviewHiddenAuthorProvider);
        yield return ("pack preview independent author runtime errors remain actionable with absent optional files", PreviewOptionalAuthorRuntime);
        yield return ("pack preview restores a modified duplicate and an explicitly retained author version against the new baseline", PreviewPackFirstReplacementChoices);
    }

    private static void PreviewPackFirstReplacementChoices() {
        foreach (bool personalDuplicate in new[] { false, true }) {
            using Fixture f = new();
            FileSpec original = f.Spec("mods/provider.jar", "provider", "1.0", project: "provider");
            FileSpec next = f.Spec(original.Relative, "provider", "2.0", project: "provider");
            FileSpec consumer = f.Spec("mods/consumer.jar", "consumer", "1.0", new() { ["provider"] = ">=2" });
            FileSpec broken = f.Spec("mods/broken.jar", "broken", "1.0", new() { ["minecraft"] = "0.1" });
            f.Install(personalDuplicate ? f.Spec(original.Relative, "consumer", "9.0") : original);
            Plan plan = f.Plan([original], [next, consumer, broken]); f.Stage(plan);
            Change choice = plan.Changes.Single(change => change.Relative == next.Relative);
            Require(choice.AllowPreserveAuthor, "The explicit retained-version choice was unavailable in the conflicted preview.");
            choice.Choice = PackUpdateChoice.PreservePersonal;
            var review = f.Engine.ReviewPreview(plan);
            Require(review.Dependencies.HasErrors && review.Actions.Any(action => action.Relative == next.Relative && action.Kind == PackUpdatePreviewActionKind.UseAuthorVersion && !action.Selected) &&
                !review.Actions.Any(action => action.Relative == consumer.Relative && action.Kind == PackUpdatePreviewActionKind.DisableIncompatible),
                "A personal duplicate or retained old author version blamed a healthy new author consumer.");
            choice.Choice = PackUpdateChoice.UseAuthor; choice.PreviewAuthorRepair = true;
            plan.Changes.Single(change => change.Relative == broken.Relative).DisableAfterUpdate = true;
            var repaired = f.Engine.ReviewPreview(plan);
            Require(!repaired.Dependencies.HasErrors && repaired.Actions.Any(action => action.Relative == next.Relative && action.Kind == PackUpdatePreviewActionKind.UseAuthorVersion && action.Selected),
                "Restoring the author baseline failed or lost the explicit repair undo action.");
        }
    }

    private static void PreviewOptionalAuthorRuntime() {
        using Fixture f = new();
        FileSpec broken = f.Spec("mods/broken-author.jar", "broken_author", "1.0", new() { ["minecraft"] = "0.1" });
        FileSpec optional = f.Spec("mods/optional.jar", "optional_author", "1.0");
        Package package = f.Package([broken, optional]);
        package.Files[optional.Relative].Payload = null; package.Files[optional.Relative].OptionalFile = true;
        Plan plan = f.Engine.Plan(f.Game, PackUpdateEngine<PackFile, Package, Change, Plan, object>.Manifest(f.Package([])), package);
        f.Stage(plan);
        var review = f.Engine.ReviewPreview(plan);
        Require(review.Dependencies.HasErrors && review.Actions.Any(action => action.Relative == broken.Relative && action.Kind == PackUpdatePreviewActionKind.DisableIncompatible && action.Details.Contains("作者新版整合包自身")),
            "An unrelated absent optional file hid an author's independently proven runtime mismatch.");
    }

    private static void PreviewPersonalRequiredProvider() {
        foreach (bool personalConsumer in new[] { false, true }) foreach (string suffix in new[] { ".disabled", ".old" }) {
            using Fixture f = new();
            FileSpec provider = f.Spec("mods/personal-provider.jar", "provider", "2.0");
            FileSpec consumer = f.Spec("mods/consumer.jar", "consumer", "1.0", new() { ["provider"] = ">=2" });
            f.Install(provider with { Relative = provider.Relative + suffix });
            if (personalConsumer) f.Install(consumer);
            Plan plan = f.Plan([], personalConsumer ? [] : [consumer]); f.Stage(plan);
            Change candidate = plan.PreviewChanges.Single(change => change.Operation == PackUpdateOperation.EnablePersonal);
            var review = f.Engine.ReviewPreview(plan);
            Require(f.Requests == 0 && candidate.KeepDisabled && !candidate.ApplyAuthor && review.Dependencies.HasErrors &&
                review.Actions.Any(action => action.Relative == provider.Relative && action.Kind == PackUpdatePreviewActionKind.EnableRequired && !action.Selected),
                "An existing compatible personal disabled provider was bypassed or silently enabled.");
            Require(!plan.Changes.Any(change => change.Operation == PackUpdateOperation.DisablePersonal && change.LocalRelative == consumer.Relative),
                "A personal consumer was disabled before the player could enable its available local provider.");
            Require(!File.Exists(f.Target(provider.Relative)) && File.ReadAllBytes(f.Target(provider.Relative + suffix)).SequenceEqual(provider.Bytes), "Preparing a personal provider changed local bytes.");
            candidate.KeepDisabled = false;
            Require(!f.Engine.ReviewPreview(plan).Dependencies.HasErrors, "Enabling the personal provider did not repair the proposed graph.");
            Require(f.Engine.PlannedOperationCount(plan) == (personalConsumer ? 2 : 3), "Preview-only personal operations were omitted from the actual operation count.");
            f.Engine.Commit(plan, f.Game, CancellationToken.None);
            JObject manifest = f.Engine.ReadManifest(f.Game)!;
            Require(!File.Exists(f.Target(provider.Relative + suffix)) && File.ReadAllBytes(f.Target(provider.Relative)).SequenceEqual(provider.Bytes) &&
                manifest["files"]![provider.Relative] is null && manifest["retained"]![provider.Relative] is null &&
                (personalConsumer || manifest["files"]![consumer.Relative]!["hash"]!.ToString() == consumer.Hash),
                "Personal enabling lost original bytes or incorrectly promoted personal content into an author declaration or retained-author record.");
            Plan subsequent = f.Engine.Plan(f.Game, manifest, f.Package(personalConsumer ? [] : [consumer]));
            Change personalProvider = subsequent.Changes.Single(change => change.LocalRelative == provider.Relative);
            Require(personalProvider.Ownership == PackUpdateOwnership.PersonalAdded && personalProvider.DisplayOnly && !personalProvider.AllowAuthor,
                "The next update failed to recognize the enabled provider as a protected personal addition.");
        }
    }
    private static void PreviewUnsafePersonalProviders() {
        foreach (string reason in new[] { "version", "conflict", "occupied", "multiple", "unknown" }) {
            using Fixture f = new();
            FileSpec provider = reason == "unknown" ? f.Raw("mods/provider.jar", Jar("readme.txt", "unknown")) :
                f.Spec("mods/provider.jar", "provider", reason == "version" ? "1.0" : "2.0", breaks: reason == "conflict" ? new() { ["healthy_author"] = "*" } : null);
            FileSpec consumer = f.Spec("mods/consumer.jar", "consumer", "1.0", new() { ["provider"] = ">=2" });
            FileSpec author = f.Spec("mods/healthy.jar", "healthy_author", "1.0");
            f.Install(provider with { Relative = provider.Relative + ".disabled" });
            if (reason == "occupied") f.Install(f.Spec(provider.Relative, "unrelated", "9.0"));
            if (reason == "multiple") f.Install(f.Spec("mods/another-provider.jar.disabled", "provider", "3.0"));
            Plan plan = f.Plan([], [consumer, author]); f.Stage(plan);
            var review = f.Engine.ReviewPreview(plan);
            Require(review.Dependencies.HasErrors && !review.Actions.Any(action => action.Kind == PackUpdatePreviewActionKind.EnableRequired),
                "An unsafe personal provider was offered for enabling: " + reason);
            Require(File.ReadAllBytes(f.Target(provider.Relative + ".disabled")).SequenceEqual(provider.Bytes), "Rejecting an unsafe provider changed its bytes: " + reason);
            Throws<IOException>(() => f.Engine.Commit(plan, f.Game, CancellationToken.None));
        }
    }
    private static void PreviewPersonalEnableRollback() {
        foreach (int cancelAt in new[] { 2, 3 }) {
            using Fixture f = new();
            FileSpec provider = f.Spec("mods/provider.jar", "provider", "2.0");
            FileSpec consumer = f.Spec("mods/consumer.jar", "consumer", "1.0", new() { ["provider"] = ">=2" });
            f.Install(provider with { Relative = provider.Relative + ".disabled" });
            Plan plan = f.Plan([], [consumer]); f.Stage(plan);
            plan.PreviewChanges.Single(change => change.Operation == PackUpdateOperation.EnablePersonal).KeepDisabled = false;
            using CancellationTokenSource cancellation = new();
            Throws<OperationCanceledException>(() => f.Engine.Commit(plan, f.Game, cancellation.Token, beforeApply: index => { if (index == cancelAt) cancellation.Cancel(); }));
            Require(!File.Exists(f.Target(provider.Relative)) && File.ReadAllBytes(f.Target(provider.Relative + ".disabled")).SequenceEqual(provider.Bytes) &&
                !File.Exists(f.Target(consumer.Relative)) && !File.Exists(f.ManifestPath), "Cancelling a personal enable failed to restore the original disabled files and author manifest.");
        }
    }
    private static void PreviewDuplicateOperationTargets() {
        using Fixture f = new();
        FileSpec active = f.Spec("mods/adapter.jar", "adapter", "1.0", new() { ["minecraft"] = "0.1" });
        FileSpec disabled = f.Spec(active.Relative + ".disabled", "reserve", "1.0");
        Plan plan = f.Plan([], [active, disabled]); f.Stage(plan);
        plan.Changes.Single(change => change.Relative == active.Relative).DisableAfterUpdate = true;
        Require(f.Engine.ReviewPreview(plan).Dependencies.HasErrors, "Two writes to the same disabled target passed the preview.");
        Throws<IOException>(() => f.Engine.ValidateChoices(plan));
        Throws<IOException>(() => f.Engine.Commit(plan, f.Game, CancellationToken.None));
        Require(!File.Exists(f.Target(active.Relative)) && !File.Exists(f.Target(disabled.Relative)), "A duplicate-operation preview changed local bytes.");
    }
    private static void PreviewPackFirstConflicts() {
        foreach (bool authorDeclares in new[] { false, true }) {
            using Fixture f = new();
            FileSpec author = f.Spec("mods/author.jar", "author", "1.0", breaks: authorDeclares ? new() { ["personal"] = "*" } : null);
            FileSpec personal = f.Spec("mods/personal.jar", "personal", "1.0", breaks: authorDeclares ? null : new() { ["author"] = "*" });
            f.Install(personal);
            Plan plan = f.Plan([], [author]); f.Stage(plan);
            Change choice = plan.Changes.Single(change => change.LocalRelative == personal.Relative);
            Require(choice.Operation == PackUpdateOperation.DisablePersonal && plan.Changes.Single(change => change.Relative == author.Relative).ApplyAuthor,
                "Personal conflict resolution did not preserve the new author baseline.");
            choice.Choice = PackUpdateChoice.PreservePersonal;
            var review = f.Engine.ReviewPreview(plan);
            Require(review.Dependencies.HasErrors && review.Actions.Any(action => action.Relative == personal.Relative && action.Kind == PackUpdatePreviewActionKind.DisableIncompatible) &&
                !review.Actions.Any(action => action.Relative == author.Relative && action.Kind == PackUpdatePreviewActionKind.DisableIncompatible),
                "A personal conflict was presented as a reason to disable the healthy author file.");
            Require(f.Engine.PlannedOperationCount(plan) == 1, "Preserving a personal file unexpectedly scheduled a write.");
            choice.Choice = PackUpdateChoice.UseAuthor;
            Require(f.Engine.PlannedOperationCount(plan) == 3, "Disabling personal content was not counted as write plus delete.");
        }
    }
    private static void PreviewHiddenAuthorProvider() {
        using Fixture f = new();
        FileSpec original = f.Spec("mods/provider.jar", "provider", "1.0", project: "provider");
        FileSpec modified = f.Spec(original.Relative, "different_personal_id", "9.0");
        FileSpec next = f.Spec(original.Relative, "provider", "2.0", project: "provider");
        FileSpec consumer = f.Spec("mods/consumer.jar", "consumer", "1.0", new() { ["provider"] = ">=2" });
        f.Install(modified);
        Plan plan = f.Plan([original], [next, consumer]); f.Stage(plan);
        Change choice = plan.Changes.Single(change => change.Relative == next.Relative);
        choice.Choice = PackUpdateChoice.PreservePersonal;
        var review = f.Engine.ReviewPreview(plan);
        Require(review.Dependencies.HasErrors && review.Actions.Any(action => action.Relative == next.Relative && action.Kind == PackUpdatePreviewActionKind.UseAuthorVersion && !action.Selected) &&
            !review.Actions.Any(action => action.Relative == consumer.Relative && action.Kind == PackUpdatePreviewActionKind.DisableIncompatible),
            "A personal override hiding an author ID blamed the author consumer instead of restoring the author provider.");
        choice.Choice = PackUpdateChoice.UseAuthor;
        Require(!f.Engine.ReviewPreview(plan).Dependencies.HasErrors, "Restoring the declared author provider failed to repair its consumer.");
    }

    private static void PreviewAlternativeProviders() {
        using Fixture f = new();
        FileSpec first = f.Spec("mods/provider-a.jar", "provider", "1.0");
        FileSpec second = f.Spec("mods/provider-b.jar", "provider", "2.0");
        FileSpec consumer = f.Spec("mods/consumer.jar", "consumer", "1.0", new() { ["provider"] = ">=1" });
        FileSpec broken = f.Spec("mods/unrelated.jar", "unrelated", "1.0", new() { ["minecraft"] = "0.1" });
        f.Install(first with { Relative = first.Relative + ".disabled" });
        f.Install(second with { Relative = second.Relative + ".disabled" });
        Plan plan = f.Plan([first, second], [first, second, consumer, broken]); f.Stage(plan);
        var review = f.Engine.ReviewPreview(plan);
        Require(review.Dependencies.HasErrors && review.Actions.Count(action => action.Kind == PackUpdatePreviewActionKind.EnableRequired) == 2 &&
            !review.Actions.Any(action => action.Relative == consumer.Relative && action.Kind == PackUpdatePreviewActionKind.DisableIncompatible),
            "Valid alternative providers were hidden by ambiguity or an unrelated existing error.");
        foreach (Change change in plan.PreviewChanges) change.KeepDisabled = false;
        plan.Changes.Single(change => change.Relative == broken.Relative).DisableAfterUpdate = true;
        var duplicateReview = f.Engine.ReviewPreview(plan);
        Require(duplicateReview.Actions.Any(action => action.Relative == first.Relative && action.Kind == PackUpdatePreviewActionKind.DisableIncompatible) &&
            duplicateReview.Actions.Any(action => action.Relative == second.Relative && action.Kind == PackUpdatePreviewActionKind.DisableIncompatible),
            "Duplicate provider identities lost their explicit disabling alternatives.");
        Throws<IOException>(() => f.Engine.Commit(plan, f.Game, CancellationToken.None));
        Require(!File.Exists(f.Target(first.Relative)) && !File.Exists(f.Target(second.Relative)), "Duplicate provider selection committed despite the final graph guard.");
    }

    private static void PreviewUnknownConfigurationChoice() {
        using Fixture f = new();
        FileSpec original = f.Raw("config/author.json", Encoding.UTF8.GetBytes("original"));
        FileSpec incoming = f.Raw(original.Relative, Encoding.UTF8.GetBytes("updated")); f.Install(original);
        JObject baseline = PackUpdateEngine<PackFile, Package, Change, Plan, object>.Manifest(f.Package([original]));
        baseline["files"]![original.Relative]!["local"] = new JObject(new JProperty("relative", original.Relative), new JProperty("ownership", "unknown"));
        Plan plan = f.Engine.Plan(f.Game, baseline, f.Package([incoming])); f.Stage(plan);
        Change choice = plan.Changes.Single();
        Require(choice.Ownership == PackUpdateOwnership.Unknown && choice.CanChoose, "Unknown same-path configuration evidence no longer exposes its explicit author choice.");
        choice.Choice = PackUpdateChoice.UseAuthor;
        Require(choice.ApplyAuthor, "An explicit author configuration choice silently became a no-op.");
        f.Engine.Commit(plan, f.Game, CancellationToken.None);
        Require(File.ReadAllBytes(f.Target(incoming.Relative)).SequenceEqual(incoming.Bytes), "The explicitly selected author configuration was not written.");
    }

    private static void PreviewMultipleStateCandidates() {
        using Fixture f = new();
        FileSpec author = f.Spec("mods/author.jar", "author", "1.0");
        f.Install(author);
        FileSpec other = f.Spec(author.Relative + ".old", "other", "9.0"); f.Install(other);
        Plan plan = f.Plan([author], [author]); f.Stage(plan);
        Require(!plan.PreviewChanges.Any() && !f.Engine.ReviewPreview(plan).Actions.Any(), "An exact author file bypassed ambiguous local enabled/disabled siblings.");
        Require(File.ReadAllBytes(f.Target(author.Relative)).SequenceEqual(author.Bytes) && File.ReadAllBytes(f.Target(other.Relative)).SequenceEqual(other.Bytes), "Preparing an ambiguous preview modified local files.");
    }

    private static void PreviewWrongProvider() {
        using Fixture f = new();
        FileSpec provider = f.Spec("mods/provider.jar", "provider", "1.0");
        FileSpec consumer = f.Spec("mods/consumer.jar", "consumer", "1.0", new() { ["provider"] = ">=2" });
        f.Install(provider with { Relative = provider.Relative + ".disabled" });
        Plan plan = f.Plan([provider], [provider, consumer]); f.Stage(plan);
        var review = f.Engine.ReviewPreview(plan);
        Require(review.Dependencies.HasErrors && !review.Actions.Any(action => action.Relative == provider.Relative && action.Kind == PackUpdatePreviewActionKind.EnableRequired) &&
            review.Actions.Any(action => action.Relative == consumer.Relative && action.Kind == PackUpdatePreviewActionKind.DisableIncompatible),
            "A matching ID with an incompatible version was presented as a repair and hid the consumer decision.");
        plan.PreviewChanges.Single().KeepDisabled = false;
        Throws<IOException>(() => f.Engine.Commit(plan, f.Game, CancellationToken.None));
        Require(File.Exists(f.Target(provider.Relative + ".disabled")) && !File.Exists(f.Target(provider.Relative)), "A failed graph committed a provider switch.");
    }
    private static void PreviewSwitchAuthorization() {
        foreach (bool forbidden in new[] { false, true }) {
            using Fixture f = new();
            FileSpec file = f.Spec("mods/author.jar", "author", "1.0"); f.Install(file);
            Plan plan = f.Plan([file], [file]); f.Stage(plan);
            Change change = plan.PreviewChanges.Single();
            if (forbidden) change.AllowAuthor = false; else change.Ownership = PackUpdateOwnership.Unknown;
            change.DisableAfterUpdate = true;
            Require(!change.ApplyAuthor && !f.Engine.ReviewPreview(plan).Actions.Any(), "An unauthorized switch became a writable author operation.");
            Throws<IOException>(() => f.Engine.Commit(plan, f.Game, CancellationToken.None));
            Require(File.ReadAllBytes(f.Target(file.Relative)).SequenceEqual(file.Bytes) && !File.Exists(f.Target(file.Relative + ".disabled")), "An unauthorized switch changed author bytes.");
        }
    }
    private static void PreviewStateRoundTrip() {
        using Fixture f = new();
        FileSpec provider = f.Spec("mods/provider.jar", "provider", "1.0"); f.Install(provider);
        Plan first = f.Plan([provider], [provider]); f.Stage(first);
        Change disable = first.PreviewChanges.Single();
        disable.DisableAfterUpdate = true; disable.DisableAfterUpdate = false;
        Require(!disable.ApplyAuthor, "Undoing an unchanged author disabling left a write operation.");
        disable.DisableAfterUpdate = true;
        f.Engine.Commit(first, f.Game, CancellationToken.None);
        FileSpec consumer = f.Spec("mods/consumer.jar", "consumer", "1.0", new() { ["provider"] = ">=1" });
        Plan second = f.Engine.Plan(f.Game, f.Engine.ReadManifest(f.Game), f.Package([provider, consumer])); f.Stage(second);
        Change enable = second.PreviewChanges.Single();
        Require(enable.Ownership == PackUpdateOwnership.Disabled && enable.KeepDisabled, "The next update forgot the explicit disabled state.");
        enable.KeepDisabled = false; enable.KeepDisabled = true;
        Require(f.Engine.ReviewPreview(second).Dependencies.HasErrors && !enable.ApplyAuthor, "Undoing enable did not restore the missing dependency.");
        enable.KeepDisabled = false;
        f.Engine.Commit(second, f.Game, CancellationToken.None);
        JObject saved = f.Engine.ReadManifest(f.Game)!;
        Require(File.ReadAllBytes(f.Target(provider.Relative)).SequenceEqual(provider.Bytes) && !File.Exists(f.Target(provider.Relative + ".disabled")) &&
            saved["files"]![provider.Relative]!["hash"]!.ToString() == provider.Hash && saved["files"]![provider.Relative]!["state"]!.ToString() == "installed",
            "Repeated updates lost author bytes, expected hash, or the final enabled state.");
    }

    private static void PreviewUnchangedIncompatible() {
        using Fixture f = new();
        FileSpec oldRenderer = f.Spec("mods/render-old.jar", "renderer", "0.5.11", project: "renderer");
        FileSpec nextRenderer = f.Spec("mods/render-new.jar", "renderer", "0.8.10", project: "renderer");
        FileSpec legacy = f.Spec("mods/legacy-adapter.jar", "legacy_adapter", "1.0", new() { ["minecraft"] = "~1.21.0", ["renderer"] = "0.5.11" }, project: "legacy");
        f.Install(oldRenderer); f.Install(legacy);
        var baseline = PackUpdateEngine<PackFile, Package, Change, Plan, object>.Manifest(f.Package([oldRenderer, legacy]));
        Plan plan = f.Engine.Plan(f.Game, baseline, f.Package([nextRenderer, legacy], minecraft: "26.2")); f.Stage(plan);
        Require(!plan.Changes.Any(change => change.Relative == legacy.Relative), "An unchanged author file became an ordinary update.");
        Change decision = plan.PreviewChanges.Single(change => change.Relative == legacy.Relative);
        var before = f.Engine.ReviewPreview(plan);
        Require(before.Dependencies.HasErrors && before.Actions.Any(action => action.Relative == legacy.Relative && action.Kind == PackUpdatePreviewActionKind.DisableIncompatible && !action.Selected) && !decision.ApplyAuthor,
            "The unchanged incompatible author file was invisible or silently disabled before consent.");
        Require(!before.Actions.Any(action => action.Relative == nextRenderer.Relative && action.Kind == PackUpdatePreviewActionKind.DisableIncompatible) &&
            before.Actions.Count(action => action.Kind == PackUpdatePreviewActionKind.DisableIncompatible) == 1,
            "A consumer's required-version mismatch incorrectly proposed disabling its healthy author provider.");
        decision.DisableAfterUpdate = true;
        Require(!f.Engine.ReviewPreview(plan).Dependencies.HasErrors, "Explicitly disabling the author adapter did not repair the proposed graph.");
        f.Engine.Commit(plan, f.Game, CancellationToken.None);
        JObject saved = f.Engine.ReadManifest(f.Game)!;
        Require(!File.Exists(f.Target(legacy.Relative)) && File.ReadAllBytes(f.Target(legacy.Relative + ".disabled")).SequenceEqual(legacy.Bytes) &&
            saved["files"]![legacy.Relative]!["hash"]!.ToString() == legacy.Hash && saved["files"]![legacy.Relative]!["state"]!.ToString() == "disabled",
            "The confirmed author disabling lost its bytes or declared provenance.");
    }
    private static void PreviewDisabledRequired() {
        foreach (bool updated in new[] { false, true }) {
            using Fixture f = new();
            FileSpec original = f.Spec("mods/provider-old.jar", "provider", "1.0", project: "provider");
            FileSpec incoming = updated ? f.Spec("mods/provider-new.jar", "provider", "2.0", project: "provider") : original;
            FileSpec consumer = f.Spec("mods/consumer.jar", "consumer", "1.0", new() { ["provider"] = updated ? ">=2" : ">=1" });
            f.Install(original with { Relative = original.Relative + ".disabled" });
            Plan plan = f.Plan([original], [incoming, consumer]); f.Stage(plan);
            var before = f.Engine.ReviewPreview(plan);
            Change decision = plan.Changes.Concat(plan.PreviewChanges).Single(change => change.Incoming?.Relative == incoming.Relative);
            Require(before.Dependencies.HasErrors && decision.KeepDisabled && before.Actions.Any(action => action.Relative == incoming.Relative && action.Kind == PackUpdatePreviewActionKind.EnableRequired && !action.Selected),
                "A disabled required provider was not offered as an explicit preview decision.");
            Require(!before.Actions.Any(action => action.Relative == consumer.Relative && action.Kind == PackUpdatePreviewActionKind.DisableIncompatible),
                "A consumer was offered for disabling when its missing requirement can be enabled instead.");
            decision.KeepDisabled = false; decision.Choice = PackUpdateChoice.UseAuthor;
            var after = f.Engine.ReviewPreview(plan);
            Require(!after.Dependencies.HasErrors && after.Actions.Any(action => action.Relative == incoming.Relative && action.Kind == PackUpdatePreviewActionKind.EnableRequired && action.Selected),
                "Enabling the required author provider did not repair the selected combination or lost the undo control.");
            f.Engine.Commit(plan, f.Game, CancellationToken.None);
            Require(!File.Exists(f.Target(original.Relative + ".disabled")) && File.ReadAllBytes(f.Target(incoming.Relative)).SequenceEqual(incoming.Bytes) &&
                f.Engine.ReadManifest(f.Game)!["files"]![incoming.Relative]!["state"]!.ToString() == "installed", "The required provider was not enabled transactionally.");
        }
    }
    private static void PreviewSnapshotsAndCollisions() {
        using Fixture f = new();
        FileSpec legacy = f.Spec("mods/adapter.jar", "adapter", "1.0", new() { ["minecraft"] = "1.21.1" }, project: "adapter");
        f.Install(legacy);
        var baseline = PackUpdateEngine<PackFile, Package, Change, Plan, object>.Manifest(f.Package([legacy]));
        Plan plan = f.Engine.Plan(f.Game, baseline, f.Package([legacy], minecraft: "26.2")); f.Stage(plan);
        Plan snapshot = f.Engine.SnapshotForPreview(plan);
        Change decision = plan.PreviewChanges.Single(); decision.DisableAfterUpdate = true;
        Require(f.Engine.ReviewPreview(snapshot).Dependencies.HasErrors && !f.Engine.ReviewPreview(plan).Dependencies.HasErrors,
            "An in-flight check observed a later UI choice instead of its own snapshot.");
        FileSpec occupied = f.Spec(legacy.Relative + ".disabled", "unrelated", "9"); f.Install(occupied);
        Throws<IOException>(() => f.Engine.ReviewPreview(plan));
        Throws<IOException>(() => f.Engine.Commit(plan, f.Game, CancellationToken.None));
        Require(File.ReadAllBytes(f.Target(legacy.Relative)).SequenceEqual(legacy.Bytes) && File.ReadAllBytes(f.Target(occupied.Relative)).SequenceEqual(occupied.Bytes), "A disable choice overwrote a newly occupied target.");
    }
    private static void PreviewPersonalChoice() {
        using Fixture f = new();
        FileSpec local = f.Spec("mods/personal.jar", "personal", "1", new() { ["minecraft"] = "1.21.1" }); f.Install(local);
        Plan plan = f.Engine.Plan(f.Game, PackUpdateEngine<PackFile, Package, Change, Plan, object>.Manifest(f.Package([])), f.Package([], minecraft: "26.2")); f.Stage(plan);
        Change choice = plan.Changes.Single();
        Require(!f.Engine.ReviewPreview(plan).Dependencies.HasErrors, "The prepared personal disabling was not valid.");
        choice.Choice = PackUpdateChoice.PreservePersonal;
        var review = f.Engine.ReviewPreview(plan);
        Require(review.Dependencies.HasErrors && review.Actions.Any(action => action.Relative == local.Relative && !action.Selected && action.Kind == PackUpdatePreviewActionKind.DisableIncompatible),
            "Keeping an incompatible personal file did not immediately reintroduce its preview decision.");
    }

    private static void PersonalAuthorComponentUpdate() {
        foreach (bool preserve in new[] { false, true }) {
            using Fixture f = new();
            FileSpec local = f.Spec("mods/local-1.jar", "shared_component", "1.0");
            FileSpec incoming = f.Spec("mods/author-2.jar", "shared_component", "2.0", project: "component");
            f.Install(local);
            Plan plan = f.Plan([], [incoming]); f.Stage(plan);
            Change update = plan.Changes.Single();
            Require(!f.Engine.ReviewPreview(plan).Actions.Any(action => action.Kind == PackUpdatePreviewActionKind.UseAuthorVersion),
                "An ordinary default author upgrade was mislabeled as an incompatible personal replacement.");
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
            if (explicitConflict) {
                var review = f.Engine.ReviewPreview(plan);
                Require(review.Actions.Any(action => action.Relative == broken.Relative && action.Kind == PackUpdatePreviewActionKind.DisableIncompatible) &&
                    review.Actions.Any(action => action.Relative == library.Relative && action.Kind == PackUpdatePreviewActionKind.DisableIncompatible),
                    "A declared incompatibility lost either affected file's disabling alternative.");
            }
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
        internal int Requests;
        internal readonly PackUpdateEngine<PackFile, Package, Change, Plan, object> Engine;
        internal string ManifestPath => Target("PCL/ModpackOrigin.json");
        internal Fixture() {
            Game = Path.Combine(root, "game"); Directory.CreateDirectory(Game);
            Engine = new(new() {
                CreateFile = () => new(), CreatePackage = () => new(), CreateChange = () => new(), CreatePlan = () => new(),
                EnsureWithin = (parent, path) => { if (!Path.GetFullPath(path).StartsWith(Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new IOException("Escaped fixture root."); },
                AtomicWrite = (path, bytes) => { Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllBytes(path, bytes); },
                Download = (_, _) => throw new Exception("The graph fixture must never use network transport."),
                Request = (_, _, _) => { Requests++; throw new IOException("The graph fixture is offline."); },
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
