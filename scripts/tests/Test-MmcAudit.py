#!/usr/bin/env python3
"""Offline MMC archive-audit regression tests using only LF-authored tiny ZIPs."""
from __future__ import annotations

import hashlib
import importlib.util
import json
from pathlib import Path
import stat
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch
import warnings
import zipfile

SCRIPT = Path(__file__).resolve().parents[1] / "mmc" / "AuditArchive.py"
spec = importlib.util.spec_from_file_location("mmc_audit", SCRIPT)
audit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audit)
MANIFEST = {"formatVersion": 1, "components": [{"uid": "net.minecraft", "version": "1.7.10"}]}


class ArchiveTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory(prefix="pcllf-mmc-audit-")
        self.root = Path(self.directory.name)

    def tearDown(self):
        self.directory.cleanup()

    def archive(self, files=None, manifest=MANIFEST, prefix=""):
        result = self.root / "sample.zip"
        with warnings.catch_warnings(), zipfile.ZipFile(result, "w", compression=zipfile.ZIP_DEFLATED) as out:
            warnings.simplefilter("ignore", UserWarning)
            if manifest is not None:
                out.writestr(prefix + "mmc-pack.json", json.dumps(manifest))
            for path, value in files or []:
                out.writestr(path, json.dumps(value) if isinstance(value, dict) else value)
        return result

    def test_nested_root_without_directory_entries_and_missing_cfg(self):
        result = self.archive([("Pack/.minecraft/config/test.cfg", "x")], prefix="Pack/")
        observed = audit.audit_archive(result)
        self.assertEqual("metadata-audited", observed["auditStatus"])
        self.assertEqual("not-evaluated", observed["compatibilityStatus"])
        self.assertEqual("Pack", observed["instanceRoot"])
        self.assertEqual(".minecraft", observed["gameRoot"]["selected"])
        self.assertFalse(observed["cfg"]["present"])
        self.assertEqual(hashlib.sha256(result.read_bytes()).hexdigest(), observed["source"]["sha256"])
        self.assertFalse(observed["archive"]["payloadCrcVerified"])

    def test_two_game_trees_and_two_library_scopes_do_not_merge(self):
        result = self.archive([
            ("minecraft/libraries/a.jar", b"game"), ("libraries/a.jar", b"launcher"),
            (".minecraft/saves/keep.txt", "other tree")])
        observed = audit.audit_archive(result)
        self.assertEqual("minecraft", observed["gameRoot"]["selected"])
        self.assertTrue(observed["gameRoot"]["bothTreesPreserved"])
        self.assertEqual(["libraries/a.jar"], observed["localLibraryFiles"])
        self.assertEqual(["minecraft/libraries/a.jar"], observed["gameLibraryFiles"])

    def test_metadata_observes_discrete_java_and_local_patch(self):
        manifest = {"formatVersion": 1, "components": [
            {"uid": "net.minecraft", "version": "1.7.10"},
            {"uid": "test.bootstrap", "version": "1", "disabled": True, "important": True}]}
        result = self.archive([("patches/test.bootstrap.json", {
            "formatVersion": 1, "uid": "test.bootstrap", "version": "2", "compatibleJavaMajors": [17, 21, 26],
            "mainClass": "example.Main", "+jvmArgs": ["-Dname=one token"],
            "+libraries": [{"name": "example:local:1", "MMC-hint": "local"}],
        })], manifest)
        observed = audit.audit_archive(result)
        self.assertTrue(observed["components"][1]["effectiveEnabled"])
        self.assertEqual([17, 21, 26], observed["patches"][0]["compatibleJavaMajors"])
        self.assertTrue(observed["patches"][0]["libraries"][0]["local"])
        self.assertEqual(["net.minecraft"], observed["remoteMetadataRequired"])

    def test_unknown_fields_and_executors_are_reported_not_claimed_supported(self):
        result = self.archive([("patches/example.json", {
            "fileId": "example", "newLaunchOperation": True, "jarMods": [{"name": "example:a:1"}],
            "+traits": ["legacyLaunch"],
        })])
        observed = audit.audit_archive(result)
        self.assertEqual(["newLaunchOperation"], observed["patches"][0]["unregisteredFields"])
        self.assertEqual(["jarMods"], observed["patches"][0]["requiresUnimplementedExecutor"])
        self.assertEqual(["legacyLaunch"], observed["patches"][0]["unregisteredOrUnsupportedTraits"])
        self.assertFalse(observed["patches"][0]["referencedByManifest"])

    def test_ambiguous_roots_rejected(self):
        with self.assertRaisesRegex(audit.AuditError, "exactly one"):
            audit.audit_archive(self.archive([("Other/mmc-pack.json", MANIFEST)]))

    def test_no_root_rejected(self):
        with self.assertRaisesRegex(audit.AuditError, "exactly one"):
            audit.audit_archive(self.archive([("readme.txt", "nothing")], manifest=None))

    def test_unsafe_paths_rejected(self):
        for name in ("../escape", "/rooted", "C:/drive", "safe/../escape", "safe\\..\\escape",
                     "safe//empty", "safe/./dot", "file:stream", "NUL.txt", "a/trailing.", "a/trailing "):
            with self.subTest(name=name), self.assertRaises(audit.AuditError):
                audit.audit_archive(self.archive([(name, "x")]))

    def test_case_collisions_and_role_collisions_rejected(self):
        for files in ([('a.jar', 'a'), ('A.jar', 'b')], [('Mods/a', 'a'), ('mods/A', 'b')],
                      [('folder', 'a'), ('folder/file', 'b')], [('same', 'a'), ('same', 'b')]):
            with self.subTest(files=files), self.assertRaises(audit.AuditError):
                audit.audit_archive(self.archive(files))

    def test_directory_aliases_preserve_both_disjoint_file_sources(self):
        observed = audit.audit_archive(self.archive([
            ('minecraft/Mods/', ''), ('minecraft/mods/', ''),
            ('minecraft/Mods/a', 'a'), ('minecraft/mods/b', 'b')]))
        self.assertEqual("metadata-audited", observed["auditStatus"])
        self.assertEqual([["minecraft/Mods", "minecraft/mods"]], observed["archive"]["directoryAliases"])
        self.assertEqual(3, observed["archive"]["fileCount"])

    def test_same_bytes_file_alias_still_rejected(self):
        with self.assertRaisesRegex(audit.AuditError, "file target"):
            audit.audit_archive(self.archive([('Mods/a', 'same'), ('mods/A', 'same')]))

    def test_directory_role_is_not_flat_file(self):
        for name in ("minecraft", ".minecraft", "libraries", "patches"):
            with self.subTest(name=name), self.assertRaisesRegex(audit.AuditError, "directory role"):
                audit.audit_archive(self.archive([(name, "conflict")]))

    def test_symlink_rejected(self):
        link = zipfile.ZipInfo("outside")
        link.create_system = 3
        link.external_attr = (stat.S_IFLNK | 0o777) << 16
        result = self.archive([(link, "../target")])
        with self.assertRaisesRegex(audit.AuditError, "Symbolic-link"):
            audit.audit_archive(result)

    def test_reserved_lf_management_metadata_rejected(self):
        for name in ("PCL/InstanceDefinition.json", "PCL/MmcSource/x/file", "PCL/MmcTransactions/x/journal"):
            with self.subTest(name=name), self.assertRaisesRegex(audit.AuditError, "reserved"):
                audit.audit_archive(self.archive([(name, "injected")]))

    def test_duplicate_json_fields_rejected(self):
        with self.assertRaisesRegex(audit.AuditError, "Duplicate JSON"):
            audit.audit_archive(self.archive([("mmc-pack.json", '{"formatVersion":1,"formatVersion":1}')], manifest=None))

    def test_invalid_manifest_schema_and_flags_rejected(self):
        for manifest in ({"formatVersion": True, "components": [{}]}, {"formatVersion": 2, "components": [{}]},
                         {"formatVersion": 1, "components": []},
                         {"formatVersion": 1, "components": [{"uid": "x", "disabled": "false"}]},
                         {"formatVersion": 1, "components": [{"uid": "x"}, {"uid": "x"}]}):
            with self.subTest(manifest=manifest), self.assertRaises(audit.AuditError):
                audit.audit_archive(self.archive(manifest=manifest))

    def test_invalid_patch_types_rejected(self):
        for prop, value in (("compatibleJavaMajors", [True]), ("compatibleJavaMajors", [0]),
                            ("+traits", "legacyLaunch"), ("+jvmArgs", [2]), ("libraries", ["bad"])):
            with self.subTest(prop=prop, value=value), self.assertRaises(audit.AuditError):
                audit.audit_archive(self.archive([("patches/x.json", {"uid": "x", prop: value})]))

    def test_duplicate_patch_uid_and_disagreeing_alias_rejected(self):
        with self.assertRaisesRegex(audit.AuditError, "Duplicate local patch"):
            audit.audit_archive(self.archive([("patches/a.json", {"uid": "x"}), ("patches/b.json", {"uid": "x"})]))
        with self.assertRaisesRegex(audit.AuditError, "Conflicting uid"):
            audit.audit_archive(self.archive([("patches/a.json", {"uid": "x", "fileId": "y"})]))

    def test_single_metadata_and_aggregate_limits_are_bounded(self):
        result = self.archive([("instance.cfg", "name=" + "a" * 50)])
        with patch.object(audit, "MAX_METADATA_BYTES", 20), self.assertRaisesRegex(audit.AuditError, "per-file"):
            audit.audit_archive(result)
        with patch.object(audit, "MAX_METADATA_TOTAL", 100), self.assertRaisesRegex(audit.AuditError, "aggregate"):
            audit.audit_archive(result)

    def test_entry_count_and_declared_total_budget(self):
        result = self.archive([("minecraft/a", "a" * 200)])
        with patch.object(audit, "MAX_ENTRIES", 1), self.assertRaisesRegex(audit.AuditError, "entry count"):
            audit.audit_archive(result)
        with patch.object(audit, "MAX_TOTAL_BYTES", 100), self.assertRaisesRegex(audit.AuditError, "total budget"):
            audit.audit_archive(result)

    def test_json_depth_nonfinite_and_text_encoding(self):
        for data in (b'{"bad":NaN}', b'\xff', b'[]', b'{"nested":[[[[0]]]]}'):
            with self.subTest(data=data), patch.object(audit, "MAX_JSON_DEPTH", 3), self.assertRaises(audit.AuditError):
                audit.parse_json(data, "test")

    def test_cfg_bom_and_first_line_are_preserved_not_executed(self):
        result = self.archive([("instance.cfg", '\ufeffname=First Line\r\nOverrideCommands=true\r\nPreLaunchCommand=never run\r\n')])
        observed = audit.audit_archive(result)
        self.assertTrue(observed["cfg"]["present"])
        self.assertFalse(observed["cfg"]["semanticsEvaluated"])

    def test_cli_rejects_same_output_without_modifying_zip(self):
        source = self.archive()
        original = source.read_bytes()
        completed = subprocess.run([sys.executable, str(SCRIPT), str(source), "--output", str(source)], capture_output=True)
        self.assertEqual(2, completed.returncode)
        self.assertEqual(original, source.read_bytes())

    def test_cli_valid_and_rejected_reports(self):
        for files, expected in (([], 0), ([("../bad", "bad")], 1)):
            with self.subTest(expected=expected):
                source = self.archive(files)
                report = self.root / "evidence" / "audit.json"
                completed = subprocess.run([sys.executable, str(SCRIPT), str(source), "--output", str(report)], capture_output=True)
                self.assertEqual(expected, completed.returncode, completed.stderr)
                observed = json.loads(report.read_text(encoding="utf-8"))
                self.assertEqual("not-evaluated", observed["compatibilityStatus"])
                self.assertEqual("rejected" if expected else "metadata-audited", observed["auditStatus"])


if __name__ == "__main__":
    unittest.main(verbosity=2)
