import { temporal } from 'zundo';
import { create } from 'zustand';
import type {
  AnimClip,
  CameraPreset,
  CharacterClass,
  EffectEntry,
  EquipmentSlot,
  GlowConfig,
  ItemEffectConfig,
  ItemEffectsDocument,
  ItemRef,
  LoadoutState,
} from '@/schema/itemEffects';
import {
  createEffectEntry,
  createItemConfig,
  emptyLoadout,
  itemKey,
} from '@/schema/itemEffects';
import { buildBloodySoldierDocument } from '@/loadout/presets';

export type EditorTab = 'glow' | 'effects' | 'particles';

export interface EditorUiState {
  selectedSlot: EquipmentSlot;
  selectedEffectId: string | null;
  editorTab: EditorTab;
  compareMode: boolean;
  showBeforeAfter: boolean;
  /** When true, left pane shows source snapshot */
  cameraPreset: CameraPreset;
  animClip: AnimClip;
  animPlaying: boolean;
  animSpeed: number;
  showBones: boolean;
  showGizmo: boolean;
  fps: number;
  fileName: string;
  /** BMD preview pipeline status */
  previewStatus: {
    loading: boolean;
    loadedParts: number;
    failedParts: number;
    usingPlaceholder: boolean;
    message: string;
  };
}

interface EditorStore {
  document: ItemEffectsDocument;
  sourceSnapshot: ItemEffectsDocument | null;
  ui: EditorUiState;

  // Document
  replaceDocument: (doc: ItemEffectsDocument, fileName?: string) => void;
  setMetaName: (name: string) => void;
  touchUpdated: () => void;

  // Loadout
  setCharacterClass: (c: CharacterClass) => void;
  setItemLevel: (level: number) => void;
  setSlotItem: (slot: EquipmentSlot, item: ItemRef | null) => void;
  applyLoadoutAndItems: (loadout: LoadoutState, items: ItemEffectsDocument['items']) => void;

  // Selection
  selectSlot: (slot: EquipmentSlot) => void;
  selectEffect: (id: string | null) => void;
  setEditorTab: (tab: EditorTab) => void;

  // Item config
  ensureSelectedItemConfig: () => ItemEffectConfig | null;
  updateGlow: (patch: Partial<GlowConfig>) => void;
  updateGlowLevel: (level: number, patch: Partial<GlowConfig['byLevel'][number]>) => void;
  addEffect: () => void;
  removeEffect: (id: string) => void;
  duplicateEffect: (id: string) => void;
  updateEffect: (id: string, patch: Partial<EffectEntry>) => void;

  // Viewport / UI
  setCompareMode: (v: boolean) => void;
  captureBeforeAfter: () => void;
  clearBeforeAfter: () => void;
  setCameraPreset: (p: CameraPreset) => void;
  setAnimClip: (c: AnimClip) => void;
  setAnimPlaying: (v: boolean) => void;
  setAnimSpeed: (v: number) => void;
  setShowBones: (v: boolean) => void;
  setShowGizmo: (v: boolean) => void;
  setFps: (fps: number) => void;
  setPreviewStatus: (status: EditorUiState['previewStatus']) => void;

  // Helpers
  getSelectedItemRef: () => ItemRef | null;
  getSelectedConfig: () => ItemEffectConfig | null;
  getSelectedEffect: () => EffectEntry | null;
}

const seed = buildBloodySoldierDocument();

const initialUi: EditorUiState = {
  selectedSlot: 'wings',
  selectedEffectId: null,
  editorTab: 'effects',
  compareMode: false,
  showBeforeAfter: false,
  cameraPreset: 'fullBody',
  animClip: 'idle',
  animPlaying: true,
  animSpeed: 1,
  showBones: false,
  showGizmo: true,
  fps: 0,
  fileName: 'item_effects.json',
  previewStatus: {
    loading: false,
    loadedParts: 0,
    failedParts: 0,
    usingPlaceholder: true,
    message: 'Aguardando pasta Data…',
  },
};

function cloneDoc(doc: ItemEffectsDocument): ItemEffectsDocument {
  return structuredClone(doc);
}

