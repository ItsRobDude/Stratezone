from __future__ import annotations

import json
from collections import Counter
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]


def load_json(relative_path: str) -> dict:
    path = REPO_ROOT / relative_path
    with path.open("r", encoding="utf-8") as handle:
        return json.load(handle)


def records_by_id(relative_path: str) -> dict[str, dict]:
    payload = load_json(relative_path)
    return {record["id"]: record for record in payload.get("records", [])}


def names_for(ids: list[str], catalog: dict[str, dict]) -> list[str]:
    return [catalog.get(item_id, {}).get("display_name", item_id) for item_id in ids]


def format_counter(counter: Counter[str], catalog: dict[str, dict]) -> str:
    if not counter:
        return "none"
    parts = []
    for item_id, count in sorted(counter.items()):
        name = catalog.get(item_id, {}).get("display_name", item_id)
        parts.append(f"{count}x {name} ({item_id})")
    return ", ".join(parts)


def main() -> int:
    units = records_by_id("game/data/units/units.json")
    buildings = records_by_id("game/data/buildings/buildings.json")
    missions = records_by_id("game/data/missions/first_landing.json")
    mission = missions["mission_first_landing"]

    player_faction = mission["player_faction_id"]
    enemy_factions = set(mission.get("enemy_faction_ids", []))

    player_units: Counter[str] = Counter()
    player_buildings: Counter[str] = Counter()
    enemy_units: Counter[str] = Counter()
    enemy_buildings: Counter[str] = Counter()

    for entity in mission.get("starting_entities", []):
        content_id = entity["content_id"]
        faction_id = entity["faction_id"]
        if content_id.startswith("unit_"):
            if faction_id == player_faction:
                player_units[content_id] += 1
            elif faction_id in enemy_factions:
                enemy_units[content_id] += 1
        elif content_id.startswith("building_"):
            if faction_id == player_faction:
                player_buildings[content_id] += 1
            elif faction_id in enemy_factions:
                enemy_buildings[content_id] += 1

    available_units = mission.get("available_unit_ids", [])
    authored_only_units = sorted(set(player_units) - set(available_units))
    available_buildings = mission.get("available_building_ids", [])
    hidden_level_1_buildings = [
        building_id
        for building_id in [
            "building_armory_annex",
            "building_vehicle_bay",
            "building_med_hall",
            "building_logistics_repair_pad",
            "building_artillery_battery",
        ]
        if building_id in buildings and building_id not in available_buildings
    ]

    print("Stratezone Mission Truth Report")
    print("==============================")
    print(f"Mission: {mission.get('display_name')} ({mission['id']})")
    duration = mission.get("target_duration_minutes", {})
    print(f"Target duration: {duration.get('min')} to {duration.get('max')} minutes")
    print(f"Map: {mission.get('map_id')}")
    print(f"Start pattern: {mission.get('start_pattern', 'deployed_base')}")
    print(f"Player faction: {player_faction}")
    print(f"Enemy factions: {', '.join(mission.get('enemy_faction_ids', []))}")
    print("")
    print("Starting roster")
    print(f"- Player units: {format_counter(player_units, units)}")
    print(f"- Player buildings: {format_counter(player_buildings, buildings)}")
    print(f"- Enemy units: {format_counter(enemy_units, units)}")
    print(f"- Enemy buildings: {format_counter(enemy_buildings, buildings)}")
    print("")
    print("Level 1 availability")
    print(f"- Trainable units: {', '.join(names_for(available_units, units))}")
    print(f"- Authored-only player units: {', '.join(names_for(authored_only_units, units)) or 'none'}")
    print(f"- Available buildings/actions: {', '.join(names_for(available_buildings, buildings))}")
    print(f"- Hidden/deferred Level 1 buildings: {', '.join(names_for(hidden_level_1_buildings, buildings)) or 'none'}")
    print("")
    print("Mission rules")
    print(f"- Objectives: {', '.join(mission.get('objectives', []))}")
    print(f"- Failure conditions: {', '.join(mission.get('failure_conditions', []))}")
    print(f"- Special rules: {', '.join(mission.get('special_rules', []))}")
    presentation = mission.get("presentation", {})
    callouts = [
        callout.get("marker", "")
        for callout in presentation.get("map_callouts", [])
        if isinstance(callout, dict) and callout.get("marker")
    ]
    print(f"- Retry hint key: {presentation.get('retry_hint_key', 'none')}")
    print(f"- Tactical note key: {presentation.get('tactical_note_key', 'none')}")
    print(f"- Map callout markers: {', '.join(callouts) or 'none'}")
    fog_rules = mission.get("fog_rules", {})
    print(
        "- Fog: "
        f"unexplored={fog_rules.get('unexplored')}, "
        f"explored terrain stays visible={fog_rules.get('explored_terrain_stays_visible')}, "
        f"track last known enemies={fog_rules.get('track_last_known_enemies')}"
    )
    print("")
    print("Enemy AI profile")
    ai_profile = mission.get("enemy_ai_profile", {})
    for key in [
        "id",
        "first_attack_delay_seconds",
        "rebuild_cooldown_seconds",
        "production_cooldown_seconds",
        "attack_group_size",
        "pressure_slowdown_multiplier",
        "train_time_multiplier",
    ]:
        print(f"- {key}: {ai_profile.get(key)}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())

