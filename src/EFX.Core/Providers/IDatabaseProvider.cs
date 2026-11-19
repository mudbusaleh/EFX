using EFX.Core.Schema;

namespace EFX.Core.Providers;

/// <summary>Database-specific inspection, SQL generation, and safe application contract.</summary>
public interface IDatabaseProvider
{
    string Name { get; }
    DatabaseSchema GetSchema(string connectionString);
    string GenerateSql(SchemaChange change);

    /// <summary>
    /// Applies a previously reviewed, typed plan. Implementations must reject unsupported or
    /// destructive operations, re-check plan preconditions, and use a transaction where possible.
    /// </summary>
    DatabaseApplyResult ApplyChanges(string connectionString, SchemaPlan plan, SchemaApproval approval);
}
