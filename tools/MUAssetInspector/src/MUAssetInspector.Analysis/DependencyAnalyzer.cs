using System.Text.RegularExpressions;
using MUAssetInspector.Core.Database;
using MUAssetInspector.Core.Domain;
using MUAssetInspector.Core.Profiles;
using MUAssetInspector.Formats.Plugins;
using MUAssetInspector.Formats.Resolution;

namespace MUAssetInspector.Analysis;

public sealed class CosmeticCatalogImporter
{
    private static readonly Regex EntryRegex = new(
        @"\{\s*(?<group>\d+)\s*,\s*(?<number>\d+)\s*,\s*""(?<name>[^""]*)""\s*,\s*""(?<model>[^""]*)""",
        RegexOptions.Compiled);

    public void ImportFromGeneratedCpp(string cppPath, AssetDatabase db, string profileName)
    {
        if (!File.Exists(cppPath))
            return;

        var text = File.ReadAllText(cppPath);
        foreach (Match m in EntryRegex.Matches(text))
        {
            var group = int.Parse(m.Groups["group"].Value);
            var number = int.Parse(m.Groups["number"].Value);
            var name = m.Groups["name"].Value;
            var model = m.Groups["model"].Value.Replace('\\', '/');
            db.UpsertItem(new ItemRecord
            {
                Group = group,
                Number = number,
                Name = name,
                ModelPath = model,
                SourceProfile = profileName
            });
        }
    }
}

public sealed class DependencyAnalyzer
{
    private readonly EffectTxtIndex _effects = new();
    private string? _sourceDataRoot;
    private string? _destDataRoot;
    private ClientProfile _profile = new();

    public void Configure(string? sourceDataRoot, string? destDataRoot, ClientProfile profile)
    {
        _sourceDataRoot = sourceDataRoot;
        _destDataRoot = destDataRoot;
        _profile = profile;
        if (sourceDataRoot is not null)
            _effects.LoadDirectory(sourceDataRoot, profile);
    }

    public void LoadEffects(string dataRoot, ClientProfile profile) =>
        Configure(dataRoot, _destDataRoot, profile);

    public DependencyNode AnalyzeItem(ItemRecord item, AssetDatabase sourceDb, AssetDatabase? destDb = null)
    {
        var root = new DependencyNode
        {
            Name = $"{item.Group}:{item.Number} {item.Name}",
            Type = DependencyType.Model
        };

        if (string.IsNullOrWhiteSpace(item.ModelPath))
            return root;

        var modelNode = new DependencyNode
        {
            Name = Path.GetFileName(item.ModelPath),
            Type = DependencyType.Model,
            RelativePath = item.ModelPath
        };
        root.Children.Add(modelNode);

        var modelAsset = sourceDb.FindByPath(item.ModelPath) ?? sourceDb.FindByFileName(item.ModelPath);
        if (modelAsset?.ParsedJson is not null)
        {
            var textures = TexturePathResolver.ExtractTextureNames(modelAsset.ParsedJson);
            foreach (var tex in textures)
            {
                var texPath = ResolveTexturePath(tex, sourceDb, item.ModelPath);
                var texNode = new DependencyNode
                {
                    Name = tex,
                    Type = DependencyType.Texture,
                    RelativePath = texPath,
                    CompareStatus = destDb is null ? null : CompareTexture(tex, item.ModelPath, destDb)
                };
                modelNode.Children.Add(texNode);

                if (tex.EndsWith(".ozt", StringComparison.OrdinalIgnoreCase))
                {
                    modelNode.Children.Add(new DependencyNode
                    {
                        Name = $"{tex} (alpha)",
                        Type = DependencyType.Alpha,
                        RelativePath = texPath,
                        CompareStatus = texNode.CompareStatus
                    });
                }
            }
        }

        var itemIndex = item.Group * 512 + item.Number;
        AddEffectNodes(modelNode, itemIndex, sourceDb, destDb);
        return root;
    }

