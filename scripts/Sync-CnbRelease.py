#!/usr/bin/env python3
"""Mirror verified GitHub releases to CNB and publish an anonymous update index.

Uses CNB_TOKEN only against CNB's API/Git endpoint. In CNB CI this is the
repository-scoped temporary token; end users never receive a token.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import tempfile
from urllib.error import HTTPError, URLError
from urllib.parse import quote, urlparse
from urllib.request import HTTPRedirectHandler, Request, build_opener
from ReleaseSyncState import receipt_for, valid_tag, validate_release

GITHUB_REPO = "DDZmumo/PCL-Living-Fork"
CNB_REPO = "ddzmumo/PCL-Living-Fork"
GITHUB_API = "https://api.github.com/repos/" + GITHUB_REPO
MAX_BYTES = 1024 * 1024 * 1024


class SafeRedirect(HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        if urlparse(newurl).scheme != "https":
            raise RuntimeError("Refusing a non-HTTPS redirect")
        if req.has_header("Authorization"):
            raise RuntimeError("Authenticated API requests must not redirect")
        return super().redirect_request(req, fp, code, msg, headers, newurl)


class Http:
    def __init__(self, token, repository):
        self.token = token
        self.repository = repository
        self.opener = build_opener(SafeRedirect())

    def open(self, url, method="GET", data=None, auth=False, headers=None):
        parsed = urlparse(url)
        if parsed.scheme != "https" or parsed.username or parsed.password:
            raise RuntimeError("Untrusted HTTPS request")
        merged = {"User-Agent": "PCLLF-CNB-Mirror", "Accept": "application/json"}
        merged.update(headers or {})
        if auth:
            if parsed.hostname != "api.cnb.cool" or not parsed.path.startswith("/" + self.repository + "/-/"):
                raise RuntimeError("CNB credentials cannot leave the target repository API")
            merged["Authorization"] = "Bearer " + self.token
            merged["Accept"] = "application/vnd.cnb.api+json"
        try:
            return self.opener.open(Request(url, data=data, headers=merged, method=method), timeout=45)
        except HTTPError as error:
            raise RuntimeError("HTTP request failed with status " + str(error.code)) from None
        except URLError:
            raise RuntimeError("Network request failed") from None

    def json(self, url, method="GET", data=None, auth=False, missing=False):
        payload = None if data is None else json.dumps(data, ensure_ascii=False).encode("utf-8")
        try:
            with self.open(url, method, payload, auth, {"Content-Type": "application/json"}) as response:
                raw = response.read(4 * 1024 * 1024 + 1)
                if len(raw) > 4 * 1024 * 1024:
                    raise RuntimeError("API response is too large")
                return json.loads(raw) if raw else None
        except RuntimeError as error:
            if missing and str(error) == "HTTP request failed with status 404":
                return None
            raise

    def download(self, url, target, size, digest):
        if size < 1 or size > MAX_BYTES:
            raise RuntimeError("Invalid attachment size")
        total, actual = 0, hashlib.sha256()
        with self.open(url) as response, target.open("wb") as output:
            while chunk := response.read(256 * 1024):
                total += len(chunk)
                if total > size:
                    raise RuntimeError("Attachment exceeds its declared size")
                actual.update(chunk)
                output.write(chunk)
        if total != size or actual.hexdigest() != digest:
            raise RuntimeError("Attachment failed SHA256/size verification")


def manifest_hash(text, name):
    values = []
    for line in text.splitlines():
        match = re.fullmatch(r"([0-9a-fA-F]{64})\s+\*?(.+?)\s*", line)
        if match and match[2] == name:
            values.append(match[1].lower())
    if len(set(values)) != 1:
        raise RuntimeError("Missing or conflicting SHA256 entry: " + name)
    return values[0]


def source_files(release):
    tag = validate_release(release)
    version = tag[1:]
    expected = ["PCL-Living-Fork-" + version + ".exe", "PCL-Living-Fork-" + version + "-win-x64.zip", "SHA256SUMS.txt"]
    result = []
    for name in expected:
        items = [item for item in release.get("assets", []) if item.get("name") == name]
        if len(items) != 1:
            raise RuntimeError("Source attachment is missing or ambiguous: " + name)
        item = items[0]
        wanted = "https://github.com/" + GITHUB_REPO + "/releases/download/" + quote(tag, safe="") + "/" + quote(name, safe="")
        if item.get("browser_download_url") != wanted or not re.fullmatch(r"sha256:[0-9a-f]{64}", item.get("digest", "")):
            raise RuntimeError("Source URL or GitHub attachment digest is invalid")
        if type(item.get("size")) is not int or not 0 < item["size"] <= MAX_BYTES:
            raise RuntimeError("Source attachment size is invalid")
        result.append(item)
    return result


class Git:
    def __init__(self, directory, token):
        directory = Path(directory)
        helper = directory / "askpass.py"
        helper.write_text(
            "import os,sys,re\nfrom urllib.parse import urlparse\n"
            "p=sys.argv[1] if len(sys.argv)>1 else ''\n"
            "m=re.search(r'https://[^\\s\\\'\\\"]+',p)\n"
            "if not m or urlparse(m[0]).hostname != 'cnb.cool': sys.exit(1)\n"
            "print('cnb' if p.lower().startswith('username') else os.environ['CNB_TOKEN'])\n",
            encoding="utf-8",
        )
        launcher = directory / ("askpass.cmd" if os.name == "nt" else "askpass")
        if os.name == "nt":
            launcher.write_text('@"' + sys.executable + '" "' + str(helper) + '" %*\r\n', encoding="utf-8")
        else:
            import shlex
            launcher.write_text("#!/bin/sh\nexec " + shlex.quote(sys.executable) + " " + shlex.quote(str(helper)) + ' "$@"\n', encoding="utf-8")
            launcher.chmod(0o700)
        self.environment = dict(os.environ, CNB_TOKEN=token, GIT_ASKPASS=str(launcher), GIT_TERMINAL_PROMPT="0")
        self.environment.update(GIT_AUTHOR_NAME="PCL Living Fork Mirror", GIT_AUTHOR_EMAIL="mirror@users.noreply.cnb.cool", GIT_COMMITTER_NAME="PCL Living Fork Mirror", GIT_COMMITTER_EMAIL="mirror@users.noreply.cnb.cool")

    def run(self, *arguments, cwd=None, optional=False):
        result = subprocess.run(["git", "-c", "credential.helper=", *arguments], cwd=cwd, env=self.environment, capture_output=True, text=True, encoding="utf-8", errors="replace")
        if result.returncode and not optional:
            raise RuntimeError("Git operation failed (check repository write permission and branch protection)")
        return result


def pipeline(repository):
    # No push trigger: publishing latest.json must not create a recursive pipeline.
    return """updates:
  web_trigger: &mirror
    - name: mirror-release
      lock:
        key: pcl-release-mirror
        wait: true
        timeout: 600
        expires: 900
      docker:
        image: python:3.12-alpine
      stages:
        - name: git
          script: apk add --no-cache git
        - name: mirror
          script: python mirror.py --repository REPOSITORY
  "crontab: */30 * * * *": *mirror
  api_trigger: *mirror
