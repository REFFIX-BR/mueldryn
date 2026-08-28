import type { ItemEffectsDocument, ItemRef, LoadoutState } from '@/schema/itemEffects';
import {
  createDefaultGlow,
  createEffectEntry,
  emptyLoadout,
  itemKey,
} from '@/schema/itemEffects';

/** Wing of Bloody Soldier — ItemType 12*512+348 = 6492 (Mudream Tooltip / catalog). */
export const BLOODY_WINGS: ItemRef = {
  name: 'Wing of Bloody Soldier',
  group: 12,
  index: 348,
};

/** Bloody Soldier set — male pieces 7–11:346 + sword 0:388 (MudreamCosmeticCatalog). */
export const BLOODY_SOLDIER_SET: Record<
  'helm' | 'armor' | 'pants' | 'gloves' | 'boots' | 'weapon',
  ItemRef
> = {
  helm: { name: 'Bloody Soldier Helm M', group: 7, index: 346 },
  armor: { name: 'Bloody Soldier Armor M', group: 8, index: 346 },
  pants: { name: 'Bloody Soldier Pants M', group: 9, index: 346 },
  gloves: { name: 'Bloody Soldier Gloves M', group: 10, index: 346 },
  boots: { name: 'Bloody Soldier Boots M', group: 11, index: 346 },
  weapon: { name: 'Bloody Soldier Sword', group: 0, index: 388 },
};

/** Hellfire set — catalog indices 7–11:350, wing 12:352, sword 0:397. */
export const HELLFIRE_SET: Record<
  'helm' | 'armor' | 'pants' | 'gloves' | 'boots' | 'weapon' | 'wings',
  ItemRef
> = {
  helm: { name: 'Hellfire Helm M', group: 7, index: 350 },
  armor: { name: 'Hellfire Armor M', group: 8, index: 350 },
  pants: { name: 'Hellfire Pants M', group: 9, index: 350 },
  gloves: { name: 'Hellfire Gloves M', group: 10, index: 350 },
  boots: { name: 'Hellfire Boots M', group: 11, index: 350 },
  weapon: { name: 'Hellfire Sword', group: 0, index: 397 },
  wings: { name: 'Wing of Hellfire', group: 12, index: 352 },
};

export type SetPresetId = 'bloodySoldier' | 'hellfire' | 'empty';

export interface SetPreset {
  id: SetPresetId;
  label: string;
  description: string;
  apply: () => { loadout: LoadoutState; items: ItemEffectsDocument['items'] };
}

function wingEffectsFromMudream(): ReturnType<typeof createEffectEntry>[] {
  // Subset inspired by CustomEffect(Static) Bloody Wings (6355 / codes 32380, 32496, 32002)
  // Author sizes stay modest — EffectSprites caps visual size for soft accents (not chest quads).
  const bones = [
    { bone: 'wing_l', boneIndex: 70, code: 32380, size: 0.55 },
    { bone: 'wing_r', boneIndex: 71, code: 32380, size: 0.6 },
    { bone: 'wing_root', boneIndex: 72, code: 32496, size: 0.35, color: { r: 0.52, g: 0.35, b: 0.09 } },
    { bone: 'wing_root', boneIndex: 72, code: 32002, size: 0.45, color: { r: 0.86, g: 0.6, b: 0.21 } },
  ];
  return bones.map((b, i) =>
    createEffectEntry({
      name: `Bloody FX ${i + 1}`,
      mudreamCode: b.code,
      bone: b.bone,
      boneIndex: b.boneIndex,
      size: b.size,
      color: b.color ?? { r: 0.55, g: 0.05, b: 0.02 },
      intensity: 0.65,
      blend: 'additive',
      particles: {
        enabled: i < 2,
        count: 14,
        size: 0.04,
        speed: 0.2,
        lifetime: 0.9,
        spread: 0.15,
        color: b.color ?? { r: 0.9, g: 0.1, b: 0.05 },
      },
    }),
  );
}

