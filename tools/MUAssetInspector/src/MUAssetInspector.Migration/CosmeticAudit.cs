using System.Text.Json;
using System.Text.RegularExpressions;
using MUAssetInspector.Core.Domain;
using MUAssetInspector.Core.Profiles;
using MUAssetInspector.Formats.Plugins;
using MUAssetInspector.Formats.Resolution;

namespace MUAssetInspector.Migration;

public sealed class CosmeticAuditItem
{
    public int Group { get; init; }
    public int Number { get; init; }
    public string Name { get; init; } = "";
    public int ItemIndex { get; init; }
    public string Status { get; init; } = "ok";
    public string? ModelRel { get; init; }
    public bool HasStaticFx { get; init; }
    public bool HasDynamicFx { get; init; }
    public bool HasMeshFx { get; init; }
    public bool HasSetFx { get; init; }
    public bool HasPetStaticFx { get; init; }
    public bool HasPetDynamicFx { get; init; }
    public int TexturesResolved { get; init; }
    public List<string> TexturesMissing { get; init; } = [];
    public List<string> Issues { get; init; } = [];
}

public sealed class CosmeticAuditReport
{
    public DateTime Utc { get; init; } = DateTime.UtcNow;
    public string SourceRoot { get; init; } = "";
    public string DestRoot { get; init; } = "";
    public int CatalogItems { get; set; }
    public int Ok { get; set; }
    public int MissingBmd { get; set; }
    public int MissingTextures { get; set; }
    public int NoEffects { get; set; }
    public int EmptyModel { get; set; }
    public bool EffectTablesIdentical { get; set; }
    public List<string> SourceOnlyEffectFiles { get; init; } = [];
    public Dictionary<string, int> ByStatus { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, Dictionary<string, int>> ByGroup { get; init; } = new();
    public List<CosmeticAuditItem> Items { get; init; } = [];
    public List<CosmeticAuditItem> ProblemItems => Items.Where(i => i.Status != "ok").ToList();
}

public sealed class CosmeticAuditor
{
    private static readonly Regex CatalogRegex = new(
        @"\{\s*(?<g>\d+)\s*,\s*(?<n>\d+)\s*,\s*L""(?<name>[^""]*)""\s*,\s*\d+\s*,\s*\d+\s*,\s*\d+\s*,\s*L""(?<dir>[^""]*)""\s*,\s*L""(?<file>[^""]*)""",
        RegexOptions.Compiled);

    private static readonly string[] EffectTableRels =
    [
        "Local/txt/CustomEffect(Static).txt",
        "Local/txt/CustomEffect(Dynamic).txt",
        "Local/txt/CSetEffect.txt",
        "Local/txt/CEffectRenderMesh.txt",
        "Local/txt/EffectID.txt",
        "Local/txt/Pets/PetEffectStatic.txt",
        "Local/txt/Pets/PetEffectDynamic.txt"
    ];

