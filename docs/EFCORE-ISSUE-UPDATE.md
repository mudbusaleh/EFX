# Suggested update for EF Core issue #39569

I've started putting the idea into a real open-source project instead of keeping it only as a proposal.

Repository:
https://github.com/mudbusaleh/EFX

The project is intentionally independent from EF Core. I'm not trying to turn this into another EF Core command or a wrapper around EF Core.

The current prototype now has scalar-only model discovery, SQLite table and column comparison, readable SQL plans, and a Zero-Command Schema Sync API. Sync runs from application code, but never applies a plan without explicit approval.

The SQLite provider currently limits automatic changes to creating missing tables and adding nullable columns. It creates a backup for existing file-backed databases and applies DDL transactionally. Destructive or ambiguous changes are blocked for manual review.

It's still very early and there are a lot of things that need to be figured out, especially schema changes, renames, data safety, versioning, providers, and querying.

I wanted to put the code somewhere public so the idea can be discussed based on something real rather than just the proposal.

If this is considered outside the scope of the EF Core repository, that's completely fine. I mainly wanted to keep the connection to the original discussion and get feedback on the direction.
