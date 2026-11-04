#!/usr/bin/env python3
"""Check the real migration selector with fixed reviewable scopes and CLI events.

Only Python's standard library and Git are needed. No product is built or run.
The temporary Git graph stays on migration/csharp-modern-dotnet; no real
repository file, index, ref, user profile or Git configuration is written.
"""
from __future__ import annotations

import argparse
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile

BRANCH = "migration/csharp-modern-dotnet"
HERE = Path(__file__).resolve().parent
GOLD = json.loads((HERE / "MigrationSelection.golden.json").read_text(encoding="utf-8"))
FIELDS = tuple(GOLD["provenance"]["scopeFields"])
BOOLEAN_FIELDS = tuple(name for name in FIELDS if name not in {"groups", "windows_scripts"})
# Do not inherit tokens, external Git commands, signing hooks or caller event
# variables. Native executable discovery still uses the caller's normal PATH.
ENV = {name: os.environ[name] for name in ("PATH", "LANG", "LC_ALL", "SYSTEMROOT", "WINDIR") if name in os.environ}
ENV.update(GIT_CONFIG_NOSYSTEM="1", GIT_CONFIG_GLOBAL=os.devnull)


def require(value: bool, reason: str) -> None:
    if not value:
        raise AssertionError(reason)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo-root", type=Path, default=HERE.parents[1], help="Repository containing the real scripts/Select-MigrationChecks.py")
    args = parser.parse_args()
    repo = args.repo_root.resolve()
    selector = repo / "scripts/Select-MigrationChecks.py"
    require(selector.is_file(), "The real production selector is missing.")
    count = 0

    def cli(paths: tuple[str, ...] | list[str] = (), *, extra: tuple[str, ...] | list[str] = (), cwd: Path = repo, env: dict[str, str] | None = None) -> dict:
        command = [sys.executable, str(selector)]
        for path in paths:
            command.extend(("--path", path))
        result = subprocess.run(command + list(extra), cwd=cwd, env=ENV if env is None else env,
                                capture_output=True, text=True, check=True, timeout=30)
        return json.loads(result.stdout)

    def expect(plan: dict, scope: str, native: bool, label: str, paths: list[str] | None = None) -> None:
        nonlocal count
        require(set(plan) == set(FIELDS) | {"native_run", "reasons", "changed_paths"}, label + ": job/report output keys changed")
        for name in (*BOOLEAN_FIELDS, "native_run"):
            require(type(plan[name]) is bool, label + ": non-boolean job flag " + name)
        expected = GOLD["scopes"][scope]
        actual = {name: plan[name] for name in FIELDS}
        require(actual == expected, label + ": changed legacy scope\nexpected=" + json.dumps(expected, ensure_ascii=True, sort_keys=True) + "\nactual=" + json.dumps(actual, ensure_ascii=True, sort_keys=True))
        require(plan["native_run"] is native, label + ": native dependency lost or added to unrelated checks")
        if paths is not None:
            require(plan["changed_paths"] == paths, label + ": input paths were lost or rewritten")
        count += 1
        print("PASS " + label, flush=True)

    # These golds were captured from the actual pre-native Root selector. They
    # retain every old group/script/oracle/credential gate, not a second router.
    for case in GOLD["legacyCases"]:
        expect(cli([case["path"]]), case["scope"], case["native"], "preserved scope: " + case["path"], [case["path"]])
    for path in GOLD["nativeOnlyPaths"]:
        expect(cli([path]), "no-checks", True, "native-only: " + path, [path])
    for path in ("scripts/tests/Test-MigrationSelection.py", "scripts/tests/MigrationSelection.golden.json"):
        expect(cli([path]), "no-checks", False, "routing self-check only: " + path, [path])
    for path in ("scripts/Test-NativeUpdateBridge.ps1", "scripts/native-upgrade/NativeUpdateFixture.cs", "scripts/tests/Test-BridgePayloadIdentity.ps1"):
        expect(cli([path]), "no-checks", True, "real native upgrade acceptance only: " + path, [path])
    mixed = ["modern/Main/FormMain.xaml.cs", "Plain Craft Launcher 2/Modules/Minecraft/WorldBackups.vb"]
    expect(cli(mixed), "world-backups", True, "mixed native and legacy consumers", mixed)
    expect(cli(extra=["--full"]), "complete", True, "explicit complete checkpoint", [])
    # No event is deliberately conservative; it must not be mistaken for a
    # valid, empty git diff (the latter is exercised below).

    with tempfile.TemporaryDirectory(prefix="PclMigrationSelection-") as temporary:
        fixture = Path(temporary)
        event_file = fixture / "event.json"
        expect(cli(extra=["--event-file", str(event_file)]), "complete", True, "missing event fails closed", [])
        hooks = fixture / "empty-hooks"
        hooks.mkdir()
        git_options = ["git", "-c", "init.templateDir=", "-c", "core.hooksPath=" + str(hooks),
                       "-c", "user.name=PCLLF selection fixture", "-c", "user.email=fixture@pcllf.invalid", "-c", "commit.gpgsign=false"]

        def git(*arguments: str) -> str:
            result = subprocess.run(git_options + list(arguments), cwd=fixture, env=ENV,
                                    capture_output=True, text=True, check=True, timeout=30)
            return result.stdout.strip()

        git("init", "--quiet", "--initial-branch=" + BRANCH, ".")
        (fixture / "docs").mkdir()
        (fixture / "docs/fixture.md").write_text("Only Git event fixture data.\n", encoding="utf-8")
        git("add", "docs/fixture.md")
        git("commit", "--quiet", "-m", "selection fixture baseline")
        initial = git("rev-parse", "HEAD")
        native_path = "modern/Main/Fixture.cs"
        (fixture / native_path).parent.mkdir(parents=True)
        (fixture / native_path).write_text("// Git path fixture; never compiled.\n", encoding="utf-8")
        git("add", native_path)
        git("commit", "--quiet", "-m", "selection fixture native input")
        native_head = git("rev-parse", "HEAD")

        def event(data: dict, event_name: str, *, scope: str, native: bool, label: str, paths: list[str]) -> None:
            event_file.write_text(json.dumps(data), encoding="utf-8")
            environment = ENV | {"GITHUB_EVENT_NAME": event_name, "GITHUB_REF": "refs/heads/" + BRANCH}
            plan = cli(extra=["--event-file", str(event_file)], cwd=fixture, env=environment)
            expect(plan, scope, native, label, paths)

        event({"before": initial, "after": native_head}, "push", scope="no-checks", native=True,
              label="actual push git diff selects only native input", paths=[native_path])
        event({"before": native_head, "after": native_head}, "push", scope="no-checks", native=False,
              label="actual empty git diff selects no product checks", paths=[])
        # Create a diverged base commit without switching any branch. Comparing
        # base..head would incorrectly include the base's shared Core change;
        # the real PR's base...head must select only the proposed native path.
        git("read-tree", initial)
        shared_path = "PCLCS/Minecraft/WorldBackupEngine.cs"
        (fixture / shared_path).parent.mkdir(parents=True)
        (fixture / shared_path).write_text("// Diverged base path fixture; never compiled.\n", encoding="utf-8")
        git("add", shared_path)
        diverged_base = git("commit-tree", git("write-tree"), "-p", initial, "-m", "selection fixture diverged base")
        require(git("branch", "--show-current") == BRANCH, "Temporary fixture switched away from the migration branch.")
        event({"pull_request": {"base": {"ref": BRANCH, "sha": diverged_base}, "head": {"sha": native_head}}},
              "pull_request", scope="no-checks", native=True, label="actual PR triple-dot excludes unrelated diverged base changes", paths=[native_path])
        event({}, "workflow_dispatch", scope="complete", native=True, label="manual checkpoint retains both complete graphs", paths=[])
        event({"before": "0" * 40, "after": native_head}, "push", scope="complete", native=True,
              label="initial diff base fails closed", paths=[])
        event({"before": "f" * 40, "after": native_head}, "push", scope="complete", native=True,
              label="unavailable git object fails closed", paths=[])
        event_file.write_text("{not-json", encoding="utf-8")
        expect(cli(extra=["--event-file", str(event_file)], cwd=fixture, env=ENV | {"GITHUB_EVENT_NAME": "push", "GITHUB_REF": "refs/heads/" + BRANCH}),
               "complete", True, "malformed event fails closed", [])
        output = fixture / "github-output"
        report = fixture / "report.json"
        untrusted = "modern/Main/path\nNOT_AN_OUTPUT=true.cs"
        require("\n" in untrusted, "The output escaping fixture must contain a real newline.")
        plan = cli([untrusted], extra=["--github-output", str(output), "--report", str(report)])
        expect(plan, "no-checks", True, "real newline cannot inject a GitHub job output", [untrusted])
        lines = output.read_text(encoding="utf-8").splitlines()
        values = dict(line.split("=", 1) for line in lines)
        require(len(lines) == len(FIELDS) + 1 and set(values) == set(FIELDS) | {"native_run"}, "Untrusted path escaped fixed-name GitHub outputs.")
        require({name: json.loads(value) for name, value in values.items()} == {name: plan[name] for name in (*FIELDS, "native_run")}, "GitHub outputs differ from the actual selected scope.")
        require(json.loads(report.read_text(encoding="utf-8")) == plan, "Saved selection report differs from stdout.")

    print("PASS actual migration selection checks: " + str(count) + "; no product build/run", flush=True)
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (AssertionError, OSError, ValueError, subprocess.SubprocessError) as error:
        print("FAIL migration selection: " + str(error), file=sys.stderr)
        if isinstance(error, subprocess.CalledProcessError):
            print(error.stderr or error.stdout or "", file=sys.stderr)
        raise SystemExit(1)
