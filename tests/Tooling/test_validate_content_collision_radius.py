"""Unit collision_radius validation keeps every unit inside the navigation grid's clearance range."""

from __future__ import annotations

import copy
import importlib.util
from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location("validate_content", ROOT / "tools/validate_content.py")
VALIDATOR = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(VALIDATOR)


class CollisionRadiusValidationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        records, errors = VALIDATOR.load_records()
        if errors:
            raise AssertionError(f"content failed to load: {errors}")
        cls.records = records

    def radius_errors(self, value=None, remove=False) -> list[str]:
        records = copy.deepcopy(self.records)
        if remove:
            del records["unit_rover"]["collision_radius"]
        else:
            records["unit_rover"]["collision_radius"] = value
        return [error for error in VALIDATOR.validate_unit_and_building_combat_fields(records) if "collision_radius" in error]

    def test_shipped_units_pass(self):
        errors = [error for error in VALIDATOR.validate_unit_and_building_combat_fields(self.records) if "collision_radius" in error]
        self.assertEqual(errors, [])

    def test_missing_radius_is_rejected(self):
        self.assertTrue(self.radius_errors(remove=True))

    def test_non_positive_or_boolean_radius_is_rejected(self):
        for value in (0, -1, True):
            with self.subTest(value=value):
                self.assertTrue(self.radius_errors(value))

    def test_radius_beyond_tracked_clearance_is_rejected(self):
        self.assertTrue(self.radius_errors(VALIDATOR.MAX_COLLISION_RADIUS + 0.5))


if __name__ == "__main__":
    unittest.main()
