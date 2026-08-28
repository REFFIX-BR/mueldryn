using System.Text.Json;
using MUAssetInspector.Core.Logging;

namespace MUAssetInspector.Migration;

public sealed class RollbackResult
{
    public int Removed { get; set; }
    public int Missing { get; set; }
    public int Failed { get; set; }
    public List<string> Log { get; } = [];
    public string ManifestPath { get; set; } = string.Empty;
}

public sealed class ImportRollback
{
    public string? FindLatestManifest(string exportsDirectory)
    {
        if (!Directory.Exists(exportsDirectory))
            return null;

        return Directory.GetFiles(exportsDirectory, "import-*.json")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    public RollbackResult Execute(string manifestPath, string destRoot, FileLogger logger)
    {
        var result = new RollbackResult { ManifestPath = manifestPath };
        if (!File.Exists(manifestPath))
            throw new FileNotFoundException("Import manifest not found", manifestPath);

        using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
        if (!doc.RootElement.TryGetProperty("files", out var files) || files.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("Manifest has no files array");

        foreach (var entry in files.EnumerateArray())
        {
            if (!entry.TryGetProperty("DestRelativePath", out var relProp))
                continue;

            var rel = relProp.GetString();
            if (string.IsNullOrWhiteSpace(rel))
                continue;

            var full = Path.Combine(destRoot, rel.Replace('/', Path.DirectorySeparatorChar));
            try
            {
                if (!File.Exists(full))
                {
                    result.Missing++;
                    result.Log.Add($"MISS: {rel}");
                    continue;
                }

                File.Delete(full);
                result.Removed++;
                result.Log.Add($"DEL: {rel}");
                logger.Info($"Rollback removed {rel}");
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Log.Add($"FAIL: {rel} — {ex.Message}");
                logger.Error($"Rollback failed {rel}: {ex}");
            }
        }

        PruneEmptyDirectories(destRoot, result);
        return result;
    }

    private static void PruneEmptyDirectories(string destRoot, RollbackResult result)
    {
        try
        {
            foreach (var dir in Directory.EnumerateDirectories(destRoot, "*", SearchOption.AllDirectories)
                         .OrderByDescending(d => d.Length))
            {
                if (!Directory.EnumerateFileSystemEntries(dir).Any())
                {
                    Directory.Delete(dir);
                    result.Log.Add($"RMDIR: {Path.GetRelativePath(destRoot, dir)}");
                }
            }
        }
        catch (Exception ex)
        {
            result.Log.Add($"WARN: prune dirs — {ex.Message}");
        }
    }
}
