# EFX Architecture

```text
Application startup / EFX.Cli demo
              |
              v
         EFX.Core
     Model Discovery
              |
              v
      Schema Comparison <---- Provider schema inspection
              |
              v
        Typed Change Plan
              |
              v
       Explicit Approval
              |
              v
     Plan-bound Approval Token
              |
              v
       Provider Validation
              |
              v
     Backup + Transactional Apply
              |
              v
         SQLite Provider
```

The core layer owns model metadata, schema comparison, typed change plans, and the approval boundary. Providers own schema inspection, provider-specific SQL generation, backup behavior, and safe execution.

The provider must never execute arbitrary SQL supplied by application code. In the current SQLite implementation, it regenerates SQL from typed `CreateTable` and `AddColumn` metadata. Destructive and ambiguous changes are represented as manual-intervention items and block the entire plan.
