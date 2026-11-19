# Changelog

## [v0.2.0] - 2026-11-19

### Added
- Zero-Command Schema Sync API with a mandatory explicit approval callback.
- SQLite schema inspection with table and column metadata.
- Real table/column comparison, SQLite type-affinity checks, nullability and primary-key checks.
- Readable schema plans and provider-generated SQL previews.
- SQLite file backup before changes, full-schema snapshot checks before and inside the apply transaction, and transactional DDL.
- Runnable sample and CLI demo.
- Tests for navigation-property exclusion, approval refusal, backup creation, and repeat sync.

### Safety
- Navigation properties are excluded from scalar column discovery.
- Automatic apply is restricted to create-table and nullable non-key add-column operations.
- Destructive or ambiguous changes, unsupported/duplicate mappings, and extra database columns block the entire plan for manual review.
- The SQLite provider regenerates SQL from typed metadata rather than executing plan SQL text.

## [v0.1.0] - 2026-11-18

- Initial project structure and model-first direction.
