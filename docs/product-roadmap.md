# Stratezone Product Roadmap

This document tracks milestone direction and open decisions.

It should stay practical. The roadmap exists to help Stratezone become playable, not to make the project look larger than it is.

## Product Definition

Stratezone is a mission-first colony RTS about building and defending a powered expedition outpost on a hostile alien world.

The intended product is:

- systems-heavy enough that the base feels alive
- RTS-readable enough that the player can command quickly
- packageable as a desktop indie game
- scoped around authored missions before sandbox or procedural expansion
- visually practical for AI-assisted concept art plus Photoshop cleanup

## Current Status

The repo is in pre-production.

Current docs in place:

- `README.md`
- `AGENTS.md`
- `docs/project-identity.md`
- `docs/technical-architecture.md`
- `docs/engineering-standards.md`
- `docs/product-roadmap.md`

No engine project, gameplay code, art pipeline, or build tooling is established yet.

## Near-Term Priorities

1. Vision lock
   - Answer the key player-experience questions.
   - Decide how light or personal worker simulation should be.
   - Decide whether the first mission starts fresh or implies a persistent expedition.

2. Engine scaffold
   - Create the Godot 4 project if that stack remains approved.
   - Add a minimal C# runtime.
   - Establish folder layout and validation commands.

3. Greybox First Landing
   - Build a rough playable map with placeholder shapes.
   - Implement camera, selection, move commands, and basic construction.
   - Add power radius, extractor, colony hub, and one enemy pressure event.

4. First combat loop
   - Add one player squad/vehicle and one enemy attacker.
   - Add building damage and repair.
   - Add an enemy infrastructure target.

5. First mission arc
   - Add objectives and win/loss state.
   - Add one midlevel twist.
   - Add readable HUD warnings.

## Milestone 0: Foundation Docs and Decisions

Goal: make the project understandable before code hardens.

Deliverables:

- project identity doc
- technical architecture doc
- engineering standards doc
- product roadmap
- stack decision
- first-mission assumptions

Exit criteria:

- a contributor can explain the game in one minute
- open design questions are explicit
- the first scaffold has a clear target

## Milestone 1: Greybox Prototype

Goal: prove basic RTS interaction and base construction.

Systems:

- camera pan/zoom
- selection
- move command
- simple unit/building entities
- command crawler or colony hub
- pylon/power radius
- extractor/resource well
- construction with placeholder workers or build progress

Exit criteria:

- the player can establish a tiny outpost
- power radius affects placement or function
- resource extraction feeds construction

## Milestone 2: First Landing Mission

Goal: prove the core mission loop.

Mission arc:

1. land
2. deploy hub
3. extend power
4. extract resources
5. survive pressure
6. repair or activate objective
7. strike enemy infrastructure
8. win or fail clearly

Systems:

- mission objectives
- enemy raid event
- basic combat
- building damage
- repair
- win/loss conditions
- HUD objective tracker

Exit criteria:

- the mission can be completed start to finish
- the player understands what went wrong when failing
- the colony and combat sides both matter

## Milestone 3: Colony Pressure Pass

Goal: make the outpost feel alive without becoming a deep colony sim.

Candidate systems:

- worker count and job priorities
- supply/stability/morale as a compact outpost health layer
- injuries or repair strain
- event warnings and consequences
- environmental pressure

Exit criteria:

- colony pressure creates decisions
- pressure is readable before it becomes dangerous
- the RTS pace remains active

## Milestone 4: Tactical Identity Pass

Goal: make combat and level design about more than direct fights.

Candidate systems:

- scout unit
- infantry/security squad
- armored vehicle
- artillery or siege unit
- engineer/repair/capture unit
- enemy power dependencies
- neutral map objects
- fog/scouting layer

Exit criteria:

- infrastructure strikes matter
- scouting creates useful information
- unit roles feel distinct
- the player has multiple viable approaches

## Milestone 5: Vertical Slice

Goal: make a small shareable build.

Deliverables:

- one polished-ish mission
- first pass art direction
- first pass sound and UI
- basic settings
- packaged Windows build
- known issues list
- playtest feedback notes

Exit criteria:

- a tester can run and play without developer explanation
- the first 20 minutes communicate Stratezone's identity
- the build produces actionable feedback

## Later Tracks

These are not first-prototype commitments:

- second mission
- second faction
- campaign layer
- persistent expedition progression
- sandbox/skirmish
- map editor
- Steam demo
- itch.io public page
- mod support
- multiplayer

## Open Decisions

- Godot 4 C# final approval vs another engine path.
- Individual workers vs worker count vs hybrid named specialists.
- Grid-based map vs continuous map with tile-aware placement.
- Squad-based combat vs individual unit control.
- How personal colonists should feel.
- How weird ancient technology should get.
- Whether the first public build should be a demo, prototype, or private playtest.
