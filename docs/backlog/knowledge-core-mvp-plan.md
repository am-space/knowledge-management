# Knowledge Core MVP Plan

- **Status:** Backlog
- **Date:** 2026-08-30
- **Owner:** Project maintainers

## Intended outcome

Deliver a usable knowledge management product that runs locally with SQLite, can be deployed for
multiple users with PostgreSQL, and exposes the same knowledge behavior to humans through the web
client and to agents through MCP.

The MVP establishes the knowledge core. Semantic vector retrieval, AI consistency analysis, and
automatic knowledge maintenance remain later capabilities built on top of this foundation.

## Product principles

- Deliver the smallest complete vertical slice before broadening the model.
- Preserve workspace ownership and stable identifiers in both local and server profiles.
- Create immutable revisions for accepted content changes.
- Keep HTTP and MCP adapters thin and backed by the same application services.
- Treat external contracts and stored data as durable from their first release.
- Expose provider capabilities explicitly instead of silently degrading behavior.
- Prefer archive and restore over destructive deletion in the initial product.

## Milestone roadmap

| Milestone | Outcome | Status |
| --- | --- | --- |
| [Milestone 0](../milestone-0-foundation-plan.md) | Executable application, verification, and persistence foundation | Active |
| Milestone 1 | First local vertical slice: personal workspace and revisioned article editing | Backlog |
| Milestone 2 | Useful knowledge core: hierarchy, relations, revisions, and keyword search | Backlog |
| Milestone 3 | Complete human and agent surfaces: web workspace, HTTP, MCP, and portability | Backlog |
| Milestone 4 | Hosted multi-user profile with authentication, membership, and deployment | Backlog |

Milestones are delivery boundaries, not separate architectures. Each completed milestone must leave
the repository in a verified and usable state.

## Milestone 0 — Executable foundation

Create the .NET solution, ASP.NET Core host, React client, test projects, canonical scripts, CI,
SQLite local configuration, PostgreSQL development configuration, and a minimal end-to-end health
path. Do not introduce knowledge-domain placeholders or empty database migrations.

Exit criteria are defined in the active
[Milestone 0 foundation plan](../milestone-0-foundation-plan.md).

## Milestone 1 — First vertical slice

### Outcome

A user can start the local application, enter an automatically created personal workspace, create an
article, edit its Markdown, save an immutable revision, and reopen it from the knowledge tree.

### Features

- Automatically create a local owner identity and personal workspace without requiring login.
- Introduce the first `Article` knowledge-node type.
- Create, read, and update an article through one application service.
- Store initial and subsequent content as immutable revisions.
- Keep the node's current-revision pointer transactionally consistent.
- Expose the workflow through HTTP.
- Render a minimal MUI application shell, knowledge tree, Markdown source editor, and preview.
- Detect concurrent edits through an explicit version or concurrency token.

### Exit criteria

- The complete workflow passes against SQLite through a browser-level or equivalent vertical test.
- Equivalent persistence semantics pass against PostgreSQL integration tests.
- A second workspace cannot read or modify the first workspace's node.
- Failed or conflicting updates do not create partial revisions.

## Milestone 2 — Useful knowledge core

### Workspaces

- Create, list, select, and rename workspaces.
- Model owner, editor, and viewer memberships.
- Enforce workspace scope in hierarchy, relation, revision, and search queries.
- Defer invitations and organizations.

### Knowledge hierarchy

- Create articles beneath a parent.
- List ancestors and children.
- Move articles within a workspace.
- Archive and restore articles.
- Reject self-parenting, hierarchy cycles, cross-workspace parents, and edits to archived nodes.

### Revision history

- List and view historical revisions.
- Display author, source, timestamp, and version metadata.
- Restore old content by creating a new current revision rather than mutating history.
- Define and implement audit behavior for moves, status transitions, and relation changes before
  those operations ship.

### Relations

Start with three relation types:

```text
relates_to
depends_on
supersedes
```

- Create and remove relations.
- List incoming and outgoing relations.
- Reject invalid, duplicate, inappropriate self-, and cross-workspace relations.
- Display related knowledge in the contextual side panel.

### Keyword search

- Search titles and Markdown content within the active workspace.
- Use SQLite FTS5 locally and PostgreSQL full-text search on the server.
- Filter archived content explicitly.
- Return the matching node, revision identity, excerpt, and stable result metadata.
- Keep result ordering deterministic for equal ranks.

