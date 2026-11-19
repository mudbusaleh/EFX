# Zero-Command Schema Sync

Zero-Command Schema Sync lets an application ask EFX to compare its model assembly with the current database during normal startup. There is no migration command to remember. **It is not silent auto-migration:** the host must provide an explicit approval callback, and EFX applies nothing unless that callback approves the complete plan.

## Quick start

```csharp
using EFX.Core;
using EFX.Sqlite;

var engine = new EfxEngine();
var provider = new SqliteProvider();

var result = engine.Sync(
    typeof(Program).Assembly,
    provider,
    "Data Source=app.db",
    plan =>
    {
        Console.WriteLine(plan.ToDisplayString());
        Console.Write("Type APPLY to approve: ");
        return string.Equals(Console.ReadLine()?.Trim(), "APPLY", StringComparison.Ordinal);
    });
```

For a web service, replace the console callback with an approval gate from your deployment process, operations dashboard, or controlled startup workflow. A non-interactive deployment should default to rejection unless an authorized operator has approved the plan.

## What this version can apply

- Create a missing table using discovered scalar model properties.
- Add a nullable, non-key column to an existing table.

## What is blocked for manual review

- Removing a table or column (EFX never drops them automatically).
- Changing a column's type or nullability, or its primary-key status.
- Adding a required column to an existing table without an explicitly designed backfill/default strategy.
- Database columns that are not mapped by the current models. EFX preserves them rather than assuming they are obsolete.
- Composite keys, duplicate table/column mappings, schema-qualified table mappings, and unsupported CLR property types.

A plan containing any manual-review item is not partially applied. Resolve the item, generate a fresh plan, and request approval again.

## SQLite protections

1. An approval callback is required by `EfxEngine.Sync`.
2. The core issues a one-time in-memory approval capability bound to the exact reviewed plan. The SQLite provider refuses direct apply calls without that capability and refuses replay.
3. The SQLite provider accepts only typed `CreateTable` and nullable `AddColumn` operations. It regenerates SQL from model metadata and does not execute the plan's display SQL string.
4. Planning does not create a missing database file; declining approval leaves the filesystem unchanged.
5. The provider compares the complete inspected schema snapshot with the live database before application and again inside the transaction. Any change to a table, column, type, nullability, key, or default after planning invalidates the plan.
6. For an existing file-backed database, EFX creates a timestamped `.efx-backup-*.db` backup before applying changes. If backup creation fails, application stops.
7. DDL operations run inside a SQLite transaction and are rolled back if any statement fails.

For a brand-new file-backed database, planning does not create the file; the first approved apply creates it, so there is no prior data file to back up. This provider version rejects `:memory:` databases and SQLite URI data sources for schema sync rather than claiming backup protection it cannot verify. Backups should be retained and copied to the organization's normal protected backup storage.

## Important production note

This is an early implementation, not a certification that any database migration is risk-free. Before using it with valuable or production data, test against a disposable copy, verify backup restoration, use a controlled deployment approval, and monitor the database after application. This version intentionally does not attempt table rebuilds, rename inference, index/foreign-key comparison, cross-provider migration, or automatic data conversion.
