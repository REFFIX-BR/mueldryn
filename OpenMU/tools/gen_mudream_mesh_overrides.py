"""Corrects mesh render flags that TextureScriptParsing infers from texture filenames.

The parser reads the segment after the last '_' as script flags (R=bright, H=hidden,
S=stream, N=noblend). Mudream's packs use those letters as ordinary name parts, so the
heuristic misfires in two ways:

  * A glow sheet whose name carries no recognised flag stays opaque, and its black
    background paints a solid quad over the model (Magma bow, Death bow, Wraith bow...).
  * An albedo whose name merely ends in _H is treated as a hidden mesh and never drawn.
    The whole Abbadon set renders as nothing because RIV_ABBADON_H is its only albedo.

Filenames are too irregular to match reliably, so classify by content instead: an almost
fully black sheet can only be an additive overlay, anything else is albedo. Emit an
override table for the textures where that disagrees with the parser, applied by the
cosmetic loader to cosmetic models only.

The lightning-plane rule (RAIOS / EFEC / EFFECT+digit) is deliberate and left untouched --
CEffectRenderMesh redraws those, so un-hiding them would double-draw.
"""
import io
import os
import re
import sys
from collections import defaultdict

from PIL import Image, ImageFile

ImageFile.LOAD_TRUNCATED_IMAGES = True

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
DATA = os.path.join(ROOT, r'MuMain\out\build\windows-x86\src\RelWithDebInfo\Data')
CATALOG = os.path.join(ROOT, r'MuMain\src\source\Data\GameData\ItemData\MudreamCosmeticCatalog.Generated.cpp')
OUT = os.path.join(ROOT, r'MuMain\src\source\Data\GameData\ItemData\MudreamMeshOverrides.Generated.cpp')

# A sheet this dark cannot be albedo; it only makes sense as an additive overlay.
DARK_RATIO_MIN = 85.0

KEY = bytes([0xd1, 0x73, 0x52, 0xf6, 0xd2, 0x9a, 0xcb, 0x27,
             0x3e, 0xaf, 0x59, 0x31, 0x37, 0xb3, 0xe7, 0xa2])
TEXNAME = re.compile(rb'[A-Za-z0-9_\-]{3,31}\.(?:jpg|tga|JPG|TGA)')
ENTRY = re.compile(
    r'\{\s*(\d+),\s*(\d+),\s*L"([^"]*)",\s*\d+,\s*\d+,\s*\d+,\s*L"([^"]*)",\s*L"([^"]*)"\s*\}')


def decrypt(buf):
    out = bytearray(len(buf))
    key = 0x5E
    for i, b in enumerate(buf):
        out[i] = ((b ^ KEY[i % 16]) - key) & 0xFF
        key = (b + 0x3D) & 0xFF
    return bytes(out)


def bmd_textures(path):
    raw = open(path, 'rb').read()
    if raw[:3] != b'BMD':
        return []
    body = decrypt(raw[8:]) if raw[3] in (0x0A, 0x0C) else raw[4:]
    seen, out = set(), []
    for m in TEXNAME.finditer(body):
        n = m.group().decode('ascii')
        if n.lower() not in seen:
            seen.add(n.lower())
            out.append(n)
    return out


def parser_flags(texname):
    """Mirror of TextureScriptParsing::parsingTScriptA -> (bright, hidden, lightning)."""
    stem = texname.rsplit('.', 1)[0]
    if '_' in stem:
        flags = stem.rsplit('_', 1)[-1].upper()
        if 0 < len(flags) <= 4 and all(c in 'RHSNW' for c in flags) and any(c in 'RHSN' for c in flags):
            return 'R' in flags, 'H' in flags, False
    up = stem.upper()
    if 'RAIOS' in up or 'EFEC' in up or ('EFFECT' in up and any(c.isdigit() for c in up)):
        return False, True, True
    return False, False, False


