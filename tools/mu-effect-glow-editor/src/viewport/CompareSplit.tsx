import { useEffect, useRef } from 'react';
import { Canvas, useFrame, useThree } from '@react-three/fiber';
import { CharacterScene } from './CharacterScene';
import { useEditorStore } from '@/state/store';
import { itemKey } from '@/schema/itemEffects';
import type { EffectEntry } from '@/schema/itemEffects';

function FpsReporter() {
  const setFps = useEditorStore((s) => s.setFps);
  const frames = useRef(0);
  const last = useRef(performance.now());

  useFrame(() => {
    frames.current += 1;
    const now = performance.now();
    if (now - last.current >= 500) {
      const fps = Math.round((frames.current * 1000) / (now - last.current));
      setFps(fps);
      frames.current = 0;
      last.current = now;
    }
  });
  return null;
}

function SceneFromDoc({
  muted,
  useSource,
}: {
  muted?: boolean;
  useSource?: boolean;
}) {
  const document = useEditorStore((s) => s.document);
  const sourceSnapshot = useEditorStore((s) => s.sourceSnapshot);
  const ui = useEditorStore((s) => s.ui);

  const doc = useSource && sourceSnapshot ? sourceSnapshot : document;

  let selectedEffect = null;
  if (!useSource && ui.selectedEffectId) {
    const ref = document.loadout.slots[ui.selectedSlot];
    if (ref) {
      const cfg = document.items[itemKey(ref)];
      selectedEffect = cfg?.effects.find((e) => e.id === ui.selectedEffectId) ?? null;
    }
  }

  let effectsOverride: EffectEntry[] | undefined;
  if (useSource && sourceSnapshot) {
    effectsOverride = [];
    for (const ref of Object.values(sourceSnapshot.loadout.slots)) {
      if (!ref) continue;
      const cfg = sourceSnapshot.items[itemKey(ref)];
      if (cfg) effectsOverride.push(...cfg.effects.filter((e) => e.enabled));
    }
  }

  return (
    <CharacterScene
      loadout={doc.loadout}
      items={doc.items}
      cameraPreset={ui.cameraPreset}
      animClip={ui.animClip}
      animPlaying={ui.animPlaying}
      animSpeed={ui.animSpeed}
      showBones={ui.showBones}
      showGizmo={ui.showGizmo}
      selectedEffect={selectedEffect}
      muted={muted}
      effectsOverride={effectsOverride}
    />
  );
}

function ResizeFix() {
  const { gl } = useThree();
  useEffect(() => {
    gl.setSize(gl.domElement.clientWidth, gl.domElement.clientHeight, false);
  }, [gl]);
  return null;
}

export function ViewportCanvas() {
  const compareMode = useEditorStore((s) => s.ui.compareMode);
  const fps = useEditorStore((s) => s.ui.fps);
  const showBeforeAfter = useEditorStore((s) => s.ui.showBeforeAfter);
  const previewStatus = useEditorStore((s) => s.ui.previewStatus);

  if (compareMode) {
    return (
      <div className="compare-split">
        <div className="compare-pane">
          <div className="viewport-label">
            <span className="tag">SOURCE</span> snapshot
            {showBeforeAfter ? ' (Before)' : ''}
          </div>
          <Canvas shadows camera={{ position: [2.2, 1.4, 2.8], fov: 40 }} dpr={[1, 1.75]}>
            <FpsReporter />
            <ResizeFix />
            <SceneFromDoc muted useSource />
          </Canvas>
        </div>
        <div className="compare-pane">
          <div className="viewport-label">
            <span className="tag">EDITED</span> working copy
            {showBeforeAfter ? ' (After)' : ''}
          </div>
          <div className="perf-hud">
            FPS {fps}
            <br />
            Preview ≈
          </div>
          <Canvas shadows camera={{ position: [2.2, 1.4, 2.8], fov: 40 }} dpr={[1, 1.75]}>
            <ResizeFix />
            <SceneFromDoc />
          </Canvas>
        </div>
      </div>
    );
  }

  return (
    <>
      <div className="viewport-label">
        <span className="tag">
          {previewStatus.usingPlaceholder ? 'Preview Renderer (approx)' : 'BMD Preview'}
        </span>{' '}
        — {previewStatus.usingPlaceholder ? 'personagem placeholder' : 'meshes do Data'}
      </div>
      <div className="perf-hud">
        FPS {fps}
        <br />
        R3F / WebGL
      </div>
      <Canvas shadows camera={{ position: [2.2, 1.4, 2.8], fov: 40 }} dpr={[1, 2]}>
        <FpsReporter />
        <SceneFromDoc />
      </Canvas>
    </>
  );
}