""".replace("REPOSITORY", repository)


def attribution():
    return """## 来源与许可

原作作者为龙腾猫跃及 PCL 原项目贡献者，原作版权归成都瓜皮龙科技有限公司。基线为上游 2.14.0.0，保留原作与第三方组件的署名和许可。

- [上游 PCL](https://github.com/Meloong-Git/PCL)
- [来源与版权声明](https://github.com/DDZmumo/PCL-Living-Fork/blob/main/NOTICE.md)
- [许可与使用指南](https://github.com/DDZmumo/PCL-Living-Fork/blob/main/LICENCE)
- [支持原作者](https://meloong.com/afd/a/LTCat)
"""


def download_readme(repository):
    return ("# PCL Living Fork 国内下载源\n\n"
            "本仓库用于同步 PCL Living Fork 的正式版与预发布版说明、安装包及校验文件，供国内用户下载。\n\n"
            "- [下载正式版与预发布版](https://cnb.cool/" + repository + "/-/releases)\n"
            "- [源码与问题反馈](https://github.com/" + GITHUB_REPO + ")\n"
            "- [公开更新清单](https://cnb.cool/" + repository + "/-/git/raw/updates/latest.json)\n\n"
            "预发布版保留独立版本清单，不替换正式版更新清单。完整源码在 GitHub 主仓库维护。`updates` 分支仅保留更新清单和发布同步配置。\n\n" + attribution())


def publish_index(git, repository, index, directory, bootstrap, latest=True, receipt=None):
    tag = validate_release(index)
    if latest and index["prerelease"]:
        raise RuntimeError("A prerelease cannot replace the stable update index")
    remote = "https://cnb.cool/" + repository
    control = Path(directory) / "control"
    exists = bool(git.run("ls-remote", remote, "refs/heads/updates").stdout.strip())
    if exists:
        git.run("clone", "--depth", "1", "--single-branch", "--branch", "updates", remote, str(control))
    else:
        control.mkdir()
        git.run("init", "--initial-branch=updates", cwd=control)
        git.run("remote", "add", "origin", remote, cwd=control)
    folder = control / "releases"
    folder.mkdir(exist_ok=True)
    version_index = "releases/" + tag + ".json"
    (control / version_index).write_text(json.dumps(index, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    files = [version_index]
    if latest:
        files.append("latest.json")
        (control / "latest.json").write_text(json.dumps(index, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    if receipt:
        if not valid_tag(receipt.get("tag_name", "")):
            raise RuntimeError("Invalid synchronization receipt tag")
        folder = control / "receipts"
        folder.mkdir(exist_ok=True)
        relative = "receipts/" + receipt["tag_name"] + ".json"
        (control / relative).write_text(json.dumps(receipt, sort_keys=True, indent=2) + "\n", encoding="utf-8")
        files.append(relative)
    if bootstrap or not exists:
        (control / "mirror.py").write_bytes(Path(__file__).read_bytes())
        (control / "ReleaseSyncState.py").write_bytes(Path(__file__).with_name("ReleaseSyncState.py").read_bytes())
        (control / ".cnb.yml").write_text(pipeline(repository), encoding="utf-8")
        (control / "README.md").write_text("# PCL Living Fork 更新清单\n\n`latest.json` 仅用于正式版更新；`releases/<tag>.json` 保存各正式版、预发布版的独立清单。同步按内容指纹去重，发布时校验 GitHub SHA256，流水线使用 CNB 临时令牌。\n\n" + attribution(), encoding="utf-8")
        files += ["mirror.py", "ReleaseSyncState.py", ".cnb.yml", "README.md"]
    git.run("add", "--", *files, cwd=control)
    if git.run("diff", "--cached", "--quiet", cwd=control, optional=True).returncode == 0:
        print("Anonymous update index is already current")
        return
    git.run("commit", "-m", "chore: refresh update mirror " + index["tag_name"], cwd=control)
    git.run("push", "origin", "HEAD:updates", cwd=control)
    print("Published anonymous update index")


def cached_index(http, repository, source, items, release):
    if not release or release.get("draft") is not False or release.get("prerelease") is not source["prerelease"]:
        return None
    try:
        value = http.json("https://cnb.cool/" + repository + "/-/git/raw/updates/releases/" + quote(source["tag_name"], safe="") + ".json", missing=True)
        if not value and not source["prerelease"]:
            value = http.json("https://cnb.cool/" + repository + "/-/git/raw/updates/latest.json", missing=True)
        if not value or value.get("tag_name") != source["tag_name"] or value.get("draft") is not False or value.get("prerelease") is not source["prerelease"]:
            return None
        if len(value.get("assets", [])) != len(items):
            return None
        for item in items:
            indexed = [asset for asset in value["assets"] if asset.get("name") == item["name"]]
            stored = [asset for asset in release.get("assets", []) if asset.get("name") == item["name"]]
            wanted = "https://cnb.cool/" + repository + "/-/releases/download/" + source["tag_name"] + "/" + item["name"]
            if len(indexed) != 1 or len(stored) != 1:
                return None
            if indexed[0].get("size") != item["size"] or indexed[0].get("digest") != item["digest"] or indexed[0].get("browser_download_url") != wanted:
                return None
            if stored[0].get("size") != item["size"] or stored[0].get("hash_algo", "").lower().replace("-", "") != "sha256" or stored[0].get("hash_value", "").lower() != item["digest"][7:]:
                return None
        return value
    except (ValueError, RuntimeError):
        return None


def release_form(source, latest):
    validate_release(source)
    return {"tag_name": source["tag_name"], "name": source["name"], "body": source["body"], "draft": False,
            "prerelease": source["prerelease"], "make_latest": "true" if latest and not source["prerelease"] else "false"}


def select_release(http, tag=""):
    if tag and not valid_tag(tag):
        raise RuntimeError("Invalid requested release tag")
    current = http.json(GITHUB_API + "/releases/latest")
    source = current if not tag or tag == current["tag_name"] else http.json(GITHUB_API + "/releases/tags/" + quote(tag, safe=""))
    source_files(source)
    if tag and source["tag_name"] != tag:
        raise RuntimeError("Source response does not match the requested release")
    return source, not source["prerelease"] and source["tag_name"] == current["tag_name"]


def sync(http, repository, directory, source=None, latest=True):
    api = "https://api.cnb.cool/" + quote(repository, safe="/") + "/-/releases"
    if source is None:
        source = http.json(GITHUB_API + "/releases/latest")
    items = source_files(source)
    latest = latest and not source["prerelease"]
    tag = source["tag_name"]
    release = http.json(api + "/tags/" + quote(tag, safe=""), auth=True, missing=True)
    cached = cached_index(http, repository, source, items, release)
    if cached:
        if release.get("body") != source["body"] or release.get("name") != source["name"] or (latest and not release.get("is_latest")):
            promote = latest and http.json(GITHUB_API + "/releases/latest")["tag_name"] == tag
            http.json(api + "/" + str(release["id"]), "PATCH", release_form(source, promote), auth=True)
        print("Release payloads unchanged; no attachment transfer needed")
        return cached
    cache = Path(directory) / "downloads"
    cache.mkdir()
    manifest = next(item for item in items if item["name"] == "SHA256SUMS.txt")
    http.download(manifest["browser_download_url"], cache / manifest["name"], manifest["size"], manifest["digest"][7:])
    text = (cache / manifest["name"]).read_text(encoding="utf-8-sig")
    for item in items:
        digest = item["digest"][7:]
        if item is not manifest:
            if manifest_hash(text, item["name"]) != digest:
                raise RuntimeError("GitHub digest and SHA256SUMS disagree")
            http.download(item["browser_download_url"], cache / item["name"], item["size"], digest)
    if release is None:
        release = http.json(api, "POST", {"tag_name": tag, "target_commitish": "main", "name": source["name"], "body": source["body"], "draft": True, "prerelease": source["prerelease"], "make_latest": "false"}, auth=True)
    release_url = api + "/" + quote(str(release["id"]), safe="")
    for item in items:
        current = http.json(release_url, auth=True)
        existing = [asset for asset in current.get("assets", []) if asset.get("name") == item["name"]]
        overwrite = False
        if existing:
            if len(existing) != 1:
                raise RuntimeError("An existing CNB attachment is ambiguous")
            algorithm = existing[0].get("hash_algo", "").lower().replace("-", "")
            value = existing[0].get("hash_value", "").lower()
            overwrite = existing[0].get("size") != item["size"] or (algorithm == "sha256" and value != item["digest"][7:])
            if not overwrite:
                continue
        upload = http.json(release_url + "/asset-upload-url", "POST", {"asset_name": item["name"], "size": item["size"], "overwrite": overwrite, "ttl": 0}, auth=True)
        with (cache / item["name"]).open("rb") as stream:
            with http.open(upload["upload_url"], "PUT", stream, headers={"Content-Type": "application/octet-stream", "Content-Length": str(item["size"])}) as response:
                response.read()
        verify = upload["verify_url"]
        if not verify.startswith(release_url + "/asset-upload-confirmation/"):
            raise RuntimeError("Untrusted upload confirmation endpoint")
        http.json(verify + ("&" if "?" in verify else "?") + "ttl=0", "POST", auth=True)
        print("Uploaded " + item["name"])
    promote = latest and http.json(GITHUB_API + "/releases/latest")["tag_name"] == tag
    http.json(release_url, "PATCH", release_form(source, promote), auth=True)
    release = http.json(release_url, auth=True)
    if release.get("draft") is not False or release.get("prerelease") is not source["prerelease"]:
        raise RuntimeError("CNB publication does not preserve the release channel")
    index = {"tag_name": tag, "name": source["name"], "draft": False, "prerelease": source["prerelease"], "assets": []}
    for item in items:
        candidates = [asset for asset in release.get("assets", []) if asset.get("name") == item["name"]]
        if len(candidates) != 1:
            raise RuntimeError("CNB publication is incomplete")
        url = candidates[0].get("browser_download_url", "")
        wanted = "https://cnb.cool/" + repository + "/-/releases/download/" + quote(tag, safe="") + "/" + quote(item["name"], safe="")
        if url != wanted:
            raise RuntimeError("CNB returned an unexpected public download URL")
        # Anonymous read-back is mandatory before advertising this mirror to players.
        http.download(url, cache / ("verified-" + item["name"]), item["size"], item["digest"][7:])
        index["assets"].append({"name": item["name"], "size": item["size"], "digest": item["digest"], "browser_download_url": url})
        print("Verified anonymous SHA256: " + item["name"])
    return index


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", default=CNB_REPO)
    parser.add_argument("--auth-file", type=Path)
    parser.add_argument("--bootstrap-ci", action="store_true")
    parser.add_argument("--tag", default=os.environ.get("GITHUB_RELEASE_TAG", ""))
    args = parser.parse_args()
    if not re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9_.-]*/[A-Za-z0-9][A-Za-z0-9_.-]*", args.repository):
        raise RuntimeError("Invalid target repository")
    token = os.environ.get("CNB_TOKEN", "")
    if args.auth_file:
        token = json.loads(args.auth_file.read_text(encoding="utf-8-sig")).get("token", "")
    if not token:
        raise RuntimeError("Set CNB_TOKEN or provide a local ignored --auth-file")
    http = Http(token, args.repository)
    with tempfile.TemporaryDirectory(prefix="pcl-cnb-mirror-") as directory:
        git = Git(directory, token)
        release, latest = select_release(http, args.tag)
        index = sync(http, args.repository, directory, release, latest)
        publish_latest = latest and not release["prerelease"] and http.json(GITHUB_API + "/releases/latest")["tag_name"] == release["tag_name"]
        publish_index(git, args.repository, index, directory, args.bootstrap_ci, latest=publish_latest, receipt=receipt_for(release))
        if not publish_latest:
            print("Prerelease or historical release synchronized; stable update index preserved")
    print("CNB release mirror is ready: " + index["tag_name"])


if __name__ == "__main__":
    try:
        main()
    except Exception as failure:
        print("Mirror failed: " + str(failure), file=sys.stderr)
        sys.exit(1)
