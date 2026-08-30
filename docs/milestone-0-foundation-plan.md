# Milestone 0 — Executable Foundation Plan

- **Status:** Active
- **Date:** 2026-08-30
- **Parent:** [Knowledge Core MVP Plan](backlog/knowledge-core-mvp-plan.md)
- **Owner:** Project maintainers

## Goal

Turn the documentation-only repository into a reproducible, executable foundation with a minimal
ASP.NET Core server, React/MUI client, SQLite local profile, PostgreSQL development profile, automated
verification, and CI—without implementing knowledge-domain behavior prematurely.

## Completion outcome

From a clean checkout, a developer can prepare dependencies, run the local application without an
external database, optionally run against PostgreSQL, load the React client, verify client-to-server
connectivity, and execute one canonical verification command locally and in CI.

## Scope

### Included

- .NET 10 solution and shared build configuration.
- ASP.NET Core server project.
- React, TypeScript, Vite, and Material UI client project.
- Unit and integration test projects.
- SQLite and PostgreSQL provider registration and configuration.
- Minimal readiness/health behavior proving the selected profile can start and connect.
- PostgreSQL development container configuration.
- Canonical setup and verification scripts.
- GitHub Actions verification workflow.
- Configuration, secret, generated-file, and local-database ignore rules.
- Documentation updates that replace bootstrap assumptions with real commands.

### Excluded

- Workspace, user, membership, knowledge node, revision, relation, or search entities.
- Authentication or authorization.
- EF Core migrations with empty or placeholder schemas.
- HTTP or MCP product operations.
- Markdown editing or a knowledge-tree UI.
- Embeddings, AI providers, consistency checking, and background jobs.
- Production deployment automation.

The first provider-specific migrations belong to Milestone 1 with the first real schema.

## Existing decisions

- [ADR-0001](adr/0001-feature-oriented-modular-monolith.md): one feature-oriented ASP.NET Core
  modular monolith.
- [ADR-0002](adr/0002-postgresql-server-and-sqlite-local-profiles.md): PostgreSQL server and SQLite
  local persistence profiles.
- [ADR-0003](adr/0003-react-and-material-ui-web-client.md): React, TypeScript, Vite, and Material UI.
- Production code lives under `src/`; .NET verification projects live under `tests/`.
- The repository uses `master` as its pull-request base and task-specific feature branches.

## Proposed project set

```text
Knowledge.sln

src/
├── Knowledge.Server/
│   └── Knowledge.Server.csproj
└── Knowledge.Web/
    └── package.json

tests/
├── Knowledge.Server.UnitTests/
│   └── Knowledge.Server.UnitTests.csproj
└── Knowledge.Server.IntegrationTests/
    └── Knowledge.Server.IntegrationTests.csproj
```

Do not create Architecture or E2E test projects until they have executable rules or workflows. Their
reserved directories may remain documentation-only through this milestone.

## Implementation plan

### 1. Pin toolchains and shared conventions

- Add `global.json` for the selected .NET 10 SDK feature band and patch roll-forward policy.
- Add `Directory.Build.props` with nullable reference types, implicit usings, deterministic builds,
  and shared language/build settings.
- Add `.editorconfig` for repository-wide text and C# conventions.
- Add `.node-version` for the supported Node.js LTS selected at implementation time.
- Expand `.gitignore` for .NET outputs, Node dependencies, frontend builds, local SQLite files,
  developer secrets, test artifacts, and IDE state.
- Record any package-manager choice; use npm unless a different choice is explicitly accepted.

Validation:

- `dotnet --version` resolves through `global.json`.
- Node and npm satisfy the documented versions.
- Generated outputs and local database files do not appear in `git status`.

### 2. Scaffold the ASP.NET Core host

