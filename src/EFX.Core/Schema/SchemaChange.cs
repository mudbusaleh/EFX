using EFX.Core.Metadata;

namespace EFX.Core.Schema;

public enum SchemaChangeKind
{
    CreateTable,
    AddColumn,
    ManualIntervention
}

/// <summary>
/// A typed schema operation. Providers must generate and validate SQL from this metadata;
/// the SQL string is for review and must never be treated as executable user input.
/// </summary>
public sealed record SchemaChange(
    SchemaChangeKind Kind,
    string Table,
    string? Column,
    string? Sql,
    bool IsDestructive = false,
    string? Description = null,
    string? Warning = null,
    ModelInfo? Model = null,
    ModelProperty? Property = null)
{
    public bool IsExecutable => (Kind is SchemaChangeKind.CreateTable or SchemaChangeKind.AddColumn)
        && !IsDestructive
        && !string.IsNullOrWhiteSpace(Sql);
}
