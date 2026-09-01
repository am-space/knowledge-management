# Persistence

This area contains the shared EF Core model, explicit SQLite/PostgreSQL provider selection, option
validation, persistence readiness check, and independent provider migration histories.

```text
Persistence/
├── Configurations/
└── Migrations/
    ├── PostgreSql/
    └── Sqlite/
```

`SqliteKnowledgeDbContext` and `PostgreSqlKnowledgeDbContext` share the model defined by
`KnowledgeDbContext` but own separate CLI-generated migrations. Run `scripts/setup.sh` before using
the repository-local `dotnet-ef` tool; exact generation commands are documented in
[`scripts/README.md`](../../../../scripts/README.md).

The initial schema stores users, workspaces, memberships, Article nodes, and immutable revisions.
Composite foreign keys enforce workspace ownership for parents and revisions. The current-revision
foreign key includes workspace and node identity, so a pointer cannot target another node's
revision. Because node and initial revision refer to each other, the current pointer is nullable at
the storage level: the context inserts the node and revision, then sets the pointer in a second
statement inside the same transaction. No committed node created through supported behavior lacks
a current revision.

Provider connectivity and empty-to-latest migration behavior are covered by integration tests. The
parent relationship remains the portable hierarchy truth; PostgreSQL `ltree`, if introduced, is a
derived optimization.
