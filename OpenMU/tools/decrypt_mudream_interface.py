#!/usr/bin/env py -3
"""Decrypt Mudream Interface assets (.tdream / .jdream / .pdream) and export PNG/JPG.

Encryption: 3-byte repeating XOR (same family as BuxConvert, Mudream-specific keys):
  .tdream -> OZT/TGA wrapper  key {0x58, 0xB6, 0x85}
  .jdream -> JPEG/OZJ          key {0x8D, 0x39, 0x1C}
  .pdream -> PNG               key {0x3A, 0xEC, 0x29}

Plain files (.png, .dds, .ozt, .ozj) are converted/copied when possible.
"""
from __future__ import annotations

import argparse
import io
import json
import struct
import sys
from dataclasses import dataclass, field
from pathlib import Path

try:
    from PIL import Image
except ImportError:
    print("Pillow required: py -3 -m pip install pillow", file=sys.stderr)
    raise

KEY_TDREAM = bytes((0x58, 0xB6, 0x85))
KEY_JDREAM = bytes((0x8D, 0x39, 0x1C))
KEY_PDREAM = bytes((0x3A, 0xEC, 0x29))

IMAGE_MAGICS = {
    "png": b"\x89PNG\r\n\x1a\n",
    "jpg": b"\xff\xd8\xff",
    "dds": b"DDS ",
}


@dataclass
class Stats:
    decrypted: dict[str, int] = field(default_factory=dict)
    copied: dict[str, int] = field(default_factory=dict)
    failed: list[str] = field(default_factory=list)

    def inc(self, bucket: dict[str, int], key: str) -> None:
        bucket[key] = bucket.get(key, 0) + 1


def xor_decrypt(data: bytes, key: bytes) -> bytes:
    return bytes(b ^ key[i % 3] for i, b in enumerate(data))


def validate_magic(data: bytes, kind: str) -> bool:
    magic = IMAGE_MAGICS.get(kind)
    return bool(magic and data.startswith(magic))


def _read_bgra_image(data: bytes, idx: int, nx: int, ny: int) -> Image.Image | None:
    need = idx + nx * ny * 4
    if len(data) < need:
        return None
    img = Image.new("RGBA", (nx, ny))
    px = img.load()
    off = idx
    for y in range(ny):
        for x in range(nx):
            b, g, r, a = data[off : off + 4]
            off += 4
            px[x, ny - 1 - y] = (r, g, b, a)
    return img


def tga_to_image(data: bytes) -> Image.Image | None:
    """Parse plain 32-bit BGRA TGA (tdream decrypt output)."""
    if len(data) < 18:
        return None
    image_type = data[2]
    nx = struct.unpack_from("<H", data, 12)[0]
    ny = struct.unpack_from("<H", data, 14)[0]
    bit = data[16]
    if bit != 32 or nx <= 0 or ny <= 0 or nx > 8192 or ny > 8192:
        return None
    if image_type not in (0, 2, 10):
        return None
    id_len = data[0]
    idx = 18 + id_len
    if image_type == 10:
        return _read_bgra_rle_image(data, idx, nx, ny)
    return _read_bgra_image(data, idx, nx, ny)


def _read_bgra_rle_image(data: bytes, idx: int, nx: int, ny: int) -> Image.Image | None:
    img = Image.new("RGBA", (nx, ny))
    px = img.load()
    off = idx
    x = y = 0
    while y < ny and off + 1 < len(data):
        header = data[off]
        off += 1
        count = (header & 0x7F) + 1
        if header & 0x80:
            if off + 3 >= len(data):
                break
            b, g, r, a = data[off : off + 4]
            off += 4
            for _ in range(count):
                if y >= ny:
                    break
                px[x, ny - 1 - y] = (r, g, b, a)
                x += 1
                if x >= nx:
                    x = 0
                    y += 1
        else:
            for _ in range(count):
                if off + 3 >= len(data) or y >= ny:
                    break
                b, g, r, a = data[off : off + 4]
                off += 4
                px[x, ny - 1 - y] = (r, g, b, a)
                x += 1
                if x >= nx:
                    x = 0
                    y += 1
    return img


