# Testing and verification

No executable application or automated test suite exists yet. Once code is scaffolded, the
repository will provide `scripts/setup.sh` and `scripts/verify.sh` as the canonical local and CI
entry points.

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
- **Architecture:** dependency direction and module boundaries worth enforcing mechanically.
- **E2E:** critical human and agent workflows through real application entry points.

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

