import { useEditorStore } from '@/state/store';
import { SET_PRESETS, type SetPresetId } from './presets';

export function SetPicker() {
  const applyLoadoutAndItems = useEditorStore((s) => s.applyLoadoutAndItems);
  const setMetaName = useEditorStore((s) => s.setMetaName);

  const apply = (id: SetPresetId) => {
    const preset = SET_PRESETS.find((p) => p.id === id);
    if (!preset) return;
    const { loadout, items } = preset.apply();
    applyLoadoutAndItems(loadout, items);
    setMetaName(preset.label);
  };

  return (
    <div className="panel-section">
      <h3>Set / Preset</h3>
      <div className="row wrap">
        {SET_PRESETS.map((p) => (
          <button key={p.id} type="button" title={p.description} onClick={() => apply(p.id)}>
            {p.label}
          </button>
        ))}
      </div>
      <p className="muted" style={{ margin: '8px 0 0', fontSize: 11, textTransform: 'none' }}>
        Full set ou custom — presets seedam group:index conhecidos do projeto.
      </p>
    </div>
  );
}
