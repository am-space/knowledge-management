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

## Milestone 2 contract review and required coverage

[Issue #21](https://github.com/am-space/knowledge-management/issues/21) specifies the
[new contracts](knowledge-contracts.md#milestone-2-workspaces-and-navigation-contract) and
[browser behavior](frontend.md#milestone-2-navigation-contract-not-yet-implemented).
The following scenario walkthrough defines expected results for that documentation decision.
It is not executable evidence that Milestone 2 is implemented. Workspace and Article implementation
deliveries must add provider/application/HTTP tests; the frontend delivery adds component and browser
tests. Integrated verification does not replace those feature-level checks.

| #21 criterion / scenario | Required result |
| --- | --- |
| AC-001: Create, list, rename, select owned workspace | Trimmed 1–200-unit name; duplicate names allowed; atomic workspace/owner membership creation; unchanged identity on rename; scoped request selects only an owned workspace |
| AC-001/002: Missing vs another owner's workspace | Same `404 workspace-not-found`, no identifying data; list includes only owned workspaces; denied trusted actor gives `403` |
| AC-002: A/B requests interleave under one owner | Each operation uses its own authorized workspace context; no global selection or cross-workspace reads/writes |
| AC-002: Viewer/editor membership without ownership | Personal-workspace list excludes it; selection/rename return the same `404` as an absent workspace |
| AC-003: Existing client after workspace create/rename/select | Legacy routes and Location/response/error shapes unchanged; still target the original default; original IDs and revisions survive upgrade |
| AC-003: Nested node read/update via legacy default route | Content can be read/updated by ID; parent is unchanged, including when an unknown parent field is sent |
| AC-004: Root vs child query | Omitted parent lists roots; supplied parent lists only direct children; valid empty parent gives `200` empty page, absent/foreign/inactive/non-Article parent gives identical `404` |
| AC-004: More than 100 items, equal timestamps, rename/title edit | Each page is bounded; canonical ID breaks time ties identically on both providers; following cursors reaches all items exactly once without mutation; edits do not change order |
| AC-004: Invalid/mismatched cursor or concurrent insertion | Invalid token/scope/page size gives field-specific `400` after authorization; reauthorize every page; live-read insertion behavior matches contract and refresh reveals earlier inserts |
| AC-004: Cross-workspace node/cursor/child indicator | No foreign summary, title, revision version, or `hasChildren` information is returned; request authorization and explicit query scope cannot be replaced by token state |
| AC-005: Root/child create and invalid parent | Null/omitted parent creates root; eligible parent commits with node/revision 1/pointer; invalid syntax is `400`, absent/foreign/ineligible parent is `404`; failure leaves no partial writes |
| AC-005: Self-parent and attempted reparent | Domain rejects self-parent; both-provider constraints reject self/cross-workspace edges; scoped update rejects parent input without a content write; legacy updates cannot mutate it |
| AC-005: Provenance and upgrades | Initial parent is attributed by immutable node creator/time; later revisions retain it; old roots keep null parents and original revision history |
| AC-006: Fresh browser or cleared/unavailable storage | Authorized default loads; all existing roots/children and workspace pages are reachable without stored IDs; nested Articles can be reopened after reload |
| AC-006: Stale preference or destination load failure | Explicit unavailable workspace never silently falls back; show retry or explicit selection action; failed navigation preserves prior selection/draft |
| AC-006: Dirty draft, failed/conflicting save, A → B → A | Cancel preserves exact draft and scope; saves cannot retarget; obsolete callbacks cannot update the active editor/tree, even after returning to A |
| AC-007: Durable decision and references | ADR-0005 extends new-route selection while ADR-0004 remains unchanged; contracts, local-mode, frontend, schema guidance, and index agree on implemented vs pending behavior |

Exact HTTP tests must assert success fields, null/omitted distinctions, canonical IDs, scoped
Location values, Problem Details types and extensions, and error privacy. Both-provider tests must
cover persisted ownership, foreign-workspace rejection, rollback, cursor ties, parent constraints,
and upgrade data preservation. Hosted PostgreSQL requests remain denied without a trusted context.
Chromium must verify fresh/cleared-storage discovery, pagination, nested creation/reload, workspace
selection, and draft protection through real HTTP and SQLite.

## Test boundaries

.NET test projects live under `tests/`. Frontend component tests live beside their React source.
Browser tests and their independent Playwright dependencies live in `tests/Knowledge.E2E.Tests/`.
Architecture tests remain a reserved directory. MCP, hosted authentication, search, background work,
and AI processing are deferred and have no executable tests yet.

Run focused checks while iterating and full verification before delivery. Never report unavailable,
skipped, or sandbox-blocked checks as passed. A local pass does not establish CI success; the
completing pull request must pass CI before closing the milestone.
