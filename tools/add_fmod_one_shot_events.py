#!/usr/bin/env python3
"""Add simple one-shot OGG events to the repository's FMOD Studio project.

FMOD stores one object graph per event. This script clones the known-good
link_summon_01 graph, gives every owned object a fresh identity, and points the
SingleSound at a freshly registered AudioFile. FMOD Studio remains responsible
for validating and building the resulting project.
"""

from __future__ import annotations

import argparse
import json
import re
import shutil
import subprocess
import uuid
from pathlib import Path


GUID_PATTERN = re.compile(r"\{[0-9a-fA-F-]{36}\}")
EVENT_TEMPLATE_ID = "{28b80ea8-0954-4dfa-9071-5bb87a48d082}"
AUDIO_TEMPLATE_ID = "{2064064b-eb1c-4e77-a301-847c542c84c5}"
SHARED_IDS = {
    "{21cb0010-b75f-4a5b-b60e-92e9ddff5ece}",  # event:/vygo/sfx
    "{31a6a70e-1f02-4134-9316-869034f110cd}",  # SFX mixer group
    "{efeb19bb-16ce-47f4-b21f-9d1b77f70568}",  # VYgo bank
    "{aa864338-a582-4286-9df3-132b1c18f066}",  # master asset folder
}


def new_guid() -> str:
    return "{" + str(uuid.uuid4()) + "}"


def probe(path: Path) -> tuple[float, float, int]:
    result = subprocess.run(
        [
            "ffprobe",
            "-v",
            "error",
            "-show_entries",
            "stream=sample_rate,channels,duration",
            "-of",
            "json",
            str(path),
        ],
        check=True,
        capture_output=True,
        text=True,
    )
    stream = json.loads(result.stdout)["streams"][0]
    return (
        float(stream["duration"]),
        float(stream["sample_rate"]) / 1000.0,
        int(stream["channels"]),
    )


def format_number(value: float) -> str:
    return f"{value:.15g}"


def add_event(
    project: Path,
    source: Path,
    event_name: str,
    event_template: str,
    audio_template: str,
) -> None:
    event_dir = project / "Metadata" / "Event"
    audio_dir = project / "Metadata" / "AudioFile"
    asset_dir = project / "Assets"

    for event_path in event_dir.glob("*.xml"):
        if f"<value>{event_name}</value>" in event_path.read_text(encoding="utf-8"):
            print(f"Skipping existing event {event_name}")
            return

    duration, frequency_khz, channels = probe(source)
    audio_id = new_guid()
    event_id = new_guid()

    audio_xml = audio_template
    audio_xml = audio_xml.replace(AUDIO_TEMPLATE_ID, audio_id)
    audio_xml = audio_xml.replace("SE_SMN_LINK_01.ogg", source.name)
    audio_xml = re.sub(
        r"(<property name=\"frequencyInKHz\">\s*<value>)[^<]+",
        rf"\g<1>{format_number(frequency_khz)}",
        audio_xml,
    )
    audio_xml = re.sub(
        r"(<property name=\"channelCount\">\s*<value>)[^<]+",
        rf"\g<1>{channels}",
        audio_xml,
    )
    audio_xml = re.sub(
        r"(<property name=\"length\">\s*<value>)[^<]+",
        rf"\g<1>{format_number(duration)}",
        audio_xml,
    )

    guid_map: dict[str, str] = {
        EVENT_TEMPLATE_ID: event_id,
        AUDIO_TEMPLATE_ID: audio_id,
    }
    for guid in dict.fromkeys(GUID_PATTERN.findall(event_template)):
        if guid not in SHARED_IDS and guid not in guid_map:
            guid_map[guid] = new_guid()

    event_xml = event_template.replace(
        "<value>link_summon_01</value>",
        f"<value>{event_name}</value>",
        1,
    )
    event_xml = re.sub(
        r"(<object class=\"SingleSound\"[\s\S]*?<property name=\"length\">\s*<value>)[^<]+",
        rf"\g<1>{format_number(duration)}",
        event_xml,
        count=1,
    )
    for old_guid, replacement in guid_map.items():
        event_xml = event_xml.replace(old_guid, replacement)

    asset_dir.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, asset_dir / source.name)
    (audio_dir / f"{audio_id}.xml").write_text(audio_xml, encoding="utf-8")
    (event_dir / f"{event_id}.xml").write_text(event_xml, encoding="utf-8")
    print(f"Added event:/vygo/sfx/{event_name} from {source.name}")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("project", type=Path)
    parser.add_argument("source_directory", type=Path)
    args = parser.parse_args()

    project = args.project.resolve()
    source_directory = args.source_directory.resolve()
    event_template = (
        project / "Metadata" / "Event" / f"{EVENT_TEMPLATE_ID}.xml"
    ).read_text(encoding="utf-8")
    audio_template = (
        project / "Metadata" / "AudioFile" / f"{AUDIO_TEMPLATE_ID}.xml"
    ).read_text(encoding="utf-8")

    events = {
        "xyz_01": "SE_SMN_XYZ_01.ogg",
        "xyz_02_01": "SE_SMN_XYZ_02_01.ogg",
        "xyz_02_02": "SE_SMN_XYZ_02_02.ogg",
        "xyz_02_03": "SE_SMN_XYZ_02_03.ogg",
        "xyz_03": "SE_SMN_XYZ_03.ogg",
        "xyz_04": "SE_SMN_XYZ_04.ogg",
        "xyz_material": "SE_SUMMON_XYZ_MATERIAL.ogg",
    }
    for event_name, filename in events.items():
        add_event(
            project,
            source_directory / filename,
            event_name,
            event_template,
            audio_template,
        )


if __name__ == "__main__":
    main()
