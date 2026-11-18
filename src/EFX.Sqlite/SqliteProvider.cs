using Microsoft.Data.Sqlite;
using EFX.Core.Providers;

namespace EFX.Sqlite;

public sealed class SqliteProvider : IDatabaseProvider
{
    public string Name => "SQLite";
    public IReadOnlyList<string> GetTables(string connectionString)
    {
        using var c = new SqliteConnection(connectionString); c.Open(); using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name;";
        using var r = cmd.ExecuteReader(); var list = new List<string>(); while (r.Read()) list.Add(r.GetString(0)); return list;
    }
    public IReadOnlyList<string> GetColumns(string connectionString, string tableName)
    {
        using var c = new SqliteConnection(connectionString); c.Open(); using var cmd = c.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info(\"{tableName.Replace("\"", "\"\"")}\");";
        using var r = cmd.ExecuteReader(); var list = new List<string>(); while (r.Read()) list.Add(r.GetString(1)); return list;
    }
    public void Execute(string connectionString, string sql)
    {
        using var c = new SqliteConnection(connectionString); c.Open(); using var cmd = c.CreateCommand(); cmd.CommandText = sql; cmd.ExecuteNonQuery();
    }
}
