#!/usr/bin/env python3
"""Select existing LF checks from changed paths; unknown dependencies fail closed to full.

No arguments read the GitHub event. Use --path repeatedly for local inspection,
--full for a complete checkpoint. Only fixed group/script names become outputs.
"""
from __future__ import annotations

import argparse
import json
import os
from pathlib import Path
import re
import subprocess
import sys

BRANCH = "migration/csharp-modern-dotnet"
GROUPS = ("world-metadata", "local-data", "personal-options", "keybindings", "quick-play",
          "resource-entry", "resource-paths", "resource-files", "resource-metadata", "world-backups", "environment-history")
# Resources is a fixed feature alias, not every group appended after the first five.
RESOURCE_GROUPS = ("resource-entry", "resource-paths", "resource-files", "resource-metadata")
# Exact existing scripts; workflows keep their original static invocations/options.
SCRIPTS = (
    "Test-Release", "Test-VersionListCancellation", "Test-AuditFixes", "Test-LauncherActivation", "Test-MigratedControls",
    "Test-Toolbox", "Test-ToolboxMemory", "Test-InstanceResources", "Test-MapInstallAndRecycle",
    "Test-BackgroundModes", "Test-ResourceUpdates", "Test-LocalGameFeatures", "Test-GameFeatureRegressions",
    "Test-ModpackUpdate", "Test-ModpackUpdateUx", "Test-ReviewFixes", "Test-EnvironmentHistory",
    "Test-BackupRetention", "Test-RecentWorldCard", "Test-LaunchDependencies", "Test-DependencyActivation",
    "Test-DependencyToggles", "Test-WorldBackups", "Test-WorldBackupGroups", "Test-SaveBackupIntegration",
    "Test-ModDependencies", "Test-ModDependencyOnline", "Test-ModDependencyGraph", "Test-InstanceClone",
    "Test-InstanceCloneGroups", "Test-RecentEnvironmentChanges", "Test-EnvironmentCoverage", "Test-KeybindingGroups",
    "Test-MapDetails", "Test-ResourceEntryPoints", "Test-WorldDetails", "Test-ScreenshotGallery",
    "Test-ToolboxDownload", "Test-ModpackMode", "Test-ModpackFolderScope", "Test-IconCompatibility",
    "Test-InstalledComponentLoading", "Test-ExportVersionIsolation",
)
WORLD_CHECKS = {"Test-WorldDetails", "Test-MapDetails", "Test-InstanceResources", "Test-WorldBackups",
                "Test-WorldBackupGroups", "Test-SaveBackupIntegration", "Test-LocalGameFeatures",
                "Test-GameFeatureRegressions", "Test-RecentWorldCard", "Test-EnvironmentHistory", "Test-BackupRetention"}
LOCAL_CHECKS = {"Test-LocalGameFeatures", "Test-GameFeatureRegressions", "Test-KeybindingGroups",
                "Test-RecentWorldCard", "Test-ModpackUpdate", "Test-ModpackUpdateUx", "Test-EnvironmentHistory",
                "Test-BackupRetention", "Test-WorldBackups", "Test-WorldBackupGroups", "Test-SaveBackupIntegration"}
RESOURCE_CHECKS = {"Test-InstanceResources", "Test-MapInstallAndRecycle", "Test-BackgroundModes",
                   "Test-ResourceUpdates", "Test-MapDetails", "Test-ResourceEntryPoints", "Test-ScreenshotGallery"}
