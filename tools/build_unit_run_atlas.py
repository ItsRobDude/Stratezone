from __future__ import annotations

import argparse
import re
from pathlib import Path

try:
    from PIL import Image, ImageDraw, ImageFont
except ImportError as exc:
    raise SystemExit(
        "Pillow is required to build unit run atlases. Install it with: python -m pip install pillow"
    ) from exc


ANGLES = (0, 45, 90, 135, 180, 225, 270, 315)
DEFAULT_FRAME_COUNT = 16
DEFAULT_VIEW_COUNT = 8
DEFAULT_CELL_SIZE = (425, 575)
DEFAULT_BOTTOM_MARGIN = 4
DEFAULT_MAX_ATLAS_BYTES = 16 * 1024 * 1024

# Source view numbers, in game compass row order: 000, 045, 090, 135, 180, 225, 270, 315.
DEFAULT_SOURCE_VIEW_ORDER = (4, 3, 2, 1, 8, 7, 6, 5)


def parse_cell_size(value: str) -> tuple[int, int]:
    match = re.fullmatch(r"(\d+)x(\d+)", value.strip().lower())
    if not match:
        raise argparse.ArgumentTypeError("Cell size must look like 425x575.")

    width = int(match.group(1))
    height = int(match.group(2))
    if width <= 0 or height <= 0:
        raise argparse.ArgumentTypeError("Cell width and height must be positive.")

    return width, height


def parse_source_view_order(value: str) -> tuple[int, ...]:
    parts = [part.strip() for part in value.split(",") if part.strip()]
    try:
        order = tuple(int(part) for part in parts)
    except ValueError as exc:
        raise argparse.ArgumentTypeError("View order must be comma-separated integers.") from exc

    expected = set(range(1, DEFAULT_VIEW_COUNT + 1))
    if set(order) != expected:
        raise argparse.ArgumentTypeError(
            f"View order must contain each source view from 1 to {DEFAULT_VIEW_COUNT} exactly once."
        )

    return order


def collect_pose_files(source_root: Path, frame_count: int, view_count: int) -> list[list[Path]]:
    pose_dirs = sorted(
        [
            path
            for path in source_root.iterdir()
            if path.is_dir() and re.fullmatch(r"\d+-360", path.name)
        ],
        key=lambda path: int(path.name.split("-")[0]),
    )

    if len(pose_dirs) != frame_count:
        raise ValueError(f"Expected {frame_count} pose folders named N-360, found {len(pose_dirs)}.")

    pose_files: list[list[Path]] = []
    for pose_dir in pose_dirs:
        frame_dir = pose_dir / "1x"
        if not frame_dir.is_dir():
            raise FileNotFoundError(f"Missing expected frame folder: {frame_dir}")

        files = sorted(frame_dir.glob("*.png"), key=lambda path: path.name)
        if len(files) != view_count:
            raise ValueError(f"Expected {view_count} PNG views in {frame_dir}, found {len(files)}.")

        pose_files.append(files)

    return pose_files


def visible_bbox(image: Image.Image) -> tuple[int, int, int, int]:
    bbox = image.convert("RGBA").split()[-1].getbbox()
    return bbox or (0, 0, image.width, image.height)


def build_normalized_frames(
    pose_files: list[list[Path]],
    source_view_order: tuple[int, ...],
    cell_size: tuple[int, int],
    bottom_margin: int,
) -> dict[int, list[Image.Image]]:
    canvas_width, canvas_height = cell_size
    max_width = 0
    max_height = 0

    for files in pose_files:
        for path in files:
            with Image.open(path).convert("RGBA") as image:
                bbox = visible_bbox(image)
                max_width = max(max_width, bbox[2] - bbox[0])
                max_height = max(max_height, bbox[3] - bbox[1])

    scale = min(canvas_width / max_width, (canvas_height - bottom_margin) / max_height, 1.0)
    normalized = {angle: [] for angle in ANGLES}

    for files in pose_files:
        for angle, source_view_number in zip(ANGLES, source_view_order):
            source_path = files[source_view_number - 1]
            with Image.open(source_path).convert("RGBA") as source_image:
                source = source_image.crop(visible_bbox(source_image))
                next_size = (
                    max(1, round(source.width * scale)),
                    max(1, round(source.height * scale)),
                )
                source = source.resize(next_size, Image.Resampling.LANCZOS)

            frame = Image.new("RGBA", (canvas_width, canvas_height), (0, 0, 0, 0))
            x = round((canvas_width - source.width) / 2)
            y = canvas_height - bottom_margin - source.height
            frame.alpha_composite(source, (x, y))
            normalized[angle].append(frame)

    return normalized


