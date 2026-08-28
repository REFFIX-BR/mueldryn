import { mapFileDecrypt } from './mapFileDecrypt';
import {
  angleQuaternion,
  concatTransforms,
  identityMat34,
  quaternionMatrix,
  type Mat34,
} from './bmdMath';

export interface BmdVertex {
  node: number;
  position: [number, number, number];
}

export interface BmdNormal {
  node: number;
  normal: [number, number, number];
  bindVertex: number;
}

export interface BmdTexCoord {
  u: number;
  v: number;
}

export interface BmdTriangle {
  polygon: number;
  vertexIndex: [number, number, number, number];
  normalIndex: [number, number, number, number];
  texCoordIndex: [number, number, number, number];
  front: boolean;
}

export interface BmdMesh {
  textureIndex: number;
  vertices: BmdVertex[];
  normals: BmdNormal[];
  texCoords: BmdTexCoord[];
  triangles: BmdTriangle[];
  textureName: string;
}

export interface BmdBone {
  dummy: boolean;
  name: string;
  parent: number;
  /** Per-action keyframe positions / rotations (radians). */
  positions: [number, number, number][][];
  rotations: [number, number, number][][];
}

export interface BmdModel {
  name: string;
  version: number;
  meshes: BmdMesh[];
  bones: BmdBone[];
  /** Keys per action */
  actionKeys: number[];
  lockPositions: boolean[];
}

class Reader {
  private view: DataView;
  ptr: number;

  constructor(
    readonly bytes: Uint8Array,
    start = 0,
  ) {
    this.view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
    this.ptr = start;
  }

  u8() {
    return this.bytes[this.ptr++];
  }

  i16() {
    const v = this.view.getInt16(this.ptr, true);
    this.ptr += 2;
    return v;
  }

  f32() {
    const v = this.view.getFloat32(this.ptr, true);
    this.ptr += 4;
    return v;
  }

  bool() {
    return this.u8() !== 0;
  }

  fixedAscii(len: number) {
    const slice = this.bytes.subarray(this.ptr, this.ptr + len);
    this.ptr += len;
    let end = slice.indexOf(0);
    if (end < 0) end = len;
    return new TextDecoder('ascii').decode(slice.subarray(0, end)).trim();
  }

  skip(n: number) {
    this.ptr += n;
  }

  vec3(): [number, number, number] {
    return [this.f32(), this.f32(), this.f32()];
  }
}

/**
 * Parse MuOnline BMD (v10 / v12) matching MuMain BMD::Open2.
 */
