/**
 * BMD / texture loaders + fidelity notes for the preview renderer.
 */

export const FIDELITY_WARNING =
  'Texturas BMD + attach approx — FX/glow ainda não são 100% idênticos ao Main.exe / Live Client.';

export const FIDELITY_WARNING_PLACEHOLDER =
  'Sem pasta Data: manequim placeholder. Ligue Data (HTTP /mu-data ou “Data folder…”) para meshes texturizados.';

export const FIDELITY_NOTES = [
  'Com pasta Data, o viewport carrega BMD + OZJ/OZT (diffuses) e meshes *_R em additive (como Bright do client).',
  'Arma/asa usam attach aproximado nos bones do preview — pose/animação BMD completa ainda é stub.',
  'Sprites Mudream são soft accents Three.js — fidelidade 100% de FX exige Live Client / Main.exe.',
  'item_effects.json é o formato externo; um reader futuro no client aplicará sem recompilar Main.',
  'Valide cosméticos finais no Main.exe.',
] as const;

export type BmdLoadOk = {
  ok: true;
  meshCount: number;
  boneCount: number;
  path: string;
};

export type BmdLoadFail = {
  ok: false;
  reason: string;
};

export type BmdLoadResult = BmdLoadOk | BmdLoadFail;

export { parseBmd, computeRestBoneMatrices } from './bmdParser';
export { loadItemBmd, bmdBytesToGroup } from './bmdMeshFactory';
export { mapFileDecrypt } from './mapFileDecrypt';
export {
  initDataRoot,
  pickDataFolder,
  getDataRootState,
  subscribeDataRoot,
  readDataFile,
} from './dataRoot';
export { resolveBmdCandidates, catalogEntryFor, COSMETIC_CATALOG } from './itemCatalog';

/** @deprecated use loadItemBmd / parseBmd */
export async function loadBmdPlaceholder(path: string): Promise<BmdLoadResult> {
  return {
    ok: false,
    reason: `Use loadItemBmd / Data folder. Stub path was: ${path}`,
  };
}

/** @deprecated use muTextures.bytesToTexture */
export async function loadMuTexturePlaceholder(path: string): Promise<BmdLoadResult> {
  return {
    ok: false,
    reason: `Use bytesToTexture via Data folder. Stub path was: ${path}`,
  };
}
