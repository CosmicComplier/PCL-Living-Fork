"""Deterministic receipts shared by GitHub triggers and the CNB publisher."""
import hashlib
import json
import re

SOURCE = "DDZmumo/PCL-Living-Fork"


def release_fingerprint(release):
    tag = release.get("tag_name", "")
    if not re.fullmatch(r"v\d+\.\d+\.\d+(?:\.\d+)?", tag) or release.get("draft") is not False or release.get("prerelease") is not False:
        raise RuntimeError("Only stable numbered releases can be synchronized")
    expected = {"PCL-Living-Fork-" + tag[1:] + ".exe", "PCL-Living-Fork-" + tag[1:] + "-win-x64.zip", "SHA256SUMS.txt"}
    assets = []
    for name in sorted(expected):
        found = [asset for asset in release.get("assets", []) if asset.get("name") == name]
        if len(found) != 1 or type(found[0].get("size")) is not int or found[0]["size"] <= 0 or not re.fullmatch(r"sha256:[0-9a-f]{64}", found[0].get("digest", "")):
            raise RuntimeError("Release attachments are incomplete or ambiguous")
        assets.append({"name": name, "size": found[0]["size"], "digest": found[0]["digest"]})
    value = {"tag_name": tag, "name": release.get("name", ""), "body": release.get("body", ""), "assets": assets}
    return hashlib.sha256(json.dumps(value, sort_keys=True, ensure_ascii=False, separators=(",", ":")).encode("utf-8")).hexdigest()


def receipt_for(release):
    return {"schema": 1, "source_repository": SOURCE, "tag_name": release["tag_name"], "source_fingerprint": release_fingerprint(release)}


def receipt_matches(receipt, release):
    return isinstance(receipt, dict) and receipt == receipt_for(release)
