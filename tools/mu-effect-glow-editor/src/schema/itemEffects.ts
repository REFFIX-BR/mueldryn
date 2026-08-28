import { z } from 'zod';

/** MU ItemType = Group * MAX_ITEM_INDEX + Number (MAX_ITEM_INDEX = 512). */
export const MAX_ITEM_INDEX = 512;

export type Vec3 = { x: number; y: number; z: number };
export type Rgb = { r: number; g: number; b: number };

export type BlendMode = 'normal' | 'additive' | 'multiply' | 'screen' | 'bright';

export type CharacterClass =
  | 'DarkKnight'
  | 'DarkWizard'
  | 'Elf'
  | 'MagicGladiator'
  | 'DarkLord'
  | 'Summoner'
  | 'RageFighter'
  | 'GrowLancer';

export type EquipmentSlot =
  | 'helm'
  | 'armor'
  | 'pants'
  | 'gloves'
  | 'boots'
  | 'weapon'
  | 'offhand'
  | 'wings'
  | 'cape';

export type CameraPreset = 'fullBody' | 'upper' | 'weapon' | 'wings';

export type AnimClip =
  | 'idle'
  | 'walk'
  | 'run'
  | 'attack'
  | 'skill'
  | 'sit'
  | 'die';

export interface ItemRef {
  /** Display name */
  name: string;
  /** MU group (e.g. 7=helm, 8=armor, 12=wings) */
  group: number;
  /** Index within group */
  index: number;
}

export interface GlowLevelEntry {
  level: number; // 0..15
  color: Rgb;
  intensity: number;
  emissive: number;
}

export interface GlowConfig {
  enabled: boolean;
  baseColor: Rgb;
  baseIntensity: number;
  meshIndex: number;
  renderFlags: number;
  byLevel: GlowLevelEntry[];
}

export interface ParticleConfig {
  enabled: boolean;
  count: number;
  size: number;
  speed: number;
  lifetime: number;
  spread: number;
  color: Rgb;
}

export interface EffectEntry {
  id: string;
  name: string;
  enabled: boolean;
  /** Mudream effect / bitmap code (e.g. 32380) */
  mudreamCode: number;
  /** Attach bone name or MU bone index */
  bone: string;
  boneIndex: number;
  position: Vec3;
  rotation: Vec3;
  scale: number;
  color: Rgb;
  intensity: number;
  blend: BlendMode;
  size: number;
  particles: ParticleConfig;
}

export interface ItemEffectConfig {
  item: ItemRef;
  glow: GlowConfig;
  effects: EffectEntry[];
}

export interface LoadoutState {
  characterClass: CharacterClass;
  /** Enhancement level for glow-by-level preview (+0..+15) */
  itemLevel: number;
  slots: Record<EquipmentSlot, ItemRef | null>;
}

export interface ItemEffectsDocument {
  schemaVersion: 1;
  meta: {
    name: string;
    author?: string;
    notes?: string;
    createdAt: string;
    updatedAt: string;
  };
  loadout: LoadoutState;
  /** Keyed by `${group}:${index}` */
  items: Record<string, ItemEffectConfig>;
}

export const vec3Schema = z.object({
  x: z.number(),
  y: z.number(),
  z: z.number(),
});

export const rgbSchema = z.object({
  r: z.number().min(0).max(1),
  g: z.number().min(0).max(1),
  b: z.number().min(0).max(1),
});

export const itemRefSchema = z.object({
  name: z.string(),
  group: z.number().int().nonnegative(),
  index: z.number().int().nonnegative(),
});

export const particleSchema = z.object({
  enabled: z.boolean(),
  count: z.number().int().nonnegative(),
  size: z.number().nonnegative(),
  speed: z.number().nonnegative(),
  lifetime: z.number().positive(),
  spread: z.number().nonnegative(),
  color: rgbSchema,
});

export const effectEntrySchema = z.object({
  id: z.string(),
  name: z.string(),
  enabled: z.boolean(),
  mudreamCode: z.number().int(),
  bone: z.string(),
  boneIndex: z.number().int(),
  position: vec3Schema,
  rotation: vec3Schema,
  scale: z.number().positive(),
  color: rgbSchema,
  intensity: z.number().nonnegative(),
  blend: z.enum(['normal', 'additive', 'multiply', 'screen', 'bright']),
  size: z.number().nonnegative(),
  particles: particleSchema,
});

export const glowLevelSchema = z.object({
  level: z.number().int().min(0).max(15),
  color: rgbSchema,
  intensity: z.number().nonnegative(),
  emissive: z.number().nonnegative(),
});

