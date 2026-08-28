namespace MUAssetInspector.Core.Paths;

public static class ProjectPaths
{
    public static string FindProjectRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "profiles")) &&
                File.Exists(Path.Combine(dir.FullName, "profiles", "mudream.json")))
                return dir.FullName;

            if (File.Exists(Path.Combine(dir.FullName, "MUAssetInspector.sln")) ||
                File.Exists(Path.Combine(dir.FullName, "MUAssetInspector.slnx")))
                return dir.FullName;

            dir = dir.Parent;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    }
}
