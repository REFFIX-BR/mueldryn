import { useEditorStore } from '@/state/store';
import type { CharacterClass, EquipmentSlot, ItemRef } from '@/schema/itemEffects';
import { itemKey, itemTypeId } from '@/schema/itemEffects';
import { SetPicker } from './SetPicker';

const CLASSES: CharacterClass[] = [
  'DarkKnight',
  'DarkWizard',
  'Elf',
  'MagicGladiator',
  'DarkLord',
  'Summoner',
  'RageFighter',
  'GrowLancer',
];

const SLOTS: { id: EquipmentSlot; label: string; defaultGroup: number }[] = [
  { id: 'helm', label: 'Helm', defaultGroup: 7 },
  { id: 'armor', label: 'Armor', defaultGroup: 8 },
  { id: 'pants', label: 'Pants', defaultGroup: 9 },
  { id: 'gloves', label: 'Gloves', defaultGroup: 10 },
  { id: 'boots', label: 'Boots', defaultGroup: 11 },
  { id: 'weapon', label: 'Weapon', defaultGroup: 0 },
  { id: 'offhand', label: 'Offhand', defaultGroup: 6 },
  { id: 'wings', label: 'Wings', defaultGroup: 12 },
  { id: 'cape', label: 'Cape', defaultGroup: 13 },
];

function SlotEditor({
  slot,
  label,
  defaultGroup,
}: {
  slot: EquipmentSlot;
  label: string;
  defaultGroup: number;
}) {
  const item = useEditorStore((s) => s.document.loadout.slots[slot]);
  const selectedSlot = useEditorStore((s) => s.ui.selectedSlot);
  const selectSlot = useEditorStore((s) => s.selectSlot);
  const setSlotItem = useEditorStore((s) => s.setSlotItem);

  const selected = selectedSlot === slot;

  const update = (patch: Partial<ItemRef>) => {
    if (!item) {
      const next: ItemRef = {
        name: patch.name ?? label,
        group: patch.group ?? defaultGroup,
        index: patch.index ?? 0,
      };
      setSlotItem(slot, next);
      return;
    }
    setSlotItem(slot, { ...item, ...patch });
  };

  return (
    <div
      className={`slot-row ${selected ? 'selected' : ''}`}
      onClick={() => selectSlot(slot)}
      role="button"
      tabIndex={0}
      onKeyDown={(e) => e.key === 'Enter' && selectSlot(slot)}
    >
      <div className="slot-name">{label}</div>
      <div className="row" style={{ gap: 4 }} onClick={(e) => e.stopPropagation()}>
        <input
          type="number"
          title="Group"
          style={{ width: 48 }}
          value={item?.group ?? ''}
          placeholder="G"
          onChange={(e) => {
            const v = e.target.value === '' ? null : Number(e.target.value);
            if (v === null) setSlotItem(slot, null);
            else update({ group: v });
          }}
          onFocus={() => selectSlot(slot)}
        />
        <input
          type="number"
          title="Index"
          style={{ width: 52 }}
          value={item?.index ?? ''}
          placeholder="Idx"
          onChange={(e) => {
            const v = e.target.value === '' ? null : Number(e.target.value);
            if (v === null) setSlotItem(slot, null);
            else update({ index: v });
          }}
          onFocus={() => selectSlot(slot)}
        />
        <button
          type="button"
          title="Clear slot"
          style={{ padding: '4px 6px' }}
          onClick={() => setSlotItem(slot, null)}
        >
          ×
        </button>
      </div>
      {item && (
        <div
          className="mono muted"
          style={{ gridColumn: '1 / -1', fontSize: 10, marginTop: -2 }}
        >
          {item.name || label} · {itemKey(item)} · type {itemTypeId(item)}
        </div>
      )}
    </div>
  );
}

export function CharacterLoadout() {
  const characterClass = useEditorStore((s) => s.document.loadout.characterClass);
  const itemLevel = useEditorStore((s) => s.document.loadout.itemLevel);
  const setCharacterClass = useEditorStore((s) => s.setCharacterClass);
  const setItemLevel = useEditorStore((s) => s.setItemLevel);

  return (
    <div className="left-panel">
      <div className="panel-section">
        <h3>Character Loadout</h3>
        <label className="field">
          Class
          <select
            value={characterClass}
            onChange={(e) => setCharacterClass(e.target.value as CharacterClass)}
          >
            {CLASSES.map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </select>
        </label>
        <label className="field" style={{ marginTop: 8 }}>
          Item Level +{itemLevel}
          <input
            type="range"
            min={0}
            max={15}
            value={itemLevel}
            onChange={(e) => setItemLevel(Number(e.target.value))}
          />
        </label>
      </div>

      <SetPicker />

      <div className="panel-section">
        <h3>Equipment</h3>
        <p className="muted" style={{ margin: '0 0 8px', fontSize: 11, textTransform: 'none' }}>
          Group : Index — clique no slot para editar glow/FX
        </p>
        {SLOTS.map((s) => (
          <SlotEditor key={s.id} slot={s.id} label={s.label} defaultGroup={s.defaultGroup} />
        ))}
      </div>
    </div>
  );
}