export const glowSchema = z.object({
  enabled: z.boolean(),
  baseColor: rgbSchema,
  baseIntensity: z.number().nonnegative(),
  meshIndex: z.number().int(),
  renderFlags: z.number().int(),
  byLevel: z.array(glowLevelSchema),
});

export const itemEffectConfigSchema = z.object({
  item: itemRefSchema,
  glow: glowSchema,
  effects: z.array(effectEntrySchema),
});

export const loadoutSchema = z.object({
  characterClass: z.enum([
    'DarkKnight',
    'DarkWizard',
    'Elf',
    'MagicGladiator',
    'DarkLord',
    'Summoner',
    'RageFighter',
    'GrowLancer',
  ]),
  itemLevel: z.number().int().min(0).max(15),
  slots: z.object({
    helm: itemRefSchema.nullable(),
    armor: itemRefSchema.nullable(),
    pants: itemRefSchema.nullable(),
    gloves: itemRefSchema.nullable(),
    boots: itemRefSchema.nullable(),
    weapon: itemRefSchema.nullable(),
    offhand: itemRefSchema.nullable(),
    wings: itemRefSchema.nullable(),
    cape: itemRefSchema.nullable(),
  }),
});

export const itemEffectsDocumentSchema = z.object({
  schemaVersion: z.literal(1),
  meta: z.object({
    name: z.string(),
    author: z.string().optional(),
    notes: z.string().optional(),
    createdAt: z.string(),
    updatedAt: z.string(),
  }),
  loadout: loadoutSchema,
  items: z.record(itemEffectConfigSchema),
});

export function itemKey(ref: Pick<ItemRef, 'group' | 'index'>): string {
  return `${ref.group}:${ref.index}`;
}

export function itemTypeId(ref: Pick<ItemRef, 'group' | 'index'>): number {
  return ref.group * MAX_ITEM_INDEX + ref.index;
}

export function parseItemType(itemType: number): { group: number; index: number } {
  return {
    group: Math.floor(itemType / MAX_ITEM_INDEX),
    index: itemType % MAX_ITEM_INDEX,
  };
}

export function createDefaultGlow(): GlowConfig {
  const byLevel: GlowLevelEntry[] = [];
  for (let level = 0; level <= 15; level++) {
    const t = level / 15;
    byLevel.push({
      level,
      color: { r: 0.85 + t * 0.15, g: 0.2 + t * 0.1, b: 0.05 },
      intensity: 0.25 + t * 0.55,
      // Kept low: preview applies a soft emissive accent only (textures stay readable).
      emissive: 0.05 + t * 0.35,
    });
  }
  return {
    enabled: true,
    baseColor: { r: 0.9, g: 0.25, b: 0.08 },
    baseIntensity: 0.45,
    meshIndex: 0,
    renderFlags: 66,
    byLevel,
  };
}

export function createDefaultParticles(color?: Rgb): ParticleConfig {
  return {
    enabled: false,
    count: 24,
    size: 0.08,
    speed: 0.4,
    lifetime: 1.2,
    spread: 0.25,
    color: color ?? { r: 1, g: 0.4, b: 0.1 },
  };
}

export function createEffectEntry(partial?: Partial<EffectEntry>): EffectEntry {
  const id =
    partial?.id ??
    `fx_${Date.now().toString(36)}_${Math.random().toString(36).slice(2, 7)}`;
  return {
    id,
    name: partial?.name ?? 'New Effect',
    enabled: partial?.enabled ?? true,
    mudreamCode: partial?.mudreamCode ?? 32380,
    bone: partial?.bone ?? 'spine',
    boneIndex: partial?.boneIndex ?? 20,
    position: partial?.position ?? { x: 0, y: 0, z: 0 },
    rotation: partial?.rotation ?? { x: 0, y: 0, z: 0 },
    scale: partial?.scale ?? 1,
    color: partial?.color ?? { r: 0.85, g: 0.15, b: 0.05 },
    intensity: partial?.intensity ?? 1,
    blend: partial?.blend ?? 'additive',
    size: partial?.size ?? 0.35,
    particles: partial?.particles ?? createDefaultParticles(partial?.color),
  };
}

export function createItemConfig(item: ItemRef): ItemEffectConfig {
  return {
    item,
    glow: createDefaultGlow(),
    effects: [],
  };
}

export function emptyLoadout(characterClass: CharacterClass = 'DarkKnight'): LoadoutState {
  return {
    characterClass,
    itemLevel: 15,
    slots: {
      helm: null,
      armor: null,
      pants: null,
      gloves: null,
      boots: null,
      weapon: null,
      offhand: null,
      wings: null,
      cape: null,
    },
  };
}
