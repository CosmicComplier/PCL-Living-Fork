#!/usr/bin/env python3
"""Validate integration workflow triggers and execute the real fail-closed gates offline."""
from __future__ import annotations

import os
from pathlib import Path
import subprocess
import sys
import yaml

ROOT = Path(__file__).resolve().parents[2]
MIGRATION = "migration/csharp-modern-dotnet"


def main() -> None:
    checks = 0
    for name in ("build.yml", "migration-validation.yml", "modern-wpf.yml", "cnb-checks.yml"):
        workflow = yaml.load((ROOT / ".github/workflows" / name).read_text(encoding="utf-8"), Loader=yaml.BaseLoader)
        events = workflow["on"]
        for event in ("push", "pull_request"):
            assert set(events[event]["branches"]) == {"main", MIGRATION}, (name, event, "missing integration branch")
            assert "paths" not in events[event] and "paths-ignore" not in events[event], (name, event, "required gate can remain pending on filtered paths")
            checks += 1
        assert events["merge_group"]["types"] == ["checks_requested"] and events["merge_group"]["branches"] == ["main"], name
        assert "workflow_dispatch" in events and "pull_request_target" not in events, name
        for job in workflow["jobs"].values():
            condition = job.get("if", "")
            assert not (MIGRATION in condition and "main" not in condition.replace(MIGRATION, "")), (name, "migration-only job condition")
        checks += 2
    reusable = yaml.load((ROOT / ".github/workflows/native-main.yml").read_text(encoding="utf-8"), Loader=yaml.BaseLoader)
    assert "workflow_call" in reusable["on"] and "workflow_dispatch" in reusable["on"]
    assert "if" not in reusable["jobs"]["select"], "Reusable entry still skips main or manual feature-branch callers"
    assert all("--allow-missing-symlink-privilege" not in step.get("run", "") for step in reusable["jobs"]["select"]["steps"]), "CI must not defer symbolic-link security coverage"
    assert set(reusable["jobs"]["native"]["strategy"]["matrix"]["configuration"]) == {"Debug", "Release"}
    assert set(reusable["jobs"]["gate"]["needs"]) == set(reusable["jobs"]) - {"gate"}, "Native gate omits an independent validation job"
    facade = next(step for step in reusable["jobs"]["native"]["steps"] if step.get("name", "").startswith("Run actual Main facade feature groups"))
    assert "--list-groups" in facade["run"] and "'registry-data'" in facade["run"] and "$groups = @('map-details'" not in facade["run"], "CI feature coverage must use the real host registry"
    assert any("--groups registry-data" in step.get("run", "") for step in reusable["jobs"]["native"]["steps"]), "Dedicated Registry group disappeared"
    checks += 3
    for workflow_name, gate_name, selection, validation in (
        ("native-main.yml", "gate", "select", "native"),
        ("migration-validation.yml", "gate", "select", "validate"),
        ("modern-wpf.yml", "gate", None, "modern-wpf"),
    ):
        workflow = yaml.load((ROOT / ".github/workflows" / workflow_name).read_text(encoding="utf-8"), Loader=yaml.BaseLoader)
        gate = workflow["jobs"][gate_name]
        assert gate["if"] in ("always()", "${{ always() }}"), workflow_name
        validations = {validation, "native-upgrade"} if workflow_name == "native-main.yml" else {validation}
        assert set(gate["needs"]) == (validations | {selection} if selection else validations), workflow_name
        steps = [step for step in gate["steps"] if step.get("run") == "python scripts/Check-CiGate.py"]
        assert len(steps) == len(validations), workflow_name
        assert {step["env"]["VALIDATION_RESULT"] for step in steps} == {"${{ needs." + job + ".result }}" for job in validations}, workflow_name
        if selection:
            assert all(step["env"]["SELECTION_RESULT"] == "${{ needs.select.result }}" for step in steps), workflow_name
        checks += 1
    for selection, selected, validation, passed in (
        ("success", "true", "success", True),
        ("success", "true", "failure", False),
        ("success", "true", "cancelled", False),
        ("success", "true", "skipped", False),
        ("success", "false", "skipped", True),
        ("success", "false", "failure", False),
        ("success", "false", "success", False),
        ("failure", "", "skipped", False),
        ("cancelled", "true", "success", False),
        ("skipped", "false", "skipped", False),
        ("success", "", "skipped", False),
        ("success", "unexpected", "success", False),
    ):
        environment = {key: value for key, value in os.environ.items() if key not in {"SELECTION_RESULT", "VALIDATION_SELECTED", "VALIDATION_RESULT"}}
        environment.update(SELECTION_RESULT=selection, VALIDATION_SELECTED=selected, VALIDATION_RESULT=validation)
        result = subprocess.run([sys.executable, str(ROOT / "scripts/Check-CiGate.py")], env=environment, capture_output=True, text=True, timeout=10)
        assert (result.returncode == 0) is passed, (selection, selected, validation, result.stdout, result.stderr)
        checks += 1
    native_steps = [step for step in reusable["jobs"]["gate"]["steps"] if step.get("run") == "python scripts/Check-CiGate.py"]
    for build_result, upgrade_result, selected, passed in (
        ("success", "success", "true", True),
        ("success", "failure", "true", False),
        ("success", "cancelled", "true", False),
        ("success", "skipped", "true", False),
        ("failure", "success", "true", False),
        ("skipped", "skipped", "false", True),
    ):
        bindings = {"${{ needs.select.result }}": "success", "${{ needs.select.outputs.native_run }}": selected,
                    "${{ needs.native.result }}": build_result, "${{ needs.native-upgrade.result }}": upgrade_result}
        results = []
        for step in native_steps:
            environment = os.environ | {name: bindings[value] for name, value in step["env"].items()}
            result = subprocess.run([sys.executable, str(ROOT / step["run"].split()[1])], env=environment, capture_output=True, text=True, timeout=10)
            results.append(result.returncode == 0)
        assert all(results) is passed, (build_result, upgrade_result, selected, results)
        checks += 1
    print(f"PASS {checks} integration CI trigger, matrix and actual fail-closed result checks; no network, publication or product execution.")


if __name__ == "__main__":
    main()
