import { defineConfig, type Plugin, type Connect } from 'vite';
import react from '@vitejs/plugin-react';
import { fileURLToPath, URL } from 'node:url';
import {
  readFileSync,
  writeFileSync,
  existsSync,
  mkdirSync,
  statSync,
  readdirSync,
  createHash,
} from 'node:fs';
import { join, normalize, sep, dirname, extname, basename } from 'node:path';
import { deflateSync } from 'node:zlib';

function resolveDataRoot(): string | null {
  const candidates = [
    join(
      fileURLToPath(new URL('.', import.meta.url)),
      '..',
      '..',
      'MuMain',
      'out',
      'build',
      'windows-x86',
      'src',
      'RelWithDebInfo',
      'Data',
    ),
    join(fileURLToPath(new URL('.', import.meta.url)), '..', '..', 'Mudream.online', 'Data'),
    process.env.MU_DATA_PATH ?? '',
  ].filter(Boolean);

  for (const r of candidates) {
    try {
      const n = normalize(r);
      if (existsSync(n) && statSync(n).isDirectory()) return n;
    } catch {
      /* skip */
    }
  }
  return null;
}

function safeJoin(root: string, urlPath: string): string | null {
  const decoded = decodeURIComponent(urlPath).replace(/^\/+/, '').replace(/\\/g, '/');
  if (decoded.includes('..')) return null;
  const full = normalize(join(root, ...decoded.split('/')));
  const rootNorm = normalize(root) + sep;
  if (
    !full.toLowerCase().startsWith(rootNorm.toLowerCase()) &&
    full.toLowerCase() !== normalize(root).toLowerCase()
  ) {
    return null;
  }
  return full;
}

/** Case-insensitive resolve under Data root (helps mixed-case packs). */
function resolveExistingFile(root: string, rel: string): string | null {
  const parts = rel.replace(/\\/g, '/').split('/').filter(Boolean);
  let dir = root;
  for (let i = 0; i < parts.length - 1; i++) {
    const want = parts[i].toLowerCase();
    let next: string | null = null;
    const exact = join(dir, parts[i]);
    if (existsSync(exact) && statSync(exact).isDirectory()) {
      next = exact;
    } else {
      try {
        const entries = readdirSync(dir);
        const hit = entries.find((e) => e.toLowerCase() === want);
        if (hit) {
          const p = join(dir, hit);
          if (statSync(p).isDirectory()) next = p;
        }
      } catch {
        return null;
      }
    }
    if (!next) return null;
    dir = next;
  }
  const fileWant = parts[parts.length - 1];
  const exactFile = join(dir, fileWant);
  if (existsSync(exactFile) && statSync(exactFile).isFile()) return exactFile;
  try {
    const entries = readdirSync(dir);
    const hit = entries.find((e) => e.toLowerCase() === fileWant.toLowerCase());
    if (hit) {
      const p = join(dir, hit);
      if (statSync(p).isFile()) return p;
    }
  } catch {
    /* */
  }
  return null;
}

function crc32(buf: Buffer): number {
  let c = ~0;
  for (let i = 0; i < buf.length; i++) {
    c ^= buf[i];
    for (let k = 0; k < 8; k++) c = c & 1 ? (0xedb88320 ^ (c >>> 1)) : c >>> 1;
  }
  return ~c >>> 0;
}

function pngChunk(type: string, data: Buffer): Buffer {
  const len = Buffer.alloc(4);
  len.writeUInt32BE(data.length, 0);
  const typeBuf = Buffer.from(type, 'ascii');
  const crcBuf = Buffer.alloc(4);
  crcBuf.writeUInt32BE(crc32(Buffer.concat([typeBuf, data])), 0);
  return Buffer.concat([len, typeBuf, data, crcBuf]);
}

function rgbaToPng(width: number, height: number, rgba: Buffer): Buffer {
  const stride = width * 4;
  const raw = Buffer.alloc((stride + 1) * height);
  for (let y = 0; y < height; y++) {
    raw[y * (stride + 1)] = 0;
    rgba.copy(raw, y * (stride + 1) + 1, y * stride, y * stride + stride);
  }
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(width, 0);
  ihdr.writeUInt32BE(height, 4);
  ihdr[8] = 8;
  ihdr[9] = 6;
  ihdr[10] = 0;
  ihdr[11] = 0;
  ihdr[12] = 0;
  const signature = Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]);
  return Buffer.concat([
    signature,
    pngChunk('IHDR', ihdr),
    pngChunk('IDAT', deflateSync(raw, { level: 6 })),
    pngChunk('IEND', Buffer.alloc(0)),
  ]);
}

function decodeOzjToJpeg(buf: Buffer): Buffer | null {
  for (const skip of [24, 28, 4, 0]) {
    if (buf.length > skip + 2 && buf[skip] === 0xff && buf[skip + 1] === 0xd8) {
      return buf.subarray(skip);
    }
  }
  if (buf[0] === 0xff && buf[1] === 0xd8) return buf;
  return null;
}

