namespace EFX.Core.Metadata;

/// <summary>Describes a persisted scalar property discovered on a model.</summary>
public sealed record ModelProperty(
    string Name,
    string ClrType,
    bool IsNullable,
    string? StoreType = null,
    bool IsPrimaryKey = false,
    string? ColumnName = null)
{
    public string EffectiveColumnName => string.IsNullOrWhiteSpace(ColumnName) ? Name : ColumnName!;
}
