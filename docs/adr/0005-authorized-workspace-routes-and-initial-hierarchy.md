# ADR-0005: Authorized workspace routes and initial hierarchy provenance

- **Status:** Accepted
- **Date:** 2026-09-24
- **Deciders:** Project maintainers

## Context

Milestone 1 fixes every local request to the bootstrapped personal workspace and discovers Articles
through a browser-local ID index. Milestone 2 must discover persisted roots and children from a fresh
browser and allow the local owner to organize content across personal workspaces. A selected ID
cannot become trusted merely because it appears in a request. Different browsers and overlapping
requests must not share mutable selection state, and existing clients must continue to work.

Assigning a parent introduces structural knowledge outside immutable content revisions. Its initial
provenance and mutation boundary must be explicit before nested creation is implemented.

## Decision

Add a workspace HTTP surface and explicitly scoped Article routes under
`/api/workspaces/{workspaceId}/articles`. The host continues to supply the trusted actor. Application
authorization verifies an `Owner` membership for the requested workspace before constructing an
immutable request-scoped workspace context and entering a knowledge use case. Workspace discovery
and management use the trusted actor directly. Client IDs are selection requests, never identities
or proof of access; persistence retains explicit workspace predicates.

Selection lives in each request's route. There is no server-side selection mutation or shared active
workspace. Missing and unowned workspace selections return the same `404`; unavailable trusted actor
context returns the existing `403`. Hosted PostgreSQL HTTP continues to fail closed. Owner-only local
operations do not introduce sharing or the future hosted role permission matrix.

Existing `/api/articles` operations keep their fixed bootstrapped workspace and existing contracts.
New clients use the explicitly scoped routes, including for that default workspace. This extends
[ADR-0004](0004-explicit-revision-version-and-trusted-workspace-context.md)'s fixed-selection policy
only for new routes. Its trusted application context, immutable revisions, explicit revision-version
concurrency, and legacy route decisions remain in force; the original record is unchanged.

Server-backed workspace and root/direct-child lists use bounded keyset continuation ordered by
immutable creation time and canonical ID. Cursors never authorize access. Browser storage contains
optional navigation preferences, not the source of Article discovery. Lists are live reads across
pages, not multi-request snapshots. Exact representations and failure behavior live in the
[contract reference](../knowledge-contracts.md#milestone-2-workspaces-and-navigation-contract).

An Article's parent may be assigned only at creation, to an existing eligible Article in the same
workspace. The node, parent assignment, revision 1, and current pointer commit atomically. The
immutable initial `ParentId`, together with node `CreatedBy` and `CreatedAt`, records hierarchy
creation provenance. Content revisions do not version hierarchy, and content updates cannot change
the parent. Existing roots retain null parents and their original provenance. Future moves, status
changes, and relations require separate audit/version decisions before implementation.

## Consequences

- Old clients retain the same default-workspace behavior and response shapes; new clients carry
  explicit workspace identity with every Article request.
- The workspace/actor context remains trusted at the application boundary. Owner authorization must
  run per request, including continuation pages, and applies equally to non-HTTP callers.
- The local resolver must distinguish trusted actor identity from authorized workspace selection;
  it cannot mutate the existing fixed context globally.
- Scoped and legacy adapters share Article application behavior. Extra routes cost contract tests,
  but avoid changing the interpretation of existing routes or introducing hidden selection state.
- Stable key ordering avoids pagination shifts from title edits or workspace rename. It is creation
  order rather than alphabetical order; a refresh is needed for concurrent inserts before a cursor.
- Initial hierarchy provenance needs no synthetic content revision or general event subsystem.
  A later move feature cannot overwrite this creation-only record without a new history design.
- The web client must protect drafts and fence asynchronous responses by workspace and navigation
  generation; browser preferences are disposable and never confer access.
- This is an accepted implementation contract, not shipped functionality. Backend, browser, provider,
  and compatibility verification remain part of the subsequent delivery issues.

## Alternatives considered

- **Shared server selection or a selection endpoint:** rejected because interleaved browser requests
  could read or write the wrong workspace and legacy clients would inherit hidden mutable scope.
- **Workspace header/query overrides on existing Article routes:** rejected to preserve the existing
  fixed-scope contract and make resource identity and create Location values explicit.
- **Continue discovering Articles from browser IDs:** rejected because clearing storage hides saved
  content and a second browser cannot discover it.
- **Unbounded lists or offset pagination:** rejected because navigation must remain reachable in
  bounded requests and earlier insertions should not shift continuation positions.
- **Include hierarchy in every content revision:** rejected for creation-only hierarchy; it conflates
  content editing with structure and prematurely chooses later move semantics.
- **General hierarchy event log now:** deferred until mutable structural operations need it; immutable
  creation metadata already attributes the only structural operation allowed in this milestone.

## Related documentation

- [Milestone 2 plan](../milestone-2-navigation-workspaces-plan.md)
- [Knowledge application and HTTP contracts](../knowledge-contracts.md)
- [Local mode](../local-mode.md)
- [Web frontend](../frontend.md)
- [Testing and verification](../testing.md)
- [Issue #21](https://github.com/am-space/knowledge-management/issues/21)
