"""Art runner failure/freshness contracts with synthetic assets; no Godot runs."""
import importlib.util
import hashlib
import json
from pathlib import Path
import struct
import subprocess
import tempfile
import unittest
from unittest.mock import patch
import zlib

MODULE = Path(__file__).resolve().parents[2] / "tools/art/check_art.py"
SPEC = importlib.util.spec_from_file_location("check_art", MODULE)
art = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(art)


def write_png(path, width=1280, height=800):
    def chunk(kind, data):
        return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data) & 0xFFFFFFFF)
    raw = b"\0" * (height * (width * 4 + 1))
    path.write_bytes(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)) + chunk(b"IDAT", zlib.compress(raw)) + chunk(b"IEND", b""))


class ArtCheckTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name).resolve()
        self.asset = "colony_hub"
        self.run_dir = self.root / "artifacts/art-check/test-run"
        # Fill the declared runtime inputs and subsequently discovered sidecars.
        # Authoring sources deliberately remain absent to exercise that boundary.
        for _ in range(3):
            for item in art.collect_inputs(self.root, self.asset):
                if item["state"] == "missing" and item["role"] != "source":
                    path = self.root / item["path"]
                    path.parent.mkdir(parents=True, exist_ok=True)
                    path.write_text("fixture\n", encoding="utf-8")
        self.set_import(self.root / "game/assets/models/studies/colony_hub/colony_hub_study.glb", self.root / "game/.godot/imported/hub-fixture.scn")
        self.engine_path = self.root / "fake-engine.exe"
        self.engine_path.write_bytes(b"synthetic engine")
        self.engine = {"executable": str(self.engine_path), "version": "4.7.stable.mono.fixture", "pin": "4.7", "sha256": art.digest(self.engine_path)}
        self.build = {"command": ["dotnet", "build", "game/Stratezone.csproj"], "exit_code": 0, "source_inputs": art.build_sources(self.root), "log": "synthetic successful build"}

    def tearDown(self):
        self.temp.cleanup()

    def set_import(self, source, cache):
        cache.parent.mkdir(parents=True, exist_ok=True)
        cache.write_bytes(b"synthetic imported scene")
        source_resource = "res://" + source.relative_to(self.root / "game").as_posix()
        cache_resource = "res://" + cache.relative_to(self.root / "game").as_posix()
        sidecar = Path(str(source) + ".import")
        sidecar.write_text(f'path="{cache_resource}"\nsource_file="{source_resource}"\n')
        stamp = cache.with_suffix(".md5")
        stamp.write_text(f'source_md5="{hashlib.md5(source.read_bytes()).hexdigest()}"\ndest_md5="{hashlib.md5(cache.read_bytes()).hexdigest()}"\n')
        settings_cache = self.root / art.IMPORT_CACHE
        settings_cache.parent.mkdir(parents=True, exist_ok=True)
        directory = source_resource.rsplit("/", 1)[0] + "/"
        settings_cache.write_text(f'::' + directory + '::timestamp\n' + source.name + '::PackedScene::uid::time::time::1::::<><><>0<>0<>' + hashlib.md5(sidecar.read_bytes()).hexdigest() + '<>' + cache_resource + '::\n')

    def fake_launch(self, command, root, timeout):
        if command[0] == "git":
            return subprocess.CompletedProcess(command, 0, "fixture-head\n" if "rev-parse" in command else "")
        args = json.loads(command[command.index("-UserArgsJson") + 1])
        output = Path(args[1])
        metadata = {"asset": self.asset, "godot_version": {"string": "4.7-stable"}, "rendering": {"method": "forward_plus", "adapter": "fixture GPU"}, "captures": []}
        for name in art.ASSETS[self.asset]["captures"]:
            write_png(output / (name + ".png"))
            metadata["captures"].append({"file": name + ".png", "viewport": [1280, 800], "camera": {"projection": "orthographic", "size_m": 13.125, "position_m": [11, 20, 14], "rotation_degrees": [-45, 38, 0]}})
        (output / "capture_metadata.json").write_text(json.dumps(metadata), encoding="utf-8")
        return subprocess.CompletedProcess(command, 0, "ART_CHECK_PASS: " + self.asset + "\n")

    def run_fixture(self, callback=None):
        with patch.object(art, "launch", side_effect=callback or self.fake_launch), patch("builtins.print"):
            return art.run_asset(self.root, self.run_dir, self.asset, "pwsh", self.engine, "forward_plus", 120, self.build)

    def receipt(self):
        return self.run_dir / self.asset / "receipt.json"

    def test_receipt_passes_with_unavailable_source_and_detects_staleness(self):
        receipt = self.run_fixture()
        self.assertEqual("passed", receipt["status"])
        self.assertTrue(receipt["source_notes"])
        self.assertTrue(receipt["unperformed_checks"])
        self.assertEqual([], art.check_receipt(self.root, self.receipt()))
        (self.root / "tools/art/capture_check.gd").write_text("changed helper")
        self.assertIn("Receipt is stale", "\n".join(art.check_receipt(self.root, self.receipt())))

    def test_new_transitive_dependency_invalidates_receipt(self):
        scene = self.root / "game/scenes/studies/ColonyHubStudy.tscn"
        material = self.root / "game/assets/new.tres"
        material.parent.mkdir(parents=True, exist_ok=True)
        material.write_text("original material")
        scene.write_text('path="res://assets/new.tres"')
        self.run_fixture()
        material.write_text("edited material")
        self.assertIn("Receipt is stale", "\n".join(art.check_receipt(self.root, self.receipt())))

    def test_new_dependency_added_after_capture_invalidates_receipt(self):
        self.run_fixture()
        (self.root / "game/scenes/studies/ColonyHubStudy.tscn").write_text('path="res://assets/new_material.tres"')
        material = self.root / "game/assets/new_material.tres"
        material.parent.mkdir(parents=True, exist_ok=True)
        material.write_text("new material")
        self.assertIn("Receipt is stale", "\n".join(art.check_receipt(self.root, self.receipt())))

    def test_resource_import_and_cache_are_hashed(self):
        cache = self.root / "game/.godot/imported/hub.scn"
        self.set_import(self.root / "game/assets/models/studies/colony_hub/colony_hub_study.glb", cache)
        self.run_fixture()
        cache.write_bytes(b"updated import cache")
        self.assertIn("Receipt is stale", "\n".join(art.check_receipt(self.root, self.receipt())))

    def test_raw_export_changed_without_import_blocks_verifier(self):
        (self.root / "game/assets/models/studies/colony_hub/colony_hub_study.glb").write_bytes(b"new export without reimport")
        def git_only(command, root, timeout):
            self.assertEqual("git", command[0])
            return subprocess.CompletedProcess(command, 0, "")
        failures = "\n".join(self.run_fixture(git_only)["failures"])
        self.assertIn("stale export/import cache", failures)
        self.assertIn("godot.ps1 -Import", failures)

    def test_import_settings_changed_without_import_blocks_verifier(self):
        sidecar = self.root / "game/assets/models/studies/colony_hub/colony_hub_study.glb.import"
        sidecar.write_text(sidecar.read_text() + "nodes/root_scale=2.0\n")
        def git_only(command, root, timeout):
            self.assertEqual("git", command[0])
            return subprocess.CompletedProcess(command, 0, "")
        self.assertIn("import settings are stale", "\n".join(self.run_fixture(git_only)["failures"]))

    def test_missing_runtime_blocks_without_launching_godot(self):
        (self.root / "game/assets/models/studies/colony_hub/colony_hub_study.glb").unlink()
        def git_only(command, root, timeout):
            self.assertEqual("git", command[0])
            return subprocess.CompletedProcess(command, 0, "")
        receipt = self.run_fixture(git_only)
        self.assertEqual("failed", receipt["status"])
        self.assertIsNone(receipt["exit_code"])
        self.assertIn("missing", "\n".join(receipt["failures"]))
        self.assertTrue((self.receipt().parent / "log.txt").is_file())

    def test_runtime_lfs_pointer_blocks_but_source_pointer_is_unverified(self):
        pointer = art.LFS_HEADER + b"\noid sha256:" + b"a" * 64 + b"\nsize 1000\n"
        runtime = self.root / "game/assets/models/studies/colony_hub/colony_hub_study.glb"
        source = self.root / "art/buildings/colony_hub/colony_hub_study.blend"
        source.write_bytes(pointer)
        blocked, notes = art.input_problems(art.collect_inputs(self.root, self.asset))
        self.assertFalse(blocked)
        self.assertIn("unverified", "\n".join(notes))
        runtime.write_bytes(pointer)
        blocked, _ = art.input_problems(art.collect_inputs(self.root, self.asset))
        self.assertIn("git lfs pull --include=", "\n".join(blocked))
        self.assertEqual("failed", self.run_fixture()["status"])

    def test_zero_exit_with_script_error_is_failure(self):
        def error(command, root, timeout):
            result = self.fake_launch(command, root, timeout)
            if command[0] != "git":
                result.stdout += "SCRIPT ERROR: Assertion failed\n"
            return result
        receipt = self.run_fixture(error)
        self.assertEqual("failed", receipt["status"])
        self.assertIn("assertion", "\n".join(receipt["failures"]))

    def test_build_failure_blocks_verifier_and_keeps_evidence(self):
        self.build.update(exit_code=1, log="C# compile failed")
        def git_only(command, root, timeout):
            self.assertEqual("git", command[0])
            return subprocess.CompletedProcess(command, 0, "")
        receipt = self.run_fixture(git_only)
        self.assertEqual("failed", receipt["status"])
        self.assertIn("C# build failed", "\n".join(receipt["failures"]))
        self.assertIn("C# compile failed", (self.receipt().parent / "log.txt").read_text())

    def test_csharp_changed_since_build_blocks_verifier(self):
        changed = self.root / "game/scripts/NewStudy.cs"
        changed.parent.mkdir(parents=True, exist_ok=True)
        changed.write_text("changed source after build")
        def git_only(command, root, timeout):
            self.assertEqual("git", command[0])
            return subprocess.CompletedProcess(command, 0, "")
        self.assertIn("C# sources changed since the build", "\n".join(self.run_fixture(git_only)["failures"]))

    def test_build_source_change_during_compile_is_failure(self):
        def changed(command, root, timeout):
            (self.root / "game/Stratezone.csproj").write_text("changed during compile")
            return subprocess.CompletedProcess(command, 0, "Build succeeded")
        with patch.object(art, "launch", side_effect=changed):
            build = art.build_project(self.root)
        self.assertEqual(1, build["exit_code"])
        self.assertIn("changed during build", build["log"])

    def test_missing_capture_is_failure_even_with_pass_marker(self):
        def missing(command, root, timeout):
            result = self.fake_launch(command, root, timeout)
            if command[0] != "git":
                (self.run_dir / self.asset / "godot_front.png").unlink()
            return result
        receipt = self.run_fixture(missing)
        self.assertEqual("failed", receipt["status"])
        self.assertIn("godot_front.png", "\n".join(receipt["failures"]))

    def test_timeout_produces_failed_receipt_and_log(self):
        def timeout(command, root, limit):
            if command[0] == "git":
                return subprocess.CompletedProcess(command, 0, "")
            raise subprocess.TimeoutExpired(command, limit, output=b"partial engine output")
        receipt = self.run_fixture(timeout)
        self.assertEqual("failed", receipt["status"])
        self.assertIn("timed out", "\n".join(receipt["failures"]))
        self.assertIn("partial engine output", (self.receipt().parent / "log.txt").read_text())

    def test_inputs_changed_during_run_are_not_certified(self):
        def changed(command, root, timeout):
            result = self.fake_launch(command, root, timeout)
            if command[0] != "git":
                (self.root / "tools/art/capture_check.gd").write_text("edited during capture")
            return result
        self.assertIn("Inputs changed during capture", "\n".join(self.run_fixture(changed)["failures"]))

    def test_output_tampering_and_engine_change_are_detected(self):
        self.run_fixture()
        (self.receipt().parent / "godot_front.png").write_bytes(b"truncated PNG")
        self.engine_path.write_bytes(b"different engine")
        failures = "\n".join(art.check_receipt(self.root, self.receipt()))
        self.assertIn("missing/invalid capture", failures)
        self.assertIn("engine executable", failures)

    def test_output_confinement_and_repeat_run_cannot_overwrite(self):
        with self.assertRaises(ValueError):
            art.run_asset(self.root, self.root / "art/buildings/colony_hub/previews", self.asset, "pwsh", self.engine, "forward_plus", 120, self.build)
        with self.assertRaises(ValueError):
            art.confined(self.root, self.root / "artifacts/art-check/../escaped", "artifacts/art-check")
        self.run_fixture()
        with self.assertRaises(FileExistsError):
            self.run_fixture()

    def test_symlinked_output_base_cannot_escape_repository(self):
        with tempfile.TemporaryDirectory() as external:
            artifacts = self.root / "artifacts"
            artifacts.mkdir()
            try:
                (artifacts / "art-check").symlink_to(external, target_is_directory=True)
            except OSError as error:
                self.skipTest("Directory symlinks unavailable: " + str(error))
            with self.assertRaisesRegex(ValueError, "base escapes"):
                art.confined(self.root, artifacts / "art-check/run/asset", "artifacts/art-check")


if __name__ == "__main__":
    unittest.main()
