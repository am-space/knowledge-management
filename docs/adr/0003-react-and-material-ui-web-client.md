# ADR-0003: Use React and Material UI for the web client

- **Status:** Accepted
- **Date:** 2026-08-29
- **Deciders:** Project maintainers

## Context

The human-facing application will require a dense, interactive workspace: hierarchical navigation,
search, Markdown editing and preview, contextual metadata, revision history, consistency issues, and
an optional graph view. The same browser client must work against local SQLite and hosted PostgreSQL
profiles through stable server contracts.

The project needs an established component foundation without designing basic form, navigation,
overlay, feedback, and accessibility behavior from scratch.

## Decision

Build the web client with React, TypeScript, and Vite. Use Material UI as the base component system
and define a compact, neutral project theme with accessible light and dark modes.

Use MUI Core for common application components. MUI X Community Tree View is the initial hierarchy
candidate. Do not adopt Pro-only features or a commercial license without a separate decision.

Choose specialized editor, graph, and diff components when their features are implemented and tested
against the actual requirements. CodeMirror and React Flow are candidates, not accepted dependencies.
Do not add a global state-management library without a concrete need.

## Consequences

- The project gains a mature React component system and can reuse existing team experience.
- Local and hosted modes share one web client.
- A custom theme is required to avoid an unmodified generic Material appearance.
- MUI X licensing boundaries must be reviewed before relying on advanced tree functionality.
- Specialized knowledge-editor and graph dependencies remain replaceable until selected explicitly.

## Alternatives considered

- **Vue:** a strong TypeScript and Vite alternative, but not selected over the team's existing React
  and MUI experience.
- **Blazor:** attractive for C# end to end, but advanced browser editors and graph components would
  likely require additional JavaScript integration.
- **Svelte:** capable and compact, but offers less benefit than adopting the established React stack.
- **Angular:** provides strong conventions but is heavier than the initial project requires.
- **Razor Pages with HTMX:** suitable for server-centric forms and CRUD, but less natural for the
  planned graph- and editor-heavy workspace.
- **shadcn/ui:** highly customizable open component code, but not selected because MUI provides a
  more complete system and avoids maintaining a second set of component primitives.

## Related documentation

- [Web frontend](../frontend.md)
- [Architecture](../architecture.md)

