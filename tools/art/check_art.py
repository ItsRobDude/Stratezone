#!/usr/bin/env python3
"""Check existing art exports and capture fresh, attributable Godot study views.

Builds current C# once before checking existing exports. No Blender execution,
asset rebuilding, LFS downloads, or visual approval. Python stdlib only; launch
every engine operation through the pinned Godot wrapper.
"""
from __future__ import annotations

import argparse
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import re
import shutil
import struct
import subprocess
import sys
import uuid
import zlib

REPO = Path(__file__).resolve().parents[2]
SCHEMA = 1
LFS_HEADER = b"version https://git-lfs.github.com/spec/v1"
RESOURCE = re.compile(r'''res://[^"\s')]+''')
ERROR = re.compile(r"SCRIPT ERROR|USER ERROR|^\s*ERROR:|Unhandled exception|System\.[A-Za-z.]*Exception|No loader found for resource", re.M)
IMPORT_CACHE = "game/.godot/editor/filesystem_cache10"

ASSETS = {
    "colony_hub": {
        "scene": "ColonyHubStudy", "verifier": "verify_colony_hub_study.gd",
        "groups": ["hub"],
        "captures": ["godot_front", "godot_rear", "godot_far", "godot_enemy"],
        "checks": "one unscaled static mesh, four linear vertex-color materials, source metric bounds, triangles and Hub budget",
    },
    "rifleman": {
        "scene": "RiflemanStudy", "verifier": "verify_rifleman_study.gd",
        "groups": ["hub", "infantry"],
        "captures": ["godot_hub_scale", "godot_reference", "godot_rifle_aim_idle", "godot_rifle_run", "godot_enemy"],
        "checks": "25-bone shared skeleton, one mesh/surface, team shader, three looping clips, track bindings and sampled leg motion",
    },
    "rover": {
        "scene": "RoverScaleStudy", "verifier": "verify_rover_scale_study.gd",
        "groups": ["hub", "infantry", "rover"],
        "captures": ["all_three", "hub_entrance", "rover_reference", "rover_rear", "rover_side", "rover_motion_red", "radar_scan"],
        "checks": "unscaled units, radar replaces gun, radar clearance/motion, wheel pivots/MultiMesh and preview motion",
    },
    "trees": {
        "scene": "TreePropsStudy", "verifier": "verify_tree_props.gd",
        "groups": ["trees", "infantry"],
        "captures": ["lineup", "pines", "cherries", "cherries_rear", "pines_rear", "far_lineup"],
        "checks": "four unscaled static props, one mesh/surface each, shared material, source metric heights/triangles, ground origins and MultiMesh compatibility",
    },
}
GROUPS = {
    "hub": ("art/buildings/colony_hub", "game/assets/models/studies/colony_hub", ["build_colony_hub_study.py"]),
    "infantry": ("art/units/rifleman", "game/assets/models/studies/rifleman", ["build_rifleman_study.py", "finalize_rifleman_study.py", "rig_infantry_study.py", "infantry_study_common.py"]),
    "rover": ("art/units/rover", "game/assets/models/studies/rover", ["build_rover_study.py", "rover_study_common.py"]),
    "trees": ("art/props/trees", "game/assets/models/props/trees", ["build_tree_props.py"]),
}
SOURCE_NAMES = {
    "hub": ["colony_hub_study.blend"], "rover": ["rover_study.blend"], "trees": ["tree_props.blend"],
    "infantry": ["rifleman_study.blend"],
}
EXPORT_NAMES = {
    "hub": ["colony_hub_study.glb"], "rover": ["rover_study.glb"],
    "trees": ["pine_a.glb", "pine_b.glb", "cherry_a.glb", "cherry_b.glb"],
    "infantry": ["rifleman_study.glb", "infantry_anims.glb"],
}
UNCHECKED = [
    "Human approval of likeness, polish, silhouette, faction readability and captured views",
    "Blender source/export equivalence and complete animation/clearance review",
    "Mission integration, collision/footprints and full mission playthrough",
    "Steam Deck hardware performance and actual terrain/crowd readability",
]


