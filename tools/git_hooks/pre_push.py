#!/usr/bin/env python3
"""Stratezone pre-push checks: Godot C# build and simulation smoke.

Skips when every pushed change is Markdown. The checks run against the working tree,
so uncommitted breakage can also block a push.
"""

from __future__ import annotations

import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
ZERO_SHA = "0" * 40
CHECKS = (
    ("Godot C# build", ["dotnet", "build", "game/Stratezone.csproj", "-nologo", "-v", "q"]),
    ("Simulation smoke", ["dotnet", "run", "--project", "tests/SimulationSmoke/SimulationSmoke.csproj"]),
)


def git(*args: str) -> str:
    result = subprocess.run(["git", *args], cwd=ROOT, capture_output=True, check=True)
    return result.stdout.decode("utf-8", errors="replace")


def pushed_paths(stdin_text: str) -> set[str]:
    paths: set[str] = set()
    for line in stdin_text.splitlines():
        fields = line.split()
        if len(fields) != 4:
            continue
        _, local_sha, _, remote_sha = fields
        if local_sha == ZERO_SHA:
            continue  # branch deletion
        if remote_sha == ZERO_SHA:
            listing = git("log", "--name-only", "--format=", local_sha, "--not", "--remotes")
        else:
            listing = git("diff", "--name-only", remote_sha, local_sha)
        paths.update(path for path in listing.splitlines() if path.strip())
    return paths


def main() -> int:
    paths = pushed_paths(sys.stdin.read())
    if not paths or all(path.endswith(".md") for path in paths):
        return 0

    for name, command in CHECKS:
        result = subprocess.run(command, cwd=ROOT, capture_output=True)
        if result.returncode != 0:
            output = (result.stdout + result.stderr).decode("utf-8", errors="replace").strip()
            tail = "\n".join(output.splitlines()[-40:])
            print(f"pre-push blocked: {name} failed (working tree).\n{tail}", file=sys.stderr)
            return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
