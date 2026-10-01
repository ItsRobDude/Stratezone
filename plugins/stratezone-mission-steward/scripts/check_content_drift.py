"""Check locked Stratezone design rules against content data and source docs.

`tools/validate_content.py` owns schema, reference, and i18n structure. This script owns
the design locks from AGENTS.md and the mission docs: what each authored mission may
offer, start with, and fail on. Every mission in game/data/missions gets the shared
checks; missions with documented locks also get their own check function.
"""

from __future__ import annotations

import json
import sys
from collections import Counter
from pathlib import Path
from typing import Any, Callable


REPO_ROOT = Path(__file__).resolve().parents[3]
MISSION_DIR = REPO_ROOT / "game" / "data" / "missions"

# AGENTS.md: cut from the first demo unless the roadmap is explicitly reopened.
CUT_FROM_FIRST_DEMO = {"building_med_hall", "building_logistics_repair_pad"}
# Missions that must exist; their locks are documented.
REQUIRED_MISSIONS = {"mission_first_landing", "mission_wells_at_the_ridge"}


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


def load_missions() -> dict[str, dict[str, Any]]:
    """Every authored mission record (records that carry starting_entities)."""
    missions: dict[str, dict[str, Any]] = {}
    for path in sorted(MISSION_DIR.glob("*.json")):
        with path.open("r", encoding="utf-8") as handle:
            payload = json.load(handle)
        for record in payload.get("records", []):
            if "starting_entities" in record:
                missions[record["id"]] = record
    return missions


class Content:
    def __init__(self) -> None:
        self.units = records_by_id("game/data/units/units.json")
        self.buildings = records_by_id("game/data/buildings/buildings.json")
        self.wells = records_by_id("game/data/resources/resource_wells.json")
        self.i18n_strings = load_json("game/data/i18n/en.json").get("strings", {})


Require = Callable[[bool, str], None]


def faction_units(mission: dict[str, Any], faction_id: str) -> Counter[str]:
    return Counter(
        entity["content_id"]
        for entity in mission.get("starting_entities", [])
        if entity.get("faction_id") == faction_id and str(entity.get("content_id", "")).startswith("unit_")
    )


def faction_buildings(mission: dict[str, Any], faction_id: str) -> Counter[str]:
    return Counter(
        entity["content_id"]
        for entity in mission.get("starting_entities", [])
        if entity.get("faction_id") == faction_id and str(entity.get("content_id", "")).startswith("building_")
    )


