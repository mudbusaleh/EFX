[English](README.md) | [العربية](README.ar.md)

# EFX

**EFX is an independent database development framework for .NET, with the ambition of becoming a more powerful alternative to Entity Framework Core.**

EFX follows a model-first direction: developers work with ordinary C# models while EFX inspects the database, compares schemas, and builds an understandable change plan.

## New: Zero-Command Schema Sync

The application can request schema synchronization as part of its normal startup flow. There is no separate migration command in the application workflow, but **there is no silent database modification**: the host must provide an explicit approval callback. Without approval, EFX does not apply the plan.

```csharp
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
        return Console.ReadLine()?.Trim() == "APPLY";
    });
```

See [Zero-Command Schema Sync](docs/zero-command-schema-sync.md) for the approval contract, protections, and current limitations.

## Current capabilities

- Discovers model classes in `Models` namespaces.
- Includes supported scalar properties and excludes navigation objects/collections.
- Uses `Id` or `{ModelName}Id` as a conventional primary key and supports `[Key]`, `[Table]`, `[Column]`, `[Required]`, and `[NotMapped]` for basic mapping.
- Uses plural table names by convention, but detects the original prototype's singular class-name table when upgrading an existing database, avoiding a duplicate table.
- Inspects SQLite tables and column metadata.
- Compares table and column presence, SQLite type affinity, nullability, and primary-key metadata.
- Generates readable SQL previews and typed change plans.
- Creates missing tables and adds nullable non-key columns after explicit approval.
- Blocks destructive or ambiguous changes, unsupported mappings, and duplicate table/column mappings for manual review.
- Leaves a missing database file untouched during planning; creates a timestamped SQLite backup before changes to an existing file-backed database and uses a transaction for DDL.
- Rejects in-memory and SQLite URI data sources for schema sync until a verified backup strategy is available.

## Run the demo

Requires the .NET 11 SDK.

```bash
dotnet restore EFX.sln
dotnet build EFX.sln --configuration Release
dotnet test EFX.sln --configuration Release
dotnet run --project samples/EFX.Sample -- efx-demo.db
```

You can also run the same demo through the CLI project:

```bash
dotnet run --project src/EFX.Cli -- demo efx-demo.db
```

The demo displays discovered models and the proposed SQL. It changes the database only after you type `APPLY` exactly. Run it again to see the no-changes result.

## Project layout

- `src/EFX.Core` - model discovery, metadata, schema planning, and explicit sync workflow.
- `src/EFX.Sqlite` - SQLite schema inspection, SQL generation, backup, and transactional apply.
- `src/EFX.Cli` - runnable command-line demo entry point.
- `samples/EFX.Sample` - sample models and Zero-Command Schema Sync demonstration.
- `tests/EFX.Tests` - unit and SQLite integration tests.
- `docs` - architecture, safety model, and roadmap.

## Safety boundaries in this version

EFX never silently applies changes. It will not automatically drop tables or columns, infer a rename, convert existing data, alter a primary key, change column nullability/type, or add a required column to a populated table without an explicit backfill strategy. Any such case blocks the full plan rather than partially applying the safe subset.

The current SQLite provider supports a deliberately limited safe set: create missing tables and add nullable non-key columns. This is an early implementation, not a guarantee of zero risk. Always validate changes on a restorable database copy before production use.

## Project status

EFX is an early open-source project. The current implementation is a foundation for model discovery, schema comparison, safe SQLite synchronization, and review-before-apply. Indexes, foreign keys, migrations/versioning, querying, richer mapping, and additional database providers remain future work.

## Origin and license

Original Creator & Founder: **Mahmoud Saleh** — [@mudbusaleh-droid](https://github.com/mudbusaleh)

MIT License.
