# Knowledge.Server

The .NET 10 ASP.NET Core host exposes `/health/live`, `/health/ready`, Article create/read/update
HTTP operations, and development OpenAPI metadata. SQLite is the default persistence profile; select PostgreSQL with
`Persistence__Provider=PostgreSql` and provide `Persistence__PostgreSqlConnectionString`.

```bash
dotnet run --project src/Knowledge.Server --urls http://localhost:5080
```

`Knowledge/` implements Article domain behavior, application services, and HTTP presentation.
`Workspaces/` implements local identity and workspace bootstrap. Shared persistence and generated
provider migrations live under `Infrastructure/Persistence/`. Remaining module and infrastructure
README directories describe planned boundaries, not executable capabilities.

See [Article contracts](../../docs/knowledge-contracts.md) and
[architecture](../../docs/architecture.md) for current behavior and deferred scope.