def digest(path: Path) -> str:
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def confined(root: Path, path: Path, base: str = "") -> Path:
    """Resolve links as well as '..'; neither inputs nor outputs may escape."""
    repository = root.resolve()
    allowed = (repository / base).resolve()
    if not allowed.is_relative_to(repository):
        raise ValueError(f"Allowed output base escapes {repository}: {root / base}")
    resolved = path.resolve()
    if not resolved.is_relative_to(allowed) or resolved == allowed:
        raise ValueError(f"Path escapes {allowed}: {path}")
    return resolved


def snapshot(root: Path, relative: str, role: str) -> dict:
    path = confined(root, root / relative)
    result = {"path": relative, "role": role, "state": "missing"}
    if not path.is_file():
        return result
    result.update(state="present", bytes=path.stat().st_size, sha256=digest(path))
    with path.open("rb") as stream:
        header = stream.read(512)
    if header.startswith(LFS_HEADER):
        result["state"] = "lfs_pointer"
        match = re.search(rb"oid sha256:([a-f0-9]{64})", header)
        result["lfs_oid"] = match.group(1).decode() if match else None
    return result


def collect_inputs(root: Path, asset: str) -> list[dict]:
    """Follow textual resource references and persistent import sidecars.

    Dynamic resource templates expand to existing files; explicit export lists
    keep missing variants visible. Hash actual imported caches and the C# DLL,
    since these are the resources Godot consumes rather than just source text.
    """
    seeds = {
        "tools/art/check_art.py": "tool", "tools/art/capture_check.gd": "tool",
        "tools/art/" + ASSETS[asset]["verifier"]: "tool", "tools/godot.ps1": "tool",
        "game/project.godot": "config", "game/Stratezone.csproj": "config",
        "game/scenes/studies/" + ASSETS[asset]["scene"] + ".tscn": "runtime",
    }
    for name in ("Stratezone.dll", "Stratezone.deps.json", "Stratezone.runtimeconfig.json"):
        seeds["game/.godot/mono/temp/bin/Debug/" + name] = "compiled"
    for group in ASSETS[asset]["groups"]:
        art_dir, runtime_dir, scripts = GROUPS[group]
        metrics = "tree_metrics.json" if group == "trees" else "study_metrics.json"
        seeds[art_dir + "/" + metrics] = "metrics"
        for name in SOURCE_NAMES[group]:
            seeds[art_dir + "/" + name] = "source"
        for name in scripts:
            seeds["tools/art/" + name] = "recipe"
        for name in EXPORT_NAMES[group]:
            seeds[runtime_dir + "/" + name] = "runtime"
        for path in (root / runtime_dir).glob("*"):
            if path.is_file():
                seeds[path.relative_to(root).as_posix()] = "runtime"
    if "infantry" in ASSETS[asset]["groups"]:
        for name in ("shared_infantry.blend", "infantry_base_upload.obj", "mixamo/infantry_base_tpose.fbx", "mixamo/rifle_aim_idle_source.fbx", "mixamo/rifle_idle_source.fbx", "mixamo/rifle_run_source.fbx"):
            seeds["art/units/shared_infantry/" + name] = "source"
    pending = list(seeds.items())
    inputs = {}
    while pending:
        relative, role = pending.pop()
        if relative in inputs:
            continue
        item = snapshot(root, relative, role)
        inputs[relative] = item
        if item["state"] != "present" or role in ("source", "recipe"):
            continue
        path = root / relative
        if path.suffix in (".glb", ".png", ".fbx", ".gltf", ".ogg", ".wav", ".ttf"):
            pending.append((relative + ".import", "import"))
        if path.suffix in (".gd", ".gdshader", ".cs") and Path(str(path) + ".uid").exists():
            pending.append((relative + ".uid", "uid"))
        if path.suffix not in (".tscn", ".tres", ".import", ".gd", ".cs", ".gdshader"):
            continue
        for reference in RESOURCE.findall(path.read_text(encoding="utf-8-sig")):
            if reference.startswith("res://../artifacts/"):
                continue  # verifier output locations, never loadable input resources
            # Quoted resources may live outside game/, but stay inside the repo.
            dependency = (root / "game" / reference[6:]).resolve()
            confined(root, dependency)
            dependency_name = dependency.relative_to(root).as_posix()
            if "{" in dependency_name:
                pattern = re.sub(r"\{[^}]+\}", "*", dependency_name)
                matches = list(root.glob(pattern))
                if not matches:
                    pending.append((pattern, "runtime"))
                else:
                    pending.extend((p.relative_to(root).as_posix(), "runtime") for p in matches)
            elif dependency_name != "game/project.godot" or role != "config":
                pending.append((dependency_name, "cache" if "/.godot/" in dependency_name else "runtime"))
                if path.suffix == ".import" and "/.godot/imported/" in dependency_name:
                    pending.append((Path(dependency_name).with_suffix(".md5").as_posix(), "cache"))
    # Source stamps let a receipt become stale when current C# differs from the
    # captured DLL. Unrelated resource strings in those files are not study deps.
    for path in (root / "game").rglob("*.cs"):
        if not {".godot", "obj", "bin"}.intersection(path.relative_to(root / "game").parts):
            relative = path.relative_to(root).as_posix()
            if relative not in inputs:
                inputs[relative] = snapshot(root, relative, "code")
    return [inputs[key] for key in sorted(inputs)]


