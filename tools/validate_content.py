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
ID_PATTERN = re.compile(r"^[a-z0-9]+(?:_[a-z0-9]+)*$")
REFERENCE_KEY_PATTERN = re.compile(r".*_ids?$")


def load_records() -> tuple[dict[str, dict[str, Any]], list[str]]:
    records: dict[str, dict[str, Any]] = {}
    errors: list[str] = []

    for path in sorted(DATA_ROOT.rglob("*.json")):
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


def validate_references(records: dict[str, dict[str, Any]]) -> list[str]:
    errors: list[str] = []
    allowed_external_prefixes = ("ai_profile_",)

    for record_id, record in records.items():
        for key, target in iter_references(record):
            if target in records:
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
        "unit_worker",
        "unit_rifleman",
        "unit_guardian",
        "unit_rover",
        "unit_commander",
        "unit_tank",
        "building_colony_hub",
        "building_barracks",
        "building_power_plant",
        "building_pylon",
        "building_extractor_refinery",
        "building_defense_tower",
        "resource_materials",
        "weapon_pistol",
        "weapon_rifle",
        "weapon_guardian_laser",
        "weapon_rocket",
    }
    missing = sorted(required_ids - set(records))
    return [f"missing required prototype id '{record_id}'" for record_id in missing]


def main() -> int:
    if not DATA_ROOT.exists():
        print(f"Missing data directory: {DATA_ROOT}", file=sys.stderr)
        return 1

    records, errors = load_records()
    errors.extend(validate_references(records))
    errors.extend(validate_required_first_landing(records))

    if errors:
        print("Content validation failed:")
        for error in errors:
            print(f"- {error}")
        return 1

    print(f"Content validation passed: {len(records)} records.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
