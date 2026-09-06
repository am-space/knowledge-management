# Knowledge.Server.IntegrationTests

Provider-backed Article lifecycle, immutable history, transaction rollback, concurrent edits,
workspace isolation, migrations, local bootstrap, and HTTP contract/privacy tests. Run through
`scripts/verify.sh --integration`; it requires Docker Compose or an isolated
`KNOWLEDGE_TEST_POSTGRES` database, which the provider tests recreate.

Portable persistence guarantees run against SQLite and PostgreSQL. HTTP tests use SQLite with
trusted test contexts for two persisted workspaces; the unauthenticated PostgreSQL host is tested
for fail-closed behavior. Hosted authentication, MCP, and background processing are not implemented
and have no executable coverage yet.
