namespace MUAssetInspector.Formats.Crypto;

public static class MapFileDecrypt
{
    private static readonly byte[] XorKey =
    [
        0xD1, 0x73, 0x52, 0xF6, 0xD2, 0x9A, 0xCB, 0x27,
        0x3E, 0xAF, 0x59, 0x31, 0x37, 0xB3, 0xE7, 0xA2
    ];

    public static byte[] Decrypt(ReadOnlySpan<byte> source)
    {
        var dst = new byte[source.Length];
        var mapKey = (byte)0x5E;
        for (var i = 0; i < source.Length; i++)
        {
            dst[i] = (byte)((source[i] ^ XorKey[i % 16]) - mapKey);
            mapKey = (byte)((source[i] + 0x3D) & 0xFF);
        }
        return dst;
    }
}
