namespace EFX.Core.Providers;

public interface IDatabaseProvider
{
    string Name { get; }
    IReadOnlyList<string> GetTables(string connectionString);
    IReadOnlyList<string> GetColumns(string connectionString, string tableName);
    void Execute(string connectionString, string sql);
}
