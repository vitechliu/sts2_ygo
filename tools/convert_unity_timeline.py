#!/usr/bin/env python3
"""审计 MDPro3 的 SummonSynchro Timeline，并生成 Godot AnimationLibrary 索引。

Unity 的绑定目标依赖 Prefab 层级，不能仅凭 playable 文件安全地生成 NodePath；
因此本工具完整保存曲线/切线/激活片段/声音/依赖到 JSON，并生成带有精确时长的
AnimationLibrary。运行时播放器使用同一组时间标记组合对应 Godot 节点。
任何未声明支持的属性都会使转换失败，避免静默丢轨。
"""

from __future__ import annotations

import argparse
import json
import re
from pathlib import Path


ATTRIBUTE_RE = re.compile(r"^\s*attribute: (.+?)\s*$", re.MULTILINE)
GUID_RE = re.compile(r"guid: ([0-9a-f]{32})")
NUMBER = r"[-+]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][-+]?\d+)?"
CLIP_RE = re.compile(
    rf"m_Start: (?P<start>{NUMBER})[\s\S]{{0,500}}?"
    r"m_Asset: \{fileID: (?P<asset>-?\d+)[^}]*\}[\s\S]{0,180}?"
    rf"m_Duration: (?P<duration>{NUMBER})"
)
OBJECT_RE = re.compile(r"--- !u!\d+ &(?P<id>-?\d+)\n(?P<body>[\s\S]*?)(?=\n--- !u!|\Z)")
KEY_RE = re.compile(
    rf"time: (?P<time>{NUMBER})\n"
    rf"\s*value: (?P<value>{NUMBER})\n"
    rf"\s*inSlope: (?P<in_slope>{NUMBER})\n"
    rf"\s*outSlope: (?P<out_slope>{NUMBER})"
)

SUPPORTED_ATTRIBUTES = {
    "m_IsActive",
    "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z",
    "localEulerAnglesRaw.x", "localEulerAnglesRaw.y", "localEulerAnglesRaw.z",
    "m_LocalScale.x", "m_LocalScale.y", "m_LocalScale.z",
    "m_Color.r", "m_Color.g", "m_Color.b", "m_Color.a",
    "material._TintColor.r", "material._TintColor.g",
    "material._TintColor.b", "material._TintColor.a",
    "material._AddColor.r", "material._AddColor.g",
    "material._AddColor.b", "material._AddColor.a",
    "material._Amplitude", "material._offset", "material._FakeBlend",
    "material._MainTex_ST.x", "material._MainTex_ST.y",
    "material._MainTex_ST.z", "material._MainTex_ST.w",
    "material._RING_Radial",
}

# Unity 会把部分 Renderer/Material 绑定序列化为 PropertyName 的整数 ID。
# 这里显式锁定源工程出现过的集合；新增未知 ID 同样会使审计失败。
SUPPORTED_HASHED_ATTRIBUTES = {
    "1", "3", "4", "109495689", "304273561", "377931145", "646366601",
    "914802057", "1090582214", "1140649264", "1359017670", "1409084720",
    "1627453126", "1677520176", "1895888582", "1945955632", "2086281974",
    "2151230803", "2227223471", "2276081884", "2305216973", "2334886179",
    "2526845255", "4215373228",
}

REQUIRED_MARKERS = {"StrongSummon", "StartCard"}
REQUIRED_SOUNDS = {
    "SE_SMN_SYNCHRO_01_01", "SE_SMN_SYNCHRO_01_04", "SE_SMN_SYNCHRO_02",
    "SE_SMN_SYNCHRO_03_01", "SE_SMN_SYNCHRO_03_02",
    "SE_SMN_SYNCHRO_03_03", "SE_SMN_SYNCHRO_03_04",
    "SE_SMN_SYNCHRO_04_01", "SE_SMN_SYNCHRO_04_02",
    "SE_SMN_SYNCHRO_04_03", "SE_SMN_SYNCHRO_04_04", "SE_SMN_SYNCHRO_05",
    "SE_SMN_CMN_CARD_01", "SE_SMN_CMN_CARD_02",
}


def quote(value: str) -> str:
    return json.dumps(value, ensure_ascii=False)