- Create `Knowledge.sln` and `src/Knowledge.Server/Knowledge.Server.csproj` targeting .NET 10.
- Add a minimal `Program.cs` using explicit, readable service registration.
- Add OpenAPI support for development without defining product endpoints.
- Add liveness and readiness behavior with stable routes and response semantics.
- Add structured console logging and environment-specific configuration.
- Keep module directories documentation-only until Milestone 1 creates real behavior.

Validation:

- The server builds and starts with the Development environment.
- Liveness succeeds without a database dependency.
- Readiness reports the configured persistence profile accurately.
- Startup and health responses do not reveal connection strings or secrets.

### 3. Establish persistence profiles

- Add EF Core relational, SQLite, and Npgsql provider dependencies to the server.
- Add one provider-selection option with explicit supported values such as `Sqlite` and `PostgreSql`.
- Register the intended DbContext/provider through Infrastructure composition without exposing
  provider types to module contracts.
- Configure a safe default local SQLite path under a gitignored data directory.
- Configure PostgreSQL through environment or development configuration without committing secrets.
- Enable SQLite foreign-key enforcement and select an explicit connection/locking policy appropriate
  for the single-process local profile.
- Do not create an empty migration or placeholder entity merely to test registration.

Validation:

- Local mode starts with no PostgreSQL service and creates/connects only to the configured local file.
- PostgreSQL mode connects to the development container.
- Invalid provider values fail startup with an actionable configuration error.
- Readiness distinguishes unavailable storage from application liveness.

### 4. Add focused server tests

- Create UnitTests and IntegrationTests projects and add them to the solution.
- Select and document the .NET test stack; default to xUnit and FluentAssertions unless implementation
  constraints justify another choice.
- Add unit tests for provider-option parsing and validation where behavior exists.
- Add integration smoke tests that start the persistence registration against temporary SQLite and
  PostgreSQL databases.
- Use an isolated PostgreSQL test database or container; do not rely on a developer's persistent data.
- Ensure test cleanup is deterministic and does not remove user-owned database files.

Validation:

- Tests pass independently and through the canonical backend verification path.
- PostgreSQL tests report a clear prerequisite failure rather than silently skipping required
  coverage.
- SQLite and PostgreSQL tests prove which provider was actually selected.

### 5. Scaffold the React client

- Create a Vite React TypeScript project in `src/Knowledge.Web` without nesting another generated
  repository.
- Add Material UI, its supported styling engine, and the selected icon package.
- Establish strict TypeScript, lint, type-check, test, and production-build commands.
- Add a minimal themed application shell with light and dark theme support.
- Add a typed client for the server health/readiness endpoint and display a small connection state.
- Configure development proxying and production static-asset hosting deliberately.
- Add one focused component or client test for the health path; do not create product UI placeholders.

Validation:

- The client lints, type-checks, tests, and builds.
- The browser loads the shell and displays reachable/unreachable server states.
- No connection string, API key, or server secret is compiled into frontend assets.

### 6. Add local orchestration

- Add a PostgreSQL service to `compose.yaml` with a health check, persistent development volume, and
  non-production credentials configurable through environment variables.
- Add documented commands for local SQLite startup and PostgreSQL-profile startup.
- Add a production-oriented multi-stage Dockerfile only if it can build both server and client
  artifacts reproducibly during this milestone; otherwise record it as a follow-up rather than
  committing a nonfunctional placeholder.
- Keep local SQLite data and PostgreSQL volumes outside version control.

Validation:

- SQLite mode runs without Docker.
- PostgreSQL mode becomes ready after the container health check succeeds.
- Stopping and restarting the development container behaves as documented.

### 7. Create canonical scripts

Implement:

```text
scripts/setup.sh
scripts/verify.sh
```

`setup.sh` should restore .NET dependencies and install frontend dependencies reproducibly.

`verify.sh` should support focused modes and a full default, for example:

```text
--all
--backend
--frontend
--integration
```

The script should build before testing, preserve useful failure output, avoid hidden environment
mutation, and return a nonzero status on failure.

Validation:

