# Knowledge application and HTTP contracts

This page defines the current public behavior for knowledge operations. Milestone 1 supports the
local Article create, read, and update vertical slice. Later operations should extend these
contracts additively.

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

Titles and Markdown are required strings. A title containing only whitespace is invalid. Concrete
size limits may be added before implementation and must then be documented and tested. Unknown or
unsupported node types are not treated as Articles.

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

See [ADR-0004](adr/0004-explicit-revision-version-and-trusted-workspace-context.md) for the durable
decision behind these contracts.