export function buildBloodySoldierDocument(): ItemEffectsDocument {
  const now = new Date().toISOString();
  const loadout = emptyLoadout('DarkKnight');
  loadout.itemLevel = 15;
  loadout.slots = {
    ...loadout.slots,
    helm: BLOODY_SOLDIER_SET.helm,
    armor: BLOODY_SOLDIER_SET.armor,
    pants: BLOODY_SOLDIER_SET.pants,
    gloves: BLOODY_SOLDIER_SET.gloves,
    boots: BLOODY_SOLDIER_SET.boots,
    weapon: BLOODY_SOLDIER_SET.weapon,
    wings: BLOODY_WINGS,
  };

  const items: ItemEffectsDocument['items'] = {};
  for (const ref of Object.values(BLOODY_SOLDIER_SET)) {
    const glow = createDefaultGlow();
    glow.baseColor = { r: 0.75, g: 0.08, b: 0.06 };
    glow.baseIntensity = 0.55;
    items[itemKey(ref)] = { item: ref, glow, effects: [] };
  }

  const wingGlow = createDefaultGlow();
  wingGlow.baseColor = { r: 0.85, g: 0.12, b: 0.05 };
  wingGlow.baseIntensity = 0.85;
  items[itemKey(BLOODY_WINGS)] = {
    item: BLOODY_WINGS,
    glow: wingGlow,
    effects: wingEffectsFromMudream(),
  };

  return {
    schemaVersion: 1,
    meta: {
      name: 'Bloody Soldier + Wings',
      author: 'mu-effect-glow-editor',
      notes: 'Seed preset — IDs from MudreamCosmeticCatalog (set 7–11:346, wing 12:348, sword 0:388).',
      createdAt: now,
      updatedAt: now,
    },
    loadout,
    items,
  };
}

export function buildHellfireDocument(): ItemEffectsDocument {
  const now = new Date().toISOString();
  const loadout = emptyLoadout('DarkKnight');
  loadout.itemLevel = 13;
  loadout.slots = {
    ...loadout.slots,
    helm: HELLFIRE_SET.helm,
    armor: HELLFIRE_SET.armor,
    pants: HELLFIRE_SET.pants,
    gloves: HELLFIRE_SET.gloves,
    boots: HELLFIRE_SET.boots,
    weapon: HELLFIRE_SET.weapon,
    wings: HELLFIRE_SET.wings,
  };

  const items: ItemEffectsDocument['items'] = {};
  for (const ref of Object.values(HELLFIRE_SET)) {
    const glow = createDefaultGlow();
    glow.baseColor = { r: 1, g: 0.45, b: 0.08 };
    glow.baseIntensity = 0.7;
    for (const lv of glow.byLevel) {
      lv.color = { r: 1, g: 0.35 + lv.level * 0.03, b: 0.05 };
    }
    const effects =
      ref.group === 12
        ? [
            createEffectEntry({
              name: 'Hellfire Ember',
              mudreamCode: 32151,
              bone: 'wing_root',
              boneIndex: 72,
              color: { r: 1, g: 0.5, b: 0.1 },
              size: 0.55,
              particles: {
                enabled: true,
                count: 32,
                size: 0.07,
                speed: 0.5,
                lifetime: 1.4,
                spread: 0.35,
                color: { r: 1, g: 0.4, b: 0.05 },
              },
            }),
          ]
        : ref.group === 0
          ? [
              createEffectEntry({
                name: 'Blade Fire',
                mudreamCode: 32049,
                bone: 'weapon',
                boneIndex: 60,
                color: { r: 1, g: 0.35, b: 0 },
                size: 0.4,
              }),
            ]
          : [];
    items[itemKey(ref)] = { item: ref, glow, effects };
  }

  return {
    schemaVersion: 1,
    meta: {
      name: 'Hellfire Set',
      author: 'mu-effect-glow-editor',
      notes: 'Warm orange preset for glow/effect authoring.',
      createdAt: now,
      updatedAt: now,
    },
    loadout,
    items,
  };
}

export const SET_PRESETS: SetPreset[] = [
  {
    id: 'bloodySoldier',
    label: 'Bloody Soldier',
    description: 'Set 7–11:346 + asa 12:348',
    apply: () => {
      const doc = buildBloodySoldierDocument();
      return { loadout: doc.loadout, items: doc.items };
    },
  },
  {
    id: 'hellfire',
    label: 'Hellfire',
    description: 'Set laranja / embers',
    apply: () => {
      const doc = buildHellfireDocument();
      return { loadout: doc.loadout, items: doc.items };
    },
  },
  {
    id: 'empty',
    label: 'Custom (vazio)',
    description: 'Slots vazios para montar loadout',
    apply: () => ({ loadout: emptyLoadout('DarkKnight'), items: {} }),
  },
];
