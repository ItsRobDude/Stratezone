#!/usr/bin/env python3
"""Validate Stratezone prototype content data."""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[1]
DATA_ROOT = ROOT / "game" / "data"
I18N_ROOT = DATA_ROOT / "i18n"
ID_PATTERN = re.compile(r"^[a-z0-9]+(?:_[a-z0-9]+)*$")
REFERENCE_KEY_PATTERN = re.compile(r".*_ids?$")
REQUIRED_I18N_KEYS = {
    "mission.objective.establish_outpost",
    "mission.objective.eliminate_enemy_force",
    "mission.objective.destroy_enemy_colony_hub",
    "mission.won.enemy_force_eliminated",
    "mission.won.enemy_colony_hub_destroyed",
    "mission.lost.commander_killed",
    "sim.need_materials",
    "sim.placement.blocked_by_building",
    "sim.placement.extractor_requires_well",
    "sim.placement.resource_well_reserved",
    "sim.placement.requires_adjacent_building",
    "sim.placement.requires_powered_support",
    "sim.placement.colony_hub_exists",
    "sim.placement.requires_colony_hub",
    "sim.placement.blocked_by_terrain",
    "sim.placement.blocked_by_energy_wall",
    "sim.placement.requires_buildable_clearing",
    "sim.placement.legal",
    "sim.placement.placed",
    "sim.upgrade.upgraded",
    "sim.upgrade.select_live_friendly",
    "sim.upgrade.invalid_target",
    "sim.upgrade.unpowered",
    "sim.upgrade.can_upgrade",
    "sim.barracks_upgrade.started",
    "sim.barracks_upgrade.can_start",
    "sim.barracks_upgrade.already_complete",
    "sim.barracks_upgrade.in_progress",
    "sim.barracks_upgrade.requires_barracks",
    "sim.barracks_upgrade.requires_colony_hub",
    "sim.barracks_upgrade.requires_powered_barracks",
    "sim.barracks_upgrade.requires_grunts",
    "sim.barracks_upgrade.queue_busy",
    "sim.production.queued",
    "sim.production.not_trainable",
    "sim.production.queue_busy",
    "sim.production.requires_live_building",
    "sim.production.producer_unpowered",
    "sim.production.requires_powered_addon",
    "sim.production.producer_upgrading",
    "sim.production.requires_barracks_upgrade",
    "sim.production.can_train",
    "sim.repair.requires_bridge",
    "sim.repair.bridge_not_damaged",
    "sim.repair.bridge_started",
    "sim.event.construction_complete",
    "sim.event.training_complete",
    "sim.event.barracks_upgrade_complete",
    "sim.event.bridge_collapsed",
    "sim.event.bridge_repair_complete",
    "ui.action.initial_hint",
    "ui.action.select_barracks_before_training",
    "ui.action.select_barracks_before_retrofit",
    "ui.action.select_tower_before_upgrading",
    "ui.action.command_unavailable_mission",
    "ui.action.ui_scale_set",
    "ui.action.select_grunt_before_building",
    "ui.action.placing_building",
    "ui.action.placement_cancelled",
    "ui.action.selected_unit",
    "ui.action.selected_building",
    "ui.action.no_selectable",
    "ui.action.selected_units",
    "ui.action.no_units_in_box",
    "ui.action.no_unit_selected",
    "ui.action.units_attacking_unit",
    "ui.action.units_attacking_building",
    "ui.action.units_attacking_bridge",
    "ui.action.selected_units_cannot_attack",
    "ui.action.moving_units",
    "ui.action.debug_commander_killed",
    "ui.action.debug_commander_missing",
    "ui.action.debug_mission_loaded",
    "ui.action.mission_restarted",
    "ui.action.quit_requested",
    "ui.action.units_repairing_bridge",
    "ui.hud.build_line",
    "ui.hud.placing_line",
    "ui.hud.status_line",
    "ui.hud.mission_line",
    "ui.hud.briefing_line",
    "ui.hud.tactical_line",
    "ui.hud.callout_line",
    "ui.hud.commander_line",
    "ui.hud.commander_missing_line",
    "ui.hud.commander_alive_status",
    "ui.hud.commander_destroyed_status",
    "ui.hud.alert_line",
    "ui.hud.scale_line",
    "ui.selection.units_with_builders",
    "ui.selection.units_combat",
    "ui.selection.no_selection_training_hint",
    "ui.selection.barracks",
    "ui.selection.defense_tower",
    "ui.selection.building",
    "ui.command.place_building",
    "ui.command.requires_grunt",
    "ui.command.guardian_retrofit_label",
    "ui.command.unit_selection_title",
    "ui.command.grunt_selection_hint",
    "ui.command.combat_selection_hint",
    "ui.command.barracks_hint",
    "ui.command.defense_tower_hint",
    "ui.command.no_direct_commands",
    "ui.command.no_selection_title",
    "ui.command.no_selection_hint",
    "ui.action_bar.progress_seconds",
    "ui.detail.barracks_upgrade",
    "ui.mission_result.won_title",
    "ui.mission_result.lost_title",
    "ui.mission_result.retry_hint",
    "ui.mission_result.controls_hint",
    "ui.alert.none",
    "ui.alert.enemy_units_spotted",
    "ui.alert.enemy_structures_spotted",
    "ui.alert.base_under_attack",
    "ui.alert.extractor_under_attack",
    "ui.alert.building_under_attack",
    "ui.alert.unit_under_attack",
    "ui.alert.power_offline",
    "ui.unit.status.blocked_prefix",
    "ui.building.destroyed_suffix",
}


