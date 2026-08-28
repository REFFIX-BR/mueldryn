using MUAssetInspector.Core.Domain;

namespace MUAssetInspector.Analysis.Viewer;

/// <summary>Phase 3 stub — Silk.NET OpenGL BMD mesh viewer.</summary>
public sealed class BmdViewer3DService
{
    public string Status => "Phase 3: BMD 3D viewer (Silk.NET) — architecture stub ready.";
    public bool IsSupported => false;

    public Task<AssetParseResult?> LoadMeshPreviewAsync(string bmdPath, CancellationToken ct = default) =>
        Task.FromResult<AssetParseResult?>(null);
}

/// <summary>Phase 3 stub — particle/effect playback when bitmap mapping is stable.</summary>
public sealed class EffectViewerService
{
    public string Status => "Phase 3: Effect viewer — awaiting stable effect-to-bitmap mapping.";

    public Task<bool> TryPlayEffectAsync(int effectBitmapId, CancellationToken ct = default) =>
        Task.FromResult(false);
}
