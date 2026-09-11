import struct
import os

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


for world in (70, 71, 72, 73):
    enc_path = os.path.join(mud, f"World{world}", f"EncTerrain{world}.att")
    out_path = os.path.join(res, f"Terrain{world}.att")
    dec = decrypt(open(enc_path, "rb").read())
    assert len(dec) == 131076, (world, len(dec))
    words = struct.unpack_from("<" + "H" * 65536, dec, 4)
    low = bytes(w & 0xFF for w in words)
    new = bytes([0, 255, 255]) + low
    old = open(out_path, "rb").read() if os.path.exists(out_path) else b""
    diff = sum(1 for a, b in zip(new, old) if a != b) + abs(len(new) - len(old))
    open(out_path, "wb").write(new)
    print(f"Terrain{world}.att bytes={len(new)} changed~{diff} mudMapId={dec[1]}")