export function parseBmd(fileData: Uint8Array): BmdModel {
  if (fileData.length < 4 || fileData[0] !== 0x42 || fileData[1] !== 0x4d || fileData[2] !== 0x44) {
    throw new Error('Invalid BMD magic');
  }
  const version = fileData[3];
  let data: Uint8Array;
  let ptr: number;

  if (version === 0x0c) {
    const encSize = new DataView(fileData.buffer, fileData.byteOffset, fileData.byteLength).getInt32(
      4,
      true,
    );
    if (encSize <= 0 || 8 + encSize > fileData.length) throw new Error('Invalid BMD v12 size');
    data = mapFileDecrypt(fileData.subarray(8, 8 + encSize));
    ptr = 0;
  } else if (version === 0x0a) {
    data = fileData;
    ptr = 4;
  } else {
    throw new Error(`Unsupported BMD version ${version}`);
  }

  const r = new Reader(data, ptr);
  const name = r.fixedAscii(32);
  const numMeshs = r.i16();
  const numBones = r.i16();
  const numActions = r.i16();
  if (numMeshs < 0 || numMeshs > 512 || numBones < 0 || numBones > 512) {
    throw new Error(`Suspicious mesh/bone counts: ${numMeshs}/${numBones}`);
  }

  const meshes: BmdMesh[] = [];
  for (let i = 0; i < numMeshs; i++) {
    const numVertices = r.i16();
    const numNormals = r.i16();
    const numTexCoords = r.i16();
    const numTriangles = r.i16();
    const textureIndex = r.i16();

    const vertices: BmdVertex[] = [];
    for (let v = 0; v < numVertices; v++) {
      const node = r.i16();
      r.skip(2); // pad to 16-byte Vertex_t
      vertices.push({ node, position: r.vec3() });
    }

    const normals: BmdNormal[] = [];
    for (let n = 0; n < numNormals; n++) {
      const node = r.i16();
      r.skip(2);
      const normal = r.vec3();
      const bindVertex = r.i16();
      r.skip(2);
      normals.push({ node, normal, bindVertex });
    }

    const texCoords: BmdTexCoord[] = [];
    for (let t = 0; t < numTexCoords; t++) {
      texCoords.push({ u: r.f32(), v: r.f32() });
    }

    const triangles: BmdTriangle[] = [];
    for (let t = 0; t < numTriangles; t++) {
      const start = r.ptr;
      const polygon = r.u8();
      // align? Triangle_t often has padding after char — MuMain memcpy Triangle_t then skip Triangle_t2 (64)
      // Layout used by community loaders / BmdMeshPlugin stride 64:
      // After reading Triangle_t fields from start, advance exactly 64.
      r.ptr = start + 1;
      // short arrays may be unaligned after char — MSVC packs with pad
      // Match MuMain: copy sizeof(Triangle_t) then ptr += sizeof(Triangle_t2)=64
      // Practical layout (64 bytes):
      // polygon(1) + pad(1) + vertex[4]i16 + normal[4]i16 + tex[4]i16 + edge[4]i16 + front(1) + pad + lightmap...
      r.skip(1); // pad
      const vertexIndex: [number, number, number, number] = [r.i16(), r.i16(), r.i16(), r.i16()];
      const normalIndex: [number, number, number, number] = [r.i16(), r.i16(), r.i16(), r.i16()];
      const texCoordIndex: [number, number, number, number] = [r.i16(), r.i16(), r.i16(), r.i16()];
      r.skip(8); // EdgeTriangleIndex[4]
      const front = r.bool();
      r.ptr = start + 64;
      triangles.push({ polygon, vertexIndex, normalIndex, texCoordIndex, front });
    }

    const textureName = r.fixedAscii(32);
    meshes.push({ textureIndex, vertices, normals, texCoords, triangles, textureName });
  }

  const actionKeys: number[] = [];
  const lockPositions: boolean[] = [];
  for (let i = 0; i < numActions; i++) {
    const keys = r.i16();
    const lock = r.bool();
    actionKeys.push(keys);
    lockPositions.push(lock);
    if (lock && keys > 0) r.skip(keys * 12);
  }

  const bones: BmdBone[] = [];
  for (let i = 0; i < numBones; i++) {
    const dummy = r.u8() !== 0;
    if (dummy) {
      bones.push({ dummy: true, name: '', parent: -1, positions: [], rotations: [] });
      continue;
    }
    const boneName = r.fixedAscii(32);
    const parent = r.i16();
    const positions: [number, number, number][][] = [];
    const rotations: [number, number, number][][] = [];
    for (let a = 0; a < numActions; a++) {
      const keys = actionKeys[a];
      const posKeys: [number, number, number][] = [];
      const rotKeys: [number, number, number][] = [];
      if (keys > 0) {
        for (let k = 0; k < keys; k++) posKeys.push(r.vec3());
        for (let k = 0; k < keys; k++) rotKeys.push(r.vec3());
      }
      positions.push(posKeys);
      rotations.push(rotKeys);
    }
    bones.push({ dummy: false, name: boneName, parent, positions, rotations });
  }

  return { name, version, meshes, bones, actionKeys, lockPositions };
}

/** Compute rest-pose world bone matrices (action 0, frame 0). */
export function computeRestBoneMatrices(model: BmdModel): Mat34[] {
  const n = model.bones.length;
  const out: Mat34[] = Array.from({ length: n }, () => identityMat34());
  const parentRoot = identityMat34();

  for (let i = 0; i < n; i++) {
    const b = model.bones[i];
    if (b.dummy) {
      out[i] = identityMat34();
      continue;
    }
    const pos = b.positions[0]?.[0] ?? ([0, 0, 0] as [number, number, number]);
    const rot = b.rotations[0]?.[0] ?? ([0, 0, 0] as [number, number, number]);
    const q = angleQuaternion(rot);
    const local = quaternionMatrix(q);
    local[0][3] = pos[0];
    local[1][3] = pos[1];
    local[2][3] = pos[2];

    if (b.parent < 0) {
      out[i] = concatTransforms(parentRoot, local);
    } else {
      out[i] = concatTransforms(out[b.parent] ?? identityMat34(), local);
    }
  }
  return out;
}
