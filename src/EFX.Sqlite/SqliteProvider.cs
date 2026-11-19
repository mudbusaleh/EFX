using System.Text.RegularExpressions;
using EFX.Core.Providers;
using EFX.Core.Schema;
using Microsoft.Data.Sqlite;

namespace EFX.Sqlite;

/// <summary>
/// SQLite provider with typed-operation validation, a pre-apply backup for file databases,
/// stale-plan checks, and transactional DDL.
/// </summary>
public sealed class SqliteProvider : IDatabaseProvider
{
    private static readonly Regex SafeDeclaredType = new("^[A-Za-z][A-Za-z0-9_]*(?:\\s*\\(\\s*\\d+(?:\\s*,\\s*\\d+)?\\s*\\))?$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public string Name => "SQLite";

    public DatabaseSchema GetSchema(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        var databasePath = GetFileDatabasePath(connectionString);

        // A missing file represents an empty schema. Avoid creating the database merely by
        // planning; a declined approval must leave the filesystem untouched.
        if (!File.Exists(databasePath))
            return DatabaseSchema.Empty;

        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        return ReadSchema(connection);
    }

    public string GenerateSql(SchemaChange change)
    {
        ArgumentNullException.ThrowIfNull(change);

        return change.Kind switch
        {
            SchemaChangeKind.CreateTable when change.Model is not null => BuildCreateTableSql(change.Table, change.Model.Properties),
            SchemaChangeKind.AddColumn when change.Property is not null => BuildAddColumnSql(change.Table, change.Property),
            SchemaChangeKind.ManualIntervention => throw new InvalidOperationException("Manual-intervention changes do not have executable SQL."),
            _ => throw new InvalidOperationException("The schema change is missing the model or property metadata required by SQLite.")
        };
    }

    public DatabaseApplyResult ApplyChanges(string connectionString, SchemaPlan plan, SchemaApproval approval)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(approval);

        if (!approval.TryAuthorize(plan))
            throw new UnauthorizedAccessException("SQLite changes can only be applied once with an EFX-issued approval for this exact plan.");

        if (!plan.CanApply)
            throw new InvalidOperationException("EFX refused to apply this plan because it contains unsupported or manual-intervention changes.");

        var databasePath = GetFileDatabasePath(connectionString);
        var databaseExistedBeforeOpen = File.Exists(databasePath);
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        // Check once before backup and again inside the transaction. The second check
        // protects against changes made by another process between planning and apply.
        ValidatePlanAgainstCurrentSchema(connection, plan);
        var backupPath = CreateBackup(connection, databasePath, databaseExistedBeforeOpen);

        using var transaction = connection.BeginTransaction(deferred: false);
        try
        {
            ValidatePlanAgainstCurrentSchema(connection, plan, transaction);
            foreach (var change in plan.ExecutableChanges)
            {
                var sql = GenerateSql(change); // Regenerate from typed metadata; never trust change.Sql.
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch (Exception exception)
        {
            transaction.Rollback();
            var backupNote = backupPath is null
                ? " No pre-existing database file needed a backup."
                : $" The pre-apply backup is available at: {backupPath}";
            throw new InvalidOperationException(
                $"EFX could not apply the schema plan. The transaction was rolled back.{backupNote}",
                exception);
        }

        return new DatabaseApplyResult(plan.ExecutableChanges.Count, backupPath);
    }

    private static DatabaseSchema ReadSchema(SqliteConnection connection, SqliteTransaction? transaction = null)
    {
        var tableNames = new List<string>();
        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' ORDER BY name;";
            using var reader = command.ExecuteReader();
            while (reader.Read())
                tableNames.Add(reader.GetString(0));
        }

        var tables = new List<DatabaseTable>(tableNames.Count);
        foreach (var tableName in tableNames)
        {
            var columns = new List<DatabaseColumn>();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $"PRAGMA table_info({QuoteIdentifier(tableName)});";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var primaryKey = reader.GetInt32(5) > 0;
                var notNull = reader.GetInt32(3) == 1;
                columns.Add(new DatabaseColumn(
                    reader.GetString(1),
                    reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    !notNull && !primaryKey,
                    primaryKey,
                    reader.IsDBNull(4) ? null : reader.GetString(4)));
            }

            tables.Add(new DatabaseTable(tableName, columns));
        }

        return new DatabaseSchema(tables);
    }

    private static void ValidatePlanAgainstCurrentSchema(
        SqliteConnection connection,
        SchemaPlan plan,
        SqliteTransaction? transaction = null)
    {
        var current = ReadSchema(connection, transaction);
        if (!SchemasMatch(plan.ExpectedSchema, current))
            throw new InvalidOperationException("The database schema changed after this plan was created. Generate and review a fresh plan before applying.");

        foreach (var change in plan.ExecutableChanges)
        {
            var table = current.FindTable(change.Table);
            if (change.Kind == SchemaChangeKind.CreateTable && table is not null)
                throw new InvalidOperationException($"The table '{change.Table}' appeared after planning. Generate a new plan before applying.");

            if (change.Kind == SchemaChangeKind.AddColumn)
            {
                if (table is null)
                    throw new InvalidOperationException($"The table '{change.Table}' no longer exists. Generate a new plan before applying.");
                if (change.Column is null || table.FindColumn(change.Column) is not null)
                    throw new InvalidOperationException($"The column '{change.Table}.{change.Column}' changed after planning. Generate a new plan before applying.");
                if (change.Property is null || !change.Property.IsNullable || change.Property.IsPrimaryKey)
                    throw new InvalidOperationException("SQLite automatic ADD COLUMN is restricted to nullable, non-key properties in this EFX version.");
            }
        }
    }