- A clean checkout can run setup and full verification using only documented prerequisites.
- Focused modes execute only their intended verification lane.
- CI invokes these scripts rather than reimplementing their commands.

### 8. Add continuous integration

- Add a GitHub Actions workflow for pull requests and `master` pushes.
- Install the pinned .NET and Node toolchains.
- Start PostgreSQL only for jobs that require it.
- Run canonical setup and verification scripts.
- Cache dependencies only when cache keys include the relevant lockfiles and toolchain inputs.
- Upload useful test output on failure without exposing secrets.

Validation:

- The workflow passes on the milestone pull request.
- A deliberately failing local test or lint check also fails the corresponding CI lane during
  workflow validation, then is reverted.

### 9. Reconcile documentation and agent guidance

- Update the root `AGENTS.md` with real setup, run, test, and verification commands.
- Replace the bootstrap statements in `README.md`, `docs/testing.md`, and relevant directory READMEs.
- Document persistence profile configuration and local data location in `docs/local-mode.md`.
- Document PostgreSQL development startup without committing credentials.
- Review the implementation against all three accepted ADRs.
- Archive this plan only in the implementation pull request after every completion criterion is met.

## Acceptance criteria

- [ ] `Knowledge.sln` builds with the pinned .NET 10 SDK.
- [ ] The ASP.NET Core host starts in SQLite mode without external services.
- [ ] The server starts in PostgreSQL mode against the documented development container.
- [ ] Liveness and readiness have stable, tested behavior and do not expose secrets.
- [ ] The React/MUI shell loads and reports server connection state.
- [ ] Frontend lint, type-check, tests, and production build pass.
- [ ] Unit and provider integration tests pass.
- [ ] `scripts/setup.sh` prepares a clean checkout.
- [ ] `scripts/verify.sh --all` is the canonical full verification and passes locally and in CI.
- [ ] Generated outputs, dependencies, local databases, test results, and secrets remain untracked.
- [ ] No knowledge-domain placeholder entities or empty migrations are introduced.
- [ ] Repository and architecture documentation matches the implemented foundation.

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| PostgreSQL and SQLite configuration drifts before domain code exists | Prove selection and connectivity in provider-specific integration smoke tests |
| Milestone becomes a framework-building exercise | Keep health connectivity as the only vertical behavior and reject speculative abstractions |
| CI duplicates local commands | Make scripts canonical and call them directly from workflows |
| Empty migrations create misleading schema history | Generate the first migrations only with the first real Milestone 1 schema |
| Local data or secrets enter Git | Use safe defaults, environment configuration, and explicit ignore/validation checks |
| Frontend scaffolding adds unnecessary dependencies | Install only React/Vite/MUI plus dependencies needed for lint, type-check, and focused testing |
| Integration tests become dependent on developer state | Use isolated temporary SQLite files and ephemeral PostgreSQL databases or containers |

## Verification matrix

| Area | Focused check | Final check |
| --- | --- | --- |
| Shared .NET configuration | Restore and build solution | `scripts/verify.sh --backend` |
| SQLite provider | Isolated SQLite integration smoke test | `scripts/verify.sh --integration` |
| PostgreSQL provider | Isolated PostgreSQL integration smoke test | `scripts/verify.sh --integration` |
| React client | Lint, type-check, focused test, build | `scripts/verify.sh --frontend` |
| Local orchestration | Manual SQLite and PostgreSQL startup smoke tests | Documented completion checklist |
| Full repository | `git diff --check`, status, link inspection | `scripts/verify.sh --all` and green CI |

## Definition of done

Milestone 0 is complete only when every acceptance criterion is satisfied, the canonical full
verification passes locally and in CI, both persistence profiles are proven without placeholder
schema, the final diff contains no generated or sensitive files, and the documentation describes the
foundation as it actually exists. In the implementation pull request, record any newly durable
decision in an ADR and archive this plan according to [`docs/AGENTS.md`](AGENTS.md).