    public CosmeticAuditReport Run(string sourceRoot, string destRoot, string catalogCppPath, ClientProfile profile)
    {
        var report = new CosmeticAuditReport { SourceRoot = sourceRoot, DestRoot = destRoot };
        report.EffectTablesIdentical = CompareEffectTables(sourceRoot, destRoot, report.SourceOnlyEffectFiles);

        var staticFx = LoadItemIndexes(Path.Combine(destRoot, "Local", "txt", "CustomEffect(Static).txt"));
        var dynFx = LoadItemIndexes(Path.Combine(destRoot, "Local", "txt", "CustomEffect(Dynamic).txt"));
        var meshFx = LoadItemIndexes(Path.Combine(destRoot, "Local", "txt", "CEffectRenderMesh.txt"));
        var petStatic = LoadItemIndexes(Path.Combine(destRoot, "Local", "txt", "Pets", "PetEffectStatic.txt"));
        var petDyn = LoadItemIndexes(Path.Combine(destRoot, "Local", "txt", "Pets", "PetEffectDynamic.txt"));
        var setFx = LoadSetKeys(Path.Combine(destRoot, "Local", "txt", "CSetEffect.txt"));

        var bmdPlugin = new BmdMeshPlugin();

        foreach (Match m in CatalogRegex.Matches(File.ReadAllText(catalogCppPath)))
        {
            var group = int.Parse(m.Groups["g"].Value);
            var number = int.Parse(m.Groups["n"].Value);
            var name = m.Groups["name"].Value;
            var dir = m.Groups["dir"].Value.Replace('/', '\\');
            var file = m.Groups["file"].Value;
            var itemIndex = group * 512 + number;
            var issues = new List<string>();
            var missingTex = new List<string>();
            var status = "ok";
            string? modelRel = null;
            var texResolved = 0;

            if (string.IsNullOrWhiteSpace(dir) || string.IsNullOrWhiteSpace(file))
            {
                status = "empty_model";
                report.EmptyModel++;
                issues.Add("Catalog ModelDir/ModelFile vazios");
            }
            else
            {
                var dirNorm = dir
                    .Replace("\\\\", "\\")
                    .Replace('/', '\\')
                    .Trim();
                if (dirNorm.StartsWith("Data\\", StringComparison.OrdinalIgnoreCase))
                    dirNorm = dirNorm["Data\\".Length..];
                dirNorm = dirNorm.Trim('\\');
                modelRel = Path.Combine(dirNorm, file + ".bmd").Replace('\\', '/');
                var destBmd = Path.Combine(destRoot, modelRel.Replace('/', Path.DirectorySeparatorChar));
                var srcBmd = Path.Combine(sourceRoot, modelRel.Replace('/', Path.DirectorySeparatorChar));

                if (!File.Exists(destBmd))
                {
                    status = "missing_bmd";
                    report.MissingBmd++;
                    issues.Add($"BMD ausente no dest: {modelRel}");
                    if (File.Exists(srcBmd))
                        issues.Add("BMD existe no Mudream source");
                }
                else
                {
                    var ctx = new ParseContext { Profile = profile, DataRoot = destRoot, RelativePath = modelRel };
                    var parsed = bmdPlugin.Parse(destBmd, ctx);
                    foreach (var tex in parsed.TextureNames)
                    {
                        var resolved = TexturePathResolver.ResolveOnDisk(destRoot, modelRel, tex, profile);
                        if (resolved is null)
                        {
                            // Prefer companion folder on source to confirm real gap
                            var onSource = TexturePathResolver.ResolveOnDisk(sourceRoot, modelRel, tex, profile);
                            missingTex.Add(onSource is null ? $"{tex} (também ausente no Mudream)" : $"{tex} → precisa {onSource}");
                        }
                        else
                        {
                            texResolved++;
                        }
                    }

                    if (missingTex.Count > 0)
                    {
                        if (status == "ok") status = "missing_textures";
                        report.MissingTextures++;
                        issues.Add($"Texturas faltando: {string.Join(", ", missingTex)}");
                    }
                }
            }

            var hasStatic = staticFx.Contains(itemIndex);
            var hasDyn = dynFx.Contains(itemIndex);
            var hasMesh = meshFx.Contains(itemIndex);
            var hasPetS = petStatic.Contains(itemIndex);
            var hasPetD = petDyn.Contains(itemIndex);
            var hasSet = setFx.Contains($"{group}:{number}");
            var hasAnyFx = hasStatic || hasDyn || hasMesh || hasPetS || hasPetD || hasSet;
            if (!hasAnyFx)
            {
                report.NoEffects++;
                if (status == "ok") status = "no_effects";
                issues.Add("Sem linha em CustomEffect/PetEffect/CEffectRenderMesh/CSetEffect");
            }

            if (status == "ok") report.Ok++;

            var item = new CosmeticAuditItem
            {
                Group = group,
                Number = number,
                Name = name,
                ItemIndex = itemIndex,
                Status = status,
                ModelRel = modelRel,
                HasStaticFx = hasStatic,
                HasDynamicFx = hasDyn,
                HasMeshFx = hasMesh,
                HasSetFx = hasSet,
                HasPetStaticFx = hasPetS,
                HasPetDynamicFx = hasPetD,
                TexturesResolved = texResolved,
                TexturesMissing = missingTex,
                Issues = issues
            };
            report.Items.Add(item);

            report.ByStatus.TryGetValue(status, out var c);
            report.ByStatus[status] = c + 1;

            var gk = group.ToString();
            if (!report.ByGroup.TryGetValue(gk, out var gmap))
            {
                gmap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                report.ByGroup[gk] = gmap;
            }
            gmap.TryGetValue("total", out var total);
            gmap["total"] = total + 1;
            gmap.TryGetValue(status, out var sc);
            gmap[status] = sc + 1;
        }

        report.CatalogItems = report.Items.Count;
        return report;
    }