    private static bool SchemasMatch(DatabaseSchema expected, DatabaseSchema current)
    {
        if (expected.Tables.Count != current.Tables.Count)
            return false;

        foreach (var expectedTable in expected.Tables)
        {
            var currentTable = current.Tables.FirstOrDefault(table =>
                string.Equals(table.Name, expectedTable.Name, StringComparison.Ordinal));
            if (currentTable is null || currentTable.Columns.Count != expectedTable.Columns.Count)
                return false;

            foreach (var expectedColumn in expectedTable.Columns)
            {
                var currentColumn = currentTable.Columns.FirstOrDefault(column =>
                    string.Equals(column.Name, expectedColumn.Name, StringComparison.Ordinal));
                if (currentColumn is null
                    || !string.Equals(currentColumn.StoreType, expectedColumn.StoreType, StringComparison.OrdinalIgnoreCase)
                    || currentColumn.IsNullable != expectedColumn.IsNullable
                    || currentColumn.IsPrimaryKey != expectedColumn.IsPrimaryKey
                    || !string.Equals(currentColumn.DefaultValue, expectedColumn.DefaultValue, StringComparison.Ordinal))
                    return false;
            }
        }

        return true;
    }

    private static string GetFileDatabasePath(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        var dataSource = builder.DataSource;
        if (string.IsNullOrWhiteSpace(dataSource))
            throw new InvalidOperationException("A SQLite file path is required for Zero-Command Schema Sync.");

        if (string.Equals(dataSource, ":memory:", StringComparison.OrdinalIgnoreCase)
            || builder.Mode == SqliteOpenMode.Memory)
            throw new NotSupportedException("Zero-Command Schema Sync currently requires a file-backed SQLite database. In-memory databases are not supported by this provider workflow.");

        if (dataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException("EFX refuses SQLite URI data sources because this version cannot verify their backup path. Use a normal file path.");

        return Path.GetFullPath(dataSource);
    }

    private static string? CreateBackup(SqliteConnection source, string databasePath, bool databaseExistedBeforeOpen)
    {
        if (!databaseExistedBeforeOpen)
            return null;

        var backupPath = $"{databasePath}.efx-backup-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}.db";
        var backupBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = backupPath,
            Mode = SqliteOpenMode.ReadWriteCreate
        };

        try
        {
            using var destination = new SqliteConnection(backupBuilder.ConnectionString);
            destination.Open();
            source.BackupDatabase(destination);
            return backupPath;
        }
        catch
        {
            if (File.Exists(backupPath))
                File.Delete(backupPath);
            throw;
        }
    }

    private static string BuildCreateTableSql(string tableName, IReadOnlyList<EFX.Core.Metadata.ModelProperty> properties)
    {
        if (properties.Count == 0)
            throw new InvalidOperationException($"Model for table '{tableName}' has no scalar properties.");

        var definitions = properties.Select(property => BuildColumnDefinition(property, includePrimaryKey: true)).ToArray();
        return $"CREATE TABLE {QuoteIdentifier(tableName)} ({string.Join(", ", definitions)});";
    }

    private static string BuildAddColumnSql(string tableName, EFX.Core.Metadata.ModelProperty property)
    {
        if (!property.IsNullable || property.IsPrimaryKey)
            throw new InvalidOperationException("Only nullable, non-key columns can be added automatically to an existing SQLite table.");

        return $"ALTER TABLE {QuoteIdentifier(tableName)} ADD COLUMN {BuildColumnDefinition(property, includePrimaryKey: false)};";
    }

    private static string BuildColumnDefinition(EFX.Core.Metadata.ModelProperty property, bool includePrimaryKey)
    {
        var columnName = QuoteIdentifier(property.EffectiveColumnName);
        var storeType = ResolveStoreType(property);
        if (includePrimaryKey && property.IsPrimaryKey)
        {
            if (string.Equals(storeType, "INTEGER", StringComparison.OrdinalIgnoreCase))
                return $"{columnName} INTEGER PRIMARY KEY AUTOINCREMENT";
            return $"{columnName} {storeType} NOT NULL PRIMARY KEY";
        }

        return $"{columnName} {storeType}{(property.IsNullable ? " NULL" : " NOT NULL")}";
    }

    private static string ResolveStoreType(EFX.Core.Metadata.ModelProperty property)
    {
        var declaredType = property.StoreType;
        if (!string.IsNullOrWhiteSpace(declaredType))
        {
            var explicitType = declaredType.Trim();
            if (!SafeDeclaredType.IsMatch(explicitType))
                throw new InvalidOperationException($"The declared SQLite type '{explicitType}' is not in the allowed safe format.");
            return explicitType;
        }

        var clrType = property.ClrType;
        if (clrType is "System.String" or "System.Char" or "System.Guid" or "System.DateTime" or "System.DateTimeOffset" or "System.DateOnly" or "System.TimeOnly" or "System.TimeSpan") return "TEXT";
        if (clrType is "System.Byte[]") return "BLOB";
        if (clrType is "System.Single" or "System.Double") return "REAL";
        if (clrType is "System.Decimal") return "NUMERIC";
        if (clrType is "System.Boolean" or "System.Byte" or "System.SByte" or "System.Int16" or "System.UInt16" or "System.Int32" or "System.UInt32" or "System.Int64" or "System.UInt64" or "System.Enum") return "INTEGER";
        throw new NotSupportedException($"CLR type '{clrType}' is not supported by the SQLite provider.");
    }

    private static string QuoteIdentifier(string identifier) => $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}
