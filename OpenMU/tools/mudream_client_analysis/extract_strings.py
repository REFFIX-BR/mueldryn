#!/usr/bin/env python3
"""Extract ASCII/UTF-16LE strings from Mudream client binaries for porting analysis.

Developmental RE aid only: inventory UI/network markers for MuMain/OpenMU ports.
Does not dump memory, bypass DRM, or produce exploits.
"""
from __future__ import annotations

import argparse
import json
import re
import sys
from collections import defaultdict
from pathlib import Path

# Keywords of interest for Mudream custom systems / protocol clues
KEYWORDS = [
    r"EventList",
    r"Events?\s*Timer",
    r"EventSchedule",
    r"ActiveInvasion",
    r"Invasion",
    r"SoulSystem",
    r"Soul\b",
    r"QuestInfo",
    r"QuestPanel",
    r"Quest",
    r"LegendUI",
    r"LegendHUD",
    r"MainMenu",
    r"DeathJournal",
    r"DamageStatistic",
    r"GuildManager",
    r"GuildOracle",
    r"PartySearch",
    r"MacroUI",
    r"Collections",
    r"CharacterOverview",
    r"HarmonyMix",
    r"InGameShop",
    r"JewelBank",
    r"VipShop",
    r"Dungeon",
    r"BossLife",
    r"NaviMap",
    r"Notification",
    r"NpcIcons",
    r"GuestAccess",
    r"GameMaster",
    r"SkillStack",
    r"PartCharge",
    r"bottom_panel",
    r"new_main_frame",
    r"GFx",
    r"Scaleform",
    r"packet",
    r"opcode",
    r"0xFA",
    r"0xFB",
    r"0xFC",
    r"0xFE",
    r"C1.*FA",
    r"Interface\\",
    r"Data\\Interface",
    r"Mudream",
    r"MuDream",
    r"Reborn",
    r"OpenMU",
    r"BugSplat",
    r"discord",
    r"fmod",
    r"\.json",
    r"\.lua",
    r"\.xml",
    r"\.bmd",
    r"Window",
    r"NewUI",
]

KEYWORD_RE = re.compile("|".join(f"(?:{k})" for k in KEYWORDS), re.IGNORECASE)


def extract_ascii(data: bytes, min_len: int = 4) -> list[str]:
    # Printable ASCII runs
    pattern = re.compile(rb"[\x20-\x7e]{" + str(min_len).encode() + rb",}")
    return [m.group().decode("ascii", errors="ignore") for m in pattern.finditer(data)]


def extract_utf16le(data: bytes, min_len: int = 4) -> list[str]:
    # UTF-16LE: char + null pairs for printable ASCII range (common in Win binaries)
    out: list[str] = []
    i = 0
    n = len(data)
    while i + 2 <= n:
        chars: list[str] = []
        j = i
        while j + 1 < n:
            lo, hi = data[j], data[j + 1]
            if hi == 0 and 0x20 <= lo <= 0x7E:
                chars.append(chr(lo))
                j += 2
            else:
                break
        if len(chars) >= min_len:
            out.append("".join(chars))
            i = j
        else:
            i += 2 if i % 2 == 0 else 1
    return out


def classify(s: str) -> list[str]:
    tags: list[str] = []
    low = s.lower()
    mapping = [
        ("event", ("eventlist", "eventschedule", "event timer", "eventstimer", "events timer")),
        ("invasion", ("invasion", "activeinvasion")),
        ("soul", ("soul",)),
        ("quest", ("quest",)),
        ("legend", ("legendui", "legendhud")),
        ("menu_hud", ("mainmenu", "bottom_panel", "new_main_frame", "legendhud")),
        ("guild", ("guildmanager", "guildoracle", "guild")),
        ("party", ("partysearch", "party")),
        ("shop", ("ingameshop", "jewelbank", "vipshop", "partcharge")),
        ("dungeon", ("dungeon", "bosslife", "boss")),
        ("macro", ("macroui", "macro")),
        ("death", ("deathjournal", "death")),
        ("damage", ("damagestatistic", "damage")),
        ("collections", ("collection",)),
        ("gfx", ("gfx", "scaleform")),
        ("protocol", ("0xfa", "0xfb", "0xfc", "0xfe", "opcode", "packet", "c1", "c2", "c3")),
        ("path", ("interface\\", "data\\", ".json", ".lua", ".xml", ".bmd", ".jpg", ".png")),
        ("brand", ("mudream", "mudream", "reborn")),
    ]
    for tag, keys in mapping:
        if any(k in low for k in keys):
            tags.append(tag)
    return tags or ["other_match"]


def analyze_file(path: Path, min_len: int = 4) -> dict:
    data = path.read_bytes()
    ascii_s = extract_ascii(data, min_len)
    utf16_s = extract_utf16le(data, min_len)

    hits: dict[str, list[dict]] = defaultdict(list)
    seen: set[str] = set()

    for enc, strings in (("ascii", ascii_s), ("utf16le", utf16_s)):
        for s in strings:
            if not KEYWORD_RE.search(s):
                continue
            key = f"{enc}:{s}"
            if key in seen:
                continue
            seen.add(key)
            for tag in classify(s):
                hits[tag].append({"encoding": enc, "string": s})

    # Cap per tag for readability
    capped = {tag: items[:200] for tag, items in sorted(hits.items())}
    return {
        "path": str(path),
        "size_bytes": len(data),
        "ascii_string_count": len(ascii_s),
        "utf16_string_count": len(utf16_s),
        "keyword_hit_tags": {k: len(v) for k, v in capped.items()},
        "hits": capped,
    }


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("binaries", nargs="+", type=Path, help="Paths to .exe/.dll")
    ap.add_argument("-o", "--out", type=Path, required=True, help="Output JSON path")
    ap.add_argument("--min-len", type=int, default=4)
    args = ap.parse_args()

    results = []
    for p in args.binaries:
        if not p.is_file():
            print(f"SKIP missing: {p}", file=sys.stderr)
            continue
        print(f"Scanning {p} ({p.stat().st_size} bytes)...")
        results.append(analyze_file(p, args.min_len))

    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(results, indent=2, ensure_ascii=False), encoding="utf-8")
    print(f"Wrote {args.out}")

    # Also write a flat interesting-strings text dump
    txt = args.out.with_suffix(".txt")
    lines: list[str] = []
    for r in results:
        lines.append(f"===== {r['path']} ({r['size_bytes']} bytes) =====")
        for tag, items in r["hits"].items():
            lines.append(f"\n--- [{tag}] ({len(items)}) ---")
            for it in items:
                lines.append(f"  [{it['encoding']}] {it['string']}")
        lines.append("")
    txt.write_text("\n".join(lines), encoding="utf-8")
    print(f"Wrote {txt}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
