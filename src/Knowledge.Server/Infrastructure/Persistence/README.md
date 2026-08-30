# Persistence

This area will contain the EF Core context, shared relational mappings, provider selection, and
provider-specific schema and query behavior.

```text
Persistence/
├── PostgreSql/
└── Sqlite/
```

Maintain provider-appropriate migrations and integration tests. The parent relationship is the
portable hierarchy truth; PostgreSQL `ltree`, if introduced, is a derived optimization.

