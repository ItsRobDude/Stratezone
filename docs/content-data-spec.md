# Stratezone Content Data Spec

This document defines the first-pass content data shape for Stratezone.

It exists to keep units, buildings, weapons, factions, resources, missions, and events out of hardcoded scene logic. The exact storage format can be JSON, Godot resources, or another text-reviewable format after the Godot scaffold exists, but the fields and ownership rules should stay stable.

## Documentation Role

- **Doc role:** Active source of truth for prototype content data shape.
- **Owns:** stable content IDs, first-pass data categories, required fields, relationships between content records, and validation expectations.
- **Does not own:** final balance, final file format, scene layout, art production, save-state shape, or release packaging.
- **Read when:** adding unit/building/weapon/resource/faction/mission data, creating content validation, or deciding whether a value belongs in content data instead of code.
- **Do not read for:** broad product vision, engine setup, or mission pacing.

## Core Rules

- Content data defines tunable facts.
- Simulation systems interpret content data.
- Presentation displays content data.
- Scene scripts should not be the source of unit stats, costs, weapon rules, resource behavior, or mission objectives.
- Save data should store stable IDs and current simulation state, not display names or Godot node paths.
- Placeholder numbers are allowed, but must be labeled as tunable.

## Storage Direction

The first storage format should be chosen after the Godot scaffold exists.

Acceptable first-pass options:

- JSON files under `game/data/`
- Godot resources under `game/data/`
- CSV only for flat balance tables where relationships stay simple

Preferred early default:

- text-reviewable files that Codex and Git can inspect easily

Avoid:

- stats embedded only in `.tscn` scenes
- display names as identifiers
- separate data copies for UI and simulation
- opaque binary-only data while the schemas are still changing

## Stable ID Rules

Use lowercase snake_case IDs.

Initial IDs are defined here and repeated in `docs/implementation-checklists.md` for checklist convenience.

Rules:

- IDs are stable once referenced by code, save data, tests, or mission data.
- Display names may change without changing IDs.
- New IDs should be added to this document before broad implementation use.
- IDs should describe role and content type, not current balance.
- Do not encode level-specific tuning in the ID.

Examples:

- Good: `unit_worker`
- Good: `building_power_plant`
- Bad: `cheap_worker_v2`
- Bad: `Node2D_EnemyBuilding`

## Data Categories

The first prototype should support these content categories:

- units
- buildings
- weapons
- resources
- resource wells
- factions
- missions
- mission events
- objectives

Future categories such as campaign progression, achievements, upgrades, and store metadata should wait until the related milestone requires them.

## Unit Definition

Required fields:

- `id`
- `display_name`
- `faction_availability`
- `role`
- `cost`
- `health`
- `movement_speed`
- `sight_range`
- `train_requirements`
- `spawn_rule`
- `weapon_ids`
- `can_construct`
- `can_repair`
- `can_attack`
- `can_capture`
- `can_run_over_infantry`
- `tags`

First-pass unit IDs:

- `unit_worker`
- `unit_rifleman`
- `unit_guardian`
- `unit_rover`
- `unit_commander`
- `unit_tank`

Prototype rules:

- `unit_worker` is expensive, non-combat, can construct, can repair, and should flee from danger.
- `unit_commander` is controllable, fragile, pistol-only, and mission-critical in First Landing.
- `unit_rover` scouts, cannot shoot, and may run over enemy infantry.
- `unit_tank` is not normally trainable in Level 1 but may be revealed from a destroyed Colony Hub.

Tunable placeholder example:

```text
id: unit_worker
display_name: Worker
role: builder_repair
cost: 150
health: 60
movement_speed: 1.0
sight_range: 6
train_requirements: building_barracks allows worker training, spawn at building_colony_hub
weapon_ids: []
can_construct: true
can_repair: true
can_attack: false
can_capture: false
can_run_over_infantry: false
tags: worker, non_combat, flees
```

The example is not final balance.

## Building Definition

Required fields:

- `id`
- `display_name`
- `role`
- `cost`
- `health`
- `footprint_radius`
- `placement_buffer`
- `requires_power`
- `provides_power`
- `power_radius`
- `pylon_link_range`
- `provides_training_rules`
- `provides_spawn_location`
- `provides_resource_extraction`
- `extractor_resource_id`
- `wall_anchor`
- `weapon_ids`
- `tags`

First-pass building IDs:

- `building_colony_hub`
- `building_barracks`
- `building_power_plant`
- `building_pylon`
- `building_extractor_refinery`
- `building_defense_tower`
- `building_gun_tower`
- `building_rocket_tower`

Prototype rules:

