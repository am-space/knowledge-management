# Milestone 1 — Local Article delivery plan

- **Status:** Active
- **Parent outcome:** [Issue #5](https://github.com/am-space/knowledge-management/issues/5)
- **Roadmap:** [Knowledge Core MVP](backlog/knowledge-core-mvp-plan.md)
- **Verification and delivery:** [Issue #12](https://github.com/am-space/knowledge-management/issues/12)

## Scope and outcome

A local user starts without login, enters a provisioned personal workspace, creates an Article,
edits and previews exact Markdown, saves immutable revisions, and reopens the Article from the tree.
SQLite is the local profile; PostgreSQL must preserve equivalent revision and transaction behavior.
This is the approved Milestone 1 slice extracted from the broader MVP roadmap. GitHub issues hold
ownership and delivery status; this document holds the durable scope and exit criteria.

## Sequencing and decisions

The external contract and trusted workspace context precede schema and application implementation.
Generated provider migrations establish stable node identity, immutable revisions, and workspace
constraints. Local bootstrap supplies trusted identities. The Article service owns transactional
create/update and concurrency behavior; HTTP maps its results and React consumes that contract.
Final verification crosses browser, HTTP, application, and persistence boundaries.

The accepted decisions are [ADR-0001](adr/0001-feature-oriented-modular-monolith.md),
[ADR-0002](adr/0002-postgresql-server-and-sqlite-local-profiles.md),
[ADR-0003](adr/0003-react-and-material-ui-web-client.md), and
[ADR-0004](adr/0004-explicit-revision-version-and-trusted-workspace-context.md).

## Exit criteria and review evidence

| Parent exit criterion | Evidence required for delivery |
| --- | --- |
| Local startup resolves owner and workspace without login | Bootstrap integration tests and the browser's first create against empty SQLite |
| Web create, reopen, edit, preview and save | Real Chromium workflow through Vite and HTTP; reload verifies the browser index and database read |
| Each accepted save appends a revision and atomically advances the pointer | Provider lifecycle tests and initial/create/update failure injection |
| Stale or competing saves leave no partial revision | Both-provider stale-version and concurrent-writer tests; browser conflict preservation |
| Shared application behavior behind HTTP and React | Thin `ArticleEndpoints` mapping to `ArticleService`, typed web HTTP client, vertical browser test |
| Equivalent PostgreSQL semantics | Provider lifecycle, rollback, cancellation, concurrency and migration tests |
| A second workspace cannot read or modify the first Article | Both-provider service tests, persisted two-workspace HTTP tests and relational constraint checks |
| Documentation and generated migrations match implementation | Living references, empty-to-latest migrations and pending-model-change checks |

The detailed executable mapping and diagnostic allowlist live in [testing](testing.md) and
[Article contracts](knowledge-contracts.md). The final change must pass `scripts/verify.sh --all`
locally and in CI. Review all parent criteria before closing issue #5 and the GitHub milestone.

## Constraints and exclusions

The tree is a temporary browser-local ID index because collection and hierarchy HTTP endpoints are
out of scope. It is not a complete server listing or content backup. Local mode is single-process;
the PostgreSQL HTTP host fails closed until hosted authentication supplies trusted identities.
Workspace selection, hosted authentication, hierarchy operations, relations, search, revision
restoration, MCP, import/export, embeddings, and AI workflows remain later milestones.

## Completion and archival

The completing pull request for issue #12 must link this plan, record local and CI verification,
and review the parent exit criteria. After those criteria pass, add its concrete PR link to the
completion banner, set this plan to `Completed`, and move it to `docs/archive/`, updating incoming
links. Keep the broader MVP roadmap in backlog because its later milestones are not delivered.
The foundation plan is already archived with its original completing PR #3.
