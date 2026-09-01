# Scripts

`setup.sh` restores .NET packages and installs the locked frontend dependencies. `verify.sh` is the
canonical local and CI verification entry point. Setup also restores the repository-local EF Core
CLI used to generate migrations.

```bash
scripts/setup.sh
scripts/verify.sh --all
```

Use `--backend`, `--frontend`, or `--integration` for a focused lane. PostgreSQL integration tests
use `KNOWLEDGE_TEST_POSTGRES` when supplied; otherwise they require Docker Compose and start the
repository's PostgreSQL service.

Generate migrations independently for each provider context:

```bash
dotnet tool run dotnet-ef migrations add <Name> --project src/Knowledge.Server \
  --startup-project src/Knowledge.Server --context SqliteKnowledgeDbContext \
  --output-dir Infrastructure/Persistence/Migrations/Sqlite
dotnet tool run dotnet-ef migrations add <Name> --project src/Knowledge.Server \
  --startup-project src/Knowledge.Server --context PostgreSqlKnowledgeDbContext \
  --output-dir Infrastructure/Persistence/Migrations/PostgreSql
```
