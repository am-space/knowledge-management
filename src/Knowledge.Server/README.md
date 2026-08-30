# Knowledge.Server

The planned ASP.NET Core host and feature-oriented modular monolith.

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