def import_settings(root: Path) -> dict[str, str]:
    """Read the pinned Godot 4.7 editor's per-resource import settings stamp.

    Keep this bounded to the cache10 format; missing or changed formats require
    an explicit editor import rather than silently claiming fresh settings.
    """
    path = confined(root, root / IMPORT_CACHE)
    stamps, directory = {}, ""
    if not path.is_file():
        return stamps
    for line in path.read_text(encoding="utf-8").splitlines():
        if line.startswith("::res://"):
            directory = line.split("::")[1]
        elif directory:
            match = re.search(r"<>0<>0<>([a-f0-9]{32})<>", line)
            if match:
                stamps[directory + line.split("::", 1)[0]] = match.group(1)
    return stamps


def import_freshness(root: Path, inputs: list[dict]) -> tuple[list[dict], list[str]]:
    stamps = import_settings(root)
    evidence, failures = [], []
    hint = "run pwsh -NoProfile -File tools/godot.ps1 -Import explicitly, then repeat the art check"
    for item in inputs:
        if not item["path"].endswith(".import") or item["state"] != "present":
            continue
        sidecar = root / item["path"]
        text = sidecar.read_text(encoding="utf-8-sig")
        source = re.search(r'^source_file="(res://[^"\n]+)"', text, re.M)
        cache = re.search(r'^path="(res://\.godot/imported/[^"\n]+)"', text, re.M)
        if not source or not cache:
            failures.append(f"{item['path']}: missing import source/cache metadata; {hint}")
            continue
        source_path = confined(root, root / "game" / source.group(1)[6:])
        cache_path = confined(root, root / "game" / cache.group(1)[6:])
        stamp_path = cache_path.with_suffix(".md5")
        try:
            stamp_text = stamp_path.read_text(encoding="utf-8")
            raw_md5 = hashlib.md5(source_path.read_bytes()).hexdigest()
            imported_md5 = hashlib.md5(cache_path.read_bytes()).hexdigest()
            source_md5 = re.search(r'^source_md5="([a-f0-9]{32})"', stamp_text, re.M)
            dest_md5 = re.search(r'^dest_md5="([a-f0-9]{32})"', stamp_text, re.M)
            settings_md5 = hashlib.md5(sidecar.read_bytes()).hexdigest()
            recorded_settings = stamps.get(source.group(1))
            evidence.append({"source": source.group(1), "source_md5": raw_md5, "cache_md5": imported_md5, "settings_md5": settings_md5, "recorded_settings_md5": recorded_settings})
            if not source_md5 or source_md5.group(1) != raw_md5 or not dest_md5 or dest_md5.group(1) != imported_md5:
                failures.append(f"{item['path']}: stale export/import cache stamp; {hint}")
            if recorded_settings != settings_md5:
                failures.append(f"{item['path']}: import settings are stale or unverified in {IMPORT_CACHE}; {hint}")
        except OSError:
            failures.append(f"{item['path']}: missing export/import cache/stamp; {hint}")
    return evidence, failures


