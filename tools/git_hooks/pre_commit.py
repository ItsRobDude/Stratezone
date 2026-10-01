#!/usr/bin/env python3
"""Stratezone pre-commit checks, shared by every agent and person committing here.

Blocks local agent state and nested-repository gitlinks, runs the content and drift
validators when data, docs, or the validators are staged, and warns about oversized
hand-written C# files and engine import churn mixed into other work.

Validators read the working tree, so partially staged files are checked as they sit on disk.
"""

from __future__ import annotations

import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
BLOCKED_PREFIXES = (".claude/worktrees/", ".claude/hook-state/")
BLOCKED_PATHS = {".claude/settings.local.json"}
VALIDATE_TRIGGERS = (
    "game/data/",
    "docs/",
    "AGENTS.md",
    "tools/validate_content.py",
    "plugins/stratezone-mission-steward/scripts/",
)
VALIDATORS = (
    ["tools/validate_content.py"],
    ["plugins/stratezone-mission-steward/scripts/check_content_drift.py"],
)
LINE_REVIEW_TRIGGER = 900
IMPORT_CHURN_THRESHOLD = 20
GITLINK_MODE = "160000"


def git(*args: str) -> str:
    result = subprocess.run(["git", *args], cwd=ROOT, capture_output=True, check=True)
    return result.stdout.decode("utf-8", errors="replace")


def staged_entries() -> list[tuple[str, str]]:
    """(new_mode, path) for every added, copied, modified, or type-changed path."""
    raw = git("diff", "--cached", "--raw", "-z", "--no-renames", "--diff-filter=ACMT")
    parts = raw.split("\0")
    entries: list[tuple[str, str]] = []
    for meta, path in zip(parts[0::2], parts[1::2]):
        if not meta.startswith(":"):
            break
        new_mode = meta[1:].split()[1]
        entries.append((new_mode, path))
    return entries


def run_validator(args: list[str]) -> tuple[bool, str]:
    result = subprocess.run([sys.executable, *args], cwd=ROOT, capture_output=True)
    output = (result.stdout + result.stderr).decode("utf-8", errors="replace").strip()
    return result.returncode == 0, output


def line_count(path: Path) -> int:
    try:
        with path.open("rb") as handle:
            return sum(1 for _ in handle)
    except OSError:
        return 0


def main() -> int:
    entries = staged_entries()
    paths = [path for _, path in entries]
    errors: list[str] = []
    warnings: list[str] = []

    for mode, path in entries:
        if path in BLOCKED_PATHS or path.startswith(BLOCKED_PREFIXES):
            errors.append(f"{path}: local agent state must not be committed (see .gitignore).")
        elif mode == GITLINK_MODE:
            errors.append(f"{path}: nested repository or worktree gitlink; unstage it with `git rm --cached {path}`.")

    if any(path.startswith(VALIDATE_TRIGGERS) for path in paths):
        for validator in VALIDATORS:
            ok, output = run_validator(validator)
            if not ok:
                errors.append(f"{validator[0]} failed:\n{output}")

    for path in paths:
        if path.endswith(".cs"):
            lines = line_count(ROOT / path)
            if lines > LINE_REVIEW_TRIGGER:
                warnings.append(
                    f"{path} has {lines} lines (review trigger is {LINE_REVIEW_TRIGGER}); split it or explain why it stays together."
                )

    churn = [path for path in paths if path.endswith((".uid", ".import"))]
    other = [path for path in paths if not path.endswith((".uid", ".import"))]
    if len(churn) >= IMPORT_CHURN_THRESHOLD and other:
        warnings.append(
            f"{len(churn)} .uid/.import files are staged with {len(other)} other files; "
            "consider committing engine import churn separately."
        )

    for warning in warnings:
        print(f"pre-commit warning: {warning}", file=sys.stderr)
    if errors:
        print("pre-commit blocked:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
