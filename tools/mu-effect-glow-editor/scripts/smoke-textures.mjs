/**
 * Offline smoke: parse Bloody BMDs + resolve OZJ/OZT beside them.
 * Run: node scripts/smoke-textures.mjs
 */
import { readFileSync, readdirSync, existsSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const XOR_KEY = new Uint8Array([
  0xd1, 0x73, 0x52, 0xf6, 0xd2, 0x9a, 0xcb, 0x27, 0x3e, 0xaf, 0x59, 0x31, 0x37, 0xb3, 0xe7, 0xa2,
]);

function mapFileDecrypt(source) {
  const dst = new Uint8Array(source.length);
  let mapKey = 0x5e;
  for (let i = 0; i < source.length; i++) {
    dst[i] = ((source[i] ^ XOR_KEY[i % 16]) - mapKey) & 0xff;
    mapKey = (source[i] + 0x3d) & 0xff;
  }
  return dst;
}

function isGlow(name) {
  return /_r$/i.test(name.replace(/\.[^.]+$/, ''));
}

function searchNames(textureName) {
  const stem = textureName.replace(/^.*[\\/]/, '').replace(/\.[^.]+$/, '');
  const glow = isGlow(textureName);
  const alpha = [`${stem}.ozt`, `${stem}.OZT`, `${stem}.ozj`, `${stem}.OZJ`, `${stem}.jpg`];
  const jpeg = [`${stem}.ozj`, `${stem}.OZJ`, `${stem}.jpg`, `${stem}.ozt`];
  return glow ? jpeg : alpha;
}

function parseTextures(fileData) {
  const version = fileData[3];
  let data;
  if (version === 0x0c) {
    const encSize = new DataView(fileData.buffer, fileData.byteOffset).getInt32(4, true);
    data = mapFileDecrypt(fileData.subarray(8, 8 + encSize));
  } else {
    data = fileData.subarray(4);
  }
  const view = new DataView(data.buffer, data.byteOffset, data.byteLength);
  let ptr = 32;
  const numMeshs = view.getInt16(ptr, true);
  ptr += 2;
  ptr += 4; // bones + actions
  const out = [];
  for (let i = 0; i < numMeshs; i++) {
    const nv = view.getInt16(ptr, true);
    ptr += 2;
    const nn = view.getInt16(ptr, true);
    ptr += 2;
    const nt = view.getInt16(ptr, true);
    ptr += 2;
    const ntri = view.getInt16(ptr, true);
    ptr += 2;
    ptr += 2;
    ptr += nv * 16 + nn * 20 + nt * 8 + ntri * 64;
    const slice = data.subarray(ptr, ptr + 32);
    ptr += 32;
    let end = slice.indexOf(0);
    if (end < 0) end = 32;
    out.push(new TextDecoder().decode(slice.subarray(0, end)).trim());
  }
  return out;
}

const dir = join(
  __dirname,
  '..',
  '..',
  '..',
  'MuMain',
  'out',
  'build',
  'windows-x86',
  'src',
  'RelWithDebInfo',
  'Data',
  'Item',
  'CustomItem',
  'Skin',
  'bloodysoldier',
);
if (!existsSync(dir)) {
  console.error('Data folder missing', dir);
  process.exit(1);
}
const listing = readdirSync(dir);
const files = [
  'Bloody_soldier_helm_bk.bmd',
  'Bloody_soldier_armor_m.bmd',
  'Bloody_soldier_pants_m.bmd',
  'Bloody_soldier_gloves_m.bmd',
  'Bloody_soldier_boots_m.bmd',
  'Bloody_soldier_sword.bmd',
  'Bloody_soldier_wing.bmd',
];
let fail = 0;
for (const f of files) {
  const tex = parseTextures(new Uint8Array(readFileSync(join(dir, f))));
  console.log(f);
  for (const t of tex) {
    const glow = isGlow(t);
    let hit = null;
    for (const name of searchNames(t)) {
      if (listing.some((x) => x.toLowerCase() === name.toLowerCase())) {
        hit = name;
        break;
      }
    }
    if (!hit) fail++;
    console.log(`  ${glow ? 'GLOW' : 'DIFF'} ${t} → ${hit ?? 'MISSING'}`);
  }
}
console.log(fail ? `FAIL missing=${fail}` : 'OK all textures resolve');
process.exit(fail ? 1 : 0);
