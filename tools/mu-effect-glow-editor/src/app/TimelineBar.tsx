import { useEditorStore } from '@/state/store';
import type { AnimClip } from '@/schema/itemEffects';

const CLIPS: { id: AnimClip; label: string }[] = [
  { id: 'idle', label: 'Idle' },
  { id: 'walk', label: 'Walk' },
  { id: 'run', label: 'Run' },
  { id: 'attack', label: 'Attack' },
  { id: 'skill', label: 'Skill' },
  { id: 'sit', label: 'Sit' },
  { id: 'die', label: 'Die' },
];

export function TimelineBar() {
  const ui = useEditorStore((s) => s.ui);
  const setAnimClip = useEditorStore((s) => s.setAnimClip);
  const setAnimPlaying = useEditorStore((s) => s.setAnimPlaying);
  const setAnimSpeed = useEditorStore((s) => s.setAnimSpeed);

  return (
    <div className="timeline-bar">
      <span className="muted mono" style={{ minWidth: 72 }}>
        ANIM
      </span>
      <button type="button" onClick={() => setAnimPlaying(!ui.animPlaying)}>
        {ui.animPlaying ? 'Pause' : 'Play'}
      </button>
      <div className="row wrap">
        {CLIPS.map((c) => (
          <button
            key={c.id}
            type="button"
            className={ui.animClip === c.id ? 'active' : ''}
            onClick={() => setAnimClip(c.id)}
          >
            {c.label}
          </button>
        ))}
      </div>
      <div className="sep" style={{ width: 1, height: 22, background: 'var(--border)' }} />
      <label className="field" style={{ width: 160, textTransform: 'none' }}>
        Speed {ui.animSpeed.toFixed(2)}×
        <input
          type="range"
          min={0.1}
          max={2.5}
          step={0.05}
          value={ui.animSpeed}
          onChange={(e) => setAnimSpeed(Number(e.target.value))}
        />
      </label>
      <span className="muted" style={{ marginLeft: 'auto', fontSize: 11 }}>
        {ui.previewStatus.usingPlaceholder
          ? 'BMD animation stub — bob/flap procedural até skeleton real'
          : `BMD mesh preview — ${ui.previewStatus.message} (anim keys ainda stub)`}
      </span>
    </div>
  );
}
