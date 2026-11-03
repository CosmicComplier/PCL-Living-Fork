#!/usr/bin/env python3
"""Inventory tracked VB/C#/XAML from a commit without inspecting the worktree.

Project membership is a source declaration, not an evaluated MSBuild result.
Only the standard library and Git are required; submodule contents are not read.
"""

import argparse
from collections import Counter, defaultdict
import fnmatch
from functools import lru_cache
import io
import json
from pathlib import Path, PurePosixPath
import posixpath
import subprocess
import sys
import xml.etree.ElementTree as ET


LF_BASELINE = "f4aa0fae62c3ee8af24c81e61844c8fdd13c8983"
PCL_BASELINE = "5ef81e490ae46b1100d61e33d0183094c7a80ca8"
SOURCE_TYPES = {".vb": "VB", ".cs": "C#", ".xaml": "XAML"}
PROJECT_TYPES = {".vbproj": ".vb", ".csproj": ".cs"}
SOURCE_ITEMS = {"Compile", "Page", "ApplicationDefinition", "Resource", "None", "Content"}
REPO = Path(__file__).resolve().parents[1]


def git(*args, input_data=None):
    result = subprocess.run(
        ["git", "-C", str(REPO), *args], input=input_data, capture_output=True, check=False
    )
    if result.returncode:
        raise ValueError(result.stderr.decode("utf-8", errors="replace").strip())
    return result.stdout


def resolve_commit(ref):
    return git("rev-parse", "--verify", "--end-of-options", ref + "^{commit}").decode().strip()


def read_tree(commit):
    blobs = {}
    submodules = []
    for record in git("ls-tree", "-r", "-z", "--full-tree", commit).split(b"\0"):
        if not record:
            continue
        header, raw_path = record.split(b"\t", 1)
        mode, kind, oid = header.decode("ascii").split()
        path = raw_path.decode("utf-8")
        if kind == "blob":
            blobs[path] = {"blob": oid, "mode": mode}
        elif kind == "commit":
            submodules.append({"path": path, "commit": oid})
    return blobs, sorted(submodules, key=lambda entry: entry["path"])


def read_blobs(oids):
    ordered = sorted(set(oids))
    response = io.BytesIO(git("cat-file", "--batch", input_data=("\n".join(ordered) + "\n").encode()))
    blobs = {}
    for expected in ordered:
        oid, kind, size = response.readline().decode("ascii").split()
        if oid != expected or kind != "blob":
            raise ValueError("Unexpected Git object while reading source blobs")
        data = response.read(int(size))
        if len(data) != int(size) or response.read(1) != b"\n":
            raise ValueError("Incomplete Git blob response")
        blobs[oid] = data
    return blobs


def local_name(tag):
    return tag.rsplit("}", 1)[-1]


def conditions(element, parents):
    result = []
    while element is not None:
        if element.get("Condition"):
            result.append(element.get("Condition"))
        element = parents.get(element)
    return list(reversed(result))


def normalize_path(project_path, value):
    return posixpath.normpath(posixpath.join(posixpath.dirname(project_path), value.replace("\\", "/")))


def matches(path, pattern):
    """Match common MSBuild globs by path segment, including recursive **."""
    parts = tuple(path.casefold().split("/"))
    segments = tuple(pattern.casefold().split("/"))

    @lru_cache(maxsize=None)
    def visit(i, j):
        if j == len(segments):
            return i == len(parts)
        if segments[j] == "**":
            return visit(i, j + 1) or (i < len(parts) and visit(i + 1, j))
        return i < len(parts) and fnmatch.fnmatchcase(parts[i], segments[j]) and visit(i + 1, j + 1)

    return visit(0, 0)


def group_for(path):
    parts = PurePosixPath(path).parts
    if parts[0] == "Plain Craft Launcher 2":
        if len(parts) > 2 and parts[1] in {"Modules", "Pages"}:
            return "/".join(parts[1:3]) if len(parts) > 3 else parts[1]
        return parts[1] if len(parts) > 2 else "Application"
    if parts[0] == "PCLCS":
        return "/".join(parts[:2]) if len(parts) > 2 else "PCLCS"
    return "/".join(parts[:-1]) or "Root"


