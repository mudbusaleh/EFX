namespace EFX.Core.Metadata;

/// <summary>Describes a model, its persisted scalar properties, and navigation metadata.</summary>
public sealed record ModelInfo(
    string Name,
    IReadOnlyList<ModelProperty> Properties,
    string? TableName = null,
    IReadOnlyList<string>? NavigationProperties = null,
    IReadOnlyList<string>? UnsupportedProperties = null,
    bool IsTableNameExplicit = false)
{
    public string EffectiveTableName => string.IsNullOrWhiteSpace(TableName) ? Name : TableName!;
    public IReadOnlyList<string> EffectiveNavigationProperties => NavigationProperties ?? Array.Empty<string>();
    public IReadOnlyList<string> EffectiveUnsupportedProperties => UnsupportedProperties ?? Array.Empty<string>();
}
