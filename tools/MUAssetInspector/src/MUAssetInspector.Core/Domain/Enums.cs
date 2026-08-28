namespace MUAssetInspector.Core.Domain;

public enum ClientRole
{
    Source,
    Destination
}

public enum AssetFormatKind
{
    Unknown,
    Ozj,
    Ozt,
    BmdMesh,
    PlainImage,
    EffectTxt,
    XmlConfig,
    Other
}

public enum ParseStatus
{
    NotParsed,
    Success,
    Partial,
    Failed,
    UnsupportedVersion
}

public enum AlphaKind
{
    Unknown,
    NoAlpha,
    BinaryAlpha,
    PartialAlpha,
    FullAlpha
}

public enum ComparisonStatus
{
    Identical,
    PresentDifferent,
    Missing,
    CaseMismatch,
    FormatMismatch
}

public enum DependencyType
{
    Model,
    Texture,
    Alpha,
    EffectStatic,
    EffectDynamic,
    SetEffect,
    MeshEffect,
    Cloth,
    EffectBitmap,
    EffectIdRemap,
    ConfigReference
}

public enum DiagnosticSeverity
{
    Info,
    Warning,
    Error
}

public enum CompatibilityLevel
{
    FullyCompatible,
    PartiallyCompatible,
    Incompatible,
    Unknown
}

public enum AssetCategory
{
    Other,
    Sets,
    Swords,
    Wings,
    Helpers,
    Effects,
    Textures,
    Player,
    Config
}

public enum ImportScope
{
    /// <summary>Item, Player, Effect, Local, Skill — no maps/monsters/shop.</summary>
    CosmeticsOnly,
    /// <summary>Every missing .bmd/.ozj/.ozt/.txt/.xml (dangerous bulk copy).</summary>
    AllCompatible
}