def source_role(path):
    if path.startswith(("Plain Craft Launcher 2/", "PCLCS/")):
        return "product"
    if path.startswith("tests/"):
        return "test"
    if path.startswith("scripts/"):
        return "tool"
    return "other"


def read_projects(tree, sources, blobs):
    projects = []
    membership = defaultdict(list)
    for path in sorted(p for p in tree if PurePosixPath(p).suffix.lower() in PROJECT_TYPES):
        root = ET.fromstring(blobs[tree[path]["blob"]])
        parents = {child: parent for parent in root.iter() for child in parent}
        sdk = bool(root.get("Sdk")) or any(local_name(e.tag) == "Sdk" for e in root)
        properties = defaultdict(list)
        for element in root.iter():
            if local_name(element.tag) in {
                "TargetFramework", "TargetFrameworks", "TargetFrameworkVersion", "UseWPF",
                "EnableDefaultItems", "EnableDefaultCompileItems", "EnableDefaultPageItems",
                "EnableDefaultApplicationDefinition",
            }:
                properties[local_name(element.tag)].append({
                    "value": (element.text or "").strip(), "conditions": conditions(element, parents)
                })
        project = {
            "path": path, "sdk_style": sdk, "properties": dict(sorted(properties.items())),
            "project_references": [], "unresolved_items": [],
        }

        def enabled(name, default=True):
            values = [entry["value"].lower() for entry in properties[name] if not entry["conditions"]]
            return values[-1] != "false" if values else default

        if sdk and enabled("EnableDefaultItems"):
            directory = posixpath.dirname(path)
            extension = PROJECT_TYPES[PurePosixPath(path).suffix.lower()]
            for source in sources:
                if directory and not source.startswith(directory + "/"):
                    continue
                relative = posixpath.relpath(source, directory or ".")
                if any(part.casefold() in {"bin", "obj"} for part in PurePosixPath(relative).parts):
                    continue
                item = None
                if PurePosixPath(source).suffix.lower() == extension and enabled("EnableDefaultCompileItems"):
                    item = "Compile"
                if PurePosixPath(source).suffix.lower() == ".xaml" and any(
                    entry["value"].lower() == "true" for entry in properties["UseWPF"]
                ):
                    if relative.casefold() in {"app.xaml", "application.xaml"}:
                        if enabled("EnableDefaultApplicationDefinition"):
                            item = "ApplicationDefinition"
                    elif enabled("EnableDefaultPageItems"):
                        item = "Page"
                if item:
                    membership[source].append({
                        "project": path, "item": item, "declaration": "sdk_default_inferred", "conditions": []
                    })
        for element in root.iter():
            item = local_name(element.tag)
            if item == "ProjectReference" and element.get("Include"):
                project["project_references"].append({
                    "path": normalize_path(path, element.get("Include")), "conditions": conditions(element, parents)
                })
            if item not in SOURCE_ITEMS:
                continue
            for operation in ("Include", "Remove", "Update"):
                value = element.get(operation)
                if not value:
                    continue
                expressions = [value, element.get("Exclude", "")]
                if any(token in expression for expression in expressions for token in ("$(", "@(", "%(")):
                    project["unresolved_items"].append({
                        "item": item, "operation": operation, "value": value,
                        "conditions": conditions(element, parents),
                    })
                    continue
                excludes = [normalize_path(path, part) for part in element.get("Exclude", "").split(";") if part]
                for pattern in value.split(";"):
                    pattern = normalize_path(path, pattern)
                    for source in sources:
                        if not matches(source, pattern) or any(matches(source, excluded) for excluded in excludes):
                            continue
                        declaration = {
                            "project": path, "item": item, "declaration": "explicit_" + operation.lower(),
                            "conditions": conditions(element, parents),
                        }
                        if any(local_name(child.tag) == "AutoGen" and (child.text or "").lower() == "true" for child in element):
                            declaration["generated"] = True
                        membership[source].append(declaration)
        project["project_references"].sort(key=lambda entry: entry["path"])
        projects.append(project)
    return projects, membership


