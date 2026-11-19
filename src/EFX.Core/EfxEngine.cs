using System.Reflection;
using EFX.Core.Discovery;
using EFX.Core.Providers;
using EFX.Core.Schema;

namespace EFX.Core;

/// <summary>
/// Model-first schema planner. Sync has no command-line dependency, but always requires
/// an explicit approval callback before touching the database.
/// </summary>
public sealed class EfxEngine
{
    private readonly ModelDiscovery _discovery = new();

    public IReadOnlyList<Metadata.ModelInfo> DiscoverModels(Assembly assembly) => _discovery.Discover(assembly);

    public SchemaPlan Plan(Assembly assembly, IDatabaseProvider provider, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var schema = provider.GetSchema(connectionString);
        var plan = new SchemaPlan(schema);
        var models = DiscoverModels(assembly);
        var duplicateTableNames = models
            .GroupBy(model => model.EffectiveTableName, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var model in models)
        {
            if (duplicateTableNames.Contains(model.EffectiveTableName))
            {
                plan.Add(new SchemaChange(
                    SchemaChangeKind.ManualIntervention,
                    model.EffectiveTableName,
                    null,
                    null,
                    IsDestructive: false,
                    Description: "Multiple discovered models map to the same table name.",
                    Warning: "Resolve duplicate table mappings before schema synchronization."));
                continue;
            }

            if (model.EffectiveUnsupportedProperties.Count > 0)
            {
                plan.Add(new SchemaChange(
                    SchemaChangeKind.ManualIntervention,
                    model.EffectiveTableName,
                    null,
                    null,
                    IsDestructive: false,
                    Description: $"The model contains unsupported property mapping(s): {string.Join(", ", model.EffectiveUnsupportedProperties)}.",
                    Warning: "EFX will not silently ignore complex properties or composite keys. Add an explicit mapping or handle this model manually."));
                continue;
            }

            if (model.Properties.Count == 0)
            {
                plan.Add(new SchemaChange(
                    SchemaChangeKind.ManualIntervention,
                    model.EffectiveTableName,
                    null,
                    null,
                    IsDestructive: false,
                    Description: "The model has no supported scalar properties. Add an explicit mapping before creating this table."));
                continue;
            }

            var mappedModel = model;
            var table = schema.FindTable(model.EffectiveTableName);

            // The original EFX prototype used the class name as its table name. Reuse that
            // legacy table when no explicit [Table] mapping was supplied, rather than
            // creating a second table beside existing user data.
            if (table is null && !model.IsTableNameExplicit)
            {
                var legacyTable = schema.FindTable(model.Name);
                if (legacyTable is not null)
                {
                    table = legacyTable;
                    mappedModel = model with { TableName = legacyTable.Name };
                }
            }

            if (table is null)
            {
                var create = new SchemaChange(
                    SchemaChangeKind.CreateTable,
                    model.EffectiveTableName,
                    null,
                    null,
                    Description: $"Create a table for model '{model.Name}'.",
                    Model: model);
                plan.Add(create with { Sql = provider.GenerateSql(create) });
                continue;
            }

            CompareExistingTable(mappedModel, table, provider, plan);
        }

        return plan;
    }

    /// <summary>
    /// Zero-command schema sync: plans at runtime and asks the host application for explicit
    /// approval. If approval is absent, declined, or the plan needs manual intervention,
    /// no database changes are applied.
    /// </summary>
    public SchemaSyncResult Sync(
        Assembly assembly,
        IDatabaseProvider provider,
        string connectionString,
        Func<SchemaPlan, bool> requestApproval)
    {
        ArgumentNullException.ThrowIfNull(requestApproval);

        var plan = Plan(assembly, provider, connectionString);
        if (!plan.HasChanges)
            return new SchemaSyncResult(plan, SchemaSyncStatus.NoChanges);

        if (!plan.CanApply)
            return new SchemaSyncResult(plan, SchemaSyncStatus.BlockedForManualIntervention);

        if (!requestApproval(plan))
            return new SchemaSyncResult(plan, SchemaSyncStatus.ApprovalDeclined);

        var approval = new SchemaApproval(plan);
        var applied = provider.ApplyChanges(connectionString, plan, approval);
        return new SchemaSyncResult(plan, SchemaSyncStatus.Applied, applied);
    }

