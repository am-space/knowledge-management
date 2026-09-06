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

| Milestone | Outcome | Scope reference |
| --- | --- | --- |
| Milestone 0 | Executable application, verification, and persistence foundation | [Archived plan](../archive/milestone-0-foundation-plan.md) |
| Milestone 1 | First local vertical slice: personal workspace and revisioned article editing | [Archived plan](../archive/milestone-1-article-plan.md) |
| Milestone 2 | Reliable local navigation and personal workspaces | [Active plan](../milestone-2-navigation-workspaces-plan.md) |
| Milestone 3 | Complete human and agent surfaces: web workspace, HTTP, MCP, and portability | This roadmap |
| Milestone 4 | Hosted multi-user profile with authentication, membership, and deployment | This roadmap |

GitHub milestones and issues track delivery status. This backlog roadmap retains future scope;
Milestone 1 has its own archived implementation plan; Milestone 2 has an active delivery plan.
Milestones are delivery boundaries, not separate architectures. Each completed milestone must leave the repository in a verified and usable state.

## Milestone 0 — Executable foundation

Create the .NET solution, ASP.NET Core host, React client, test projects, canonical scripts, CI,
SQLite local configuration, PostgreSQL development configuration, and a minimal end-to-end health
path. Do not introduce knowledge-domain placeholders or empty database migrations.

Exit criteria are retained in the archived
[Milestone 0 foundation plan](../archive/milestone-0-foundation-plan.md).

## Milestone 1 — First vertical slice

Approved scope, sequencing, and exit evidence are in the
[Milestone 1 Article plan](../archive/milestone-1-article-plan.md), for
[parent issue #5](https://github.com/am-space/knowledge-management/issues/5).

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

## Milestone 2 — Reliable local navigation and workspaces

Approved scope and exit criteria are in the
[active Milestone 2 plan](../milestone-2-navigation-workspaces-plan.md).

- List saved Articles from the server, including roots and children, without browser-local IDs.
- Create Articles at the root or beneath an existing Article and expand/collapse the tree.
- Create, list, rename, and switch personal workspaces owned by the trusted local owner.
- Preserve unsaved drafts across navigation decisions and enforce workspace isolation.
- Verify fresh-browser discovery, cleared-storage recovery, and nested navigation after reload.
- Preserve equivalent application and persistence behavior on SQLite and PostgreSQL; hosted HTTP
  access remains denied until hosted authentication is implemented.

Membership management, sharing, hierarchy moves, archive/restore, revision history UI and restoration,
relations, and search are excluded.

## Deferred knowledge-core scope — schedule after Milestone 2

The following scope is retained in backlog without implementation issues or a new milestone
commitment. Shape its delivery boundary before starting it; Milestone 3 surfaces that depend on these
capabilities cannot be completed until the relevant core behavior is delivered.

- Hierarchy moves and ancestor listing; reject self-parenting, cycles, and cross-workspace parents.
- Archive and restore Articles; reject edits to archived nodes and define archived-tree behavior.
- List and view historical revisions with author, source, timestamp, and version metadata.
- Restore historical content by appending a new revision.
- Define audit/version semantics before shipping moves, status transitions, or relation changes.
- Create/remove and list incoming/outgoing `relates_to`, `depends_on`, and `supersedes` relations;
  reject invalid, duplicate, inappropriate self-, and cross-workspace relations.
- Display related knowledge in a contextual panel.
- Search titles and Markdown in the active workspace using SQLite FTS5 and PostgreSQL full-text
  search, with explicit archive filtering, exact revision identity, excerpts, stable metadata, and
  deterministic ordering for equal ranks.
- Verify these behaviors on both providers, expose usable HTTP and basic web workflows with exact
  contract tests, and cover loading, empty, error, authorization, and conflict states.

Membership management and owner/editor/viewer authorization remain in Milestone 4. Local workspace
ownership in Milestone 2 does not introduce sharing, invitations, or hosted authentication.

## Milestone 3 — Human and agent surfaces

These surfaces build on delivered core operations. Basic HTTP and web workflows ship with each
core feature; this milestone completes the richer experience, agent access, and portability.

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
- Audit/version semantics before deferred hierarchy moves, status transitions, and relation changes.
- Markdown editor dependency before the complete Milestone 3 editor experience.
- Import conflict and trust model before portability ships.
- Whether local semantic search is valuable enough to select a vector implementation after the MVP.

## Definition of MVP done

The MVP is complete when Milestones 0 through 4 and the deferred knowledge-core scope meet their exit
criteria, the same knowledge model is usable through local SQLite and hosted PostgreSQL profiles,
core human and MCP workflows are verified,
documentation reflects the resulting behavior, and deferred AI/vector/synchronization features are
not required for normal knowledge work.

