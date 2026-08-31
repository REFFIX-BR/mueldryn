#!/usr/bin/env py -3
"""Build Pegasus-style contact-sheet catalogs for Mudream Interface PNG/JPG assets."""
from __future__ import annotations

import argparse
import json
import math
import sys
from dataclasses import dataclass, field
from pathlib import Path

try:
    from PIL import Image, ImageDraw, ImageFont
except ImportError:
    print("Pillow required: py -3 -m pip install pillow", file=sys.stderr)
    raise

IMAGE_EXTS = {".png", ".jpg", ".jpeg"}
SKIP_DIRS = {"_raw", "_catalog", "__pycache__"}

# Pegasus _catalog visual style
BG_COLOR = (28, 30, 34)
HEADER_COLOR = (247, 213, 88)
LABEL_COLOR = (210, 214, 220)
CHECKER_A = (48, 52, 58)
CHECKER_B = (60, 64, 70)

COLS = 10
CELL_W = 118
LABEL_H = 18
THUMB_MAX = 96
CELL_PAD = 6
HEADER_H = 34
MAX_SHEET_HEIGHT = 12000
MAX_ASSETS_PER_SHEET = 300


@dataclass
class CatalogStats:
    sheets: list[dict] = field(default_factory=list)
    skipped_folders: list[str] = field(default_factory=list)
    errors: list[str] = field(default_factory=list)


def load_font(size: int = 11) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    for name in ("arial.ttf", "segoeui.ttf", "calibri.ttf"):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            continue
    return ImageFont.load_default()


def iter_image_files(root: Path) -> list[Path]:
    files: list[Path] = []
    for path in sorted(root.rglob("*")):
        if not path.is_file():
            continue
        if path.suffix.lower() not in IMAGE_EXTS:
            continue
        rel = path.relative_to(root)
        if rel.parts and rel.parts[0] in SKIP_DIRS:
            continue
        files.append(path)
    return files


def group_by_folder(root: Path, files: list[Path]) -> dict[str, list[Path]]:
    groups: dict[str, list[Path]] = {}
    for path in files:
        rel = path.relative_to(root)
        key = rel.parts[0] if len(rel.parts) > 1 else "root"
        groups.setdefault(key, []).append(path)
    return dict(sorted(groups.items(), key=lambda kv: (kv[0] != "root", kv[0].lower())))


