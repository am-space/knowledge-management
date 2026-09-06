# Documentation

This index separates living system documentation, accepted decisions, proposed work, and historical
material.

## Current direction

| Document | Purpose |
| --- | --- |
| [`architecture.md`](architecture.md) | Runtime profiles, module boundaries, and dependency direction |
| [`database-schema.md`](database-schema.md) | Conceptual relational model and provider constraints |
| [`knowledge-contracts.md`](knowledge-contracts.md) | Knowledge application inputs, results, HTTP shapes, and errors |
| [`local-mode.md`](local-mode.md) | SQLite local profile, capabilities, and limitations |
| [`frontend.md`](frontend.md) | React and Material UI client direction |
| [`testing.md`](testing.md) | Executable coverage, verification commands, and remaining test boundaries |
| [`work-tracking.md`](work-tracking.md) | GitHub Issue responsibilities, lifecycle, and relationship to repository plans |

## Architecture decision records

- [`ADR-0001`](adr/0001-feature-oriented-modular-monolith.md) — feature-oriented modular monolith
- [`ADR-0002`](adr/0002-postgresql-server-and-sqlite-local-profiles.md) — PostgreSQL server and SQLite local profiles
- [`ADR-0003`](adr/0003-react-and-material-ui-web-client.md) — React and Material UI web client
- [`ADR-0004`](adr/0004-explicit-revision-version-and-trusted-workspace-context.md) — explicit revision version and trusted workspace context

Start new decisions from [`adr/template.md`](adr/template.md).

## Active plans

- [`milestone-1-article-plan.md`](milestone-1-article-plan.md) — local revisioned Article scope,
  verification evidence, and delivery exit criteria

## Backlog plans

- [`knowledge-core-mvp-plan.md`](backlog/knowledge-core-mvp-plan.md) — roadmap from executable
  foundation through the local and hosted Knowledge Core MVP

## Plans and history

- [`backlog/`](backlog/README.md) contains shaped multi-issue work without an implementation
  commitment.
- [`archive/`](archive/README.md) contains completed or superseded plans retained for context.
- [`milestone-0-foundation-plan.md`](archive/milestone-0-foundation-plan.md) — completed executable
  foundation, retained as history
- The root [`knowledge-management-system-design.md`](../knowledge-management-system-design.md) is the
  original design proposal. Accepted ADRs and the living reference documents above take precedence
  if the proposal becomes outdated.