function mutateSelectedConfig(
  state: { document: ItemEffectsDocument; ui: EditorUiState },
  fn: (cfg: ItemEffectConfig) => void,
): Partial<EditorStore> | null {
  const slotItem = state.document.loadout.slots[state.ui.selectedSlot];
  if (!slotItem) return null;
  const key = itemKey(slotItem);
  const items = { ...state.document.items };
  const existing = items[key] ?? createItemConfig(slotItem);
  const cfg = structuredClone(existing);
  fn(cfg);
  items[key] = cfg;
  return {
    document: {
      ...state.document,
      items,
      meta: { ...state.document.meta, updatedAt: new Date().toISOString() },
    },
  };
}

export const useEditorStore = create<EditorStore>()(
  temporal(
    (set, get) => ({
      document: seed,
      sourceSnapshot: cloneDoc(seed),
      ui: {
        ...initialUi,
        selectedEffectId: seed.items[itemKey(seed.loadout.slots.wings!)]?.effects[0]?.id ?? null,
      },

      replaceDocument: (doc, fileName) =>
        set({
          document: doc,
          sourceSnapshot: cloneDoc(doc),
          ui: {
            ...get().ui,
            fileName: fileName ?? get().ui.fileName,
            selectedEffectId: null,
            showBeforeAfter: false,
          },
        }),

      setMetaName: (name) =>
        set((s) => ({
          document: {
            ...s.document,
            meta: { ...s.document.meta, name, updatedAt: new Date().toISOString() },
          },
        })),

      touchUpdated: () =>
        set((s) => ({
          document: {
            ...s.document,
            meta: { ...s.document.meta, updatedAt: new Date().toISOString() },
          },
        })),

      setCharacterClass: (characterClass) =>
        set((s) => ({
          document: {
            ...s.document,
            loadout: { ...s.document.loadout, characterClass },
            meta: { ...s.document.meta, updatedAt: new Date().toISOString() },
          },
        })),

      setItemLevel: (itemLevel) =>
        set((s) => ({
          document: {
            ...s.document,
            loadout: {
              ...s.document.loadout,
              itemLevel: Math.min(15, Math.max(0, Math.round(itemLevel))),
            },
            meta: { ...s.document.meta, updatedAt: new Date().toISOString() },
          },
        })),

      setSlotItem: (slot, item) =>
        set((s) => {
          const slots = { ...s.document.loadout.slots, [slot]: item };
          const items = { ...s.document.items };
          if (item && !items[itemKey(item)]) {
            items[itemKey(item)] = createItemConfig(item);
          }
          return {
            document: {
              ...s.document,
              loadout: { ...s.document.loadout, slots },
              items,
              meta: { ...s.document.meta, updatedAt: new Date().toISOString() },
            },
            ui: { ...s.ui, selectedSlot: slot },
          };
        }),

      applyLoadoutAndItems: (loadout, items) =>
        set((s) => ({
          document: {
            ...s.document,
            loadout,
            items: { ...s.document.items, ...items },
            meta: { ...s.document.meta, updatedAt: new Date().toISOString() },
          },
          sourceSnapshot: cloneDoc({
            ...s.document,
            loadout,
            items: { ...s.document.items, ...items },
          }),
        })),

      selectSlot: (selectedSlot) =>
        set((s) => ({
          ui: { ...s.ui, selectedSlot, selectedEffectId: null },
        })),

      selectEffect: (selectedEffectId) => set((s) => ({ ui: { ...s.ui, selectedEffectId } })),

      setEditorTab: (editorTab) => set((s) => ({ ui: { ...s.ui, editorTab } })),

      ensureSelectedItemConfig: () => {
        const s = get();
        const ref = s.document.loadout.slots[s.ui.selectedSlot];
        if (!ref) return null;
        const key = itemKey(ref);
        if (!s.document.items[key]) {
          set({
            document: {
              ...s.document,
              items: { ...s.document.items, [key]: createItemConfig(ref) },
            },
          });
        }
        return get().document.items[itemKey(ref)] ?? null;
      },

      updateGlow: (patch) =>
        set((s) => {
          const next = mutateSelectedConfig(s, (cfg) => {
            cfg.glow = { ...cfg.glow, ...patch };
          });
          return next ?? s;
        }),

      updateGlowLevel: (level, patch) =>
        set((s) => {
          const next = mutateSelectedConfig(s, (cfg) => {
            cfg.glow.byLevel = cfg.glow.byLevel.map((e) =>
              e.level === level ? { ...e, ...patch } : e,
            );
          });
          return next ?? s;
        }),

      addEffect: () =>
        set((s) => {
          const fx = createEffectEntry();
          const next = mutateSelectedConfig(s, (cfg) => {
            cfg.effects = [...cfg.effects, fx];
          });
          if (!next) return s;
          return {
            ...next,
            ui: { ...s.ui, selectedEffectId: fx.id, editorTab: 'effects' },
          };
        }),

      removeEffect: (id) =>
        set((s) => {
          const next = mutateSelectedConfig(s, (cfg) => {
            cfg.effects = cfg.effects.filter((e) => e.id !== id);
          });
          if (!next) return s;
          return {
            ...next,
            ui: {
              ...s.ui,
              selectedEffectId: s.ui.selectedEffectId === id ? null : s.ui.selectedEffectId,
            },
          };
        }),

      duplicateEffect: (id) =>
        set((s) => {
          let newId: string | null = null;
          const next = mutateSelectedConfig(s, (cfg) => {
            const src = cfg.effects.find((e) => e.id === id);
            if (!src) return;
            const copy = createEffectEntry({
              ...structuredClone(src),
              id: undefined,
              name: `${src.name} Copy`,
            });
            newId = copy.id;
            cfg.effects = [...cfg.effects, copy];
          });
          if (!next) return s;
          return {
            ...next,
            ui: { ...s.ui, selectedEffectId: newId },
          };
        }),

      updateEffect: (id, patch) =>
        set((s) => {
          const next = mutateSelectedConfig(s, (cfg) => {
            cfg.effects = cfg.effects.map((e) => (e.id === id ? { ...e, ...patch } : e));
          });
          return next ?? s;
        }),

      setCompareMode: (compareMode) => set((s) => ({ ui: { ...s.ui, compareMode } })),

      captureBeforeAfter: () =>
        set((s) => ({
          sourceSnapshot: cloneDoc(s.document),
          ui: { ...s.ui, showBeforeAfter: true, compareMode: true },
        })),

      clearBeforeAfter: () =>
        set((s) => ({
          ui: { ...s.ui, showBeforeAfter: false },
        })),

      setCameraPreset: (cameraPreset) => set((s) => ({ ui: { ...s.ui, cameraPreset } })),
      setAnimClip: (animClip) => set((s) => ({ ui: { ...s.ui, animClip } })),
      setAnimPlaying: (animPlaying) => set((s) => ({ ui: { ...s.ui, animPlaying } })),
      setAnimSpeed: (animSpeed) => set((s) => ({ ui: { ...s.ui, animSpeed } })),
      setShowBones: (showBones) => set((s) => ({ ui: { ...s.ui, showBones } })),
      setShowGizmo: (showGizmo) => set((s) => ({ ui: { ...s.ui, showGizmo } })),
      setFps: (fps) => set((s) => ({ ui: { ...s.ui, fps } })),
      setPreviewStatus: (previewStatus) => set((s) => ({ ui: { ...s.ui, previewStatus } })),

      getSelectedItemRef: () => {
        const s = get();
        return s.document.loadout.slots[s.ui.selectedSlot];
      },

      getSelectedConfig: () => {
        const s = get();
        const ref = s.document.loadout.slots[s.ui.selectedSlot];
        if (!ref) return null;
        return s.document.items[itemKey(ref)] ?? null;
      },

      getSelectedEffect: () => {
        const s = get();
        const cfg = s.getSelectedConfig();
        if (!cfg || !s.ui.selectedEffectId) return null;
        return cfg.effects.find((e) => e.id === s.ui.selectedEffectId) ?? null;
      },
    }),
    {
      partialize: (state) => ({
        document: state.document,
      }),
      limit: 50,
    },
  ),
);

/** Undo/redo API from zundo temporal middleware */
export const temporalApi = () => useEditorStore.temporal.getState();
