# Persistence

This area contains the EF Core context, explicit SQLite/PostgreSQL provider selection, option
validation, and persistence readiness check. Provider-specific schema and query behavior will be
added with the first domain schema in Milestone 1.

```text
Persistence/
├── PostgreSql/
└── Sqlite/
```

No migrations exist yet: the first provider-appropriate migrations must accompany the first real
schema. Provider connectivity is covered by integration tests. The parent relationship remains the
portable hierarchy truth; PostgreSQL `ltree`, if introduced, is a derived optimization.