# These UI files serve the resource page and its existing offscreen consumers.
# Exact paths keep unrelated/future platform contracts on the full fallback.
RESOURCE_PAGE_PATHS = {
    "PCLCS/Platform/Windows/ResourcePageContracts.cs",
    "PCLCS/Platform/Windows/ResourcePageController.cs",
    "PCLCS/Platform/Windows/ResourcePageController.Recycle.cs",
    "PCLCS/Platform/Windows/ResourcePageController.Backups.cs",
    "Plain Craft Launcher 2/Pages/PageInstance/PageInstanceResources.xaml.vb",
    "Plain Craft Launcher 2/Pages/PageInstance/PageInstanceResources.Recycle.vb",
    "Plain Craft Launcher 2/Pages/PageInstance/PageInstanceResources.Backups.vb",
    "Plain Craft Launcher 2/Pages/PageInstance/PageInstanceResources.xaml",
}
RESOURCE_IMAGE_PATHS = {
    "PCLCS/Platform/Windows/ResourceThumbnailController.cs",
    "PCLCS/Platform/Windows/InstanceResourceImages.cs",
    "Plain Craft Launcher 2/Pages/PageInstance/MyScreenshotItem.xaml.vb",
    "Plain Craft Launcher 2/Pages/PageInstance/MyScreenshotItem.xaml",
}
RESOURCE_PAGE_CHECKS = RESOURCE_CHECKS | {"Test-SaveBackupIntegration", "Test-InstanceClone"}
# The shared decoder also supplies recent-world images through PageLaunchRight.
RESOURCE_IMAGE_CHECKS = RESOURCE_CHECKS | {"Test-RecentWorldCard"}
# The update facade's hash helper is shared with launch/dependency activation.
RESOURCE_ONLINE_CHECKS = RESOURCE_CHECKS | {"Test-LaunchDependencies", "Test-DependencyActivation", "Test-DependencyToggles"}
# Backup engine/workflow callers: grouped saves, clone guards and pre-upgrade flow.
# Launch recovery/process-stop behavior is exercised directly by Test-WorldBackups.
WORLD_BACKUP_PATHS = {
    "PCLCS/Minecraft/WorldBackupContracts.cs",
    "PCLCS/Minecraft/WorldBackupEngine.cs",
    "PCLCS/Minecraft/WorldBackupEngine.Restore.cs",
    "PCLCS/Minecraft/WorldBackupWorkflow.cs",
    "Plain Craft Launcher 2/Modules/Minecraft/WorldBackups.vb",
}
WORLD_BACKUP_CHECKS = {
    "Test-WorldBackups", "Test-WorldBackupGroups", "Test-BackupRetention", "Test-SaveBackupIntegration",
    "Test-InstanceResources", "Test-MapInstallAndRecycle", "Test-InstanceClone", "Test-InstanceCloneGroups",
    "Test-ModpackUpdate", "Test-ModpackUpdateUx",
}
# History owns these exact sources; future shared contracts still fail closed.
# Its callers include modpack updates, clone migration and launch preparation;
# the retained VB transaction and real instance adapters run in Windows checks.
ENVIRONMENT_HISTORY_PATHS = {
    "PCLCS/Minecraft/EnvironmentHistoryContracts.cs",
    "PCLCS/Minecraft/EnvironmentHistoryEngine.cs",
    "PCLCS/Minecraft/EnvironmentHistoryEngine.Restore.cs",
    "PCLCS/Minecraft/EnvironmentHistoryWorkflow.cs",
    "PCLCS/Minecraft/RecentEnvironmentChanges.cs",
    "Plain Craft Launcher 2/Modules/Minecraft/EnvironmentHistory.vb",
    "Plain Craft Launcher 2/Modules/Minecraft/RecentEnvironmentChanges.vb",
}
ENVIRONMENT_HISTORY_CHECKS = {
    "Test-EnvironmentHistory", "Test-RecentEnvironmentChanges", "Test-EnvironmentCoverage", "Test-BackupRetention",
    "Test-ModpackUpdate", "Test-ModpackUpdateUx", "Test-InstanceClone", "Test-InstanceCloneGroups", "Test-LaunchDependencies",
}
# Store is the shared path/transaction facade consumed by these existing chains.
STORE_CHECKS = RESOURCE_CHECKS | WORLD_CHECKS | LOCAL_CHECKS | {
    "Test-ReviewFixes", "Test-InstanceClone", "Test-InstanceCloneGroups", "Test-RecentEnvironmentChanges",
    "Test-EnvironmentCoverage", "Test-LaunchDependencies", "Test-DependencyActivation", "Test-DependencyToggles",
    "Test-ModDependencies", "Test-ModDependencyOnline", "Test-ModDependencyGraph", "Test-ExportVersionIsolation"}
BULK_CHECKS = {"Test-WorldBackups", "Test-WorldBackupGroups", "Test-SaveBackupIntegration",
               "Test-ModDependencies", "Test-ModDependencyOnline", "Test-ModDependencyGraph", "Test-InstanceClone",
               "Test-InstanceCloneGroups", "Test-RecentEnvironmentChanges", "Test-EnvironmentCoverage",
               "Test-KeybindingGroups", "Test-MapDetails", "Test-ResourceEntryPoints", "Test-WorldDetails", "Test-ScreenshotGallery"}


