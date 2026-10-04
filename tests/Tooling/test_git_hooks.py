"""Exercise the pre-commit hook against real staged changes in disposable repositories."""

from __future__ import annotations

import contextlib
import importlib.util
import io
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch


ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location("pre_commit", ROOT / "tools/git_hooks/pre_commit.py")
HOOK = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(HOOK)


class PreCommitDeletionTests(unittest.TestCase):
    def setUp(self):
        self.scratch_root = (ROOT / "artifacts/tooling-tests").resolve()
        self.scratch_root.mkdir(parents=True, exist_ok=True)
        self.temp = tempfile.TemporaryDirectory(prefix="hooks-", dir=self.scratch_root)
        self.repo = Path(self.temp.name).resolve()
        self.root_patch = patch.object(HOOK, "ROOT", self.repo)
        self.root_patch.start()
        self.git("init", "-q")
        for name in ("game/data/example.json", ".claude/settings.local.json", "notes.txt"):
            path = self.repo / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("{}", encoding="utf-8")
        self.git("add", ".")
        self.git("-c", "user.name=Tooling Fixture", "-c", "user.email=fixture@example.invalid",
                 "-c", "commit.gpgsign=false", "commit", "-qm", "fixture")

    def tearDown(self):
        self.root_patch.stop()
        if not self.repo.is_relative_to(self.scratch_root):
            raise AssertionError("Scratch cleanup escaped the intended artifacts directory")
        self.temp.cleanup()

    def git(self, *args):
        return subprocess.run(["git", *args], cwd=self.repo, check=True, capture_output=True)

    def delete(self, path):
        (self.repo / path).unlink()
        self.git("add", "-u")

    def test_data_deletion_runs_both_validators_and_blocks_invalid_content(self):
        self.delete("game/data/example.json")
        with patch.object(HOOK, "run_validator", return_value=(False, "missing referenced record")) as validator:
            with contextlib.redirect_stderr(io.StringIO()) as stderr:
                self.assertEqual(HOOK.main(), 1)
        self.assertEqual(validator.call_count, 2)
        self.assertIn("missing referenced record", stderr.getvalue())

    def test_deleted_blob_is_not_sent_to_cat_file(self):
        self.delete("game/data/example.json")
        with patch.object(HOOK, "run_validator", return_value=(True, "")):
            with patch.object(HOOK, "blob_sizes", wraps=HOOK.blob_sizes) as sizes:
                self.assertEqual(HOOK.main(), 0)
        sizes.assert_called_once_with([])

    def test_removing_local_state_is_allowed(self):
        self.delete(".claude/settings.local.json")
        with contextlib.redirect_stderr(io.StringIO()) as stderr:
            self.assertEqual(HOOK.main(), 0)
        self.assertNotIn("blocked", stderr.getvalue())

    def test_other_deletion_does_not_run_content_validation(self):
        self.delete("notes.txt")
        with patch.object(HOOK, "run_validator") as validator:
            self.assertEqual(HOOK.main(), 0)
        validator.assert_not_called()


if __name__ == "__main__":
    unittest.main()
