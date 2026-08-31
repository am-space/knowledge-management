# AGENTS.md

This file provides repository-wide guidance to Codex when working in this repository.

## Project status

This repository has an executable Milestone 0 foundation. It includes the ASP.NET Core server,
React client, SQLite and PostgreSQL persistence profiles, focused tests, canonical scripts, and CI.
Knowledge-domain behavior and the first database migrations begin in Milestone 1.

Start with [`docs/README.md`](docs/README.md). Accepted decisions live in `docs/adr/`, and living
reference pages describe the current direction. The root
[`knowledge-management-system-design.md`](knowledge-management-system-design.md) is the initial
proposal; accepted ADRs and current reference documentation take precedence if they differ from it.

Canonical commands:

```bash
scripts/setup.sh
dotnet run --project src/Knowledge.Server --urls http://localhost:5080
npm run dev --prefix src/Knowledge.Web
scripts/verify.sh --all
```

Focused verification is available through `scripts/verify.sh --backend`, `--frontend`, and
`--integration`. PostgreSQL development uses `docker compose up --detach --wait postgres`.

## Product overview

The project is a multi-user knowledge management system for humans and AI agents. It combines:

- hierarchical organization for navigation and inherited context;
- semantic graph relations for meaning, dependency, and impact analysis;
- stable knowledge-node identity with immutable content revisions;
- workspace-scoped ownership and access;
- full-text, vector, graph, and hierarchy-aware retrieval;
- deterministic and AI-assisted consistency analysis;
- HTTP and MCP interfaces over the same application behavior.

The intended initial stack is .NET 10, ASP.NET Core, PostgreSQL for hosted multi-user deployments,
SQLite for the local personal profile, React with TypeScript, Docker, `pgvector` for PostgreSQL, and
optional PostgreSQL `ltree`. The application should begin as a feature-oriented modular monolith.

## Repository structure

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

Do not create empty architectural layers merely to match a diagram. Add module-internal `Domain`,
`Features`, `Presentation`, and `Infrastructure` directories with the code that first needs them.

## Git workflow

- Never commit feature work directly to `master`.
- Before the first task commit, create or switch to a dedicated branch. Unless the user specifies a
  name, use `feature/<short-description>` for enhancements and `fix/<short-description>` for bugs.
- Preserve unrelated staged and working-tree changes; do not include them in task commits.
- Do not commit, push, or open a pull request unless the user authorizes it.
- Target `master` when a pull request is requested.

## Architecture

Organize the backend as a feature-oriented modular monolith. The planned top-level backend modules
are Knowledge, Search, Consistency, and Workspaces, with shared infrastructure for persistence, AI,
authentication, and observability.

Within a module, preserve these responsibility and dependency rules:

```text
Presentation (HTTP and MCP)
          |
          v
Features / Application
          |
          v
Domain

Infrastructure implements persistence and external integrations used by the above layers.
```

- **Domain** owns entities, value objects, state transitions, and business invariants. It must not
  depend on ASP.NET Core, MCP transports, EF Core, PostgreSQL, an LLM provider, or UI concerns.
- **Features / Application** owns use cases and orchestration. Prefer focused services such as
  `NodeService`, `RelationService`, `KnowledgeSearch`, and `ImpactAnalyzer`. Extract a dedicated
  workflow when its rules, dependencies, or lifecycle become substantial.
- **Presentation** maps HTTP or MCP input and output and delegates to application behavior. Keep it
  thin and do not duplicate a use case between transports.
- **Infrastructure** owns EF Core mappings, relational database implementations, migrations,
  background execution, search implementations, provider adapters, and other external I/O.

Do not introduce microservices, MediatR, generic repositories, mechanical one-handler-per-operation
CQRS, or interfaces without a concrete boundary or testing need. EF Core may be used directly from
application services where that remains clear and testable. Prefer explicit, local code over
framework ceremony.

## Core invariants

- Every tenant-owned aggregate must be scoped to a workspace. Never select or mutate a workspace
  from an untrusted client-supplied identity without authorization against the authenticated user.
- A `KnowledgeNode` has stable identity. Editing current knowledge creates a new immutable
  `KnowledgeRevision`; it must not overwrite revision history.
- Define explicitly whether hierarchy moves, status transitions, and relation changes are versioned
  or audited before implementing them. Do not let those changes bypass history accidentally.
- A node cannot be its own parent. Prevent invalid hierarchy cycles, invalid relation types,
  inappropriate self-relations, cross-workspace edges, and invalid status transitions.
- Keep the current-revision pointer and revision sequence transactionally consistent. Design for
  concurrent edits rather than relying on last-write-wins behavior.
- Store canonical semantic relations structurally. Markdown links may complement the graph but must
  not become the only source of relationship truth.
- Treat HTTP routes, DTOs, MCP tool names, argument schemas, resource URIs, and result shapes as
  external contracts. Prefer additive evolution; intentional breaking changes require explicit
  migration and compatibility guidance.
