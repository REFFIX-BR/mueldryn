import {
  itemEffectsDocumentSchema,
  type ItemEffectsDocument,
} from '@/schema/itemEffects';

export function serializeDocument(doc: ItemEffectsDocument): string {
  const stamped: ItemEffectsDocument = {
    ...doc,
    meta: {
      ...doc.meta,
      updatedAt: new Date().toISOString(),
    },
  };
  return JSON.stringify(stamped, null, 2);
}

export function parseDocument(json: string): ItemEffectsDocument {
  const raw = JSON.parse(json) as unknown;
  const parsed = itemEffectsDocumentSchema.safeParse(raw);
  if (!parsed.success) {
    throw new Error(
      `JSON inválido: ${parsed.error.issues.map((i) => i.message).join('; ')}`,
    );
  }
  return parsed.data;
}

export async function downloadJson(doc: ItemEffectsDocument, fileName: string): Promise<void> {
  const blob = new Blob([serializeDocument(doc)], { type: 'application/json' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = fileName.endsWith('.json') ? fileName : `${fileName}.json`;
  a.click();
  URL.revokeObjectURL(url);
}

export async function saveWithFilePicker(
  doc: ItemEffectsDocument,
  suggestedName: string,
): Promise<string | null> {
  const w = window as Window & {
    showSaveFilePicker?: (opts: {
      suggestedName?: string;
      types?: { description: string; accept: Record<string, string[]> }[];
    }) => Promise<FileSystemFileHandle>;
  };

  if (typeof w.showSaveFilePicker === 'function') {
    try {
      const handle = await w.showSaveFilePicker({
        suggestedName,
        types: [
          {
            description: 'MU Item Effects JSON',
            accept: { 'application/json': ['.json'] },
          },
        ],
      });
      const writable = await handle.createWritable();
      await writable.write(serializeDocument(doc));
      await writable.close();
      return handle.name;
    } catch (err) {
      if ((err as Error).name === 'AbortError') return null;
      throw err;
    }
  }

  await downloadJson(doc, suggestedName);
  return suggestedName;
}

export async function loadWithFilePicker(): Promise<{
  doc: ItemEffectsDocument;
  fileName: string;
} | null> {
  const w = window as Window & {
    showOpenFilePicker?: (opts: {
      multiple?: boolean;
      types?: { description: string; accept: Record<string, string[]> }[];
    }) => Promise<FileSystemFileHandle[]>;
  };

  if (typeof w.showOpenFilePicker === 'function') {
    try {
      const [handle] = await w.showOpenFilePicker({
        multiple: false,
        types: [
          {
            description: 'MU Item Effects JSON',
            accept: { 'application/json': ['.json'] },
          },
        ],
      });
      const file = await handle.getFile();
      const text = await file.text();
      return { doc: parseDocument(text), fileName: file.name };
    } catch (err) {
      if ((err as Error).name === 'AbortError') return null;
      throw err;
    }
  }

  return new Promise((resolve, reject) => {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = 'application/json,.json';
    input.onchange = async () => {
      const file = input.files?.[0];
      if (!file) {
        resolve(null);
        return;
      }
      try {
        const text = await file.text();
        resolve({ doc: parseDocument(text), fileName: file.name });
      } catch (e) {
        reject(e);
      }
    };
    input.click();
  });
}