def load_records() -> tuple[dict[str, dict[str, Any]], list[str]]:
    records: dict[str, dict[str, Any]] = {}
    errors: list[str] = []

    for path in sorted(DATA_ROOT.rglob("*.json")):
        if I18N_ROOT in path.parents:
            continue

        try:
            payload = json.loads(path.read_text(encoding="utf-8"))
        except json.JSONDecodeError as exc:
            errors.append(f"{path}: invalid JSON: {exc}")
            continue

        items = payload.get("records")
        if not isinstance(items, list):
            errors.append(f"{path}: expected top-level 'records' array")
            continue

        for index, item in enumerate(items):
            if not isinstance(item, dict):
                errors.append(f"{path}: record {index} is not an object")
                continue

            record_id = item.get("id")
            if not isinstance(record_id, str) or not record_id:
                errors.append(f"{path}: record {index} has missing or invalid id")
                continue

            if not ID_PATTERN.match(record_id):
                errors.append(f"{path}: id '{record_id}' is not lowercase snake_case")

            if record_id in records:
                errors.append(f"{path}: duplicate id '{record_id}'")
            else:
                item["_source_path"] = str(path.relative_to(ROOT))
                records[record_id] = item

    return records, errors


def iter_references(value: Any, parent_key: str = ""):
    if isinstance(value, dict):
        for key, child in value.items():
            yield from iter_references(child, key)
    elif isinstance(value, list):
        for child in value:
            yield from iter_references(child, parent_key)
    elif isinstance(value, str):
        if REFERENCE_KEY_PATTERN.match(parent_key) or parent_key in {
            "content_id",
            "faction_id",
            "resource_id",
            "unit_id",
            "map_id",
            "ai_profile_id",
            "starts_claimed_by",
            "resource_wells",
            "objectives",
        }:
            yield parent_key, value


def collect_map_object_ids(records: dict[str, dict[str, Any]]) -> set[str]:
    ids: set[str] = set()
    for record in records.values():
        map_objects = record.get("map_objects")
        if not isinstance(map_objects, list):
            continue
        for item in map_objects:
            if isinstance(item, dict) and isinstance(item.get("id"), str):
                ids.add(item["id"])
    return ids


def validate_references(records: dict[str, dict[str, Any]]) -> list[str]:
    errors: list[str] = []
    allowed_external_prefixes = ("ai_profile_",)
    map_object_ids = collect_map_object_ids(records)

    for record_id, record in records.items():
        for key, target in iter_references(record):
            if target in records:
                continue
            if key in {"object_id", "central_island_attack_via_bridge_id"} and target in map_object_ids:
                continue
            if target.startswith(allowed_external_prefixes):
                continue
            errors.append(
                f"{record['_source_path']}: {record_id}.{key} references missing id '{target}'"
            )

    return errors


