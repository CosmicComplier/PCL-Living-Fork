#!/usr/bin/env python3
"""Check real native Main resources and generator without rebuilding/copying Main."""
import argparse
import copy
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import shutil
import subprocess
import tempfile
import xml.etree.ElementTree as ET

ARTIFACTS = {"core": ("PCLCS", "PCLCS.dll"), "presentation": ("PCLLF.Presentation", "PCLLF.Presentation.dll"),
             "shared": ("MeloongCore", "MeloongCore.dll"), "json": ("Newtonsoft.Json", "Newtonsoft.Json.dll"),
             "registry": ("Microsoft.Win32.Registry", "Microsoft.Win32.Registry.dll")}


def digest(path):
    with path.open("rb") as stream:
        result = hashlib.sha256()
        for chunk in iter(lambda: stream.read(65536), b""):
            result.update(chunk)
        return result.hexdigest()


def execute(command, expected=None):
    result = subprocess.run(command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                            text=True, encoding="utf-8", errors="replace")
    if expected is None:
        assert result.returncode == 0, result.stdout
    else:
        assert result.returncode == 1 and expected in result.stdout, result.stdout
    return result.stdout


def locate(root, relative):
    current = root
    for name in PurePosixPath(relative).parts:
        choices = [p for p in current.iterdir() if p.name.casefold() == name.casefold()]
        assert len(choices) == 1, f"Missing/ambiguous fixed input: {relative}"
        current = choices[0]
    assert current.is_file(), current
    return current


