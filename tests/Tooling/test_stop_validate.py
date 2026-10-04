"""Stop-hook invalidation checks using real Git state; validation itself is mocked."""

from __future__ import annotations

import importlib.util
import io
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch


ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location("stop_validate", ROOT / ".claude/hooks/stop_validate.py")
HOOK = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(HOOK)


class StopValidationTests(unittest.TestCase):
    def setUp(self):
        self.scratch_root = (ROOT / "artifacts/tooling-tests").resolve()
        self.scratch_root.mkdir(parents=True, exist_ok=True)
        self.temp = tempfile.TemporaryDirectory(prefix="stop-", dir=self.scratch_root)
        self.repo = Path(self.temp.name).resolve()
        self.root_patch = patch.object(HOOK, "ROOT", self.repo)
        self.state_patch = patch.object(HOOK, "STATE_PATH", self.repo / "state.json")
        self.root_patch.start()
        self.state_patch.start()
        self.git("init", "-q")
        self.launcher = self.repo / "tools/godot.ps1"
        self.launcher.parent.mkdir()
        self.launcher.write_text("original", encoding="utf-8")
        self.git("add", ".")
        self.commit("fixture")

    def tearDown(self):
        self.root_patch.stop()
        self.state_patch.stop()
        if not self.repo.is_relative_to(self.scratch_root):
            raise AssertionError("Scratch cleanup escaped the intended artifacts directory")
        self.temp.cleanup()

    def git(self, *args):
        return subprocess.run(["git", *args], cwd=self.repo, check=True, capture_output=True)

    def commit(self, message):
        self.git("-c", "user.name=Tooling Fixture", "-c", "user.email=fixture@example.invalid",
                 "-c", "commit.gpgsign=false", "commit", "-qm", message)

    def test_changed_launcher_is_validated_once(self):
        self.launcher.write_text("changed", encoding="utf-8")
        stdin = io.TextIOWrapper(io.BytesIO(b"{}"))
        with patch.object(HOOK.sys, "stdin", stdin), patch.object(HOOK, "run_steps", return_value=None) as checks:
            self.assertEqual(HOOK.main(), 0)
            self.assertEqual(HOOK.main(), 0)
        self.assertEqual(checks.call_count, 1)

    def test_same_diff_under_new_head_invalidates_prior_pass(self):
        self.launcher.write_text("changed", encoding="utf-8")
        old = HOOK.fingerprint()
        (self.repo / "unwatched.txt").write_text("new head", encoding="utf-8")
        self.git("add", "unwatched.txt")
        self.commit("new baseline")
        self.assertNotEqual(old, HOOK.fingerprint())

    def test_untracked_main_scene_triggers_check(self):
        path = self.repo / "game/scenes/main/New.tscn"
        path.parent.mkdir(parents=True)
        path.write_text("scene", encoding="utf-8")
        self.assertIsNotNone(HOOK.fingerprint())

    def test_untracked_art_tool_triggers_check(self):
        path = self.repo / "tools/art/check_art.py"
        path.parent.mkdir(parents=True)
        path.write_text("art tool", encoding="utf-8")
        self.assertIsNotNone(HOOK.fingerprint())

    def test_clean_tree_does_not_run_validation(self):
        stdin = io.TextIOWrapper(io.BytesIO(b"{}"))
        with patch.object(HOOK.sys, "stdin", stdin), patch.object(HOOK, "run_steps") as checks:
            self.assertEqual(HOOK.main(), 0)
        checks.assert_not_called()


if __name__ == "__main__":
    unittest.main()