def validate_required_first_landing(records: dict[str, dict[str, Any]]) -> list[str]:
    required_ids = {
        "mission_first_landing",
        "faction_player_expedition",
        "faction_private_military",
        "unit_grunt",
        "unit_cadet",
        "unit_rifleman",
        "unit_guardian",
        "unit_rover",
        "unit_commander",
        "unit_medium_tank",
        "unit_tank",
        "building_colony_hub",
        "building_barracks",
        "building_armory_annex",
        "building_vehicle_bay",
        "building_power_plant",
        "building_pylon",
        "building_extractor_refinery",
        "building_med_hall",
        "building_logistics_repair_pad",
        "building_defense_tower",
        "building_artillery_battery",
        "barracks_upgrade_guardian_retrofit",
        "resource_materials",
    }
    missing = sorted(required_ids - set(records))
    return [f"missing required prototype id '{record_id}'" for record_id in missing]


def validate_no_separate_weapon_layer(records: dict[str, dict[str, Any]]) -> list[str]:
    errors: list[str] = []
    for record_id, record in records.items():
        if "weapon_ids" in record:
            errors.append(
                f"{record['_source_path']}: {record_id} uses weapon_ids; attack stats belong on the unit/building record"
            )
        if record_id.startswith("weapon_"):
            errors.append(
                f"{record['_source_path']}: {record_id} uses a separate weapon record; use direct attack stats instead"
            )
    return errors


def validate_unit_and_building_combat_fields(records: dict[str, dict[str, Any]]) -> list[str]:
    errors: list[str] = []
    shared_fields = {
        "health",
        "damage_resistances",
        "movement_speed",
        "sight_range",
        "attack_damage",
        "attack_range",
        "attack_cooldown",
        "damage_type",
        "area_radius",
        "friendly_fire",
        "target_filters",
    }
    building_fields = {
        "build_time_seconds",
        "health",
        "damage_resistances",
        "attack_damage",
        "attack_range",
        "attack_cooldown",
        "damage_type",
        "area_radius",
        "friendly_fire",
        "target_filters",
    }

    for record_id, record in records.items():
        if record_id.startswith("unit_"):
            required = shared_fields | {"train_time_seconds"}
            missing = sorted(field for field in required if field not in record)
            for field in missing:
                errors.append(f"{record['_source_path']}: {record_id} missing unit field '{field}'")
            train_time = record.get("train_time_seconds")
            if isinstance(train_time, (int, float)) and train_time < 0:
                errors.append(f"{record['_source_path']}: {record_id} has negative train_time_seconds")

        if record_id.startswith("building_"):
            missing = sorted(field for field in building_fields if field not in record)
            for field in missing:
                errors.append(f"{record['_source_path']}: {record_id} missing building field '{field}'")
            build_time = record.get("build_time_seconds")
            if build_time != 0:
                errors.append(
                    f"{record['_source_path']}: {record_id} build_time_seconds must be 0 for the first prototype"
                )

        resistances = record.get("damage_resistances")
        if record_id.startswith(("unit_", "building_")):
            if not isinstance(resistances, dict):
                errors.append(f"{record['_source_path']}: {record_id} damage_resistances must be an object")
                continue
            for damage_type in ("ballistic", "energy", "explosive", "crush"):
                value = resistances.get(damage_type)
                if not isinstance(value, (int, float)):
                    errors.append(
                        f"{record['_source_path']}: {record_id} damage_resistances.{damage_type} must be numeric"
                    )

    return errors


