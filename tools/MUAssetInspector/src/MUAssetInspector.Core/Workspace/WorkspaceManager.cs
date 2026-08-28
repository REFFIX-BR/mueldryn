using MUAssetInspector.Core.Domain;

namespace MUAssetInspector.Core.Workspace;

public sealed class WorkspaceManager
{
    public string Root { get; }

    public WorkspaceManager(string projectRoot)
    {
        Root = Path.Combine(projectRoot, "Workspace");
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(LogsDirectory);
        Directory.CreateDirectory(ReportsDirectory);
        Directory.CreateDirectory(CacheDirectory);
        Directory.CreateDirectory(ExportsDirectory);
    }

    public string LogsDirectory => Path.Combine(Root, "Logs");
    public string ReportsDirectory => Path.Combine(Root, "Reports");
    public string CacheDirectory => Path.Combine(Root, "Cache");
    public string ExportsDirectory => Path.Combine(Root, "Exports");

    public string GetSafeWritePath(string relativePath)
    {
        var full = Path.GetFullPath(Path.Combine(Root, relativePath));
        if (!full.StartsWith(Root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Path escapes workspace root.");
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        return full;
    }

    public string GetDatabasePath(ClientRole role) =>
        Path.Combine(CacheDirectory, role == ClientRole.Source ? "source.db" : "destination.db");
}
