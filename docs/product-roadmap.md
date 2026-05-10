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
- shaped like a campaign arc of simulation-driven missions, not a story-heavy cutscene campaign
- visually practical for AI-assisted concept art plus Photoshop cleanup
- realistic about asset production: AI-assisted concepts, Illustrator vectorization, cleanup, and turntable-derived directional frames are valid working paths while paid art is out of reach
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
- `docs/content-roadmap.md`
- `docs/scaffold-plan.md`
- `docs/first-landing-mission-spec.md`
- `docs/system-contracts.md`
- `docs/content-data-spec.md`
- `docs/implementation-checklists.md`
- `docs/release-roadmap.md`

The initial `game/` project, placeholder content data, and validation stack exist. The greybox slice now supports camera pan/zoom, click and box selection, right-click move and attack commands with small formation spread, grunt-driven building placement, powered construction rules, resource extraction, short serial Barracks queues for Level 1 units, basic combat with outgoing and incoming fire flashes, enemy production/rebuild pressure from limited resources, fog visibility, Defense Tower wall links, in-place armed tower upgrades, a forward enemy Pylon weak point that powers the central Extractor and tower-wall route, Commander loss, destroy-all-enemies win state, and a localized bottom action bar with command costs, queued-count feedback, and hover details.

Godot .NET 4.6.2 and .NET SDK 8 are installed on this machine. Content validation, the Godot C# build, simulation smoke checks, and a Godot headless smoke check pass locally.

The first prototype stack is locked as Godot 4 with C#.

## Settled Direction

- Grunts are important recruitable units. They are useless in combat, flee from attackers, and replacing them costs resources and slows the outpost.
- The primary format is mission RTS, not survival sandbox.
- Each level starts as a fresh scenario, closer to a classic RTS campaign. The project can still feel like a simulator inside each mission, but the first demo should be a structured mission arc rather than an open-ended sandbox.
- Combat uses individual units with varied cost, strength, and specialty.
- Some units should perform best grouped or supported; elite/expensive units can stand alone better.
- Resource gathering uses refinery/extractor buildings placed over scarce limited wells that trickle resources and can deplete.
- The first enemy faction is a private military force using the same basic buildings and technology as the player, reskinned in red or an alternate color until later art direction proves a stronger need.
- Commander units are controllable troops, not abstract heroes. They should be used in the same practical RTS spirit as Dominion-style commanders: valuable, vulnerable, mission-relevant units on the map.
- The first mission is a small 5-10 minute top-down RTS scenario in bright readable meadows/fields with light forest.
- Fog of war uses black unexplored areas. Explored areas stay visible after scouting instead of reverting to gray shroud, and units/buildings in explored terrain remain visible in real time.
- First prototype buildings are Colony Hub, Barracks, Power Plant, Pylon, Extractor/Refinery, and Defense Tower.
- Gun Towers and Rocket Towers are preferred as in-place upgrades from Defense Towers. They keep wall-anchor behavior while adding weaponry and higher cost.
- First prototype units are Grunt, Cadet, Rifleman, Guardian, Rover, and Commander.
- Colony Hub is where new units spawn.
- Barracks controls what can be trained by level, allowed troop count, and upgrade unlocks.
- Guardian production should be unlocked by upgrading the Barracks itself. Vehicle Bay is the powered physical Barracks add-on for Rover/heavy-armor capacity.
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
- Missions can have varied failure criteria: commander killed, main base destroyed, transport lost, convoy failed, required Grunt/equipment lost, or combined fail states. Timer-expiry mission failures are not planned for the first demo.

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

3. Mission grammar and architecture hardening
   - Make the next authored mission cheaper to add before introducing another broad gameplay layer.
   - Split or justify large hand-written files that are already over the review trigger, especially `Main.cs` and broad smoke coverage.
   - Preserve the simulation/presentation boundary while adding mission templates, validation helpers, and scenario-specific tests.

4. Level 2 planning target
   - Build a resource-race mission with more strategic base-building terrain: cliffs, water, chokepoints, and Defense Tower wall opportunities.
   - Use Level 2 as the likely first Barracks Guardian upgrade mission if that does not overload the resource-race proof.
   - Keep the first enemy same-tech and same-building for now, using red or alternate-color presentation.
   - Use environmental layout and resource competition before adding Med Hall, Repair Pad, or Artillery.

5. Release runway
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
- grunt-importance direction
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
- construction with recruitable grunt units
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
- player-commanded Grunt repair for damaged friendly structures
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

## Milestone 3: Mission Grammar and Architecture Hardening

Goal: make new authored missions maintainable before the project adds more mechanics.

This milestone is intentionally not a content-expansion milestone. It exists because the first mission now proves enough runtime truth that future work needs stronger seams before Level 2 grows.

Architecture work:

- split or explicitly justify hand-written files over the 900-line review trigger
- separate broad smoke coverage into mission/scenario-focused checks when it starts slowing diagnosis
- create a repeatable mission data pattern for markers, starting entities, resource wells, objectives, failure conditions, and AI profile
- define first-pass map logic for terrain regions, passability, buildable clearings, resource basins, and chokepoint markers
- add a lightweight map preview/debug path if terrain authorship becomes hard to inspect from JSON alone
- keep all new mission rules in simulation/data layers rather than Godot scene-only code
- add or document root-level validation commands so future runs do not depend on remembered command sequences
- keep localization keys mandatory for new objective, warning, command, and blocked-action text

Exit criteria:

- Level 2 can be added mostly through data plus narrow simulation/presentation seams
- map data can express blocked terrain, buildable areas, resource basins, and tower-wall chokepoint candidates without relying on scene-only placement
- `Main.cs` and smoke coverage have clear ownership, split points, or a documented reason to stay together
- scenario checks can prove a mission route without replaying every unrelated system assertion
- event triggers have grace/cooldown or coalescing rules before Level 2 relies on multiple base-building pressure triggers
- no new major mechanic has been added just to make the roadmap look larger

