import type { EquipmentSlot, ItemRef } from '@/schema/itemEffects';

/**
 * Relative paths under Data/ (no leading Data/).
 * Sourced from MudreamCosmeticCatalog.Generated.cpp.
 */
export interface CatalogEntry {
  group: number;
  index: number;
  name: string;
  /** Folder relative to Data, e.g. Item/CustomItem/Skin/bloodysoldier */
  dir: string;
  /** BMD file stem without .bmd */
  file: string;
}

function normDir(dir: string): string {
  return dir
    .replace(/^Data[\\/]/i, '')
    .replace(/\\/g, '/')
    .replace(/\/+$/, '');
}

/** Compact catalog for cosmetics used by editor presets (+ fallbacks). */
export const COSMETIC_CATALOG: CatalogEntry[] = [
  // Bloody Soldier
  { group: 0, index: 388, name: 'Bloody Soldier Sword', dir: 'Item/CustomItem/Skin/bloodysoldier', file: 'Bloody_soldier_sword' },
  { group: 0, index: 389, name: 'Bloody Soldier Two-Hand Blade', dir: 'Item/CustomItem/Skin/bloodysoldier', file: 'Bloody_soldier_blade' },
  { group: 7, index: 346, name: 'Bloody Soldier Helm M', dir: 'Item/CustomItem/Skin/bloodysoldier', file: 'Bloody_soldier_helm_bk' },
  { group: 7, index: 347, name: 'Bloody Soldier Helm F', dir: 'Item/CustomItem/Skin/bloodysoldier', file: 'Bloody_soldier_helm_bk' },
  { group: 8, index: 346, name: 'Bloody Soldier Armor M', dir: 'Item/CustomItem/Skin/bloodysoldier', file: 'Bloody_soldier_armor_m' },
  { group: 8, index: 347, name: 'Bloody Soldier Armor F', dir: 'Item/CustomItem/Skin/bloodysoldier', file: 'Bloody_soldier_armor_f' },
  { group: 9, index: 346, name: 'Bloody Soldier Pants M', dir: 'Item/CustomItem/Skin/bloodysoldier', file: 'Bloody_soldier_pants_m' },
  { group: 9, index: 347, name: 'Bloody Soldier Pants F', dir: 'Item/CustomItem/Skin/bloodysoldier', file: 'Bloody_soldier_pants_f' },
  { group: 10, index: 346, name: 'Bloody Soldier Gloves M', dir: 'Item/CustomItem/Skin/bloodysoldier', file: 'Bloody_soldier_gloves_m' },
  { group: 10, index: 347, name: 'Bloody Soldier Gloves F', dir: 'Item/CustomItem/Skin/bloodysoldier', file: 'Bloody_soldier_gloves_f' },
  { group: 11, index: 346, name: 'Bloody Soldier Boots M', dir: 'Item/CustomItem/Skin/bloodysoldier', file: 'Bloody_soldier_boots_m' },
  { group: 11, index: 347, name: 'Bloody Soldier Boots F', dir: 'Item/CustomItem/Skin/bloodysoldier', file: 'Bloody_soldier_boots_f' },
  { group: 12, index: 347, name: 'Cloak of Bloody Soldier', dir: 'Item/CustomItem/Skin/bloodysoldier', file: 'Bloody_soldier_cape' },
  { group: 12, index: 348, name: 'Wing of Bloody Soldier', dir: 'Item/CustomItem/Skin/bloodysoldier', file: 'Bloody_soldier_wing' },
  { group: 6, index: 323, name: 'Bloody Soldier Shield', dir: 'Item/CustomItem/Skin/bloodysoldier', file: 'Bloody_soldier_shield' },
  // Hellfire
  { group: 0, index: 397, name: 'Hellfire Sword', dir: 'Item/CustomItem/Skin/Hellfire', file: 'Hellfire_sword' },
  { group: 7, index: 350, name: 'Hellfire Helm M', dir: 'Item/CustomItem/Skin/Hellfire', file: 'Hellfire_helm_Inventory_m' },
  { group: 7, index: 351, name: 'Hellfire Helm F', dir: 'Item/CustomItem/Skin/Hellfire', file: 'Hellfire_helm_Inventory_f' },
  { group: 8, index: 350, name: 'Hellfire Armor M', dir: 'Item/CustomItem/Skin/Hellfire', file: 'Hellfire_armor_m' },
  { group: 8, index: 351, name: 'Hellfire Armor F', dir: 'Item/CustomItem/Skin/Hellfire', file: 'Hellfire_armor_f' },
  { group: 9, index: 350, name: 'Hellfire Pants M', dir: 'Item/CustomItem/Skin/Hellfire', file: 'Hellfire_pants_m' },
  { group: 9, index: 351, name: 'Hellfire Pants F', dir: 'Item/CustomItem/Skin/Hellfire', file: 'Hellfire_pants_f' },
  { group: 10, index: 350, name: 'Hellfire Gloves M', dir: 'Item/CustomItem/Skin/Hellfire', file: 'Hellfire_gloves_m' },
  { group: 10, index: 351, name: 'Hellfire Gloves F', dir: 'Item/CustomItem/Skin/Hellfire', file: 'Hellfire_gloves_f' },
  { group: 11, index: 350, name: 'Hellfire Boots M', dir: 'Item/CustomItem/Skin/Hellfire', file: 'Hellfire_boots_m' },
  { group: 11, index: 351, name: 'Hellfire Boots F', dir: 'Item/CustomItem/Skin/Hellfire', file: 'Hellfire_boots_f' },
  { group: 12, index: 351, name: 'Cloak of Hellfire', dir: 'Item/CustomItem/Skin/Hellfire', file: 'Hellfire_cape' },
  { group: 12, index: 352, name: 'Wing of Hellfire', dir: 'Item/CustomItem/Skin/Hellfire', file: 'Hellfire_wing' },
].map((e) => ({ ...e, dir: normDir(e.dir) }));

