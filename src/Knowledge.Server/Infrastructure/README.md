# Shared infrastructure

Cross-module technical capabilities live here when they do not belong to one business module:

- relational persistence and provider selection;
- AI and embedding provider adapters;
- authentication;
- background execution and reliable post-commit handoff;
- observability.

Provider SDK and database dialect types must not leak into Domain or public HTTP/MCP contracts.