def dark_ratio(folder, texname):
    stem = texname.rsplit('.', 1)[0]
    for ext, off in (('.ozj', 24), ('.OZJ', 24), ('.ozt', 4), ('.OZT', 4)):
        p = os.path.join(folder, stem + ext)
        if not os.path.exists(p):
            continue
        try:
            im = Image.open(io.BytesIO(open(p, 'rb').read()[off:])).convert('RGB')
        except Exception:
            return None
        im.thumbnail((96, 96))
        px = list(im.getdata())
        dark = sum(1 for r, g, b in px if r < 24 and g < 24 and b < 24)
        return 100.0 * dark / len(px)
    return None


def main():
    src = open(CATALOG, encoding='utf-8', errors='ignore').read()
    entries = ENTRY.findall(src)
    print(f'itens no catalogo cosmetico: {len(entries)}')

    overrides = {}          # stem -> (bright, visible)
    users = defaultdict(set)
    reason = {}
    checked = set()

    for group, number, name, mdir, mfile in entries:
        mdir = mdir.replace('\\\\', '\\')
        folder = os.path.join(DATA, mdir[5:] if mdir.lower().startswith('data\\') else mdir)
        bmd = os.path.join(folder, mfile + '.bmd')
        if not os.path.exists(bmd):
            continue
        for tex in bmd_textures(bmd):
            stem = tex.rsplit('.', 1)[0].upper()
            bright, hidden, lightning = parser_flags(tex)
            if lightning:
                continue  # deliberate; CEffectRenderMesh owns these
            key = (folder.lower(), stem)
            if key not in checked:
                checked.add(key)
                r = dark_ratio(folder, tex)
                if r is None:
                    continue
                want_bright = r >= DARK_RATIO_MIN
                # An explicit _R suffix is the artist stating the mesh is a glow map, so
                # never second-guess it -- only the two failure modes below get corrected.
                if hidden:
                    # Nothing in these packs actually wants a hidden mesh: _H is part of
                    # the name. Un-hide, and make it additive only if it reads as a glow.
                    overrides[stem] = (want_bright, True)
                    reason[stem] = (f'{"glow" if want_bright else "albedo"} escondida '
                                    f'por _H ({r:.0f}% preto)')
                elif not bright and want_bright:
                    overrides[stem] = (True, False)
                    reason[stem] = f'glow desenhada opaca ({r:.0f}% preto)'
            if stem in overrides:
                users[stem].add(name)

    names = sorted(overrides)
    print(f'texturas com flags incorretas: {len(names)}\n')
    for n in names:
        bright, _ = overrides[n]
        kind = 'aditiva' if bright else 'visivel'
        print(f'  {n:34s} -> {kind:<9} {reason[n]:40s} {len(users[n])} itens')

    lines = [
        '// <auto-generated by OpenMU/tools/gen_mudream_mesh_overrides.py>',
        '// Render flags that TextureScriptParsing infers wrongly from these packs\' texture',
        '// names: glow sheets left opaque (black quads over the model) and albedos whose name',
        '// ends in _H, which the parser hides so the item renders as nothing.',
        '// Classified by texture content; applied to cosmetic models only.',
        '#include "stdafx.h"',
        '#include "MudreamCosmeticLoader.h"',
        '',
        'namespace MudreamCosmetics',
        '{',
        'const MeshTextureOverride kMeshTextureOverrides[] = {',
    ]
    for n in names:
        bright, visible = overrides[n]
        lines.append(f'    {{ "{n}", {"true" if bright else "false"}, '
                     f'{"true" if visible else "false"} }},')
    lines += [
        '};',
        'const int kMeshTextureOverrideCount = '
        'sizeof(kMeshTextureOverrides) / sizeof(kMeshTextureOverrides[0]);',
        '}',
        '',
    ]
    with open(OUT, 'w', encoding='utf-8', newline='\n') as f:
        f.write('\n'.join(lines))
    print(f'\nGerado {len(names)} entradas -> {OUT}')
    return 0


if __name__ == '__main__':
    sys.exit(main())
