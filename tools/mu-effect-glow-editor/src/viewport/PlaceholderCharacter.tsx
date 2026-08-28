import { useMemo, useRef } from 'react';
import { useFrame } from '@react-three/fiber';
import * as THREE from 'three';
import type { EquipmentSlot, GlowConfig, ItemRef, LoadoutState, Rgb } from '@/schema/itemEffects';
import { itemKey } from '@/schema/itemEffects';
import { PLACEHOLDER_BONES } from '@/mu/bones';

const SLOT_COLORS: Record<EquipmentSlot, string> = {
  helm: '#6a7380',
  armor: '#5a6574',
  pants: '#4e5866',
  gloves: '#667288',
  boots: '#3f4754',
  weapon: '#8a909a',
  offhand: '#7a808a',
  wings: '#9aa3b0',
  cape: '#555e6c',
};

function applyGlowColor(
  base: string,
  glow: GlowConfig | undefined,
  level: number,
  hasItem: boolean,
): THREE.Color {
  if (!hasItem) return new THREE.Color('#2a3038');
  if (!glow?.enabled) return new THREE.Color(base);
  const lv = glow.byLevel.find((e) => e.level === level) ?? glow.byLevel[level];
  const c: Rgb = lv?.color ?? glow.baseColor;
  const intensity = (lv?.intensity ?? glow.baseIntensity) * 0.35;
  return new THREE.Color(c.r, c.g, c.b).lerp(new THREE.Color(base), 1 - Math.min(1, intensity));
}

function emissiveFromGlow(glow: GlowConfig | undefined, level: number): THREE.Color {
  if (!glow?.enabled) return new THREE.Color(0, 0, 0);
  const lv = glow.byLevel.find((e) => e.level === level);
  const c = lv?.color ?? glow.baseColor;
  const e = Math.min(0.18, (lv?.emissive ?? glow.baseIntensity * 0.5) * 0.15);
  // Soft accent only — placeholder must not look like solid orange
  return new THREE.Color(c.r * e, c.g * e, bClamp(c.b * e));
}

function bClamp(v: number) {
  return Math.min(0.85, Math.max(0, v));
}

interface PartProps {
  position: [number, number, number];
  args: [number, number, number] | [number, number];
  color: THREE.Color;
  emissive: THREE.Color;
  shape?: 'box' | 'capsule' | 'sphere';
  rotation?: [number, number, number];
}

function ArmorPart({
  position,
  args,
  color,
  emissive,
  shape = 'box',
  rotation = [0, 0, 0],
}: PartProps) {
  return (
    <mesh position={position} rotation={rotation} castShadow receiveShadow>
      {shape === 'sphere' ? (
        <sphereGeometry args={args as [number, number]} />
      ) : shape === 'capsule' ? (
        <capsuleGeometry args={args as [number, number]} />
      ) : (
        <boxGeometry args={args as [number, number, number]} />
      )}
      <meshStandardMaterial
        color={color}
        emissive={emissive}
        emissiveIntensity={1}
        metalness={0.35}
        roughness={0.55}
      />
    </mesh>
  );
}

export interface PlaceholderCharacterProps {
  loadout: LoadoutState;
  items: Record<string, { glow: GlowConfig; item: ItemRef }>;
  animClip: string;
  animPlaying: boolean;
  animSpeed: number;
  showBones?: boolean;
  /** Dim / desaturate for SOURCE compare pane */
  muted?: boolean;
}

