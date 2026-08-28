using MUAssetInspector.Core.Domain;

namespace MUAssetInspector.Formats.Plugins;

public sealed class OztPlugin : IAssetFormatPlugin
{
    public string[] Extensions => [".ozt", ".tga"];

    public bool CanParse(ReadOnlySpan<byte> header, string fileName)
    {
        return Path.GetExtension(fileName).Equals(".ozt", StringComparison.OrdinalIgnoreCase) ||
               Path.GetExtension(fileName).Equals(".tga", StringComparison.OrdinalIgnoreCase);
    }

    public AssetParseResult Parse(string path, ParseContext ctx)
    {
        var bytes = File.ReadAllBytes(path);
        var (width, height, alphaKind, hasAlpha) = TryParseOzt(bytes, ctx.Profile.OztHeaderSkip)
            ?? TryParseOzt(bytes, 4)
            ?? throw new InvalidDataException("Unable to parse OZT/TGA payload");

        return new AssetParseResult
        {
            Format = AssetFormatKind.Ozt,
            Status = ParseStatus.Success,
            Width = width,
            Height = height,
            HasAlpha = hasAlpha,
            AlphaKind = alphaKind
        };
    }

    internal static (int Width, int Height, AlphaKind AlphaKind, bool HasAlpha)? TryParseOzt(byte[] data, int headerSkip)
    {
        if (data.Length < headerSkip + 18)
            return null;

        var index = headerSkip + 12;
        index += 4;
        var nx = BitConverter.ToInt16(data, index); index += 2;
        var ny = BitConverter.ToInt16(data, index); index += 2;
        var bit = data[index];
        if (bit != 32 || nx <= 0 || ny <= 0 || nx > 8192 || ny > 8192)
            return null;

        index += 2;
        var pixelBytes = (long)nx * ny * 4;
        if (index + pixelBytes > data.Length)
            return null;

        var alphaKind = AlphaAnalyzer.ClassifyBgra(data.AsSpan(index, (int)pixelBytes));
        var hasAlpha = alphaKind != AlphaKind.NoAlpha && alphaKind != AlphaKind.Unknown;
        return (nx, ny, alphaKind, hasAlpha);
    }
}

public static class AlphaAnalyzer
{
    public static AlphaKind ClassifyBgra(ReadOnlySpan<byte> pixels)
    {
        if (pixels.Length < 4)
            return AlphaKind.Unknown;

        var transparent = 0;
        var opaque = 0;
        var partial = 0;
        var total = pixels.Length / 4;

        for (var i = 3; i < pixels.Length; i += 4)
        {
            var a = pixels[i];
            if (a == 0) transparent++;
            else if (a == 255) opaque++;
            else partial++;
        }

        if (partial > total * 0.05)
            return partial > transparent ? AlphaKind.PartialAlpha : AlphaKind.FullAlpha;
        if (transparent > 0 && opaque > 0 && partial == 0)
            return AlphaKind.BinaryAlpha;
        if (transparent > total * 0.5)
            return AlphaKind.FullAlpha;
        if (transparent == 0)
            return AlphaKind.NoAlpha;
        return AlphaKind.PartialAlpha;
    }
}
