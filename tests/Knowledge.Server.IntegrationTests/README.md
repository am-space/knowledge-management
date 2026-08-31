# Knowledge.Server.IntegrationTests

ASP.NET Core health and provider connectivity smoke tests for SQLite and PostgreSQL. Run through
`scripts/verify.sh --integration`; the script requires Docker Compose or
`KNOWLEDGE_TEST_POSTGRES`.

Integration tests for PostgreSQL, SQLite, HTTP, MCP, authentication, background processing, and
cross-workspace isolation. Database behavior must be exercised against both supported providers when
the behavior is intended to be portable.
