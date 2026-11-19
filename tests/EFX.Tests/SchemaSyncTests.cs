using EFX.Core;
using EFX.Core.Schema;
using EFX.Sample.Models;
using EFX.Sqlite;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EFX.Tests;

public class SchemaSyncTests
{
    [Fact]
    public void DeclinedApprovalDoesNotChangeDatabase()
    {
        using var database = new TemporaryDatabase();
        var engine = new EfxEngine();
        var provider = new SqliteProvider();

        var result = engine.Sync(typeof(User).Assembly, provider, database.ConnectionString, _ => false);

        Assert.Equal(SchemaSyncStatus.ApprovalDeclined, result.Status);
        Assert.Empty(provider.GetSchema(database.ConnectionString).Tables);
        Assert.False(File.Exists(database.DatabasePath));
    }

    [Fact]
    public void ApprovedSyncCreatesTablesAndBackupForExistingDatabase()
    {
        using var database = new TemporaryDatabase();
        using (var connection = new SqliteConnection(database.ConnectionString))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE ExistingData (Id INTEGER PRIMARY KEY, Value TEXT); INSERT INTO ExistingData(Value) VALUES ('keep');";
            command.ExecuteNonQuery();
        }

        var engine = new EfxEngine();
        var provider = new SqliteProvider();
        var result = engine.Sync(typeof(User).Assembly, provider, database.ConnectionString, plan => plan.CanApply);

