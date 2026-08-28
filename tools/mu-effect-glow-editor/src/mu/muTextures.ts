import * as THREE from 'three';

const OZJ_SKIP = 24;
const OZT_SKIP = 4;

function basenameNoExt(name: string): string {
  const base = name.replace(/^.*[\\/]/, '');
  return base.replace(/\.[^.]+$/, '');
}

/** True for MU "Bright" / emissive companion maps (…_R.jpg). */
export function isGlowTextureName(textureName: string): boolean {
  const stem = basenameNoExt(textureName).toLowerCase();
  return /_r$/.test(stem);
}

/**
 * Search order matches MuMain OpenTexture / GlobalBitmap:
 * prefer OZT (alpha) over OZJ for cloth/wings; keep _R as OZJ/JPG first.
 */
export function textureSearchNames(textureName: string): string[] {
  const stem = basenameNoExt(textureName);
  if (!stem) return [];
  const glow = isGlowTextureName(textureName);

  const alphaFirst = [
    `${stem}.OZT`,
    `${stem}.ozt`,
    `${stem}.TGA`,
    `${stem}.tga`,
    `${stem}.OZJ`,
    `${stem}.ozj`,
    `${stem}.JPG`,
    `${stem}.jpg`,
    `${stem}.PNG`,
    `${stem}.png`,
  ];
  const jpegFirst = [
    `${stem}.OZJ`,
    `${stem}.ozj`,
    `${stem}.JPG`,
    `${stem}.jpg`,
    `${stem}.OZT`,
    `${stem}.ozt`,
    `${stem}.TGA`,
    `${stem}.tga`,
    `${stem}.PNG`,
    `${stem}.png`,
  ];

  const primary = glow ? jpegFirst : alphaFirst;

  // Mudream pack variants (OpenTexture tryAlphaVariants) — skip for glow maps
  if (glow) return primary;

  const variants: string[] = [];
  let base = stem;
  for (const suf of ['_BS', '_CP', '_MT']) {
    if (base.toLowerCase().endsWith(suf.toLowerCase())) {
      base = base.slice(0, -suf.length);
      break;
    }
  }
  for (const suf of ['_FIX', '_BS', '_BS1', '_GW1E']) {
    variants.push(`${base}${suf}.OZT`, `${base}${suf}.ozt`);
  }
  return [...variants, ...primary];
}

/** Decode OZJ (JPEG after header skip) or raw JPEG bytes → Blob. */
export function decodeOzjToBlob(bytes: Uint8Array): Blob {
  let jpeg = bytes;
  if (bytes.length > OZJ_SKIP + 2 && bytes[OZJ_SKIP] === 0xff && bytes[OZJ_SKIP + 1] === 0xd8) {
    jpeg = bytes.subarray(OZJ_SKIP);
  } else if (!(bytes[0] === 0xff && bytes[1] === 0xd8)) {
    for (const skip of [24, 28, 4, 0]) {
      if (bytes.length > skip + 2 && bytes[skip] === 0xff && bytes[skip + 1] === 0xd8) {
        jpeg = bytes.subarray(skip);
        break;
      }
    }
  }
  return new Blob([jpeg.buffer.slice(jpeg.byteOffset, jpeg.byteOffset + jpeg.byteLength) as ArrayBuffer], {
    type: 'image/jpeg',
  });
}

/** Decode OZT / TGA-like BGRA (32-bit) → ImageData. Matches MuMain OpenTga (4-byte prefix). */
export function decodeOztToImageData(bytes: Uint8Array): ImageData {
  const trySkip = (skip: number): ImageData | null => {
    if (bytes.length < skip + 18) return null;
    const off = skip;
    const width = bytes[off + 12] | (bytes[off + 13] << 8);
    const height = bytes[off + 14] | (bytes[off + 15] << 8);
    const bpp = bytes[off + 16];
    if (bpp !== 32 || width <= 0 || height <= 0 || width > 8192 || height > 8192) return null;
    const dataStart = off + 18;
    const need = width * height * 4;
    if (dataStart + need > bytes.length) return null;

    const rgba = new Uint8ClampedArray(need);
    const src = bytes.subarray(dataStart, dataStart + need);
    const desc = bytes[off + 17];
    const topOrigin = (desc & 0x20) !== 0;
    for (let y = 0; y < height; y++) {
      const srcY = topOrigin ? y : height - 1 - y;
      for (let x = 0; x < width; x++) {
        const si = (srcY * width + x) * 4;
        const di = (y * width + x) * 4;
        rgba[di] = src[si + 2];
        rgba[di + 1] = src[si + 1];
        rgba[di + 2] = src[si];
        rgba[di + 3] = src[si + 3];
      }
    }
    return new ImageData(rgba, width, height);
  };

  return (
    trySkip(OZT_SKIP) ??
    trySkip(0) ??
    (() => {
      throw new Error('Unable to parse OZT/TGA');
    })()
  );
}

export async function bytesToTexture(
  bytes: Uint8Array,
  fileName: string,
): Promise<THREE.Texture> {
  const lower = fileName.toLowerCase();
  const loader = new THREE.TextureLoader();

  if (lower.endsWith('.ozt') || lower.endsWith('.tga')) {
    const imageData = decodeOztToImageData(bytes);
    const tex = new THREE.DataTexture(
      imageData.data,
      imageData.width,
      imageData.height,
      THREE.RGBAFormat,
    );
    tex.needsUpdate = true;
    tex.colorSpace = THREE.SRGBColorSpace;
    tex.flipY = false;
    tex.wrapS = THREE.RepeatWrapping;
    tex.wrapT = THREE.RepeatWrapping;
    tex.minFilter = THREE.LinearMipmapLinearFilter;
    tex.magFilter = THREE.LinearFilter;
    tex.generateMipmaps = true;
    return tex;
  }

  const blob =
    lower.endsWith('.ozj') || (bytes[0] !== 0xff && bytes.length > 24)
      ? decodeOzjToBlob(bytes)
      : new Blob([
          bytes.buffer.slice(bytes.byteOffset, bytes.byteOffset + bytes.byteLength) as ArrayBuffer,
        ]);
  const url = URL.createObjectURL(blob);
  try {
    const tex = await loader.loadAsync(url);
    tex.colorSpace = THREE.SRGBColorSpace;
    tex.flipY = false;
    tex.wrapS = THREE.RepeatWrapping;
    tex.wrapT = THREE.RepeatWrapping;
    tex.minFilter = THREE.LinearMipmapLinearFilter;
    tex.magFilter = THREE.LinearFilter;
    return tex;
  } finally {
    URL.revokeObjectURL(url);
  }
}

/** Soft radial disc for additive FX sprites (replaces solid red quads). */
export function createSoftSpriteTexture(size = 64): THREE.Texture {
  const canvas = document.createElement('canvas');
  canvas.width = size;
  canvas.height = size;
  const ctx = canvas.getContext('2d')!;
  const g = ctx.createRadialGradient(size / 2, size / 2, 0, size / 2, size / 2, size / 2);
  g.addColorStop(0, 'rgba(255,255,255,1)');
  g.addColorStop(0.35, 'rgba(255,255,255,0.55)');
  g.addColorStop(0.7, 'rgba(255,255,255,0.12)');
  g.addColorStop(1, 'rgba(255,255,255,0)');
  ctx.fillStyle = g;
  ctx.fillRect(0, 0, size, size);
  const tex = new THREE.CanvasTexture(canvas);
  tex.colorSpace = THREE.SRGBColorSpace;
  tex.needsUpdate = true;
  return tex;
}
