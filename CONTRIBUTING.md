# Contributing to EFX

Thank you for your interest in contributing to EFX!

EFX is an independent database development framework for .NET, with the ambition of becoming a more powerful alternative to Entity Framework Core.

We welcome developers, ideas, bug reports, discussions, and code contributions.

## How to Contribute

1. Fork the EFX repository.
2. Create a new branch for your changes.
3. Make your changes and add tests when appropriate.
4. Run `dotnet restore EFX.sln`, `dotnet build EFX.sln --configuration Release`, and `dotnet test EFX.sln --configuration Release`.
5. For schema/provider changes, add tests that prove both the intended operation and the no-data-loss behavior when approval is denied or a plan is unsafe.
6. Submit a Pull Request explaining your changes.

## Contribution Guidelines

- Keep code clean and maintainable.
- Follow the existing project architecture.
- Include tests for new functionality when appropriate.
- Never introduce silent schema changes or automatic destructive operations.
- Keep provider execution restricted to validated typed plans; do not execute arbitrary SQL from callers.
- Avoid introducing breaking changes without discussion.
- Keep discussions respectful and constructive.

## Issues and Suggestions

You can open an issue to report bugs, suggest features, or discuss improvements.

## Project Founder

EFX was originally created by **Mahmoud Saleh**.

GitHub: https://github.com/mudbusaleh-droid

Thank you for helping EFX grow!
