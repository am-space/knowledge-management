# Search module

Owns retrieval orchestration, context assembly, ranking, and explicit search capabilities.

Provider-specific search belongs in Infrastructure: PostgreSQL full-text search and `pgvector` for
the server profile, SQLite FTS5 for local keyword search, and a future deliberate local-vector
implementation if one is selected.

