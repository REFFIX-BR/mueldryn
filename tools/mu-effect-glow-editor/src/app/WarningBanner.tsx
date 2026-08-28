import { useSyncExternalStore } from 'react';
import {
  FIDELITY_WARNING,
  FIDELITY_WARNING_PLACEHOLDER,
  getDataRootState,
  subscribeDataRoot,
} from '@/mu/fidelity';
import { useEditorStore } from '@/state/store';

export function WarningBanner() {
  const data = useSyncExternalStore(subscribeDataRoot, getDataRootState, getDataRootState);
  const preview = useEditorStore((s) => s.ui.previewStatus);
  const meshesOk = data.ready && !preview.usingPlaceholder && preview.loadedParts > 0;

  return (
    <div className="warning-banner" role="status">
      <strong>Warning</strong>
      <span>{meshesOk ? FIDELITY_WARNING : FIDELITY_WARNING_PLACEHOLDER}</span>
      <span className="muted" style={{ marginLeft: 'auto', fontSize: 11 }}>
        {data.ready ? data.label : 'Data: —'} · {preview.message}
      </span>
    </div>
  );
}