def build_inventory(ref, upstream_ref):
    commit = resolve_commit(ref)
    upstream_commit = resolve_commit(upstream_ref)
    tree, submodules = read_tree(commit)
    upstream, _ = read_tree(upstream_commit)
    source_paths = sorted(p for p in tree if PurePosixPath(p).suffix.lower() in SOURCE_TYPES)
    project_paths = [p for p in tree if PurePosixPath(p).suffix.lower() in PROJECT_TYPES]
    blobs = read_blobs(tree[p]["blob"] for p in source_paths + project_paths)
    projects, membership = read_projects(tree, source_paths, blobs)
    files = []
    for path in source_paths:
        status = "added" if path not in upstream else "unchanged" if tree[path] == upstream[path] else "modified"
        files.append({
            "path": path, "language": SOURCE_TYPES[PurePosixPath(path).suffix.lower()],
            "lines": len(blobs[tree[path]["blob"]].splitlines()), "blob": tree[path]["blob"],
            "group": group_for(path), "role": source_role(path),
            "generated": ".designer." in path.lower() or any(entry.get("generated") for entry in membership[path]),
            "upstream_status": status, "project_declarations": membership[path],
        })
    removed = sorted(p for p in upstream if PurePosixPath(p).suffix.lower() in SOURCE_TYPES and p not in tree)
    summary = {"files": len(files), "lines": sum(entry["lines"] for entry in files)}
    for key in ("language", "group", "role", "upstream_status"):
        totals = {}
        for value in sorted({entry[key] for entry in files}):
            selected = [entry for entry in files if entry[key] == value]
            totals[value] = {"files": len(selected), "lines": sum(entry["lines"] for entry in selected)}
        summary["by_" + key] = totals
    summary["product_vb"] = {
        "files": sum(entry["role"] == "product" and entry["language"] == "VB" for entry in files),
        "lines": sum(entry["lines"] for entry in files if entry["role"] == "product" and entry["language"] == "VB"),
    }
    summary["without_project_declaration"] = sum(not entry["project_declarations"] for entry in files)
    summary["removed_upstream_sources"] = len(removed)
    return {
        "schema_version": 1, "source_commit": commit, "upstream_commit": upstream_commit,
        "scope": "tracked VB/C#/XAML blobs in the selected Git tree; submodule contents excluded",
        "semantics": {
            "lines": "bytes.splitlines(); empty file = 0; final unterminated line counts",
            "upstream_status": "same relative path and Git blob/mode; no rename detection",
            "project_declarations": "literal project XML and common globs; SDK defaults inferred; MSBuild conditions/imports/properties not evaluated; Remove/Update recorded, not applied",
            "path_matching": "project item paths matched case-insensitively for the Windows product",
        },
        "summary": summary, "projects": projects, "submodules": submodules,
        "files": files, "removed_upstream_sources": removed,
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--ref", default=LF_BASELINE, help="Git commit/ref to inventory (default: initial LF baseline)")
    parser.add_argument("--upstream-ref", default=PCL_BASELINE, help="PCL commit/ref used for same-path comparison")
    parser.add_argument("--output", required=True, type=Path, help="Output JSON path")
    args = parser.parse_args()
    try:
        inventory = build_inventory(args.ref, args.upstream_ref)
        args.output.parent.mkdir(parents=True, exist_ok=True)
        with args.output.open("w", encoding="utf-8", newline="\n") as stream:
            json.dump(inventory, stream, ensure_ascii=False, indent=2)
            stream.write("\n")
    except (OSError, ValueError, ET.ParseError) as error:
        parser.exit(1, "Inventory failed: " + str(error) + "\n")
    summary = inventory["summary"]
    counts = Counter(entry["language"] for entry in inventory["files"])
    print("{}: {} source files, {} lines; VB={}, C#={}, XAML={}".format(
        inventory["source_commit"], summary["files"], summary["lines"], counts["VB"], counts["C#"], counts["XAML"]
    ))
    return 0


if __name__ == "__main__":
    sys.exit(main())