    public string WriteJson(CosmeticAuditReport report, string exportsDir)
    {
        Directory.CreateDirectory(exportsDir);
        var path = Path.Combine(exportsDir, $"cosmetic-audit-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
        var slim = new
        {
            report.Utc,
            report.SourceRoot,
            report.DestRoot,
            report.CatalogItems,
            report.Ok,
            report.MissingBmd,
            report.MissingTextures,
            report.NoEffects,
            report.EmptyModel,
            report.EffectTablesIdentical,
            report.SourceOnlyEffectFiles,
            report.ByStatus,
            report.ByGroup,
            problemItems = report.ProblemItems.Select(i => new
            {
                i.Group, i.Number, i.Name, i.ItemIndex, i.Status, i.ModelRel,
                i.HasStaticFx, i.HasDynamicFx, i.HasMeshFx, i.HasSetFx,
                i.HasPetStaticFx, i.HasPetDynamicFx,
                i.TexturesResolved, i.TexturesMissing, i.Issues
            }),
            pets = report.Items.Where(i => i.Group == 13).Select(i => new
            {
                i.Number, i.Name, i.ItemIndex, i.Status, i.ModelRel,
                i.HasPetStaticFx, i.HasPetDynamicFx, i.HasStaticFx, i.HasDynamicFx, i.HasMeshFx, i.Issues
            }),
            wings = report.Items.Where(i => i.Group == 12).Select(i => new
            {
                i.Number, i.Name, i.Status, i.HasStaticFx, i.HasDynamicFx, i.HasMeshFx, i.TexturesMissing
            })
        };
        File.WriteAllText(path, JsonSerializer.Serialize(slim, new JsonSerializerOptions { WriteIndented = true }));
        return path;
    }

    private static bool CompareEffectTables(string sourceRoot, string destRoot, List<string> sourceOnly)
    {
        var identical = true;
        foreach (var rel in EffectTableRels)
        {
            var s = Path.Combine(sourceRoot, rel.Replace('/', Path.DirectorySeparatorChar));
            var d = Path.Combine(destRoot, rel.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(s) || !File.Exists(d))
            {
                identical = false;
                continue;
            }
            if (!File.ReadAllBytes(s).SequenceEqual(File.ReadAllBytes(d)))
                identical = false;
        }

        foreach (var extra in new[]
                 {
                     "Local/txt/Pets/PetGlow.txt",
                     "Local/txt/Pets/EarthQuakeSkill.txt",
                     "Local/txt/CustomEffect(Dynamic)_effects.txt"
                 })
        {
            var s = Path.Combine(sourceRoot, extra.Replace('/', Path.DirectorySeparatorChar));
            var d = Path.Combine(destRoot, extra.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(s) && !File.Exists(d))
                sourceOnly.Add(extra);
        }

        return identical;
    }

    private static HashSet<int> LoadItemIndexes(string path)
    {
        var set = new HashSet<int>();
        if (!File.Exists(path)) return set;
        foreach (var line in File.ReadLines(path))
        {
            var t = line.Trim();
            if (t.Length == 0 || t.StartsWith("//") || t.StartsWith("end", StringComparison.OrdinalIgnoreCase))
                continue;
            var cols = Regex.Split(t, @"\s+");
            if (cols.Length > 0 && int.TryParse(cols[0], out var idx))
                set.Add(idx);
        }
        return set;
    }

    private static HashSet<string> LoadSetKeys(string path)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(path)) return set;
        foreach (var line in File.ReadLines(path))
        {
            var t = line.Trim();
            if (t.Length == 0 || t.StartsWith("//")) continue;
            var cols = Regex.Split(t, @"\s+");
            if (cols.Length >= 2 && int.TryParse(cols[0], out var g) && int.TryParse(cols[1], out var n))
                set.Add($"{g}:{n}");
        }
        return set;
    }
}
