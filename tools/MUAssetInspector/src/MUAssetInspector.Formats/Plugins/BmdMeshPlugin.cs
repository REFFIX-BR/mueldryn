using System.Text;
using System.Text.RegularExpressions;
using MUAssetInspector.Core.Domain;
using MUAssetInspector.Formats.Crypto;

namespace MUAssetInspector.Formats.Plugins;

public sealed class BmdMeshPlugin : IAssetFormatPlugin
{
    private static readonly Regex TextureNameRegex = new(
        @"[\w\\/.-]+\.(ozj|ozt|jpg|tga|bmp|png)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public string[] Extensions => [".bmd"];

    public bool CanParse(ReadOnlySpan<byte> header, string fileName) =>
        header.Length >= 3 && header[0] == (byte)'B' && header[1] == (byte)'M' && header[2] == (byte)'D';

    public AssetParseResult Parse(string path, ParseContext ctx)
    {
        var fileData = File.ReadAllBytes(path);
        if (fileData.Length < 4)
            return Fail("File too small", ParseStatus.Failed);

        var ptr = 3;
        var version = fileData[ptr++];
        byte[] data;
        ParseStatus status = ParseStatus.Success;

        if (version == 0x0C)
        {
            if (fileData.Length < ptr + 4)
                return Fail("Invalid v12 header", ParseStatus.Failed);
            var encSize = BitConverter.ToInt32(fileData, ptr);
            ptr += 4;
            if (encSize <= 0 || ptr + encSize > fileData.Length)
                return Fail("Invalid encrypted size", ParseStatus.Failed);
            data = MapFileDecrypt.Decrypt(fileData.AsSpan(ptr, encSize));
        }
        else if (version == 0x0A)
        {
            ptr = 4;
            data = fileData;
        }
        else if (version == 0x0E)
        {
            return new AssetParseResult
            {
                Format = AssetFormatKind.BmdMesh,
                Status = ParseStatus.UnsupportedVersion,
                Metadata = { ["version"] = version },
                ErrorMessage = "BMD v14 not yet supported"
            };
        }
        else
        {
            return Fail($"Unknown BMD version {version}", ParseStatus.UnsupportedVersion);
        }

        var textureNames = ExtractEmbeddedTextureNames(data);
        var structured = TryParseStructuredTextures(data, textureNames);

        return new AssetParseResult
        {
            Format = AssetFormatKind.BmdMesh,
            Status = status,
            TextureNames = structured.Count > 0 ? structured : textureNames,
            ReferencedPaths = (structured.Count > 0 ? structured : textureNames).ToList(),
            Metadata =
            {
                ["version"] = version,
                ["meshCount"] = TryReadMeshCount(data)
            }
        };
    }

    private static List<string> TryParseStructuredTextures(byte[] data, List<string> fallback)
    {
        try
        {
            if (data.Length < 38)
                return fallback;

            var ptr = 32;
            var numMeshs = BitConverter.ToInt16(data, ptr); ptr += 2;
            var numBones = BitConverter.ToInt16(data, ptr); ptr += 2;
            var numActions = BitConverter.ToInt16(data, ptr); ptr += 2;
            if (numMeshs <= 0 || numMeshs > 512)
                return fallback;

            var names = new List<string>();
            for (var i = 0; i < numMeshs; i++)
            {
                if (ptr + 10 > data.Length)
                    break;
                var numVertices = BitConverter.ToInt16(data, ptr); ptr += 2;
                var numNormals = BitConverter.ToInt16(data, ptr); ptr += 2;
                var numTexCoords = BitConverter.ToInt16(data, ptr); ptr += 2;
                var numTriangles = BitConverter.ToInt16(data, ptr); ptr += 2;
                ptr += 2; // texture index

                var vertSize = numVertices * 16;
                var normSize = numNormals * 20;
                var texSize = numTexCoords * 8;
                var triSize = numTriangles * 64;
                var block = vertSize + normSize + texSize + triSize;
                if (ptr + block + 32 > data.Length)
                    break;
                ptr += block;

                var name = Encoding.ASCII.GetString(data, ptr, 32).TrimEnd('\0');
                ptr += 32;
                if (!string.IsNullOrWhiteSpace(name))
                    names.Add(name);
            }

            return names.Count > 0 ? names : fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private static List<string> ExtractEmbeddedTextureNames(byte[] data)
    {
        var text = Encoding.ASCII.GetString(data);
        return TextureNameRegex.Matches(text)
            .Select(m => m.Value.Replace('\\', '/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static int TryReadMeshCount(byte[] data)
    {
        if (data.Length < 38)
            return 0;
        return BitConverter.ToInt16(data, 32);
    }

    private static AssetParseResult Fail(string msg, ParseStatus status) => new()
    {
        Format = AssetFormatKind.BmdMesh,
        Status = status,
        ErrorMessage = msg
    };
}