    public DependencyNode AnalyzeAsset(AssetRecord asset, AssetDatabase sourceDb, AssetDatabase? destDb = null)
    {
        var root = new DependencyNode
        {
            Name = asset.RelativePath,
            Type = DependencyType.Model,
            RelativePath = asset.RelativePath
        };

        if (asset.ParsedJson is not null)
        {
            foreach (var tex in TexturePathResolver.ExtractTextureNames(asset.ParsedJson))
            {
                var texPath = ResolveTexturePath(tex, sourceDb, asset.RelativePath);
                root.Children.Add(new DependencyNode
                {
                    Name = tex,
                    Type = DependencyType.Texture,
                    RelativePath = texPath,
                    CompareStatus = destDb is null ? null : CompareTexture(tex, asset.RelativePath, destDb)
                });
            }
        }
        return root;
    }

    private void AddEffectNodes(DependencyNode parent, int itemIndex, AssetDatabase sourceDb, AssetDatabase? destDb)
    {
        if (_effects.Static.TryGetValue(itemIndex, out var staticFx))
        {
            foreach (var fx in staticFx)
            {
                parent.Children.Add(new DependencyNode
                {
                    Name = $"Static effect {fx.EffectBitmap}",
                    Type = DependencyType.EffectStatic,
                    CompareStatus = destDb is null ? null : CompareEffectRegistration(itemIndex, DependencyType.EffectStatic, destDb)
                });
            }
        }

        if (_effects.Dynamic.TryGetValue(itemIndex, out var dynamicFx))
        {
            foreach (var fx in dynamicFx)
            {
                parent.Children.Add(new DependencyNode
                {
                    Name = $"Dynamic effect {fx.EffectBitmap}",
                    Type = DependencyType.EffectDynamic
                });
            }
        }

        if (_effects.MeshEffects.TryGetValue(itemIndex, out var meshFx))
        {
            foreach (var fx in meshFx)
            {
                parent.Children.Add(new DependencyNode
                {
                    Name = $"Mesh FX mesh={fx.MeshIndex} flags={fx.RenderFlags}",
                    Type = DependencyType.MeshEffect,
                    CompareStatus = destDb is null ? null : CompareEffectRegistration(itemIndex, DependencyType.MeshEffect, destDb)
                });
            }
        }
    }

    private static List<string> ExtractTextureNames(string parsedJson) =>
        TexturePathResolver.ExtractTextureNames(parsedJson);

    private string? ResolveTexturePath(string textureName, AssetDatabase db, string? ownerRelativePath)
    {
        return TexturePathResolver.Resolve(_sourceDataRoot ?? string.Empty, db, ownerRelativePath, textureName, _profile)
            ?? textureName.Replace('\\', '/');
    }

    private ComparisonStatus? CompareTexture(string textureRef, string? ownerRelativePath, AssetDatabase destDb)
    {
        var resolved = TexturePathResolver.Resolve(_destDataRoot ?? string.Empty, destDb, ownerRelativePath, textureRef, _profile);
        if (resolved is null)
            return ComparisonStatus.Missing;

        var destHit = destDb.FindByPath(resolved) ?? destDb.FindByFileName(resolved);
        if (destHit is not null)
            return ComparisonStatus.Identical;

        if (_destDataRoot is not null)
        {
            var full = Path.Combine(_destDataRoot, resolved.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(full))
                return ComparisonStatus.Identical;
        }

        return ComparisonStatus.Missing;
    }

    private static ComparisonStatus? ComparePath(string? relativePath, AssetDatabase destDb)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return ComparisonStatus.Missing;
        var exact = destDb.FindByPath(relativePath);
        if (exact is not null)
            return ComparisonStatus.Identical;
        var byName = destDb.FindByFileName(relativePath);
        if (byName is not null)
            return string.Equals(byName.RelativePath, relativePath, StringComparison.OrdinalIgnoreCase)
                ? ComparisonStatus.Identical
                : ComparisonStatus.CaseMismatch;
        return ComparisonStatus.Missing;
    }

    private ComparisonStatus CompareEffectRegistration(int itemIndex, DependencyType type, AssetDatabase destDb)
    {
        // Heuristic: if effect txt tables exist in destination, treat as present
        var hasEffectConfig = destDb.QueryAssets(category: AssetCategory.Config).Any(a =>
            a.RelativePath.Contains("CustomEffect", StringComparison.OrdinalIgnoreCase) ||
            a.RelativePath.Contains("CEffectRenderMesh", StringComparison.OrdinalIgnoreCase));
        return hasEffectConfig ? ComparisonStatus.PresentDifferent : ComparisonStatus.Missing;
    }
}