const byKey = new Map(COSMETIC_CATALOG.map((e) => [`${e.group}:${e.index}`, e]));

const ARMOR_PREFIX: Record<number, string> = {
  7: 'HelmMale',
  8: 'ArmorMale',
  9: 'PantMale',
  10: 'GloveMale',
  11: 'BootMale',
};

const WEAPON_PREFIX: Record<number, string> = {
  0: 'Sword',
  1: 'Axe',
  2: 'Mace',
  3: 'Spear',
  4: 'Bow',
  5: 'Staff',
  6: 'Shield',
};

export function catalogEntryFor(group: number, index: number): CatalogEntry | null {
  return byKey.get(`${group}:${index}`) ?? null;
}

export interface ResolvedBmdPath {
  /** Relative to Data root, forward slashes */
  relativePath: string;
  label: string;
  source: 'catalog' | 'heuristic';
}

/**
 * Map loadout item → BMD path candidates (first existing wins at load time).
 */
export function resolveBmdCandidates(ref: ItemRef): ResolvedBmdPath[] {
  const out: ResolvedBmdPath[] = [];
  const cat = catalogEntryFor(ref.group, ref.index);
  if (cat) {
    out.push({
      relativePath: `${cat.dir}/${cat.file}.bmd`,
      label: cat.name,
      source: 'catalog',
    });
    // case variants for Skin folder
    const altDir = cat.dir.replace(/bloodysoldier/i, (m) =>
      m[0] === m[0].toUpperCase() ? 'bloodysoldier' : 'Bloodysoldier',
    );
    if (altDir !== cat.dir) {
      out.push({
        relativePath: `${altDir}/${cat.file}.bmd`,
        label: cat.name,
        source: 'catalog',
      });
    }
  }

  const fileNum = ref.index + 1;
  const padded = fileNum < 10 ? `0${fileNum}` : String(fileNum);

  if (ARMOR_PREFIX[ref.group]) {
    out.push({
      relativePath: `Player/${ARMOR_PREFIX[ref.group]}${padded}.bmd`,
      label: `${ARMOR_PREFIX[ref.group]}${padded}`,
      source: 'heuristic',
    });
  }
  if (WEAPON_PREFIX[ref.group]) {
    out.push({
      relativePath: `Item/${WEAPON_PREFIX[ref.group]}${padded}.bmd`,
      label: `${WEAPON_PREFIX[ref.group]}${padded}`,
      source: 'heuristic',
    });
  }
  if (ref.group === 12) {
    out.push({
      relativePath: `Item/Wing${padded}.bmd`,
      label: `Wing${padded}`,
      source: 'heuristic',
    });
  }

  return out;
}

export function slotGroupDefault(slot: EquipmentSlot): number {
  const map: Record<EquipmentSlot, number> = {
    helm: 7,
    armor: 8,
    pants: 9,
    gloves: 10,
    boots: 11,
    weapon: 0,
    offhand: 6,
    wings: 12,
    cape: 12,
  };
  return map[slot];
}
