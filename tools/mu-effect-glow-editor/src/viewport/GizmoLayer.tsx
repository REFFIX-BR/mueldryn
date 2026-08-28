import { Html } from '@react-three/drei';
import { findBone } from '@/mu/bones';
import type { EffectEntry } from '@/schema/itemEffects';

/** Lightweight attachment markers — not TransformControls (keeps MVP stable). */
export function GizmoLayer({
  effect,
  visible,
}: {
  effect: EffectEntry | null;
  visible: boolean;
}) {
  if (!visible || !effect) return null;
  const bone = findBone(effect.bone) ?? findBone(effect.boneIndex);
  const base = bone?.position ?? [0, 1.3, 0];
  const pos: [number, number, number] = [
    base[0] + effect.position.x,
    base[1] + effect.position.y,
    base[2] + effect.position.z,
  ];

  return (
    <group position={pos}>
      <axesHelper args={[0.35]} />
      <mesh>
        <sphereGeometry args={[0.04, 12, 12]} />
        <meshBasicMaterial color="#4a8fd4" />
      </mesh>
      <Html distanceFactor={6} style={{ pointerEvents: 'none' }}>
        <div
          style={{
            background: 'rgba(14,17,22,0.85)',
            border: '1px solid #2a3444',
            color: '#d7dee8',
            padding: '2px 6px',
            fontSize: 10,
            whiteSpace: 'nowrap',
            borderRadius: 3,
            fontFamily: 'IBM Plex Mono, monospace',
          }}
        >
          {effect.name} @ {effect.bone}
        </div>
      </Html>
    </group>
  );
}