def input_problems(inputs: list[dict]) -> tuple[list[str], list[str]]:
    blocked, source_notes = [], []
    for item in inputs:
        if item["state"] == "present":
            continue
        path = item["path"]
        note = f"{path}: {item['state']}"
        if item["role"] == "source":
            source_notes.append(note + " (authoring source remains unverified)")
        elif item["state"] == "lfs_pointer":
            blocked.append(note + f'; fetch only the needed runtime file with git lfs pull --include="{path}" --exclude=""')
        else:
            blocked.append(note + "; restore the input, then build/import explicitly if it is generated")
    return blocked, source_notes


def png_size(path: Path) -> tuple[int, int]:
    data = path.read_bytes()
    if not data.startswith(b"\x89PNG\r\n\x1a\n"):
        raise ValueError("invalid PNG signature")
    offset, dimensions, compressed, ended = 8, None, bytearray(), False
    while offset + 12 <= len(data):
        length = struct.unpack_from(">I", data, offset)[0]
        kind = data[offset + 4:offset + 8]
        payload = data[offset + 8:offset + 8 + length]
        if offset + length + 12 > len(data):
            raise ValueError("truncated PNG")
        crc = struct.unpack_from(">I", data, offset + 8 + length)[0]
        if zlib.crc32(kind + payload) & 0xFFFFFFFF != crc:
            raise ValueError("PNG checksum failure")
        if kind == b"IHDR":
            dimensions = struct.unpack_from(">II", payload)
        elif kind == b"IDAT":
            compressed.extend(payload)
        elif kind == b"IEND":
            ended = True
            break
        offset += length + 12
    if not ended or dimensions is None or not compressed or not zlib.decompress(compressed):
        raise ValueError("incomplete PNG")
    return dimensions


def inspect_outputs(root: Path, output: Path, asset: str, renderer: str) -> tuple[dict | None, list[dict], list[str]]:
    failures, captures = [], []
    metadata = None
    try:
        metadata = json.loads(confined(root, output / "capture_metadata.json", "artifacts/art-check").read_text(encoding="utf-8"))
        names = [item["file"] for item in metadata["captures"]]
        expected = [name + ".png" for name in ASSETS[asset]["captures"]]
        if metadata["asset"] != asset or sorted(names) != sorted(expected):
            failures.append("Capture metadata asset or file set differs from the verifier contract")
        if metadata["rendering"]["method"] != renderer:
            failures.append(f"Requested {renderer}, actual renderer is {metadata['rendering']['method']}")
        if not metadata["godot_version"]["string"] or not metadata["rendering"]["adapter"]:
            failures.append("Missing actual engine/GPU capture metadata")
        for item in metadata["captures"]:
            camera = item["camera"]
            if item["viewport"] != [1280, 800] or camera["projection"] != "orthographic" or camera["size_m"] <= 0 or len(camera["position_m"]) != 3 or abs(camera["rotation_degrees"][0] + 45) > .01:
                failures.append(f"Unexpected viewport/camera in {item['file']}")
    except (OSError, ValueError, KeyError, TypeError):
        failures.append("Capture metadata is missing or invalid")
    for name in ASSETS[asset]["captures"]:
        try:
            path = confined(root, output / (name + ".png"), "artifacts/art-check")
            if png_size(path) != (1280, 800):
                raise ValueError("expected 1280x800")
            captures.append({"file": name + ".png", "bytes": path.stat().st_size, "sha256": digest(path)})
        except (OSError, ValueError, struct.error, zlib.error) as error:
            failures.append(f"{name}.png: missing/invalid capture ({error})")
    return metadata, captures, failures


def launch(command: list[str], root: Path, timeout: int) -> subprocess.CompletedProcess:
    return subprocess.run(command, cwd=root, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True, encoding="utf-8", errors="replace", timeout=timeout)


def engine_info(root: Path, shell: str) -> dict:
    command = [shell, "-NoProfile", "-File", str(root / "tools/godot.ps1"), "-Which"]
    result = launch(command, root, 30)
    if result.returncode:
        raise ValueError(result.stdout.strip())
    exe = re.search(r"^Godot: (.+)$", result.stdout, re.M)
    version = re.search(r"^Version: (.+?) \(pin: Godot.NET.Sdk (.+?)\)$", result.stdout, re.M)
    if not exe or not version:
        raise ValueError("Cannot parse tools/godot.ps1 -Which output: " + result.stdout)
    path = Path(exe.group(1).strip())
    return {"executable": str(path), "version": version.group(1), "pin": version.group(2), "sha256": digest(path)}


