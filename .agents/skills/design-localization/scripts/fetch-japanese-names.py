#!/usr/bin/env python3
"""读取已核实的真实卡号，输出带来源的日文名候选；不修改游戏本地化。"""

import argparse
import json
import os
import sys
import tempfile
import time
import urllib.error
import urllib.request
from datetime import datetime, timezone
from pathlib import Path


BASE_URL = "https://ygocdb.com/api/v0/card/"


def positive_id(value):
    value = int(value)
    if value <= 0:
        raise argparse.ArgumentTypeError("卡号必须为正整数")
    return value


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError(f"重复 JSON key: {key}")
        result[key] = value
    return result


def extract_name(payload, card_id):
    if not isinstance(payload, dict) or type(payload.get("id")) is not int or payload["id"] != card_id:
        raise ValueError("响应 id 不匹配，拒绝采用名称")
    text = payload.get("text")
    name = text.get("jp_name") if isinstance(text, dict) else None
    if not isinstance(name, str) or not name.strip():
        raise ValueError("缺少非空 text.jp_name")
    if any(char in name for char in "<>\r\n"):
        raise ValueError("日文名包含 HTML 或换行，需人工核查")
    cid = payload.get("cid")
    if cid is not None and (type(cid) is not int or cid <= 0):
        raise ValueError("响应 cid 无效")
    return {
        "card_id": card_id,
        "cid": cid,
        "jpn": name,
        "source_url": BASE_URL + str(card_id),
        "source_field": "text.jp_name",
        "queried_at": datetime.now(timezone.utc).isoformat(),
        "official_url": (
            "https://www.db.yugioh-card.com/yugiohdb/card_search.action"
            f"?ope=2&cid={cid}&request_locale=ja" if cid else None
        ),
    }


def valid_cached(record, card_id):
    if not isinstance(record, dict):
        return False
    try:
        extract_name({"id": record.get("card_id"), "cid": record.get("cid"),
                      "text": {"jp_name": record.get("jpn")}}, card_id)
        datetime.fromisoformat(record["queried_at"])
    except (ValueError, TypeError, KeyError):
        return False
    return (record.get("source_url") == BASE_URL + str(card_id)
            and record.get("source_field") == "text.jp_name")


def write_cache(path, cache):
    # 同目录原子替换，避免请求中断留下半份 JSON。
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = None
    try:
        with tempfile.NamedTemporaryFile(mode="w", encoding="utf-8", dir=path.parent,
                                         prefix=path.name + ".", delete=False) as handle:
            temporary = handle.name
            json.dump(cache, handle, ensure_ascii=False, indent=2)
            handle.write("\n")
        os.replace(temporary, path)
    finally:
        if temporary and os.path.exists(temporary):
            os.unlink(temporary)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("card_ids", type=positive_id, nargs="+")
    parser.add_argument("--cache", type=Path, required=True, help="带来源的本地查询缓存 JSON")
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--refresh", action="store_true")
    mode.add_argument("--offline", action="store_true")
    args = parser.parse_args()
    cache = {}
    if args.cache.exists():
        try:
            cache = json.loads(args.cache.read_text(encoding="utf-8-sig"), object_pairs_hook=unique_object)
            if not isinstance(cache, dict):
                raise ValueError("缓存根节点必须为对象")
        except (OSError, ValueError) as error:
            parser.error(f"无法读取缓存，不覆盖原文件：{error}")

    resolved, unresolved = [], []
    requested = False
    for card_id in dict.fromkeys(args.card_ids):
        record = cache.get(str(card_id))
        if not args.refresh and valid_cached(record, card_id):
            resolved.append({**record, "from_cache": True})
            continue
        if args.offline:
            unresolved.append({"card_id": card_id, "reason": "没有有效的本地缓存"})
            continue
        try:
            # 串行限速；失败不静默重试或回退到未经核实的名称。
            if requested:
                time.sleep(0.5)
            requested = True
            request = urllib.request.Request(BASE_URL + str(card_id), headers={
                "User-Agent": "VYgo-localization/1.0", "Accept": "application/json"})
            with urllib.request.urlopen(request, timeout=20) as response:
                raw = response.read(2 * 1024 * 1024 + 1)
            if len(raw) > 2 * 1024 * 1024:
                raise ValueError("响应超过 2 MiB，需核对接口")
            payload = json.loads(raw.decode("utf-8-sig"), object_pairs_hook=unique_object)
            record = extract_name(payload, card_id)
        except (OSError, ValueError, urllib.error.URLError) as error:
            unresolved.append({"card_id": card_id, "reason": str(error)})
            continue
        cache[str(card_id)] = record
        write_cache(args.cache, cache)
        resolved.append({**record, "from_cache": False})

    json.dump({"resolved": resolved, "unresolved": unresolved}, sys.stdout, ensure_ascii=False, indent=2)
    print()
    return 1 if unresolved else 0


if __name__ == "__main__":
    sys.exit(main())