- `building_colony_hub` is the spawn location for trained units.
- `building_barracks` controls what can be trained by level, troop capacity, and unlocks.
- `building_power_plant` provides local power.
- `building_pylon` extends or links power.
- `building_extractor_refinery` extracts from a resource well and stops when unpowered, destroyed, or depleted.
- tower-class buildings can be wall anchors when powered and compatible.

Tunable placeholder example:

```text
id: building_barracks
display_name: Barracks
role: training_control
cost: 250
health: 400
footprint_radius: 2
placement_buffer: 1
requires_power: true
provides_power: false
power_radius: 0
pylon_link_range: 0
provides_training_rules: true
provides_spawn_location: false
provides_resource_extraction: false
extractor_resource_id: none
wall_anchor: false
weapon_ids: []
tags: production, powered
```

The example is not final balance.

## Weapon Definition

Required fields:

- `id`
- `display_name`
- `damage`
- `range`
- `cooldown`
- `damage_type`
- `area_radius`
- `friendly_fire`
- `target_filters`
- `projectile_behavior`
- `tags`

First-pass weapon IDs:

- `weapon_pistol`
- `weapon_rifle`
- `weapon_guardian_laser`
- `weapon_rocket`

Prototype rules:

- normal gunfire should not cause friendly fire.
- explosive weapons can cause friendly fire.
- Commander uses `weapon_pistol`.
- Guardian uses `weapon_guardian_laser`.

## Resource and Well Definitions

Resource required fields:

- `id`
- `display_name`
- `storage_behavior`
- `shown_in_hud`

Resource well required fields:

- `id`
- `resource_id`
- `capacity`
- `extraction_rate`
- `starts_claimed_by`
- `depletes`
- `tags`

First-pass resource ID:

- `resource_materials`

Prototype rules:

- First Landing uses one limited resource.
- Wells are scarce, trickle income, and can deplete.
- Extractor/Refinery income stops when the well depletes, the building is unpowered, or the building is destroyed.

## Faction Definition

Required fields:

- `id`
- `display_name`
- `color_key`
- `available_unit_ids`
- `available_building_ids`
- `economy_modifiers`
- `production_modifiers`
- `ai_profile_id`
- `tags`

First-pass faction IDs:

- `faction_player_expedition`
- `faction_private_military`

Prototype rules:

- The private military faction uses player-like technology with different color/skin.
- Level 1 private military production should be slower than normal baseline.
- Enemy rebuild and production must spend limited resources.

## Mission Definition

Required fields:

- `id`
- `display_name`
- `target_duration_minutes`
- `map_id`
- `player_faction_id`
- `enemy_faction_ids`
- `starting_entities`
- `starting_resources`
- `resource_wells`
- `objectives`
- `failure_conditions`
- `event_ids`
- `fog_rules`
- `special_rules`
- `tags`

First-pass mission ID:

- `mission_first_landing`

Prototype rules:

- starts already landed
- lasts about 5-10 minutes
- uses bright readable meadow/field terrain with light forest
- includes a controllable Commander at base
- includes one contested central well
- includes a central choke that can be blocked with tower-wall play
- includes an enemy pylon weak point that can disable an enemy tower route
- wins by destroying all required enemy targets
- loses if the Commander dies
- destroying either Colony Hub reveals a tank without changing win/loss by itself

## Mission Event Definition

Required fields:

- `id`
- `display_name`
- `trigger`
- `warning_text`
- `spawn_rules`
- `targeting_rule`
- `resource_cost`
- `repeat_behavior`
- `tags`

Prototype rules:

- events should be inspectable and tunable
- Level 1 pressure should be tame but active
- event warnings should appear before danger when practical

## Objective Definition

Required fields:

- `id`
- `display_name`
- `objective_type`
- `target_filters`
- `success_condition`
- `failure_condition`
- `hud_text`
- `completion_behavior`
- `tags`

Prototype objective types:

- destroy required enemy targets
- protect mission-critical unit
- survive event pressure, if needed for a mission beat

## Validation Expectations

Once content files exist, validation should check:

- all IDs are lowercase snake_case
- all referenced IDs exist
- no duplicate IDs exist
- required fields are present
- numeric values are in sane ranges
- no unit references missing weapons
- no mission references missing factions, objectives, events, resources, or map IDs
- First Landing includes the required units, buildings, resource rules, commander fail state, and enemy faction
- placeholder values are clearly marked or tracked until balanced

Validation should be callable from a documented command once tooling exists.

## Content Data Done Means

A content data change is done when:

- the changed records use stable IDs
- references resolve
- values are not duplicated in scene/UI logic
- relevant system contracts still match
- validation passes once validation exists
- manual closeout reports any placeholder values or assumptions
