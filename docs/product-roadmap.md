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
- built around limited RTS-style resource wells, not survival-game resource hauling

## Current Status

The repo is in pre-production with a playable greybox Godot 4 C# RTS slice.

Current docs in place:

- `README.md`
- `AGENTS.md`
- `docs/project-identity.md`
- `docs/technical-architecture.md`
- `docs/engineering-standards.md`
- `docs/product-roadmap.md`
- `docs/scaffold-plan.md`
- `docs/first-landing-mission-spec.md`
- `docs/system-contracts.md`
- `docs/content-data-spec.md`
- `docs/implementation-checklists.md`
- `docs/release-roadmap.md`

The initial `game/` project, placeholder content data, and validation stack exist. The greybox slice now supports camera pan/zoom, click and box selection, right-click move and attack commands with small formation spread, worker-driven building placement, powered construction rules, resource extraction, short serial Barracks queues for Level 1 units, basic combat with outgoing and incoming fire flashes, enemy production/rebuild pressure from limited resources, fog visibility, Defense Tower wall links, in-place armed tower upgrades, a forward enemy Pylon weak point that powers the central Extractor and tower-wall route, Commander loss, destroy-all-enemies win state, and a localized bottom action bar with command costs, queued-count feedback, and hover details.

Godot .NET 4.6.2 and .NET SDK 8 are installed on this machine. Content validation, the Godot C# build, simulation smoke checks, and a Godot headless smoke check pass locally.

The first prototype stack is locked as Godot 4 with C#.

## Settled Direction

- Workers are important recruitable units. They are useless in combat, flee from attackers, and replacing them costs resources and slows the outpost.
- The primary format is mission RTS, not survival sandbox.
- Each level starts as a fresh scenario, closer to a classic RTS campaign.
- Combat uses individual units with varied cost, strength, and specialty.
- Some units should perform best grouped or supported; elite/expensive units can stand alone better.
- Resource gathering uses refinery/extractor buildings placed over scarce limited wells that trickle resources and can deplete.
- The first enemy faction is a private military force with similar technology/troops, reskinned and tuned differently.
- The first mission includes a controllable on-map commander troop who must be defended. He is fragile, carries a pistol, and currently exists mainly as a fail condition.
- The first mission is a small 5-10 minute top-down RTS scenario in bright readable meadows/fields with light forest.
- Fog of war uses black unexplored areas. Explored areas stay visible after scouting instead of reverting to gray shroud, and units/buildings in explored terrain remain visible in real time.
- First prototype buildings are Colony Hub, Barracks, Power Plant, Pylon, Extractor/Refinery, and Defense Tower.
- Gun Towers and Rocket Towers are preferred as in-place upgrades from Defense Towers. They keep wall-anchor behavior while adding weaponry and higher cost.
- First prototype units are Worker, Cadet, Rifleman, Guardian, Rover, and Commander.
- Colony Hub is where new units spawn.
- Barracks controls what can be trained by level, allowed troop count, and upgrade unlocks.
- Barracks upgrades should be physical powered add-on modules built adjacent to the Barracks. Armory Annex unlocks Guardian/explosive tech. Vehicle Bay unlocks Rover/heavy-armor capacity.
- Power Plant generates power in a small radius. Underpowered buildings shut off.
- Pylons link power over long distances.
- Defense Towers create energy walls between compatible tower pairs; enemies must destroy or disable a tower to open the path.
- Enemy bases should rebuild and produce from limited resources, racing the player for additional wells, but Level 1 should do this slower than normal.
- Tanks are not normally trainable in Level 1, but destroying either player's or enemy's Colony Hub reveals a Medium Tank without changing win/loss conditions by itself; reveal-only tanks do not block destroy-all victory.
- The first playable target is playable ugly: placeholder shapes are acceptable, no story cutscenes are required, and art direction can wait until gameplay works.
- Explosive friendly fire exists; normal gunfire does not.
- First-pass combat balance should follow the old-school RTS formula: basic infantry die quickly, base structures take a long time to crack with small arms, armor shrugs off ballistics, and explosives are the siege lane.
- Ancient tech is out of scope for now.
- The tone is military-industrial with restrained future utility tech, such as rocket towers and laser-armed troops.
- Missions can have varied failure criteria: commander killed, main base destroyed, transport lost, convoy failed, or combined fail states.

