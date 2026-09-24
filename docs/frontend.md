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
rendering neither normalizes nor replaces the source, and raw HTML is not enabled. Preview links
open in a new tab, leaving the editor and its draft intact. Starting a new draft moves keyboard
focus to Title. Successful reads and saves adopt the server response, including its concurrency
version. Editing and navigation are
disabled during saves; opening an Article temporarily disables editing and saving, and only the
latest navigation request can update the editor. Opening any Article (including reloading the
selected Article) or starting a new draft asks for discard confirmation when there are unsaved
changes. Cancel keeps the draft so it can be saved before navigating.

The tree loads at most four indexed Articles concurrently and displays each result as it arrives,
merging with Articles created while loading. It tracks the number of remaining requests and
times out each request after 15 seconds so stalled requests become retryable errors. Failed
requests display an error and can be retried without reloading the page; successfully loaded
Articles remain available.
Retries request only the failed IDs, which stay in the browser index unless the server returns
not found. Empty Markdown is accepted in both source and preview modes. A `409` preserves the
draft and offers an explicit reload of the current server revision.

Browser index persistence failures display a separate warning without treating successful server
writes as failed saves. The current session retains the saved Article and revision, but the tree
may be incomplete after reloading if browser storage is unavailable.

## Milestone 2 navigation contract (not yet implemented)

The accepted [HTTP contract](knowledge-contracts.md#milestone-2-workspaces-and-navigation-contract)
and [ADR-0005](adr/0005-authorized-workspace-routes-and-initial-hierarchy.md) replace browser-index
discovery with authorized server listing. The behavior above remains the shipped Milestone 1 UI
until the navigation delivery implements these rules.

### Startup, reload, and discovery

1. Load the owned workspace list and its `defaultWorkspaceId`. All workspace pages must be reachable
   in the selector. With no remembered selection, load that default via its scoped routes, not the
   first item in creation order. A remembered ID can be checked directly even if its list item is on
   a later page.
2. Validate a remembered workspace with `GET /api/workspaces/{workspaceId}`. On `404`, explain that
   the remembered workspace is unavailable and offer an explicit action to open the default or
   another owned workspace. On network/server error offer retry; on `403` show access denied.
   Never reinterpret a failed explicit selection as default-workspace content. A malformed stored
   preference can be discarded locally with a notice and the same explicit selection action.
3. Load roots with the scoped collection endpoint. Expand a node to load its direct children.
   Display accessible load-more controls wherever `nextCursor` is non-null, including the workspace
   selector. Empty lists, loading, and failed pages have distinct states. A failed next page retains
   previous items and retries that page; a rejected cursor offers refresh from the beginning.
4. Optional preferences may store workspace/Article IDs and expanded-node IDs, keyed by workspace;
   never rely on them for discovery or store Markdown in the old index. Storage failure does not
   fail a server write or prevent navigation. Clear or ignore the old browser Article index; do not
   import or delete server content based on its entries.
5. On reload, reauthorize the remembered workspace and read the remembered Article through its
   scoped route. The Article's `parentId` and parent Article reads can reconstruct the path to a
   root without an ancestor endpoint. Guard against repeated IDs; if a path is unavailable, report
   the failure and refresh navigation instead of looping. Load the relevant child pages to reveal
   that path. Without preferences, all nested Articles remain reachable by expanding from roots.
   A missing remembered Article clears only that selection, reports not found, and leaves the
   workspace tree available. Network failures retain a retryable selection.

Tree ordering follows the server's creation-time/ID ordering. Merge pages by ID, not title; edits
update labels without reordering. A successful Article create uses the returned scoped identity and
refreshes the affected root/child list, marks its parent expandable, and opens the saved Article.
If its position lies beyond loaded pages, retain it as the selected Article and load pages as needed
to reveal it; never treat a partial page as the whole collection. Workspace create returns an owned
workspace and may select it through the same draft-protected flow. Rename updates the label without
changing selection. List/read failures after successful creates do not turn those writes into failed
saves or cause automatic duplicate POSTs.

### Draft and asynchronous request safety

The active selection includes workspace ID and, when applicable, Article ID and revision version.
All Article reads, creates, updates, and child lists use scoped routes. Never use legacy routes as a
fallback for a failed scoped request. New child drafts retain their intended workspace and parent;
changing navigation cannot retarget a draft's eventual save.

Workspace/Article switching, starting a new root/child draft, and explicit content reload must share
the existing discard confirmation behavior. Cancel leaves selection, exact Markdown, version, and
pending draft parent intact. A user can cancel, save in the original workspace, and then navigate.
Saving disables selection changes until completion. A failed save or revision conflict retains the
draft; conflict reload requires the same discard decision. Browser reload/tab close uses the browser's
unsaved-changes prompt where supported; durable draft recovery is not promised by this milestone.
Expanding/collapsing or paging the tree does not replace editor content or require discard.

After discard is approved, validate the destination before committing selection. If validation or
loading fails, retain the previous selection and draft and show the destination error. On success,
adopt the new workspace/Article atomically and clear old workspace tree/error state. Show the active
workspace explicitly so duplicate names are not treated as identical identities; the selector must
provide an ID-based disambiguator when names repeat.
While a destination is loading, disable editor changes/saves so edits cannot appear after the
discard decision; a newer navigation attempt supersedes the pending one using the same rules.

Fence every asynchronous result by workspace ID and a navigation generation; Article reads also
match the selected Article, and tree pages match parent and list generation. Abort obsolete work
where possible, but also ignore its eventual success/error callbacks. Include generations so an
old response for workspace A cannot win after A → B → A. A late workspace rename/list response must
not switch the editor, and a late tree summary must not replace a newer saved title/version.
Navigation errors never populate another workspace with retained tree or editor content.

Use typed clients for the documented success and Problem Details shapes. Keep authorization and
parent rules on the server. Loading, empty, retry, access denied, not found, validation, saving,
conflict, and discard states must have accessible labels, keyboard actions, and focus management.

## Browser verification

`scripts/verify.sh --e2e` runs Chromium against the actual Vite client, ASP.NET Core server, and an
isolated temporary SQLite database. It covers create, exact source/preview round trips, browser
reload and tree reopening, revision saves, a real concurrent writer, conflict draft preservation,
discard cancellation, and a cleared Markdown body. Component tests retain focused asynchronous
loading, navigation, storage-failure, and accessibility coverage.

## Client boundaries

- Business invariants and workspace authorization remain on the server.
- Frontend validation provides feedback but is not the only enforcement.
- API types must match HTTP contracts, including optionality and error semantics.
- Touched flows must handle loading, empty, error, authorization, keyboard, accessibility, and
  narrow-screen states.
- Do not add a global state-management library until application state demonstrates a concrete need.

See [ADR-0003](adr/0003-react-and-material-ui-web-client.md).
