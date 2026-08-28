using System.Xml.Linq;
using MUAssetInspector.Core.Domain;

namespace MUAssetInspector.Formats.Plugins;

public sealed class XmlConfigPlugin : IAssetFormatPlugin
{
    public string[] Extensions => [".xml"];

    public bool CanParse(ReadOnlySpan<byte> header, string fileName)
    {
        var name = Path.GetFileName(fileName);
        return name.Equals("ItemCloth.xml", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("WingsEffects.xml", StringComparison.OrdinalIgnoreCase);
    }

    public AssetParseResult Parse(string path, ParseContext ctx)
    {
        var doc = XDocument.Load(path);
        var refs = new List<string>();
        foreach (var attr in doc.Descendants().SelectMany(e => e.Attributes()))
        {
            var val = attr.Value;
            if (val.Contains('.') && (val.EndsWith(".ozj", StringComparison.OrdinalIgnoreCase) ||
                                      val.EndsWith(".ozt", StringComparison.OrdinalIgnoreCase) ||
                                      val.EndsWith(".tga", StringComparison.OrdinalIgnoreCase)))
                refs.Add(val.Replace('\\', '/'));
        }

        return new AssetParseResult
        {
            Format = AssetFormatKind.XmlConfig,
            Status = ParseStatus.Success,
            ReferencedPaths = refs.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            Metadata = { ["config"] = Path.GetFileName(path) }
        };
    }
}
