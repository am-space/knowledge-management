# ADR-0002: Support PostgreSQL server and SQLite local profiles

- **Status:** Accepted
- **Date:** 2026-08-29
- **Deciders:** Project maintainers

## Context

Hosted collaboration requires multi-user concurrency, strong operational tooling, tenant isolation,
full-text search, and vector search. Personal use, development, demos, and simple self-hosting benefit
from a zero-service, portable database that can run with one application process.

Supporting SQLite by creating a separate simplified product model would fragment behavior and make
knowledge difficult to move between local and hosted installations. Treating SQLite as identical to
PostgreSQL would also hide meaningful differences in concurrency, SQL dialects, hierarchy
optimization, and vector capabilities.

## Decision

Support two first-class persistence profiles:

- PostgreSQL for hosted, collaborative, and production server deployments.
- SQLite for single-process local and personal installations.

Both profiles preserve the same stable identifiers, immutable revision semantics, workspace-owned
model, application services, and HTTP/MCP contracts. Local mode retains `WorkspaceId` even when it
automatically creates one personal workspace.

The parent relationship is the portable hierarchy source of truth. PostgreSQL may derive an `ltree`
path; SQLite uses recursive CTEs. PostgreSQL uses native full-text search and `pgvector`; SQLite uses
FTS5 and does not advertise vector similarity until a deliberate local implementation is selected.

Provider-specific mappings, migrations, queries, constraints, and integration tests remain in
Infrastructure. PostgreSQL capabilities are not reduced to the lowest common denominator.

Local mode is standalone, not an offline replica. Export/import provides initial portability.
Bidirectional synchronization requires a separate decision.

## Consequences

- Personal installations can run without PostgreSQL or Docker.
- The knowledge model remains portable between deployment profiles.
- Persistence and search require explicit provider capabilities and two integration paths.
- Schema changes require provider-appropriate migrations and validation.
- SQLite does not promise PostgreSQL-equivalent concurrency or multi-user tenant isolation.
- Local semantic vector search and local/server synchronization remain future decisions.

## Alternatives considered

- **PostgreSQL only:** simpler to implement, but imposes unnecessary operational cost on local use.
- **SQLite only:** insufficient as the intended hosted multi-user production database.
- **Separate local domain model:** rejected because it would fragment contracts and portability.
- **Pretend feature parity:** rejected because silent degradation would make retrieval behavior
  unpredictable.

## Related documentation

- [Architecture](../architecture.md)
- [Conceptual database schema](../database-schema.md)
- [Local mode](../local-mode.md)

