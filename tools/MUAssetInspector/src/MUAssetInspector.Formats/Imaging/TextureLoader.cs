using MUAssetInspector.Core.Domain;
using MUAssetInspector.Formats.Plugins;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MUAssetInspector.Formats.Imaging;

public sealed class TextureLoader
{
    public Image<Rgba32>? LoadPreview(string path, ClientProfile profile)
    {
        var ext = Path.GetExtension(path);
        try
        {
            if (ext.Equals(".ozj", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = File.ReadAllBytes(path);
                if (bytes.Length <= profile.OzjHeaderSkip) return null;
                return Image.Load<Rgba32>(bytes.AsSpan(profile.OzjHeaderSkip).ToArray());
            }

            if (ext.Equals(".ozt", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = File.ReadAllBytes(path);
                var parsed = OztPlugin.TryParseOzt(bytes, profile.OztHeaderSkip) ?? OztPlugin.TryParseOzt(bytes, 4);
                if (parsed is null) return null;
                return DecodeOztBgra(bytes, profile.OztHeaderSkip) ?? DecodeOztBgra(bytes, 4);
            }

            return Image.Load<Rgba32>(path);
        }
        catch
        {
            return null;
        }
    }

    public Image<L8>? LoadAlphaChannel(string path, ClientProfile profile)
    {
        using var rgba = LoadPreview(path, profile);
        if (rgba is null) return null;

        var alpha = new Image<L8>(rgba.Width, rgba.Height);
        rgba.ProcessPixelRows(alpha, (source, dest) =>
        {
            for (var y = 0; y < source.Height; y++)
            {
                var src = source.GetRowSpan(y);
                var dst = dest.GetRowSpan(y);
                for (var x = 0; x < src.Length; x++)
                    dst[x] = new L8(src[x].A);
            }
        });
        return alpha;
    }

    public Image<Rgba32>? CreateDifference(Image<Rgba32> source, Image<Rgba32> dest)
    {
        var width = Math.Max(source.Width, dest.Width);
        var height = Math.Max(source.Height, dest.Height);
        source.Mutate(x => x.Resize(width, height));
        dest.Mutate(x => x.Resize(width, height));

        var diff = new Image<Rgba32>(width, height);
        diff.ProcessPixelRows(source, dest, (d, s, t) =>
        {
            for (var y = 0; y < d.Height; y++)
            {
                var ds = d.GetRowSpan(y);
                var ss = s.GetRowSpan(y);
                var ts = t.GetRowSpan(y);
                for (var x = 0; x < ds.Length; x++)
                {
                    ds[x] = new Rgba32(
                        (byte)Math.Abs(ss[x].R - ts[x].R),
                        (byte)Math.Abs(ss[x].G - ts[x].G),
                        (byte)Math.Abs(ss[x].B - ts[x].B),
                        255);
                }
            }
        });
        return diff;
    }

    private static Image<Rgba32>? DecodeOztBgra(byte[] data, int headerSkip)
    {
        var parsed = OztPlugin.TryParseOzt(data, headerSkip);
        if (parsed is null) return null;
        var (width, height, _, _) = parsed.Value;
        var index = headerSkip + 12 + 4 + 2 + 2 + 2;
        var image = new Image<Rgba32>(width, height);
        var off = index;
        for (var y = height - 1; y >= 0; y--)
        {
            for (var x = 0; x < width; x++)
            {
                var b = data[off++];
                var g = data[off++];
                var r = data[off++];
                var a = data[off++];
                image[x, y] = new Rgba32(r, g, b, a);
            }
        }
        return image;
    }
}
