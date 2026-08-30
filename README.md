# Knowledge Management

An AI-native knowledge management system for humans and agents. The product combines hierarchical
organization, semantic graph relations, immutable revisions, workspace isolation, hybrid retrieval,
impact analysis, and consistency checking.

## Status

The repository is in its design and bootstrap stage. It contains the agreed project structure and
architecture documentation, but no executable application, build, migration, or test suite yet.

## Planned runtime profiles

- **Local:** a single application instance using a portable SQLite database.
- **Server:** a hosted multi-user application using PostgreSQL, with `pgvector` and optional `ltree`.

Both profiles use the same domain model and HTTP/MCP contracts.

## Repository layout

```text
src/
  Knowledge.Server/    ASP.NET Core modular monolith
  Knowledge.Web/       React and TypeScript web client

tests/
  Knowledge.Server.UnitTests/
  Knowledge.Server.IntegrationTests/
  Knowledge.Server.ArchitectureTests/
  Knowledge.E2E.Tests/

docs/                  Reference documentation, ADRs, plans, and history
scripts/               Canonical setup and verification entry points once implemented
```

Start with the [documentation index](docs/README.md). The original
[system design](knowledge-management-system-design.md) remains useful background; accepted ADRs and
living reference documentation take precedence when they differ from that initial proposal.

