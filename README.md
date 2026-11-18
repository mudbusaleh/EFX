# EFX

**EFX is an independent database development framework for .NET, created with the ambition of becoming a more powerful alternative to Entity Framework Core.**

EFX is built around a model-first approach to database development. Its vision is to give developers a simpler, more direct, and more controlled way to work with databases, without depending on Entity Framework Core.

The long-term goal is not simply to provide another ORM, but to rethink the everyday database development workflow in .NET.

## Vision

EFX aims to provide a complete and independent database development experience, covering:

- Model definition and discovery
- Database schema management
- Schema comparison and synchronization
- Intelligent migrations and change planning
- SQL generation and execution
- Database querying and operations
- Multiple database providers

The ambition is to make database development more intuitive, transparent, reliable, and powerful.

## Initial Focus

The initial development phase focuses on:

- C# model discovery
- SQLite schema inspection
- Schema comparison
- Readable change plans
- SQL generation
- Explicit database apply workflow

SQLite is the first provider used to establish and validate the core architecture.

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
Database Apply
```

## Project Layout

- `src/EFX.Core` - Core model metadata and planning
- `src/EFX.Sqlite` - SQLite provider
- `src/EFX.Cli` - Command-line entry point
- `samples/EFX.Sample` - Example application
- `tests/EFX.Tests` - Tests
- `docs` - Architecture and roadmap

## Commands Being Explored

```bash
efx db plan
efx db script
efx db apply
```

## Development Status

EFX is currently in its early open-source development stage.

The initial implementation focuses on establishing the core architecture and validating the model-first database workflow.

The broader vision is to evolve EFX into a complete database development framework for .NET, with the long-term ambition of surpassing the capabilities and developer experience of existing solutions, including Entity Framework Core.

The features and commands listed as being explored or planned are not necessarily implemented yet.

## Open Source & Contributions

EFX is an open-source project and welcomes developers who share the vision of improving database development in .NET.

Contributions, ideas, discussions, and feedback are welcome.

## Project Origin & Founder

EFX was originally conceived and initiated by **Mahmoud Saleh**, the original creator and founder of the project.

The project is intended to grow through open-source collaboration while preserving recognition of its original creator and all contributors.

**Original Creator & Founder:** Mahmoud Saleh

**GitHub:** [@mudbusaleh-droid](https://github.com/mudbusaleh-droid)

**Contact:** mudbusaleh@gmail.com

## License

MIT License
