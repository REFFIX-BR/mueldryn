using MUAssetInspector.Analysis;
using MUAssetInspector.Core.Database;
using MUAssetInspector.Core.Domain;
using MUAssetInspector.Core.Logging;
using MUAssetInspector.Core.Profiles;
using MUAssetInspector.Formats.Resolution;

namespace MUAssetInspector.Migration;

public enum ImportPlanKind
{
    Copy,
    SkipAlreadyPresent,
    SkipUnsupported,
    SkipSourceNotFound
}

public sealed class ImportPlanItem
{
    public required string SourceFullPath { get; init; }
    public required string DestFullPath { get; init; }
    public required string DestRelativePath { get; init; }
    public required ImportPlanKind Kind { get; init; }
    public required string Reason { get; init; }
    public bool WillCopy => Kind == ImportPlanKind.Copy;
}

public sealed class ImportResult
{
    public int Copied { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public List<string> Log { get; } = [];
    public string? ManifestPath { get; set; }
}

/// <summary>
/// Copies Mudream assets into MuMain Data using only formats MuMain loads (OZJ/OZT/BMD/txt/xml).
/// Resolves BMD texture refs like devil_wing.tga → Item/devil_wing.OZT on disk.
/// </summary>
public sealed class AssetImporter
{
    private static readonly HashSet<string> AcceptedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".bmd", ".ozj", ".ozt", ".txt", ".xml"
    };

