using MUAssetInspector.Analysis;
using MUAssetInspector.Core.Domain;
using MUAssetInspector.Core.Profiles;
using MUAssetInspector.Core.Paths;
using MUAssetInspector.Core.Workspace;
using MUAssetInspector.Migration;

if (args.Length == 0)
{
    PrintHelp();
    return 1;
}

var command = args[0].ToLowerInvariant();
var projectRoot = ProjectPaths.FindProjectRoot();
var workspace = new WorkspaceManager(projectRoot);
var profiles = new ClientProfileManager(Path.Combine(projectRoot, "profiles"));
var profileName = GetArg(args, "--profile") ?? "mudream";
var profile = profiles.Load(profileName);

switch (command)
{
    case "analyze":
    {
        var source = GetArg(args, "--source") ?? throw new ArgumentException("--source required");
        var dest = GetArg(args, "--dest") ?? throw new ArgumentException("--dest required");
        var logger = new MUAssetInspector.Core.Logging.FileLogger(workspace);
        var batch = new BatchAnalyzer(logger);
        var progress = new Progress<MUAssetInspector.Core.Domain.ScanProgress>(p =>
            Console.WriteLine($"[{p.FilesProcessed}/{p.FilesDiscovered}] {p.CurrentFile}"));
        var (comparisons, summary) = await batch.AnalyzeAllAsync(source, dest, profile, workspace, progress);
        var reportDir = GetArg(args, "--report") ?? workspace.ReportsDirectory;
        Directory.CreateDirectory(reportDir);
        var exporter = new ReportExporter();
        var json = exporter.ExportJson(summary, comparisons, Path.Combine(reportDir, "cli-report.json"));
        var html = exporter.ExportHtml(summary, comparisons, Path.Combine(reportDir, "cli-report.html"));
        Console.WriteLine($"Reports: {json}, {html}");
        return 0;
    }
    case "batch":
    {
        var source = GetArg(args, "--source") ?? throw new ArgumentException("--source required");
        var dest = GetArg(args, "--dest");
        var logger = new MUAssetInspector.Core.Logging.FileLogger(workspace);
        var batch = new BatchAnalyzer(logger);
        var (comparisons, summary) = await batch.AnalyzeAllAsync(source, dest ?? source, profile, workspace);
        Console.WriteLine(summary.Entries.FirstOrDefault()?.Message ?? "Done");
        Console.WriteLine($"Compared: {comparisons.Count}");
        return 0;
    }
    case "import":
    {
        var source = GetArg(args, "--source") ?? throw new ArgumentException("--source required");
        var dest = GetArg(args, "--dest") ?? throw new ArgumentException("--dest required");
        var rel = GetArg(args, "--asset");
        var allMissing = args.Contains("--all-missing");
        var cosmeticsOnly = args.Contains("--cosmetics") || (allMissing && !args.Contains("--all"));
        var withEffects = args.Contains("--effects") || allMissing;
        if (string.IsNullOrWhiteSpace(rel) && !allMissing)
            throw new ArgumentException("Use --asset <relativePath> or --all-missing");

        var logger = new MUAssetInspector.Core.Logging.FileLogger(workspace);
        var batch = new BatchAnalyzer(logger);
        var progress = new Progress<MUAssetInspector.Core.Domain.ScanProgress>(p =>
            Console.WriteLine($"[{p.FilesProcessed}/{p.FilesDiscovered}] {p.CurrentFile}"));

        var (sourceDb, destDb) = await batch.ScanBothAsync(source, dest, profile, workspace, progress);
        try
        {
            var importer = new AssetImporter();
            var manifest = new WorkspaceManifestWriter(workspace.ExportsDirectory);
            var analyzer = new DependencyAnalyzer();
            analyzer.Configure(source, dest, profile);
            var compat = new CompatibilityEngine();

            List<ImportPlanItem> plans;
            if (allMissing)
            {
                var comparisons = compat.CompareTrees(sourceDb, destDb);
                var scope = cosmeticsOnly ? ImportScope.CosmeticsOnly : ImportScope.AllCompatible;
                plans = importer.PlanAllMissingCompatible(comparisons, sourceDb, destDb, source, dest, profile, scope);
            }
            else
            {
                var asset = sourceDb.FindByPath(rel!) ?? sourceDb.FindByFileName(rel!);
                if (asset is null)
                    throw new FileNotFoundException($"Asset not found in source scan: {rel}");
                var tree = analyzer.AnalyzeAsset(asset, sourceDb, destDb);
                plans = importer.PlanFromDependencyTree(tree, source, dest, sourceDb, destDb, profile);
            }

            if (withEffects)
                plans.AddRange(importer.PlanMudreamEffectBundle(source, dest, profile));

            var result = importer.Execute(plans, logger, manifest);
            Console.WriteLine($"Import: {result.Copied} copied, {result.Skipped} skipped, {result.Failed} failed");
            foreach (var line in result.Log.TakeLast(20))
                Console.WriteLine(line);
            if (result.ManifestPath is not null)
                Console.WriteLine($"Manifest: {result.ManifestPath}");
            return result.Failed > 0 ? 2 : 0;
        }
        finally
        {
            sourceDb.Dispose();
            destDb.Dispose();
        }
    }
    case "rollback":
    {
        var dest = GetArg(args, "--dest") ?? throw new ArgumentException("--dest required");
        var manifest = GetArg(args, "--manifest");
        var logger = new MUAssetInspector.Core.Logging.FileLogger(workspace);
        var rollback = new ImportRollback();
        manifest ??= rollback.FindLatestManifest(workspace.ExportsDirectory)
            ?? throw new InvalidOperationException("No import manifest in Workspace/Exports");
        var result = rollback.Execute(manifest, dest, logger);
        Console.WriteLine($"Rollback: {result.Removed} removed, {result.Missing} already gone, {result.Failed} failed");
        foreach (var line in result.Log.TakeLast(20))
            Console.WriteLine(line);
        return result.Failed > 0 ? 2 : 0;
    }
    case "audit-cosmetics":
    {
        var source = GetArg(args, "--source") ?? throw new ArgumentException("--source required");
        var dest = GetArg(args, "--dest") ?? throw new ArgumentException("--dest required");
        var catalog = GetArg(args, "--catalog")
            ?? Path.GetFullPath(Path.Combine(projectRoot, "..", "..", "MuMain", "src", "source", "Data", "GameData", "ItemData", "MudreamCosmeticCatalog.Generated.cpp"));
        if (!File.Exists(catalog))
            throw new FileNotFoundException("Catalog not found", catalog);

        var auditor = new CosmeticAuditor();
        Console.WriteLine($"Auditing catalog: {catalog}");
        var report = auditor.Run(source, dest, catalog, profile);
        var path = auditor.WriteJson(report, workspace.ExportsDirectory);
        Console.WriteLine($"Catalog items: {report.CatalogItems}");
        Console.WriteLine($"OK: {report.Ok} | missing BMD: {report.MissingBmd} | missing textures: {report.MissingTextures} | no effects: {report.NoEffects} | empty model: {report.EmptyModel}");
        Console.WriteLine($"Effect tables identical: {report.EffectTablesIdentical}");
        foreach (var (status, count) in report.ByStatus.OrderByDescending(kv => kv.Value))
            Console.WriteLine($"  {status}: {count}");
        var pets = report.Items.Where(i => i.Group == 13).ToList();
        Console.WriteLine($"Pets/helpers (group 13): {pets.Count} | problems: {pets.Count(p => p.Status != "ok")}");
        foreach (var p in pets.Where(x => x.Status != "ok").Take(25))
            Console.WriteLine($"  PET {p.Number} {p.Name}: {p.Status} — {string.Join("; ", p.Issues)}");
        Console.WriteLine($"Report: {path}");
        return report.ProblemItems.Count > 0 ? 2 : 0;
    }
    default:
        PrintHelp();
        return 1;
}

static string? GetArg(string[] args, string name)
{
    var idx = Array.IndexOf(args, name);
    return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
}

static void PrintHelp()
{
    Console.WriteLine("""
        MUAssetInspector.Cli
          analyze --source <path> --dest <path> [--profile mudream] [--report <dir>]
          batch --source <path> [--dest <path>] [--profile mudream]
          import --source <path> --dest <path> --asset <relativePath> [--profile mudream] [--effects]
          import --source <path> --dest <path> --all-missing [--cosmetics] [--all] [--effects]
          rollback --dest <path> [--manifest <import-json>]
          audit-cosmetics --source <path> --dest <path> [--catalog <MudreamCosmeticCatalog.Generated.cpp>]
        """);
}
