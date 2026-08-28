#!/usr/bin/env py -3
"""Exporta ícones PNG de cosméticos Mudream (OZJ/OZT) para a web Morpheus.

Saída: MorpheusWeb_SuporteS21(2)/resources/images/items/visual/{Group}-{Number}.png

Uso:
  py -3 tools/export-cosmetic-icons.py
  py -3 tools/export-cosmetic-icons.py --only 7-316,7-313,12-317
"""
from __future__ import annotations

import argparse
import io
import json
import struct
import sys
from pathlib import Path

try:
    from PIL import Image, ImageOps
except ImportError:
    print("Instale Pillow: py -3 -m pip install pillow", file=sys.stderr)
    raise

ROOT = Path(__file__).resolve().parents[1]
WORKSPACE = ROOT.parent
MODELS_JSON = WORKSPACE / "OpenMU" / "tools" / "mudream_cosmetic_models.json"
CATALOG_JSON = WORKSPACE / "OpenMU" / "tools" / "mudream_cosmetic_catalog.json"
OUT_DIR = ROOT / "resources" / "images" / "items" / "visual"
DATA_ROOTS = [
    WORKSPACE / "MuMain" / "src" / "bin" / "Data",
    WORKSPACE / "Mudream.online" / "Data",
]


def load_jpeg_blob(data: bytes) -> Image.Image:
    for off in (24, 0):
        if off < len(data) - 3 and data[off : off + 3] == b"\xff\xd8\xff":
            img = Image.open(io.BytesIO(data[off:]))
            img.load()
            return img.convert("RGBA")
    raise ValueError("OZJ/JPEG inválido")


def read_bgra(data: bytes, idx: int, nx: int, ny: int) -> Image.Image:
    img = Image.new("RGBA", (nx, ny))
    px = img.load()
    off = idx
    for y in range(ny):
        for x in range(nx):
            b, g, r, a = data[off : off + 4]
            off += 4
            px[x, ny - 1 - y] = (r, g, b, a)
    return img


def load_ozt(data: bytes) -> Image.Image | None:
    if len(data) < 20:
        return None
    for idx in (16, 4):
        if idx + 6 >= len(data):
            continue
        nx = struct.unpack_from("<h", data, idx)[0]
        ny = struct.unpack_from("<h", data, idx + 2)[0]
        bit = data[idx + 4]
        if bit != 32 or nx <= 0 or ny <= 0 or nx > 4096 or ny > 4096:
            continue
        start = idx + 6
        if start + nx * ny * 4 <= len(data):
            return read_bgra(data, start, nx, ny)
    return None


def load_texture(path: Path) -> Image.Image | None:
    try:
        data = path.read_bytes()
    except OSError:
        return None
    ext = path.suffix.lower()
    try:
        if ext == ".ozj":
            return load_jpeg_blob(data)
        if ext == ".ozt":
            return load_ozt(data)
        if ext in (".png", ".jpg", ".jpeg", ".gif", ".webp"):
            img = Image.open(path)
            img.load()
            return img.convert("RGBA")
    except Exception:
        return None
    return None


def skin_dir_for(row: dict) -> Path | None:
    raw_dir = row.get("Dir", "").replace("\\\\", "\\")
    rel = raw_dir.replace("Data\\", "").replace("Data/", "").strip("\\/")
    for base in DATA_ROOTS:
        candidate = base / rel
        if candidate.is_dir():
            return candidate
    return None


def glob_one(folder: Path, patterns: list[str]) -> Path | None:
    for pat in patterns:
        hits = sorted(folder.glob(pat))
        if hits:
            return hits[0]
    return None


