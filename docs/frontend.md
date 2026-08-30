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

## Client boundaries

- Business invariants and workspace authorization remain on the server.
- Frontend validation provides feedback but is not the only enforcement.
- API types must match HTTP contracts, including optionality and error semantics.
- Touched flows must handle loading, empty, error, authorization, keyboard, accessibility, and
  narrow-screen states.
- Do not add a global state-management library until application state demonstrates a concrete need.

See [ADR-0003](adr/0003-react-and-material-ui-web-client.md).

