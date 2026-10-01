"""Claude Code PostToolUse hook: re-run content checks after Claude edits game data.

Runs tools/validate_content.py and the mission drift check when an Edit/Write/MultiEdit
touched a JSON file under game/data/. Exit 2 reports the failure back to Claude; the edit
itself has already happened. Failures can be transient mid-way through a multi-file change.
"""

from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
VALIDATORS = (
    "tools/validate_content.py",
    "plugins/stratezone-mission-steward/scripts/check_content_drift.py",
)


def edited_path(payload: dict) -> str | None:
    file_path = (payload.get("tool_input") or {}).get("file_path")
    if not file_path:
        return None
    try:
        return Path(file_path).resolve().relative_to(ROOT).as_posix()
    except ValueError:
        return None


def main() -> int:
    try:
        payload = json.loads(sys.stdin.buffer.read().decode("utf-8", errors="replace"))
    except ValueError:
        return 0

    relative = edited_path(payload)
    if not relative or not (relative.startswith("game/data/") and relative.endswith(".json")):
        return 0

    failures: list[str] = []
    for validator in VALIDATORS:
        result = subprocess.run([sys.executable, validator], cwd=ROOT, capture_output=True)
        if result.returncode != 0:
            output = (result.stdout + result.stderr).decode("utf-8", errors="replace").strip()
            failures.append(f"{validator}:\n{output}")

    if not failures:
        return 0

    sys.stderr.reconfigure(encoding="utf-8", errors="replace")
    print(
        f"Content checks failed after editing {relative} "
        "(ignore if you are mid-way through a multi-file change):",
        file=sys.stderr,
    )
    for failure in failures:
        print(failure, file=sys.stderr)
    return 2


if __name__ == "__main__":
    sys.exit(main())
