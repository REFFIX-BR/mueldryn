import { useEffect } from 'react';
import { OrbitControls } from '@react-three/drei';
import { useThree } from '@react-three/fiber';
import type { CameraPreset } from '@/schema/itemEffects';

const PRESETS: Record<
  CameraPreset,
  { position: [number, number, number]; target: [number, number, number] }
> = {
  fullBody: { position: [2.2, 1.4, 2.8], target: [0, 1.0, 0] },
  upper: { position: [1.4, 1.7, 1.8], target: [0, 1.45, 0] },
  weapon: { position: [-1.6, 1.3, 1.2], target: [-0.45, 1.1, 0.1] },
  wings: { position: [0.2, 1.6, -2.6], target: [0, 1.4, -0.2] },
};

export function CameraController({ preset }: { preset: CameraPreset }) {
  const { camera } = useThree();

  useEffect(() => {
    const p = PRESETS[preset];
    camera.position.set(...p.position);
    camera.lookAt(...p.target);
    camera.updateProjectionMatrix();
  }, [preset, camera]);

  const target = PRESETS[preset].target;

  return (
    <OrbitControls
      makeDefault
      target={target}
      enablePan
      enableZoom
      enableRotate
      minDistance={0.8}
      maxDistance={8}
      maxPolarAngle={Math.PI * 0.92}
    />
  );
}
