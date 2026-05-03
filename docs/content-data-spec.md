# Stratezone Content Data Spec

This document defines the first-pass content data shape for Stratezone.

It exists to keep units, buildings, factions, resources, missions, and events out of hardcoded scene logic. Combat stats live directly on unit and building records for now; Stratezone does not use separate weapon records in the first prototype.

## Documentation Role

- **Doc role:** Active source of truth for prototype content data shape.
- **Owns:** stable content IDs, first-pass data categories, required fields, relationships between content records, and validation expectations.
- **Does not own:** final balance, final file format, scene layout, art production, save-state shape, or release packaging.
- **Read when:** adding unit/building/resource/faction/mission data, creating content validation, or deciding whether a value belongs in content data instead of code.
- **Do not read for:** broad product vision, engine setup, or mission pacing.

## Core Rules

- Content data defines tunable facts.
- Simulation systems interpret content data.
- Presentation displays content data.
- Scene scripts should not be the source of unit stats, costs, attack rules, resource behavior, or mission objectives.
- Save data should store stable IDs and current simulation state, not display names or Godot node paths.
- Placeholder numbers are allowed, but must be labeled as tunable.
- Player-facing names and UI copy should resolve through localization keys; `display_name` is English fallback only.

## Storage Direction

The current first-pass storage format is JSON under `game/data/`.

This is intentionally simple and text-reviewable. Godot resources can replace or wrap this later only if they make authoring, validation, or runtime loading meaningfully better.

Acceptable future options:

- Godot resources under `game/data/`
- CSV only for flat balance tables where relationships stay simple

Avoid:

- stats embedded only in `.tscn` scenes
- display names as identifiers
- separate data copies for UI and simulation
- separate weapon records before the game actually needs equipment/modular weapons
- opaque binary-only data while the schemas are still changing

## Localization Data

First-pass localization lives under `game/data/i18n/`.

Current file:

- `game/data/i18n/en.json`

Rules:

- content name keys are derived from stable IDs, such as `unit.unit_worker.name`, `building.building_power_plant.name`, `resource.resource_materials.name`, and `faction.faction_private_military.name`
- mission, objective, command, HUD, warning, validation, and result text should use explicit localization keys
- localized strings must not be used as save IDs, content references, test identity, or gameplay rule inputs
- existing `display_name` fields remain English fallback during the prototype
- missing player-facing keys should be treated as validation failures once a surface is wired to localization

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
- resources
- resource wells
- maps
- factions
- missions
- mission events
- objectives

Future categories such as campaign progression, achievements, abstract upgrade trees, and store metadata should wait until the related milestone requires them. Barracks add-ons and armed tower upgrades should be represented as building records first, because they are physical map objects or in-place building conversions.

## Unit Definition

Required fields:

- `id`
- `display_name`
- `faction_availability`
- `role`
- `cost`
- `train_time_seconds`
- `health`
- `damage_resistances`
- `movement_speed`
- `sight_range`
- `train_requirements`
- `spawn_rule`
- `attack_damage`
- `attack_range`
- `attack_cooldown`
- `damage_type`
- `area_radius`
- `friendly_fire`
- `target_filters`
- `can_construct`
- `can_repair`
- `can_attack`
- `can_capture`
- `can_run_over_infantry`
- `tags`

Optional train requirement fields:

- `required_addon_building_id`

First-pass unit IDs:

- `unit_worker`
- `unit_cadet`
- `unit_rifleman`
- `unit_guardian`
- `unit_rover`
- `unit_commander`
- `unit_medium_tank`
- `unit_tank`

Prototype rules:

- `unit_worker` is expensive, non-combat, can construct, can repair, and should flee from danger.
- `unit_cadet` is the cheapest trainable infantry. It should cost less than Rifleman and have lower health and damage.
- `unit_rifleman` is intentionally fragile. First-pass health should stay around 40-50 so infantry caught out of position die fast.
- `unit_commander` is controllable, fragile, pistol-only, and mission-critical in First Landing.
- `unit_rover` scouts, cannot shoot, and may run over enemy infantry.
- `unit_guardian` is the anti-armor infantry proof role: lower raw damage than Rifleman, but energy damage that performs meaningfully better against Medium and Heavy Tanks than Rifleman ballistics.
- `unit_guardian` should require `building_armory_annex` where Barracks add-ons are enabled by the mission.
- `unit_rover` should require `building_vehicle_bay` where Barracks add-ons are enabled by the mission.
- In First Landing, `unit_guardian`, `unit_rover`, and `unit_commander` may be authored as starting/scenario units but must not be listed as trainable mission units.
- `unit_medium_tank` is the Level 1 reveal tank. It is not normally trainable, has lower health and smaller splash than the Heavy Tank, and its shell should leave a full-health Rifleman near 30 percent health.
- `unit_tank` is now the Heavy Tank record. It is the promoted old tank profile and should remain a heavier later answer with stronger explosive splash and high ballistic resistance.
- Troop training time varies by unit. More expensive or heavier units should generally take longer.
- Unit attack speed, damage, range, damage type, area, and friendly-fire behavior live directly on the unit record.
- Units have health and resistances; armor is not a pickup or separate equipment system in the first prototype.

