"""Claude Code Stop hook: validate code, tests, and content before Claude finishes a turn.

When watched paths differ from HEAD and that change set has not been checked yet, runs the
content validators, the Godot C# build, and the simulation smoke. Exit 2 blocks the stop and
sends the failure to Claude. An unchanged failing tree is reported once, and at most
MAX_BLOCKS stops are blocked in a row, so a broken tree never traps Claude in a loop.
State lives in .claude/hook-state/ (gitignored).
"""

from __future__ import annotations

import hashlib
import json
import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
STATE_PATH = ROOT / ".claude" / "hook-state" / "stop_validate.json"
WATCHED = [
    "game/scripts",
    "game/data",
    "game/Stratezone.csproj",
    "tests",
    "tools/validate_content.py",
    "plugins/stratezone-mission-steward/scripts",
]
STEPS = (
    ("Content validation", [sys.executable, "tools/validate_content.py"]),
    ("Content drift check", [sys.executable, "plugins/stratezone-mission-steward/scripts/check_content_drift.py"]),
    ("Godot C# build", ["dotnet", "build", "game/Stratezone.csproj", "-nologo", "-v", "q"]),
    ("Simulation smoke", ["dotnet", "run", "--project", "tests/SimulationSmoke/SimulationSmoke.csproj"]),
)
MAX_BLOCKS = 3


def git_bytes(*args: str) -> bytes:
    return subprocess.run(["git", *args], cwd=ROOT, capture_output=True, check=True).stdout


def fingerprint() -> str | None:
    """Hash of watched changes relative to HEAD, or None when nothing watched has changed."""
    diff = git_bytes("diff", "HEAD", "--binary", "--", *WATCHED)
    untracked = [
        path
        for path in git_bytes("ls-files", "--others", "--exclude-standard", "-z", "--", *WATCHED)
        .decode("utf-8", errors="replace")
        .split("\0")
        if path
    ]
    if not diff and not untracked:
        return None
    digest = hashlib.sha256(diff)
    for path in sorted(untracked):
        digest.update(path.encode("utf-8"))
        try:
            digest.update((ROOT / path).read_bytes())
        except OSError:
            pass
    return digest.hexdigest()


def load_state() -> dict:
    try:
        return json.loads(STATE_PATH.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        return {}


def save_state(state: dict) -> None:
    STATE_PATH.parent.mkdir(parents=True, exist_ok=True)
    STATE_PATH.write_text(json.dumps(state), encoding="utf-8")


def run_steps() -> tuple[str, str] | None:
    """Run each step in order; return (step name, output tail) for the first failure."""
    for name, command in STEPS:
        result = subprocess.run(command, cwd=ROOT, capture_output=True)
        if result.returncode != 0:
            output = (result.stdout + result.stderr).decode("utf-8", errors="replace").strip()
            return name, "\n".join(output.splitlines()[-40:])
    return None


def main() -> int:
    sys.stdin.buffer.read()  # hook payload is not needed; drain it
    current = fingerprint()
    if current is None:
        return 0

    state = load_state()
    if current in (state.get("last_pass"), state.get("last_fail")):
        return 0

    failure = run_steps()
    if failure is None:
        save_state({"last_pass": current, "blocks": 0})
        return 0

    name, tail = failure
    blocks = int(state.get("blocks", 0)) + 1
    save_state({"last_fail": current, "blocks": blocks})
    if blocks > MAX_BLOCKS:
        print(json.dumps({"systemMessage": f"Stratezone stop check: {name} still failing; not blocking again."}))
        return 0

    sys.stderr.reconfigure(encoding="utf-8", errors="replace")
    print(f"Stratezone stop check failed at {name}. Fix it or explain why before finishing:\n{tail}", file=sys.stderr)
    return 2


if __name__ == "__main__":
    sys.exit(main())
