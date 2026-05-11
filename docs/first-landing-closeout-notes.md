# First Landing Closeout Notes

Date: 2026-05-11

This note records the current closeout evidence for the First Landing greybox route. It is not a balance lock or a public-demo readiness claim.

## Mission Truth Summary

Generated with `python plugins\stratezone-mission-steward\scripts\mission_truth_report.py`.

- Mission: `mission_first_landing` / Greybox Demo, target duration 5-10 minutes.
- Player starts with 1 Commander, 1 Grunt, 1 Guardian, 1 Rover, and 1 Colony Hub.
- Enemy starts with 3 Riflemen, 1 Colony Hub, 1 Barracks, 1 Power Plant, 2 Pylons, and 2 Defense Towers.
- Level 1 trainable units remain Grunt, Cadet, and Rifleman.
- Commander, Guardian, and Rover remain authored-only player units for Level 1.
- Hidden/deferred Level 1 buildings remain Armory Annex, Vehicle Bay, Med Hall, Logistics / Repair Pad, and Artillery Battery.
- Mission rules remain destroy all enemies, protect Commander, black unexplored fog, explored terrain stays visible, and Colony Hub destruction reveals a Medium Tank.
- Enemy AI profile remains slow/readable: first attack at 115 seconds, attack group size 1, pressure slowdown 0.55, train time multiplier 1.8.

## Automated Evidence

- `tests/SimulationSmoke/FirstLandingMissionSmoke.cs` proves the authored enemy Pylon weak point exists, starts powered, powers the central enemy Extractor, and supports the enemy tower-wall route.
- Destroying the enemy Pylon increments internal power-strike memory, shuts off the central enemy Extractor, and drops the enemy tower-wall route.
- Destroying the enemy central Extractor releases the central well for player retake, and the smoke route proves a powered player Pylon chain can reach that well.
- The mission AI profile delays first committed pressure, sends a scout before the first attack, commits only the small configured attack group, and leaves defenders at base.
- The broader simulation smoke suite proves Commander death loss, F7/debug Commander loss, destroy-all-enemies win, hostile Colony Hub Medium Tank occupant release, repair behavior, fog visibility, production, tower upgrades, explosive friendly fire, and crush behavior.

## Validation

Command run:

```powershell
powershell -ExecutionPolicy Bypass -File plugins\stratezone-mission-steward\scripts\validate_stratezone.ps1
```

Result: passed.

Checks covered:

- Content validation: passed, 38 records.
- Godot C# build: passed with 0 warnings and 0 errors.
- Simulation smoke: passed.
- Content drift check: passed, including Medium Tank reveal-only drift rule.
- Godot headless smoke: passed, loading `mission_first_landing` and the F5 map editor/tuner overlays.

## Remaining Risks

- A full natural manual win/loss replay was not performed during this closeout; current confidence comes from existing user win-run evidence, deterministic smoke coverage, and Godot headless scene load.
- First Landing pacing is still tunable. The evidence proves the route and AI profile, not final feel.
- The enemy Pylon weak point is intentionally discoverable rather than tutorial-obvious; future playtests should watch whether players notice it without a heavy prompt.
- Final art, audio, minimap, packaged build flow, and public-demo readability are still later milestones.
