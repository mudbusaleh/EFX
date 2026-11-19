# Roadmap

## Phase 1 - Safe foundation

- [x] Repository structure
- [x] Core project and SQLite provider
- [x] Model discovery that excludes navigation properties
- [x] SQLite schema inspection
- [x] Table and column comparison
- [x] Typed change plans and SQL preview
- [x] Explicit approval before any apply
- [x] SQLite backup and transactional DDL
- [x] Zero-Command Schema Sync sample
- [x] Automated tests for discovery and safe synchronization

## Phase 2 - Schema workflow

- [ ] Schema version history
- [ ] Explicit rename mapping and reviewed rename plans
- [ ] Index and foreign-key comparison
- [ ] Composite keys and richer constraints
- [ ] Data backfill/default strategies
- [ ] Migration artifact generation and rollback design

## Phase 3 - Data API

- [ ] Query API
- [ ] Insert/update/delete API
- [ ] Relationship loading and persistence
- [ ] Seed data

## Phase 4 - Providers

- [ ] PostgreSQL
- [ ] SQL Server
- [ ] MySQL

## Phase 5 - Production workflow

- [ ] Deployment approval integrations
- [ ] Backup retention and restore verification tooling
- [ ] CI/CD integration
- [ ] Observability and audit log
- [ ] Production-grade integration test matrix
