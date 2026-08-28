using MUAssetInspector.Core.Domain;
using SixLabors.ImageSharp;

namespace MUAssetInspector.Formats.Plugins;

public sealed class PlainImagePlugin : IAssetFormatPlugin
{
    public string[] Extensions => [".png", ".bmp"];

    public bool CanParse(ReadOnlySpan<byte> header, string fileName)
    {
        var ext = Path.GetExtension(fileName);
        return ext.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
               ext.Equals(".bmp", StringComparison.OrdinalIgnoreCase);
    }

    public AssetParseResult Parse(string path, ParseContext ctx)
    {
        using var image = Image.Load(path);
        var hasAlpha = image.PixelType.BitsPerPixel > 24;
        return new AssetParseResult
        {
            Format = AssetFormatKind.PlainImage,
            Status = ParseStatus.Success,
            Width = image.Width,
            Height = image.Height,
            HasAlpha = hasAlpha,
            AlphaKind = hasAlpha ? AlphaKind.PartialAlpha : AlphaKind.NoAlpha
        };
    }
}
