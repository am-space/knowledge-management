# Scripts

`setup.sh` restores .NET packages, installs the locked frontend and browser-test dependencies, and
downloads Playwright's Chromium headless shell. `verify.sh` is the
canonical local and CI verification entry point. Setup also restores the repository-local EF Core
CLI used to generate migrations.

```bash
scripts/setup.sh
scripts/verify.sh --all
```

Use `--backend`, `--frontend`, `--integration`, or `--e2e` for a focused lane. PostgreSQL integration tests
use `KNOWLEDGE_TEST_POSTGRES` when supplied; otherwise they require Docker Compose and start the
repository's PostgreSQL service. The browser lane uses dedicated loopback ports 5081/5174
and a temporary SQLite file, and cleans up after itself. See [testing](../docs/testing.md) for
browser system dependencies, failure artifacts, and coverage.

Generate migrations independently for each provider context:

```bash
dotnet tool run dotnet-ef migrations add <Name> --project src/Knowledge.Server \
  --startup-project src/Knowledge.Server --context SqliteKnowledgeDbContext \
  --output-dir Infrastructure/Persistence/Migrations/Sqlite
dotnet tool run dotnet-ef migrations add <Name> --project src/Knowledge.Server \
  --startup-project src/Knowledge.Server --context PostgreSqlKnowledgeDbContext \
  --output-dir Infrastructure/Persistence/Migrations/PostgreSql
```
