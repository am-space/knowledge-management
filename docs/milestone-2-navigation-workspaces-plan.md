# Milestone 2 — Reliable local navigation and workspaces

- **Status:** Active
- **Parent outcome:** [Issue #20](https://github.com/am-space/knowledge-management/issues/20)
- **Roadmap:** [Knowledge Core MVP](backlog/knowledge-core-mvp-plan.md)

## Outcome and scope

A local user can find all saved Articles from a fresh browser, navigate nested Articles, and organize
knowledge across personal workspaces. Server data is the source of truth for Article discovery;
browser storage may retain optional UI preferences but is never required to recover saved content.

Deliver server-backed root and child listing, root/child Article creation, tree expansion/collapse,
and personal workspace creation, listing, renaming, and switching. Every workspace belongs to the
trusted local owner. Existing Articles and the bootstrapped personal workspace remain discoverable.

## Sequencing and contract decisions

Specify workspace selection and authorization before implementing workspace operations. A selected
workspace identifier is only a request to access a workspace: authorize it against the trusted owner
at the application boundary and scope persistence queries explicitly. Define request isolation,
default selection, stale/missing workspace behavior, and compatibility with existing Article routes.
Do not make one browser's selection mutable global state affecting another browser's requests.

Specify root/child listing, deterministic ordering and bounded retrieval, parent validation, and the
representation of parent identity before extending Article creation. A parent is assigned at creation;
reparenting is excluded. Define initial hierarchy provenance without allowing unaudited later moves.
Record durable accepted decisions in a new ADR if needed; do not rewrite accepted ADR-0004.

Implement workspace behavior and then scoped Article listing/creation through application services
and thin HTTP endpoints, followed by the web workflow and end-to-end verification. Preserve exact
Markdown, immutable revisions, concurrency checks, and existing Article identity throughout.
Generate provider migrations with the EF CLI if needed and test upgrades containing existing Articles.

## Exit criteria

- A fresh browser and a browser with cleared site data discover all saved Articles in the selected
  workspace, including Articles created before this milestone.
- Roots and children are reachable, creation under a parent works, and nesting survives reload.
- The trusted local owner can create, list, rename, and switch their personal workspaces.
- Workspace selection is authorized; unauthorized identifiers and cross-workspace parents cannot
  expose or change another workspace's data.
- Switching workspaces or Articles never silently discards unsaved edits; stale requests cannot
  populate the newly selected workspace with results from a previous selection.
- Loading, empty, error, authorization, not-found, and conflict states are explicit and accessible.
- Both providers pass application/persistence, hierarchy, workspace-isolation, and HTTP contract
  coverage under trusted test contexts. PostgreSQL hosted HTTP remains denied without authentication.
- Browser tests cover fresh/cleared storage, nested navigation, workspace switching, and draft safety.
- Canonical full verification passes; living documentation matches delivery and this plan is archived
  only after its parent outcome and required issues are satisfied and merged.

## Non-goals and risks

Defer hierarchy moves, archive/restore, revision history UI and restoration, relations, keyword
search, membership management, sharing, hosted authentication, MCP, and import/export. Deferred scope
remains in the roadmap, without implementation issues for this milestone.

Local mode remains single-process and trusts its local owner; additional personal workspaces do not
turn it into a hosted multi-user service. Browser preferences are disposable. Existing browser-local
Article indexes must no longer be the discovery source, and database upgrades must preserve Articles.