def ozt_to_image(data: bytes) -> Image.Image | None:
    """Parse Mu Online OZT (TGA-derived wrapper) into RGBA PIL image."""
    if len(data) < 22:
        return None
    idx = 16  # GlobalBitmap: skip 12 + 4 byte OZT marker
    nx = struct.unpack_from("<h", data, idx)[0]
    idx += 2
    ny = struct.unpack_from("<h", data, idx)[0]
    idx += 2
    bit = data[idx]
    idx += 2
    if bit != 32 or nx <= 0 or ny <= 0 or nx > 8192 or ny > 8192:
        return None
    return _read_bgra_image(data, idx, nx, ny)


def texture_blob_to_image(data: bytes) -> Image.Image | None:
    """Try plain TGA first (tdream), then OZT wrapper (.ozt)."""
    return tga_to_image(data) or ozt_to_image(data)


def load_jpeg_blob(data: bytes, prefer_ozj_header: bool = False) -> Image.Image:
    offsets = [24, 0] if prefer_ozj_header else [0, 24]
    last_error: Exception | None = None
    for off in offsets:
        if off >= len(data) - 3 or not validate_magic(data[off:], "jpg"):
            continue
        try:
            img = Image.open(io.BytesIO(data[off:]))
            img.load()
            return img.convert("RGB")
        except Exception as exc:  # noqa: BLE001
            last_error = exc
    raise ValueError(f"not a JPEG/OZJ blob: {last_error}")