## Milestone 4: Level 2 - Resource Race, Terrain Chokes, and First Guardian Upgrade

Goal: prove player-built strategic base pressure through terrain and resource competition, with a Barracks Guardian upgrade as the likely first powered unlock if the mission can carry it.

Mission shape:

- same-tech human opponent using player-like structures and units with red or alternate-color presentation
- the player builds and places the base instead of starting with a finished base
- scarce resource wells that force a race for expansion timing
- cliffs, water, or other impassable terrain that create readable chokepoints without requiring complex terrain simulation
- authored buildable clearings and resource basins that make base expansion readable without a visible grid
- Defense Tower wall placement that matters because of the terrain, not because a tutorial says so
- enemy power dependencies and extractor routes that can be scouted and attacked
- likely first Barracks Guardian upgrade, justified by enemy armor, hardened defense, or tower-anchor pressure
- pressure triggers do not stack immediate raids when normal base-building milestones happen close together
- quick RTS failure/retry expectations rather than persistent campaign consequences
- no Vehicle Bay requirement
- no Med Hall, Logistics / Repair Pad, or Artillery unless one is clearly needed to make the mission work

Exit criteria:

- the player makes a real choice between expanding, walling, repairing, or attacking
- the resource race is legible before it becomes punishing
- at least one chokepoint can be shaped with Defense Tower walls
- blocked terrain affects placement and/or movement in a way smoke checks can prove
- Guardian production remains a specialist answer if the Barracks upgrade enters this mission
- infrastructure strikes matter without requiring a new faction or story system
- the mission proves a second repeatable level-design pattern after First Landing

## Milestone 5: Level 3 - Vehicle Bay and Rover Tactical Unlock Candidate

Goal: assign and prove the first Vehicle Bay / Rover production mission, with Level 3 as the working slot and Level 4 as the fallback if mission shape demands it.

Deliverables:

- an authored mission where the Vehicle Bay matters as a powered physical add-on
- Rover production or Rover access earned through mission setup rather than assumed globally
- route, scouting, transport, crush, or vehicle-pressure design that makes Rover access useful without invalidating infantry
- confirmation that Barracks Guardian upgrade either landed cleanly in Level 2 or was deliberately moved
- at most one support/siege system from Med Hall, Logistics / Repair Pad, or Artillery Battery if the mission proves a hard need
- enemy use of the same tech family unless a later art/design pass deliberately changes that

Exit criteria:

- the player understands why the Vehicle Bay exists
- Rover remains utility/mobile pressure, not a replacement for the infantry roster
- power disruption can affect the unlock path
- the mission adds tactical identity without broad roster bloat

## Milestone 6: Demo Mission Set Shape

Goal: decide and prove the remaining first-demo mission archetypes before packaging work dominates.

Deliverables:

- Level 4 and Level 5 mission briefs or greybox starts
- one additional mission archetype beyond resource race / first Guardian upgrade and Vehicle Bay / Rover
- Vehicle Bay / Rover production is assigned to Mission 3 or Mission 4
- authored start patterns are named: player-built base, full/deployed base, partial base, no-base moving force, or allotted/irreplaceable troops
- Mission 5 is no-base in the current demo outline
- timer-expiry failure stays out of the first demo unless the roadmap is deliberately reopened
- player-facing mission presentation needs are named: briefing, objective text, warnings, map callouts, failure text, success text, and localization keys
- a decision on whether the demo uses one or two support/siege systems total
- a written cut line for systems that stay after the first public demo

Exit criteria:

- the first five-level demo has a coherent sequence of playable lessons
- each mission has one primary proof target and one clear reason to exist
- no player loadout system is needed for the first demo
- failure conditions are clear, mission-specific, and not hidden timers
- new player-facing mission presentation copy is localizable
- no mission depends on lore or cutscenes to explain its mechanical purpose

## Milestone 7: Playtest Build

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

## Milestone 8: Public Demo / Itch Build

Goal: prepare a public or semi-public downloadable build through itch.io.

Current public-build decision: the first public artifact should be a demo covering the first five levels. The project is still pre-demo; the current greybox First Landing work should not be marketed as the public demo, and repeatable level-design proof is still missing.

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

## Milestone 9: Steam Page Candidate

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

## Milestone 10: Steam Demo or Early Access Candidate

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

## Milestone 11: Sellable Release Candidate

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

- second through fifth missions for the first public demo
- second faction
- campaign layer
- persistent expedition progression, only if mission-first structure earns it
- sandbox/skirmish
- map editor
- polished terrain-art pipeline beyond the first greybox/prototype terrain kit
- mod support
- multiplayer

## Open Decisions

- Exact building footprint/buffer values for constrained maps.
- Grunt replacement cost relative to basic combat units.
- Exact Vehicle Bay/Rover production mission slot. Current direction is that Vehicle Bay enters the first demo in Mission 3 or Mission 4, never Level 1.
- Runtime/data path for replacing the current Armory Annex placeholder with the approved Barracks Guardian upgrade.
- Exact Artillery/authored siege implementation for Mission 5. Current first-demo support/siege direction is powered Grunt-hacked defense equipment in Mission 4 plus Artillery/authored siege equipment in Mission 5. Med Hall, Logistics / Repair Pad, and Neutral Repair Platform are cut from the first demo unless playtests deliberately reopen them.
- Level 4 and Level 5 mission archetypes and failure-condition mix.
- Whether Steam starts with the public five-level demo, a separate playtest branch, or a later Early Access candidate.
- Whether the first paid release targets itch.io first, Steam first, or both after the demo proves itself.
