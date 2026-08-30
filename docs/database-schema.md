# Conceptual database schema

This document describes the provider-neutral relational model. It is not yet an EF Core mapping or
migration specification.

## Identity and tenancy

```text
User
  └── Membership ──> Workspace
                         └── tenant-owned knowledge
```

### User

```text
Id
DisplayName
CreatedAt
```

Authentication-specific fields should be modeled only after the authentication approach is chosen.

### Workspace

```text
Id
Name
CreatedAt
CreatedBy
```

### Membership

```text
WorkspaceId
UserId
Role
JoinedAt
```

The initial roles are expected to be owner, editor, and viewer. The exact permission matrix remains
to be specified before authorization is implemented.

## Knowledge

### KnowledgeNode

```text
Id
WorkspaceId
ParentId
Type
CurrentRevisionId
Status
CreatedAt
CreatedBy
```

The node carries stable identity and current structural state. A node cannot be its own parent;
hierarchy cycles and cross-workspace parents are invalid.

### KnowledgeRevision

```text
Id
WorkspaceId
NodeId
Version
Title
ContentMarkdown
Source
CreatedAt
CreatedBy
```

Revisions are immutable. `(NodeId, Version)` must be unique and the current-revision pointer must be
updated transactionally. Concurrent update semantics must be specified before implementation.

### KnowledgeRelation

```text
Id
WorkspaceId
SourceNodeId
TargetNodeId
Type
CreatedAt
CreatedBy
```

Both endpoints must belong to the same workspace. Relation uniqueness, allowed self-relations, and
whether relation changes are versioned or audited remain explicit pre-implementation decisions.

## Derived and consistency data

Future records include embeddings, change sets, consistency reports, and consistency issues. Every
derived artifact must include the source revision identity and workspace scope needed to reject stale
or cross-workspace results.

## Provider behavior

| Capability | PostgreSQL server | SQLite local |
| --- | --- | --- |
| Hierarchy truth | `ParentId` | `ParentId` |
| Hierarchy optimization | Optional derived `ltree` path | Recursive CTE |
| Keyword search | PostgreSQL full-text search | FTS5 |
| Vector search | `pgvector` | Unavailable until explicitly implemented |
| Tenant isolation | Application checks; possible future RLS | Application checks in a single-process profile |
| Migrations | Provider-specific EF Core migrations | Provider-specific EF Core migrations |

Database constraints and indexes must reinforce domain invariants on both providers. PostgreSQL must
not be reduced to SQLite's lowest common denominator.

