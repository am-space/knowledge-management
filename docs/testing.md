# Testing and verification

`scripts/setup.sh` and `scripts/verify.sh` are the canonical local and CI entry points. The full
verification builds the server, runs unit and SQLite/PostgreSQL integration tests, then lints,
type-checks, tests, and builds the frontend.

```bash
scripts/setup.sh
scripts/verify.sh --all
```

Focused modes are `--backend`, `--frontend`, and `--integration`. Integration verification starts
the Compose PostgreSQL service unless `KNOWLEDGE_TEST_POSTGRES` supplies an isolated test database.

## Test topology

```text
tests/
├── Knowledge.Server.UnitTests/
├── Knowledge.Server.IntegrationTests/
├── Knowledge.Server.ArchitectureTests/
└── Knowledge.E2E.Tests/
```

- **Unit:** domain behavior and focused application services, organized by module.
- **Integration:** relational providers, HTTP, MCP, authentication, background work, and
  cross-workspace isolation.
- **Architecture:** reserved until executable dependency rules exist.
- **E2E:** reserved until critical product workflows exist.

Frontend component and hook tests should normally live beside their React source. Browser E2E tests
remain in the top-level E2E project or suite.

## Required coverage by concern

| Concern | Evidence |
| --- | --- |
| Domain invariant | Unit tests for success, edge, and rejection paths |
| Portable persistence | Equivalent PostgreSQL and SQLite integration tests |
| Provider optimization | Provider-specific integration tests |
| Workspace resolution | At least two workspaces proving cross-workspace rejection |
| HTTP or MCP contract | Exact external names, shapes, errors, and authorization semantics |
| Revision update | Transactionality, concurrency, immutable history, and current pointer |
| Background processing | Retry, idempotency, stale-revision handling, and failure visibility |
| AI proposal | Structured output validation, provenance, and no silent application |

Run focused checks while iterating and the full repository verification before delivery. Never
report an unavailable, skipped, or sandbox-blocked check as passed.
