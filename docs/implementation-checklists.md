# Stratezone Implementation Checklists

This document turns milestones into concrete acceptance checks.

It exists to prevent vague progress. A feature is not done because it exists in a scene or sounds right in a summary. It is done when the player-facing behavior is observable, the simulation rule is in the right ownership layer, and the repo has evidence that it works.

## Documentation Role

- **Doc role:** Active checklist for implementation acceptance.
- **Owns:** milestone acceptance checks, work-type done rules, evidence expectations, and baseline content IDs.
- **Does not own:** product vision, system architecture, final balance, or store-readiness policy.
- **Read when:** planning implementation, closing out code work, reviewing a milestone, or deciding whether a task is actually done.
- **Do not read for:** high-level game identity or release platform process.

## Global Closeout Checklist

Every implementation closeout should report:

- files changed
- whether the work is docs, code, assets, tooling, or mixed
- commands run
- manual tests performed
- commands or manual checks not run, with reason
- behavior verified from the player or simulation perspective
- known risks, TODOs, or follow-up work
- any hand-written code files over 900 lines, with the reason they were not split

Do not claim a feature is complete if it was only compiled but not observed.

## Scope Guard

Before adding a mechanic, tool, dependency, asset pipeline, or content type, confirm:

- it maps to a current milestone or documented release gate
- it supports First Landing, the sellable build runway, or an explicitly reopened scope
- it does not add multiplayer, procedural campaigns, deep colonist simulation, ancient-tech progression, or lore systems by accident
- it does not require broad refactors unrelated to the task
- it does not turn an existing file into a catch-all module where separate state, commands, rules, events, or presentation adapters would be clearer

If the behavior is not supported by a doc, stop and update the smallest relevant doc or ask before implementing it.

## Content ID Contract

Use stable, boring IDs for data and saveable references.

Initial IDs are defined in `docs/content-data-spec.md` and repeated here for checklist convenience:

- `mission_first_landing`
- `faction_player_expedition`
- `faction_private_military`
- `unit_grunt`
- `unit_cadet`
- `unit_rifleman`
- `unit_guardian`
- `unit_rover`
- `unit_commander`
- `unit_medium_tank`
- `unit_tank`
- `building_colony_hub`
- `building_barracks`
- `building_power_plant`
- `building_pylon`
- `building_extractor_refinery`
- `building_defense_tower`
- `building_gun_tower`
- `building_rocket_tower`
- `building_armory_annex` (legacy placeholder; not the planned first-demo Guardian unlock path)
- `building_vehicle_bay`
- `building_med_hall`
- `building_logistics_repair_pad`
- `building_artillery_battery`
- `resource_materials`
Rules:

- IDs are lowercase snake_case.
- Save data should store IDs, not scene paths or display names.
- Display names may change without changing IDs.
- New IDs should be added to `docs/content-data-spec.md` before broad use.
- Player-facing text should use localization keys; `display_name` is fallback only.

## Simulation Ownership Examples

Good:

- `PowerSystem` decides whether a building is powered.
- `MissionObjectiveSystem` decides whether the mission is won or lost.
- `ResourceSystem` decides extraction, spending, and depletion.
- UI reads current resource, power, objective, and selection state from simulation.
- Scene scripts submit commands such as build, move, repair, and attack.

Bad:

- a sprite script decides a building is powered because it is near a visual radius
- a HUD panel decides the player won because a label changed
- a scene node directly subtracts resources without a simulation command
- an animation completion event is the only source of damage truth
- a Godot node path becomes the canonical save identity for a unit or building

## Milestone 1 Checklist: Greybox Prototype

Acceptance checks:

- camera can pan and zoom
- player can select one unit
- player can box-select multiple units, if included in the pass
- player can right-click move selected units
- grouped move and attack commands keep units in a small spread formation instead of stacking them on one point
- bottom action bar exposes available building and troop commands with costs and detail hints
- attacks have a small readable flash or direction cue so damage is visible before final art
- destroyed building outlines are hidden from the active playfield
- HUD, command, validation, and objective text added after the i18n foundation uses localization keys
- buildings and units have stable IDs
- unit attack, movement, health, and resistance values come from content data
- building health, resistance, and attack values come from content data
- building `build_time_seconds` is `0` for the first prototype
- player can place a basic building with visible placement feedback
- blocked placement is rejected
- a grunt can construct a building by command
- a grunt can construct a powered Barracks add-on adjacent to a Barracks
- Power Plant powers nearby structures
- Pylon extends or links power
- unpowered Barracks visibly stops providing its function
- unpowered Barracks add-ons visibly stop providing unlock/capacity effects
- Extractor/Refinery generates income only on a resource well
- non-extractor buildings cannot be placed over open resource wells
- Defense Towers can create a blocking wall link
- Defense Towers can upgrade in place into Gun/Rocket Tower variants without dropping a powered wall link
- fog starts black outside known areas

Evidence:

- simulation/unit tests for power, resource, and placement where available
- manual run notes showing the outpost can be established
- screenshots or notes for visible power/fog/build feedback once visuals exist

## Milestone 2 Checklist: First Landing Mission

Acceptance checks:

- mission starts already landed
- Commander is present, controllable, fragile, and pistol-only
- Commander death triggers loss
- player can build power, Barracks, Extractor/Refinery, and defenses
- powered Barracks accepts a short serial troop queue and reports when that queue is full
- Vehicle Bay is silently locked and hidden in Level 1
- player can repair damaged assets, with material cost scaling by missing health percentage
- central contested well exists
- destroyed Extractor/Refinery buildings release their well claim so the contested well can be retaken
- mission setup uses authored data for starting entities, wells, and enemy AI build slots
- enemy pressure pacing comes from a mission AI profile rather than scene-only timing
- enemy is visible at or near fog edge
- enemies in explored terrain remain visible in real time
- enemies in never-explored black fog are hidden
- enemy produces or rebuilds only when it has resources
- enemy pressure is tame but active
- enemy pylon weak point can disable the enemy tower-wall route
- enemy pylon weak point powers the enemy central Extractor so destroying it creates a visible infrastructure-strike route
- Rover scouts but cannot shoot
- Rover can run over enemy infantry if that behavior is included
- all required enemy targets destroyed triggers win
- destroyed Barracks and Power Plants release same-faction Cadets before victory/loss checks finish
- destroying either Colony Hub releases a Medium Tank occupant, and hostile Hub occupants must be killed before mission completion
- Guardian energy fire, released Medium Tanks, and Rocket Tower explosives outperform comparable ballistic options against armored vehicles
- Cadet recruits fastest, Rifleman recruits only slightly slower, and Guardian recruits slower as a specialist; current content data should be retuned if it does not match that feel

Evidence:

- one completed mission run; current evidence includes a user-completed greybox win run
- one commander-death loss run; deterministic smoke coverage currently proves Commander death and the F7/debug loss path
- smoke coverage for central well retake, enemy Pylon weak point, and tower-wall shutdown
- smoke coverage for Guardian-vs-armor damage math, Medium Tank occupant release on both sides, and Rocket Tower anti-armor tuning
- repair smoke coverage for command start, switching targets, friendly-only restriction, proportional material cost, and no-negative-spend behavior
- notes for any missing or intentionally placeholder behavior
- localization key coverage for mission result, objective, command, and blocked-action text
- closeout notes should distinguish verified playable behavior from deferred systems such as Med Hall, Logistics / Repair Pad, final balance, and final art

## Milestone 3 Checklist: Mission Grammar and Architecture Hardening

Acceptance checks:

- hand-written files over the 900-line review trigger are split or have a documented reason to stay together
- broad smoke coverage is split or grouped enough that a failed mission route points to the relevant system quickly
- mission data has a repeatable pattern for markers, starting entities, resource wells, objectives, failure conditions, and AI profile
- Barracks Guardian upgrade has a runtime/data path that replaces the old Armory Annex Guardian gate
- mission setup supports deployed starter base, player-built base, partial damaged base, allotted/irreplaceable troops, and no-base force starts without copying scene logic
- map data can represent first-pass terrain regions, passability blockers, readable base/expansion pockets, resource basins, chokepoint markers, and explicit restricted-build scenarios
- new mission setup can be added through data plus narrow simulation/presentation seams
- event trigger pacing has grace/cooldown/coalescing runtime support so normal early build milestones cannot stack immediate raids
- mission presentation fields have a localization-ready data path for briefings, objectives, failure/success messages, and later warnings/map callouts/retry hints/tactical notes
- the in-game map editor/tuner can inspect current mission markers, terrain regions, map objects, live resource wells, pylon ranges, and wall-link candidates without becoming the source of gameplay truth
- editor/tuner changes can still be copied/printed as reviewable JSON snippets, and guarded source saves are limited to owned marker/region/object arrays with diff preview, one `.bak`, and validation-friendly JSON output
- marker selection, region/object selection, nudge/move behavior, marker/region creation, dependency-aware delete, resize handles, rename warnings, undo/redo, validation, JSON export, edited-definition materialization, and guarded save behavior are covered by plain C# smoke checks
- editor/tuner errors and invalid operations emit structured diagnostics with operation, mission/map, selection, counts, and exception details
- root or documented validation commands cover content validation, C# build, simulation smoke, and Godot headless launch
- new objective, warning, command, and blocked-action text uses localization keys
- no new major mechanic is added only to make the milestone feel larger

Evidence:

- file-size and ownership notes for any remaining large files; current `Main.cs` command-panel ownership lives in a partial file
- smoke/test organization notes showing how Level 2 routes will be proven; current smoke files separate content, First Landing, Mission 2, and mission trigger checks
- start-pattern proof notes
- map editor/tuner notes or snippets for terrain/passability authoring
- one dry-run plan for adding a second mission without copying First Landing scene logic
- implementation follow-up list for any runtime/data seams not completed in this milestone
- validation command output

## Milestone 4 Checklist: Level 2 - Wells at the Ridge

Acceptance checks:

- Level 2 uses the same-tech human enemy family with red or alternate-color presentation
- Level 2 has the player build and place the base instead of starting with a finished base
- scarce wells force a visible resource race
- cliffs, water, or other impassable terrain create readable chokepoints without a complex terrain-simulation expansion
- Level 2 is meaningfully larger than Level 1, with a farther enemy base, a longer bridge/choke, and top/bottom exploration lanes
- readable clearings give the player practical base-space cues without becoming hidden build whitelists
- resource basins make important wells read as tactical map pockets
- Defense Tower wall placement matters because of the map shape
- enemy power or extractor infrastructure can be scouted and attacked
- Barracks Guardian upgrade and Guardian production are included if the Level 2 route can carry the first unlock without losing the resource-race proof
- Guardian remains anti-armor / anti-defense specialist if introduced here
- Armory Annex is not used as the Guardian unlock path
- player-facing warnings report only known events, not hidden attack planning
- early raid triggers coalesce or queue behind cooldowns instead of stacking when Barracks and first Extractor appear back to back
- early enemy patrols visit a few authored exploration areas before the main attack timer, encouraging scouting without hidden-plan alerts
- pressure creates a choice between repair, defense, expansion, or attack
- quick fail/retry flow is acceptable; no persistent campaign consequence is required
- Vehicle Bay remains absent from Level 2 unless the roadmap is deliberately reopened
- Med Hall, Logistics / Repair Pad, and Artillery remain absent unless one is needed for the mission's primary proof

Evidence:

- one expansion/resource-race route note
- one defensive wall/chokepoint route note
- one Barracks Guardian upgrade route note if the unlock enters Level 2
- smoke or debug evidence for any new terrain/passability/buildability rule
- smoke evidence that the Mission 2 player can deploy the Hub inside the landing clearing, build Power/Barracks/Extractor, chain Pylons through the authored power corridor to the central island well, train the second Grunt, complete Guardian Retrofit, and train a Guardian
- smoke evidence that the enemy does not claim the central island well on its normal rebuild beat and waits for the mission-profile central-well claim delay
- smoke evidence that Mission 2 starts with a real enemy defense wall, the wall buffer blocks the intended bridgehead crossing lanes, units can shoot through a wall when range allows, idle combat units auto-fire at visible hostile troops/tanks but not buildings, enemy forward expansion pylons are not rebuilt after the player owns the linked central island well, base-breach rebuilds prefer defense/wall power over expansion pylons, idle enemy defenders pursue base intruders before the scheduled attack timer, and early enemy patrols fan out across multiple authored exploration markers
- notes for any support/siege system deliberately kept out