    private static void CompareExistingTable(
        Metadata.ModelInfo model,
        DatabaseTable table,
        IDatabaseProvider provider,
        SchemaPlan plan)
    {
        var modelColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in model.Properties)
        {
            var columnName = property.EffectiveColumnName;
            modelColumns.Add(columnName);
            var databaseColumn = table.FindColumn(columnName);

            if (databaseColumn is null)
            {
                if (!property.IsNullable)
                {
                    plan.Add(new SchemaChange(
                        SchemaChangeKind.ManualIntervention,
                        table.Name,
                        columnName,
                        null,
                        IsDestructive: false,
                        Description: "A required column is missing from an existing table.",
                        Warning: "EFX will not invent a default value or modify existing rows. Add a reviewed backfill/default strategy manually.",
                        Property: property));
                    continue;
                }

                var add = new SchemaChange(
                    SchemaChangeKind.AddColumn,
                    table.Name,
                    columnName,
                    null,
                    Description: "Add a nullable model property to the existing table.",
                    Model: model,
                    Property: property);
                plan.Add(add with { Sql = provider.GenerateSql(add) });
                continue;
            }

            if (!TypesAreCompatible(property, databaseColumn, provider.Name))
            {
                plan.Add(new SchemaChange(
                    SchemaChangeKind.ManualIntervention,
                    table.Name,
                    columnName,
                    null,
                    IsDestructive: true,
                    Description: $"Column type differs (database: '{databaseColumn.StoreType}', model: '{property.StoreType ?? property.ClrType}').",
                    Warning: "Type conversion can truncate, reject, or otherwise alter existing data. EFX will not rebuild this table automatically.",
                    Property: property));
                continue;
            }

            if (property.IsNullable != databaseColumn.IsNullable || property.IsPrimaryKey != databaseColumn.IsPrimaryKey)
            {
                plan.Add(new SchemaChange(
                    SchemaChangeKind.ManualIntervention,
                    table.Name,
                    columnName,
                    null,
                    IsDestructive: true,
                    Description: "Nullability or primary-key metadata differs between the model and database.",
                    Warning: "This change can affect existing rows or constraints and needs a manually reviewed migration.",
                    Property: property));
            }
        }

        foreach (var databaseColumn in table.Columns.Where(column => !modelColumns.Contains(column.Name)))
        {
            plan.Add(new SchemaChange(
                SchemaChangeKind.ManualIntervention,
                table.Name,
                databaseColumn.Name,
                null,
                IsDestructive: false,
                Description: "The database contains a column that is not mapped by the current model.",
                Warning: "EFX preserves unmapped columns and will not drop them automatically."));
        }
    }

    private static bool TypesAreCompatible(Metadata.ModelProperty property, DatabaseColumn column, string providerName)
    {
        if (string.Equals(providerName, "SQLite", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(property.StoreType))
                return string.Equals(NormalizeType(property.StoreType), NormalizeType(column.StoreType), StringComparison.OrdinalIgnoreCase);

            var expectedType = MapClrTypeToSqlite(property.ClrType);
            return string.Equals(NormalizeSqliteAffinity(expectedType), NormalizeSqliteAffinity(column.StoreType), StringComparison.OrdinalIgnoreCase);
        }

        var declaredStoreType = property.StoreType;
        if (!string.IsNullOrWhiteSpace(declaredStoreType))
            return string.Equals(NormalizeType(declaredStoreType), NormalizeType(column.StoreType), StringComparison.OrdinalIgnoreCase);

        return string.Equals(property.ClrType, column.StoreType, StringComparison.OrdinalIgnoreCase);
    }

    private static string MapClrTypeToSqlite(string clrType)
    {
        var type = clrType.Split(',')[0];
        if (type.EndsWith("[]", StringComparison.Ordinal)) return "BLOB";
        if (type is "System.String" or "System.Char" or "System.Guid" or "System.DateTime" or "System.DateTimeOffset" or "System.DateOnly" or "System.TimeOnly" or "System.TimeSpan") return "TEXT";
        if (type is "System.Single" or "System.Double") return "REAL";
        if (type is "System.Decimal") return "NUMERIC";
        if (type is "System.Boolean" or "System.Byte" or "System.SByte" or "System.Int16" or "System.UInt16" or "System.Int32" or "System.UInt32" or "System.Int64" or "System.UInt64" or "System.Enum") return "INTEGER";
        throw new NotSupportedException($"CLR type '{clrType}' has no safe SQLite type mapping in this EFX version.");
    }

    private static string NormalizeType(string value) => value.Trim().ToUpperInvariant();

    private static string NormalizeSqliteAffinity(string value)
    {
        var type = value.ToUpperInvariant();
        if (type.Contains("INT", StringComparison.Ordinal)) return "INTEGER";
        if (type.Contains("CHAR", StringComparison.Ordinal) || type.Contains("CLOB", StringComparison.Ordinal) || type.Contains("TEXT", StringComparison.Ordinal)) return "TEXT";
        if (type.Contains("BLOB", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(type)) return "BLOB";
        if (type.Contains("REAL", StringComparison.Ordinal) || type.Contains("FLOA", StringComparison.Ordinal) || type.Contains("DOUB", StringComparison.Ordinal)) return "REAL";
        return "NUMERIC";
    }
}
