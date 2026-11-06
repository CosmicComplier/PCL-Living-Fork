using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace PCLCS;

public sealed partial class PackUpdateEngine<TFile, TPackage, TChange, TPlan, TNetFile>
    where TFile : PackUpdateFileData where TPackage : PackUpdatePackageData<TFile>
    where TChange : PackUpdateChangeData<TFile> where TPlan : PackUpdatePlanData<TPackage, TChange> {
    public static bool RequiresBaselineRepair(JObject? baseline) => baseline is null ||
        baseline["schema"]?.Type != JTokenType.Integer || baseline["schema"]!.ToString() != "2" ||
        baseline["complete"]?.Type != JTokenType.Boolean || baseline["complete"]!.Value<bool>() != true ||
        baseline["baselineNeedsRepair"] is JToken repair && repair.Type != JTokenType.Null &&
            (repair.Type != JTokenType.Boolean || repair.Value<bool>());

    // Author declarations remain immutable expected bytes. Local observations are
    // useful to explain optional/disabled/preserved states, never ownership proof.
    private void ObserveInstalledFile(string root, string relative, JObject entry, CancellationToken token = default) {
        token.ThrowIfCancellationRequested();
        string fileName = Target(root, relative);
        if (File.Exists(fileName)) {
            entry["state"] = Matches(fileName, entry["hash"]?.ToString(), token) ? "installed" : "modified";
            entry["local"] = LocalObservation(relative, Digest(fileName, token: token));
            return;
        }
        string[] disabled = new[] { relative + ".disabled", relative + ".old" }
            .Where(candidate => File.Exists(Target(root, candidate))).ToArray();
        if (disabled.Length == 1) {
            entry["state"] = "disabled";
            entry["local"] = LocalObservation(disabled[0], Digest(Target(root, disabled[0]), token: token));
        } else if (disabled.Length > 1) {
            entry["state"] = "ambiguous";
            entry["localCandidates"] = new JArray(disabled);
        } else {
            entry["state"] = entry["optional"]?.Value<bool>() == true ? "optional-absent" : "missing";
        }
    }

    private static JObject LocalObservation(string relative, string? hash, string ownership = "author") => new(
        new JProperty("relative", relative), new JProperty("hash", hash), new JProperty("ownership", ownership));

    private static string LocalOwnership(TChange change) => change.Ownership switch {
        PackUpdateOwnership.PersonalAdded => "personal-added",
        PackUpdateOwnership.Ambiguous => "ambiguous",
        _ => "author"
    };

    // Called before the transaction starts: describe the declared next package and
    // the planned outcome separately, without claiming preserved bytes were installed.
    public JObject BuildNextManifest(TPlan plan, CancellationToken token = default) {
        token.ThrowIfCancellationRequested();
        JObject next = Manifest(plan.Package!);
        JObject files = (JObject)next["files"]!;
        foreach (JProperty entry in files.Properties()) {
            token.ThrowIfCancellationRequested();
            JObject author = (JObject)entry.Value;
            TChange? change = plan.Changes.FirstOrDefault(item => item.Incoming is not null &&
                string.Equals(item.Incoming.Relative, entry.Name, StringComparison.OrdinalIgnoreCase));
            if (change is not null && change.ApplyAuthor && change.Operation != PackUpdateOperation.DisablePersonal) {
                author["state"] = change.Ownership == PackUpdateOwnership.Disabled && change.KeepDisabled ? "disabled" : "installed";
                author["local"] = LocalObservation(change.DestinationRelative!, author["hash"]!.ToString());
            } else if (change is not null) {
                string? local = change.Operation == PackUpdateOperation.DisablePersonal ? change.DestinationRelative : change.LocalRelative;
                if (local is not null && change.Before is not null) {
                    author["state"] = local.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase) || local.EndsWith(".old", StringComparison.OrdinalIgnoreCase) ? "disabled" :
                        change.Ownership == PackUpdateOwnership.AuthorRenamed && !change.Conflict && !change.AllowPreserveAuthor ? "renamed" : "retained";
                    author["local"] = LocalObservation(local, change.Before, LocalOwnership(change));
                } else {
                    ObserveInstalledFile(plan.Root!, entry.Name, author, token);
                    if (author["local"] is JObject observation) observation["ownership"] = LocalOwnership(change);
                }
                PreserveOriginalAssociation(author, change, plan.Baseline);
            } else {
                ObserveInstalledFile(plan.Root!, entry.Name, author, token);
                // An unchanged package can leave an exact renamed author file in
                // place. Preserve the verified path observation across that update.
                JObject? previous = plan.Baseline?["files"]?[entry.Name] as JObject;
                string? local = previous?["local"]?["relative"]?.ToString();
                if (author["local"] is null && local is not null && File.Exists(Target(plan.Root!, local))) {
                    author["local"] = LocalObservation(local, Digest(Target(plan.Root!, local), token: token), previous?["local"]?["ownership"]?.ToString() ?? "author");
                    author["state"] = local.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase) || local.EndsWith(".old", StringComparison.OrdinalIgnoreCase) ? "disabled" :
                        Matches(Target(plan.Root!, local), author["hash"]?.ToString(), token) ? "renamed" : "retained";
                }
                // A path becoming a new author target is not permission to adopt a
                // player file. Preserve that provenance even when bytes happen to match.
                if (author["local"] is JObject observed && previous?["local"] is JObject prior &&
                    string.Equals(observed["relative"]?.ToString(), prior["relative"]?.ToString(), StringComparison.OrdinalIgnoreCase) &&
                    prior["ownership"]?.ToString() is "personal-added" or "ambiguous")
                    observed["ownership"] = prior["ownership"]!.DeepClone();
            }
        }

        JObject retained = new();
        foreach (TChange disabled in plan.Changes.Where(change => change.Operation == PackUpdateOperation.DisablePersonal &&
            change.ApplyAuthor && change.BaselineRelative is not null && change.Before is not null)) {
            JToken? author = plan.Baseline?["files"]?[disabled.BaselineRelative!];
            retained[disabled.DestinationRelative] = new JObject(new JProperty("authorRelative", disabled.BaselineRelative), new JProperty("hash", author?["hash"]),
                new JProperty("identity", author?["identity"]), new JProperty("fileIdentity", author?["fileIdentity"]),
                new JProperty("observedHash", disabled.Before), new JProperty("state", "disabled"), new JProperty("ownership", "personal-retained"));
        }
        // Previously retained author files are personal state at this point. They
        // remain outside files, and are dropped once deleted or intentionally replaced.
        if (plan.Baseline?["retained"] is JObject previousRetained) {
            foreach (JProperty entry in previousRetained.Properties()) {
                token.ThrowIfCancellationRequested();
                string path = RelativePath(entry.Name);
                if (File.Exists(Target(plan.Root!, path)) && !WillReplaceOrRemove(plan, path) &&
                    !files.Properties().Any(author => string.Equals(author.Value["local"]?["relative"]?.ToString(), path, StringComparison.OrdinalIgnoreCase))) {
                    retained[path] = entry.Value.DeepClone();
                    retained[path]!["observedHash"] = Digest(Target(plan.Root!, path), token: token);
                    retained[path]!["ownership"] = "personal-retained";
                }
            }
        }
        foreach (TChange change in plan.Changes.Where(item => !item.ApplyAuthor && item.BaselineRelative is not null && item.LocalRelative is not null && item.Before is not null)) {
            token.ThrowIfCancellationRequested();
            string path = change.LocalRelative!;
            if (files.Properties().Any(entry => string.Equals(entry.Value["local"]?["relative"]?.ToString(), path, StringComparison.OrdinalIgnoreCase))) continue;
            if (WillReplaceOrRemove(plan, path)) continue;
            JToken? previous = plan.Baseline?["files"]?[change.BaselineRelative!];
            retained[path] = new JObject(new JProperty("authorRelative", change.BaselineRelative), new JProperty("hash", previous?["hash"]),
                new JProperty("identity", previous?["identity"]), new JProperty("fileIdentity", previous?["fileIdentity"]),
                new JProperty("observedHash", change.Before), new JProperty("state", "retained"), new JProperty("ownership", "personal-retained"));
        }
        next["retained"] = retained;
        return next;
    }

    private static bool WillReplaceOrRemove(TPlan plan, string relative) => plan.Changes.Any(change => change.ApplyAuthor &&
        (string.Equals(change.LocalRelative, relative, StringComparison.OrdinalIgnoreCase) || string.Equals(change.DestinationRelative, relative, StringComparison.OrdinalIgnoreCase)));

    private static void PreserveOriginalAssociation(JObject entry, TChange change, JObject? baseline) {
        if (change.BaselineRelative is null) return;
        entry["baselineRelative"] = change.BaselineRelative;
        JToken? previous = baseline?["files"]?[change.BaselineRelative];
        if (previous?["hash"] is not null) entry["originalHash"] = previous["hash"]!.DeepClone();
    }
}