## Near-Term Priorities

1. First Landing tactical route closeout
   - Treat the user-completed win run as the first playable proof for the current greybox slice.
   - Keep Commander-loss verification lightweight through the F7/debug path unless natural combat loss exposes a distinct bug.
   - Prove the enemy Pylon weak point, central well retake, tower-wall route, and attack pacing from data-backed checks and manual notes.
   - Patch only controls, readability, pacing, data drift, or localization issues that block the existing mission loop.

2. Validation and scaffold upkeep
   - Keep Godot .NET and .NET SDK versions documented.
   - Keep the project compiling after each implementation pass.
   - Keep content validation passing as data grows.
   - Expand automated checks only when they protect deterministic behavior found during playability passes.

3. Tactical proof follow-up
   - Tune the enemy pylon weak point, central well pressure, tower-wall route, and attack pacing from additional playtest evidence.
   - Decide whether repair is necessary for First Landing or should wait for later tactical identity work.
   - Add a midlevel twist only after the basic win/loss run is reliably understandable.

4. Release runway
   - Keep `docs/release-roadmap.md` current as build tooling appears.
   - Add packaged-build checks before any public demo.
   - Separate prototype completeness from sellable release readiness.

## Milestone 0: Foundation Docs and Decisions

Goal: make the project understandable before code hardens.

Deliverables:

- project identity doc
- technical architecture doc
- engineering standards doc
- product roadmap
- stack decision
- scaffold plan
- First Landing mission spec
- system contracts
- content data spec
- implementation checklists
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

Current status: the greybox base/control loop is functionally present. Remaining work here should be treated as bug fixing or evidence capture, while new gameplay direction should land under Milestone 2 unless it directly repairs controls, construction, power, resources, fog, or command readability.

Systems:

- camera pan/zoom
- selection
- move command
- simple unit/building entities
- Colony Hub spawn location
- Barracks training rules, troop cap, and powered add-on unlock path
- Power Plant radius and underpowered shutoff
- Pylon long-distance power linking
- extractor/refinery on a resource well
- Defense Tower energy wall links
- in-place Defense Tower upgrades into armed tower variants
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

Current status: active. A user playtest has completed a win run in the current slice, and deterministic smoke coverage proves the Commander-loss/debug path. The next validation target is repeatable mission routing: central well retake, forward enemy power strike, tower-wall opening, and readable pressure.

Mission arc:

1. start already landed
2. get bearings around the base and commander
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
- repair deferred unless playtesting shows First Landing needs it
- win/loss conditions
- at least one non-base-destruction failure criterion
- on-map commander defend condition
- enemy rebuild/production from limited resources
- central contested well
- enemy pylon weak point that can disable an enemy tower route
- Colony Hub Medium Tank reveal without changing win/loss rules
- Guardian anti-armor infantry tuning proven against Medium and Heavy Tanks without making Guardian a better anti-infantry Rifleman
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
- Med Hall content record for slow infantry healing that spends resources while active
- Logistics / Repair Pad content record for powered vehicle maintenance
- Artillery Battery content record as fragile static siege infrastructure with a minimum range
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

Goal: make one mission feel like a small, coherent game slice.

Deliverables:

- one polished-ish mission
- first pass art direction
- first pass sound and UI
- basic settings
- packaged Windows build
- known issues list
- playtest feedback notes
- build version display

Exit criteria:

- a tester can run and play without developer explanation
- the first 20 minutes communicate Stratezone's identity
- the build produces actionable feedback
- the build is still a prototype, not a sellable release

