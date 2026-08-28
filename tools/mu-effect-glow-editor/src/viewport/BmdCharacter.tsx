import { useEffect, useMemo, useRef, useState } from 'react';
import { useFrame } from '@react-three/fiber';
import * as THREE from 'three';
import type { EquipmentSlot, GlowConfig, ItemRef, LoadoutState } from '@/schema/itemEffects';
import { itemKey } from '@/schema/itemEffects';
import { disposeObject3D, loadItemBmd } from '@/mu/bmdMeshFactory';
import { getDataRootState, subscribeDataRoot } from '@/mu/dataRoot';
import { PLACEHOLDER_BONES } from '@/mu/bones';

const SLOT_ORDER: EquipmentSlot[] = [
  'boots',
  'pants',
  'armor',
  'gloves',
  'helm',
  'weapon',
  'offhand',
  'wings',
  'cape',
];

export interface BmdCharacterProps {
  loadout: LoadoutState;
  items: Record<string, { glow: GlowConfig; item: ItemRef }>;
  animClip: string;
  animPlaying: boolean;
  animSpeed: number;
  showBones?: boolean;
  muted?: boolean;
  onStatus?: (status: BmdLoadStatus) => void;
}

export interface BmdLoadStatus {
  loading: boolean;
  loadedParts: number;
  failedParts: number;
  usingPlaceholder: boolean;
  message: string;
}

export function BmdCharacter({
  loadout,
  items,
  animClip,
  animPlaying,
  animSpeed,
  showBones,
  muted,
  onStatus,
}: BmdCharacterProps) {
  const root = useRef<THREE.Group>(null);
  const [parts, setParts] = useState<THREE.Group[]>([]);
  const [dataEpoch, setDataEpoch] = useState(0);

  const loadKey = useMemo(() => {
    const slots = SLOT_ORDER.map((s) => {
      const r = loadout.slots[s];
      return r ? `${s}:${r.group}:${r.index}` : `${s}:-`;
    }).join('|');
    return `${slots}|L${loadout.itemLevel}|m${muted ? 1 : 0}`;
  }, [loadout, muted]);

  useEffect(() => subscribeDataRoot(() => setDataEpoch((n) => n + 1)), []);

  useEffect(() => {
    let cancelled = false;
    const spawned: THREE.Group[] = [];

    const run = async () => {
      const data = getDataRootState();
      if (!data.ready) {
        setParts([]);
        onStatus?.({
          loading: false,
          loadedParts: 0,
          failedParts: 0,
          usingPlaceholder: true,
          message: 'Sem pasta Data — manequim placeholder',
        });
        return;
      }

      onStatus?.({
        loading: true,
        loadedParts: 0,
        failedParts: 0,
        usingPlaceholder: true,
        message: 'Carregando BMDs…',
      });

      let loaded = 0;
      let failed = 0;
      let textured = 0;

      for (const slot of SLOT_ORDER) {
        const ref = loadout.slots[slot];
        if (!ref) continue;
        const glow = items[itemKey(ref)]?.glow;
        try {
          const part = await loadItemBmd(ref, {
            glow,
            level: loadout.itemLevel,
            muted,
            slot,
          });
          if (cancelled) {
            if (part) disposeObject3D(part.group);
            return;
          }
          if (part) {
            spawned.push(part.group);
            loaded++;
            textured += part.texturedMeshes;
          } else {
            failed++;
          }
        } catch {
          failed++;
        }
      }

      if (cancelled) {
        for (const g of spawned) disposeObject3D(g);
        return;
      }

      setParts(spawned);
      onStatus?.({
        loading: false,
        loadedParts: loaded,
        failedParts: failed,
        usingPlaceholder: loaded === 0,
        message:
          loaded > 0
            ? `BMD ok — ${loaded} peça(s), ${textured} mesh(es) texturizado(s)${failed ? `, ${failed} falha(s)` : ''}`
            : 'Nenhum BMD encontrado para o loadout',
      });
    };

    void run();

    return () => {
      cancelled = true;
      for (const g of spawned) disposeObject3D(g);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [loadKey, dataEpoch]);

  useFrame(({ clock }) => {
    if (!root.current) return;
    const t = clock.getElapsedTime() * (animPlaying ? animSpeed : 0);
    const bob =
      animClip === 'idle'
        ? Math.sin(t * 2) * 0.01
        : animClip === 'walk'
          ? Math.sin(t * 6) * 0.03
          : animClip === 'run'
            ? Math.sin(t * 10) * 0.045
            : 0;
    root.current.position.y = bob;
  });

  useEffect(() => {
    return () => {
      for (const g of parts) disposeObject3D(g);
    };
  }, [parts]);

  return (
    <group ref={root}>
      {parts.map((g, i) => (
        <primitive key={`${g.name}-${i}`} object={g} />
      ))}
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
