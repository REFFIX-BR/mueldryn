import { useEditorStore } from '@/state/store';
import { hexToRgb, rgbToHex } from '@/mu/interfaces';
import type { ParticleConfig } from '@/schema/itemEffects';
import { itemKey } from '@/schema/itemEffects';

export function ParticleEditor() {
  const selectedSlot = useEditorStore((s) => s.ui.selectedSlot);
  const selectedEffectId = useEditorStore((s) => s.ui.selectedEffectId);
  const loadout = useEditorStore((s) => s.document.loadout);
  const items = useEditorStore((s) => s.document.items);
  const updateEffect = useEditorStore((s) => s.updateEffect);

  const ref = loadout.slots[selectedSlot];
  const cfg = ref ? items[itemKey(ref)] : null;
  const selected = cfg?.effects.find((e) => e.id === selectedEffectId) ?? null;

  if (!selected) {
    return (
      <div className="panel-section">
        <p className="muted">Selecione um efeito na aba Effects.</p>
      </div>
    );
  }

  const p = selected.particles;
  const patch = (partial: Partial<ParticleConfig>) => {
    updateEffect(selected.id, { particles: { ...p, ...partial } });
  };

  return (
    <div className="panel-section">
      <h3>Particles — {selected.name}</h3>
      <label className="row" style={{ marginBottom: 8 }}>
        <input
          type="checkbox"
          checked={p.enabled}
          onChange={(e) => patch({ enabled: e.target.checked })}
        />
        <span className="muted" style={{ textTransform: 'none' }}>
          Enabled
        </span>
      </label>

      <div className="grid-2">
        <label className="field">
          Count
          <input
            type="number"
            min={0}
            max={200}
            value={p.count}
            onChange={(e) => patch({ count: Number(e.target.value) })}
          />
        </label>
        <label className="field">
          Size {p.size.toFixed(3)}
          <input
            type="range"
            min={0.01}
            max={0.3}
            step={0.005}
            value={p.size}
            onChange={(e) => patch({ size: Number(e.target.value) })}
          />
        </label>
        <label className="field">
          Speed {p.speed.toFixed(2)}
          <input
            type="range"
            min={0}
            max={2}
            step={0.05}
            value={p.speed}
            onChange={(e) => patch({ speed: Number(e.target.value) })}
          />
        </label>
        <label className="field">
          Lifetime {p.lifetime.toFixed(2)}
          <input
            type="range"
            min={0.2}
            max={4}
            step={0.1}
            value={p.lifetime}
            onChange={(e) => patch({ lifetime: Number(e.target.value) })}
          />
        </label>
        <label className="field">
          Spread {p.spread.toFixed(2)}
          <input
            type="range"
            min={0}
            max={1.5}
            step={0.05}
            value={p.spread}
            onChange={(e) => patch({ spread: Number(e.target.value) })}
          />
        </label>
        <label className="field">
          Color
          <div className="row">
            <div className="color-swatch">
              <input
                type="color"
                value={rgbToHex(p.color)}
                onChange={(e) => patch({ color: hexToRgb(e.target.value) })}
              />
            </div>
          </div>
        </label>
      </div>
      <p className="muted" style={{ marginTop: 8, fontSize: 11, textTransform: 'none' }}>
        Partículas approx (Points). Pipeline MU real virá via IEffectRenderer.
      </p>
    </div>
  );
}
