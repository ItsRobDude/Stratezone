from __future__ import annotations

import json
import sys
from collections import Counter
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[3]


def load_json(relative_path: str) -> dict[str, Any]:
    path = REPO_ROOT / relative_path
    with path.open("r", encoding="utf-8") as handle:
        return json.load(handle)


def records_by_id(relative_path: str) -> dict[str, dict[str, Any]]:
    payload = load_json(relative_path)
    return {record["id"]: record for record in payload.get("records", [])}


def read_text(relative_path: str) -> str:
    path = REPO_ROOT / relative_path
    return path.read_text(encoding="utf-8")


def main() -> int:
    issues: list[str] = []

    def require(condition: bool, message: str) -> None:
        if not condition:
            issues.append(message)

    units = records_by_id("game/data/units/units.json")
    buildings = records_by_id("game/data/buildings/buildings.json")
    missions = records_by_id("game/data/missions/first_landing.json")
    wells = records_by_id("game/data/resources/resource_wells.json")
    i18n_strings = load_json("game/data/i18n/en.json").get("strings", {})

    mission = missions.get("mission_first_landing")
    require(mission is not None, "mission_first_landing is missing from first_landing.json.")
    if mission is None:
        return finish(issues)

    marker_ids = {marker["id"] for marker in mission.get("mission_markers", [])}
    referenced_markers = []
    for entity in mission.get("starting_entities", []):
        referenced_markers.append(entity.get("marker"))
    for placement in mission.get("resource_well_placements", []):
        referenced_markers.append(placement.get("marker"))
    for marker in referenced_markers:
        require(marker in marker_ids, f"Mission references missing marker: {marker}")

    for entity in mission.get("starting_entities", []):
        content_id = entity.get("content_id")
        known = content_id in units or content_id in buildings
        require(known, f"Starting entity references unknown content id: {content_id}")

    for well_id in mission.get("resource_wells", []):
        require(well_id in wells, f"Mission references unknown resource well: {well_id}")

    expected_trainable = ["unit_grunt", "unit_cadet", "unit_rifleman"]
    require(
        mission.get("available_unit_ids") == expected_trainable,
        "Level 1 trainable units must stay exactly: unit_grunt, unit_cadet, unit_rifleman.",
    )

    level_1_locked_units = {
        "unit_guardian",
        "unit_rover",
        "unit_commander",
        "unit_medium_tank",
        "unit_tank",
    }
    trainable_units = set(mission.get("available_unit_ids", []))
    require(
        trainable_units.isdisjoint(level_1_locked_units),
        f"Level 1 has locked units in trainable list: {sorted(trainable_units & level_1_locked_units)}",
    )

    player_units = Counter(
        entity["content_id"]
        for entity in mission.get("starting_entities", [])
        if entity.get("faction_id") == mission.get("player_faction_id")
        and str(entity.get("content_id", "")).startswith("unit_")
    )
    expected_player_units = Counter(
        {
            "unit_grunt": 1,
            "unit_guardian": 1,
            "unit_rover": 1,
            "unit_commander": 1,
        }
    )
    require(
        player_units == expected_player_units,
        f"Level 1 player starting units drifted. Expected {dict(expected_player_units)}, found {dict(player_units)}.",
    )

    unavailable_level_1_buildings = {
        "building_armory_annex",
        "building_vehicle_bay",
        "building_med_hall",
        "building_logistics_repair_pad",
        "building_artillery_battery",
    }
    available_buildings = set(mission.get("available_building_ids", []))
    require(
        available_buildings.isdisjoint(unavailable_level_1_buildings),
        f"Level 1 includes deferred buildings: {sorted(available_buildings & unavailable_level_1_buildings)}",
    )

    require(
        "commander_killed" in mission.get("failure_conditions", []),
        "Level 1 must keep commander_killed as a failure condition.",
    )
    require(
        "colony_hub_destroyed_reveals_tank" in mission.get("special_rules", []),
        "First Landing data should acknowledge the permanent Colony Hub Medium Tank occupant rule.",
    )

    fog_rules = mission.get("fog_rules", {})
    require(fog_rules.get("unexplored") == "black", "Fog rules must keep unexplored terrain black.")
    require(
        fog_rules.get("explored_terrain_stays_visible") is True,
        "Fog rules must keep explored terrain visible.",
    )
    require(
        fog_rules.get("track_last_known_enemies") is False,
        "Level 1 should not track last-known enemies as a shroud mechanic.",
    )

    ai_profile = mission.get("enemy_ai_profile", {})
    attack_group_size = ai_profile.get("attack_group_size")
    require(
        isinstance(attack_group_size, int) and 1 <= attack_group_size <= 3,
        "Level 1 enemy attack groups should stay small and readable: 1 to 3 attackers.",
    )
    require(
        ai_profile.get("first_attack_delay_seconds", 0) >= 60,
        "Level 1 first attack delay should leave a readable setup window.",
    )
    require(
        ai_profile.get("pressure_slowdown_multiplier", 1) <= 1,
        "Level 1 pressure slowdown multiplier should not speed pressure above baseline.",
    )

    grunt = units.get("unit_grunt", {})
    require(grunt.get("can_construct") is True, "Grunt must be able to construct.")
    require(grunt.get("can_repair") is True, "Grunt must be able to repair.")
    require(grunt.get("can_attack") is False, "Grunt must remain non-combat in the first prototype.")

    rover = units.get("unit_rover", {})
    require(rover.get("can_attack") is False, "Rover should remain a no-gun scout in Level 1.")
    require(rover.get("can_run_over_infantry") is True, "Rover should keep its crush/scout identity if present.")

    commander = units.get("unit_commander", {})
    require(commander.get("role") == "mission_critical_vip", "Commander should stay mission-critical.")
    require(
        commander.get("faction_availability") == ["faction_player_expedition"],
        "Commander should not become a normal enemy/trainable unit.",
    )

    medium_tank = units.get("unit_medium_tank", {})
    require(
        medium_tank.get("spawn_rule") == "colony_hub_destroyed_reveal",
        "Medium Tank should stay the permanent Colony Hub destruction occupant.",
    )
    require(
        "hub_destroy_reveal" in medium_tank.get("tags", []),
        "Medium Tank should keep the permanent Hub-destruction reveal tag.",
    )

    heavy_tank = units.get("unit_tank", {})
    require(
        heavy_tank.get("spawn_rule") != "colony_hub_destroyed_reveal",
        "Heavy Tank should not be the Colony Hub destruction occupant.",
    )

    barracks = buildings.get("building_barracks", {})
    require(barracks.get("requires_power") is True, "Barracks should require power.")
    require(barracks.get("provides_training_rules") is True, "Barracks should own training rules.")

    vehicle_bay = buildings.get("building_vehicle_bay", {})
    require(vehicle_bay.get("requires_power") is True, "Vehicle Bay should remain powered.")
    require(
        vehicle_bay.get("requires_adjacent_building_id") == "building_barracks",
        "Vehicle Bay should remain a physical Barracks add-on.",
    )

    for tower_id in ["building_defense_tower", "building_gun_tower", "building_rocket_tower"]:
        tower = buildings.get(tower_id, {})
        require(tower.get("wall_anchor") is True, f"{tower_id} should remain a wall anchor.")
        require(tower.get("requires_power") is True, f"{tower_id} should require power.")

    for tower_id in ["building_gun_tower", "building_rocket_tower"]:
        tower = buildings.get(tower_id, {})
        require(
            tower.get("upgrade_from_building_id") == "building_defense_tower",
            f"{tower_id} should upgrade from building_defense_tower.",
        )
        require(
            tower.get("upgrade_preserves_wall_anchor") is True,
            f"{tower_id} should preserve wall-anchor behavior.",
        )

    for unit_id in units:
        require(f"unit.{unit_id}.name" in i18n_strings, f"Missing localization name key for {unit_id}.")
        require(f"unit.{unit_id}.short_name" in i18n_strings, f"Missing localization short_name key for {unit_id}.")

    for building_id in buildings:
        require(
            f"building.{building_id}.name" in i18n_strings,
            f"Missing localization name key for {building_id}.",
        )
        require(
            f"building.{building_id}.short_name" in i18n_strings,
            f"Missing localization short_name key for {building_id}.",
        )

    for objective_id in mission.get("objectives", []):
        require(
            f"objective.{objective_id}.name" in i18n_strings,
            f"Missing localization name key for {objective_id}.",
        )

    for event_id in mission.get("event_ids", []):
        require(
            f"event.{event_id}.name" in i18n_strings,
            f"Missing localization name key for {event_id}.",
        )

    doc_text = "\n".join(
        [
            read_text("docs/content-data-spec.md"),
            read_text("docs/implementation-checklists.md"),
            read_text("docs/first-landing-mission-spec.md"),
        ]
    )
    for content_id in expected_trainable + sorted(level_1_locked_units):
        require(content_id in doc_text, f"Core Level 1 content id is missing from source docs: {content_id}")

    return finish(issues)


def finish(issues: list[str]) -> int:
    if issues:
        print("Stratezone content drift check failed:")
        for issue in issues:
            print(f"- {issue}")
        return 1

    print("Stratezone content drift check passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())

