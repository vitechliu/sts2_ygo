#!/usr/bin/env python3
"""Crop, resize, and validate a VYgo event illustration."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image


TARGET_SIZE = (3440, 1616)
PREVIEW_SIZE = (1720, 808)
ALLOWED_MODES = {"RGB", "RGBA"}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input", type=Path, help="Source image to crop and resize.")
    parser.add_argument("--output", type=Path, required=True, help="Final PNG path.")
    parser.add_argument("--preview", type=Path, help="Optional 1720x808 preview path.")
    parser.add_argument(
        "--check-only",
        action="store_true",
        help="Validate --output without modifying it.",
    )
    return parser.parse_args()


def validate(path: Path) -> None:
    if not path.is_file():
        raise SystemExit(f"Missing image: {path}")
    with Image.open(path) as image:
        if image.format != "PNG":
            raise SystemExit(f"Expected PNG, got {image.format}: {path}")
        if image.size != TARGET_SIZE:
            raise SystemExit(
                f"Expected {TARGET_SIZE[0]}x{TARGET_SIZE[1]}, got "
                f"{image.width}x{image.height}: {path}"
            )
        if image.mode not in ALLOWED_MODES:
            raise SystemExit(f"Expected RGB or RGBA, got {image.mode}: {path}")
    print(f"OK {path}: {TARGET_SIZE[0]}x{TARGET_SIZE[1]} PNG")


def cover_crop(image: Image.Image, target_size: tuple[int, int]) -> Image.Image:
    target_width, target_height = target_size
    target_ratio = target_width / target_height
    source_ratio = image.width / image.height

    if source_ratio > target_ratio:
        crop_width = round(image.height * target_ratio)
        left = max(0, (image.width - crop_width) // 2)
        box = (left, 0, left + crop_width, image.height)
    else:
        crop_height = round(image.width / target_ratio)
        top = max(0, (image.height - crop_height) // 2)
        box = (0, top, image.width, top + crop_height)

    return image.crop(box).resize(target_size, Image.Resampling.LANCZOS)


def finalize(source: Path, output: Path, preview: Path | None) -> None:
    if not source.is_file():
        raise SystemExit(f"Missing source image: {source}")

    with Image.open(source) as image:
        image.load()
        if image.mode not in ALLOWED_MODES:
            image = image.convert("RGBA" if "A" in image.getbands() else "RGB")
        final = cover_crop(image, TARGET_SIZE)

    output.parent.mkdir(parents=True, exist_ok=True)
    final.save(output, format="PNG", optimize=True)

    if preview is not None:
        preview.parent.mkdir(parents=True, exist_ok=True)
        final.resize(PREVIEW_SIZE, Image.Resampling.LANCZOS).save(
            preview,
            format="PNG",
            optimize=True,
        )

    validate(output)


def main() -> None:
    args = parse_args()
    if args.check_only:
        validate(args.output)
        return
    if args.input is None:
        raise SystemExit("--input is required unless --check-only is used.")
    finalize(args.input, args.output, args.preview)


if __name__ == "__main__":
    main()
