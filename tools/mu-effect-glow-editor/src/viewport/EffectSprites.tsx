import { useMemo, useRef } from 'react';
import { useFrame } from '@react-three/fiber';
import * as THREE from 'three';
import type { EffectEntry } from '@/schema/itemEffects';
import { findBone } from '@/mu/bones';
import { createSoftSpriteTexture } from '@/mu/muTextures';

function blendToThree(blend: EffectEntry['blend']): THREE.Blending {
  switch (blend) {
    case 'additive':
    case 'bright':
    case 'screen':
      return THREE.AdditiveBlending;
    case 'multiply':
      return THREE.MultiplyBlending;
    default:
      return THREE.NormalBlending;
  }
}

/** Cap visual size so Mudream author sizes stay soft accents, not chest-covering quads. */
function visualSize(entry: EffectEntry): number {
  return Math.min(0.55, Math.max(0.08, entry.scale * entry.size * 0.28));
}

function EffectSprite({ entry, softMap }: { entry: EffectEntry; softMap: THREE.Texture }) {
  const ref = useRef<THREE.Mesh>(null);
  const bone = findBone(entry.bone) ?? findBone(entry.boneIndex);
  const base = bone?.position ?? ([0, 1.3, 0] as [number, number, number]);

  useFrame(({ clock, camera }) => {
    if (!ref.current) return;
    const t = clock.getElapsedTime();
    const pulse = 0.9 + Math.sin(t * 2.4 + entry.intensity) * 0.1;
    const s = visualSize(entry) * pulse;
    ref.current.scale.setScalar(s);
    ref.current.quaternion.copy(camera.quaternion);
  });

  if (!entry.enabled) return null;

  const pos: [number, number, number] = [
    base[0] + entry.position.x,
    base[1] + entry.position.y,
    base[2] + entry.position.z,
  ];

  return (
    <mesh ref={ref} position={pos} renderOrder={10}>
      <planeGeometry args={[1, 1]} />
      <meshBasicMaterial
        map={softMap}
        color={new THREE.Color(entry.color.r, entry.color.g, entry.color.b)}
        transparent
        opacity={Math.min(0.55, 0.18 + entry.intensity * 0.22)}
        blending={blendToThree(entry.blend)}
        depthWrite={false}
        side={THREE.DoubleSide}
      />
    </mesh>
  );
}

function ParticleBurst({ entry }: { entry: EffectEntry }) {
  const points = useRef<THREE.Points>(null);
  const bone = findBone(entry.bone) ?? findBone(entry.boneIndex);
  const base = bone?.position ?? ([0, 1.3, 0] as [number, number, number]);
  const p = entry.particles;

  const { positions, velocities } = useMemo(() => {
    const count = Math.max(1, Math.min(48, p.count));
    const positions = new Float32Array(count * 3);
    const velocities = new Float32Array(count * 3);
    for (let i = 0; i < count; i++) {
      positions[i * 3] = (Math.random() - 0.5) * p.spread;
      positions[i * 3 + 1] = Math.random() * p.spread;
      positions[i * 3 + 2] = (Math.random() - 0.5) * p.spread;
      velocities[i * 3] = (Math.random() - 0.5) * p.speed;
      velocities[i * 3 + 1] = Math.random() * p.speed;
      velocities[i * 3 + 2] = (Math.random() - 0.5) * p.speed;
    }
    return { positions, velocities };
  }, [p.count, p.spread, p.speed]);

  useFrame((_, delta) => {
    if (!points.current || !p.enabled) return;
    const attr = points.current.geometry.getAttribute('position') as THREE.BufferAttribute;
    const arr = attr.array as Float32Array;
    for (let i = 0; i < Math.min(p.count, arr.length / 3); i++) {
      arr[i * 3] += velocities[i * 3] * delta;
      arr[i * 3 + 1] += velocities[i * 3 + 1] * delta;
      arr[i * 3 + 2] += velocities[i * 3 + 2] * delta;
      if (arr[i * 3 + 1] > p.lifetime) {
        arr[i * 3] = (Math.random() - 0.5) * p.spread;
        arr[i * 3 + 1] = 0;
        arr[i * 3 + 2] = (Math.random() - 0.5) * p.spread;
      }
    }
    attr.needsUpdate = true;
  });

  if (!entry.enabled || !p.enabled) return null;

  const pos: [number, number, number] = [
    base[0] + entry.position.x,
    base[1] + entry.position.y,
    base[2] + entry.position.z,
  ];

  return (
    <points ref={points} position={pos} renderOrder={11}>
      <bufferGeometry>
        <bufferAttribute attach="attributes-position" args={[positions, 3]} />
      </bufferGeometry>
      <pointsMaterial
        size={Math.min(0.08, p.size)}
        color={new THREE.Color(p.color.r, p.color.g, p.color.b)}
        transparent
        opacity={0.65}
        depthWrite={false}
        blending={THREE.AdditiveBlending}
        sizeAttenuation
      />
    </points>
  );
}

export function EffectSprites({ effects }: { effects: EffectEntry[] }) {
  const softMap = useMemo(() => createSoftSpriteTexture(64), []);
  return (
    <group>
      {effects.map((e) => (
        <group key={e.id}>
          <EffectSprite entry={e} softMap={softMap} />
          <ParticleBurst entry={e} />
        </group>
      ))}
    </group>
  );
}