def main():
    source = Path(__file__).resolve().parents[1]
    repository = source.parent.parent
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--main", type=Path, required=True, help="Actual RID build LivingPCL.dll, not the single-file exe")
    parser.add_argument("--dotnet", default=shutil.which("dotnet") or "dotnet")
    parser.add_argument("--generator", type=Path, default=source / "bin/Release/net10.0/ResourceGenerator.dll")
    parser.add_argument("--schema", type=Path, default=source / "LF.Resources.schema.json")
    parser.add_argument("--assets", type=Path, default=repository / "Plain Craft Launcher 2")
    parser.add_argument("--native-project", type=Path, default=repository / "modern/Main/PCL-Living-Fork.Modern.csproj")
    parser.add_argument("--reference-assembly", type=Path)
    parser.add_argument("--defer-windows-getters", action="store_true",
                        help="Explicit Linux-only byte/metadata pass; report 54 actual Windows getters pending")
    for name in ARTIFACTS:
        parser.add_argument("--" + name, type=Path)
    args = parser.parse_args()
    assert args.main.is_file() and args.generator.is_file() and args.schema.is_file()
    artifacts = {name: (getattr(args, name) or args.main.parent / filename).resolve() for name, (_, filename) in ARTIFACTS.items()}
    assert all(p.is_file() for p in artifacts.values()), "Pass existing actual Main dependency DLLs; no placeholder fallback"
    spec = json.loads(args.schema.read_text(encoding="utf-8-sig"))
    expected = {key: digest(artifacts[name]) for name, (key, _) in ARTIFACTS.items()}
    fixed = {row["key"]: locate(args.assets, row["source"]) for row in spec["payloads"] if "source" in row}
    expected.update({key: digest(path) for key, path in fixed.items()})
    assert len(expected) == 18
    dotnet_path = Path(shutil.which(args.dotnet) or args.dotnet).resolve()
    candidates = sorted(dotnet_path.parent.glob("packs/Microsoft.NETCore.App.Ref/10.*/ref/net10.0/Microsoft.Win32.Registry.dll"))
    reference = args.reference_assembly or (candidates[-1] if candidates else None)
    assert reference and reference.is_file(), "Supply actual SDK reference assembly with --reference-assembly"
    # All generated/fixture outputs are temporary. Actual Main and dynamic binaries are never copied.
    with tempfile.TemporaryDirectory(prefix="pcllf-native-resources-") as temporary:
        root = Path(temporary)

        def generate(output, rejection=None, **changes):
            options = {"lf-project": args.assets, "native-project": args.native_project,
                       "schema": args.schema, **artifacts, "output": output, **changes}
            command = [args.dotnet, str(args.generator)]
            for key, value in options.items():
                command.extend(["--" + key, str(value)])
            return execute(command, rejection)

        output = root / "generated"
        generate(output)
        report = json.loads((output / "resource-manifest.json").read_text())
        assert report["resourceCount"] == 18 and report["typedProperties"] == {p["name"]: p["key"] for p in spec["typedProperties"]}
        assert {p["key"]: p["sha256"] for p in report["payloads"] if "sha256" in p} == expected
        project = ET.parse(args.native_project)
        logical = {next((c.text for c in item if c.tag.rsplit("}", 1)[-1] == "Link"), item.get("Include")) for item in project.iter() if item.tag.rsplit("}", 1)[-1] == "Resource"}
        assert {p["logical"] for p in report["packAssets"]} == logical, "Actual Native pack inventory changed/lost"
        for asset in report["packAssets"]:
            assert digest(Path(asset["source"])) == asset["sha256"]
        assert not (output / "Resources.Designer.cs").read_bytes().startswith(b"\xef\xbb\xbf"), "Generated C# BOM changed"
        (root / "expected.json").write_text(json.dumps(expected), encoding="utf-8")
        reader = root / "reader"
        shutil.copytree(source / "tests/ResourceChecks", reader,
                        ignore=shutil.ignore_patterns("bin", "obj", "__pycache__"))
        execute([args.dotnet, "build", str(reader / "ResourceChecks.csproj"), "-c", "Release", "-warnaserror", "-v:q"])
        reader_tfm = "net10.0-windows" if os.name == "nt" else "net10.0"
        print(execute([args.dotnet, str(reader / "bin/Release" / reader_tfm / "ResourceChecks.dll"), str(args.main.resolve()),
                       str(output / "PCL.Resources.resources"), str(root / "expected.json"), str(args.schema),
                       "defer-windows-getters" if args.defer_windows_getters else "full"]).strip())
        vendor = root / "vendor-only"
        for row in spec["payloads"]:
            if "source" in row:
                target = vendor / row["source"]
                target.parent.mkdir(parents=True, exist_ok=True)
                shutil.copyfile(fixed[row["key"]], target)
        assert not any(p.suffix.lower() in {".vb", ".vbproj", ".resx"} for p in vendor.rglob("*") if p.is_file())
        isolated = root / "vendor-output"
        generate(isolated, **{"lf-project": vendor})
        assert digest(isolated / "PCL.Resources.resources") == digest(output / "PCL.Resources.resources"), "Vendor-only binary changed"
        print("PASS vendor-only input contains no VB/vbproj/resx; binary and input BOM bytes retained")
        reject = root / "rejected"
        generate(reject, "Do not embed a reference assembly", registry=reference)
        generate(reject, "PCLCS must use .NETCoreApp,Version=v10.0", core=artifacts["shared"])
        generate(reject, "Wrong actual assembly identity for PCLCS", core=artifacts["presentation"])
        help_file = vendor / next(p["source"] for p in spec["payloads"] if p["key"] == "Help")
        original = help_file.read_bytes()
        try:
            help_file.write_bytes(b"")
            generate(reject, "Empty resource: Help", **{"lf-project": vendor})
        finally:
            help_file.write_bytes(original)
        for name, modify, diagnostic in (
            ("duplicate-key", lambda s: s["payloads"].__setitem__(1, s["payloads"][0]), "Duplicate LF key"),
            ("duplicate-property", lambda s: s["typedProperties"].__setitem__(1, s["typedProperties"][0]), "Invalid native typed resource interface"),
            ("unsupported-type", lambda s: s["payloads"][0].update(type="System.String"), "JsonException")):
            changed = copy.deepcopy(spec)
            modify(changed)
            fixture = root / (name + ".json")
            fixture.write_text(json.dumps(changed), encoding="utf-8")
            generate(reject, diagnostic, schema=fixture)
        duplicate = ET.Element("Project")
        group = ET.SubElement(duplicate, "ItemGroup")
        for _ in range(2):
            entry = ET.SubElement(group, "Resource", Include=report["packAssets"][0]["source"])
            ET.SubElement(entry, "Link").text = report["packAssets"][0]["logical"]
        fixture = root / "DuplicateAssets.csproj"
        ET.ElementTree(duplicate).write(fixture, encoding="utf-8")
        generate(reject, "Ambiguous or conditional native resource inventory", **{"native-project": fixture})
        print("PASS 8 actual CLI rejections: reference/framework/identity/empty payload/duplicate key/property/unsupported type/duplicate asset")


if __name__ == "__main__":
    main()