Tunable placeholder example:

```text
id: unit_worker
display_name: Worker
role: builder_repair
cost: 150
train_time_seconds: 18
health: 60
damage_resistances: ballistic 0.0, energy 0.0, explosive -0.15, crush 0.0
movement_speed: 1.0
sight_range: 6
train_requirements: building_barracks allows worker training, spawn at building_colony_hub
attack_damage: 0
attack_range: 0
attack_cooldown: 0
damage_type: none
area_radius: 0
friendly_fire: false
target_filters: none
can_construct: true
can_repair: true
can_attack: false
can_capture: false
can_run_over_infantry: false
tags: worker, non_combat, flees
```

The example is not final balance.

First-pass resistance intent:

- Basic infantry should die fast against other infantry.
- Heavy armor should feel nearly impenetrable to ballistic infantry fire, while Medium Tanks should be meaningfully faster to kill than Heavy Tanks.
- Guardian energy fire should be worse than Rifleman fire against basic infantry but more than twice as effective as Rifleman fire against Medium and Heavy Tanks.
- Revealed Medium Tanks and Rocket Tower explosives should also outperform Rifleman/Gun Tower ballistics against armored vehicles.
- Buildings should resist casual ballistic damage enough to preserve siege pacing.
- Buildings should have negative explosive resistance so Rocket Towers, Tanks, and later siege weapons are the base-cracking lane.
- The Colony Hub should keep its early siege ratio of 1200 health and 0.25 ballistic resistance unless playtests prove the ratio wrong.
- Rover and tank crush damage should remain high enough to instantly kill basic infantry when the player micros vehicles into exposed infantry.

## Building Definition

Required fields:

- `id`
- `display_name`
- `role`
- `cost`
- `build_time_seconds`
- `health`
- `damage_resistances`
- `footprint_radius`
- `placement_buffer`
- `requires_power`
- `provides_power`
- `power_radius`
- `pylon_link_range`
- `wall_link_range`
- `provides_training_rules`
- `provides_spawn_location`
- `provides_resource_extraction`
- `extractor_resource_id`
- `wall_anchor`
- `attack_damage`
- `attack_range`
- `attack_cooldown`
- `damage_type`
- `area_radius`
- `friendly_fire`
- `target_filters`
- `tags`

Optional relationship fields for physical add-ons and in-place upgrades:

- `requires_adjacent_building_id`
- `training_unlock_unit_ids`
- `troop_capacity_delta`
- `heavy_armor_capacity_delta`
- `upgrade_from_building_id`
- `upgrade_preserves_wall_anchor`

First-pass building IDs:

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

Prototype rules:

- `building_colony_hub` is the spawn location for trained units.
- `building_barracks` controls what can be trained by level, troop capacity, and unlocks.
- `building_armory_annex` is a powered Barracks add-on that unlocks Guardian training and explosive tech where the mission allows it.
- `building_vehicle_bay` is a powered Barracks add-on that unlocks Rover training and heavy-armor capacity where the mission allows it.
- `building_power_plant` provides local power.
- `building_pylon` extends or links power.
- `building_extractor_refinery` extracts from a resource well and stops when unpowered, destroyed, or depleted.
- `building_med_hall` heals infantry in a radius, requires power, and spends resources while actively healing.
- `building_logistics_repair_pad` repairs parked vehicles, requires power, and spends resources while actively repairing.
- tower-class buildings can be wall anchors when powered and compatible.
- powered wall anchors create energy wall segments with nearby powered wall anchors within `wall_link_range`.
- `building_gun_tower` and `building_rocket_tower` should normally be created by upgrading `building_defense_tower` in place and should preserve wall-anchor behavior.
- `building_artillery_battery` is fragile static siege infrastructure with long range, explosive damage, friendly fire, and a minimum range.
- Building construction is instant for now: `build_time_seconds` should be `0` for first-pass buildings.
- Armed tower attack stats live directly on the building record.

