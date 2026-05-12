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

- content name keys are derived from stable IDs, such as `unit.unit_grunt.name`, `building.building_power_plant.name`, `resource.resource_materials.name`, and `faction.faction_private_military.name`
- mission, objective, command, HUD, warning, validation, and result text should use explicit localization keys
- mission presentation fields such as briefing title/body, objective HUD text, warning text, map callouts, failure messages, success messages, retry hints, and tactical notes must be localizable once wired
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

- Good: `unit_grunt`
- Good: `building_power_plant`
- Bad: `cheap_grunt_v2`
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
- Barracks upgrades

Future categories such as campaign progression, achievements, broad abstract upgrade trees, and store metadata should wait until the related milestone requires them. Vehicle Bay add-ons and armed tower upgrades should be represented as building records first, because they are physical map objects or in-place building conversions. Guardian production is represented by one explicit Barracks upgrade record, not an Armory Annex building.

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
- `required_barracks_upgrade_id`

First-pass unit IDs:

- `unit_grunt`
- `unit_cadet`
- `unit_rifleman`
- `unit_guardian`
- `unit_rover`
- `unit_commander`
- `unit_medium_tank`
- `unit_tank`

Prototype rules:

- `unit_grunt` is expensive, non-combat, can construct, can repair, and should flee from danger.
- `unit_cadet` is the cheapest and fastest trainable infantry. It should cost less than Rifleman, have lower health and damage, and recruit in only a few seconds.
- `unit_rifleman` is intentionally fragile. First-pass health should stay around 40-50 so infantry caught out of position die fast. It should train quickly, roughly 3-4 seconds in the intended fast classic-RTS feel, and only slightly slower than Cadet.
- `unit_commander` is controllable, fragile, pistol-only, and mission-critical in First Landing.
- `unit_rover` scouts, cannot shoot, and may run over enemy infantry.
- `unit_guardian` is the anti-armor infantry proof role: slower and more expensive than Rifleman, lower raw damage than Rifleman, but energy damage that performs meaningfully better against Medium and Heavy Tanks than Rifleman ballistics.
- `unit_guardian` requires `barracks_upgrade_guardian_retrofit` where the mission enables Guardian production.
- `unit_rover` should require `building_vehicle_bay` where Barracks add-ons are enabled by the mission.
- In First Landing, `unit_guardian`, `unit_rover`, and `unit_commander` may be authored as starting/scenario units but must not be listed as trainable mission units.
- `unit_medium_tank` is the permanent Hub-destruction occupant tank for every mission. It is not normally trainable in the early demo, has lower health and smaller splash than the Heavy Tank, and its shell should leave a full-health Rifleman near 30 percent health.
- `unit_tank` is now the Heavy Tank record. It is the promoted old tank profile, is not the Colony Hub occupant, and should remain a heavier later answer with stronger explosive splash and high ballistic resistance.
- Troop training time varies by unit. Cadet is fastest, Rifleman is only slightly slower, and Guardian is slower because it is a specialized anti-armor / anti-defense unit.
- Unit attack speed, damage, range, damage type, area, and friendly-fire behavior live directly on the unit record.
- Units have health and resistances; armor is not a pickup or separate equipment system in the first prototype.

Tunable placeholder example:

```text
id: unit_grunt
display_name: Grunt
role: builder_repair
cost: 150
train_time_seconds: 18
health: 60
damage_resistances: ballistic 0.0, energy 0.0, explosive -0.15, crush 0.0
movement_speed: 1.0
sight_range: 6
train_requirements: building_barracks allows grunt training, spawn at building_colony_hub
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
tags: grunt, non_combat, flees
```

The example is not final balance.

First-pass resistance intent:

- Basic infantry should die fast against other infantry.
- Heavy armor should feel nearly impenetrable to ballistic infantry fire, while Medium Tanks should be meaningfully faster to kill than Heavy Tanks.
- Guardian energy fire should be worse than Rifleman fire against basic infantry but more than twice as effective as Rifleman fire against Medium and Heavy Tanks.
- Released Medium Tanks and Rocket Tower explosives should also outperform Rifleman/Gun Tower ballistics against armored vehicles.
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
- `building_armory_annex` (legacy reserved record; not the first-demo Guardian unlock path)
- `building_vehicle_bay`
- `building_med_hall`
- `building_logistics_repair_pad`
- `building_artillery_battery`

