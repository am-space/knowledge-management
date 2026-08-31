# Knowledge.Server

The .NET 10 ASP.NET Core host currently exposes `/health/live`, `/health/ready`, and development
OpenAPI metadata. SQLite is the default persistence profile; select PostgreSQL with
`Persistence__Provider=PostgreSql` and provide `Persistence__PostgreSqlConnectionString`.

```bash
dotnet run --project src/Knowledge.Server --urls http://localhost:5080
```

The host is the entry point for the planned feature-oriented modular monolith.

```text
Knowledge.Server/
├── Modules/
│   ├── Knowledge/
│   ├── Workspaces/
│   ├── Search/
│   └── Consistency/
├── Infrastructure/
│   ├── Persistence/
│   ├── AI/
│   ├── Authentication/
│   ├── BackgroundJobs/
│   └── Observability/
└── Common/
```

Each module may contain `Domain`, `Features`, `Presentation`, and `Infrastructure` folders as its
implementation appears. Do not create empty architectural layers when a module does not need them.