def choose(paths: list[str], full: bool = False, reason: str = "changed paths") -> dict:
    groups: set[str] = set()
    scripts: set[str] = set()
    oracle = inventory = product = False
    modern_test_change = False
    reasons = [reason]
    unknown = []

    def include(selected, checks, *, io_oracle=False, build=True):
        nonlocal oracle, product
        groups.update(selected)
        scripts.update(checks)
        oracle |= io_oracle
        product |= build

    for name in paths:
        # Product directories are considered before documentation extensions.
        leaf = name.rsplit("/", 1)[-1]
        if name == "docs/migration/source-inventory.baseline.json" or name == "scripts/Inventory-Migration.py":
            inventory = True
        elif name.startswith("tests/PCLLF.Modern.Wpf.Tests/") or name == ".github/workflows/modern-wpf.yml":
            # These sources execute in their own real NET10 WPF workflow and do
            # not reference the VB product or require its unrelated regressions.
            modern_test_change = True
        elif name == "tests/PCLLF.Windows.Tests/ResourceThumbnailControllerChecks.cs":
            include((), {"Test-ScreenshotGallery"})
        elif name == "tests/PCLLF.Windows.Tests/KeybindingControllerChecks.cs":
            include((), {"Test-KeybindingGroups"})
        elif name == "scripts/Select-MigrationChecks.py" or name.startswith(".github/workflows/"):
            full = True
            reasons.append("check routing/workflow changed")
        elif name.startswith("tests/PCLLF.PathOracle/") or name == "scripts/Test-MigrationResourcePaths.ps1":
            include(RESOURCE_GROUPS[1:], (), io_oracle=True, build=False)
        elif name in {"tests/PCLLF.Migration.Tests/Program.cs", "tests/PCLLF.Migration.Tests/PCLLF.Migration.Tests.csproj"}:
            include(GROUPS, (), build=False)
        elif name.startswith("tests/PCLLF.Migration.Tests/"):
            stem = leaf.removesuffix("Tests.cs")
            if stem.startswith("WorldMetadata"):
                include(("world-metadata",), (), build=False)
            elif stem == "LocalGameData":
                include(("local-data",), (), build=False)
            elif stem == "WorldBackupPipeline":
                include(("world-backups",), (), build=False)
            elif stem == "EnvironmentHistoryPipeline":
                include(("environment-history",), (), build=False)
            elif stem.startswith("PersonalOptions"):
                include(("personal-options",), (), build=False)
            elif stem.startswith("InstanceKeybinding"):
                include(("keybindings",), (), build=False)
            elif stem.startswith("QuickPlay"):
                include(("quick-play",), (), build=False)
            elif stem.startswith("InstanceResourceEntry"):
                include(("resource-entry",), (), build=False)
            elif stem.startswith("InstanceResourcePath"):
                include(("resource-paths",), (), build=False)
            elif stem.startswith(("InstanceResourceDetails", "InstanceResourcePackMetadata", "InstanceResourceMetadata")):
                include(("resource-metadata",), (), build=False)
            elif stem in {"ResourceRecyclePipeline", "InstanceResourceOnline"}:
                include(("resource-files",), (), build=False)
            elif stem in {"InstanceResourceKindRules", "InstanceResourceNameRules", "InstanceResourceFileRules", "InstanceResourcePackageRules", "InstanceResourcePipeline"}:
                include(("resource-files",), (), build=False)
            else:
                unknown.append(name)
        elif name.startswith("scripts/Test-") and leaf.removesuffix(".ps1") in SCRIPTS:
            include((), (leaf.removesuffix(".ps1"),))
        elif name in WORLD_BACKUP_PATHS:
            include(("world-backups",), WORLD_BACKUP_CHECKS)
        elif name in ENVIRONMENT_HISTORY_PATHS:
            include(("environment-history",), ENVIRONMENT_HISTORY_CHECKS)
        elif name == "PCLCS/Platform/Windows/KeybindingPageController.cs":
            include(("keybindings",), {"Test-LocalGameFeatures", "Test-GameFeatureRegressions", "Test-KeybindingGroups"})
        elif name in {"PCLCS/Platform/Windows/RecentWorldPageController.cs", "PCLCS/Platform/Windows/RecentWorldQuickPlay.cs"}:
            # These exact controllers own only the recent-world card chain.
            # PageLaunchRight remains a shared account/launch VB page and keeps
            # the conservative full fallback for future edits outside that chain.
            include(("quick-play",), {"Test-LocalGameFeatures", "Test-GameFeatureRegressions", "Test-RecentWorldCard"})
        elif name in RESOURCE_PAGE_PATHS:
            include(RESOURCE_GROUPS, RESOURCE_PAGE_CHECKS)
        elif name in RESOURCE_IMAGE_PATHS:
            include(RESOURCE_GROUPS, RESOURCE_IMAGE_CHECKS)
        elif name.startswith("PCLCS/Minecraft/") and leaf.endswith(".cs"):
            if leaf.startswith("InstanceResourceOnline"):
                include(RESOURCE_GROUPS, RESOURCE_ONLINE_CHECKS)
            elif leaf in {"ResourceRecycleFiles.cs", "MapDownloadFiles.cs"}:
                include(RESOURCE_GROUPS, RESOURCE_CHECKS)
            elif leaf.startswith("WorldMetadata"):
                include(("world-metadata", "quick-play", "resource-entry", "resource-files"), WORLD_CHECKS)
            elif leaf == "LocalGameDataStore.cs":
                include(("local-data", "personal-options", "keybindings", "quick-play", "world-backups", "environment-history"), LOCAL_CHECKS)
            elif leaf.startswith("PersonalOptions"):
                include(("personal-options", "keybindings"), LOCAL_CHECKS)
            elif leaf.startswith("InstanceKeybinding"):
                include(("keybindings",), {"Test-LocalGameFeatures", "Test-GameFeatureRegressions", "Test-KeybindingGroups"})
            elif leaf.startswith("QuickPlay"):
                include(("quick-play",), {"Test-LocalGameFeatures", "Test-GameFeatureRegressions", "Test-RecentWorldCard"})
            elif leaf.startswith(("InstanceResourceDetails", "InstanceResourcePackMetadata", "LegacyPackMetadataNumber")):
                include(("resource-metadata",), {"Test-InstanceResources", "Test-MapDetails"}, io_oracle=True)
            elif leaf.startswith("InstanceResourceEntry"):
                include(RESOURCE_GROUPS, RESOURCE_CHECKS)
            elif leaf.startswith("InstanceResourcePath") or leaf == "LegacyPathCharacters.cs":
                include((*RESOURCE_GROUPS, "quick-play"), STORE_CHECKS, io_oracle=True)
            elif leaf.startswith("InstanceResource"):
                include(RESOURCE_GROUPS, STORE_CHECKS, io_oracle=True)
            else:
                unknown.append(name)
        elif name == "Plain Craft Launcher 2/Modules/Minecraft/InstanceResourceUpdates.vb":
            include(RESOURCE_GROUPS, RESOURCE_ONLINE_CHECKS)
        elif name in {"Plain Craft Launcher 2/Modules/Minecraft/ResourceRecycleStore.vb", "Plain Craft Launcher 2/Modules/Minecraft/MapDownloadInstaller.vb"}:
            include(RESOURCE_GROUPS, RESOURCE_CHECKS)
        elif name == "Plain Craft Launcher 2/Modules/Minecraft/WorldMetadata.vb":
            include(("world-metadata", "quick-play", "resource-entry", "resource-files"), WORLD_CHECKS)
        elif name == "Plain Craft Launcher 2/Modules/Minecraft/LocalGameData.vb":
            include(("local-data", "personal-options", "keybindings", "quick-play", "world-backups", "environment-history"), LOCAL_CHECKS)
        elif name == "Plain Craft Launcher 2/Modules/Minecraft/PersonalGameSettings.vb":
            include(("personal-options", "keybindings"), LOCAL_CHECKS)
        elif name in {"Plain Craft Launcher 2/Modules/Minecraft/InstanceKeybindings.vb",
                       "Plain Craft Launcher 2/Pages/PageInstance/PageInstanceKeybindings.xaml.vb"}:
            include(("keybindings",), {"Test-LocalGameFeatures", "Test-GameFeatureRegressions", "Test-KeybindingGroups"})
        elif name == "Plain Craft Launcher 2/Modules/Minecraft/QuickPlayWorlds.vb":
            include(("quick-play",), {"Test-LocalGameFeatures", "Test-GameFeatureRegressions", "Test-RecentWorldCard"})
        elif name == "Plain Craft Launcher 2/Modules/Minecraft/InstanceResourceStore.vb":
            include((*RESOURCE_GROUPS, "quick-play", "local-data", "personal-options", "keybindings", "world-metadata"), STORE_CHECKS, io_oracle=True)
        elif name.startswith(("PCLCS/", "Plain Craft Launcher 2/", "MeloongCore")) or name == ".gitmodules":
            unknown.append(name)
        elif name.startswith("docs/") or name.lower().endswith(".md") or name in {"LICENCE", "LICENSE", ".gitignore"}:
            pass
        else:
            unknown.append(name)
    if modern_test_change:
        reasons.append("modern WPF test/workflow changes run in modern-wpf.yml")
    if unknown:
        full = True
        reasons.append("unclassified/shared change: " + ", ".join(unknown))
    if full:
        groups, scripts = set(GROUPS), set(SCRIPTS)
        oracle = inventory = product = True
    # Any product build retains credential, legacy-update build/handoff and artifact gates in build.yml.
    selected_scripts = [name for name in SCRIPTS if name in scripts]
    return {"full": full, "product_build": product, "migration_run": bool(groups or oracle or inventory),
            "groups": [name for name in GROUPS if name in groups], "windows_scripts": selected_scripts,
            "world_checks": bool(scripts & BULK_CHECKS), "engine_only": "Test-WorldBackups" in scripts,
            "oracle": oracle, "inventory": inventory or full, "reasons": reasons, "changed_paths": paths}