def draw_checkerboard(draw: ImageDraw.ImageDraw, box: tuple[int, int, int, int], tile: int = 8) -> None:
    x0, y0, x1, y1 = box
    for y in range(y0, y1, tile):
        for x in range(x0, x1, tile):
            color = CHECKER_A if ((x // tile) + (y // tile)) % 2 == 0 else CHECKER_B
            draw.rectangle([x, y, min(x + tile, x1), min(y + tile, y1)], fill=color)


def fit_thumbnail(img: Image.Image, max_size: int) -> Image.Image:
    w, h = img.size
    if w <= 0 or h <= 0:
        return img
    scale = min(max_size / w, max_size / h, 1.0)
    if scale >= 1.0:
        return img
    nw = max(1, int(w * scale))
    nh = max(1, int(h * scale))
    return img.resize((nw, nh), Image.Resampling.LANCZOS)


def sheet_layout(asset_count: int) -> tuple[int, int, int]:
    cols = COLS
    cell_h = LABEL_H + THUMB_MAX + CELL_PAD * 2
    rows = math.ceil(asset_count / cols)
    width = cols * CELL_W + CELL_PAD * 2
    height = HEADER_H + rows * cell_h + CELL_PAD
    if height > MAX_SHEET_HEIGHT:
        max_rows = max(1, (MAX_SHEET_HEIGHT - HEADER_H - CELL_PAD) // cell_h)
        max_assets = max_rows * cols
        return cols, cell_h, max_assets
    return cols, cell_h, asset_count


def build_sheet(
    group_name: str,
    assets: list[Path],
    root: Path,
    *,
    part: int = 1,
    parts: int = 1,
) -> Image.Image:
    cols, cell_h, _ = sheet_layout(len(assets))
    rows = math.ceil(len(assets) / cols)
    width = cols * CELL_W + CELL_PAD * 2
    height = HEADER_H + rows * cell_h + CELL_PAD

    sheet = Image.new("RGB", (width, height), BG_COLOR)
    draw = ImageDraw.Draw(sheet)
    font = load_font(11)
    header_font = load_font(13)

    suffix = f" ({part}/{parts})" if parts > 1 else ""
    header = f"{group_name} - {len(assets)} assets{suffix}"
    draw.text((8, 8), header, fill=HEADER_COLOR, font=header_font)

    for idx, path in enumerate(assets):
        col = idx % cols
        row = idx // cols
        cx = CELL_PAD + col * CELL_W
        cy = HEADER_H + row * cell_h

        try:
            img = Image.open(path)
            img.load()
            if img.mode not in ("RGBA", "RGB", "L"):
                img = img.convert("RGBA")
            elif img.mode == "RGB":
                img = img.convert("RGBA")
        except Exception:
            img = Image.new("RGBA", (THUMB_MAX, THUMB_MAX), (255, 0, 255, 255))

        label = f"{path.stem} {img.width}x{img.height}"
        if len(label) > 22:
            label = label[:19] + "..."
        draw.text((cx + 2, cy + 1), label, fill=LABEL_COLOR, font=font)

        thumb_box = (cx + 4, cy + LABEL_H + 2, cx + CELL_W - 4, cy + cell_h - 4)
        draw_checkerboard(draw, thumb_box)
        thumb = fit_thumbnail(img, THUMB_MAX)
        tx = thumb_box[0] + (thumb_box[2] - thumb_box[0] - thumb.width) // 2
        ty = thumb_box[1] + (thumb_box[3] - thumb_box[1] - thumb.height) // 2
        if thumb.mode == "RGBA":
            sheet.paste(thumb, (tx, ty), thumb)
        else:
            sheet.paste(thumb, (tx, ty))

    return sheet


def chunk_assets(assets: list[Path]) -> list[list[Path]]:
    if len(assets) <= MAX_ASSETS_PER_SHEET:
        _, _, cap = sheet_layout(len(assets))
        if len(assets) <= cap:
            return [assets]
    chunks: list[list[Path]] = []
    for i in range(0, len(assets), MAX_ASSETS_PER_SHEET):
        chunk = assets[i : i + MAX_ASSETS_PER_SHEET]
        _, _, cap = sheet_layout(len(chunk))
        if len(chunk) <= cap:
            chunks.append(chunk)
        else:
            for j in range(0, len(chunk), cap):
                chunks.append(chunk[j : j + cap])
    return chunks


def write_group_catalogs(
    group_name: str,
    assets: list[Path],
    root: Path,
    out_dirs: list[Path],
    stats: CatalogStats,
) -> None:
    if not assets:
        stats.skipped_folders.append(f"{group_name}: empty")
        return

    chunks = chunk_assets(assets)
    parts = len(chunks)
    for out_dir in out_dirs:
        out_dir.mkdir(parents=True, exist_ok=True)

    for part_idx, chunk in enumerate(chunks, start=1):
        sheet = build_sheet(group_name, chunk, root, part=part_idx, parts=parts)
        base = group_name if parts == 1 else f"{group_name}_part{part_idx:02d}"
        filename = f"{base}.png"
        for out_dir in out_dirs:
            out_path = out_dir / filename
            sheet.save(out_path, format="PNG")
        stats.sheets.append(
            {
                "group": group_name,
                "part": part_idx,
                "parts": parts,
                "asset_count": len(chunk),
                "size": list(sheet.size),
                "files": [str(out_dirs[0] / filename)],
            }
        )


def main() -> int:
    default_src = Path(
        r"c:\Users\JUNIOR-DEV\Downloads\MuServer-20260803T155926Z-1-001\OpenMU\tools\mudream_interface_export"
    )
    default_mirror = Path(
        r"c:\Users\JUNIOR-DEV\Downloads\MuServer-20260803T155926Z-1-001\Mudream.online\Data\Interface\_decrypted"
    )
    parser = argparse.ArgumentParser(description="Generate Mudream Interface asset catalogs")
    parser.add_argument("--src", type=Path, default=default_src)
    parser.add_argument(
        "--out",
        type=Path,
        nargs="+",
        default=[
            default_src / "_catalog",
            default_mirror.parent / "_catalog",
        ],
    )
    args = parser.parse_args()

    src_root: Path = args.src
    out_dirs = [Path(p) for p in args.out]
    stats = CatalogStats()

    files = iter_image_files(src_root)
    groups = group_by_folder(src_root, files)

    for group_name, assets in groups.items():
        try:
            write_group_catalogs(group_name, assets, src_root, out_dirs, stats)
        except Exception as exc:  # noqa: BLE001
            stats.errors.append(f"{group_name}: {exc}")

    report = {
        "source": str(src_root),
        "catalog_outputs": [str(p) for p in out_dirs],
        "image_files_scanned": len(files),
        "groups": {name: len(paths) for name, paths in groups.items()},
        "sheet_count": len(stats.sheets),
        "sheets": stats.sheets,
        "skipped_folders": stats.skipped_folders,
        "errors": stats.errors,
    }

    for out_dir in out_dirs:
        report_path = out_dir / "catalog_report.json"
        out_dir.mkdir(parents=True, exist_ok=True)
        report_path.write_text(json.dumps(report, indent=2), encoding="utf-8")

    print(f"Scanned {len(files)} images in {len(groups)} groups")
    print(f"Created {len(stats.sheets)} catalog sheet(s)")
    for out_dir in out_dirs:
        print(f"Output: {out_dir}")
    if stats.errors:
        print(f"Errors: {len(stats.errors)}")
        for line in stats.errors[:10]:
            print(" ", line)
    return 0 if not stats.errors else 1


if __name__ == "__main__":
    raise SystemExit(main())