def validate_mission_profiles(records: dict[str, dict[str, Any]]) -> list[str]:
    errors: list[str] = []
    allowed_start_patterns = {
        "deployed_base",
        "player_places_colony_hub",
        "partial_damaged_base",
        "no_base_force",
        "allotted_force",
    }
    numeric_fields = {
        "first_rebuild_delay_seconds",
        "first_central_well_rebuild_delay_seconds",
        "first_attack_delay_seconds",
        "first_patrol_delay_seconds",
        "patrol_interval_seconds",
        "rebuild_cooldown_seconds",
        "production_cooldown_seconds",
        "central_well_interest",
        "central_well_rebuild_cooldown_seconds",
        "pressure_slowdown_multiplier",
        "train_time_multiplier",
    }

    map_object_ids = collect_map_object_ids(records)

    for record_id, record in records.items():
        if not record_id.startswith("mission_"):
            continue

        start_pattern = record.get("start_pattern")
        if start_pattern not in allowed_start_patterns:
            errors.append(
                f"{record['_source_path']}: {record_id}.start_pattern must be one of {', '.join(sorted(allowed_start_patterns))}"
            )

        presentation = record.get("presentation")
        if isinstance(presentation, dict):
            marker_ids = {
                marker.get("id")
                for marker in record.get("mission_markers", [])
                if isinstance(marker, dict)
            }
            map_callouts = presentation.get("map_callouts")
            if map_callouts is not None:
                if not isinstance(map_callouts, list):
                    errors.append(f"{record['_source_path']}: {record_id}.presentation.map_callouts must be a list")
                else:
                    for index, callout in enumerate(map_callouts):
                        if not isinstance(callout, dict):
                            errors.append(f"{record['_source_path']}: {record_id}.presentation.map_callouts[{index}] must be an object")
                            continue
                        marker_id = callout.get("marker")
                        text_key = callout.get("text_key")
                        if marker_id not in marker_ids:
                            errors.append(
                                f"{record['_source_path']}: {record_id}.presentation.map_callouts[{index}] references missing mission marker '{marker_id}'"
                            )
                        if not isinstance(text_key, str) or not text_key:
                            errors.append(
                                f"{record['_source_path']}: {record_id}.presentation.map_callouts[{index}].text_key must be a localization key"
                            )

        profile = record.get("enemy_ai_profile")
        if profile is None:
            continue
        if not isinstance(profile, dict):
            errors.append(f"{record['_source_path']}: {record_id}.enemy_ai_profile must be an object")
            continue

        for field in numeric_fields:
            value = profile.get(field)
            if value is not None and (not isinstance(value, (int, float)) or value < 0):
                errors.append(
                    f"{record['_source_path']}: {record_id}.enemy_ai_profile.{field} must be a non-negative number"
                )

        max_rebuilds = profile.get("max_central_well_rebuilds")
        if max_rebuilds is not None and (not isinstance(max_rebuilds, int) or max_rebuilds < 0):
            errors.append(
                f"{record['_source_path']}: {record_id}.enemy_ai_profile.max_central_well_rebuilds must be a non-negative integer"
            )

        for integer_field in ("patrol_group_size", "max_patrol_dispatches"):
            value = profile.get(integer_field)
            if value is not None and (not isinstance(value, int) or value < 0):
                errors.append(
                    f"{record['_source_path']}: {record_id}.enemy_ai_profile.{integer_field} must be a non-negative integer"
                )

        patrol_markers = profile.get("patrol_markers")
        if patrol_markers is not None:
            if not isinstance(patrol_markers, list) or not all(isinstance(marker_id, str) and marker_id for marker_id in patrol_markers):
                errors.append(
                    f"{record['_source_path']}: {record_id}.enemy_ai_profile.patrol_markers must be a list of marker id strings"
                )
            else:
                marker_ids = {
                    marker.get("id")
                    for marker in record.get("mission_markers", [])
                    if isinstance(marker, dict)
                }
                for marker_id in patrol_markers:
                    if marker_id not in marker_ids:
                        errors.append(
                            f"{record['_source_path']}: {record_id}.enemy_ai_profile.patrol_markers references missing mission marker '{marker_id}'"
                        )

        bridge_id = profile.get("central_island_attack_via_bridge_id")
        if bridge_id is not None and bridge_id not in map_object_ids:
            errors.append(
                f"{record['_source_path']}: {record_id}.enemy_ai_profile.central_island_attack_via_bridge_id references missing map object '{bridge_id}'"
            )

    return errors


def i18n_key_for_record(record_id: str) -> str:
    prefix = record_id.split("_", 1)[0]
    return f"{prefix}.{record_id}.name"


