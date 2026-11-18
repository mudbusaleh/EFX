# EFX

EFX is an independent database development framework for .NET.

The idea started from a much smaller problem: making common database development tasks easier when working with C# models.

While exploring that problem, I realized that the workflow could be much bigger than entity scaffolding or migration commands.

EFX is an attempt to build a different approach to database development in .NET.

The model is the starting point.

```csharp
public class User
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Email { get; set; }

    public DateTime CreatedAt { get; set; }
}
```

EFX can discover the model and use it to understand the database structure that the application expects.

The goal is to make the common cases simple while still allowing explicit configuration when something needs more control.

## The basic workflow

The main workflow I'm exploring is:

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

For example:

```bash
efx db plan
```

should be able to show what has changed between the current models and the database.

Something like:

```text
EFX Database Plan

Database: AppDatabase

Changes detected:

+ CREATE TABLE Products
+ ADD COLUMN Products.Description
+ CREATE TABLE Users
+ CREATE TABLE Orders

Relationships:

+ Orders.UserId -> Users.Id

No destructive changes detected.

Plan generated successfully.
```

The important part is that EFX should explain the change before applying it.

SQL should also be visible:

```bash
efx db script
```

And applying changes should be an explicit step:

```bash
efx db apply
```

## Safety

EFX is intended to be safe by default.

Destructive changes such as:

- dropping tables
- removing columns
- changing data types
- potentially losing existing data

should not happen silently.

Possible rename scenarios should also be identified instead of automatically treating a rename as a delete followed by a new column.

## Current status

EFX is an early open-source prototype.

The project is intentionally starting small.

The first implementation is focused on establishing the core architecture around:

- model discovery
- database schema inspection
- schema comparison
- change planning
- SQL generation
- database providers

SQLite is the first provider being explored.

This project is not intended to be a replacement for EF Core.

EF Core is a mature project with a large ecosystem, and EFX is exploring a different approach to database development.

## Long-term direction

Some of the areas I'm exploring include:

- schema versioning
- migrations
- querying
- relationships
- change tracking
- seed data
- database initialization
- schema inspection
- SQL generation
- production deployment
- multiple database providers

The initial provider direction is:

- SQLite
- SQL Server
- PostgreSQL
- MySQL

These are goals for the project, not claims that they are already implemented.

## Why this exists

The project came from thinking about the everyday database workflow in .NET.

I wanted to see what database development could look like if the model stayed at the center and the repetitive database work became easier to understand and manage.

I'm building this as a real open-source project and using the early stages to test the architecture, the workflow, and the idea itself.

Feedback is welcome.
