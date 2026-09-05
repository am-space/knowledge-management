# Web frontend

The web client uses React, TypeScript, and Vite. Material UI is the base component system, with a
custom neutral and compact theme rather than an unmodified default Material appearance.

## Planned application shell

```text
┌──────────────────────────────────────────────────────────────┐
│ Workspace │ Search │ Create knowledge │ Account              │
├────────────────┬───────────────────────────┬─────────────────┤
│ Knowledge tree │ Markdown editor / preview │ Context panel   │
│ Favorites      │                           │ Relations       │
│ Recent         │                           │ Dependencies    │
│ Archived       │                           │ Consistency     │
├────────────────┴───────────────────────────┴─────────────────┤
│ Save state │ Revision │ Background status                   │
└──────────────────────────────────────────────────────────────┘
```

## Component direction

Material UI provides the application shell, forms, dialogs, drawers, menus, tabs, lists, alerts,
tooltips, progress indicators, and theming. MUI X Community Tree View is the initial hierarchy
candidate. Do not depend on Pro-only reordering, lazy loading, or virtualization without a separate
licensing and product decision.

Specialized components should be selected when the corresponding feature is implemented:

- an exact Markdown editor before introducing rich-text/Markdown round-trip conversion;
- an optional interactive graph view after graph navigation proves useful;
- a revision diff view when revision comparison is implemented.

CodeMirror and React Flow are current candidates, not accepted dependencies.

## Milestone 1 local Article workflow

The local profile opens directly into its automatically resolved personal workspace. The initial
Article tree is a browser-local index of IDs returned by successful creates; it reloads the current
Article representation through `GET /api/articles/{id}` and does not cache knowledge content. This
temporary index is necessary because the Milestone 1 HTTP contract has no collection endpoint.

Article source is edited as an exact multiline string and previewed with `react-markdown`. Preview
rendering neither normalizes nor replaces the source, and raw HTML is not enabled. Successful reads
and saves adopt the server response, including its concurrency version. Editing and navigation are
disabled during saves; opening an Article temporarily disables editing and saving, and only the
latest navigation request can update the editor. Opening any Article (including reloading the
selected Article) or starting a new draft asks for discard confirmation when there are unsaved
changes. Cancel keeps the draft so it can be saved before navigating.

Startup tree results merge with Articles created while loading. Failed requests display an error
and can be retried without reloading the page; successfully loaded Articles remain available.
Retries request only the failed IDs, which stay in the browser index unless the server returns
not found. Empty Markdown is accepted in both source and preview modes. A `409` preserves the
draft and offers an explicit reload of the current server revision.

Browser index persistence failures display a separate warning without treating successful server
writes as failed saves. The current session retains the saved Article and revision, but the tree
may be incomplete after reloading if browser storage is unavailable.

## Client boundaries

- Business invariants and workspace authorization remain on the server.
- Frontend validation provides feedback but is not the only enforcement.
- API types must match HTTP contracts, including optionality and error semantics.
- Touched flows must handle loading, empty, error, authorization, keyboard, accessibility, and
  narrow-screen states.
- Do not add a global state-management library until application state demonstrates a concrete need.

See [ADR-0003](adr/0003-react-and-material-ui-web-client.md).
