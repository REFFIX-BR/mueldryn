import { useEditorStore, temporalApi } from '@/state/store';
import { downloadJson, loadWithFilePicker, saveWithFilePicker } from '@/io/saveLoad';
import {
  AssetInspectorImport,
  LauncherPublish,
  LiveClientPreview,
} from '@/bridge';
import { pickDataFolder, getDataRootState, subscribeDataRoot } from '@/mu/dataRoot';
import { useSyncExternalStore } from 'react';
import type { CameraPreset } from '@/schema/itemEffects';

const CAMERA_PRESETS: { id: CameraPreset; label: string }[] = [
  { id: 'fullBody', label: 'Full Body' },
  { id: 'upper', label: 'Upper' },
  { id: 'weapon', label: 'Weapon' },
  { id: 'wings', label: 'Wings' },
];

export function Toolbar() {
  const document = useEditorStore((s) => s.document);
  const ui = useEditorStore((s) => s.ui);
  const replaceDocument = useEditorStore((s) => s.replaceDocument);
  const setCompareMode = useEditorStore((s) => s.setCompareMode);
  const captureBeforeAfter = useEditorStore((s) => s.captureBeforeAfter);
  const setCameraPreset = useEditorStore((s) => s.setCameraPreset);
  const setShowBones = useEditorStore((s) => s.setShowBones);
  const setShowGizmo = useEditorStore((s) => s.setShowGizmo);
  const setMetaName = useEditorStore((s) => s.setMetaName);
  const dataRoot = useSyncExternalStore(subscribeDataRoot, getDataRootState, getDataRootState);

  const onSave = async () => {
    try {
      const name = await saveWithFilePicker(document, ui.fileName);
      if (name) useEditorStore.setState((s) => ({ ui: { ...s.ui, fileName: name } }));
    } catch (e) {
      alert(`Falha ao salvar: ${(e as Error).message}`);
    }
  };

  const onExport = async () => {
    await downloadJson(document, ui.fileName || 'item_effects.json');
  };

  const onLoad = async () => {
    try {
      const result = await loadWithFilePicker();
      if (!result) return;
      replaceDocument(result.doc, result.fileName);
    } catch (e) {
      alert(`Falha ao carregar: ${(e as Error).message}`);
    }
  };

  const onPickData = async () => {
    const st = await pickDataFolder();
    if (st.lastError) alert(st.lastError);
  };

  const onUndo = () => temporalApi().undo();
  const onRedo = () => temporalApi().redo();

  const bridgeToast = (msg: string) => alert(msg);

  return (
    <div className="toolbar">
      <div className="brand">
        MU <span>Effect &amp; Glow</span> Editor
      </div>
      <div className="sep" />
      <button type="button" onClick={onLoad} title="Load item_effects.json">
        Load
      </button>
      <button type="button" className="primary" onClick={onSave} title="Save JSON">
        Save
      </button>
      <button type="button" onClick={onExport} title="Download JSON">
        Export
      </button>
      <button
        type="button"
        className={dataRoot.ready ? 'active' : ''}
        onClick={onPickData}
        title="Selecionar pasta Data do cliente (Player/Item BMDs). No npm run dev o Vite já tenta MuMain/…/Data."
      >
        {dataRoot.ready ? 'Data ✓' : 'Data folder…'}
      </button>
      <div className="sep" />
      <button type="button" onClick={onUndo} title="Undo">
        Undo
      </button>
      <button type="button" onClick={onRedo} title="Redo">
        Redo
      </button>
      <div className="sep" />
      <button
        type="button"
        className={ui.compareMode ? 'active' : ''}
        onClick={() => setCompareMode(!ui.compareMode)}
      >
        Compare
      </button>
      <button type="button" onClick={captureBeforeAfter} title="Snapshot Before/After">
        Before/After
      </button>
      <div className="sep" />
      {CAMERA_PRESETS.map((p) => (
        <button
          key={p.id}
          type="button"
          className={ui.cameraPreset === p.id ? 'active' : ''}
          onClick={() => setCameraPreset(p.id)}
        >
          {p.label}
        </button>
      ))}
      <div className="sep" />
      <button
        type="button"
        className={ui.showBones ? 'active' : ''}
        onClick={() => setShowBones(!ui.showBones)}
      >
        Bones
      </button>
      <button
        type="button"
        className={ui.showGizmo ? 'active' : ''}
        onClick={() => setShowGizmo(!ui.showGizmo)}
      >
        Gizmo
      </button>
      <div className="sep" />
      <input
        type="text"
        style={{ width: 160 }}
        value={document.meta.name}
        onChange={(e) => setMetaName(e.target.value)}
        title="Document name"
      />
      <div style={{ flex: 1 }} />
      <button
        type="button"
        className="ghost"
        title={LiveClientPreview.describe().message}
        onClick={() => bridgeToast(LiveClientPreview.describe().message)}
      >
        Live Client
      </button>
      <button
        type="button"
        className="ghost"
        title={AssetInspectorImport.describe().message}
        onClick={() => bridgeToast(AssetInspectorImport.describe().message)}
      >
        Assets
      </button>
      <button
        type="button"
        className="ghost"
        title={LauncherPublish.describe().message}
        onClick={() => bridgeToast(LauncherPublish.describe().message)}
      >
        Publish
      </button>
    </div>
  );
}
