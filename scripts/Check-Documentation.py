#!/usr/bin/env python3
"""Check tracked Markdown locally without SDKs, product execution or HTTP."""
from __future__ import annotations

import argparse
import importlib.util
from pathlib import Path
import re
import subprocess
import sys
from urllib.parse import unquote, urlsplit

ROOT = Path(__file__).resolve().parents[1]


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo-root", type=Path, default=ROOT)
    args = parser.parse_args()
    root = args.repo_root.resolve()
    spec = importlib.util.spec_from_file_location("ci_selection", ROOT / "scripts/Select-MigrationChecks.py")
    selector = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(selector)
    tracked = subprocess.run(["git", "ls-files", "--stage", "-z"], cwd=root, stdout=subprocess.PIPE, check=True).stdout
    files, submodules = [], []
    for entry in tracked.split(b"\0"):
        if not entry:
            continue
        metadata, name = entry.decode("utf-8").split("\t", 1)
        if metadata.startswith("160000 "):
            submodules.append(name)
        elif selector.is_documentation_path(name) and name.lower().endswith((".md", ".mdx")) and (root / name).is_file():
            files.append(name)
    errors: list[str] = []
    links = 0

    def check_link(file: str, target: str) -> None:
        nonlocal links
        target = target.strip()
        if target.startswith("<") and ">" in target:
            target = target[1:target.index(">")]
        else:
            target = re.split(r'\s+[\"\']', target, maxsplit=1)[0]
        if not target or target.startswith("#"):
            return
        parsed = urlsplit(target)
        if parsed.scheme or parsed.netloc:
            return
        path = unquote(parsed.path)
        resolved = ((root / path.lstrip("/")) if path.startswith("/") else (root / file).parent / path).resolve()
        try:
            relative = resolved.relative_to(root).as_posix()
        except ValueError:
            errors.append(f"{file}: local link escapes repository: {target}")
            return
        links += 1
        if not resolved.exists() and not any(relative == module or relative.startswith(module + "/") for module in submodules):
            errors.append(f"{file}: missing local link: {target}")

    for name in files:
        try:
            content = (root / name).read_text(encoding="utf-8-sig")
        except UnicodeError:
            errors.append(f"{name}: Markdown must be UTF-8")
            continue
        fence = None
        outside = []
        for line in content.splitlines():
            match = re.match(r"^\s{0,3}(`{3,}|~{3,})(.*)$", line)
            if match:
                marker = match.group(1)
                if fence is None:
                    fence = marker
                elif marker[0] == fence[0] and len(marker) >= len(fence) and not match.group(2).strip():
                    fence = None
                continue
            if fence is None:
                outside.append(line)
        if fence:
            errors.append(f"{name}: unclosed code fence")
        prose = "\n".join(outside)
        # Inline code can mention old paths as test inputs; it is not a link.
        prose = re.sub(r"(`+)(.*?)\1", "", prose)
        for target in re.findall(r"!?\[[^\]\n]*\]\(([^\n]*?)\)", prose):
            check_link(name, target)
        for target in re.findall(r"(?m)^\s{0,3}\[[^\]]+\]:\s*(\S+)", prose):
            check_link(name, target)
        for target in re.findall(r'\b(?:href|src|srcset)\s*=\s*[\"\']([^\"\']+)[\"\']', prose):
            check_link(name, target)
    if errors:
        print("\n".join("FAIL docs: " + error for error in errors), file=sys.stderr)
        return 1
    print(f"PASS documentation: {len(files)} tracked Markdown files, {links} local links, balanced fences; no product build or network.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
