# Testing and verification

`scripts/setup.sh` and `scripts/verify.sh` are the canonical local and CI entry points. Setup
restores .NET packages and the EF CLI, installs both locked npm dependency sets, and downloads
Playwright's Chromium headless shell. Full verification builds the server, runs unit tests, lints,
type-checks, tests and builds the frontend, runs SQLite/PostgreSQL integration tests, and executes
the browser workflow.

```bash
scripts/setup.sh
scripts/verify.sh --all
```

Linux machines also need Chromium system libraries. Install them once when missing; CI includes
this step explicitly:

```bash
npm run install-browser --prefix tests/Knowledge.E2E.Tests -- --with-deps
```

## Verification lanes

| Mode | Coverage and prerequisites |
| --- | --- |
| `--backend` | .NET build/compiler analysis and unit tests |
| `--frontend` | ESLint, TypeScript, Vitest component tests, and Vite production build |
| `--integration` | .NET build and provider/HTTP tests; Docker Compose or `KNOWLEDGE_TEST_POSTGRES` required |
| `--e2e` | .NET build and Chromium workflow against Vite, HTTP, and a temporary SQLite database |
| `--all` | All of the above; the full CI lane invokes this same command |

Run setup before verification. The integration lane starts the Compose PostgreSQL service unless
`KNOWLEDGE_TEST_POSTGRES` supplies an isolated test database. Tests drop and recreate that database;
never point this setting at a development or production store containing data you need.

The browser lane owns server processes on loopback ports 5081 and 5174 and fails if those ports are
already occupied. It never reuses a running development server. Each run creates a temporary SQLite
file, starts the server with the local profile, and cleans up the processes and database on exit.
Playwright's fresh browser context starts with empty storage. No Article HTTP response is mocked.
Failure traces and screenshots are written under `tests/Knowledge.E2E.Tests/test-results/`, ignored
by Git, and uploaded on CI failure. They contain synthetic test data.

## Executable evidence

| Guarantee | Test suite |
| --- | --- |
| No-login local owner/workspace bootstrap and repeat initialization | `LocalWorkspaceInitializerTests`, Chromium readiness and first create |
| Create, exact Markdown preview, reopen after browser reload, edit and save | `tests/Knowledge.E2E.Tests/articles.spec.js` |
| Real conflicting writer, draft retained, canceled discard, reload, empty-body save | Same Chromium workflow, plus focused `App.test.tsx` cases |
| Immutable history, revision sequence and current pointer on both providers | `ArticleServiceTests.Lifecycle_PreservesHistoryConcurrencyAndWorkspaceIsolation` |
| Stale version rejection and two competing writers commit exactly one revision | `ArticleServiceTests` lifecycle and concurrent-update tests on both providers |
| Injected create/update persistence failure and cancellation leave no partial writes | `ArticleServiceTests`, `ArticleEndpointTests`, and `KnowledgeMigrationTests` |
| Two workspaces cannot read or update each other's Articles | Application tests on both providers; HTTP tests with two persisted owners/workspaces and unchanged-data checks |
| Database rejects cross-workspace revision and current-pointer references | `KnowledgeMigrationTests` on SQLite and PostgreSQL |
| Empty-to-latest migrations, repeat migration, no pending model changes | `KnowledgeMigrationTests` on both providers |
| HTTP success/error shapes, validation, cancellation and denied hosted context | `ArticleEndpointTests` |
| Content-bearing database failures produce safe logs and generic responses | `ArticleEndpointTests.PersistenceFailure_RedactsLogsAndResponseAndRollsBack`; middleware unit tests |
| Asynchronous tree/editor races, failed loading retry and browser-storage failures | `src/Knowledge.Web/src/App.test.tsx` |

Relational constraints reinforce write integrity; they do not replace workspace-filtered reads or
implement PostgreSQL row-level security. HTTP tests inject a trusted second workspace at the host
boundary; public requests cannot choose a workspace. This is not hosted authentication coverage.

## Test boundaries

.NET test projects live under `tests/`. Frontend component tests live beside their React source.
Browser tests and their independent Playwright dependencies live in `tests/Knowledge.E2E.Tests/`.
Architecture tests remain a reserved directory. MCP, hosted authentication, search, background work,
and AI processing are deferred and have no executable tests yet.

Run focused checks while iterating and full verification before delivery. Never report unavailable,
skipped, or sandbox-blocked checks as passed. A local pass does not establish CI success; the
completing pull request must pass CI before closing the milestone.
