using MUAssetInspector.Core.Domain;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MUAssetInspector.Formats.Plugins;

public sealed class OzjPlugin : IAssetFormatPlugin
{
    public string[] Extensions => [".ozj", ".jpg", ".jpeg"];

    public bool CanParse(ReadOnlySpan<byte> header, string fileName)
    {
        var ext = Path.GetExtension(fileName);
        if (ext.Equals(".ozj", StringComparison.OrdinalIgnoreCase))
            return header.Length >= 27 && header[24] == 0xFF && header[25] == 0xD8;
        return header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
    }

    public AssetParseResult Parse(string path, ParseContext ctx)
    {
        var bytes = File.ReadAllBytes(path);
        var skip = ctx.Profile.OzjHeaderSkip;
        ReadOnlySpan<byte> jpeg = bytes;
        if (Path.GetExtension(path).Equals(".ozj", StringComparison.OrdinalIgnoreCase))
        {
            if (bytes.Length <= skip)
                return Fail("OZJ too small");
            jpeg = bytes.AsSpan(skip);
        }

        if (jpeg.Length < 3 || jpeg[0] != 0xFF || jpeg[1] != 0xD8)
            return Fail("Invalid JPEG payload");

        using var image = Image.Load<Rgb24>(jpeg.ToArray());
        return new AssetParseResult
        {
            Format = AssetFormatKind.Ozj,
            Status = ParseStatus.Success,
            Width = image.Width,
            Height = image.Height,
            HasAlpha = false,
            AlphaKind = AlphaKind.NoAlpha,
            Metadata = { ["headerSkip"] = skip }
        };
    }

    private static AssetParseResult Fail(string msg) => new()
    {
        Format = AssetFormatKind.Ozj,
        Status = ParseStatus.Failed,
        ErrorMessage = msg
    };
}
