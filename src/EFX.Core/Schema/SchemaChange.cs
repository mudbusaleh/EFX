namespace EFX.Core.Schema;

public enum SchemaChangeKind { CreateTable, AddColumn }

public sealed record SchemaChange(SchemaChangeKind Kind, string Table, string? Column, string? Sql);
