using System.Text.RegularExpressions;
using MUAssetInspector.Core.Domain;

namespace MUAssetInspector.Formats.Plugins;

public sealed class EffectTxtPlugin : IAssetFormatPlugin
{
    public string[] Extensions => [".txt"];

    public bool CanParse(ReadOnlySpan<byte> header, string fileName)
    {
        var name = Path.GetFileName(fileName);
        return name.StartsWith("CustomEffect", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("CSetEffect.txt", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("CEffectRenderMesh.txt", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("EffectID.txt", StringComparison.OrdinalIgnoreCase);
    }

    public AssetParseResult Parse(string path, ParseContext ctx)
    {
        var name = Path.GetFileName(path);
        var lines = File.ReadAllLines(path);
        var metadata = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (name.StartsWith("CustomEffect", StringComparison.OrdinalIgnoreCase))
        {
            var entries = EffectTxtIndex.ParseCustomEffect(lines);
            metadata["kind"] = name.Contains("Dynamic", StringComparison.OrdinalIgnoreCase) ? "Dynamic" : "Static";
            metadata["entries"] = entries.Count;
            metadata["itemIndexes"] = entries.Select(e => e.ItemIndex).Distinct().ToList();
            return new AssetParseResult
            {
                Format = AssetFormatKind.EffectTxt,
                Status = ParseStatus.Success,
                Metadata = metadata,
                ReferencedPaths = entries.Select(e => e.EffectBitmap.ToString()).Distinct().ToList()
            };
        }

        if (name.Equals("CSetEffect.txt", StringComparison.OrdinalIgnoreCase))
        {
            var entries = EffectTxtIndex.ParseSetEffect(lines);
            metadata["kind"] = "SetEffect";
            metadata["entries"] = entries.Count;
            return new AssetParseResult
            {
                Format = AssetFormatKind.EffectTxt,
                Status = ParseStatus.Success,
                Metadata = metadata
            };
        }

        if (name.Equals("CEffectRenderMesh.txt", StringComparison.OrdinalIgnoreCase))
        {
            var entries = EffectTxtIndex.ParseMeshEffect(lines);
            metadata["kind"] = "MeshEffect";
            metadata["entries"] = entries.Count;
            metadata["itemIndexes"] = entries.Select(e => e.ItemIndex).Distinct().ToList();
            return new AssetParseResult
            {
                Format = AssetFormatKind.EffectTxt,
                Status = ParseStatus.Success,
                Metadata = metadata
            };
        }

        if (name.Equals("EffectID.txt", StringComparison.OrdinalIgnoreCase))
        {
            var remaps = EffectTxtIndex.ParseEffectIdRemap(lines);
            metadata["kind"] = "EffectIdRemap";
            metadata["entries"] = remaps.Count;
            return new AssetParseResult
            {
                Format = AssetFormatKind.EffectTxt,
                Status = ParseStatus.Success,
                Metadata = metadata
            };
        }

        return new AssetParseResult
        {
            Format = AssetFormatKind.EffectTxt,
            Status = ParseStatus.Failed,
            ErrorMessage = "Unknown effect table"
        };
    }
}

public sealed class EffectTxtIndex
{
    public record StaticEffectEntry(int ItemIndex, int EffectBitmap, int Bone);
    public record DynamicEffectEntry(int ItemIndex, int EffectBitmap, int Bone, int SubType);
    public record SetEffectEntry(int Group, int Number, int EffectBitmap);
    public record MeshEffectEntry(int ItemIndex, int MeshIndex, int RenderFlags, int TextureId);
    public record EffectIdRemapEntry(int OldId, int NewId);

    private readonly Dictionary<int, List<StaticEffectEntry>> _static = new();
    private readonly Dictionary<int, List<DynamicEffectEntry>> _dynamic = new();
    private readonly List<SetEffectEntry> _set = [];
    private readonly Dictionary<int, List<MeshEffectEntry>> _mesh = new();
    private readonly Dictionary<int, int> _remap = new();

    public void LoadDirectory(string dataRoot, ClientProfile profile)
    {
        foreach (var rel in profile.EffectTables)
        {
            var path = Path.Combine(dataRoot, rel.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
                continue;
            LoadFile(path);
        }

        var txtDir = Path.Combine(dataRoot, "Local", "txt");
        if (Directory.Exists(txtDir))
        {
            foreach (var file in Directory.GetFiles(txtDir, "*.txt"))
                LoadFile(file);
        }

        if (profile.IncludeServerOverrides)
        {
            var local = Path.Combine(dataRoot, "Local");
            if (Directory.Exists(local))
            {
                foreach (var file in Directory.GetDirectories(local, "Server*")
                             .SelectMany(d => Directory.GetFiles(d, "*.txt", SearchOption.AllDirectories)))
                    LoadFile(file);
            }
        }
    }

    public void LoadFile(string path)
    {
        var name = Path.GetFileName(path);
        var lines = File.ReadAllLines(path);
        if (name.StartsWith("CustomEffect", StringComparison.OrdinalIgnoreCase))
        {
            var isDynamic = name.Contains("Dynamic", StringComparison.OrdinalIgnoreCase);
            if (isDynamic)
            {
                foreach (var e in ParseCustomEffectDynamic(lines))
                {
                    if (!_dynamic.TryGetValue(e.ItemIndex, out var list))
                        _dynamic[e.ItemIndex] = list = [];
                    list.Add(e);
                }
            }
            else
            {
                foreach (var e in ParseCustomEffect(lines))
                {
                    if (!_static.TryGetValue(e.ItemIndex, out var list))
                        _static[e.ItemIndex] = list = [];
                    list.Add(e);
                }
            }
        }
        else if (name.Equals("CSetEffect.txt", StringComparison.OrdinalIgnoreCase))
            _set.AddRange(ParseSetEffect(lines));
        else if (name.Equals("CEffectRenderMesh.txt", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var e in ParseMeshEffect(lines))
            {
                if (!_mesh.TryGetValue(e.ItemIndex, out var list))
                    _mesh[e.ItemIndex] = list = [];
                list.Add(e);
            }
        }
        else if (name.Equals("EffectID.txt", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var e in ParseEffectIdRemap(lines))
                _remap[e.OldId] = e.NewId;
        }
    }

    public IReadOnlyDictionary<int, List<StaticEffectEntry>> Static => _static;
    public IReadOnlyDictionary<int, List<DynamicEffectEntry>> Dynamic => _dynamic;
    public IReadOnlyList<SetEffectEntry> SetEffects => _set;
    public IReadOnlyDictionary<int, List<MeshEffectEntry>> MeshEffects => _mesh;
    public IReadOnlyDictionary<int, int> EffectIdRemap => _remap;

    public int RemapEffectId(int id) => _remap.TryGetValue(id, out var mapped) ? mapped : id;

    public static List<StaticEffectEntry> ParseCustomEffect(IEnumerable<string> lines) =>
        ParseRows(lines, cols => new StaticEffectEntry(ParseInt(cols, 0), ParseInt(cols, 1), ParseInt(cols, 2)));

    public static List<DynamicEffectEntry> ParseCustomEffectDynamic(IEnumerable<string> lines) =>
        ParseRows(lines, cols => new DynamicEffectEntry(ParseInt(cols, 0), ParseInt(cols, 1), ParseInt(cols, 2), ParseInt(cols, 5)));

    public static List<SetEffectEntry> ParseSetEffect(IEnumerable<string> lines) =>
        ParseRows(lines, cols => new SetEffectEntry(ParseInt(cols, 0), ParseInt(cols, 1), ParseInt(cols, 4)));

    public static List<MeshEffectEntry> ParseMeshEffect(IEnumerable<string> lines) =>
        ParseRows(lines, cols => new MeshEffectEntry(ParseInt(cols, 0), ParseInt(cols, 1), ParseInt(cols, 3), ParseInt(cols, 8)));

    public static List<EffectIdRemapEntry> ParseEffectIdRemap(IEnumerable<string> lines)
    {
        var result = new List<EffectIdRemapEntry>();
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("//"))
                continue;
            var cols = SplitColumns(line);
            if (cols.Length >= 2 && int.TryParse(cols[0], out var oldId) && int.TryParse(cols[1], out var newId))
                result.Add(new EffectIdRemapEntry(oldId, newId));
        }
        return result;
    }

    private static List<T> ParseRows<T>(IEnumerable<string> lines, Func<string[], T> map)
    {
        var result = new List<T>();
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("//") || line.StartsWith("end", StringComparison.OrdinalIgnoreCase))
                continue;
            var cols = SplitColumns(line);
            if (cols.Length < 3)
                continue;
            try { result.Add(map(cols)); } catch { /* skip malformed */ }
        }
        return result;
    }

    private static string[] SplitColumns(string line) =>
        Regex.Split(line.Trim(), @"\s+");

    private static int ParseInt(string[] cols, int index) =>
        index < cols.Length && int.TryParse(cols[index], out var v) ? v : 0;
}
