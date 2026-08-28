using MUAssetInspector.Analysis;
using MUAssetInspector.Core.Database;
using MUAssetInspector.Core.Domain;
using MUAssetInspector.Core.Logging;
using MUAssetInspector.Core.Profiles;
using MUAssetInspector.Core.Paths;
using MUAssetInspector.Core.Scanning;
using MUAssetInspector.Core.Workspace;
using MUAssetInspector.Formats;
using MUAssetInspector.Formats.Imaging;
using MUAssetInspector.Formats.Resolution;
using MUAssetInspector.Migration;
using SixLabors.ImageSharp.Formats.Png;

namespace MUAssetInspector.App.Services;

public sealed class InspectorSession : IDisposable
{
    private AssetDatabase? _sourceDb;
    private AssetDatabase? _destDb;

    public WorkspaceManager Workspace { get; }
    public FileLogger Logger { get; }
    public ClientProfileManager Profiles { get; }
    public ClientProfile ActiveProfile { get; private set; }
    public string? SourceRoot { get; private set; }
    public string? DestRoot { get; private set; }
    public DependencyAnalyzer DependencyAnalyzer { get; } = new();
    public CompatibilityEngine CompatibilityEngine { get; } = new();
    public TextureLoader TextureLoader { get; } = new();
    public ReportExporter ReportExporter { get; } = new();
    public CosmeticCatalogImporter CatalogImporter { get; } = new();

    public InspectorSession(string projectRoot)
    {
        Workspace = new WorkspaceManager(projectRoot);
        Logger = new FileLogger(Workspace);
        Profiles = new ClientProfileManager(Path.Combine(projectRoot, "profiles"));
        ActiveProfile = Profiles.ListProfiles().FirstOrDefault() is { } p ? Profiles.Load(p) : new ClientProfile { Name = "default" };
    }

    public void SetProfile(string name) => ActiveProfile = Profiles.Load(name);

    public void SetRoots(string? source, string? dest)
    {
        var normalizedSource = DataRootNormalizer.Normalize(source);
        var normalizedDest = DataRootNormalizer.Normalize(dest);

        if (string.Equals(SourceRoot, normalizedSource, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(DestRoot, normalizedDest, StringComparison.OrdinalIgnoreCase))
            return;

        SourceRoot = normalizedSource;
        DestRoot = normalizedDest;
        _sourceDb?.Dispose();
        _destDb?.Dispose();
        _sourceDb = normalizedSource is not null ? new AssetDatabase(Workspace.GetDatabasePath(ClientRole.Source)) : null;
        _destDb = normalizedDest is not null ? new AssetDatabase(Workspace.GetDatabasePath(ClientRole.Destination)) : null;
        DependencyAnalyzer.Configure(normalizedSource, normalizedDest, ActiveProfile);
    }

    public string? ResolvePreviewFile(AssetRecord asset)
    {
        var roots = new[] { (SourceRoot, SourceDb), (DestRoot, DestDb) };
        foreach (var (root, db) in roots)
        {
            if (root is null) continue;

            if (asset.Format is AssetFormatKind.Ozj or AssetFormatKind.Ozt or AssetFormatKind.PlainImage)
            {
                var direct = Path.Combine(root, asset.RelativePath.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(direct)) return direct;
            }

            if (asset.Format == AssetFormatKind.BmdMesh)
            {
                foreach (var tex in TexturePathResolver.ExtractTextureNames(asset.ParsedJson))
                {
                    var rel = TexturePathResolver.Resolve(root, db, asset.RelativePath, tex, ActiveProfile);
                    if (rel is null) continue;
                    var full = Path.Combine(root, rel.Replace('/', Path.DirectorySeparatorChar));
                    if (File.Exists(full)) return full;
                }
            }
        }

        return null;
    }

    public AssetDatabase? SourceDb => _sourceDb;
    public AssetDatabase? DestDb => _destDb;

    public async Task ScanAsync(ClientRole role, IProgress<ScanProgress>? progress, CancellationToken ct)
    {
        var root = role == ClientRole.Source ? SourceRoot : DestRoot;
        var db = role == ClientRole.Source ? _sourceDb : _destDb;
        if (root is null || db is null)
            throw new InvalidOperationException("Set source/destination roots first.");

        var bridge = new AssetFormatManager(ActiveProfile, root);
        var scanner = new RecursiveScanner();
        Logger.Info($"Scanning {role} at {root}");
        await scanner.ScanAsync(root, role, db, bridge, progress, ct);
        Logger.Info($"Scan complete for {role}");
    }

    public Avalonia.Media.Imaging.Bitmap? LoadPreviewBitmap(string fullPath)
    {
        using var image = TextureLoader.LoadPreview(fullPath, ActiveProfile);
        if (image is null) return null;
        using var ms = new MemoryStream();
        image.Save(ms, new PngEncoder());
        ms.Position = 0;
        return new Avalonia.Media.Imaging.Bitmap(ms);
    }

    public void ImportCatalogIfConfigured()
    {
        if (_sourceDb is null || SourceRoot is null || string.IsNullOrWhiteSpace(ActiveProfile.CosmeticCatalog))
            return;

        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "MuMain", "src", "source", "Data", "GameData", "ItemData", ActiveProfile.CosmeticCatalog),
            Path.Combine(SourceRoot, "..", "..", "source", "Data", "GameData", "ItemData", ActiveProfile.CosmeticCatalog)
        };

