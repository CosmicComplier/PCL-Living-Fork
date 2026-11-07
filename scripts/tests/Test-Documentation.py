#!/usr/bin/env python3
"""Exercise the real lightweight document checker in an owned Git fixture."""
from pathlib import Path
import os
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[2]
ENV = {name: os.environ[name] for name in ("PATH", "SYSTEMROOT", "WINDIR", "LANG", "LC_ALL") if name in os.environ}
ENV.update(GIT_CONFIG_NOSYSTEM="1", GIT_CONFIG_GLOBAL=os.devnull, PYTHONDONTWRITEBYTECODE="1")


def main() -> None:
    checks = 0
    with tempfile.TemporaryDirectory(prefix="PclDocumentation-") as temporary:
        root = Path(temporary).resolve()
        assert root.parent == Path(tempfile.gettempdir()).resolve()
        subprocess.run(["git", "-c", "init.templateDir=", "init", "--quiet", "--initial-branch=docs-fixture", str(root)], env=ENV, check=True)
        (root / "docs/assets").mkdir(parents=True)
        (root / "docs/guide.md").write_text("# Guide\n\n[Home](../README.md)\n", encoding="utf-8")
        (root / "docs/with space.md").write_text("# Space\n", encoding="utf-8")
        (root / "docs/assets/icon.svg").write_text('<svg xmlns="http://www.w3.org/2000/svg"/>', encoding="utf-8")
        (root / "README.md").write_text("# Fixture\n", encoding="utf-8")
        subprocess.run(["git", "add", "README.md", "docs"], cwd=root, env=ENV, check=True)
        subprocess.run(["git", "update-index", "--add", "--cacheinfo", "160000," + "1" * 40 + ",MeloongCore"], cwd=root, env=ENV, check=True)

        def sample(markdown: str, passes: bool, label: str) -> None:
            nonlocal checks
            (root / "README.md").write_text(markdown, encoding="utf-8")
            result = subprocess.run([sys.executable, str(ROOT / "scripts/Check-Documentation.py"), "--repo-root", str(root)],
                                    env=ENV, capture_output=True, text=True, timeout=30)
            assert (result.returncode == 0) is passes, (label, result.stdout, result.stderr)
            checks += 1

        sample('# Fixture\n[Guide](docs/guide.md#guide)\n[Space](docs/with%20space.md)\n<img src="docs/assets/icon.svg">\n', True, "existing Markdown/HTML/escaped local links")
        sample('[Missing](docs/missing.md)\n', False, "dangling Markdown link")
        sample('<img src="docs/assets/missing.svg">\n', False, "dangling HTML image")
        sample('[outside](../outside.md)\n', False, "link outside owned repository")
        sample('```powershell\nWrite-Output fixture\n', False, "unclosed code fence")
        sample('```text\n[not a link](missing.md)\n```\n`[inline](missing.md)`\n', True, "code examples are not live links")
        sample('[Guide][guide]\n\n[guide]: docs/guide.md\n', True, "reference link definition")
        sample('[Guide][guide]\n\n[guide]: docs/absent.md\n', False, "dangling reference definition")
        sample('[Shared](MeloongCore/LICENSE.txt)\n', True, "known submodule need not be initialized")
        sample('[Remote](https://fixture.invalid/never-requested)\n', True, "external links do not make network requests")
    print(f"PASS {checks} documentation checker cases; no network, SDK or product execution.")


if __name__ == "__main__":
    main()
