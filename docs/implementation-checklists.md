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
- `unit_worker`
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
- `building_armory_annex`
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
- a worker can construct a building by command
- a worker can construct a powered Barracks add-on adjacent to a Barracks
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
- player can use or bypass Barracks add-ons according to Level 1 pacing rules
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
- destroying either Colony Hub reveals a Medium Tank without changing win/loss by itself, and the reveal-only tank does not block victory
- Guardian energy fire, revealed Medium Tanks, and Rocket Tower explosives outperform comparable ballistic options against armored vehicles

Evidence:

- one completed mission run; current evidence includes a user-completed greybox win run
- one commander-death loss run; deterministic smoke coverage currently proves Commander death and the F7/debug loss path
- smoke coverage for central well retake, enemy Pylon weak point, and tower-wall shutdown
- smoke coverage for Guardian-vs-armor damage math, Medium Tank reveal on both sides, and Rocket Tower anti-armor tuning
- notes for any missing or intentionally placeholder behavior
- localization key coverage for mission result, objective, command, and blocked-action text
- closeout notes should distinguish verified playable behavior from deferred systems such as repair, final balance, and final art

## Milestone 3 Checklist: Colony Pressure Pass

Acceptance checks:

- worker loss creates a real resource/time setback
- worker replacement is possible if affordable
- pressure warning appears before major danger
- pressure creates a choice between repair, defense, expansion, or attack
- pressure does not turn into deep colonist simulation
- RTS pace remains active during pressure events

Evidence:

- before/after notes showing pressure changed player decisions
- tests or debug output for event triggers where available

## Milestone 4 Checklist: Tactical Identity Pass

Acceptance checks:

- infrastructure strikes matter in at least one mission route
- scouting reveals useful tactical information
- Cadet, Rifleman, Guardian, Rover, Commander, Medium Tank, and Heavy Tank roles are distinct
- Guardian is proven as anti-armor infantry without becoming a better anti-infantry Rifleman
- Level 1 starts the player with one Worker, one Guardian, one Rover, and one Commander, while only Worker, Cadet, and Rifleman are trainable
- first enemy AI production can choose between Level 1-available combat troops based on resources and requirements
- enemy can scout/rally before first pressure, retreat damaged attackers, avoid immediately recommitting badly damaged returnees, and regroup after a wiped attack group
- rival-officer memory stays internal and does not add adaptation alerts or hidden-plan UI text
- troop train times vary by unit, with more expensive or heavier units generally taking longer
- explosive friendly fire works if explosive units are present
- normal gunfire does not friendly-fire
- player can win through something smarter than direct unit spam
- Med Hall heals infantry slowly, requires power, and spends resources only while healing
- Logistics / Repair Pad repairs parked vehicles, requires power, and spends resources only while repairing
- Artillery Battery is fragile, long-range, explosive, has a minimum range, and cannot defend itself up close

Evidence:

- one brute-force route note
- one infrastructure-strike route note
- combat/system tests for role-specific rules where practical

## Milestone 5 Checklist: Vertical Slice

Acceptance checks:

- one mission has coherent art direction, sound, UI, and feedback
- basic settings exist
- packaged Windows build runs outside the editor
- known issues are documented
- build version is visible
- first 20 minutes communicate Stratezone's identity
- build is clearly marked prototype, not sellable release

Evidence:

- packaged build smoke notes
- playtest feedback notes
- known issues list

## Public Build Checklist

Before any itch.io or Steam-facing build:

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