Prototype rules:

- `building_colony_hub` is the spawn location for trained units.
- `building_barracks` controls what can be trained by level, troop capacity, and unlocks.
- `building_armory_annex` is a legacy reserved record. The first-demo Guardian path is a Barracks upgrade, not an Armory Annex building.
- `building_vehicle_bay` is a powered Barracks add-on that unlocks Rover training and heavy-armor capacity where the mission allows it. In First Landing it is silently locked and hidden from the player.
- `building_power_plant` provides local power.
- `building_pylon` extends or links power.
- prototype Pylon link range is `30` content units so expansion chains read as deliberate infrastructure, not dense pylon spam.
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

Barracks upgrade example:

```text
id: barracks_upgrade_guardian_retrofit
display_name: Guardian Retrofit
cost: 350
duration_seconds: 25
required_grunt_count: 2
required_grunt_range: 10
requires_powered_barracks: true
requires_colony_hub: true
unlock_unit_ids: unit_guardian
tags: barracks, upgrade, guardian_unlock
```

This is a concrete Barracks upgrade record, not the start of a broad research tree. The runtime requires a powered live Barracks, live Colony Hub, enough nearby Grunts, clear Barracks training queue, and materials before it starts the timed retrofit.

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
- `building_logistics_repair_pad` is a powered platform that repairs mechanical units parked on it. Grunts repair buildings only; vehicle repair belongs to the pad or another dedicated system, not the Grunt repair command.
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

Planned first-pass map logic fields:

- `terrain_regions`: authored rectangles, polygons, or circles with stable IDs, terrain kind, and gameplay flags
- `map_objects`: authored map-owned objects such as bridges, with stable IDs, shape, geometry, health, starting intact state, passability flags, and tags
- `requires_buildable_regions`: optional boolean; default false. When false, base missions allow building anywhere except blocked terrain, resource-well reservations, power/support limits, footprint overlap, and mission-specific rules. Set true only for an explicitly restricted scenario.
- `buildable_regions`: authored base/expansion readability hints where normal footprint/buffer rules still apply if `requires_buildable_regions` is true
- `blocked_regions`: impassable terrain such as cliffs, ridges, deep water, wreck fields, or map-edge blockers
- `resource_basins`: visual/logical pockets around important wells
- `chokepoint_markers`: authored spots intended for Defense Tower wall play, attack lanes, or route proof
- `visual_lanes`: roads, dirt paths, or open corridors that guide the player visually before any movement-speed bonus exists

First-pass `map_objects` bridge shape:

```json
{
  "id": "bridge_central_isle_west",
  "object_type": "bridge",
  "shape": "rect",
  "center": { "x": -260, "y": 0 },
  "size": { "x": 180, "y": 90 },
  "max_health": 600,
  "starts_intact": true,
  "blocks_movement_when_broken": true,
  "tags": ["bridge", "central_isle"]
}
```

Bridge prototype rules:

- Bridge objects are geographic facts of the map, not a generic neutral-infrastructure framework.
- Intact bridges subtract passable geometry from blocked water/cliff terrain for pathfinding.
- Broken bridges add no passable geometry, so underlying blocked terrain applies normally.
- Bridge IDs must be stable because missions, editor exports, smoke tests, and future saves may refer to them.

First-pass map ID:

- `map_first_landing_greybox`

Prototype rules:

- First Landing starts as a small greybox map.
- The map should include a player start, enemy edge-of-fog reveal, central choke, contested well, and enemy pylon weak point.
- Terrain and art values are placeholders until the first playable map exists.
- Milestone 3 should make terrain regions real enough to support Level 2 route proof, with the F5 in-game map editor/tuner used as an internal Stratezone-shaped authoring aid for mission markers, terrain regions, and map objects. It may create, move, resize, delete, inspect, validate, export snippets, and guarded-save owned marker/region/object arrays, but content JSON remains the canonical reviewable data store.
- Milestone 4 should prove at least one blocked terrain feature, one readable base/expansion pocket, one resource basin, and one Defense Tower wall chokepoint. Base missions should not use buildable pockets as a hidden whitelist unless explicitly specified.

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
- `start_pattern`
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

Allowed `start_pattern` values:

