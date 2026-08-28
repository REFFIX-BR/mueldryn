using System.Text;
using System.Text.Json;
using MUAssetInspector.Core.Database;
using MUAssetInspector.Core.Domain;

namespace MUAssetInspector.Analysis;

public sealed class CompatibilityEngine
{
    public DiagnosticResult Diagnose(DependencyNode root, AssetDatabase sourceDb, AssetDatabase destDb)
    {
        var result = new DiagnosticResult { Subject = root.Name };
        Walk(root, sourceDb, destDb, result);
        result.Level = result.Entries.Any(e => e.Severity == DiagnosticSeverity.Error)
            ? CompatibilityLevel.Incompatible
            : result.Entries.Any(e => e.Severity == DiagnosticSeverity.Warning)
                ? CompatibilityLevel.PartiallyCompatible
                : CompatibilityLevel.FullyCompatible;
        return result;
    }

    public List<ComparisonRecord> CompareTrees(AssetDatabase sourceDb, AssetDatabase destDb, IEnumerable<AssetRecord>? subset = null)
    {
        var records = new List<ComparisonRecord>();
        foreach (var source in subset ?? sourceDb.AllAssets)
        {
            var dest = destDb.FindByPath(source.RelativePath);
            ComparisonStatus status;
            if (dest is null)
            {
                var byName = destDb.FindByFileName(source.RelativePath);
                status = byName is null ? ComparisonStatus.Missing : ComparisonStatus.CaseMismatch;
                dest = byName;
            }
            else if (string.Equals(source.Sha256, dest.Sha256, StringComparison.OrdinalIgnoreCase))
                status = ComparisonStatus.Identical;
            else
                status = ComparisonStatus.PresentDifferent;

            var rec = new ComparisonRecord
            {
                SourceAssetId = source.Id,
                DestAssetId = dest?.Id,
                Status = status,
                DiffJson = JsonSerializer.Serialize(new { source.RelativePath, destPath = dest?.RelativePath })
            };
            records.Add(rec);
            destDb.SaveComparison(rec);
        }
        return records;
    }

    private static void Walk(DependencyNode node, AssetDatabase sourceDb, AssetDatabase destDb, DiagnosticResult result)
    {
        if (node.CompareStatus is ComparisonStatus.Missing)
        {
            result.Entries.Add(new DiagnosticEntry
            {
                Severity = DiagnosticSeverity.Error,
                Message = $"{node.Type} '{node.Name}' missing in destination",
                Solution = node.RelativePath is not null ? $"Copy from Source: {node.RelativePath}" : null,
                RefName = node.Name
            });
        }
        else if (node.CompareStatus is ComparisonStatus.CaseMismatch)
        {
            result.Entries.Add(new DiagnosticEntry
            {
                Severity = DiagnosticSeverity.Warning,
                Message = $"Case mismatch for '{node.Name}'",
                Solution = "Rename destination file to match source casing",
                RefName = node.Name
            });
        }
        else if (node.CompareStatus is ComparisonStatus.PresentDifferent)
        {
            result.Entries.Add(new DiagnosticEntry
            {
                Severity = DiagnosticSeverity.Warning,
                Message = $"'{node.Name}' exists but content differs",
                Solution = "Review hash diff and re-copy if source is authoritative",
                RefName = node.Name
            });
        }

        foreach (var child in node.Children)
            Walk(child, sourceDb, destDb, result);
    }
}

public sealed class ReportExporter
{
    public string ExportJson(DiagnosticResult diagnostic, IEnumerable<ComparisonRecord> comparisons, string outputPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        var payload = new
        {
            generatedUtc = DateTime.UtcNow,
            diagnostic,
            comparisons = comparisons.Select(c => new { c.SourceAssetId, c.DestAssetId, c.Status, c.DiffJson })
        };
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(outputPath, json, Encoding.UTF8);
        return outputPath;
    }

    public string ExportHtml(DiagnosticResult diagnostic, IEnumerable<ComparisonRecord> comparisons, string outputPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'><title>MU Asset Inspector Report</title>");
        sb.AppendLine("<style>body{font-family:Segoe UI,sans-serif;margin:24px} .err{color:#c00}.warn{color:#a60}.ok{color:#080}</style></head><body>");
        sb.AppendLine($"<h1>Diagnostic: {System.Net.WebUtility.HtmlEncode(diagnostic.Subject)}</h1>");
        sb.AppendLine($"<p>Compatibility: <strong>{diagnostic.Level}</strong></p><ul>");
        foreach (var entry in diagnostic.Entries)
        {
            var cls = entry.Severity switch
            {
                DiagnosticSeverity.Error => "err",
                DiagnosticSeverity.Warning => "warn",
                _ => "ok"
            };
            sb.AppendLine($"<li class='{cls}'>[{entry.Severity}] {System.Net.WebUtility.HtmlEncode(entry.Message)}");
            if (!string.IsNullOrWhiteSpace(entry.Solution))
                sb.AppendLine($"<br/><em>{System.Net.WebUtility.HtmlEncode(entry.Solution)}</em>");
            sb.AppendLine("</li>");
        }
        sb.AppendLine("</ul><h2>Comparisons</h2><table border='1' cellpadding='6'><tr><th>Source</th><th>Status</th></tr>");
        foreach (var c in comparisons.Take(5000))
            sb.AppendLine($"<tr><td>{c.SourceAssetId}</td><td>{c.Status}</td></tr>");
        sb.AppendLine("</table></body></html>");
        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        return outputPath;
    }
}
