import * as THREE from 'three';
import {
  computeRestBoneMatrices,
  parseBmd,
  type BmdModel,
} from './bmdParser';
import { muToThree, vectorTransform } from './bmdMath';
import { bytesToTexture, isGlowTextureName, textureSearchNames } from './muTextures';
import { findTextureBytes, readDataFile } from './dataRoot';
import { resolveBmdCandidates } from './itemCatalog';
import type { EquipmentSlot, GlowConfig, ItemRef, Rgb } from '@/schema/itemEffects';
import { PLACEHOLDER_BONES } from './bones';

export interface LoadedBmdPart {
  group: THREE.Group;
  model: BmdModel;
  path: string;
  texturedMeshes: number;
  glowMeshes: number;
}

/** World pose for non-body BMD parts (weapon/wings) — approx Mu player attach. */
export function slotAttachTransform(slot: EquipmentSlot): {
  position: [number, number, number];
  rotation: [number, number, number];
  scale: number;
} {
  const bone = (name: string) => PLACEHOLDER_BONES.find((b) => b.name === name)?.position;
  switch (slot) {
    case 'weapon':
      return {
        position: bone('weapon') ?? [-0.55, 1.08, 0.12],
        rotation: [0.55, 0.15, -1.15],
        scale: 1,
      };
    case 'offhand':
      return {
        position: bone('offhand') ?? [0.55, 1.08, 0.1],
        rotation: [0.4, -0.35, 0.9],
        scale: 1,
      };
    case 'wings':
      return {
        position: bone('wing_root') ?? [0, 1.38, -0.22],
        rotation: [0.05, 0, 0],
        scale: 1,
      };
    case 'cape':
      return {
        position: bone('cape') ?? [0, 1.32, -0.28],
        rotation: [0.15, 0, 0],
        scale: 1,
      };
    default:
      // Armor / helm / pants / gloves / boots share Bip01 skeleton — origin is correct.
      return { position: [0, 0, 0], rotation: [0, 0, 0], scale: 1 };
  }
}

function glowEmissive(glow: GlowConfig | undefined, level: number, muted?: boolean): THREE.Color {
  if (muted || !glow?.enabled) return new THREE.Color(0, 0, 0);
  const lv = glow.byLevel.find((e) => e.level === level);
  const c: Rgb = lv?.color ?? glow.baseColor;
  // Soft accent only — never wash the whole mesh orange/red.
  const e = Math.min(0.22, (lv?.emissive ?? glow.baseIntensity * 0.5) * 0.12);
  return new THREE.Color(c.r * e, c.g * e, Math.min(0.35, c.b * e));
}

function buildGeometry(model: BmdModel, meshIndex: number): THREE.BufferGeometry | null {
  const mesh = model.meshes[meshIndex];
  if (!mesh || mesh.triangles.length === 0) return null;
  const bones = computeRestBoneMatrices(model);

  const positions: number[] = [];
  const normals: number[] = [];
  const uvs: number[] = [];
  const indices: number[] = [];

  let corner = 0;
  for (const tri of mesh.triangles) {
    const nCorners = tri.polygon >= 4 ? 4 : 3;
    const cornerIdx: number[] = [];
    for (let c = 0; c < nCorners; c++) {
      const vi = tri.vertexIndex[c];
      const ni = tri.normalIndex[c];
      const ti = tri.texCoordIndex[c];
      if (vi < 0 || vi >= mesh.vertices.length) continue;
      const vert = mesh.vertices[vi];
      const bone = bones[vert.node] ?? bones[0];
      const world = vectorTransform(vert.position, bone);
      const p = muToThree(world);
      positions.push(p[0], p[1], p[2]);

      if (ni >= 0 && ni < mesh.normals.length) {
        const n = mesh.normals[ni];
        const nBone = bones[n.node] ?? bone;
        const nw: [number, number, number] = [
          n.normal[0] * nBone[0][0] + n.normal[1] * nBone[0][1] + n.normal[2] * nBone[0][2],
          n.normal[0] * nBone[1][0] + n.normal[1] * nBone[1][1] + n.normal[2] * nBone[1][2],
          n.normal[0] * nBone[2][0] + n.normal[1] * nBone[2][1] + n.normal[2] * nBone[2][2],
        ];
        const nt = muToThree(nw, 1);
        const len = Math.hypot(nt[0], nt[1], nt[2]) || 1;
        normals.push(nt[0] / len, nt[1] / len, nt[2] / len);
      } else {
        normals.push(0, 1, 0);
      }

      if (ti >= 0 && ti < mesh.texCoords.length) {
        uvs.push(mesh.texCoords[ti].u, mesh.texCoords[ti].v);
      } else {
        uvs.push(0, 0);
      }
      cornerIdx.push(corner++);
    }
    if (cornerIdx.length >= 3) {
      indices.push(cornerIdx[0], cornerIdx[1], cornerIdx[2]);
      if (cornerIdx.length === 4) {
        indices.push(cornerIdx[0], cornerIdx[2], cornerIdx[3]);
      }
    }
  }

  if (indices.length === 0) return null;
  const geo = new THREE.BufferGeometry();
  geo.setAttribute('position', new THREE.Float32BufferAttribute(positions, 3));
  geo.setAttribute('normal', new THREE.Float32BufferAttribute(normals, 3));
  geo.setAttribute('uv', new THREE.Float32BufferAttribute(uvs, 2));
  geo.setIndex(indices);
  geo.computeBoundingSphere();
  return geo;
}