    private static readonly HashSet<string> RejectedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".tga", ".jpg", ".jpeg", ".png", ".bmp", ".smd"
    };

    public List<ImportPlanItem> PlanFromDependencyTree(
        DependencyNode root,
        string sourceRoot,
        string destRoot,
        AssetDatabase sourceDb,
        AssetDatabase? destDb,
        ClientProfile profile)
    {
        var plans = new List<ImportPlanItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        WalkDependencies(root, root.RelativePath, plans, seen, sourceRoot, destRoot, sourceDb, destDb, profile);
        return plans;
    }

    public List<ImportPlanItem> PlanAllMissingCompatible(
        IEnumerable<ComparisonRecord> comparisons,
        AssetDatabase sourceDb,
        AssetDatabase? destDb,
        string sourceRoot,
        string destRoot,
        ClientProfile profile,
        ImportScope scope = ImportScope.CosmeticsOnly)
    {
        var plans = new List<ImportPlanItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var c in comparisons.Where(x => x.Status is ComparisonStatus.Missing or ComparisonStatus.CaseMismatch))
        {
            var source = sourceDb.AllAssets.FirstOrDefault(a => a.Id == c.SourceAssetId);
            if (source is null) continue;
            if (!ImportPathFilter.MatchesScope(source.RelativePath, scope, profile))
                continue;
            AddPlan(source.RelativePath, null, source.RelativePath, plans, seen, sourceRoot, destRoot, sourceDb, destDb, profile, "Missing in destination");
        }

        return plans;
    }

    public List<ImportPlanItem> PlanMudreamEffectBundle(string sourceRoot, string destRoot, ClientProfile profile)
    {
        var plans = new List<ImportPlanItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rel in profile.EffectTables)
        {
            AddPlan(rel, null, rel, plans, seen, sourceRoot, destRoot, null, null, profile, "Mudream effect table");
        }

        return plans.Where(p => p.WillCopy).ToList();
    }

    public ImportResult Execute(
        IEnumerable<ImportPlanItem> plans,
        FileLogger logger,
        WorkspaceManifestWriter? manifestWriter = null)
    {
        var result = new ImportResult();
        var toCopy = plans.Where(p => p.WillCopy).ToList();

        foreach (var plan in plans.Where(p => !p.WillCopy))
        {
            if (ImportPathFilter.IsEditorJunk(plan.DestRelativePath))
                continue;

            result.Skipped++;
            result.Log.Add($"SKIP: {plan.DestRelativePath} — {plan.Reason}");
        }

        foreach (var plan in toCopy)
        {
            try
            {
                if (!File.Exists(plan.SourceFullPath))
                {
                    result.Failed++;
                    result.Log.Add($"FAIL: source not found {plan.SourceFullPath}");
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(plan.DestFullPath)!);
                File.Copy(plan.SourceFullPath, plan.DestFullPath, overwrite: true);
                result.Copied++;
                result.Log.Add($"COPY: {plan.DestRelativePath}");
                logger.Info($"Imported {plan.DestRelativePath}");
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Log.Add($"FAIL: {plan.DestRelativePath} — {ex.Message}");
                logger.Error($"Import failed {plan.DestRelativePath}: {ex}");
            }
        }

        if (manifestWriter is not null && toCopy.Count > 0)
            result.ManifestPath = manifestWriter.WriteImportManifest(toCopy, result);

        return result;
    }

    private void WalkDependencies(
        DependencyNode node,
        string? ownerRelativePath,
        List<ImportPlanItem> plans,
        HashSet<string> seen,
        string sourceRoot,
        string destRoot,
        AssetDatabase sourceDb,
        AssetDatabase? destDb,
        ClientProfile profile)
    {
        if (node.Type is DependencyType.Model or DependencyType.Texture or DependencyType.Alpha or DependencyType.EffectBitmap or DependencyType.ConfigReference)
        {
            if (node.CompareStatus is ComparisonStatus.Missing or ComparisonStatus.CaseMismatch or ComparisonStatus.PresentDifferent or null)
            {
                var refName = node.RelativePath ?? node.Name;
                AddPlan(refName, ownerRelativePath, refName, plans, seen, sourceRoot, destRoot, sourceDb, destDb, profile,
                    node.CompareStatus?.ToString() ?? "Dependency");
            }
        }

        foreach (var child in node.Children)
            WalkDependencies(child, ownerRelativePath ?? node.RelativePath, plans, seen, sourceRoot, destRoot, sourceDb, destDb, profile);
    }

    private void AddPlan(
        string refName,
        string? ownerRelativePath,
        string logicalName,
        List<ImportPlanItem> plans,
        HashSet<string> seen,
        string sourceRoot,
        string destRoot,
        AssetDatabase? sourceDb,
        AssetDatabase? destDb,
        ClientProfile profile,
        string reason)
    {
        var resolvedRel = TexturePathResolver.Resolve(sourceRoot, sourceDb, ownerRelativePath, refName, profile)
            ?? refName.Replace('\\', '/');

        var ext = Path.GetExtension(resolvedRel);
        if (RejectedExtensions.Contains(ext))
        {
            var fallback = TexturePathResolver.ResolveOnDisk(sourceRoot, ownerRelativePath, refName, profile);
            if (fallback is null)
            {
                plans.Add(new ImportPlanItem
                {
                    Kind = ImportPlanKind.SkipUnsupported,
                    Reason = $"MuMain não carrega {ext} — sem .ozj/.ozt no source",
                    DestRelativePath = refName,
                    SourceFullPath = string.Empty,
                    DestFullPath = string.Empty
                });
                return;
            }
            resolvedRel = fallback;
            ext = Path.GetExtension(resolvedRel);
        }

        if (!AcceptedExtensions.Contains(ext))
        {
            plans.Add(new ImportPlanItem
            {
                Kind = ImportPlanKind.SkipUnsupported,
                Reason = $"Extensão não suportada pelo MuMain: {ext}",
                DestRelativePath = resolvedRel,
                SourceFullPath = string.Empty,
                DestFullPath = string.Empty
            });
            return;
        }

        if (!seen.Add(resolvedRel))
            return;

        var srcFull = Path.Combine(sourceRoot, resolvedRel.Replace('/', Path.DirectorySeparatorChar));
        var destFull = Path.Combine(destRoot, resolvedRel.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(srcFull))
        {
            plans.Add(new ImportPlanItem
            {
                Kind = ImportPlanKind.SkipSourceNotFound,
                Reason = "Arquivo não encontrado no source",
                DestRelativePath = resolvedRel,
                SourceFullPath = srcFull,
                DestFullPath = destFull
            });
            return;
        }

        if (File.Exists(destFull) && destDb is not null)
        {
            var existing = destDb.FindByPath(resolvedRel) ?? destDb.FindByFileName(resolvedRel);
            if (existing is not null && File.Exists(destFull))
            {
                plans.Add(new ImportPlanItem
                {
                    Kind = ImportPlanKind.SkipAlreadyPresent,
                    Reason = "Já existe no destino",
                    DestRelativePath = resolvedRel,
                    SourceFullPath = srcFull,
                    DestFullPath = destFull
                });
                return;
            }
        }
        else if (File.Exists(destFull))
        {
            plans.Add(new ImportPlanItem
            {
                Kind = ImportPlanKind.SkipAlreadyPresent,
                Reason = "Já existe no destino (disco)",
                DestRelativePath = resolvedRel,
                SourceFullPath = srcFull,
                DestFullPath = destFull
            });
            return;
        }

        plans.Add(new ImportPlanItem
        {
            Kind = ImportPlanKind.Copy,
            Reason = reason,
            DestRelativePath = resolvedRel,
            SourceFullPath = srcFull,
            DestFullPath = destFull
        });
    }
}

public sealed class WorkspaceManifestWriter
{
    private readonly string _exportsDir;

    public WorkspaceManifestWriter(string exportsDir)
    {
        _exportsDir = exportsDir;
        Directory.CreateDirectory(_exportsDir);
    }

    public string WriteImportManifest(IReadOnlyList<ImportPlanItem> copied, ImportResult result)
    {
        var path = Path.Combine(_exportsDir, $"import-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
        var payload = new
        {
            copiedUtc = DateTime.UtcNow,
            result.Copied,
            result.Skipped,
            result.Failed,
            files = copied.Select(c => new { c.DestRelativePath, c.SourceFullPath, c.Reason })
        };
        File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        return path;
    }
}
