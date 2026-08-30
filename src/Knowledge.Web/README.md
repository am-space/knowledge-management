# Knowledge.Web

The React, TypeScript, Vite, and Material UI client is a minimal application shell that reports
server readiness. During development, Vite proxies `/health` to `http://localhost:5080`.

```bash
npm run dev --prefix src/Knowledge.Web
```

Production assets are created with `npm run build --prefix src/Knowledge.Web`. Deployment hosting
is intentionally deferred until a reproducible application image is introduced.

This foundation will grow into the planned product client.

Material UI is the base component system. The initial application shell is expected to provide a
workspace selector, searchable knowledge tree, Markdown editor and preview, contextual relation and
consistency panels, revision history, and accessible light and dark themes.

Evaluate specialized dependencies when their features are implemented. Current candidates include
CodeMirror for exact Markdown editing and React Flow for an optional graph view; neither is an
accepted dependency yet.