### Exit criteria

- Hierarchy, revision, relation, and search behavior is covered on both database providers.
- The web client exposes complete loading, empty, error, authorization, and conflict states.
- Public HTTP contracts have exact contract tests.

## Milestone 3 — Human and agent surfaces

### Web workspace

- Workspace selector and searchable knowledge tree.
- Markdown editor and preview.
- Context panel for relations, dependencies, status, and revision information.
- Revision history and basic content comparison.
- Search dialog with keyboard navigation.
- Responsive layout and accessible light and dark themes.

Select the Markdown editor only after testing exact Markdown round trips and large-document behavior.
An interactive graph view is not required for this milestone.

### HTTP API

- Workspace operations.
- Node creation, reading, editing, moving, archiving, and restoring.
- Revision history and restoration.
- Relation management.
- Keyword search and context retrieval.
- Stable validation, concurrency, authorization, and error semantics.

### MCP surface

Initial tools:

```text
create_knowledge
get_knowledge
update_knowledge
search_knowledge
get_context
link_knowledge
get_related_knowledge
get_history
```

- Delegate every tool to the same application services used by HTTP.
- Treat tool names, descriptions, argument schemas, and result shapes as external contracts.
- Derive workspace scope from authenticated host context, never from an untrusted model-supplied
  database path or tenant identity.

### Portability

- Export a workspace to a documented, versioned portable format.
- Import into an empty or selected workspace with explicit conflict behavior.
- Preserve stable IDs and revision history where safe and supported.
- Do not imply that export/import provides bidirectional synchronization.

### Exit criteria

- Representative human workflows pass through the web application.
- Equivalent agent workflows pass through MCP.
- HTTP and MCP operations produce the same domain results and authorization decisions.
- Export/import round trips representative workspace data without losing revision or relation
  integrity.

## Milestone 4 — Hosted multi-user profile

- Select and implement the authentication approach.
- Add account lifecycle and authenticated principals.
- Manage memberships and owner, editor, and viewer authorization.
- Run the production profile with PostgreSQL and Docker.
- Prove cross-user and cross-workspace isolation through integration and end-to-end tests.
- Add operational health checks, migration execution, backup guidance, and deployment documentation.
- Audit security-sensitive membership, export, and administrative operations.

PostgreSQL row-level security remains a separate decision. Application authorization and
workspace-scoped persistence queries are required regardless of whether RLS is later adopted.

## Cross-cutting acceptance requirements

- IDs and timestamps have one documented representation across persistence, HTTP, MCP, export, and
  the frontend.
- Cancellation and async behavior flow through database, filesystem, network, and AI boundaries.
- Sensitive data and stored knowledge are excluded or redacted from diagnostic logs.
- Database constraints reinforce workspace and graph invariants.
- Migrations are generated and tested separately for PostgreSQL and SQLite where provider behavior
  differs.
- Every external contract change is additive or explicitly documented as breaking.
- Living documentation is updated with the behavior it describes.

## Explicitly deferred from the MVP

- Embeddings and vector similarity.
- Local vector storage.
- AI semantic consistency analysis.
- Automatic AI-generated updates.
- Advanced impact analysis beyond deterministic graph traversal.
- Interactive graph visualization.
- WYSIWYG editing and Markdown/rich-text round-trip conversion.
- Organizations, invitations, billing, and enterprise policy.
- Real-time collaborative editing.
- PostgreSQL row-level security.
- Local/server synchronization and offline collaboration.
- Large relation taxonomies and user-defined relation types.

## Decisions required before affected milestones

- Authentication provider and account lifecycle before Milestone 4.
- Audit/version semantics for hierarchy, status, and relation changes before Milestone 2.
- Markdown editor dependency before the complete Milestone 3 editor experience.
- Import conflict and trust model before portability ships.
- Whether local semantic search is valuable enough to select a vector implementation after the MVP.

## Definition of MVP done

The MVP is complete when Milestones 0 through 4 meet their exit criteria, the same knowledge model is
usable through local SQLite and hosted PostgreSQL profiles, core human and MCP workflows are verified,
documentation reflects the resulting behavior, and deferred AI/vector/synchronization features are
not required for normal knowledge work.

