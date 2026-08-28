/** MuMain-compatible 3×4 bone math (ZzzMathLib). */

export type Mat34 = [
  [number, number, number, number],
  [number, number, number, number],
  [number, number, number, number],
];

export function identityMat34(): Mat34 {
  return [
    [1, 0, 0, 0],
    [0, 1, 0, 0],
    [0, 0, 1, 0],
  ];
}

/** Angles in radians → quaternion XYZW (W last), matching AngleQuaternion. */
export function angleQuaternion(angles: [number, number, number]): [number, number, number, number] {
  let angle = angles[2] * 0.5;
  const sy = Math.sin(angle);
  const cy = Math.cos(angle);
  angle = angles[1] * 0.5;
  const sp = Math.sin(angle);
  const cp = Math.cos(angle);
  angle = angles[0] * 0.5;
  const sr = Math.sin(angle);
  const cr = Math.cos(angle);

  return [
    sr * cp * cy - cr * sp * sy,
    cr * sp * cy + sr * cp * sy,
    cr * cp * sy - sr * sp * cy,
    cr * cp * cy + sr * sp * sy,
  ];
}

export function quaternionMatrix(q: [number, number, number, number]): Mat34 {
  const [x, y, z, w] = q;
  return [
    [
      1 - 2 * y * y - 2 * z * z,
      2 * x * y - 2 * w * z,
      2 * x * z + 2 * w * y,
      0,
    ],
    [
      2 * x * y + 2 * w * z,
      1 - 2 * x * x - 2 * z * z,
      2 * y * z - 2 * w * x,
      0,
    ],
    [
      2 * x * z - 2 * w * y,
      2 * y * z + 2 * w * x,
      1 - 2 * x * x - 2 * y * y,
      0,
    ],
  ];
}

export function concatTransforms(in1: Mat34, in2: Mat34): Mat34 {
  const out: Mat34 = [
    [0, 0, 0, 0],
    [0, 0, 0, 0],
    [0, 0, 0, 0],
  ];
  for (let i = 0; i < 3; i++) {
    out[i][0] = in1[i][0] * in2[0][0] + in1[i][1] * in2[1][0] + in1[i][2] * in2[2][0];
    out[i][1] = in1[i][0] * in2[0][1] + in1[i][1] * in2[1][1] + in1[i][2] * in2[2][1];
    out[i][2] = in1[i][0] * in2[0][2] + in1[i][1] * in2[1][2] + in1[i][2] * in2[2][2];
    out[i][3] =
      in1[i][0] * in2[0][3] + in1[i][1] * in2[1][3] + in1[i][2] * in2[2][3] + in1[i][3];
  }
  return out;
}

export function vectorTransform(v: [number, number, number], m: Mat34): [number, number, number] {
  return [
    v[0] * m[0][0] + v[1] * m[0][1] + v[2] * m[0][2] + m[0][3],
    v[0] * m[1][0] + v[1] * m[1][1] + v[2] * m[1][2] + m[1][3],
    v[0] * m[2][0] + v[1] * m[2][1] + v[2] * m[2][2] + m[2][3],
  ];
}

/** MU (Z-up) → Three.js (Y-up), scaled to ~meters. */
export const MU_TO_THREE_SCALE = 0.0125;

export function muToThree(v: [number, number, number], scale = MU_TO_THREE_SCALE): [number, number, number] {
  return [v[0] * scale, v[2] * scale, -v[1] * scale];
}
