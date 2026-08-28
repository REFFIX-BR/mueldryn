import { useEditorStore } from '@/state/store';
import type { EditorTab } from '@/state/store';
import { GlowEditor } from './GlowEditor';
import { EffectsEditor } from './EffectsEditor';
import { ParticleEditor } from './ParticleEditor';
import { itemKey, itemTypeId } from '@/schema/itemEffects';
import {
  AssetInspectorImport,
  LauncherPublish,
  LiveClientPreview,
} from '@/bridge';

const TABS: { id: EditorTab; label: string }[] = [
  { id: 'glow', label: 'Glow' },
  { id: 'effects', label: 'Effects' },
  { id: 'particles', label: 'Particles' },
];

export function PropertiesPanel() {
  const tab = useEditorStore((s) => s.ui.editorTab);
  const setEditorTab = useEditorStore((s) => s.setEditorTab);
  const slot = useEditorStore((s) => s.ui.selectedSlot);
  const ref = useEditorStore((s) => s.document.loadout.slots[s.ui.selectedSlot]);

  return (
    <div className="right-panel">
      <div className="panel-section" style={{ paddingBottom: 6 }}>
        <h3>Properties</h3>
        <div className="mono" style={{ fontSize: 12 }}>
          Slot: <span style={{ color: 'var(--accent)' }}>{slot}</span>
          {ref ? (
            <>
              <br />
              {ref.name}
              <br />
              <span className="muted">
                {itemKey(ref)} · ItemType {itemTypeId(ref)}
              </span>
            </>
          ) : (
            <span className="muted"> — vazio</span>
          )}
        </div>
      </div>

      <div className="tabs">
        {TABS.map((t) => (
          <button
            key={t.id}
            type="button"
            className={tab === t.id ? 'active' : ''}
            onClick={() => setEditorTab(t.id)}
          >
            {t.label}
          </button>
        ))}
      </div>

      {tab === 'glow' && <GlowEditor />}
      {tab === 'effects' && <EffectsEditor />}
      {tab === 'particles' && <ParticleEditor />}

      <div className="stub-note">
        Bridges: {LiveClientPreview.id} · {AssetInspectorImport.id} · {LauncherPublish.id} —
        stubs prontos para integração.
      </div>
    </div>
  );
}