def parse_playable(path: Path, root: Path) -> dict[str, object]:
    text = path.read_text(encoding="utf-8-sig")
    attributes = sorted(set(ATTRIBUTE_RE.findall(text)))
    unsupported = [
        value for value in attributes
        if value not in SUPPORTED_ATTRIBUTES and value not in SUPPORTED_HASHED_ATTRIBUTES
    ]
    if unsupported:
        raise ValueError(f"{path}: 未映射属性: {', '.join(unsupported)}")

    objects = {match["id"]: match["body"] for match in OBJECT_RE.finditer(text)}
    labels: dict[str, str] = {}
    markers: list[str] = []
    for object_id, body in objects.items():
        label_match = re.search(r"^\s*(?:startLabel|label): (.+?)\s*$", body, re.MULTILINE)
        if label_match:
            labels[object_id] = label_match.group(1)
        display_match = re.search(r"^\s*m_DisplayName: (StrongSummon|StartCard)\s*$", body, re.MULTILINE)
        if display_match:
            markers.append(display_match.group(1))

    clips = []
    for match in CLIP_RE.finditer(text):
        start = float(match["start"])
        duration = float(match["duration"])
        asset_id = match["asset"]
        clips.append({
            "start": start,
            "duration": duration,
            "asset_file_id": asset_id,
            "label": labels.get(asset_id),
        })
    length = max((clip["start"] + clip["duration"] for clip in clips), default=0.0)

    curves = []
    for attribute in attributes:
        # 同一属性可能被多个绑定使用；逐段保留键值，供人工和自动审计比对。
        positions = [match.start() for match in re.finditer(
            rf"^\s*attribute: {re.escape(attribute)}\s*$", text, re.MULTILINE
        )]
        for index, start in enumerate(positions):
            end = positions[index + 1] if index + 1 < len(positions) else min(len(text), start + 5000)
            keys = [
                {
                    "time": float(key["time"]),
                    "value": float(key["value"]),
                    "in_slope": float(key["in_slope"]),
                    "out_slope": float(key["out_slope"]),
                }
                for key in KEY_RE.finditer(text[start:end])
            ]
            curves.append({"attribute": attribute, "keys": keys})

    return {
        "path": path.relative_to(root).as_posix(),
        "name": path.stem,
        "length": length,
        "attributes": attributes,
        "hashed_shader_property_ids": [value for value in attributes if value.isdigit()],
        "clips": clips,
        "markers": sorted(set(markers)),
        "sounds": sorted({label for label in labels.values() if label.startswith("SE_")}),
        "dependencies": sorted(set(GUID_RE.findall(text))),
        "curves": curves,
    }


def write_library(path: Path, timelines: list[dict[str, object]]) -> None:
    lines = [f'[gd_resource type="AnimationLibrary" load_steps={len(timelines) + 1} format=3]', ""]
    ids = []
    for index, timeline in enumerate(timelines, 1):
        resource_id = f"Animation_{index:02d}"
        ids.append((timeline["name"], resource_id))
        lines.extend([
            f'[sub_resource type="Animation" id="{resource_id}"]',
            f'resource_name = {quote(str(timeline["name"]))}',
            f'length = {max(float(timeline["length"]), 0.001):.9g}',
            "",
        ])
    lines.append("[resource]")
    lines.append("_data = {")
    for index, (name, resource_id) in enumerate(ids):
        comma = "," if index + 1 < len(ids) else ""
        lines.append(f'{quote(str(name))}: SubResource("{resource_id}"){comma}')
    lines.append("}")
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def audit_meshes(mesh_root: Path) -> list[dict[str, object]]:
    results = []
    for path in sorted(mesh_root.glob("*.obj")):
        counts = {"vertices": 0, "uvs": 0, "normals": 0, "faces": 0}
        for line in path.read_text(encoding="utf-8").splitlines():
            if line.startswith("v "): counts["vertices"] += 1
            elif line.startswith("vt "): counts["uvs"] += 1
            elif line.startswith("vn "): counts["normals"] += 1
            elif line.startswith("f "): counts["faces"] += 1
        if not all(counts.values()):
            raise ValueError(f"{path}: OBJ 缺少顶点、UV、法线或三角面")
        results.append({"path": path.name, **counts})
    # 运行时只保留 Circle01..06 与 Accent；SpeedLine/effect01 已由 Godot 原生
    # 光束和脉冲实现替代，不再把未加载的转换产物计入发布资源。
    if len(results) != 7:
        raise ValueError(f"同调运行时 Mesh 应为 7 个，实际为 {len(results)} 个")
    return results


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("source", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--godot-library", type=Path, required=True)
    parser.add_argument("--mesh-root", type=Path, required=True)
    args = parser.parse_args()

    source = args.source.resolve()
    files = sorted(source.rglob("*.playable"))
    if len(files) != 28:
        raise ValueError(f"SummonSynchro 应包含 28 个 playable，实际为 {len(files)} 个")
    timelines = [parse_playable(path, source) for path in files]
    markers = {marker for timeline in timelines for marker in timeline["markers"]}
    sounds = {sound for timeline in timelines for sound in timeline["sounds"]}
    if missing := REQUIRED_MARKERS - markers:
        raise ValueError(f"缺少 Timeline Marker: {sorted(missing)}")
    if missing := REQUIRED_SOUNDS - sounds:
        raise ValueError(f"缺少声音标签: {sorted(missing)}")

    report = {
        "source": source.name,
        "playable_count": len(timelines),
        "prefab_count": len(list(source.rglob("*.prefab"))),
        "mesh_count": len(list(source.rglob("*.asset"))),
        "material_count": len(list(source.rglob("*.mat"))),
        "texture_count": len(list(source.rglob("*.png"))),
        "supported_attributes": sorted(SUPPORTED_ATTRIBUTES),
        "supported_hashed_attributes": sorted(SUPPORTED_HASHED_ATTRIBUTES, key=int),
        "markers": sorted(markers),
        "sounds": sorted(sounds),
        "meshes": audit_meshes(args.mesh_root),
        "timelines": timelines,
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    write_library(args.godot_library, timelines)
    print(
        f"审计通过：{len(timelines)} Playable、{len(report['meshes'])} Mesh、"
        f"{len(sounds)} 个声音标签"
    )


if __name__ == "__main__":
    main()
