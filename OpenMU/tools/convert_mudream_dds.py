#!/usr/bin/env py -3
"""Convert Mudream Interface .dds files to PNG in export and _decrypted mirrors."""
from __future__ import annotations

import argparse
import json
import sys
from dataclasses import dataclass, field
from pathlib import Path

try:
    from PIL import Image
except ImportError:
    print("Pillow required: py -3 -m pip install pillow", file=sys.stderr)
    raise


@dataclass
class DdsStats:
    success: int = 0
    failed: list[str] = field(default_factory=list)
    skipped_existing: int = 0


def dds_to_png(data: bytes) -> Image.Image:
    img = Image.open(__import__("io").BytesIO(data))
    img.load()
    return img.convert("RGBA")


def save_png(img: Image.Image, path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    img.save(path, format="PNG")


def rel_png_for_dds(rel: Path) -> Path:
    return rel.with_suffix(".png")


def find_dds_files(src_root: Path) -> list[Path]:
    skip = {"_decrypted", "_catalog"}
    files: list[Path] = []
    for path in sorted(src_root.rglob("*.dds")):
        if any(part in skip for part in path.relative_to(src_root).parts):
            continue
        files.append(path)
    return files


def convert_one(
    src: Path,
    src_root: Path,
    out_roots: list[Path],
    stats: DdsStats,
    *,
    overwrite: bool,
) -> None:
    rel = src.relative_to(src_root)
    rel_png = rel_png_for_dds(rel)

    try:
        data = src.read_bytes()
        img = dds_to_png(data)
    except Exception as exc:  # noqa: BLE001
        stats.failed.append(f"{rel}: {exc}")
        return

    for out_root in out_roots:
        out_path = out_root / rel_png
        if out_path.exists() and not overwrite:
            stats.skipped_existing += 1
            continue
        save_png(img, out_path)

    stats.success += 1


def main() -> int:
    default_src = Path(
        r"c:\Users\JUNIOR-DEV\Downloads\MuServer-20260803T155926Z-1-001\Mudream.online\Data\Interface"
    )
    default_out = Path(
        r"c:\Users\JUNIOR-DEV\Downloads\MuServer-20260803T155926Z-1-001\OpenMU\tools\mudream_interface_export"
    )
    default_mirror = default_src / "_decrypted"

    parser = argparse.ArgumentParser(description="Convert Mudream Interface DDS to PNG")
    parser.add_argument("--src", type=Path, default=default_src)
    parser.add_argument("--out", type=Path, default=default_out)
    parser.add_argument("--mirror", type=Path, default=default_mirror)
    parser.add_argument("--raw", type=Path, default=None, help="fallback scan for .dds in _raw")
    parser.add_argument("--overwrite", action="store_true")
    args = parser.parse_args()

    src_root: Path = args.src
    out_roots = [args.out]
    if args.mirror:
        out_roots.append(args.mirror)

    stats = DdsStats()
    dds_files = find_dds_files(src_root)
    raw_root = args.raw or (args.out / "_raw")

    for src in dds_files:
        convert_one(src, src_root, out_roots, stats, overwrite=args.overwrite)

    report = {
        "source": str(src_root),
        "outputs": [str(p) for p in out_roots],
        "raw_fallback": str(raw_root),
        "dds_found": len(dds_files),
        "converted_success": stats.success,
        "skipped_existing": stats.skipped_existing,
        "failed_count": len(stats.failed),
        "failed": stats.failed,
    }

    report_path = args.out / "dds_conversion_report.json"
    report_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.write_text(json.dumps(report, indent=2), encoding="utf-8")

    decrypt_report = args.out / "decrypt_report.json"
    if decrypt_report.exists():
        merged = json.loads(decrypt_report.read_text(encoding="utf-8"))
        copied = merged.setdefault("copied_or_converted", {})
        copied["dds->png"] = stats.success
        if "dds-raw-only" in copied:
            copied["dds-raw-only"] = 0
        merged["dds_conversion"] = {
            "success": stats.success,
            "failed_count": len(stats.failed),
        }
        decrypt_report.write_text(json.dumps(merged, indent=2), encoding="utf-8")

    print(f"DDS found: {len(dds_files)}")
    print(f"Converted: {stats.success}")
    print(f"Skipped (existing): {stats.skipped_existing}")
    print(f"Failed: {len(stats.failed)}")
    print(f"Report: {report_path}")
    if stats.failed:
        for line in stats.failed[:10]:
            print(" ", line)
    return 0 if not stats.failed else 1


if __name__ == "__main__":
    raise SystemExit(main())