- `deployed_base`: the player begins with an authored live base.
- `player_places_colony_hub`: the player starts with a force and must place the first Colony Hub.
- `partial_damaged_base`: the player begins with a damaged or disrupted authored base.
- `no_base_force`: the player begins with units/equipment and no normal base.
- `allotted_force`: the mission starts from a fixed authored force, with limited or no production assumptions.

Mission presentation fields:

- `presentation.briefing_title_key`, `briefing_body_key`, `start_objective_key`, `success_key`, and `failure_key` are localized text hooks for the core mission flow.
- `presentation.retry_hint_key` is an optional localized hint shown on mission failure.
- `presentation.tactical_note_key` is an optional localized HUD/debug note for player-known tactical framing.
- `presentation.map_callouts` is an optional list of sparse callouts with `marker` and `text_key`; each marker must match an authored mission marker and each text key must exist in localization.
- Presentation fields must not reveal hidden enemy plans. They should name known terrain, known risks, or concise retry advice.

Mission marker fields:

- `mission_markers` use stable `id` plus `position`.
- `mission_markers.tags` is optional, and should be used for required-feature validation when the marker's role is broader than its exact ID, such as `player_start`, `contested_well`, or `enemy_edge_of_fog`.
- Required-feature validation should prefer exact IDs and tags over marker-name substring guesses.

Prototype rules:

- starts already landed
- lasts about 5-10 minutes
- uses bright readable meadow/field terrain with light forest
- includes a controllable Commander at base
- includes one contested central well
- includes a central choke that can be blocked with tower-wall play
- includes an enemy pylon weak point that can disable an enemy tower route
- includes authored mission markers for base positions, wells, AI build slots, rally points, and choke points
- includes an enemy AI profile for first rebuild delay, first central-well claim delay, first attack delay, optional early patrol delay/interval/count, rebuild cadence, production cadence, attack group size, patrol group size, central-well interest, contested-well rebuild cooldown/limit, pressure slowdown, train-time multiplier, and authored patrol marker list
- may include `presentation` keys for briefing title/body, start objective, success, failure, retry hints, tactical notes, and map callouts; these keys must exist in localization once wired
- may include `mission_triggers` for authored pacing beats such as coalesced base-building pressure
- may include `mission_object_overrides`, each with `object_id` and `starting_health_percent`, so a later mission can start with a damaged map object without duplicating the map record
- uses `available_unit_ids` as the trainable-unit truth for both player production and enemy AI production in that mission
- uses `available_building_ids` to hide or lock mission-inappropriate build and upgrade commands without deleting future content records
- future first-demo missions should use authored `starting_entities`, `starting_resources`, `available_unit_ids`, and `available_building_ids` rather than a player-selected loadout system
- future first-demo missions may use partial bases, allotted or irreplaceable troops, or no-base starts when the mission proof target needs that shape
- starts the player with exactly one `unit_grunt`, one `unit_guardian`, one `unit_rover`, and one `unit_commander`
- exposes `unit_grunt`, `unit_cadet`, and `unit_rifleman` as Level 1 trainable units; `unit_guardian`, `unit_rover`, and `unit_commander` stay scenario/start-only for Level 1
- hides `building_vehicle_bay` in Level 1 rather than presenting it as a disabled command
- wins by destroying all required enemy targets
- loses if the Commander dies
- destroying either Colony Hub releases a Medium Tank occupant; hostile Hub occupants are required targets and prevent mission completion while alive

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
- `mission_triggers` now support first-pass pacing guards through watched player building IDs, minimum elapsed time, coalescing window, cooldown, max fire count, and enemy attack group size
- early base-building triggers such as Barracks built and first Extractor built should coalesce into one pressure beat instead of firing back-to-back raids
- new warning text should use localization keys; current `warning_text` fields are English prototype fallbacks until older event records are migrated to the keyed presentation path

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

Localization guidance:

- new objective HUD text should use localization keys; current `hud_text` fields are English prototype fallbacks until objective presentation is fully keyed
- failure and success messages should use stable result keys plus arguments, not raw localized strings in simulation rules

First-demo failure-condition guidance:

- authored missions may fail on declared critical units, Colony Hub/base loss, transport/extraction loss, required Grunt/equipment loss, or combinations that remain readable
- timer-expiry mission failure is not planned for the first demo unless the roadmap is explicitly reopened
- countdown-style events may exist for warnings, arrivals, extraction pacing, or optional pressure, but not as hidden mission-loss timers

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