## Milestone 5 Checklist: Level 3 - Mobile Response

Acceptance checks:

- Vehicle Bay is a physical powered Barracks add-on in the mission, not only a menu upgrade
- Rover production or Rover access is earned through mission setup
- Rover role is useful without replacing infantry or turning the mission into a vehicle-only test
- power disruption can affect the unlock path
- troop train times preserve fast classic-RTS pacing
- enemy still uses the same human tech family unless a later art/design pass deliberately changes it
- at most one support/siege system enters this milestone, and only if the mission needs it

Evidence:

- one route note showing why Vehicle Bay/Rover mattered
- smoke coverage for the unlock path and Rover role
- notes explaining whether Med Hall, Logistics / Repair Pad, or Artillery stayed deferred

## Milestone 6 Checklist: Level 4/5 Greybox Lock

Acceptance checks:

- Level 4 Broken Outpost has a greybox start or mission brief with partial damaged base, broken power, Grunt repair, and powered hack/control of defense equipment
- Level 5 Frozen Breakout has a greybox start or mission brief with no-base force, snow route, extraction/siege objective, and authored siege equipment or Artillery Battery scenario object
- each Level 4/5 mission has one primary proof target
- the first demo remains a campaign-like arc of authored simulation scenarios, not a story-heavy cutscene plan
- mission starts are authored; no player loadout screen is required for the first demo
- at least one later mission start pattern is named if used: player-built base, partial base, no-base moving force, allotted/irreplaceable troops, or normal base start
- Mission 5 is no-base in the current demo outline
- the demo uses the preferred support/siege pair unless the roadmap is deliberately reopened: powered Grunt-hacked defense equipment in Mission 4 and Artillery/authored siege equipment in Mission 5
- Med Hall, Logistics / Repair Pad, and Neutral Repair Platform are cut from the first demo
- each mission has a clear failure-condition mix and restart expectation, with no timer-expiry mission failure unless the roadmap is explicitly reopened
- cut systems are named instead of left as vague future work

Evidence:

- Level 4 and Level 5 greybox/brief notes
- per-mission proof target, start-pattern, and failure-condition list for Levels 4 and 5
- implementation follow-up for tuning the Barracks Guardian upgrade route after Level 2 playtests
- explicit cut/defer list for systems outside the first public demo

## Milestone 7 Checklist: Demo Readability and Localization Pass

Acceptance checks:

- each demo mission has a concise briefing title/body and one tactical note only when useful
- objective tracker text is direct, verb-led, and names mission-critical fail objects before they can fail the mission
- warnings report known events only and do not reveal hidden enemy planning, rebuilding, or targeting
- map callouts are sparse and support wells, ridges, chokes, power lines, equipment, extraction, or siege areas
- failure messages name the cause and, where useful, include a short tactical retry hint
- success messages state one truthful consequence leading into the next authored mission
- all new briefing, objective, warning, callout, failure, success, retry, and tactical-note copy uses localization keys
- quick restart/quit flow is checked across the demo
- no mission depends on cutscenes or heavy story text to explain its mechanical purpose

Evidence:

- per-mission presentation/localization string list
- localization validation output
- notes from at least one full demo readability pass, even if greybox assets remain rough

## Public Build Checklist

Before any itch.io or Steam-facing build:

- public demo scope is the first five levels, not the current greybox-only mission
- current project state remains pre-demo until working level design exists beyond the prototype loop
- packaged Windows build runs from a clean folder
- build includes version, channel, and commit or build identifier
- player can launch, play, restart, quit, and relaunch
- store/page claims match the actual build
- player-facing strings needed for the build exist in localization data
- credits and license notes exist
- generated, purchased, or edited assets have provenance notes
- support/contact path exists
- release notes or known issues exist
- upload process is documented or scripted