## Milestone 6: Playtest Build

Goal: let a small private tester play without the developer narrating.

Code and tool work:

- package a Windows build from Godot
- expose repeatable build/run commands or documented steps
- add basic settings for resolution/window mode, input basics, and volume
- add restart/quit flow
- add visible version/build info
- document log or crash-report location
- add a playtest feedback template

Exit criteria:

- a tester can launch, play, fail, restart, and quit without editor access
- one 20-30 minute session produces useful feedback
- known issues are tracked in writing

## Milestone 7: Public Demo / Itch Build

Goal: prepare a public or semi-public downloadable build through itch.io.

Code and tool work:

- create repeatable Windows export steps
- create a release folder layout that contains only shippable files
- version build filenames and in-game build display
- prepare an itch upload path using `butler push`
- test a clean download/install/run path

Store/page work:

- itch page draft
- screenshots from the actual build
- short description that matches the build
- install notes and known issues
- minimum supported OS/hardware notes

Exit criteria:

- the Windows build can be downloaded and run outside the repo
- the itch page does not claim features missing from the build
- strangers can give gameplay feedback instead of setup feedback

## Milestone 8: Steam Page Candidate

Goal: prepare for Steam visibility before a full release claim.

Code and tool work:

- keep a stable demo or playtest branch
- ensure the build includes every feature claimed on the page
- add a release checklist that separates store-page readiness from build readiness
- decide whether Steam starts with a demo, playtest, Early Access candidate, or full release candidate

Store/page work:

- capsule/key art plan
- screenshots from current build
- short trailer or gameplay capture plan
- truthful feature list
- Steam tags and genre positioning
- Coming Soon timing plan

Exit criteria:

- store claims match the playable build
- Steam submission work has a checklist
- missing features are not hidden inside marketing copy

## Milestone 9: Steam Demo or Early Access Candidate

Goal: submit a build and page that can survive platform review.

Code and tool work:

- packaged Windows build on a release branch
- repeatable Steam build upload steps
- clean first-run flow
- settings, restart, credits, license notes, and support info
- crash/log capture documented
- release notes and known issues

Store/platform work:

- complete Steam store presence checklist
- complete Steam game build checklist
- submit store page before build review
- account for review time and Coming Soon visibility

Exit criteria:

- the store page and build describe the same game
- the build launches and plays outside the editor
- remaining blockers are platform/process issues, not missing basics

## Milestone 10: Sellable Release Candidate

Goal: make a build that can reasonably be sold.

Code and tool work:

- final release branch
- versioned build artifact
- clean install/uninstall behavior
- save/load or clearly documented mission-run expectations
- stable performance on target hardware
- final credits and license audit
- post-launch patch process

Store/business work:

- final screenshots and trailer
- final store copy that matches the build
- price decision
- support/contact path
- launch discount decision if applicable
- first-patch plan

Exit criteria:

- a buyer can install, play, understand, quit, relaunch, and get support
- the repo can reproduce the release build
- the build is honest enough to sell, not just useful for feedback

## Later Tracks

These are not first-prototype commitments:

- second mission
- second faction
- campaign layer
- persistent expedition progression, only if mission-first structure earns it
- sandbox/skirmish
- map editor
- mod support
- multiplayer

## Open Decisions

- Exact building footprint/buffer values for constrained maps.
- Worker replacement cost relative to basic combat units.
- Exact Level 1 use of Armory Annex and Vehicle Bay.
- Whether Med Hall, Logistics / Repair Pad, and Artillery Battery should appear in First Landing or wait for a later tactical mission.
- First campaign mission archetypes and failure-condition mix.
- Whether the first public build should be a demo, prototype, or private playtest.
- Whether Steam starts with a demo, playtest, Early Access candidate, or full release candidate.
- Whether the first paid release targets itch.io first, Steam first, or both after the demo proves itself.