def build_sources(root: Path) -> list[dict]:
    paths = [root / "game/Stratezone.csproj"]
    paths.extend(path for path in (root / "game").rglob("*.cs") if not {".godot", "obj", "bin"}.intersection(path.relative_to(root / "game").parts))
    return [snapshot(root, path.relative_to(root).as_posix(), "code") for path in sorted(paths)]


def build_project(root: Path) -> dict:
    command = ["dotnet", "build", str(root / "game/Stratezone.csproj")]
    sources = build_sources(root)
    try:
        result = launch(command, root, 180)
        log, code = result.stdout, result.returncode
        if sources != build_sources(root):
            log += "\nC# source inputs changed during build; repeat with stable sources.\n"
            code = 1
        return {"command": command, "exit_code": code, "source_inputs": sources, "log": log}
    except (OSError, subprocess.TimeoutExpired) as error:
        return {"command": command, "exit_code": None, "source_inputs": sources, "log": str(error)}


def run_asset(root: Path, run_dir: Path, asset: str, shell: str, engine: dict, renderer: str, timeout: int, build: dict) -> dict:
    output = confined(root, run_dir / asset, "artifacts/art-check")
    output.mkdir(parents=True, exist_ok=False)
    before = collect_inputs(root, asset)
    failures, source_notes = input_problems(before)
    import_evidence, import_failures = import_freshness(root, before)
    failures.extend(import_failures)
    if build.get("exit_code") != 0:
        failures.append("C# build failed or timed out; verifier was not launched")
    elif build.get("source_inputs") != build_sources(root):
        failures.append("C# sources changed since the build; verifier was not launched")
    if engine.get("status") == "unavailable":
        failures.append("Pinned Godot is unavailable: " + engine["reason"])
    command = [shell, "-NoProfile", "-File", str(root / "tools/godot.ps1"), "-Check", "-TimeoutSeconds", str(timeout), "-Script", "res://../tools/art/" + ASSETS[asset]["verifier"], "-UserArgsJson", json.dumps(["--output-dir", str(output)]), "--resolution", "1280x800", "--rendering-method", renderer]
    log, exit_code, metadata, captures = "Preflight blocked; Godot was not launched.\n", None, None, []
    if not failures:
        try:
            result = launch(command, root, timeout + 30)
            log, exit_code = result.stdout, result.returncode
        except (OSError, subprocess.TimeoutExpired) as error:
            log = str(error)
            if isinstance(error, subprocess.TimeoutExpired) and error.stdout:
                log += "\n" + (error.stdout.decode(errors="replace") if isinstance(error.stdout, bytes) else error.stdout)
            failures.append("Launcher failed or timed out; see log.txt")
        if exit_code != 0:
            failures.append(f"Godot launcher returned {exit_code}")
        if ERROR.search(log):
            failures.append("Engine, assertion or C# error found in log")
        if f"ART_CHECK_PASS: {asset}" not in log:
            failures.append("Verifier completion marker missing")
        metadata, captures, capture_failures = inspect_outputs(root, output, asset, renderer)
        failures.extend(capture_failures)
        if collect_inputs(root, asset) != before:
            failures.append("Inputs changed during capture; repeat after changes/imports finish")
        if import_freshness(root, before)[0] != import_evidence:
            failures.append("Import freshness stamps changed during capture; repeat with stable imports")
    (output / "log.txt").write_text("C# build:\n" + build["log"] + "\nGodot verification:\n" + log, encoding="utf-8")
    git = launch(["git", "rev-parse", "HEAD"], root, 10)
    status = launch(["git", "status", "--porcelain"], root, 10)
    receipt = {
        "schema": SCHEMA, "asset": asset, "created_utc": datetime.now(timezone.utc).isoformat(),
        "status": "passed" if not failures else "failed", "failures": failures,
        "repository": str(root), "revision": git.stdout.strip(), "working_tree_dirty": bool(status.stdout.strip()),
        "engine": engine, "build": build, "command": command, "requested_renderer": renderer,
        "exit_code": exit_code, "inputs": before, "import_evidence": import_evidence, "capture_metadata": metadata, "captures": captures,
        "log_sha256": digest(output / "log.txt"),
        "capture_metadata_sha256": digest(output / "capture_metadata.json") if (output / "capture_metadata.json").is_file() else None,
        "structural_checks": ASSETS[asset]["checks"] if not failures else "Not established; inspect failures and log",
        "source_notes": source_notes, "unperformed_checks": UNCHECKED,
    }
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    print(f"{asset}: {receipt['status'].upper()} - {output / 'receipt.json'}")
    for failure in failures:
        print("  " + failure)
    for note in source_notes:
        print("  " + note)
    return receipt


