using EFX.Core;
using EFX.Core.Schema;
using EFX.Sqlite;
using EFX.Sample.Models;
using Microsoft.Data.Sqlite;

namespace EFX.Sample;

/// <summary>Runnable showcase for the Zero-Command Schema Sync workflow.</summary>
public static class EfxSample
{
    public static int Run(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        Console.WriteLine("EFX - Zero-Command Schema Sync");
        Console.WriteLine($"Database: {Path.GetFullPath(databasePath)}");
        Console.WriteLine();

        var engine = new EfxEngine();
        var provider = new SqliteProvider();
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.GetFullPath(databasePath)
        }.ConnectionString;
        var models = engine.DiscoverModels(typeof(User).Assembly);

        Console.WriteLine($"Discovered {models.Count} models:");
        foreach (var model in models)
        {
            Console.WriteLine($"  - {model.Name} -> {model.EffectiveTableName} ({model.Properties.Count} scalar columns)");
        }
        Console.WriteLine();

        var result = engine.Sync(
            typeof(User).Assembly,
            provider,
            connectionString,
            plan =>
            {
                Console.WriteLine(plan.ToDisplayString());
                Console.WriteLine();
                Console.WriteLine("No database change will be made unless you explicitly approve it.");
                Console.Write("Type APPLY to approve these changes: ");
                return string.Equals(Console.ReadLine()?.Trim(), "APPLY", StringComparison.Ordinal);
            });

        if (result.Status == SchemaSyncStatus.BlockedForManualIntervention)
        {
            Console.WriteLine(result.Plan.ToDisplayString());
        }

        Console.WriteLine();
        switch (result.Status)
        {
            case SchemaSyncStatus.NoChanges:
                Console.WriteLine("The database is already synchronized. Nothing to do.");
                break;
            case SchemaSyncStatus.ApprovalDeclined:
                Console.WriteLine("Approval declined. No database changes were made.");
                break;
            case SchemaSyncStatus.BlockedForManualIntervention:
                Console.WriteLine("Sync was blocked. Review the plan and resolve the manual items first.");
                return 2;
            case SchemaSyncStatus.Applied:
                var applyResult = result.ApplyResult ?? throw new InvalidOperationException("The provider did not return an apply result.");
                Console.WriteLine($"Applied {applyResult.ExecutedStatements} schema statement(s).");
                Console.WriteLine(applyResult.BackupPath is null
                    ? "No backup was needed because this database file did not exist before the approved apply."
                    : $"Backup created: {applyResult.BackupPath}");
                break;
        }

        Console.WriteLine();
        Console.WriteLine("Current database tables:");
        foreach (var table in provider.GetSchema(connectionString).Tables)
            Console.WriteLine($"  - {table.Name}: {string.Join(", ", table.Columns.Select(column => column.Name))}");

        return result.Status == SchemaSyncStatus.BlockedForManualIntervention ? 2 : 0;
    }
}
