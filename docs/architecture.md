# EFX Architecture

EFX is split into a core layer, database providers, and a CLI.

```text
EFX.Cli
   |
   v
EFX.Core
   |
   +---- EFX.Sqlite
   |
   +---- future providers
```

The core layer should not contain SQLite-specific SQL. Providers handle database-specific details.

Planning and applying changes are separate operations. Destructive operations should not be silently applied.
