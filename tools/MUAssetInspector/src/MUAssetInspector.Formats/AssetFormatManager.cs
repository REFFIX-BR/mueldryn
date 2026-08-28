using MUAssetInspector.Core.Domain;
using MUAssetInspector.Core.Profiles;
using MUAssetInspector.Core.Scanning;
using MUAssetInspector.Formats.Plugins;

namespace MUAssetInspector.Formats;

public interface IAssetFormatPlugin
{
    string[] Extensions { get; }
    bool CanParse(ReadOnlySpan<byte> header, string fileName);
    AssetParseResult Parse(string path, ParseContext ctx);
}

public sealed class AssetFormatManager : AssetFormatManagerBridge
{
    private readonly List<IAssetFormatPlugin> _plugins;
    private readonly ClientProfile _profile;
    private readonly string _dataRoot;

    public AssetFormatManager(ClientProfile profile, string dataRoot, IEnumerable<IAssetFormatPlugin>? extraPlugins = null)
    {
        _profile = profile;
        _dataRoot = dataRoot;
        _plugins =
        [
            new BmdMeshPlugin(),
            new OzjPlugin(),
            new OztPlugin(),
            new PlainImagePlugin(),
            new EffectTxtPlugin(),
            new XmlConfigPlugin(),
            ..(extraPlugins ?? [])
        ];
    }

    public AssetParseResult? TryParse(string fullPath, string relativePath, ClientRole role)
    {
        try
        {
            var header = ReadHeader(fullPath, 64);
            var plugin = _plugins.FirstOrDefault(p => p.CanParse(header, fullPath));
            if (plugin is null)
                return null;

            var ctx = new ParseContext
            {
                Profile = _profile,
                DataRoot = _dataRoot,
                RelativePath = relativePath
            };
            return plugin.Parse(fullPath, ctx);
        }
        catch (Exception ex)
        {
            return new AssetParseResult
            {
                Format = AssetFormatKind.Unknown,
                Status = ParseStatus.Failed,
                ErrorMessage = ex.Message
            };
        }
    }

    public AssetParseResult Parse(string path, ParseContext ctx)
    {
        var header = ReadHeader(path, 64);
        var plugin = _plugins.FirstOrDefault(p => p.CanParse(header, path))
            ?? throw new NotSupportedException($"No plugin for {path}");
        return plugin.Parse(path, ctx);
    }

    private static byte[] ReadHeader(string path, int count)
    {
        using var fs = File.OpenRead(path);
        var buffer = new byte[Math.Min(count, (int)Math.Max(0, fs.Length))];
        _ = fs.Read(buffer, 0, buffer.Length);
        return buffer;
    }
}