Tunable placeholder example:

```text
id: building_barracks
display_name: Barracks
role: training_control
cost: 250
build_time_seconds: 0
health: 400
damage_resistances: ballistic 0.2, energy 0.1, explosive 0.0, crush 0.4
footprint_radius: 2
placement_buffer: 1
requires_power: true
provides_power: false
power_radius: 0
pylon_link_range: 0
wall_link_range: 0
provides_training_rules: true
provides_spawn_location: false
provides_resource_extraction: false
extractor_resource_id: none
requires_adjacent_building_id: none
training_unlock_unit_ids: none
troop_capacity_delta: 0
heavy_armor_capacity_delta: 0
upgrade_from_building_id: none
upgrade_preserves_wall_anchor: false
wall_anchor: false
attack_damage: 0
attack_range: 0
attack_cooldown: 0
damage_type: none
area_radius: 0
friendly_fire: false
target_filters: none
tags: production, powered
```

The example is not final balance.

Barracks add-on placeholder example:

```text
id: building_armory_annex
display_name: Armory Annex
role: barracks_addon_unlock
cost: 180
build_time_seconds: 0
requires_power: true
requires_adjacent_building_id: building_barracks
training_unlock_unit_ids: unit_guardian
troop_capacity_delta: 0
heavy_armor_capacity_delta: 0
upgrade_from_building_id: none
upgrade_preserves_wall_anchor: false
wall_anchor: false
tags: production, addon, powered
```

In-place tower upgrade placeholder example:

```text
id: building_rocket_tower
display_name: Rocket Tower
role: explosive_wall_anchor
cost: 420
build_time_seconds: 0
requires_power: true
requires_adjacent_building_id: none
training_unlock_unit_ids: none
troop_capacity_delta: 0
heavy_armor_capacity_delta: 0
upgrade_from_building_id: building_defense_tower
upgrade_preserves_wall_anchor: true
wall_anchor: true
wall_link_range: 7
attack_damage: 55
attack_cooldown: 2.4
damage_type: explosive
area_radius: 2
friendly_fire: true
tags: tower, wall_anchor, armed, explosive, powered, upgrade
```

Support and siege building placeholder intent:

- `building_med_hall` heals infantry within a radius, slowly spends resources while healing, and requires power.
- `building_logistics_repair_pad` is a powered platform that repairs mechanical units parked on it, keeping Workers out of front-line vehicle repair when the player plans ahead.
- `building_artillery_battery` is a fragile, expensive static siege emplacement with long range, explosive damage, friendly fire, and a minimum range that prevents close self-defense.

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
- Open resource wells are reserved for Extractor/Refinery placement; non-extractor buildings should not cover usable wells.
- Destroyed Extractor/Refinery buildings release their well claim; wrecks must not prevent a new Extractor from being placed on the same well.

## Map Definition

Required fields:

- `id`
- `display_name`
- `biome`
- `target_size`
- `required_features`
- `tags`

First-pass map ID:

- `map_first_landing_greybox`

Prototype rules:

- First Landing starts as a small greybox map.
- The map should include a player start, enemy edge-of-fog reveal, central choke, contested well, and enemy pylon weak point.
- Terrain and art values are placeholders until the first playable map exists.

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
- `available_unit_ids`
- `available_building_ids`
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
- includes authored mission markers for base positions, wells, AI build slots, rally points, and choke points
- includes an enemy AI profile for first attack delay, rebuild cadence, production cadence, attack group size, central-well interest, pressure slowdown, and train-time multiplier
- uses `available_unit_ids` as the trainable-unit truth for both player production and enemy AI production in that mission
- uses `available_building_ids` to hide or lock mission-inappropriate build and upgrade commands without deleting future content records
- starts the player with exactly one `unit_worker`, one `unit_guardian`, one `unit_rover`, and one `unit_commander`
- exposes `unit_worker`, `unit_cadet`, and `unit_rifleman` as Level 1 trainable units; `unit_guardian`, `unit_rover`, and `unit_commander` stay scenario/start-only for Level 1
- wins by destroying all required enemy targets
- loses if the Commander dies
- destroying either Colony Hub reveals a Medium Tank without changing win/loss by itself; reveal-only tanks are not required destroy-all targets

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
- required English localization keys exist
- required fields are present
- numeric values are in sane ranges
- no unit or building uses separate `weapon_ids`
- combat-capable units and armed buildings include direct attack stats
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
