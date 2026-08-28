using System.Text.Json.Serialization;

namespace MUAssetInspector.Core.Domain;

public sealed class AssetRecord
{
    public long Id { get; set; }
    public ClientRole ClientRole { get; set; }
    public string RelativePath { get; set; } = string.Empty;
    public string FileNameLower { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public AssetFormatKind Format { get; set; } = AssetFormatKind.Unknown;
    public string Sha256 { get; set; } = string.Empty;
    public long Size { get; set; }
    public long LastWriteUtcTicks { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public bool HasAlpha { get; set; }
    public AlphaKind AlphaKind { get; set; } = AlphaKind.Unknown;
    public ParseStatus ParseStatus { get; set; } = ParseStatus.NotParsed;
    public string? ParsedJson { get; set; }
    public AssetCategory Category { get; set; } = AssetCategory.Other;
}

public sealed class DependencyRecord
{
    public long Id { get; set; }
    public long AssetId { get; set; }
    public long? DependsOnAssetId { get; set; }
    public DependencyType DepType { get; set; }
    public string RefName { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
}

public sealed class ItemRecord
{
    public long Id { get; set; }
    public int Group { get; set; }
    public int Number { get; set; }
    public string Name { get; set; } = string.Empty;
    public long? ModelAssetId { get; set; }
    public string SourceProfile { get; set; } = string.Empty;
    public string? ModelPath { get; set; }
}

public sealed class ComparisonRecord
{
    public long Id { get; set; }
    public long SourceAssetId { get; set; }
    public long? DestAssetId { get; set; }
    public ComparisonStatus Status { get; set; }
    public string? DiffJson { get; set; }
}

public sealed class DiagnosticEntry
{
    public DiagnosticSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Solution { get; set; }
    public string? RefName { get; set; }
}

public sealed class DiagnosticResult
{
    public CompatibilityLevel Level { get; set; } = CompatibilityLevel.Unknown;
    public List<DiagnosticEntry> Entries { get; set; } = [];
    public string Subject { get; set; } = string.Empty;
}

public sealed class ClientProfile
{
    public string Name { get; set; } = string.Empty;
    public string DataRootHint { get; set; } = "Data";
    public int[] BmdVersions { get; set; } = [10, 12];
    public int OzjHeaderSkip { get; set; } = 24;
    public int OztHeaderSkip { get; set; } = 16;
    public string[] TextureFallbacks { get; set; } = [".ozj", ".OZJ", ".jpg", ".tga", ".ozt", ".OZT"];
    public string[] EffectTables { get; set; } = [];
    public string? CosmeticCatalog { get; set; }
    public int ItemIndexBase { get; set; } = 300;
    public bool IncludeServerOverrides { get; set; }
    public string[] ImportIncludePrefixes { get; set; } =
    [
        "Item/", "Player/", "Effect/", "Local/", "Skill/"
    ];
    public string[] ImportExcludePrefixes { get; set; } =
    [
        "World", "Monster/", "InGameShop", "Interface/", "NPC/", "Object/"
    ];
}

public sealed class AssetParseResult
{
    public AssetFormatKind Format { get; set; }
    public ParseStatus Status { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public bool HasAlpha { get; set; }
    public AlphaKind AlphaKind { get; set; } = AlphaKind.Unknown;
    public Dictionary<string, object?> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> ReferencedPaths { get; set; } = [];
    public List<string> TextureNames { get; set; } = [];
    public string? ErrorMessage { get; set; }

    public string ToJson() =>
        System.Text.Json.JsonSerializer.Serialize(new
        {
            Format,
            Status,
            Width,
            Height,
            HasAlpha,
            AlphaKind,
            Metadata,
            ReferencedPaths,
            TextureNames,
            ErrorMessage
        });
}

public sealed class ParseContext
{
    public required ClientProfile Profile { get; init; }
    public required string DataRoot { get; init; }
    public required string RelativePath { get; init; }
}

public sealed class DependencyNode
{
    public string Name { get; set; } = string.Empty;
    public DependencyType Type { get; set; }
    public string? RelativePath { get; set; }
    public ComparisonStatus? CompareStatus { get; set; }
    public List<DependencyNode> Children { get; set; } = [];
}

public sealed class ScanProgress
{
    public int FilesDiscovered { get; set; }
    public int FilesProcessed { get; set; }
    public int FilesSkipped { get; set; }
    public string? CurrentFile { get; set; }
    public bool IsComplete { get; set; }
}

public interface IExternalBridge
{
    void SendToUiEditor(long assetId);
    void SendToLivePreview(string packagePath);
}

public sealed class NullExternalBridge : IExternalBridge
{
    public void SendToUiEditor(long assetId) { }
    public void SendToLivePreview(string packagePath) { }
}
