# Local mode

Local mode provides a self-contained personal installation using SQLite.

## Configuration and startup

SQLite is the default profile. Its default database is
`src/Knowledge.Server/data/knowledge.db`, which is excluded from version control. Start it with:

```bash
dotnet run --project src/Knowledge.Server --urls http://localhost:5080
```

Override settings with standard ASP.NET Core configuration, for example
`Persistence__SqliteConnectionString`. Local mode enables SQLite foreign keys, connection pooling,
and a 30-second busy timeout. `LocalWorkspace__OwnerDisplayName` and
`LocalWorkspace__WorkspaceName` customize the names created on first startup. They do not select or
change the trusted identities. Local mode remains a single-process profile.

For PostgreSQL development:

```bash
docker compose up --detach --wait postgres
Persistence__Provider=PostgreSql \
Persistence__PostgreSqlConnectionString='Host=localhost;Port=54329;Database=knowledge_test;Username=knowledge;Password=knowledge-dev-only' \
dotnet run --project src/Knowledge.Server --urls http://localhost:5080
```

The Compose credentials are development-only defaults and can be overridden through the variables
shown in `.env.example`.

## Available experience

- Start one application process without requiring PostgreSQL or Docker.
- Store knowledge in one configurable SQLite database file.
- Automatically create or select a personal workspace.
- Create, list, inspect, and rename owner-authorized personal workspaces through HTTP.
- Create, reopen, edit, preview, and save Articles through the web client and HTTP.
- Preserve immutable revisions and report stale saves as conflicts.

MCP, export/import, and hosted authentication are not yet available. Start the web client separately
with `npm run dev --prefix src/Knowledge.Web`; the server does not host its production assets yet.
The tree is a browser-local index of Article IDs, not a server collection. Clearing browser storage
removes navigation entries without deleting server content; see [frontend](frontend.md).

## Shipped local identity and workspace resolution

Startup idempotently provisions one configured local owner, an owner membership, and one personal
workspace in one transaction after applying SQLite migrations. The owner and workspace use stable
application-defined IDs, so restarting resolves the same records without creating duplicates. A
startup failure rolls back all provisioning and stops the host with an actionable log message.

The trusted local host exposes the resolved owner and original personal workspace through the legacy
Article application context. Client-supplied route values, headers, query parameters, request bodies,
database paths, or IDs cannot override that legacy scope. The PostgreSQL profile does not register
this initializer or local context. Until hosted authentication
provides a trusted identity and membership resolver, it registers a denied workspace context so
knowledge requests return the documented `403` response instead of selecting an untrusted tenant.

Knowledge persistence remains explicitly filtered by the resolved workspace ID. This keeps the
local shortcut at the host boundary and preserves the same application and persistence contract
needed by the future authenticated server profile. See
[Knowledge application and HTTP contracts](knowledge-contracts.md) and
[ADR-0004](adr/0004-explicit-revision-version-and-trusted-workspace-context.md).

## Milestone 2 workspace behavior

[ADR-0005](adr/0005-authorized-workspace-routes-and-initial-hierarchy.md) adds personal workspace
management and authorized selection while retaining the bootstrap identities and existing Article
routes. The [Milestone 2 contract](knowledge-contracts.md#milestone-2-workspaces-and-navigation-contract)
defines the exact routes, shapes, pagination, validation, and errors. Workspace operations now ship;
scoped Article routes and browser navigation remain pending.

The trusted local owner may create, list, rename, and select workspaces where they have an `Owner`
membership. Creation establishes that membership atomically. Workspace names may repeat; stable IDs
identify them. Rename does not alter identity or bootstrap provenance, and startup must not reset a
saved rename from configuration. Owner, editor, and viewer role management and sharing are deferred.

The host still supplies actor identity. `GET /api/workspaces/{workspaceId}` validates a selection;
the application authorization service can then return an immutable context for future scoped Article
routes. Every request makes its own selection; no global selected-workspace setting, cookie, or
client identity override is introduced. Missing and unowned explicit selections both return `404`
without falling back. An unavailable trusted actor still returns `403`. PostgreSQL development
hosting remains denied; trusted provider test contexts do not imply hosted authentication is
implemented.

Existing `/api/articles` clients always use the original personal workspace. Creating or selecting
another workspace never changes their default. The current browser client still uses the legacy
Article routes. Issue #24 will consume scoped routes from #23 and choose the authorized default ID
from the workspace list, then discover roots and nested content through server continuation pages.

Once browser navigation ships, optional browser preferences may remember workspace, selected Article,
and expansion state. They must be validated against server responses and are not needed to recover
saved content. Clearing storage or using another browser will not remove navigation entries from the
server. Existing browser ID indexes require no data import; new navigation stops using them as a
discovery source. See
[frontend selection rules](frontend.md#milestone-2-navigation-contract-not-yet-implemented) for stale
preferences, reload, failure, and draft handling.

Upgrade retains the original owner, workspace, memberships, Article IDs, null root parents, revision
history, and current pointers. It does not reconstruct data from browser storage. Any required
migration is generated and tested on both providers. The local profile remains single-process and
personal; more owned workspaces do not introduce independent authenticated users.

## Preserved semantics

Local mode retains users, workspaces, memberships, stable knowledge-node IDs, immutable revisions,
and workspace IDs. Structured relations remain planned. It must not use a
simplified local-only domain model.

## Search capabilities

Keyword search, hierarchy traversal, and graph relations are not implemented. The planned local
implementations are SQLite FTS5, recursive CTEs, and indexed relational tables respectively.

Local vector search is not part of the accepted initial profile. The application must expose search
capabilities explicitly so callers can omit semantic similarity when it is unavailable. A local
vector extension or in-process index requires a later decision and representative benchmarks.

## Operational limits

- Local mode is intended for one running application instance.
- It does not promise PostgreSQL-equivalent concurrent-write behavior or multi-user isolation.
- The database file and any backups contain user knowledge and must be protected accordingly.
- Background work must remain retryable and idempotent even when executed in the same process.

## Not synchronization

A local database is not an offline replica of a hosted workspace. Bidirectional synchronization
would require change tracking, identity and authorization rules, deletion semantics, conflict
resolution, and encrypted transport. That work is outside the initial local profile.

See [ADR-0002](adr/0002-postgresql-server-and-sqlite-local-profiles.md).
