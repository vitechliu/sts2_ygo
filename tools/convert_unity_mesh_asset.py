#!/usr/bin/env python3
"""Convert the uncompressed vertex/index data in a Unity text Mesh asset to OBJ.

This intentionally supports the channel formats used by MDPro3's SummonXYZ meshes:
Float32, Float16, and UNorm8. It keeps the conversion reproducible without requiring
Unity or AssetStudio.
"""

from __future__ import annotations

import argparse
import re
import struct
from pathlib import Path


CHANNEL_PATTERN = re.compile(
    r"    - stream: (?P<stream>\d+)\n"
    r"      offset: (?P<offset>\d+)\n"
    r"      format: (?P<format>\d+)\n"
    r"      dimension: (?P<dimension>\d+)"
)


def require(pattern: str, text: str, label: str) -> str:
    match = re.search(pattern, text, re.MULTILINE)
    if match is None:
        raise ValueError(f"Missing {label}")
    return match.group(1)


def read_components(data: bytes, offset: int, fmt: int, dimension: int) -> list[float]:
    dimension &= 0xF
    if fmt == 0:
        return list(struct.unpack_from(f"<{dimension}f", data, offset))
    if fmt == 1:
        return list(struct.unpack_from(f"<{dimension}e", data, offset))
    if fmt == 2:
        return [value / 255.0 for value in data[offset : offset + dimension]]
    raise ValueError(f"Unsupported Unity vertex format {fmt}")


def convert(source: Path, destination: Path) -> None:
    text = source.read_text(encoding="utf-8-sig")
    name = require(r"^  m_Name: (.+)$", text, "mesh name")
    vertex_count = int(require(r"    m_VertexCount: (\d+)", text, "vertex count"))
    data_size = int(require(r"    m_DataSize: (\d+)", text, "vertex data size"))
    vertex_hex = require(r"    _typelessdata: ([0-9a-fA-F]+)", text, "vertex data")
    index_hex = require(r"  m_IndexBuffer: ([0-9a-fA-F]+)", text, "index buffer")
    index_format = int(require(r"  m_IndexFormat: (\d+)", text, "index format"))

    vertex_data = bytes.fromhex(vertex_hex)
    index_data = bytes.fromhex(index_hex)
    if len(vertex_data) != data_size or data_size % vertex_count != 0:
        raise ValueError("Vertex data size does not match the declared mesh layout")
    stride = data_size // vertex_count

    channels = [
        {
            "stream": int(match.group("stream")),
            "offset": int(match.group("offset")),
            "format": int(match.group("format")),
            "dimension": int(match.group("dimension")) & 0xF,
        }
        for match in CHANNEL_PATTERN.finditer(text)
    ]
    if len(channels) < 5 or channels[0]["dimension"] < 3:
        raise ValueError("Mesh is missing position/UV channel declarations")
    if any(channel["stream"] != 0 for channel in channels if channel["dimension"]):
        raise ValueError("Multi-stream Unity meshes are not supported")

    position_channel = channels[0]
    normal_channel = channels[1] if channels[1]["dimension"] >= 3 else None
    uv_channel = channels[4] if channels[4]["dimension"] >= 2 else None

    positions: list[tuple[float, float, float]] = []
    normals: list[tuple[float, float, float]] = []
    uvs: list[tuple[float, float]] = []
    for vertex in range(vertex_count):
        base = vertex * stride
        position = read_components(
            vertex_data,
            base + position_channel["offset"],
            position_channel["format"],
            position_channel["dimension"],
        )
        positions.append((position[0], position[1], -position[2]))

        if normal_channel is not None:
            normal = read_components(
                vertex_data,
                base + normal_channel["offset"],
                normal_channel["format"],
                normal_channel["dimension"],
            )
            normals.append((normal[0], normal[1], -normal[2]))

        if uv_channel is not None:
            uv = read_components(
                vertex_data,
                base + uv_channel["offset"],
                uv_channel["format"],
                uv_channel["dimension"],
            )
            uvs.append((uv[0], 1.0 - uv[1]))

    index_code = "H" if index_format == 0 else "I"
    index_size = struct.calcsize(index_code)
    indices = list(struct.unpack(f"<{len(index_data) // index_size}{index_code}", index_data))
    if len(indices) % 3 != 0 or max(indices, default=-1) >= vertex_count:
        raise ValueError("Index buffer is not a valid triangle list")

    destination.parent.mkdir(parents=True, exist_ok=True)
    with destination.open("w", encoding="utf-8", newline="\n") as output:
        output.write(f"# Converted from {source.name}\n")
        output.write(f"o {name}\n")
        for x, y, z in positions:
            output.write(f"v {x:.9g} {y:.9g} {z:.9g}\n")
        for u, v in uvs:
            output.write(f"vt {u:.9g} {v:.9g}\n")
        for x, y, z in normals:
            output.write(f"vn {x:.9g} {y:.9g} {z:.9g}\n")

        has_uv = len(uvs) == vertex_count
        has_normal = len(normals) == vertex_count
        for face in range(0, len(indices), 3):
            # Reversing winding accompanies the Unity-to-Godot Z-axis flip.
            triangle = (indices[face], indices[face + 2], indices[face + 1])
            tokens: list[str] = []
            for index in triangle:
                obj_index = index + 1
                if has_uv and has_normal:
                    tokens.append(f"{obj_index}/{obj_index}/{obj_index}")
                elif has_uv:
                    tokens.append(f"{obj_index}/{obj_index}")
                elif has_normal:
                    tokens.append(f"{obj_index}//{obj_index}")
                else:
                    tokens.append(str(obj_index))
            output.write("f " + " ".join(tokens) + "\n")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("source", type=Path)
    parser.add_argument("destination", type=Path)
    args = parser.parse_args()
    convert(args.source, args.destination)


if __name__ == "__main__":
    main()
