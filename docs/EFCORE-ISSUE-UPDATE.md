# Suggested update for EF Core issue #39569

I've started putting the idea into a real open-source project instead of keeping it only as a proposal.

Repository:
https://github.com/YOUR-USERNAME/EFX

The project is intentionally independent from EF Core. I'm not trying to turn this into another EF Core command or a wrapper around EF Core.

The first version is focused on the basic architecture: discovering C# models, inspecting the database schema, comparing the two, generating a change plan, and generating SQL before anything is applied.

I'm starting with SQLite so I can work through the core ideas without trying to solve every database provider at the same time.

It's still very early and there are a lot of things that need to be figured out, especially schema changes, renames, data safety, versioning, providers, and querying.

I wanted to put the code somewhere public so the idea can be discussed based on something real rather than just the proposal.

If this is considered outside the scope of the EF Core repository, that's completely fine. I mainly wanted to keep the connection to the original discussion and get feedback on the direction.
