namespace EFX.Core.Schema;

public sealed record DatabaseColumn(
    string Name,
    string StoreType,
    bool IsNullable,
    bool IsPrimaryKey,
    string? DefaultValue);

public sealed record DatabaseTable(string Name, IReadOnlyList<DatabaseColumn> Columns)
{
    public DatabaseColumn? FindColumn(string name) => Columns.FirstOrDefault(
        column => string.Equals(column.Name, name, StringComparison.OrdinalIgnoreCase));
}

public sealed record DatabaseSchema(IReadOnlyList<DatabaseTable> Tables)
{
    public DatabaseTable? FindTable(string name) => Tables.FirstOrDefault(
        table => string.Equals(table.Name, name, StringComparison.OrdinalIgnoreCase));

    public static DatabaseSchema Empty { get; } = new(Array.Empty<DatabaseTable>());
}

public sealed record DatabaseApplyResult(int ExecutedStatements, string? BackupPath);
