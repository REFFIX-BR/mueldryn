using MUAssetInspector.Core.Domain;
using MUAssetInspector.Formats.Crypto;
using MUAssetInspector.Formats.Plugins;

namespace MUAssetInspector.Tests;

public class MapFileDecryptTests
{
    [Fact]
    public void Decrypt_RoundTrip_LengthPreserved()
    {
        var source = new byte[] { 0x10, 0x20, 0x30, 0x40, 0x50 };
        var decrypted = MapFileDecrypt.Decrypt(source);
        Assert.Equal(source.Length, decrypted.Length);
        Assert.NotEqual(source, decrypted);
    }
}

public class OztPluginTests
{
    [Fact]
    public void ClassifyAlpha_BinaryAndFull()
    {
        var pixels = new byte[16];
        pixels[3] = 0; pixels[7] = 255; pixels[11] = 255; pixels[15] = 0;
        Assert.Equal(AlphaKind.BinaryAlpha, AlphaAnalyzer.ClassifyBgra(pixels));

        var transparent = new byte[8];
        Assert.Equal(AlphaKind.FullAlpha, AlphaAnalyzer.ClassifyBgra(transparent));
    }
}

public class OzjPluginTests
{
    [Fact]
    public void CanParse_JpegMagic()
    {
        var plugin = new OzjPlugin();
        var header = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0xFF, 0xD8, 0xFF };
        Assert.True(plugin.CanParse(header, "test.ozj"));
    }
}

public class BmdPluginTests
{
    [Fact]
    public void CanParse_BmdHeader()
    {
        var plugin = new BmdMeshPlugin();
        var header = new byte[] { (byte)'B', (byte)'M', (byte)'D', 0x0A };
        Assert.True(plugin.CanParse(header, "model.bmd"));
    }
}

public class EffectTxtTests
{
    [Fact]
    public void ParseEffectIdRemap()
    {
        var lines = new[] { "// comment", "32131 32142", "32002 32003" };
        var remaps = EffectTxtIndex.ParseEffectIdRemap(lines);
        Assert.Equal(2, remaps.Count);
        Assert.Equal(32142, remaps[0].NewId);
    }
}

public class MudreamFixtureTests
{
    private static string WorkspaceRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));

    [Fact]
    public void ParseRealOzj_WhenFixtureExists()
    {
        var path = Path.Combine(WorkspaceRoot, "Mudream.online", "Data", "Item", "CustomItem", "Skin", "Magma", "RIV_CAPAMAGMA.ozj");
        if (!File.Exists(path))
            return;

        var plugin = new OzjPlugin();
        var ctx = new ParseContext
        {
            Profile = new ClientProfile(),
            DataRoot = Path.GetDirectoryName(path)!,
            RelativePath = "Item/CustomItem/Skin/Magma/RIV_CAPAMAGMA.ozj"
        };
        var result = plugin.Parse(path, ctx);
        Assert.Equal(ParseStatus.Success, result.Status);
        Assert.True(result.Width > 0);
        Assert.True(result.Height > 0);
    }

    [Fact]
    public void ParseRealBmd_WhenFixtureExists()
    {
        var dir = Path.Combine(WorkspaceRoot, "Mudream.online", "Data", "Item", "CustomItem", "Skin", "Fallen_Angel");
        if (!Directory.Exists(dir))
            return;

        var bmd = Directory.GetFiles(dir, "*.bmd").FirstOrDefault();
        if (bmd is null) return;

        var plugin = new BmdMeshPlugin();
        var ctx = new ParseContext
        {
            Profile = new ClientProfile(),
            DataRoot = Path.GetDirectoryName(bmd)!,
            RelativePath = Path.GetFileName(bmd)
        };
        var result = plugin.Parse(bmd, ctx);
        Assert.True(result.Status is ParseStatus.Success or ParseStatus.UnsupportedVersion);
    }
}
