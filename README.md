# EFX

EFX is an independent database development framework for .NET.

The project explores a model-first workflow for database development without depending on Entity Framework Core.

## Initial focus

- C# model discovery
- SQLite schema inspection
- schema comparison
- readable change plans
- SQL generation
- explicit database apply workflow

SQLite is the first provider used to explore the core architecture.

EFX is an early open-source prototype. It is not a replacement for EF Core and roadmap items are not claims of completed functionality.

## Workflow

```text
C# Models
    |
    v
Model Discovery
    |
    v
Schema Comparison
    |
    v
Change Plan
    |
    v
SQL Generation
    |
    v
Review
    |
    v
Database
```

## Project layout

- `src/EFX.Core` - core model metadata and planning
- `src/EFX.Sqlite` - SQLite provider
- `src/EFX.Cli` - command-line entry point
- `samples/EFX.Sample` - example application
- `tests/EFX.Tests` - tests
- `docs` - architecture and roadmap

## Commands being explored

```bash
efx db plan
efx db script
efx db apply
```

## License

MIT