def event_selection(event_file: str) -> tuple[list[str], bool, str]:
    event = json.loads(Path(event_file).read_text(encoding="utf-8"))
    event_name = os.environ.get("GITHUB_EVENT_NAME", "")
    pr = event.get("pull_request")
    migration = (pr.get("base", {}).get("ref") == BRANCH) if isinstance(pr, dict) else (
        os.environ.get("GITHUB_REF", event.get("ref", "")) == "refs/heads/" + BRANCH)
    if not migration:
        return [], True, "non-migration branch keeps complete product checks"
    if event_name == "workflow_dispatch":
        return [], True, "manual complete checkpoint"
    if isinstance(pr, dict):
        base, head = pr.get("base", {}).get("sha"), pr.get("head", {}).get("sha")
        # PR comparison includes only the proposed delta, not unrelated base commits.
        separator = "..."
    else:
        base, head, separator = event.get("before"), event.get("after"), ".."
    if not all(isinstance(value, str) and re.fullmatch(r"[0-9a-fA-F]{40}", value) and set(value) != {"0"} for value in (base, head)):
        return [], True, "missing/initial diff base: complete checkpoint"
    process = subprocess.run(["git", "diff", "--name-only", "--no-renames", "-z", base + separator + head],
                             stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=False)
    if process.returncode:
        return [], True, "diff unavailable: complete checkpoint"
    return [value.decode("utf-8") for value in process.stdout.split(b"\0") if value], False, "changed paths"


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--path", action="append", default=[], help="Changed Git path; repeat for local inspection")
    parser.add_argument("--event-file", help="GitHub event JSON; defaults to GITHUB_EVENT_PATH")
    parser.add_argument("--full", action="store_true", help="Explicit complete checkpoint")
    parser.add_argument("--github-output", help="Append fixed-name GitHub job outputs")
    parser.add_argument("--report", help="Write selected/unselected scope as JSON")
    args = parser.parse_args()
    if args.path:
        paths, full, reason = args.path, args.full, "explicit changed paths"
    elif args.full:
        paths, full, reason = [], True, "explicit complete checkpoint"
    else:
        try:
            paths, full, reason = event_selection(args.event_file or os.environ["GITHUB_EVENT_PATH"])
        except (KeyError, OSError, ValueError, UnicodeError):
            paths, full, reason = [], True, "event unavailable: complete checkpoint"
    plan = choose(paths, full, reason)
    serialized = json.dumps(plan, ensure_ascii=True, indent=2) + "\n"
    if args.report:
        report = Path(args.report)
        report.parent.mkdir(parents=True, exist_ok=True)
        report.write_text(serialized, encoding="utf-8")
    if args.github_output:
        with Path(args.github_output).open("a", encoding="utf-8", newline="\n") as output:
            for name in ("full", "product_build", "migration_run", "groups", "windows_scripts", "world_checks", "engine_only", "oracle", "inventory"):
                output.write(name + "=" + json.dumps(plan[name], separators=(",", ":")) + "\n")
    print(serialized, end="")
    return 0


if __name__ == "__main__":
    sys.exit(main())
