"""Building footprint validation rejects malformed outlines before the C# loader sees them."""

from __future__ import annotations

import copy
import importlib.util
from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location("validate_content", ROOT / "tools/validate_content.py")
VALIDATOR = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(VALIDATOR)


def rect(width, height):
    return [{"shape": "rect", "center": {"x": 0, "y": 0}, "size": {"x": width, "y": height}}]


def polygon(points):
    return [{"shape": "polygon", "points": [{"x": x, "y": y} for x, y in points]}]


class FootprintValidationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        records, errors = VALIDATOR.load_records()
        if errors:
            raise AssertionError(f"content failed to load: {errors}")
        cls.records = records

    def footprint_errors(self, record_id: str, **changes) -> list[str]:
        records = copy.deepcopy(self.records)
        records[record_id].update(changes)
        return VALIDATOR.validate_building_footprints(records)

    def test_shipped_footprints_pass(self):
        self.assertEqual(VALIDATOR.validate_building_footprints(self.records), [])

    def test_self_intersecting_star_is_rejected(self):
        # Corners turn the same way at every vertex, but the outline loops twice.
        star = polygon([(0, -2), (1.2, 1.6), (-1.9, -0.6), (1.9, -0.6), (-1.2, 1.6)])
        errors = self.footprint_errors("building_artillery_battery", footprint=star)
        self.assertTrue(any("non-self-intersecting" in error for error in errors), errors)

    def test_concave_polygon_is_rejected(self):
        dart = polygon([(-1, -1), (1, -1), (0, 0), (1, 1), (-1, 1)])
        self.assertTrue(self.footprint_errors("building_pylon", footprint=dart))

    def test_boolean_coordinates_are_rejected(self):
        footprint = rect(2, 2)
        footprint[0]["size"]["x"] = True
        errors = self.footprint_errors("building_pylon", footprint=footprint)
        self.assertTrue(any("numeric center and size" in error for error in errors), errors)

    def test_boolean_buffer_is_rejected(self):
        self.assertTrue(self.footprint_errors("building_pylon", placement_buffer=True))

    def test_footprint_must_cover_origin(self):
        off_origin = [{"shape": "rect", "center": {"x": 5, "y": 0}, "size": {"x": 1, "y": 1}}]
        errors = self.footprint_errors("building_pylon", footprint=off_origin)
        self.assertTrue(any("cover the building origin" in error for error in errors), errors)

    def test_in_place_upgrade_keeps_base_footprint(self):
        errors = self.footprint_errors("building_gun_tower", footprint=rect(3, 3))
        self.assertTrue(any("upgrades in place" in error for error in errors), errors)

    def test_buffer_must_be_uniform(self):
        errors = self.footprint_errors("building_pylon", placement_buffer=1)
        self.assertTrue(any("must be uniform" in error for error in errors), errors)

    def test_retired_radius_is_rejected(self):
        errors = self.footprint_errors("building_pylon", footprint_radius=1)
        self.assertTrue(any("retired" in error for error in errors), errors)


if __name__ == "__main__":
    unittest.main()
