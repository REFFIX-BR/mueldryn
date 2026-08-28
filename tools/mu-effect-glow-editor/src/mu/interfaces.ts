import type { EffectEntry, GlowConfig, LoadoutState, Rgb } from '@/schema/itemEffects';

/**
 * Clean interfaces for a future MuMain-faithful renderer backend.
 * Current viewport implements these with Three.js placeholders.
 */

export interface BoneTransform {
  name: string;
  index: number;
  position: [number, number, number];
  quaternion: [number, number, number, number];
}

export interface ICharacterRenderer {
  setLoadout(loadout: LoadoutState): void;
  setAnimation(clip: string, speed: number, playing: boolean): void;
  getBone(nameOrIndex: string | number): BoneTransform | null;
  dispose(): void;
}

export interface EffectInstance {
  entry: EffectEntry;
  /** Resolved world position for approx preview */
  worldPosition: [number, number, number];
}

export interface IEffectRenderer {
  setEffects(effects: EffectInstance[]): void;
  setGlow(slotKey: string, glow: GlowConfig, level: number): void;
  setCompareMode(enabled: boolean): void;
  dispose(): void;
}

export function rgbToHex(c: Rgb): string {
  const to = (v: number) =>
    Math.round(Math.min(1, Math.max(0, v)) * 255)
      .toString(16)
      .padStart(2, '0');
  return `#${to(c.r)}${to(c.g)}${to(c.b)}`;
}

export function hexToRgb(hex: string): Rgb {
  const h = hex.replace('#', '');
  const n = parseInt(h.length === 3 ? h.split('').map((c) => c + c).join('') : h, 16);
  return {
    r: ((n >> 16) & 255) / 255,
    g: ((n >> 8) & 255) / 255,
    b: (n & 255) / 255,
  };
}
