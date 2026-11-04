#!/usr/bin/env python3
"""Trigger the release-only CNB mirror after a GitHub Release event."""
import argparse
import json
import os
import re
import sys
import time
from urllib.error import HTTPError, URLError
from urllib.parse import quote
from urllib.request import HTTPRedirectHandler, Request, build_opener
from ReleaseSyncState import SOURCE, receipt_for, receipt_matches, valid_tag

REPOSITORY = "ddzmumo/PCL-Living-Fork"
API = "https://api.cnb.cool/" + REPOSITORY + "/-/build/"
PAGE = "https://cnb.cool/" + REPOSITORY + "/-/build/logs/"


def public_json(url, missing=False):
    prefixes = ("https://api.github.com/repos/" + SOURCE + "/releases/", "https://cnb.cool/" + REPOSITORY + "/-/git/raw/updates/receipts/")
    if not url.startswith(prefixes):
        raise RuntimeError("Untrusted release state endpoint")
    try:
        req = Request(url, headers={"Accept": "application/json", "User-Agent": "PCLLF-Release-Event"})
        with build_opener(NoRedirect()).open(req, timeout=15) as response:
            raw = response.read(4 * 1024 * 1024 + 1)
            if len(raw) > 4 * 1024 * 1024:
                raise RuntimeError("Release state response is too large")
            return json.loads(raw)
    except HTTPError as error:
        if missing and error.code == 404:
            return None
        raise RuntimeError("Release state request failed with HTTP " + str(error.code)) from None
    except URLError:
        raise RuntimeError("Release state request failed") from None


def trigger_if_changed(token, tag="", wait=False, read=public_json, request=None):
    if tag and not valid_tag(tag):
        raise RuntimeError("Invalid requested release tag")
    release = read("https://api.github.com/repos/" + SOURCE + "/releases/" + ("tags/" + quote(tag, safe="") if tag else "latest"))
    actual_tag = release.get("tag_name", "")
    if tag and actual_tag != tag:
        raise RuntimeError("Release state does not match the requested tag")
    receipt_for(release)
    receipt = read("https://cnb.cool/" + REPOSITORY + "/-/git/raw/updates/receipts/" + quote(actual_tag, safe="") + ".json", missing=True)
    if receipt_matches(receipt, release):
        print("Already synchronized with identical notes and attachments; no CNB task started", flush=True)
        return None
    return trigger(token, actual_tag, wait, request=request or request_json)


class NoRedirect(HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        raise RuntimeError("Authenticated trigger requests must not redirect")


def request_json(url, token, data=None):
    if not url.startswith(API):
        raise RuntimeError("Trigger credential cannot leave the target CNB build API")
    body = None if data is None else json.dumps(data).encode("utf-8")
    req = Request(url, data=body, method="GET" if data is None else "POST", headers={"Authorization": "Bearer " + token, "Accept": "application/vnd.cnb.api+json", "Content-Type": "application/json", "User-Agent": "PCLLF-Release-Event"})
    try:
        with build_opener(NoRedirect()).open(req, timeout=30) as response:
            raw = response.read(1024 * 1024 + 1)
            if len(raw) > 1024 * 1024:
                raise RuntimeError("Trigger response is too large")
            return json.loads(raw)
    except HTTPError as error:
        raise RuntimeError("CNB trigger request failed with HTTP " + str(error.code)) from None
    except URLError:
        raise RuntimeError("CNB trigger request failed") from None


def trigger(token, tag="", wait=False, request=request_json, sleep=time.sleep, clock=time.monotonic, timeout=600):
    if not token or any(character.isspace() for character in token):
        raise RuntimeError("CNB_TRIGGER_TOKEN is not configured or has an invalid format")
    if tag and not valid_tag(tag):
        raise RuntimeError("Only numbered release tags can be synchronized")
    payload = {"branch": "updates", "event": "api_trigger", "title": "GitHub Release sync" + (" " + tag if tag else ""), "sync": "false"}
    if tag:
        payload["env"] = {"GITHUB_RELEASE_TAG": tag}
    result = request(API + "start", token, payload)
    sn = result.get("sn", "")
    if result.get("success") is not True or not re.fullmatch(r"[A-Za-z0-9_-]+", sn):
        raise RuntimeError("CNB did not accept the release synchronization request")
    print("Triggered CNB release mirror: " + PAGE + sn, flush=True)
    if wait:
        deadline = clock() + timeout
        while clock() < deadline:
            status = request(API + "status/" + quote(sn, safe=""), token).get("status", "").lower()
            if status == "success":
                print("CNB release synchronization succeeded", flush=True)
                return sn
            if status in {"failed", "failure", "error", "cancelled", "canceled", "timeout", "timed_out", "killed"}:
                raise RuntimeError("CNB release synchronization ended with status " + status)
            sleep(5)
        raise RuntimeError("Timed out waiting for CNB release synchronization")
    return sn


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--tag", default=os.environ.get("CNB_RELEASE_TAG", ""))
    parser.add_argument("--wait", action="store_true")
    args = parser.parse_args()
    trigger_if_changed(os.environ.get("CNB_TRIGGER_TOKEN", ""), args.tag, args.wait)


if __name__ == "__main__":
    try:
        main()
    except Exception as error:
        print("Release sync trigger failed: " + str(error), file=sys.stderr)
        sys.exit(1)
