# Knowledge Management

An AI-native knowledge management system for humans and agents. The product combines hierarchical
organization, semantic graph relations, immutable revisions, workspace isolation, hybrid retrieval,
impact analysis, and consistency checking.

## Status

The local SQLite profile provisions a personal workspace and supports creating, reopening, editing,
and previewing Markdown Articles through React and HTTP. Saves create immutable revisions with
explicit conflict detection. PostgreSQL integration tests exercise the same persistence behavior;
hosted authentication, hierarchy operations, search, relations, and MCP remain future work.

## Prerequisites and setup

- .NET SDK 10.0.3xx (the patch is selected by `global.json`)
- Node.js 22.23.1 and npm
- Docker with Compose for PostgreSQL development and full integration verification
- Chromium system dependencies for browser tests (see [testing](docs/testing.md))

Prepare a checkout with:

```bash
scripts/setup.sh
```

Run the SQLite server and web client in separate terminals:

```bash
dotnet run --project src/Knowledge.Server --urls http://localhost:5080
npm run dev --prefix src/Knowledge.Web
```

The client is available at `http://localhost:5173`; Vite proxies `/health` and `/api` requests to the server.

Run the full repository verification with:

```bash
scripts/verify.sh --all
```

## Planned runtime profiles

- **Local:** a single application instance using a portable SQLite database.
- **Server:** a hosted multi-user application using PostgreSQL, with `pgvector` and optional `ltree`.

Both providers use the same domain model and Article application contracts. PostgreSQL HTTP Article
requests are denied until a trusted hosted identity resolver is implemented.

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
scripts/               Canonical setup and verification entry points
```

Start with the [documentation index](docs/README.md). The original
[system design](knowledge-management-system-design.md) remains useful background; accepted ADRs and
living reference documentation take precedence when they differ from that initial proposal.
Development work follows the documented [GitHub Issue lifecycle](docs/work-tracking.md), which keeps
delivery status in GitHub and durable plans and decisions in the repository.