def check_content_rules(content: Content, require: Require) -> None:
    """Unit and building locks that hold in every mission."""
    units = content.units
    buildings = content.buildings

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

    guardian = units.get("unit_guardian", {})
    require(
        guardian.get("train_requirements", {}).get("required_barracks_upgrade_id") == "barracks_upgrade_guardian_retrofit",
        "Guardian production must stay gated by the Barracks Guardian upgrade (barracks_upgrade_guardian_retrofit).",
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
    require(
        barracks.get("destroyed_reveal") == {"unit_id": "unit_cadet", "count": 3, "occupant": False},
        "Barracks should keep its quiet three-Cadet destroyed-building reveal.",
    )

    colony_hub = buildings.get("building_colony_hub", {})
    require(
        colony_hub.get("destroyed_reveal") == {"unit_id": "unit_medium_tank", "count": 1, "occupant": True},
        "Colony Hub should keep its Medium Tank occupant destroyed-building reveal.",
    )

    power_plant = buildings.get("building_power_plant", {})
    require(
        power_plant.get("destroyed_reveal") == {"unit_id": "unit_cadet", "count": 1, "occupant": False},
        "Power Plant should keep its quiet one-Cadet destroyed-building reveal.",
    )

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
        require(f"unit.{unit_id}.name" in content.i18n_strings, f"Missing localization name key for {unit_id}.")
        require(
            f"unit.{unit_id}.short_name" in content.i18n_strings,
            f"Missing localization short_name key for {unit_id}.",
        )

    for building_id in buildings:
        require(
            f"building.{building_id}.name" in content.i18n_strings,
            f"Missing localization name key for {building_id}.",
        )
        require(
            f"building.{building_id}.short_name" in content.i18n_strings,
            f"Missing localization short_name key for {building_id}.",
        )


def check_shared_mission_rules(mission: dict[str, Any], content: Content, require: Require) -> None:
    """Locks that apply to every authored mission."""
    mission_id = mission["id"]

    marker_ids = {marker["id"] for marker in mission.get("mission_markers", [])}
    referenced_markers = [entity.get("marker") for entity in mission.get("starting_entities", [])]
    referenced_markers += [placement.get("marker") for placement in mission.get("resource_well_placements", [])]
    for marker in referenced_markers:
        require(marker in marker_ids, f"{mission_id} references missing marker: {marker}")

    for entity in mission.get("starting_entities", []):
        content_id = entity.get("content_id")
        known = content_id in content.units or content_id in content.buildings
        require(known, f"{mission_id} starting entity references unknown content id: {content_id}")

    for well_id in mission.get("resource_wells", []):
        require(well_id in content.wells, f"{mission_id} references unknown resource well: {well_id}")

    for objective_id in mission.get("objectives", []):
        require(
            f"objective.{objective_id}.name" in content.i18n_strings,
            f"{mission_id}: missing localization name key for {objective_id}.",
        )

    for event_id in mission.get("event_ids", []):
        require(
            f"event.{event_id}.name" in content.i18n_strings,
            f"{mission_id}: missing localization name key for {event_id}.",
        )

    # AGENTS.md fog lock: black unexplored, explored stays visible, no last-known shroud.
    fog_rules = mission.get("fog_rules", {})
    require(fog_rules.get("unexplored") == "black", f"{mission_id}: fog must keep unexplored terrain black.")
    require(
        fog_rules.get("explored_terrain_stays_visible") is True,
        f"{mission_id}: fog must keep explored terrain visible.",
    )
    require(
        fog_rules.get("track_last_known_enemies") is False,
        f"{mission_id}: fog should not track last-known enemies as a shroud mechanic.",
    )

    available_buildings = set(mission.get("available_building_ids", []))
    require(
        available_buildings.isdisjoint(CUT_FROM_FIRST_DEMO),
        f"{mission_id} offers buildings cut from the first demo: {sorted(available_buildings & CUT_FROM_FIRST_DEMO)}",
    )

    # AGENTS.md: base-building pressure triggers need grace, cooldown, or coalescing.
    for trigger in mission.get("mission_triggers", []):
        has_damping = any(
            (trigger.get(field) or 0) > 0
            for field in ("min_elapsed_seconds", "cooldown_seconds", "coalesce_window_seconds")
        )
        require(
            has_damping,
            f"{mission_id}: trigger {trigger.get('id')} needs grace, cooldown, or coalescing so early builds don't stack raids.",
        )


def check_first_landing(mission: dict[str, Any], content: Content, require: Require) -> None:
    """Level 1 locks from AGENTS.md and docs/first-landing-mission-spec.md."""
    expected_trainable = ["unit_grunt", "unit_cadet", "unit_rifleman"]
    require(
        mission.get("start_pattern") == "deployed_base",
        "First Landing should keep the deployed-base start pattern.",
    )
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

    player_units = faction_units(mission, mission.get("player_faction_id"))
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

    presentation = mission.get("presentation", {})
    require(
        presentation.get("retry_hint_key") == "mission.mission_first_landing.retry_hint",
        "First Landing should keep its localized retry hint key.",
    )
    require(
        presentation.get("tactical_note_key") == "mission.mission_first_landing.tactical_note",
        "First Landing should keep its localized tactical note key.",
    )

    doc_text = "\n".join(
        [
            read_text("docs/content-data-spec.md"),
            read_text("docs/implementation-checklists.md"),
            read_text("docs/first-landing-mission-spec.md"),
            read_text("docs/system-contracts.md"),
        ]
    )
    for content_id in expected_trainable + sorted(level_1_locked_units):
        require(content_id in doc_text, f"Core Level 1 content id is missing from source docs: {content_id}")


def check_wells_at_the_ridge(mission: dict[str, Any], content: Content, require: Require) -> None:
    """Level 2 locks from docs/content-roadmap.md (Mission 2 and Demo Outline Guardrails)."""
    player_faction = mission.get("player_faction_id")

    # "The player still builds and places the base; do not start with a finished base."
    require(
        mission.get("start_pattern") == "player_places_colony_hub",
        "Wells at the Ridge must keep the player-places-Colony-Hub start, not a prebuilt base.",
    )
    require(
        "player_places_colony_hub" in mission.get("special_rules", []),
        "Wells at the Ridge data should acknowledge the player_places_colony_hub rule.",
    )
    prebuilt = faction_buildings(mission, player_faction)
    require(
        not prebuilt,
        f"Wells at the Ridge must not start the player with buildings; found {dict(prebuilt)}.",
    )

    # "No Vehicle Bay, no Med Hall, no Logistics / Repair Pad, no Artillery."
    deferred_buildings = {
        "building_vehicle_bay",
        "building_med_hall",
        "building_logistics_repair_pad",
        "building_artillery_battery",
    }
    available_buildings = set(mission.get("available_building_ids", []))
    require(
        available_buildings.isdisjoint(deferred_buildings),
        f"Wells at the Ridge includes deferred buildings: {sorted(available_buildings & deferred_buildings)}",
    )

    # No Vehicle Bay means no Rover or tank production; Commanders are never trainable.
    not_trainable = {"unit_rover", "unit_commander", "unit_medium_tank", "unit_tank"}
    trainable_units = set(mission.get("available_unit_ids", []))
    require(
        trainable_units.isdisjoint(not_trainable),
        f"Wells at the Ridge lists units it cannot produce: {sorted(trainable_units & not_trainable)}",
    )

    # "The enemy may contest the central island well, but should not start with it."
    contested_well = "well_wells_ridge_contested"
    contested_markers = {
        placement.get("marker")
        for placement in mission.get("resource_well_placements", [])
        if placement.get("well_id") == contested_well
    }
    require(bool(contested_markers), f"Wells at the Ridge is missing the {contested_well} placement.")
    claimed = [
        f"{entity.get('faction_id')}:{entity.get('content_id')}"
        for entity in mission.get("starting_entities", [])
        if entity.get("marker") in contested_markers
    ]
    require(not claimed, f"The contested central well must start unclaimed; found {claimed}.")

    require(
        "commander_killed" in mission.get("failure_conditions", []),
        "Wells at the Ridge should keep commander_killed as a failure condition.",
    )


MISSION_CHECKS: dict[str, Callable[[dict[str, Any], Content, Require], None]] = {
    "mission_first_landing": check_first_landing,
    "mission_wells_at_the_ridge": check_wells_at_the_ridge,
}


def main() -> int:
    issues: list[str] = []

    def require(condition: bool, message: str) -> None:
        if not condition:
            issues.append(message)

    content = Content()
    missions = load_missions()

    for mission_id in sorted(REQUIRED_MISSIONS):
        require(mission_id in missions, f"{mission_id} is missing from game/data/missions.")

    check_content_rules(content, require)
    for mission_id, mission in missions.items():
        check_shared_mission_rules(mission, content, require)
        mission_check = MISSION_CHECKS.get(mission_id)
        if mission_check is not None:
            mission_check(mission, content, require)

    return finish(issues, sorted(missions))


def finish(issues: list[str], mission_ids: list[str]) -> int:
    if issues:
        print("Stratezone content drift check failed:")
        for issue in issues:
            print(f"- {issue}")
        return 1

    print(f"Stratezone content drift check passed ({', '.join(mission_ids)}).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
