# ADR-0004: Explicit revision version and trusted workspace context

- **Status:** Accepted
- **Date:** 2026-08-31
- **Deciders:** Project maintainers

## Context

The first knowledge vertical slice must behave consistently through SQLite and PostgreSQL and later
through both HTTP and MCP. It needs to prevent lost updates, preserve immutable revision history,
and scope every operation to a workspace without accepting tenant identity from an untrusted
caller. These choices affect the application boundary, public contracts, persistence transactions,
and future hosted authorization.

## Decision

Knowledge use cases receive a trusted workspace and actor context resolved by the host before the
use case runs. Public Article operations do not accept a workspace ID. Local mode idempotently
provisions and resolves one configured owner and personal workspace; hosted mode will resolve the
same context from an authenticated principal and membership.

Each node's revision `Version` is a positive, monotonically increasing integer beginning at 1. An
Article read or mutation result returns the exact current revision ID and version. Update requires
the caller's `expectedRevisionVersion`. The persistence operation conditionally advances the
current revision in the same transaction that inserts the next immutable revision. If the expected
version is stale, the operation reports a revision conflict and commits neither change.

The HTTP contract carries the token explicitly in JSON. It does not use an opaque concurrency token,
an `ETag`, or `If-Match` for Milestone 1. Requests for a node absent from the trusted workspace,
including an ID belonging to another workspace, have the same not-found result.

## Consequences

- Application, HTTP, future MCP, and both persistence profiles share one understandable concurrency
  value.
- Clients can display revision numbers and use the same value to prevent lost updates.
- Update implementations must use a transaction and a database-enforced conditional write or
  equivalent concurrency mechanism; an in-memory pre-check is insufficient.
- Conflicts and all rejected mutations must leave revision history and the current pointer unchanged.
- Local-mode convenience remains at the trusted host boundary and does not remove workspace IDs or
  workspace filtering from the model.
- Hiding cross-workspace node existence reduces tenant information disclosure, while failure to
  resolve an authorized active workspace remains a distinct access-denied condition.
- A later API version may add standards-based HTTP conditional requests, but it must define how they
  compose with or replace the explicit application token.

## Alternatives considered

- **Last write wins:** rejected because it silently loses accepted edits and violates the revision
  consistency requirements.
- **Opaque row-version token:** rejected for the first slice because provider-specific generation is
  harder to keep equivalent across SQLite and PostgreSQL and conveys no revision meaning.
- **Revision ID as the token:** viable but rejected because the already-required sequential version
  provides a smaller, user-visible contract while revision IDs remain useful for exact identity.
- **HTTP `ETag` and `If-Match` only:** rejected because concurrency is an application invariant also
  needed by MCP and non-HTTP callers.
- **Client-supplied workspace ID:** rejected because it makes tenant selection an untrusted input and
  increases the risk of cross-workspace access.

## Related documentation

- [Knowledge application and HTTP contracts](../knowledge-contracts.md)
- [Conceptual database schema](../database-schema.md)
- [Local mode](../local-mode.md)
- [ADR-0002: PostgreSQL server and SQLite local profiles](0002-postgresql-server-and-sqlite-local-profiles.md)
