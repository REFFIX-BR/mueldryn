using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MUAssetInspector.App.Services;
using MUAssetInspector.Core.Domain;
using MUAssetInspector.Core.Paths;
using MUAssetInspector.Core.Scanning;
using MUAssetInspector.Formats.Resolution;

namespace MUAssetInspector.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly InspectorSession _session;
    private CancellationTokenSource? _scanCts;

    [ObservableProperty] private string _sourceRoot = string.Empty;
    [ObservableProperty] private string _destRoot = string.Empty;
    [ObservableProperty] private string _selectedProfile = "mudream";
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _statusText = "Ready";
    [ObservableProperty] private string _logTail = string.Empty;
    [ObservableProperty] private string _inspectorText = "Select an asset";
    [ObservableProperty] private string _diagnosticText = string.Empty;
    [ObservableProperty] private Bitmap? _previewImage;
    [ObservableProperty] private Bitmap? _alphaImage;
    [ObservableProperty] private AssetTreeNodeViewModel? _selectedNode;

    public ObservableCollection<string> Profiles { get; } = [];
    public ObservableCollection<AssetTreeNodeViewModel> AssetTree { get; } = [];
    public ObservableCollection<DependencyLineViewModel> Dependencies { get; } = [];
    public ObservableCollection<string> CategoryFilters { get; } =
    [
        "All", "Sets", "Swords", "Wings", "Helpers", "Effects", "Textures", "Config"
    ];

    [ObservableProperty] private string _selectedCategory = "All";

    public MainViewModel() : this(CreateDefaultSession()) { }

    public MainViewModel(InspectorSession session)
    {
        _session = session;
        foreach (var p in _session.Profiles.ListProfiles())
            Profiles.Add(p);
        if (Profiles.Count > 0)
            SelectedProfile = Profiles.Contains("mudream") ? "mudream" : Profiles[0];
    }

    private static InspectorSession CreateDefaultSession() =>
        new(ProjectPaths.FindProjectRoot());

    public void SetSourceRoot(string path) => SourceRoot = DataRootNormalizer.Normalize(path) ?? path;
    public void SetDestRoot(string path) => DestRoot = DataRootNormalizer.Normalize(path) ?? path;

    [RelayCommand]
    private async Task ScanAsync()
    {
        if (string.IsNullOrWhiteSpace(SourceRoot) && string.IsNullOrWhiteSpace(DestRoot))
        {
            StatusText = "Set at least one Data root.";
            return;
        }

        _scanCts?.Cancel();
        _scanCts = new CancellationTokenSource();
        _session.SetProfile(SelectedProfile);
        _session.SetRoots(
            string.IsNullOrWhiteSpace(SourceRoot) ? null : DataRootNormalizer.Normalize(SourceRoot),
            string.IsNullOrWhiteSpace(DestRoot) ? null : DataRootNormalizer.Normalize(DestRoot));

        var progress = new Progress<ScanProgress>(p =>
        {
            StatusText = $"Scanning {p.CurrentFile} ({p.FilesProcessed}/{p.FilesDiscovered}, skipped {p.FilesSkipped})";
        });

        try
        {
            if (!string.IsNullOrWhiteSpace(SourceRoot))
                await _session.ScanAsync(ClientRole.Source, progress, _scanCts.Token);
            if (!string.IsNullOrWhiteSpace(DestRoot))
                await _session.ScanAsync(ClientRole.Destination, progress, _scanCts.Token);

            _session.ImportCatalogIfConfigured();
            RefreshTree();
            StatusText = "Scan complete";
            LogTail = _session.Logger.Tail();
        }
        catch (Exception ex)
        {
            StatusText = $"Scan failed: {ex.Message}";
            _session.Logger.Error(ex.ToString());
            LogTail = _session.Logger.Tail();
        }
    }

    [RelayCommand]
    private void RefreshTreeCommand() => RefreshTree();

    public void RefreshTree()
    {
        AssetTree.Clear();
        var db = _session.SourceDb ?? _session.DestDb;
        if (db is null) return;

        AssetCategory? cat = SelectedCategory switch
        {
            "Sets" => AssetCategory.Sets,
            "Wings" => AssetCategory.Wings,
            "Effects" => AssetCategory.Effects,
            "Textures" => AssetCategory.Textures,
            "Config" => AssetCategory.Config,
            "Player" => AssetCategory.Player,
            _ => null
        };

        var groups = db.QueryAssetsSnapshot(SearchText, cat)
            .GroupBy(a => Path.GetDirectoryName(a.RelativePath)?.Replace('\\', '/') ?? "")
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            var folder = new AssetTreeNodeViewModel { Name = string.IsNullOrEmpty(group.Key) ? "(root)" : group.Key, IsFolder = true };
            foreach (var asset in group.OrderBy(a => a.RelativePath, StringComparer.OrdinalIgnoreCase))
                folder.Children.Add(new AssetTreeNodeViewModel { Name = Path.GetFileName(asset.RelativePath), Asset = asset });
            AssetTree.Add(folder);
        }
    }

    partial void OnSelectedNodeChanged(AssetTreeNodeViewModel? value) => _ = SelectAssetAsync(value);

    public async Task SelectAssetAsync(AssetTreeNodeViewModel? node)
    {
        if (node?.Asset is null) return;
        var asset = node.Asset;
        InspectorText = $"""
            Path: {asset.RelativePath}
            Format: {asset.Format}
            SHA-256: {asset.Sha256}
            Size: {asset.Size:N0} bytes
            Parse: {asset.ParseStatus}
            Alpha: {asset.AlphaKind}
            Dimensions: {asset.Width}x{asset.Height}
            """;

        Dependencies.Clear();
        if (_session.SourceDb is not null)
        {
            var tree = _session.DependencyAnalyzer.AnalyzeAsset(asset, _session.SourceDb, _session.DestDb);
            FlattenDependencies(tree, 0);
            if (_session.DestDb is not null)
            {
                var diag = _session.CompatibilityEngine.Diagnose(tree, _session.SourceDb, _session.DestDb);
                DiagnosticText = string.Join(Environment.NewLine, diag.Entries.Select(e => $"[{e.Severity}] {e.Message}" + (e.Solution is null ? "" : $" -> {e.Solution}")));
            }
        }

        var previewFile = _session.ResolvePreviewFile(asset);
        PreviewImage = null;
        AlphaImage = null;

        if (previewFile is not null && File.Exists(previewFile))
        {
            InspectorText += $"\nPreview: {previewFile}";
            PreviewImage = _session.LoadPreviewBitmap(previewFile);
            await Task.Run(() =>
            {
                using var alpha = _session.TextureLoader.LoadAlphaChannel(previewFile, _session.ActiveProfile);
                if (alpha is null) return;
                using var ms = new MemoryStream();
                alpha.Save(ms, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
                ms.Position = 0;
                AlphaImage = new Bitmap(ms);
            });
        }
        else if (asset.Format == AssetFormatKind.BmdMesh)
        {
            var tex = TexturePathResolver.ExtractTextureNames(asset.ParsedJson).FirstOrDefault();
            InspectorText += tex is null
                ? "\nPreview: BMD sem textura embarcada"
                : $"\nPreview: textura '{tex}' não encontrada (.ozj/.ozt)";
        }
        else if (asset.Format is AssetFormatKind.Ozj or AssetFormatKind.Ozt or AssetFormatKind.PlainImage)
        {
            InspectorText += "\nPreview: arquivo de imagem não encontrado no disco";
        }
    }

    private void FlattenDependencies(DependencyNode node, int depth)
    {
        Dependencies.Add(new DependencyLineViewModel
        {
            Indent = depth,
            Text = $"{node.Type}: {node.Name}" + (node.CompareStatus is null ? "" : $" [{node.CompareStatus}]")
        });
        foreach (var child in node.Children)
            FlattenDependencies(child, depth + 1);
    }

    [RelayCommand]
    private async Task ImportSelectedAsync()
    {
        if (SelectedNode?.Asset is null)
        {
            StatusText = "Select an asset to import.";
            return;
        }

        try
        {
            StatusText = "Importing selected asset chain...";
            var result = await Task.Run(() => _session.ImportSelectedAsset(SelectedNode.Asset));
            await RescanDestAsync();
            StatusText = $"Import: {result.Copied} copied, {result.Skipped} skipped, {result.Failed} failed";
            LogTail = string.Join(Environment.NewLine, result.Log.TakeLast(40));
            if (SelectedNode?.Asset is not null)
                await SelectAssetAsync(SelectedNode);
        }
        catch (Exception ex)
        {
            StatusText = $"Import failed: {ex.Message}";
            _session.Logger.Error(ex.ToString());
            LogTail = _session.Logger.Tail();
        }
    }

    [RelayCommand]
    private async Task ImportCosmeticsMissingAsync()
    {
        try
        {
            StatusText = "Importing missing cosmetics (Item/Player/Effect/Local/Skill)...";
            var result = await Task.Run(() => _session.ImportAllMissingCompatible(ImportScope.CosmeticsOnly, includeEffectTables: true));
            await RescanDestAsync();
            StatusText = $"Cosmetics import: {result.Copied} copied, {result.Skipped} skipped, {result.Failed} failed";
            LogTail = string.Join(Environment.NewLine, result.Log.TakeLast(40));
            RefreshTree();
        }
        catch (Exception ex)
        {
            StatusText = $"Import failed: {ex.Message}";
            _session.Logger.Error(ex.ToString());
            LogTail = _session.Logger.Tail();
        }
    }

    [RelayCommand]
    private async Task ImportAllMissingAsync()
    {
        try
        {
            StatusText = "Importing ALL missing files (maps, monsters, shop...) — pode dar bigode";
            var result = await Task.Run(() => _session.ImportAllMissingCompatible(ImportScope.AllCompatible, includeEffectTables: true));
            await RescanDestAsync();
            StatusText = $"Full import: {result.Copied} copied, {result.Skipped} skipped, {result.Failed} failed";
            LogTail = string.Join(Environment.NewLine, result.Log.TakeLast(40));
            RefreshTree();
        }
        catch (Exception ex)
        {
            StatusText = $"Import failed: {ex.Message}";
            _session.Logger.Error(ex.ToString());
            LogTail = _session.Logger.Tail();
        }
    }

    [RelayCommand]
    private async Task RollbackLastImportAsync()
    {
        try
        {
            StatusText = "Rolling back last import...";
            var result = await Task.Run(() => _session.RollbackLastImport());
            await RescanDestAsync();
            StatusText = $"Rollback: {result.Removed} removed, {result.Missing} already gone, {result.Failed} failed";
            LogTail = string.Join(Environment.NewLine, result.Log.TakeLast(40));
            RefreshTree();
        }
        catch (Exception ex)
        {
            StatusText = $"Rollback failed: {ex.Message}";
            _session.Logger.Error(ex.ToString());
            LogTail = _session.Logger.Tail();
        }
    }

    private async Task RescanDestAsync()
    {
        if (string.IsNullOrWhiteSpace(DestRoot)) return;
        var progress = new Progress<ScanProgress>(p => StatusText = $"Re-scan dest {p.FilesProcessed}/{p.FilesDiscovered}");
        await _session.ScanAsync(ClientRole.Destination, progress, CancellationToken.None);
    }

    [RelayCommand]
    private void ExportReport()
    {
        if (_session.SourceDb is null || _session.DestDb is null)
        {
            StatusText = "Need both source and destination scanned.";
            return;
        }

        var comparisons = _session.CompatibilityEngine.CompareTrees(_session.SourceDb, _session.DestDb);
        var diag = new DiagnosticResult
        {
            Subject = "Full workspace",
            Entries = [new DiagnosticEntry { Severity = DiagnosticSeverity.Info, Message = $"{comparisons.Count} assets compared" }]
        };
        var json = _session.ReportExporter.ExportJson(diag, comparisons, Path.Combine(_session.Workspace.ReportsDirectory, "report.json"));
        var html = _session.ReportExporter.ExportHtml(diag, comparisons, Path.Combine(_session.Workspace.ReportsDirectory, "report.html"));
        StatusText = $"Exported {json} and {html}";
        LogTail = _session.Logger.Tail();
    }

    partial void OnSearchTextChanged(string value) => RefreshTree();
    partial void OnSelectedCategoryChanged(string value) => RefreshTree();
}

public sealed class AssetTreeNodeViewModel
{
    public string Name { get; set; } = string.Empty;
    public bool IsFolder { get; set; }
    public AssetRecord? Asset { get; set; }
    public ObservableCollection<AssetTreeNodeViewModel> Children { get; } = [];
    public override string ToString() => Name;
}

public sealed class DependencyLineViewModel
{
    public int Indent { get; set; }
    public string Text { get; set; } = string.Empty;
}
