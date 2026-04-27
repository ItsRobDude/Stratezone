# Stratezone Product Roadmap

This document tracks milestone direction and open decisions.

It should stay practical. The roadmap exists to help Stratezone become playable, not to make the project look larger than it is.

## Product Definition

Stratezone is a mission-first colony RTS about building and defending a powered expedition outpost in fresh military-industrial battlefield scenarios.

The intended product is:

- systems-heavy enough that the base feels alive
- RTS-readable enough that the player can command quickly
- packageable as a desktop indie game
- scoped around authored missions before sandbox or procedural expansion
- visually practical for AI-assisted concept art plus Photoshop cleanup
- grounded in restrained near-future military utility rather than ancient-tech mystery
- built around RTS-style economy wells, not survival-game resource hauling

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

## Settled Direction

- Workers are important recruitable units. They are useless in combat, flee from attackers, and replacing them costs resources and slows the outpost.
- The primary format is mission RTS, not survival sandbox.
- Each level starts as a fresh scenario, closer to a classic RTS campaign.
- Combat uses individual units with varied cost, strength, and specialty.
- Some units should perform best grouped or supported; elite/expensive units can stand alone better.
- Resource gathering uses refinery/extractor buildings placed over money-making wells.
- The first enemy faction is human with similar technology/troops, reskinned and tuned differently.
- The first mission includes an on-map commander troop who must be defended. He is fragile, carries a pistol, and currently exists mainly as a fail condition.
- Fog of war uses black unexplored areas. Explored areas stay visible after scouting instead of reverting to gray shroud.
- First prototype buildings are Colony Hub, Barracks, Power Plant, Pylon, Extractor/Refinery, and Defense Tower.
- Gun Towers and Rocket Towers can also act as Defense Tower wall anchors, but cost more because they are armed.
- First prototype units are Worker, Rifleman, Guardian, Rover, and Commander.
- Colony Hub is where new units spawn.
- Barracks controls allowed troop count; upgrades unlock new troop purchases.
- Power Plant generates power in a small radius. Underpowered buildings shut off.
- Pylons link power over long distances.
- Defense Towers create energy walls between compatible tower pairs; enemies must destroy or disable a tower to open the path.
- Enemy bases should rebuild and produce from limited resources, racing the player for additional wells, but Level 1 should do this slower than normal.
- Ancient tech is out of scope for now.
- The tone is military-industrial with restrained future utility tech, such as rocket towers and laser-armed troops.
- Missions can have varied failure criteria: commander killed, main base destroyed, transport lost, convoy failed, or combined fail states.

## Near-Term Priorities

1. Vision lock
   - Decide worker replacement cost relative to basic combat units.
   - Decide first-pass building footprint/buffer values.
   - Decide first-pass troop cap values and Barracks upgrade unlocks.
   - Decide Level 1 enemy production speed and resource handicap.
   - Decide the first campaign's mission archetypes.
   - Decide how much sci-fi utility tech belongs in the first unit roster.

2. Engine scaffold
   - Create the Godot 4 project if that stack remains approved.
   - Add a minimal C# runtime.
   - Establish folder layout and validation commands.

3. Greybox First Landing
   - Build a rough playable map with placeholder shapes.
   - Implement camera, selection, move commands, and basic construction.
   - Add Colony Hub, Barracks, Power Plant radius, Pylon linking, Extractor/Refinery, fog of war, Defense Tower wall links, and one enemy pressure event.

4. First combat loop
   - Add Worker, Rifleman, Guardian, Rover, Commander, and same-tech human enemy equivalents.
   - Add building damage and repair.
   - Add an enemy infrastructure target.

5. First mission arc
   - Add destroy-all-enemies objective and win/loss state.
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
- worker-importance direction
- first enemy faction direction
- commander-as-unit direction
- refinery/extractor economy direction
- fresh-scenario campaign direction

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
- Colony Hub unit spawning
- Barracks troop cap and upgrade unlock path
- Power Plant radius and underpowered shutoff
- Pylon long-distance power linking
- extractor/refinery on a resource well
- Defense Tower energy wall links
- construction with recruitable worker units
- first fog-of-war pass
- hidden placement spacing/buffer constraints with no visible grid

Exit criteria:

- the player can establish a tiny outpost
- power radius affects placement or function
- resource extraction feeds construction
- unpowered buildings visibly shut off

## Milestone 2: First Landing Mission

Goal: prove the core mission loop.

Mission arc:

1. land
2. deploy hub
3. build power plant, pylons, and barracks
4. extract resources
5. survive pressure
6. scout through black unexplored fog of war
7. destroy all enemies on the map
8. win or fail clearly

Systems:

- mission objectives
- enemy raid event
- basic combat
- building damage
- repair
- win/loss conditions
- at least one non-base-destruction failure criterion
- on-map commander defend condition
- enemy rebuild/production from limited resources
- defense tower wall path-blocking
- HUD objective tracker

Exit criteria:

- the mission can be completed start to finish
- the player understands what went wrong when failing
- the colony and combat sides both matter

## Milestone 3: Colony Pressure Pass

Goal: make the outpost feel alive without becoming a deep colony sim.

Candidate systems:

- expensive worker units and replacement cost
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
- infantry/security unit
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
- persistent expedition progression, only if mission-first structure earns it
- sandbox/skirmish
- map editor
- Steam demo
- itch.io public page
- mod support
- multiplayer

## Open Decisions

- Godot 4 C# final approval vs another engine path.
- Exact building footprint/buffer values for constrained maps.
- Worker replacement cost relative to basic combat units.
- First campaign mission archetypes and failure-condition mix.
- Whether the first public build should be a demo, prototype, or private playtest.