def save_png(img: Image.Image, path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    if img.mode not in ("RGBA", "RGB", "L"):
        img = img.convert("RGBA")
    img.save(path, format="PNG")


def save_jpg(img: Image.Image, path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    img.convert("RGB").save(path, format="JPEG", quality=95)


def export_binary(data: bytes, out_path: Path) -> None:
    out_path.parent.mkdir(parents=True, exist_ok=True)
    out_path.write_bytes(data)


def rel_out(src_root: Path, out_root: Path, rel: Path, new_name: str) -> Path:
    return out_root / rel.parent / new_name


def process_file(src: Path, src_root: Path, out_root: Path, raw_root: Path, stats: Stats) -> None:
    rel = src.relative_to(src_root)
    ext = src.suffix.lower()

    try:
        if ext == ".tdream":
            raw = xor_decrypt(src.read_bytes(), KEY_TDREAM)
            rel_raw = rel.with_suffix(".tga")
            export_binary(raw, raw_root / rel_raw)
            img = texture_blob_to_image(raw)
            if img is None:
                raise ValueError("TGA/OZT parse failed after XOR")
            out = rel_out(src_root, out_root, rel, src.stem + ".png")
            save_png(img, out)
            stats.inc(stats.decrypted, "tdream->png")

        elif ext == ".jdream":
            raw = xor_decrypt(src.read_bytes(), KEY_JDREAM)
            rel_raw = rel.with_suffix(".ozj" if raw[24:27] == b"\xff\xd8\xff" else ".jpg")
            export_binary(raw, raw_root / rel_raw)
            img = load_jpeg_blob(raw, prefer_ozj_header=False)
            out = rel_out(src_root, out_root, rel, src.stem + ".jpg")
            save_jpg(img, out)
            stats.inc(stats.decrypted, "jdream->jpg")

        elif ext == ".pdream":
            raw = xor_decrypt(src.read_bytes(), KEY_PDREAM)
            if not validate_magic(raw, "png"):
                raise ValueError("PNG magic missing after XOR")
            export_binary(raw, raw_root / rel.with_suffix(".png"))
            img = Image.open(io.BytesIO(raw))
            out = rel_out(src_root, out_root, rel, src.stem + ".png")
            save_png(img.convert("RGBA"), out)
            stats.inc(stats.decrypted, "pdream->png")

        elif ext == ".png":
            img = Image.open(src)
            out = rel_out(src_root, out_root, rel, src.name)
            save_png(img.convert("RGBA"), out)
            stats.inc(stats.copied, "png")

        elif ext == ".ozt":
            raw = src.read_bytes()
            export_binary(raw, raw_root / rel)
            img = texture_blob_to_image(raw)
            if img is None:
                raise ValueError("OZT/TGA parse failed")
            out = rel_out(src_root, out_root, rel, src.stem + ".png")
            save_png(img, out)
            stats.inc(stats.copied, "ozt->png")

        elif ext == ".ozj":
            raw = src.read_bytes()
            export_binary(raw, raw_root / rel)
            img = load_jpeg_blob(raw, prefer_ozj_header=True)
            out = rel_out(src_root, out_root, rel, src.stem + ".jpg")
            save_jpg(img, out)
            stats.inc(stats.copied, "ozj->jpg")

        elif ext == ".jpg":
            img = Image.open(src)
            out = rel_out(src_root, out_root, rel, src.name)
            save_jpg(img, out)
            stats.inc(stats.copied, "jpg")

        elif ext == ".dds":
            raw = src.read_bytes()
            export_binary(raw, raw_root / rel)
            img = Image.open(io.BytesIO(raw))
            img.load()
            out = rel_out(src_root, out_root, rel, src.stem + ".png")
            save_png(img.convert("RGBA"), out)
            stats.inc(stats.copied, "dds->png")

        elif ext == ".json":
            export_binary(src.read_bytes(), raw_root / rel)
            stats.inc(stats.copied, "json")

        else:
            stats.inc(stats.copied, f"skipped{ext}")

    except Exception as exc:  # noqa: BLE001 - batch tool collects failures
        stats.failed.append(f"{rel}: {exc}")


def main() -> int:
    default_src = Path(r"c:\Users\JUNIOR-DEV\Downloads\MuServer-20260803T155926Z-1-001\Mudream.online\Data\Interface")
    default_out = Path(r"c:\Users\JUNIOR-DEV\Downloads\MuServer-20260803T155926Z-1-001\OpenMU\tools\mudream_interface_export")
    parser = argparse.ArgumentParser(description="Decrypt Mudream Interface assets")
    parser.add_argument("--src", type=Path, default=default_src)
    parser.add_argument("--out", type=Path, default=default_out)
    parser.add_argument("--raw", type=Path, default=None, help="intermediate decrypted binaries")
    args = parser.parse_args()

    src_root: Path = args.src
    out_root: Path = args.out
    raw_root: Path = args.raw or (out_root / "_raw")
    stats = Stats()

    files = sorted(p for p in src_root.rglob("*") if p.is_file())
    for src in files:
        process_file(src, src_root, out_root, raw_root, stats)

    report = {
        "source": str(src_root),
        "output_png_jpg": str(out_root),
        "raw_decrypted": str(raw_root),
        "encryption": {
            "tdream": {"key_hex": KEY_TDREAM.hex(), "format": "plain TGA (32-bit BGRA)"},
            "jdream": {"key_hex": KEY_JDREAM.hex(), "format": "JPEG/OZJ"},
            "pdream": {"key_hex": KEY_PDREAM.hex(), "format": "PNG"},
        },
        "decrypted": stats.decrypted,
        "copied_or_converted": stats.copied,
        "failed_count": len(stats.failed),
        "failed_samples": stats.failed[:30],
    }
    report_path = out_root / "decrypt_report.json"
    report_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.write_text(json.dumps(report, indent=2), encoding="utf-8")

    total_ok = sum(stats.decrypted.values()) + sum(stats.copied.values())
    print(f"Processed {len(files)} files from {src_root}")
    print(f"Decrypted: {stats.decrypted}")
    print(f"Copied/converted: {stats.copied}")
    print(f"Failed: {len(stats.failed)}")
    print(f"Report: {report_path}")
    if stats.failed:
        print("First failures:")
        for line in stats.failed[:10]:
            print(" ", line)
    return 0 if not stats.failed else 1


if __name__ == "__main__":
    raise SystemExit(main())
