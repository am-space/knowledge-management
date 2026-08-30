# Source projects

Production code belongs under `src/`.

- `Knowledge.Server` is the ASP.NET Core modular monolith and hosts HTTP and MCP adapters.
- `Knowledge.Web` is the React and TypeScript web client.

Do not split Domain, Application, Infrastructure, and Presentation into separate assemblies merely
to reproduce horizontal Clean Architecture layers. Keep the main server feature-oriented and add an
assembly only when it represents a concrete deployment, provider, ownership, or build boundary.