function decodeOztToRgba(buf: Buffer): { width: number; height: number; rgba: Buffer } | null {
  for (const skip of [4, 0]) {
    if (buf.length < skip + 18) continue;
    const width = buf[skip + 12] | (buf[skip + 13] << 8);
    const height = buf[skip + 14] | (buf[skip + 15] << 8);
    const bpp = buf[skip + 16];
    if (bpp !== 32 || width <= 0 || height <= 0 || width > 8192 || height > 8192) continue;
    const dataStart = skip + 18;
    const need = width * height * 4;
    if (dataStart + need > buf.length) continue;
    const desc = buf[skip + 17];
    const topOrigin = (desc & 0x20) !== 0;
    const rgba = Buffer.alloc(need);
    for (let y = 0; y < height; y++) {
      const srcY = topOrigin ? y : height - 1 - y;
      for (let x = 0; x < width; x++) {
        const si = dataStart + (srcY * width + x) * 4;
        const di = (y * width + x) * 4;
        rgba[di] = buf[si + 2];
        rgba[di + 1] = buf[si + 1];
        rgba[di + 2] = buf[si];
        rgba[di + 3] = buf[si + 3];
      }
    }
    return { width, height, rgba };
  }
  return null;
}

function cacheDir(): string {
  const d = join(fileURLToPath(new URL('.', import.meta.url)), '.preview-cache');
  if (!existsSync(d)) mkdirSync(d, { recursive: true });
  return d;
}

function cachedPath(rel: string, ext: string): string {
  const hash = createHash('sha1').update(rel.toLowerCase()).digest('hex').slice(0, 16);
  const base = basename(rel).replace(/[^a-zA-Z0-9._-]/g, '_');
  return join(cacheDir(), `${hash}_${base}${ext}`);
}

function muDataPlugin(): Plugin {
  let root: string | null = null;
  return {
    name: 'mu-data-static',
    configureServer(server) {
      root = resolveDataRoot();
      if (root) console.log(`[mu-data] Serving Data from ${root}`);
      else
        console.warn(
          '[mu-data] No Data folder found. Set MU_DATA_PATH or use “Data folder…” in the editor.',
        );
      console.log(`[mu-tex] Preview cache → ${cacheDir()}`);

      const handler: Connect.NextHandleFunction = (req, res, next) => {
        if (!req.url) return next();

        // Decoded texture cache: /mu-tex/<relpath>
        if (req.url.startsWith('/mu-tex')) {
          if (!root) {
            res.statusCode = 503;
            res.end('Data root not configured');
            return;
          }
          const url = req.url.slice('/mu-tex'.length).split('?')[0] || '/';
          const rel = decodeURIComponent(url).replace(/^\/+/, '');
          const filePath = resolveExistingFile(root, rel);
          if (!filePath) {
            res.statusCode = 404;
            res.end('Not found');
            return;
          }
          try {
            const ext = extname(filePath).toLowerCase();
            const raw = readFileSync(filePath);
            if (ext === '.ozj' || ext === '.jpg' || ext === '.jpeg') {
              const outFile = cachedPath(rel, '.jpg');
              if (!existsSync(outFile)) {
                const jpeg = ext === '.ozj' ? decodeOzjToJpeg(raw) : raw;
                if (!jpeg) {
                  res.statusCode = 422;
                  res.end('OZJ decode failed');
                  return;
                }
                mkdirSync(dirname(outFile), { recursive: true });
                writeFileSync(outFile, jpeg);
              }
              res.statusCode = 200;
              res.setHeader('Content-Type', 'image/jpeg');
              res.setHeader('Cache-Control', 'public, max-age=3600');
              res.end(readFileSync(outFile));
              return;
            }
            if (ext === '.ozt' || ext === '.tga') {
              const outFile = cachedPath(rel, '.png');
              if (!existsSync(outFile)) {
                const decoded = decodeOztToRgba(raw);
                if (!decoded) {
                  res.statusCode = 422;
                  res.end('OZT decode failed');
                  return;
                }
                mkdirSync(dirname(outFile), { recursive: true });
                writeFileSync(outFile, rgbaToPng(decoded.width, decoded.height, decoded.rgba));
              }
              res.statusCode = 200;
              res.setHeader('Content-Type', 'image/png');
              res.setHeader('Cache-Control', 'public, max-age=3600');
              res.end(readFileSync(outFile));
              return;
            }
            res.statusCode = 415;
            res.end('Unsupported texture type');
          } catch (e) {
            res.statusCode = 500;
            res.end(String(e));
          }
          return;
        }

        if (!req.url.startsWith('/mu-data')) return next();
        const url = req.url.slice('/mu-data'.length).split('?')[0] || '/';
        if (url === '/__ping' || url === '/__ping/') {
          res.statusCode = 200;
          res.setHeader('Content-Type', 'application/json');
          res.end(JSON.stringify({ ok: !!root, root, tex: '/mu-tex' }));
          return;
        }
        if (!root) {
          res.statusCode = 503;
          res.end('Data root not configured');
          return;
        }
        const rel = decodeURIComponent(url).replace(/^\/+/, '');
        const filePath = resolveExistingFile(root, rel) ?? safeJoin(root, url);
        if (!filePath || !existsSync(filePath) || !statSync(filePath).isFile()) {
          res.statusCode = 404;
          res.end('Not found');
          return;
        }
        try {
          const buf = readFileSync(filePath);
          res.statusCode = 200;
          res.setHeader('Content-Type', 'application/octet-stream');
          res.setHeader('Cache-Control', 'no-cache');
          res.end(buf);
        } catch {
          res.statusCode = 500;
          res.end('Read error');
        }
      };
      server.middlewares.use(handler);
    },
  };
}

export default defineConfig({
  plugins: [react(), muDataPlugin()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    port: 5177,
    open: true,
    fs: {
      allow: ['..'],
    },
  },
});