export function PlaceholderCharacter({
  loadout,
  items,
  animClip,
  animPlaying,
  animSpeed,
  showBones,
  muted,
}: PlaceholderCharacterProps) {
  const root = useRef<THREE.Group>(null);
  const wingL = useRef<THREE.Group>(null);
  const wingR = useRef<THREE.Group>(null);
  const weapon = useRef<THREE.Group>(null);

  const level = loadout.itemLevel;

  const parts = useMemo(() => {
    const glowOf = (slot: EquipmentSlot) => {
      const ref = loadout.slots[slot];
      if (!ref) return undefined;
      return items[itemKey(ref)]?.glow;
    };
    const has = (slot: EquipmentSlot) => !!loadout.slots[slot];

    const mk = (slot: EquipmentSlot) => ({
      color: applyGlowColor(SLOT_COLORS[slot], glowOf(slot), level, has(slot)),
      emissive: muted
        ? new THREE.Color(0, 0, 0)
        : emissiveFromGlow(glowOf(slot), level).multiplyScalar(has(slot) ? 1 : 0),
      equipped: has(slot),
    });

    return {
      helm: mk('helm'),
      armor: mk('armor'),
      pants: mk('pants'),
      gloves: mk('gloves'),
      boots: mk('boots'),
      weapon: mk('weapon'),
      offhand: mk('offhand'),
      wings: mk('wings'),
      cape: mk('cape'),
    };
  }, [loadout, items, level, muted]);

  useFrame(({ clock }) => {
    if (!root.current) return;
    const t = clock.getElapsedTime() * (animPlaying ? animSpeed : 0);
    const bob =
      animClip === 'idle'
        ? Math.sin(t * 2) * 0.015
        : animClip === 'walk'
          ? Math.sin(t * 6) * 0.04
          : animClip === 'run'
            ? Math.sin(t * 10) * 0.06
            : animClip === 'attack'
              ? Math.sin(t * 8) * 0.02
              : 0;
    root.current.position.y = bob;

    if (wingL.current && wingR.current) {
      const flap = Math.sin(t * (animClip === 'idle' ? 1.5 : 3)) * 0.15;
      wingL.current.rotation.z = 0.35 + flap;
      wingR.current.rotation.z = -0.35 - flap;
      wingL.current.rotation.y = 0.2;
      wingR.current.rotation.y = -0.2;
    }

    if (weapon.current && animClip === 'attack' && animPlaying) {
      weapon.current.rotation.x = Math.sin(t * 10) * 0.6 - 0.2;
    } else if (weapon.current) {
      weapon.current.rotation.x = -0.2;
    }
  });

  return (
    <group ref={root}>
      {/* Body core */}
      <ArmorPart
        position={[0, 1.35, 0]}
        args={[0.38, 0.55, 0.22]}
        color={parts.armor.color}
        emissive={parts.armor.emissive}
      />
      <ArmorPart
        position={[0, 0.95, 0]}
        args={[0.34, 0.35, 0.2]}
        color={parts.pants.color}
        emissive={parts.pants.emissive}
      />
      {/* Head / helm */}
      <ArmorPart
        position={[0, 1.78, 0]}
        args={[0.16, 16, 12]}
        shape="sphere"
        color={parts.helm.color}
        emissive={parts.helm.emissive}
      />
      {/* Arms */}
      <ArmorPart
        position={[0.32, 1.25, 0]}
        args={[0.07, 0.28]}
        shape="capsule"
        color={parts.gloves.color}
        emissive={parts.gloves.emissive}
      />
      <ArmorPart
        position={[-0.32, 1.25, 0]}
        args={[0.07, 0.28]}
        shape="capsule"
        color={parts.gloves.color}
        emissive={parts.gloves.emissive}
      />
      {/* Legs */}
      <ArmorPart
        position={[0.12, 0.45, 0]}
        args={[0.08, 0.35]}
        shape="capsule"
        color={parts.boots.color}
        emissive={parts.boots.emissive}
      />
      <ArmorPart
        position={[-0.12, 0.45, 0]}
        args={[0.08, 0.35]}
        shape="capsule"
        color={parts.boots.color}
        emissive={parts.boots.emissive}
      />
      <ArmorPart
        position={[0.12, 0.08, 0.04]}
        args={[0.12, 0.06, 0.2]}
        color={parts.boots.color}
        emissive={parts.boots.emissive}
      />
      <ArmorPart
        position={[-0.12, 0.08, 0.04]}
        args={[0.12, 0.06, 0.2]}
        color={parts.boots.color}
        emissive={parts.boots.emissive}
      />

      {/* Weapon */}
      {parts.weapon.equipped && (
        <group ref={weapon} position={[-0.55, 1.1, 0.1]}>
          <ArmorPart
            position={[0, 0.25, 0]}
            args={[0.04, 0.7, 0.06]}
            color={parts.weapon.color}
            emissive={parts.weapon.emissive}
            rotation={[0.2, 0, 0.15]}
          />
        </group>
      )}

      {/* Offhand */}
      {parts.offhand.equipped && (
        <ArmorPart
          position={[0.52, 1.1, 0.08]}
          args={[0.22, 0.28, 0.05]}
          color={parts.offhand.color}
          emissive={parts.offhand.emissive}
          rotation={[0.1, 0.4, 0]}
        />
      )}

      {/* Wings — careful: local emissive only, not full-body wash */}
      {parts.wings.equipped && (
        <group position={[0, 1.4, -0.22]}>
          <group ref={wingL} position={[0.08, 0, 0]}>
            <ArmorPart
              position={[0.45, 0.15, 0]}
              args={[0.75, 0.08, 0.35]}
              color={parts.wings.color}
              emissive={parts.wings.emissive}
              rotation={[0.1, 0.15, 0.4]}
            />
          </group>
          <group ref={wingR} position={[-0.08, 0, 0]}>
            <ArmorPart
              position={[-0.45, 0.15, 0]}
              args={[0.75, 0.08, 0.35]}
              color={parts.wings.color}
              emissive={parts.wings.emissive}
              rotation={[0.1, -0.15, -0.4]}
            />
          </group>
        </group>
      )}

      {/* Cape */}
      {parts.cape.equipped && (
        <ArmorPart
          position={[0, 1.2, -0.28]}
          args={[0.4, 0.7, 0.04]}
          color={parts.cape.color}
          emissive={parts.cape.emissive}
          rotation={[0.25, 0, 0]}
        />
      )}

      {showBones &&
        PLACEHOLDER_BONES.map((b) => (
          <mesh key={b.name} position={b.position}>
            <sphereGeometry args={[0.03, 8, 8]} />
            <meshBasicMaterial color="#4a8fd4" transparent opacity={0.85} />
          </mesh>
        ))}
    </group>
  );
}
