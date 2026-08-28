using System.Text.Json;
using MUAssetInspector.Analysis;
using MUAssetInspector.Core.Database;
using MUAssetInspector.Core.Domain;
using MUAssetInspector.Core.Logging;
using MUAssetInspector.Core.Profiles;
using MUAssetInspector.Core.Scanning;
using MUAssetInspector.Core.Workspace;
using MUAssetInspector.Formats;

namespace MUAssetInspector.Migration;

public sealed class BatchAnalyzer
{
    private readonly FileLogger _logger;

    public BatchAnalyzer(FileLogger logger) => _logger = logger;

    public async Task<(List<ComparisonRecord> Comparisons, DiagnosticResult Summary)> AnalyzeAllAsync(
        string sourceRoot,
        string destRoot,
        ClientProfile profile,
        WorkspaceManager workspace,
        IProgress<ScanProgress>? progress = null,
        CancellationToken ct = default)
    {
        using var sourceDb = new AssetDatabase(workspace.GetDatabasePath(ClientRole.Source));
        using var destDb = new AssetDatabase(workspace.GetDatabasePath(ClientRole.Destination));

        var sourceBridge = new AssetFormatManager(profile, sourceRoot);
        var destBridge = new AssetFormatManager(profile, destRoot);
        var scanner = new RecursiveScanner();

        _logger.Info($"Batch scan source: {sourceRoot}");
        await scanner.ScanAsync(sourceRoot, ClientRole.Source, sourceDb, sourceBridge, progress, ct);
        _logger.Info($"Batch scan destination: {destRoot}");
        await scanner.ScanAsync(destRoot, ClientRole.Destination, destDb, destBridge, progress, ct);

        var engine = new CompatibilityEngine();
        var comparisons = engine.CompareTrees(sourceDb, destDb);
        var missing = comparisons.Count(c => c.Status == ComparisonStatus.Missing);
        var summary = new DiagnosticResult
        {
            Subject = "Batch Analysis",
            Level = missing > 0 ? CompatibilityLevel.PartiallyCompatible : CompatibilityLevel.FullyCompatible,
            Entries =
            [
                new DiagnosticEntry
                {
                    Severity = missing > 0 ? DiagnosticSeverity.Warning : DiagnosticSeverity.Info,
                    Message = $"Compared {comparisons.Count} assets, {missing} missing in destination"
                }
            ]
        };
        return (comparisons, summary);
    }

    public async Task<(AssetDatabase SourceDb, AssetDatabase DestDb)> ScanBothAsync(
        string sourceRoot,
        string destRoot,
        ClientProfile profile,
        WorkspaceManager workspace,
        IProgress<ScanProgress>? progress = null,
        CancellationToken ct = default)
    {
        var sourceDb = new AssetDatabase(workspace.GetDatabasePath(ClientRole.Source));
        var destDb = new AssetDatabase(workspace.GetDatabasePath(ClientRole.Destination));

        var sourceBridge = new AssetFormatManager(profile, sourceRoot);
        var destBridge = new AssetFormatManager(profile, destRoot);
        var scanner = new RecursiveScanner();

        _logger.Info($"Batch scan source: {sourceRoot}");
        await scanner.ScanAsync(sourceRoot, ClientRole.Source, sourceDb, sourceBridge, progress, ct);
        _logger.Info($"Batch scan destination: {destRoot}");
        await scanner.ScanAsync(destRoot, ClientRole.Destination, destDb, destBridge, progress, ct);

        return (sourceDb, destDb);
    }
}

public sealed class AutoRepairPreview
{
    public sealed record RepairAction(string SourcePath, string DestPath, string Action);

    public List<RepairAction> PreviewMissing(IEnumerable<ComparisonRecord> comparisons, AssetDatabase sourceDb, AssetDatabase destDb, string sourceRoot, string destRoot)
    {
        var actions = new List<RepairAction>();
        foreach (var c in comparisons.Where(x => x.Status == ComparisonStatus.Missing))
        {
            var source = sourceDb.AllAssets.FirstOrDefault(a => a.Id == c.SourceAssetId);
            if (source is null) continue;
            var destRel = source.RelativePath;
            var srcFull = Path.Combine(sourceRoot, destRel.Replace('/', Path.DirectorySeparatorChar));
            var destFull = Path.Combine(destRoot, destRel.Replace('/', Path.DirectorySeparatorChar));
            actions.Add(new RepairAction(srcFull, destFull, "Copy"));
        }
        return actions;
    }

    public string ExportManifest(IEnumerable<RepairAction> actions, string workspaceExports)
    {
        Directory.CreateDirectory(workspaceExports);
        var path = Path.Combine(workspaceExports, "repair-preview.json");
        File.WriteAllText(path, JsonSerializer.Serialize(actions, new JsonSerializerOptions { WriteIndented = true }));
        return path;
    }
}

public sealed class MigrationEngine
{
    public string CreateMigrationPackage(IEnumerable<AutoRepairPreview.RepairAction> actions, WorkspaceManager workspace)
    {
        var packageDir = workspace.GetSafeWritePath($"MigrationPackages/{DateTime.UtcNow:yyyyMMdd-HHmmss}");
        Directory.CreateDirectory(packageDir);
        var manifest = actions.Select(a => new { a.SourcePath, a.DestPath, a.Action }).ToList();
        File.WriteAllText(Path.Combine(packageDir, "manifest.json"), JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
        return packageDir;
    }
}

public sealed class BmdViewerPlaceholder
{
    public string Status => "Phase 3: BMD 3D viewer (Silk.NET) — architecture stub ready.";
}

public sealed class EffectViewerPlaceholder
{
    public string Status => "Phase 3: Effect viewer — awaiting stable effect-to-bitmap mapping.";
}