def pick_texture(folder: Path, group: int, file_base: str) -> Path | None:
    fb = file_base.lower()
    section_patterns: dict[int, list[str]] = {
        7: [
            f"{file_base}.ozj",
            f"{file_base}.ozt",
            "*_hel*.ozj",
            "*helm*.ozj",
            "RIV_STYLE_SET_helm.ozj",
            "RIV_SETBK*_hel*.ozj",
            "RIV_*_hel*.ozj",
        ],
        8: [
            f"{file_base}.ozj",
            "RIV_STYLE_SET.ozj",
            "RIV_STYLE_SETR.ozj",
            "RIV_SETBK*.ozj",
            "*armor*.ozj",
        ],
        9: [
            f"{file_base}.ozj",
            "RIV_STYLE_SETF.ozt",
            "RIV_STYLE_SET.ozj",
            "*pants*.ozj",
        ],
        10: [
            f"{file_base}.ozj",
            "*gloves*.ozj",
            "RIV_STYLE_SET.ozj",
        ],
        11: [
            f"{file_base}.ozj",
            "*boots*.ozj",
            "RIV_STYLE_SET.ozj",
        ],
        12: [
            f"{file_base}.ozj",
            "*wing*.ozj",
            "*WING*.ozj",
            "*cape*.ozj",
            "*Cape*.ozj",
            "RIV_WING*.ozj",
            "RIV_CAPA*.ozj",
        ],
        13: [
            f"{file_base}.ozj",
            "*ring*.ozj",
            "*pendant*.ozj",
            "*acc*.ozj",
        ],
    }
    weapon_groups = {0, 2, 4, 5, 6}
    if group in weapon_groups:
        patterns = [f"{file_base}.ozj", f"{file_base}.ozt", f"{fb}*.ozj", "RIV_STYLE_SW*.ozj"]
    else:
        patterns = section_patterns.get(group, [f"{file_base}.ozj", f"{file_base}.ozt"])

    found = glob_one(folder, patterns)
    if found:
        return found

    # fallback: any RIV texture in skin folder
    return glob_one(folder, ["RIV_*.ozj", "RIV_*.ozt", "*.ozj"])


def normalize_icon(img: Image.Image, size: int = 128) -> Image.Image:
    img = img.convert("RGBA")
    # recorte quadrado central
    w, h = img.size
    side = min(w, h)
    left = (w - side) // 2
    top = (h - side) // 2
    img = img.crop((left, top, left + side, top + side))
    img = ImageOps.contain(img, (size, size), method=Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    ox = (size - img.width) // 2
    oy = (size - img.height) // 2
    canvas.paste(img, (ox, oy), img)
    return canvas


def load_json(path: Path):
    text = path.read_text(encoding="utf-8-sig")
    return json.loads(text)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--only", help="Chaves Group-Number separadas por vírgula")
    parser.add_argument("--size", type=int, default=128)
    args = parser.parse_args()

    only = set()
    if args.only:
        only = {x.strip() for x in args.only.split(",") if x.strip()}

    if not MODELS_JSON.is_file():
        print("Não encontrado:", MODELS_JSON, file=sys.stderr)
        return 1

    models = load_json(MODELS_JSON)
    cosmetic_keys = set()
    if CATALOG_JSON.is_file():
        for row in load_json(CATALOG_JSON):
            if row.get("Cosmetic") and "Group" in row and "Number" in row:
                cosmetic_keys.add(f"{row['Group']}-{row['Number']}")

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    ok = 0
    skip = 0
    fail = 0

    for row in models:
        group = int(row["Group"])
        number = int(row["Number"])
        key = f"{group}-{number}"
        if cosmetic_keys and key not in cosmetic_keys:
            continue
        if only and key not in only:
            continue

        folder = skin_dir_for(row)
        file_base = row.get("File", "")
        out_path = OUT_DIR / f"{key}.png"
        if not folder:
            fail += 1
            print(f"[FAIL] {key} pasta não encontrada: {row.get('Dir')}")
            continue

        tex = pick_texture(folder, group, file_base)
        if not tex:
            fail += 1
            print(f"[FAIL] {key} sem textura em {folder.name}")
            continue

        img = load_texture(tex)
        if img is None:
            fail += 1
            print(f"[FAIL] {key} não leu {tex.name}")
            continue

        icon = normalize_icon(img, args.size)
        icon.save(out_path, format="PNG", optimize=True)
        ok += 1
        print(f"[OK] {key} {row.get('Name')} <- {tex.name}")

    print(f"\nExportados: {ok} | Falhas: {fail} | Pasta: {OUT_DIR}")
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
