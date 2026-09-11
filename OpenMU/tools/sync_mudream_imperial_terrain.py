"""Decrypt Mudream EncTerrain82.att into OpenMU Terrain70-73.att.

Eldryn dungeon keeps map numbers 69-72 (World70-73), but uses Mudream's
Imperial Guardian / Karutan 2 walkmesh (World82) so client visuals match
after World70-73 assets are overlaid from World82.
"""
import os
import struct

key = bytes([0xD1, 0x73, 0x52, 0xF6, 0xD2, 0x9A, 0xCB, 0x27, 0x3E, 0xAF, 0x59, 0x31, 0x37, 0xB3, 0xE7, 0xA2])
bux = bytes([0xFC, 0xCF, 0xAB])
mud = r"c:\Users\JUNIOR-DEV\Downloads\MuServer-20260803T155926Z-1-001\Mudream.online\Data"
res = r"c:\Users\JUNIOR-DEV\Downloads\MuServer-20260803T155926Z-1-001\OpenMU\src\Persistence\Initialization\Resources"


def decrypt(src: bytes) -> bytearray:
    w = 0x5E
    dst = bytearray(len(src))
    for i, b in enumerate(src):
        dst[i] = ((b ^ key[i % 16]) - w) & 0xFF
        w = (b + 0x3D) & 0xFF
    for i in range(len(dst)):
        dst[i] ^= bux[i % 3]
    return dst


enc_path = os.path.join(mud, "World82", "EncTerrain82.att")
dec = decrypt(open(enc_path, "rb").read())
assert len(dec) == 131076, len(dec)
words = struct.unpack_from("<" + "H" * 65536, dec, 4)
low = bytes(w & 0xFF for w in words)
new = bytes([0, 255, 255]) + low

for world in (70, 71, 72, 73):
    out_path = os.path.join(res, f"Terrain{world}.att")
    old = open(out_path, "rb").read() if os.path.exists(out_path) else b""
    diff = sum(1 for a, b in zip(new, old) if a != b) + abs(len(new) - len(old))
    open(out_path, "wb").write(new)
    print(f"Terrain{world}.att bytes={len(new)} changed~{diff} source=EncTerrain82")