def build_atlas(normalized: dict[int, list[Image.Image]], cell_size: tuple[int, int]) -> Image.Image:
    canvas_width, canvas_height = cell_size
    frame_count = len(next(iter(normalized.values())))
    atlas = Image.new("RGBA", (canvas_width * frame_count, canvas_height * len(ANGLES)), (0, 0, 0, 0))

    for row, angle in enumerate(ANGLES):
        for column, frame in enumerate(normalized[angle]):
            atlas.alpha_composite(frame, (column * canvas_width, row * canvas_height))

    return atlas


def save_preview(
    normalized: dict[int, list[Image.Image]],
    output_path: Path,
    title: str,
) -> None:
    try:
        font = ImageFont.truetype("arial.ttf", 18)
    except OSError:
        font = ImageFont.load_default()

    thumbnail_width = 85
    thumbnail_height = 115
    label_width = 70
    frame_count = len(next(iter(normalized.values())))
    preview = Image.new(
        "RGB",
        (label_width + thumbnail_width * frame_count, 34 + thumbnail_height * len(ANGLES)),
        "white",
    )
    draw = ImageDraw.Draw(preview)
    draw.text((8, 6), title, fill=(0, 0, 0), font=font)

    for column in range(frame_count):
        draw.text((label_width + column * thumbnail_width + 30, 8), str(column + 1), fill=(0, 0, 0), font=font)

    for row, angle in enumerate(ANGLES):
        y = 34 + row * thumbnail_height
        draw.text((8, y + 45), f"{angle:03}", fill=(0, 0, 0), font=font)
        for column, frame in enumerate(normalized[angle]):
            composite = Image.new("RGBA", frame.size, (238, 238, 238, 255))
            composite.alpha_composite(frame)
            image = composite.convert("RGB")
            image.thumbnail((thumbnail_width, thumbnail_height), Image.Resampling.LANCZOS)
            x = label_width + column * thumbnail_width + (thumbnail_width - image.width) // 2
            preview.paste(image, (x, y + (thumbnail_height - image.height) // 2))

    output_path.parent.mkdir(parents=True, exist_ok=True)
    preview.save(output_path)


def main() -> int:
    parser = argparse.ArgumentParser(description="Build a directional unit run atlas from N-360 turntable pose folders.")
    parser.add_argument("--unit", required=True, help="Unit asset slug, for example rifleman.")
    parser.add_argument("--source", type=Path, required=True, help="Source folder containing 1-360, 2-360, ... folders.")
    parser.add_argument(
        "--assets-root",
        type=Path,
        default=Path("game") / "assets" / "units",
        help="Path to game/assets/units.",
    )
    parser.add_argument("--frame-count", type=int, default=DEFAULT_FRAME_COUNT)
    parser.add_argument("--cell-size", type=parse_cell_size, default=DEFAULT_CELL_SIZE)
    parser.add_argument("--bottom-margin", type=int, default=DEFAULT_BOTTOM_MARGIN)
    parser.add_argument(
        "--source-view-order",
        type=parse_source_view_order,
        default=DEFAULT_SOURCE_VIEW_ORDER,
        help="Comma-separated source view numbers in game compass order. Default: 4,3,2,1,8,7,6,5.",
    )
    parser.add_argument(
        "--max-atlas-bytes",
        type=int,
        default=DEFAULT_MAX_ATLAS_BYTES,
        help="Fail if the packed PNG atlas exceeds this many bytes.",
    )
    parser.add_argument("--preview-out", type=Path, help="Optional preview contact sheet path.")
    args = parser.parse_args()

    if args.frame_count <= 0:
        raise ValueError("--frame-count must be positive.")
    if args.bottom_margin < 0:
        raise ValueError("--bottom-margin cannot be negative.")

    pose_files = collect_pose_files(args.source, args.frame_count, DEFAULT_VIEW_COUNT)
    normalized = build_normalized_frames(
        pose_files,
        args.source_view_order,
        args.cell_size,
        args.bottom_margin,
    )
    atlas = build_atlas(normalized, args.cell_size)

    output_dir = args.assets_root / args.unit / "animations" / "run_directional"
    output_dir.mkdir(parents=True, exist_ok=True)
    output_path = output_dir / f"{args.unit}_run_directional_atlas.png"
    atlas.save(output_path, optimize=True)

    atlas_bytes = output_path.stat().st_size
    if atlas_bytes > args.max_atlas_bytes:
        raise ValueError(
            f"Run atlas is {atlas_bytes:,} bytes, above max {args.max_atlas_bytes:,}: {output_path}"
        )

    print(f"Wrote {output_path} ({atlas_bytes:,} bytes)")

    if args.preview_out is not None:
        save_preview(normalized, args.preview_out, f"{args.unit} run atlas preview")
        print(f"Wrote {args.preview_out}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
