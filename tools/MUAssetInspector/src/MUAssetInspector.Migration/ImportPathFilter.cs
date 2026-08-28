using MUAssetInspector.Core.Domain;
using MUAssetInspector.Core.Profiles;

namespace MUAssetInspector.Migration;

public static class ImportPathFilter
{
    private static readonly HashSet<string> JunkExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".old", ".death", ".bak", ".tmp", ".backup"
    };

    /// <summary>Mudream editor leftovers — not real client assets.</summary>
    public static bool IsEditorJunk(string relativePath)
    {
        var ext = Path.GetExtension(relativePath);
        return JunkExtensions.Contains(ext);
    }

    public static bool MatchesScope(string relativePath, ImportScope scope, ClientProfile profile)
    {
        if (IsEditorJunk(relativePath))
            return false;

        if (scope == ImportScope.AllCompatible)
            return true;

        var normalized = relativePath.Replace('\\', '/');
        foreach (var exclude in profile.ImportExcludePrefixes)
        {
            if (StartsWithPrefix(normalized, exclude))
                return false;
        }

        foreach (var include in profile.ImportIncludePrefixes)
        {
            if (StartsWithPrefix(normalized, include))
                return true;
        }

        return false;
    }

    private static bool StartsWithPrefix(string path, string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return false;

        var p = prefix.Replace('\\', '/').TrimEnd('/');
        if (path.StartsWith(p, StringComparison.OrdinalIgnoreCase))
        {
            if (path.Length == p.Length)
                return true;
            var next = path[p.Length];
            return next is '/' or '\\';
        }

        return false;
    }
}