function makeDiffuseMaterial(
  map: THREE.Texture | null,
  hasAlpha: boolean,
  emissive: THREE.Color,
): THREE.Material {
  if (map) {
    return new THREE.MeshStandardMaterial({
      map,
      color: 0xffffff,
      metalness: 0.15,
      roughness: 0.65,
      emissive,
      emissiveIntensity: 1,
      side: THREE.DoubleSide,
      transparent: hasAlpha,
      alphaTest: hasAlpha ? 0.15 : 0,
      depthWrite: !hasAlpha,
    });
  }
  // Missing texture: dark slate — never orange debug wash
  return new THREE.MeshStandardMaterial({
    color: 0x3a424c,
    metalness: 0.2,
    roughness: 0.7,
    emissive: new THREE.Color(0, 0, 0),
    side: THREE.DoubleSide,
  });
}

function makeGlowMaterial(map: THREE.Texture | null): THREE.Material {
  // Mu RENDER_BRIGHT / additive companion mesh — must NOT paint opaque orange.
  return new THREE.MeshBasicMaterial({
    map: map ?? undefined,
    color: map ? 0xffffff : 0xff4422,
    transparent: true,
    opacity: map ? 0.85 : 0.25,
    blending: THREE.AdditiveBlending,
    depthWrite: false,
    side: THREE.DoubleSide,
  });
}

export async function bmdBytesToGroup(
  bytes: Uint8Array,
  bmdRelativePath: string,
  opts?: { glow?: GlowConfig; level?: number; muted?: boolean; slot?: EquipmentSlot },
): Promise<LoadedBmdPart> {
  const model = parseBmd(bytes);
  const root = new THREE.Group();
  root.name = bmdRelativePath;

  const emissive = glowEmissive(opts?.glow, opts?.level ?? 15, opts?.muted);
  const textureCache = new Map<string, { tex: THREE.Texture | null; fileName: string | null }>();
  let texturedMeshes = 0;
  let glowMeshes = 0;

  for (let i = 0; i < model.meshes.length; i++) {
    const mesh = model.meshes[i];
    const geo = buildGeometry(model, i);
    if (!geo) continue;

    const isGlow = isGlowTextureName(mesh.textureName);
    let map: THREE.Texture | null = null;
    let fileName: string | null = null;
    const texKey = mesh.textureName.toLowerCase();
    if (textureCache.has(texKey)) {
      const cached = textureCache.get(texKey)!;
      map = cached.tex;
      fileName = cached.fileName;
    } else {
      try {
        const found = await findTextureBytes(
          bmdRelativePath,
          mesh.textureName,
          textureSearchNames(mesh.textureName),
        );
        if (found) {
          map = await bytesToTexture(found.bytes, found.fileName);
          fileName = found.fileName;
        }
      } catch (e) {
        console.warn('[bmd] texture fail', mesh.textureName, e);
        map = null;
      }
      textureCache.set(texKey, { tex: map, fileName });
    }

    if (map) texturedMeshes++;
    if (isGlow) glowMeshes++;

    const lowerFile = (fileName ?? mesh.textureName).toLowerCase();
    const hasAlpha =
      !isGlow && (lowerFile.endsWith('.ozt') || lowerFile.endsWith('.tga') || /_bs|_fix|_cp/.test(lowerFile));

    const mat = isGlow ? makeGlowMaterial(map) : makeDiffuseMaterial(map, hasAlpha, emissive);
    const m = new THREE.Mesh(geo, mat);
    m.castShadow = !isGlow;
    m.receiveShadow = !isGlow;
    m.name = mesh.textureName || `mesh_${i}`;
    m.renderOrder = isGlow ? 2 : hasAlpha ? 1 : 0;
    root.add(m);
  }

  if (opts?.slot) {
    const t = slotAttachTransform(opts.slot);
    root.position.set(...t.position);
    root.rotation.set(...t.rotation);
    root.scale.setScalar(t.scale);
  }

  return { group: root, model, path: bmdRelativePath, texturedMeshes, glowMeshes };
}

export async function loadItemBmd(
  ref: ItemRef,
  opts?: { glow?: GlowConfig; level?: number; muted?: boolean; slot?: EquipmentSlot },
): Promise<LoadedBmdPart | null> {
  const candidates = resolveBmdCandidates(ref);
  for (const c of candidates) {
    const bytes = await readDataFile(c.relativePath);
    if (!bytes) continue;
    try {
      return await bmdBytesToGroup(bytes, c.relativePath, opts);
    } catch (e) {
      console.warn('BMD parse failed', c.relativePath, e);
    }
  }
  return null;
}

export function disposeObject3D(obj: THREE.Object3D) {
  obj.traverse((child) => {
    const mesh = child as THREE.Mesh;
    if (mesh.isMesh) {
      mesh.geometry?.dispose();
      const mats = Array.isArray(mesh.material) ? mesh.material : [mesh.material];
      for (const m of mats) {
        if (!m) continue;
        const std = m as THREE.MeshStandardMaterial;
        std.map?.dispose();
        m.dispose();
      }
    }
  });
}
