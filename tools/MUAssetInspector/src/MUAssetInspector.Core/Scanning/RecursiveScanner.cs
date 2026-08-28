using MUAssetInspector.Core.Database;
using MUAssetInspector.Core.Domain;
using MUAssetInspector.Core.Hashing;

namespace MUAssetInspector.Core.Scanning;

public sealed class RecursiveScanner
{
    private static readonly string[] PriorityRelativePaths =
    [
        "Local/txt",
        "Local/Renewal",
        "Local/Server",
        "Item/CustomItem",
        "Effect",
        "Player"
    ];

    public async Task<ScanProgress> ScanAsync(
        string dataRoot,
        ClientRole role,
        AssetDatabase database,
        AssetFormatManagerBridge formatBridge,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var scanProgress = new ScanProgress();
        if (!Directory.Exists(dataRoot))
            throw new DirectoryNotFoundException(dataRoot);

        var files = DiscoverFiles(dataRoot).ToList();
        scanProgress.FilesDiscovered = files.Count;

        var processed = 0;
        var skipped = 0;

        await Parallel.ForEachAsync(files, cancellationToken, async (fullPath, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(dataRoot, fullPath).Replace('\\', '/');
            var info = new FileInfo(fullPath);

            var existing = database.FindByPath(relative);
            if (existing is not null &&
                existing.Size == info.Length &&
                existing.LastWriteUtcTicks == info.LastWriteTimeUtc.Ticks)
            {
                Interlocked.Increment(ref skipped);
                scanProgress.FilesSkipped = skipped;
                return;
            }

            var sha = await StreamingHash.ComputeSha256Async(fullPath, ct).ConfigureAwait(false);
            var ext = Path.GetExtension(fullPath);
            var record = new AssetRecord
            {
                ClientRole = role,
                RelativePath = relative,
                FileNameLower = Path.GetFileName(fullPath).ToLowerInvariant(),
                Extension = ext,
                Sha256 = sha,
                Size = info.Length,
                LastWriteUtcTicks = info.LastWriteTimeUtc.Ticks,
                Category = ClassifyCategory(relative)
            };

            try
            {
                var parse = formatBridge.TryParse(fullPath, relative, role);
                if (parse is not null)
                {
                    record.Format = parse.Format;
                    record.ParseStatus = parse.Status;
                    record.Width = parse.Width;
                    record.Height = parse.Height;
                    record.HasAlpha = parse.HasAlpha;
                    record.AlphaKind = parse.AlphaKind;
                    record.ParsedJson = parse.ToJson();
                }
            }
            catch
            {
                record.ParseStatus = ParseStatus.Failed;
            }

            database.UpsertAsset(record);
            Interlocked.Increment(ref processed);
            scanProgress.FilesProcessed = processed;
            scanProgress.CurrentFile = relative;
            progress?.Report(scanProgress);
        }).ConfigureAwait(false);

        scanProgress.IsComplete = true;
        progress?.Report(scanProgress);
        return scanProgress;
    }

    public static IEnumerable<string> DiscoverFiles(string dataRoot)
    {
        foreach (var file in Directory.EnumerateFiles(dataRoot, "*.*", SearchOption.AllDirectories))
        {
            yield return file;
        }
    }

    public static AssetCategory ClassifyCategory(string relativePath)
    {
        var p = relativePath.Replace('\\', '/');
        if (p.Contains("/Effect/", StringComparison.OrdinalIgnoreCase) || p.StartsWith("Effect/", StringComparison.OrdinalIgnoreCase))
            return AssetCategory.Effects;
        if (p.Contains("/Wing", StringComparison.OrdinalIgnoreCase) || p.Contains("Wing5", StringComparison.OrdinalIgnoreCase))
            return AssetCategory.Wings;
        if (p.Contains("/Skin/", StringComparison.OrdinalIgnoreCase))
            return AssetCategory.Sets;
        if (p.Contains("/Player/", StringComparison.OrdinalIgnoreCase))
            return AssetCategory.Player;
        if (p.EndsWith(".ozj", StringComparison.OrdinalIgnoreCase) ||
            p.EndsWith(".ozt", StringComparison.OrdinalIgnoreCase) ||
            p.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
            p.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            return AssetCategory.Textures;
        if (p.Contains("/Local/", StringComparison.OrdinalIgnoreCase))
            return AssetCategory.Config;
        return AssetCategory.Other;
    }
}

public interface AssetFormatManagerBridge
{
    AssetParseResult? TryParse(string fullPath, string relativePath, ClientRole role);
}
