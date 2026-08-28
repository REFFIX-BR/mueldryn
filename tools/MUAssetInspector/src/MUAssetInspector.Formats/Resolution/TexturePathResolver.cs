using MUAssetInspector.Core.Database;
using MUAssetInspector.Core.Domain;
using MUAssetInspector.Core.Profiles;

namespace MUAssetInspector.Formats.Resolution;

public static class TexturePathResolver
{
    public static string? ResolveOnDisk(string dataRoot, string? ownerRelativePath, string textureRef, ClientProfile profile)
    {
        if (string.IsNullOrWhiteSpace(textureRef))
            return null;

        var baseName = Path.GetFileNameWithoutExtension(textureRef.Replace('\\', '/'));
        var ownerDir = string.IsNullOrWhiteSpace(ownerRelativePath)
            ? string.Empty
            : (Path.GetDirectoryName(ownerRelativePath.Replace('\\', '/')) ?? string.Empty).Replace('\\', '/');

        var candidateDirs = new List<string>();
        if (!string.IsNullOrEmpty(ownerDir))
            candidateDirs.Add(ownerDir);
        candidateDirs.AddRange(["Item", "Player", "Effect", "Object9"]);

        foreach (var dir in candidateDirs.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            foreach (var ext in profile.TextureFallbacks)
            {
                var rel = string.IsNullOrEmpty(dir) ? baseName + ext : $"{dir}/{baseName}{ext}";
                var full = Path.Combine(dataRoot, rel.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(full))
                    return rel.Replace('\\', '/');
            }
        }

        return null;
    }

    public static string? ResolveInDatabase(AssetDatabase db, string textureRef, ClientProfile profile)
    {
        var normalized = textureRef.Replace('\\', '/');
        var hit = db.FindByPath(normalized) ?? db.FindByFileName(normalized);
        if (hit is not null)
            return hit.RelativePath;

        var baseName = Path.GetFileNameWithoutExtension(normalized);
        foreach (var ext in profile.TextureFallbacks)
        {
            hit = db.FindByFileName(baseName + ext);
            if (hit is not null)
                return hit.RelativePath;
        }

        return null;
    }

    public static string? Resolve(
        string dataRoot,
        AssetDatabase? db,
        string? ownerRelativePath,
        string textureRef,
        ClientProfile profile)
    {
        return ResolveOnDisk(dataRoot, ownerRelativePath, textureRef, profile)
            ?? (db is null ? null : ResolveInDatabase(db, textureRef, profile));
    }

    public static List<string> ExtractTextureNames(string? parsedJson)
    {
        if (string.IsNullOrWhiteSpace(parsedJson))
            return [];

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(parsedJson);
            if (doc.RootElement.TryGetProperty("TextureNames", out var names))
                return names.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => s.Length > 0).ToList();
            if (doc.RootElement.TryGetProperty("ReferencedPaths", out var refs))
                return refs.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => s.Length > 0).ToList();
        }
        catch { }

        return [];
    }
}