- AI consistency analysis creates reports, issues, or proposed changes. It must not silently rewrite
  accepted knowledge. Human approval remains the default for applying AI-generated changes.
- Scope AI analysis to a deliberate impact set and record provenance sufficient to explain which
  revisions and relationships informed a result.
- Keep LLM and embedding integrations provider-independent at the application boundary. Provider
  SDK types must not leak into the domain or public contracts.

## Persistence and background work

- Support two deliberate persistence profiles: PostgreSQL for hosted, collaborative, and production
  deployments; SQLite for single-process local and personal installations.
- Preserve the same domain identities, revision rules, workspace ownership, application behavior,
  and public contracts across both profiles. Keep `WorkspaceId` in SQLite even when local mode
  initially creates only one personal workspace.
- Use database constraints and indexes to reinforce domain and workspace invariants rather than
  relying only on application checks.
- Keep `WorkspaceId` filtering explicit and reviewable in tenant-owned queries. Introduce PostgreSQL
  row-level security only through a documented decision and with isolation tests. Do not present
  SQLite local mode as providing PostgreSQL-equivalent concurrent multi-user isolation.
- Keep the parent relationship as portable hierarchy truth. PostgreSQL may use `ltree` as a derived
  optimization; SQLite should initially use recursive CTEs.
- Use PostgreSQL full-text search and `pgvector` in the server profile. Use SQLite FTS5 for local
  keyword search. Local vector search must be an explicit capability backed by a deliberate
  implementation; do not silently emulate or advertise unavailable semantic search.
- Keep provider-specific queries, schema configuration, and search optimizations in Infrastructure.
  Do not weaken the PostgreSQL design to the lowest common denominator shared with SQLite.
- Generate EF Core migrations with the CLI; never hand-write them. Maintain and test provider-
  appropriate migrations when schemas or SQL differ, and specify the intended DbContext and provider.
- Treat local/server synchronization as a separate feature requiring its own conflict and security
  design. The initial SQLite profile is a standalone store; use export/import for portability.
- Treat embeddings, relation extraction, impact analysis, and consistency checks as retryable,
  idempotent background work when they are asynchronous.
- Associate derived artifacts such as embeddings and consistency results with the exact revision
  that produced them. Do not serve stale derived data as if it represented the current revision.
- Use an outbox or an equivalently reliable transactional handoff before depending on post-commit
  background processing for correctness.

## Security and privacy

- Enforce authorization at application entry points and tenant scope again in persistence paths.
- Never log or return credentials, access tokens, API keys, raw authorization headers, or knowledge
  belonging to another workspace.
- Treat stored knowledge and prompts as potentially sensitive. Use structured, redacted logs and
  explicit response/export allowlists.
- Add cross-workspace isolation coverage whenever authentication, authorization, context building,
  graph traversal, search, imports, exports, or persistence scope changes.

## Implementation conventions

- Follow established naming, nullability, async, cancellation, and folder conventions once code
  exists. Extend existing patterns before introducing new ones.
- Keep changes small and scoped to the requested behavior. Avoid unrelated refactors and speculative
  abstractions.
- Put business rules in the domain or application layer, not controllers, MCP handlers, React
  components, prompts, or database adapters.
- Keep logging structured and preserve cancellation-token flow through database, filesystem,
  network, AI, and background operations.
- Use Markdown for initial knowledge content unless an accepted decision changes the storage format.
- Update relevant living documentation in the same change that alters architecture, behavior,
  public contracts, schema, operations, or security assumptions.

## Documentation process

Documentation under `docs/` is divided into living reference pages, accepted ADRs, backlog plans,
active plans, and archived plans. Follow [`docs/AGENTS.md`](docs/AGENTS.md) for lifecycle mechanics.

- Update `docs/README.md` when documentation is added, moved, renamed, or removed.
- Record a durable accepted decision in the next numbered ADR using `docs/adr/template.md`.
- Do not rewrite an accepted ADR to match a later decision; supersede it with a new ADR.
- Keep potential work in `docs/backlog/`, approved or active plans directly in `docs/`, and completed
  plans in `docs/archive/`.

## Testing and validation

Use `scripts/setup.sh` to restore/install dependencies and `scripts/verify.sh --all` for canonical
full verification. The verification workflow covers:

- backend restore, build, compiler analysis, and tests;
- frontend dependency installation, lint, type-check, tests, and production build;
- database migration validation;
- focused integration tests for PostgreSQL and SQLite behavior;
- HTTP and MCP contract tests;
- cross-workspace isolation tests;
- end-to-end tests for critical human and agent workflows.

Run focused checks while iterating and the full repository verification before delivery. Never
describe a skipped, unavailable, or sandbox-blocked check as passed.

## Scoped guidance

Add nested `AGENTS.md` files only when a real subtree needs rules more specific than this file. Good
candidates after the corresponding structure exists are persistence and migrations, MCP contracts,
the web server, the React client, and documentation lifecycle rules. Keep repository-wide invariants
here and avoid duplicating them in nested files.
