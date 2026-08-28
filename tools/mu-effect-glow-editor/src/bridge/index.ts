/**
 * Future bridge stubs — modular hooks for Asset Inspector, Live Client, Launcher.
 */

export interface BridgeStatus {
  available: boolean;
  message: string;
}

export const LiveClientPreview = {
  id: 'live-client-preview' as const,
  describe(): BridgeStatus {
    return {
      available: false,
      message:
        'Live Client Preview: stub. Futuro: stream de pose/FX para Main.exe / OpenMU via IPC ou socket.',
    };
  },
  async connect(_host?: string): Promise<BridgeStatus> {
    return this.describe();
  },
  async pushPreview(_payload: unknown): Promise<BridgeStatus> {
    return {
      available: false,
      message: 'pushPreview não implementado — aguardando bridge no cliente.',
    };
  },
};

export const AssetInspectorImport = {
  id: 'asset-inspector-import' as const,
  describe(): BridgeStatus {
    return {
      available: false,
      message:
        'Asset Inspector Import: stub. Futuro: importar BMD/textures de tools/MUAssetInspector.',
    };
  },
  async importFromInspector(_assetId: string): Promise<BridgeStatus> {
    return this.describe();
  },
};

export const LauncherPublish = {
  id: 'launcher-publish' as const,
  describe(): BridgeStatus {
    return {
      available: false,
      message:
        'Launcher Publish: stub. Futuro: publicar item_effects.json no pacote do launcher / CDN.',
    };
  },
  async publish(_doc: unknown, _channel: 'dev' | 'stage' | 'live' = 'dev'): Promise<BridgeStatus> {
    return this.describe();
  },
};
