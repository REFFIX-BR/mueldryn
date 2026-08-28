/**
 * One-shot: node scripts/gen-sample.mjs
 * Generates examples/bloody_soldier_wing.json from presets.
 */
import { writeFileSync, mkdirSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const root = join(__dirname, '..');

const MAX = 512;
const key = (g, i) => `${g}:${i}`;

function glow(base) {
  const byLevel = [];
  for (let level = 0; level <= 15; level++) {
    const t = level / 15;
    byLevel.push({
      level,
      color: {
        r: Math.min(1, base.r + t * 0.1),
        g: Math.min(1, base.g + t * 0.05),
        b: base.b,
      },
      intensity: 0.35 + t * 1.2,
      emissive: 0.1 + t * 0.9,
    });
  }
  return {
    enabled: true,
    baseColor: base,
    baseIntensity: 0.55,
    meshIndex: 0,
    renderFlags: 66,
    byLevel,
  };
}

function fx(partial) {
  return {
    id: partial.id,
    name: partial.name,
    enabled: true,
    mudreamCode: partial.mudreamCode,
    bone: partial.bone,
    boneIndex: partial.boneIndex,
    position: { x: 0, y: 0, z: 0 },
    rotation: { x: 0, y: 0, z: 0 },
    scale: 1,
    color: partial.color,
    intensity: 0.9,
    blend: 'additive',
    size: partial.size,
    particles: partial.particles ?? {
      enabled: false,
      count: 24,
      size: 0.08,
      speed: 0.4,
      lifetime: 1.2,
      spread: 0.25,
      color: partial.color,
    },
  };
}

const wings = { name: 'Wing of Bloody Soldier', group: 12, index: 348 };
const set = {
  helm: { name: 'Bloody Soldier Helm M', group: 7, index: 346 },
  armor: { name: 'Bloody Soldier Armor M', group: 8, index: 346 },
  pants: { name: 'Bloody Soldier Pants M', group: 9, index: 346 },
  gloves: { name: 'Bloody Soldier Gloves M', group: 10, index: 346 },
  boots: { name: 'Bloody Soldier Boots M', group: 11, index: 346 },
  weapon: { name: 'Bloody Soldier Sword', group: 0, index: 388 },
};

const now = new Date().toISOString();
const items = {};
for (const ref of Object.values(set)) {
  items[key(ref.group, ref.index)] = {
    item: ref,
    glow: glow({ r: 0.75, g: 0.08, b: 0.06 }),
    effects: [],
  };
}

const wingGlow = glow({ r: 0.85, g: 0.12, b: 0.05 });
wingGlow.baseIntensity = 0.85;
items[key(12, 348)] = {
  item: wings,
  glow: wingGlow,
  effects: [
    fx({
      id: 'fx_bloody_wing_l',
      name: 'Bloody FX 1',
      mudreamCode: 32380,
      bone: 'wing_l',
      boneIndex: 70,
      size: 1.1,
      color: { r: 0.3, g: 0, b: 0 },
      particles: {
        enabled: true,
        count: 18,
        size: 0.06,
        speed: 0.25,
        lifetime: 1,
        spread: 0.2,
        color: { r: 0.9, g: 0.1, b: 0.05 },
      },
    }),
    fx({
      id: 'fx_bloody_wing_r',
      name: 'Bloody FX 2',
      mudreamCode: 32380,
      bone: 'wing_r',
      boneIndex: 71,
      size: 1.3,
      color: { r: 0.3, g: 0, b: 0 },
      particles: {
        enabled: true,
        count: 18,
        size: 0.06,
        speed: 0.25,
        lifetime: 1,
        spread: 0.2,
        color: { r: 0.9, g: 0.1, b: 0.05 },
      },
    }),
    fx({
      id: 'fx_bloody_ember',
      name: 'Bloody FX 3',
      mudreamCode: 32496,
      bone: 'wing_root',
      boneIndex: 72,
      size: 0.5,
      color: { r: 0.52, g: 0.35, b: 0.09 },
    }),
    fx({
      id: 'fx_bloody_gold',
      name: 'Bloody FX 4',
      mudreamCode: 32002,
      bone: 'wing_root',
      boneIndex: 72,
      size: 0.85,
      color: { r: 0.86, g: 0.6, b: 0.21 },
    }),
  ],
};

const doc = {
  schemaVersion: 1,
  meta: {
    name: 'Bloody Soldier + Wings',
    author: 'mu-effect-glow-editor',
    notes:
      'Seed — Wing of Bloody Soldier 12:348; set 7–11:346; sword 0:388 (MudreamCosmeticCatalog).',
    createdAt: now,
    updatedAt: now,
  },
  loadout: {
    characterClass: 'DarkKnight',
    itemLevel: 15,
    slots: {
      helm: set.helm,
      armor: set.armor,
      pants: set.pants,
      gloves: set.gloves,
      boots: set.boots,
      weapon: set.weapon,
      offhand: null,
      wings,
      cape: null,
    },
  },
  items,
};

const outDir = join(root, 'examples');
mkdirSync(outDir, { recursive: true });
const out = join(outDir, 'bloody_soldier_wing.json');
writeFileSync(out, JSON.stringify(doc, null, 2));
console.log('Wrote', out, 'ItemType wings=', 12 * MAX + 348);
