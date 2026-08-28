import { useCallback, useState } from 'react';
import { PlaceholderCharacter } from './PlaceholderCharacter';
import { BmdCharacter, type BmdLoadStatus } from './BmdCharacter';
import { EffectSprites } from './EffectSprites';
import { CameraController } from './CameraController';
import { GizmoLayer } from './GizmoLayer';
import { ContactShadows, Grid } from '@react-three/drei';
import type {
  AnimClip,
  CameraPreset,
  EffectEntry,
  ItemEffectConfig,
  ItemEffectsDocument,
  LoadoutState,
} from '@/schema/itemEffects';
import { itemKey } from '@/schema/itemEffects';
import { useEditorStore } from '@/state/store';

export interface CharacterSceneProps {
  loadout: LoadoutState;
  items: ItemEffectsDocument['items'];
  cameraPreset: CameraPreset;
  animClip: AnimClip;
  animPlaying: boolean;
  animSpeed: number;
  showBones: boolean;
  showGizmo: boolean;
  selectedEffect: EffectEntry | null;
  muted?: boolean;
  effectsOverride?: EffectEntry[];
}

function collectEffects(
  loadout: LoadoutState,
  items: Record<string, ItemEffectConfig>,
): EffectEntry[] {
  const out: EffectEntry[] = [];
  for (const ref of Object.values(loadout.slots)) {
    if (!ref) continue;
    const cfg = items[itemKey(ref)];
    if (!cfg) continue;
    out.push(...cfg.effects.filter((e) => e.enabled));
  }
  return out;
}

export function CharacterScene({
  loadout,
  items,
  cameraPreset,
  animClip,
  animPlaying,
  animSpeed,
  showBones,
  showGizmo,
  selectedEffect,
  muted,
  effectsOverride,
}: CharacterSceneProps) {
  const effects = effectsOverride ?? collectEffects(loadout, items);
  const setPreviewStatus = useEditorStore((s) => s.setPreviewStatus);
  const [localStatus, setLocalStatus] = useState<BmdLoadStatus | null>(null);

  const onStatus = useCallback(
    (status: BmdLoadStatus) => {
      setLocalStatus(status);
      if (!muted) setPreviewStatus(status);
    },
    [muted, setPreviewStatus],
  );

  const showPlaceholder = !localStatus || localStatus.usingPlaceholder;

  return (
    <>
      <color attach="background" args={[muted ? '#050506' : '#000000']} />
      <fog attach="fog" args={[muted ? '#050506' : '#000000', 8, 18]} />
      <ambientLight intensity={0.55} />
      <directionalLight
        position={[3.2, 5.5, 2.4]}
        intensity={1.45}
        castShadow
        shadow-mapSize={[1024, 1024]}
      />
      <directionalLight position={[-2.5, 2.2, -2.5]} intensity={0.45} color="#a8b8d0" />
      <directionalLight position={[0, 1.5, 4]} intensity={0.35} color="#ffd8c8" />
      <hemisphereLight args={['#7a8aa0', '#101418', 0.5]} />

      <CameraController preset={cameraPreset} />

      <BmdCharacter
        loadout={loadout}
        items={items}
        animClip={animClip}
        animPlaying={animPlaying}
        animSpeed={animSpeed}
        showBones={showBones && !showPlaceholder}
        muted={muted}
        onStatus={onStatus}
      />

      {showPlaceholder && (
        <PlaceholderCharacter
          loadout={loadout}
          items={items}
          animClip={animClip}
          animPlaying={animPlaying}
          animSpeed={animSpeed}
          showBones={showBones}
          muted={muted}
        />
      )}

      {!muted && <EffectSprites effects={effects} />}
      {muted && effectsOverride && <EffectSprites effects={effectsOverride} />}

      <GizmoLayer effect={selectedEffect} visible={showGizmo && !muted} />

      <Grid
        position={[0, 0, 0]}
        args={[12, 12]}
        cellSize={0.5}
        cellThickness={0.6}
        cellColor="#1e2633"
        sectionSize={2}
        sectionThickness={1}
        sectionColor="#2a3648"
        fadeDistance={10}
        infiniteGrid
      />
      <ContactShadows opacity={0.45} scale={8} blur={2.5} far={4} />
    </>
  );
}
