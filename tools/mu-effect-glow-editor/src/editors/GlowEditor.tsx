import { useEditorStore } from '@/state/store';
import { hexToRgb, rgbToHex } from '@/mu/interfaces';
import { itemKey } from '@/schema/itemEffects';

export function GlowEditor() {
  const selectedSlot = useEditorStore((s) => s.ui.selectedSlot);
  const loadout = useEditorStore((s) => s.document.loadout);
  const items = useEditorStore((s) => s.document.items);
  const updateGlow = useEditorStore((s) => s.updateGlow);
  const updateGlowLevel = useEditorStore((s) => s.updateGlowLevel);
  const ensure = useEditorStore((s) => s.ensureSelectedItemConfig);

  const ref = loadout.slots[selectedSlot];
  const cfg = ref ? items[itemKey(ref)] : null;
  const level = loadout.itemLevel;

  if (!cfg) {
    return (
      <div className="panel-section">
        <p className="muted">Selecione um slot equipado para editar glow.</p>
        <button type="button" onClick={() => ensure()}>
          Criar config
        </button>
      </div>
    );
  }

  const glow = cfg.glow;
  const lv = glow.byLevel.find((e) => e.level === level) ?? glow.byLevel[0];

  return (
    <div className="panel-section">
      <h3>Glow</h3>
      <label className="row" style={{ marginBottom: 8 }}>
        <input
          type="checkbox"
          checked={glow.enabled}
          onChange={(e) => updateGlow({ enabled: e.target.checked })}
        />
        <span className="muted" style={{ textTransform: 'none' }}>
          Enabled (mesh-local — sem wash full-body)
        </span>
      </label>

      <div className="grid-2" style={{ marginBottom: 8 }}>
        <label className="field">
          Base color
          <div className="row">
            <div className="color-swatch">
              <input
                type="color"
                value={rgbToHex(glow.baseColor)}
                onChange={(e) => updateGlow({ baseColor: hexToRgb(e.target.value) })}
              />
            </div>
            <span className="mono muted">{rgbToHex(glow.baseColor)}</span>
          </div>
        </label>
        <label className="field">
          Base intensity {glow.baseIntensity.toFixed(2)}
          <input
            type="range"
            min={0}
            max={2}
            step={0.05}
            value={glow.baseIntensity}
            onChange={(e) => updateGlow({ baseIntensity: Number(e.target.value) })}
          />
        </label>
      </div>

      <div className="grid-2" style={{ marginBottom: 12 }}>
        <label className="field">
          Mesh index
          <input
            type="number"
            value={glow.meshIndex}
            onChange={(e) => updateGlow({ meshIndex: Number(e.target.value) })}
          />
        </label>
        <label className="field">
          Render flags
          <input
            type="number"
            value={glow.renderFlags}
            onChange={(e) => updateGlow({ renderFlags: Number(e.target.value) })}
            title="ex: 66 = TEXTURE|BRIGHT"
          />
        </label>
      </div>

      <h3>Glow by level (+{level})</h3>
      {lv && (
        <div className="grid-2">
          <label className="field">
            Level color
            <div className="row">
              <div className="color-swatch">
                <input
                  type="color"
                  value={rgbToHex(lv.color)}
                  onChange={(e) =>
                    updateGlowLevel(level, { color: hexToRgb(e.target.value) })
                  }
                />
              </div>
            </div>
          </label>
          <label className="field">
            Intensity {lv.intensity.toFixed(2)}
            <input
              type="range"
              min={0}
              max={2.5}
              step={0.05}
              value={lv.intensity}
              onChange={(e) =>
                updateGlowLevel(level, { intensity: Number(e.target.value) })
              }
            />
          </label>
          <label className="field" style={{ gridColumn: '1 / -1' }}>
            Emissive {lv.emissive.toFixed(2)}
            <input
              type="range"
              min={0}
              max={2}
              step={0.05}
              value={lv.emissive}
              onChange={(e) =>
                updateGlowLevel(level, { emissive: Number(e.target.value) })
              }
            />
          </label>
        </div>
      )}
      <p className="muted" style={{ marginTop: 8, fontSize: 11, textTransform: 'none' }}>
        Preview usa emissive moderado por peça. Ajuste o nível global no painel esquerdo.
      </p>
    </div>
  );
}
