---
name: mission-steward
description: Use for Stratezone repo-local validation, First Landing mission truth reports, content drift checks, and implementation closeouts that need docs/data/simulation alignment.
---

# Stratezone Mission Steward

Use this skill only inside the Stratezone checkout.

## Purpose

Keep Codex runs aligned with Stratezone's mission-first RTS scope. The plugin is intentionally narrow: it helps validate the current repo, summarize mission truth, and catch drift between data, docs, and simulation expectations. It should not invent new mechanics, factions, lore, tools, or release scope.

## Required Repo Context

Before broad changes, read:

- `AGENTS.md`
- `docs/project-identity.md`
- `docs/technical-architecture.md`
- `docs/system-contracts.md`
- `docs/content-data-spec.md`
- `docs/implementation-checklists.md`
- `docs/first-landing-mission-spec.md`
- `docs/product-roadmap.md`

For narrow changes, read `AGENTS.md` plus the smallest relevant docs and code/data files.

## Main Commands

Run the full local validation stack:

```powershell
powershell -ExecutionPolicy Bypass -File .\plugins\stratezone-mission-steward\scripts\validate_stratezone.ps1
```

Generate a First Landing mission truth summary:

```powershell
python .\plugins\stratezone-mission-steward\scripts\mission_truth_report.py
```

Check Level 1 content/data drift:

```powershell
python .\plugins\stratezone-mission-steward\scripts\check_content_drift.py
```

Skip the Godot launch only when the task does not need engine verification or Godot is unavailable:

```powershell
powershell -ExecutionPolicy Bypass -File .\plugins\stratezone-mission-steward\scripts\validate_stratezone.ps1 -SkipGodot
```

## Workflow

1. Confirm the task's milestone lane before editing. For current work, prefer First Landing readability, repeatability, mission grammar, validation, and drift prevention.
2. Do not widen into Level 2, Vehicle Bay, Med Hall, Logistics / Repair Pad, Artillery, multiplayer, procedural content, ancient-tech lore, or release packaging unless the user explicitly asks.
3. Treat `game/data/` and `tests/SimulationSmoke/Program.cs` as runtime evidence, not just docs support.
4. Use `mission_truth_report.py` before making direction calls about Level 1 availability, starting roster, objectives, failure conditions, enemy pressure, fog, and hidden locks.
5. Use `check_content_drift.py` after touching mission data, unit/building data, localization keys, or source docs that define content IDs.
6. Run `validate_stratezone.ps1` before claiming implementation work is done, unless the user asked for findings-only or a docs-only answer.

## Closeout Shape

Report:

- files changed
- whether the work is docs, code, assets, tooling, or mixed
- commands run
- manual checks performed
- behavior verified
- checks not run and why
- known risks or follow-up work
- any hand-written code files over 900 lines touched or left relevant

If validation was skipped or blocked, say exactly why.

