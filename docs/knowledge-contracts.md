# Knowledge application and HTTP contracts

This page defines shipped Milestone 1 behavior and the agreed Milestone 2 contracts. The sections
through Compatibility guidance describe the implemented local Article slice. The final
[Milestone 2 section](#milestone-2-workspaces-and-navigation-contract) specifies implementation
requirements; its routes and behavior are not available yet.

## Trusted workspace context

Every knowledge operation receives an application-owned `WorkspaceContext` containing the active
workspace and actor identities. Presentation adapters resolve this context before invoking a
knowledge use case. Article routes and request bodies do not accept a workspace ID.

In local mode, startup idempotently provisions one configured local owner, that owner's membership,
and one personal workspace. The local host resolves every request to that owner and workspace. A
database ID, path, header, query parameter, route value, or request-body value supplied by a client
must not select or override them. Hosted mode will replace this resolver with authenticated
principal and membership resolution without changing knowledge use cases or Article contracts.

Persistence queries and mutations still include the trusted workspace ID. When an Article ID does
not exist in that workspace, the operation returns `NotFound`; it does not reveal whether the same
ID exists elsewhere. Milestone 1 therefore exposes no distinct cross-workspace access response.

## Representations

Public JSON uses camel case and the following representations:

| Value | JSON representation |
| --- | --- |
| Node, revision, workspace, and actor IDs | UUID string in canonical lowercase hyphenated form |
| Timestamp | UTC RFC 3339 string with a `Z` suffix and sufficient precision to round-trip |
| Revision version | Positive JSON integer, starting at `1` and increasing by exactly one per node |
| Article type | The lowercase string `article` |
| Markdown | UTF-8 JSON string whose line endings and content round-trip without transformation |

An Article response represents stable node identity and its exact current immutable revision:

```json
{
  "id": "8a73e7fc-58e8-463b-9f3d-d2d641380adb",
  "type": "article",
  "createdAt": "2026-08-31T17:00:00Z",
  "createdBy": "50c68ff7-a599-4bf8-849b-775c84919f9a",
  "currentRevision": {
    "id": "c28bcfb5-0b81-4f69-9f88-206af7851184",
    "version": 1,
    "title": "First article",
    "contentMarkdown": "# First article\n",
    "createdAt": "2026-08-31T17:00:00Z",
    "createdBy": "50c68ff7-a599-4bf8-849b-775c84919f9a"
  }
}
```

`currentRevision.version` is the concurrency value. Clients retain it from a create or read result
and send it as `expectedRevisionVersion` when updating. Revision IDs identify exact content for
history and derived artifacts but are not the update token.

## Application operations

The Article application boundary has these inputs and results:

| Operation | Input | Success | Expected failures |
| --- | --- | --- | --- |
| Create | Trusted context, `title`, `contentMarkdown` | `Created` with the Article at revision 1 | `ValidationFailed` |
| Get | Trusted context, node ID | `Found` with the exact current Article revision | `NotFound` |
| Update | Trusted context, node ID, `expectedRevisionVersion`, `title`, `contentMarkdown` | `Updated` with the new exact current revision | `ValidationFailed`, `NotFound`, `RevisionConflict` |

These operations are implemented by the transport-independent `ArticleService`. Its result carries
one of the statuses above, an Article on success, field errors for validation failure, or the
current version for a revision conflict. HTTP and future MCP adapters map that result without
reimplementing workspace, revision, or concurrency behavior.

Titles and Markdown are required, non-null strings. Markdown may be empty. A title containing only whitespace is invalid, and a
title cannot exceed 500 characters after surrounding whitespace is removed. Unknown or unsupported
node types are not treated as Articles.

Create atomically inserts the node, revision 1, and current-revision pointer. Update performs the
following work in one database transaction:

1. Load the Article within the trusted workspace.
2. Compare its current revision version with `expectedRevisionVersion`.
3. Insert one immutable revision at the next version.
4. Move the node's current-revision pointer to that revision.

The version comparison must be enforced by a conditional database write or equivalent concurrency
check, not only by an earlier in-memory comparison. A stale version returns `RevisionConflict` and
creates no revision. Validation, not-found, cancellation, or persistence failure also leaves no
partial revision or pointer change. A successful update creates a revision even if its content is
identical; the operation records an accepted edit, while the UI may avoid submitting unchanged
content.

## HTTP routes

Milestone 1 exposes:

| Method and route | Request | Success |
| --- | --- | --- |
| `POST /api/articles` | `{ "title": string, "contentMarkdown": string }` | `201 Created`, Article body, and `Location: /api/articles/{id}` |
| `GET /api/articles/{id}` | None | `200 OK` with Article body |
| `PUT /api/articles/{id}` | `{ "expectedRevisionVersion": integer, "title": string, "contentMarkdown": string }` | `200 OK` with the updated Article body |

Create and update return the same Article representation as get. The API does not use `ETag` or
`If-Match` in Milestone 1; the explicit version is transport-independent and is also available to
future MCP adapters.

Failures use `application/problem+json` and RFC 9457 Problem Details. Each response includes
`type`, `title`, `status`, and `traceId`. Validation responses additionally include an `errors`
object keyed by camel case request field names. Problem `type` values are stable URNs:

| Application result or HTTP failure | Status | Problem `type` |
| --- | --- | --- |
| Malformed JSON, invalid route ID, or validation failure | `400` | `urn:knowledge:problem:validation` |
| Article absent from the trusted workspace | `404` | `urn:knowledge:problem:article-not-found` |
| Stale `expectedRevisionVersion` | `409` | `urn:knowledge:problem:revision-conflict` |
| Active workspace cannot be resolved from trusted context | `403` | `urn:knowledge:problem:workspace-access-denied` |

A revision-conflict response includes `currentRevisionVersion` so a client can explain the conflict
and offer a reload. It does not echo stored title or Markdown. A workspace access failure concerns
the trusted principal or host context itself; it is distinct from looking up a node outside that
context, which remains `404` to avoid cross-workspace disclosure.

Unexpected failures use a generic `500` Problem Details response without knowledge content,
credentials, database details, or exception text.

## Diagnostic allowlist

Article error logs contain a fixed event message, request trace ID, and exception type name.
They omit exception objects, messages, stacks, request bodies, titles, and Markdown. EF Core
sensitive-data logging is disabled, and its save-failure and query-iteration-failure events are
suppressed because they include raw provider exceptions even when parameter logging is disabled.
SQL command diagnostics retain parameter placeholders; the Article middleware supplies the safe
correlated error event. The generic `500` body contains only `type`, `title`, `status`, and `traceId`.
Other problem bodies add only the documented validation fields or current revision version.

These guarantees cover the shipped Article request path. Hosted authentication, future request/body
logging, and additional diagnostics require their own privacy review before being enabled.

## Compatibility guidance

Article routes, methods, field names, representations, status codes, Problem Details types, and the
meaning of `expectedRevisionVersion` are public contracts. Clients must ignore unknown response and
Problem Details properties so the API can add metadata compatibly. Servers may add optional
properties, new routes, and new problem types without changing the Milestone 1 operations. Renaming
or removing fields, changing their types or meanings, or reusing an existing problem type for a
different condition requires explicit migration and compatibility guidance.

See [ADR-0004](adr/0004-explicit-revision-version-and-trusted-workspace-context.md) for the durable
decision behind these contracts.

## Milestone 2 workspaces and navigation contract

This contract resolves [issue #21](https://github.com/am-space/knowledge-management/issues/21).
[ADR-0005](adr/0005-authorized-workspace-routes-and-initial-hierarchy.md) extends the fixed local
workspace decision to authorized selection on new routes. Existing routes keep their default scope.
The workspace, listing, and frontend deliveries implement this contract separately.

### Trusted owner and request selection

The local host supplies the actor identity; HTTP never supplies an actor, owner, database path, or
connection string. Workspace operations use that trusted actor without requiring an active workspace.
An existing `Owner` membership authorizes access to a workspace. `CreatedBy` is provenance, not an
authorization substitute. Viewer/editor memberships confer no access through this personal-workspace
surface; sharing and role management remain deferred.

New Article routes contain `/api/workspaces/{workspaceId}/articles`. The route ID is an untrusted
selection request. Application authorization must verify the trusted actor's owner membership before
constructing the request's immutable `WorkspaceContext` or entering an Article use case. Every
knowledge query, parent lookup, and write then includes that authorized workspace ID. A non-HTTP
caller must pass through the same authorization boundary; transport parsing alone grants no access.

Selection is represented by each request's route, not by a server-side selected-workspace record,
mutable singleton, session, cookie, header, or query override. Two interleaved requests from the same
owner may select different owned workspaces safely. Browser preferences never become trusted context.
There is no `select` mutation endpoint. `GET /api/workspaces/{workspaceId}` validates a candidate
selection; every subsequent request independently authorizes it again.

The bootstrapped personal workspace retains its stable ID and is always the default for legacy
Article routes. Renaming it, creating another workspace, or changing a browser selection does not
change that default. A failed explicit selection never silently falls back to it. Without a trusted
actor the new surface returns `403` with `urn:knowledge:problem:workspace-access-denied`; PostgreSQL
hosted HTTP remains denied until authentication exists. Both providers can exercise these contracts
with trusted test-host identities.

### Workspace operations

Transport-independent workspace application behavior owns create/list/get/rename and ownership
authorization. HTTP maps its results to the responses below. Article listing and parent validation
likewise belong in shared Article application behavior, including pagination bounds for non-HTTP
callers; endpoints do not duplicate these rules.

A workspace representation has exactly these defined fields (the normal additive response rule
still applies):

```json
{
  "id": "616fa41a-20c4-4677-9af0-77f6d2e685ca",
  "name": "Personal",
  "createdAt": "2026-09-24T12:00:00Z",
  "createdBy": "50c68ff7-a599-4bf8-849b-775c84919f9a"
}
```

| Method and route | Input | Success |
| --- | --- | --- |
| `GET /api/workspaces` | Optional `pageSize`, `cursor` | `200` with `{ "items": Workspace[], "nextCursor": string or null, "defaultWorkspaceId": UUID }` |
| `POST /api/workspaces` | `{ "name": string }` | `201`, Workspace body, `Location: /api/workspaces/{id}` |
| `GET /api/workspaces/{workspaceId}` | None | `200`, Workspace body |
| `PUT /api/workspaces/{workspaceId}` | `{ "name": string }` | `200`, renamed Workspace body |

List returns only owned workspaces. `defaultWorkspaceId` identifies the authorized bootstrapped
workspace on every page, even when its item is on another page. If that default can no longer be
authorized, list returns the workspace-access-denied `403` instead of exposing an unauthorized ID
or choosing another default. Explicit owned-workspace operations do not depend on default selection.

`name` is a required non-null string, trimmed using the existing domain whitespace rules, with
1–200 UTF-16 code units after trimming. Duplicate names are allowed; IDs distinguish workspaces.
Creation generates a new ID and atomically inserts the workspace and the trusted actor's `Owner`
membership. A failure or cancellation observed before transaction commit leaves neither record;
after commit, both records are durable even if the request is canceled or its response is lost.
No input can assign a different owner.
Rename preserves ID, `CreatedAt`, `CreatedBy`, and memberships. An identical normalized name is a
successful no-op. Concurrent renames use the last committed name; workspace names are mutable labels
and do not create Article revisions or a workspace-name audit history in Milestone 2. Workspace
deletion, ownership transfer, and membership mutations are unavailable.

### Article routes and representations

| Method and route | Input | Success |
| --- | --- | --- |
| `GET /api/workspaces/{workspaceId}/articles` | Optional `parentId`, `pageSize`, `cursor` | `200`, Article summary page |
| `POST /api/workspaces/{workspaceId}/articles` | `{ "title": string, "contentMarkdown": string, "parentId"?: UUID or null }` | `201`, scoped Article body, `Location: /api/workspaces/{workspaceId}/articles/{id}` |
| `GET /api/workspaces/{workspaceId}/articles/{id}` | None | `200`, scoped Article body |
| `PUT /api/workspaces/{workspaceId}/articles/{id}` | Existing update body with `expectedRevisionVersion`, `title`, `contentMarkdown` | `200`, scoped Article body |

The scoped Article body contains every existing Article field plus `workspaceId` and `parentId`
(UUID or JSON null). The workspace ID is server-confirmed context, not an authorization token.
Create, get, and update use this same representation. Revision and exact Markdown semantics remain
unchanged. Scoped update rejects any supplied `parentId`, even null or the current value, as a
validation error; changing hierarchy is not a content update.
Scoped get/update operate on active Articles with a committed current revision; otherwise they return
Article-not-found. This does not change legacy behavior or introduce a status transition operation.

Listing without `parentId` returns roots only. A canonical UUID selects that Article's direct
children, not all descendants. Empty, repeated, malformed, and literal `null` query values are
validation errors; root selection is expressed by omission. Parent existence and suitability are
checked in the authorized workspace before returning children, so an absent parent gives `404`,
while an existing parent with no children gives a successful empty page. No ancestor endpoint,
recursive subtree response, move, or archive operation is introduced.

An Article summary page has this shape:

```json
{
  "items": [
    {
      "id": "8a73e7fc-58e8-463b-9f3d-d2d641380adb",
      "workspaceId": "616fa41a-20c4-4677-9af0-77f6d2e685ca",
      "parentId": null,
      "type": "article",
      "title": "First article",
      "createdAt": "2026-09-24T12:00:00Z",
      "currentRevisionId": "c28bcfb5-0b81-4f69-9f88-206af7851184",
      "currentRevisionVersion": 1,
      "hasChildren": false
    }
  ],
  "nextCursor": null
}
```

Summaries omit Markdown and author details. `title`, `currentRevisionId`, and
`currentRevisionVersion` describe one committed current revision of that node, never a mixture of
revisions. `hasChildren` considers only eligible direct children in the same workspace. The listed
nodes and selectable parents are active Articles with a committed current revision. Milestone 1
Articles meet these conditions and are roots; unsupported types or inactive parents are treated as
absent by these new operations. No archived-content filter is exposed in this milestone.

### Ordering and bounded retrieval

Workspace and Article lists use the same pagination rules:

- Order by immutable `createdAt` ascending, then ID ascending. Equal timestamps compare UUIDs by
  their canonical lowercase hyphenated strings in ordinal order, identically on both providers.
  Workspace rename and Article title/revision edits do not move items between pages.
- `pageSize` is an integer from 1 through 100, default 50. Invalid or repeated values return `400`;
  the server does not silently clamp them. Each response has at most that many items. There is no
  total count. An empty list returns `items: []` and `nextCursor: null`.
- A non-null `nextCursor` means more items existed when the page was read. Pass it unchanged with
  the same workspace, parent selection, and page size. The continuation starts strictly after the
  last returned `(createdAt, id)`; it does not use offsets. The final page returns null.
- Cursors are opaque, versioned, URL-safe strings of at most 2048 characters. Their encoded state
  binds the operation, trusted actor, workspace/parent scope where applicable, page size, and last
  ordering key. They contain no titles, names, or Markdown. Encoding is not encryption and a cursor
  grants no authorization. Revalidate actor ownership and parent suitability on every page and
  always derive query scope from the authorized request, never from cursor state alone.
- Empty, repeated, oversized, malformed, unsupported-version, or scope/page-size-mismatched cursors
  return `400` with an `errors.cursor` entry after scope authorization. Tokens have no time expiry
  in this version; they remain valid across process restarts. Clients restart a list if rejected.
- Pages are live reads, not a snapshot across requests. With no mutations, following continuations
  visits every eligible item exactly once. A concurrent insertion before the last key requires a
  refresh to appear; one after it may appear on a later page. Current titles and child indicators
  can change. Refresh starts without a cursor, and the UI deduplicates merged results by node ID.

### Creation-time parent validation and provenance

Omitted or null `parentId` creates a root. A non-null value must be a canonical UUID identifying an
eligible, already committed Article in the authorized workspace. Missing, unsupported, inactive, or
cross-workspace parents produce the same Article-not-found response without echoing parent data.
Invalid UUID syntax is a `400` field error. The application checks suitability inside the creation
transaction; database constraints reinforce same-workspace parenting and prohibit self-parenting.

The server generates the new node ID. The domain also rejects `parentId == id`, including non-HTTP
construction, as a validation failure. Creation cannot introduce a hierarchy cycle: its only edge
points from a new node to an existing node, and no operation changes an existing parent. Integrity
tests must still cover self-parenting and the cross-workspace parent constraint on both providers.

Parent assignment, the node, revision 1, and the current-revision pointer commit atomically.
`ParentId` is immutable creation metadata in Milestone 2, attributed to the node's existing
`CreatedBy` and `CreatedAt`. Revision 1 records the content accepted in that same transaction.
This is the initial hierarchy provenance record; no synthetic content revision or separate move
event is created. All later content updates retain that parent. Historical content does not imply
a versioned hierarchy. Existing roots keep null parents and their original provenance; upgrade
does not invent events or rewrite revisions. Moves, status transitions, and relations must define
their own audit/version semantics before any such mutation ships.

### New-route errors and response allowlist

All new routes use the existing Problem Details envelope and privacy rules. This table adds or
clarifies failures for the new surface; existing routes retain their current behavior.

| Condition | Status and problem type | Validation field, if applicable |
| --- | --- | --- |
| No trusted actor, including current hosted PostgreSQL HTTP | `403`, `urn:knowledge:problem:workspace-access-denied` | None |
| Well-formed workspace ID is missing or is not owned by actor | `404`, `urn:knowledge:problem:workspace-not-found` | None |
| Malformed route or parent UUID | `400`, `urn:knowledge:problem:validation` | `workspaceId`, `id`, or `parentId` |
| Invalid name/content/version/list query or supplied scoped-update parent | `400`, `urn:knowledge:problem:validation` | `name`, `title`, `contentMarkdown`, `expectedRevisionVersion`, `parentId`, `pageSize`, or `cursor` |
| Article or selected parent absent/ineligible in authorized workspace | `404`, `urn:knowledge:problem:article-not-found` | None |
| Self-parenting attempted at application boundary | `400`, `urn:knowledge:problem:validation` | `parentId` |
| Stale content update | `409`, `urn:knowledge:problem:revision-conflict` | Existing `currentRevisionVersion` extension |

The workspace-not-found problem title is `Workspace not found.` It has no workspace name,
owner, membership, or selection details. Existing problem titles remain unchanged. Structurally
malformed requests can fail validation without a database lookup. For well-formed requests, check
trusted identity, authorize the requested workspace, then resolve the parent/Article or continuation.
Thus a missing workspace and another owner's workspace have indistinguishable responses, even when
an Article or cursor from elsewhere is also supplied. Never return a revision-conflict version from
outside the authorized workspace.

For workspace list/create, a missing trusted actor uses the same workspace-access-denied status and
problem type, with the neutral title `A trusted actor is required.` These operations do not require
an active workspace, so they must not use the legacy active-workspace title. Existing legacy Article
routes retain their current title and response contract.

Responses expose only the documented workspace, scoped Article, summary, page, and error fields.
Validation messages describe the rule without echoing names, content, or cursors. Extend the shipped
diagnostic allowlist to the new paths: no request bodies, workspace names, Markdown, raw provider
exceptions, or cursor values in application error logs. Cancellation/failure must leave no partial
workspace/membership or node/revision writes.

### Compatibility and delivery boundary

`POST`, `GET`, and `PUT /api/articles[/{id}]` retain their request shapes, response shapes, status
codes, Location values, and fixed default-workspace behavior. Legacy creates remain roots; these
routes do not gain parent or workspace selection. Their existing unknown-body-field handling remains
unchanged: such fields cannot select an owner/workspace or change a parent. A legacy read or content
update of a nested Article in the default workspace still works by ID and preserves its parent.
Existing clients need no migration. Scoped and legacy adapters delegate to the same Article behavior.

Milestone 2 clients use scoped routes consistently, including for the default workspace. They must
retain the full scoped Location or the `(workspaceId, id)` pair; a bare Article ID does not encode
selection. The server-backed list discovers existing Articles independently of browser storage.
The bootstrapped owner, workspace, memberships, node IDs, revisions, and current pointers survive
upgrade unchanged. Any implementation-driven schema change requires CLI-generated migrations and
upgrade tests for both providers. This contract change adds no runtime code or migrations.
