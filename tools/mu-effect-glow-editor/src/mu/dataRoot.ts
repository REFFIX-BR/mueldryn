/**
 * Data root access: Vite /mu-data middleware (local) + optional File System Access picker.
 */

export type DataBackend = 'http' | 'fs-handle' | 'none';

export interface DataRootState {
  backend: DataBackend;
  label: string;
  /** DirectoryHandle when using picker */
  handle: FileSystemDirectoryHandle | null;
  ready: boolean;
  lastError: string | null;
}

type Listener = (s: DataRootState) => void;

let state: DataRootState = {
  backend: 'none',
  label: 'Nenhuma pasta Data',
  handle: null,
  ready: false,
  lastError: null,
};

const listeners = new Set<Listener>();

function emit() {
  for (const l of listeners) l(state);
}

export function getDataRootState(): DataRootState {
  return state;
}

export function subscribeDataRoot(listener: Listener): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

async function probeHttp(): Promise<boolean> {
  try {
    const res = await fetch('/mu-data/__ping', { method: 'GET' });
    if (!res.ok) return false;
    const j = (await res.json()) as { ok?: boolean; root?: string };
    if (!j.ok) return false;
    state = {
      backend: 'http',
      label: j.root ? `HTTP: ${j.root}` : 'HTTP /mu-data',
      handle: null,
      ready: true,
      lastError: null,
    };
    emit();
    return true;
  } catch {
    return false;
  }
}

/** Call once on app boot. */
export async function initDataRoot(): Promise<DataRootState> {
  const ok = await probeHttp();
  if (!ok) {
    state = {
      ...state,
      backend: 'none',
      ready: false,
      label: 'Selecione a pasta Data (Player/Item)',
    };
    emit();
  }
  return state;
}

export async function pickDataFolder(): Promise<DataRootState> {
  const w = window as Window & {
    showDirectoryPicker?: (opts?: { id?: string; mode?: string }) => Promise<FileSystemDirectoryHandle>;
  };
  if (!w.showDirectoryPicker) {
    state = {
      ...state,
      lastError: 'Este browser não suporta seleção de pasta (use Chrome/Edge).',
    };
    emit();
    return state;
  }
  try {
    const handle = await w.showDirectoryPicker({ id: 'mu-data', mode: 'read' });
    // Sanity: prefer a folder that contains Player or Item
    let label = handle.name;
    try {
      await handle.getDirectoryHandle('Player');
      label = `${handle.name}/ (Player ok)`;
    } catch {
      try {
        await handle.getDirectoryHandle('Item');
        label = `${handle.name}/ (Item ok)`;
      } catch {
        /* still accept */
      }
    }
    state = {
      backend: 'fs-handle',
      label,
      handle,
      ready: true,
      lastError: null,
    };
    emit();
  } catch (e) {
    if ((e as Error).name !== 'AbortError') {
      state = { ...state, lastError: (e as Error).message };
      emit();
    }
  }
  return state;
}

async function readViaHandle(
  handle: FileSystemDirectoryHandle,
  relativePath: string,
): Promise<Uint8Array | null> {
  const parts = relativePath.replace(/\\/g, '/').split('/').filter(Boolean);
  let dir: FileSystemDirectoryHandle = handle;
  for (let i = 0; i < parts.length - 1; i++) {
    try {
      dir = await dir.getDirectoryHandle(parts[i]);
    } catch {
      return null;
    }
  }
  const fileName = parts[parts.length - 1];
  // case-insensitive-ish: try exact then scan
  try {
    const fh = await dir.getFileHandle(fileName);
    const file = await fh.getFile();
    return new Uint8Array(await file.arrayBuffer());
  } catch {
    /* try case variants */
  }
  const variants = [
    fileName,
    fileName.toLowerCase(),
    fileName.toUpperCase(),
    fileName.replace(/\.bmd$/i, '.BMD'),
    fileName.replace(/\.bmd$/i, '.bmd'),
  ];
  for (const name of variants) {
    try {
      const fh = await dir.getFileHandle(name);
      const file = await fh.getFile();
      return new Uint8Array(await file.arrayBuffer());
    } catch {
      /* next */
    }
  }
  return null;
}

export async function readDataFile(relativePath: string): Promise<Uint8Array | null> {
  const rel = relativePath.replace(/\\/g, '/').replace(/^\/+/, '');
  if (state.backend === 'http') {
    const url = `/mu-data/${rel.split('/').map(encodeURIComponent).join('/')}`;
    try {
      const res = await fetch(url);
      if (!res.ok) return null;
      return new Uint8Array(await res.arrayBuffer());
    } catch {
      return null;
    }
  }
  if (state.backend === 'fs-handle' && state.handle) {
    return readViaHandle(state.handle, rel);
  }
  return null;
}

export async function resolveExistingPath(candidates: string[]): Promise<string | null> {
  for (const c of candidates) {
    const bytes = await readDataFile(c);
    if (bytes && bytes.length > 0) return c;
  }
  return null;
}

/** List files in a directory (http lists via HEAD attempts only — use candidates). */
export async function findTextureBytes(
  bmdRelativePath: string,
  textureName: string,
  searchNames: string[],
): Promise<{ bytes: Uint8Array; fileName: string } | null> {
  const dir = bmdRelativePath.replace(/\\/g, '/').split('/').slice(0, -1).join('/');
  const tryDirs = [
    dir,
    'Item',
    'Player',
    'Item/CustomItem/Skin/bloodysoldier',
    'Item/CustomItem/Skin/Bloodysoldier',
    'Item/CustomItem/Skin/Hellfire',
    'Item/CustomItem/Skin/hellfire',
  ];
  for (const d of tryDirs) {
    for (const name of searchNames) {
      const rel = d ? `${d}/${name}` : name;
      const bytes = await readDataFile(rel);
      if (bytes) return { bytes, fileName: name };
    }
  }
  if (textureName) {
    const base = textureName.replace(/^.*[\\/]/, '');
    const bytes = await readDataFile(`${dir}/${base}`);
    if (bytes) return { bytes, fileName: base };
    // stem without extension
    const stem = base.replace(/\.[^.]+$/, '');
    for (const ext of ['.ozt', '.OZT', '.ozj', '.OZJ', '.tga', '.jpg']) {
      const bytes2 = await readDataFile(`${dir}/${stem}${ext}`);
      if (bytes2) return { bytes: bytes2, fileName: `${stem}${ext}` };
    }
  }
  return null;
}