def check_receipt(root: Path, path: Path) -> list[str]:
    path = confined(root, path, "artifacts/art-check")
    receipt = json.loads(path.read_text(encoding="utf-8"))
    if receipt.get("schema") != SCHEMA or receipt.get("asset") not in ASSETS:
        raise ValueError("Unsupported art receipt schema/asset")
    failures = []
    if receipt["status"] != "passed":
        failures.append("Receipt records a failed run")
    if receipt["inputs"] != collect_inputs(root, receipt["asset"]):
        failures.append("Receipt is stale: input hashes, availability or dependency set changed")
    evidence, import_failures = import_freshness(root, collect_inputs(root, receipt["asset"]))
    failures.extend(import_failures)
    if evidence != receipt["import_evidence"]:
        failures.append("Receipt is stale: import freshness evidence changed")
    _, captures, output_failures = inspect_outputs(root, path.parent, receipt["asset"], receipt["requested_renderer"])
    failures.extend(output_failures)
    if captures != receipt["captures"]:
        failures.append("Capture hashes differ from the receipt")
    for name, key in (("log.txt", "log_sha256"), ("capture_metadata.json", "capture_metadata_sha256")):
        artifact = confined(root, path.parent / name, "artifacts/art-check")
        if not artifact.is_file() or digest(artifact) != receipt.get(key):
            failures.append(f"{name} is missing or changed")
    engine = Path(receipt["engine"].get("executable", ""))
    if not engine.is_file() or digest(engine) != receipt["engine"].get("sha256"):
        failures.append("Recorded engine executable is missing or changed")
    return failures


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    choice = parser.add_mutually_exclusive_group(required=True)
    choice.add_argument("--asset", choices=ASSETS)
    choice.add_argument("--all", action="store_true")
    choice.add_argument("--check-receipt", type=Path, metavar="PATH", help="verify receipt inputs and outputs still match; does not run Godot")
    parser.add_argument("--renderer", choices=("forward_plus", "mobile", "gl_compatibility"), default="forward_plus")
    parser.add_argument("--timeout-seconds", type=int, default=120, help="bounded launcher timeout per asset (default: 120)")
    args = parser.parse_args(argv)
    try:
        if args.check_receipt:
            failures = check_receipt(REPO, args.check_receipt)
            print("\n".join(failures) if failures else "PASS: receipt inputs and captures are current; human and Deck checks remain unperformed")
            return 1 if failures else 0
        if args.timeout_seconds < 1:
            raise ValueError("--timeout-seconds must be positive")
        shell = shutil.which("pwsh")
        if not shell:
            raise ValueError("PowerShell 7 (pwsh) is required by tools/godot.ps1")
        run_id = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ") + "-" + uuid.uuid4().hex[:8]
        run_dir = confined(REPO, REPO / "artifacts/art-check" / run_id, "artifacts/art-check")
        build = build_project(REPO)
        try:
            engine = engine_info(REPO, shell)
        except (OSError, ValueError, subprocess.SubprocessError) as error:
            engine = {"status": "unavailable", "reason": str(error)}
        receipts = [run_asset(REPO, run_dir, asset, shell, engine, args.renderer, args.timeout_seconds, build) for asset in (ASSETS if args.all else [args.asset])]
        return 1 if any(receipt["status"] != "passed" for receipt in receipts) else 0
    except (OSError, ValueError, KeyError, TypeError, subprocess.SubprocessError) as error:
        print("FAIL: " + str(error), file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
