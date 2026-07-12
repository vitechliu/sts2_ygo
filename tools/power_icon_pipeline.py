#!/usr/bin/env python3
"""Finalize and validate transparent VYgo Power icons."""

from __future__ import annotations

import argparse
from collections import Counter
import json
import math
import sys
from pathlib import Path

try:
    from PIL import Image, ImageChops, ImageColor, ImageFilter
except ImportError as exc:
    raise SystemExit(
        "Pillow is required. In Codex desktop, call load_workspace_dependencies and "
        "run this script with the bundled Python executable."
    ) from exc


FINAL_SIZE = 256
PREVIEW_SIZE = 64


def pixels(image: Image.Image):
    """Return the non-deprecated Pillow pixel iterator when available."""
    if hasattr(image, "get_flattened_data"):
        return image.get_flattened_data()
    return image.getdata()


def border_key(image: Image.Image) -> tuple[int, int, int]:
    rgb = image.convert("RGB")
    width, height = rgb.size
    band = max(1, min(width, height) // 64)
    samples: list[tuple[int, int, int]] = []
    samples.extend(pixels(rgb.crop((0, 0, width, band))))
    samples.extend(pixels(rgb.crop((0, height - band, width, height))))
    samples.extend(pixels(rgb.crop((0, band, band, height - band))))
    samples.extend(pixels(rgb.crop((width - band, band, width, height - band))))
    channels = list(zip(*samples))
    return tuple(sorted(channel)[len(channel) // 2] for channel in channels)  # type: ignore[return-value]


def remove_key(
    image: Image.Image,
    key: tuple[int, int, int],
    transparent_threshold: float,
    opaque_threshold: float,
) -> Image.Image:
    if opaque_threshold <= transparent_threshold:
        raise ValueError("opaque threshold must exceed transparent threshold")

    source = image.convert("RGBA")
    output: list[tuple[int, int, int, int]] = []
    kr, kg, kb = key
    key_peak = max(key)
    key_floor = min(key)
    key_channels = [
        index
        for index, value in enumerate(key)
        if value >= key_peak - 8 and value >= key_floor + 64
    ]
    if not key_channels:
        key_channels = [max(range(3), key=lambda index: key[index])]
    non_key_channels = [index for index in range(3) if index not in key_channels]

    for red, green, blue, original_alpha in pixels(source):
        distance = math.sqrt((red - kr) ** 2 + (green - kg) ** 2 + (blue - kb) ** 2)
        matte = max(0.0, min(1.0, (distance - transparent_threshold) / (opaque_threshold - transparent_threshold)))
        alpha = round(original_alpha * matte)

        channels = [red, green, blue]
        if alpha > 0 and non_key_channels:
            despill_ceiling = max(channels[index] for index in non_key_channels)
            for index in key_channels:
                channels[index] = min(channels[index], despill_ceiling)
        if alpha == 0:
            channels = [0, 0, 0]
        output.append((channels[0], channels[1], channels[2], alpha))

    result = Image.new("RGBA", source.size)
    result.putdata(output)
    return result


def alpha_bbox(image: Image.Image, threshold: int = 8) -> tuple[int, int, int, int] | None:
    alpha = image.getchannel("A").point(lambda value: 255 if value > threshold else 0)
    return alpha.getbbox()


def premultiplied_resize(image: Image.Image, size: tuple[int, int]) -> Image.Image:
    return image.convert("RGBa").resize(size, Image.Resampling.LANCZOS).convert("RGBA")


def normalize_canvas(image: Image.Image, padding: int, crop_alpha_threshold: int = 8) -> Image.Image:
    bbox = alpha_bbox(image, threshold=crop_alpha_threshold)
    if bbox is None:
        raise ValueError("no visible pixels remain after background removal")

    cropped = image.crop(bbox)
    available = FINAL_SIZE - 2 * padding
    scale = min(available / cropped.width, available / cropped.height)
    resized_size = (
        max(1, round(cropped.width * scale)),
        max(1, round(cropped.height * scale)),
    )
    resized = premultiplied_resize(cropped, resized_size)
    canvas = Image.new("RGBA", (FINAL_SIZE, FINAL_SIZE), (0, 0, 0, 0))
    offset = ((FINAL_SIZE - resized.width) // 2, (FINAL_SIZE - resized.height) // 2)
    canvas.alpha_composite(resized, offset)
    return canvas


def add_outer_stroke(image: Image.Image, width: int, color: tuple[int, int, int, int]) -> Image.Image:
    if width <= 0:
        return image
    alpha = image.getchannel("A")
    expanded = alpha.filter(ImageFilter.MaxFilter(width * 2 + 1))
    outline_alpha = ImageChops.subtract(expanded, alpha)
    outline = Image.new("RGBA", image.size, color)
    outline.putalpha(ImageChops.multiply(outline_alpha, Image.new("L", image.size, color[3])))
    return Image.alpha_composite(outline, image)


def icon_report(image: Image.Image) -> dict[str, object]:
    rgba = image.convert("RGBA")
    bbox = alpha_bbox(rgba)
    alpha = rgba.getchannel("A")
    width, height = rgba.size
    alpha_values = list(pixels(alpha))
    visible = sum(value > 8 for value in alpha_values)
    partial = sum(8 < value < 247 for value in alpha_values)
    corners = [alpha.getpixel(point) for point in ((0, 0), (width - 1, 0), (0, height - 1), (width - 1, height - 1))]
    margins = None
    if bbox is not None:
        margins = {
            "left": bbox[0],
            "top": bbox[1],
            "right": width - bbox[2],
            "bottom": height - bbox[3],
        }

    preview = premultiplied_resize(rgba, (PREVIEW_SIZE, PREVIEW_SIZE))
    opaque_colors = Counter(
        (red // 32, green // 32, blue // 32)
        for red, green, blue, pixel_alpha in pixels(preview)
        if pixel_alpha >= 128
    )
    color_floor = max(1, round(sum(opaque_colors.values()) * 0.005))
    effective_colors = sum(count >= color_floor for count in opaque_colors.values())
    return {
        "size": [width, height],
        "mode": rgba.mode,
        "bbox": list(bbox) if bbox else None,
        "margins": margins,
        "coverage": round(visible / (width * height), 4),
        "partial_alpha_ratio": round(partial / max(visible, 1), 4),
        "corner_alpha": corners,
        "preview_effective_color_bins": effective_colors,
    }


def validation_errors(report: dict[str, object], max_color_bins: int = 18) -> list[str]:
    errors: list[str] = []
    if report["size"] != [FINAL_SIZE, FINAL_SIZE]:
        errors.append("image must be exactly 256x256")
    if report["bbox"] is None:
        errors.append("image has no visible content")
    if any(value != 0 for value in report["corner_alpha"]):  # type: ignore[union-attr]
        errors.append("all four corners must be fully transparent")
    margins = report["margins"]
    if isinstance(margins, dict) and min(margins.values()) < 12:
        errors.append("visible content needs at least 12px padding on every side")
    coverage = float(report["coverage"])
    if coverage < 0.18:
        errors.append("visible content is too small (coverage below 18%)")
    if coverage > 0.72:
        errors.append("visible content is too dense (coverage above 72%)")
    if float(report["partial_alpha_ratio"]) > 0.18:
        errors.append("too many semi-transparent pixels; check for glow, blur, or incomplete key removal")
    if int(report["preview_effective_color_bins"]) > max_color_bins:
        errors.append(
            f"too many effective colors at 64px (limit {max_color_bins}); "
            "simplify gradients and texture"
        )
    return errors


def write_json(path: Path | None, data: dict[str, object]) -> None:
    text = json.dumps(data, ensure_ascii=False, indent=2) + "\n"
    if path is None:
        print(text, end="")
    else:
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding="utf-8")


def finalize(args: argparse.Namespace) -> int:
    image = Image.open(args.input)
    rgba = image.convert("RGBA")
    has_transparency = rgba.getchannel("A").getextrema()[0] < 255
    if not has_transparency or args.force_key:
        key = border_key(rgba) if args.key == "auto" else ImageColor.getrgb(args.key)
        rgba = remove_key(rgba, key, args.transparent_threshold, args.opaque_threshold)

    result = normalize_canvas(rgba, args.padding, args.crop_alpha_threshold)
    stroke_color = ImageColor.getcolor(args.stroke_color, "RGBA")
    result = add_outer_stroke(result, args.stroke, stroke_color)
    result = result.convert("RGBA")

    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    result.save(output, format="PNG", optimize=True)

    if args.preview:
        preview = premultiplied_resize(result, (PREVIEW_SIZE, PREVIEW_SIZE))
        preview_path = Path(args.preview)
        preview_path.parent.mkdir(parents=True, exist_ok=True)
        preview.save(preview_path, format="PNG", optimize=True)

    report = icon_report(result)
    report["errors"] = validation_errors(report, args.max_color_bins)
    write_json(Path(args.report) if args.report else None, report)
    return 1 if args.strict and report["errors"] else 0


def check(args: argparse.Namespace) -> int:
    report = icon_report(Image.open(args.image))
    report["errors"] = validation_errors(report, args.max_color_bins)
    write_json(None, report)
    return 1 if args.strict and report["errors"] else 0


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    subparsers = parser.add_subparsers(dest="command", required=True)

    final = subparsers.add_parser("finalize", help="remove a flat background and produce a normalized 256px icon")
    final.add_argument("--input", required=True, type=Path)
    final.add_argument("--output", required=True, type=Path)
    final.add_argument("--preview", type=Path, help="optional 64x64 QA preview")
    final.add_argument("--report", type=Path, help="optional JSON QA report")
    final.add_argument("--key", default="auto", help="auto or a CSS color such as #00ff00")
    final.add_argument("--force-key", action="store_true", help="remove a key even if the source already has transparency")
    final.add_argument("--transparent-threshold", type=float, default=16.0)
    final.add_argument("--opaque-threshold", type=float, default=180.0)
    final.add_argument("--padding", type=int, default=20)
    final.add_argument(
        "--crop-alpha-threshold",
        type=int,
        default=8,
        help="ignore alpha at or below this value when finding the subject bounds",
    )
    final.add_argument("--max-color-bins", type=int, default=18)
    final.add_argument("--stroke", type=int, default=0, help="optional outer stroke width at 256px")
    final.add_argument("--stroke-color", default="#2b1720ff")
    final.add_argument("--strict", action="store_true")
    final.set_defaults(func=finalize)

    inspect = subparsers.add_parser("check", help="validate an existing final icon")
    inspect.add_argument("image", type=Path)
    inspect.add_argument("--max-color-bins", type=int, default=18)
    inspect.add_argument("--strict", action="store_true")
    inspect.set_defaults(func=check)
    return parser


def main() -> int:
    args = build_parser().parse_args()
    try:
        return args.func(args)
    except (OSError, ValueError) as exc:
        print(f"error: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