def validate_map_objects(records: dict[str, dict[str, Any]]) -> list[str]:
    errors: list[str] = []
    map_object_ids = collect_map_object_ids(records)

    for record_id, record in records.items():
        map_objects = record.get("map_objects")
        if map_objects is not None:
            if not isinstance(map_objects, list):
                errors.append(f"{record['_source_path']}: {record_id}.map_objects must be a list")
            else:
                for index, item in enumerate(map_objects):
                    if not isinstance(item, dict):
                        errors.append(f"{record['_source_path']}: {record_id}.map_objects[{index}] must be an object")
                        continue
                    object_id = item.get("id")
                    if not isinstance(object_id, str) or not ID_PATTERN.match(object_id):
                        errors.append(f"{record['_source_path']}: {record_id}.map_objects[{index}].id must be lowercase snake_case")
                    if item.get("object_type") != "bridge":
                        errors.append(f"{record['_source_path']}: {record_id}.map_objects[{index}].object_type must be bridge for the first pass")
                    if item.get("shape") not in {"rect", "circle"}:
                        errors.append(f"{record['_source_path']}: {record_id}.map_objects[{index}].shape must be rect or circle")
                    if not isinstance(item.get("center"), dict):
                        errors.append(f"{record['_source_path']}: {record_id}.map_objects[{index}].center must be an object")
                    if item.get("shape") == "rect" and not isinstance(item.get("size"), dict):
                        errors.append(f"{record['_source_path']}: {record_id}.map_objects[{index}].size must be an object for rect objects")
                    if item.get("shape") == "circle" and not isinstance(item.get("radius"), (int, float)):
                        errors.append(f"{record['_source_path']}: {record_id}.map_objects[{index}].radius must be numeric for circle objects")
                    if not isinstance(item.get("max_health"), (int, float)) or item.get("max_health", 0) <= 0:
                        errors.append(f"{record['_source_path']}: {record_id}.map_objects[{index}].max_health must be positive")

        overrides = record.get("mission_object_overrides")
        if overrides is None:
            continue
        if not isinstance(overrides, list):
            errors.append(f"{record['_source_path']}: {record_id}.mission_object_overrides must be a list")
            continue
        for index, item in enumerate(overrides):
            if not isinstance(item, dict):
                errors.append(f"{record['_source_path']}: {record_id}.mission_object_overrides[{index}] must be an object")
                continue
            object_id = item.get("object_id")
            if object_id not in map_object_ids:
                errors.append(f"{record['_source_path']}: {record_id}.mission_object_overrides[{index}] references missing map object '{object_id}'")
            percent = item.get("starting_health_percent")
            if not isinstance(percent, (int, float)) or percent < 0 or percent > 1:
                errors.append(f"{record['_source_path']}: {record_id}.mission_object_overrides[{index}].starting_health_percent must be between 0 and 1")

    return errors


def validate_i18n(records: dict[str, dict[str, Any]]) -> list[str]:
    errors: list[str] = []
    path = I18N_ROOT / "en.json"
    if not path.exists():
        return [f"missing localization file '{path.relative_to(ROOT)}'"]

    try:
        payload = json.loads(path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as exc:
        return [f"{path.relative_to(ROOT)}: invalid JSON: {exc}"]

    strings = payload.get("strings")
    if not isinstance(strings, dict):
        return [f"{path.relative_to(ROOT)}: expected top-level 'strings' object"]

    required_keys = set(REQUIRED_I18N_KEYS)
    for record_id, record in records.items():
        if isinstance(record.get("display_name"), str):
            required_keys.add(i18n_key_for_record(record_id))
            if record_id.startswith(("unit_", "building_")):
                required_keys.add(i18n_short_key_for_record(record_id))
        presentation = record.get("presentation")
        if record_id.startswith("mission_") and isinstance(presentation, dict):
            for key_name, key_value in presentation.items():
                if key_name.endswith("_key") and isinstance(key_value, str) and key_value:
                    required_keys.add(key_value)
            map_callouts = presentation.get("map_callouts")
            if isinstance(map_callouts, list):
                for callout in map_callouts:
                    if isinstance(callout, dict):
                        text_key = callout.get("text_key")
                        if isinstance(text_key, str) and text_key:
                            required_keys.add(text_key)

    for key in sorted(required_keys):
        value = strings.get(key)
        if not isinstance(value, str) or not value:
            errors.append(f"{path.relative_to(ROOT)}: missing localization key '{key}'")

    return errors


def i18n_short_key_for_record(record_id: str) -> str:
    prefix = record_id.split("_", 1)[0]
    return f"{prefix}.{record_id}.short_name"


def main() -> int:
    if not DATA_ROOT.exists():
        print(f"Missing data directory: {DATA_ROOT}", file=sys.stderr)
        return 1

    records, errors = load_records()
    errors.extend(validate_references(records))
    errors.extend(validate_required_first_landing(records))
    errors.extend(validate_no_separate_weapon_layer(records))
    errors.extend(validate_unit_and_building_combat_fields(records))
    errors.extend(validate_mission_profiles(records))
    errors.extend(validate_map_objects(records))
    errors.extend(validate_i18n(records))

    if errors:
        print("Content validation failed:")
        for error in errors:
            print(f"- {error}")
        return 1

    print(f"Content validation passed: {len(records)} records.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
