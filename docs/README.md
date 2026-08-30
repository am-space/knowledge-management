# Documentation

This index separates living system documentation, accepted decisions, proposed work, and historical
material.

## Current direction

| Document | Purpose |
| --- | --- |
| [`architecture.md`](architecture.md) | Runtime profiles, module boundaries, and dependency direction |
| [`database-schema.md`](database-schema.md) | Conceptual relational model and provider constraints |
| [`local-mode.md`](local-mode.md) | SQLite local profile, capabilities, and limitations |
| [`frontend.md`](frontend.md) | React and Material UI client direction |
| [`testing.md`](testing.md) | Planned test topology and verification policy |

## Architecture decision records

- [`ADR-0001`](adr/0001-feature-oriented-modular-monolith.md) — feature-oriented modular monolith
- [`ADR-0002`](adr/0002-postgresql-server-and-sqlite-local-profiles.md) — PostgreSQL server and SQLite local profiles
- [`ADR-0003`](adr/0003-react-and-material-ui-web-client.md) — React and Material UI web client

Start new decisions from [`adr/template.md`](adr/template.md).

## Active plans

- [`milestone-0-foundation-plan.md`](milestone-0-foundation-plan.md) — executable application,
  verification, and persistence foundation

## Backlog plans

- [`knowledge-core-mvp-plan.md`](backlog/knowledge-core-mvp-plan.md) — roadmap from executable
  foundation through the local and hosted Knowledge Core MVP

## Plans and history

- [`backlog/`](backlog/README.md) contains potential work without an implementation commitment.
- [`archive/`](archive/README.md) contains completed or superseded plans retained for context.
- The root [`knowledge-management-system-design.md`](../knowledge-management-system-design.md) is the
  original design proposal. Accepted ADRs and the living reference documents above take precedence
  if the proposal becomes outdated.
