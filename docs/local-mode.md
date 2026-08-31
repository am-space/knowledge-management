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
and a 30-second busy timeout. It remains a single-process profile.

For PostgreSQL development:

```bash
docker compose up --detach --wait postgres
Persistence__Provider=PostgreSql \
Persistence__PostgreSqlConnectionString='Host=localhost;Port=54329;Database=knowledge_test;Username=knowledge;Password=knowledge-dev-only' \
dotnet run --project src/Knowledge.Server --urls http://localhost:5080
```

The Compose credentials are development-only defaults and can be overridden through the variables
shown in `.env.example`.

## Intended experience

- Start one application process without requiring PostgreSQL or Docker.
- Store knowledge in one configurable SQLite database file.
- Automatically create or select a personal workspace.
- Expose the same web, HTTP, and MCP behavior available in server mode where supported.
- Allow explicit export and import for backup and portability.

## Local identity and workspace resolution

Startup idempotently provisions one configured local owner, an owner membership, and one personal
workspace. The trusted local host resolves requests to those identities before invoking application
behavior. Client-supplied route values, headers, query parameters, request bodies, database paths,
or IDs cannot override the active workspace.

Knowledge persistence remains explicitly filtered by the resolved workspace ID. This keeps the
local shortcut at the host boundary and preserves the same application and persistence contract
needed by the future authenticated server profile. See
[Knowledge application and HTTP contracts](knowledge-contracts.md) and
[ADR-0004](adr/0004-explicit-revision-version-and-trusted-workspace-context.md).

## Preserved semantics

Local mode retains users where authentication requires them, workspaces, memberships, stable
knowledge-node IDs, immutable revisions, structured relations, and workspace IDs. It must not use a
simplified local-only domain model.

## Search capabilities

SQLite FTS5 provides local keyword search. Recursive CTEs provide hierarchy traversal. Graph
relations use ordinary indexed relational tables.

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