        foreach (var candidate in candidates.Select(Path.GetFullPath))
        {
            if (!File.Exists(candidate)) continue;
            CatalogImporter.ImportFromGeneratedCpp(candidate, _sourceDb, ActiveProfile.Name);
            return;
        }
    }

    public ImportResult ImportSelectedAsset(AssetRecord asset, bool includeEffectTables = false)
    {
        EnsureImportReady();
        var importer = new AssetImporter();
        var tree = DependencyAnalyzer.AnalyzeAsset(asset, _sourceDb!, _destDb);
        var plans = importer.PlanFromDependencyTree(tree, SourceRoot!, DestRoot!, _sourceDb!, _destDb, ActiveProfile);
        if (includeEffectTables)
            plans.AddRange(importer.PlanMudreamEffectBundle(SourceRoot!, DestRoot!, ActiveProfile));
        return ExecuteImport(importer, plans);
    }

    public ImportResult ImportAllMissingCompatible(ImportScope scope = ImportScope.CosmeticsOnly, bool includeEffectTables = false)
    {
        EnsureImportReady();
        var importer = new AssetImporter();
        var comparisons = CompatibilityEngine.CompareTrees(_sourceDb!, _destDb!);
        var plans = importer.PlanAllMissingCompatible(comparisons, _sourceDb!, _destDb, SourceRoot!, DestRoot!, ActiveProfile, scope);
        if (includeEffectTables)
            plans.AddRange(importer.PlanMudreamEffectBundle(SourceRoot!, DestRoot!, ActiveProfile));
        return ExecuteImport(importer, plans);
    }

    public RollbackResult RollbackLastImport()
    {
        if (DestRoot is null)
            throw new InvalidOperationException("Configure Dest before rollback.");

        var rollback = new ImportRollback();
        var manifest = rollback.FindLatestManifest(Workspace.ExportsDirectory)
            ?? throw new InvalidOperationException("No import manifest found in Workspace/Exports.");
        var result = rollback.Execute(manifest, DestRoot, Logger);
        Logger.Info($"Rollback done: {result.Removed} removed, {result.Missing} already gone, {result.Failed} failed");
        return result;
    }

    private ImportResult ExecuteImport(AssetImporter importer, List<ImportPlanItem> plans)
    {
        var manifest = new WorkspaceManifestWriter(Workspace.ExportsDirectory);
        var result = importer.Execute(plans, Logger, manifest);
        Logger.Info($"Import done: {result.Copied} copied, {result.Skipped} skipped, {result.Failed} failed");
        return result;
    }

    private void EnsureImportReady()
    {
        if (SourceRoot is null || DestRoot is null || _sourceDb is null || _destDb is null)
            throw new InvalidOperationException("Configure Source and Dest, then Scan both sides before importing.");
    }

    public void Dispose()
    {
        _sourceDb?.Dispose();
        _destDb?.Dispose();
    }
}
