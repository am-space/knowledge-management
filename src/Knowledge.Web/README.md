# Knowledge.Web

The React, TypeScript, Vite, and Material UI client provides the local Article workflow. A user can
create and reopen Articles from a browser-local knowledge tree, edit exact Markdown, preview it,
and save revisioned changes through the typed HTTP client. During development, Vite proxies
`/health` and `/api` to `http://localhost:5080`.

```bash
npm run dev --prefix src/Knowledge.Web
```

Production assets are created with `npm run build --prefix src/Knowledge.Web`. Deployment hosting
is intentionally deferred until a reproducible application image is introduced.

Because the Milestone 1 HTTP contract has no collection route, the tree stores only previously
created Article IDs in local browser storage and reloads their current representations from the
server. Article content is never stored in browser storage. Clearing site data clears this index,
not the server-side Articles.

Material UI is the base component system. The initial application shell is expected to provide a
workspace selector, searchable knowledge tree, Markdown editor and preview, contextual relation and
consistency panels, revision history, and accessible light and dark themes.

The initial editor uses a labelled multiline text field so source round-trips without conversion,
and `react-markdown` renders the preview without enabling raw HTML. CodeMirror remains a candidate
for a later, more capable editor, and React Flow remains a candidate for an optional graph view.
