---
name: mission-steward
description: Use for Stratezone validation, launching Godot, mission truth reports for any authored mission (First Landing, Wells at the Ridge, later levels), content drift checks, and implementation closeouts that need docs/data/simulation alignment.
---

# Stratezone Mission Steward

Use this skill only inside the Stratezone checkout. This file is shared by Codex (this plugin) and Claude Code (`.claude/skills/mission-steward/SKILL.md` points here). Edit this file, not the pointer.

## Purpose

Keep agent runs aligned with Stratezone's mission-first RTS scope. The skill is intentionally narrow: it validates the repo, launches Godot safely, summarizes mission truth, and catches drift between data, docs, and simulation expectations. It must not invent mechanics, factions, lore, tools, or release scope.

## Required Repo Context

Read `AGENTS.md` first. For broad changes, read the docs in its "Source of Truth" list that the task touches. The 3D overhaul docs (`docs/3d-presentation-plan.md`, `docs/3d-art-direction.md`) own the current milestone order. For narrow changes, read `AGENTS.md` plus the smallest relevant docs and code/data files. Prefer reading the relevant sections of long docs over whole files.

## Main Commands

Run the full local validation stack (content, C# build, simulation smoke, drift check, headless Godot smoke):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\plugins\stratezone-mission-steward\scripts\validate_stratezone.ps1
```

Skip the Godot step only when the task does not need engine verification (docs- or data-only work):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\plugins\stratezone-mission-steward\scripts\validate_stratezone.ps1 -SkipGodot
```

Report what every mission offers, starts with, and fails on (or one mission with `--mission <id>`):

```powershell
python .\plugins\stratezone-mission-steward\scripts\mission_truth_report.py
python .\plugins\stratezone-mission-steward\scripts\mission_truth_report.py --mission mission_wells_at_the_ridge
```

Check locked design rules for every mission against data and docs:

```powershell
python .\plugins\stratezone-mission-steward\scripts\check_content_drift.py
```

Launch Godot only through the repo launcher. It resolves the Godot .NET build matching the `Godot.NET.Sdk` pin in `game/Stratezone.csproj` and fails on engine/C# errors even when Godot exits 0:

```powershell
pwsh -NoProfile -File tools/godot.ps1 -Which    # show resolved build and pin
pwsh -NoProfile -File tools/godot.ps1 -Smoke    # headless main-scene run, error scan
pwsh -NoProfile -File tools/godot.ps1 -Import   # headless editor import pass, error scan
pwsh -NoProfile -File tools/godot.ps1           # launch the game windowed
```

## Workflow

1. Confirm the task's milestone lane before editing. The current lane is the 3D presentation overhaul in `docs/3d-presentation-plan.md` ("Order Of Work"), alongside Level 2 (Wells at the Ridge) closeout.
2. Do not widen into Vehicle Bay production, Med Hall, Logistics / Repair Pad, Neutral Repair Platform, Artillery outside Mission 5, multiplayer, procedural content, ancient-tech lore, or release packaging unless the user explicitly asks.
3. Treat `game/data/` and `tests/SimulationSmoke/` as runtime evidence, not just docs support. If a smoke test locks in behavior the docs never chose, flag it instead of treating it as truth.
4. Run `mission_truth_report.py` before making direction calls about any mission's availability, starting roster, objectives, failure conditions, enemy pressure, triggers, fog, or hidden locks.
5. Run `check_content_drift.py` after touching mission data, unit/building data, localization keys, or source docs that define content IDs. When a doc locks a new mission rule, add the check in the same pass.
6. After any Godot editor or import run, check `git status` for `.uid`/`.import` churn and commit it separately from gameplay changes.
7. Run `validate_stratezone.ps1` before claiming implementation work is done, unless the user asked for findings only or a docs-only answer.

## Closeout Shape

Report:

- files changed
- whether the work is docs, code, data, assets, tooling, or mixed
- commands run
- manual checks performed
- behavior verified
- checks not run and why
- known risks or follow-up work
- any hand-written code files over 900 lines touched or left relevant

If validation was skipped or blocked, say exactly why.