        Assert.Equal(SchemaSyncStatus.Applied, result.Status);
        var applyResult = Assert.IsType<DatabaseApplyResult>(result.ApplyResult);
        var backupPath = Assert.IsType<string>(applyResult.BackupPath);
        Assert.True(File.Exists(backupPath));
        Assert.Contains(provider.GetSchema(database.ConnectionString).Tables, table => table.Name == "Users");
    }

    [Fact]
    public void OrderNavigationPropertyDoesNotCreateAUserColumn()
    {
        using var database = new TemporaryDatabase();
        var engine = new EfxEngine();
        var provider = new SqliteProvider();
        var result = engine.Sync(typeof(Order).Assembly, provider, database.ConnectionString, _ => true);

        Assert.Equal(SchemaSyncStatus.Applied, result.Status);
        var orderTable = Assert.Single(provider.GetSchema(database.ConnectionString).Tables, table => table.Name == "Orders");
        Assert.Contains(orderTable.Columns, column => column.Name == "UserId");
        Assert.DoesNotContain(orderTable.Columns, column => column.Name == "User");
    }

    [Fact]
    public void SecondSyncDoesNotRequestApprovalOrExecuteAgain()
    {
        using var database = new TemporaryDatabase();
        var engine = new EfxEngine();
        var provider = new SqliteProvider();
        engine.Sync(typeof(User).Assembly, provider, database.ConnectionString, _ => true);

        var approvalRequested = false;
        var result = engine.Sync(typeof(User).Assembly, provider, database.ConnectionString, _ =>
        {
            approvalRequested = true;
            return true;
        });

        Assert.Equal(SchemaSyncStatus.NoChanges, result.Status);
        Assert.False(approvalRequested);
    }


    [Fact]
    public void AddsMissingNullableColumnAndPreservesExistingRows()
    {
        using var database = new TemporaryDatabase();
        using (var connection = new SqliteConnection(database.ConnectionString))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE Users (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, CreatedAt TEXT NOT NULL); INSERT INTO Users(Name, CreatedAt) VALUES ('A user', '2026-01-01');";
            command.ExecuteNonQuery();
        }

        var engine = new EfxEngine();
        var provider = new SqliteProvider();
        var result = engine.Sync(typeof(User).Assembly, provider, database.ConnectionString, plan => plan.CanApply);

        Assert.Equal(SchemaSyncStatus.Applied, result.Status);
        var userTable = provider.GetSchema(database.ConnectionString).FindTable("Users");
        Assert.NotNull(userTable);
        var emailColumn = userTable!.FindColumn("Email");
        Assert.NotNull(emailColumn);
        Assert.True(emailColumn!.IsNullable);

        using var verify = new SqliteConnection(database.ConnectionString);
        verify.Open();
        using var verifyCommand = verify.CreateCommand();
        verifyCommand.CommandText = "SELECT Name, Email FROM Users WHERE Id = 1;";
        using var reader = verifyCommand.ExecuteReader();
        Assert.True(reader.Read());
        Assert.Equal("A user", reader.GetString(0));
        Assert.True(reader.IsDBNull(1));
    }

    [Fact]
    public void ColumnTypeMismatchIsDetectedAndNeverRebuiltAutomatically()
    {
        using var database = new TemporaryDatabase();
        using (var connection = new SqliteConnection(database.ConnectionString))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE Users (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name INTEGER NOT NULL, Email TEXT, CreatedAt TEXT NOT NULL);";
            command.ExecuteNonQuery();
        }

        var approvalRequested = false;
        var provider = new SqliteProvider();
        var result = new EfxEngine().Sync(typeof(User).Assembly, provider, database.ConnectionString, _ =>
        {
            approvalRequested = true;
            return true;
        });

        Assert.Equal(SchemaSyncStatus.BlockedForManualIntervention, result.Status);
        Assert.False(approvalRequested);
        Assert.Contains(result.Plan.Changes, change =>
            change.Kind == SchemaChangeKind.ManualIntervention
            && change.Table == "Users"
            && change.Column == "Name");
        Assert.Equal("INTEGER", provider.GetSchema(database.ConnectionString).FindTable("Users")!.FindColumn("Name")!.StoreType);
    }

    [Fact]
    public void ExtraDatabaseColumnBlocksWholePlanInsteadOfDroppingData()
    {
        using var database = new TemporaryDatabase();
        using (var connection = new SqliteConnection(database.ConnectionString))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE Users (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, CreatedAt TEXT NOT NULL, LegacyValue TEXT); INSERT INTO Users(Name, CreatedAt, LegacyValue) VALUES ('A user', '2026-01-01', 'keep-me');";
            command.ExecuteNonQuery();
        }

        var approvalRequested = false;
        var provider = new SqliteProvider();
        var result = new EfxEngine().Sync(typeof(User).Assembly, provider, database.ConnectionString, _ =>
        {
            approvalRequested = true;
            return true;
        });

        Assert.Equal(SchemaSyncStatus.BlockedForManualIntervention, result.Status);
        Assert.False(approvalRequested);
        Assert.DoesNotContain(provider.GetSchema(database.ConnectionString).Tables, table => table.Name == "Products");
        Assert.Contains(provider.GetSchema(database.ConnectionString).FindTable("Users")!.Columns, column => column.Name == "LegacyValue");
    }


    [Fact]
    public void StalePlanIsRejectedBeforeAnyPlannedChangeIsApplied()
    {
        using var database = new TemporaryDatabase();
        var provider = new SqliteProvider();
        var engine = new EfxEngine();

        Assert.Throws<InvalidOperationException>(() => engine.Sync(
            typeof(User).Assembly,
            provider,
            database.ConnectionString,
            _ =>
            {
                using var connection = new SqliteConnection(database.ConnectionString);
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "CREATE TABLE Users (Id INTEGER PRIMARY KEY);";
                command.ExecuteNonQuery();
                return true;
            }));

        var tables = provider.GetSchema(database.ConnectionString).Tables;
        Assert.Contains(tables, table => table.Name == "Users");
        Assert.DoesNotContain(tables, table => table.Name == "Orders");
        Assert.DoesNotContain(tables, table => table.Name == "Products");
    }


    [Fact]
    public void AnySchemaChangeAfterPlanningRejectsTheWholePlan()
    {
        using var database = new TemporaryDatabase();
        using (var connection = new SqliteConnection(database.ConnectionString))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE Users (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, CreatedAt TEXT NOT NULL);";
            command.ExecuteNonQuery();
        }

        var provider = new SqliteProvider();
        var engine = new EfxEngine();
        Assert.Throws<InvalidOperationException>(() => engine.Sync(
            typeof(User).Assembly,
            provider,
            database.ConnectionString,
            _ =>
            {
                using var connection = new SqliteConnection(database.ConnectionString);
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "ALTER TABLE Users ADD COLUMN AddedByAnotherProcess TEXT;";
                command.ExecuteNonQuery();
                return true;
            }));

        var columns = provider.GetSchema(database.ConnectionString).FindTable("Users")!.Columns;
        Assert.Contains(columns, column => column.Name == "AddedByAnotherProcess");
        Assert.DoesNotContain(columns, column => column.Name == "Email");
    }

    [Fact]
    public void LegacySingularTableIsDetectedInsteadOfCreatingDuplicatePluralTable()
    {
        using var database = new TemporaryDatabase();
        using (var connection = new SqliteConnection(database.ConnectionString))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE User (Id INTEGER PRIMARY KEY AUTOINCREMENT);";
            command.ExecuteNonQuery();
        }

        var provider = new SqliteProvider();
        var result = new EfxEngine().Sync(typeof(User).Assembly, provider, database.ConnectionString, _ => true);

        Assert.Equal(SchemaSyncStatus.BlockedForManualIntervention, result.Status);
        var tableNames = provider.GetSchema(database.ConnectionString).Tables.Select(table => table.Name).ToArray();
        Assert.Contains("User", tableNames);
        Assert.DoesNotContain("Users", tableNames);
    }

    private sealed class TemporaryDatabase : IDisposable
    {
        private readonly string _path = Path.Combine(Path.GetTempPath(), $"efx-tests-{Guid.NewGuid():N}.db");
        public string DatabasePath => _path;
        public string ConnectionString => $"Data Source={_path}";
        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(_path)) File.Delete(_path);
            foreach (var backup in Directory.GetFiles(Path.GetTempPath(), Path.GetFileName(_path) + ".efx-backup-*.db"))
                File.Delete(backup);
        }
    }
}
