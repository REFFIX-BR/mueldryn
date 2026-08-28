/**
 * Generic humanoid bone map for placeholder attachment.
 * Indices approximate MU-style bone lists; remap when BMD lands.
 */
export interface BoneDef {
  name: string;
  index: number;
  /** Local offset from character root (Y-up, meters-ish) */
  position: [number, number, number];
  parent?: string;
}

export const PLACEHOLDER_BONES: BoneDef[] = [
  { name: 'root', index: 0, position: [0, 0, 0] },
  { name: 'pelvis', index: 1, position: [0, 0.95, 0], parent: 'root' },
  { name: 'spine', index: 20, position: [0, 1.25, 0], parent: 'pelvis' },
  { name: 'chest', index: 21, position: [0, 1.45, 0], parent: 'spine' },
  { name: 'neck', index: 22, position: [0, 1.62, 0], parent: 'chest' },
  { name: 'head', index: 23, position: [0, 1.78, 0], parent: 'neck' },
  { name: 'shoulder_l', index: 30, position: [0.22, 1.48, 0], parent: 'chest' },
  { name: 'elbow_l', index: 31, position: [0.42, 1.25, 0], parent: 'shoulder_l' },
  { name: 'hand_l', index: 32, position: [0.55, 1.05, 0.05], parent: 'elbow_l' },
  { name: 'shoulder_r', index: 40, position: [-0.22, 1.48, 0], parent: 'chest' },
  { name: 'elbow_r', index: 41, position: [-0.42, 1.25, 0], parent: 'shoulder_r' },
  { name: 'hand_r', index: 42, position: [-0.55, 1.05, 0.05], parent: 'elbow_r' },
  { name: 'hip_l', index: 50, position: [0.12, 0.95, 0], parent: 'pelvis' },
  { name: 'knee_l', index: 51, position: [0.14, 0.55, 0], parent: 'hip_l' },
  { name: 'foot_l', index: 52, position: [0.14, 0.08, 0.04], parent: 'knee_l' },
  { name: 'hip_r', index: 53, position: [-0.12, 0.95, 0], parent: 'pelvis' },
  { name: 'knee_r', index: 54, position: [-0.14, 0.55, 0], parent: 'hip_r' },
  { name: 'foot_r', index: 55, position: [-0.14, 0.08, 0.04], parent: 'knee_r' },
  { name: 'weapon', index: 60, position: [-0.62, 1.1, 0.15], parent: 'hand_r' },
  { name: 'offhand', index: 61, position: [0.58, 1.1, 0.1], parent: 'hand_l' },
  { name: 'wing_l', index: 70, position: [0.35, 1.4, -0.2], parent: 'chest' },
  { name: 'wing_r', index: 71, position: [-0.35, 1.4, -0.2], parent: 'chest' },
  { name: 'wing_root', index: 72, position: [0, 1.42, -0.25], parent: 'chest' },
  { name: 'cape', index: 80, position: [0, 1.35, -0.3], parent: 'chest' },
];

export function findBone(nameOrIndex: string | number): BoneDef | undefined {
  if (typeof nameOrIndex === 'number') {
    return PLACEHOLDER_BONES.find((b) => b.index === nameOrIndex);
  }
  return (
    PLACEHOLDER_BONES.find((b) => b.name === nameOrIndex) ??
    PLACEHOLDER_BONES.find((b) => String(b.index) === nameOrIndex)
  );
}
