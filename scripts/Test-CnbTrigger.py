import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location("trigger", Path(__file__).with_name("Trigger-CnbSync.py"))
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class TriggerTests(unittest.TestCase):
    def test_release_event_targets_only_mirror_branch(self):
        calls = []
        def request(url, token, data=None):
            calls.append((url, token, data))
            return {"success": True, "sn": "cnb-synthetic-1"}
        result = module.trigger("synthetic-secret", "v1.3.4", request=request)
        self.assertEqual(result, "cnb-synthetic-1")
        self.assertEqual(calls[0][0], module.API + "start")
        self.assertEqual(calls[0][2]["branch"], "updates")
        self.assertEqual(calls[0][2]["event"], "api_trigger")
        self.assertEqual(calls[0][2]["env"], {"GITHUB_RELEASE_TAG": "v1.3.4"})
        self.assertNotIn("synthetic-secret", str(calls[0][2]))

    def test_missing_secret_and_bad_tags_never_call_api(self):
        def request(*args):
            self.fail("Invalid request reached API")
        for token, tag in (("", "v1.3.4"), ("bad\nsecret", "v1.3.4"), ("synthetic", "v1.3.4-beta"), ("synthetic", "x; echo unsafe")):
            with self.assertRaises(RuntimeError):
                module.trigger(token, tag, request=request)

    def test_waits_for_cnb_success(self):
        statuses = iter(("pending", "running", "success"))
        def request(url, token, data=None):
            return {"success": True, "sn": "cnb-synthetic-2"} if data else {"status": next(statuses)}
        sleeps = []
        self.assertEqual(module.trigger("synthetic", wait=True, request=request, sleep=sleeps.append), "cnb-synthetic-2")
        self.assertEqual(sleeps, [5, 5])

    def test_rejected_and_failed_builds_report_failure(self):
        with self.assertRaises(RuntimeError):
            module.trigger("synthetic", request=lambda *args: {"success": False})
        def request(url, token, data=None):
            return {"success": True, "sn": "cnb-synthetic-3"} if data else {"status": "failed"}
        with self.assertRaises(RuntimeError):
            module.trigger("synthetic", wait=True, request=request)

    def test_wait_is_bounded(self):
        times = iter((0, 0, 10))
        def request(url, token, data=None):
            return {"success": True, "sn": "cnb-synthetic-4"} if data else {"status": "running"}
        with self.assertRaises(RuntimeError):
            module.trigger("synthetic", wait=True, request=request, sleep=lambda _: None, clock=lambda: next(times), timeout=5)

    def test_credential_cannot_be_sent_to_other_api(self):
        with self.assertRaises(RuntimeError):
            module.request_json("https://api.github.com/repos/example", "synthetic")


if __name__ == "__main__":
    unittest.main()
