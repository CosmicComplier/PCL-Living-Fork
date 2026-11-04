#!/usr/bin/env python3
"""Exercise the actual LF projection task in isolated tool/RID WPF builds."""
import argparse
import hashlib
from pathlib import Path
import shutil
import subprocess
import tempfile


def run(command, cwd):
    result = subprocess.run(command, cwd=cwd, stdout=subprocess.PIPE,
                            stderr=subprocess.STDOUT, text=True,
                            encoding="utf-8", errors="replace")
    if result.returncode:
        raise RuntimeError(result.stdout)
    return result.stdout


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet", default=shutil.which("dotnet") or "dotnet")
    args = parser.parse_args()
    original = Path(__file__).resolve().parents[1]
    with tempfile.TemporaryDirectory(prefix="pcllf-build-events-") as temporary:
        root = Path(temporary) / "BuildEvents"
        for name in ("LfWithEvents.targets", "filter-task/Lf.Build.Tasks.csproj",
                     "filter-task/LfFilterWithEvents.cs"):
            target = root / name
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(original / name, target)
        shutil.copytree(original / "tests" / "Probe", root / "tests" / "Probe")
        shutil.copytree(original / "tests" / "ToolChecks", root / "tests" / "ToolChecks")
        checks = root / "tests" / "ToolChecks" / "ToolChecks.csproj"
        run([args.dotnet, "build", str(checks), "-c", "Release", "-warnaserror", "-v:q"], root)
        print(run([args.dotnet, str(checks.parent / "bin/Release/net10.0/ToolChecks.dll"),
                   str(root / "cases")], root).strip())
        probe = root / "tests" / "Probe"
        run([args.dotnet, "build", str(probe / "Probe.csproj"), "-c", "Release",
             "-p:RuntimeIdentifier=win-x64", "-p:SelfContained=true", "-warnaserror", "-v:m"], root)
        output = probe / "obj/Release/net10.0-windows/win-x64"
        sdk = output / "Pages/Deep/EventRoot.g.cs"
        before = (output / "OriginalSdkGeneratedHashes.txt").read_text().split()
        current = hashlib.sha256(sdk.read_bytes()).hexdigest().lower()
        assert before and all(value.lower() == current for value in before), "SDK .g.cs was changed"
        projected, = (output / "WithEventsProjected").glob("*.EventRoot.g.cs")
        assert projected != sdk and "this.Fire =" in projected.read_text(encoding="utf-8-sig")
        assert "internal Probe.LocalButton Fire;" in sdk.read_text(encoding="utf-8-sig")
        assert "internal Probe.LocalButton Fire;" not in projected.read_text(encoding="utf-8-sig")
        print("PASS nested local-type WPF pass2, win-x64/self-contained/embedded sources, original SDK .g.cs SHA unchanged")


if __name__ == "__main__":
    main()
