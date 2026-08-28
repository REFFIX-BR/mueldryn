import { useEditorStore } from '@/state/store';
import { AttachmentPicker } from './AttachmentPicker';
import { hexToRgb, rgbToHex } from '@/mu/interfaces';
import type { BlendMode, Vec3 } from '@/schema/itemEffects';
import { itemKey } from '@/schema/itemEffects';

const BLENDS: BlendMode[] = ['normal', 'additive', 'multiply', 'screen', 'bright'];

export function EffectsEditor() {
  const selectedSlot = useEditorStore((s) => s.ui.selectedSlot);
  const selectedEffectId = useEditorStore((s) => s.ui.selectedEffectId);
  const loadout = useEditorStore((s) => s.document.loadout);
  const items = useEditorStore((s) => s.document.items);
  const selectEffect = useEditorStore((s) => s.selectEffect);
  const addEffect = useEditorStore((s) => s.addEffect);
  const removeEffect = useEditorStore((s) => s.removeEffect);
  const duplicateEffect = useEditorStore((s) => s.duplicateEffect);
  const updateEffect = useEditorStore((s) => s.updateEffect);
  const ensure = useEditorStore((s) => s.ensureSelectedItemConfig);

  const ref = loadout.slots[selectedSlot];
  const cfg = ref ? items[itemKey(ref)] : null;

  if (!cfg) {
    return (
      <div className="panel-section">
        <p className="muted">Sem item selecionado.</p>
        <button type="button" onClick={() => ensure()}>
          Criar config
        </button>
      </div>
    );
  }

  const selected = cfg.effects.find((e) => e.id === selectedEffectId) ?? null;

  const setVec = (key: 'position' | 'rotation', axis: keyof Vec3, value: number) => {
    if (!selected) return;
    updateEffect(selected.id, {
      [key]: { ...selected[key], [axis]: value },
    });
  };

  return (
    <>
      <div className="panel-section">
        <h3>Effects</h3>
        <div className="row" style={{ marginBottom: 8 }}>
          <button type="button" className="primary" onClick={addEffect}>
            Add
          </button>
          <button
            type="button"
            disabled={!selected}
            onClick={() => selected && duplicateEffect(selected.id)}
          >
            Duplicate
          </button>
          <button
            type="button"
            disabled={!selected}
            onClick={() => selected && removeEffect(selected.id)}
          >
            Remove
          </button>
        </div>
        <ul className="effect-list">
          {cfg.effects.length === 0 && (
            <li className="muted" style={{ cursor: 'default' }}>
              Nenhum efeito — Add para criar
            </li>
          )}
          {cfg.effects.map((e) => (
            <li
              key={e.id}
              className={e.id === selectedEffectId ? 'selected' : ''}
              onClick={() => selectEffect(e.id)}
            >
              <span
                style={{
                  width: 10,
                  height: 10,
                  borderRadius: 2,
                  background: rgbToHex(e.color),
                  flexShrink: 0,
                }}
              />
              <span style={{ flex: 1 }}>{e.name}</span>
              <span className="mono muted">{e.bone}</span>
            </li>
          ))}
        </ul>
      </div>

      {selected && (
        <div className="panel-section">
          <h3>Effect properties</h3>
          <label className="row" style={{ marginBottom: 8 }}>
            <input
              type="checkbox"
              checked={selected.enabled}
              onChange={(e) => updateEffect(selected.id, { enabled: e.target.checked })}
            />
            <span className="muted" style={{ textTransform: 'none' }}>
              Enabled
            </span>
          </label>

          <label className="field" style={{ marginBottom: 8 }}>
            Name
            <input
              type="text"
              value={selected.name}
              onChange={(e) => updateEffect(selected.id, { name: e.target.value })}
            />
          </label>

          <div className="grid-2" style={{ marginBottom: 8 }}>
            <label className="field">
              Mudream code
              <input
                type="number"
                value={selected.mudreamCode}
                onChange={(e) =>
                  updateEffect(selected.id, { mudreamCode: Number(e.target.value) })
                }
              />
            </label>
            <label className="field">
              Blend
              <select
                value={selected.blend}
                onChange={(e) =>
                  updateEffect(selected.id, { blend: e.target.value as BlendMode })
                }
              >
                {BLENDS.map((b) => (
                  <option key={b} value={b}>
                    {b}
                  </option>
                ))}
              </select>
            </label>
          </div>

          <AttachmentPicker
            bone={selected.bone}
            boneIndex={selected.boneIndex}
            onChange={(bone, boneIndex) => updateEffect(selected.id, { bone, boneIndex })}
          />

          <h3 style={{ marginTop: 12 }}>Position</h3>
          <div className="grid-3">
            {(['x', 'y', 'z'] as const).map((axis) => (
              <label key={axis} className="field">
                {axis}
                <input
                  type="number"
                  step={0.05}
                  value={selected.position[axis]}
                  onChange={(e) => setVec('position', axis, Number(e.target.value))}
                />
              </label>
            ))}
          </div>

          <h3 style={{ marginTop: 12 }}>Rotation</h3>
          <div className="grid-3">
            {(['x', 'y', 'z'] as const).map((axis) => (
              <label key={axis} className="field">
                {axis}
                <input
                  type="number"
                  step={1}
                  value={selected.rotation[axis]}
                  onChange={(e) => setVec('rotation', axis, Number(e.target.value))}
                />
              </label>
            ))}
          </div>

          <div className="grid-2" style={{ marginTop: 12 }}>
            <label className="field">
              Scale {selected.scale.toFixed(2)}
              <input
                type="range"
                min={0.1}
                max={3}
                step={0.05}
                value={selected.scale}
                onChange={(e) =>
                  updateEffect(selected.id, { scale: Number(e.target.value) })
                }
              />
            </label>
            <label className="field">
              Size {selected.size.toFixed(2)}
              <input
                type="range"
                min={0.05}
                max={2}
                step={0.05}
                value={selected.size}
                onChange={(e) =>
                  updateEffect(selected.id, { size: Number(e.target.value) })
                }
              />
            </label>
            <label className="field">
              Intensity {selected.intensity.toFixed(2)}
              <input
                type="range"
                min={0}
                max={3}
                step={0.05}
                value={selected.intensity}
                onChange={(e) =>
                  updateEffect(selected.id, { intensity: Number(e.target.value) })
                }
              />
            </label>
            <label className="field">
              Color
              <div className="row">
                <div className="color-swatch">
                  <input
                    type="color"
                    value={rgbToHex(selected.color)}
                    onChange={(e) =>
                      updateEffect(selected.id, { color: hexToRgb(e.target.value) })
                    }
                  />
                </div>
              </div>
            </label>
          </div>
        </div>
      )}
    </>
  );
}
