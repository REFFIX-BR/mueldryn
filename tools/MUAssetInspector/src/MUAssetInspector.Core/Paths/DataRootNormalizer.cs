namespace MUAssetInspector.Core.Paths;

public static class DataRootNormalizer
{
    public static string? Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var full = Path.GetFullPath(path.Trim().TrimEnd('\\', '/'));
        if (Path.GetFileName(full).Equals("Data", StringComparison.OrdinalIgnoreCase))
            return full;

        var dataChild = Path.Combine(full, "Data");
        if (Directory.Exists(dataChild))
            return dataChild;

        return full;
    }
}
