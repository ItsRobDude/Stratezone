from __future__ import annotations

import argparse
from pathlib import Path

try:
    from PIL import Image
except ImportError as exc:
    raise SystemExit(
        "Pillow is required to build unit atlases. Install it with: python -m pip install pillow"
    ) from exc


ANGLES = (0, 45, 90, 135, 180, 225, 270, 315)
ATLAS_COLUMNS = 4


def build_atlas(unit_dir: Path, unit_slug: str) -> Path:
    directional_dir = unit_dir / "directional"
    frames: list[Image.Image] = []

    for angle in ANGLES:
        frame_path = directional_dir / f"{unit_slug}_{angle:03}.png"
        if not frame_path.exists():
            raise FileNotFoundError(f"Missing directional frame: {frame_path}")

        frames.append(Image.open(frame_path).convert("RGBA"))

    first_size = frames[0].size
    mismatched = [
        f"{unit_slug}_{angle:03}.png"
        for angle, frame in zip(ANGLES, frames)
        if frame.size != first_size
    ]
    if mismatched:
        raise ValueError(
            f"{unit_slug} frames must share one canvas size. Mismatched: {', '.join(mismatched)}"
        )

    rows = (len(frames) + ATLAS_COLUMNS - 1) // ATLAS_COLUMNS
    atlas = Image.new("RGBA", (first_size[0] * ATLAS_COLUMNS, first_size[1] * rows), (0, 0, 0, 0))

    for index, frame in enumerate(frames):
        x = index % ATLAS_COLUMNS * first_size[0]
        y = index // ATLAS_COLUMNS * first_size[1]
        atlas.alpha_composite(frame, (x, y))

    output_path = unit_dir / f"{unit_slug}_directional_atlas.png"
    atlas.save(output_path)
    return output_path


def main() -> int:
    parser = argparse.ArgumentParser(description="Build directional unit sprite atlases.")
    parser.add_argument(
        "--assets-root",
        type=Path,
        default=Path("game") / "assets" / "units",
        help="Path to game/assets/units.",
    )
    parser.add_argument(
        "--unit",
        action="append",
        dest="units",
        help="Unit slug to build. Defaults to all unit folders with directional frames.",
    )
    args = parser.parse_args()

    assets_root = args.assets_root
    unit_slugs = args.units or sorted(
        path.name for path in assets_root.iterdir() if (path / "directional").is_dir()
    )

    for unit_slug in unit_slugs:
        output_path = build_atlas(assets_root / unit_slug, unit_slug)
        print(output_path)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
