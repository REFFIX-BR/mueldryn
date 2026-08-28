using System.Text;
using MUAssetInspector.Core.Workspace;

namespace MUAssetInspector.Core.Logging;

public sealed class FileLogger
{
    private readonly string _logPath;
    private readonly object _lock = new();

    public FileLogger(WorkspaceManager workspace)
    {
        _logPath = Path.Combine(workspace.LogsDirectory, "analyzer.log");
    }

    public void Info(string message) => Write("INFO", message);
    public void Warn(string message) => Write("WARN", message);
    public void Error(string message) => Write("ERROR", message);

    public void Write(string level, string message)
    {
        var line = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} [{level}] {message}{Environment.NewLine}";
        lock (_lock)
        {
            File.AppendAllText(_logPath, line, Encoding.UTF8);
        }
    }

    public string Tail(int maxLines = 50)
    {
        if (!File.Exists(_logPath))
            return string.Empty;

        var lines = File.ReadAllLines(_logPath);
        return string.Join(Environment.NewLine, lines.TakeLast(maxLines));
    }
}
